using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// 1.0.32.13 body-reaction voice addon. Separate from the locked combat voice (1.0.32.6): own clip folders, own prefabs,
    /// own law (BodyVoicePolicy). It only listens to the combat voice's public Spoke event to stay quiet around combat lines
    /// and to stop its own line when one starts. Clips: voices/body/&lt;male|female&gt;/&lt;onset|repeat|recover&gt;/*.wav, licensed
    /// Gamemaster Human Male B / Female A (never committed to GitHub). Missing pools are logged and stay silent.
    ///
    /// 1.0.32.14 extends this same addon (no second controller, no status writer) with body states from ChatGPT's
    /// 2026-10-28 handoff, each on its native status owner: Burning status, Frost status, a long continuous swim
    /// (Character.IsSwimming), heavy-attack stamina running out (NpcBody), and one quiet exhale when burning/frost ends.
    /// Pools: voices/body/&lt;g&gt;/&lt;burning_onset|burning_repeat|frost|swim|exhaustion|env_recover&gt;. One voice per actor:
    /// poison and combat lines take precedence; state lines start only when the actor is silent. The poison path, its
    /// 40 clips and timings are unchanged.
    /// </summary>
    public static class BodyReactionVoice
    {
        static readonly string[] Pools = { "onset", "repeat", "recover" };
        static readonly string[] StatePools = { "burning_onset", "burning_repeat", "frost", "swim", "exhaustion", "env_recover" };
        static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        static readonly Dictionary<string, AudioClip[]> Clips = new Dictionary<string, AudioClip[]>();
        static readonly Dictionary<string, string[]> ClipNames = new Dictionary<string, string[]>();
        static readonly List<AudioClip> Keep = new List<AudioClip>();
        static readonly Dictionary<int, BodyVoicePolicy> Law = new Dictionary<int, BodyVoicePolicy>();
        static readonly Dictionary<int, float> LastCombat = new Dictionary<int, float>();
        static readonly Dictionary<int, GameObject[]> Playing = new Dictionary<int, GameObject[]>();
        static readonly Dictionary<int, ActorStates> States = new Dictionary<int, ActorStates>();
        static float nextTick, nextPrune;
        static bool auditionBusy;
        public static string Summary { get; private set; }
        public static int Lines { get; private set; }

        sealed class ActorStates
        {
            public readonly LastingStatePolicy Burning, Frost;
            public readonly EventStatePolicy Exhaustion, Swim;
            public readonly SwimTracker SwimTrack;
            public readonly BodyActorGate Gate = new BodyActorGate();
            public readonly ClipPicker Picker;
            public int LastExhaustions = -1;
            public string PlayingState;              // state whose line is sounding now (null = none / poison)
            public float PlayingUntil;
            public ActorStates(int id)
            {
                Burning = new LastingStatePolicy(id ^ 0x5a17, 6f, 9f, 3f);
                Frost = new LastingStatePolicy(id ^ 0x2f0c, 8f, 12f, 3f);
                Exhaustion = new EventStatePolicy(45f);
                Swim = new EventStatePolicy(30f);
                SwimTrack = new SwimTracker(CorePlugin.BodySwimSeconds, 1.5f);
                Picker = new ClipPicker(id ^ 0x77d1);
            }
        }

        public static void Build()
        {
            var parts = new List<string>();
            var stateParts = new List<string>();
            string root = Path.Combine(Path.GetDirectoryName(typeof(BodyReactionVoice).Assembly.Location) ?? "", Path.Combine("voices", "body"));
            GameObject template = PrefabManager.Instance.GetPrefab("sfx_goblin_hit");   // container only (ZSFX + ZNetView)
            foreach (string gender in new[] { "male", "female" })
            {
                foreach (string pool in Pools) LoadPool(root, template, gender, pool, parts);
                foreach (string pool in StatePools) LoadPool(root, template, gender, pool, stateParts);
            }
            Summary = String.Join(", ", parts.ToArray()) + "; states 1.0.32.14: " + String.Join(", ", stateParts.ToArray());
            CompanionCombatVoice.Spoke += OnCombatLine;
        }

        static void LoadPool(string root, GameObject template, string gender, string pool, List<string> parts)
        {
            string dir = Path.Combine(root, Path.Combine(gender, pool));
            string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.wav") : new string[0];
            Array.Sort(files, StringComparer.Ordinal);
            var clips = new List<AudioClip>(); var names = new List<string>();
            foreach (string f in files)
            {
                try { AudioClip c = WavClip.Load(f, false); if (c != null && c.length > 0f) { clips.Add(c); names.Add(Path.GetFileName(f)); } }
                catch (Exception e) { CorePlugin.Warn("body voice " + Path.GetFileName(f) + ": " + e.Message); }
            }
            if (clips.Count == 0 || template == null) { parts.Add(gender + " " + pool + " 0"); return; }
            string name = "LWC_sfx_body_" + gender[0] + "_" + pool;
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            if (prefab == null)
            {
                prefab = PrefabManager.Instance.CreateClonedPrefab(name, template);
                if (prefab != null && prefab.GetComponent<ZNetView>() != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
            }
            ZSFX sfx = prefab == null ? null : prefab.GetComponent<ZSFX>();
            if (sfx == null) { parts.Add(gender + " " + pool + " no ZSFX"); return; }
            sfx.m_audioClips = clips.ToArray();
            Keep.AddRange(clips);
            sfx.m_minPitch = sfx.m_maxPitch = 1f;
            float vol = Mathf.Min(1f, sfx.m_maxVol * CorePlugin.CompanionVoiceGain);
            sfx.m_minVol = sfx.m_maxVol = vol;
            string key = gender[0] + ":" + pool;
            Prefabs[key] = prefab;
            Clips[key] = clips.ToArray();
            ClipNames[key] = names.ToArray();
            parts.Add(gender + " " + pool + " " + clips.Count);
        }

        static void OnCombatLine(NpcKernel kernel, string key)
        {
            if (kernel == null || kernel.Body == null || kernel.Body.Human == null) return;
            int id = kernel.Body.Human.GetInstanceID();
            LastCombat[id] = Time.time;
            Stop(id);                                   // combat lines win: our line ends when one starts
        }

        static void Stop(int id)
        {
            ActorStates st;
            if (States.TryGetValue(id, out st)) { st.Gate.Cut(Time.time); st.PlayingState = null; }
            GameObject[] made;
            if (!Playing.TryGetValue(id, out made) || made == null) return;
            foreach (GameObject go in made)
            {
                if (go == null) continue;
                ZNetView view = go.GetComponent<ZNetView>();
                if (view != null && view.IsValid()) { if (view.IsOwner()) ZNetScene.instance.Destroy(go); }
                else UnityEngine.Object.Destroy(go);
            }
            Playing.Remove(id);
        }

        static bool Poisoned(Humanoid h)
        {
            SEMan se = h.GetSEMan();
            if (se == null) return false;
            foreach (StatusEffect e in se.GetStatusEffects()) if (e is SE_Poison) return true;
            return false;
        }

        static bool Has(Humanoid h, int hash)
        {
            SEMan se = h.GetSEMan();
            return se != null && se.HaveStatusEffect(hash);
        }

        /// <summary>Called from the plugin Update, four times a second; owner-side only.</summary>
        public static void Tick()
        {
            if (!CorePlugin.BodyVoiceEnabled || Time.time < nextTick) return;
            nextTick = Time.time + .25f;
            foreach (NpcKernel k in NpcKernel.LiveSet)
            {
                if (k == null || k.Body == null || k.Body.Human == null || !k.Body.IsOwner) continue;
                Humanoid h = k.Body.Human;
                int id = h.GetInstanceID();
                BodyVoicePolicy law;
                if (!Law.TryGetValue(id, out law)) { law = new BodyVoicePolicy(id); Law[id] = law; }
                float lastCombat;
                if (!LastCombat.TryGetValue(id, out lastCombat)) lastCombat = float.NegativeInfinity;
                bool dead = h.IsDead();
                if (dead) Stop(id);
                BodyLine line = law.Step(Time.time, !dead && Poisoned(h), dead, lastCombat);
                if (line != BodyLine.None)
                {
                    Say(k, h, id, line == BodyLine.Onset ? "onset" : line == BodyLine.Repeat ? "repeat" : "recover");
                    continue;                           // one line per actor per tick; poison first
                }
                StepStates(k, h, id, dead, lastCombat);
            }
            if (Time.time >= nextPrune) Prune();
        }

        static void Say(NpcKernel k, Humanoid h, int id, string pool)
        {
            NpcAppearance a = k.GetComponent<NpcAppearance>();
            char g = a != null && a.Female ? 'f' : 'm';
            GameObject prefab;
            if (!Prefabs.TryGetValue(g + ":" + pool, out prefab) || prefab == null)
            {
                if (CorePlugin.DevVoiceLog) CorePlugin.Report("BODYVOICE-SKIP " + h.GetHoverName() + " line=" + pool + " reason=no clip (" + (g == 'f' ? "female" : "male") + ")");
                return;                                  // no opposite-gender fallback
            }
            Stop(id);
            var list = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab, m_enabled = true, m_attach = true } } };
            Playing[id] = list.Create(h.transform.position, h.transform.rotation, h.transform);
            Lines++;
            // 1.0.32.14: body-state lines stay silent while a poison line sounds (longest clip of the pool).
            Gate(id).Started(Time.time, Longest(g + ":" + pool), false);
            if (CorePlugin.DevVoiceLog) CorePlugin.Report("BODYVOICE " + h.GetHoverName() + " (" + (g == 'f' ? "female" : "male") + ") line=poison-" + pool + " t=" + Time.time.ToString("0.00"));
        }

        // ---------------- 1.0.32.14 body states ----------------

        static BodyActorGate Gate(int id) { return Actor(id).Gate; }

        static ActorStates Actor(int id)
        {
            ActorStates st;
            if (!States.TryGetValue(id, out st)) { st = new ActorStates(id); States[id] = st; }
            return st;
        }

        static float Longest(string key)
        {
            AudioClip[] c; float m = 0f;
            if (Clips.TryGetValue(key, out c)) foreach (AudioClip a in c) if (a != null && a.length > m) m = a.length;
            return m;
        }

        static void StepStates(NpcKernel k, Humanoid h, int id, bool dead, float lastCombat)
        {
            ActorStates st = Actor(id);
            float now = Time.time;
            bool burning = !dead && Has(h, SEMan.s_statusEffectBurning);
            bool frost = !dead && Has(h, SEMan.s_statusEffectFrost);
            bool swimming = !dead && h.IsSwimming();
            int ex = k.Body.Exhaustions;
            bool exhausted = st.LastExhaustions >= 0 && ex > st.LastExhaustions;
            st.LastExhaustions = ex;

            // A state that ended takes its line with it (no stale cry after the fire is out).
            if (st.PlayingState != null && now < st.PlayingUntil &&
                ((st.PlayingState == "burning" && !burning) || (st.PlayingState == "frost" && !frost)))
                Stop(id);

            bool quiet = !dead && st.Gate.Quiet(now, lastCombat) && !Sounding(id);
            bool longSwimEnded = st.SwimTrack.Step(now, swimming, dead);

            // Every policy steps every tick (so timers stay true); at most one line starts, in this order.
            StateLine b = CorePlugin.BodyBurning ? st.Burning.Step(now, burning, dead, quiet) : StateLine.None;
            StateLine f = CorePlugin.BodyFrost ? st.Frost.Step(now, frost, dead, quiet && b == StateLine.None) : StateLine.None;
            bool free = quiet && b == StateLine.None && f == StateLine.None;
            StateLine s = CorePlugin.BodySwim ? st.Swim.Step(now, longSwimEnded, dead, free) : StateLine.None;
            StateLine e = CorePlugin.BodyExhaustion ? st.Exhaustion.Step(now, exhausted, dead, free && s == StateLine.None) : StateLine.None;

            if (b != StateLine.None) Voice(k, h, id, st, "burning", b);
            else if (f != StateLine.None) Voice(k, h, id, st, "frost", f);
            else if (s != StateLine.None) Voice(k, h, id, st, "swim", s);
            else if (e != StateLine.None) Voice(k, h, id, st, "exhaustion", e);
        }

        static bool Sounding(int id)
        {
            GameObject[] made;
            if (!Playing.TryGetValue(id, out made) || made == null) return false;
            foreach (GameObject go in made)
            {
                if (go == null) continue;
                AudioSource src = go.GetComponent<AudioSource>();
                if (src != null && src.isPlaying) return true;
            }
            return false;
        }

        static void Voice(NpcKernel k, Humanoid h, int id, ActorStates st, string state, StateLine line)
        {
            string pool;
            bool recover = line == StateLine.Recover;
            if (recover)
            {
                if (!CorePlugin.BodyEnvRecover || !st.Gate.RecoverAllowed(Time.time)) return;
                pool = "env_recover";
            }
            else if (state == "burning") pool = line == StateLine.Onset ? "burning_onset" : "burning_repeat";
            else pool = state;                          // frost, swim, exhaustion: one pool each

            NpcAppearance a = k.GetComponent<NpcAppearance>();
            char g = a != null && a.Female ? 'f' : 'm';
            string key = g + ":" + pool;
            GameObject prefab; AudioClip[] clips;
            if (!Prefabs.TryGetValue(key, out prefab) || prefab == null || !Clips.TryGetValue(key, out clips) || clips.Length == 0)
            {
                if (CorePlugin.DevVoiceLog) CorePlugin.Report("BODYVOICE-SKIP " + h.GetHoverName() + " line=" + state + "-" + LineName(line) + " pool=" + pool + " reason=no clip (" + (g == 'f' ? "female" : "male") + ")");
                return;                                  // silence, never the other gender
            }
            int i = st.Picker.Pick(clips.Length);
            Stop(id);
            var list = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab, m_enabled = true, m_attach = true } } };
            GameObject[] made = list.Create(h.transform.position, h.transform.rotation, h.transform);
            foreach (GameObject go in made)
            {
                ZSFX z = go == null ? null : go.GetComponent<ZSFX>();
                if (z != null) z.m_audioClips = new[] { clips[i] };   // ZSFX plays in Start, after this
            }
            Playing[id] = made;
            Lines++;
            st.Gate.Started(Time.time, clips[i].length, recover);
            st.PlayingState = recover ? null : state;
            st.PlayingUntil = Time.time + clips[i].length;
            if (CorePlugin.DevVoiceLog)
                CorePlugin.Report("BODYVOICE " + h.GetHoverName() + " (" + (g == 'f' ? "female" : "male") + ") line=" + state + "-" + LineName(line) +
                    " pool=" + pool + " clip=" + ClipNames[key][i] + " t=" + Time.time.ToString("0.00"));
        }

        static string LineName(StateLine l)
        {
            switch (l) { case StateLine.Onset: return "onset"; case StateLine.Repeat: return "repeat"; case StateLine.Recover: return "recover"; default: return "event"; }
        }

        static void Prune()
        {
            nextPrune = Time.time + 30f;
            var live = new HashSet<int>();
            foreach (NpcKernel k in NpcKernel.LiveSet)
                if (k != null && k.Body != null && k.Body.Human != null) live.Add(k.Body.Human.GetInstanceID());
            foreach (int id in new List<int>(States.Keys)) if (!live.Contains(id)) States.Remove(id);
            foreach (int id in new List<int>(Playing.Keys)) if (!live.Contains(id)) Playing.Remove(id);
        }

        // ---------------- lab audition ----------------

        /// <summary>lwbodyaudition list | &lt;pool&gt; [male|female]: plays every clip of one pool on the nearest companion of that
        /// gender, 3 s apart, and logs each file name so Brian can accept or reject clip by clip.</summary>
        public static void Audition(string[] args)
        {
            if (args.Length < 2 || args[1] == "list")
            {
                var have = new List<string>();
                foreach (var kv in ClipNames) have.Add(kv.Key + " " + kv.Value.Length);
                have.Sort(StringComparer.Ordinal);
                CorePlugin.Report("AUDITION pools: " + (have.Count == 0 ? "none loaded" : String.Join(", ", have.ToArray())) +
                    ". Usage: lwbodyaudition <pool> [male|female]");
                return;
            }
            if (auditionBusy) { CorePlugin.Report("AUDITION already running."); return; }
            string pool = args[1];
            bool female = args.Length > 2 && args[2].StartsWith("f", StringComparison.OrdinalIgnoreCase);
            string key = (female ? 'f' : 'm') + ":" + pool;
            if (!Clips.ContainsKey(key)) { CorePlugin.Report("AUDITION: no " + (female ? "female" : "male") + " clips in pool '" + pool + "'. Try lwbodyaudition list."); return; }
            Humanoid target = null; float best = float.MaxValue;
            Vector3 me = Player.m_localPlayer != null ? Player.m_localPlayer.transform.position : Vector3.zero;
            foreach (NpcKernel k in NpcKernel.LiveSet)
            {
                if (k == null || k.Body == null || k.Body.Human == null || !k.Body.IsOwner || k.Body.Human.IsDead()) continue;
                NpcAppearance a = k.GetComponent<NpcAppearance>();
                if ((a != null && a.Female) != female) continue;
                float d = Vector3.Distance(me, k.Body.Human.transform.position);
                if (d < best) { best = d; target = k.Body.Human; }
            }
            if (target == null) { CorePlugin.Report("AUDITION: no " + (female ? "female" : "male") + " companion loaded."); return; }
            CorePlugin.Instance.StartCoroutine(PlayAll(target, key));
        }

        static IEnumerator PlayAll(Humanoid h, string key)
        {
            auditionBusy = true;
            AudioClip[] clips = Clips[key]; string[] names = ClipNames[key]; GameObject prefab = Prefabs[key];
            int id = h.GetInstanceID();
            for (int i = 0; i < clips.Length; i++)
            {
                if (h == null || h.IsDead()) break;
                Stop(id);
                var list = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = prefab, m_enabled = true, m_attach = true } } };
                GameObject[] made = list.Create(h.transform.position, h.transform.rotation, h.transform);
                foreach (GameObject go in made)
                {
                    ZSFX z = go == null ? null : go.GetComponent<ZSFX>();
                    if (z != null) z.m_audioClips = new[] { clips[i] };
                }
                Playing[id] = made;
                Gate(id).Started(Time.time, clips[i].length + 3f, false);   // no live state line over the audition
                CorePlugin.Report("AUDITION " + key + " " + (i + 1) + "/" + clips.Length + " " + names[i] + " (" + clips[i].length.ToString("0.00") + " s) on " + h.GetHoverName());
                yield return new WaitForSeconds(Mathf.Max(3f, clips[i].length + 1.5f));
            }
            CorePlugin.Report("AUDITION " + key + " done.");
            auditionBusy = false;
        }
    }
}

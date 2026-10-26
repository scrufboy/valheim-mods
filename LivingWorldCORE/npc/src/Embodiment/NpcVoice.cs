using System;
using System.Collections.Generic;
using System.IO;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Companion voice from vanilla assets, the same recipe Souls of the Land's RRR NPCs use
    /// (verified in RRRCore Npc.DesignSfxDeepen): clone the Fuling sounds sfx_goblin_alerted /
    /// _hit / _death / _idle and lower their pitch range to 0.8-0.9. Presentation only: holds no
    /// behavior domain and decides nothing. Hit, death and idle are played by native code
    /// (Character hit/death effects, BaseAI idle timer); curious and battle-cry lines are played
    /// once on entering Readiness / Combat, at most every CueCooldown seconds.
    /// </summary>
    public static class NpcVoice
    {
        public const float MinPitch = .8f, MaxPitch = .9f, CueCooldown = 10f;
        static readonly string[] Sources = { "sfx_goblin_alerted", "sfx_goblin_hit", "sfx_goblin_death", "sfx_goblin_idle" };
        static readonly Dictionary<string, GameObject> Clips = new Dictionary<string, GameObject>();
        public static string Missing { get; private set; }

        public static void Build()
        {
            Clips.Clear();
            var missing = new List<string>();
            foreach (string source in Sources)
            {
                string name = "LWC_" + source.Replace("goblin_", "");
                GameObject clone = PrefabManager.Instance.GetPrefab(name);
                if (clone == null && PrefabManager.Instance.GetPrefab(source) != null)
                {
                    clone = PrefabManager.Instance.CreateClonedPrefab(name, source);
                    // 1.0.22: the Fuling sfx carry a ZNetView, so every line is a networked object with a
                    // saved record. Unregistered, the game could not resolve "LWC_sfx_alerted" on reload
                    // ("Missing prefab hash 1098461344") and the respawn flooded ZNetScene errors.
                    if (clone != null && clone.GetComponent<ZNetView>() != null)
                        PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, false));
                }
                if (clone == null) { missing.Add(source); continue; }
                ZSFX sfx = clone.GetComponent<ZSFX>();
                if (sfx != null) { sfx.m_minPitch = MinPitch; sfx.m_maxPitch = MaxPitch; }
                Clips[source.Substring("sfx_goblin_".Length)] = clone;
            }
            // War cry (1.0.23): the Fuling Berserker's taunt roar (vanilla sfx_goblinbrute_taunt; its clips are
            // human male screams), halfway between its native (beast) pitch and the rejected 1.0-1.1. Registered like the voice lines.
            GameObject cry = PrefabManager.Instance.GetPrefab(WarCryName);
            if (cry == null && PrefabManager.Instance.GetPrefab(WarCrySource) != null)
            {
                cry = PrefabManager.Instance.CreateClonedPrefab(WarCryName, WarCrySource);
                if (cry != null && cry.GetComponent<ZNetView>() != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(cry, false));
            }
            if (cry == null) missing.Add(WarCrySource);
            else
            {
                // 1.0.26 (Brian, live): the native Berserker roar is below human (beast-like); 1.0.23's 1.0-1.1
                // was too high (Fuling-like). Target: a deep human shout = halfway between native and 1.0-1.1.
                ZSFX sfx = cry.GetComponent<ZSFX>();
                if (sfx == null) WarCryPitch = "no ZSFX";
                else
                {
                    float nativeMin = sfx.m_minPitch, nativeMax = sfx.m_maxPitch;
                    sfx.m_minPitch = (nativeMin + TooHighMin) * .5f;
                    sfx.m_maxPitch = (nativeMax + TooHighMax) * .5f;
                    WarCryPitch = "native " + nativeMin + "-" + nativeMax + " -> " + sfx.m_minPitch + "-" + sfx.m_maxPitch;
                }
                Clips["warcry"] = cry;
            }
            BuildFemale();
            Missing = missing.Count == 0 ? "" : String.Join(", ", missing.ToArray());
        }

        // ---- Female voice set (1.0.27): Brian's licensed "Ultimate Female Voice Pack" (topmademarti), loaded from
        // voices/female/<line>/*.wav beside this DLL. Same five lines as the male set; female companions use it,
        // male companions keep the pitched Fuling set. Missing folder or line -> that line stays male. ----
        static readonly string[] FemaleLines = { "alerted", "hit", "death", "idle", "warcry" };
        public static string FemaleSummary { get; private set; }

        static void BuildFemale()
        {
            var parts = new List<string>();
            string root = Path.Combine(Path.GetDirectoryName(typeof(NpcVoice).Assembly.Location) ?? "", Path.Combine("voices", "female"));
            if (!Directory.Exists(root)) { FemaleSummary = "none (no voices/female folder)"; return; }
            GameObject template = PrefabManager.Instance.GetPrefab("sfx_goblin_hit");
            if (template == null) { FemaleSummary = "none (template sfx missing)"; return; }
            foreach (string line in FemaleLines)
            {
                string dir = Path.Combine(root, line);
                if (!Directory.Exists(dir)) continue;
                var clips = new List<AudioClip>();
                foreach (string file in Directory.GetFiles(dir, "*.wav"))
                {
                    try { clips.Add(WavClip.Load(file)); }
                    catch (Exception e) { CorePlugin.Warn("female voice " + Path.GetFileName(file) + ": " + e.Message); }
                }
                if (clips.Count == 0) continue;
                string name = "LWC_sfx_f_" + line;
                GameObject prefab = PrefabManager.Instance.GetPrefab(name);
                if (prefab == null)
                {
                    prefab = PrefabManager.Instance.CreateClonedPrefab(name, template);
                    if (prefab != null && prefab.GetComponent<ZNetView>() != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
                }
                ZSFX sfx = prefab == null ? null : prefab.GetComponent<ZSFX>();
                if (sfx == null) continue;
                sfx.m_audioClips = clips.ToArray();
                Loaded.AddRange(clips);   // strong references as well
                sfx.m_minPitch = FemaleMinPitch;
                sfx.m_maxPitch = FemaleMaxPitch;
                Clips["f:" + line] = prefab;
                parts.Add(line + " " + clips.Count);
            }
            FemaleSummary = parts.Count == 0 ? "none" : String.Join(", ", parts.ToArray());
        }

        public const float FemaleMinPitch = .97f, FemaleMaxPitch = 1.03f;   // human voice: natural variation only
        static readonly List<AudioClip> Loaded = new List<AudioClip>();
        static readonly HashSet<string> Reported = new HashSet<string>();

        /// <summary>A female line is usable only while its clips still exist; otherwise the male line plays (and it is logged once).</summary>
        static bool FemaleUsable(string key)
        {
            GameObject prefab;
            if (!Clips.TryGetValue("f:" + key, out prefab) || prefab == null) return false;
            ZSFX sfx = prefab.GetComponent<ZSFX>();
            int alive = 0;
            if (sfx != null && sfx.m_audioClips != null)
                foreach (AudioClip c in sfx.m_audioClips) if (c != null && c.length > 0f) alive++;
            if (!Reported.Contains(key))
            {
                Reported.Add(key);
                CorePlugin.Warn("female voice '" + key + "' first use: " + alive + " of " +
                    (sfx == null || sfx.m_audioClips == null ? 0 : sfx.m_audioClips.Length) + " clips alive" +
                    (alive == 0 ? " -> using the male line" : ""));
            }
            return alive > 0;
        }

        static bool IsFemale(NpcKernel kernel)
        {
            NpcAppearance a = kernel == null ? null : kernel.GetComponent<NpcAppearance>();
            return a != null && a.Female;
        }

        /// <summary>Once per load: a female companion's death cry uses the female set (the prefab is shared by both genders).</summary>
        internal static void ApplyGender(NpcKernel kernel)
        {
            if (!IsFemale(kernel) || !FemaleUsable("death")) return;
            Humanoid human = kernel.Body == null ? null : kernel.Body.Human;
            if (human != null) human.m_deathEffects = Voiced(human.m_deathEffects, "f:death");
        }

        const string WarCrySource = "sfx_goblinbrute_taunt", WarCryName = "LWC_sfx_warcry";
        public static string WarCryPitch { get; private set; }
        const float TooHighMin = 1f, TooHighMax = 1.1f;   // 1.0.23 setting, rejected live as Fuling-like

        /// <summary>The once-per-raid war cry line (no policy roll: WarCry decides).</summary>
        internal static void SayWarCry(NpcKernel kernel) { Say(kernel, "warcry"); }

        static EffectList List(string key)
        {
            GameObject clip;
            if (!Clips.TryGetValue(key, out clip)) return null;
            return new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = clip, m_enabled = true, m_attach = true } } };
        }

        /// <summary>Factory-time: death keeps the voice (native); ordinary hits are silent; no idle chatter.</summary>
        internal static void Configure(Humanoid human, MonsterAI ai)
        {
            human.m_hitEffects = Voiced(human.m_hitEffects, null);
            human.m_deathEffects = Voiced(human.m_deathEffects, "death");
            ai.m_idleSound = new EffectList();
            ai.m_idleSoundChance = 0f;
            ai.m_alertedEffects = new EffectList();
        }

        static EffectList Voiced(EffectList original, string key)
        {
            GameObject clip = null;
            if (key != null && !Clips.TryGetValue(key, out clip)) return original;
            var keep = new List<EffectList.EffectData>();
            if (original != null && original.m_effectPrefabs != null)
                foreach (EffectList.EffectData e in original.m_effectPrefabs)
                    if (e != null && e.m_prefab != null && !e.m_prefab.name.StartsWith("sfx_", StringComparison.Ordinal) &&
                        !e.m_prefab.name.StartsWith("LWC_sfx_", StringComparison.Ordinal)) keep.Add(e);
            if (key != null) keep.Add(new EffectList.EffectData { m_prefab = clip, m_enabled = true, m_attach = true });
            return new EffectList { m_effectPrefabs = keep.ToArray() };
        }

        // ---- runtime: the rules live in VoicePolicy; this only plays and silences ----
        static readonly VoicePolicy Policy = new VoicePolicy();
        static readonly System.Random Dice = new System.Random();
        static readonly Dictionary<int, GameObject[]> Speaking = new Dictionary<int, GameObject[]>();
        static readonly Dictionary<NpcKernel, KeyValuePair<float, string>> Pending = new Dictionary<NpcKernel, KeyValuePair<float, string>>();
        public static int Lines { get; private set; }
        public static event Action<NpcKernel, string> Spoke;

        static void Say(NpcKernel kernel, string key)
        {
            if (kernel == null || kernel.Body == null || kernel.Body.Human == null || kernel.Body.Human.IsDead()) return;
            EffectList line = IsFemale(kernel) && FemaleUsable(key) ? List("f:" + key) : List(key);
            if (line == null) return;
            Character body = kernel.Body.Human;
            Silence(body);
            Speaking[body.GetInstanceID()] = line.Create(body.transform.position, body.transform.rotation, body.transform);
            Lines++;
            Action<NpcKernel, string> spoke = Spoke;
            if (spoke != null) spoke(kernel, key);
        }

        /// <summary>Being hit cuts off whatever this companion was shouting.</summary>
        /// <returns>true if a line was actually still playing and got cut off.</returns>
        static bool Silence(Character body)
        {
            GameObject[] active;
            if (!Speaking.TryGetValue(body.GetInstanceID(), out active) || active == null) return false;
            bool cut = false;
            foreach (GameObject go in active)
            {
                if (go == null) continue;
                cut = true;
                // A networked line must leave through ZNetScene, or the scene keeps a dead entry and
                // throws every frame it sweeps (1.0.17-1.0.21 respawn error floods).
                ZNetView view = go.GetComponent<ZNetView>();
                if (view != null && view.IsValid()) { if (view.IsOwner()) ZNetScene.instance.Destroy(go); }
                else UnityEngine.Object.Destroy(go);
            }
            Speaking.Remove(body.GetInstanceID());
            return cut;
        }

        internal static void OnNewTarget(NpcKernel kernel, Character enemy)
        {
            if (enemy == null) return;
            if (Policy.NewEnemy(Time.time, enemy.GetInstanceID(), (float)Dice.NextDouble())) Say(kernel, "alerted");
        }

        internal static void OnHurt(NpcKernel kernel, float damage)
        {
            Character body = kernel.Body.Human;
            bool interrupted = Silence(body);
            VoicePolicy.HurtLine line = Policy.Hurt(Time.time, body.GetInstanceID(), damage, body.GetMaxHealth(),
                interrupted, (float)Dice.NextDouble());
            if (line == VoicePolicy.HurtLine.None) return;
            Say(kernel, "hit");
            if (line != VoicePolicy.HurtLine.Devastated) return;
            NpcKernel partner = NearestPartner(kernel, 25f);
            if (partner != null) Pending[partner] = new KeyValuePair<float, string>(Time.time + VoicePolicy.PartnerReactDelay, "alerted");
        }

        internal static void OnStrike(Character attacker, Character target, float damage, bool killed)
        {
            if (target == null || target.GetComponent<NpcKernel>() != null) return;
            if (!Policy.Mighty(Time.time, damage, target.GetMaxHealth(), killed, (float)Dice.NextDouble())) return;
            NpcKernel speaker = attacker == null ? null : attacker.GetComponent<NpcKernel>();
            if (speaker == null) speaker = NearestCompanionTo(target.transform.position, 25f);
            if (speaker != null) Say(speaker, "idle");   // the taunting chatter
        }

        internal static void Tick(NpcKernel kernel)
        {
            KeyValuePair<float, string> due;
            if (!Pending.TryGetValue(kernel, out due) || Time.time < due.Key) return;
            Pending.Remove(kernel);
            Say(kernel, due.Value);
        }

        static NpcKernel NearestPartner(NpcKernel kernel, float radius)
        {
            NpcLeaderBinding mine = kernel.GetComponent<NpcLeaderBinding>();
            NpcKernel best = null; float bestD = radius;
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (k == null || k == kernel || k.Body == null || k.Body.Human == null) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (mine == null || b == null || b.LeaderId != mine.LeaderId) continue;
                float d = Vector3.Distance(k.transform.position, kernel.transform.position);
                if (d <= bestD) { bestD = d; best = k; }
            }
            return best;
        }

        static NpcKernel NearestCompanionTo(Vector3 point, float radius)
        {
            NpcKernel best = null; float bestD = radius;
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (k == null || k.Body == null || k.Body.Human == null || k.Body.Human.IsDead()) continue;
                float d = Vector3.Distance(k.transform.position, point);
                if (d <= bestD) { bestD = d; best = k; }
            }
            return best;
        }
    }

    /// <summary>Harmony postfix on Character.ApplyDamage: one landed hit, after armor. Voice + telemetry.</summary>
    public static class StrikeHook
    {
        public static event Action<Character, Character, float, bool> Landed;

        public static void Postfix(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null) return;
            Character attacker = hit.GetAttacker();
            bool byCompanion = attacker != null && attacker.GetComponent<NpcKernel>() != null;
            bool byLeader = attacker != null && attacker == Player.m_localPlayer;
            if (!byCompanion && !byLeader) return;
            float damage = hit.GetTotalDamage();
            bool killed = __instance.IsDead() || __instance.GetHealth() <= 0f;
            try { NpcVoice.OnStrike(attacker, __instance, damage, killed); }
            catch (Exception e) { CorePlugin.Warn("voice strike: " + e.Message); }
            Action<Character, Character, float, bool> landed = Landed;
            if (landed != null && byCompanion) landed(attacker, __instance, damage, killed);
        }
    }
}

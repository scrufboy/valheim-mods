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
            SplitSurprised01();
            BuildFemale();
            Missing = missing.Count == 0 ? "" : String.Join(", ", missing.ToArray());
        }

        // ---- 1.0.32.1: male alerted Surprised01 holds two separate utterances; play either one, never both. ----
        const string Surprised01 = "Male_LS_D_Surprised01";
        public static string MaleAlertedSummary { get; private set; }

        /// <summary>Replaces the Surprised01 entry of the male alerted set with its two atomic halves (voices/male/*_A/_B.wav)
        /// at the slot's original weight (VoiceSlots.SplitOne): A 1/6, B 1/6, Surprised05 1/3, Surprised06 1/3. Load-time only;
        /// anything unexpected leaves the vanilla set untouched and says so.</summary>
        static void SplitSurprised01()
        {
            GameObject alerted;
            ZSFX sfx = Clips.TryGetValue("alerted", out alerted) && alerted != null ? alerted.GetComponent<ZSFX>() : null;
            if (sfx == null || sfx.m_audioClips == null) { MaleAlertedSummary = "unchanged (no alerted set)"; CorePlugin.Warn("male alerted split: no alerted set"); return; }
            var byName = new Dictionary<string, AudioClip>();
            var names = new List<string>();
            foreach (AudioClip c in sfx.m_audioClips)
            {
                if (c == null) continue;
                if (c.name.StartsWith(Surprised01 + "_", StringComparison.Ordinal)) { MaleAlertedSummary = "already split"; return; }
                if (!byName.ContainsKey(c.name)) { byName[c.name] = c; names.Add(c.name); }
            }
            string dir = Path.Combine(Path.GetDirectoryName(typeof(NpcVoice).Assembly.Location) ?? "", Path.Combine("voices", "male"));
            string pathA = Path.Combine(dir, Surprised01 + "_A.wav"), pathB = Path.Combine(dir, Surprised01 + "_B.wav");
            string[] flat = VoiceSlots.SplitOne(names.ToArray(), Surprised01, Surprised01 + "_A", Surprised01 + "_B");
            if (flat == null) { MaleAlertedSummary = "unchanged (" + Surprised01 + " not in the set: " + String.Join(", ", names.ToArray()) + ")"; CorePlugin.Warn("male alerted split: " + MaleAlertedSummary); return; }
            if (!File.Exists(pathA) || !File.Exists(pathB)) { MaleAlertedSummary = "unchanged (missing voices/male split files)"; CorePlugin.Warn("male alerted split: " + MaleAlertedSummary); return; }
            try
            {
                AudioClip a = WavClip.Load(pathA, false), b = WavClip.Load(pathB, false);   // keep the recording's level: no normalization
                if (a.samples == 0 || b.samples == 0) { MaleAlertedSummary = "unchanged (empty split file)"; CorePlugin.Warn("male alerted split: " + MaleAlertedSummary); return; }
                byName[a.name] = a; byName[b.name] = b;
                Loaded.Add(a); Loaded.Add(b);
                var clips = new AudioClip[flat.Length];
                for (int i = 0; i < flat.Length; i++) clips[i] = byName[flat[i]];
                sfx.m_audioClips = clips;
                var share = new List<string>();
                foreach (string n in names)
                {
                    if (n == Surprised01) { share.Add(n + "_A 1/" + (names.Count * 2)); share.Add(n + "_B 1/" + (names.Count * 2)); }
                    else share.Add(n + " 1/" + names.Count);
                }
                MaleAlertedSummary = String.Join(", ", share.ToArray()) + "; array " + String.Join(" ", flat) + " (" + a.length.ToString("0.000") + " s / " + b.length.ToString("0.000") + " s, not normalized)";
            }
            catch (Exception e) { MaleAlertedSummary = "unchanged (" + e.Message + ")"; CorePlugin.Warn("male alerted split: " + MaleAlertedSummary); }
        }

        // ---- Female voice set: Brian's licensed "Ultimate Female Voice Pack" (topmademarti). ----
        // 1.0.32 layout: voices/female/clips/*.wav + voices/female/lines.txt ("line: file, file, ..."), so clips are
        // approved or rejected by editing names. (Fallback: the 1.0.27 voices/female/<line>/*.wav folders.)
        // Same lines and the same VoicePolicy/WarCryPolicy for both genders; gender only picks the clip pool in Say.
        static readonly string[] FemaleLines = { "alerted", "exert", "hit", "hit_heavy", "idle", "death", "warcry" };
        public static string FemaleSummary { get; private set; }
        static readonly Dictionary<string, string> ClipLine = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static void BuildFemale()
        {
            var parts = new List<string>();
            string root = Path.Combine(Path.GetDirectoryName(typeof(NpcVoice).Assembly.Location) ?? "", Path.Combine("voices", "female"));
            if (!Directory.Exists(root)) { FemaleSummary = "none (no voices/female folder)"; return; }
            GameObject template = PrefabManager.Instance.GetPrefab("sfx_goblin_hit");
            if (template == null) { FemaleSummary = "none (template sfx missing)"; return; }
            Dictionary<string, List<string>> plan = ReadPlan(root);
            var byHash = new Dictionary<string, string>();
            foreach (string line in FemaleLines)
            {
                List<string> files;
                if (!plan.TryGetValue(line, out files) || files.Count == 0) { CorePlugin.Warn("female voice: line '" + line + "' has no clips -> male line"); continue; }
                var clips = new List<AudioClip>();
                var loadedHere = new Dictionary<string, AudioClip>();   // a weighted slot repeats a file: load it once
                foreach (string file in files)
                {
                    AudioClip again;
                    if (loadedHere.TryGetValue(file, out again)) { clips.Add(again); continue; }
                    string name = Path.GetFileName(file);
                    string other;
                    if (ClipLine.TryGetValue(name, out other) && other != line)
                        CorePlugin.Warn("female voice: " + name + " is in both '" + other + "' and '" + line + "'");
                    ClipLine[name] = line;
                    try
                    {
                        string hash = Hash(file);
                        if (byHash.TryGetValue(hash, out other) && other != line + "/" + name)
                            CorePlugin.Warn("female voice: " + line + "/" + name + " has the same audio as " + other);
                        byHash[hash] = line + "/" + name;
                        AudioClip clip = WavClip.Load(file);
                        if (clip.length <= 0f || clip.samples == 0) { CorePlugin.Warn("female voice: " + name + " is empty"); continue; }
                        loadedHere[file] = clip;
                        clips.Add(clip);
                    }
                    catch (Exception e) { CorePlugin.Warn("female voice " + name + ": " + e.Message); }
                }
                if (clips.Count == 0) { CorePlugin.Warn("female voice: line '" + line + "' loaded no clips -> male line"); continue; }
                string prefabName = "LWC_sfx_f_" + line;
                GameObject prefab = PrefabManager.Instance.GetPrefab(prefabName);
                if (prefab == null)
                {
                    prefab = PrefabManager.Instance.CreateClonedPrefab(prefabName, template);
                    if (prefab != null && prefab.GetComponent<ZNetView>() != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
                }
                ZSFX sfx = prefab == null ? null : prefab.GetComponent<ZSFX>();
                if (sfx == null) continue;
                sfx.m_audioClips = clips.ToArray();
                Loaded.AddRange(clips);   // strong references as well
                sfx.m_minPitch = FemaleMinPitch;
                sfx.m_maxPitch = FemaleMaxPitch;
                if (TemplateVolume < 0f) TemplateVolume = sfx.m_maxVol;
                // One gain stage on top of consistent normalization: CorePlugin.FemaleVoiceGain (config).
                sfx.m_minVol = sfx.m_maxVol = Mathf.Min(1f, TemplateVolume * CorePlugin.FemaleVoiceGain);
                Clips["f:" + line] = prefab;
                string note;
                parts.Add(SlotNotes.TryGetValue(line, out note) ? line + " " + loadedHere.Count + " (" + note + ")" : line + " " + clips.Count);
            }
            FemaleSummary = (parts.Count == 0 ? "none" : String.Join(", ", parts.ToArray())) + "; clips normalized to 0.98 peak, volume " +
                TemplateVolume + " x " + CorePlugin.FemaleVoiceGain + (plan.ContainsKey("#source") ? "; " + plan["#source"][0] : "");
        }

        /// <summary>lines.txt plan if present, else one folder per line.</summary>
        static readonly Dictionary<string, string> SlotNotes = new Dictionary<string, string>();

        static Dictionary<string, List<string>> ReadPlan(string root)
        {
            SlotNotes.Clear();
            var plan = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string manifest = Path.Combine(root, "lines.txt"), clipDir = Path.Combine(root, "clips");
            if (File.Exists(manifest))
            {
                foreach (string raw in File.ReadAllLines(manifest))
                {
                    string row = raw.Trim();
                    int colon = row.IndexOf(':');
                    if (row.Length == 0 || row.StartsWith("#") || colon <= 0) continue;
                    string line = row.Substring(0, colon).Trim();
                    // 1.0.32.2: "a | b" in one comma entry = one logical slot with two atomic alternatives (the male
                    // Surprised01 model). Every slot keeps equal weight; VoiceSlots.Flatten builds the weighted pick array.
                    var slots = new List<string[]>();
                    bool grouped = false;
                    foreach (string part in row.Substring(colon + 1).Split(','))
                    {
                        var atoms = new List<string>();
                        foreach (string atom in part.Split('|'))
                        {
                            string f = atom.Trim();
                            if (f.Length == 0) continue;
                            string path = Path.Combine(clipDir, f);
                            if (File.Exists(path)) atoms.Add(path); else CorePlugin.Warn("female voice: " + line + " lists missing file " + f);
                        }
                        if (atoms.Count > 1) grouped = true;
                        if (atoms.Count > 0) slots.Add(atoms.ToArray());
                    }
                    plan[line] = new List<string>(VoiceSlots.Flatten(slots));
                    if (grouped)
                    {
                        var shares = new List<string>();
                        foreach (string[] slot in slots)
                            foreach (string atom in slot) shares.Add(Path.GetFileNameWithoutExtension(atom) + " 1/" + (slots.Count * slot.Length));
                        SlotNotes[line] = slots.Count + " slots, " + plan[line].Count + " entries: " + String.Join(", ", shares.ToArray());
                    }
                }
                plan["#source"] = new List<string> { "lines.txt" };
                return plan;
            }
            foreach (string line in FemaleLines)
            {
                string dir = Path.Combine(root, line);
                if (Directory.Exists(dir)) plan[line] = new List<string>(Directory.GetFiles(dir, "*.wav"));
            }
            plan["#source"] = new List<string> { "per-line folders" };
            return plan;
        }

        static string Hash(string path)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
                return BitConverter.ToString(md5.ComputeHash(File.ReadAllBytes(path)));
        }

        public const float FemaleMinPitch = .97f, FemaleMaxPitch = 1.03f;   // human voice: natural variation only
        static float TemplateVolume = -1f;
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

        /// <summary>1.0.32.3: the 1.0.31 Say, restored. Behavior is one shared path for every companion; gender appears only
        /// in the final pool lookup (female pool if loaded, else the male Fuling pool).</summary>
        static void Say(NpcKernel kernel, string key)
        {
            if (kernel == null || kernel.Body == null || kernel.Body.Human == null || kernel.Body.Human.IsDead()) return;
            Character body = kernel.Body.Human;
            int id = body.GetInstanceID();
            // 1.0.32.4: a lower-priority line (alerted, exert, idle) never cuts off pain that is still sounding.
            KeyValuePair<int, float> held;
            if (Holding.TryGetValue(id, out held) && Time.time < held.Value && Priority(key) < held.Key) { Skip(kernel, key, "lower priority"); return; }
            EffectList line = IsFemale(kernel) && FemaleUsable(key) ? List("f:" + key) : List(MaleKey(key));
            if (line == null) { Skip(kernel, key, "no clip"); return; }
            Silence(body);
            GameObject[] made = line.Create(body.transform.position, body.transform.rotation, body.transform);
            Speaking[id] = made;
            Holding[id] = new KeyValuePair<int, float>(Priority(key), Time.time + HoldSeconds);
            if (key == "hit" || key == "hit_heavy") LastPain[id] = new KeyValuePair<float, string>(Time.time, key);
            Lines++;
            if (CorePlugin.DevVoiceLog) LogLine(kernel, body, key, made);
            Action<NpcKernel, string> spoke = Spoke;
            if (spoke != null) spoke(kernel, key);
        }

        // ---- 1.0.32.4 priority (Brian 20:27): stagger / devastating 4, ordinary hit 3, war cry 2, alerted / exert / idle 1. ----
        const float HoldSeconds = .7f;   // a pain line is protected from lower lines for about its length
        static readonly Dictionary<int, KeyValuePair<int, float>> Holding = new Dictionary<int, KeyValuePair<int, float>>();
        static readonly Dictionary<int, KeyValuePair<float, string>> LastPain = new Dictionary<int, KeyValuePair<float, string>>();
        static int Priority(string key) { return key == "hit_heavy" ? 4 : key == "hit" ? 3 : key == "warcry" ? 2 : 1; }

        static void Skip(NpcKernel kernel, string key, string why)
        {
            if (!CorePlugin.DevVoiceLog || kernel == null || kernel.Body == null || kernel.Body.Human == null) return;
            CorePlugin.Report("VOICE-SKIP " + kernel.Body.Human.GetHoverName() + " line=" + key + " reason=" + why + " t=" + Time.time.ToString("0.00"));
        }

        /// <summary>Development log only (DevVoiceLog): names the exact clip by narrowing this one instance to a uniform
        /// pick from its pool (the same pick ZSFX would make). Runs after the line has already been decided and created.</summary>
        static void LogLine(NpcKernel kernel, Character body, string key, GameObject[] made)
        {
            bool female = IsFemale(kernel);
            CorePlugin.Report("VOICE " + body.GetHoverName() + " (" + (female ? "female" : "male") + ") line=" + key +
                " clip=" + Narrow(made) + (female && !FemaleUsable(key) ? " FALLBACK=male" : ""));
        }

        /// <summary>Male lines that share a vanilla clip set: exertion and heavy hurt use the Fuling grunt.</summary>
        static string MaleKey(string key) { return key == "exert" || key == "hit_heavy" ? "hit" : key; }

        static string Narrow(GameObject[] made)
        {
            if (made == null) return "?";
            foreach (GameObject go in made)
            {
                ZSFX z = go == null ? null : go.GetComponent<ZSFX>();
                if (z == null || z.m_audioClips == null || z.m_audioClips.Length == 0) continue;
                var alive = new List<AudioClip>();
                foreach (AudioClip c in z.m_audioClips) if (c != null) alive.Add(c);
                if (alive.Count == 0) return "none";
                AudioClip pick = alive[UnityEngine.Random.Range(0, alive.Count)];
                z.m_audioClips = new[] { pick };      // instance copy only; the prefab keeps its pool
                return pick.name;
            }
            return "?";
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
            if (enemy == null || kernel == null || kernel.Body == null) return;
            float lastStrike;
            float sinceStrike = LastStrike.TryGetValue(kernel.GetInstanceID(), out lastStrike) ? Time.time - lastStrike : float.PositiveInfinity;
            float distance = Vector3.Distance(kernel.transform.position, enemy.transform.position);
            if (!VoicePolicy.Noticing(sinceStrike, kernel.Frame.Senses.SecondsSinceDamaged, distance)) return;
            if (Policy.NewEnemy(Time.time, enemy.GetInstanceID(), (float)Dice.NextDouble())) Say(kernel, "alerted");
        }

        internal static void OnHurt(NpcKernel kernel, float damage)
        {
            Character body = kernel.Body.Human;
            // 1.0.32.4: the stagger cry already answered this blow (stagger and damage are one event).
            if (Policy.StaggerAnswered(Time.time, body.GetInstanceID())) { Skip(kernel, "hit", "lower priority"); return; }
            bool interrupted = Silence(body);
            string why;
            VoicePolicy.HurtLine line = Policy.Hurt(Time.time, body.GetInstanceID(), damage, body.GetMaxHealth(),
                interrupted, (float)Dice.NextDouble(), out why);
            if (line == VoicePolicy.HurtLine.None) { Skip(kernel, "hit", why); return; }
            // 1.0.31 said "hit" for every hurt line; "hit_heavy" only names the female devastated pool (males resolve it to "hit").
            Say(kernel, line == VoicePolicy.HurtLine.Devastated ? "hit_heavy" : "hit");
            if (line != VoicePolicy.HurtLine.Devastated) return;
            NpcKernel partner = NearestPartner(kernel, 25f);
            if (partner != null) Pending[partner] = new KeyValuePair<float, string>(Time.time + VoicePolicy.PartnerReactDelay, "alerted");
        }

        /// <summary>1.0.32.4: Character.RPC_Stagger prefix (owner). Not yet staggering = a new stagger: one guaranteed pain cry.</summary>
        public static void RpcStaggerPrefix(Character __instance)
        {
            if (__instance == null || __instance is Player) return;
            NpcBody body = NpcBody.Of(__instance);
            if (body == null || !body.IsOwner || __instance.IsDead() || __instance.IsStaggering()) return;
            NpcKernel kernel = body.Root.GetComponent<NpcKernel>();
            if (kernel == null) return;
            try
            {
                int id = __instance.GetInstanceID();
                if (CorePlugin.DevVoiceLog) CorePlugin.Report("VOICE-STAGGER " + __instance.GetHoverName() + " entered stagger t=" + Time.time.ToString("0.00"));
                if (!Policy.Stagger(Time.time, id)) { Skip(kernel, "hit_heavy", "cooldown"); return; }
                KeyValuePair<float, string> pain;   // the same blow already drew a devastating cry: one line per event
                if (LastPain.TryGetValue(id, out pain) && Time.time - pain.Key < VoicePolicy.SameEventWindow && pain.Value == "hit_heavy")
                { Skip(kernel, "hit_heavy", "lower priority"); return; }
                Holding.Remove(id);   // stagger pain outranks whatever is sounding
                Say(kernel, "hit_heavy");
            }
            catch (Exception e) { kernel.ReportFault("voice stagger", e); }
        }

        static readonly Dictionary<int, float> LastStrike = new Dictionary<int, float>();

        /// <summary>A companion's attack just started (Humanoid.StartAttack). Ordinary: one exertion decision per attack, played
        /// now (during the swing). Heavy (1.0.32.4): always voiced, but held until the committed attack frame.</summary>
        internal static void OnSwing(NpcKernel kernel, bool heavy)
        {
            if (kernel == null || kernel.Body == null || kernel.Body.Human == null) return;
            Humanoid human = kernel.Body.Human;
            int id = human.GetInstanceID();
            object attack = NativeApi.CurrentAttack.GetValue(human);   // once per attack start, not per frame
            object decided;
            if (attack != null && Decided.TryGetValue(id, out decided) && ReferenceEquals(decided, attack)) return;   // one decision per attack (bow draw)
            if (attack != null) Decided[id] = attack;
            string why;
            if (!Policy.Exert(Time.time, kernel.GetInstanceID(), heavy, (float)Dice.NextDouble(), out why)) { Skip(kernel, "exert", why); return; }
            if (!heavy) { Say(kernel, "exert"); return; }
            HeavyPending[id] = Time.time;
            if (CorePlugin.DevVoiceLog) CorePlugin.Report("VOICE-HEAVY " + human.GetHoverName() + " selected t=" + Time.time.ToString("0.00") + " (exertion waits for the attack frame)");
        }

        static readonly Dictionary<int, object> Decided = new Dictionary<int, object>();
        static readonly Dictionary<int, float> HeavyPending = new Dictionary<int, float>();
        const float HeavyPendingLimit = 3f;   // an attack that never reaches its frame is treated as aborted

        /// <summary>1.0.32.4: Attack.OnAttackTrigger postfix: the attack became active. A pending heavy exertion plays now, once.</summary>
        public static void AttackTriggerPostfix(Attack __instance)
        {
            if (__instance == null || HeavyPending.Count == 0) return;
            Humanoid human = NativeApi.AttackCharacter.GetValue(__instance) as Humanoid;
            if (human == null || human is Player) return;
            int id = human.GetInstanceID();
            float since;
            if (!HeavyPending.TryGetValue(id, out since)) return;
            HeavyPending.Remove(id);
            NpcBody body = NpcBody.Of(human);
            NpcKernel kernel = body == null || !body.IsOwner ? null : body.Root.GetComponent<NpcKernel>();
            if (kernel == null) return;
            try
            {
                if (human.IsDead() || human.IsStaggering()) { Skip(kernel, "exert", "attack aborted"); return; }
                if (CorePlugin.DevVoiceLog) CorePlugin.Report("VOICE-HEAVY " + human.GetHoverName() + " attack frame t=" + Time.time.ToString("0.00") + " (+" + (Time.time - since).ToString("0.00") + " s after selection)");
                Say(kernel, "exert");
            }
            catch (Exception e) { kernel.ReportFault("voice heavy", e); }
        }

        internal static void OnStrike(Character attacker, Character target, float damage, bool killed)
        {
            NpcKernel striker = attacker == null ? null : attacker.GetComponent<NpcKernel>();
            if (striker != null) LastStrike[striker.GetInstanceID()] = Time.time;
            if (target == null || target.GetComponent<NpcKernel>() != null) return;
            if (!Policy.Mighty(Time.time, damage, target.GetMaxHealth(), killed, (float)Dice.NextDouble())) return;
            NpcKernel speaker = attacker == null ? null : attacker.GetComponent<NpcKernel>();
            if (speaker == null) speaker = NearestCompanionTo(target.transform.position, 25f);
            if (speaker != null) Say(speaker, "idle");   // the taunting chatter
        }

        internal static void Tick(NpcKernel kernel)
        {
            Humanoid human = kernel == null || kernel.Body == null ? null : kernel.Body.Human;
            float since;
            if (human != null && HeavyPending.TryGetValue(human.GetInstanceID(), out since) &&
                (human.IsDead() || Time.time - since > HeavyPendingLimit || (Time.time - since > .25f && !human.InAttack())))
            {
                HeavyPending.Remove(human.GetInstanceID());
                Skip(kernel, "exert", "attack aborted");
            }
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

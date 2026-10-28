using System;
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
    /// </summary>
    public static class BodyReactionVoice
    {
        static readonly string[] Pools = { "onset", "repeat", "recover" };
        static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        static readonly List<AudioClip> Keep = new List<AudioClip>();
        static readonly Dictionary<int, BodyVoicePolicy> Law = new Dictionary<int, BodyVoicePolicy>();
        static readonly Dictionary<int, float> LastCombat = new Dictionary<int, float>();
        static readonly Dictionary<int, GameObject[]> Playing = new Dictionary<int, GameObject[]>();
        static float nextTick;
        public static string Summary { get; private set; }
        public static int Lines { get; private set; }

        public static void Build()
        {
            var parts = new List<string>();
            string root = Path.Combine(Path.GetDirectoryName(typeof(BodyReactionVoice).Assembly.Location) ?? "", Path.Combine("voices", "body"));
            GameObject template = PrefabManager.Instance.GetPrefab("sfx_goblin_hit");   // container only (ZSFX + ZNetView)
            foreach (string gender in new[] { "male", "female" })
                foreach (string pool in Pools)
                {
                    string dir = Path.Combine(root, Path.Combine(gender, pool));
                    string[] files = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.wav") : new string[0];
                    Array.Sort(files, StringComparer.Ordinal);
                    var clips = new List<AudioClip>();
                    foreach (string f in files)
                    {
                        try { AudioClip c = WavClip.Load(f, false); if (c != null && c.length > 0f) clips.Add(c); }
                        catch (Exception e) { CorePlugin.Warn("body voice " + Path.GetFileName(f) + ": " + e.Message); }
                    }
                    if (clips.Count == 0 || template == null) { parts.Add(gender + " " + pool + " 0"); continue; }
                    string name = "LWC_sfx_body_" + gender[0] + "_" + pool;
                    GameObject prefab = PrefabManager.Instance.GetPrefab(name);
                    if (prefab == null)
                    {
                        prefab = PrefabManager.Instance.CreateClonedPrefab(name, template);
                        if (prefab != null && prefab.GetComponent<ZNetView>() != null) PrefabManager.Instance.AddPrefab(new CustomPrefab(prefab, false));
                    }
                    ZSFX sfx = prefab == null ? null : prefab.GetComponent<ZSFX>();
                    if (sfx == null) { parts.Add(gender + " " + pool + " no ZSFX"); continue; }
                    sfx.m_audioClips = clips.ToArray();
                    Keep.AddRange(clips);
                    sfx.m_minPitch = sfx.m_maxPitch = 1f;
                    float vol = Mathf.Min(1f, sfx.m_maxVol * CorePlugin.CompanionVoiceGain);
                    sfx.m_minVol = sfx.m_maxVol = vol;
                    Prefabs[gender[0] + ":" + pool] = prefab;
                    parts.Add(gender + " " + pool + " " + clips.Count);
                }
            Summary = String.Join(", ", parts.ToArray());
            CompanionCombatVoice.Spoke += OnCombatLine;
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
                if (line == BodyLine.None) continue;
                Say(k, h, id, line == BodyLine.Onset ? "onset" : line == BodyLine.Repeat ? "repeat" : "recover");
            }
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
            if (CorePlugin.DevVoiceLog) CorePlugin.Report("BODYVOICE " + h.GetHoverName() + " (" + (g == 'f' ? "female" : "male") + ") line=poison-" + pool + " t=" + Time.time.ToString("0.00"));
        }
    }
}

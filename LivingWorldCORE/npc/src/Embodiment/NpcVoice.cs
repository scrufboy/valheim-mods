using System;
using System.Collections.Generic;
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
                    clone = PrefabManager.Instance.CreateClonedPrefab(name, source);
                if (clone == null) { missing.Add(source); continue; }
                ZSFX sfx = clone.GetComponent<ZSFX>();
                if (sfx != null) { sfx.m_minPitch = MinPitch; sfx.m_maxPitch = MaxPitch; }
                Clips[source.Substring("sfx_goblin_".Length)] = clone;
            }
            Missing = missing.Count == 0 ? "" : String.Join(", ", missing.ToArray());
        }

        static EffectList List(string key)
        {
            GameObject clip;
            if (!Clips.TryGetValue(key, out clip)) return null;
            return new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = clip, m_enabled = true, m_attach = true } } };
        }

        /// <summary>Factory-time: voice the hit/death effects (vfx kept, player sfx replaced) and the idle timer.</summary>
        internal static void Configure(Humanoid human, MonsterAI ai)
        {
            human.m_hitEffects = Voiced(human.m_hitEffects, "hit");
            human.m_deathEffects = Voiced(human.m_deathEffects, "death");
            EffectList idle = List("idle");
            if (idle != null)
            {
                ai.m_idleSound = idle;
                ai.m_idleSoundInterval = 30f;
                ai.m_idleSoundChance = .25f;
            }
        }

        static EffectList Voiced(EffectList original, string key)
        {
            GameObject clip;
            if (!Clips.TryGetValue(key, out clip)) return original;
            var keep = new List<EffectList.EffectData>();
            if (original != null && original.m_effectPrefabs != null)
                foreach (EffectList.EffectData e in original.m_effectPrefabs)
                    if (e != null && e.m_prefab != null && !e.m_prefab.name.StartsWith("sfx_", StringComparison.Ordinal)) keep.Add(e);
            keep.Add(new EffectList.EffectData { m_prefab = clip, m_enabled = true, m_attach = true });
            return new EffectList { m_effectPrefabs = keep.ToArray() };
        }

        // ---- runtime cues (one per phase entry, rate limited per companion) ----
        static readonly Dictionary<int, float> LastCue = new Dictionary<int, float>();
        public static int CuesPlayed { get; private set; }

        internal static void OnPhaseChange(Character body, NpcPhase from, NpcPhase to)
        {
            if (body == null) return;
            string key = to == NpcPhase.Combat && from != NpcPhase.CombatClear && from != NpcPhase.CombatRegroup ? "alerted"
                : to == NpcPhase.Readiness && from == NpcPhase.Calm ? "idle" : null;
            if (key == null) return;
            int id = body.GetInstanceID();
            float last;
            if (LastCue.TryGetValue(id, out last) && Time.time - last < CueCooldown) return;
            EffectList cue = List(key);
            if (cue == null) return;
            LastCue[id] = Time.time;
            CuesPlayed++;
            cue.Create(body.transform.position, body.transform.rotation, body.transform);
        }
    }
}

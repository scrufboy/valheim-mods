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
                    if (e != null && e.m_prefab != null && !e.m_prefab.name.StartsWith("sfx_", StringComparison.Ordinal)) keep.Add(e);
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
            EffectList line = List(key);
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
            foreach (GameObject go in active) if (go != null) { cut = true; UnityEngine.Object.Destroy(go); }
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

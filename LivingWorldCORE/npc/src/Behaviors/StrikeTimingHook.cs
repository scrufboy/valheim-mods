using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// A2.3 (3e5ac3c): companions learn how long each attack takes from start to its hit frame by watching it happen
    /// (ObservedCombat provenance), within 30 m of a companion. Harmony postfix on Attack.OnAttackTrigger; READ-ONLY:
    /// it never changes the attack. Parry is attempted only on attacks learned this way (DefenseArbiter).
    /// </summary>
    public static class StrikeTimingHook
    {
        const float WatchRadius = 30f;
        public static int Learned { get; private set; }
        static readonly Attack[] seen = new Attack[32];
        static int next;

        public static void Postfix(Attack __instance)
        {
            try
            {
                Humanoid h = __instance == null ? null : NativeApi.AttackCharacter.GetValue(__instance) as Humanoid;
                if (h == null || h.IsPlayer() || h.GetComponent<NpcKernel>() != null) return;
                if (!Watched(h.transform.position)) return;
                float elapsed; bool secondary;
                Attack a = NpcBody.AttackOf(h, out elapsed, out secondary);
                if (!ReferenceEquals(a, __instance)) return;
                // Multi-hit attacks trigger more than once: only the first hit frame of each strike is its windup.
                for (int i = 0; i < seen.Length; i++) if (ReferenceEquals(seen[i], __instance)) return;
                seen[next] = __instance; next = (next + 1) % seen.Length;
                string key = global::Utils.GetPrefabName(h.gameObject) + ":" + __instance.m_attackAnimation + (secondary ? ":2" : "");
                NativeCombatBehavior.Timings.Learn(key, elapsed);
                Learned++;
            }
            catch (System.Exception e) { CorePlugin.Warn("strike timing: " + e.Message); }
        }

        static bool Watched(Vector3 p)
        {
            foreach (NpcKernel k in NpcKernel.LiveSet)
                if (k != null && (k.transform.position - p).sqrMagnitude <= WatchRadius * WatchRadius) return true;
            return false;
        }
    }
}

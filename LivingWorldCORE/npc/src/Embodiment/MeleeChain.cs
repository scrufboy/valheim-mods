using System;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Chained melee, the Player's way. Two hooks, no decisions:
    ///  - Humanoid.UpdateAttack (every physics step): for a companion mid-swing, ask NpcBody.TryChain.
    ///  - Humanoid.HaveQueuedChain: answers yes only for the actor TryChain is starting right now,
    ///    which is what lets native StartAttack begin the next swing inside the current one.
    /// The rule is ChainPolicy; the permission is NativeCombatBehavior holding the hands.
    /// </summary>
    public static class MeleeChainHook
    {
        public static event Action<NpcKernel, int, float> Chained;

        public static void QueuedPostfix(Humanoid __instance, ref bool __result)
        {
            if (!__result && NpcBody.ChainQueued(__instance)) __result = true;
        }

        public static void UpdatePostfix(Humanoid __instance)
        {
            if (__instance == null || __instance is Player) return;
            foreach (NpcKernel kernel in NpcKernel.All)
            {
                if (kernel == null || kernel.Body == null || !ReferenceEquals(kernel.Body.Human, __instance)) continue;
                int level;
                try { level = kernel.Body.TryChain(); }
                catch (Exception e) { kernel.ReportFault("melee chain", e); return; }
                if (level < 0) return;
                Action<NpcKernel, int, float> chained = Chained;
                if (chained != null) chained(kernel, level, kernel.Body.LastChainDistance);
                return;
            }
        }
    }
}

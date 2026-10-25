using System;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Melee follow-ups, the Player's way. Two hooks, no decisions:
    ///  - Humanoid.UpdateAttack (every physics step): for a companion, ask NpcBody.TryFollowUp
    ///    (next combo hit, or a heavy attack when HeavyPolicy says it pays).
    ///  - Humanoid.HaveQueuedChain: answers yes only for the actor TryFollowUp is starting right now,
    ///    which is what lets native StartAttack begin the next swing inside the current one.
    /// The rules are ChainPolicy and HeavyPolicy; the permission is NativeCombatBehavior holding the hands.
    /// </summary>
    public static class MeleeChainHook
    {
        public static event Action<NpcKernel, int, float> Chained;
        public static event Action<NpcKernel> Heavy;

        public static void QueuedPostfix(Humanoid __instance, ref bool __result)
        {
            if (!__result && NpcBody.ChainQueued(__instance)) __result = true;
        }

        public static void UpdatePostfix(Humanoid __instance)
        {
            if (__instance == null || __instance is Player) return;
            NpcBody body = NpcBody.Of(__instance);
            if (body == null) return;
            NpcKernel kernel = body.Root.GetComponent<NpcKernel>();
            int started;
            try { started = body.TryFollowUp(); }
            catch (Exception e) { if (kernel != null) kernel.ReportFault("melee follow-up", e); return; }
            if (kernel == null) return;
            if (started == 1)
            {
                Action<NpcKernel, int, float> chained = Chained;
                if (chained != null) chained(kernel, body.LastChainLevel, body.LastChainDistance);
            }
            else if (started == 2)
            {
                Action<NpcKernel> heavy = Heavy;
                if (heavy != null) heavy(kernel);
            }
        }
    }

    /// <summary>
    /// Stamina parity: Valheim's base Character has infinite stamina (HaveStamina = true,
    /// UseStamina = no-op; only Player overrides them). For a companion on its owner, these hooks
    /// answer from NpcBody's pool instead, so native attacks, blocks and jumps pay their real cost,
    /// and running drains it and stops when it is empty (Player.CheckRun's rule).
    /// Every other character is untouched.
    /// </summary>
    public static class StaminaParity
    {
        public static bool HavePrefix(Character __instance, float amount, ref bool __result)
        {
            NpcBody body = NpcBody.Of(__instance);
            if (body == null || !body.IsOwner) return true;
            __result = body.HaveStaminaFor(amount);
            return false;
        }

        public static bool UsePrefix(Character __instance, float stamina)
        {
            NpcBody body = NpcBody.Of(__instance);
            if (body == null || !body.IsOwner) return true;
            body.SpendStamina(stamina);
            return false;
        }

        public static void MaxPostfix(Character __instance, ref float __result)
        {
            NpcBody body = NpcBody.Of(__instance);
            if (body != null) __result = body.MaxStamina;
        }

        public static void RunPostfix(Humanoid __instance, float dt, ref bool __result)
        {
            if (!__result || __instance is Player) return;
            NpcBody body = NpcBody.Of(__instance);
            if (body == null || !body.IsOwner) return;
            __result = body.RunAllowed(dt);
        }
    }
}

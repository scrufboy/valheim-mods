using System;
namespace LivingWorld.HumanNpc
{
    public enum CompanionPosture { Upright, Crouch, Sit }
    public static class CompanionPosturePolicy
    {
        // Pose requests never win over combat, swimming, a blocked entrance,
        // or a missing/unbound leader. Merely standing still is NOT sitting.
        public static CompanionPosture Choose(bool eligible, bool combat, bool bodySafe,
            bool leaderCrouched, bool leaderSitting, bool arrived, bool passageClear, float settledSeconds)
        {
            if (!eligible || combat || !bodySafe) return CompanionPosture.Upright;
            if (leaderCrouched) return CompanionPosture.Crouch;
            if (leaderSitting && arrived && passageClear && !Single.IsNaN(settledSeconds) &&
                !Single.IsInfinity(settledSeconds) && settledSeconds >= 2f) return CompanionPosture.Sit;
            return CompanionPosture.Upright;
        }
    }
}

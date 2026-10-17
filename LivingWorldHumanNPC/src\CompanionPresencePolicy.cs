using System;

namespace LivingWorld.HumanNpc
{
    public enum CompanionPace { Stand, Walk, Run }

    // Authored human-companion spacing, not a replacement pathfinder. Separate
    // entry/exit distances prevent noise at a boundary from flipping the pace.
    public static class CompanionPresencePolicy
    {
        public const float StopDistance = 2.2f;
        public const float ResumeDistance = 2.9f;
        public const float RunDistance = 5f;
        public const float WalkAgainDistance = 3.5f;
        public const float IdleAttentionSeconds = 12f;
        public const float LeaderMovementThreshold = 0.15f;
        public const float FormationRear = 3.2f;
        public const float FormationSide = 2.1f;

        public static float FormationSideOffset(bool archer, bool cleric, int stableIdentity)
        {
            if (archer) return -FormationSide;
            if (cleric) return FormationSide;
            return (stableIdentity & 1) == 0 ? -FormationSide : FormationSide;
        }

        public static CompanionPace NextPace(CompanionPace previous, float distance, bool mustCross)
        {
            if (!Finite(distance) || distance < 0) return CompanionPace.Stand;
            // Reaching the other side of a nearby wall/door takes precedence
            // over the comfort radius. Door travel owns its walking approach.
            if (mustCross) return CompanionPace.Walk;
            if (distance <= StopDistance) return CompanionPace.Stand;
            if (previous == CompanionPace.Stand && distance < ResumeDistance) return CompanionPace.Stand;
            if (previous == CompanionPace.Run && distance > WalkAgainDistance) return CompanionPace.Run;
            return distance >= RunDistance ? CompanionPace.Run : CompanionPace.Walk;
        }

        public static bool AttendLeader(bool eligible, bool hasCombat, bool travelling, float quietSeconds)
        {
            return eligible && !hasCombat && Finite(quietSeconds) && quietSeconds >= 0 &&
                (travelling || quietSeconds < IdleAttentionSeconds);
        }

        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}


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
        public const float FormationSide = 1.65f;
        public const float FormationStopDistance = 0.8f;
        public const float LeaderExclusionRadius = 1.25f;
        public const float LeaderExclusionMargin = 0.45f;
        public const float CatchUpDistance = 12f;
        public const float CatchUpReleaseDistance = 7f;
        public const float CatchUpRepathSeconds = .75f;

        public static bool CatchUp(bool active, float leaderDistance)
        {
            if (!Finite(leaderDistance) || leaderDistance < 0f) return false;
            return active ? leaderDistance > CatchUpReleaseDistance : leaderDistance >= CatchUpDistance;
        }

        public static float FormationSideOffset(bool archer, bool cleric, int stableIdentity)
        {
            if (archer) return -FormationSide;
            if (cleric) return FormationSide;
            return (stableIdentity & 1) == 0 ? -FormationSide : FormationSide;
        }

        public static CompanionPace NextFormationPace(CompanionPace previous, float slotDistance)
        {
            return NextPace(previous, slotDistance + StopDistance - FormationStopDistance, false);
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

        public static bool PathEntersLeaderExclusion(float actorX, float actorZ,
            float destinationX, float destinationZ, float leaderX, float leaderZ)
        {
            if (!Finite(actorX) || !Finite(actorZ) || !Finite(destinationX) || !Finite(destinationZ) ||
                !Finite(leaderX) || !Finite(leaderZ)) return true;
            float startX = actorX - leaderX, startZ = actorZ - leaderZ;
            float radiusSquared = LeaderExclusionRadius * LeaderExclusionRadius;
            if (startX * startX + startZ * startZ < radiusSquared) return true;
            float dx = destinationX - actorX, dz = destinationZ - actorZ;
            float lengthSquared = dx * dx + dz * dz;
            if (lengthSquared < .0001f) return false;
            float t = -(startX * dx + startZ * dz) / lengthSquared;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            float nearestX = startX + dx * t, nearestZ = startZ + dz * t;
            return nearestX * nearestX + nearestZ * nearestZ < radiusSquared;
        }

        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}

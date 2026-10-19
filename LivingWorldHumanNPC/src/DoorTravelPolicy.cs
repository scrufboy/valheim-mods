using System;

namespace LivingWorld.HumanNpc
{
    // Unity-independent decisions. The runtime supplies observations; this never
    // sets a door's network state or drives a character's transform.
    public enum DoorTravelPhase { None, Approach, Opening, Crossing, Closing, ClosePending }
    public enum DoorTravelAction { Native, Approach, Open, Wait, Cross, Close, Finish, Cancel }
    public struct DoorTravelInput
    {
        public DoorTravelPhase Phase;
        public bool Authorized, DoorValid, AccessAllowed, Keyed, CannotClose;
        public bool TimedOut, InUseRange, IsOpen, CanInteract, Crossed, OpenedHere;
        public bool CrossingClear, CloseDelayElapsed, WithinCloseRange;
    }
    public static class DoorTravelPolicy
    {
        public const float ScanInterval = 0.3f;
        public const float ScanRange = 12f;
        public const float UseRange = 2.5f;
        public const float CloseRange = 4f;
        public const float PassClearance = 1.4f;
        public const float MaxIntentSeconds = 15f;

        public static bool DirectBlocker(bool hit, bool firstSolidIsDoor, float distance,
            bool access, bool claimed, bool retired)
        {
            return hit && firstSolidIsDoor && Finite(distance) && distance >= 0 &&
                distance <= ScanRange && access && !claimed && !retired;
        }

        // A following radius is not proof of arrival through a wall. A committed
        // doorway must finish crossing even when the leader is less than 3m away.
        public static bool ContinueShortFollow(bool authorized, bool matchingGoal,
            float distance, bool committedCrossing, bool blocked)
        {
            return authorized && matchingGoal && Finite(distance) && distance >= 0 &&
                distance < 3f && (committedCrossing || blocked);
        }

        // A directly observed door on the current leader segment is route
        // knowledge, not knowledge of another entrance. Keep the actual leader
        // goal long enough for native pathing to reach and operate that door.
        public static bool UseLeaderDoorGoal(bool authorized, bool matchingGoal,
            float distance, bool committedCrossing, bool blocked, bool directDoorObserved)
        {
            if (!authorized || !matchingGoal || !Finite(distance) || distance < 0) return false;
            if (committedCrossing) return true;
            if (distance > ScanRange) return false;
            return directDoorObserved || (distance < 3f && blocked);
        }

        public static bool RouteCandidate(float actorSide, float goalSide, float distance,
            float heightDifference, bool access, bool claimed, bool retired)
        {
            return access && !claimed && !retired && CrossesPlane(actorSide, goalSide) &&
                Finite(distance) && distance <= ScanRange && distance >= 0 &&
                Finite(heightDifference) && Math.Abs(heightDifference) <= 1.5f;
        }

        public static bool FullLeg(bool nativeComplete, float endpointError, int count)
        {
            return nativeComplete && count > 0 && count <= 128 &&
                Finite(endpointError) && endpointError >= 0 && endpointError <= 0.75f;
        }

        public static DoorTravelAction Evaluate(DoorTravelInput x)
        {
            if (x.Phase == DoorTravelPhase.None) return DoorTravelAction.Native;
            if (!x.Authorized || !x.DoorValid || !x.AccessAllowed || x.Keyed || x.CannotClose ||
                (x.TimedOut && x.Phase != DoorTravelPhase.Closing))
                return DoorTravelAction.Cancel;
            if (x.Phase == DoorTravelPhase.Approach)
            {
                if (x.IsOpen) return DoorTravelAction.Cancel; // Somebody else opened it: no closing claim.
                if (!x.InUseRange) return DoorTravelAction.Approach;
                return x.CanInteract ? DoorTravelAction.Open : DoorTravelAction.Wait;
            }
            if (x.Phase == DoorTravelPhase.Opening)
                return x.IsOpen && x.CanInteract ? DoorTravelAction.Cross : DoorTravelAction.Wait;
            if (x.Phase == DoorTravelPhase.Crossing)
            {
                if (!x.IsOpen) return DoorTravelAction.Cancel; // Do not fight another user's close.
                return x.Crossed ? DoorTravelAction.Native : DoorTravelAction.Cross;
            }
            if (x.Phase == DoorTravelPhase.Closing)
            {
                // We opened this door and are standing at the safe exit. Do not
                // abandon the close obligation merely because another party
                // member needs longer to clear the doorway. Follow remains
                // intentionally paused until it is safe to close.
                if (!x.IsOpen) return DoorTravelAction.Finish;
                if (!x.OpenedHere || !x.WithinCloseRange) return DoorTravelAction.Cancel;
                return x.CrossingClear && x.CloseDelayElapsed && x.CanInteract ? DoorTravelAction.Close : DoorTravelAction.Native;
            }
            if (x.Phase == DoorTravelPhase.ClosePending)
                return x.IsOpen ? DoorTravelAction.Native : DoorTravelAction.Finish;
            return DoorTravelAction.Cancel;
        }

        public static bool CrossesPlane(float actorSide, float goalSide)
        {
            return Finite(actorSide) && Finite(goalSide) && Math.Abs(actorSide) >= 0.25f &&
                Math.Abs(goalSide) >= 0.4f && Math.Sign(actorSide) != Math.Sign(goalSide);
        }
        public static bool HasCrossed(float entrySign, float side, float lateral, float halfWidth)
        {
            return Finite(entrySign) && Math.Abs(entrySign) == 1f && Finite(side) &&
                Finite(lateral) && Finite(halfWidth) && halfWidth > 0 &&
                side * entrySign <= -PassClearance && Math.Abs(lateral) <= halfWidth + 0.8f;
        }
        public static bool OccupiesDoorway(float lateral, float side, float height, float radius, float halfWidth)
        {
            if (!Finite(lateral) || !Finite(side) || !Finite(height) || !Finite(radius) || !Finite(halfWidth) || radius < 0 || halfWidth <= 0) return true;
            return Math.Abs(lateral) <= halfWidth + radius + 0.25f && Math.Abs(side) <= 1.1f + radius &&
                height >= -2f && height <= 3.5f;
        }
        public static bool Access(bool localBoundOwner, bool actorAuthority, bool doorAuthority,
            bool keyed, bool cannotClose, bool requiresWard, bool nativeWardAllows)
        {
            return localBoundOwner && actorAuthority && doorAuthority && !keyed && !cannotClose &&
                (!requiresWard || nativeWardAllows);
        }
        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}

using System;

namespace LivingWorld.Companions.VikingAISalvage
{
    public enum WorkState
    {
        Proposed,
        Reserved,
        Traveling,
        AtTarget,
        Acting,
        Committed,
        Completed,
        Cancelled
    }

    public sealed class WorkTransaction
    {
        public string ActorIdentity;
        public string TargetIdentity;
        public string ReservationToken;
        public WorkState State;
        public string Failure;
    }

    public static class WorkTransactionPolicy
    {
        public static WorkTransaction Begin(string actorIdentity, string targetIdentity, string reservationToken)
        {
            if (string.IsNullOrEmpty(actorIdentity) || string.IsNullOrEmpty(targetIdentity) || string.IsNullOrEmpty(reservationToken))
                throw new ArgumentException("actor, target and reservation identities are required");
            return new WorkTransaction {
                ActorIdentity = actorIdentity,
                TargetIdentity = targetIdentity,
                ReservationToken = reservationToken,
                State = WorkState.Proposed
            };
        }

        public static bool Reserve(WorkTransaction work, bool authorityGranted, bool resourceAvailable)
        {
            if (!At(work, WorkState.Proposed)) return false;
            if (!authorityGranted || !resourceAvailable) return Cancel(work, authorityGranted ? "resource-unavailable" : "authority-denied");
            work.State = WorkState.Reserved;
            return true;
        }

        public static bool StartTravel(WorkTransaction work) { return Advance(work, WorkState.Reserved, WorkState.Traveling); }
        public static bool Arrive(WorkTransaction work) { return Advance(work, WorkState.Traveling, WorkState.AtTarget); }
        public static bool StartAction(WorkTransaction work) { return Advance(work, WorkState.AtTarget, WorkState.Acting); }

        public static bool Commit(WorkTransaction work, bool targetStillValid, bool inputConsumed, bool outputRecorded)
        {
            if (!At(work, WorkState.Acting)) return false;
            if (!targetStillValid || !inputConsumed || !outputRecorded)
                return Cancel(work, !targetStillValid ? "target-invalid" : "transaction-incomplete");
            work.State = WorkState.Committed;
            return true;
        }

        public static bool Complete(WorkTransaction work) { return Advance(work, WorkState.Committed, WorkState.Completed); }

        public static bool InterruptForCombat(WorkTransaction work)
        {
            if (work == null || work.State == WorkState.Committed || work.State == WorkState.Completed || work.State == WorkState.Cancelled)
                return false;
            Cancel(work, "combat-handoff");
            return true;
        }

        private static bool Advance(WorkTransaction work, WorkState from, WorkState to)
        {
            if (!At(work, from)) return false;
            work.State = to;
            return true;
        }

        private static bool Cancel(WorkTransaction work, string reason)
        {
            work.State = WorkState.Cancelled;
            work.Failure = reason;
            return false;
        }

        private static bool At(WorkTransaction work, WorkState state) { return work != null && work.State == state; }
    }
}

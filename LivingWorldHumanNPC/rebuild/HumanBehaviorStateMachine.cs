using System;

namespace LivingWorld.HumanNpcV2
{
    public enum HumanBehaviorState
    {
        AwaitingAppearance,
        AwaitingLeader,
        IdleGuard,
        Traversal,
        TraversalSprint,
        CatchUp,
        CombatReadiness,
        NativeCombat,
        CombatClear,
        Regroup
    }

    internal struct HumanBehaviorSignal
    {
        internal bool InputValid;
        internal bool AppearanceReady;
        internal bool LeaderAvailable;
        internal bool LeaderMoving;
        internal bool LeaderSprinting;
        internal bool LeaderArmed;
        internal bool NativeAlert;
        internal bool CommittedThreat;
        internal float LeaderDistance;
        internal float FormationDistance;
    }

    internal sealed class HumanBehaviorStateMachine
    {
        const float CatchUpEnterDistance = 12f;
        const float CatchUpReleaseDistance = 7f;
        const float RegroupArrivalDistance = 2.4f;

        readonly CombatStateLatch combat;

        internal HumanBehaviorState State { get; private set; }
        internal int ClearScans { get { return combat.ClearScans; } }
        internal int RequiredClearScans { get { return combat.RequiredClearScans; } }

        internal HumanBehaviorStateMachine(int requiredClearScans)
        {
            combat = new CombatStateLatch(requiredClearScans);
            State = HumanBehaviorState.AwaitingAppearance;
        }

        internal HumanBehaviorState Step(HumanBehaviorSignal signal)
        {
            if (!signal.InputValid) return State;

            if (!signal.AppearanceReady)
            {
                State = HumanBehaviorState.AwaitingAppearance;
                return State;
            }

            if (signal.CommittedThreat)
            {
                combat.Observe(true);
                State = HumanBehaviorState.NativeCombat;
                return State;
            }

            if (combat.Active)
            {
                if (combat.Observe(false))
                {
                    State = HumanBehaviorState.CombatClear;
                    return State;
                }

                if (!signal.LeaderAvailable)
                {
                    State = HumanBehaviorState.AwaitingLeader;
                    return State;
                }

                if (signal.NativeAlert || signal.LeaderArmed)
                {
                    State = HumanBehaviorState.CombatReadiness;
                    return State;
                }

                State = HumanBehaviorState.Regroup;
                return State;
            }

            if (!signal.LeaderAvailable)
            {
                State = HumanBehaviorState.AwaitingLeader;
                return State;
            }

            if (signal.NativeAlert || signal.LeaderArmed)
            {
                State = HumanBehaviorState.CombatReadiness;
                return State;
            }

            if (State == HumanBehaviorState.Regroup && signal.FormationDistance > RegroupArrivalDistance)
                return State;

            if (signal.LeaderDistance >= CatchUpEnterDistance ||
                (State == HumanBehaviorState.CatchUp && signal.LeaderDistance > CatchUpReleaseDistance))
            {
                State = HumanBehaviorState.CatchUp;
                return State;
            }

            if (signal.LeaderSprinting)
            {
                State = HumanBehaviorState.TraversalSprint;
                return State;
            }

            if (signal.LeaderMoving)
            {
                State = HumanBehaviorState.Traversal;
                return State;
            }

            State = HumanBehaviorState.IdleGuard;
            return State;
        }

        internal void Retain()
        {
            combat.Retain();
        }
    }
}

using System;

namespace LivingWorld.HumanNpcV2
{
    internal sealed class CombatCohesionLatch
    {
        readonly int requiredOutsideScans;
        readonly float outerLeaderDistance;
        readonly float outerPartyDistance;
        readonly float pairLeaderFloor;
        readonly float innerLeaderDistance;
        readonly float innerPartyDistance;

        int outsideScans;

        internal bool Returning { get; private set; }
        internal bool JustEntered { get; private set; }
        internal bool JustExited { get; private set; }
        internal int OutsideScans { get { return outsideScans; } }

        internal CombatCohesionLatch(int requiredOutsideScans, float outerLeaderDistance,
            float outerPartyDistance, float pairLeaderFloor, float innerLeaderDistance,
            float innerPartyDistance)
        {
            if (requiredOutsideScans < 1) throw new ArgumentOutOfRangeException("requiredOutsideScans");
            if (innerLeaderDistance >= outerLeaderDistance || innerPartyDistance >= outerPartyDistance)
                throw new ArgumentException("Combat cohesion requires distinct inner and outer boundaries.");
            this.requiredOutsideScans = requiredOutsideScans;
            this.outerLeaderDistance = outerLeaderDistance;
            this.outerPartyDistance = outerPartyDistance;
            this.pairLeaderFloor = pairLeaderFloor;
            this.innerLeaderDistance = innerLeaderDistance;
            this.innerPartyDistance = innerPartyDistance;
        }

        internal bool Observe(bool committedThreat, float leaderDistance, float nearestPartyDistance)
        {
            JustEntered = false;
            JustExited = false;

            if (Returning)
            {
                bool partyInside = nearestPartyDistance < 0f || nearestPartyDistance <= innerPartyDistance;
                if (leaderDistance <= innerLeaderDistance && partyInside)
                {
                    Returning = false;
                    JustExited = true;
                    outsideScans = 0;
                }
                return Returning;
            }

            if (!committedThreat)
            {
                outsideScans = 0;
                return false;
            }

            bool partySeparated = nearestPartyDistance >= 0f && nearestPartyDistance > outerPartyDistance &&
                leaderDistance > pairLeaderFloor;
            bool outside = leaderDistance > outerLeaderDistance || partySeparated;
            outsideScans = outside ? outsideScans + 1 : 0;
            if (outsideScans >= requiredOutsideScans)
            {
                Returning = true;
                JustEntered = true;
                outsideScans = 0;
            }
            return Returning;
        }

        internal bool Retain()
        {
            JustEntered = false;
            JustExited = false;
            return Returning;
        }
    }
}

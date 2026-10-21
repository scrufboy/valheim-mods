using System;

namespace LivingWorld.HumanNpcV2
{
    internal sealed class CombatStateLatch
    {
        readonly int requiredClearScans;

        internal bool Active { get; private set; }
        internal int ClearScans { get; private set; }
        internal int RequiredClearScans { get { return requiredClearScans; } }

        internal CombatStateLatch(int requiredClearScans)
        {
            if (requiredClearScans < 1) throw new ArgumentOutOfRangeException("requiredClearScans");
            this.requiredClearScans = requiredClearScans;
        }

        internal bool Observe(bool committedThreat)
        {
            if (committedThreat)
            {
                Active = true;
                ClearScans = 0;
                return true;
            }
            if (!Active) return false;
            ClearScans++;
            if (ClearScans < requiredClearScans) return true;
            Active = false;
            ClearScans = 0;
            return false;
        }

        internal void Retain()
        {
            // Deliberately empty. A transient failed tick is not evidence for a
            // state change, so neither Active nor ClearScans may move.
        }
    }
}

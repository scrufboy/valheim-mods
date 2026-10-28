// Living World C.O.R.E. - companion bomb throw (1.0.32.11). Pure decision law, no Unity: offline-testable.
// First shared thrown-area capability (PROJECT-BOUNDARIES special-attack track). One bounded special action
// inside native combat: native MonsterAI keeps target, movement and facing; the throw itself is the native
// Humanoid attack of a vanilla bomb item, so projectile, blast, ownership and consumption are the game's own.
// Native data (log, Valheim 1.0.20): BombOoze/BombBile aiRange 2, blast radius 4, hitFriendly/hitOwner false.
// Native choice would throw at 2 m, so bombs never sit in the inventory: a finite reserve is kept on the
// person and one bomb enters the hands only for the throw.
using System;

namespace LivingWorld.Core
{
    public struct BombSituation
    {
        public int Reserve;              // bombs this person still carries
        public float TargetDistance;     // to the current native target (-1 none)
        public bool TargetVisible;       // native line of sight
        public bool Busy;                // attacking, drawing, staggered, blocking or already throwing
        public float NearestAllyToTarget;// leader or companion closest to the target (metres; +inf none)
        public float SelfToTarget;       // same as TargetDistance, for the owner-safety check
        public int HostilesNearTarget;   // enemies within the blast radius of the target, target included
        public float TargetMaxHealth;    // native max health of the target
        public float FacingAngle;        // degrees between own forward and the target (native aim turns at most 90)
    }

    public sealed class BombPolicy
    {
        public const float BlastRadius = 4f;          // vanilla oozebomb/bilebomb explosion radius
        public const float SafetyMargin = 1.5f;       // no ally inside blast + margin
        public const float MinRange = 6f;             // never at its feet
        public const float MaxRange = 14f;            // inside the throw's flight at vel 20, gravity 10
        public const float Cooldown = 12f;            // seconds between throws per person
        public const int CrowdAtLeast = 2;            // worth a bomb: a group...
        public const float ToughAtLeast = 200f;       // ...or one tough enemy
        public const float MaxFacing = 45f;           // native MonsterAI turns to its target; wait until it has

        float nextAllowed;

        public string LastReason = "";

        public bool Decide(float now, BombSituation s)
        {
            if (s.Reserve <= 0) { LastReason = "none carried"; return false; }
            if (now < nextAllowed) { LastReason = "cooldown"; return false; }
            if (s.TargetDistance < 0f) { LastReason = "no target"; return false; }
            if (s.Busy) { LastReason = "busy"; return false; }
            if (s.TargetDistance < MinRange) { LastReason = "too close"; return false; }
            if (s.TargetDistance > MaxRange) { LastReason = "too far"; return false; }
            if (!s.TargetVisible) { LastReason = "no line of sight"; return false; }
            if (s.FacingAngle > MaxFacing) { LastReason = "not facing"; return false; }
            if (s.NearestAllyToTarget < BlastRadius + SafetyMargin) { LastReason = "ally near target"; return false; }
            if (s.SelfToTarget < BlastRadius + SafetyMargin) { LastReason = "self in blast"; return false; }
            if (s.HostilesNearTarget < CrowdAtLeast && s.TargetMaxHealth < ToughAtLeast) { LastReason = "not worth a bomb"; return false; }
            LastReason = "throw";
            return true;
        }

        /// <summary>Called once the throw actually started (native attack accepted).</summary>
        public void Started(float now) { nextAllowed = now + Cooldown; }

        /// <summary>A refused or interrupted throw retries soon, not after the full cooldown.</summary>
        public void Failed(float now) { nextAllowed = now + 2f; }
    }
}

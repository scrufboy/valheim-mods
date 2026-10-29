// Living World C.O.R.E. - party clouds and hazard awareness (1.0.32.15). Pure, offline-testable.
// Brian 2026-10-29: own and party bomb clouds get a reduced effect for the player class and the companions, and the
// party has a heightened awareness of clouds. "It wouldn't make sense that people in a party would not know where the
// poison is." Enemy clouds stay fully native. Native SE_Poison / SE_Burning keep doing every point of damage.
using System;

namespace LivingWorld.Core
{
    public enum CloudActor { Other, Player, Companion }

    public static class CloudExposure
    {
        /// <summary>
        /// The game's own rule leaves a cloud's thrower (m_hitOwner off), same-named characters (m_hitSame off) and
        /// friends (m_hitFriendly) untouched. For a party cloud hitting a party member this returns the fraction of
        /// the cloud's damage that member takes instead; 0 means the game's own rule stands.
        /// </summary>
        /// <param name="sameActor">the target is the thrower</param>
        /// <param name="localPlayer">the player is the local leader (other network players keep vanilla)</param>
        public static float Fraction(CloudActor owner, CloudActor target, bool sameActor, bool localPlayer,
            bool companionsOn, bool playerOn, float fraction)
        {
            if (fraction <= 0f) return 0f;
            if (fraction > 1f) fraction = 1f;
            if (owner == CloudActor.Other || target == CloudActor.Other) return 0f;
            if (target == CloudActor.Player)
            {
                if (!playerOn || !localPlayer) return 0f;
                return owner == CloudActor.Companion || sameActor ? fraction : 0f;   // own bomb, or a companion's
            }
            // target is a companion: its own cloud, a partner's, or the leader's
            return companionsOn ? fraction : 0f;
        }

        /// <summary>A lingering harmful cloud (poison or fire over time), not an instant blast.</summary>
        public static bool Harmful(float poison, float fire, float ttl)
        {
            return (poison > 0f || fire > 0f) && ttl >= 2f;
        }
    }

    /// <summary>
    /// When to step out of a cloud. Enter after being inside for Confirm seconds; leave the cloud with margin
    /// (ClearMargin) before stopping; give up after MaxSeconds (pinned, cornered) and wait Cooldown before trying again.
    /// </summary>
    public sealed class HazardEscapePolicy
    {
        public const float Confirm = .25f, ClearMargin = 1.5f, InsideMargin = .4f, MaxSeconds = 5f, Cooldown = 1.5f;
        float insideSince = -1f, startedAt, endedAt = float.NegativeInfinity;
        public bool Active { get; private set; }
        public string Outcome { get; private set; }

        public HazardEscapePolicy() { Outcome = ""; }

        /// <param name="distance">horizontal distance from the cloud centre (+inf: no known cloud)</param>
        /// <param name="radius">cloud radius</param>
        public bool Step(float now, float distance, float radius, bool dead)
        {
            if (dead) { Active = false; insideSince = -1f; return false; }
            bool inside = distance <= radius + InsideMargin;
            if (Active)
            {
                if (distance >= radius + ClearMargin) return End(now, "clear");
                if (now - startedAt >= MaxSeconds) return End(now, "time");
                return true;
            }
            if (!inside) { insideSince = -1f; return false; }
            if (insideSince < 0f) insideSince = now;
            if (now - insideSince < Confirm || now - endedAt < Cooldown) return false;
            Active = true; startedAt = now; Outcome = ""; return true;
        }

        bool End(float now, string why) { Active = false; endedAt = now; insideSince = -1f; Outcome = why; return false; }
    }
}

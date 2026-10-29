// Living World C.O.R.E. - gait matching (1.0.32.10). Pure decision law, no Unity: offline-testable.
// Brian, 2026-10-28: "the bots do not match pace when I walk slow. they should also walk slow."
// Live evidence the same day: with the leader at 0.8-2.6 m/s, companions alternated between stopped and a
// 3.5-4 m/s jog, because the calm pace floor was the native jog speed. This law picks walk or jog and a speed
// that follows the leader's smoothed pace, plus a bounded catch-up term when the companion falls behind its spot.
using System;

namespace LivingWorld.Core
{
    public struct GaitChoice
    {
        public bool Walk;      // native walk mode (Character.SetWalk): walk animation at Speed
        public float Speed;    // metres per second for the chosen mode
        public bool Matching;  // slower than the native jog because the leader is
    }

    public sealed class GaitPolicy
    {
        public const float Margin = .15f;          // a touch faster than the leader so the party does not drift apart
        public const float MinSpeed = .8f;         // never crawl
        public const float WalkBelow = 2.2f;       // under this the walk animation reads right; above it, a slow jog
        public const float Slack = 3f;             // metres from the spot that need no catch-up
        public const float CatchGain = .35f;       // extra m/s per metre beyond the slack
        public const float MatchWithin = 8f;       // farther from the leader: the ordinary pace and catch-up rules
        public const float Smoothing = .6f;        // seconds; leader velocity is noisy frame to frame
        // 1.0.32.16 (Brian live 2026-10-29: "their walking is a little slow ... if I'm just standing there"): matching is
        // for a leader who is walking. Standing still (the kernel's LeaderMoving threshold), the ordinary pace takes them
        // to their spot or guard post. Telemetry: companions moved at 0.7 m/s with the leader standing after 1.0.32.10,
        // 2.7-3.4 m/s before.
        public const float StandingBelow = .35f;

        float smooth = -1f;

        public void Reset() { smooth = -1f; }

        public float SmoothedLeaderSpeed { get { return smooth < 0f ? 0f : smooth; } }

        /// <param name="leaderSpeed">leader ground speed this tick</param>
        /// <param name="leaderWalkToggle">the leader's own walk toggle is on</param>
        /// <param name="leaderDistance">companion to leader</param>
        /// <param name="spotDistance">companion to its formation spot</param>
        /// <param name="nativeJog">the companion's native jog speed (Humanoid.m_speed at spawn)</param>
        public GaitChoice Decide(float dt, float leaderSpeed, bool leaderWalkToggle, float leaderDistance, float spotDistance, float nativeJog)
        {
            if (smooth < 0f) smooth = leaderSpeed;
            else smooth += (leaderSpeed - smooth) * Math.Min(1f, Math.Max(0f, dt) / Smoothing);

            var g = new GaitChoice { Walk = false, Speed = nativeJog, Matching = false };
            if (leaderDistance < 0f || leaderDistance > MatchWithin || leaderSpeed < StandingBelow) return g;
            float lag = Math.Max(0f, spotDistance - Slack) * CatchGain;
            float want = Math.Min(nativeJog, Math.Max(MinSpeed, smooth + Margin + lag));
            g.Speed = want;
            g.Matching = want < nativeJog - .05f;
            g.Walk = g.Matching && (leaderWalkToggle || want < WalkBelow);
            return g;
        }
    }
}

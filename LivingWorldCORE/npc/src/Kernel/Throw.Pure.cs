// Living World C.O.R.E. - throw planning (1.0.32.12). Pure decision law, no Unity: offline-testable.
// Brian, 2026-10-28: throw like the Souls of the Land Pillager, but decide like a player who knows where the
// group is and where it is headed: clustered groups, farther off, incoming; aim at the best point; still miss
// sometimes (native spread, targets that step aside). Never at the leader or a companion.
using System;

namespace LivingWorld.Core
{
    public struct ThrowBody
    {
        public P2 Position;
        public P2 Velocity;
        public float Height;       // ground height (metres), for the ballistic solution
        public float MaxHealth;
    }

    public struct ThrowPlan
    {
        public bool Throw;
        public P2 Aim;             // ground point to land on
        public float AimHeight;
        public float Elevation;    // launch elevation, radians, for the fixed throw speed
        public int Members;        // enemies in the chosen cluster
        public int Incoming;       // of them, closing on the party
        public float Distance;     // to the aim point
        public float Lead;         // metres the aim is ahead of the cluster centre
        public float FriendClearance; // closest friend to the aim point (now or predicted)
        public float TurnNeeded;   // degrees to face the aim point
        public string Reason;
    }

    public static class ThrowPlanner
    {
        public const float Speed = 12f;            // Greydwarf_throw / Souls Pillager throw speed
        public const float Gravity = 10f;          // oozebomb_projectile gravity
        public const float ClusterRadius = 3.5f;   // members within this of a seed form a cluster
        public const float BlastRadius = 4f;       // oozebomb_explosion
        public const float FriendMargin = 1.5f;    // never a friend within blast + margin, now or when it lands
        public const float MinRange = 6f, MaxRange = 13.5f;   // 14.4 m is the flat-ground maximum at 12 m/s
        public const float LeadFactor = .8f;       // aim most of the way to where they are headed
        public const float IncomingWeight = 1.5f;
        public const float ToughAtLeast = 200f;
        public const float MaxTurn = 25f;          // throw once facing within this

        /// <summary>Low-arc launch elevation for a horizontal distance and height difference, or NaN.</summary>
        public static float Elevation(float d, float dh)
        {
            float v2 = Speed * Speed, g = Gravity;
            float root = v2 * v2 - g * (g * d * d + 2f * dh * v2);
            if (root < 0f || d < 1e-3f) return float.NaN;
            return (float)Math.Atan((v2 - Math.Sqrt(root)) / (g * d));
        }

        public static float FlightTime(float d, float elevation) { return d / (Speed * (float)Math.Cos(elevation)); }

        public static ThrowPlan Plan(P2 self, float selfHeight, P2 forward, P2 partyCentre, ThrowBody[] hostiles, int hostileCount,
            ThrowBody[] friends, int friendCount)
        {
            var best = new ThrowPlan { Throw = false, Reason = "no hostiles" };
            float bestScore = 0f;
            string lastReason = hostileCount == 0 ? "no hostiles" : "none in range";
            for (int s = 0; s < hostileCount; s++)
            {
                float seedD = P2.Distance(self, hostiles[s].Position);
                if (seedD < MinRange - ClusterRadius || seedD > MaxRange + ClusterRadius) continue;
                // cluster around this seed
                P2 sum = new P2(0f, 0f), vel = new P2(0f, 0f); float h = 0f, score = 0f, tough = 0f; int n = 0, incoming = 0;
                for (int i = 0; i < hostileCount; i++)
                {
                    if (P2.Distance(hostiles[i].Position, hostiles[s].Position) > ClusterRadius) continue;
                    n++; sum = sum + hostiles[i].Position; vel = vel + hostiles[i].Velocity; h += hostiles[i].Height;
                    tough = Math.Max(tough, hostiles[i].MaxHealth);
                    P2 toParty = (partyCentre - hostiles[i].Position).Normalized;
                    bool closing = P2.Dot(hostiles[i].Velocity, toParty) > .5f;
                    if (closing) incoming++;
                    score += closing ? IncomingWeight : 1f;
                }
                if (n < 2 && tough < ToughAtLeast) { lastReason = "not worth it (single weak)"; continue; }
                P2 centre = sum * (1f / n); vel = vel * (1f / n); h /= n;
                float d0 = P2.Distance(self, centre);
                float e0 = Elevation(d0, h - selfHeight);
                if (float.IsNaN(e0)) { lastReason = "out of reach"; continue; }
                P2 aim = centre + vel * (FlightTime(d0, e0) * LeadFactor);
                float d = P2.Distance(self, aim);
                if (d < MinRange) { lastReason = "too close"; continue; }
                if (d > MaxRange) { lastReason = "too far"; continue; }
                float e = Elevation(d, h - selfHeight);
                if (float.IsNaN(e)) { lastReason = "out of reach"; continue; }
                float t = FlightTime(d, e);
                // hard rule (Brian): never a throw that lands near the leader or a companion, now or on landing
                float clear = float.PositiveInfinity;
                for (int f = 0; f < friendCount; f++)
                {
                    clear = Math.Min(clear, P2.Distance(friends[f].Position, aim));
                    clear = Math.Min(clear, P2.Distance(friends[f].Position + friends[f].Velocity * t, aim));
                }
                if (clear < BlastRadius + FriendMargin) { lastReason = "friend near the landing"; continue; }
                score += Math.Min(1f, d / MaxRange) * .25f;            // farther off is better (they are not on us yet)
                if (score <= bestScore) continue;
                P2 to = aim - self;
                float turn = (float)(Math.Acos(Math.Max(-1f, Math.Min(1f, P2.Dot(forward.Normalized, to.Normalized)))) * 180.0 / Math.PI);
                bestScore = score;
                best = new ThrowPlan
                {
                    Throw = true, Aim = aim, AimHeight = h, Elevation = e, Members = n, Incoming = incoming, Distance = d,
                    Lead = P2.Distance(aim, centre), FriendClearance = clear, TurnNeeded = turn, Reason = "throw"
                };
            }
            if (!best.Throw) best.Reason = lastReason;
            return best;
        }
    }
}

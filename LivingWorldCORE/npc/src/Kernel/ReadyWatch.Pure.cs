// Living World C.O.R.E. - ready watch law (1.0.32.18). Pure, offline-testable.
// Brian 2026-10-29: weapon drawn and the leader standing still, companions should not "just stand still, staring off
// into space": stand their ground, at the ready, turning head and shoulders to keep an eye on things, somewhat mobile in
// the spot. A variant of the guard, not the sweep: each covers the outside half on its own side of the leader, looks
// change every few seconds (sometimes a slow sweep across the half), now and then a short shift of footing.
using System;

namespace LivingWorld.Core
{
    public struct WatchLook
    {
        public float From, To;      // bearings in degrees relative to the leader's facing (+ right, - left)
        public float Seconds;       // dwell (From == To) or sweep duration
    }

    public sealed class ReadyWatchPolicy
    {
        public const float HalfMin = 20f, HalfMax = 160f;       // the outside half on one side, not straight ahead/behind
        public const float MinChange = 35f;                     // a new look is a real turn
        public const float DwellMin = 2.5f, DwellMax = 5f;
        public const float SweepChance = .3f;
        public const float StepEveryMin = 7f, StepEveryMax = 14f, StepChance = .45f;
        public const float StepMin = .4f, StepMax = 1f, MaxDrift = 1.4f;

        readonly Random rng;
        float last = float.NaN;

        public ReadyWatchPolicy(int seed) { rng = new Random(seed); }

        /// <param name="side">+1 right half, -1 left half</param>
        public WatchLook NextLook(int side)
        {
            float s = side >= 0 ? 1f : -1f;
            float b = Pick(s);
            var look = new WatchLook { From = b, To = b, Seconds = DwellMin + (float)rng.NextDouble() * (DwellMax - DwellMin) };
            if (rng.NextDouble() < SweepChance)
            {
                float to = Pick(s);
                look.To = to;
                last = to;
            }
            return look;
        }

        float Pick(float s)
        {
            for (int i = 0; i < 12; i++)
            {
                float b = s * (HalfMin + (float)rng.NextDouble() * (HalfMax - HalfMin));
                if (float.IsNaN(last) || Math.Abs(b - last) >= MinChange) { last = b; return b; }
            }
            last = s * (HalfMin + HalfMax) * .5f;   // fallback: the middle of the half
            return last;
        }

        public float NextStepDelay() { return StepEveryMin + (float)rng.NextDouble() * (StepEveryMax - StepEveryMin); }

        /// <summary>A shift of footing: an offset from the current spot, kept within MaxDrift of home. False = stay put.</summary>
        public bool Step(P2 self, P2 home, out P2 to)
        {
            to = self;
            if (rng.NextDouble() >= StepChance) return false;
            for (int i = 0; i < 6; i++)
            {
                double a = rng.NextDouble() * Math.PI * 2d;
                float r = StepMin + (float)rng.NextDouble() * (StepMax - StepMin);
                P2 c = new P2(self.X + (float)Math.Sin(a) * r, self.Z + (float)Math.Cos(a) * r);
                if (P2.Distance(c, home) <= MaxDrift) { to = c; return true; }
            }
            return false;
        }

        /// <summary>Bearing at time t into a look (linear sweep; a dwell holds).</summary>
        public static float At(WatchLook look, float t)
        {
            if (look.Seconds <= 0f || look.From == look.To) return look.To;
            float k = t / look.Seconds; k = k < 0f ? 0f : k > 1f ? 1f : k;
            return look.From + (look.To - look.From) * k;
        }

        /// <summary>Which half to watch: own side of the leader, unless the partner already holds it.</summary>
        public static int ChooseSide(float lateral, int partnerSide)
        {
            int own = lateral >= 0f ? 1 : -1;
            return partnerSide == own ? -own : own;
        }
    }
}

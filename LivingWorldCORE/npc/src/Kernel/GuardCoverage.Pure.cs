using System;

namespace LivingWorld.Core
{
    // Deterministic memory of completed guard coverage. A direction becomes
    // covered only after its full field-of-view sweep finishes.
    public sealed class GuardCoverage
    {
        public const int SectorCount = 12;
        readonly double[] resolvedAt = new double[SectorCount];
        int preferred = -1;

        public GuardCoverage() { Reset(); }

        public void Reset()
        {
            for (int index = 0; index < resolvedAt.Length; index++)
                resolvedAt[index] = Double.NegativeInfinity;
            preferred = -1;
        }

        public void Resolve(int sector, double now)
        {
            if (!Valid(sector) || Double.IsNaN(now) || Double.IsInfinity(now) || now < 0d) return;
            resolvedAt[sector] = now;
            preferred = Opposite(sector);
        }

        public int Choose(int unavailableMask, int stableOffset)
        {
            unavailableMask &= (1 << SectorCount) - 1;
            if (unavailableMask != 0)
            {
                int start = Normalize(stableOffset);
                int chosen = -1, bestMinimum = -1, bestTotal = -1;
                double oldest = Double.PositiveInfinity;
                for (int step = 0; step < SectorCount; step++)
                {
                    int candidate = (start + step) % SectorCount;
                    if ((unavailableMask & (1 << candidate)) != 0) continue;
                    int minimum = SectorCount, total = 0;
                    for (int occupied = 0; occupied < SectorCount; occupied++)
                    {
                        if ((unavailableMask & (1 << occupied)) == 0) continue;
                        int distance = CircularDistance(candidate, occupied);
                        if (distance < minimum) minimum = distance;
                        total += distance;
                    }
                    if (minimum > bestMinimum || minimum == bestMinimum && total > bestTotal ||
                        minimum == bestMinimum && total == bestTotal && resolvedAt[candidate] < oldest)
                    {
                        chosen = candidate;
                        bestMinimum = minimum;
                        bestTotal = total;
                        oldest = resolvedAt[candidate];
                    }
                }
                return chosen;
            }

            if (Valid(preferred)) return preferred;
            int first = Normalize(stableOffset), best = -1;
            double stalest = Double.PositiveInfinity;
            for (int step = 0; step < SectorCount; step++)
            {
                int candidate = (first + step) % SectorCount;
                if ((unavailableMask & (1 << candidate)) != 0) continue;
                if (resolvedAt[candidate] < stalest)
                {
                    stalest = resolvedAt[candidate];
                    best = candidate;
                }
            }
            return best;
        }

        public static int Opposite(int sector)
        {
            return Valid(sector) ? (sector + SectorCount / 2) % SectorCount : -1;
        }

        public static double Radians(int sector)
        {
            return Valid(sector) ? sector * Math.PI * 2d / SectorCount : 0d;
        }

        static int CircularDistance(int first, int second)
        {
            int distance = Math.Abs(first - second);
            return Math.Min(distance, SectorCount - distance);
        }

        static int Normalize(int value)
        {
            int normalized = value % SectorCount;
            return normalized < 0 ? normalized + SectorCount : normalized;
        }

        static bool Valid(int sector) { return sector >= 0 && sector < SectorCount; }
    }
}

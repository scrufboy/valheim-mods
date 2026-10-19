using System;

namespace LivingWorld.HumanNpc
{
    // Small, deterministic memory for settled guard coverage. It records what
    // the actor actually finished scanning; choosing a destination is therefore
    // driven by missing/stale coverage rather than an idle random roll.
    public sealed class GuardCoveragePolicy
    {
        public const int SectorCount = 12;
        readonly double[] resolvedAt = new double[SectorCount];
        int preferred = -1;

        public GuardCoveragePolicy() { Reset(); }

        public void Reset()
        {
            for (int i = 0; i < resolvedAt.Length; ++i) resolvedAt[i] = Double.NegativeInfinity;
            preferred = -1;
        }

        public void Prefer(int sector)
        {
            preferred = ValidSector(sector) ? sector : -1;
        }

        public void Resolve(int sector, double now)
        {
            if (!ValidSector(sector) || !Finite(now) || now < 0) return;
            resolvedAt[sector] = now;
            preferred = Opposite(sector);
        }

        public int Choose(int unavailableMask, int stableOffset)
        {
            if (ValidSector(preferred) && (unavailableMask & (1 << preferred)) == 0) return preferred;
            int start = Normalize(stableOffset), best = -1;
            double oldest = Double.PositiveInfinity;
            for (int step = 0; step < SectorCount; ++step)
            {
                int sector = (start + step) % SectorCount;
                if ((unavailableMask & (1 << sector)) != 0) continue;
                if (resolvedAt[sector] < oldest)
                { oldest = resolvedAt[sector]; best = sector; }
            }
            return best;
        }

        public double LastResolved(int sector)
        { return ValidSector(sector) ? resolvedAt[sector] : Double.NegativeInfinity; }

        public static int Opposite(int sector)
        { return ValidSector(sector) ? (sector + SectorCount / 2) % SectorCount : -1; }

        public static double Radians(int sector)
        { return ValidSector(sector) ? sector * Math.PI * 2.0 / SectorCount : 0.0; }

        static int Normalize(int value)
        {
            int normalized = value % SectorCount;
            return normalized < 0 ? normalized + SectorCount : normalized;
        }

        static bool ValidSector(int sector) { return sector >= 0 && sector < SectorCount; }
        static bool Finite(double value) { return !Double.IsNaN(value) && !Double.IsInfinity(value); }
    }
}


using System;
using LivingWorld.HumanNpc;

public static class GuardCoveragePolicyTests
{
    static int checks;
    static void Check(bool condition, string message)
    { checks++; if (!condition) throw new Exception(message); }

    public static int Main()
    {
        var coverage = new GuardCoveragePolicy();
        Check(coverage.Choose(0, 3) == 3, "Fresh coverage begins at deterministic actor offset.");
        coverage.Resolve(3, 10);
        Check(coverage.Choose(0, 0) == 9, "A completed scan requires the opposing sector next.");
        Check(GuardCoveragePolicy.Opposite(9) == 3, "Opposing sectors are symmetric.");
        coverage.Resolve(9, 20);
        Check(coverage.Choose(1 << 3, 0) == 9, "One active buddy is covered from the opposing sector.");
        Check(coverage.Choose((1 << 0) | (1 << 4), 0) == 8,
            "A third guard takes the largest uncovered sector instead of copying one buddy.");
        Check(coverage.Choose((1 << 0) | (1 << 3) | (1 << 6), 0) == 9,
            "A fourth guard completes temporary all-round coverage.");
        for (int i = 0; i < GuardCoveragePolicy.SectorCount; ++i) coverage.Resolve(i, 30 + i);
        Check(coverage.Choose(0, 0) == 5, "The explicit opposite of the last completed sector wins before age scoring.");
        coverage.Prefer(-1);
        Check(coverage.Choose(0, 0) == 0, "Without an explicit opposite, the stalest resolved sector is selected.");
        coverage.Reset();
        Check(Double.IsNegativeInfinity(coverage.LastResolved(0)), "Reset removes old-area coverage.");
        Console.WriteLine("GuardCoveragePolicy PASS: " + checks + " checks.");
        return 0;
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LivingWorld.FormidableAI.Kernel
{
    internal sealed class BenchmarkModule : IFormidableCombatModule
    {
        public string Id { get { return "benchmark.class"; } }
        public IEnumerable<CombatProposal> Evaluate(CombatFrame frame)
        {
            yield return new CombatProposal("attack", DecisionDomain.Attack | DecisionDomain.Facing,
                50, frame.AuthorizedTargetIdentity, frame.Time + .1f);
        }
    }

    internal static class KernelBenchmark
    {
        public static int Main()
        {
            const int iterations = 100000;
            CombatArbitrator arbitrator = new CombatArbitrator();
            IList<IFormidableCombatModule> modules = new List<IFormidableCombatModule> { new BenchmarkModule() };
            ThreatObservation[] threats = { new ThreatObservation("enemy", 3f, true, true) };
            CombatFrame frame = new CombatFrame(1, 10f, "actor", "huntress", "enemy", true, true,
                1f, 1f, threats);

            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            long retainedBefore = GC.GetTotalMemory(true);
            int gen0Before = GC.CollectionCount(0);
            Stopwatch watch = Stopwatch.StartNew();
            for (int i = 0; i < iterations; i++) arbitrator.Evaluate(frame, modules);
            watch.Stop();
            long retainedAfter = GC.GetTotalMemory(false);
            Console.WriteLine("iterations=" + iterations +
                " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3") +
                " avg_us=" + (watch.Elapsed.TotalMilliseconds * 1000.0 / iterations).ToString("F3") +
                " gen0_collections=" + (GC.CollectionCount(0) - gen0Before) +
                " retained_bytes_delta=" + (retainedAfter - retainedBefore));
            return 0;
        }
    }
}

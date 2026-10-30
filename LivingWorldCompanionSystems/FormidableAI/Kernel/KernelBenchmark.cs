using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LivingWorld.FormidableAI.Kernel
{
    internal static class KernelBenchmark
    {
        private sealed class StableModule : IBufferedFormidableCombatModule
        {
            private readonly CombatProposal _proposal;
            public string Id { get; private set; }
            public StableModule(string id, CombatProposal proposal) { Id = id; _proposal = proposal; }
            public IEnumerable<CombatProposal> Evaluate(CombatFrame frame) { return null; }
            public void Collect(CombatFrame frame, IList<CombatProposal> output) { output.Add(_proposal); }
        }

        public static int Main()
        {
            CombatFrame frame = new CombatFrame(1, 1f, "actor", "warden", "enemy", true, true, 1f, 1f,
                new[] { new ThreatObservation("enemy", 2f, true, true) });
            var modules = new List<IFormidableCombatModule> {
                new StableModule("warden", new CombatProposal("block", DecisionDomain.Defense, 50, "enemy", 1000000f)),
                new StableModule("archer", new CombatProposal("aim", DecisionDomain.Facing | DecisionDomain.Attack, 40, "enemy", 1000000f)) };
            CombatArbitrator arbiter = new CombatArbitrator(); ArbitrationBuffer buffer = new ArbitrationBuffer();
            for (int i = 0; i < 1000; i++) arbiter.EvaluateInto(frame, modules, buffer);
            GC.Collect(); long before = GC.GetTotalMemory(true); Stopwatch watch = Stopwatch.StartNew();
            const int iterations = 500000;
            for (int i = 0; i < iterations; i++) arbiter.EvaluateInto(frame, modules, buffer);
            watch.Stop(); long after = GC.GetTotalMemory(false);
            Console.WriteLine("iterations=" + iterations + " elapsed_ms=" + watch.Elapsed.TotalMilliseconds.ToString("F3") +
                " retained_bytes_delta=" + (after - before));
            Console.WriteLine("winner_defense=" + buffer.LeaseFor(DecisionDomain.Defense).OwnerId +
                " winner_attack=" + buffer.LeaseFor(DecisionDomain.Attack).OwnerId);
            return 0;
        }
    }
}

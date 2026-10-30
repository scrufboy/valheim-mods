using System;
using System.Collections.Generic;

namespace LivingWorld.FormidableAI.Kernel
{
    internal static class KernelTests
    {
        private sealed class Module : IFormidableCombatModule
        {
            private readonly CombatProposal[] _proposals;
            public string Id { get; private set; }
            public Module(string id, params CombatProposal[] proposals) { Id = id; _proposals = proposals; }
            public IEnumerable<CombatProposal> Evaluate(CombatFrame frame) { return _proposals; }
        }

        private sealed class BrokenModule : IFormidableCombatModule
        {
            public string Id { get { return "broken"; } }
            public IEnumerable<CombatProposal> Evaluate(CombatFrame frame) { throw new InvalidOperationException("isolated failure"); }
        }

        private sealed class BufferedModule : IBufferedFormidableCombatModule
        {
            private readonly CombatProposal _proposal;
            public string Id { get; private set; }
            public BufferedModule(string id, CombatProposal proposal) { Id = id; _proposal = proposal; }
            public IEnumerable<CombatProposal> Evaluate(CombatFrame frame) { throw new Exception("allocating path used"); }
            public void Collect(CombatFrame frame, IList<CombatProposal> output) { output.Add(_proposal); }
        }

        private static int _passed;
        public static int Main()
        {
            Run("frame owns immutable observation array", FrameIsSnapshot);
            Run("unclaimed domains fall back to native", NativeFallback);
            Run("priority wins overlapping domains", PriorityWins);
            Run("ties are independent of module order", TiesAreDeterministic);
            Run("non-overlapping class actions compose", NonOverlappingCompose);
            Run("multi-domain intents are atomic", MultiDomainIsAtomic);
            Run("expired intents cannot take ownership", ExpiredRejected);
            Run("module failure is isolated and disabled", FaultIsolated);
            Run("cancellation hands domains to native", CancellationHandsBack);
            Run("remote owner cannot grant module leases", RemoteFallsBack);
            Run("reused buffer matches compatibility winners", ReusedBufferMatches);
            Run("reused buffer contains isolated faults", ReusedBufferFaultIsolation);
            Run("reused buffer observes cancellation handback", ReusedBufferCancellation);
            Console.WriteLine("PASS: " + _passed + " FormidableAI kernel tests");
            return 0;
        }

        private static CombatFrame Frame(bool ownerReady = true)
        {
            return new CombatFrame(7, 10f, "actor-1", "warden", "enemy-1", ownerReady, true,
                .8f, .6f, new[] { new ThreatObservation("enemy-1", 3f, true, true) });
        }

        private static CombatProposal Proposal(string action, DecisionDomain domains, int priority, float validUntil = 11f)
        { return new CombatProposal(action, domains, priority, "enemy-1", validUntil); }

        private static void FrameIsSnapshot()
        {
            ThreatObservation original = new ThreatObservation("enemy-1", 2f, true, false);
            ThreatObservation[] threats = { original };
            CombatFrame frame = new CombatFrame(1, 0f, "actor", "archer", null, true, false, 2f, -1f, threats);
            threats[0] = new ThreatObservation("enemy-2", 9f, false, true);
            Assert(frame.GetThreat(0).Identity == "enemy-1", "caller array mutated frame");
            Assert(frame.HealthFraction == 1f && frame.StaminaFraction == 0f, "frame did not normalize resource facts");
        }

        private static void NativeFallback()
        {
            ArbitrationResult result = new CombatArbitrator().Evaluate(Frame(), new List<IFormidableCombatModule>());
            Assert(result.LeaseCount == 8, "missing decision domains");
            for (int i = 0; i < result.LeaseCount; i++) Assert(result.GetLease(i).IsNativeFallback, "domain lacked native fallback");
        }

        private static void PriorityWins()
        {
            var modules = new List<IFormidableCombatModule> {
                new Module("low", Proposal("random-block", DecisionDomain.Defense, 10)),
                new Module("warden", Proposal("timed-block", DecisionDomain.Defense, 50)) };
            DomainLease lease = new CombatArbitrator().Evaluate(Frame(), modules).LeaseFor(DecisionDomain.Defense);
            Assert(lease.OwnerId == "warden" && lease.ActionId == "timed-block", "higher priority lost");
        }

        private static void TiesAreDeterministic()
        {
            IFormidableCombatModule a = new Module("alpha", Proposal("a", DecisionDomain.Movement, 20));
            IFormidableCombatModule z = new Module("zeta", Proposal("z", DecisionDomain.Movement, 20));
            string first = new CombatArbitrator().Evaluate(Frame(), new List<IFormidableCombatModule> { z, a }).LeaseFor(DecisionDomain.Movement).OwnerId;
            string second = new CombatArbitrator().Evaluate(Frame(), new List<IFormidableCombatModule> { a, z }).LeaseFor(DecisionDomain.Movement).OwnerId;
            Assert(first == "alpha" && second == "alpha", "registration order changed winner");
        }

        private static void NonOverlappingCompose()
        {
            var modules = new List<IFormidableCombatModule> {
                new Module("warden", Proposal("block", DecisionDomain.Defense, 50)),
                new Module("cleric", Proposal("heal", DecisionDomain.Support, 40)) };
            ArbitrationResult result = new CombatArbitrator().Evaluate(Frame(), modules);
            Assert(result.LeaseFor(DecisionDomain.Defense).OwnerId == "warden", "defense missing");
            Assert(result.LeaseFor(DecisionDomain.Support).OwnerId == "cleric", "support missing");
            Assert(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback, "unclaimed attack lost native fallback");
        }

        private static void MultiDomainIsAtomic()
        {
            var modules = new List<IFormidableCombatModule> {
                new Module("archer", Proposal("reposition-and-aim", DecisionDomain.Movement | DecisionDomain.Facing, 60)),
                new Module("look", Proposal("face-only", DecisionDomain.Facing, 70)) };
            ArbitrationResult result = new CombatArbitrator().Evaluate(Frame(), modules);
            Assert(result.LeaseFor(DecisionDomain.Facing).OwnerId == "look", "high facing intent lost");
            Assert(result.LeaseFor(DecisionDomain.Movement).IsNativeFallback, "partial multi-domain intent was accepted");
        }

        private static void ExpiredRejected()
        {
            var modules = new List<IFormidableCombatModule> { new Module("old", Proposal("old-action", DecisionDomain.Attack, 99, 9f)) };
            Assert(new CombatArbitrator().Evaluate(Frame(), modules).LeaseFor(DecisionDomain.Attack).IsNativeFallback, "expired action won");
        }

        private static void FaultIsolated()
        {
            CombatArbitrator arbiter = new CombatArbitrator();
            var modules = new List<IFormidableCombatModule> { new BrokenModule(), new Module("safe", Proposal("block", DecisionDomain.Defense, 5)) };
            ArbitrationResult result = arbiter.Evaluate(Frame(), modules);
            Assert(result.FaultCount == 1 && result.GetFault(0).ModuleId == "broken", "fault not reported");
            Assert(result.LeaseFor(DecisionDomain.Defense).OwnerId == "safe", "healthy module was lost");
            Assert(arbiter.IsModuleDisabled("broken"), "broken module was not disabled");
            Assert(arbiter.Evaluate(Frame(), modules).FaultCount == 0, "disabled module was retried globally");
        }

        private static void CancellationHandsBack()
        {
            CombatArbitrator arbiter = new CombatArbitrator();
            var modules = new List<IFormidableCombatModule> { new Module("archer", Proposal("aim", DecisionDomain.Facing | DecisionDomain.Attack, 10)) };
            arbiter.Evaluate(Frame(), modules);
            Assert(arbiter.CancelOwner("archer", "target-lost"), "owner cancellation failed");
            ArbitrationResult result = arbiter.Evaluate(Frame(), new List<IFormidableCombatModule>());
            Assert(result.LeaseFor(DecisionDomain.Facing).IsNativeFallback && result.LeaseFor(DecisionDomain.Attack).IsNativeFallback, "cancel did not hand back");
        }

        private static void RemoteFallsBack()
        {
            var modules = new List<IFormidableCombatModule> { new Module("warden", Proposal("block", DecisionDomain.Defense, 100)) };
            Assert(new CombatArbitrator().Evaluate(Frame(false), modules).LeaseFor(DecisionDomain.Defense).IsNativeFallback, "remote instance gained action ownership");
        }

        private static void ReusedBufferMatches()
        {
            var modules = new List<IFormidableCombatModule> {
                new BufferedModule("warden", Proposal("block", DecisionDomain.Defense, 50)),
                new BufferedModule("archer", Proposal("aim", DecisionDomain.Facing | DecisionDomain.Attack, 40)) };
            CombatArbitrator compatibility = new CombatArbitrator();
            ArbitrationResult expected = compatibility.Evaluate(Frame(), modules);
            CombatArbitrator reused = new CombatArbitrator();
            ArbitrationBuffer buffer = new ArbitrationBuffer();
            reused.EvaluateInto(Frame(), modules, buffer);
            for (int i = 0; i < expected.LeaseCount; i++)
            {
                DomainLease a = expected.GetLease(i); DomainLease b = buffer.GetLease(i);
                Assert(a.Domain == b.Domain && a.OwnerId == b.OwnerId && a.ActionId == b.ActionId, "buffer winner mismatch");
            }
            DomainLease sameObject = buffer.LeaseFor(DecisionDomain.Defense);
            reused.EvaluateInto(Frame(), modules, buffer);
            Assert(object.ReferenceEquals(sameObject, buffer.LeaseFor(DecisionDomain.Defense)), "buffer lease object was replaced");
        }

        private static void ReusedBufferFaultIsolation()
        {
            CombatArbitrator arbiter = new CombatArbitrator(); ArbitrationBuffer buffer = new ArbitrationBuffer();
            var modules = new List<IFormidableCombatModule> { new BrokenModule(), new BufferedModule("safe", Proposal("block", DecisionDomain.Defense, 5)) };
            arbiter.EvaluateInto(Frame(), modules, buffer);
            Assert(buffer.FaultCount == 1 && buffer.GetFault(0).ModuleId == "broken", "buffer fault missing");
            Assert(buffer.LeaseFor(DecisionDomain.Defense).OwnerId == "safe", "buffer fault damaged safe winner");
            arbiter.EvaluateInto(Frame(), modules, buffer);
            Assert(buffer.FaultCount == 0, "disabled fault retried in buffer path");
        }

        private static void ReusedBufferCancellation()
        {
            CombatArbitrator arbiter = new CombatArbitrator(); ArbitrationBuffer buffer = new ArbitrationBuffer();
            var modules = new List<IFormidableCombatModule> { new BufferedModule("archer", Proposal("aim", DecisionDomain.Facing | DecisionDomain.Attack, 5)) };
            arbiter.EvaluateInto(Frame(), modules, buffer);
            Assert(arbiter.CancelOwner("archer", "lost"), "buffer owner cancel failed");
            arbiter.EvaluateInto(Frame(), new List<IFormidableCombatModule>(), buffer);
            Assert(buffer.LeaseFor(DecisionDomain.Facing).IsNativeFallback && buffer.LeaseFor(DecisionDomain.Attack).IsNativeFallback, "buffer did not hand back");
        }

        private static void Run(string name, Action test) { test(); _passed++; Console.WriteLine("ok - " + name); }
        private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}

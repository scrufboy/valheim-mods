using System;
using System.Collections.Generic;

namespace LivingWorld.FormidableAI.Shadow
{
    internal static class ShadowTelemetryTests
    {
        private static int _passed;

        public static int Main()
        {
            Run("matching action and timing compare equal", MatchingAction);
            Run("owner conflict wins classification", OwnerConflict);
            Run("target and attack mismatches are distinct", IdentityMismatches);
            Run("early and late predictions retain signed delta", TimingMismatches);
            Run("missing sides are explicit", MissingSides);
            Run("bounded trace evicts oldest deterministically", BoundedTraceEvictsOldest);
            Run("snapshot cannot mutate trace", SnapshotCannotMutateTrace);
            Run("telemetry exposes no execution command", TelemetryHasNoExecutionCommand);
            Console.WriteLine("PASS: " + _passed + " FormidableAI shadow tests");
            return 0;
        }

        private static void MatchingAction()
        {
            ShadowComparison c = ShadowComparer.Compare(Native(DomainOwner.Native, "target", "attack", 10f),
                Prediction(DomainOwner.Native, "target", "attack", 10.1f));
            Check(c.Mismatch == MismatchKind.Match && Near(c.TimingDeltaSeconds, .1f), "match failed");
        }

        private static void OwnerConflict()
        {
            ShadowComparison c = ShadowComparer.Compare(Native(DomainOwner.Native, "target-a", "attack-a", 1f),
                Prediction(DomainOwner.Formidable, "target-b", "attack-b", 1f));
            Check(c.Mismatch == MismatchKind.DomainOwnerConflict, "owner conflict was hidden by later mismatch");
        }

        private static void IdentityMismatches()
        {
            ShadowComparison target = ShadowComparer.Compare(Native(DomainOwner.Native, "a", "x", 1f),
                Prediction(DomainOwner.Native, "b", "x", 1f));
            ShadowComparison attack = ShadowComparer.Compare(Native(DomainOwner.Native, "a", "x", 1f),
                Prediction(DomainOwner.Native, "a", "y", 1f));
            Check(target.Mismatch == MismatchKind.TargetMismatch, "target mismatch lost");
            Check(attack.Mismatch == MismatchKind.AttackMismatch, "attack mismatch lost");
        }

        private static void TimingMismatches()
        {
            ShadowComparison early = ShadowComparer.Compare(Native(DomainOwner.Native, "a", "x", 5f),
                Prediction(DomainOwner.Native, "a", "x", 4.7f));
            ShadowComparison late = ShadowComparer.Compare(Native(DomainOwner.Native, "a", "x", 5f),
                Prediction(DomainOwner.Native, "a", "x", 5.3f));
            Check(early.Mismatch == MismatchKind.PredictionEarly && early.TimingDeltaSeconds < 0f,
                "early prediction classification/delta failed");
            Check(late.Mismatch == MismatchKind.PredictionLate && late.TimingDeltaSeconds > 0f,
                "late prediction classification/delta failed");
        }

        private static void MissingSides()
        {
            Check(ShadowComparer.Compare(null, Prediction(DomainOwner.Native, "a", null, 1f)).Mismatch ==
                MismatchKind.MissingNativeObservation, "missing native not classified");
            Check(ShadowComparer.Compare(Native(DomainOwner.Native, "a", null, 1f), null).Mismatch ==
                MismatchKind.MissingFormidablePrediction, "missing prediction not classified");
        }

        private static void BoundedTraceEvictsOldest()
        {
            var trace = new BoundedShadowTrace(3);
            for (int i = 0; i < 5; i++) trace.Add("npc", ShadowComparer.Compare(
                Native(DomainOwner.Native, "a", "attack-" + i, i),
                Prediction(DomainOwner.Native, "a", "attack-" + i, i)));
            IList<ShadowTraceEntry> entries = trace.Snapshot();
            Check(trace.Count == 3 && entries.Count == 3, "trace exceeded capacity");
            Check(entries[0].Sequence == 2 && entries[1].Sequence == 3 && entries[2].Sequence == 4,
                "trace order/eviction changed");
        }

        private static void SnapshotCannotMutateTrace()
        {
            var trace = new BoundedShadowTrace(2);
            trace.Add("npc", ShadowComparer.Compare(Native(DomainOwner.Native, "a", "x", 1f),
                Prediction(DomainOwner.Native, "a", "x", 1f)));
            bool rejected = false;
            try { trace.Snapshot().Add(null); }
            catch (NotSupportedException) { rejected = true; }
            Check(rejected && trace.Count == 1, "snapshot mutated trace");
        }

        private static void TelemetryHasNoExecutionCommand()
        {
            Type[] types = { typeof(NativeObservation), typeof(FormidablePrediction), typeof(ShadowComparison),
                typeof(ShadowTraceEntry) };
            foreach (Type type in types)
            foreach (var property in type.GetProperties())
            {
                string name = property.Name.ToLowerInvariant();
                Check(!name.Contains("command") && !name.Contains("velocity") && !name.Contains("destination") &&
                    !name.Contains("direction"), "execution surface leaked: " + type.Name + "." + property.Name);
            }
        }

        private static NativeObservation Native(DomainOwner owner, string target, string attack, float time)
        {
            return new NativeObservation(CombatDomain.Defense, owner, ShadowAction.Block, target, attack, time);
        }

        private static FormidablePrediction Prediction(DomainOwner owner, string target, string attack, float time)
        {
            return new FormidablePrediction(CombatDomain.Defense, owner, ShadowAction.Block,
                "observed-strike", target, attack, time);
        }

        private static bool Near(float left, float right) { return Math.Abs(left - right) < .0001f; }
        private static void Run(string name, Action test) { test(); _passed++; Console.WriteLine("ok - " + name); }
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    }
}

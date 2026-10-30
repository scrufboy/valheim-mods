using System;
using System.Collections.Generic;

namespace LivingWorld.FormidableAI.Shadow
{
    public enum CombatDomain
    {
        Perception,
        Targeting,
        Movement,
        Facing,
        Defense,
        Weapon,
        Attack,
        Support,
        Disengagement
    }

    public enum DomainOwner
    {
        None,
        Native,
        Formidable,
        NativeFallback
    }

    public enum ShadowAction
    {
        None,
        Observe,
        Investigate,
        Approach,
        Reposition,
        Face,
        Block,
        Dodge,
        Attack,
        Cast,
        Support,
        Disengage,
        Yield
    }

    public enum MismatchKind
    {
        Match,
        MissingNativeObservation,
        MissingFormidablePrediction,
        MissingIdentity,
        DomainOwnerConflict,
        TargetMismatch,
        AttackMismatch,
        ActionMismatch,
        PredictionEarly,
        PredictionLate
    }

    public sealed class NativeObservation
    {
        public CombatDomain Domain { get; private set; }
        public DomainOwner Owner { get; private set; }
        public ShadowAction Action { get; private set; }
        public string TargetIdentity { get; private set; }
        public string AttackIdentity { get; private set; }
        public float ActionTimeSeconds { get; private set; }

        public NativeObservation(CombatDomain domain, DomainOwner owner, ShadowAction action,
            string targetIdentity, string attackIdentity, float actionTimeSeconds)
        {
            Domain = domain;
            Owner = owner;
            Action = action;
            TargetIdentity = targetIdentity;
            AttackIdentity = attackIdentity;
            ActionTimeSeconds = actionTimeSeconds;
        }
    }

    public sealed class FormidablePrediction
    {
        public CombatDomain Domain { get; private set; }
        public DomainOwner ExpectedOwner { get; private set; }
        public ShadowAction Action { get; private set; }
        public string Reason { get; private set; }
        public string TargetIdentity { get; private set; }
        public string AttackIdentity { get; private set; }
        public float PredictedTimeSeconds { get; private set; }

        public FormidablePrediction(CombatDomain domain, DomainOwner expectedOwner, ShadowAction action,
            string reason, string targetIdentity, string attackIdentity, float predictedTimeSeconds)
        {
            Domain = domain;
            ExpectedOwner = expectedOwner;
            Action = action;
            Reason = reason;
            TargetIdentity = targetIdentity;
            AttackIdentity = attackIdentity;
            PredictedTimeSeconds = predictedTimeSeconds;
        }
    }

    public sealed class ShadowComparison
    {
        public MismatchKind Mismatch { get; private set; }
        public float TimingDeltaSeconds { get; private set; }
        public NativeObservation Native { get; private set; }
        public FormidablePrediction Prediction { get; private set; }

        public ShadowComparison(MismatchKind mismatch, float timingDeltaSeconds,
            NativeObservation native, FormidablePrediction prediction)
        {
            Mismatch = mismatch;
            TimingDeltaSeconds = timingDeltaSeconds;
            Native = native;
            Prediction = prediction;
        }
    }

    public static class ShadowComparer
    {
        public const float DefaultTimingToleranceSeconds = .12f;

        public static ShadowComparison Compare(NativeObservation native,
            FormidablePrediction prediction, float timingToleranceSeconds)
        {
            if (timingToleranceSeconds < 0f) throw new ArgumentOutOfRangeException("timingToleranceSeconds");
            if (native == null && prediction == null)
                return new ShadowComparison(MismatchKind.MissingIdentity, 0f, null, null);
            if (native == null)
                return new ShadowComparison(MismatchKind.MissingNativeObservation, 0f, null, prediction);
            if (prediction == null)
                return new ShadowComparison(MismatchKind.MissingFormidablePrediction, 0f, native, null);

            float delta = prediction.PredictedTimeSeconds - native.ActionTimeSeconds;
            if (native.Domain != prediction.Domain || native.Owner != prediction.ExpectedOwner)
                return new ShadowComparison(MismatchKind.DomainOwnerConflict, delta, native, prediction);
            if (string.IsNullOrEmpty(native.TargetIdentity) || string.IsNullOrEmpty(prediction.TargetIdentity))
                return new ShadowComparison(MismatchKind.MissingIdentity, delta, native, prediction);
            if (!string.Equals(native.TargetIdentity, prediction.TargetIdentity, StringComparison.Ordinal))
                return new ShadowComparison(MismatchKind.TargetMismatch, delta, native, prediction);
            if (!SameOptionalIdentity(native.AttackIdentity, prediction.AttackIdentity))
                return new ShadowComparison(MismatchKind.AttackMismatch, delta, native, prediction);
            if (native.Action != prediction.Action)
                return new ShadowComparison(MismatchKind.ActionMismatch, delta, native, prediction);
            if (delta < -timingToleranceSeconds)
                return new ShadowComparison(MismatchKind.PredictionEarly, delta, native, prediction);
            if (delta > timingToleranceSeconds)
                return new ShadowComparison(MismatchKind.PredictionLate, delta, native, prediction);
            return new ShadowComparison(MismatchKind.Match, delta, native, prediction);
        }

        public static ShadowComparison Compare(NativeObservation native, FormidablePrediction prediction)
        {
            return Compare(native, prediction, DefaultTimingToleranceSeconds);
        }

        private static bool SameOptionalIdentity(string left, string right)
        {
            if (string.IsNullOrEmpty(left) && string.IsNullOrEmpty(right)) return true;
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }

    public sealed class ShadowTraceEntry
    {
        public long Sequence { get; private set; }
        public string ActorIdentity { get; private set; }
        public ShadowComparison Comparison { get; private set; }

        public ShadowTraceEntry(long sequence, string actorIdentity, ShadowComparison comparison)
        {
            Sequence = sequence;
            ActorIdentity = actorIdentity;
            Comparison = comparison;
        }
    }

    public sealed class BoundedShadowTrace
    {
        private readonly ShadowTraceEntry[] _entries;
        private int _start;
        private int _count;
        private long _nextSequence;

        public int Capacity { get { return _entries.Length; } }
        public int Count { get { return _count; } }

        public BoundedShadowTrace(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException("capacity");
            _entries = new ShadowTraceEntry[capacity];
        }

        public ShadowTraceEntry Add(string actorIdentity, ShadowComparison comparison)
        {
            if (string.IsNullOrEmpty(actorIdentity)) throw new ArgumentException("actor identity is required");
            if (comparison == null) throw new ArgumentNullException("comparison");
            var entry = new ShadowTraceEntry(_nextSequence++, actorIdentity, comparison);
            int index;
            if (_count < _entries.Length)
            {
                index = (_start + _count) % _entries.Length;
                _count++;
            }
            else
            {
                index = _start;
                _start = (_start + 1) % _entries.Length;
            }
            _entries[index] = entry;
            return entry;
        }

        public IList<ShadowTraceEntry> Snapshot()
        {
            var result = new List<ShadowTraceEntry>(_count);
            for (int i = 0; i < _count; i++)
                result.Add(_entries[(_start + i) % _entries.Length]);
            return result.AsReadOnly();
        }
    }
}

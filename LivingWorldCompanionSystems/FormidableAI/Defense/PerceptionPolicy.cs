using System;

namespace LivingWorld.FormidableAI.Defense
{
    public enum AwarenessStage
    {
        Unaware,
        Suspicious,
        Alerted,
        Engaged
    }

    public sealed class PerceptionObservation
    {
        public string TargetIdentity { get; private set; }
        public float EvidencePerSecond { get; private set; }
        public float DeltaSeconds { get; private set; }
        public bool FormidableOwnsPerception { get; private set; }

        public PerceptionObservation(string targetIdentity, float evidencePerSecond, float deltaSeconds,
            bool formidableOwnsPerception)
        {
            TargetIdentity = targetIdentity;
            EvidencePerSecond = evidencePerSecond;
            DeltaSeconds = deltaSeconds;
            FormidableOwnsPerception = formidableOwnsPerception;
        }
    }

    public sealed class PerceptionState
    {
        public string TargetIdentity { get; private set; }
        public float Evidence { get; private set; }
        public AwarenessStage Stage { get; private set; }

        public PerceptionState(string targetIdentity, float evidence, AwarenessStage stage)
        {
            TargetIdentity = targetIdentity;
            Evidence = evidence;
            Stage = stage;
        }
    }

    public static class PerceptionPolicy
    {
        public static PerceptionState Observe(PerceptionState previous, PerceptionObservation observation)
        {
            if (observation == null) throw new ArgumentNullException("observation");
            if (!observation.FormidableOwnsPerception) return previous ?? Empty();
            if (string.IsNullOrEmpty(observation.TargetIdentity)) throw new ArgumentException("target identity is required");
            if (observation.DeltaSeconds < 0f) throw new ArgumentOutOfRangeException("DeltaSeconds");

            float evidence = previous != null && string.Equals(previous.TargetIdentity,
                observation.TargetIdentity, StringComparison.Ordinal) ? previous.Evidence : 0f;
            evidence += Math.Max(0f, observation.EvidencePerSecond) * observation.DeltaSeconds;
            return new PerceptionState(observation.TargetIdentity, evidence, Classify(evidence));
        }

        public static PerceptionState Decay(PerceptionState previous, float evidencePerSecond,
            float deltaSeconds, bool formidableOwnsPerception)
        {
            if (previous == null || !formidableOwnsPerception) return previous ?? Empty();
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException("deltaSeconds");
            float evidence = Math.Max(0f, previous.Evidence - Math.Max(0f, evidencePerSecond) * deltaSeconds);
            return evidence == 0f ? Empty() : new PerceptionState(previous.TargetIdentity, evidence, Classify(evidence));
        }

        public static PerceptionState Empty()
        {
            return new PerceptionState(null, 0f, AwarenessStage.Unaware);
        }

        private static AwarenessStage Classify(float evidence)
        {
            if (evidence >= 1f) return AwarenessStage.Engaged;
            if (evidence >= .65f) return AwarenessStage.Alerted;
            if (evidence >= .25f) return AwarenessStage.Suspicious;
            return AwarenessStage.Unaware;
        }
    }
}

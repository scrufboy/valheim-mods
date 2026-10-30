using System;

namespace LivingWorld.Companions.VikingAISalvage
{
    public enum PerceptionStage
    {
        Unaware,
        Suspicious,
        Alerted,
        Engaged
    }

    public sealed class PerceptionEvidencePolicy
    {
        private string _targetId;
        private float _evidence;

        public string TargetId { get { return _targetId; } }
        public float Evidence { get { return _evidence; } }
        public PerceptionStage Stage { get; private set; }

        public PerceptionStage Observe(string targetId, float evidencePerSecond, float deltaSeconds, bool committedCombat)
        {
            if (committedCombat) return Stage;
            if (string.IsNullOrEmpty(targetId)) throw new ArgumentException("target identity is required", "targetId");
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException("deltaSeconds");
            if (!string.Equals(_targetId, targetId, StringComparison.Ordinal))
            {
                _targetId = targetId;
                _evidence = 0f;
                Stage = PerceptionStage.Unaware;
            }

            _evidence += Math.Max(0f, evidencePerSecond) * deltaSeconds;
            Stage = Classify(_evidence);
            return Stage;
        }

        public void Decay(float evidencePerSecond, float deltaSeconds, bool committedCombat)
        {
            if (committedCombat) return;
            if (deltaSeconds < 0f) throw new ArgumentOutOfRangeException("deltaSeconds");
            _evidence = Math.Max(0f, _evidence - Math.Max(0f, evidencePerSecond) * deltaSeconds);
            Stage = Classify(_evidence);
            if (_evidence == 0f) _targetId = null;
        }

        private static PerceptionStage Classify(float evidence)
        {
            if (evidence >= 1f) return PerceptionStage.Engaged;
            if (evidence >= 0.65f) return PerceptionStage.Alerted;
            if (evidence >= 0.25f) return PerceptionStage.Suspicious;
            return PerceptionStage.Unaware;
        }
    }
}

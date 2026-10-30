using System;

namespace LivingWorld.FormidableAI.Kernel
{
    public sealed class ThreatObservation
    {
        public string Identity { get; private set; }
        public float Distance { get; private set; }
        public bool Visible { get; private set; }
        public bool Attacking { get; private set; }

        public ThreatObservation(string identity, float distance, bool visible, bool attacking)
        {
            if (string.IsNullOrEmpty(identity)) throw new ArgumentException("identity is required", "identity");
            Identity = identity;
            Distance = distance;
            Visible = visible;
            Attacking = attacking;
        }
    }

    public sealed class CombatFrame
    {
        private readonly ThreatObservation[] _threats;

        public long Sequence { get; private set; }
        public float Time { get; private set; }
        public string ActorIdentity { get; private set; }
        public string ClassId { get; private set; }
        public string AuthorizedTargetIdentity { get; private set; }
        public bool OwnerReady { get; private set; }
        public bool CombatCommitted { get; private set; }
        public float HealthFraction { get; private set; }
        public float StaminaFraction { get; private set; }
        public int ThreatCount { get { return _threats.Length; } }

        public CombatFrame(long sequence, float time, string actorIdentity, string classId,
            string authorizedTargetIdentity, bool ownerReady, bool combatCommitted,
            float healthFraction, float staminaFraction, ThreatObservation[] threats)
        {
            if (sequence < 0) throw new ArgumentOutOfRangeException("sequence");
            if (string.IsNullOrEmpty(actorIdentity)) throw new ArgumentException("actor identity is required", "actorIdentity");
            Sequence = sequence;
            Time = time;
            ActorIdentity = actorIdentity;
            ClassId = classId ?? string.Empty;
            AuthorizedTargetIdentity = authorizedTargetIdentity;
            OwnerReady = ownerReady;
            CombatCommitted = combatCommitted;
            HealthFraction = Clamp01(healthFraction);
            StaminaFraction = Clamp01(staminaFraction);
            _threats = threats == null ? new ThreatObservation[0] : (ThreatObservation[])threats.Clone();
        }

        public ThreatObservation GetThreat(int index) { return _threats[index]; }

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}

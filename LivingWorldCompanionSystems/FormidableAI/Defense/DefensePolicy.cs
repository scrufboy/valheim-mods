using System;

namespace LivingWorld.FormidableAI.Defense
{
    public enum DefenseAction
    {
        None,
        Block,
        Dodge,
        YieldToNative
    }

    public sealed class StrikeObservation
    {
        public string ActorIdentity { get; private set; }
        public string ThreatIdentity { get; private set; }
        public string AttackIdentity { get; private set; }
        public bool FormidableOwnsDefense { get; private set; }
        public bool AuthorizedThreat { get; private set; }
        public bool AttackActive { get; private set; }
        public bool AttackBlockable { get; private set; }
        public bool HasShield { get; private set; }
        public bool CanDodge { get; private set; }
        public bool Incapacitated { get; private set; }
        public float TimeSeconds { get; private set; }
        public float TimeToImpactSeconds { get; private set; }
        public float Distance { get; private set; }
        public float FacingDot { get; private set; }
        public float Stamina { get; private set; }

        public StrikeObservation(string actorIdentity, string threatIdentity, string attackIdentity,
            bool formidableOwnsDefense, bool authorizedThreat, bool attackActive, bool attackBlockable,
            bool hasShield, bool canDodge, bool incapacitated, float timeSeconds,
            float timeToImpactSeconds, float distance, float facingDot, float stamina)
        {
            ActorIdentity = actorIdentity;
            ThreatIdentity = threatIdentity;
            AttackIdentity = attackIdentity;
            FormidableOwnsDefense = formidableOwnsDefense;
            AuthorizedThreat = authorizedThreat;
            AttackActive = attackActive;
            AttackBlockable = attackBlockable;
            HasShield = hasShield;
            CanDodge = canDodge;
            Incapacitated = incapacitated;
            TimeSeconds = timeSeconds;
            TimeToImpactSeconds = timeToImpactSeconds;
            Distance = distance;
            FacingDot = facingDot;
            Stamina = stamina;
        }
    }

    public sealed class DefenseMemory
    {
        public string LastAttackIdentity { get; private set; }
        public float ReadyAtSeconds { get; private set; }

        public DefenseMemory(string lastAttackIdentity, float readyAtSeconds)
        {
            LastAttackIdentity = lastAttackIdentity;
            ReadyAtSeconds = readyAtSeconds;
        }

        public static DefenseMemory Empty()
        {
            return new DefenseMemory(null, 0f);
        }
    }

    public sealed class DefenseDecision
    {
        public DefenseAction Action { get; private set; }
        public string Reason { get; private set; }
        public string ThreatIdentity { get; private set; }
        public string AttackIdentity { get; private set; }
        public DefenseMemory NextMemory { get; private set; }

        public DefenseDecision(DefenseAction action, string reason, StrikeObservation strike,
            DefenseMemory nextMemory)
        {
            Action = action;
            Reason = reason;
            ThreatIdentity = strike == null ? null : strike.ThreatIdentity;
            AttackIdentity = strike == null ? null : strike.AttackIdentity;
            NextMemory = nextMemory;
        }
    }

    public static class DefensePolicy
    {
        public const float ReactionWindowSeconds = .45f;
        public const float MaximumThreatDistance = 5f;
        public const float MinimumFacingDot = .35f;
        public const float BlockStaminaCost = 10f;
        public const float DodgeStaminaCost = 15f;
        public const float ReactionCooldownSeconds = .8f;

        public static DefenseDecision Decide(DefenseMemory memory, StrikeObservation strike)
        {
            if (strike == null) throw new ArgumentNullException("strike");
            memory = memory ?? DefenseMemory.Empty();

            if (!strike.FormidableOwnsDefense)
                return Decision(DefenseAction.YieldToNative, "defense-domain-not-owned", strike, memory);
            if (!strike.AuthorizedThreat)
                return Decision(DefenseAction.None, "threat-not-authorized", strike, memory);
            if (!strike.AttackActive || string.IsNullOrEmpty(strike.AttackIdentity))
                return Decision(DefenseAction.None, "no-observed-attack", strike, memory);
            if (string.Equals(memory.LastAttackIdentity, strike.AttackIdentity, StringComparison.Ordinal))
                return Decision(DefenseAction.None, "attack-already-reacted", strike, memory);
            if (strike.TimeSeconds < memory.ReadyAtSeconds)
                return Decision(DefenseAction.None, "reaction-cooldown", strike, memory);
            if (strike.Incapacitated)
                return Decision(DefenseAction.YieldToNative, "actor-incapacitated", strike, memory);
            if (strike.TimeToImpactSeconds < 0f || strike.TimeToImpactSeconds > ReactionWindowSeconds)
                return Decision(DefenseAction.None, "outside-strike-window", strike, memory);
            if (strike.Distance > MaximumThreatDistance)
                return Decision(DefenseAction.None, "threat-out-of-range", strike, memory);
            if (strike.FacingDot < MinimumFacingDot)
                return Decision(DefenseAction.None, "threat-outside-defense-arc", strike, memory);

            if (strike.AttackBlockable && strike.HasShield && strike.Stamina >= BlockStaminaCost)
                return React(DefenseAction.Block, "block-observed-strike", memory, strike);
            if (strike.CanDodge && strike.Stamina >= DodgeStaminaCost)
                return React(DefenseAction.Dodge, "dodge-observed-strike", memory, strike);
            return Decision(DefenseAction.YieldToNative, "no-formidable-defense-available", strike, memory);
        }

        private static DefenseDecision React(DefenseAction action, string reason, DefenseMemory memory,
            StrikeObservation strike)
        {
            var next = new DefenseMemory(strike.AttackIdentity, strike.TimeSeconds + ReactionCooldownSeconds);
            return Decision(action, reason, strike, next);
        }

        private static DefenseDecision Decision(DefenseAction action, string reason,
            StrikeObservation strike, DefenseMemory memory)
        {
            return new DefenseDecision(action, reason, strike, memory);
        }
    }
}

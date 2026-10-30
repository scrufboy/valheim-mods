using System;

namespace LivingWorld.Companions.FormidableAI.Classes
{
    public enum ClassActionKind
    {
        Yield,
        Investigate,
        CommunicateThreat,
        Defend,
        ProtectAlly,
        Reposition,
        HealAlly,
        CastSupport,
        RangedAttack,
        ReachAttack,
        ThrowWeapon,
        PressureTarget
    }

    public enum ClassActionDomain
    {
        None,
        Awareness,
        Defense,
        MovementIntent,
        Support,
        WeaponAction
    }

    public sealed class ClassCombatFacts
    {
        public bool OwnerAuthorized;
        public bool ActorReady;
        public bool CommittedCombat;
        public bool TargetAuthorized;
        public bool IncomingStrike;
        public bool ShieldReady;
        public bool PersonalThreat;
        public bool AllyThreatened;
        public bool AllyCritical;
        public bool SupportSafe;
        public bool LineOfFireClear;
        public bool AllyInLineOfFire;
        public bool TooCloseForWeapon;
        public bool PreferredRange;
        public bool TargetInReach;
        public bool ThrowReady;
        public bool ThrowRecoverySafe;
        public bool EvidenceToInvestigate;
        public bool ThreatAlreadyCommunicated;
        public bool AbilityReady;
    }

    public sealed class ClassActionRequest
    {
        public ClassActionKind Kind;
        public ClassActionDomain Domain;
        public int Priority;
        public string Reason;

        public static ClassActionRequest Yield(string reason)
        {
            return new ClassActionRequest { Kind = ClassActionKind.Yield, Domain = ClassActionDomain.None, Priority = 0, Reason = reason };
        }
    }

    public static class ClassTacticalPolicy
    {
        public static ClassActionRequest Decide(ClassProfile profile, ClassCombatFacts facts)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (facts == null) throw new ArgumentNullException("facts");
            if (!facts.OwnerAuthorized || !facts.ActorReady) return ClassActionRequest.Yield("owner-or-actor-not-ready");

            // An imminent personal strike preempts support, positioning and attack.
            if (facts.CommittedCombat && facts.IncomingStrike && facts.ShieldReady &&
                profile.Has(ClassCapability.ShieldDefense))
                return Request(ClassActionKind.Defend, ClassActionDomain.Defense, 100, "observed-incoming-strike");

            if (!facts.CommittedCombat)
                return DecidePrecombat(profile, facts);
            if (!facts.TargetAuthorized)
                return ClassActionRequest.Yield("combat-target-not-authorized");

            switch (profile.Class)
            {
                case FormidableClass.Warden: return Warden(profile, facts);
                case FormidableClass.Huntress: return Huntress(profile, facts);
                case FormidableClass.Mystic: return Mystic(profile, facts);
                case FormidableClass.Skirmisher: return Skirmisher(profile, facts);
                case FormidableClass.Commander: return Commander(profile, facts);
                case FormidableClass.Pathfinder: return Pathfinder(profile, facts);
                default: return ClassActionRequest.Yield("unknown-class");
            }
        }

        private static ClassActionRequest DecidePrecombat(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.EvidenceToInvestigate && profile.Has(ClassCapability.Investigation))
                return Request(ClassActionKind.Investigate, ClassActionDomain.Awareness, 30, "target-isolated-evidence");
            if (facts.EvidenceToInvestigate && !facts.ThreatAlreadyCommunicated &&
                profile.Has(ClassCapability.ThreatCommunication))
                return Request(ClassActionKind.CommunicateThreat, ClassActionDomain.Awareness, 25, "new-threat-evidence");
            return ClassActionRequest.Yield("no-precombat-class-action");
        }

        private static ClassActionRequest Warden(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.AllyThreatened && profile.Has(ClassCapability.ProtectAlly))
                return Request(ClassActionKind.ProtectAlly, ClassActionDomain.MovementIntent, 80, "ally-protection-window");
            return Request(ClassActionKind.PressureTarget, ClassActionDomain.WeaponAction, 40, "hold-defensive-pressure");
        }

        private static ClassActionRequest Huntress(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.TooCloseForWeapon || !facts.LineOfFireClear || facts.AllyInLineOfFire)
                return Request(ClassActionKind.Reposition, ClassActionDomain.MovementIntent, 75,
                    facts.AllyInLineOfFire ? "ally-in-firing-lane" : "unsafe-range-or-lane");
            if (facts.PreferredRange && profile.Has(ClassCapability.RangedPressure))
                return Request(ClassActionKind.RangedAttack, ClassActionDomain.WeaponAction, 55, "safe-ranged-window");
            return ClassActionRequest.Yield("no-safe-huntress-action");
        }

        private static ClassActionRequest Mystic(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.PersonalThreat) return ClassActionRequest.Yield("personal-threat-preempts-support");
            if (facts.AllyCritical && facts.SupportSafe && facts.AbilityReady && profile.Has(ClassCapability.HealSupport))
                return Request(ClassActionKind.HealAlly, ClassActionDomain.Support, 90, "critical-ally-safe-heal");
            if (facts.SupportSafe && facts.AbilityReady && profile.Has(ClassCapability.CastSupport))
                return Request(ClassActionKind.CastSupport, ClassActionDomain.Support, 60, "safe-support-window");
            return ClassActionRequest.Yield("support-not-safe-or-ready");
        }

        private static ClassActionRequest Skirmisher(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.ThrowReady && facts.ThrowRecoverySafe && !facts.AllyInLineOfFire &&
                profile.Has(ClassCapability.BoundedThrow))
                return Request(ClassActionKind.ThrowWeapon, ClassActionDomain.WeaponAction, 70, "bounded-throw-window");
            if (facts.TargetInReach && profile.Has(ClassCapability.ReachControl))
                return Request(ClassActionKind.ReachAttack, ClassActionDomain.WeaponAction, 50, "target-in-reach-band");
            return Request(ClassActionKind.Reposition, ClassActionDomain.MovementIntent, 45, "recover-reach-band");
        }

        private static ClassActionRequest Commander(ClassProfile profile, ClassCombatFacts facts)
        {
            if (facts.AllyThreatened && profile.Has(ClassCapability.ProtectAlly))
                return Request(ClassActionKind.ProtectAlly, ClassActionDomain.MovementIntent, 85, "command-protection-priority");
            return Request(ClassActionKind.PressureTarget, ClassActionDomain.WeaponAction, 60, "anchor-threat-without-chasing");
        }

        private static ClassActionRequest Pathfinder(ClassProfile profile, ClassCombatFacts facts)
        {
            // Investigation never survives committed combat.
            if (facts.TooCloseForWeapon || !facts.LineOfFireClear || facts.AllyInLineOfFire)
                return Request(ClassActionKind.Reposition, ClassActionDomain.MovementIntent, 65, "scout-combat-lane");
            return Request(ClassActionKind.RangedAttack, ClassActionDomain.WeaponAction, 50, "scout-yields-awareness-to-combat");
        }

        private static ClassActionRequest Request(ClassActionKind kind, ClassActionDomain domain, int priority, string reason)
        {
            return new ClassActionRequest { Kind = kind, Domain = domain, Priority = priority, Reason = reason };
        }
    }
}

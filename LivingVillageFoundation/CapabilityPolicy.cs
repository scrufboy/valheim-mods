using System;

namespace LivingVillageCore
{
    [Flags]
    public enum CommonAbility
    {
        None = 0, Repair = 1, Eat = 2, SelfMaintain = 4, Carry = 8
    }

    public enum DutySpecialty
    {
        Commoner, Maintenance, Guard, Archer, Steward, Jarl, Matron,
        Cleric, Cook, Smith, Farmer, Hunter, Gatherer
    }

    public enum DutyDecision
    {
        Idle, CombatEmergency, CommandedFollow, Rest, Eat, SpecialtyDuty, CommonRepair
    }

    // Snapshot supplied by sensing and the steward's scheduler. This policy never
    // moves an actor, manufactures resources, selects targets, or invents jobs.
    public sealed class DutyContext
    {
        public bool IsAdult;
        public DutySpecialty Specialty;
        public bool CombatEmergency;
        public bool FollowCommandActive;
        public bool NeedsRest;
        public bool NeedsMeal;
        public bool MealAvailable;
        public bool SpecialtyDutyPending;
        public bool CommonRepairAssigned;
    }

    public static class CapabilityPolicy
    {
        public static CommonAbility Abilities(bool isAdult)
        {
            return isAdult ? CommonAbility.Repair | CommonAbility.Eat |
                CommonAbility.SelfMaintain | CommonAbility.Carry : CommonAbility.None;
        }

        public static bool IsDefender(DutySpecialty specialty)
        {
            return specialty == DutySpecialty.Guard || specialty == DutySpecialty.Archer;
        }

        // Lower scores mean earlier assignment. A specialty changes availability
        // and priority, never an adult's underlying common capabilities.
        public static int CommonRepairPriority(DutySpecialty specialty)
        {
            if (specialty == DutySpecialty.Maintenance) return 0;
            if (specialty == DutySpecialty.Commoner) return 10;
            if (IsDefender(specialty)) return 100;
            return 50;
        }

        public static DutyDecision Choose(DutyContext context)
        {
            if (context == null) throw new ArgumentNullException("context");
            if (context.CombatEmergency) return DutyDecision.CombatEmergency;
            if (context.FollowCommandActive) return DutyDecision.CommandedFollow;
            if (context.NeedsRest) return DutyDecision.Rest;
            if (context.NeedsMeal && context.MealAvailable) return DutyDecision.Eat;
            if (!context.IsAdult) return DutyDecision.Idle;
            if (context.SpecialtyDutyPending) return DutyDecision.SpecialtyDuty;
            if (context.CommonRepairAssigned) return DutyDecision.CommonRepair;
            return DutyDecision.Idle;
        }
    }
}

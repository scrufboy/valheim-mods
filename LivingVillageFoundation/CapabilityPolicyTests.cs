using System;
using LivingVillageCore;

public static class CapabilityPolicyTests
{
    static int checks;
    static void Assert(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }

    public static void Main()
    {
        CommonAbility all = CommonAbility.Repair | CommonAbility.Eat |
            CommonAbility.SelfMaintain | CommonAbility.Carry;
        foreach (DutySpecialty role in Enum.GetValues(typeof(DutySpecialty)))
        {
            Assert(CapabilityPolicy.Abilities(true) == all, role + " adult common abilities");
            DutyContext c = new DutyContext { IsAdult = true, Specialty = role,
                SpecialtyDutyPending = true, CommonRepairAssigned = true };
            Assert(CapabilityPolicy.Choose(c) == DutyDecision.SpecialtyDuty,
                role + " own duty before ordinary repair");
            c.SpecialtyDutyPending = false;
            Assert(CapabilityPolicy.Choose(c) == DutyDecision.CommonRepair,
                role + " common repair when released from duty");
        }
        DutyContext busy = new DutyContext { IsAdult = true, Specialty = DutySpecialty.Guard,
            CombatEmergency = true, FollowCommandActive = true, NeedsRest = true,
            NeedsMeal = true, MealAvailable = true, SpecialtyDutyPending = true,
            CommonRepairAssigned = true };
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.CombatEmergency, "combat wins");
        busy.CombatEmergency = false;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.CommandedFollow, "follow before routine");
        busy.FollowCommandActive = false;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.Rest, "rest before routine");
        busy.NeedsRest = false;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.Eat, "meal before routine");
        busy.MealAvailable = false;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.SpecialtyDuty,
            "unavailable food must not stall work");
        busy.NeedsMeal = false;
        busy.SpecialtyDutyPending = false;
        busy.CommonRepairAssigned = false;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.Idle, "no invented job");
        busy.IsAdult = false;
        busy.CommonRepairAssigned = true;
        Assert(CapabilityPolicy.Choose(busy) == DutyDecision.Idle, "no youth labor inferred");
        Assert(CapabilityPolicy.Abilities(false) == CommonAbility.None, "youth policy deferred");
        Assert(CapabilityPolicy.CommonRepairPriority(DutySpecialty.Maintenance) <
            CapabilityPolicy.CommonRepairPriority(DutySpecialty.Commoner), "maintenance priority");
        Assert(CapabilityPolicy.CommonRepairPriority(DutySpecialty.Commoner) <
            CapabilityPolicy.CommonRepairPriority(DutySpecialty.Guard), "guard last resort");
        bool rejected = false;
        try { CapabilityPolicy.Choose(null); } catch (ArgumentNullException) { rejected = true; }
        Assert(rejected, "null snapshot rejected");
        Console.WriteLine("CapabilityPolicy: " + checks + " checks passed.");
    }
}

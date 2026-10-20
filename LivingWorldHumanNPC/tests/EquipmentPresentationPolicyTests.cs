using System;
using LivingWorld.HumanNpc;
static class EquipmentPresentationPolicyTests
{
    static int checks;
    static void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
    static void Main()
    {
        Check(EquipmentPresentationPolicy.Stow(false, true) == StowAction.Complete, "verified hidden loadout is complete");
        Check(EquipmentPresentationPolicy.Stow(true, false) == StowAction.HideVisible, "visible loadout must be hidden");
        Check(EquipmentPresentationPolicy.Stow(true, true) == StowAction.HideVisible, "mixed slots must normalize to hidden");
        Check(EquipmentPresentationPolicy.Stow(false, false) == StowAction.RestoreThenHide, "missing loadout is restored before stow");
        Check(EquipmentPresentationPolicy.VerifiedStowed(false, true), "hidden-only presentation verifies");
        Check(!EquipmentPresentationPolicy.VerifiedStowed(true, true), "mixed presentation is not a pass");
        Check(!EquipmentPresentationPolicy.VerifiedStowed(false, false), "empty slots are not a pass");
        Check(EquipmentPresentationPolicy.UtilityOwnsHands(true, false, true), "visible torch plus hidden combat pair owns peaceful hands");
        Check(!EquipmentPresentationPolicy.UtilityOwnsHands(false, false, true), "darkness request without visible torch does not own hands");
        Check(!EquipmentPresentationPolicy.UtilityOwnsHands(true, true, true), "torch cannot own hands beside a visible shield or weapon");
        Check(!EquipmentPresentationPolicy.UtilityOwnsHands(true, false, false), "torch cannot erase the persistent back presentation");
        Check(EquipmentPresentationPolicy.PairShield(true, true, true, false), "authorized one-handed combat pairs an available shield");
        Check(!EquipmentPresentationPolicy.PairShield(false, true, true, false), "peaceful presentation never draws a shield");
        Check(!EquipmentPresentationPolicy.PairShield(true, false, true, false), "bows and two-handed weapons do not pair a shield");
        Check(!EquipmentPresentationPolicy.PairShield(true, true, false, false), "missing shield fails closed");
        Check(!EquipmentPresentationPolicy.PairShield(true, true, true, true), "equipped shield is left alone");
        Console.WriteLine("EquipmentPresentationPolicy PASS: " + checks + " checks.");
    }
}

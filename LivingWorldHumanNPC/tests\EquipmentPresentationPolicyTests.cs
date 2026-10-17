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
        Console.WriteLine("EquipmentPresentationPolicy PASS: " + checks + " checks.");
    }
}

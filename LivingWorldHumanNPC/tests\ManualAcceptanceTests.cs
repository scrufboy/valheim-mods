using System;
using LivingWorld.ManualTest;
class ManualAcceptanceTests
{
    static int count;
    static void Check(bool value, string name) { count++; if (!value) throw new Exception(name); }
    static void Main()
    {
        Check(!ManualAcceptance.BlocksLabFootprint(false, false), "generic visual scenery and loose items cannot veto the prepared plot");
        Check(ManualAcceptance.BlocksLabFootprint(true, false), "character colliders remain protected");
        Check(ManualAcceptance.BlocksLabFootprint(false, true), "existing structure colliders remain protected");
        Check(ManualAcceptance.BlocksLabFootprint(true, true), "combined character and structure remain protected");
        // Keep the fixture conservatively short; the old 18-character name
        // could not be entered through Valheim's character creation screen.
        Check(ManualAcceptance.ProfileName.Length <= 10, "test character stays within our ten-character fixture budget");
        Check(ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_Tester", true, false, 0), "short named solo fixture allowed");
        Check(ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "lwc_tester", true, false, 0), "native lowercase character filename allowed");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_HumanTester_v1", true, false, 0), "unusable old character name rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_HumanTester", true, false, 0), "truncated old character name rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", null, true, false, 0), "missing character rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "", true, false, 0), "empty character rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_Tester ", true, false, 0), "extra character suffix rejected");
        Check(!ManualAcceptance.WorldAllowed(null, "LWC_Tester", true, false, 0), "missing world rejected");
        Check(!ManualAcceptance.WorldAllowed("MyMainWorld", "LWC_Tester", true, false, 0), "live world rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "MainCharacter", true, false, 0), "live character rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_Tester", false, false, 0), "remote client rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_Tester", true, true, 0), "open server rejected");
        Check(!ManualAcceptance.WorldAllowed("LWC_HumanLab_v1", "LWC_Tester", true, false, 1), "remote peer rejected");
        Check(ManualAcceptance.MovementPassed(true, true, 0f, 8f, 3f, 0f), "grounded goal arrival allowed");
        Check(!ManualAcceptance.MovementPassed(false, true, 0f, 8f, 3f, 0f), "displacement without path rejected");
        Check(!ManualAcceptance.MovementPassed(true, false, -3f, 8f, 3f, 0f), "falling displacement rejected");
        Check(!ManualAcceptance.MovementPassed(true, true, -3f, 8f, 3f, 0f), "unresolved vertical physics rejected");
        Check(!ManualAcceptance.MovementPassed(true, true, 0f, 4f, 3f, 0f), "trivial movement rejected");
        Check(!ManualAcceptance.MovementPassed(true, true, 0f, 8f, 5f, 0f), "partial progress without arrival rejected");
        Check(!ManualAcceptance.MovementPassed(true, true, 0f, 8f, 3f, 2f), "moving target invalidates goal check");
        Check(ManualAcceptance.CombatPassed(true, 4f, true), "attributed equipped combat allowed");
        Check(!ManualAcceptance.CombatPassed(true, 0f, true), "unattributed damage rejected");
        Check(!ManualAcceptance.CombatPassed(false, 4f, true), "missing target acquisition rejected");
        Check(!ManualAcceptance.CombatPassed(true, 4f, false), "missing weapon rejected");
        Check(!ManualAcceptance.FullCombatPassed(true, 4f, true, false, false), "first hit is not full fight evidence");
        Check(ManualAcceptance.FullCombatPassed(true, 40f, true, true, false), "observed unassisted native target death allowed");
        Check(!ManualAcceptance.FullCombatPassed(true, 40f, true, true, true), "external damage invalidates unassisted check");
        Check(!ManualAcceptance.FullCombatPassed(false, 40f, true, true, false), "full fight still requires acquisition");
        Check(!ManualAcceptance.FullCombatPassed(true, 0f, true, true, false), "death without attributed damage rejected");
        Console.WriteLine("ManualAcceptanceTests PASS: " + count + " checks");
    }
}

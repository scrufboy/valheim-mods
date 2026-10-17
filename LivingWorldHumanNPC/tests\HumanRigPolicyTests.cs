using System;
using LivingWorld.HumanNpc;

internal static class HumanRigPolicyTests
{
    static int checks;
    static void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static void Reject(Action action, string message)
    {
        bool rejected = false;
        try { action(); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected, message);
    }
    static int Main()
    {
        HumanRigPolicy.ValidateBoneNames(new[] { "Hips", "Head", "LeftHand", "RightHand" }); checks++;
        Reject(() => HumanRigPolicy.ValidateBoneNames(null), "null bone list");
        Reject(() => HumanRigPolicy.ValidateBoneNames(new string[0]), "empty bone list");
        Reject(() => HumanRigPolicy.ValidateBoneNames(new[] { "Head", "Head" }), "duplicate bone");
        Reject(() => HumanRigPolicy.ValidateBoneNames(new[] { "Head", null }), "null bone");
        Reject(() => HumanRigPolicy.ValidateBoneNames(new[] { "" }), "unnamed bone");
        Check(HumanRigPolicy.MayInitializeAppearance(true, true, ""), "fresh owner");
        Check(!HumanRigPolicy.MayInitializeAppearance(true, false, ""), "remote cannot initialize");
        Check(!HumanRigPolicy.MayInitializeAppearance(false, true, ""), "invalid view");
        Check(!HumanRigPolicy.MayInitializeAppearance(true, true, HumanRigPolicy.Revision), "reload must not overwrite");
        Check(!HumanRigPolicy.MayInitializeAppearance(true, true, "different"), "conflict must not overwrite");
        Check(HumanRigPolicy.RevisionCompatible(""), "fresh revision");
        Check(HumanRigPolicy.RevisionCompatible(HumanRigPolicy.Revision), "same revision");
        Check(!HumanRigPolicy.RevisionCompatible("different"), "changed revision");
        Check(HumanRigPolicy.EquipmentReady(true, true, "sword", "sword"), "native equipped item");
        Check(!HumanRigPolicy.EquipmentReady(false, true, "sword", "sword"), "unarmed fallback");
        Check(!HumanRigPolicy.EquipmentReady(true, false, "sword", "sword"), "unequipped");
        Check(!HumanRigPolicy.EquipmentReady(true, true, "sword", "club"), "wrong weapon");
        Check(!HumanRigPolicy.EquipmentReady(true, true, "", ""), "empty weapon");
        Console.WriteLine("HumanRigPolicyTests PASS: " + checks + " checks (policy only; not Unity execution).");
        return 0;
    }
}

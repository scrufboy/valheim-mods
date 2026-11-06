using System;
using LivingWorld.FactionRuntime;

class RuntimeRegistryTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }

    static void Main()
    {
        RelationRegistry registry = RelationRegistry.Parse(
            LivingWorldFactionRuntimePlugin.DefaultAssignments,
            LivingWorldFactionRuntimePlugin.DefaultRelations);
        Check(registry.AssignmentCount == 3, "three exact assignments");
        Check(registry.RelationCount == 8, "four symmetric relations become eight directed rules");
        string faction;
        Check(registry.TryGetManagedFaction("RRRN_Pillager", out faction) && faction == "LWC.Pillagers",
            "Pillager exact managed identity");
        Check(registry.TryGetManagedFaction("RRRN_OrcBrute", out faction) && faction == "LWC.Eitrherjar",
            "OrcBrute exact managed identity");
        Check(!registry.TryGetManagedFaction("Draugr", out faction), "native Draugr is not relabeled");
        RelationDisposition result;
        Check(registry.TryResolveFacts("LWC.Pillagers", "Undead", null, "Undead", out result) &&
            result == RelationDisposition.Hostile, "managed Pillager overrides stale Undead as first argument");
        Check(registry.TryResolveFacts(null, "Undead", "LWC.Pillagers", "Undead", out result) &&
            result == RelationDisposition.Hostile, "managed Pillager overrides stale Undead as second argument");
        Check(!registry.TryResolveFacts(null, "Undead", null, "Undead", out result), "unknown Undead pair falls through vanilla");
        Check(!registry.TryResolveFacts("LWC.Pillagers", "Undead", "LWC.Pillagers", "Undead", out result),
            "same managed faction falls through vanilla self-faction rules");
        Check(!registry.TryResolveFacts(null, "AnimalsVeg", null, "Undead", out result), "unknown native pair falls through vanilla");
        Check(registry.TryResolveFacts("LWC.Eitrherjar", "PlainsMonsters", null, "PlainsMonsters", out result) &&
            result == RelationDisposition.Hostile, "managed Eitrherjar does not inherit Fuling non-hostility");
        RelationRegistry friendly = RelationRegistry.Parse("A=mod.a;B=mod.b", "mod.a<->mod.b=Friendly");
        Check(friendly.TryResolveFacts("mod.a", "Boss", "mod.b", "Undead", out result) &&
            result == RelationDisposition.Friendly, "authorable friendly override");
        bool rejected = false;
        try { RelationRegistry.Parse("A=one;A=two", ""); } catch (FormatException) { rejected = true; }
        Check(rejected, "duplicate assignment rejected");
        System.Console.WriteLine("RuntimeRegistryTests PASS: " + checks + " checks");
    }
}

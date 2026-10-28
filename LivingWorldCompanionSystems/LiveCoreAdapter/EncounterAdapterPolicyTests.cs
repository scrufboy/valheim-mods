using System;
using LivingWorld.CompanionSystems.LiveCoreAdapter;

static class EncounterAdapterPolicyTests
{
    static int failures;
    static void Check(bool value, string name) { if (!value) { failures++; Console.Error.WriteLine("FAIL " + name); } }
    static EncounterActorIds Ids() { return new EncounterActorIds { PersonId="person-1", EncounterId="encounter-1", SiteId="site-1", CampId="camp-1", IncidentId="incident-1" }; }

    public static int Main()
    {
        var claims = new PersonSpawnClaims();
        Check(EncounterAdapterPolicy.IsSupportedPrefab("LWC_HumanGuard_v2"), "scanned guard prefab accepted");
        Check(EncounterAdapterPolicy.IsSupportedPrefab("LWC_HumanArcher_v2"), "scanned archer prefab accepted");
        Check(EncounterAdapterPolicy.IsSupportedPrefab("LWC_HumanScout_v1"), "scanned legacy prefab accepted");
        Check(!EncounterAdapterPolicy.IsSupportedPrefab("Unscanned_Encounter_Actor"), "unscanned prefab refused");
        int deliveries = 0;
        try { AdapterCompletion.Deliver<object>((value, why) => { deliveries++; throw new Exception("consumer"); }, null, "created"); }
        catch (Exception error) { Check(error.Message == "consumer", "consumer exception propagated"); }
        Check(deliveries == 1, "throwing consumer invoked exactly once");
        Check(claims.TryClaim("person-1"), "first process claim");
        Check(!claims.TryClaim("person-1"), "duplicate process claim refused");
        Check(claims.Release("person-1") && claims.TryClaim("person-1"), "unload releases only process claim");

        string reason;
        var f = new FirstContactFacts { Ids=Ids(), IsServer=true, IsOwner=true, ActorLoaded=true,
            PlayerWitnessed=true, AttributedDamage=true };
        Check(EncounterAdapterPolicy.ObserveFirstContact(f, out reason) == ContactDirective.RecordWitnessedDanger,
            "damage records witnessed danger");
        f.NativeCombatCommitted=true;
        Check(EncounterAdapterPolicy.ObserveFirstContact(f, out reason) == ContactDirective.YieldToNativeCombat,
            "combat state yields to native combat");
        f.PlayerWitnessed=false;
        Check(EncounterAdapterPolicy.ObserveFirstContact(f, out reason) == ContactDirective.None && reason=="local_witness_required",
            "remote damage is not first contact");
        f.PlayerWitnessed=true; f.AttributedDamage=false; f.HostileTargetingActor=false;
        Check(EncounterAdapterPolicy.ObserveFirstContact(f, out reason) == ContactDirective.None && reason=="material_danger_required",
            "mere visibility is peaceful awareness");
        return failures;
    }
}

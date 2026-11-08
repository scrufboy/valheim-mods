using System;
using LivingWorld.CompanionSystems.EncounterAllegiance;

class CaptiveBehemothAllegianceTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }
    static CaptiveBehemothEvent Event(string id, CaptiveBehemothAction action, float time)
    { return new CaptiveBehemothEvent { EventId = id, Action = action, IsAuthority = true, WorldTime = time }; }
    static CaptiveBehemothRecord Apply(CaptiveBehemothRecord record, CaptiveBehemothEvent fact)
    { CaptiveBehemothResult result = CaptiveBehemothAllegiancePolicy.Apply(record, fact); Check(result.Accepted, fact.EventId + " accepted"); return result.Record; }
    static bool Relation(CaptiveBehemothRecord record, EncounterActorRole other, out InstanceDisposition disposition)
    { return CaptiveBehemothAllegiancePolicy.TryResolve(record, record.BehemothId, EncounterActorRole.Other, "other", other, out disposition); }

    static void Main()
    {
        CaptiveBehemothRecord state = CaptiveBehemothAllegiancePolicy.Create("swamp-site-1", "behemoth-1");
        InstanceDisposition relation;
        Check(Relation(state, EncounterActorRole.NeckCaptor, out relation) && relation == InstanceDisposition.Friendly,
            "dormant captive safe with captors");
        Check(!Relation(state, EncounterActorRole.Player, out relation), "dormant player relation defers");

        CaptiveBehemothEvent bind = Event("bind-1", CaptiveBehemothAction.BindingCompleted, 10);
        bind.VerifiedBindingReceipt = true; bind.ControllerId = "shaman-1"; bind.BindingRevision = 1;
        state = Apply(state, bind);
        Check(state.Mode == CaptiveBehemothMode.Controlled && state.ControllerId == "shaman-1", "verified bind controls exact beast");
        Check(Relation(state, EncounterActorRole.CaptorMinion, out relation) && relation == InstanceDisposition.Friendly,
            "controlled beast protects captor minions");
        Check(Relation(state, EncounterActorRole.Player, out relation) && relation == InstanceDisposition.Hostile,
            "controlled beast attacks player");
        state = Apply(state, Event("deploy", CaptiveBehemothAction.PhysicalDeployment, 11));
        Check(state.Mode == CaptiveBehemothMode.Controlled, "physical deployment does not break spell");

        CaptiveBehemothEvent fakeBreak = Event("fake-break", CaptiveBehemothAction.SpellBroken, 12);
        fakeBreak.FrenzyDuration = 10;
        Check(!CaptiveBehemothAllegiancePolicy.Apply(state, fakeBreak).Accepted, "cosmetic or unverified break refused");
        CaptiveBehemothEvent broken = Event("break-1", CaptiveBehemothAction.SpellBroken, 12);
        broken.VerifiedSpellBreak = true; broken.FrenzyDuration = 10;
        CaptiveBehemothResult brokenResult = CaptiveBehemothAllegiancePolicy.Apply(state, broken);
        Check(brokenResult.Accepted && brokenResult.ClearControlPresentation && brokenResult.StartFrenzyPresentation,
            "real spell break clears orange and starts red frenzy");
        state = brokenResult.Record;
        foreach (EncounterActorRole role in new[] { EncounterActorRole.Player, EncounterActorRole.ControllerShaman,
            EncounterActorRole.CaptorMinion, EncounterActorRole.Undead, EncounterActorRole.Other })
            Check(Relation(state, role, out relation) && relation == InstanceDisposition.Hostile, "frenzy has no friends " + role);
        Check(!CaptiveBehemothAllegiancePolicy.Apply(state, Event("early", CaptiveBehemothAction.TimeAdvanced, 21)).Accepted,
            "bounded frenzy does not cool early");

        CaptiveBehemothEvent rebound = Event("bind-2", CaptiveBehemothAction.BindingCompleted, 15);
        rebound.VerifiedBindingReceipt = true; rebound.ControllerId = "shaman-1"; rebound.BindingRevision = 2;
        CaptiveBehemothResult reboundResult = CaptiveBehemothAllegiancePolicy.Apply(state, rebound);
        Check(reboundResult.Accepted && reboundResult.RestoreControlPresentation && reboundResult.Record.Mode == CaptiveBehemothMode.Controlled,
            "surviving shaman can rebind during frenzy");
        state = reboundResult.Record;
        Check(!CaptiveBehemothAllegiancePolicy.Apply(state, Event("stale-cool", CaptiveBehemothAction.TimeAdvanced, 30)).Accepted,
            "stale release timer cannot turn rebound beast green");
        CaptiveBehemothEvent staleBind = Event("stale-bind", CaptiveBehemothAction.BindingCompleted, 31);
        staleBind.VerifiedBindingReceipt = true; staleBind.ControllerId = "shaman-1"; staleBind.BindingRevision = 1;
        Check(!CaptiveBehemothAllegiancePolicy.Apply(state, staleBind).Accepted, "stale cast receipt cannot rebind");

        CaptiveBehemothEvent brokenAgain = Event("break-2", CaptiveBehemothAction.SpellBroken, 40);
        brokenAgain.VerifiedSpellBreak = true; brokenAgain.FrenzyDuration = 5;
        state = Apply(state, brokenAgain);
        CaptiveBehemothResult cooled = CaptiveBehemothAllegiancePolicy.Apply(state, Event("cool", CaptiveBehemothAction.TimeAdvanced, 45));
        Check(cooled.Accepted && cooled.RestoreUndeadPresentation && cooled.Record.Mode == CaptiveBehemothMode.ReleasedUndead,
            "expired frenzy restores green normal undead mode");
        state = cooled.Record;
        Check(Relation(state, EncounterActorRole.Undead, out relation) && relation == InstanceDisposition.Friendly,
            "released normal Behemoth allies only with undead");
        Check(Relation(state, EncounterActorRole.NeckCaptor, out relation) && relation == InstanceDisposition.Hostile,
            "released normal Behemoth remains hostile to Necks");
        Check(Relation(state, EncounterActorRole.Player, out relation) && relation == InstanceDisposition.Hostile,
            "released normal Behemoth remains hostile to player");
        Check(!Relation(state, EncounterActorRole.Other, out relation), "unwritten other relation defers to native");
        Check(CaptiveBehemothAllegiancePolicy.TryResolve(state, "other", EncounterActorRole.Undead,
            state.BehemothId, EncounterActorRole.Other, out relation) && relation == InstanceDisposition.Friendly,
            "instance relation works in both argument orders");

        string saved = CaptiveBehemothPersistence.Write(state); CaptiveBehemothRecord restored;
        Check(CaptiveBehemothPersistence.TryRead(saved, out restored), "record roundtrip");
        Check(restored.EncounterId == "swamp-site-1" && restored.BehemothId == "behemoth-1" &&
            restored.Mode == CaptiveBehemothMode.ReleasedUndead && restored.BindingRevision == 2,
            "links mode and revisions persist");
        Check(!CaptiveBehemothPersistence.TryRead(saved.Replace("ReleasedUndead", "InventedMode"), out restored),
            "unknown persisted mode fails closed");
        Check(CaptiveBehemothPersistence.TryRead(saved, out restored), "valid record remains readable after malformed rejection");
        CaptiveBehemothResult replay = CaptiveBehemothAllegiancePolicy.Apply(restored, Event("cool", CaptiveBehemothAction.TimeAdvanced, 99));
        Check(replay.Accepted && replay.Replayed && replay.Record.Revision == restored.Revision, "event replay idempotent after reload");

        CaptiveBehemothResult death = CaptiveBehemothAllegiancePolicy.Apply(restored, Event("death", CaptiveBehemothAction.DeathConfirmed, 100));
        Check(death.Accepted && death.ClearAllPresentation && death.Record.Mode == CaptiveBehemothMode.Dead,
            "death terminal and clears ritual presentation");
        Check(!CaptiveBehemothAllegiancePolicy.Apply(death.Record, bind).Accepted, "dead beast cannot be rebound");
        Check(!CaptiveBehemothAllegiancePolicy.TryResolve(death.Record, "behemoth-1", EncounterActorRole.Other,
            "player", EncounterActorRole.Player, out relation), "dead beast has no relation override");

        CaptiveBehemothEvent client = Event("client", CaptiveBehemothAction.BindingCompleted, 1);
        client.IsAuthority = false; client.VerifiedBindingReceipt = true; client.ControllerId = "shaman"; client.BindingRevision = 1;
        Check(!CaptiveBehemothAllegiancePolicy.Apply(CaptiveBehemothAllegiancePolicy.Create("e", "b"), client).Accepted,
            "nonauthority cannot mutate state");
        System.Console.WriteLine("CaptiveBehemothAllegianceTests PASS: " + checks + " checks");
    }
}

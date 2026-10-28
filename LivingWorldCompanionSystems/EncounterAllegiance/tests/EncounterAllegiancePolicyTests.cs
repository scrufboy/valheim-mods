using System;
using LivingWorld.CompanionSystems.EncounterAllegiance;

class EncounterAllegiancePolicyTests
{
    static int checks;
    static void Check(bool yes, string label) { checks++; if (!yes) throw new Exception("FAIL " + label); }
    static FactionIdentity Player() { return new FactionIdentity { Allegiance = Allegiance.Player }; }
    static ContactFact Fact(string id, ContactAct act, bool person = true, bool player = true)
    { return new ContactFact { FactId = id, Act = act, IsServer = true, WitnessedByPerson = person, WitnessedByPlayer = player, Actor = Player(), InsideAuthoredBoundary = true }; }
    static PersonContactRecord Apply(PersonContactRecord r, string id, ContactAct act)
    { ContactResult result = EncounterAllegiancePolicy.Apply(r, Fact(id, act)); Check(result.Accepted, id + " accepted"); return result.Record; }

    static void Main()
    {
        PersonContactRecord hearthless = EncounterAllegiancePolicy.Create("person-runa", "camp-splitash", Allegiance.Hearthless);
        ContactResult sight = EncounterAllegiancePolicy.Apply(hearthless, Fact("sight", ContactAct.Notice));
        Check(sight.Accepted && sight.Record.Readiness == Readiness.Observing, "friendly Hearthless observes");
        Check(sight.Record.Relationship == Relationship.Unknown, "mutual sight does not prove friendship");
        Check(sight.Presentation == Presentation.SilhouetteAndBehavior, "identity concealed at first read");
        Check(sight.Record.PersonNoticedPlayer && sight.Record.PlayerNoticedPerson, "bilateral notice recorded");
        PersonContactRecord revealed;
        Check(EncounterAllegiancePolicy.TryReveal(sight.Record, true, "Runa", out revealed), "verified reveal succeeds");
        Check(revealed.IdentityRevealed && revealed.RevealedName == "Runa", "name persisted");
        PersonContactRecord ignored;
        Check(!EncounterAllegiancePolicy.TryReveal(revealed, true, "Other Runa", out ignored), "one reveal only");
        PersonContactRecord neutral = Apply(EncounterAllegiancePolicy.Create("n1", "c-neutral", Allegiance.Unaffiliated), "n-notice", ContactAct.Notice);
        Check(neutral.Readiness == Readiness.Wary && neutral.Relationship == Relationship.Unknown, "unaffiliated human is wary without omniscient proof");

        ContactResult client = EncounterAllegiancePolicy.Apply(hearthless, new ContactFact { FactId = "client", Act = ContactAct.Notice, IsServer = false, WitnessedByPerson = true });
        Check(!client.Accepted && client.Reason == "server-authority-required", "client cannot mutate");
        ContactResult unseen = EncounterAllegiancePolicy.Apply(hearthless, Fact("unseen", ContactAct.Theft, false, false));
        Check(!unseen.Accepted && unseen.Reason == "unwitnessed-fact", "no omniscient theft response");
        PersonContactRecord armed = Apply(sight.Record, "weapon-up", ContactAct.WeaponRaised);
        Check(armed.Readiness == Readiness.Guarded && armed.Relationship == Relationship.Unknown, "weapon changes readiness not history");
        ContactResult lowered = EncounterAllegiancePolicy.Apply(armed, Fact("weapon-down", ContactAct.WeaponLowered));
        Check(lowered.Record.Readiness == Readiness.Wary && lowered.YieldToPeacefulPresentation, "lowered weapon de-escalates and yields");
        PersonContactRecord trespass = Apply(lowered.Record, "inside", ContactAct.Trespass);
        Check(trespass.Relationship == Relationship.Wary && trespass.Readiness == Readiness.Guarded, "witnessed trespass matters");
        PersonContactRecord aid = Apply(trespass, "aid", ContactAct.Aid);
        Check(aid.Relationship == Relationship.Neutral && aid.Readiness == Readiness.Wary, "witnessed aid repairs relation");
        PersonContactRecord theft = Apply(aid, "theft", ContactAct.Theft);
        Check(theft.Relationship == Relationship.Wary, "witnessed theft worsens relation");
        PersonContactRecord grievance = Apply(theft, "grievance", ContactAct.FactionGrievance);
        Check(grievance.Relationship == Relationship.Hostile, "authored witnessed grievance worsens relation");
        ContactResult attack = EncounterAllegiancePolicy.Apply(grievance, Fact("attack", ContactAct.AttackCommitted));
        Check(attack.Record.Readiness == Readiness.CombatCommitted && attack.Record.Relationship == Relationship.Hostile, "committed attack is hostile");
        Check(attack.YieldToNativeCombat, "native combat owns committed fight");
        ContactResult replay = EncounterAllegiancePolicy.Apply(attack.Record, Fact("attack", ContactAct.AttackCommitted));
        Check(replay.Accepted && replay.Replayed && replay.Record.Revision == attack.Record.Revision, "fact replay idempotent");
        ContactResult peaceAfter = EncounterAllegiancePolicy.Apply(attack.Record, Fact("late-peace", ContactAct.PeacefulSignal));
        Check(peaceAfter.Record.Readiness == Readiness.CombatCommitted && peaceAfter.YieldToPeacefulPresentation, "signal does not steal committed combat control");

        PersonContactRecord pillager = Apply(EncounterAllegiancePolicy.Create("p1", "c1", Allegiance.Pillager), "p-notice", ContactAct.Notice);
        Check(pillager.Readiness == Readiness.Guarded && pillager.Relationship == Relationship.Unknown, "Pillager silhouette is guarded but not omniscient proof");
        PersonContactRecord legionary = Apply(EncounterAllegiancePolicy.Create("e1", "c2", Allegiance.Eitrherjar), "e-notice", ContactAct.Notice);
        Check(legionary.Readiness == Readiness.Guarded && legionary.Relationship == Relationship.Unknown, "Eitrherjar silhouette is guarded but not identity proof");
        Check(AllegianceMatrix.Get(Allegiance.Eitrherjar, new FactionIdentity { Allegiance = Allegiance.Fuling }) == Disposition.Hostile, "Eitrherjar not allied to Fulings");
        Check(AllegianceMatrix.Get(Allegiance.Eitrherjar, new FactionIdentity { Allegiance = Allegiance.Draugr }) == Disposition.Hostile, "Eitrherjar not allied to draugr");
        Check(AllegianceMatrix.Get(Allegiance.Hearthless, new FactionIdentity { Allegiance = Allegiance.Wildlife }) == Disposition.Neutral, "wildlife is not omniscient enemy");
        Check(AllegianceMatrix.Get(Allegiance.Hearthless, new FactionIdentity { Allegiance = Allegiance.OtherMod }) == Disposition.Neutral, "unknown mod fails neutral");
        Check(AllegianceMatrix.Get(Allegiance.Hearthless, new FactionIdentity { Allegiance = Allegiance.OtherMod,
            AdapterFactionId = "example.raiders", HasAuthoredDisposition = true, AuthoredDisposition = Disposition.Hostile }) == Disposition.Hostile,
            "other-mod authored adapter fact honored");
        Check(!EncounterAllegiancePolicy.CanRecruit(pillager) && !EncounterAllegiancePolicy.CanRecruit(legionary), "hostile people not generally recruitable");
        PersonContactRecord trusted = Apply(revealed, "aid-one", ContactAct.Aid);
        trusted = Apply(trusted, "aid-two", ContactAct.Aid);
        Check(EncounterAllegiancePolicy.CanRecruit(trusted), "friendly Hearthless with earned relation may pass allegiance recruitment gate");

        string saved = EncounterAllegiancePersistence.Write(revealed); PersonContactRecord restored;
        Check(EncounterAllegiancePersistence.TryRead(saved, out restored), "persistent record roundtrip");
        Check(restored.PersonId == revealed.PersonId && restored.CampId == revealed.CampId && restored.IdentityRevealed, "IDs and reveal survive reload");
        Check(restored.Relationship == Relationship.Unknown && restored.RevealedName == "Runa", "relationship and identity survive reload");
        Check(!EncounterAllegiancePersistence.TryRead(saved.Replace("lwc-encounter-allegiance-v1", "future"), out restored), "future schema fails closed");
        Console.WriteLine("EncounterAllegiancePolicyTests PASS: " + checks + " checks");
    }
}

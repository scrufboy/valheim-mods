using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.NeedsAndAffinity;

class NeedsAndAffinityPolicyTests
{
    static int checks;
    static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL " + label); }

    static NeedCandidate Candidate(string request, string person, NeedKind kind, string target)
    {
        return new NeedCandidate { RequestId = request, PersonId = person, EncounterId = "split-ash-01",
            AuthoredNeedId = request + ".authored", TargetId = target, Kind = kind,
            Description = "Help with " + target, PersonNearby = true, TargetNearby = true, TargetLoaded = true,
            AuthoredForPerson = true, FactCurrentlyTrue = true, ProgressionLegal = true, RequiredAmount = 2 };
    }

    static CompletionEvidence Evidence(RequestRecord request, string evidenceId)
    {
        return new CompletionEvidence { RequestId = request.RequestId, PersonId = request.PersonId,
            TargetId = request.TargetId, Kind = request.Kind, EvidenceId = evidenceId, Authoritative = true,
            PersonNearby = true, TargetNearby = true, TargetLoaded = true, RequirementSatisfied = true,
            ProgressionLegal = true, DeliveredAmount = request.RequiredAmount, WorldTick = 30 };
    }

    static RequestSelection Offer(NeedsState state, string command, string person, params NeedCandidate[] values)
    { return NeedsAndAffinityPolicy.SelectRequest(state, command, 4455, 10, person, new List<NeedCandidate>(values)); }

    static void Main()
    {
        var food = Candidate("runa.food.01", "runa", NeedKind.Food, "splitash-food-shortage");
        var danger = Candidate("runa.danger.01", "runa", NeedKind.NearbyDanger, "splitash-raider-band");
        var first = Offer(new NeedsState(), "offer-1", "runa", food, danger);
        Check(first.Offered, "verified need offered");
        var reordered = Offer(new NeedsState(), "offer-2", "runa", danger, food);
        Check(reordered.Request.RequestId == first.Request.RequestId, "selection is deterministic and input-order independent");

        var accepted = NeedsAndAffinityPolicy.Respond(first.State, "respond-1", first.Request.RequestId, true, 11);
        Check(accepted.Accepted && !accepted.GrantsReward, "acceptance is not a reward");
        var completed = NeedsAndAffinityPolicy.Complete(accepted.State, "complete-1", Evidence(first.Request, "delivery-zdo-44"));
        Check(completed.Accepted && completed.GrantsReward, "verified concrete aid commits once");
        var duplicate = NeedsAndAffinityPolicy.Complete(completed.State, "complete-2", Evidence(first.Request, "delivery-zdo-45"));
        Check(!duplicate.Accepted && duplicate.Reason == "aid_already_committed" && !duplicate.GrantsReward, "duplicate aid has no reward exploit");
        var commandReplay = NeedsAndAffinityPolicy.Complete(completed.State, "complete-1", Evidence(first.Request, "delivery-zdo-44"));
        Check(commandReplay.Replayed && !commandReplay.GrantsReward, "network replay is idempotent");

        string saved = NeedsPersistence.Write(completed.State); NeedsState loaded;
        Check(NeedsPersistence.TryRead(saved, out loaded), "state reloads");
        Check(loaded.Requests[first.Request.RequestId].Status == RequestStatus.Completed &&
            NeedsAndAffinityPolicy.ReadRelationship(loaded, "runa") == RelationshipReading.Trusting,
            "reload preserves outcome and qualitative relationship");
        Check(!NeedsPersistence.TryRead(saved.Replace(NeedsPersistence.Schema, "future-schema"), out loaded), "unknown schema fails closed");

        var refusedOffer = Offer(new NeedsState(), "offer-refuse", "edda",
            Candidate("edda.settlement.01", "edda", NeedKind.SettlementHelp, "splitash-repair"));
        var refused = NeedsAndAffinityPolicy.Respond(refusedOffer.State, "refuse-1", refusedOffer.Request.RequestId, false, 14);
        Check(refused.Accepted && refused.State.Requests[refusedOffer.Request.RequestId].Status == RequestStatus.Refused,
            "refusal closes request without hostility");
        Check(NeedsAndAffinityPolicy.ReadRelationship(refused.State, "edda") == RelationshipReading.Known,
            "refusal is remembered as a boundary, not disobedience");

        var griefed = NeedsAndAffinityPolicy.RecordGriefing(completed.State, "grief-1", "runa", "camp-chest-broken-7", 40);
        Check(griefed.Accepted && NeedsAndAffinityPolicy.ReadRelationship(griefed.State, "runa") == RelationshipReading.Unsafe,
            "witnessed griefing overrides trust reading");
        Check(!NeedsAndAffinityPolicy.BuildInvitationHandoff(griefed.State, "runa", true).ReadyForRescueReleaseReview,
            "griefing blocks factual handoff");

        var bad = Candidate("bad-target", "runa", NeedKind.NearbyDanger, "unloaded-nearest-enemy"); bad.TargetLoaded = false;
        Check(Offer(new NeedsState(), "bad-offer", "runa", bad).Reason == "no_verified_authored_need", "unloaded world target rejected");
        var owned = Candidate("owned-target", "runa", NeedKind.SettlementHelp, "player-chest"); owned.PlayerOwnsTarget = true;
        Check(!Offer(new NeedsState(), "owned-offer", "runa", owned).Offered, "player-owned target rejected");
        var illegal = Candidate("illegal-mat", "runa", NeedKind.LegalDungeonMaterials, "crypt-iron"); illegal.ProgressionLegal = false;
        Check(!Offer(new NeedsState(), "illegal-offer", "runa", illegal).Offered, "illegal dungeon material rejected");

        var badCompletionOffer = Offer(new NeedsState(), "bad-complete-offer", "runa", danger);
        var badAccepted = NeedsAndAffinityPolicy.Respond(badCompletionOffer.State, "bad-complete-accept", danger.RequestId, true, 12);
        var wrongTarget = Evidence(badCompletionOffer.Request, "wrong-target-evidence"); wrongTarget.TargetId = "some-other-danger";
        Check(!NeedsAndAffinityPolicy.Complete(badAccepted.State, "bad-complete", wrongTarget).Accepted,
            "completion must match exact authored world target");

        var two = new NeedsState();
        var runaOffer = Offer(two, "two-runa", "runa", Candidate("runa.food.02", "runa", NeedKind.Food, "runa-pot"));
        var eddaOffer = Offer(runaOffer.State, "two-edda", "edda", Candidate("edda.repair.02", "edda", NeedKind.SettlementHelp, "edda-roof"));
        Check(runaOffer.Request.Kind == NeedKind.Food && eddaOffer.Request.Kind == NeedKind.SettlementHelp &&
            eddaOffer.State.Requests.Count == 2, "two people retain distinct concrete needs");

        var fullRoster = NeedsAndAffinityPolicy.BuildInvitationHandoff(completed.State, "runa", false);
        Check(!fullRoster.ReadyForRescueReleaseReview && Array.IndexOf(fullRoster.Reasons, "bonded_roster_full") >= 0,
            "full roster produces truthful handoff refusal");
        Check(completed.State.Requests[first.Request.RequestId].Status == RequestStatus.Completed,
            "full roster does not erase person or aid history");
        var openRoster = NeedsAndAffinityPolicy.BuildInvitationHandoff(completed.State, "runa", true);
        Check(openRoster.ReadyForRescueReleaseReview && openRoster.ConcreteNeedCompleted,
            "open roster only hands evidence to RescueRelease review");

        var arcOffer = Offer(new NeedsState(), "arc-offer", "yrsa",
            Candidate("yrsa.danger.01", "yrsa", NeedKind.NearbyDanger, "verified-watch-danger"));
        var arcAgree = NeedsAndAffinityPolicy.Respond(arcOffer.State, "arc-agree", arcOffer.Request.RequestId, true, 51);
        var travelHandoff = NeedsAndAffinityPolicy.BuildTemporaryTravelHandoff(arcAgree.State, arcOffer.Request.RequestId);
        Check(travelHandoff.MayRequestTemporaryAssignment && travelHandoff.PersonId == "yrsa",
            "agreement supplies evidence but does not assign temporary travel");
        Check(!NeedsAndAffinityPolicy.RecordEncounterActivation(arcAgree.State, "arc-live-bad", arcOffer.Request.RequestId, "", false).Accepted,
            "event record requires EncounterActivation proof");
        var allied = NeedsAndAffinityPolicy.RecordEncounterActivation(arcAgree.State, "arc-live", arcOffer.Request.RequestId, "activation-77", true);
        Check(allied.Accepted && allied.Arc.Phase == AidArcPhase.EventActivated, "verified activation proves event, not alliance assignment");
        Check(!NeedsAndAffinityPolicy.RecordWitnessedContribution(allied.State, "arc-wrong", arcOffer.Request.RequestId,
            "activation-other", "hit-1", true).Accepted, "contribution must belong to same activated encounter");
        var contribution = NeedsAndAffinityPolicy.RecordWitnessedContribution(allied.State, "arc-contribution",
            arcOffer.Request.RequestId, "activation-77", "defended-lane-1", true);
        Check(contribution.Accepted && contribution.Arc.ContributionWitnessed, "authoritative contribution is remembered once");
        Check(!NeedsAndAffinityPolicy.BuildPersonalInvitationHandoff(contribution.State, arcOffer.Request.RequestId).MayPresentPersonalOffer,
            "contribution before completed need cannot unlock personal offer");
        var arcComplete = NeedsAndAffinityPolicy.Complete(contribution.State, "arc-complete", Evidence(arcOffer.Request, "danger-clear-1"));
        Check(arcComplete.Accepted && arcComplete.State.AidArcs[arcOffer.Request.RequestId].Phase == AidArcPhase.TrustEstablished,
            "completed need plus witnessed contribution establishes factual trust");
        var eligible = NeedsAndAffinityPolicy.RecordInvitationEligibility(arcComplete.State, "arc-eligible", arcOffer.Request.RequestId);
        Check(eligible.Accepted && eligible.Arc.Phase == AidArcPhase.InvitationEligible,
            "later personal offer eligibility is recorded without recruiting");
        NeedsState reloadedArc;
        Check(NeedsPersistence.TryRead(NeedsPersistence.Write(eligible.State), out reloadedArc) &&
            reloadedArc.AidArcs[arcOffer.Request.RequestId].Phase == AidArcPhase.InvitationEligible &&
            reloadedArc.AidArcs[arcOffer.Request.RequestId].ContributionEvidenceId == "defended-lane-1",
            "aid arc and witnessed contribution survive reload");

        QuestRequestFact brokerFact;
        Check(NeedsAndAffinityPolicy.TryBuildQuestRequestFact(completed.State, first.Request.RequestId, out brokerFact) &&
            brokerFact.RequestId == first.Request.RequestId && brokerFact.PersonId == "runa" &&
            brokerFact.TargetId == first.Request.TargetId && brokerFact.Status == RequestStatus.Completed,
            "quest broker receives stable request and target facts");
        Check(!NeedsAndAffinityPolicy.TryBuildQuestRequestFact(completed.State, "unknown", out brokerFact),
            "quest broker cannot invent an unknown request");

        var repeat = Candidate(first.Request.RequestId, "runa", NeedKind.Food, "splitash-food-shortage");
        Check(!Offer(completed.State, "repeat-offer", "runa", repeat).Offered, "resolved authored need cannot be farmed");
        Check(completed.State.Requests.Count == 1, "policy does not simulate new needs offscreen");
        Console.WriteLine("NeedsAndAffinityPolicyTests PASS: " + checks + " checks");
    }
}

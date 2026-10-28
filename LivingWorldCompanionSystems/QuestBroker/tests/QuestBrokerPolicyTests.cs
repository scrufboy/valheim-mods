using System;
using LivingWorld.CompanionSystems.QuestBroker;

class QuestBrokerPolicyTests
{
    static int checks;
    static void Check(bool condition, string label) { checks++; if (!condition) throw new Exception("FAIL " + label); }
    static RequestCandidate Candidate(RequestKind kind)
    {
        return new RequestCandidate { AuthoredRequestId="splitash.safe-road", PartyId="party-a", IssuerPersonId="runa",
            EncounterId="split-ash-01", WorldId="world-9", SiteId="site-greydwarf-nest-2", SiteIdentity="site-greydwarf-nest-2:v3",
            Biome="Meadows", Description="Make the west approach safe.", Kind=kind, DistanceFromIssuer=620,
            ExpiresWorldTick=5000, ServerAuthority=true, SitePersisted=true, SiteKnownToIssuer=true, WorldFactCurrent=true,
            SafeGround=true, Reachable=true, BiomeMatches=true, ProgressionLegal=true };
    }
    static PresentationProof Camp(long tick)
    { return new PresentationProof { SourceId="runa", SourcePersonId="runa", SourceKind=OfferSourceKind.CampPerson,
        ServerAuthority=true, SourceExists=true, SourceLoadedAndNearby=true, SourceKnowsRequest=true, WorldTick=tick }; }
    static PresentationProof Board(long tick)
    { return new PresentationProof { SourceId="board-oak-1", SettlementId="oakstead", SourceKind=OfferSourceKind.SettlementBoard,
        ServerAuthority=true, SourceExists=true, SourceLoadedAndNearby=true, SourceKnowsRequest=true,
        BoardOrJarlAtKnownSettlement=true, WorldTick=tick }; }
    static PresentationProof Jarl(long tick)
    { return new PresentationProof { SourceId="jarl-astrid", SourcePersonId="jarl-astrid", SettlementId="oakstead", SourceKind=OfferSourceKind.Jarl,
        ServerAuthority=true, SourceExists=true, SourceLoadedAndNearby=true, SourceKnowsRequest=true,
        BoardOrJarlAtKnownSettlement=true, WorldTick=tick }; }
    static CompletionProof Completion(WorldRequest r, string evidence)
    { return new CompletionProof { RequestId=r.RequestId, WorldId=r.WorldId, SiteId=r.SiteId, SiteIdentity=r.SiteIdentity,
        EvidenceId=evidence, ServerAuthority=true, TargetLoaded=true, PlayerAtTarget=true, ObjectiveObservedComplete=true,
        ProgressionLegal=true, WorldTick=3000 }; }
    static ArrivalProof Arrival(WorldRequest r, string evidence)
    { return new ArrivalProof { RequestId=r.RequestId, PartyId=r.PartyId, WorldId=r.WorldId, SiteId=r.SiteId,
        SiteIdentity=r.SiteIdentity, EvidenceId=evidence, ServerAuthority=true, TargetLoaded=true, PartyAtTarget=true,
        ProgressionLegal=true, WorldTick=2000 }; }

    static void Main()
    {
        var cfg = new BrokerConfig { MaximumTargetDistance=1000, MinimumLifetimeTicks=100, ResolutionCooldownTicks=500 };
        var made = QuestBrokerPolicy.Create(new BrokerState(), cfg, "create-1", 77, 1000, Candidate(RequestKind.CompanionNeed), Camp(1000));
        Check(made.Applied && made.MayPresent && made.Request.Status == RequestStatus.Offered, "verified camp offer creates persistent request");

        var privateBoard = QuestBrokerPolicy.PresentExisting(made.State, "private-board", made.Request.RequestId, Board(1040));
        Check(!privateBoard.Applied && privateBoard.Reason == "private_request_cannot_use_public_surface",
            "private companion need cannot leak onto a public board");
        var privateJarl = QuestBrokerPolicy.PresentExisting(made.State, "private-jarl", made.Request.RequestId, Jarl(1040));
        Check(!privateJarl.Applied && privateJarl.Reason == "private_request_cannot_use_public_surface",
            "private companion need cannot be relayed by a jarl");

        var publicCandidate = Candidate(RequestKind.CompanionNeed); publicCandidate.Visibility=RequestVisibility.PublicSurfaces;
        var publicMade = QuestBrokerPolicy.Create(new BrokerState(), cfg, "public-create", 77, 1000, publicCandidate, Camp(1000));
        var board = QuestBrokerPolicy.PresentExisting(publicMade.State, "board-1", publicMade.Request.RequestId, Board(1050));
        Check(board.Applied && board.State.Requests.Count == 1 && board.State.Presentations.Count == 2,
            "camp and board present one request rather than duplicate quests");
        Check(board.Presentation.RequestId == publicMade.Request.RequestId, "authorized public sharing retains persistent request identity");
        var concurrent = Candidate(RequestKind.WorldService); concurrent.AuthoredRequestId="another-live-request"; concurrent.SiteId="site-other"; concurrent.SiteIdentity="site-other:v1";
        Check(QuestBrokerPolicy.Create(board.State, cfg, "concurrent", 77, 1100, concurrent, Camp(1100)).Reason == "active_request_limit_reached",
            "party cannot accumulate an unbounded active request list");

        var duplicateAuthored = QuestBrokerPolicy.Create(board.State, cfg, "create-duplicate", 77, 1100, publicCandidate, Camp(1100));
        Check(!duplicateAuthored.Applied && duplicateAuthored.Reason == "authored_request_already_exists", "authored request cannot be regenerated");

        var accepted = QuestBrokerPolicy.Respond(board.State, "accept-1", publicMade.Request.RequestId, true, 1200);
        Check(accepted.Applied && accepted.Request.Status == RequestStatus.Accepted, "acceptance persists on shared request");
        var tooEarly = QuestBrokerPolicy.Complete(accepted.State, "complete-before-arrival", Completion(accepted.Request, "premature"));
        Check(!tooEarly.Applied && tooEarly.Reason == "authoritative_target_arrival_required", "completion requires moving to the exact known place");
        var arrived = QuestBrokerPolicy.Arrive(accepted.State, "arrive-1", Arrival(accepted.Request, "party-entered-site-1"));
        Check(arrived.Applied && arrived.EncounterActivation == null && arrived.Request.SiteIdentity == accepted.Request.SiteIdentity,
            "ordinary request commits authoritative loaded arrival without spawning");
        var proof = Completion(accepted.Request, "clear-event-81");
        var completed = QuestBrokerPolicy.Complete(arrived.State, "complete-1", proof);
        Check(completed.SingleClaimCommitted && completed.ClaimId == "claim:" + publicMade.Request.RequestId, "completion creates one stable claim");
        Check(completed.AidHandoff.ReadyForNeedsAndAffinityReview && completed.AidHandoff.PartyId == accepted.Request.PartyId &&
            completed.AidHandoff.SiteIdentity == accepted.Request.SiteIdentity,
            "completion hands exact evidence to NeedsAndAffinity without awarding respect");
        var secondClaim = QuestBrokerPolicy.Complete(completed.State, "complete-2", proof);
        Check(!secondClaim.Applied && secondClaim.Reason == "outcome_already_claimed", "completed request cannot become an infinite bounty farm");
        var replay = QuestBrokerPolicy.Complete(completed.State, "complete-1", proof);
        Check(replay.Replayed && !replay.SingleClaimCommitted, "network replay cannot claim twice");

        string saved = QuestBrokerPersistence.Write(completed.State); BrokerState loaded;
        Check(QuestBrokerPersistence.TryRead(saved, out loaded), "state reloads");
        Check(loaded.Requests[publicMade.Request.RequestId].Status == RequestStatus.Completed && loaded.Presentations.Count == 2 &&
            loaded.Requests[publicMade.Request.RequestId].Visibility == RequestVisibility.PublicSurfaces,
            "reload preserves offer surfaces, outcome and claim");
        Check(!QuestBrokerPersistence.TryRead(saved.Replace(QuestBrokerPersistence.Schema, "future-schema"), out loaded), "unknown schema fails closed");

        var refusedMade = QuestBrokerPolicy.Create(new BrokerState(), cfg, "ref-create", 77, 1000, Candidate(RequestKind.WorldService), Camp(1000));
        var refused = QuestBrokerPolicy.Respond(refusedMade.State, "refuse", refusedMade.Request.RequestId, false, 1200);
        Check(refused.Request.Status == RequestStatus.Refused && refused.Request.ResolvedWorldTick == 1200, "refusal is deterministic and persisted");
        Check(!QuestBrokerPolicy.Respond(refused.State, "accept-late", refusedMade.Request.RequestId, true, 1300).Applied, "refusal cannot be rerolled through another surface");

        var expiringCandidate = Candidate(RequestKind.WorldService); expiringCandidate.ExpiresWorldTick=1300;
        var expiring = QuestBrokerPolicy.Create(new BrokerState(), cfg, "exp-create", 77, 1000, expiringCandidate, Camp(1000));
        var expired = QuestBrokerPolicy.Expire(expiring.State, "expire", expiring.Request.RequestId, true, 1300);
        Check(expired.Request.Status == RequestStatus.Expired && !expired.SingleClaimCommitted, "expiry closes record without offscreen success or reward");

        var far = Candidate(RequestKind.WorldService); far.DistanceFromIssuer=1001;
        Check(QuestBrokerPolicy.Create(new BrokerState(), cfg, "far", 77, 1000, far, Camp(1000)).Reason == "target_outside_bounded_distance", "unbounded target rejected");
        var invented = Candidate(RequestKind.WorldService); invented.SitePersisted=false;
        Check(QuestBrokerPolicy.Create(new BrokerState(), cfg, "invented", 77, 1000, invented, Camp(1000)).Reason == "persisted_known_world_fact_required", "invented map target rejected");
        var unsafeTarget = Candidate(RequestKind.WorldService); unsafeTarget.Reachable=false;
        Check(QuestBrokerPolicy.Create(new BrokerState(), cfg, "unsafe", 77, 1000, unsafeTarget, Camp(1000)).Reason == "unsafe_or_unreachable_target", "unreachable target rejected");
        var locked = Candidate(RequestKind.WorldService); locked.ProgressionLegal=false;
        Check(QuestBrokerPolicy.Create(new BrokerState(), cfg, "locked", 77, 1000, locked, Camp(1000)).Reason == "progression_gate_failed", "progression-locked target rejected");

        var mini = Candidate(RequestKind.AuthoredMiniboss);
        Check(QuestBrokerPolicy.Create(new BrokerState(), cfg, "mini-bad", 77, 1000, mini, Camp(1000)).Reason == "verified_authored_encounter_adapter_required",
            "miniboss cannot be invented without authored safe adapter");
        mini.AuthoredEncounter=true; mini.SafeSpawnAdapterVerified=true; mini.AuthoredEncounterId="splitash-ashbrute"; mini.EncounterAdapterId="encounter-adapter-v1";
        var miniMade = QuestBrokerPolicy.Create(new BrokerState(), cfg, "mini-good", 77, 1000, mini, Camp(1000));
        var miniAccepted = QuestBrokerPolicy.Respond(miniMade.State, "mini-accept", miniMade.Request.RequestId, true, 1200);
        var miniArrivalProof = Arrival(miniAccepted.Request, "miniboss-site-arrival"); miniArrivalProof.EncounterAdapterReady=true;
        var miniArrived = QuestBrokerPolicy.Arrive(miniAccepted.State, "mini-arrive", miniArrivalProof);
        Check(miniArrived.EncounterActivation != null && miniArrived.EncounterActivation.RequestId == miniAccepted.Request.RequestId,
            "arrival emits adapter handoff instead of spawning authored miniboss");
        var miniProof = Completion(miniAccepted.Request, "miniboss-death-owned-1");
        Check(QuestBrokerPolicy.Complete(miniArrived.State, "mini-complete-bad", miniProof).Reason == "verified_encounter_adapter_outcome_required",
            "miniboss completion requires adapter-owned outcome");
        miniProof.EncounterAdapterOwnsOutcome=true;
        Check(QuestBrokerPolicy.Complete(miniArrived.State, "mini-complete", miniProof).SingleClaimCommitted,
            "verified authored miniboss outcome can close once");

        var raid = Candidate(RequestKind.AuthoredRaid); raid.AuthoredRequestId="splitash-small-raid";
        raid.AuthoredEncounter=true; raid.SafeSpawnAdapterVerified=true; raid.AuthoredEncounterId="splitash-small-raid-v1"; raid.EncounterAdapterId="encounter-adapter-v1";
        var raidMade = QuestBrokerPolicy.Create(new BrokerState(), cfg, "raid-create", 77, 1000, raid, Camp(1000));
        var raidAccepted = QuestBrokerPolicy.Respond(raidMade.State, "raid-accept", raidMade.Request.RequestId, true, 1200);
        var raidArrivalProof = Arrival(raidAccepted.Request, "raid-site-arrival"); raidArrivalProof.EncounterAdapterReady=true;
        var raidArrived = QuestBrokerPolicy.Arrive(raidAccepted.State, "raid-arrive", raidArrivalProof);
        Check(raidArrived.EncounterActivation != null && raidArrived.EncounterActivation.Kind == RequestKind.AuthoredRaid &&
            raidArrived.EncounterActivation.PartyId == raidAccepted.Request.PartyId,
            "small authored raid activation preserves request, party and site identity");
        BrokerState raidReloaded;
        Check(QuestBrokerPersistence.TryRead(QuestBrokerPersistence.Write(raidArrived.State), out raidReloaded) &&
            raidReloaded.Requests[raidAccepted.Request.RequestId].ActivationId == raidArrived.EncounterActivation.ActivationId,
            "raid activation identity survives reload for adapter duplicate suppression");
        Check(QuestBrokerPolicy.Arrive(raidArrived.State, "raid-arrive-again", raidArrivalProof).Reason == "arrival_already_committed",
            "arrival cannot trigger the authored encounter twice");

        var wrongArrival = Arrival(raidAccepted.Request, "wrong-party-arrival"); wrongArrival.PartyId="party-b";
        Check(QuestBrokerPolicy.Arrive(raidAccepted.State, "wrong-party-arrive", wrongArrival).Reason == "arrival_identity_mismatch",
            "another party cannot activate the accepted request");

        var wrongSiteMade = QuestBrokerPolicy.Create(new BrokerState(), cfg, "wrongsite-create", 77, 1000, Candidate(RequestKind.WorldService), Camp(1000));
        var wrongSiteAccepted = QuestBrokerPolicy.Respond(wrongSiteMade.State, "wrongsite-accept", wrongSiteMade.Request.RequestId, true, 1200);
        var wrongSiteArrived = QuestBrokerPolicy.Arrive(wrongSiteAccepted.State, "wrongsite-arrive", Arrival(wrongSiteAccepted.Request, "right-arrival"));
        var wrongSite = Completion(wrongSiteAccepted.Request, "elsewhere"); wrongSite.SiteIdentity="same-name-new-instance";
        Check(QuestBrokerPolicy.Complete(wrongSiteArrived.State, "wrongsite-complete", wrongSite).Reason == "completion_site_identity_mismatch",
            "completion binds exact persisted site identity");

        var cooldownCandidate = Candidate(RequestKind.WorldService); cooldownCandidate.AuthoredRequestId="later-request"; cooldownCandidate.SiteIdentity="site-2:v1"; cooldownCandidate.SiteId="site-2"; cooldownCandidate.ExpiresWorldTick=6000;
        Check(QuestBrokerPolicy.Create(completed.State, cfg, "during-cooldown", 77, 3200, cooldownCandidate, Camp(3200)).Reason == "resolution_cooldown_active", "resolution cooldown blocks immediate replacement");
        Check(QuestBrokerPolicy.Create(completed.State, cfg, "after-cooldown", 77, 3500, cooldownCandidate, Camp(3500)).Applied, "new authored request allowed after cooldown");

        Console.WriteLine("QuestBrokerPolicyTests PASS: " + checks + " checks");
    }
}

using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.CampLifecycle;
using LivingWorld.CompanionSystems.Discovery;
using LivingWorld.CompanionSystems.FriendInNeed;
using LivingWorld.CompanionSystems.QuestBroker;
using LivingWorld.Companions.RescueRelease;

namespace LivingWorld.CompanionSystems.RuntimeIntegration
{
    static class RuntimeIntegrationTests
    {
        sealed class MemoryStore : IEncounterArcStore
        {
            readonly Dictionary<string, EncounterArcState> values = new Dictionary<string, EncounterArcState>();
            public EncounterArcState Load(string id) { EncounterArcState x; return values.TryGetValue(id, out x) ? x.Copy() : null; }
            public bool CompareExchange(string id, long revision, EncounterArcState replacement)
            {
                EncounterArcState x; bool exists=values.TryGetValue(id, out x);
                if ((!exists && revision != 0) || (exists && x.Revision != revision)) return false;
                values[id]=replacement.Copy(); return true;
            }
        }
        static int checks;
        static void Check(bool value, string label) { checks++; if (!value) throw new Exception("FAIL: " + label); }
        static EncounterArcState Arc()
        {
            const string enc="lwc-enc-001", site="site-splitash-01", camp="camp-splitash-01", person="person-runa-01";
            var roster=new WorldState();
            roster.People.Add(person, new CompanionRecord { PersonId=person, Name="Runa", Role="guard", AppearanceId="runa-a",
                OriginEncounterId=enc, OriginCampId=camp, Rescue=RescueOutcome.Unresolved, Assignment=Assignment.Expedition });
            roster.Destinations.Add("home-01", new SafeDestination { DestinationId="home-01", Kind=DestinationKind.Home, Established=true });
            return new EncounterArcState { EncounterId=enc, SiteId=site, PersonId=person, CampId=camp, IncidentId="incident-splitash-01",
                Lead=new LeadRecord { PartyId=7, TemplateId="splitash", EncounterId=enc, SiteId=site, State=LeadState.Active, CreatedDay=10, ClosedDay=-1 },
                Camp=new CampState { CampId=camp, Disposition=CampDisposition.Active, SchemaVersion=1 },
                CampPerson=new CampPersonState { PersonId=person, CampId=camp, Life=PersonLife.Living, Presence=PersonPresence.AtCamp, SchemaVersion=1 },
                Roster=roster };
        }
        public static int Main()
        {
            var store=new MemoryStore(); var runtime=new EncounterArcRuntime(store); var arc=Arc();
            Check(runtime.Create("tx-create", arc).Committed, "create aggregate");
            Check(!runtime.Create("tx-create-other", arc).Committed, "same encounter cannot duplicate");
            var claims=new PersonSpawnClaims(); Check(claims.TryClaim(arc.CampPerson), "first actor claim");
            Check(!claims.TryClaim(arc.CampPerson), "duplicate loaded actor refused"); claims.Release(arc.CampPerson);
            Check(claims.TryClaim(arc.CampPerson), "return after unload reclaims same identity");

            EncounterArcState s=store.Load(arc.EncounterId);
            var unloaded=runtime.ObservePerson(s.EncounterId, s.Revision, "tx-unload", new Observation { WorldTick=1000,
                ZoneLoaded=false, PlayerNear=false, AuthoritativeOwner=true, ActorExists=false, ConfirmedDeath=true });
            Check(!unloaded.Committed && unloaded.Reason=="observation_requires_no_persistence", "unload is not death transaction");
            s=store.Load(arc.EncounterId); Check(s.CampPerson.Life==PersonLife.Living && s.Roster.People[s.PersonId].Life==LifeState.Alive, "no offscreen death");

            var alert=runtime.WitnessDanger(s.EncounterId, s.Revision, "tx-alert", s.IncidentId, DangerSubject.Camp,
                ThreatEvidence.CampDamage, 20, false);
            Check(alert.Committed && alert.State.Alert.Phase==FriendInNeedPhase.Alerting, "material alert committed");
            s=alert.State;
            var combat=runtime.WitnessDanger(s.EncounterId, s.Revision, "tx-combat", s.IncidentId, DangerSubject.Camp,
                ThreatEvidence.VisibleHostile, 21, true);
            Check(combat.Committed && combat.State.Alert.Phase==FriendInNeedPhase.YieldedToCombat, "alert yields to native combat");
            bool yielded=false; foreach(var c in combat.Commands) if(c.Kind==FriendInNeedCommandKind.YieldToNativeCombat) yielded=true;
            Check(yielded, "combat handoff command emitted");

            s=combat.State; var rescue=runtime.ResolveRescue(s.EncounterId, s.Revision, "tx-rescue", RescueOutcome.Rescued, LeadState.Resolved, 11);
            Check(rescue.Committed && rescue.State.Lead.State==LeadState.Resolved, "lead closes with rescue");
            Check(rescue.State.Roster.People[s.PersonId].Rescue==RescueOutcome.Rescued, "rescue in same aggregate revision");
            long rev=rescue.State.Revision; var replay=runtime.ResolveRescue(s.EncounterId, rev, "tx-rescue", RescueOutcome.Rescued, LeadState.Resolved, 11);
            Check(replay.Committed && replay.Replayed && replay.State.Revision==rev, "transaction replay cannot duplicate");

            s=rescue.State; var release=runtime.RequestRelease(s.EncounterId, s.Revision, "tx-release", "home-01",
                new ReleaseSafety { DestinationZoneLoaded=true, CompanionPresentAtDestination=true, LoanExchangeComplete=true });
            Check(release.Committed && release.State.Roster.People[s.PersonId].Assignment==Assignment.BondedResident, "safe release commits resident handoff");
            Check(release.State.CampPerson.CampId==s.CampId, "destination is not silently aliased to camp identity");

            s=release.State; var facts=runtime.RecordFacts(s.EncounterId, s.Revision, "tx-earned-trust",
                new List<ArcFact> { new ArcFact { Key="respect.aid_outcome", Value="completed" },
                    new ArcFact { Key="affinity.camp_light", Value="warm" } });
            Check(facts.Committed && facts.State.Facts["respect.aid_outcome"]=="completed", "aid outcome persists as remembered fact");
            Check(facts.State.Roster.People[s.PersonId].Assignment==Assignment.BondedResident, "fact write does not perform recruitment");

            s=facts.State; var knowledge=runtime.RecordFacts(s.EncounterId, s.Revision, "tx-map-knowledge",
                new List<ArcFact> { new ArcFact { Key="knowledge.map.target_a", Value="true" } });
            var broker=new BrokerState(); var config=new BrokerConfig();
            var candidate=new RequestCandidate { AuthoredRequestId="authored-001", PartyId="party-7", IssuerPersonId=s.PersonId,
                EncounterId=s.EncounterId, WorldId="world-1", SiteId="target-site-a", SiteIdentity="world-1:target-site-a",
                Biome="Meadows", Description="Aid the road", Kind=RequestKind.CompanionNeed, DistanceFromIssuer=500,
                ExpiresWorldTick=5000, ServerAuthority=true, SitePersisted=true, SiteKnownToIssuer=true, WorldFactCurrent=true,
                SafeGround=true, Reachable=true, BiomeMatches=true, ProgressionLegal=true };
            var campProof=new PresentationProof { SourceId="camp-runa", SourcePersonId=s.PersonId, SourceKind=OfferSourceKind.CampPerson,
                ServerAuthority=true, SourceExists=true, SourceLoadedAndNearby=true, SourceKnowsRequest=true, WorldTick=100 };
            var offered=QuestBrokerBridge.Create(broker, config, "broker-create", 99, 100, candidate, campProof);
            Check(offered.Applied && offered.State.Requests.Count==1, "QuestBroker owns one request record");
            var boardProof=new PresentationProof { SourceId="board-01", SettlementId="settlement-1", SourceKind=OfferSourceKind.SettlementBoard,
                ServerAuthority=true, SourceExists=true, SourceLoadedAndNearby=true, SourceKnowsRequest=true,
                BoardOrJarlAtKnownSettlement=true, WorldTick=101 };
            var privateBoard=QuestBrokerBridge.Present(offered.State, "broker-private-board", offered.Request.RequestId, boardProof);
            Check(!privateBoard.Applied && privateBoard.Reason=="private_request_cannot_use_public_surface" &&
                privateBoard.State.Requests.Count==1 && privateBoard.State.Presentations.Count==1,
                "private camp request cannot relay to board");
            candidate.Visibility=RequestVisibility.PublicSurfaces; candidate.AuthoredRequestId="authored-public-001";
            var publicOffer=QuestBrokerBridge.Create(new BrokerState(), config, "broker-public-create", 99, 100, candidate, campProof);
            Check(publicOffer.Applied && publicOffer.Request.Visibility==RequestVisibility.PublicSurfaces,
                "public relay requires explicit authorization");
            var board=QuestBrokerBridge.Present(publicOffer.State, "broker-board", publicOffer.Request.RequestId, boardProof);
            Check(board.Applied && board.State.Requests.Count==1 && board.State.Presentations.Count==2,
                "authorized camp and board share one broker record");
            Check(knowledge.State.Facts.Count==3, "integration stores knowledge evidence but no request ledger");

            Console.WriteLine("Runtime integration tests passed: " + checks); return 0;
        }
    }
}

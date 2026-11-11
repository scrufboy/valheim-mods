using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.CampLifecycle;
using LivingWorld.CompanionSystems.Discovery;
using LivingWorld.CompanionSystems.FriendInNeed;
using LivingWorld.CompanionSystems.EncounterAllegiance;
using LivingWorld.CompanionSystems.EncounterActivation;
using LivingWorld.CompanionSystems.EncounterLoadouts;
using LivingWorld.CompanionSystems.NeedsAndAffinity;
using LivingWorld.CompanionSystems.QuestBroker;
using LivingWorld.CompanionSystems.SpecialistCombat.PredictionAndThrows;
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
            s=store.Load(arc.EncounterId); Check(s.CampPerson.Life==PersonLife.Living && s.Roster.People[s.PersonId].Life==LivingWorld.Companions.RescueRelease.LifeState.Alive, "no offscreen death");

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

            var contact=EncounterAllegiancePolicy.Create(s.PersonId, s.CampId, Allegiance.Hearthless);
            var aided=EncounterAllegianceBridge.Apply(s, contact, new ContactFact { FactId="contact-aid-1", Act=ContactAct.Aid,
                IsServer=true, WitnessedByPerson=true, WitnessedByPlayer=true, Actor=new FactionIdentity { Allegiance=Allegiance.Player } });
            Check(aided.Accepted && aided.Record.Relationship==LivingWorld.CompanionSystems.EncounterAllegiance.Relationship.Friendly,
                "witnessed aid improves friendly encounter without recruitment");
            var attacked=EncounterAllegianceBridge.Apply(s, aided.Record, new ContactFact { FactId="contact-attack-1",
                Act=ContactAct.AttackCommitted, IsServer=true, WitnessedByPerson=true,
                Actor=new FactionIdentity { Allegiance=Allegiance.Pillager } });
            Check(attacked.Accepted && attacked.YieldToNativeCombat, "committed hostile contact requests native combat handoff");

            var needs=new NeedsState(); var need=new NeedCandidate { RequestId="need-aid-1", PersonId=s.PersonId,
                EncounterId=s.EncounterId, AuthoredNeedId="authored-aid", TargetId="aid-target", Description="Bring aid",
                Kind=NeedKind.SettlementHelp, PersonNearby=true, TargetNearby=true, TargetLoaded=true, AuthoredForPerson=true,
                FactCurrentlyTrue=true, RequiredAmount=1 };
            var selected=NeedsAidBridge.Select(s, needs, "needs-select", 99, 200, new List<NeedCandidate>{need});
            var accepted=NeedsAndAffinityPolicy.Respond(selected.State, "needs-accept", selected.Request.RequestId, true, 201);
            var aidCandidate=new RequestCandidate { AuthoredRequestId=selected.Request.RequestId, PartyId="party-7", IssuerPersonId=s.PersonId,
                EncounterId=s.EncounterId, WorldId="world-1", SiteId="aid-target", SiteIdentity="world-1:aid-target",
                Biome="Meadows", Description="Travel together to aid the road", Kind=RequestKind.CompanionNeed,
                DistanceFromIssuer=500, ExpiresWorldTick=5000, ServerAuthority=true, SitePersisted=true,
                SiteKnownToIssuer=true, WorldFactCurrent=true, SafeGround=true, Reachable=true, BiomeMatches=true, ProgressionLegal=true };
            var aidCampProof=new PresentationProof { SourceId="camp-runa",SourcePersonId=s.PersonId,SourceKind=OfferSourceKind.CampPerson,
                ServerAuthority=true,SourceExists=true,SourceLoadedAndNearby=true,SourceKnowsRequest=true,WorldTick=202 };
            var aidBrokerOffer=QuestBrokerBridge.Create(new BrokerState(),config,"aid-broker-create",99,202,aidCandidate,aidCampProof);
            var aidBrokerAccepted=QuestBrokerPolicy.Respond(aidBrokerOffer.State,"aid-broker-accept",aidBrokerOffer.Request.RequestId,true,203);
            var aidBrokerArrived=QuestBrokerPolicy.Arrive(aidBrokerAccepted.State,"aid-broker-arrive",new ArrivalProof {
                RequestId=aidBrokerAccepted.Request.RequestId,PartyId="party-7",WorldId="world-1",SiteId="aid-target",
                SiteIdentity="world-1:aid-target",EvidenceId="arrival-aid-1",ServerAuthority=true,TargetLoaded=true,
                PartyAtTarget=true,ProgressionLegal=true,EncounterAdapterReady=true,WorldTick=204 });
            var plan=new ActivationPlan { RequestId=aidBrokerAccepted.Request.RequestId,PartyId="party-7",IssuerPersonId=s.PersonId,
                WorldId="world-1",SiteId="aid-target",SiteIdentity="world-1:aid-target",Mode=TargetMode.AuthoredGroup,
                AdapterId="verified-hostile-group-v1",AuthoredEncounterId="road-hostiles-small" };
            var activationRegistered=EncounterActivationBridge.Register(s,aidBrokerArrived.State,new ActivationLedger(),"activation-register",plan);
            Check(activationRegistered.Applied && activationRegistered.State.Entries.Count==1,"activation owns one execution entry for broker request");
            var activationAccepted=EncounterActivationPolicy.Accept(activationRegistered.State,"activation-accept",plan.RequestId,true,204);
            var armed=EncounterActivationPolicy.Arm(activationAccepted.State,"activation-arm",new ArmingProof { RequestId=plan.RequestId,
                WorldId=plan.WorldId,SiteId=plan.SiteId,SiteIdentity=plan.SiteIdentity,ObservingPlayerId="player-7",ServerAuthority=true,
                BrokerRequestAccepted=true,ProgressionLegal=true,SiteStillSafe=true,SiteLoaded=true,IssuerGroupLoadedAtTarget=true,
                PlayerLoadedAtTarget=true,PlayerDeliberatelyApproached=true,IssuerPersonIdentityPreserved=true,
                IssuerPersonalEquipmentPreserved=true,WorldTick=205 });
            Check(armed.Applied && armed.Directive==ActivationDirective.RequestSpawn && armed.Entry.SpawnClaimId=="encounter-claim:"+plan.RequestId,
                "activation persists single claim before spawn request");
            var active=EncounterActivationPolicy.ConfirmSpawn(armed.State,"activation-spawn",new SpawnConfirmation { RequestId=plan.RequestId,
                SpawnClaimId=armed.Entry.SpawnClaimId,SpawnRequestId=armed.Entry.SpawnRequestId,SpawnReceiptId="receipt-aid-1",
                WorldId=plan.WorldId,SiteId=plan.SiteId,SiteIdentity=plan.SiteIdentity,ServerAuthority=true,AdapterAccepted=true,
                ActorProvenanceBound=true,WorldTick=206 });
            var tempRoster=Arc().Roster; tempRoster.People[s.PersonId].Assignment=Assignment.Known;
            var temporary=TemporaryAidBridge.Start(s,accepted.State,aidBrokerArrived.State,active.State,tempRoster,
                new RescueReleasePolicy(),"temporary-start",selected.Request.RequestId,plan.RequestId,"home-01",false);
            Check(temporary.Decision==TemporaryAidDecision.Started && temporary.State.People[s.PersonId].Assignment==Assignment.TemporaryAlly,
                "RescueRelease alone owns temporary field assignment");
            var alliance=NeedsAndAffinityPolicy.RecordEncounterActivation(accepted.State,"needs-alliance",selected.Request.RequestId,
                plan.RequestId,active.Entry.Status==ActivationStatus.Active);
            Check(alliance.Accepted && alliance.Arc.Phase==AidArcPhase.EventActivated,
                "Needs remembers activation evidence without owning assignment or execution");
            var clear=EncounterActivationPolicy.ObserveClear(active.State,"activation-clear",new OutcomeProof { RequestId=plan.RequestId,
                WorldId=plan.WorldId,SiteId=plan.SiteId,SiteIdentity=plan.SiteIdentity,EvidenceId="outcome-aid-1",
                SpawnClaimId=active.Entry.SpawnClaimId,ServerAuthority=true,TargetLoaded=true,PlayerLoadedAtTarget=true,
                IssuerWitnessOrAlertPreserved=true,ObjectiveObservedComplete=true,PlayerOrPartyContributed=true,
                AllClaimedActorsDead=true,WorldTick=220 });
            Check(clear.Applied && clear.Directive==ActivationDirective.NotifyBrokerClear,"activation owns one observed clear outcome");
            var brokerComplete=QuestBrokerPolicy.Complete(aidBrokerArrived.State,"aid-broker-complete",new LivingWorld.CompanionSystems.QuestBroker.CompletionProof {
                RequestId=plan.RequestId,WorldId=plan.WorldId,SiteId=plan.SiteId,SiteIdentity=plan.SiteIdentity,EvidenceId=clear.Entry.OutcomeEvidenceId,
                ServerAuthority=true,TargetLoaded=true,PlayerAtTarget=true,ObjectiveObservedComplete=true,ProgressionLegal=true,
                EncounterAdapterOwnsOutcome=true,WorldTick=221 });
            Check(brokerComplete.Applied && brokerComplete.SingleClaimCommitted,"QuestBroker alone commits request outcome and claim: "+brokerComplete.Reason);
            var contribution=NeedsAndAffinityPolicy.RecordWitnessedContribution(alliance.State,"needs-contribution",selected.Request.RequestId,
                plan.RequestId,clear.Entry.OutcomeEvidenceId,true);
            var helped=NeedsAidBridge.Complete(s, contribution.State, "needs-complete", new CompletionEvidence { RequestId=selected.Request.RequestId,
                PersonId=s.PersonId, TargetId="aid-target", EvidenceId="aid-evidence-1", Kind=NeedKind.SettlementHelp,
                PersonNearby=true, TargetNearby=true, TargetLoaded=true, Authoritative=true, RequirementSatisfied=true,
                DeliveredAmount=1, WorldTick=222 });
            Check(helped.Accepted && helped.GrantsReward && NeedsAndAffinityPolicy.ReadRelationship(helped.State,s.PersonId)==RelationshipReading.Trusting,
                "temporary concrete aid is remembered in Needs-owned state");
            Check(helped.State.AidArcs[selected.Request.RequestId].Phase==AidArcPhase.TrustEstablished,
                "Needs trust follows witnessed contribution and concrete completion");
            var returned=new RescueReleasePolicy().RequestTemporaryReturn(temporary.State,"temporary-return",s.PersonId,
                TemporaryAidEndReason.RequestCompleted,new ReleaseSafety { DestinationZoneLoaded=true,CompanionPresentAtDestination=true,
                    LoanExchangeComplete=true });
            Check(returned.Decision==TemporaryAidDecision.Returned && returned.State.People[s.PersonId].Assignment==Assignment.ReleasedKnown,
                "RescueRelease returns temporary ally without promotion: "+returned.Reason);

            var profile=new EncounterProfile { PersonId=s.PersonId,DisplayName="Runa",OriginEncounterId=s.EncounterId,
                OriginCampId=s.CampId,FactionId="hearthless",RoleId="watcher",OriginBiome=BiomeTier.Meadows,
                DistantSilhouette="shield and spear",LocalHistory="split-ash watcher" };
            var baseVariant=new LoadoutVariant { VariantId="runa-meadows",Tier=BiomeTier.Meadows,
                ArmorSilhouette="hide coat",WeaponSilhouette="flint spear" };
            baseVariant.Gear.Add(new GearRecord { ItemId="SpearFlint",Kind=GearKind.Weapon,Tier=BiomeTier.Meadows,Quantity=1,Equipped=true,PersonalBound=true });
            var forestVariant=new LoadoutVariant { VariantId="runa-forest",Tier=BiomeTier.BlackForest,RequiredWorldKey="defeated_elder",
                RequiredStationFact="station.forge",RequiredMaterialFact="material.bronze",ArmorSilhouette="troll-hide coat",WeaponSilhouette="bronze spear" };
            forestVariant.Gear.Add(new GearRecord { ItemId="SpearBronze",Kind=GearKind.Weapon,Tier=BiomeTier.BlackForest,Quantity=1,Equipped=true,PersonalBound=true });
            profile.Variants.Add(baseVariant); profile.Variants.Add(forestVariant);
            var loadout=EncounterLoadoutBridge.Create(s,profile,"runa-meadows",new HashSet<string>());
            var upgrade=EncounterLoadoutBridge.Upgrade(s,profile,loadout.State,new UpgradeRequest { PersonId=s.PersonId,
                TargetVariantId="runa-forest",WorldKeys=new HashSet<string>{"defeated_elder"},
                VerifiedFacts=new HashSet<string>{"station.forge","material.bronze"} });
            Check(upgrade.Accepted && upgrade.State.PersonId==loadout.State.PersonId && upgrade.State.Revision==loadout.State.Revision+1,
                "loadout upgrade preserves same person lineage");

            var throwState=new ThrowState { AvailableAmmo=1 }; var throwConfig=new ThrowConfig { Kind=ThrowKind.Spear,
                ProjectileSpeed=20, Gravity=9.8, MaximumFlightSeconds=5, MinimumRange=1, MaximumRange=50,
                MaximumSenseAgeSeconds=1, MinimumConfidence=.2, DirectionChangePenalty=.2, SafetyMargin=1 };
            var throwFacts=new ThrowFacts { ActorId=s.PersonId, PersonId=s.PersonId, EncounterId=s.EncounterId, GearRevision=1,
                TargetId="enemy-1", RequestId="throw-1", OwnerPeerId=8, LocalPeerId=8,
                ServerAuthoritative=true, ActorNetworkOwner=true, CombatOwnsAction=true, NativeAttackReady=true,
                TerrainArcClear=true, BuildingArcClear=true, TargetAuthorized=true, ImpactGroundReachable=true,
                Origin=new Vec3(0,0,0), EncounterCenter=new Vec3(10,0,0), EncounterRadius=30,
                Target=new MotionSample { Position=new Vec3(10,0,0), Velocity=new Vec3(0,0,0), PreviousVelocity=new Vec3(0,0,0), Visible=true, AgeSeconds=.1 } };
            var reserved=SpecialistCombatBridge.Reserve(s,throwState,throwConfig,throwFacts);
            Check(reserved.Authorized && reserved.State.AvailableAmmo==1, "specialist reserves but native release still owns ammunition: "+reserved.Reason+" actor="+throwFacts.ActorId+" target="+throwFacts.TargetId+" request="+throwFacts.RequestId);
            var released=PredictionAndThrowsPolicy.ConfirmNativeRelease(reserved.State,"throw-1");
            Check(released.Authorized && released.State.AvailableAmmo==0, "ammunition changes only after native release confirmation");

            Console.WriteLine("Runtime integration tests passed: " + checks); return 0;
        }
    }
}

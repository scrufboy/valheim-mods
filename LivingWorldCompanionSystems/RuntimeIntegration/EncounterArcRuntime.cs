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
using LivingWorld.CompanionSystems.SpecialistCombat.MeleeRoles;
using LivingWorld.CompanionSystems.SpecialistCombat.PredictionAndThrows;
using LivingWorld.Companions.RescueRelease;

namespace LivingWorld.CompanionSystems.RuntimeIntegration
{
    // One persisted aggregate is the transaction boundary. An adapter may put the serialized
    // aggregate in a world ZDO, database row, or file, but must compare the expected revision.
    public interface IEncounterArcStore
    {
        EncounterArcState Load(string encounterId);
        bool CompareExchange(string encounterId, long expectedRevision, EncounterArcState replacement);
    }

    public sealed class EncounterArcState
    {
        public const int CurrentSchema = 1;
        public int SchemaVersion = CurrentSchema;
        public long Revision;
        public string EncounterId, SiteId, PersonId, CampId, IncidentId;
        public LeadRecord Lead;
        public CampState Camp;
        public CampPersonState CampPerson;
        public FriendInNeedState Alert;
        public WorldState Roster;
        // Extension facts are evidence, not recruitment decisions. Producers own their
        // vocabulary; the runtime only persists stable keys and ordinal values atomically.
        public readonly Dictionary<string, string> Facts = new Dictionary<string, string>(StringComparer.Ordinal);
        public readonly HashSet<string> AppliedTransactions = new HashSet<string>(StringComparer.Ordinal);

        public EncounterArcState Copy()
        {
            var copy = new EncounterArcState {
                SchemaVersion = SchemaVersion, Revision = Revision, EncounterId = EncounterId,
                SiteId = SiteId, PersonId = PersonId, CampId = CampId, IncidentId = IncidentId,
                Lead = CopyLead(Lead), Camp = Camp == null ? null : Camp.Copy(),
                CampPerson = CampPerson == null ? null : CampPerson.Copy(),
                Alert = Alert == null ? null : Alert.Copy(), Roster = Roster == null ? null : Roster.Copy()
            };
            foreach (var pair in Facts) copy.Facts.Add(pair.Key, pair.Value);
            foreach (string id in AppliedTransactions) copy.AppliedTransactions.Add(id);
            return copy;
        }

        static LeadRecord CopyLead(LeadRecord x)
        {
            return x == null ? null : new LeadRecord { PartyId=x.PartyId, TemplateId=x.TemplateId,
                EncounterId=x.EncounterId, SiteId=x.SiteId, State=x.State, CreatedDay=x.CreatedDay,
                ClosedDay=x.ClosedDay, X=x.X, Y=x.Y, Z=x.Z };
        }
    }

    public sealed class ArcTransition
    {
        public bool Committed, Replayed;
        public string Reason;
        public EncounterArcState State;
        public FriendInNeedCommand[] Commands = new FriendInNeedCommand[0];
        public RuntimeDirective LifecycleDirective;
        public ActionOwner LifecycleOwner;
    }

    public sealed class ArcFact
    {
        public string Key;
        public string Value;
    }

    // Process-local actor claims prevent two loaded GameObjects for one persisted person.
    // A claim is released on zone unload; identity and life remain in the aggregate.
    public sealed class PersonSpawnClaims
    {
        readonly HashSet<string> claimed = new HashSet<string>(StringComparer.Ordinal);
        public bool TryClaim(CampPersonState person) { return claimed.Add(CampLifecyclePolicy.SpawnClaimKey(person)); }
        public void Release(CampPersonState person) { claimed.Remove(CampLifecyclePolicy.SpawnClaimKey(person)); }
        public int Count { get { return claimed.Count; } }
    }

    public sealed class EncounterArcRuntime
    {
        readonly IEncounterArcStore store;
        readonly RescueReleasePolicy rescue = new RescueReleasePolicy();

        public EncounterArcRuntime(IEncounterArcStore store)
        { if (store == null) throw new ArgumentNullException("store"); this.store = store; }

        public ArcTransition Create(string transactionId, EncounterArcState initial)
        {
            string invalid = Validate(initial);
            if (invalid != null) return Refused(invalid, initial);
            if (initial.Revision != 0) return Refused("initial_revision_must_be_zero", initial);
            var next = initial.Copy(); next.AppliedTransactions.Add(transactionId); next.Revision = 1;
            return store.CompareExchange(next.EncounterId, 0, next)
                ? Done("arc_created", next) : Refused("encounter_already_exists", store.Load(next.EncounterId));
        }

        public ArcTransition WitnessDanger(string encounterId, long expectedRevision, string transactionId,
            string incidentId, DangerSubject subject, ThreatEvidence evidence, double now, bool nativeCombatCommitted)
        {
            FriendInNeedCommand[] commands = new FriendInNeedCommand[0];
            return Change(encounterId, expectedRevision, transactionId, delegate(EncounterArcState next) {
                if (!Same(next.IncidentId, incidentId)) return "incident_identity_mismatch";
                FriendInNeedResult result = FriendInNeedContract.Witness(next.Alert, incidentId, subject,
                    subject == DangerSubject.Camp ? next.CampId : next.PersonId, evidence, now, nativeCombatCommitted);
                if (result.State == next.Alert && result.Commands.Length == 0) return "no_material_danger_change";
                next.Alert = result.State; commands = result.Commands; return null;
            }, delegate(ArcTransition result) { result.Commands = commands; });
        }

        public ArcTransition ObservePerson(string encounterId, long expectedRevision, string transactionId, Observation observation)
        {
            RuntimeDirective directive = RuntimeDirective.None; ActionOwner owner = ActionOwner.None;
            return Change(encounterId, expectedRevision, transactionId, delegate(EncounterArcState next) {
                LifecycleDecision decision = CampLifecyclePolicy.Observe(next.CampPerson, next.Camp, observation);
                directive = decision.Directive; owner = decision.Owner;
                if (!decision.Persist) return "observation_requires_no_persistence";
                next.CampPerson = decision.State;
                if (decision.State.Life == PersonLife.Dead)
                {
                    var death = new DeathRecord { Place=decision.State.DeathPlace, Cause=decision.State.DeathCause,
                        WorldTime=decision.State.DeathWorldTick };
                    LivingWorld.Companions.RescueRelease.PolicyResult committed = rescue.CommitDeath(next.Roster, transactionId + ":death", next.PersonId, death);
                    if (!committed.Accepted) return committed.Reason;
                    next.Roster = committed.State;
                    if (next.Lead.State == LeadState.Active)
                        DiscoveryPolicy.TryClose(next.Lead, LeadState.Death, (int)observation.WorldTick);
                }
                return null;
            }, delegate(ArcTransition result) { result.LifecycleDirective=directive; result.LifecycleOwner=owner; });
        }

        public ArcTransition ResolveRescue(string encounterId, long expectedRevision, string transactionId,
            RescueOutcome outcome, LeadState leadOutcome, int worldDay)
        {
            return Change(encounterId, expectedRevision, transactionId, delegate(EncounterArcState next) {
                if (leadOutcome != LeadState.Resolved && leadOutcome != LeadState.Refused && leadOutcome != LeadState.Abandoned)
                    return "invalid_lead_outcome";
                LivingWorld.Companions.RescueRelease.PolicyResult result = rescue.ResolveRescue(next.Roster, transactionId + ":rescue", next.PersonId, outcome);
                if (!result.Accepted) return result.Reason;
                if (!DiscoveryPolicy.TryClose(next.Lead, leadOutcome, worldDay)) return "lead_close_refused";
                next.Roster = result.State; return null;
            }, null);
        }

        // Camp/world observers, affinity, voice, and earned-respect modules enter through
        // this evidence-only seam. Invitation remains a later, separate yes/no policy call.
        public ArcTransition RecordFacts(string encounterId, long expectedRevision, string transactionId,
            IList<ArcFact> facts)
        {
            return Change(encounterId, expectedRevision, transactionId, delegate(EncounterArcState next) {
                if (facts == null || facts.Count == 0) return "facts_required";
                for (int i=0; i<facts.Count; i++)
                {
                    ArcFact fact=facts[i];
                    if (fact == null || !Id(fact.Key) || fact.Value == null || fact.Value.Length > 256)
                        return "invalid_fact";
                    next.Facts[fact.Key]=fact.Value;
                }
                return null;
            }, null);
        }

        public ArcTransition RequestRelease(string encounterId, long expectedRevision, string transactionId,
            string destinationId, ReleaseSafety safety)
        {
            return Release(encounterId, expectedRevision, transactionId, destinationId, safety, false);
        }

        public ArcTransition CommitRelease(string encounterId, long expectedRevision, string transactionId,
            string destinationId, ReleaseSafety proof)
        {
            return Release(encounterId, expectedRevision, transactionId, destinationId, proof, true);
        }

        ArcTransition Release(string encounterId, long revision, string transactionId, string destinationId,
            ReleaseSafety proof, bool commit)
        {
            return Change(encounterId, revision, transactionId, delegate(EncounterArcState next) {
                ReleaseResult result = commit
                    ? rescue.CommitRelease(next.Roster, transactionId + ":release", next.PersonId, destinationId, proof)
                    : rescue.RequestRelease(next.Roster, transactionId + ":release", next.PersonId, destinationId, proof);
                if (result.Decision == ReleaseDecision.Rejected) return result.Reason;
                // SafeDestination ids are not necessarily CampLifecycle camp ids. The live adapter
                // must explicitly map them before changing CampPerson; never silently alias them.
                next.Roster = result.State;
                return null;
            }, null);
        }

        delegate string Mutation(EncounterArcState state);
        delegate void Decorate(ArcTransition result);
        ArcTransition Change(string encounterId, long expectedRevision, string transactionId, Mutation mutation, Decorate decorate)
        {
            if (String.IsNullOrEmpty(transactionId)) return Refused("transaction_id_required", null);
            EncounterArcState current = store.Load(encounterId);
            if (current == null) return Refused("unknown_encounter", null);
            if (current.AppliedTransactions.Contains(transactionId))
                return new ArcTransition { Committed=true, Replayed=true, Reason="transaction_already_applied", State=current };
            if (current.Revision != expectedRevision) return Refused("revision_conflict", current);
            var next = current.Copy(); string error = mutation(next);
            if (error != null) return Refused(error, current);
            next.AppliedTransactions.Add(transactionId); next.Revision++;
            error = Validate(next); if (error != null) return Refused(error, current);
            ArcTransition answer = store.CompareExchange(encounterId, expectedRevision, next)
                ? Done("transaction_committed", next) : Refused("concurrent_write", store.Load(encounterId));
            if (decorate != null) decorate(answer); return answer;
        }

        public static string Validate(EncounterArcState s)
        {
            if (s == null || s.SchemaVersion != EncounterArcState.CurrentSchema) return "unsupported_or_missing_arc";
            if (!Id(s.EncounterId) || !Id(s.SiteId) || !Id(s.PersonId) || !Id(s.CampId) || !Id(s.IncidentId)) return "stable_ids_required";
            if (s.Lead == null || s.Camp == null || s.CampPerson == null || s.Roster == null) return "incomplete_aggregate";
            if (!Same(s.EncounterId, s.Lead.EncounterId) || !Same(s.SiteId, s.Lead.SiteId) ||
                !Same(s.CampId, s.Camp.CampId) || !Same(s.CampId, s.CampPerson.CampId) || !Same(s.PersonId, s.CampPerson.PersonId))
                return "cross_module_identity_mismatch";
            CompanionRecord person;
            if (!s.Roster.People.TryGetValue(s.PersonId, out person) || person == null ||
                !Same(person.PersonId, s.PersonId) || !Same(person.OriginEncounterId, s.EncounterId) || !Same(person.OriginCampId, s.CampId))
                return "roster_identity_mismatch";
            return null;
        }

        static bool Id(string x) { return !String.IsNullOrWhiteSpace(x) && x.IndexOf('|') < 0; }
        static bool Same(string a, string b) { return StringComparer.Ordinal.Equals(a, b); }
        static ArcTransition Done(string reason, EncounterArcState state) { return new ArcTransition { Committed=true, Reason=reason, State=state }; }
        static ArcTransition Refused(string reason, EncounterArcState state) { return new ArcTransition { Reason=reason, State=state }; }
    }

    // QuestBroker is the sole durable owner of request lifecycle and revision. This bridge
    // intentionally returns BrokerState from QuestBrokerPolicy and never copies it into an arc.
    public static class QuestBrokerBridge
    {
        public static BrokerResult Create(BrokerState broker, BrokerConfig config, string commandId,
            long worldSeed, long worldTick, RequestCandidate candidate, PresentationProof source)
        { return QuestBrokerPolicy.Create(broker, config, commandId, worldSeed, worldTick, candidate, source); }

        public static BrokerResult Present(BrokerState broker, string commandId, string requestId, PresentationProof source)
        { return QuestBrokerPolicy.PresentExisting(broker, commandId, requestId, source); }
    }

    // Module states remain owned by their modules. These bridges validate the shared arc
    // identity, then return the module result for its own revisioned store to persist.
    public static class EncounterAllegianceBridge
    {
        public static ContactResult Apply(EncounterArcState arc, PersonContactRecord contact, ContactFact fact)
        {
            if (arc == null || contact == null || contact.PersonId != arc.PersonId || contact.CampId != arc.CampId)
                return new ContactResult { Reason="arc_contact_identity_mismatch", Record=contact };
            return EncounterAllegiancePolicy.Apply(contact, fact);
        }
    }

    public static class NeedsAidBridge
    {
        public static RequestSelection Select(EncounterArcState arc, NeedsState needs, string commandId,
            long worldSeed, long worldTick, IList<NeedCandidate> candidates)
        {
            if (arc == null) return new RequestSelection { Reason="missing_arc", State=needs };
            return NeedsAndAffinityPolicy.SelectRequest(needs, commandId, worldSeed, worldTick, arc.PersonId, candidates);
        }

        public static ChoiceResult Complete(EncounterArcState arc, NeedsState needs, string commandId, CompletionEvidence evidence)
        {
            if (arc == null || evidence == null || evidence.PersonId != arc.PersonId)
                return new ChoiceResult { Reason="arc_aid_identity_mismatch", State=needs };
            RequestRecord request;
            if (needs == null || !needs.Requests.TryGetValue(evidence.RequestId, out request) || request.EncounterId != arc.EncounterId)
                return new ChoiceResult { Reason="arc_aid_request_mismatch", State=needs };
            return NeedsAndAffinityPolicy.Complete(needs, commandId, evidence);
        }
    }

    public static class SpecialistCombatBridge
    {
        public static ThrowDecision Reserve(EncounterArcState arc, ThrowState state, ThrowConfig config, ThrowFacts facts)
        {
            if (arc == null || facts == null || facts.ActorId != arc.PersonId ||
                facts.PersonId != arc.PersonId || facts.EncounterId != arc.EncounterId)
                return new ThrowDecision { Reason="arc_specialist_identity_mismatch", State=state };
            return PredictionAndThrowsPolicy.Reserve(state, config, facts);
        }

        public static MeleeDecision Knife(EncounterArcState arc, string actorPersonId, KnifeLungeContext context)
        {
            if (arc == null || actorPersonId != arc.PersonId)
                return new MeleeDecision { Request=MeleeRequest.None, Refusal=Refusal.TargetNotAuthorized,
                    Reason="arc_specialist_identity_mismatch" };
            return MeleeRolePolicy.EvaluateKnifeLunge(context);
        }

        public static MeleeDecision Heavy(EncounterArcState arc, string actorPersonId, HeavyPressureContext context)
        {
            if (arc == null || actorPersonId != arc.PersonId)
                return new MeleeDecision { Request=MeleeRequest.None, Refusal=Refusal.TargetNotAuthorized,
                    Reason="arc_specialist_identity_mismatch" };
            return MeleeRolePolicy.EvaluateHeavyPressure(context);
        }
    }

    public static class EncounterActivationBridge
    {
        public static ActivationResult Register(EncounterArcState arc, BrokerState broker, ActivationLedger ledger,
            string commandId, ActivationPlan plan)
        {
            WorldRequest request;
            if (arc == null || broker == null || plan == null || !broker.Requests.TryGetValue(plan.RequestId, out request))
                return new ActivationResult { Reason="broker_request_required", State=ledger };
            if (request.Status != LivingWorld.CompanionSystems.QuestBroker.RequestStatus.Accepted ||
                request.IssuerPersonId != arc.PersonId || request.EncounterId != arc.EncounterId ||
                plan.IssuerPersonId != request.IssuerPersonId || plan.PartyId != request.PartyId ||
                plan.WorldId != request.WorldId || plan.SiteId != request.SiteId || plan.SiteIdentity != request.SiteIdentity)
                return new ActivationResult { Reason="activation_broker_identity_mismatch", State=ledger };
            return EncounterActivationPolicy.RegisterOffer(ledger,commandId,plan);
        }
    }

    public static class TemporaryAidBridge
    {
        public static TemporaryAidResult Start(EncounterArcState arc, NeedsState needs, BrokerState broker,
            ActivationLedger activation, WorldState roster, RescueReleasePolicy policy, string commandId,
            string needRequestId, string brokerRequestId, string returnDestinationId, bool allowPortalTravel)
        {
            RequestRecord need; AidArcRecord aid; WorldRequest request; ActivationEntry execution;
            if (arc == null || needs == null || broker == null || activation == null || roster == null || policy == null ||
                !needs.Requests.TryGetValue(needRequestId,out need) || !needs.AidArcs.TryGetValue(needRequestId,out aid) ||
                !broker.Requests.TryGetValue(brokerRequestId,out request) || !activation.Entries.TryGetValue(brokerRequestId,out execution))
                return new TemporaryAidResult { Decision=TemporaryAidDecision.Rejected, Reason="complete_aid_chain_required", State=roster };
            TemporaryTravelEvidenceHandoff travel=NeedsAndAffinityPolicy.BuildTemporaryTravelHandoff(needs,needRequestId);
            if (need.PersonId!=arc.PersonId || need.EncounterId!=arc.EncounterId || aid.PersonId!=arc.PersonId ||
                !travel.MayRequestTemporaryAssignment || travel.PersonId!=arc.PersonId ||
                request.AuthoredRequestId!=need.RequestId || request.Status!=LivingWorld.CompanionSystems.QuestBroker.RequestStatus.Accepted ||
                execution.Plan.IssuerPersonId!=arc.PersonId ||
                (execution.Status!=ActivationStatus.Accepted && execution.Status!=ActivationStatus.Arming && execution.Status!=ActivationStatus.Active))
                return new TemporaryAidResult { Decision=TemporaryAidDecision.Rejected, Reason="aid_chain_identity_or_acceptance_mismatch", State=roster };
            return policy.StartTemporaryAid(roster,commandId,new TemporaryAidFacts { RequestId=needRequestId,
                PersonId=arc.PersonId,EncounterId=arc.EncounterId,ReturnDestinationId=returnDestinationId,
                AuthoredRequest=true,RequestAccepted=true,PersonExplicitlyAgreed=true,AllowPortalTravel=allowPortalTravel });
        }
    }

    public static class EncounterLoadoutBridge
    {
        public static LivingWorld.CompanionSystems.EncounterLoadouts.PolicyResult Create(EncounterArcState arc,
            EncounterProfile profile,string variantId,ISet<string> worldKeys)
        {
            if (!SameArc(arc,profile)) return new LivingWorld.CompanionSystems.EncounterLoadouts.PolicyResult { Reason="arc_loadout_identity_mismatch" };
            return EncounterLoadoutPolicy.Create(profile,variantId,worldKeys);
        }

        public static LivingWorld.CompanionSystems.EncounterLoadouts.PolicyResult Upgrade(EncounterArcState arc,
            EncounterProfile profile,PersonLoadoutState current,UpgradeRequest request)
        {
            if (!SameArc(arc,profile) || current==null || current.PersonId!=arc.PersonId)
                return new LivingWorld.CompanionSystems.EncounterLoadouts.PolicyResult { Reason="arc_loadout_identity_mismatch",State=current };
            return EncounterLoadoutPolicy.Upgrade(profile,current,request);
        }

        static bool SameArc(EncounterArcState arc,EncounterProfile profile)
        { return arc!=null && profile!=null && profile.PersonId==arc.PersonId &&
            profile.OriginEncounterId==arc.EncounterId && profile.OriginCampId==arc.CampId; }
    }
}

using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterActivation
{
    public enum ActivationStatus { Offered, Accepted, Arming, Active, Clear, Failed, Abandoned }
    public enum TargetMode { NaturalWorld, AuthoredGroup, AuthoredMiniboss }
    public enum ActivationDirective { None, RequestSpawn, ObserveNaturalTarget, NotifyBrokerClear }

    public sealed class ActivationPlan
    {
        public string RequestId, PartyId, IssuerPersonId, WorldId, SiteId, SiteIdentity;
        public string AdapterId, AuthoredEncounterId;
        public TargetMode Mode;
        public ActivationPlan Copy() { return (ActivationPlan)MemberwiseClone(); }
    }

    public sealed class ActivationEntry
    {
        public ActivationPlan Plan;
        public ActivationStatus Status;
        public string SpawnClaimId, SpawnRequestId, SpawnReceiptId, OutcomeEvidenceId;
        public long Revision, AcceptedWorldTick, ActivatedWorldTick, ResolvedWorldTick;
        public ActivationEntry Copy() { return new ActivationEntry { Plan=Plan == null ? null : Plan.Copy(), Status=Status,
            SpawnClaimId=SpawnClaimId, SpawnRequestId=SpawnRequestId, SpawnReceiptId=SpawnReceiptId,
            OutcomeEvidenceId=OutcomeEvidenceId, Revision=Revision, AcceptedWorldTick=AcceptedWorldTick,
            ActivatedWorldTick=ActivatedWorldTick, ResolvedWorldTick=ResolvedWorldTick }; }
    }

    public sealed class ActivationLedger
    {
        public long Revision;
        public readonly Dictionary<string, ActivationEntry> Entries = new Dictionary<string, ActivationEntry>(StringComparer.Ordinal);
        public readonly HashSet<string> AppliedCommands = new HashSet<string>(StringComparer.Ordinal);
        public ActivationLedger Copy() { var n=new ActivationLedger { Revision=Revision };
            foreach (var p in Entries) n.Entries.Add(p.Key,p.Value.Copy());
            foreach (var id in AppliedCommands) n.AppliedCommands.Add(id); return n; }
    }

    // Every boolean is an authoritative adapter observation. Missing evidence fails closed.
    public sealed class ArmingProof
    {
        public string RequestId, WorldId, SiteId, SiteIdentity, ObservingPlayerId;
        public bool ServerAuthority, BrokerRequestAccepted, ProgressionLegal, SiteStillSafe;
        public bool SiteLoaded, IssuerGroupLoadedAtTarget, PlayerLoadedAtTarget, PlayerDeliberatelyApproached;
        public bool IssuerPersonIdentityPreserved, IssuerPersonalEquipmentPreserved;
        public long WorldTick;
    }

    public sealed class SpawnConfirmation
    {
        public string RequestId, SpawnClaimId, SpawnRequestId, SpawnReceiptId, WorldId, SiteId, SiteIdentity;
        public bool ServerAuthority, AdapterAccepted, ActorProvenanceBound;
        public long WorldTick;
    }

    public sealed class OutcomeProof
    {
        public string RequestId, WorldId, SiteId, SiteIdentity, EvidenceId, SpawnClaimId;
        public bool ServerAuthority, TargetLoaded, PlayerLoadedAtTarget, IssuerWitnessOrAlertPreserved;
        public bool ObjectiveObservedComplete, PlayerOrPartyContributed, AllClaimedActorsDead;
        public bool CommittedBossDeath;
        public long WorldTick;
    }

    public sealed class ActivationResult
    {
        public bool Applied, Replayed;
        public string Reason;
        public ActivationDirective Directive;
        public ActivationEntry Entry;
        public ActivationLedger State;
    }
}

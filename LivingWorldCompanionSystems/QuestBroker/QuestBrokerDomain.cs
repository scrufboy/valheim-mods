using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.QuestBroker
{
    public enum RequestKind { CompanionNeed, WorldService, AuthoredMiniboss }
    public enum RequestStatus { Offered, Accepted, Refused, Completed, Expired }
    public enum OfferSourceKind { CampPerson, SettlementBoard, Jarl }
    public enum RequestVisibility { PrivateIssuerOnly, PublicSurfaces }

    public sealed class BrokerConfig
    {
        public double MaximumTargetDistance = 2500d;
        public long ResolutionCooldownTicks = 12000;
        public long MinimumLifetimeTicks = 1200;
        public int MaximumActiveRequestsPerParty = 1;
    }

    // Facts are supplied by the server adapter from persisted campaign/site records.
    // Boolean observations fail closed; the policy never scans for a convenient target.
    public sealed class RequestCandidate
    {
        public string AuthoredRequestId, PartyId, IssuerPersonId, EncounterId;
        public string WorldId, SiteId, SiteIdentity, Biome, Description;
        public string RequiredWorldKey, AuthoredEncounterId, EncounterAdapterId;
        public RequestKind Kind;
        public RequestVisibility Visibility;
        public double DistanceFromIssuer;
        public long ExpiresWorldTick;
        public bool ServerAuthority, SitePersisted, SiteKnownToIssuer, WorldFactCurrent;
        public bool SafeGround, Reachable, BiomeMatches, ProgressionLegal;
        public bool AuthoredEncounter, SafeSpawnAdapterVerified;
    }

    public sealed class PresentationProof
    {
        public string SourceId, SourcePersonId, SettlementId;
        public OfferSourceKind SourceKind;
        public bool ServerAuthority, SourceExists, SourceLoadedAndNearby;
        public bool SourceKnowsRequest, BoardOrJarlAtKnownSettlement;
        public long WorldTick;
    }

    public sealed class OfferPresentation
    {
        public string PresentationId, RequestId, SourceId, SourcePersonId, SettlementId;
        public OfferSourceKind SourceKind;
        public long PresentedWorldTick;
        public OfferPresentation Copy() { return (OfferPresentation)MemberwiseClone(); }
    }

    public sealed class WorldRequest
    {
        public string RequestId, AuthoredRequestId, PartyId, IssuerPersonId, EncounterId;
        public string WorldId, SiteId, SiteIdentity, Biome, Description;
        public string RequiredWorldKey, AuthoredEncounterId, EncounterAdapterId;
        public RequestKind Kind;
        public RequestVisibility Visibility;
        public RequestStatus Status;
        public long CreatedWorldTick, ExpiresWorldTick, ResolvedWorldTick;
        public string CompletionEvidenceId, ClaimId;
        public WorldRequest Copy() { return (WorldRequest)MemberwiseClone(); }
    }

    public sealed class BrokerState
    {
        public long Revision;
        public readonly Dictionary<string, WorldRequest> Requests = new Dictionary<string, WorldRequest>(StringComparer.Ordinal);
        public readonly Dictionary<string, OfferPresentation> Presentations = new Dictionary<string, OfferPresentation>(StringComparer.Ordinal);
        public readonly HashSet<string> AppliedCommands = new HashSet<string>(StringComparer.Ordinal);
        public BrokerState Copy()
        {
            var n = new BrokerState { Revision = Revision };
            foreach (var p in Requests) n.Requests.Add(p.Key, p.Value.Copy());
            foreach (var p in Presentations) n.Presentations.Add(p.Key, p.Value.Copy());
            foreach (var id in AppliedCommands) n.AppliedCommands.Add(id);
            return n;
        }
    }

    public sealed class CompletionProof
    {
        public string RequestId, WorldId, SiteId, SiteIdentity, EvidenceId;
        public bool ServerAuthority, TargetLoaded, PlayerAtTarget, ObjectiveObservedComplete;
        public bool ProgressionLegal, EncounterAdapterOwnsOutcome;
        public long WorldTick;
    }

    public sealed class BrokerResult
    {
        public bool Applied, Replayed, MayPresent, SingleClaimCommitted;
        public string Reason, ClaimId;
        public WorldRequest Request;
        public OfferPresentation Presentation;
        public BrokerState State;
    }
}

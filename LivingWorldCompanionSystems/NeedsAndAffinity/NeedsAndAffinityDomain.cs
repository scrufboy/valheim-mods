using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.NeedsAndAffinity
{
    public enum NeedKind { Food, NearbyDanger, LegalDungeonMaterials, SettlementHelp }
    public enum RequestStatus { Offered, Accepted, Refused, Completed, Invalidated }
    public enum RememberedChoice { Helped, RefusedWithoutHarm, HonoredBoundary, Griefed }
    public enum RelationshipReading { Wary, Known, Trusting, Unsafe }
    public enum AidArcPhase { NeedKnown, AidAgreed, EventActivated, ContributionWitnessed, TrustEstablished, InvitationEligible, Closed }

    // A candidate is authored content joined to current, adapter-verified world facts.
    // It is not a free-form quest generated from an NPC scan.
    public sealed class NeedCandidate
    {
        public string RequestId, PersonId, EncounterId, AuthoredNeedId, TargetId, Description;
        public NeedKind Kind;
        public bool PersonNearby, TargetNearby, TargetLoaded, AuthoredForPerson, FactCurrentlyTrue;
        public bool ProgressionLegal, PlayerOwnsTarget, IsRepeatable;
        public int RequiredAmount;
    }

    public sealed class RequestRecord
    {
        public string RequestId, PersonId, EncounterId, AuthoredNeedId, TargetId, Description;
        public NeedKind Kind;
        public RequestStatus Status;
        public int RequiredAmount;
        public long OfferedWorldTick, ResolvedWorldTick;
        public RequestRecord Copy() { return (RequestRecord)MemberwiseClone(); }
    }

    public sealed class ChoiceMemory
    {
        public string MemoryId, PersonId, RequestId, EvidenceId;
        public RememberedChoice Choice;
        public long WorldTick;
        public ChoiceMemory Copy() { return (ChoiceMemory)MemberwiseClone(); }
    }

    // Person-facing evidence only. QuestBroker owns operational quest status;
    // EncounterActivation owns one claimed event; RescueRelease owns travel assignment.
    public sealed class AidArcRecord
    {
        public string RequestId, PersonId, ActivationId, ContributionEvidenceId;
        public AidArcPhase Phase;
        public bool PlayerAgreed, EventActivationVerified, ContributionWitnessed, BoundaryHonored;
        public AidArcRecord Copy() { return (AidArcRecord)MemberwiseClone(); }
    }

    public sealed class NeedsState
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public long Revision;
        public readonly Dictionary<string, RequestRecord> Requests = new Dictionary<string, RequestRecord>(StringComparer.Ordinal);
        public readonly Dictionary<string, ChoiceMemory> Memories = new Dictionary<string, ChoiceMemory>(StringComparer.Ordinal);
        public readonly Dictionary<string, AidArcRecord> AidArcs = new Dictionary<string, AidArcRecord>(StringComparer.Ordinal);
        public readonly HashSet<string> AppliedCommands = new HashSet<string>(StringComparer.Ordinal);

        public NeedsState Copy()
        {
            var copy = new NeedsState { SchemaVersion = SchemaVersion, Revision = Revision };
            foreach (var pair in Requests) copy.Requests.Add(pair.Key, pair.Value.Copy());
            foreach (var pair in Memories) copy.Memories.Add(pair.Key, pair.Value.Copy());
            foreach (var pair in AidArcs) copy.AidArcs.Add(pair.Key, pair.Value.Copy());
            foreach (var id in AppliedCommands) copy.AppliedCommands.Add(id);
            return copy;
        }
    }

    public sealed class RequestSelection
    {
        public bool Offered;
        public string Reason;
        public RequestRecord Request;
        public NeedsState State;
        public bool Replayed;
    }

    public sealed class ChoiceResult
    {
        public bool Accepted;
        public string Reason;
        public NeedsState State;
        public bool Replayed;
        public bool GrantsReward;
    }

    public sealed class CompletionEvidence
    {
        public string RequestId, PersonId, TargetId, EvidenceId;
        public NeedKind Kind;
        public bool PersonNearby, TargetNearby, TargetLoaded, Authoritative;
        public bool RequirementSatisfied, ProgressionLegal, PlayerOwnsTarget;
        public int DeliveredAmount;
        public long WorldTick;
    }

    // RescueRelease remains the only owner of final yes/no recruitment and assignment.
    public sealed class InvitationEvidenceHandoff
    {
        public string PersonId;
        public bool ReadyForRescueReleaseReview;
        public bool ConcreteNeedCompleted;
        public bool HonoredBoundary;
        public bool UnresolvedGriefing;
        public string[] Reasons;
    }

    public sealed class AidArcResult
    {
        public bool Accepted, Replayed;
        public string Reason;
        public NeedsState State;
        public AidArcRecord Arc;
    }

    public sealed class PersonalInvitationHandoff
    {
        public string RequestId, PersonId;
        public bool MayPresentPersonalOffer;
        public string[] Reasons;
    }

    // Evidence for RescueRelease to consider; it neither assigns nor reserves a slot.
    public sealed class TemporaryTravelEvidenceHandoff
    {
        public string RequestId, PersonId;
        public bool MayRequestTemporaryAssignment;
        public string[] Reasons;
    }

    // Read-only fact projection for the separate QuestBroker. It deliberately
    // contains no coordinates, seed/map lookup, board/jarl routing, or reward.
    public sealed class QuestRequestFact
    {
        public string RequestId, PersonId, EncounterId, AuthoredNeedId, TargetId, Description;
        public NeedKind Kind;
        public RequestStatus Status;
        public int RequiredAmount;
    }
}

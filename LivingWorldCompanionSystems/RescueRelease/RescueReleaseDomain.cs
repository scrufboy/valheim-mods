using System;
using System.Collections.Generic;

namespace LivingWorld.Companions.RescueRelease
{
    public enum LifeState { Alive, Dead }
    public enum RescueOutcome { Unresolved, Rescued, RefusedAid, Abandoned, Died }
    public enum Assignment { Known, BondedResident, Expedition, AwaitingSafeHandoff, ReleasedKnown, TemporaryAlly, AwaitingTemporaryReturn, Dead }
    public enum DestinationKind { Home, OriginCamp, ReclaimedVillage }
    public enum InvitationDecision { Yes, No }
    public enum ReleaseDecision { Committed, WaitingForSafety, Rejected }
    public enum TemporaryAidDecision { Started, WaitingForReturn, Returned, Rejected }
    public enum TemporaryAidEndReason { RequestCompleted, PersonWithdrew, PlayerEnded }
    public enum GearOwner { Companion, PlayerLoan }

    public sealed class GearItem
    {
        public string ItemId;
        public GearOwner Owner;
        public bool Recovered;

        public GearItem Copy() { return (GearItem)MemberwiseClone(); }
    }

    public sealed class SafeDestination
    {
        public string DestinationId;
        public DestinationKind Kind;
        public bool Established;

        public SafeDestination Copy() { return (SafeDestination)MemberwiseClone(); }
    }

    public sealed class DeathRecord
    {
        public string Name;
        public string Role;
        public string AppearanceId;
        public string Place;
        public long WorldTime;
        public string Cause;

        public DeathRecord Copy() { return (DeathRecord)MemberwiseClone(); }
    }

    public sealed class CompanionRecord
    {
        public string PersonId;
        public string Name;
        public string Role;
        public string AppearanceId;
        public string OriginEncounterId;
        public string OriginCampId;
        public string HomeId;
        public bool DebugIdentity;
        public LifeState Life = LifeState.Alive;
        public RescueOutcome Rescue = RescueOutcome.Unresolved;
        public Assignment Assignment = Assignment.Known;
        public string PendingDestinationId;
        public string TemporaryRequestId;
        public string TemporaryReturnDestinationId;
        public bool TemporaryPortalTravelAllowed;
        public string LastSafePositionId;
        public string Relationship;
        public string History;
        public string Injuries;
        public List<GearItem> Gear = new List<GearItem>();
        public DeathRecord Death;

        public CompanionRecord Copy()
        {
            var copy = (CompanionRecord)MemberwiseClone();
            copy.Death = Death == null ? null : Death.Copy();
            copy.Gear = new List<GearItem>();
            for (var i = 0; i < Gear.Count; i++) copy.Gear.Add(Gear[i].Copy());
            return copy;
        }
    }

    public sealed class WorldState
    {
        public const int CurrentSchemaVersion = 1;
        public int SchemaVersion = CurrentSchemaVersion;
        public long Revision;
        public readonly Dictionary<string, CompanionRecord> People = new Dictionary<string, CompanionRecord>(StringComparer.Ordinal);
        public readonly Dictionary<string, SafeDestination> Destinations = new Dictionary<string, SafeDestination>(StringComparer.Ordinal);
        public readonly HashSet<string> AppliedCommands = new HashSet<string>(StringComparer.Ordinal);

        public WorldState Copy()
        {
            var copy = new WorldState { SchemaVersion = SchemaVersion, Revision = Revision };
            foreach (var pair in People) copy.People.Add(pair.Key, pair.Value.Copy());
            foreach (var pair in Destinations) copy.Destinations.Add(pair.Key, pair.Value.Copy());
            foreach (var id in AppliedCommands) copy.AppliedCommands.Add(id);
            return copy;
        }
    }

    public sealed class InvitationFacts
    {
        public string SafeDestinationId;
        public bool SharedDefense;
        public bool RescueCompleted;
        public bool HonoredWarning;
        public bool ReturnedProperty;
        public bool SanctuaryOffered;
        public bool CompatiblePurpose;
        public bool PriorStanding;
        public bool DefendedHearth;
        public bool Hostile;
        public bool Betrayed;
        public bool DependentCannotTravel;
        public bool ImmediateDutyUnfinished;
        public bool IncompatibleAllegiance;
        public bool EssentialSafeDestinationMissing;
        public bool RoleVowBlocksTravel;
    }

    public sealed class InvitationResult
    {
        public InvitationDecision Decision;
        public string[] Reasons;
        public WorldState State;
        public bool Replayed;
    }

    public sealed class PolicyResult
    {
        public bool Accepted;
        public string Reason;
        public WorldState State;
        public bool Replayed;
    }

    public sealed class ReleaseResult
    {
        public ReleaseDecision Decision;
        public string Reason;
        public WorldState State;
        public bool Replayed;
    }

    public sealed class ReleaseSafety
    {
        public bool InCombat;
        public bool Incapacitated;
        public bool Sailing;
        public bool PortalTransit;
        public bool UnsafeTerrain;
        public bool DestinationZoneLoaded;
        public bool CompanionPresentAtDestination;
        public bool LoanExchangeComplete;
    }

    // Translated from committed NeedsAndAffinity/QuestBroker evidence. This
    // module does not own or complete the request ledger.
    public sealed class TemporaryAidFacts
    {
        public string RequestId;
        public string PersonId;
        public string EncounterId;
        public string ReturnDestinationId;
        public bool AuthoredRequest;
        public bool RequestAccepted;
        public bool PersonExplicitlyAgreed;
        public bool AllowPortalTravel;
    }

    public sealed class TemporaryAidResult
    {
        public TemporaryAidDecision Decision;
        public string Reason;
        public WorldState State;
        public bool Replayed;
    }
}

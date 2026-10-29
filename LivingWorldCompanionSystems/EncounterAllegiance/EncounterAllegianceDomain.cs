using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public enum Allegiance { Player, Hearthbound, Hearthless, Unaffiliated, Pillager, Eitrherjar, Fuling, Draugr, Wildlife, OtherMod }
    public enum Disposition { Friendly, Neutral, Hostile }
    public enum Readiness { Unaware, Observing, Wary, Guarded, CombatCommitted }
    public enum Relationship { Unknown, Friendly, Neutral, Wary, Hostile }
    public enum ContactAct { Notice, PeacefulSignal, WeaponRaised, WeaponLowered, Trespass, LeaveBoundary, Aid, Theft, FactionGrievance, AttackCommitted }
    public enum Presentation { SilhouetteAndBehavior, RevealedIdentity }

    public sealed class FactionIdentity
    {
        public Allegiance Allegiance;
        public string AdapterFactionId;
        public bool HasAuthoredDisposition;
        public Disposition AuthoredDisposition;
    }

    public sealed class PersonContactRecord
    {
        public string PersonId;
        public string CampId;
        public Allegiance Allegiance;
        public Readiness Readiness = Readiness.Unaware;
        public Relationship Relationship = Relationship.Unknown;
        public bool PersonNoticedPlayer;
        public bool PlayerNoticedPerson;
        public bool IdentityRevealed;
        public string RevealedName;
        public long Revision;
        public readonly HashSet<string> AppliedFactIds = new HashSet<string>(StringComparer.Ordinal);

    }

    public sealed class ContactFact
    {
        public string FactId;
        public ContactAct Act;
        public bool IsServer;
        public bool WitnessedByPerson;
        public bool WitnessedByPlayer;
        public bool InsideAuthoredBoundary;
        public FactionIdentity Actor;
    }

    public sealed class ContactResult
    {
        public bool Accepted;
        public string Reason;
        public PersonContactRecord Record;
        public Presentation Presentation;
        public bool YieldToNativeCombat;
        public bool YieldToPeacefulPresentation;
        public bool Replayed;
    }
}

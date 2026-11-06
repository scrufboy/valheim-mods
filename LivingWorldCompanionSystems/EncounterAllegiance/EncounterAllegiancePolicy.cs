using System;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public static class EncounterAllegiancePolicy
    {
        public const string Schema = "lwc-encounter-allegiance-v1";

        public static PersonContactRecord Create(string personId, string campId, Allegiance allegiance)
        {
            if (String.IsNullOrEmpty(personId) || String.IsNullOrEmpty(campId)) throw new ArgumentException("Stable person and camp IDs are required");
            return new PersonContactRecord { PersonId = personId, CampId = campId, Allegiance = allegiance };
        }

        public static ContactResult Apply(PersonContactRecord current, ContactFact fact)
        {
            if (!Valid(current) || fact == null || String.IsNullOrEmpty(fact.FactId)) return Denied(current, "invalid-input");
            if (!fact.IsServer) return Denied(current, "server-authority-required");
            if (current.AppliedFactIds.Contains(fact.FactId)) return Result(current, "already-applied", false, false, true);
            if (!fact.WitnessedByPerson && !fact.WitnessedByPlayer) return Denied(current, "unwitnessed-fact");

            PersonContactRecord next = Clone(current);
            next.AppliedFactIds.Add(fact.FactId);
            next.Revision++;
            if (fact.WitnessedByPerson) next.PersonNoticedPlayer = true;
            if (fact.WitnessedByPlayer) next.PlayerNoticedPerson = true;

            bool combat = false, peaceful = false;
            Disposition baseline = fact.Actor == null ? Disposition.Neutral : AllegianceMatrix.Get(next.Allegiance, fact.Actor);
            switch (fact.Act)
            {
                case ContactAct.Notice:
                    next.Readiness = Max(next.Readiness, baseline == Disposition.Hostile ? Readiness.Guarded :
                        baseline == Disposition.Friendly ? Readiness.Observing : Readiness.Wary);
                    // Sight informs posture, not moral certainty. Relationship
                    // changes require contact, witnessed conduct, or prior facts.
                    break;
                case ContactAct.PeacefulSignal:
                case ContactAct.WeaponLowered:
                case ContactAct.LeaveBoundary:
                    peaceful = true;
                    if (next.Readiness != Readiness.CombatCommitted) next.Readiness = StepDown(next.Readiness);
                    break;
                case ContactAct.WeaponRaised:
                    if (fact.WitnessedByPerson) next.Readiness = Max(next.Readiness, Readiness.Guarded);
                    break;
                case ContactAct.Trespass:
                    if (fact.WitnessedByPerson && fact.InsideAuthoredBoundary) { next.Readiness = Max(next.Readiness, Readiness.Guarded); next.Relationship = Worsen(next.Relationship); }
                    break;
                case ContactAct.Aid:
                    if (fact.WitnessedByPerson) { next.Readiness = StepDown(next.Readiness); next.Relationship = Improve(next.Relationship); }
                    break;
                case ContactAct.Theft:
                case ContactAct.FactionGrievance:
                    if (fact.WitnessedByPerson) { next.Readiness = Max(next.Readiness, Readiness.Guarded); next.Relationship = Worsen(next.Relationship); }
                    break;
                case ContactAct.AttackCommitted:
                    if (fact.WitnessedByPerson) { next.Readiness = Readiness.CombatCommitted; next.Relationship = Relationship.Hostile; combat = true; }
                    break;
            }
            return Result(next, "accepted", combat, peaceful, false);
        }

        public static bool TryReveal(PersonContactRecord current, bool isServer, string verifiedName, out PersonContactRecord revealed)
        {
            revealed = current;
            if (!isServer || !Valid(current) || current.IdentityRevealed || !current.PersonNoticedPlayer ||
                !current.PlayerNoticedPerson || String.IsNullOrEmpty(verifiedName) || verifiedName.Length > 80) return false;
            revealed = Clone(current); revealed.IdentityRevealed = true; revealed.RevealedName = verifiedName; revealed.Revision++; return true;
        }

        public static bool CanRecruit(PersonContactRecord record)
        { return Valid(record) && record.Allegiance == Allegiance.Hearthless && record.Relationship == Relationship.Friendly; }

        static Relationship Improve(Relationship value)
        { return value == Relationship.Hostile ? Relationship.Wary : value == Relationship.Wary ? Relationship.Neutral : value == Relationship.Unknown || value == Relationship.Neutral ? Relationship.Friendly : value; }
        static Relationship Worsen(Relationship value)
        { return value == Relationship.Friendly ? Relationship.Neutral : value == Relationship.Neutral || value == Relationship.Unknown ? Relationship.Wary : Relationship.Hostile; }
        static Readiness StepDown(Readiness value)
        { return value == Readiness.Guarded ? Readiness.Wary : value == Readiness.Wary ? Readiness.Observing : value; }
        static Readiness Max(Readiness a, Readiness b) { return a > b ? a : b; }
        static bool Valid(PersonContactRecord r) { return r != null && !String.IsNullOrEmpty(r.PersonId) && !String.IsNullOrEmpty(r.CampId); }
        static PersonContactRecord Clone(PersonContactRecord value)
        {
            PersonContactRecord copy = new PersonContactRecord { PersonId = value.PersonId, CampId = value.CampId, Allegiance = value.Allegiance,
                Readiness = value.Readiness, Relationship = value.Relationship, PersonNoticedPlayer = value.PersonNoticedPlayer,
                PlayerNoticedPerson = value.PlayerNoticedPerson, IdentityRevealed = value.IdentityRevealed,
                RevealedName = value.RevealedName, Revision = value.Revision };
            foreach (string id in value.AppliedFactIds) copy.AppliedFactIds.Add(id);
            return copy;
        }
        static ContactResult Denied(PersonContactRecord r, string reason) { return new ContactResult { Accepted = false, Reason = reason, Record = r, Presentation = Present(r) }; }
        static ContactResult Result(PersonContactRecord r, string reason, bool combat, bool peaceful, bool replayed)
        { return new ContactResult { Accepted = true, Reason = reason, Record = r, Presentation = Present(r), YieldToNativeCombat = combat, YieldToPeacefulPresentation = peaceful, Replayed = replayed }; }
        static Presentation Present(PersonContactRecord r) { return r != null && r.IdentityRevealed ? Presentation.RevealedIdentity : Presentation.SilhouetteAndBehavior; }
    }
}

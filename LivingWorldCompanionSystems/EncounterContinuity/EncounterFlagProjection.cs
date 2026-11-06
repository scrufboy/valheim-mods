using System;

namespace LivingWorld.CompanionSystems.EncounterContinuity
{
    [Flags] public enum EncounterFlag
    {
        None=0, SiteClaimed=1, PersonKnown=2, MutualNotice=4, AidAccepted=8,
        AidAgreed=16, TemporaryTravel=32, EventClaimed=64, EventResolved=128,
        AidVerified=256, InvitationOffered=512, Bonded=1024, ActiveParty=2048
    }

    // Facts are read from Discovery, Allegiance, Needs, Broker, Activation, and
    // RescueRelease. This projection does not own or persist any of their ledgers.
    public sealed class EncounterSourceFacts
    {
        public string PersonId, EncounterId, SiteId;
        public bool ServerAuthority, SiteClaimed, PersonKnown, MutualNotice;
        public bool AidAccepted, PersonAgreed, TemporaryTravel, EventClaimed, EventResolved;
        public bool AidVerified, InvitationOffered, Bonded, ActiveParty;
        public int ActiveFieldCount;
        public bool ZoneLoaded, ActorExists, ActorOwned, PlayerNear, ServerResidentSafe;
        public bool TargetZoneLoaded, PlayerAtTarget;
    }

    public sealed class EncounterView
    {
        public bool Valid;
        public string Reason;
        public EncounterFlag DurableFlags;
        public bool SceneActorAvailable, CanOfferAid, CanStartTemporaryTravel;
        public bool CanActivateSiteEvent, CanOfferPartyInvitation, CanAcceptPartyInvitation;
    }

    public static class EncounterFlagProjection
    {
        public const int ActiveFieldLimit = 2;

        public static EncounterView Project(EncounterSourceFacts f)
        {
            if(f==null || !f.ServerAuthority || String.IsNullOrWhiteSpace(f.PersonId) ||
               String.IsNullOrWhiteSpace(f.EncounterId) || String.IsNullOrWhiteSpace(f.SiteId))
                return Invalid("server_identity_required");
            if(f.ActiveFieldCount<0 || f.ActiveFieldCount>ActiveFieldLimit ||
               (f.PersonKnown && !f.SiteClaimed) || (f.MutualNotice && !f.PersonKnown) ||
               (f.PersonAgreed && !f.AidAccepted) || (f.TemporaryTravel && (!f.PersonAgreed || !f.AidAccepted)) ||
               (f.EventClaimed && !f.AidAccepted) || (f.EventResolved && !f.EventClaimed) ||
               (f.InvitationOffered && !f.PersonKnown) || (f.Bonded && !f.InvitationOffered) ||
               (f.ActiveParty && !f.Bonded)) return Invalid("source_ledgers_disagree");

            EncounterFlag flags=EncounterFlag.None;
            if(f.SiteClaimed)flags|=EncounterFlag.SiteClaimed;
            if(f.PersonKnown)flags|=EncounterFlag.PersonKnown;
            if(f.MutualNotice)flags|=EncounterFlag.MutualNotice;
            if(f.AidAccepted)flags|=EncounterFlag.AidAccepted;
            if(f.PersonAgreed)flags|=EncounterFlag.AidAgreed;
            if(f.TemporaryTravel)flags|=EncounterFlag.TemporaryTravel;
            if(f.EventClaimed)flags|=EncounterFlag.EventClaimed;
            if(f.EventResolved)flags|=EncounterFlag.EventResolved;
            if(f.AidVerified)flags|=EncounterFlag.AidVerified;
            if(f.InvitationOffered)flags|=EncounterFlag.InvitationOffered;
            if(f.Bonded)flags|=EncounterFlag.Bonded;
            if(f.ActiveParty)flags|=EncounterFlag.ActiveParty;

            // A resident camp may remain active without a player, but the runtime
            // must have proved a loaded zone and owned actor before exposing scene actions.
            bool actor=f.ZoneLoaded && f.ActorExists && f.ActorOwned &&
                (f.PlayerNear || f.ServerResidentSafe);
            var view=new EncounterView { Valid=true,Reason="projected_from_authoritative_ledgers",
                DurableFlags=flags,SceneActorAvailable=actor,
                CanOfferAid=actor && f.MutualNotice && !f.AidAccepted,
                CanStartTemporaryTravel=actor && f.PersonAgreed && !f.TemporaryTravel &&
                    f.ActiveFieldCount<ActiveFieldLimit,
                CanActivateSiteEvent=f.AidAccepted && f.EventClaimed && !f.EventResolved &&
                    f.TargetZoneLoaded && f.PlayerAtTarget,
                CanOfferPartyInvitation=actor && f.AidVerified && !f.TemporaryTravel &&
                    !f.InvitationOffered && !f.Bonded,
                CanAcceptPartyInvitation=actor && f.InvitationOffered && !f.TemporaryTravel && !f.Bonded &&
                    f.ActiveFieldCount<ActiveFieldLimit };
            return view;
        }

        static EncounterView Invalid(string reason)
        { return new EncounterView { Reason=reason,DurableFlags=EncounterFlag.None }; }
    }
}

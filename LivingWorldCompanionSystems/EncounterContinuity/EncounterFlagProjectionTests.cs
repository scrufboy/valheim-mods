using System;
using LivingWorld.CompanionSystems.EncounterContinuity;

static class EncounterFlagProjectionTests
{
    static int checks;
    static void Check(bool pass,string label) { if(!pass)throw new Exception("FAIL "+label);checks++; }
    static EncounterSourceFacts Facts()
    { return new EncounterSourceFacts { PersonId="person.a",EncounterId="encounter.1",SiteId="site.1",
        ServerAuthority=true,SiteClaimed=true,PersonKnown=true,ZoneLoaded=true,ActorExists=true,
        ActorOwned=true,PlayerNear=true,ActiveFieldCount=0 }; }

    static void Main()
    {
        var f=Facts();
        var unknown=EncounterFlagProjection.Project(f);
        Check(unknown.Valid && !unknown.CanOfferAid && !unknown.CanAcceptPartyInvitation,
            "mere sight of a known person offers no aid or automatic party place");
        f.MutualNotice=true;
        var contact=EncounterFlagProjection.Project(f);
        Check(contact.CanOfferAid && !contact.CanAcceptPartyInvitation,
            "mutual contact allows a request but not recruitment");
        f.AidAccepted=true;f.PersonAgreed=true;
        var accepted=EncounterFlagProjection.Project(f);
        Check(accepted.CanStartTemporaryTravel && !accepted.CanOfferPartyInvitation,
            "accepted aid and NPC agreement allow temporary travel only");
        f.TemporaryTravel=true;f.ActiveFieldCount=1;
        var traveling=EncounterFlagProjection.Project(f);
        Check(!traveling.CanStartTemporaryTravel && !traveling.CanAcceptPartyInvitation,
            "temporary aid consumes a field position without becoming a permanent bond");
        f.EventClaimed=true;f.TargetZoneLoaded=true;f.PlayerAtTarget=true;
        Check(EncounterFlagProjection.Project(f).CanActivateSiteEvent,
            "persisted event claim activates at the loaded target when the player arrives");
        f.ZoneLoaded=false;f.ActorExists=false;f.TargetZoneLoaded=false;
        var unloaded=EncounterFlagProjection.Project(f);
        Check(unloaded.Valid && !unloaded.SceneActorAvailable && !unloaded.CanActivateSiteEvent &&
            (unloaded.DurableFlags&EncounterFlag.TemporaryTravel)!=0 &&
            (unloaded.DurableFlags&EncounterFlag.EventClaimed)!=0,
            "zone unload hides scene actions but preserves accepted aid and event flags");
        f.TargetZoneLoaded=true;
        Check(!EncounterFlagProjection.Project(f).SceneActorAvailable &&
            EncounterFlagProjection.Project(f).CanActivateSiteEvent,
            "loaded target can activate claimed event while the issuer camp is unloaded");
        f.ZoneLoaded=true;f.ActorExists=true;f.TargetZoneLoaded=true;
        Check(EncounterFlagProjection.Project(f).CanActivateSiteEvent,
            "reload resumes the same claimed event without another quest roll");
        f.PlayerNear=false;
        Check(!EncounterFlagProjection.Project(f).SceneActorAvailable,
            "distant unretained zone cannot pretend to have an active actor");
        f.ServerResidentSafe=true;
        Check(EncounterFlagProjection.Project(f).SceneActorAvailable,
            "verified server-resident area keeps actor presentation available");
        f.PlayerNear=true;f.EventResolved=true;f.AidVerified=true;
        Check(!EncounterFlagProjection.Project(f).CanOfferPartyInvitation,
            "verified aid cannot convert an active temporary journey into a bond");
        f.TemporaryTravel=false;
        var helped=EncounterFlagProjection.Project(f);
        Check(!helped.CanActivateSiteEvent && helped.CanOfferPartyInvitation,
            "verified aid ends event and may permit a later distinct invitation");
        f.InvitationOffered=true;
        Check(EncounterFlagProjection.Project(f).CanAcceptPartyInvitation,
            "explicit later offer can be accepted with an open field position");
        f.ActiveFieldCount=2;
        Check(!EncounterFlagProjection.Project(f).CanAcceptPartyInvitation,
            "two-person field limit blocks an immediate third follower");
        f.ActiveFieldCount=1;f.Bonded=true;f.ActiveParty=true;
        var joined=EncounterFlagProjection.Project(f);
        Check(joined.Valid && !joined.CanAcceptPartyInvitation &&
            (joined.DurableFlags&EncounterFlag.ActiveParty)!=0,
            "accepted bond and assignment are durable separate facts");
        f.ZoneLoaded=false;f.ActorExists=false;
        var away=EncounterFlagProjection.Project(f);
        Check(away.DurableFlags==joined.DurableFlags && !away.SceneActorAvailable,
            "party bond survives unload while scene actor availability changes");

        var bad=Facts();bad.PersonAgreed=true;
        Check(!EncounterFlagProjection.Project(bad).Valid,"agreement cannot predate accepted aid");
        bad=Facts();bad.ActiveParty=true;
        Check(!EncounterFlagProjection.Project(bad).Valid,"party assignment cannot predate bond");
        bad=Facts();bad.EventResolved=true;
        Check(!EncounterFlagProjection.Project(bad).Valid,"event result cannot predate claim");
        bad=Facts();bad.ActiveFieldCount=3;
        Check(!EncounterFlagProjection.Project(bad).Valid,"three active companions violate balance cap");
        bad=Facts();bad.ServerAuthority=false;
        Check(!EncounterFlagProjection.Project(bad).Valid,"client cannot mint quest flags");
        Console.WriteLine("EncounterFlagProjectionTests PASS: "+checks+" checks");
    }
}

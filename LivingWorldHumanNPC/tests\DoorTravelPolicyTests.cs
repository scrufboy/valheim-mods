using System;
using LivingWorld.HumanNpc;
static class DoorTravelPolicyTests
{
    static int checks;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    static void Is(DoorTravelAction expected, DoorTravelInput x, string label) { Check(DoorTravelPolicy.Evaluate(x) == expected, label); }
    static DoorTravelInput Safe(DoorTravelPhase phase)
    {
        return new DoorTravelInput { Phase=phase, Authorized=true, DoorValid=true, AccessAllowed=true,
            InUseRange=true, CanInteract=true, OpenedHere=true, CrossingClear=true,
            CloseDelayElapsed=true, WithinCloseRange=true };
    }
    static void Main()
    {
        Is(DoorTravelAction.Native, new DoorTravelInput(), "No intent does nothing");
        var x=Safe(DoorTravelPhase.Approach);
        x.InUseRange=false; Is(DoorTravelAction.Approach,x,"Approach before interaction");
        x.InUseRange=true; Is(DoorTravelAction.Open,x,"Open in reach");
        x.CanInteract=false; Is(DoorTravelAction.Wait,x,"Respect native animation lock");
        x.IsOpen=true; Is(DoorTravelAction.Cancel,x,"Other actor opening grants no close claim");
        x=Safe(DoorTravelPhase.Opening);
        Is(DoorTravelAction.Wait,x,"Never resend toggle while open pending");
        x.IsOpen=true; x.CanInteract=false; Is(DoorTravelAction.Wait,x,"Wait for open animation");
        x.CanInteract=true; Is(DoorTravelAction.Cross,x,"Cross acknowledged open door");
        x=Safe(DoorTravelPhase.Crossing); Is(DoorTravelAction.Cancel,x,"Other actor closed door: do not reopen");
        x.IsOpen=true; Is(DoorTravelAction.Cross,x,"Keep stable crossing destination");
        x.Crossed=true; Is(DoorTravelAction.Native,x,"Return to original native route after crossing");
        x=Safe(DoorTravelPhase.Closing); x.IsOpen=true;
        Is(DoorTravelAction.Close,x,"Close only owned open intent");
        x.CrossingClear=false; Is(DoorTravelAction.Native,x,"Do not close on another character or stop original travel");
        x.TimedOut=true; Is(DoorTravelAction.Native,x,"Closing waits beyond the approach timeout while the convoy occupies the doorway");
        x.CrossingClear=true; Is(DoorTravelAction.Close,x,"Closing completes after delayed convoy clearance instead of abandoning follow-through");
        x.TimedOut=false;
        x.CrossingClear=true; x.CloseDelayElapsed=false; Is(DoorTravelAction.Native,x,"Delay close");
        x.CloseDelayElapsed=true; x.OpenedHere=false; Is(DoorTravelAction.Cancel,x,"Never close pre-opened door");
        x.OpenedHere=true; x.WithinCloseRange=false; Is(DoorTravelAction.Cancel,x,"No remote closing");
        x=Safe(DoorTravelPhase.ClosePending); x.IsOpen=true; Is(DoorTravelAction.Native,x,"No repeated close toggle");
        x.IsOpen=false; Is(DoorTravelAction.Finish,x,"Completion requires closed state");
        foreach(DoorTravelPhase phase in Enum.GetValues(typeof(DoorTravelPhase)))
        {
            if(phase==DoorTravelPhase.None) continue;
            x=Safe(phase); x.Authorized=false; Is(DoorTravelAction.Cancel,x,"Combat/owner cancellation "+phase);
            x=Safe(phase); x.DoorValid=false; Is(DoorTravelAction.Cancel,x,"Unloaded door "+phase);
            x=Safe(phase); x.AccessAllowed=false; Is(DoorTravelAction.Cancel,x,"Ward permission changed "+phase);
            x=Safe(phase); x.Keyed=true; Is(DoorTravelAction.Cancel,x,"Never unlock keyed door "+phase);
            x=Safe(phase); x.CannotClose=true; Is(DoorTravelAction.Cancel,x,"No one-way dungeon gate "+phase);
            if(phase!=DoorTravelPhase.Closing){x=Safe(phase); x.TimedOut=true; Is(DoorTravelAction.Cancel,x,"Bounded intent "+phase);}
        }
        Check(DoorTravelPolicy.Access(true,true,true,false,false,true,true),"Ordinary ward-aware wood door is usable when permitted");
        Check(!DoorTravelPolicy.Access(true,true,true,false,false,true,false),"Unauthorized ward refused");
        Check(!DoorTravelPolicy.Access(false,true,true,false,false,false,true),"Cannot borrow another local player's rights");
        Check(!DoorTravelPolicy.Access(true,false,true,false,false,false,true),"Remote NPC cannot operate door");
        Check(!DoorTravelPolicy.Access(true,true,false,false,false,false,true),"No door ownership stealing");
        Check(!DoorTravelPolicy.Access(true,true,true,true,false,false,true),"Key requirement never bypassed");
        Check(DoorTravelPolicy.CrossesPlane(2,-2),"Route crosses plane");
        Check(DoorTravelPolicy.CrossesPlane(-2,2),"Both directions supported");
        Check(!DoorTravelPolicy.CrossesPlane(2,3),"Same side is not route intent");
        Check(!DoorTravelPolicy.CrossesPlane(0,-2),"Standing in doorway is not fresh approach");
        Check(!DoorTravelPolicy.CrossesPlane(float.NaN,2),"Invalid geometry rejected");
        Check(!DoorTravelPolicy.CrossesPlane(float.PositiveInfinity,-2),"Infinite geometry rejected");
        Check(DoorTravelPolicy.ContinueShortFollow(true,true,2f,false,true),"Wall invalidates short-radius arrival");
        Check(DoorTravelPolicy.ContinueShortFollow(true,true,2f,true,false),"Finish doorway even when leader already close");
        Check(!DoorTravelPolicy.ContinueShortFollow(true,true,2f,false,false),"Normal short following remains native");
        Check(!DoorTravelPolicy.ContinueShortFollow(false,true,2f,true,true),"Combat/authority veto preserved");
        Check(!DoorTravelPolicy.ContinueShortFollow(true,false,2f,true,true),"Unrelated movement not captured");
        Check(!DoorTravelPolicy.ContinueShortFollow(true,true,3f,true,true),"Long following remains native");
        Check(!DoorTravelPolicy.ContinueShortFollow(true,true,float.NaN,true,true),"Invalid distance refused");
        Check(!DoorTravelPolicy.ContinueShortFollow(true,true,-1f,true,true),"Negative distance refused");
        Check(DoorTravelPolicy.DirectBlocker(true,true,2f,true,false,false),"door directly blocking travel is eligible");
        Check(!DoorTravelPolicy.DirectBlocker(true,false,2f,true,false,false),"nearby side door is not inferred as route knowledge");
        Check(!DoorTravelPolicy.DirectBlocker(true,true,4f,true,false,false),"distant door is not omnisciently selected");
        Check(!DoorTravelPolicy.DirectBlocker(true,true,2f,false,false,false),"direct door still requires access");
        Check(DoorTravelPolicy.RouteCandidate(2,-2,8,0,true,false,false),"Nearby authorized opposite-side entrance");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,8,0,false,false,false),"Permission required");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,8,0,true,true,false),"Other actor's claim respected");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,8,0,true,false,true),"Failed entrance backoff respected");
        Check(!DoorTravelPolicy.RouteCandidate(2,2,8,0,true,false,false),"Same-side door not selected");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,13,0,true,false,false),"Search radius bounded");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,8,2,true,false,false),"Different floor refused");
        Check(!DoorTravelPolicy.RouteCandidate(2,-2,float.NaN,0,true,false,false),"Invalid entrance position refused");
        Check(DoorTravelPolicy.FullLeg(true,0.1f,3),"Full path reaches endpoint");
        Check(!DoorTravelPolicy.FullLeg(false,0.1f,3),"Incomplete native route refused");
        Check(!DoorTravelPolicy.FullLeg(true,2f,3),"Projected path cannot stop across wall");
        Check(!DoorTravelPolicy.FullLeg(true,0,0),"Empty route not accepted as proof");
        Check(!DoorTravelPolicy.FullLeg(true,0,129),"Probe length bounded");
        Check(!DoorTravelPolicy.FullLeg(true,float.NaN,3),"Invalid endpoint refused");
        Check(DoorTravelPolicy.HasCrossed(1,-1.5f,0,0.7f),"Fully crossed");
        Check(DoorTravelPolicy.HasCrossed(-1,1.5f,0,0.7f),"Reverse crossing");
        Check(!DoorTravelPolicy.HasCrossed(1,-0.2f,0,0.7f),"Partial crossing insufficient");
        Check(!DoorTravelPolicy.HasCrossed(1,2,0,0.7f),"Not crossed by backing away");
        Check(!DoorTravelPolicy.HasCrossed(1,-2,3,0.7f),"Walking around house is not doorway completion");
        Check(DoorTravelPolicy.OccupiesDoorway(0,0,0,0.5f,0.7f),"Person blocks close");
        Check(DoorTravelPolicy.OccupiesDoorway(0,1.5f,0,0.5f,0.7f),"Body radius included");
        Check(!DoorTravelPolicy.OccupiesDoorway(0,2.2f,0,0.5f,0.7f),"Far side clear");
        Check(!DoorTravelPolicy.OccupiesDoorway(4,0,0,0.5f,0.7f),"Unrelated nearby villager does not block close");
        Check(!DoorTravelPolicy.OccupiesDoorway(0,0,5,0.5f,0.7f),"Upstairs person does not block lower door");
        Check(DoorTravelPolicy.OccupiesDoorway(float.NaN,0,0,0.5f,0.7f),"Unknown clearance blocks close");
        Check(DoorTravelPolicy.OccupiesDoorway(0,4,0,-1,0.7f),"Invalid body radius blocks close");
        Check(DoorTravelPolicy.OccupiesDoorway(0,4,0,0.5f,-1),"Invalid door geometry blocks close");
        Console.WriteLine("DoorTravelPolicyTests PASS: "+checks+" checks; Unity door traversal NOT RUN.");
    }
}

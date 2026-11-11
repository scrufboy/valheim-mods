using System;
using LivingWorld.CompanionSystems.SpecialistCombat.PredictionAndThrows;

class PredictionAndThrowsTests
{
    static int checks;
    static void Check(bool x,string label){checks++;if(!x)throw new Exception("FAIL "+label);}
    static ThrowConfig Spear(){return new ThrowConfig{Kind=ThrowKind.Spear,ProjectileSpeed=20,Gravity=9.81,MaximumFlightSeconds=4,MinimumRange=2,MaximumRange=50,MaximumSenseAgeSeconds=1,MinimumConfidence=.35,DirectionChangePenalty=.7,SafetyMargin=.5,EscapeSpeed=4};}
    static ThrowFacts Facts(Vec3 velocity){return new ThrowFacts{ActorId="hunter-body",PersonId="person-hunter",EncounterId="encounter-1",GearRevision=3,TargetId="enemy",RequestId="r1",OwnerPeerId=7,LocalPeerId=7,ServerAuthoritative=true,ActorNetworkOwner=true,CombatOwnsAction=true,NativeAttackReady=true,TerrainArcClear=true,BuildingArcClear=true,TargetAuthorized=true,ImpactGroundReachable=true,EscapeRouteReachable=true,Origin=new Vec3(0,1,0),EncounterCenter=new Vec3(0,0,0),EncounterRadius=60,Target=new MotionSample{Position=new Vec3(20,1,0),Velocity=velocity,PreviousVelocity=velocity,Visible=true,AgeSeconds=.1}};}
    static ThrowState Ammo(int n){return new ThrowState{AvailableAmmo=n};}
    static void Main()
    {
        var stationary=PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),Facts(new Vec3(0,0,0)));
        Check(stationary.Authorized&&stationary.FlightSeconds>.9&&stationary.AimPoint.Y>stationary.PredictedImpact.Y,"stationary ballistic prediction");
        var lateral=PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),Facts(new Vec3(0,0,4)));
        Check(lateral.Authorized&&lateral.PredictedImpact.Z>4,"lateral lead");
        var approaching=PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),Facts(new Vec3(-4,0,0)));
        var departing=PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),Facts(new Vec3(4,0,0)));
        Check(approaching.FlightSeconds<stationary.FlightSeconds&&departing.FlightSeconds>stationary.FlightSeconds,"approach and departure flight times");
        var turn=Facts(new Vec3(0,0,4));turn.Target.PreviousVelocity=new Vec3(0,0,-4);
        Check(!PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),turn).Authorized,"direction change reduces confidence");
        var wall=Facts(new Vec3(0,0,0));wall.TerrainArcClear=false;
        Check(PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),wall).Reason=="ballistic_arc_obstructed","low wall blocks arc");
        var ally=Facts(new Vec3(0,0,0));ally.Allies.Add(new MovingBody{Position=new Vec3(20,1,-4),Velocity=new Vec3(0,0,4),Radius=.5});
        Check(!PredictionAndThrowsPolicy.Reserve(Ammo(2),Spear(),ally).Authorized,"ally crossing predicted lane");

        var bomb=Spear();bomb.Kind=ThrowKind.Area;bomb.ProjectileSpeed=10;bomb.FuseSeconds=3;bomb.BlastRadius=5;bomb.SafetyMargin=1;
        var close=Facts(new Vec3(0,0,0));close.Target.Position=new Vec3(4,1,0);close.RequestId="bomb";close.EscapeRouteReachable=false;
        Check(PredictionAndThrowsPolicy.Reserve(Ammo(1),bomb,close).Reason=="blast_escape_not_proven","blast escape required");
        close.EscapeRouteReachable=true;
        Check(PredictionAndThrowsPolicy.Reserve(Ammo(1),bomb,close).Authorized,"reachable blast escape accepted");

        var one=PredictionAndThrowsPolicy.Reserve(Ammo(1),Spear(),Facts(new Vec3(0,0,0)));
        var released=PredictionAndThrowsPolicy.ConfirmNativeRelease(one.State,"r1");
        Check(released.Authorized&&released.State.AvailableAmmo==0,"native confirmation depletes finite ammo");
        var replay=PredictionAndThrowsPolicy.ConfirmNativeRelease(released.State,"r1");
        Check(replay.Replayed&&replay.State.AvailableAmmo==0,"release confirmation idempotent");
        var reloaded=released.State.Copy();reloaded.AvailableAmmo=1;var nextFacts=Facts(new Vec3(0,0,0));nextFacts.RequestId="r2";
        Check(PredictionAndThrowsPolicy.Reserve(reloaded,Spear(),nextFacts).Authorized,"reload snapshot permits new request");
        Check(!PredictionAndThrowsPolicy.Reserve(Ammo(0),Spear(),Facts(new Vec3(0,0,0))).Authorized,"zero ammunition refuses");
        var remote=Facts(new Vec3(0,0,0));remote.LocalPeerId=8;
        Check(PredictionAndThrowsPolicy.Reserve(Ammo(1),Spear(),remote).Reason=="not_authoritative_owner","remote peer cannot commit");
        var duplicate=PredictionAndThrowsPolicy.Reserve(one.State,Spear(),Facts(new Vec3(0,0,0)));
        Check(duplicate.Replayed&&duplicate.Authorized&&duplicate.AimPoint.X==one.AimPoint.X&&duplicate.AimPoint.Y==one.AimPoint.Y,"reservation replay preserves exact committed aim");
        Console.WriteLine("PredictionAndThrowsTests PASS: "+checks+" checks");
    }
}

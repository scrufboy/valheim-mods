using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.SpecialistCombat.MeleeRoles;

static class MeleeRolePolicyTests
{
    static int checks;
    static void Check(bool value,string name){if(!value)throw new Exception("FAIL: "+name);checks++;}
    static NativeAttackCandidate Attack(AttackShape shape,string id){return new NativeAttackCandidate{AttackId=id,RequiredWeaponId="verified-item",AnimationId="runtime-proven",Shape=shape,LiveVerified=true,IsEquippedWeapon=true,IsCurrentAnimationSet=true,WindupSeconds=.3,StepDistance=2,MinimumContact=.7,MaximumContact=3,ContactRadius=.45};}
    static SpaceEvidence Safe(){return new SpaceEvidence{EndpointReachable=true,StepPathClear=true,EndpointInsideTacticalBounds=true};}
    static KnifeLungeContext Knife(){return new KnifeLungeContext{IsAuthoritativeOwner=true,CombatOwnsActor=true,TargetAuthorized=true,ActorPosition=new PlanarPoint(0,0),TargetPrevious=new MotionSample{Position=new PlanarPoint(1.5,0),AgeSeconds=.2},TargetNow=new MotionSample{Position=new PlanarPoint(1.6,0),AgeSeconds=0},MaximumMotionSampleAgeSeconds=.25,TargetRadius=.3,NativeAttack=Attack(AttackShape.Thrust,"knife-lunge"),Space=Safe()};}
    static HeavyPressureContext Heavy(){return new HeavyPressureContext{IsAuthoritativeOwner=true,CombatOwnsActor=true,TargetAuthorized=true,TargetDistance=2,LineNeedsHolding=true,Space=Safe(),NativeAttacks=new[]{Attack(AttackShape.Thrust,"atgeir-thrust"),Attack(AttackShape.Sweep,"atgeir-sweep"),Attack(AttackShape.Spin,"atgeir-spin")}};}
    public static int Main(){
        var l=MeleeRolePolicy.EvaluateKnifeLunge(Knife());
        Check(l.Request==MeleeRequest.NativeKnifeLunge && l.PredictedContact.X>1.6,"recent target motion predicts native contact time");
        Check(l.ReachableEndpoint.X<=2 && l.CommitsTargetLock,"endpoint is bounded by native step and target locks at commit");
        Check(!l.AdapterMayMoveAfterCommit&&!l.AdapterMayRetargetAfterCommit,"adapter cannot teleport or retarget postcommit");
        var stale=Knife();stale.TargetPrevious.AgeSeconds=.5;Check(MeleeRolePolicy.EvaluateKnifeLunge(stale).Refusal==Refusal.MissingRecentMotion,"stale motion fails closed");
        var close=Knife();close.TargetNow.Position=new PlanarPoint(.1,0);close.TargetPrevious.Position=new PlanarPoint(.1,0);Check(MeleeRolePolicy.EvaluateKnifeLunge(close).Refusal==Refusal.TooClose,"already-close target cancels before commit");
        var far=Knife();far.TargetNow.Position=new PlanarPoint(8,0);far.TargetPrevious.Position=new PlanarPoint(8,0);Check(MeleeRolePolicy.EvaluateKnifeLunge(far).Refusal==Refusal.BeyondContact,"impossible contact cancels");
        var blocked=Knife();blocked.Space.StepPathClear=false;Check(MeleeRolePolicy.EvaluateKnifeLunge(blocked).Refusal==Refusal.PathObstructed,"obstacle cancels");
        var ally=Knife();ally.Space.AllyInStepOrContactVolume=true;Check(MeleeRolePolicy.EvaluateKnifeLunge(ally).Refusal==Refusal.AllyOrNeutralUnsafe,"ally in lunge volume cancels");
        var neutral=Knife();neutral.Space.ProtectedNeutralInStepOrContactVolume=true;Check(MeleeRolePolicy.EvaluateKnifeLunge(neutral).Refusal==Refusal.AllyOrNeutralUnsafe,"neutral in lunge volume cancels");
        var ground=Knife();ground.Space.EndpointReachable=false;Check(MeleeRolePolicy.EvaluateKnifeLunge(ground).Refusal==Refusal.EndpointUnreachable,"unreachable endpoint cancels");
        var guessed=Knife();guessed.NativeAttack.LiveVerified=false;Check(MeleeRolePolicy.EvaluateKnifeLunge(guessed).Refusal==Refusal.NativeAttackUnverified,"unverified animation cannot become an attack");
        var remote=Knife();remote.IsAuthoritativeOwner=false;Check(MeleeRolePolicy.EvaluateKnifeLunge(remote).Refusal==Refusal.NotAuthoritative,"remote peer cannot predict commitment");

        var h=Heavy();Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Request==MeleeRequest.NativeThrust,"hold line prefers safe in-range thrust");
        h.TargetStaggered=true;Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Request==MeleeRequest.NativeSpin,"stagger permits safe spin pressure");
        h.Space.UnsafeArcs.Add(AttackShape.Spin);Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Request==MeleeRequest.NativeSweep,"unsafe spin falls back to safe sweep");
        h.Space.UnsafeArcs.Add(AttackShape.Sweep);h.Space.UnsafeArcs.Add(AttackShape.Thrust);var hold=MeleeRolePolicy.EvaluateHeavyPressure(h);Check(hold.Request==MeleeRequest.HoldLine&&hold.Refusal==Refusal.NoSafeNativeAttack,"all unsafe arcs yield a capability request, not an attack");
        h=Heavy();h.Space.ProtectedNeutralInStepOrContactVolume=true;Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Refusal==Refusal.AllyOrNeutralUnsafe,"heavy pressure fails closed on protected neutral");
        h=Heavy();h.NativeAttacks[0].IsEquippedWeapon=false;h.NativeAttacks[1].IsEquippedWeapon=false;h.NativeAttacks[2].IsEquippedWeapon=false;Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Refusal==Refusal.WeaponOrAnimationMismatch,"actual weapon and animation set are mandatory");
        h=Heavy();h.RecoveryActive=true;Check(MeleeRolePolicy.EvaluateHeavyPressure(h).Refusal==Refusal.RecoveryActive,"native recovery suppresses new request");
        Check(MeleeRoleProfiles.BlackForestKnifeSkirmisher.UsesCommittedLunge&&MeleeRoleProfiles.BlackForestKnifeSkirmisher.Biome=="Black Forest","Black Forest skirmisher role is data");
        Check(MeleeRoleProfiles.PlainsHeavyReaver.UsesHeavyPressure&&MeleeRoleProfiles.PlainsHeavyReaver.PresentationProofState.Contains("live proof"),"Plains heavy does not assert unverified presentation");
        Console.WriteLine("MeleeRolePolicyTests PASS: "+checks+" checks. Unity/Valheim/network integration NOT RUN.");return 0;
    }
}

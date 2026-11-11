using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Shaman;

class ShamanTests : IShamanDecisionSource
{
    int checks;
    ShamanState state;
    ShamanFacts facts;
    ShamanDecision decision;
    readonly ShamanSettings settings=new ShamanSettings(3,30,10,20); // fixture values, not shipped balance
    public ShamanDecision Observe(CombatFrame frame) {return decision;}
    void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    void Reset() {
        state=new ShamanState{Mode=ShamanMode.Support};
        facts=new ShamanFacts{Sequence=1,Time=10,ActorIdentity="shaman",EncounterIdentity="raid-1",OwnerReady=true,Alive=true,
            EncounterActive=true,SafeFromThreat=true,EscapeReachable=true,BeastClaimAvailable=true,
            LinkClear=true,BeastDistance=12};
    }
    ShamanDecision Tick(){return decision=ShamanPolicy.Tick(state,settings,facts);}
    void Bind() {
        facts.BeastAlive=true;facts.BeastIdentity="beast";
        Check(Tick().Action==ShamanAction.Rebind,"request actual binding first");
        facts.BindingCompleted=true;
        facts.CompletedBindingRevision=state.BindingRevision;
        Check(Tick().Mode==ShamanMode.Controlling,"native binding receipt establishes link");
        facts.BindingCompleted=false;
    }
    void Run() {
        Reset();Bind();Check(decision.OrangeControlEyes&&decision.StaffTether,"orange eyes and tether only while linked");
        Check(!decision.BeastHostileToAll,"controlled beast not berserk");
        Check(ShamanPolicy.Damaged(state,settings,true,10,1),"accepted damage releases immediately");
        var d=Tick();Check(d.Mode==ShamanMode.Retreating&&d.Action==ShamanAction.Retreat,"retreat after damage");
        Check(d.BeastHostileToAll&&!d.StaffTether&&!d.OrangeControlEyes,"uncontrolled beast berserk without control effects");
        facts.Time=13;facts.SafeFromThreat=false;Check(Tick().Action==ShamanAction.Retreat,"delay alone cannot restore control");
        facts.SafeFromThreat=true;Check(Tick().Action==ShamanAction.Rebind,"safe recovery rebinds");
        facts.BindingCompleted=true;facts.CompletedBindingRevision=state.BindingRevision;
        Check(Tick().Mode==ShamanMode.Controlling,"reacquire after actual completed cast");
        Check(!Tick().ReleaseBeast,"steady control does not repeat release");
        Check(!ShamanPolicy.Damaged(state,settings,true,13,0)&&state.LinkActive,"blocked hit leaves control intact");
        Check(!ShamanPolicy.Damaged(state,settings,false,13,5)&&state.LinkActive,"nonowner cannot break control");
        Check(!ShamanPolicy.Damaged(state,settings,true,13,float.NaN)&&state.LinkActive,"invalid damage ignored");

        Reset();Bind();long oldReceipt=state.BindingRevision;
        ShamanPolicy.Damaged(state,settings,true,10,1);facts.Time=13;Tick();
        facts.BindingCompleted=true;facts.CompletedBindingRevision=oldReceipt;
        Check(Tick().Mode==ShamanMode.Rebinding&&!state.LinkActive,"interrupted cast receipt cannot restore a new link");

        Reset();Bind();facts.BeastIdentity=null;facts.BeastAlive=false;Tick();
        Check(state.BeastIdentity==null&&!state.LinkActive,"confirmed beast removal clears stale identity without a matching snapshot ID");
        facts.Time=13;facts.BeastIdentity="replacement";facts.BeastAlive=true;
        Check(Tick().Action==ShamanAction.Rebind&&state.BeastIdentity=="replacement","replacement beast can bind after confirmed old removal");

        Reset();Bind();ShamanPolicy.Damaged(state,settings,true,10,1);
        ShamanPolicy.Damaged(state,settings,true,12,1);facts.Time=13;
        Check(Tick().Action==ShamanAction.Retreat,"repeat injury extends recovery");
        facts.Time=15;facts.BeastDistance=50;Check(Tick().Action!=ShamanAction.Rebind,"no out-of-range link");
        facts.BeastDistance=12;facts.LinkClear=false;Check(Tick().Action!=ShamanAction.Rebind,"no obstructed link");
        facts.LinkClear=true;facts.BeastClaimAvailable=false;Check(Tick().Action!=ShamanAction.Rebind,"do not steal another controller's beast");
        Check(!decision.BeastHostileToAll,"old controller cannot override another active claim");

        Reset();Bind();facts.Alive=false;d=Tick();
        Check(d.Mode==ShamanMode.Dead&&d.ReleaseBeast&&d.BeastHostileToAll,"shaman death releases beast");
        Check(!Tick().ReleaseBeast,"death release exactly once");
        facts.Alive=true;Check(Tick().Mode==ShamanMode.Dead,"no accidental resurrection from a stale alive sample");
        Reset();Bind();facts.OwnerReady=false;d=Tick();
        Check(state.LinkActive&&d.Action==ShamanAction.None,"ownership absence cannot mutate control");

        Reset();facts.ShieldReady=true;facts.SelfShieldNeeded=true;
        Check(Tick().Action==ShamanAction.ShieldSelf,"protect self with no beast present");
        facts.SelfShieldNeeded=false;facts.AllyShieldNeeded=true;facts.ShieldAllyEligible=true;facts.ShieldTargetIdentity="neckman";
        Check(Tick().Action==ShamanAction.ShieldAlly,"protect authored allies");
        facts.ShieldTargetIsBehemoth=true;Check(Tick().Action!=ShamanAction.ShieldAlly,"never shield any behemoth");
        facts.ShieldTargetIsBehemoth=false;facts.ShieldTargetIdentity="beast";state.BeastIdentity="beast";
        facts.BeastIdentity="beast";facts.BeastAlive=true;facts.LinkClear=false;
        Check(Tick().Action!=ShamanAction.ShieldAlly,"exclude bound beast even with wrong ally flag");
        Check(ShamanPolicy.ShieldEligible(true,false,true,false),"self eligible");
        Check(!ShamanPolicy.ShieldEligible(false,true,true,true),"beast excluded even when allied");
        Check(!ShamanPolicy.ShieldEligible(false,false,true,false),"enemy shield refused");

        Reset();facts.MinionReady=true;facts.SpawnPointValid=true;facts.MinionPrefabAvailable=true;
        Check(Tick().Action==ShamanAction.SummonMinion,"replenish available minions");
        facts.LivingMinions=9;facts.ReservedMinions=1;facts.EncounterLivingMinions=9;facts.EncounterReservedMinions=1;
        Check(Tick().Action!=ShamanAction.SummonMinion,"reservations count toward individual cap");
        facts.LivingMinions=0;facts.ReservedMinions=0;facts.EncounterLivingMinions=20;facts.EncounterReservedMinions=0;
        Check(Tick().Action!=ShamanAction.SummonMinion,"shared encounter cap");
        facts.EncounterLivingMinions=0;facts.SpawnPointValid=false;Check(Tick().Action!=ShamanAction.SummonMinion,"no invented emergence location");
        facts.SpawnPointValid=true;facts.MinionPrefabAvailable=false;Check(Tick().Action!=ShamanAction.SummonMinion,"missing purchased creature does not become a substitute");

        Reset();facts.BeastSummonReady=true;facts.BeastSpawnPointValid=true;facts.BeastPrefabAvailable=true;
        Check(Tick().Action==ShamanAction.SummonBeast,"summon beast only with valid native inputs");
        ShamanPolicy.CommitBeastSummon(state,false,"raid-1",true);
        Check(Tick().Action==ShamanAction.SummonBeast,"nonowner receipt cannot consume encounter summon");
        ShamanPolicy.CommitBeastSummon(state,true,"raid-1",true);
        Check(Tick().Action!=ShamanAction.SummonBeast,"accepted summon is latched for the encounter even after removal");
        facts.EncounterIdentity="raid-2";
        Check(Tick().Action==ShamanAction.SummonBeast,"new real encounter permits a new beast");
        facts.BeastSummonReserved=true;Check(Tick().Action!=ShamanAction.SummonBeast,"no repeated pending beast summon");
        facts.BeastSummonReady=false;facts.ThreatVisible=true;facts.ClearShot=true;facts.BlastReady=true;facts.AuthorizedTargetIdentity="player";
        Check(Tick().Action==ShamanAction.RangedBlast,"failed/unavailable summon leaves ranged support available");
        facts.ClearShot=false;Check(Tick().Action==ShamanAction.None,"blocked spell has no melee substitute");

        facts.GroundLightningReady=true;facts.GroundOriginValid=true;facts.GroundAttackPathClear=true;
        Check(Tick().Action==ShamanAction.GroundLightning,"verified ground route can select additional electrical attack");
        facts.GroundOriginValid=false;
        Check(Tick().Action==ShamanAction.None,"ground attack refuses invented ground origin");
        facts.GroundOriginValid=true;facts.GroundAttackPathClear=false;facts.ClearShot=true;
        Check(Tick().Action==ShamanAction.RangedBlast,"unavailable ground route preserves existing ranged spell");
        facts.GroundAttackPathClear=true;facts.ShieldReady=true;facts.SelfShieldNeeded=true;
        Check(Tick().Action==ShamanAction.ShieldSelf,"new ground attack does not displace required self protection");
        facts.ShieldReady=false;facts.SelfShieldNeeded=false;

        var module=new ShamanProposalModule(this);
        var frame=new CombatFrame(1,10,"shaman","","player",true,true,1,1,null);
        facts.ClearShot=true;Tick();
        var result=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).OwnerId==module.Id,"existing arbitrator receives spell proposal");
        Check(result.LeaseFor(DecisionDomain.Defense).IsNativeFallback,"no competing defense writer");
        Check(result.LeaseFor(DecisionDomain.Facing).OwnerId==module.Id,"ground spell owns facing through same arbitrator");
        var deadFrame=new CombatFrame(1,10,"shaman","","player",true,true,0,1,null);
        result=new CombatArbitrator().Evaluate(deadFrame,new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"dead frame cannot acquire a spell lease");
        var idleFrame=new CombatFrame(1,10,"shaman","","player",true,false,1,1,null);
        result=new CombatArbitrator().Evaluate(idleFrame,new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"uncommitted frame cannot acquire a combat lease");
        decision.Sequence=0;
        result=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"stale decision refused");
        decision.Sequence=1;decision.ActorIdentity="another-shaman";
        result=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"cross actor decision refused");
        Console.WriteLine("Shaman policy: "+checks+" checks passed; native actor/effects/networking unverified.");
    }
    static void Main(){new ShamanTests().Run();}
}

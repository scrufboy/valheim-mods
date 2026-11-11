using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Shaman;

class ReviewReproductions : IShamanDecisionSource
{
    ShamanDecision decision;
    public ShamanDecision Observe(CombatFrame frame) { return decision; }
    static ShamanFacts Facts() {
        return new ShamanFacts { Sequence=1,Time=10,ActorIdentity="shaman",OwnerReady=true,Alive=true,
            EncounterActive=true,SafeFromThreat=true,EscapeReachable=true,BeastClaimAvailable=true,
            LinkClear=true,BeastDistance=12 };
    }
    static void Assert(bool condition,string message) { if(!condition)throw new Exception(message); Console.WriteLine(message); }
    void Run() {
        var settings=new ShamanSettings(3,30,10,20);
        var state=new ShamanState();var f=Facts();
        f.BeastIdentity="A";f.BeastAlive=true;
        ShamanPolicy.Tick(state,settings,f);
        f.BindingCompleted=true;f.CompletedBindingRevision=state.BindingRevision;
        ShamanPolicy.Tick(state,settings,f);
        Assert(state.LinkActive,"setup: A linked");
        f.BindingCompleted=false;f.BeastIdentity=null;f.BeastAlive=false;
        var d=ShamanPolicy.Tick(state,settings,f);
        Assert(d.ReleaseBeast && state.BeastIdentity=="A","REPRO: missing-beast snapshot retains stale identity A");
        f.Time=14;f.BeastIdentity="B";f.BeastAlive=true;
        d=ShamanPolicy.Tick(state,settings,f);
        Assert(d.Action!=ShamanAction.Rebind && state.BeastIdentity=="A","REPRO: replacement beast B can never bind after recovery");

        state=new ShamanState();f=Facts();
        f.BeastSummonReady=true;f.BeastSpawnPointValid=true;f.BeastPrefabAvailable=true;
        Assert(ShamanPolicy.Tick(state,settings,f).Action==ShamanAction.SummonBeast,"setup: first beast summon");
        f.BeastAlive=true;f.BeastIdentity="A";
        ShamanPolicy.Tick(state,settings,f);
        f.BindingCompleted=true;f.CompletedBindingRevision=state.BindingRevision;
        ShamanPolicy.Tick(state,settings,f);
        f.BeastAlive=false;f.BindingCompleted=false;
        d=ShamanPolicy.Tick(state,settings,f);
        f.Time=14;d=ShamanPolicy.Tick(state,settings,f);
        Assert(d.Action==ShamanAction.SummonBeast,"REPRO: same shaman can summon another beast after first dies unless external readiness latches it");

        decision=new ShamanDecision { Sequence=1,ActorIdentity="shaman",Mode=ShamanMode.Support,
            Action=ShamanAction.RangedBlast,ActionTarget="player" };
        var module=new ShamanProposalModule(this);
        var frame=new CombatFrame(1,10,"shaman","","player",true,false,0,1,null);
        var result=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{module});
        Assert(!result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"REPRO: dead and combat-uncommitted frame receives ranged Attack lease");
        decision.Action=ShamanAction.Retreat;decision.ActionTarget=null;
        frame=new CombatFrame(1,10,"shaman","","player",true,true,1,1,null);
        result=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{module});
        Assert(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"CONTRACT GAP: retreat returns Attack to native AI; no-melee requires native loadout/executor enforcement");
        Console.WriteLine("Independent source reproductions only; native gameplay/network integration not tested.");
    }
    static void Main() { new ReviewReproductions().Run(); }
}

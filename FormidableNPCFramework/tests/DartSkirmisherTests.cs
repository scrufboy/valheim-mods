using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Swarm;
class DartTests : IDartFactSource
{
    DartFacts facts;
    int checks;
    public DartFacts Observe(CombatFrame frame) { return facts; }
    CombatFrame Frame(float distance, bool visible, bool owner)
    {
        return new CombatFrame(1, 10, "actor", "", "target", owner, true, 1, 1,
            new[] { new ThreatObservation("target", distance, visible, false) });
    }
    void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
    CombatProposal Pick(DartSkirmisherModule module, CombatFrame frame)
    {
        CombatProposal result = null;
        foreach (var proposal in module.Evaluate(frame)) { Check(result == null, "single proposal"); result = proposal; }
        return result;
    }
    void Reset()
    {
        facts = new DartFacts { Sequence=1, ActorIdentity="actor", TargetIdentity="target",
            WeaponReady=true, ClearShot=true, FirePermit=true, RecoveryComplete=true, PackCommitted=true };
    }
    void Run()
    {
        var module = new DartSkirmisherModule(this, 3, 15);
        Reset(); Check(Pick(module, Frame(8,true,true)).ActionId=="lwc.dart.fire","observed clear authorized shot");
        facts.ClearShot=false; Check(Pick(module,Frame(8,true,true))==null,"blocked lane");
        Reset(); facts.FirePermit=false; Check(Pick(module,Frame(8,true,true))==null,"bounded pack firing permit");
        Reset(); facts.RecoveryComplete=false; Check(Pick(module,Frame(8,true,true))==null,"recovery blocks firing");
        Reset(); Check(Pick(module,Frame(8,false,true))==null,"no hidden target shot");
        Check(Pick(module,Frame(8,true,false))==null,"nonowner yields");
        Reset(); facts.Sequence=0; Check(Pick(module,Frame(8,true,true))==null,"stale facts");
        Reset(); facts.ActorIdentity="other"; Check(Pick(module,Frame(8,true,true))==null,"cross actor facts");
        Reset(); Check(Pick(module,Frame(20,true,true))==null,"outside real weapon range");
        Reset(); Check(Pick(module,Frame(1,true,true))==null,"no invented escape route");
        facts.EscapeReachable=true; Check(Pick(module,Frame(1,true,true)).ActionId=="lwc.dart.evade","reachable close escape");
        Reset(); facts.PackCommitted=false; facts.RegroupReachable=true;
        Check(Pick(module,Frame(8,true,true)).ActionId=="lwc.dart.regroup","regroup before pack commitment");
        Reset(); Check(Pick(module,Frame(float.NaN,true,true))==null,"nonfinite distance");
        var arbitrator = new CombatArbitrator();
        facts.ClearShot=false;
        var result=arbitrator.Evaluate(Frame(8,true,true),new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).IsNativeFallback,"blocked shot leaves existing owner");
        Reset(); result=arbitrator.Evaluate(Frame(8,true,true),new List<IFormidableCombatModule>{module});
        Check(result.LeaseFor(DecisionDomain.Attack).OwnerId==module.Id,"shared arbitrator owns attack");
        Check(result.LeaseFor(DecisionDomain.Defense).IsNativeFallback,"does not steal defense");
        Console.WriteLine("Dart policy: "+checks+" checks passed; no Unity executor/gameplay claim.");
    }
    static void Main() { new DartTests().Run(); }
}

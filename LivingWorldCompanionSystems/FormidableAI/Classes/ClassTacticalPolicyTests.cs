using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;
namespace LivingWorld.Companions.FormidableAI.Classes
{
 internal static class ClassTacticalPolicyTests
 {
  sealed class Facts:IClassCombatFactSource{public ClassCombatFacts Value;public ClassCombatFacts Observe(CombatFrame f){return Value;}}
  static int passed;
  public static int Main(){Run("defense wins mixed conflict",DefenseWins);Run("lane creates movement proposal",Lane);Run("personal threat cancels support",CancelSupport);Run("heal owns support and facing",Heal);Run("throw owns three domains",Throw);Run("recovery retains domains",Recovery);Run("target loss cancels",TargetLoss);Run("investigation ends at combat",Investigation);Run("adapter enters kernel",Adapter);Run("unauthorized yields",Unauthorized);Console.WriteLine("PASS: "+passed+" FormidableAI class proposal tests");return 0;}
  static ClassCombatFacts Base(){return new ClassCombatFacts{OwnerAuthorized=true,ActorReady=true,CommittedCombat=true,TargetAuthorized=true,LineOfFireClear=true,SupportSafe=true,AbilityReady=true,ThrowReady=true,ThrowRecoverySafe=true,TargetInReach=true,Lifecycle=ActionLifecyclePhase.Idle};}
  static ClassDecision D(FormidableClass c,ClassCombatFacts f){return ClassTacticalPolicy.Decide(ClassProfile.Create(c),f,"enemy-1");}
  static void DefenseWins(){var p=new ClassProfile(FormidableClass.Warden,ClassCapability.ShieldDefense|ClassCapability.ProtectAlly|ClassCapability.HealSupport);var f=Base();f.IncomingStrike=true;f.ShieldReady=true;f.AllyThreatened=true;f.AllyCritical=true;var x=ClassTacticalPolicy.Decide(p,f,"enemy-1");A(x.Kind==ClassActionKind.Defend&&x.Domains==DecisionDomain.Defense,"mixed conflict lost defense");}
  static void Lane(){var f=Base();f.AllyInLineOfFire=true;var x=D(FormidableClass.Huntress,f);A(x.Kind==ClassActionKind.Reposition&&(x.Domains&DecisionDomain.Movement)!=0&&x.Reason=="ally-in-firing-lane","unsafe shot");}
  static void CancelSupport(){var f=Base();f.PersonalThreat=true;f.Lifecycle=ActionLifecyclePhase.Started;f.CurrentActionId="support:heal-critical";f.CurrentDomains=DecisionDomain.Support|DecisionDomain.Facing;f.CurrentTargetIdentity="ally-1";var x=D(FormidableClass.Mystic,f);A(!x.ProposesOwnership&&x.Reason=="cancel-current-support:personal-threat","support not cancelled");}
  static void Heal(){var f=Base();f.AllyCritical=true;var x=D(FormidableClass.Mystic,f);A(x.Kind==ClassActionKind.HealAlly&&x.Domains==(DecisionDomain.Facing|DecisionDomain.Support),"heal domains wrong");}
  static void Throw(){var x=D(FormidableClass.Skirmisher,Base());A(x.Domains==(DecisionDomain.Equipment|DecisionDomain.Facing|DecisionDomain.Attack),"throw not atomic");}
  static void Recovery(){var f=Base();f.Lifecycle=ActionLifecyclePhase.Confirmed;f.CurrentActionId="attack:bounded-throw";f.CurrentDomains=DecisionDomain.Equipment|DecisionDomain.Facing|DecisionDomain.Attack;f.CurrentTargetIdentity="enemy-1";f.ActionCommitObserved=true;var x=D(FormidableClass.Skirmisher,f);A(x.Kind==ClassActionKind.RecoverAction&&x.Domains==f.CurrentDomains&&x.Reason=="confirmed-action-recovery-not-complete","recovery released early");}
  static void TargetLoss(){var f=Base();f.TargetAuthorized=false;f.Lifecycle=ActionLifecyclePhase.Started;f.CurrentActionId="attack:ranged";f.CurrentDomains=DecisionDomain.Facing|DecisionDomain.Attack;var x=ClassTacticalPolicy.Decide(ClassProfile.Create(FormidableClass.Huntress),f,null);A(!x.ProposesOwnership&&x.Reason=="cancel-current-action:target-lost-or-unauthorized","target loss retained action");}
  static void Investigation(){var f=new ClassCombatFacts{OwnerAuthorized=true,ActorReady=true,EvidenceToInvestigate=true,Lifecycle=ActionLifecyclePhase.Idle};A(D(FormidableClass.Pathfinder,f).Kind==ClassActionKind.Investigate,"evidence ignored");f.CommittedCombat=true;f.TargetAuthorized=true;f.LineOfFireClear=true;A(D(FormidableClass.Pathfinder,f).Kind==ClassActionKind.RangedAttack,"investigation survived combat");}
  static void Adapter(){var s=new Facts{Value=Base()};s.Value.AllyInLineOfFire=true;var m=new ClassProposalModule("class.huntress",ClassProfile.Create(FormidableClass.Huntress),s);var frame=new CombatFrame(1,10f,"actor","huntress","enemy-1",true,true,1f,1f,new[]{new ThreatObservation("enemy-1",8f,true,false)});var r=new CombatArbitrator().Evaluate(frame,new List<IFormidableCombatModule>{m});A(r.LeaseFor(DecisionDomain.Movement).OwnerId=="class.huntress"&&r.LeaseFor(DecisionDomain.Attack).IsNativeFallback&&m.LastDecision.Reason=="ally-in-firing-lane","adapter arbitration failed");}
  static void Unauthorized(){foreach(FormidableClass c in Enum.GetValues(typeof(FormidableClass)))A(!D(c,new ClassCombatFacts{ActorReady=true}).ProposesOwnership,"unauthorized acted "+c);}
  static void Run(string n,Action t){t();passed++;Console.WriteLine("ok - "+n);}static void A(bool c,string m){if(!c)throw new Exception(m);}
 }
}

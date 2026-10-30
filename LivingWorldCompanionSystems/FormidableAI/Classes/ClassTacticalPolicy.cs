using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;

namespace LivingWorld.Companions.FormidableAI.Classes
{
    public enum ClassActionKind { Yield, Investigate, CommunicateThreat, Defend, ProtectAlly, Reposition, HealAlly, CastSupport, RangedAttack, ReachAttack, ThrowWeapon, PressureTarget, ContinueAction, RecoverAction }
    public enum ActionLifecyclePhase { Idle, Started, Confirmed, Recovering, Cancelled }

    public sealed class ClassCombatFacts
    {
        public bool OwnerAuthorized, ActorReady, CommittedCombat, TargetAuthorized;
        public bool IncomingStrike, ShieldReady, PersonalThreat, AllyThreatened, AllyCritical, SupportSafe;
        public bool LineOfFireClear, AllyInLineOfFire, BelowPreferredRange, AbovePreferredRange, TargetInReach;
        public bool ThrowReady, ThrowRecoverySafe, EvidenceToInvestigate, ThreatAlreadyCommunicated, AbilityReady;
        public ActionLifecyclePhase Lifecycle;
        public string CurrentActionId, CurrentTargetIdentity;
        public DecisionDomain CurrentDomains;
        public bool ActionCommitObserved, RecoveryComplete;
    }

    public sealed class ClassDecision
    {
        public ClassActionKind Kind;
        public DecisionDomain Domains;
        public int Priority;
        public string ActionId, TargetIdentity, Reason;
        public bool ProposesOwnership;
        public static ClassDecision Yield(string reason) { return new ClassDecision { Kind=ClassActionKind.Yield, Domains=DecisionDomain.None, ActionId="yield", Reason=reason }; }
    }

    public interface IClassCombatFactSource { ClassCombatFacts Observe(CombatFrame frame); }

    public sealed class ClassProposalModule : IFormidableCombatModule
    {
        private readonly ClassProfile _profile;
        private readonly IClassCombatFactSource _facts;
        public string Id { get; private set; }
        public ClassDecision LastDecision { get; private set; }
        public ClassProposalModule(string id, ClassProfile profile, IClassCombatFactSource facts)
        {
            if(string.IsNullOrEmpty(id)) throw new ArgumentException("module identity is required","id");
            if(profile==null) throw new ArgumentNullException("profile"); if(facts==null) throw new ArgumentNullException("facts");
            Id=id; _profile=profile; _facts=facts;
        }
        public IEnumerable<CombatProposal> Evaluate(CombatFrame frame)
        {
            LastDecision=ClassTacticalPolicy.Decide(_profile,_facts.Observe(frame),frame.AuthorizedTargetIdentity);
            if(!LastDecision.ProposesOwnership) return new CombatProposal[0];
            return new[]{new CombatProposal(LastDecision.ActionId,LastDecision.Domains,LastDecision.Priority,LastDecision.TargetIdentity,frame.Time+.15f)};
        }
    }

    public static class ClassTacticalPolicy
    {
        public static ClassDecision Decide(ClassProfile profile, ClassCombatFacts facts, string target)
        {
            if(profile==null) throw new ArgumentNullException("profile"); if(facts==null) throw new ArgumentNullException("facts");
            if(!facts.OwnerAuthorized||!facts.ActorReady) return ClassDecision.Yield("owner-or-actor-not-ready");
            ClassDecision lifecycle=Lifecycle(facts,target); if(lifecycle!=null) return lifecycle;
            if(facts.CommittedCombat&&facts.IncomingStrike&&facts.ShieldReady&&profile.Has(ClassCapability.ShieldDefense))
                return Request(ClassActionKind.Defend,"defend:observed-strike",DecisionDomain.Defense,100,target,"observed-incoming-strike");
            if(!facts.CommittedCombat) return Precombat(profile,facts,target);
            if(!facts.TargetAuthorized||string.IsNullOrEmpty(target)) return ClassDecision.Yield("combat-target-not-authorized");
            switch(profile.Class){
                case FormidableClass.Warden:return Warden(profile,facts,target);
                case FormidableClass.Huntress:return Huntress(profile,facts,target);
                case FormidableClass.Mystic:return Mystic(profile,facts,target);
                case FormidableClass.Skirmisher:return Skirmisher(profile,facts,target);
                case FormidableClass.Commander:return Commander(profile,facts,target);
                case FormidableClass.Pathfinder:return Pathfinder(profile,facts,target);
                default:return ClassDecision.Yield("unknown-class");}
        }

        private static ClassDecision Lifecycle(ClassCombatFacts f,string target)
        {
            if(f.Lifecycle==ActionLifecyclePhase.Cancelled) return ClassDecision.Yield("action-cancelled-await-handback");
            if(f.Lifecycle==ActionLifecyclePhase.Idle) return null;
            if(f.CommittedCombat&&(!f.TargetAuthorized||string.IsNullOrEmpty(target))) return ClassDecision.Yield("cancel-current-action:target-lost-or-unauthorized");
            if((f.CurrentDomains&DecisionDomain.Support)!=0&&f.PersonalThreat) return ClassDecision.Yield("cancel-current-support:personal-threat");
            if(string.IsNullOrEmpty(f.CurrentActionId)||f.CurrentDomains==DecisionDomain.None) return ClassDecision.Yield("cancel-current-action:invalid-lifecycle-facts");
            if(f.Lifecycle==ActionLifecyclePhase.Started&&!f.ActionCommitObserved)
                return Request(ClassActionKind.ContinueAction,f.CurrentActionId,f.CurrentDomains,110,f.CurrentTargetIdentity,"await-action-commit-confirmation");
            if((f.Lifecycle==ActionLifecyclePhase.Confirmed||f.Lifecycle==ActionLifecyclePhase.Recovering)&&!f.RecoveryComplete)
                return Request(ClassActionKind.RecoverAction,"recover:"+f.CurrentActionId,f.CurrentDomains,105,f.CurrentTargetIdentity,"confirmed-action-recovery-not-complete");
            return null;
        }
        private static ClassDecision Precombat(ClassProfile p,ClassCombatFacts f,string t)
        {
            if(f.EvidenceToInvestigate&&p.Has(ClassCapability.Investigation)) return Request(ClassActionKind.Investigate,"investigate:evidence",DecisionDomain.Perception|DecisionDomain.Movement,30,t,"target-isolated-evidence");
            if(f.EvidenceToInvestigate&&!f.ThreatAlreadyCommunicated&&p.Has(ClassCapability.ThreatCommunication)) return Request(ClassActionKind.CommunicateThreat,"communicate:threat",DecisionDomain.Perception,25,t,"new-threat-evidence");
            return ClassDecision.Yield("no-precombat-class-action");
        }
        private static ClassDecision Warden(ClassProfile p,ClassCombatFacts f,string t)
        { return f.AllyThreatened&&p.Has(ClassCapability.ProtectAlly)?Request(ClassActionKind.ProtectAlly,"protect:ally",DecisionDomain.Movement|DecisionDomain.Facing,80,t,"ally-protection-window"):Request(ClassActionKind.PressureTarget,"pressure:defensive",DecisionDomain.Attack,40,t,"hold-defensive-pressure"); }
        private static ClassDecision Huntress(ClassProfile p,ClassCombatFacts f,string t)
        {
            if(f.BelowPreferredRange||!f.LineOfFireClear||f.AllyInLineOfFire) return Request(ClassActionKind.Reposition,"reposition:ranged-lane",DecisionDomain.Movement|DecisionDomain.Facing,75,t,f.AllyInLineOfFire?"ally-in-firing-lane":"unsafe-range-or-lane");
            if(f.AbovePreferredRange) return Request(ClassActionKind.Reposition,"reposition:close-range",DecisionDomain.Movement,65,t,"outside-effective-range");
            return p.Has(ClassCapability.RangedPressure)?Request(ClassActionKind.RangedAttack,"attack:ranged",DecisionDomain.Facing|DecisionDomain.Attack,55,t,"safe-ranged-window"):ClassDecision.Yield("no-safe-huntress-action");
        }
        private static ClassDecision Mystic(ClassProfile p,ClassCombatFacts f,string t)
        {
            if(f.PersonalThreat) return ClassDecision.Yield("personal-threat-preempts-support");
            if(f.AllyCritical&&f.SupportSafe&&f.AbilityReady&&p.Has(ClassCapability.HealSupport)) return Request(ClassActionKind.HealAlly,"support:heal-critical",DecisionDomain.Facing|DecisionDomain.Support,90,t,"critical-ally-safe-heal");
            if(f.SupportSafe&&f.AbilityReady&&p.Has(ClassCapability.CastSupport)) return Request(ClassActionKind.CastSupport,"support:cast",DecisionDomain.Facing|DecisionDomain.Support,60,t,"safe-support-window");
            return ClassDecision.Yield("support-not-safe-or-ready");
        }
        private static ClassDecision Skirmisher(ClassProfile p,ClassCombatFacts f,string t)
        {
            if(f.ThrowReady&&f.ThrowRecoverySafe&&!f.AllyInLineOfFire&&p.Has(ClassCapability.BoundedThrow)) return Request(ClassActionKind.ThrowWeapon,"attack:bounded-throw",DecisionDomain.Equipment|DecisionDomain.Facing|DecisionDomain.Attack,70,t,"bounded-throw-window");
            if(f.TargetInReach&&p.Has(ClassCapability.ReachControl)) return Request(ClassActionKind.ReachAttack,"attack:reach",DecisionDomain.Facing|DecisionDomain.Attack,50,t,"target-in-reach-band");
            return Request(ClassActionKind.Reposition,"reposition:reach-band",DecisionDomain.Movement|DecisionDomain.Facing,45,t,"recover-reach-band");
        }
        private static ClassDecision Commander(ClassProfile p,ClassCombatFacts f,string t)
        { return f.AllyThreatened&&p.Has(ClassCapability.ProtectAlly)?Request(ClassActionKind.ProtectAlly,"protect:command",DecisionDomain.Movement|DecisionDomain.Facing,85,t,"command-protection-priority"):Request(ClassActionKind.PressureTarget,"pressure:anchor",DecisionDomain.Movement|DecisionDomain.Facing|DecisionDomain.Attack,60,t,"anchor-threat-without-chasing"); }
        private static ClassDecision Pathfinder(ClassProfile p,ClassCombatFacts f,string t)
        { return f.BelowPreferredRange||!f.LineOfFireClear||f.AllyInLineOfFire?Request(ClassActionKind.Reposition,"reposition:scout-lane",DecisionDomain.Movement|DecisionDomain.Facing,65,t,"scout-combat-lane"):Request(ClassActionKind.RangedAttack,"attack:scout-ranged",DecisionDomain.Facing|DecisionDomain.Attack,50,t,"scout-yields-awareness-to-combat"); }
        private static ClassDecision Request(ClassActionKind k,string id,DecisionDomain d,int p,string t,string r)
        { return new ClassDecision{Kind=k,ActionId=id,Domains=d,Priority=p,TargetIdentity=t,Reason=r,ProposesOwnership=true}; }
    }
}

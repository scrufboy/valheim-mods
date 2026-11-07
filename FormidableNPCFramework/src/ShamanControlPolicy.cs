// Independent LWC state policy. Native owner supplies facts and commits results.
using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;

namespace LivingWorld.FormidableAI.Shaman
{
    public enum ShamanMode { Support, Controlling, Retreating, Rebinding, Dead }
    public enum ShamanAction { None, Retreat, Rebind, ShieldSelf, ShieldAlly, SummonMinion, SummonBeast, RangedBlast, GroundLightning }
    public sealed class ShamanState
    {
        public ShamanMode Mode;
        public string BeastIdentity;
        public string BeastSummonedEncounter;
        public bool LinkActive, BeastReleased;
        public float RebindNotBefore;
        public long BindingRevision;
    }
    public sealed class ShamanFacts
    {
        public long Sequence;
        public float Time, BeastDistance;
        public string ActorIdentity, BeastIdentity, AuthorizedTargetIdentity, ShieldTargetIdentity;
        public string EncounterIdentity;
        public bool OwnerReady, Alive, EncounterActive, ThreatVisible, ClearShot, BlastReady;
        public bool GroundLightningReady, GroundOriginValid, GroundAttackPathClear;
        public bool BeastAlive, PreviousBeastGone, BeastClaimAvailable, LinkClear, SafeFromThreat, EscapeReachable;
        public bool SelfShieldNeeded, AllyShieldNeeded, ShieldReady, ShieldAllyEligible, ShieldTargetIsBehemoth;
        public bool MinionReady, SpawnPointValid, MinionPrefabAvailable;
        public bool BeastSummonReady, BeastSpawnPointValid, BeastPrefabAvailable, BeastSummonReserved;
        public int LivingMinions, ReservedMinions, EncounterLivingMinions, EncounterReservedMinions;
        // Receipts: emitted only after native action acceptance, not animation intent.
        public bool BindingCompleted;
        public long CompletedBindingRevision;
    }
    public sealed class ShamanSettings
    {
        public readonly float RebindDelay, BindingRange;
        public readonly int MinionCap, EncounterMinionCap;
        public ShamanSettings(float rebindDelay, float bindingRange, int minionCap, int encounterMinionCap)
        {
            if(!Finite(rebindDelay)||!Finite(bindingRange)||rebindDelay<=0||bindingRange<=0||
               minionCap<1||minionCap>32||encounterMinionCap<minionCap||encounterMinionCap>128)
                throw new ArgumentException("Explicit bounded shaman settings required.");
            RebindDelay=rebindDelay; BindingRange=bindingRange; MinionCap=minionCap; EncounterMinionCap=encounterMinionCap;
        }
        internal static bool Finite(float x) { return !Single.IsNaN(x)&&!Single.IsInfinity(x); }
    }
    public sealed class ShamanDecision
    {
        public long Sequence;
        public string ActorIdentity;
        public long BindingRevision;
        public ShamanMode Mode;
        public ShamanAction Action;
        public string ActionTarget;
        public bool ReleaseBeast, OrangeControlEyes, StaffTether, BeastHostileToAll;
    }
    public static class ShamanPolicy
    {
        // Called only for the accepted, owner-reserved summon transaction.
        // Evaluating a proposal never consumes or repeats an encounter's beast.
        public static void CommitBeastSummon(ShamanState state,bool owner,string encounterIdentity,bool accepted)
        {
            if(state==null)throw new ArgumentNullException("state");
            if(owner&&accepted&&!String.IsNullOrEmpty(encounterIdentity))state.BeastSummonedEncounter=encounterIdentity;
        }
        // Explicit role flags, not native faction equality. Never shields the beast.
        public static bool ShieldEligible(bool self, bool authoredAlly, bool alive, bool behemoth)
        { return alive&&!behemoth&&(self||authoredAlly); }

        public static bool Damaged(ShamanState state, ShamanSettings settings, bool owner, float now, float acceptedDamage)
        {
            if(state==null||settings==null)throw new ArgumentNullException();
            if(!owner||state.Mode==ShamanMode.Dead||!ShamanSettings.Finite(now)||
               !ShamanSettings.Finite(acceptedDamage)||acceptedDamage<=0)return false;
            bool released=state.LinkActive;
            state.BeastReleased=state.BeastReleased||released;
            state.LinkActive=false; state.Mode=ShamanMode.Retreating;
            state.BindingRevision++;
            state.RebindNotBefore=Math.Max(state.RebindNotBefore,now+settings.RebindDelay);
            return released;
        }

        public static ShamanDecision Tick(ShamanState state, ShamanSettings settings, ShamanFacts f)
        {
            if(state==null||settings==null||f==null)throw new ArgumentNullException();
            if(!f.OwnerReady||!ShamanSettings.Finite(f.Time)||String.IsNullOrEmpty(f.ActorIdentity))
                return new ShamanDecision{Mode=state.Mode}; // no nonowner transition or command
            if(state.Mode==ShamanMode.Dead)
                return Describe(state,f,ShamanAction.None,null,false,f.BeastAlive&&state.BeastIdentity==f.BeastIdentity);
            bool release=false;
            bool sameBeast=!String.IsNullOrEmpty(state.BeastIdentity)&&state.BeastIdentity==f.BeastIdentity;
            bool beastPresent=sameBeast&&f.BeastAlive;
            if(!f.Alive) {
                release=state.LinkActive; state.LinkActive=false;
                state.BeastReleased=state.BeastReleased||release; state.Mode=ShamanMode.Dead;
                return Describe(state,f,ShamanAction.None,null,release,beastPresent);
            }
            bool validRange=ShamanSettings.Finite(f.BeastDistance)&&f.BeastDistance>=0&&f.BeastDistance<=settings.BindingRange;
            if(state.LinkActive&&(!beastPresent||!f.LinkClear||!f.BeastClaimAvailable||!validRange)) {
                release=true; state.LinkActive=false; state.BeastReleased=beastPresent;
                state.Mode=ShamanMode.Retreating;
                state.RebindNotBefore=Math.Max(state.RebindNotBefore,f.Time+settings.RebindDelay);
            }
            if((!f.BeastAlive||f.PreviousBeastGone)&&!String.IsNullOrEmpty(state.BeastIdentity)) {
                // Death/removal clears only this encounter's recorded beast.
                state.BeastIdentity=null; state.LinkActive=false; state.BeastReleased=false;
            }
            if(state.Mode==ShamanMode.Retreating) {
                if(!f.SafeFromThreat||f.Time<state.RebindNotBefore)
                    return Describe(state,f,f.EscapeReachable?ShamanAction.Retreat:ShamanAction.None,null,release,beastPresent);
                state.Mode=ShamanMode.Support;
            }
            if(!state.LinkActive&&f.BeastAlive&&!String.IsNullOrEmpty(f.BeastIdentity)&&
               (String.IsNullOrEmpty(state.BeastIdentity)||sameBeast)&&f.BeastClaimAvailable&&
               f.LinkClear&&validRange&&f.SafeFromThreat&&f.Time>=state.RebindNotBefore) {
                state.BeastIdentity=f.BeastIdentity;
                if(f.BindingCompleted&&state.Mode==ShamanMode.Rebinding&&f.CompletedBindingRevision==state.BindingRevision) {
                    state.LinkActive=true;state.BeastReleased=false;state.Mode=ShamanMode.Controlling;
                } else {
                    if(state.Mode!=ShamanMode.Rebinding)state.BindingRevision++;
                    state.Mode=ShamanMode.Rebinding;
                    return Describe(state,f,ShamanAction.Rebind,f.BeastIdentity,release,true);
                }
            } else if(!state.LinkActive) state.Mode=ShamanMode.Support;
            if(!f.SafeFromThreat&&f.EscapeReachable)
                return Describe(state,f,ShamanAction.Retreat,null,release,beastPresent);
            ShamanAction action=ShamanAction.None;string target=null;
            if(f.ShieldReady&&f.SelfShieldNeeded) {action=ShamanAction.ShieldSelf;target=f.ActorIdentity;}
            else if(f.ShieldReady&&f.AllyShieldNeeded&&f.ShieldAllyEligible&&!f.ShieldTargetIsBehemoth&&
                    !String.IsNullOrEmpty(f.ShieldTargetIdentity)&&f.ShieldTargetIdentity!=f.BeastIdentity&&
                    f.ShieldTargetIdentity!=state.BeastIdentity) {action=ShamanAction.ShieldAlly;target=f.ShieldTargetIdentity;}
            else if(f.EncounterActive&&CanSummonMinion(settings,f))action=ShamanAction.SummonMinion;
            else if(f.EncounterActive&&!f.BeastAlive&&!state.LinkActive&&String.IsNullOrEmpty(state.BeastIdentity)&&
                    !String.IsNullOrEmpty(f.EncounterIdentity)&&state.BeastSummonedEncounter!=f.EncounterIdentity&&
                    f.BeastSummonReady&&f.BeastSpawnPointValid&&f.BeastPrefabAvailable&&!f.BeastSummonReserved)
                action=ShamanAction.SummonBeast;
            else if(f.EncounterActive&&f.ThreatVisible&&f.GroundLightningReady&&f.GroundOriginValid&&f.GroundAttackPathClear&&
                    !String.IsNullOrEmpty(f.AuthorizedTargetIdentity)&&f.AuthorizedTargetIdentity!=f.ActorIdentity) {
                action=ShamanAction.GroundLightning;target=f.AuthorizedTargetIdentity;
            }
            else if(f.EncounterActive&&f.ThreatVisible&&f.ClearShot&&f.BlastReady&&
                    !String.IsNullOrEmpty(f.AuthorizedTargetIdentity)&&f.AuthorizedTargetIdentity!=f.ActorIdentity) {
                action=ShamanAction.RangedBlast;target=f.AuthorizedTargetIdentity;
            }
            return Describe(state,f,action,target,release,f.BeastAlive&&state.BeastIdentity==f.BeastIdentity);
        }
        public static bool CanSummonMinion(ShamanSettings settings,ShamanFacts f)
        {
            return f.MinionReady&&f.SpawnPointValid&&f.MinionPrefabAvailable&&
                f.LivingMinions>=0&&f.ReservedMinions>=0&&f.EncounterLivingMinions>=0&&f.EncounterReservedMinions>=0&&
                f.EncounterLivingMinions>=f.LivingMinions&&f.EncounterReservedMinions>=f.ReservedMinions&&
                (long)f.LivingMinions+f.ReservedMinions<settings.MinionCap&&
                (long)f.EncounterLivingMinions+f.EncounterReservedMinions<settings.EncounterMinionCap;
        }
        static ShamanDecision Describe(ShamanState s,ShamanFacts f,ShamanAction action,string target,bool release,bool beastPresent)
        {
            return new ShamanDecision {Sequence=f.Sequence,ActorIdentity=f.ActorIdentity,BindingRevision=s.BindingRevision,Mode=s.Mode,Action=action,ActionTarget=target,ReleaseBeast=release,
                OrangeControlEyes=s.LinkActive,StaffTether=s.LinkActive,
                BeastHostileToAll=beastPresent&&f.BeastClaimAvailable&&s.BeastReleased&&!s.LinkActive};
        }
    }

    // Reads the already committed owner decision; uses the existing arbitrator.
    // Native executor must implement these actions. This module cannot move,
    // shield, spawn, heal, apply damage, change factions, or recolor a renderer.
    public interface IShamanDecisionSource { ShamanDecision Observe(CombatFrame frame); }
    public sealed class ShamanProposalModule : IFormidableCombatModule
    {
        readonly IShamanDecisionSource source;
        public string Id {get{return "lwc.shaman.support-controller";}}
        public ShamanProposalModule(IShamanDecisionSource source){if(source==null)throw new ArgumentNullException();this.source=source;}
        public IEnumerable<CombatProposal> Evaluate(CombatFrame frame)
        {
            if(frame==null||!frame.OwnerReady||!frame.CombatCommitted||!ShamanSettings.Finite(frame.Time)||
               !ShamanSettings.Finite(frame.HealthFraction)||frame.HealthFraction<=0)yield break;
            var d=source.Observe(frame);if(d==null||d.Sequence!=frame.Sequence||d.ActorIdentity!=frame.ActorIdentity||d.Mode==ShamanMode.Dead||d.Action==ShamanAction.None)yield break;
            bool offensive=d.Action==ShamanAction.RangedBlast||d.Action==ShamanAction.GroundLightning;
            if(offensive&&d.ActionTarget!=frame.AuthorizedTargetIdentity)yield break;
            DecisionDomain domains;
            if(d.Action==ShamanAction.Retreat)domains=DecisionDomain.Movement|DecisionDomain.Attack;
            else if(offensive)domains=DecisionDomain.Attack|DecisionDomain.Facing;
            else domains=DecisionDomain.Support|DecisionDomain.Attack;
            if(d.ActionTarget!=null&&d.ActionTarget!=frame.ActorIdentity)domains|=DecisionDomain.Facing;
            yield return new CombatProposal("lwc.shaman."+d.Action.ToString().ToLowerInvariant(),domains,
                d.Action==ShamanAction.Retreat?600:250,d.ActionTarget,frame.Time+.1f);
        }
    }
}

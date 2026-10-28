using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.CombatSupport
{
    public enum MagicKind { Ward, Heal, Missile }
    public enum CastStatus { Reserved, Committed, Cancelled }

    public sealed class CastRecord
    {
        public MagicKind Kind;
        public CastStatus Status;
        public int Cost;
        public long CooldownTicks, ReservedAtTick;
        public CastRecord Copy() { return (CastRecord)MemberwiseClone(); }
    }

    public sealed class MagicState
    {
        public string PersonId;
        public int Current, Maximum;
        public long NextCastTick, Revision;
        public readonly Dictionary<string,CastRecord> Casts = new Dictionary<string,CastRecord>(StringComparer.Ordinal);
        public readonly HashSet<string> Restorations = new HashSet<string>(StringComparer.Ordinal);
        public MagicState Copy()
        { var x=new MagicState { PersonId=PersonId,Current=Current,Maximum=Maximum,
            NextCastTick=NextCastTick,Revision=Revision };
          foreach(var pair in Casts)x.Casts.Add(pair.Key,pair.Value.Copy());
          foreach(string id in Restorations)x.Restorations.Add(id); return x; }
    }

    public sealed class MagicCosts
    {
        public int Ward, Heal, Missile;
        public long WardCooldown, HealCooldown, MissileCooldown;
        public int Cost(MagicKind kind)
        { return kind==MagicKind.Ward?Ward:kind==MagicKind.Heal?Heal:Missile; }
        public long Cooldown(MagicKind kind)
        { return kind==MagicKind.Ward?WardCooldown:kind==MagicKind.Heal?HealCooldown:MissileCooldown; }
    }

    public sealed class CastFacts
    {
        public string RequestId, TargetId;
        public MagicKind Kind;
        public long WorldTick;
        public bool ServerAuthority, ActorOwner, CombatOwnsAction, NativeAttackReady, ExactAnimationVerified;
        public bool TargetAuthorized, TargetAlive, TargetIsAlly, WardAlreadyPresent;
        public bool LineOfSight, FriendlyFireSafe;
        public double TargetHealthFraction;
    }

    public sealed class MagicDecision
    {
        public bool Accepted, Replayed;
        public string Reason;
        public MagicState State;
    }

    // Only a server-side request and resource ledger. Native attack/status code applies effects.
    public static class MagicSupportPolicy
    {
        public static MagicDecision Reserve(MagicState source, MagicCosts costs, CastFacts facts)
        {
            if(!Valid(source,costs) || facts==null || String.IsNullOrWhiteSpace(facts.RequestId) ||
               String.IsNullOrWhiteSpace(facts.TargetId) || !Enum.IsDefined(typeof(MagicKind),facts.Kind))
                return No(source,"invalid_cast_input");
            CastRecord prior;
            if(source.Casts.TryGetValue(facts.RequestId,out prior))
                return new MagicDecision { Accepted=prior.Status==CastStatus.Reserved,Replayed=true,
                    Reason="cast_request_already_recorded",State=source };
            if(!facts.ServerAuthority || !facts.ActorOwner || !facts.CombatOwnsAction ||
               !facts.NativeAttackReady || !facts.ExactAnimationVerified)
                return No(source,"native_cast_owner_or_animation_unproved");
            if(!facts.TargetAuthorized || !facts.TargetAlive || !facts.LineOfSight || !facts.FriendlyFireSafe)
                return No(source,"target_or_cast_lane_invalid");
            if(facts.Kind==MagicKind.Ward && (!facts.TargetIsAlly || facts.WardAlreadyPresent))
                return No(source,"ward_not_needed_or_eligible");
            if(facts.Kind==MagicKind.Heal && (!facts.TargetIsAlly || facts.TargetHealthFraction<=0 ||
                facts.TargetHealthFraction>=0.6)) return No(source,"heal_not_needed_or_eligible");
            if(facts.Kind==MagicKind.Missile && facts.TargetIsAlly)
                return No(source,"missile_cannot_target_ally");
            if(facts.WorldTick<source.NextCastTick || HasReservation(source))
                return No(source,"cast_cooldown_or_reservation_active");
            int cost=costs.Cost(facts.Kind);
            if(source.Current<cost) return No(source,"insufficient_magic_resource");
            MagicState next=source.Copy();next.Casts.Add(facts.RequestId,new CastRecord {
                Kind=facts.Kind,Status=CastStatus.Reserved,Cost=cost,CooldownTicks=costs.Cooldown(facts.Kind),
                ReservedAtTick=facts.WorldTick });
            next.Revision++;
            return Yes(next,"native_cast_request_reserved");
        }

        public static MagicDecision ConfirmNativeEvent(MagicState source,string requestId,long worldTick,
            bool serverAuthority,bool actorOwner,bool nativeEventObserved)
        {
            CastRecord record;
            if(source==null || String.IsNullOrWhiteSpace(requestId) || !source.Casts.TryGetValue(requestId,out record))
                return No(source,"unknown_cast_request");
            if(record.Status!=CastStatus.Reserved)
                return new MagicDecision { Accepted=record.Status==CastStatus.Committed,Replayed=true,
                    Reason="cast_request_already_terminal",State=source };
            if(!serverAuthority || !actorOwner || !nativeEventObserved || worldTick<record.ReservedAtTick)
                return No(source,"authoritative_native_event_required");
            if(source.Current<record.Cost)return No(source,"resource_debit_conflict");
            MagicState next=source.Copy();next.Current-=record.Cost;
            next.Casts[requestId].Status=CastStatus.Committed;
            next.NextCastTick=Math.Max(next.NextCastTick,worldTick+record.CooldownTicks);
            next.Revision++;return Yes(next,"native_cast_event_committed");
        }

        public static MagicDecision CancelBeforeNativeEvent(MagicState source,string requestId,
            bool serverAuthority,bool actorOwner)
        {
            CastRecord record;
            if(source==null || String.IsNullOrWhiteSpace(requestId) || !source.Casts.TryGetValue(requestId,out record))
                return No(source,"unknown_cast_request");
            if(record.Status!=CastStatus.Reserved)
                return new MagicDecision { Accepted=record.Status==CastStatus.Cancelled,Replayed=true,
                    Reason="cast_request_already_terminal",State=source };
            if(!serverAuthority || !actorOwner)return No(source,"authoritative_cancel_required");
            MagicState next=source.Copy();next.Casts[requestId].Status=CastStatus.Cancelled;
            next.Revision++;return Yes(next,"cast_cancelled_without_effect_or_debit");
        }

        public static MagicDecision Restore(MagicState source,string eventId,int amount,bool serverAuthority,
            bool actorOwner,bool combatActive,bool safeRestOrVerifiedSupply)
        {
            if(source==null || String.IsNullOrWhiteSpace(eventId) || amount<=0 || !ValidState(source))
                return No(source,"invalid_restoration");
            if(source.Restorations.Contains(eventId))return new MagicDecision { Accepted=true,Replayed=true,
                Reason="restoration_already_recorded",State=source };
            if(!serverAuthority || !actorOwner || combatActive || !safeRestOrVerifiedSupply || HasReservation(source))
                return No(source,"restoration_not_authorized");
            MagicState next=source.Copy();next.Current=Math.Min(next.Maximum,next.Current+amount);
            next.Restorations.Add(eventId);next.Revision++;return Yes(next,"bounded_resource_restored");
        }

        static bool Valid(MagicState state,MagicCosts costs)
        { return ValidState(state) && costs!=null && costs.Ward>0 && costs.Heal>0 && costs.Missile>0 &&
            costs.WardCooldown>0 && costs.HealCooldown>0 && costs.MissileCooldown>0; }
        static bool ValidState(MagicState state)
        { return state!=null && !String.IsNullOrWhiteSpace(state.PersonId) && state.Maximum>0 &&
            state.Current>=0 && state.Current<=state.Maximum; }
        static bool HasReservation(MagicState state)
        { foreach(var x in state.Casts.Values)if(x.Status==CastStatus.Reserved)return true;return false; }
        static MagicDecision Yes(MagicState s,string reason)
        { return new MagicDecision { Accepted=true,Reason=reason,State=s }; }
        static MagicDecision No(MagicState s,string reason)
        { return new MagicDecision { Reason=reason,State=s }; }
    }
}

using System;
using LivingWorld.CompanionSystems.CombatSupport;

static class MagicSupportTests
{
    static int checks;
    static void Check(bool pass,string name) { if(!pass)throw new Exception("FAIL "+name);checks++; }
    static MagicCosts Costs()
    { return new MagicCosts { Ward=35,Heal=40,Missile=25,WardCooldown=80,HealCooldown=100,MissileCooldown=30 }; }
    static MagicState State()
    { return new MagicState { PersonId="person.warder",Current=80,Maximum=80 }; }
    static CastFacts Facts(MagicKind kind,string id)
    { return new CastFacts { RequestId=id,TargetId="ally.1",Kind=kind,WorldTick=100,
        ServerAuthority=true,ActorOwner=true,CombatOwnsAction=true,NativeAttackReady=true,
        ExactAnimationVerified=true,TargetAuthorized=true,TargetAlive=true,TargetIsAlly=true,
        TargetHealthFraction=.3,LineOfSight=true,FriendlyFireSafe=true }; }

    static void Main()
    {
        var start=State();var costs=Costs();var ward=Facts(MagicKind.Ward,"cast.ward.1");
        var reserve=MagicSupportPolicy.Reserve(start,costs,ward);
        Check(reserve.Accepted && reserve.State.Current==80 && start.Casts.Count==0,
            "ward reservation is immutable and does not debit before native event");
        Check(MagicSupportPolicy.Reserve(reserve.State,costs,ward).Replayed,
            "repeated request cannot reserve twice");
        Check(!MagicSupportPolicy.Reserve(reserve.State,costs,Facts(MagicKind.Heal,"cast.heal.1")).Accepted,
            "one actor has one cast reservation at a time");
        Check(!MagicSupportPolicy.ConfirmNativeEvent(reserve.State,"cast.ward.1",100,false,true,true).Accepted,
            "client cannot commit a cast");
        Check(!MagicSupportPolicy.ConfirmNativeEvent(reserve.State,"cast.ward.1",100,true,true,false).Accepted,
            "no effect or debit without observed native animation event");
        Check(!MagicSupportPolicy.ConfirmNativeEvent(reserve.State,"cast.ward.1",99,true,true,true).Accepted,
            "out-of-order event cannot move cooldown into the past");
        var committed=MagicSupportPolicy.ConfirmNativeEvent(reserve.State,"cast.ward.1",100,true,true,true);
        Check(committed.Accepted && committed.State.Current==45 && committed.State.NextCastTick==180,
            "native ward event spends finite resource and starts cooldown");
        Check(MagicSupportPolicy.ConfirmNativeEvent(committed.State,"cast.ward.1",100,true,true,true).Replayed &&
            committed.State.Current==45,"network replay cannot double-debit");
        ward.RequestId="cast.ward.2";ward.WorldTick=200;ward.WardAlreadyPresent=true;
        Check(!MagicSupportPolicy.Reserve(committed.State,costs,ward).Accepted,
            "ward cannot stack on an already warded ally");

        var heal=Facts(MagicKind.Heal,"cast.heal.2");heal.WorldTick=200;
        heal.TargetAlive=false;
        Check(!MagicSupportPolicy.Reserve(committed.State,costs,heal).Accepted,
            "heal never revives a committed death");
        heal.TargetAlive=true;heal.TargetHealthFraction=.95;
        Check(!MagicSupportPolicy.Reserve(committed.State,costs,heal).Accepted,
            "healing does not waste a cast on healthy ally");
        heal.TargetHealthFraction=.25;
        var healing=MagicSupportPolicy.Reserve(committed.State,costs,heal);
        Check(healing.Accepted,"wounded living ally may receive a bounded heal request");
        Check(!MagicSupportPolicy.CancelBeforeNativeEvent(healing.State,"cast.heal.2",false,true).Accepted,
            "client cannot cancel server cast");
        var cancelled=MagicSupportPolicy.CancelBeforeNativeEvent(healing.State,"cast.heal.2",true,true);
        Check(cancelled.Accepted && cancelled.State.Current==45 && cancelled.State.NextCastTick==180,
            "interrupted pre-event cast has no effect or resource debit");
        Check(!MagicSupportPolicy.ConfirmNativeEvent(cancelled.State,"cast.heal.2",200,true,true,true).Accepted,
            "cancelled cast cannot later apply effect");

        var missile=Facts(MagicKind.Missile,"cast.missile.1");missile.WorldTick=220;
        Check(!MagicSupportPolicy.Reserve(cancelled.State,costs,missile).Accepted,
            "missile cannot target ally");
        missile.TargetIsAlly=false;missile.FriendlyFireSafe=false;
        Check(!MagicSupportPolicy.Reserve(cancelled.State,costs,missile).Accepted,
            "missile requires a safe native target lane");
        missile.FriendlyFireSafe=true;
        var attack=MagicSupportPolicy.Reserve(cancelled.State,costs,missile);
        Check(attack.Accepted,"authorized enemy may receive one native missile request");
        var fired=MagicSupportPolicy.ConfirmNativeEvent(attack.State,"cast.missile.1",220,true,true,true);
        Check(fired.Accepted && fired.State.Current==20 && fired.State.NextCastTick==250,
            "missile depletes reserve and starts its own cooldown");
        var next=Facts(MagicKind.Heal,"cast.heal.3");next.WorldTick=260;
        Check(!MagicSupportPolicy.Reserve(fired.State,costs,next).Accepted,
            "empty reserve refuses expensive support cast");

        Check(!MagicSupportPolicy.Restore(fired.State,"rest.1",30,true,true,true,true).Accepted,
            "resource cannot refill during combat");
        Check(!MagicSupportPolicy.Restore(fired.State,"rest.1",30,true,true,false,false).Accepted,
            "resource cannot refill without verified rest or supply");
        var restored=MagicSupportPolicy.Restore(fired.State,"rest.1",30,true,true,false,true);
        Check(restored.Accepted && restored.State.Current==50 && fired.State.Current==20,
            "safe verified restoration changes a copy of the bounded reserve");
        Check(MagicSupportPolicy.Restore(restored.State,"rest.1",30,true,true,false,true).Replayed &&
            restored.State.Current==50,"restoration evidence cannot refill twice");
        var capped=MagicSupportPolicy.Restore(restored.State,"rest.2",100,true,true,false,true);
        Check(capped.Accepted && capped.State.Current==80,"restoration never exceeds maximum reserve");
        Console.WriteLine("MagicSupportTests PASS: "+checks+" checks");
    }
}

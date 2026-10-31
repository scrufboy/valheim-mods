// FormidableAI A2 corrected candidate offline policy tests: immutable roles, initiative, strike-window defense, lanes, leash.
// Each is named after the live evidence or directive line it answers.
using System;
using System.Collections.Generic;
using LivingWorld.Core;

static class FormidableA2Tests
{
    public static void RunAll(Action<string, Action> run)
    {
        run("A2.2 identity: Guard, Archer and Scout remain their authored roles; unknown roles stay unknown", RolesRemainThemselves);
        run("A2.2 weapon (2026-10-31: 20% melee vs distant): Archer bows from 6 m, Guard holds the sword to 12 m", RoleWeaponBands);
        run("A2.2 weapon: a target closing within 1.2 s is met with the weapon in hand, not a swap", WeaponNoSwapUnderCharge);
        run("A2.1 initiative: committed beyond a Guard's reach = ready in formation; inside it = engage", InitiativeGuardReach);
        run("A2.1 initiative: an Archer opens on a committed contact at bow range", InitiativeArcherOpening);
        run("A2.1 initiative: a Guard intercepts a contact going for the leader", InitiativeGuardIntercept);
        run("A2.1 initiative: noticed = observe, alerted = ready, alerted while sneaking = hold stealth", InitiativeStates);
        run("A2.1 initiative: wildlife is watched, never initiated on", InitiativeWildlife);
        run("A2.1 initiative: anything committed on me inside 6 m is engaged by every class, leash or not", InitiativeSelfDefense);
        run("A2.1 arbiter: commitment enters combat without a native target; readiness from an alerted contact", ArbiterInitiative);
        run("A2.4 leash: a Guard does not chase a target 20 m from the leader; defends himself anyway", LeashHolds);
        run("A2.1 party-committed contact stays fightable when native senses lost it (no wallhack: party sighted it)", PartyCommittedEngaged);
        run("A2.3 defense: block raised before predicted impact and held through it", DefenseBlockTiming);
        run("A2.3 defense: parry only on a learned attack, inside the native 0.25 s perfect-block window", DefenseParryWindow);
        run("A2.3 defense: too late to react = absorb; no shield + free lane = evade (Archer)", DefenseAbsorbEvade);
        run("A2.3 defense: interrupt only when my hit lands first and the class takes the trade", DefenseInterrupt);
        run("A2.3 defense: projectile impact includes flight time", DefenseProjectile);
        run("A2.3 defense: no strike, no raise - ten quiet seconds never raise a shield (no cadence)", DefenseNoCadence);
        run("A2.3 defense: one window per strike, never re-opened for the same swing", DefenseOneWindowPerStrike);
        run("A2.3 defense: windups are learned from watched strikes; prior until 3 samples", StrikeTimingLearned);
        run("A2.3 defense: counter only after a defended strike in reach", CounterRule);
        run("A2.4 lanes: a fall-back runs away from the threat, never past it, never into blocked ground", LaneAway);
        run("A2.4 lanes: among safe lanes, the one toward the partner wins; no lane = -1 (stand)", LaneCoverAndNone);
        run("A2.3 evade: side-step chooses the free side", EvadeSide);
        run("A2.4 review: the leash also lets go of a target already held; protected-member attackers are never 'chasing'", LeashKeepRule);
        run("A2.4 review: a lane in use that now passes an enemy is excluded and replaced", LaneRescored);
        run("A2.3 review: an unlearned guard is held while the attacker still swings", GuardExtends);
        run("A2.1 review: intercept never reaches past the 30 m keep rule (no drop/re-take loop)", GuardLimit);
    }

    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }

    static void RolesRemainThemselves()
    {
        CombatDoctrine guard = CombatDoctrines.ForRole("guard");
        CombatDoctrine archer = CombatDoctrines.ForRole("archer");
        CombatDoctrine scout = CombatDoctrines.ForRole("scout");
        CombatDoctrine authored = CombatDoctrines.ForRole("cleric");
        Assert(guard.Role == CombatRole.Guard && guard.RoleName == "Guard", "Guard identity changed");
        Assert(archer.Role == CombatRole.Archer && archer.RoleName == "Archer", "Archer identity changed");
        Assert(scout.Role == CombatRole.Scout && scout.RoleName == "Scout", "Scout identity changed");
        Assert(authored.Role == CombatRole.Unknown && authored.RoleName == "cleric", "unknown authored role was converted");
    }

    static WeaponChoice.Pick W(string role, float d, bool holdingBow, bool holdingMelee, float ttc = float.PositiveInfinity, bool flying = false)
    {
        return CombatDoctrines.Weapon(CombatDoctrines.ForRole(role), true, true, true, holdingBow, holdingMelee, d, flying, ttc);
    }

    static void RoleWeaponBands()
    {
        Assert(W("archer", 7f, false, true) == WeaponChoice.Pick.Bow, "Archer kept the sword at 7 m");
        Assert(W("archer", 3f, true, false) == WeaponChoice.Pick.Melee, "Archer kept the bow at 3 m");
        Assert(W("guard", 10f, false, true) != WeaponChoice.Pick.Bow, "Guard swapped to bow at 10 m");
        Assert(W("guard", 13f, false, true) == WeaponChoice.Pick.Bow, "Guard kept the sword at 13 m");
        Assert(W("guard", 25f, false, true, float.PositiveInfinity, true) == WeaponChoice.Pick.Bow, "flying target got melee");
        Assert(CombatDoctrines.Weapon(CombatDoctrines.ForRole("archer"), true, false, true, false, false, 20f, false, float.PositiveInfinity)
            == WeaponChoice.Pick.Melee, "no arrows but chose the bow");
    }

    static void WeaponNoSwapUnderCharge()
    {
        Assert(W("guard", 13f, false, true, .8f) == WeaponChoice.Pick.Keep, "swapped to bow with a charge 0.8 s out");
        Assert(W("guard", 13f, false, false, .8f) == WeaponChoice.Pick.Melee, "empty hands under a charge did not draw melee");
        Assert(W("guard", 13f, false, true, 5f) == WeaponChoice.Pick.Bow, "a slow approach blocked the bow");
    }

    static InitiativeOption Contact(int id, ContactState state, float dist, float leaderDist = 5f)
    {
        return new InitiativeOption { Id = id, State = state, Distance = dist, LeaderDistance = leaderDist, TimeToContact = float.PositiveInfinity };
    }

    static int Pick(string role, out ContactState strongest, out string why, params InitiativeOption[] o)
    {
        return InitiativePolicy.Choose(new List<InitiativeOption>(o), CombatDoctrines.ForRole(role), out strongest, out why);
    }

    static void InitiativeGuardReach()
    {
        ContactState st; string why;
        int i = Pick("guard", out st, out why, Contact(1, ContactState.Committed, 40f, 40f));
        Assert(i < 0 && st == ContactState.Committed, "Guard charged a committed contact at 40 m");
        Assert(InitiativePolicy.Response(st, false, false) == InitiativeResponse.Ready, "committed beyond reach is not readiness");
        i = Pick("guard", out st, out why, Contact(1, ContactState.Committed, 12f, 12f));
        Assert(i == 0 && InitiativePolicy.Response(st, false, true) == InitiativeResponse.Engage, "Guard did not engage at 12 m");
    }

    static void InitiativeArcherOpening()
    {
        ContactState st; string why;
        int i = Pick("archer", out st, out why, Contact(1, ContactState.Committed, 28f, 26f));
        Assert(i == 0 && why == "ranged opening", "Archer did not open at 28 m (" + why + ")");
    }

    static void InitiativeGuardIntercept()
    {
        ContactState st; string why;
        InitiativeOption onLeader = Contact(2, ContactState.Committed, 18f, 10f); onLeader.TargetsLeader = true;
        InitiativeOption other = Contact(3, ContactState.Committed, 11f, 9f);
        int i = Pick("guard", out st, out why, other, onLeader);
        Assert(i == 1 && why == "intercept: on leader", "Guard did not intercept the leader's attacker (" + why + ")");
    }

    static void InitiativeStates()
    {
        Assert(InitiativePolicy.Response(ContactState.Noticed, false, false) == InitiativeResponse.Observe, "noticed did not observe");
        Assert(InitiativePolicy.Response(ContactState.Alerted, false, false) == InitiativeResponse.Ready, "alerted did not ready");
        Assert(InitiativePolicy.Response(ContactState.Alerted, true, false) == InitiativeResponse.HoldStealth, "alerted broke stealth");
        Assert(InitiativePolicy.Response(ContactState.Committed, true, true) == InitiativeResponse.Engage, "committed while sneaking did not engage");
    }

    static void InitiativeWildlife()
    {
        ContactState st; string why;
        InitiativeOption deer = Contact(1, ContactState.Committed, 5f); deer.Wildlife = true;
        Assert(Pick("scout", out st, out why, deer) < 0 && st == ContactState.None, "initiated on wildlife");
        deer.HurtUs = true;
        Assert(Pick("scout", out st, out why, deer) == 0, "wildlife that hurt us is not answered");
    }

    static void InitiativeSelfDefense()
    {
        ContactState st; string why;
        InitiativeOption onMe = Contact(1, ContactState.Committed, 4f, 40f); onMe.TargetsMe = true;
        Assert(Pick("guard", out st, out why, onMe) == 0 && why == "on me", "self-defence blocked by the leash");
    }

    static PhaseInputs Calm()
    {
        return new PhaseInputs { AppearanceReady = true, HasLeader = true, LeaderDistance = 3f, NearestPartyDistance = 3f, FormationDistance = 1f };
    }

    static void ArbiterInitiative()
    {
        var a = new PhaseArbiter();
        PhaseInputs s = Calm(); s.InitiativeReady = true;
        Assert(a.Step(s, .1f) == NpcPhase.Readiness, "alerted contact did not ready");
        s.InitiativeReady = false; s.InitiativeEngage = true;
        Assert(a.Step(s, .1f) == NpcPhase.Combat && a.Reason == "awareness: committed contact", "commitment did not enter combat: " + a.Reason);
    }

    static TargetCandidate Foe(int id, float dist, float leaderDist)
    {
        return new TargetCandidate { Id = id, Distance = dist, CanSee = true, Alerted = true, LeaderDistance = leaderDist, PartnerDistance = -1f };
    }

    static void LeashHolds()
    {
        string why;
        var far = Foe(1, 12f, 20f);   // alerted and seen mid-fight, going for nobody of ours
        int i = TargetPolicy.Choose(new List<TargetCandidate> { far }, -1, true, false, 3f, CombatDoctrines.ForRole("guard").Leash, out why);
        Assert(i < 0, "Guard chased a target 20 m from the leader");
        var onMe = Foe(2, 2f, 20f); onMe.TargetsMe = true;
        i = TargetPolicy.Choose(new List<TargetCandidate> { far, onMe }, -1, true, false, 3f, 14f, out why);
        Assert(i == 1 && why == "on me", "leash blocked self-defence");
        i = TargetPolicy.Choose(new List<TargetCandidate> { far }, -1, true, false, 3f, out why);
        Assert(i == 0, "old overload changed (no leash)");
    }

    static void PartyCommittedEngaged()
    {
        var c = Foe(1, 20f, 15f); c.CanSee = false; c.Alerted = false;
        Assert(!TargetPolicy.Engaged(c, true, false), "unseen, unalerted contact engaged without party evidence");
        c.PartyCommitted = true;
        Assert(TargetPolicy.Engaged(c, true, false), "party-committed contact not engageable");
    }

    static DefenseCapability Me(bool shield, float reach = 0f, bool lane = false)
    {
        return new DefenseCapability { ShieldInHand = shield, OwnReach = reach, OwnWindup = .55f, LaneFree = lane, Health01 = 1f, Reaction = .2f };
    }

    static StrikeObservation Strike(float elapsed, float windup, bool learned = false, float dist = 1.5f)
    {
        return new StrikeObservation { AttackerId = 7, Distance = dist, Reach = 2f, AimedAtMe = true, Elapsed = elapsed, Windup = windup, WindupLearned = learned };
    }

    static void DefenseBlockTiming()
    {
        DefensePlan p = DefenseArbiter.Plan(Strike(0f, .6f), Me(true), CombatDoctrines.ForRole("archer"));
        Assert(p.Choice == DefenseChoice.Block, "no block: " + p.Choice);
        Assert(Math.Abs(p.ImpactIn - .6f) < 1e-4f && p.ActIn < p.ImpactIn && p.ActIn >= .2f - 1e-4f, "raise not before impact / before reaction");
        Assert(p.ActIn + p.HoldFor >= p.ImpactIn + DefenseArbiter.HoldAfter - 1e-4f, "lowered before the impact passed");
    }

    static void DefenseParryWindow()
    {
        DefensePlan p = DefenseArbiter.Plan(Strike(0f, .6f, true), Me(true), CombatDoctrines.ForRole("guard"));
        Assert(p.Choice == DefenseChoice.Parry, "learned strike not parried by a Guard: " + p.Choice);
        float lead = p.ImpactIn - p.ActIn;
        Assert(lead > 0f && lead < DefenseArbiter.PerfectBlockInterval, "parry raise outside the perfect-block window: " + lead);
        p = DefenseArbiter.Plan(Strike(0f, .6f, false), Me(true), CombatDoctrines.ForRole("guard"));
        Assert(p.Choice == DefenseChoice.Block, "parry attempted on an unlearned attack");
    }

    static void DefenseAbsorbEvade()
    {
        DefensePlan p = DefenseArbiter.Plan(Strike(.55f, .6f), Me(true), CombatDoctrines.ForRole("guard"));
        Assert(p.Choice == DefenseChoice.Absorb, "raised with 0.05 s to impact (faster than reaction): " + p.Choice);
        p = DefenseArbiter.Plan(Strike(0f, .7f), Me(false, 0f, true), CombatDoctrines.ForRole("archer"));
        Assert(p.Choice == DefenseChoice.Evade, "Archer with a free lane did not evade: " + p.Choice);
        p = DefenseArbiter.Plan(Strike(0f, .7f), Me(false, 0f, false), CombatDoctrines.ForRole("archer"));
        Assert(p.Choice == DefenseChoice.Absorb, "evaded into a blocked lane");
    }

    static void DefenseInterrupt()
    {
        DefensePlan p = DefenseArbiter.Plan(Strike(0f, .9f, false, 1.5f), Me(false, 2.5f), CombatDoctrines.ForRole("scout"));
        Assert(p.Choice == DefenseChoice.Interrupt, "Scout did not take the faster hit: " + p.Choice);
        p = DefenseArbiter.Plan(Strike(0f, .6f, false, 1.5f), Me(false, 2.5f), CombatDoctrines.ForRole("scout"));
        Assert(p.Choice != DefenseChoice.Interrupt, "interrupted a strike that lands first");
        p = DefenseArbiter.Plan(Strike(0f, .9f, false, 1.5f), Me(true, 2.5f), CombatDoctrines.ForRole("guard"));
        Assert(p.Choice != DefenseChoice.Interrupt, "a Guard traded hits instead of guarding");
    }

    static void DefenseProjectile()
    {
        var s = new StrikeObservation { AttackerId = 3, Distance = 20f, Ranged = true, AimedAtMe = true, Elapsed = .2f, Windup = .5f, ProjectileSpeed = 40f };
        Assert(Math.Abs(DefenseArbiter.ImpactIn(s) - (.3f + .5f)) < 1e-4f, "projectile flight time not included");
        Assert(DefenseArbiter.Plan(s, Me(true), CombatDoctrines.ForRole("guard")).Choice == DefenseChoice.Block, "aimed arrow not blocked");
    }

    static void DefenseNoCadence()
    {
        var w = new DefenseWindow();
        for (float t = 0f; t < 10f; t += .1f) Assert(!w.ShieldUp(t) && !w.Due(t), "raised without a strike at " + t);
        var none = new DefensePlan { Choice = DefenseChoice.None };
        Assert(!w.Offer(1f, 1f, none), "opened a window without a plan");
    }

    static void DefenseOneWindowPerStrike()
    {
        var w = new DefenseWindow();
        DefensePlan p = DefenseArbiter.Plan(Strike(0f, .6f), Me(true), CombatDoctrines.ForRole("guard"));
        Assert(w.Offer(0f, 0f, p), "first strike not offered");
        Assert(!w.ShieldUp(p.ActIn - .05f) && w.ShieldUp(p.ActIn + .01f), "raise not at the planned time");
        float release = p.ActIn + p.HoldFor;
        Assert(w.Expire(release + .01f) && !w.ShieldUp(release + .02f), "not released after the window");
        Assert(!w.Offer(release + .1f, 0f, p), "same swing re-opened a window");
        Assert(w.Windows == 1, "windows counted wrong");
        Assert(w.Offer(2f, 1.9f, p), "a new swing by the same attacker was ignored");
    }

    static void StrikeTimingLearned()
    {
        var b = new StrikeTimingBook();
        bool learned;
        b.Learn("Goblin:attack", .4f); b.Learn("Goblin:attack", .44f);
        Assert(Math.Abs(b.Windup("Goblin:attack", false, out learned) - DefenseArbiter.WindupPrior) < 1e-4f && !learned, "learned from 2 samples");
        b.Learn("Goblin:attack", .42f);
        float w = b.Windup("Goblin:attack", false, out learned);
        Assert(learned && Math.Abs(w - .42f) < .01f, "mean windup wrong: " + w);
        b.Learn("x", 9f);
        Assert(b.Samples("x") == 0, "accepted an impossible 9 s windup");
    }

    static void CounterRule()
    {
        Assert(DefenseArbiter.Counter(true, true, 1.5f, 2f, false), "no counter after a parry stagger in reach");
        Assert(!DefenseArbiter.Counter(false, true, 1.5f, 2f, false), "countered after taking the hit");
        Assert(!DefenseArbiter.Counter(true, false, 4f, 2f, false), "countered out of reach");
        Assert(!DefenseArbiter.Counter(true, true, 1.5f, 2f, true), "countered mid-swing");
    }

    static void LaneAway()
    {
        P2 me = new P2(0f, 0f);
        P2[] threats = { new P2(0f, 4f) };   // north
        float score; string why;
        int lane = LanePlanner.Choose(me, threats, null, 1, null, false, me, 0f, out score, out why);
        P2 d = LanePlanner.Direction(lane);
        Assert(d.Z < -.5f, "fall-back did not run away from the threat (lane " + lane + ")");
        var blocked = new bool[LanePlanner.Directions];
        blocked[LanePlanner.Nearest(new P2(0f, -1f))] = true;
        lane = LanePlanner.Choose(me, threats, null, 1, blocked, false, me, 0f, out score, out why);
        Assert(!blocked[lane], "chose a blocked lane");
        P2[] side = { new P2(3f, 3f) };
        for (int i = 0; i < LanePlanner.Directions; i++)
        {
            P2 dir = LanePlanner.Direction(i);
            if (LanePlanner.SegmentDistance(me, dir * LanePlanner.Step, side[0]) < LanePlanner.PassClearance)
                Assert(LanePlanner.Choose(me, side, null, 1, null, false, me, 0f, out score, out why) != i, "lane passes the threat");
        }
    }

    static void LaneCoverAndNone()
    {
        P2 me = new P2(0f, 0f);
        P2[] threats = { new P2(0f, 4f) };
        float score; string why;
        P2 partner = new P2(-8f, -2f);
        int lane = LanePlanner.Choose(me, threats, null, 1, null, true, partner, 1f, out score, out why);
        P2 d = LanePlanner.Direction(lane);
        Assert(d.X < 0f && d.Z <= 0f, "cover lane not toward the partner (lane " + lane + ")");
        var all = new bool[LanePlanner.Directions];
        for (int i = 0; i < all.Length; i++) all[i] = true;
        Assert(LanePlanner.Choose(me, threats, null, 1, all, true, partner, 1f, out score, out why) == -1, "no-lane did not return -1");
    }

    static void LeashKeepRule()
    {
        var c = Foe(1, 12f, 20f);
        Assert(!TargetPolicy.WithinLeash(c, 14f, false), "held a target 20 m from the leader past a 14 m leash");
        Assert(TargetPolicy.WithinLeash(c, 14f, true), "self-defence outside the leash refused");
        c.TargetsLeader = true;
        Assert(TargetPolicy.WithinLeash(c, 14f, false), "an attacker going for the leader counted as chasing");
    }

    static void LaneRescored()
    {
        P2 me = new P2(0f, 0f);
        int south = LanePlanner.Nearest(new P2(0f, -1f));
        Assert(!float.IsNaN(LanePlanner.Score(south, me, new[] { new P2(0f, 4f) }, null, 1, null, false, me, 0f)), "south lane excluded with the threat north");
        Assert(float.IsNaN(LanePlanner.Score(south, me, new[] { new P2(0f, -3f) }, null, 1, null, false, me, 0f)), "lane through a threat still scored");
    }

    static void GuardExtends()
    {
        var w = new DefenseWindow();
        DefensePlan p = DefenseArbiter.Plan(Strike(0f, .55f), Me(true), CombatDoctrines.ForRole("guard"));
        w.Offer(0f, 0f, p);
        float release = w.ReleaseAt;
        w.ExtendRelease(release + .5f);
        Assert(w.ShieldUp(release + .3f), "guard dropped while the window was extended");
        w.ExtendRelease(release - 1f);
        Assert(Math.Abs(w.ReleaseAt - (release + .5f)) < 1e-4f, "extension shortened the window");
    }

    static void GuardLimit()
    {
        ContactState st; string why;
        InitiativeOption far = Contact(1, ContactState.Committed, 33f, 20f); far.TargetsPartner = true;
        Assert(Pick("archer", out st, out why, far) < 0, "intercept at 33 m (past the 30 m keep rule)");
        far.Distance = 27f;
        Assert(Pick("archer", out st, out why, far) == 0, "no intercept at 27 m");
    }

    static void EvadeSide()
    {
        P2 me = new P2(0f, 0f), attacker = new P2(0f, 2f);
        var blocked = new bool[LanePlanner.Directions];
        int s = LanePlanner.SideStep(me, attacker, blocked, false, me);
        P2 d = LanePlanner.Direction(s);
        Assert(Math.Abs(d.Z) < .5f, "side-step not sideways");
        blocked[s] = true;
        int t = LanePlanner.SideStep(me, attacker, blocked, false, me);
        Assert(t >= 0 && t != s, "did not take the other free side");
    }
}

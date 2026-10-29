// Regression ledger, part 1: offline policy tests.
// Every test is named after a failure that actually happened in a live run.
// The build refuses to package if any of these fail.
using System;
using System.Collections.Generic;
using LivingWorld.Core;

static class RegressionLedgerTests
{
    static int passed, failed;
    static readonly List<string> failures = new List<string>();

    static int Main()
    {
        Run("1.0.32.8 spacing: leader standing in the route is walked around, never touched", SpacingLeaderBlocksRoute);
        Run("1.0.32.8 spacing: a standing companion in the route is walked around", SpacingCompanionBlocksCompanion);
        Run("1.0.32.8 spacing: narrow corridor - lower slot goes, higher slot waits, no side flipping", SpacingNarrowCorridor);
        Run("1.0.32.8 spacing: a companion already in its soft region does not reposition", SpacingHoldsInsideRegion);
        Run("1.0.32.8 spacing: brief proximity does not start a sidestep", SpacingBriefProximityIgnored);
        Run("1.0.32.8 spacing: sidestep releases smoothly after clearance, side locked", SpacingReleasesSmoothly);
        Run("1.0.32.8 spacing: never sidesteps into an invalid lane; with no lane it waits and never hands back into the blockage", SpacingNoInvalidLane);
        Run("1.0.32.8b blocked: a leader still blocking both lanes after 1.5 s is never entered or pushed", SpacingBlockedLeaderHeld);
        Run("1.0.32.8b blocked: a companion still blocking both lanes after 1.5 s is never entered or pushed", SpacingBlockedCompanionHeld);
        Run("1.0.32.8b blocked: the yielding companion stays stopped without side oscillation", SpacingYielderStill);
        Run("1.0.32.8b blocked: the right-of-way companion keeps moving; the yielder gives way, no contact", SpacingRightOfWayMoves);
        Run("1.0.32.8b blocked: movement resumes once the corridor clears", SpacingResumesAfterClear);
        Run("1.0.32.8b blocked: a permanently unreachable spot never causes approach/back-off oscillation", SpacingUnreachableStable);
        Run("1.0.32.8 spacing: local crowding never calls a regroup", SpacingCrowdingNeverRegroups);
        Run("1.0.32.8 spacing: a party walking together never sidesteps (leader weaving, walk to run)", SpacingFormationTravelQuiet);
        Run("1.0.32.8 spacing (Brian): leader in a menu is never pushed; companions ease back out", SpacingMenuNeverPushes);
        Run("1.0.32.9 live 2026-10-28: a companion backs away from the leader before touching him", SpacingLeaderBackOffBeforeContact);
        Run("1.0.32.9 live 2026-10-28: two standing companions pressed together separate; only the yielder moves, no oscillation", SpacingPressedMatesSeparate);
        Run("1.0.32.9 live 2026-10-28: a companion pressed against the leader with a wall behind it still separates sideways", SpacingBackOffAroundWall);
        Run("1.0.32.9 live 2026-10-28: a standing spot against a right-of-way companion moves off it", SpacingGoalOffMate);
        Run("1.0.32.10 gait (Brian): leader walking slowly, companions walk at his pace", GaitWalksWithSlowLeader);
        Run("1.0.32.10 gait: a slow jog is matched without the walk animation; a full jog stays native", GaitJogBands);
        Run("1.0.32.10 gait: a companion behind its spot catches up, bounded by its native jog", GaitCatchUpBounded);
        Run("1.0.32.10 gait: the leader's walk toggle always means walk; far away means ordinary pace", GaitToggleAndRange);
        Run("1.0.32.10 gait: noisy leader speed does not flip walk/jog every tick", GaitNoFlicker);
        Run("1.0.32.12 throw: an incoming cluster farther off is chosen over a closer pair standing still", ThrowPrefersIncomingCluster);
        Run("1.0.32.12 throw (Brian hard rule): never lands near the leader or a companion, now or when it lands", ThrowNeverNearFriends);
        Run("1.0.32.12 throw: aims ahead of a moving group, inside reach, not at its feet", ThrowLeadsAndRange);
        Run("1.0.32.12 throw: ballistic elevation lands at the planned distance for the Souls throw speed", ThrowBallistics);
        Run("1.0.32.12 throw: a lone weak enemy is not worth it; a lone tough one is; cooldown holds", ThrowWorthAndCadence);
        Run("1.0.32.13 body voice: one onset cough, bounded repeats, one recovery breath", BodyVoiceOnsetRepeatRecover);
        Run("1.0.32.13 body voice: combat lines win; an onset is deferred then dropped, never stacked", BodyVoiceYieldsToCombat);
        Run("1.0.32.13 body voice: nothing after death; a one-tick flicker is not an exposure", BodyVoiceDeathAndFlicker);
        Run("1.0.32.13 poison law unchanged by 1.0.32.14: same constants", BodyVoicePoisonLocked);
        Run("1.0.32.14 burning/frost: one onset, sparse repeats, one exhale only after real exposure", StateLastingOnsetRepeatRecover);
        Run("1.0.32.14 states: never over a combat or body line; deferred onset dropped, not stacked", StateYieldsWhenNotQuiet);
        Run("1.0.32.14 states: nothing while dead; a status flicker is not a condition", StateDeathAndFlicker);
        Run("1.0.32.14 swim: short swim or bobbing out says nothing; a long swim earns one breath on leaving", SwimOnlyAfterLongSwim);
        Run("1.0.32.14 exhaustion: one breath per event, 45 s cooldown, dropped if it cannot be voiced", ExhaustionEventCooldown);
        Run("1.0.32.14 actor gate: one voice at a time, min gap, exhale at most every 30 s", ActorGateOneVoice);
        Run("1.0.32.14 clip choice: never the same clip twice in a row", ClipPickerNoRepeat);
        Run("1.0.32.15 party clouds: reduced share for the party, enemies and strangers untouched", PartyCloudFractions);
        Run("1.0.32.15 party clouds: only lingering poison/fire clouds count", PartyCloudHarmful);
        Run("1.0.32.15 hazard escape: confirm, leave with margin, give up when pinned, cooldown", HazardEscapeLaw);
        Run("LOCK combat voice 1.0.32.6: every rule constant is the approved value", CombatVoiceConstantsLocked);
        Run("LOCK combat voice: alert only before contact, once per enemy, party cooldown", CombatVoiceAlertsLocked);
        Run("LOCK combat voice: every heavy exertion voiced, ordinary swings 30% / 2.5 s", CombatVoiceExertionLocked);
        Run("LOCK combat voice: ordinary 65%/1.5 s, devastating always/4 s, hit mid-line always", CombatVoiceHurtLocked);
        Run("LOCK combat voice: one stagger cry per 1.5 s; its damage is the same event", CombatVoiceStaggerLocked);
        Run("LOCK combat voice: gloat only at kills or 25% blows, 35%, 10 s", CombatVoiceGloatLocked);
        Run("LOCK combat voice: war cry only at a calm raid start", CombatVoiceWarCryLocked);
        Run("LOCK combat voice: death = one unattached line from the identity's own pool", CombatVoiceDeathLocked);
        Run("1.0.32.6 Fuling raid: death cry from the gendered pool, unattached, exactly one", DeathVoiceIsOneUnattachedGenderedLine);
        Run("1.0.32.2: female surprised pair shares one slot (A 1/6, B 1/6, others 1/3)", FemaleSlotFlatten);
        Run("1.0.32.1: Surprised01 split keeps its 1/3 slot weight (A 1/6, B 1/6)", SplitKeepsSlotWeight);
        Run("1.0.30 live 1.0.29: noticing before, exertion during, reactions after", VoiceBeforeDuringAfter);
        Run("1.0.23 live 1.0.22: split pair defends the partner, not the leader regroup", SpartaDefendsThePartner);
        Run("1.0.23: war cry only at a raid start, never costing a hit", WarCryNeverCostsAHit);
        Run("1.0.21: heavy only at an opening (stagger / crowd), never into a converging crowd", HeavyOnlyWhenItPays);
        Run("1.0.21: heavy-only pool refills on the Player curve, never blocked", StaminaFollowsPlayerRules);
        Run("1.0.18 lab: gear kits are complete and resupply their own arrow", KitsAreCompleteAndResupplyTheirOwnArrow);
        Run("1.0.17: melee chains only while the next swing can land", ChainContinuesOnlyWhenTheSwingCanLand);
        Run("0.6.3 Bonemass: distant ranged target does not trigger party regroup", RangedTargetIsNotSeparation);
        Run("0.6.4/0.6.5/1.0.5: returning companion fights what engages it, re-measures before returning", ReturningActorDefendsLocally);
        Run("0.6.5 gap: returning companion without local threat stays in regroup", ReturningWithoutThreatRegroups);
        Run("0.3.40 churn: one target gap does not end combat", SingleGapDoesNotEndCombat);
        Run("Contract: clear needs five uninterrupted quiet seconds", ClearNeedsFiveSeconds);
        Run("Contract: alerted hostile nearby resets the clear timer", AlertedHostileResetsClear);
        Run("Contract: damage during clear returns straight to combat", DamageDuringClearReturnsToCombat);
        Run("0.5.2: leader with no visible weapon is not readiness", UnarmedLeaderIsCalm);
        Run("0.5.0: alert without a target is readiness, not combat", AlertWithoutTargetIsReadiness);
        Run("0.6.x: cohesion latch exits only inside the inner area", CohesionExitsOnlyInside);
        Run("0.6.x: cohesion ignores brief separations under the tick threshold", CohesionNeedsSustainedSeparation);
        Run("Kernel: two behaviors can never hold the same domain", SchedulerIsExclusive);
        Run("Kernel: combat (highest priority) wins every contested domain", SchedulerPriority);
        Run("Kernel: partial claims are never granted", SchedulerAllOrNothing);
        Run("0.3.47: torch does not flicker at a light boundary", TorchHysteresis);
        Run("0.3.59/0.3.60: torch is off immediately in combat and readiness", TorchOffInCombat);
        Run("Contract: torch waits five seconds after combat", TorchWaitsAfterCombat);
        Run("0.3.46: torch off while leader sneaks", TorchOffInStealth);
        Run("0.3.46: built light hysteresis 6 m in / 10 m out", BuiltLightHysteresis);
        Run("0.6.0: guard prefers the opposite sector after a sweep", GuardPrefersOpposite);
        Run("0.6.0: guard never picks a sector a teammate holds", GuardAvoidsTeammate);
        Run("0.3.59/0.4.1: every male gets a beard, no female does", BeardsByModel);
        Run("0.3.59: every companion gets a real head-hair style", HairAlwaysChosen);
        Run("1.0.0 live run: shield pairs with a one-handed weapon", ShieldPairsWithOneHanded);
        Run("1.0.0 live run: no shield beside bow, two-hander or occupied off hand", ShieldNeverWrongPair);
        Run("1.0.0 live run: bows have a minimum native engagement range", BowMinimumRange);
        Run("Block: native AI never blocks, so we raise after a human reaction delay", BlockAfterReaction);
        Run("Block: no shield in hand, no block", BlockNeedsShield);
        Run("Block: never while attacking, drawing or staggered", BlockNeverWhileBusy);
        Run("Block: melee only in close quarters and in front", BlockMeleeCloseAndFront);
        Run("Block: projectile only when aimed at this NPC", BlockProjectileTargeted);
        Run("Block: enemy attacking someone else is ignored", BlockIgnoresOthers);
        Run("Block: maximum hold then cooldown (no perfect turtle)", BlockHoldAndCooldown);
        Run("Block: lowered the moment the attack ends", BlockLowersWhenOver);
        Run("Voice: one call-out per new enemy per party, never for one already seen", VoiceNewEnemyOnce);
        Run("Voice: party cooldown stops a chorus", VoicePartyCooldown);
        Run("Voice: devastating hits always cry out (cooldown); ordinary hits only sometimes", VoiceHurt);
        Run("Voice: hit while gloating is cut off and usually answers", VoiceInterrupted);
        Run("Voice: mighty blows and kills taunt only rarely", VoiceMighty);
        Run("1.0.11 Moder: a flying target always gets the bow, even close", FlyingGetsBow);
        Run("Weapon: ground target far = bow, close = melee", GroundByDistance);
        Run("Weapon: 6-10 m band keeps what is in hand (no flip-flop)", WeaponHysteresis);
        Run("Weapon: no arrows = melee", NoAmmoMelee);
        Run("1.0.6 deaths: badly hurt with an enemy close = fall back", FallBackStarts);
        Run("Fall-back: not while healthy, not with nothing close", FallBackNeedsBoth);
        Run("Fall-back ends: clear to bow range", FallBackEndsClear);
        Run("Fall-back ends: caught = stand and fight", FallBackEndsCaught);
        Run("Fall-back ends: time cap = stand and fight (no endless retreat)", FallBackEndsTime);
        Run("Fall-back ends: healed", FallBackEndsHealed);
        Run("Fall-back: cooldown, then at most 3 per fight, then last stand", FallBackLimits);
        Run("Fall-back: resets when the fight ends", FallBackResets);
        Run("1.0.5 fix: an engaged enemy close by blocks the return to the leader", PinnedNeverReturns);
        Run("1.0.5 fix: an engaged enemy arriving ends a return under way", PinEndsReturn);
        Run("1.0.4 fix: deer are watched, never fought", DeerNeverFought);
        Run("Rule: no fight starts until a creature commits to the party", NoUnprovokedFight);
        Run("Rule: a creature that picks one of us is fought before it lands a hit", AggroIsEnough);
        Run("Rule: during a fight, other alerted hostiles in sight join the list", EncounterWidens);
        Run("Rule: leader sneaking = only what already committed", SneakOnlyCommitted);
        Run("1.0.4 fix: what is hitting me beats a nearer creature hitting someone else", OnMeFirst);
        Run("1.0.4 fix: keep the current target; no hopping between near-equal enemies", KeepCurrent);
        Run("1.0.4 fix: a fleeing creature beyond 30 m is dropped, not chased", BeyondRangeDropped);
        Run("Rule: unseen alerted hostiles are not picked mid-fight (no wallhack)", NoWallhack);

        Console.WriteLine();
        Console.WriteLine("Regression ledger: " + passed + " passed, " + failed + " failed.");
        foreach (string f in failures) Console.WriteLine("  FAIL " + f);
        return failed == 0 ? 0 : 1;
    }

    static void Run(string name, Action test)
    {
        try { test(); passed++; Console.WriteLine("  pass " + name); }
        catch (Exception e) { failed++; failures.Add(name + " :: " + e.Message); Console.WriteLine("  FAIL " + name + " :: " + e.Message); }
    }

    static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }

    static PhaseInputs Calm()
    {
        return new PhaseInputs { AppearanceReady = true, HasLeader = true, LeaderDistance = 3f, NearestPartyDistance = 3f, FormationDistance = 1f };
    }

    static NpcPhase StepN(PhaseArbiter a, PhaseInputs s, int ticks)
    {
        NpcPhase p = a.Phase;
        for (int i = 0; i < ticks; i++) p = a.Step(s, Tuning.DecisionInterval);
        return p;
    }

    // --- Combat / cohesion ---

    static void RangedTargetIsNotSeparation()
    {
        var a = new PhaseArbiter();
        var s = Calm();
        s.HasTarget = true; s.LeaderDistance = 4.2f; s.NearestPartyDistance = 4f; // target itself 21.6 m away
        Assert(StepN(a, s, 50) == NpcPhase.Combat, "expected Combat, got " + a.Phase);
        Assert(!a.Cohesion.Returning, "cohesion latch fired on enemy range");
    }

    static void ReturningActorDefendsLocally()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true; s.LeaderDistance = 25f; s.NearestPartyDistance = 25f;
        Assert(StepN(a, s, Tuning.CohesionOutsideTicks + 1) == NpcPhase.CombatRegroup, "expected CombatRegroup, got " + a.Phase);
        s.LocalThreat = true; s.DamagedRecently = true;
        Assert(StepN(a, s, 1) == NpcPhase.Combat, "returning actor under close attack must fight, got " + a.Phase);
        Assert(!a.Cohesion.Returning, "1.0.5: an engaged enemy close by ends the return (no walking away mid-fight)");
        s.LocalThreat = false; s.DamagedRecently = false; s.HasTarget = true;
        Assert(StepN(a, s, 1) == NpcPhase.Combat, "1.0.5: no instant flip back to returning, got " + a.Phase);
        Assert(StepN(a, s, Tuning.CohesionOutsideTicks) == NpcPhase.CombatRegroup, "sustained separation with nothing close must still return, got " + a.Phase);
    }

    static void ReturningWithoutThreatRegroups()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true; s.LeaderDistance = 30f; s.NearestPartyDistance = -1f;
        StepN(a, s, Tuning.CohesionOutsideTicks + 5);
        Assert(a.Phase == NpcPhase.CombatRegroup, "got " + a.Phase);
    }

    static void SingleGapDoesNotEndCombat()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true;
        StepN(a, s, 5);
        s.HasTarget = false;
        Assert(StepN(a, s, 1) == NpcPhase.CombatClear, "a single gap must enter verification, got " + a.Phase);
        s.HasTarget = true;
        Assert(StepN(a, s, 1) == NpcPhase.Combat, "target back must be combat, got " + a.Phase);
    }

    static void ClearNeedsFiveSeconds()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true;
        StepN(a, s, 3);
        s.HasTarget = false;
        int ticks = (int)Math.Round(Tuning.ClearSeconds / Tuning.DecisionInterval);
        Assert(StepN(a, s, ticks - 2) == NpcPhase.CombatClear, "cleared too early: " + a.Phase);
        StepN(a, s, 3);
        Assert(a.Phase == NpcPhase.Regroup || a.Phase == NpcPhase.Calm, "did not clear after 5 s: " + a.Phase);
    }

    static void AlertedHostileResetsClear()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true;
        StepN(a, s, 2);
        s.HasTarget = false;
        StepN(a, s, 40);
        s.AlertedHostileNear = true;
        StepN(a, s, 1);
        Assert(a.QuietSeconds == 0f, "alerted hostile must reset the clear interval");
        s.AlertedHostileNear = false;
        Assert(StepN(a, s, 40) == NpcPhase.CombatClear, "must need a fresh full interval, got " + a.Phase);
    }

    static void DamageDuringClearReturnsToCombat()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true;
        StepN(a, s, 2);
        s.HasTarget = false;
        StepN(a, s, 20);
        s.DamagedRecently = true;
        Assert(StepN(a, s, 1) == NpcPhase.Combat, "damage must return to combat, got " + a.Phase);
    }

    static void UnarmedLeaderIsCalm()
    {
        var a = new PhaseArbiter();
        Assert(StepN(a, Calm(), 3) == NpcPhase.Calm, "got " + a.Phase);
    }

    static void AlertWithoutTargetIsReadiness()
    {
        var a = new PhaseArbiter();
        var s = Calm(); s.NativeAlert = true;
        Assert(StepN(a, s, 3) == NpcPhase.Readiness, "got " + a.Phase);
    }

    static void CohesionExitsOnlyInside()
    {
        var c = new CohesionLatch();
        for (int i = 0; i < Tuning.CohesionOutsideTicks; i++) c.Observe(true, 20f, 20f);
        Assert(c.Returning, "latch should have entered");
        c.Observe(true, 10f, 10f);
        Assert(c.Returning, "latch left between inner and outer boundary");
        c.Observe(true, Tuning.CohesionInnerLeader - .1f, Tuning.CohesionInnerParty - .1f);
        Assert(!c.Returning, "latch did not clear inside the inner area");
    }

    static void CohesionNeedsSustainedSeparation()
    {
        var c = new CohesionLatch();
        for (int i = 0; i < Tuning.CohesionOutsideTicks - 1; i++) c.Observe(true, 20f, 20f);
        c.Observe(true, 5f, 5f);
        for (int i = 0; i < Tuning.CohesionOutsideTicks - 1; i++) c.Observe(true, 20f, 20f);
        Assert(!c.Returning, "brief separations must not latch");
    }

    // --- Kernel scheduler ---

    static readonly NpcDomain[] RealClaims = {
        NpcDomain.All, NpcDomain.All, NpcDomain.Posture, NpcDomain.Posture,
        NpcDomain.Look, NpcDomain.Look, NpcDomain.Movement, NpcDomain.Hands
    };

    static void SchedulerIsExclusive()
    {
        var rng = new Random(7);
        var granted = new bool[RealClaims.Length];
        for (int trial = 0; trial < 5000; trial++)
        {
            var wants = new bool[RealClaims.Length];
            for (int i = 0; i < wants.Length; i++) wants[i] = rng.Next(2) == 1;
            DomainScheduler.Resolve(wants, RealClaims, granted);
            NpcDomain taken = NpcDomain.None;
            for (int i = 0; i < granted.Length; i++)
            {
                if (!granted[i]) continue;
                Assert((taken & RealClaims[i]) == 0, "domain granted twice in trial " + trial);
                taken |= RealClaims[i];
            }
        }
    }

    static void SchedulerPriority()
    {
        var wants = new bool[RealClaims.Length];
        for (int i = 0; i < wants.Length; i++) wants[i] = true;
        var granted = new bool[RealClaims.Length];
        DomainScheduler.Resolve(wants, RealClaims, granted);
        Assert(granted[0], "combat not granted");
        for (int i = 1; i < granted.Length; i++) Assert(!granted[i], "behavior " + i + " ran alongside combat");
    }

    static void SchedulerAllOrNothing()
    {
        var claims = new[] { NpcDomain.Movement, NpcDomain.Movement | NpcDomain.Hands, NpcDomain.Hands };
        var granted = new bool[3];
        DomainScheduler.Resolve(new[] { true, true, true }, claims, granted);
        Assert(granted[0] && !granted[1] && granted[2], "partial claim granted or hands lost");
    }

    // --- Torch ---

    static void TorchHysteresis()
    {
        var t = new TorchPolicy();
        float dt = .1f;
        for (int i = 0; i < 40; i++) t.Step(NpcPhase.Calm, true, false, false, false, float.PositiveInfinity, dt);
        Assert(t.On, "torch did not light in darkness");
        int changes = 0; bool last = t.On;
        for (int i = 0; i < 100; i++)
        {
            bool nearLight = (i / 5) % 2 == 0; // light flickers every half second
            t.Step(NpcPhase.Calm, true, nearLight, false, false, float.PositiveInfinity, dt);
            if (t.On != last) { changes++; last = t.On; }
        }
        Assert(changes == 0, "torch flickered " + changes + " times");
    }

    static void TorchOffInCombat()
    {
        foreach (NpcPhase p in new[] { NpcPhase.Combat, NpcPhase.CombatClear, NpcPhase.CombatRegroup, NpcPhase.Readiness })
        {
            var t = new TorchPolicy();
            for (int i = 0; i < 40; i++) t.Step(NpcPhase.Calm, true, false, false, false, float.PositiveInfinity, .1f);
            t.Step(p, true, false, false, false, float.PositiveInfinity, .1f);
            Assert(!t.On, "torch stayed lit in " + p);
        }
    }

    static void TorchWaitsAfterCombat()
    {
        var t = new TorchPolicy();
        for (int i = 0; i < 40; i++) t.Step(NpcPhase.Calm, true, false, false, false, 1f, .1f);
        Assert(!t.On, "torch lit one second after combat");
    }

    static void TorchOffInStealth()
    {
        var t = new TorchPolicy();
        for (int i = 0; i < 40; i++) t.Step(NpcPhase.Calm, true, false, false, false, float.PositiveInfinity, .1f);
        t.Step(NpcPhase.Calm, true, false, true, false, float.PositiveInfinity, .1f);
        Assert(!t.On, "torch stayed lit while leader sneaks");
    }

    static void BuiltLightHysteresis()
    {
        Assert(BuiltLight.Near(false, 5.9f), "6 m entry");
        Assert(!BuiltLight.Near(false, 8f), "8 m entered without prior");
        Assert(BuiltLight.Near(true, 9.9f), "left before 10 m");
        Assert(!BuiltLight.Near(true, 10.1f), "held past 10 m");
    }

    // --- Guard coverage ---

    static void GuardPrefersOpposite()
    {
        var g = new GuardCoverage();
        g.Resolve(2, 1d);
        Assert(g.Choose(0, 0) == GuardCoverage.Opposite(2), "opposite not preferred");
    }

    static void GuardAvoidsTeammate()
    {
        var g = new GuardCoverage();
        for (int mask = 1; mask < (1 << GuardCoverage.SectorCount) - 1; mask += 37)
        {
            int chosen = g.Choose(mask, 3);
            Assert(chosen >= 0 && (mask & (1 << chosen)) == 0, "picked an occupied sector for mask " + mask);
        }
    }

    // --- Identity ---

    static void BeardsByModel()
    {
        var rng = new Random(11);
        for (int i = 0; i < 2000; i++)
        {
            NpcIdentityRecord r = NpcIdentityRecord.Create(rng, 34, 22);
            Assert(r.Female ? r.Beard.Length == 0 : r.Beard.StartsWith("Beard"), "bad beard for " + r.Name);
        }
    }

    static void ShieldPairsWithOneHanded()
    {
        Assert(CombatHands.ShouldPairShield(CombatHands.Kind.OneHanded, false), "sword with free off hand must take the shield");
    }

    static void ShieldNeverWrongPair()
    {
        Assert(!CombatHands.ShouldPairShield(CombatHands.Kind.OneHanded, true), "off hand already used");
        Assert(!CombatHands.ShouldPairShield(CombatHands.Kind.Ranged, false), "bow takes both hands");
        Assert(!CombatHands.ShouldPairShield(CombatHands.Kind.TwoHanded, false), "two-hander");
        Assert(!CombatHands.ShouldPairShield(CombatHands.Kind.Torch, false), "torch is not a weapon");
        Assert(!CombatHands.ShouldPairShield(CombatHands.Kind.None, false), "empty hands");
    }

    static void BowMinimumRange()
    {
        Assert(CombatHands.IsRanged(CombatHands.Kind.Ranged) && !CombatHands.IsRanged(CombatHands.Kind.OneHanded), "ranged classification");
        Assert(CombatHands.RangedMinimum >= 6f && CombatHands.RangedMinimum <= 12f, "bow minimum outside the sane 6-12 m band");
    }

    static List<ThreatObservation> One(float d, bool attacking, bool me, bool ranged, bool front)
    {
        return new List<ThreatObservation> { new ThreatObservation { Distance = d, Attacking = attacking, TargetsMe = me, Ranged = ranged, InFront = front } };
    }

    static bool Run(BlockPolicy b, float from, float to, bool shield, bool busy, List<ThreatObservation> t)
    {
        bool r = false;
        for (float now = from; now <= to + 1e-4f; now += .1f) r = b.Step(now, shield, busy, t);
        return r;
    }

    static void BlockAfterReaction()
    {
        var b = new BlockPolicy(.2f);
        var t = One(2f, true, true, false, true);
        Assert(!b.Step(0f, true, false, t), "raised with zero reaction time");
        Assert(Run(b, .1f, .3f, true, false, t), "not raised after reaction delay");
    }

    static void BlockNeedsShield()
    {
        var b = new BlockPolicy(.12f);
        Assert(!Run(b, 0f, 1f, false, false, One(2f, true, true, false, true)), "blocked without a shield");
    }

    static void BlockNeverWhileBusy()
    {
        var b = new BlockPolicy(.12f);
        Assert(!Run(b, 0f, 1f, true, true, One(2f, true, true, false, true)), "blocked during own attack");
    }

    static void BlockMeleeCloseAndFront()
    {
        Assert(!Run(new BlockPolicy(.12f), 0f, 1f, true, false, One(6f, true, true, false, true)), "melee block at 6 m");
        Assert(!Run(new BlockPolicy(.12f), 0f, 1f, true, false, One(2f, true, true, false, false)), "melee block facing away");
        Assert(Run(new BlockPolicy(.12f), 0f, .5f, true, false, One(3f, true, true, false, true)), "no block at 3 m in front");
    }

    static void BlockProjectileTargeted()
    {
        Assert(Run(new BlockPolicy(.12f), 0f, .5f, true, false, One(20f, true, true, true, false)), "no block vs aimed arrow at 20 m");
        Assert(!Run(new BlockPolicy(.12f), 0f, 1f, true, false, One(40f, true, true, true, true)), "block vs arrow beyond 30 m");
    }

    static void BlockIgnoresOthers()
    {
        Assert(!Run(new BlockPolicy(.12f), 0f, 1f, true, false, One(2f, true, false, false, true)), "blocked an attack aimed at someone else");
    }

    static void BlockHoldAndCooldown()
    {
        var b = new BlockPolicy(.12f);
        var t = One(2f, true, true, false, true);
        Run(b, 0f, .3f, true, false, t);
        Assert(b.Raised, "not raised");
        Run(b, .4f, 1.3f, true, false, t);
        Assert(!b.Raised, "held past the maximum");
        Assert(!b.Step(1.4f, true, false, t), "re-raised inside the cooldown");
        Assert(Run(b, 1.5f, 2.2f, true, false, t), "never re-raised after cooldown");
    }

    static void BlockLowersWhenOver()
    {
        var b = new BlockPolicy(.12f);
        Run(b, 0f, .3f, true, false, One(2f, true, true, false, true));
        Assert(!b.Step(.4f, true, false, One(2f, false, true, false, true)), "stayed up after the attack ended");
    }

    static void HairAlwaysChosen()
    {
        var rng = new Random(13);
        for (int i = 0; i < 2000; i++)
        {
            NpcIdentityRecord r = NpcIdentityRecord.Create(rng, 34, 22);
            int n;
            Assert(r.Hair.StartsWith("Hair") && int.TryParse(r.Hair.Substring(4), out n) && n >= 1 && n <= 34, "bad hair " + r.Hair);
        }
    }

    static TargetCandidate Foe(int id, float dist) { return new TargetCandidate { Id = id, Distance = dist, CanSee = true, Health01 = 1f, PartnerDistance = -1f, Alerted = true }; }

    static int Pick(bool encounter, bool sneak, int current, params TargetCandidate[] c)
    {
        string reason;
        int i = TargetPolicy.Choose(c, current, encounter, sneak, out reason);
        return i < 0 ? -1 : c[i].Id;
    }

    static void DeerNeverFought()
    {
        var deer = Foe(1, 12f); deer.Wildlife = true;
        Assert(Pick(false, false, -1, deer) == -1, "fought a deer in calm");
        Assert(Pick(true, false, -1, deer) == -1, "fought a fleeing deer during a fight");
    }

    static void NoUnprovokedFight()
    {
        Assert(Pick(false, false, -1, Foe(1, 15f)) == -1, "started a fight with a creature that chose nobody");
    }

    static void AggroIsEnough()
    {
        var g = Foe(1, 15f); g.TargetsLeader = true; g.Alerted = false;
        Assert(Pick(false, false, -1, g) == 1, "waited for the leader to be hit");
        var h = Foe(2, 15f); h.TargetsPartner = true;
        Assert(Pick(false, false, -1, h) == 2, "ignored the creature after the partner");
    }

    static void EncounterWidens()
    {
        var shaman = Foe(1, 20f);
        Assert(Pick(true, false, -1, shaman) == 1, "ignored an alerted shaman mid-fight");
        shaman.Alerted = false;
        Assert(Pick(true, false, -1, shaman) == -1, "attacked an unaware creature mid-fight");
    }

    static void SneakOnlyCommitted()
    {
        var boar = Foe(1, 10f);
        Assert(Pick(true, true, -1, boar) == -1, "broke stealth on an uncommitted creature");
        boar.TargetsMe = true;
        Assert(Pick(true, true, -1, boar) == 1, "did not answer a creature that found us");
    }

    static void OnMeFirst()
    {
        var hitter = Foe(1, 3f); hitter.HurtMe = true;
        var other = Foe(2, 2f); other.TargetsLeader = true;
        Assert(Pick(true, false, 2, hitter, other) == 1, "ran off from the creature hitting it");
    }

    static void KeepCurrent()
    {
        var a = Foe(1, 10f); a.TargetsLeader = true;
        var b = Foe(2, 6f); b.TargetsLeader = true;
        Assert(Pick(true, false, 1, a, b) == 1, "hopped to a nearer target");
        Assert(Pick(true, false, -1, a, b) == 2, "fresh pick was not the nearest");
    }

    static void BeyondRangeDropped()
    {
        var neck = Foe(1, 35f); neck.TargetsMe = true;
        Assert(Pick(true, false, 1, neck) == -1, "kept a target beyond 30 m");
    }

    static void NoWallhack()
    {
        var hidden = Foe(1, 12f); hidden.CanSee = false;
        Assert(Pick(true, false, -1, hidden) == -1, "picked an alerted hostile it cannot see");
    }

    static void PinnedNeverReturns()
    {
        var a = new PhaseArbiter();
        var s = new PhaseInputs { AppearanceReady = true, HasLeader = true, HasTarget = true, LeaderDistance = 25f,
            NearestPartyDistance = 3f, LocalThreat = true };
        for (int i = 0; i < 50; i++) Assert(a.Step(s, .1f) == NpcPhase.Combat, "left a fight with an enemy on it");
    }

    static void PinEndsReturn()
    {
        var a = new PhaseArbiter();
        var s = new PhaseInputs { AppearanceReady = true, HasLeader = true, HasTarget = true, LeaderDistance = 25f, NearestPartyDistance = 3f };
        for (int i = 0; i < 20; i++) a.Step(s, .1f);
        Assert(a.Phase == NpcPhase.CombatRegroup, "setup: should be returning");
        s.LocalThreat = true;
        Assert(a.Step(s, .1f) == NpcPhase.Combat, "kept walking away from an enemy that engaged it");
        s.LocalThreat = false;
        Assert(a.Step(s, .1f) == NpcPhase.Combat, "return resumed instantly instead of re-measuring separation");
    }

    static void FallBackStarts()
    {
        var p = new FallBackPolicy();
        Assert(p.Step(0f, true, .20f, 2f), "badly hurt with an enemy at 2 m did not fall back");
    }

    static void FallBackNeedsBoth()
    {
        var p = new FallBackPolicy();
        Assert(!p.Step(0f, true, .80f, 2f), "fell back while healthy");
        Assert(!p.Step(0f, true, .20f, 9f), "fell back with nothing close");
        Assert(!p.Step(0f, false, .20f, 2f), "fell back outside a fight");
    }

    static void FallBackEndsClear()
    {
        var p = new FallBackPolicy(); p.Step(0f, true, .2f, 2f);
        Assert(!p.Step(2f, true, .22f, 10.5f) && p.LastOutcome.StartsWith("clear"), "did not stop at bow range: " + p.LastOutcome);
    }

    static void FallBackEndsCaught()
    {
        var p = new FallBackPolicy(); p.Step(0f, true, .2f, 2f);
        Assert(p.Step(.5f, true, .2f, 2f), "caught inside the grace second ended it (enemy starts adjacent)");
        Assert(!p.Step(1.5f, true, .2f, 2f) && p.LastOutcome.StartsWith("caught"), "kept running while caught: " + p.LastOutcome);
    }

    static void FallBackEndsTime()
    {
        var p = new FallBackPolicy(); p.Step(0f, true, .2f, 2f);
        Assert(p.Step(5.9f, true, .2f, 5f), "ended early");
        Assert(!p.Step(6.0f, true, .2f, 5f) && p.LastOutcome.StartsWith("time"), "retreat had no time cap: " + p.LastOutcome);
    }

    static void FallBackEndsHealed()
    {
        var p = new FallBackPolicy(); p.Step(0f, true, .2f, 2f);
        Assert(!p.Step(3f, true, .61f, 4f) && p.LastOutcome == "recovered", "did not return when healed");
    }

    static void FallBackLimits()
    {
        var p = new FallBackPolicy();
        float t = 0f;
        for (int i = 0; i < 3; i++)
        {
            Assert(p.Step(t, true, .2f, 2f), "fall-back " + (i + 1) + " refused");
            t += 6f; p.Step(t, true, .2f, 5f);             // time cap
            Assert(!p.Step(t + 1f, true, .2f, 2f), "no cooldown after fall-back " + (i + 1));
            t += 5.5f;
        }
        Assert(!p.Step(t, true, .2f, 2f) && p.LastStand, "a 4th fall-back in one fight");
    }

    static void FallBackResets()
    {
        var p = new FallBackPolicy();
        float t = 0f;
        for (int i = 0; i < 3; i++) { p.Step(t, true, .2f, 2f); t += 6f; p.Step(t, true, .2f, 5f); t += 5.5f; }
        p.Step(t, false, .2f, 99f);
        Assert(p.Step(t + 6f, true, .2f, 2f), "last stand carried over into the next fight");
    }

    static WeaponChoice.Pick W(bool holdingBow, bool holdingMelee, float d, bool flying, bool ammo)
    { return WeaponChoice.Choose(true, ammo, true, holdingBow, holdingMelee, d, flying); }

    static void FlyingGetsBow()
    {
        Assert(W(false, true, 3f, true, true) == WeaponChoice.Pick.Bow, "sword against a drake at 3 m");
        Assert(W(false, true, 15f, true, true) == WeaponChoice.Pick.Bow, "sword against a drake at 15 m");
    }

    static void GroundByDistance()
    {
        Assert(W(false, true, 18f, false, true) == WeaponChoice.Pick.Bow, "sword at 18 m");
        Assert(W(true, false, 3f, false, true) == WeaponChoice.Pick.Melee, "bow at 3 m");
    }

    static void WeaponHysteresis()
    {
        Assert(W(true, false, 8f, false, true) == WeaponChoice.Pick.Keep, "dropped the bow at 8 m");
        Assert(W(false, true, 8f, false, true) == WeaponChoice.Pick.Keep, "dropped the sword at 8 m");
    }

    static void NoAmmoMelee()
    {
        Assert(W(true, false, 20f, true, false) == WeaponChoice.Pick.Melee, "kept an empty bow");
    }

    // ---- 1.0.32.8 party spacing. A tiny native-Follow model: stop inside 3 m of the anchor, else walk 2 m/s at it.
    sealed class OpenGround : ISpacingTerrain { public bool Clear(P2 a, P2 b) { return true; } }
    sealed class Corridor : ISpacingTerrain
    {   // a 1 m wide corridor along z = 0: any lane that leaves it is blocked
        public bool Clear(P2 a, P2 b) { P2 d = b - a; float l = d.Length; if (l < .05f) return true; P2 end = a + d * (Math.Min(l, 3f) / l); return Math.Abs(end.Z) < .5f; }
    }
    sealed class WallOnLeft : ISpacingTerrain
    {   // nothing is standable at x < -0.3 (relative to a walk along +z the left side is -x)
        public bool Clear(P2 a, P2 b) { P2 d = b - a; float l = d.Length; if (l < .05f) return true; P2 end = a + d * (Math.Min(l, 3f) / l); return end.X > -.3f; }
    }

    sealed class Walker
    {
        public P2 Pos; public P2 Goal; public int Slot, SlotSide; public PartySpacing Law = new PartySpacing();
        public SpacingResult Last; public float Speed; public int Enters, Waits, SideFlips; int lastSide;
        public float MaxStepOffset;
        public void Tick(float now, float dt, SpacingNeighbor[] near, int n, ISpacingTerrain terrain, bool standing, bool menu)
        {
            P2 walk = Goal - Pos; float l = walk.Length;
            P2 aim = l > PartySpacing.HoldEnter ? Goal + walk * (2.6f / l) : Goal;   // TraversalBehavior.Beyond
            float before = Last.Offset;
            Last = Law.Step(now, dt, Pos, Speed, aim, Goal, standing, menu, SlotSide, Slot, near, n, terrain);
            MaxStepOffset = Math.Max(MaxStepOffset, Math.Abs(Last.Offset - before));
            if (Last.Event == "enter") Enters++;
            if (Last.Mode == SpacingMode.Wait) Waits++;
            if (Last.Side != 0 && lastSide != 0 && Last.Side != lastSide && Last.Event != "enter") SideFlips++;
            if (Last.Side != 0) lastSide = Last.Side;
            P2 anchor = Last.Mode == SpacingMode.Hold || Last.Mode == SpacingMode.Wait ? Pos
                : Last.Mode == SpacingMode.BackOff ? Last.Point + Last.Shift * 3.2f
                : Last.Mode == SpacingMode.Sidestep ? Last.Point + (Last.Point - Pos).Normalized * 3.6f : aim + Last.Shift;
            P2 to = anchor - Pos; float d = to.Length;
            if (d < 3f) { Speed = 0f; return; }                                     // BaseAI.Follow: StopMoving inside 3 m
            P2 step = to * (Math.Min(2f * dt, d) / d); Pos = Pos + step; Speed = 2f;
        }
    }

    static SpacingNeighbor Leader(P2 at) { return new SpacingNeighbor { Id = 0, IsLeader = true, Position = at, Slot = -1 }; }
    static SpacingNeighbor Mate(int id, P2 at, int slot) { return new SpacingNeighbor { Id = id, Position = at, Slot = slot }; }

    static void SpacingLeaderBlocksRoute()
    {   // companion in front of a standing leader, its spot directly behind him: the straight walk goes through him
        var w = new Walker { Pos = new P2(0f, 5f), Goal = new P2(0f, -3.2f), Slot = 0, SlotSide = 1 };
        var near = new[] { Leader(new P2(0f, 0f)) };
        float closest = 99f;
        for (int i = 0; i < 150; i++) { w.Tick(i * .1f, .1f, near, 1, new OpenGround(), true, false); closest = Math.Min(closest, w.Pos.Length); }
        Assert(w.Enters >= 1 && w.Last.Mode != SpacingMode.Wait, "no sidestep around the leader");
        Assert(closest >= PartySpacing.ContactLeader + .2f, "came within " + closest.ToString("0.00") + " m of the leader");
        Assert(P2.Distance(w.Pos, w.Goal) <= PartySpacing.HoldExit, "never reached its spot: " + P2.Distance(w.Pos, w.Goal).ToString("0.00") + " m");
        Assert(w.SideFlips == 0, "side flipped " + w.SideFlips + " times");
    }

    static void SpacingCompanionBlocksCompanion()
    {
        var w = new Walker { Pos = new P2(0f, 6f), Goal = new P2(0f, -2f), Slot = 1, SlotSide = -1 };
        var near = new[] { Leader(new P2(20f, 0f)), Mate(7, new P2(0f, 2f), 0) };
        float closest = 99f;
        for (int i = 0; i < 150; i++) { w.Tick(i * .1f, .1f, near, 2, new OpenGround(), true, false); closest = Math.Min(closest, P2.Distance(w.Pos, near[1].Position)); }
        Assert(w.Enters >= 1, "walked straight into the standing companion");
        Assert(closest >= PartySpacing.ContactMate + .2f, "came within " + closest.ToString("0.00") + " m of the companion");
        Assert(P2.Distance(w.Pos, w.Goal) <= PartySpacing.HoldExit, "never reached its spot");
    }

    static void SpacingNarrowCorridor()
    {   // two companions meet head-on in a 1 m corridor: no lane either side
        var a = new Walker { Pos = new P2(-6f, 0f), Goal = new P2(8f, 0f), Slot = 0, SlotSide = 1 };
        var b = new Walker { Pos = new P2(6f, 0f), Goal = new P2(-8f, 0f), Slot = 3, SlotSide = -1 };
        var t = new Corridor();
        var na = new SpacingNeighbor[2]; var nb = new SpacingNeighbor[2];
        for (int i = 0; i < 40; i++)
        {
            na[0] = Leader(new P2(0f, 30f)); na[1] = Mate(2, b.Pos, 3); na[1].Velocity = new P2(-b.Speed, 0f);
            nb[0] = Leader(new P2(0f, 30f)); nb[1] = Mate(1, a.Pos, 0); nb[1].Velocity = new P2(a.Speed, 0f);
            a.Tick(i * .1f, .1f, na, 2, t, false, false);
            b.Tick(i * .1f, .1f, nb, 2, t, false, false);
        }
        Assert(a.Waits == 0, "the lower slot waited " + a.Waits + " ticks");
        Assert(b.Waits > 0, "the higher slot never gave way");
        Assert(a.SideFlips == 0 && b.SideFlips == 0, "sidestep side oscillated");
        Assert(a.Last.Offset == 0f || a.Last.Mode != SpacingMode.Sidestep, "sidestepped out of the corridor");
    }

    static void SpacingHoldsInsideRegion()
    {   // standing in its region, well spaced: the spot centre wobbling (leader shuffling) must not move it
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(0f, 0f)), Mate(5, new P2(4f, -3f), 1) };
        P2 me = new P2(-1.5f, -2.3f);
        for (int i = 0; i < 60; i++)
        {
            P2 goal = new P2(-1.65f + (float)Math.Sin(i) * .6f, -2.75f + (float)Math.Cos(i) * .6f);
            SpacingResult r = law.Step(i * .1f, .1f, me, 0f, goal, goal, true, false, 1, 0, near, 2, new OpenGround());
            Assert(r.Mode == SpacingMode.Hold, "tick " + i + ": left its region for the mathematical centre (" + r.Mode + ")");
        }
        SpacingResult far = law.Step(7f, .1f, me, 0f, new P2(-1.65f, -5f), new P2(-1.65f, -5f), true, false, 1, 0, near, 2, new OpenGround());
        Assert(far.Mode != SpacingMode.Hold, "held even though its spot is 2.7 m away");
    }

    static void SpacingBriefProximityIgnored()
    {
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(30f, 0f)), Mate(4, new P2(0f, 2f), 1) };
        P2 me = new P2(0f, 0f), aim = new P2(0f, 8f);
        SpacingResult r = law.Step(0f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, 2, new OpenGround());
        r = law.Step(.1f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, 2, new OpenGround());
        Assert(r.Event != "enter" && r.Mode == SpacingMode.Travel, "sidestepped after 0.1 s of proximity");
        r = law.Step(.2f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, 1, new OpenGround());   // gone
        r = law.Step(.3f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, 2, new OpenGround());   // back again: timer restarts
        Assert(r.Mode == SpacingMode.Travel, "a flicker of proximity started an episode");
    }

    static void SpacingReleasesSmoothly()
    {
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(30f, 0f)), Mate(4, new P2(-.6f, 3f), 1) };   // -x = left of a walk along +z, outside the head-on band
        P2 me = new P2(0f, 0f), aim = new P2(0f, 10f);
        int side = 0; float peak = 0f, last = 0f; bool released = false, monotone = true;
        for (int i = 0; i < 60; i++)
        {
            int n = i < 25 ? 2 : 1;                                   // the companion walks away at 2.5 s
            SpacingResult r = law.Step(i * .1f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, n, new OpenGround());
            if (r.Side != 0) { if (side == 0) side = r.Side; Assert(r.Side == side, "side changed mid-episode"); }
            if (r.Event == "release") released = true;
            if (released && r.Offset > last + 1e-4f) monotone = false;
            Assert(Math.Abs(r.Offset - last) <= PartySpacing.RateIn * .1f + 1e-4f, "offset jumped " + (r.Offset - last).ToString("0.00") + " m in one tick");
            peak = Math.Max(peak, r.Offset); last = r.Offset;
        }
        Assert(side == -1, "obstacle slightly left of the line: must step right");
        Assert(peak >= PartySpacing.MinOffset && peak <= PartySpacing.MaxOffset, "offset out of bounds: " + peak);
        Assert(released && monotone && last == 0f, "release not smooth or not complete (last " + last + ")");
    }

    static void SpacingNoInvalidLane()
    {   // leader dead ahead, head-on; the formation side (left) is a wall: must not step left; right is fine
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(0f, 3f)) };
        P2 me = new P2(0f, 0f), aim = new P2(0f, 9f);
        SpacingResult r = default(SpacingResult);
        for (int i = 0; i < 5; i++) r = law.Step(i * .1f, .1f, me, 2f, aim, aim, false, false, 1, 0, near, 1, new WallOnLeft());
        Assert(r.Mode != SpacingMode.Sidestep || r.Point.X > 0f, "sidestepped into the wall");
        // both sides walled: wait for the leader and keep waiting while he blocks (1.0.32.8b: no hand-back into him)
        var boxed = new PartySpacing(); bool waited = false;
        for (int i = 0; i < 60; i++)
        {
            SpacingResult q = boxed.Step(i * .1f, .1f, me, waited ? 0f : 2f, aim, aim, false, false, 1, 0, near, 1, new Corridor0());
            Assert(q.Mode != SpacingMode.Sidestep, "sidestepped with no lane");
            if (q.Mode == SpacingMode.Wait) waited = true;
            Assert(!waited || q.Mode == SpacingMode.Wait, "tick " + i + ": handed back (" + q.Mode + ") while the leader still blocks");
        }
        Assert(waited, "never waited");
    }
    sealed class Corridor0 : ISpacingTerrain { public bool Clear(P2 a, P2 b) { P2 d = b - a; float l = d.Length; if (l < .05f) return true; P2 e = a + d * (Math.Min(l, 3f) / l); return Math.Abs(e.X) < .2f; } }

    // ---- 1.0.32.8b blocked corridor. Corridor0 walls both lanes (only |x| < 0.2 is standable off the walk line).
    static float Run1(Walker w, SpacingNeighbor[] near, int ticks, ISpacingTerrain t, out int afterBlockTravel, out int backOffs, out int transitions)
    {
        float closest = 99f; afterBlockTravel = 0; backOffs = 0; transitions = 0; bool blocked = false; SpacingMode last = SpacingMode.Travel;
        for (int i = 0; i < ticks; i++)
        {
            w.Tick(i * .1f, .1f, near, near.Length, t, false, false);
            if (w.Last.Mode == SpacingMode.Wait) blocked = true;
            if (blocked && w.Last.Mode == SpacingMode.Travel) afterBlockTravel++;
            if (w.Last.Mode == SpacingMode.BackOff) backOffs++;
            if (i > 20 && w.Last.Mode != last) transitions++;
            last = w.Last.Mode;
            foreach (var n in near) closest = Math.Min(closest, P2.Distance(w.Pos, n.Position));
        }
        return closest;
    }

    static void SpacingBlockedLeaderHeld()
    {
        var w = new Walker { Pos = new P2(0f, 0f), Goal = new P2(0f, 12f), Slot = 0, SlotSide = 1 };
        w.Law.SelfId = 10;
        int travel, back, trans;
        float closest = Run1(w, new[] { Leader(new P2(0f, 4f)) }, 80, new Corridor0(), out travel, out back, out trans);
        Assert(w.Waits > 15, "did not hold behind the leader (" + w.Waits + " wait ticks)");
        Assert(travel == 0, "resumed native Follow " + travel + " ticks while the leader still blocks");
        Assert(closest >= PartySpacing.LeaderClear, "entered the leader's clearance: " + closest.ToString("0.00") + " m");
        Assert(back == 0 && w.SideFlips == 0, "back-off or side flipping while held");
    }

    static void SpacingBlockedCompanionHeld()
    {
        foreach (int blockerSlot in new[] { 0, 3 })   // blocker has right of way (we yield) / we have right of way (stop short)
        {
            var w = new Walker { Pos = new P2(0f, 0f), Goal = new P2(0f, 12f), Slot = 1, SlotSide = -1 };
            w.Law.SelfId = 11;
            int travel, back, trans;
            var near = new[] { Leader(new P2(30f, 0f)), Mate(7, new P2(0f, 4f), blockerSlot) };
            float closest = Run1(w, near, 80, new Corridor0(), out travel, out back, out trans);
            Assert(closest >= PartySpacing.ContactMate + .2f, "slot " + blockerSlot + " blocker: came within " + closest.ToString("0.00") + " m");
            Assert(w.Last.Mode == SpacingMode.Wait, "slot " + blockerSlot + " blocker: not holding at the end (" + w.Last.Mode + ")");
            if (blockerSlot == 0) Assert(travel == 0, "yielder resumed walking into the blocker");
        }
    }

    static void SpacingYielderStill()
    {
        var w = new Walker { Pos = new P2(0f, 0f), Goal = new P2(0f, 12f), Slot = 2, SlotSide = 1 };
        w.Law.SelfId = 12;
        var near = new[] { Leader(new P2(30f, 0f)), Mate(5, new P2(0f, 3.5f), 0) };
        P2 stoppedAt = new P2(99f, 99f); int moved = 0;
        for (int i = 0; i < 80; i++)
        {
            w.Tick(i * .1f, .1f, near, 2, new Corridor0(), false, false);
            if (w.Last.Mode == SpacingMode.Wait)
            {
                if (stoppedAt.X == 99f) stoppedAt = w.Pos;
                else if (P2.Distance(stoppedAt, w.Pos) > .01f) moved++;
            }
        }
        Assert(stoppedAt.X != 99f && moved == 0, "the yielding companion crept forward " + moved + " ticks");
        Assert(w.SideFlips == 0, "side oscillated " + w.SideFlips + " times");
    }

    static void SpacingRightOfWayMoves()
    {   // head-on in a 1 m corridor: slot 0 has right of way, slot 3 gives way by backing out
        var a = new Walker { Pos = new P2(-6f, 0f), Goal = new P2(10f, 0f), Slot = 0, SlotSide = 1 };
        var b = new Walker { Pos = new P2(4f, 0f), Goal = new P2(-10f, 0f), Slot = 3, SlotSide = -1 };
        a.Law.SelfId = 20; b.Law.SelfId = 21;
        var t = new Corridor();
        var na = new SpacingNeighbor[2]; var nb = new SpacingNeighbor[2];
        float closest = 99f, startA = a.Pos.X; P2 lastA = a.Pos, lastB = b.Pos;
        for (int i = 0; i < 80; i++)
        {
            na[0] = Leader(new P2(0f, 30f)); na[1] = Mate(21, b.Pos, 3); na[1].Velocity = (b.Pos - lastB) * 10f;
            nb[0] = Leader(new P2(0f, 30f)); nb[1] = Mate(20, a.Pos, 0); nb[1].Velocity = (a.Pos - lastA) * 10f;
            lastA = a.Pos; lastB = b.Pos;
            a.Tick(i * .1f, .1f, na, 2, t, false, false);
            b.Tick(i * .1f, .1f, nb, 2, t, false, false);
            closest = Math.Min(closest, P2.Distance(a.Pos, b.Pos));
        }
        Assert(a.Pos.X - startA >= 6f, "right-of-way companion only advanced " + (a.Pos.X - startA).ToString("0.0") + " m");
        Assert(closest >= PartySpacing.ContactMate, "the two companions touched: " + closest.ToString("0.00") + " m");
        Assert(a.Waits == 0 || a.Pos.X - startA >= 6f, "right-of-way companion stuck waiting");
        Assert(a.SideFlips == 0 && b.SideFlips == 0, "side oscillation in the corridor");
    }

    static void SpacingResumesAfterClear()
    {
        var w = new Walker { Pos = new P2(0f, 0f), Goal = new P2(0f, 12f), Slot = 0, SlotSide = 1 };
        w.Law.SelfId = 13;
        var near = new[] { Leader(new P2(0f, 4f)) };
        bool held = false;
        for (int i = 0; i < 140; i++)
        {
            if (i == 40) near[0] = Leader(new P2(8f, 4f));      // the leader steps out of the corridor
            w.Tick(i * .1f, .1f, near, 1, new Corridor0(), false, false);
            if (i < 40 && w.Last.Mode == SpacingMode.Wait) held = true;
        }
        Assert(held, "never held while blocked");
        Assert(P2.Distance(w.Pos, w.Goal) <= 3.2f, "did not resume and arrive after the corridor cleared (" + P2.Distance(w.Pos, w.Goal).ToString("0.0") + " m short)");
    }

    static void SpacingUnreachableStable()
    {
        var w = new Walker { Pos = new P2(0f, 0f), Goal = new P2(0f, 8f), Slot = 1, SlotSide = -1 };
        w.Law.SelfId = 14;
        int travel, back, trans;
        float closest = Run1(w, new[] { Leader(new P2(0f, 5f)) }, 200, new Corridor0(), out travel, out back, out trans);
        Assert(back == 0, "backed off " + back + " ticks");
        Assert(trans <= 1, "mode changed " + trans + " times while the spot stayed unreachable");
        Assert(closest >= PartySpacing.LeaderClear, "reached into the leader: " + closest.ToString("0.00"));
    }

    static void SpacingCrowdingNeverRegroups()
    {
        Assert(!PartySpacing.CrowdingCallsRegroup(), "crowding is wired to regroup");
        // the largest correction spacing can make stays far inside the cohesion limits that drive regroup
        Assert(PartySpacing.MaxOffset + PartySpacing.MenuLeaderClear < Tuning.CohesionInnerLeader, "spacing correction can reach the cohesion limit");
        // a crowded calm party stays calm in the phase law (crowding is not an input to it)
        var input = Calm(); input.LeaderDistance = 1.2f; input.NearestPartyDistance = .8f; input.FormationDistance = 1.5f;
        var arbiter = new PhaseArbiter();
        NpcPhase p = StepN(arbiter, input, 100);
        Assert(p == NpcPhase.Calm, "a crowded calm party left Calm: " + p);
    }

    static void SpacingFormationTravelQuiet()
    {   // two companions in the rear slots while the leader walks / runs and weaves: nobody is being closed on
        foreach (float speed in new[] { 2f, 4f, 7f })
        {
            var laws = new[] { new PartySpacing(), new PartySpacing() };
            var pos = new[] { new P2(-1.65f, -2.75f), new P2(1.65f, -2.75f) };
            for (int i = 0; i < 300; i++)
            {
                float now = i * .1f;
                P2 l = new P2((float)Math.Sin(now * .7f) * 1.2f, speed * now);
                for (int k = 0; k < 2; k++)
                {
                    var near = new[] { new SpacingNeighbor { Id = 0, IsLeader = true, Position = l, Velocity = new P2(0f, speed) },
                        new SpacingNeighbor { Id = 2 - k, Position = pos[1 - k], Slot = 1 - k, Velocity = new P2(0f, speed) } };
                    P2 slot = l + new P2(k == 0 ? -1.65f : 1.65f, -2.75f), aim = slot + new P2(0f, 3.25f);
                    SpacingResult r = laws[k].Step(now, .1f, pos[k], speed, aim, slot, false, false, k == 0 ? 1 : -1, k, near, 2, new OpenGround());
                    Assert(r.Mode == SpacingMode.Travel && r.Event == null, "formation travel at " + speed + " m/s triggered " + r.Mode + " " + r.Event + " at " + now.ToString("0.0") + " s");
                    P2 to = aim - pos[k]; float d = to.Length;
                    if (d >= 3f) pos[k] = pos[k] + to * (Math.Min(speed * 1.3f * .1f, d) / d);
                }
            }
        }
    }

    static void SpacingMenuNeverPushes()
    {
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(0f, 0f)) };
        P2 me = new P2(1.5f, 0f);
        SpacingResult r = law.Step(0f, .1f, me, 0f, new P2(-1.65f, -2.75f), new P2(-1.65f, -2.75f), true, true, 1, 0, near, 1, new OpenGround());
        Assert(r.Mode == SpacingMode.BackOff && P2.Distance(r.Point, near[0].Position) >= PartySpacing.MenuLeaderClear, "in a menu at 1.5 m it did not ease back");
        P2 goal = PartySpacing.KeepGoalOffLeader(new P2(.5f, -.5f), near[0].Position, true);
        Assert(P2.Distance(goal, near[0].Position) >= PartySpacing.MenuLeaderClear, "a standing spot stayed inside the menu clearance");
        // not in a menu, 1.5 m away and standing still: no back-off (1.0.32.9: it starts inside 1.3 m)
        var calm = new PartySpacing();
        SpacingResult c = calm.Step(0f, .1f, me, 0f, me, me, true, false, 1, 0, near, 1, new OpenGround());
        Assert(c.Mode != SpacingMode.BackOff, "backed off with no menu and no contact");
        // a walker approaching a leader in a menu never gets inside the clearance
        var w = new Walker { Pos = new P2(0f, 6f), Goal = new P2(0f, -3.2f), Slot = 0, SlotSide = 1 };
        float closest = 99f;
        for (int i = 0; i < 150; i++) { w.Tick(i * .1f, .1f, near, 1, new OpenGround(), true, true); closest = Math.Min(closest, w.Pos.Length); }
        Assert(closest >= PartySpacing.ContactLeader + .5f, "came within " + closest.ToString("0.00") + " m of a leader in a menu");
    }


    // ---- 1.0.32.9 corrections from the 2026-10-28 live spacing test (DevSpacingLog + telemetry + OBS video).
    sealed class WallBehindLeft : ISpacingTerrain
    {   // nothing standable at x > 1.6: a companion east of the leader cannot back straight out
        public bool Clear(P2 a, P2 b) { return b.X <= 1.6f; }
    }

    static void SpacingLeaderBackOffBeforeContact()
    {
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(0f, 0f)) };
        P2 me = new P2(1.2f, 0f);
        SpacingResult r = law.Step(0f, .1f, me, 0f, me, me, false, false, 1, 0, near, 1, new OpenGround());
        Assert(r.Mode == SpacingMode.BackOff, "at 1.2 m it waited for contact (" + r.Mode + ")");
        Assert(P2.Distance(r.Point, near[0].Position) >= PartySpacing.LeaderClear, "backed off to less than the clearance");
        var w = new Walker { Pos = new P2(0f, 6f), Goal = new P2(0f, -1.0f), Slot = 0, SlotSide = 1 };
        float closest = 99f;
        for (int i = 0; i < 200; i++) { w.Tick(i * .1f, .1f, near, 1, new OpenGround(), true, false); closest = Math.Min(closest, w.Pos.Length); }
        Assert(closest >= PartySpacing.ContactLeader, "touched the leader: " + closest.ToString("0.00") + " m");
    }

    static void SpacingPressedMatesSeparate()
    {   // the indoor case: both standing spots on the same point, bodies at 0.98 m
        var a = new Walker { Pos = new P2(0f, 0f), Goal = new P2(.2f, 0f), Slot = 0, SlotSide = 1 };
        var b = new Walker { Pos = new P2(.98f, 0f), Goal = new P2(.2f, 0f), Slot = 2, SlotSide = -1 };
        b.Law.SelfId = 2; a.Law.SelfId = 1;
        var lead = Leader(new P2(0f, 6f));
        var na = new SpacingNeighbor[2]; var nb = new SpacingNeighbor[2];
        int aMoves = 0, bBackOffs = 0; SpacingMode lastB = SpacingMode.Travel; float minD = 99f, endD = 0f;
        for (int i = 0; i < 200; i++)
        {
            na[0] = lead; na[1] = Mate(2, b.Pos, 2); nb[0] = lead; nb[1] = Mate(1, a.Pos, 0);
            P2 before = a.Pos;
            a.Tick(i * .1f, .1f, na, 2, new OpenGround(), true, false);
            b.Tick(i * .1f, .1f, nb, 2, new OpenGround(), true, false);
            if (a.Last.Mode == SpacingMode.BackOff) aMoves++;
            if (b.Last.Mode == SpacingMode.BackOff && lastB != SpacingMode.BackOff) bBackOffs++;
            lastB = b.Last.Mode;
            endD = P2.Distance(a.Pos, b.Pos); if (i > 50) minD = Math.Min(minD, endD);
        }
        Assert(aMoves == 0, "the right-of-way companion backed off " + aMoves + " ticks");
        Assert(endD >= PartySpacing.MateBackOffEnter, "still pressed: " + endD.ToString("0.00") + " m");
        Assert(minD >= PartySpacing.MateBackOffEnter, "drifted back into contact after separating: " + minD.ToString("0.00") + " m");
        Assert(bBackOffs <= 2, "approach/back-off oscillation: " + bBackOffs + " back-off episodes");
    }

    static void SpacingBackOffAroundWall()
    {
        var law = new PartySpacing();
        var near = new[] { Leader(new P2(0f, 0f)) };
        P2 me = new P2(1.0f, 0f);
        SpacingResult r = law.Step(0f, .1f, me, 0f, me, me, false, false, 1, 0, near, 1, new WallBehindLeft());
        Assert(r.Mode == SpacingMode.BackOff, "with a wall behind it the companion stayed pressed (" + r.Mode + ")");
        Assert(r.Point.X <= 1.6f && P2.Distance(r.Point, near[0].Position) >= PartySpacing.LeaderClear, "backed off into the wall or not far enough");
    }

    static void SpacingGoalOffMate()
    {
        var law = new PartySpacing { SelfId = 9 };
        var near = new[] { Leader(new P2(0f, 8f)), Mate(1, new P2(0f, 0f), 0) };
        P2 me = new P2(0f, -5f), goal = new P2(.3f, 0f);
        SpacingResult r = law.Step(0f, .1f, me, 2f, goal, goal, true, false, -1, 2, near, 2, new OpenGround());
        P2 anchor = goal + r.Shift;
        Assert(r.Mode != SpacingMode.Travel || P2.Distance(anchor, near[1].Position) >= PartySpacing.MateClear, "aimed at a spot pressed against the other companion");
    }


    // ---- 1.0.32.10 gait matching (Brian 2026-10-28; live: companions stop-go at 0-4 m/s while he walked 0.8-2.6 m/s)
    static GaitChoice Settle(GaitPolicy p, float speed, bool toggle, float leaderD, float spotD, float jog)
    {
        GaitChoice g = new GaitChoice();
        for (int i = 0; i < 30; i++) g = p.Decide(.1f, speed, toggle, leaderD, spotD, jog);
        return g;
    }

    static void GaitWalksWithSlowLeader()
    {
        GaitChoice g = Settle(new GaitPolicy(), 1.5f, false, 3f, 1f, 5f);
        Assert(g.Walk && g.Matching, "did not walk with a 1.5 m/s leader");
        Assert(Math.Abs(g.Speed - 1.65f) < .05f, "walk speed " + g.Speed.ToString("0.00") + " instead of leader + 0.15");
    }

    static void GaitJogBands()
    {
        GaitChoice slow = Settle(new GaitPolicy(), 3f, false, 3f, 1f, 5f);
        Assert(!slow.Walk && slow.Matching && Math.Abs(slow.Speed - 3.15f) < .05f, "slow jog not matched: " + slow.Speed.ToString("0.00"));
        GaitChoice full = Settle(new GaitPolicy(), 5f, false, 3f, 1f, 5f);
        Assert(!full.Walk && !full.Matching && full.Speed == 5f, "a full jog was slowed");
    }

    static void GaitCatchUpBounded()
    {
        GaitChoice g = Settle(new GaitPolicy(), 1.5f, false, 7f, 7f, 5f);
        Assert(g.Speed > 1.65f + 1f, "fell behind its spot and did not speed up: " + g.Speed.ToString("0.00"));
        GaitChoice far = Settle(new GaitPolicy(), 4.5f, false, 7.5f, 20f, 5f);
        Assert(far.Speed <= 5f, "catch-up exceeded the native jog");
    }

    static void GaitToggleAndRange()
    {
        GaitChoice t = Settle(new GaitPolicy(), 2.5f, true, 3f, 1f, 5f);
        Assert(t.Walk, "leader has walk toggled at 2.5 m/s but the companion jogged");
        GaitChoice far = Settle(new GaitPolicy(), 1.5f, false, 9f, 9f, 5f);
        Assert(!far.Walk && !far.Matching && far.Speed == 5f, "slowed down while 9 m from the leader");
        GaitChoice still = Settle(new GaitPolicy(), 0f, false, 3f, 1f, 5f);
        Assert(still.Speed >= GaitPolicy.MinSpeed, "crawled below the minimum speed");
    }

    static void GaitNoFlicker()
    {
        var p = new GaitPolicy(); int flips = 0; bool last = false;
        for (int i = 0; i < 100; i++)
        {
            GaitChoice g = p.Decide(.1f, i % 2 == 0 ? 1.2f : 2.6f, false, 3f, 1f, 5f);
            if (i > 5 && g.Walk != last) flips++;
            last = g.Walk;
        }
        Assert(flips <= 2, "walk/jog flipped " + flips + " times on a noisy 1.2/2.6 m/s leader");
    }


    // ---- 1.0.32.12 throw planner (Souls Pillager throw, player-like decision)
    static ThrowBody Foe(float x, float z, float vx = 0f, float vz = 0f, float hp = 60f) { return new ThrowBody { Position = new P2(x, z), Velocity = new P2(vx, vz), MaxHealth = hp }; }
    static ThrowBody Friend(float x, float z, float vx = 0f, float vz = 0f) { return new ThrowBody { Position = new P2(x, z), Velocity = new P2(vx, vz) }; }
    static ThrowPlan PlanAt(ThrowBody[] h, ThrowBody[] f)
    {
        return ThrowPlanner.Plan(new P2(0f, 0f), 0f, new P2(0f, 1f), new P2(0f, -1f), h, h.Length, f, f.Length);
    }

    static void ThrowPrefersIncomingCluster()
    {   // a still pair at 7 m to the right, three coming at us from 12 m ahead
        var h = new[] { Foe(7f, 0f), Foe(7.5f, .5f), Foe(0f, 12f, 0f, -1.5f), Foe(.8f, 12.4f, 0f, -1.5f), Foe(-.8f, 12.3f, 0f, -1.5f) };
        ThrowPlan p = PlanAt(h, new[] { Friend(0f, -2f) });
        Assert(p.Throw && p.Members == 3 && p.Incoming == 3, "did not pick the incoming three (members " + p.Members + ", incoming " + p.Incoming + ", " + p.Reason + ")");
    }

    static void ThrowNeverNearFriends()
    {
        var h = new[] { Foe(0f, 10f), Foe(1f, 10f), Foe(-1f, 10.5f) };
        ThrowPlan near = PlanAt(h, new[] { Friend(0f, 6f) });       // leader 4 m from the group
        Assert(!near.Throw && near.Reason == "friend near the landing", "threw with a friend 4 m from the landing (" + near.Reason + ")");
        ThrowPlan walking = PlanAt(h, new[] { Friend(0f, 3f, 0f, 4f) });   // a companion running into the blast
        Assert(!walking.Throw, "threw where a companion will be when it lands");
        ThrowPlan clear = PlanAt(h, new[] { Friend(0f, -2f) });
        Assert(clear.Throw && clear.FriendClearance >= ThrowPlanner.BlastRadius + ThrowPlanner.FriendMargin, "a clear throw was refused or too close");
    }

    static void ThrowLeadsAndRange()
    {
        var moving = new[] { Foe(4f, 10f, -2f, 0f), Foe(4.5f, 10.5f, -2f, 0f) };
        ThrowPlan p = PlanAt(moving, new ThrowBody[0]);
        Assert(p.Throw && p.Lead > .5f && p.Aim.X < 4.2f, "did not aim ahead of a group walking left (lead " + p.Lead.ToString("0.0") + ")");
        Assert(!PlanAt(new[] { Foe(0f, 3f), Foe(.5f, 3f) }, new ThrowBody[0]).Throw, "threw at its feet");
        Assert(!PlanAt(new[] { Foe(0f, 20f), Foe(.5f, 20f) }, new ThrowBody[0]).Throw, "threw beyond reach");
    }

    static void ThrowBallistics()
    {
        foreach (float d in new[] { 6f, 9f, 13f })
        {
            float e = ThrowPlanner.Elevation(d, 0f);
            Assert(!float.IsNaN(e), "no solution at " + d + " m");
            float range = ThrowPlanner.Speed * ThrowPlanner.Speed * (float)Math.Sin(2 * e) / ThrowPlanner.Gravity;
            Assert(Math.Abs(range - d) < .05f, "elevation lands at " + range.ToString("0.00") + " m instead of " + d);
        }
        Assert(float.IsNaN(ThrowPlanner.Elevation(16f, 0f)), "claimed a solution beyond 14.4 m");
    }

    static void ThrowWorthAndCadence()
    {
        Assert(!PlanAt(new[] { Foe(0f, 9f) }, new ThrowBody[0]).Throw, "wasted a throw on one weak enemy");
        Assert(PlanAt(new[] { Foe(0f, 9f, 0f, 0f, 600f) }, new ThrowBody[0]).Throw, "would not throw at one troll-sized enemy");
        var c = new ThrowCadence { Cooldown = 10f };
        Assert(c.Ready(0f), "not ready at start"); c.Started(0f);
        Assert(!c.Ready(9.9f) && c.Ready(10f), "cooldown wrong");
        c.Failed(20f); Assert(c.Ready(22f), "a failed throw waited the full cooldown");
    }


    // ---- 1.0.32.13 body-reaction voice addon (poison/gas)
    static void BodyVoiceOnsetRepeatRecover()
    {
        var p = new BodyVoicePolicy(7); int onset = 0, repeat = 0, recover = 0; float lastRepeat = -99f, minGap = 99f;
        for (int i = 0; i < 200; i++)   // 20 s exposed, then clear
        {
            float t = i * .1f;
            BodyLine l = p.Step(t, true, false, float.NegativeInfinity);
            if (l == BodyLine.Onset) onset++;
            if (l == BodyLine.Repeat) { repeat++; if (lastRepeat > -99f) minGap = Math.Min(minGap, t - lastRepeat); lastRepeat = t; }
        }
        for (int i = 200; i < 230; i++) if (p.Step(i * .1f, false, false, float.NegativeInfinity) == BodyLine.Recover) recover++;
        Assert(onset == 1, "onset " + onset + " times");
        Assert(repeat >= 2 && repeat <= 5, "repeats in 20 s: " + repeat);
        Assert(minGap >= BodyVoicePolicy.RepeatMin - .11f, "repeats too close: " + minGap.ToString("0.0") + " s");
        Assert(recover == 1, "recovery breaths: " + recover);
    }

    static void BodyVoiceYieldsToCombat()
    {
        var p = new BodyVoicePolicy(1); int onset = 0;
        for (int i = 0; i < 25; i++) { float t = i * .1f; if (p.Step(t, true, false, t) == BodyLine.Onset) onset++; }   // combat line every tick
        Assert(onset == 0, "coughed over a combat line");
        var q = new BodyVoicePolicy(1); int late = 0;
        for (int i = 0; i < 40; i++) { float t = i * .1f; if (q.Step(t, true, false, t < 1f ? t : float.NegativeInfinity) == BodyLine.Onset) late++; }
        Assert(late == 1, "an onset deferred by a short combat line never played");
    }

    static void BodyVoiceDeathAndFlicker()
    {
        var p = new BodyVoicePolicy(3); int any = 0;
        for (int i = 0; i < 100; i++) if (p.Step(i * .1f, true, true, float.NegativeInfinity) != BodyLine.None) any++;
        Assert(any == 0, "spoke while dead");
        var q = new BodyVoicePolicy(3); int flick = 0;
        for (int i = 0; i < 20; i++) if (q.Step(i * .1f, i % 3 == 0, false, float.NegativeInfinity) != BodyLine.None) flick++;
        Assert(flick == 0, "a 0.1 s flicker counted as exposure");
    }

    // ---- 1.0.32.14 body states (burning, frost, long swim, exhaustion, environment recovery)
    static void BodyVoicePoisonLocked()
    {
        Assert(BodyVoicePolicy.Confirm == .3f && BodyVoicePolicy.RepeatMin == 4f && BodyVoicePolicy.RepeatMax == 7f, "poison timing changed");
        Assert(BodyVoicePolicy.CombatQuiet == 1.2f && BodyVoicePolicy.MinForRecover == 1.5f && BodyVoicePolicy.CombatDefer == 1.5f, "poison priority changed");
    }

    static void StateLastingOnsetRepeatRecover()
    {
        var p = new LastingStatePolicy(5, 6f, 9f, 3f); int on = 0, rep = 0, rec = 0; float last = -99f, gap = 99f;
        for (int i = 0; i < 300; i++)   // 30 s burning
        {
            float t = i * .1f; StateLine l = p.Step(t, true, false, true);
            if (l == StateLine.Onset) on++;
            if (l == StateLine.Repeat) { rep++; if (last > -99f) gap = Math.Min(gap, t - last); last = t; }
        }
        for (int i = 300; i < 320; i++) if (p.Step(i * .1f, false, false, true) == StateLine.Recover) rec++;
        Assert(on == 1, "onset " + on);
        Assert(rep >= 2 && rep <= 5, "repeats in 30 s: " + rep);
        Assert(gap >= 6f - .11f, "repeats too close: " + gap);
        Assert(rec == 1, "recoveries " + rec);
        var q = new LastingStatePolicy(5, 6f, 9f, 3f); int shortRec = 0;      // 2 s of burning: cry, no exhale
        for (int i = 0; i < 20; i++) q.Step(i * .1f, true, false, true);
        for (int i = 20; i < 40; i++) if (q.Step(i * .1f, false, false, true) == StateLine.Recover) shortRec++;
        Assert(shortRec == 0, "exhaled after a fleeting exposure");
        var n = new LastingStatePolicy(5, 0f, 0f, 3f); int nr = 0;            // RepeatMin 0 = no repeats
        for (int i = 0; i < 300; i++) if (n.Step(i * .1f, true, false, true) == StateLine.Repeat) nr++;
        Assert(nr == 0, "repeated with repeats off");
    }

    static void StateYieldsWhenNotQuiet()
    {
        var p = new LastingStatePolicy(2, 6f, 9f, 3f); int any = 0;
        for (int i = 0; i < 100; i++) if (p.Step(i * .1f, true, false, false) != StateLine.None) any++;
        Assert(any == 0, "spoke while not quiet");
        var q = new LastingStatePolicy(2, 6f, 9f, 3f); int on = 0;           // busy for 3 s: onset dropped, repeat later
        for (int i = 0; i < 30; i++) if (q.Step(i * .1f, true, false, false) == StateLine.Onset) on++;
        for (int i = 30; i < 40; i++) if (q.Step(i * .1f, true, false, true) == StateLine.Onset) on++;
        Assert(on == 0, "a dropped onset played late");
    }

    static void StateDeathAndFlicker()
    {
        var p = new LastingStatePolicy(4, 6f, 9f, 3f); int any = 0;
        for (int i = 0; i < 100; i++) if (p.Step(i * .1f, true, true, true) != StateLine.None) any++;
        Assert(any == 0, "spoke while dead");
        var q = new LastingStatePolicy(4, 6f, 9f, 3f); int fl = 0;
        for (int i = 0; i < 30; i++) if (q.Step(i * .1f, i % 3 == 0, false, true) != StateLine.None) fl++;
        Assert(fl == 0, "a flicker counted");
        var e = new EventStatePolicy(45f); int ev = 0;
        for (int i = 0; i < 10; i++) if (e.Step(i * .1f, true, true, true) != StateLine.None) ev++;
        Assert(ev == 0, "event voiced while dead");
    }

    static void SwimOnlyAfterLongSwim()
    {
        var s = new SwimTracker(20f, 1.5f); int fired = 0;
        for (int i = 0; i < 100; i++) if (s.Step(i * .1f, true, false)) fired++;           // 10 s swim
        for (int i = 100; i < 140; i++) if (s.Step(i * .1f, false, false)) fired++;
        Assert(fired == 0, "short swim earned a breath");
        var l = new SwimTracker(20f, 1.5f); int lf = 0; float at = -1f;
        for (int i = 0; i < 300; i++) { bool bob = i % 50 == 0 && i > 0; if (l.Step(i * .1f, !bob, false)) lf++; }   // 30 s, bobbing out briefly
        Assert(lf == 0, "bobbing out ended the swim");
        for (int i = 300; i < 340; i++) if (l.Step(i * .1f, false, false)) { lf++; at = i * .1f; }
        Assert(lf == 1, "long swim breaths: " + lf);
        Assert(at >= 30f + 1.4f && at <= 30f + 1.7f, "breath came at " + at);
    }

    static void ExhaustionEventCooldown()
    {
        var e = new EventStatePolicy(45f); int n = 0;
        if (e.Step(0f, true, false, true) == StateLine.Event) n++;
        if (e.Step(10f, true, false, true) == StateLine.Event) n++;          // inside cooldown
        if (e.Step(46f, true, false, true) == StateLine.Event) n++;
        Assert(n == 2, "exhaustion lines " + n);
        var d = new EventStatePolicy(45f); int late = 0;                       // cannot be voiced for 2 s: dropped
        d.Step(0f, true, false, false); d.Step(1f, false, false, false);
        if (d.Step(2f, false, false, true) == StateLine.Event) late++;
        Assert(late == 0, "a dropped event played late");
        var w = new EventStatePolicy(45f); int ok = 0;                         // busy 1 s: voiced when quiet
        w.Step(0f, true, false, false);
        if (w.Step(1f, false, false, true) == StateLine.Event) ok++;
        Assert(ok == 1, "a briefly deferred event never played");
    }

    static void ActorGateOneVoice()
    {
        var g = new BodyActorGate();
        Assert(g.Quiet(0f, float.NegativeInfinity), "fresh actor not quiet");
        g.Started(0f, 1.4f, false);
        Assert(!g.Quiet(1f, float.NegativeInfinity), "overlapped a sounding line");
        Assert(!g.Quiet(2f, float.NegativeInfinity), "min gap not held");
        Assert(g.Quiet(2.6f, float.NegativeInfinity), "stayed busy too long");
        Assert(!g.Quiet(3f, 2.5f), "spoke within 1.2 s of a combat line");
        g.Started(3f, 1f, true);
        Assert(!g.RecoverAllowed(20f) && g.RecoverAllowed(33f), "exhale cooldown wrong");
        g.Started(40f, 5f, false); g.Cut(41f);
        Assert(g.Quiet(42.6f, float.NegativeInfinity), "a cut line still held the voice");
    }

    static void ClipPickerNoRepeat()
    {
        var p = new ClipPicker(9); int prev = -1; var seen = new bool[4];
        for (int i = 0; i < 200; i++) { int c = p.Pick(4); Assert(c != prev, "same clip twice"); seen[c] = true; prev = c; }
        Assert(seen[0] && seen[1] && seen[2] && seen[3], "a clip never played");
        Assert(p.Pick(1) == 0 && p.Pick(0) == -1, "edge pools wrong");
    }

    // ---- 1.0.32.15 party clouds and hazard escape
    static void PartyCloudFractions()
    {
        const float f = .35f;
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Companion, true, false, true, true, f) == f, "own cloud on a companion");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Companion, false, false, true, true, f) == f, "partner's cloud");
        Assert(CloudExposure.Fraction(CloudActor.Player, CloudActor.Companion, false, false, true, true, f) == f, "leader's cloud on a companion");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Player, false, true, true, true, f) == f, "companion cloud on the leader");
        Assert(CloudExposure.Fraction(CloudActor.Player, CloudActor.Player, true, true, true, true, f) == f, "leader's own cloud");
        Assert(CloudExposure.Fraction(CloudActor.Player, CloudActor.Player, false, false, true, true, f) == 0f, "another network player must stay vanilla");
        Assert(CloudExposure.Fraction(CloudActor.Other, CloudActor.Companion, false, false, true, true, f) == 0f, "enemy cloud is native, not ours");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Other, false, false, true, true, f) == 0f, "enemy target is native");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Companion, true, false, false, true, f) == 0f, "companions switch off");
        Assert(CloudExposure.Fraction(CloudActor.Player, CloudActor.Player, true, true, true, false, f) == 0f, "player switch off");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Companion, true, false, true, true, 0f) == 0f, "fraction 0 = vanilla");
        Assert(CloudExposure.Fraction(CloudActor.Companion, CloudActor.Companion, true, false, true, true, 3f) == 1f, "fraction capped at 1");
    }

    static void PartyCloudHarmful()
    {
        Assert(CloudExposure.Harmful(40f, 0f, 10f), "ooze cloud");
        Assert(CloudExposure.Harmful(30f, 15f, 10f), "bile cloud");
        Assert(!CloudExposure.Harmful(0f, 0f, 10f), "no poison/fire");
        Assert(!CloudExposure.Harmful(40f, 0f, .5f), "an instant blast is not a cloud");
    }

    static void HazardEscapeLaw()
    {
        var p = new HazardEscapePolicy(); const float r = 4f;
        Assert(!p.Step(0f, 3f, r, false), "escaped without confirming");
        Assert(p.Step(.3f, 3f, r, false), "did not escape after confirm");
        Assert(p.Step(.6f, 4.8f, r, false), "stopped at the edge (needs margin)");
        Assert(!p.Step(.9f, 5.6f, r, false) && p.Outcome == "clear", "did not stop when clear");
        Assert(!p.Step(1f, 3f, r, false) && !p.Step(1.3f, 3f, r, false), "re-escaped inside the cooldown");
        Assert(p.Step(2.5f, 3f, r, false), "no escape after the cooldown");
        Assert(!p.Step(7.6f, 3f, r, false) && p.Outcome == "time", "pinned escape never gave up");
        var q = new HazardEscapePolicy();
        Assert(!q.Step(0f, 1f, r, true) && !q.Step(1f, 1f, r, true), "escaped while dead");
        Assert(!q.Step(2f, float.PositiveInfinity, r, false), "escaped with no cloud");
    }

    // ---- Combat-voice lock (approved 1.0.32.6, 2026-10-27). Companion combat module, combat voice: COMPLETE.
    // These values were tuned live by Brian. Changing any of them needs his approval and a new lock; never "tidy" them.
    static void CombatVoiceConstantsLocked()
    {
        Assert(CombatVoicePolicy.NewEnemyChance == .7f && CombatVoicePolicy.PartyCooldown == 4f && CombatVoicePolicy.HeardMemory == 180f, "alert law changed");
        Assert(CombatVoicePolicy.NoticeQuietSeconds == 3f && CombatVoicePolicy.NoticeMinDistance == 6f, "alert gate changed");
        Assert(CombatVoicePolicy.PartnerReactDelay == .6f, "partner reaction delay changed");
        Assert(CombatVoicePolicy.ExertHeavyChance == 1f && CombatVoicePolicy.ExertSwingChance == .30f && CombatVoicePolicy.ExertCooldown == 2.5f, "exertion law changed");
        Assert(CombatVoicePolicy.GruntChance == .65f && CombatVoicePolicy.GruntCooldown == 1.5f, "ordinary-hit law changed");
        Assert(CombatVoicePolicy.HurtFraction == .15f && CombatVoicePolicy.HurtCooldown == 4f, "devastating-hit law changed");
        Assert(CombatVoicePolicy.InterruptedChance == 1f && CombatVoicePolicy.InterruptedCooldown == 1.5f, "interruption law changed");
        Assert(CombatVoicePolicy.StaggerCooldown == 1.5f && CombatVoicePolicy.SameEventWindow == .3f, "stagger law changed");
        Assert(CombatVoicePolicy.MightyFraction == .25f && CombatVoicePolicy.MightyChance == .35f && CombatVoicePolicy.MightyCooldown == 10f, "gloat law changed");
        Assert(WarCryPolicy.ClearRadius == 6f, "war cry clearance changed");
    }

    static void CombatVoiceAlertsLocked()
    {
        Assert(CombatVoicePolicy.Noticing(3f, 3f, 6f), "a calm sighting at 6 m is not called out");
        Assert(!CombatVoicePolicy.Noticing(2.9f, 9f, 20f) && !CombatVoicePolicy.Noticing(9f, 2.9f, 20f), "called out mid-fight");
        Assert(!CombatVoicePolicy.Noticing(9f, 9f, 5.9f), "called out an enemy already on us");
        var v = new CombatVoicePolicy();
        Assert(v.NewEnemy(0f, 1, .69f), "alert at .69 roll silent (chance .7)");
        Assert(!v.NewEnemy(10f, 1, .0f), "same enemy called out twice inside 180 s");
        Assert(!v.NewEnemy(3.9f, 2, .0f), "second alert inside the 4 s party cooldown");
        Assert(v.NewEnemy(4.1f, 2, .0f) == false, "an enemy heard during the cooldown is remembered, not re-announced");
        Assert(v.NewEnemy(4.2f, 3, .0f), "a new enemy after the cooldown is not called out");
        Assert(!new CombatVoicePolicy().NewEnemy(0f, 1, .7f), "alert at .7 roll (chance .7)");
    }

    static void CombatVoiceExertionLocked()
    {
        var v = new CombatVoicePolicy();
        Assert(v.Exert(0f, 1, true, .999f) && v.Exert(.1f, 1, true, .999f), "a heavy attack went unvoiced (roll or cooldown)");
        Assert(!v.Exert(.2f, 1, false, .0f), "ordinary swing voiced inside 2.5 s of the last exertion");
        Assert(v.Exert(2.7f, 1, false, .29f), "ordinary swing at .29 silent (chance .30)");
        Assert(!v.Exert(9f, 1, false, .30f), "ordinary swing at .30 voiced");
        Assert(v.Exert(9f, 2, false, .0f), "one companion's exertion silenced another");
    }

    static void CombatVoiceHurtLocked()
    {
        var v = new CombatVoicePolicy();
        Assert(v.Hurt(0f, 1, 37.5f, 250f, false, .999f) == CombatVoicePolicy.HurtLine.Devastated, "15% blow not a guaranteed devastating cry");
        Assert(v.Hurt(3.9f, 1, 60f, 250f, false, .0f) == CombatVoicePolicy.HurtLine.None, "pain cry inside the 4 s cooldown");
        Assert(v.Hurt(4.1f, 1, 60f, 250f, false, .999f) == CombatVoicePolicy.HurtLine.Devastated, "devastating cry after 4 s missing");
        var w = new CombatVoicePolicy();
        Assert(w.Hurt(0f, 1, 37f, 250f, false, .64f) == CombatVoicePolicy.HurtLine.Grunt, "ordinary hit at .64 silent (chance .65)");
        Assert(w.Hurt(1.4f, 1, 10f, 250f, false, .0f) == CombatVoicePolicy.HurtLine.None, "ordinary hit inside 1.5 s voiced");
        Assert(w.Hurt(9f, 1, 10f, 250f, false, .65f) == CombatVoicePolicy.HurtLine.None, "ordinary hit at .65 voiced");
        Assert(w.Hurt(9.1f, 1, 10f, 250f, true, .999f) == CombatVoicePolicy.HurtLine.Grunt, "hit mid-line not always answered");
    }

    static void CombatVoiceStaggerLocked()
    {
        var v = new CombatVoicePolicy();
        Assert(v.Stagger(0f, 1), "a new stagger drew no cry");
        Assert(v.StaggerAnswered(.29f, 1) && !v.StaggerAnswered(.31f, 1), "stagger and its damage are not one 0.3 s event");
        Assert(!v.Stagger(1.4f, 1) && v.Stagger(1.6f, 1), "stagger cry cooldown is not 1.5 s");
        Assert(v.Stagger(0f, 2), "one companion's stagger silenced another");
    }

    static void CombatVoiceGloatLocked()
    {
        var v = new CombatVoicePolicy();
        Assert(!v.Mighty(0f, 24f, 100f, false, .0f), "gloated at a 24% blow");
        Assert(v.Mighty(0f, 25f, 100f, false, .34f), "no gloat at a 25% blow, .34 roll");
        Assert(!v.Mighty(9.9f, 0f, 100f, true, .0f) && v.Mighty(10.1f, 0f, 100f, true, .0f), "gloat cooldown is not 10 s");
        Assert(!new CombatVoicePolicy().Mighty(0f, 0f, 100f, true, .35f), "gloated at .35 roll (chance .35)");
    }

    static void CombatVoiceWarCryLocked()
    {
        Assert(WarCryPolicy.Allowed(true, 6.1f, false, false, false), "no war cry at a calm raid start");
        Assert(!WarCryPolicy.Allowed(false, 99f, false, false, false), "war cry outside a new raid");
        Assert(!WarCryPolicy.Allowed(true, 6f, false, false, false), "war cry with an enemy inside 6 m");
        Assert(!WarCryPolicy.Allowed(true, 99f, true, false, false) && !WarCryPolicy.Allowed(true, 99f, false, true, false) &&
               !WarCryPolicy.Allowed(true, 99f, false, false, true), "war cry while attacking, falling back or moving");
    }

    static void CombatVoiceDeathLocked()
    {
        Assert(DeathVoice.Key(true) == "f:death" && DeathVoice.Key(false) == "m:death", "death pool not chosen by identity gender");
        Assert(!DeathVoice.Attach, "death line attached to the body destroyed in the same OnDeath call");
        foreach (string enemy in new[] { "sfx_goblin_death", "sfx_goblinbrute_death", "sfx_troll_death", "LWC_sfx_death" })
            Assert(DeathVoice.Merge(new[] { enemy }, "m:death").Count == 1, enemy + " survives beside the companion death line");
    }

    static void DeathVoiceIsOneUnattachedGenderedLine()
    {
        Assert(DeathVoice.Key(true) == "f:death" && DeathVoice.Key(false) == "m:death", "death pool is not chosen by gender");
        Assert(!DeathVoice.Attach, "death line is attached to the body that is destroyed in the same OnDeath call");
        // Native Fuling death list: its cry is dropped, non-voice effects (vfx, ragdoll) are kept in order.
        var native = new[] { "vfx_goblin_death", "sfx_goblin_death", "Goblin_ragdoll" };
        var once = DeathVoice.Merge(native, "f:death");
        Assert(string.Join(" ", once.ToArray()) == "vfx_goblin_death Goblin_ragdoll f:death", "death list is " + string.Join(" ", once.ToArray()));
        // Re-applying (a second ApplyGender, or a list that already holds our line) never stacks a second cry.
        var twice = DeathVoice.Merge(new[] { "vfx_goblin_death", "LWC_sfx_m_death", "LWC_sfx_f_death" }, "f:death");
        int lines = 0; foreach (string n in twice) if (n == "f:death" || DeathVoice.IsVoice(n)) lines++;
        Assert(lines == 1, lines + " death voices after re-apply");
        Assert(DeathVoice.Merge(native, null).Count == 2, "factory-time list still holds an enemy death voice");
        Assert(DeathVoice.IsVoice("LWC_sfx_m_death") && DeathVoice.IsVoice("sfx_goblin_death") && !DeathVoice.IsVoice("vfx_goblin_death"), "voice filter wrong");
    }

    static void FemaleSlotFlatten()
    {
        string[] flat = VoiceSlots.Flatten(new[] { new[] { "inv01", "inv03" }, new[] { "rq02" }, new[] { "rq03" } });
        Assert(string.Join(" ", flat) == "inv01 inv03 rq02 rq02 rq03 rq03", "female alerted array is " + string.Join(" ", flat));
        string[] plain = VoiceSlots.Flatten(new[] { new[] { "a" }, new[] { "b" }, new[] { "c" } });
        Assert(string.Join(" ", plain) == "a b c", "ungrouped lines changed weight: " + string.Join(" ", plain));
        string[] mixed = VoiceSlots.Flatten(new[] { new[] { "a", "b", "c" }, new[] { "d", "e" } });
        Assert(mixed.Length == 12, "3- and 2-atom slots are not both 1/2");
    }

    static void SplitKeepsSlotWeight()
    {
        string[] flat = VoiceSlots.SplitOne(new[] { "S01", "S05", "S06" }, "S01", "S01_A", "S01_B");
        Assert(flat != null && flat.Length == 6, "split set is not six entries");
        int a = 0, b = 0, s05 = 0, s06 = 0, s01 = 0;
        foreach (string n in flat) { if (n == "S01_A") a++; else if (n == "S01_B") b++; else if (n == "S05") s05++; else if (n == "S06") s06++; else if (n == "S01") s01++; }
        Assert(a == 1 && b == 1, "A and B are not 1/6 each");
        Assert(s05 == 2 && s06 == 2, "Surprised05/06 lost their 1/3");
        Assert(s01 == 0, "the whole Surprised01 still plays (A+B would follow it)");
        Assert(VoiceSlots.SplitOne(new[] { "S05", "S06" }, "S01", "x", "y") == null, "split invented a missing slot");
    }

    static void VoiceNewEnemyOnce()
    {
        var v = new CombatVoicePolicy();
        Assert(v.NewEnemy(0f, 7, .1f), "new enemy not called out");
        Assert(!v.NewEnemy(10f, 7, .1f), "same enemy called out twice");
        var w = new CombatVoicePolicy();
        Assert(!w.NewEnemy(0f, 8, .95f), "spoke on a failed roll");
        Assert(!w.NewEnemy(10f, 8, .1f), "silent first sighting allowed a later call-out for the same enemy");
    }

    static void VoicePartyCooldown()
    {
        var v = new CombatVoicePolicy();
        Assert(v.NewEnemy(0f, 1, .1f), "first call-out refused");
        Assert(!v.NewEnemy(1f, 2, .1f), "two call-outs within the party cooldown");
        Assert(v.NewEnemy(5f, 3, .1f), "call-out refused after the cooldown");
    }

    static void VoiceHurt()
    {
        var v = new CombatVoicePolicy();
        Assert(v.Hurt(0f, 1, 45f, 250f, false, .99f) == CombatVoicePolicy.HurtLine.Devastated, "silent at a devastating hit");
        Assert(v.Hurt(1f, 1, 60f, 250f, false, .0f) == CombatVoicePolicy.HurtLine.None, "no cooldown on pain cries");
        Assert(v.Hurt(1f, 2, 60f, 250f, false, .99f) == CombatVoicePolicy.HurtLine.Devastated, "one companion's cooldown silenced the other");
        var w = new CombatVoicePolicy();
        Assert(w.Hurt(0f, 1, 20f, 250f, false, .9f) == CombatVoicePolicy.HurtLine.None, "grunted at every ordinary hit");
        Assert(w.Hurt(0f, 1, 20f, 250f, false, .1f) == CombatVoicePolicy.HurtLine.Grunt, "never grunts at ordinary hits");
        Assert(w.Hurt(1f, 1, 20f, 250f, false, .1f) == CombatVoicePolicy.HurtLine.None, "grunts with no cooldown");
    }

    static void VoiceInterrupted()
    {
        var v = new CombatVoicePolicy();
        Assert(v.Hurt(0f, 1, 20f, 250f, true, .5f) == CombatVoicePolicy.HurtLine.Grunt, "gloat cut off with no answer at 50% roll");
        Assert(v.Hurt(.5f, 1, 20f, 250f, true, .1f) == CombatVoicePolicy.HurtLine.None, "answers stack with no cooldown");
        Assert(v.Hurt(2f, 1, 20f, 250f, true, .99f) == CombatVoicePolicy.HurtLine.Grunt, "1.0.32.4: hit mid-line not always answered");
        var w = new CombatVoicePolicy();
        Assert(w.Hurt(0f, 1, 20f, 250f, false, .6f) == CombatVoicePolicy.HurtLine.Grunt, "1.0.32.4: ordinary hit at .6 roll silent (chance .65)");
        Assert(w.Hurt(1.6f, 1, 20f, 250f, false, .1f) == CombatVoicePolicy.HurtLine.Grunt, "1.0.32.4: grunt cooldown longer than 1.5 s");
    }

    static void VoiceMighty()
    {
        var v = new CombatVoicePolicy();
        Assert(!v.Mighty(0f, 10f, 100f, false, .1f), "taunted at a small hit");
        Assert(!v.Mighty(0f, 60f, 100f, false, .9f), "taunted on a failed roll");
        Assert(v.Mighty(0f, 60f, 100f, false, .1f), "no taunt at a mighty blow");
        Assert(!v.Mighty(5f, 0f, 100f, true, .1f), "taunts inside the cooldown");
        Assert(v.Mighty(11f, 0f, 100f, true, .1f), "kill after cooldown not taunted");
    }

    static bool C(bool allowed, bool window, bool melee, bool secondary, bool alive, float d, float angle)
    { return ChainPolicy.Continue(allowed, window, melee, secondary, alive, d, 2.2f, angle, 30f); }

    static void ChainContinuesOnlyWhenTheSwingCanLand()
    {
        Assert(C(true, true, true, false, true, 1.5f, 10f), "no follow-up with the target in reach and in front");
        Assert(!C(true, false, true, false, true, 1.5f, 10f), "chained outside the native chain window");
        Assert(!C(false, true, true, false, true, 1.5f, 10f), "chained while combat does not own the hands");
        Assert(!C(true, true, false, false, true, 1.5f, 10f), "chained a bow");
        Assert(!C(true, true, true, true, true, 1.5f, 10f), "chained off a secondary attack");
        Assert(!C(true, true, true, false, false, 1.5f, 10f), "swung again at a dead target");
        Assert(!C(true, true, true, false, true, 4f, 10f), "chained at a target out of reach");
        Assert(!C(true, true, true, false, true, 1.5f, 80f), "chained at a target behind it");
        Assert(ChainPolicy.Continue(true, true, true, false, true, 1f, 2f, 40f, 0f), "weapon with no AI angle never gets the default angle");
    }

    static void KitsAreCompleteAndResupplyTheirOwnArrow()
    {
        foreach (string kit in new[] { CompanionKits.Iron, CompanionKits.Plains })
        {
            string[] items = CompanionKits.Items(kit);
            Assert(items != null && items.Length == 7, kit + " kit is not armor, legs, cape, bow, arrows, sword, shield");
            Assert(items[4].StartsWith("Arrow"), kit + " kit slot 5 is not arrows");
            Assert(CompanionKits.Arrow(kit) == items[4], kit + " quiver resupplies a different arrow");
        }
        Assert(CompanionKits.Items("PLAINS") != null, "kit names are case sensitive");
        Assert(CompanionKits.Items("mistlands") == null, "unknown kit accepted");
        Assert(CompanionKits.Arrow("") == "ArrowWood", "no kit does not resupply wood arrows");
        Assert(CompanionKits.ClampQuality(9) == 4 && CompanionKits.ClampQuality(0) == 1, "quality outside 1-4 accepted");
    }

    static HeavyPolicy.Why H(HeavyPolicy.Shape shape, float stamina, float edge, float facing, bool staggered, int inArea, int closing)
    { return HeavyPolicy.Opening(shape, stamina, 28f, true, edge, 2.7f, facing, staggered, inArea, closing); }

    static void HeavyOnlyWhenItPays()
    {
        var S = HeavyPolicy.Shape.Single; var A = HeavyPolicy.Shape.Area;
        Assert(H(S, 100f, 1.5f, 5f, true, 1, 0) == HeavyPolicy.Why.Staggered, "no opening on a staggered target in reach");
        Assert(H(S, 100f, 1.5f, 5f, true, 1, 2) == HeavyPolicy.Why.None, "single heavy committed with two enemies closing in");
        Assert(H(S, 20f, 1.5f, 5f, true, 1, 0) == HeavyPolicy.Why.None, "heavy without the stamina for it");
        Assert(H(S, 28f, 1.5f, 5f, true, 1, 0) == HeavyPolicy.Why.Staggered, "exact cost refused (no hidden reserve any more)");
        Assert(H(S, 100f, 4f, 5f, true, 1, 0) == HeavyPolicy.Why.None, "heavy out of reach");
        Assert(H(S, 100f, 1.5f, 45f, true, 1, 0) == HeavyPolicy.Why.None, "narrow heavy not lined up");
        Assert(H(S, 100f, 1.5f, 5f, false, 1, 0) == HeavyPolicy.Why.None, "heavy without a stagger (only openings)");
        Assert(H(A, 100f, 1.5f, 90f, false, 3, 3) == HeavyPolicy.Why.Crowd, "area heavy refused a crowd");
        Assert(H(A, 100f, 1.5f, 90f, false, 1, 0) == HeavyPolicy.Why.None, "area heavy on one ordinary target");
        Assert(HeavyPolicy.Opening(HeavyPolicy.Shape.None, 100f, 0f, true, 1f, 2f, 0f, true, 3, 0) == HeavyPolicy.Why.None, "weapon without a heavy used one");
        Assert(HeavyPolicy.ShapeOf(true, true, 90f, false) == HeavyPolicy.Shape.None, "spear throw treated as a melee heavy");
        Assert(HeavyPolicy.ShapeOf(true, false, 360f, false) == HeavyPolicy.Shape.Area, "atgeir spin not an area heavy");
        Assert(HeavyPolicy.ShapeOf(true, false, 10f, false) == HeavyPolicy.Shape.Single, "sword stab not single-target");
    }

    static void StaminaFollowsPlayerRules()
    {
        Assert(System.Math.Abs(StaminaRules.RegenPerSecond(150f, 150f) - 6f) < .01f, "full-pool regen is not 6/s");
        Assert(System.Math.Abs(StaminaRules.RegenPerSecond(0f, 150f) - 12f) < .01f, "empty-pool regen is not 12/s");
    }

    static int PickSplit(float separation, int current, params TargetCandidate[] c)
    {
        string reason;
        int i = TargetPolicy.Choose(c, current, true, false, separation, out reason);
        return i < 0 ? -1 : c[i].Id;
    }

    static void SpartaDefendsThePartner()
    {
        var nearMe = Foe(1, 6f); nearMe.TargetsMe = true;                          // coming at me, not at contact
        var onPartner = Foe(2, 35f); onPartner.TargetsPartner = true; onPartner.PartnerDistance = 2f;
        Assert(PickSplit(30f, -1, nearMe, onPartner) == 2, "split pair: ignored the enemy on the partner");
        var hittingMe = Foe(3, 1.5f); hittingMe.HurtMe = true;
        Assert(PickSplit(30f, -1, hittingMe, onPartner) == 3, "split pair: walked away from an enemy at contact");
        Assert(PickSplit(30f, 1, nearMe, onPartner) == 2, "split pair: kept its own target over the partner's attacker");
        Assert(PickSplit(8f, -1, nearMe, onPartner) == 1, "together: partner rule overrode the normal choice");
        var tooFar = Foe(4, 70f); tooFar.TargetsPartner = true; tooFar.PartnerDistance = 1f;
        Assert(PickSplit(65f, -1, tooFar) == -1, "crossed more than the partner reach");
        var notOnThem = Foe(5, 35f); notOnThem.TargetsPartner = true; notOnThem.PartnerDistance = 15f;
        Assert(PickSplit(30f, -1, notOnThem) == -1, "chased an enemy that is not on the partner, beyond my range");
        var a = new PhaseArbiter();
        var s = Calm(); s.HasTarget = true; s.LeaderDistance = 30f; s.NearestPartyDistance = 30f; s.PartnerUnderAttack = true;
        Assert(StepN(a, s, 30) == NpcPhase.Combat, "regrouped to the leader while the partner was under attack");
        s.PartnerUnderAttack = false;
        Assert(StepN(a, s, 30) == NpcPhase.CombatRegroup, "no regroup once nobody is on the partner");
    }

    static void WarCryNeverCostsAHit()
    {
        Assert(WarCryPolicy.Allowed(true, 20f, false, false, false), "no war cry at a raid start");
        Assert(!WarCryPolicy.Allowed(true, 3f, false, false, false), "war cry with an enemy in reach");
        Assert(!WarCryPolicy.Allowed(true, 20f, true, false, false), "war cry mid-swing");
        Assert(!WarCryPolicy.Allowed(true, 20f, false, true, false), "war cry while falling back");
        Assert(!WarCryPolicy.Allowed(false, 20f, false, false, false), "war cry without a raid");
    }

    static void VoiceBeforeDuringAfter()
    {
        Assert(CombatVoicePolicy.Noticing(10f, 10f, 15f), "no call-out on a fresh sighting before contact");
        Assert(!CombatVoicePolicy.Noticing(.9f, 10f, 15f), "call-out right after its own strike (1.0.29: 0.87 s lag)");
        Assert(!CombatVoicePolicy.Noticing(10f, 1f, 15f), "call-out right after being hit");
        Assert(!CombatVoicePolicy.Noticing(10f, 10f, 2f), "call-out at an enemy already on it");
        var v = new CombatVoicePolicy();
        Assert(v.Exert(0f, 1, true, .5f), "no exertion on a heavy");
        Assert(!v.Exert(1f, 1, false, .1f), "ordinary exertion with no cooldown");
        Assert(v.Exert(1.5f, 1, true, .99f), "1.0.32.4: a heavy went unvoiced (roll or cooldown)");
        Assert(v.Exert(4f, 1, false, .2f), "1.0.32.4: ordinary swing not voiced at .2 roll after 2.5 s");
        Assert(!v.Exert(5f, 1, false, .2f), "1.0.32.4: ordinary exertion inside the 2.5 s cooldown");
        var st = new CombatVoicePolicy();
        Assert(st.Stagger(0f, 1), "stagger entry not voiced");
        Assert(!st.Stagger(1f, 1), "stagger voiced twice inside 1.5 s");
        Assert(st.Stagger(1.6f, 1) && st.Stagger(1.6f, 2), "stagger cooldown wrong");
        Assert(st.StaggerAnswered(1.8f, 1) && !st.StaggerAnswered(2f, 1), "same-event window is not 0.3 s");
        Assert(!v.Exert(5f, 1, false, .5f), "exertion on most ordinary swings");
        Assert(v.Exert(5f, 2, false, .1f), "one companion's cooldown silenced another");
    }
}

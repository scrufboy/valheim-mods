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

    static void VoiceNewEnemyOnce()
    {
        var v = new VoicePolicy();
        Assert(v.NewEnemy(0f, 7, .1f), "new enemy not called out");
        Assert(!v.NewEnemy(10f, 7, .1f), "same enemy called out twice");
        var w = new VoicePolicy();
        Assert(!w.NewEnemy(0f, 8, .95f), "spoke on a failed roll");
        Assert(!w.NewEnemy(10f, 8, .1f), "silent first sighting allowed a later call-out for the same enemy");
    }

    static void VoicePartyCooldown()
    {
        var v = new VoicePolicy();
        Assert(v.NewEnemy(0f, 1, .1f), "first call-out refused");
        Assert(!v.NewEnemy(1f, 2, .1f), "two call-outs within the party cooldown");
        Assert(v.NewEnemy(5f, 3, .1f), "call-out refused after the cooldown");
    }

    static void VoiceHurt()
    {
        var v = new VoicePolicy();
        Assert(v.Hurt(0f, 1, 45f, 250f, false, .99f) == VoicePolicy.HurtLine.Devastated, "silent at a devastating hit");
        Assert(v.Hurt(1f, 1, 60f, 250f, false, .0f) == VoicePolicy.HurtLine.None, "no cooldown on pain cries");
        Assert(v.Hurt(1f, 2, 60f, 250f, false, .99f) == VoicePolicy.HurtLine.Devastated, "one companion's cooldown silenced the other");
        var w = new VoicePolicy();
        Assert(w.Hurt(0f, 1, 20f, 250f, false, .9f) == VoicePolicy.HurtLine.None, "grunted at every ordinary hit");
        Assert(w.Hurt(0f, 1, 20f, 250f, false, .1f) == VoicePolicy.HurtLine.Grunt, "never grunts at ordinary hits");
        Assert(w.Hurt(1f, 1, 20f, 250f, false, .1f) == VoicePolicy.HurtLine.None, "grunts with no cooldown");
    }

    static void VoiceInterrupted()
    {
        var v = new VoicePolicy();
        Assert(v.Hurt(0f, 1, 20f, 250f, true, .5f) == VoicePolicy.HurtLine.Grunt, "gloat cut off with no answer at 50% roll");
        Assert(v.Hurt(.5f, 1, 20f, 250f, true, .1f) == VoicePolicy.HurtLine.None, "answers stack with no cooldown");
    }

    static void VoiceMighty()
    {
        var v = new VoicePolicy();
        Assert(!v.Mighty(0f, 10f, 100f, false, .1f), "taunted at a small hit");
        Assert(!v.Mighty(0f, 60f, 100f, false, .9f), "taunted on a failed roll");
        Assert(v.Mighty(0f, 60f, 100f, false, .1f), "no taunt at a mighty blow");
        Assert(!v.Mighty(5f, 0f, 100f, true, .1f), "taunts inside the cooldown");
        Assert(v.Mighty(11f, 0f, 100f, true, .1f), "kill after cooldown not taunted");
    }
}

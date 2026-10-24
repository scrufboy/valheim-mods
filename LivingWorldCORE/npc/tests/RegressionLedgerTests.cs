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
        Run("0.6.4/0.6.5: returning companion still defends itself when hit up close", ReturningActorDefendsLocally);
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
        Run("Target: archer shoots the enemy attacking the leader, not the nearest", ArcherProtectsLeader);
        Run("Target: archer prefers an enemy shooter over a closer brute", ArcherPrefersShooters);
        Run("Target: archer defends herself when something is on her", ArcherSelfDefense);
        Run("Target: guard peels the enemy off the archer", GuardPeelsForArcher);
        Run("Target: guard does not chase a distant shooter out of the party", GuardDoesNotChaseShooter);
        Run("Target: no flip-flop between near-equal targets (hysteresis)", TargetHysteresis);
        Run("Target: unseen enemies are never chosen (no wallhack)", TargetNeedsSight);
        Run("Target: nothing valid leaves native choice alone", TargetNothingValid);
        Run("0.x regression: deer are watched, never fought", DeerNeverFought);
        Run("0.x rule: no fight starts until a creature commits to the party", NoUnprovokedFight);
        Run("0.x rule: a creature that picks us is fought before it lands a hit", AggroIsEnough);
        Run("0.x rule: during a fight, other alerted hostiles join the list", EncounterWidens);
        Run("0.x rule: leader sneaking = only what already committed", SneakOnlyCommitted);
        Run("Engagement: wildlife that hurts us is fair game", WildlifeThatHurts);
        Run("Engagement: config off = nearest engaged, rule still applies", NearestEngagedWhenUnranked);

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
        Assert(a.Cohesion.Returning, "latch must stay armed during local defense");
        s.LocalThreat = false; s.DamagedRecently = false; s.HasTarget = true;
        Assert(StepN(a, s, 1) == NpcPhase.CombatRegroup, "after local threat ends it must resume returning, got " + a.Phase);
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

    static int Pick(CombatRole role, int current, params TargetCandidate[] c)
    {
        string reason;
        int i = TargetPolicy.Choose(role, c, current, out reason);
        return i < 0 ? -1 : c[i].Id;
    }

    static void ArcherProtectsLeader()
    {
        var near = Foe(1, 9f); var onLeader = Foe(2, 20f); onLeader.TargetsLeader = true;
        Assert(Pick(CombatRole.Archer, -1, near, onLeader) == 2, "archer ignored the leader's attacker");
    }

    static void ArcherPrefersShooters()
    {
        var brute = Foe(1, 10f); var shooter = Foe(2, 22f); shooter.Ranged = true;
        Assert(Pick(CombatRole.Archer, -1, brute, shooter) == 2, "archer ignored the enemy archer");
    }

    static void ArcherSelfDefense()
    {
        var onMe = Foe(1, 2f); onMe.TargetsMe = true; onMe.CanSee = false;
        var shooter = Foe(2, 20f); shooter.Ranged = true; shooter.TargetsLeader = true;
        Assert(Pick(CombatRole.Archer, -1, onMe, shooter) == 1, "archer ignored the enemy on top of her");
    }

    static void GuardPeelsForArcher()
    {
        var near = Foe(1, 4f); var onArcher = Foe(2, 9f); onArcher.TargetsPartner = true; onArcher.PartnerDistance = 1.5f;
        Assert(Pick(CombatRole.Guard, -1, near, onArcher) == 2, "guard did not peel for the archer");
    }

    static void GuardDoesNotChaseShooter()
    {
        var shooter = Foe(1, 24f); shooter.Ranged = true; var brute = Foe(2, 8f);
        Assert(Pick(CombatRole.Guard, -1, shooter, brute) == 2, "guard ran off after a distant shooter");
    }

    static void TargetHysteresis()
    {
        var a = Foe(1, 10f); var b = Foe(2, 8f);
        Assert(Pick(CombatRole.Guard, 1, a, b) == 1, "switched for a 2 m difference");
        var c = Foe(3, 9f); c.TargetsPartner = true;
        Assert(Pick(CombatRole.Guard, 1, a, c) == 3, "kept the old target despite a clear reason to switch");
    }

    static void TargetNeedsSight()
    {
        var hidden = Foe(1, 12f); hidden.CanSee = false; hidden.TargetsLeader = true;
        var seen = Foe(2, 25f);
        Assert(Pick(CombatRole.Archer, -1, hidden, seen) == 2, "chose an enemy it cannot see");
    }

    static void TargetNothingValid()
    {
        var far = Foe(1, 45f); var hidden = Foe(2, 10f); hidden.CanSee = false;
        Assert(Pick(CombatRole.Archer, -1, far, hidden) == -1, "invented a target");
        Assert(Pick(CombatRole.Archer, -1) == -1, "empty list");
    }

    static int Pick2(bool encounter, bool sneak, bool ranked, params TargetCandidate[] c)
    {
        string reason;
        int i = TargetPolicy.Choose(CombatRole.Archer, c, -1, encounter, sneak, ranked, out reason);
        return i < 0 ? -1 : c[i].Id;
    }

    static void DeerNeverFought()
    {
        var deer = Foe(1, 12f); deer.Wildlife = true;
        Assert(Pick2(false, false, true, deer) == -1, "fought a deer in calm");
        Assert(Pick2(true, false, true, deer) == -1, "fought a fleeing deer during a fight");
    }

    static void NoUnprovokedFight()
    {
        var greydwarf = Foe(1, 15f);
        Assert(Pick2(false, false, true, greydwarf) == -1, "started a fight with an alerted greydwarf that chose nobody");
    }

    static void AggroIsEnough()
    {
        var g = Foe(1, 15f); g.TargetsLeader = true;
        Assert(Pick2(false, false, true, g) == 1, "waited to be hit");
        var h = Foe(2, 15f); h.TargetsPartner = true; h.Alerted = false;
        Assert(Pick2(false, false, true, h) == 2, "ignored the creature after her partner");
    }

    static void EncounterWidens()
    {
        var shaman = Foe(1, 20f); shaman.Ranged = true;
        Assert(Pick2(true, false, true, shaman) == 1, "ignored an alerted shaman mid-fight");
        shaman.Alerted = false;
        Assert(Pick2(true, false, true, shaman) == -1, "attacked an unaware creature mid-fight");
    }

    static void SneakOnlyCommitted()
    {
        var boar = Foe(1, 10f);
        Assert(Pick2(true, true, true, boar) == -1, "broke stealth on an uncommitted creature");
        boar.TargetsMe = true;
        Assert(Pick2(true, true, true, boar) == 1, "did not answer a creature that found us");
    }

    static void WildlifeThatHurts()
    {
        var w = Foe(1, 3f); w.Wildlife = true; w.HurtUs = true;
        Assert(Pick2(false, false, true, w) == 1, "let wildlife maul us");
    }

    static void NearestEngagedWhenUnranked()
    {
        var deer = Foe(1, 3f); deer.Wildlife = true;
        var a = Foe(2, 20f); a.TargetsLeader = true;
        var b = Foe(3, 9f); b.TargetsMe = true;
        Assert(Pick2(false, false, false, deer, a, b) == 3, "unranked mode picked wrong");
    }
}

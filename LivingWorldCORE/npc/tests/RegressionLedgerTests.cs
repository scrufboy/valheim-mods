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
}

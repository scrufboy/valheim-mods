using System;
using LivingWorld.HumanNpcV2;

internal static class FoundationContractTests
{
    static int failures;

    static void Check(bool condition, string message)
    {
        if (!condition) { failures++; Console.Error.WriteLine("FAIL: " + message); }
    }

    static void Identity()
    {
        var random = new Random(44721);
        bool male = false;
        bool female = false;
        for (int index = 0; index < 10000; index++)
        {
            HumanIdentityRecord identity = HumanIdentityRecord.Create(random, 20, 18);
            Check(!String.IsNullOrEmpty(identity.Name) && identity.Name.Contains(" "), "identity requires a full name");
            Check(identity.Hair.StartsWith("Hair", StringComparison.Ordinal), "every identity requires native hair");
            if (identity.Female)
            {
                female = true;
                Check(identity.Beard.Length == 0, "female identity cannot receive a beard");
            }
            else
            {
                male = true;
                Check(identity.Beard.StartsWith("Beard", StringComparison.Ordinal), "male identity requires a native beard");
            }
        }
        Check(male && female, "randomized population must exercise both model types");
    }

    static void DurableCombatState()
    {
        var latch = new CombatStateLatch(25);
        Check(!latch.Active && !latch.Observe(false), "calm begins calm");
        Check(latch.Observe(true) && latch.Active, "a committed threat enters combat immediately");
        for (int scan = 1; scan < 25; scan++)
        {
            Check(latch.Observe(false), "combat must survive incomplete clear verification " + scan);
            Check(latch.Active && latch.ClearScans == scan, "clear scan count must advance without leaving combat");
        }
        latch.Retain();
        Check(latch.Active && latch.ClearScans == 24, "a transient failed tick must retain the exact combat state");
        Check(!latch.Observe(false) && !latch.Active, "only the final verified clear scan may release combat");
        Check(latch.Observe(true), "combat can reacquire after release");
        for (int scan = 0; scan < 12; scan++) latch.Observe(false);
        Check(latch.Observe(true) && latch.ClearScans == 0, "a reacquired threat cancels accumulated clear evidence");
    }

    static HumanBehaviorSignal Signal(float leaderDistance, float formationDistance)
    {
        return new HumanBehaviorSignal
        {
            InputValid = true,
            AppearanceReady = true,
            LeaderAvailable = true,
            LeaderDistance = leaderDistance,
            FormationDistance = formationDistance
        };
    }

    static void ExclusiveBehaviorStates()
    {
        var machine = new HumanBehaviorStateMachine(3);
        HumanBehaviorSignal signal = Signal(3f, 1f);
        Check(machine.Step(signal) == HumanBehaviorState.IdleGuard, "ready calm actor must enter idle guard");

        signal.LeaderMoving = true;
        Check(machine.Step(signal) == HumanBehaviorState.Traversal, "moving leader must select traversal");
        signal.LeaderSprinting = true;
        Check(machine.Step(signal) == HumanBehaviorState.TraversalSprint, "sprinting leader must select sprint traversal");
        signal.LeaderArmed = true;
        Check(machine.Step(signal) == HumanBehaviorState.CombatReadiness, "drawn leader weapon must select readiness without inventing a target");

        signal.LeaderDistance = 13f;
        Check(machine.Step(signal) == HumanBehaviorState.CombatReadiness,
            "readiness must outrank catch-up so movement cannot stow an armed companion");
        signal.LeaderDistance = 3f;

        signal.CommittedThreat = true;
        Check(machine.Step(signal) == HumanBehaviorState.NativeCombat, "committed threat must hand off to native combat immediately");
        signal.CommittedThreat = false;
        signal.LeaderArmed = false;
        Check(machine.Step(signal) == HumanBehaviorState.CombatClear && machine.ClearScans == 1,
            "first clear scan must keep combat ownership");
        HumanBehaviorState retained = machine.Step(new HumanBehaviorSignal { InputValid = false });
        Check(retained == HumanBehaviorState.CombatClear && machine.ClearScans == 1,
            "invalid input must retain state and may not count as clear evidence");
        Check(machine.Step(signal) == HumanBehaviorState.CombatClear && machine.ClearScans == 2,
            "second clear scan must keep combat ownership");
        Check(machine.Step(signal) == HumanBehaviorState.Regroup, "only complete clear verification may enter regroup");

        signal.FormationDistance = 3f;
        Check(machine.Step(signal) == HumanBehaviorState.Regroup, "regroup must persist until its formation destination is reached");
        signal.FormationDistance = 1f;
        signal.LeaderMoving = false;
        signal.LeaderSprinting = false;
        Check(machine.Step(signal) == HumanBehaviorState.IdleGuard, "completed regroup must return to calm state selection");

        var distanceMachine = new HumanBehaviorStateMachine(3);
        signal = Signal(13f, 13f);
        Check(distanceMachine.Step(signal) == HumanBehaviorState.CatchUp, "twelve metre separation must enter catch-up");
        signal.LeaderDistance = 8f;
        Check(distanceMachine.Step(signal) == HumanBehaviorState.CatchUp, "catch-up hysteresis must prevent boundary flicker");
        signal.LeaderDistance = 6f;
        signal.LeaderMoving = true;
        Check(distanceMachine.Step(signal) == HumanBehaviorState.Traversal, "catch-up may release only inside seven metres");

        var alertMachine = new HumanBehaviorStateMachine(2);
        signal = Signal(3f, 1f);
        signal.NativeAlert = true;
        Check(alertMachine.Step(signal) == HumanBehaviorState.CombatReadiness,
            "native alert without a creature target is readiness, not committed combat");
        signal.NativeAlert = false;
        signal.CommittedThreat = true;
        Check(alertMachine.Step(signal) == HumanBehaviorState.NativeCombat,
            "a real creature target must still commit native combat");
        signal.CommittedThreat = false;
        signal.NativeAlert = true;
        Check(alertMachine.Step(signal) == HumanBehaviorState.CombatClear,
            "alert does not bypass the verified combat-clear window");
        Check(alertMachine.Step(signal) == HumanBehaviorState.CombatReadiness,
            "completed clear verification with a remaining alert returns to readiness");
    }

    static void GuardCoverage()
    {
        var coverage = new GuardCoverageV2();
        Check(coverage.Choose(0, 3) == 3, "fresh guard coverage starts at its stable actor offset");
        coverage.Resolve(3, 10d);
        Check(coverage.Choose(0, 0) == 9, "completed scan makes the opposing sector next");
        Check(GuardCoverageV2.Opposite(9) == 3, "opposing guard sectors are symmetric");
        Check(coverage.Choose(1 << 3, 0) == 9, "a second guard covers the opposing field");
        Check(coverage.Choose((1 << 0) | (1 << 4), 0) == 8,
            "additional guards maximize separation from every occupied sector");
        coverage.Reset();
        Check(coverage.Choose(0, 7) == 7, "new-area reset discards previous coverage history");
    }

    public static int Main()
    {
        Identity();
        DurableCombatState();
        ExclusiveBehaviorStates();
        GuardCoverage();
        if (failures != 0) return 1;
        Console.WriteLine("Foundation contracts PASS: identity, combat ownership, exclusive states, and coordinated guard coverage.");
        return 0;
    }
}

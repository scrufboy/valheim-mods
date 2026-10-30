using System;

namespace LivingWorld.Companions.FormidableAI.Classes
{
    internal static class ClassTacticalPolicyTests
    {
        private static int _passed;

        public static int Main()
        {
            Run("warden blocks observed strike before protecting ally", WardenDefensePreemptsProtection);
            Run("huntress protects firing lane", HuntressProtectsLane);
            Run("mystic refuses support under personal threat", MysticYieldsUnderThreat);
            Run("mystic heals critical ally in safe window", MysticHealsSafely);
            Run("skirmisher throw is bounded by recovery and lane", SkirmisherThrowIsBounded);
            Run("commander protects ally before pressure", CommanderProtectsFirst);
            Run("pathfinder investigation stops at combat commitment", PathfinderYieldsInvestigation);
            Run("unauthorized owner yields every class", UnauthorizedOwnerYields);
            Console.WriteLine("PASS: " + _passed + " FormidableAI class policy tests");
            return 0;
        }

        private static ClassCombatFacts BaseCombat()
        {
            return new ClassCombatFacts { OwnerAuthorized = true, ActorReady = true, CommittedCombat = true,
                TargetAuthorized = true, LineOfFireClear = true, PreferredRange = true, SupportSafe = true,
                AbilityReady = true, ThrowReady = true, ThrowRecoverySafe = true, TargetInReach = true };
        }

        private static ClassActionRequest Decide(FormidableClass role, ClassCombatFacts facts)
        {
            return ClassTacticalPolicy.Decide(ClassProfile.Create(role), facts);
        }

        private static void WardenDefensePreemptsProtection()
        {
            ClassCombatFacts f = BaseCombat(); f.IncomingStrike = true; f.ShieldReady = true; f.AllyThreatened = true;
            ClassActionRequest x = Decide(FormidableClass.Warden, f);
            Assert(x.Kind == ClassActionKind.Defend && x.Priority == 100, "defense did not preempt movement");
        }

        private static void HuntressProtectsLane()
        {
            ClassCombatFacts f = BaseCombat(); f.AllyInLineOfFire = true;
            ClassActionRequest x = Decide(FormidableClass.Huntress, f);
            Assert(x.Kind == ClassActionKind.Reposition && x.Domain == ClassActionDomain.MovementIntent, "unsafe shot requested");
        }

        private static void MysticYieldsUnderThreat()
        {
            ClassCombatFacts f = BaseCombat(); f.PersonalThreat = true; f.AllyCritical = true;
            Assert(Decide(FormidableClass.Mystic, f).Kind == ClassActionKind.Yield, "support stole personal-defense window");
        }

        private static void MysticHealsSafely()
        {
            ClassCombatFacts f = BaseCombat(); f.AllyCritical = true;
            Assert(Decide(FormidableClass.Mystic, f).Kind == ClassActionKind.HealAlly, "critical safe heal not selected");
        }

        private static void SkirmisherThrowIsBounded()
        {
            ClassCombatFacts f = BaseCombat();
            Assert(Decide(FormidableClass.Skirmisher, f).Kind == ClassActionKind.ThrowWeapon, "safe throw not selected");
            f.AllyInLineOfFire = true;
            Assert(Decide(FormidableClass.Skirmisher, f).Kind == ClassActionKind.ReachAttack, "unsafe throw was not rejected");
        }

        private static void CommanderProtectsFirst()
        {
            ClassCombatFacts f = BaseCombat(); f.AllyThreatened = true;
            Assert(Decide(FormidableClass.Commander, f).Kind == ClassActionKind.ProtectAlly, "commander ignored ally threat");
        }

        private static void PathfinderYieldsInvestigation()
        {
            ClassCombatFacts f = new ClassCombatFacts { OwnerAuthorized = true, ActorReady = true, EvidenceToInvestigate = true };
            Assert(Decide(FormidableClass.Pathfinder, f).Kind == ClassActionKind.Investigate, "precombat evidence ignored");
            f.CommittedCombat = true; f.TargetAuthorized = true; f.LineOfFireClear = true;
            Assert(Decide(FormidableClass.Pathfinder, f).Kind == ClassActionKind.RangedAttack, "investigation survived combat handoff");
        }

        private static void UnauthorizedOwnerYields()
        {
            foreach (FormidableClass role in Enum.GetValues(typeof(FormidableClass)))
                Assert(Decide(role, new ClassCombatFacts { ActorReady = true }).Kind == ClassActionKind.Yield,
                    "unauthorized class acted: " + role);
        }

        private static void Run(string name, Action test)
        {
            test(); _passed++; Console.WriteLine("ok - " + name);
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}

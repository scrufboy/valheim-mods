using System;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Defense;
using LivingWorld.Companions.FormidableAI.Classes;

namespace LivingWorld.FormidableAI.Integration
{
    internal static class IntegrationTests
    {
        private static int _passed;

        public static int Main()
        {
            Run("exact defense owns defense domain", ExactDefenseOwnsDomain);
            Run("class defense does not duplicate strike policy", ClassDefenseDoesNotDuplicate);
            Run("defense and class movement compose", DefenseAndMovementCompose);
            Run("class weapon action owns attack and facing", WeaponActionMapsDomains);
            Run("native owns every unrequested domain", NativeOwnsRemainder);
            Run("remote actor cannot receive formidable ownership", RemoteActorCannotAct);
            Console.WriteLine("PASS: " + _passed + " FormidableAI integration tests");
            return 0;
        }

        private static CombatFrame Frame(bool owner)
        {
            return new CombatFrame(7, 10f, "person-1", "test-class", "enemy-1",
                owner, true, 1f, 1f,
                new[] { new ThreatObservation("enemy-1", 3f, true, true) });
        }

        private static DefenseDecision Block()
        {
            StrikeObservation strike = new StrikeObservation("person-1", "enemy-1", "swing-1",
                true, true, true, true, true, true, false, 10f, .2f, 2f, .8f, 50f);
            return DefensePolicy.Decide(DefenseMemory.Empty(), strike);
        }

        private static ClassActionRequest Request(ClassActionKind kind, ClassActionDomain domain, int priority)
        {
            return new ClassActionRequest { Kind = kind, Domain = domain, Priority = priority, Reason = "test" };
        }

        private static FormidableIntegrationResult Evaluate(bool owner, ClassActionRequest role, DefenseDecision defense)
        {
            return new FormidableIntegration().Evaluate(new FormidableIntegrationInput {
                Frame = Frame(owner), ClassRequest = role, DefenseDecision = defense
            });
        }

        private static void ExactDefenseOwnsDomain()
        {
            FormidableIntegrationResult x = Evaluate(true, null, Block());
            DomainLease lease = x.Arbitration.LeaseFor(DecisionDomain.Defense);
            Assert(lease.OwnerId == "formidable.defense" && lease.ActionId == "defense:block", "defense lease missing");
        }

        private static void ClassDefenseDoesNotDuplicate()
        {
            FormidableIntegrationResult x = Evaluate(true,
                Request(ClassActionKind.Defend, ClassActionDomain.Defense, 100), null);
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Defense).IsNativeFallback,
                "generic class request became a second defense brain");
        }

        private static void DefenseAndMovementCompose()
        {
            FormidableIntegrationResult x = Evaluate(true,
                Request(ClassActionKind.ProtectAlly, ClassActionDomain.MovementIntent, 80), Block());
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Defense).OwnerId == "formidable.defense", "defense lost");
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Movement).OwnerId == "formidable.class", "movement intent lost");
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Facing).OwnerId == "formidable.class", "atomic facing lease lost");
        }

        private static void WeaponActionMapsDomains()
        {
            FormidableIntegrationResult x = Evaluate(true,
                Request(ClassActionKind.RangedAttack, ClassActionDomain.WeaponAction, 55), null);
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Attack).OwnerId == "formidable.class", "attack not owned");
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Facing).OwnerId == "formidable.class", "facing not owned");
        }

        private static void NativeOwnsRemainder()
        {
            FormidableIntegrationResult x = Evaluate(true, null, null);
            foreach (DecisionDomain domain in new[] { DecisionDomain.Perception, DecisionDomain.Targeting,
                DecisionDomain.Movement, DecisionDomain.Facing, DecisionDomain.Defense,
                DecisionDomain.Equipment, DecisionDomain.Attack, DecisionDomain.Support })
                Assert(x.Arbitration.LeaseFor(domain).IsNativeFallback, "native fallback absent: " + domain);
        }

        private static void RemoteActorCannotAct()
        {
            FormidableIntegrationResult x = Evaluate(false,
                Request(ClassActionKind.PressureTarget, ClassActionDomain.WeaponAction, 99), Block());
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Attack).IsNativeFallback, "remote class acted");
            Assert(x.Arbitration.LeaseFor(DecisionDomain.Defense).IsNativeFallback, "remote defense acted");
        }

        private static void Run(string name, Action test) { test(); _passed++; Console.WriteLine("ok - " + name); }
        private static void Assert(bool condition, string message) { if (!condition) throw new Exception(message); }
    }
}

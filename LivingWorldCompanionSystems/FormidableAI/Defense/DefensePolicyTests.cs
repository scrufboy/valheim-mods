using System;

namespace LivingWorld.FormidableAI.Defense
{
    internal static class DefensePolicyTests
    {
        private static int _passed;

        public static int Main()
        {
            Run("perception ignores query rate", PerceptionIgnoresQueryRate);
            Run("perception resets on target switch", PerceptionResetsOnTargetSwitch);
            Run("unowned perception does not mutate", UnownedPerceptionDoesNotMutate);
            Run("block uses observed strike facts", BlockUsesObservedStrikeFacts);
            Run("unblockable strike chooses dodge", UnblockableStrikeChoosesDodge);
            Run("reaction occurs once per attack", ReactionOccursOncePerAttack);
            Run("cooldown blocks different immediate attack", CooldownBlocksImmediateAttack);
            Run("unowned defense yields to native", UnownedDefenseYieldsToNative);
            Run("incapacitated actor yields to native", IncapacitatedActorYieldsToNative);
            Run("policy emits no movement command", PolicyEmitsNoMovementCommand);
            Console.WriteLine("PASS: " + _passed + " FormidableAI defense tests");
            return 0;
        }

        private static void PerceptionIgnoresQueryRate()
        {
            AwarenessStage a = Simulate(30);
            AwarenessStage b = Simulate(60);
            AwarenessStage c = Simulate(120);
            Check(a == b && b == c && c == AwarenessStage.Alerted, "frame/query-rate changed stage");
        }

        private static AwarenessStage Simulate(int hz)
        {
            PerceptionState state = PerceptionPolicy.Empty();
            for (int i = 0; i < hz; i++)
                state = PerceptionPolicy.Observe(state, new PerceptionObservation("enemy-a", .8f, 1f / hz, true));
            return state.Stage;
        }

        private static void PerceptionResetsOnTargetSwitch()
        {
            PerceptionState state = PerceptionPolicy.Observe(PerceptionPolicy.Empty(),
                new PerceptionObservation("enemy-a", 1f, .7f, true));
            Check(state.Stage == AwarenessStage.Alerted, "first target did not accumulate");
            state = PerceptionPolicy.Observe(state, new PerceptionObservation("enemy-b", 1f, .1f, true));
            Check(state.TargetIdentity == "enemy-b" && state.Stage == AwarenessStage.Unaware,
                "evidence leaked to new target");
        }

        private static void UnownedPerceptionDoesNotMutate()
        {
            PerceptionState state = new PerceptionState("enemy-a", .7f, AwarenessStage.Alerted);
            PerceptionState next = PerceptionPolicy.Observe(state,
                new PerceptionObservation("enemy-a", 100f, 1f, false));
            Check(object.ReferenceEquals(state, next), "unowned perception wrote state");
        }

        private static void BlockUsesObservedStrikeFacts()
        {
            DefenseDecision d = DefensePolicy.Decide(DefenseMemory.Empty(), Strike("attack-1", true,
                true, true, false, .20f, 2f, .9f, 20f, true, false, 10f));
            Check(d.Action == DefenseAction.Block && d.Reason == "block-observed-strike", "valid block not selected");
        }

        private static void UnblockableStrikeChoosesDodge()
        {
            DefenseDecision d = DefensePolicy.Decide(DefenseMemory.Empty(), Strike("attack-2", true,
                false, true, true, .15f, 2f, .8f, 20f, true, false, 10f));
            Check(d.Action == DefenseAction.Dodge, "unblockable strike did not dodge");
        }

        private static void ReactionOccursOncePerAttack()
        {
            StrikeObservation strike = Strike("attack-3", true, true, true, true, .1f, 1f, 1f, 30f, true, false, 4f);
            DefenseDecision first = DefensePolicy.Decide(DefenseMemory.Empty(), strike);
            DefenseDecision repeated = DefensePolicy.Decide(first.NextMemory, strike);
            Check(first.Action == DefenseAction.Block && repeated.Action == DefenseAction.None &&
                repeated.Reason == "attack-already-reacted", "same attack reacted twice");
        }

        private static void CooldownBlocksImmediateAttack()
        {
            DefenseDecision first = DefensePolicy.Decide(DefenseMemory.Empty(), Strike("attack-4", true,
                true, true, true, .1f, 1f, 1f, 30f, true, false, 7f));
            DefenseDecision second = DefensePolicy.Decide(first.NextMemory, Strike("attack-5", true,
                true, true, true, .1f, 1f, 1f, 30f, true, false, 7.1f));
            Check(second.Action == DefenseAction.None && second.Reason == "reaction-cooldown",
                "cooldown did not suppress immediate second reaction");
        }

        private static void UnownedDefenseYieldsToNative()
        {
            DefenseDecision d = DefensePolicy.Decide(DefenseMemory.Empty(), Strike("attack-6", false,
                true, true, true, .1f, 1f, 1f, 30f, true, false, 1f));
            Check(d.Action == DefenseAction.YieldToNative, "unowned defense did not yield");
        }

        private static void IncapacitatedActorYieldsToNative()
        {
            DefenseDecision d = DefensePolicy.Decide(DefenseMemory.Empty(), Strike("attack-7", true,
                true, true, true, .1f, 1f, 1f, 30f, true, true, 1f));
            Check(d.Action == DefenseAction.YieldToNative, "incapacitated defense acted");
        }

        private static void PolicyEmitsNoMovementCommand()
        {
            foreach (var property in typeof(DefenseDecision).GetProperties())
            {
                string name = property.Name.ToLowerInvariant();
                Check(!name.Contains("position") && !name.Contains("direction") && !name.Contains("velocity"),
                    "defense decision exposed movement command: " + property.Name);
            }
        }

        private static StrikeObservation Strike(string attackId, bool owns, bool blockable,
            bool shield, bool dodge, float impact, float distance, float facing, float stamina,
            bool authorized, bool incapacitated, float now)
        {
            return new StrikeObservation("npc-1", "enemy-1", attackId, owns, authorized, true,
                blockable, shield, dodge, incapacitated, now, impact, distance, facing, stamina);
        }

        private static void Run(string name, Action test)
        {
            test(); _passed++; Console.WriteLine("ok - " + name);
        }

        private static void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
    }
}

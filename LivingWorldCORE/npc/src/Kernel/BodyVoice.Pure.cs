// Living World C.O.R.E. - body-reaction voice law (1.0.32.13). Pure, offline-testable.
// Separate addon beside the locked combat voice (1.0.32.6): reactions of the body to a condition, starting with poison
// and gas. One onset cough when the condition begins, a bounded repeat while it lasts, one recovery breath when it
// ends. Combat lines always win; nothing after death. (Brian 2026-10-28: "agreed... proceed".)
using System;

namespace LivingWorld.Core
{
    public enum BodyLine { None, Onset, Repeat, Recover }

    public sealed class BodyVoicePolicy
    {
        public const float Confirm = .3f;            // condition must hold this long (one poison tick can flicker)
        public const float RepeatMin = 4f, RepeatMax = 7f;
        public const float CombatQuiet = 1.2f;       // never start within this of a combat line
        public const float MinForRecover = 1.5f;     // a recovery breath only after real exposure
        public const float CombatDefer = 1.5f;       // an onset waits at most this long for combat voice to finish

        bool active, onsetDone; float since = -1f, nextRepeat, pendingSince = -1f;
        readonly Random rng;

        public BodyVoicePolicy(int seed) { rng = new Random(seed); }

        public bool Active { get { return active; } }

        /// <param name="exposed">the condition (poison status) is present now</param>
        /// <param name="lastCombatLine">time of this companion's last combat-voice line (-inf none)</param>
        public BodyLine Step(float now, bool exposed, bool dead, float lastCombatLine)
        {
            if (dead) { active = false; onsetDone = false; since = -1f; pendingSince = -1f; return BodyLine.None; }
            bool combatQuiet = now - lastCombatLine >= CombatQuiet;
            if (exposed)
            {
                if (since < 0f) since = now;
                if (!active && now - since >= Confirm) { active = true; onsetDone = false; pendingSince = now; }
                if (!active) return BodyLine.None;
                if (!onsetDone)
                {
                    if (combatQuiet) { onsetDone = true; nextRepeat = now + Next(); return BodyLine.Onset; }
                    if (now - pendingSince > CombatDefer) { onsetDone = true; nextRepeat = now + Next(); }   // skipped, not stacked
                    return BodyLine.None;
                }
                if (now >= nextRepeat)
                {
                    nextRepeat = now + Next();
                    return combatQuiet ? BodyLine.Repeat : BodyLine.None;
                }
                return BodyLine.None;
            }
            // condition gone
            bool wasActive = active; float lasted = since < 0f ? 0f : now - since;
            active = false; onsetDone = false; since = -1f; pendingSince = -1f;
            if (wasActive && lasted >= MinForRecover && combatQuiet) return BodyLine.Recover;
            return BodyLine.None;
        }

        float Next() { return RepeatMin + (float)rng.NextDouble() * (RepeatMax - RepeatMin); }
    }
}

// Living World C.O.R.E. - body-state voice law (1.0.32.14). Pure, offline-testable.
// Extends the 1.0.32.13 body-reaction addon with the environment states from ChatGPT's handoff
// (Living_World_CORE_Body_Reactions_COMPLETE_PRIVATE_2026-10-28, BODY_REACTIONS_REQUIREMENTS.md).
// The poison law (BodyVoicePolicy) and the locked combat voice 1.0.32.6 are untouched.
// Triggers are native status owners only (source-inspected): Burning and Frost statuses, Character.IsSwimming,
// and the companion's own heavy-attack stamina pool reaching empty. No damage ticks, no environment guesses.
using System;

namespace LivingWorld.Core
{
    public enum StateLine { None, Onset, Repeat, Recover, Event }

    /// <summary>
    /// A lasting condition with a native status (Burning, Frost): one onset once it holds, sparse repeats while it lasts
    /// (none if RepeatMin is 0), one recovery when it ends after real exposure. A line is only started when the actor is
    /// quiet (no combat line, no other body line); an onset waits at most Defer, then is dropped, never stacked.
    /// </summary>
    public sealed class LastingStatePolicy
    {
        public const float Confirm = .3f, Defer = 1.5f;
        public readonly float RepeatMin, RepeatMax, MinForRecover;
        bool active, onsetDone; float since = -1f, nextRepeat, pendingSince = -1f;
        readonly Random rng;

        public LastingStatePolicy(int seed, float repeatMin, float repeatMax, float minForRecover)
        { rng = new Random(seed); RepeatMin = repeatMin; RepeatMax = repeatMax; MinForRecover = minForRecover; }

        public bool Active { get { return active; } }

        public StateLine Step(float now, bool present, bool dead, bool quiet)
        {
            if (dead) { Reset(); return StateLine.None; }
            if (present)
            {
                if (since < 0f) since = now;
                if (!active && now - since >= Confirm) { active = true; onsetDone = false; pendingSince = now; }
                if (!active) return StateLine.None;
                if (!onsetDone)
                {
                    if (quiet) { onsetDone = true; nextRepeat = now + Next(); return StateLine.Onset; }
                    if (now - pendingSince > Defer) { onsetDone = true; nextRepeat = now + Next(); }
                    return StateLine.None;
                }
                if (RepeatMin > 0f && now >= nextRepeat)
                {
                    nextRepeat = now + Next();
                    return quiet ? StateLine.Repeat : StateLine.None;
                }
                return StateLine.None;
            }
            bool was = active; float lasted = since < 0f ? 0f : now - since;
            Reset();
            return was && lasted >= MinForRecover && quiet ? StateLine.Recover : StateLine.None;
        }

        void Reset() { active = false; onsetDone = false; since = -1f; pendingSince = -1f; }
        float Next() { return RepeatMin + (float)rng.NextDouble() * (RepeatMax - RepeatMin); }
    }

    /// <summary>
    /// A one-shot body event (stamina ran out; came out of a long swim): at most one line per event, then a cooldown.
    /// An event that cannot be voiced within Defer (combat line, other body line) is dropped, not queued.
    /// </summary>
    public sealed class EventStatePolicy
    {
        public const float Defer = 1.5f;
        public readonly float Cooldown;
        float last = float.NegativeInfinity, pending = -1f;

        public EventStatePolicy(float cooldown) { Cooldown = cooldown; }

        public StateLine Step(float now, bool happened, bool dead, bool quiet)
        {
            if (dead) { pending = -1f; return StateLine.None; }
            if (happened && pending < 0f && now - last >= Cooldown) pending = now;
            if (pending >= 0f && now - pending > Defer) pending = -1f;   // too late: dropped
            if (pending < 0f) return StateLine.None;
            if (quiet) { pending = -1f; last = now; return StateLine.Event; }
            return StateLine.None;
        }
    }

    /// <summary>
    /// Long-swim tracker. Being wet or a short swim is not breathless: only a continuous swim of at least MinSwim seconds
    /// counts, and the catch-breath comes once, when the actor leaves the water. Bobbing out for under Gap seconds does not
    /// end the swim.
    /// </summary>
    public sealed class SwimTracker
    {
        public readonly float MinSwim, Gap;
        float start = -1f, lastIn = -1f;

        public SwimTracker(float minSwim, float gap) { MinSwim = minSwim; Gap = gap; }

        /// <returns>true once, when a long swim has just ended</returns>
        public bool Step(float now, bool swimming, bool dead)
        {
            if (dead) { start = -1f; return false; }
            if (swimming) { if (start < 0f) start = now; lastIn = now; return false; }
            if (start < 0f || now - lastIn < Gap) return false;
            bool longSwim = lastIn - start >= MinSwim;
            start = -1f;
            return longSwim;
        }
    }

    /// <summary>
    /// One actor, one voice. Body-state lines only start when nothing of this actor's is sounding (combat lines and
    /// poison lines included), with a minimum gap between any two body lines. The quiet "condition over" exhale is
    /// rarer still: at most one per RecoverCooldown across all states.
    /// </summary>
    public sealed class BodyActorGate
    {
        public const float MinGap = 2.5f, RecoverCooldown = 30f, CombatQuiet = 1.2f;
        float busyUntil = float.NegativeInfinity, lastStart = float.NegativeInfinity, lastRecover = float.NegativeInfinity;

        public bool Quiet(float now, float lastCombatLine)
        { return now >= busyUntil && now - lastStart >= MinGap && now - lastCombatLine >= CombatQuiet; }

        public bool RecoverAllowed(float now) { return now - lastRecover >= RecoverCooldown; }

        public void Started(float now, float length, bool recover)
        { lastStart = now; busyUntil = now + (length > 0f ? length : 0f); if (recover) lastRecover = now; }

        public void Cut(float now) { if (busyUntil > now) busyUntil = now; }
    }

    /// <summary>Clip choice inside a pool: random, never the same clip twice in a row when the pool has two or more.</summary>
    public sealed class ClipPicker
    {
        readonly Random rng; int last = -1;
        public ClipPicker(int seed) { rng = new Random(seed); }
        public int Pick(int count)
        {
            if (count <= 0) return -1;
            if (count == 1) { last = 0; return 0; }
            int i;
            if (last < 0 || last >= count) i = rng.Next(count);
            else { i = rng.Next(count - 1); if (i >= last) i++; }
            last = i; return i;
        }
    }
}

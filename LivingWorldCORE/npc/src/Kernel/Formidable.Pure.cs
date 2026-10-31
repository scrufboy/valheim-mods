// Living World C.O.R.E. - FormidableAI A2 corrected candidate: initiative, role-aware tactics,
// strike-window defense and fallback lanes. Character identity is read, never created or rewritten here.
//
// Pure decision layer: no Unity, no Valheim. The runtime samples facts, these rules decide, NpcBody executes.
// Every number here is a TEST PARAMETER chosen conservatively for the first live session; telemetry reports each one.
using System;
using System.Collections.Generic;

namespace LivingWorld.Core
{
    // ------------------------------------------------------------------------------------------------------------
    // A2.2 Existing-role combat doctrine
    // ------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A tactical reading of the creation role already owned by the character. This is not character identity,
    /// is never persisted, and is never inferred from equipment. Unknown preserves an authored role that this
    /// combat revision does not yet know how to specialize.
    /// </summary>
    public enum CombatRole { Unknown, Guard, Archer, Scout }

    /// <summary>
    /// What a class has been trained to prefer. The tactical core reads these preferences together with the actual
    /// loadout; a preference never creates a capability (no shield in the inventory = no block, whatever the class).
    /// </summary>
    public struct CombatDoctrine
    {
        public CombatRole Role;
        public string RoleName;
        // Weapon bands (ground targets). Bow at/after BowFrom, melee under MeleeUnder, hysteresis between.
        public float BowFrom, MeleeUnder;   // the class's own bow minimum (native AI will not fire inside it) is MeleeUnder
        // Initiative: a committed contact is engaged (target taken) inside EngageRange of me; beyond it the class holds
        // readiness in formation. Always under TargetPolicy.MaxRange (30 m), so the native keep/drop rule never fights it. Leash: a new target farther than this from the leader is not chased (self/partner
        // defence excepted).
        public float EngageRange, Leash;
        // Defense preferences, 0..1 (weights, not dice).
        public float Parry, Interrupt, Evade, Absorb;
        // Fall-back lane preference: prefer a lane that ends near the partner (cover) over pure distance.
        public float CoverWeight;
        public bool RangedOpening;   // takes committed contacts at bow range as an opening
    }

    public static class CombatDoctrines
    {
        public const float SwapCommit = 1.2f;    // a closing target this soon is met with the weapon in hand (no swap)

        public static CombatDoctrine ForRole(string role)
        {
            string normalized = string.IsNullOrEmpty(role) ? "" : role.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "archer":
                    return new CombatDoctrine { Role = CombatRole.Archer, RoleName = "Archer", BowFrom = 6f, MeleeUnder = 4f, EngageRange = 28f, Leash = 28f,
                        Parry = .2f, Interrupt = .1f, Evade = .8f, Absorb = .1f, CoverWeight = .8f, RangedOpening = true };
                case "scout":
                    return new CombatDoctrine { Role = CombatRole.Scout, RoleName = "Scout", BowFrom = 10f, MeleeUnder = 5f, EngageRange = 24f, Leash = 22f,
                        Parry = .3f, Interrupt = .7f, Evade = .9f, Absorb = .1f, CoverWeight = .4f, RangedOpening = true };
                case "guard":
                    return new CombatDoctrine { Role = CombatRole.Guard, RoleName = "Guard", BowFrom = 12f, MeleeUnder = 7f, EngageRange = 14f, Leash = 14f,
                        Parry = .7f, Interrupt = .3f, Evade = .2f, Absorb = .3f, CoverWeight = .6f, RangedOpening = false };
                default:
                    // Neutral behavior for an authored role not yet specialized. Crucially, this does not call it a Guard.
                    return new CombatDoctrine { Role = CombatRole.Unknown, RoleName = string.IsNullOrEmpty(role) ? "Unknown" : role,
                        BowFrom = 10f, MeleeUnder = 7f, EngageRange = 18f, Leash = 18f,
                        Parry = .4f, Interrupt = .4f, Evade = .5f, Absorb = .3f, CoverWeight = .5f, RangedOpening = false };
            }
        }

        /// <summary>The weapon for the current target, by class bands. Flying: bow if usable. A target closing on me this soon
        /// is met with the weapon already in hand (a swap would be caught mid-change).</summary>
        public static WeaponChoice.Pick Weapon(CombatDoctrine d, bool hasBow, bool hasAmmo, bool hasMelee, bool holdingBow, bool holdingMelee,
            float distance, bool targetFlying, float timeToContact)
        {
            bool bowUsable = hasBow && hasAmmo;
            if (!bowUsable) return hasMelee ? WeaponChoice.Pick.Melee : WeaponChoice.Pick.Keep;
            if (targetFlying || !hasMelee) return WeaponChoice.Pick.Bow;
            if (timeToContact < SwapCommit && distance < d.BowFrom + 4f) return holdingBow || holdingMelee ? WeaponChoice.Pick.Keep : WeaponChoice.Pick.Melee;
            if (distance >= d.BowFrom) return WeaponChoice.Pick.Bow;
            if (distance < d.MeleeUnder) return WeaponChoice.Pick.Melee;
            if (holdingBow || holdingMelee) return WeaponChoice.Pick.Keep;
            return distance >= (d.BowFrom + d.MeleeUnder) * .5f ? WeaponChoice.Pick.Bow : WeaponChoice.Pick.Melee;
        }
    }

    /// <summary>
    /// Packaging boundary for a future Living World Hostile Peoples pack (3e5ac3c). A content pack supplies doctrine for its own
    /// actors through this interface; it never forks awareness, arbitration or defense. Companions use the built-in source.
    /// </summary>
    public interface IFormidableProfileSource
    {
        /// <summary>Doctrine for an actor this source owns (by prefab/family name), or false.</summary>
        bool TryGetDoctrine(string family, out CombatDoctrine doctrine);
    }

    public static class FormidableProfiles
    {
        static readonly List<IFormidableProfileSource> sources = new List<IFormidableProfileSource>();
        public static void Register(IFormidableProfileSource source) { if (source != null && !sources.Contains(source)) sources.Add(source); }
        public static int Count { get { return sources.Count; } }

        public static bool TryGet(string family, out CombatDoctrine doctrine)
        {
            for (int i = 0; i < sources.Count; i++) if (sources[i].TryGetDoctrine(family, out doctrine)) return true;
            doctrine = default(CombatDoctrine);
            return false;
        }
    }

    // ------------------------------------------------------------------------------------------------------------
    // A2.1 Initiative: the shared awareness frame becomes a prompt, class-aware response
    // ------------------------------------------------------------------------------------------------------------

    public enum InitiativeResponse { None, Observe, Ready, HoldStealth, Engage }

    /// <summary>One awareness contact as one member's initiative sees it (from the party's ContactBook; no world search).</summary>
    public struct InitiativeOption
    {
        public int Id;
        public ContactState State;
        public float Distance;          // from this member
        public float LeaderDistance;    // contact to the leader, -1 if no leader
        public float TimeToContact;     // to the party, +inf if not closing
        public bool TargetsMe, TargetsLeader, TargetsPartner, HurtUs, Wildlife;
    }

    /// <summary>
    /// Who: this member. What: observe, ready, hold stealth or engage. When: on the awareness transition (10 Hz), not on the
    /// native 2 s re-pick. Where: the contact chosen. Why: the reason string. How: class doctrine (engage range, leash,
    /// opening at range). Commitment never waits for native target acquisition.
    /// </summary>
    public static class InitiativePolicy
    {
        public const float SelfDefense = 6f;     // anything committed on me this close is engaged whatever the class
        public const float GuardLimit = 29f;     // under TargetPolicy.MaxRange (30): an engaged target is never dropped by the keep rule

        public static InitiativeResponse Response(ContactState strongest, bool leaderSneaking, bool engageable)
        {
            if (strongest == ContactState.Committed) return engageable ? InitiativeResponse.Engage : leaderSneaking ? InitiativeResponse.HoldStealth : InitiativeResponse.Ready;
            if (strongest == ContactState.Alerted) return leaderSneaking ? InitiativeResponse.HoldStealth : InitiativeResponse.Ready;
            if (strongest == ContactState.Noticed) return InitiativeResponse.Observe;
            return InitiativeResponse.None;
        }

        /// <summary>The committed contact this member should engage now, or -1. strongest = strongest non-wildlife state seen.</summary>
        public static int Choose(IList<InitiativeOption> options, CombatDoctrine d, out ContactState strongest, out string why)
        {
            strongest = ContactState.None;
            why = "nothing committed";
            int best = -1;
            float bestScore = float.NegativeInfinity;
            if (options == null) return -1;
            for (int i = 0; i < options.Count; i++)
            {
                InitiativeOption o = options[i];
                bool threat = !o.Wildlife || o.HurtUs;
                if (threat && o.State != ContactState.Lost && o.State > strongest) strongest = o.State;
                if (o.State != ContactState.Committed || !threat) continue;
                bool onMe = (o.TargetsMe || o.HurtUs) && o.Distance <= SelfDefense;
                bool inReach = o.Distance <= d.EngageRange;
                bool leashed = o.LeaderDistance < 0f || o.LeaderDistance <= d.Leash;
                bool guarding = (o.TargetsLeader || o.TargetsPartner) && o.Distance <= Math.Min(d.EngageRange + 6f, GuardLimit);   // intercept for a protected member
                if (!onMe && !(inReach && leashed) && !guarding) continue;
                float score = onMe ? 1000f : 0f;
                if (o.HurtUs) score += 120f;
                if (o.TargetsLeader) score += d.Role == CombatRole.Guard ? 90f : 60f;
                if (o.TargetsPartner) score += d.Role == CombatRole.Guard ? 75f : 50f;
                if (o.TargetsMe) score += 45f;
                if (d.RangedOpening && o.Distance >= d.BowFrom) score += 20f;     // the opening shot is the archer's job
                score -= o.Distance * (d.RangedOpening ? .5f : 2f);
                if (!float.IsInfinity(o.TimeToContact)) score += Math.Max(0f, 10f - o.TimeToContact) * 2f;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0)
            {
                InitiativeOption o = options[best];
                why = o.Distance <= SelfDefense && (o.TargetsMe || o.HurtUs) ? "on me" : o.HurtUs ? "hurt us" : o.TargetsLeader ? "intercept: on leader"
                    : o.TargetsPartner ? "intercept: on partner" : d.RangedOpening && o.Distance >= d.BowFrom ? "ranged opening" : "committed in reach";
            }
            else if (strongest == ContactState.Committed) why = "committed beyond " + d.RoleName + " reach: ready in formation";
            return best;
        }
    }

    // ------------------------------------------------------------------------------------------------------------
    // A2.3 Strike-window defense
    // ------------------------------------------------------------------------------------------------------------

    /// <summary>One incoming strike as fairly perceivable: the attacker visibly started an attack aimed at me.</summary>
    public struct StrikeObservation
    {
        public int AttackerId;
        public float Distance;       // attacker edge to me
        public float Reach;          // the attack's own range
        public bool AimedAtMe;       // its target is me, or it faces me within reach
        public bool Ranged;
        public float Elapsed;        // seconds since this attack started (native attack timer)
        public float Windup;         // expected start->hit time for this attack (learned or prior)
        public bool WindupLearned;   // Windup comes from observed strikes of this attack, not the prior
        public float ProjectileSpeed;
        public bool Heavy;
    }

    /// <summary>What this companion can do right now (actual loadout and body state).</summary>
    public struct DefenseCapability
    {
        public bool ShieldInHand;
        public bool BusyOwnAttack;     // mid-swing / drawing: cannot raise
        public float OwnReach;         // melee weapon reach, 0 if none in hand
        public float OwnWindup;        // own melee start->hit, seconds
        public bool LaneFree;          // a side-step lane exists (evade)
        public float Health01;
        public float Reaction;         // per-person 0.12 - 0.30 s
    }

    public enum DefenseChoice { None, Block, Parry, Evade, Interrupt, Counter, Absorb }

    public struct DefensePlan
    {
        public DefenseChoice Choice;
        public int AttackerId;
        public float ImpactIn;    // seconds from now
        public float ActIn;       // when to act (raise / step / swing), seconds from now
        public float HoldFor;     // block/parry: how long after acting to keep the shield up
        public string Why;
    }

    /// <summary>
    /// One decision per incoming strike: Block (raise before impact, hold through it), Parry (raise inside the native perfect-block
    /// window before impact), Evade (side-step lane), Interrupt (own hit lands first), Absorb (take it). Counter follows a
    /// successful block or parry. No timer cadence: nothing is raised without a predicted impact.
    /// </summary>
    public static class DefenseArbiter
    {
        public const float WindupPrior = .55f, HeavyWindupPrior = .85f, Unknown = .55f;
        public const float BlockLead = .35f;      // block raised this long before predicted impact
        public const float ParryLead = .20f;      // inside Valheim's 0.25 s perfect-block interval; the 0.1 s tick raises 0.10-0.20 s before impact
        public const float PerfectBlockInterval = .25f;
        public const float HoldAfter = .25f;      // keep the shield up past the predicted impact (timing error)
        public const float EvadeMinimum = .35f;   // a step needs this long to clear the arc
        public const float TickDelay = .1f;       // the kernel acts on the next 0.1 s decision tick
        public const float InterruptMargin = .1f;
        public const float ReachSlack = 1.2f;

        public static float ImpactIn(StrikeObservation s)
        {
            float windupLeft = Math.Max(0f, s.Windup - s.Elapsed);
            if (!s.Ranged) return windupLeft;
            float speed = s.ProjectileSpeed > 1f ? s.ProjectileSpeed : 30f;
            return windupLeft + Math.Max(0f, s.Distance) / speed;
        }

        public static bool Threatens(StrikeObservation s)
        {
            if (!s.AimedAtMe) return false;
            if (s.Ranged) return s.Distance <= 40f;
            return s.Distance <= s.Reach + ReachSlack;
        }

        public static DefensePlan Plan(StrikeObservation s, DefenseCapability me, CombatDoctrine d)
        {
            var p = new DefensePlan { AttackerId = s.AttackerId, ImpactIn = ImpactIn(s), Choice = DefenseChoice.None, Why = "no threat" };
            if (!Threatens(s)) return p;
            float t = p.ImpactIn;
            float reaction = me.Reaction;
            // Interrupt: my swing lands first, in reach, and the class takes that trade.
            if (!s.Ranged && !me.BusyOwnAttack && me.OwnReach > 0f && s.Distance <= me.OwnReach + .5f &&
                reaction + me.OwnWindup + InterruptMargin < t && d.Interrupt >= .5f && !s.Heavy)
            {
                p.Choice = DefenseChoice.Interrupt; p.ActIn = 0f; p.Why = "my hit lands first"; return p;
            }
            if (me.ShieldInHand && !me.BusyOwnAttack)
            {
                // Parry needs a trusted timing: a learned windup, and time to react before the perfect-block window.
                if (d.Parry >= .5f && s.WindupLearned && !s.Ranged && t >= reaction + ParryLead)
                {
                    p.Choice = DefenseChoice.Parry; p.ActIn = Math.Max(reaction, t - ParryLead);
                    p.HoldFor = (t - p.ActIn) + HoldAfter; p.Why = "parry window"; return p;
                }
                if (t >= reaction || s.Ranged)
                {
                    p.Choice = DefenseChoice.Block; p.ActIn = Math.Max(reaction, t - BlockLead);
                    if (p.ActIn > t) p.ActIn = reaction;
                    p.HoldFor = Math.Max(0f, t - p.ActIn) + HoldAfter; p.Why = s.Ranged ? "block projectile" : "block strike"; return p;
                }
            }
            if (me.LaneFree && t >= reaction + TickDelay + EvadeMinimum && (d.Evade >= .5f || s.Heavy || me.Health01 < .35f))
            {
                p.Choice = DefenseChoice.Evade; p.ActIn = reaction; p.Why = s.Heavy ? "evade heavy" : "evade strike"; return p;
            }
            p.Choice = DefenseChoice.Absorb; p.ActIn = 0f;
            p.Why = t < reaction ? "too late to react" : me.BusyOwnAttack ? "committed to own swing" : "no shield or lane";
            return p;
        }

        /// <summary>After a defended strike: counter if the attacker is in my reach and I am free (one swing, not a loop).</summary>
        public static bool Counter(bool defended, bool attackerStaggered, float distance, float ownReach, bool busy)
        {
            return defended && !busy && ownReach > 0f && distance <= ownReach + .5f && (attackerStaggered || distance <= ownReach);
        }
    }

    /// <summary>
    /// Observed strike timings (start -> hit frame) per attack, learned from strikes the party sees (ObservedCombat provenance).
    /// Before enough samples the prior is used and the window is flagged unlearned (no parry is attempted on an unlearned attack).
    /// </summary>
    public sealed class StrikeTimingBook
    {
        public const int MinSamples = 3, Capacity = 128;
        struct Entry { public string Key; public float Mean; public int Samples; }
        readonly Entry[] entries = new Entry[Capacity];
        int count;
        public int Count { get { return count; } }

        public void Learn(string key, float hitTime)
        {
            if (string.IsNullOrEmpty(key) || hitTime <= .05f || hitTime > 3f) return;
            for (int i = 0; i < count; i++)
                if (entries[i].Key == key)
                {
                    int n = Math.Min(entries[i].Samples + 1, 20);
                    entries[i].Mean += (hitTime - entries[i].Mean) / n;
                    entries[i].Samples = entries[i].Samples + 1;
                    return;
                }
            if (count >= Capacity) return;
            entries[count++] = new Entry { Key = key, Mean = hitTime, Samples = 1 };
        }

        public float Windup(string key, bool heavy, out bool learned)
        {
            for (int i = 0; i < count; i++)
                if (entries[i].Key == key && entries[i].Samples >= MinSamples) { learned = true; return entries[i].Mean; }
            learned = false;
            return heavy ? DefenseArbiter.HeavyWindupPrior : DefenseArbiter.WindupPrior;
        }

        public int Samples(string key)
        {
            for (int i = 0; i < count; i++) if (entries[i].Key == key) return entries[i].Samples;
            return 0;
        }
    }

    /// <summary>
    /// The per-companion defense window. A window opens for one attacker strike (attacker id + strike start), acts at the planned
    /// time, holds through the predicted impact, closes, and is never re-opened for the same strike. Raising requires a plan.
    /// </summary>
    public sealed class DefenseWindow
    {
        public bool Open { get; private set; }
        public DefensePlan Plan { get; private set; }
        public float OpenedAt { get; private set; }
        public float ImpactAt { get; private set; }
        public float ActAt { get; private set; }
        public float ReleaseAt { get; private set; }
        public bool Acted { get; private set; }
        public int Windows { get; private set; }
        int lastAttacker; float lastStrikeStart = float.NegativeInfinity;

        /// <summary>Open a window for this strike if it is new. strikeStart identifies the strike (now - elapsed).</summary>
        public bool Offer(float now, float strikeStart, DefensePlan plan)
        {
            if (plan.Choice == DefenseChoice.None) return false;
            if (plan.AttackerId == lastAttacker && Math.Abs(strikeStart - lastStrikeStart) < .15f) return false;   // same strike
            if (Open) return false;                                  // one window at a time: the current strike is answered first
            lastAttacker = plan.AttackerId; lastStrikeStart = strikeStart;
            Plan = plan; Open = true; Acted = false; Windows++;
            OpenedAt = now; ImpactAt = now + plan.ImpactIn; ActAt = now + plan.ActIn;
            ReleaseAt = plan.Choice == DefenseChoice.Block || plan.Choice == DefenseChoice.Parry ? ActAt + plan.HoldFor : ImpactAt + DefenseArbiter.HoldAfter;
            return true;
        }

        /// <summary>Keep the window (and the guard) until at least this time: an unlearned, slower attack still swinging.</summary>
        public void ExtendRelease(float until) { if (Open && until > ReleaseAt) ReleaseAt = until; }

        /// <summary>Whether the shield should be up now (block / parry only).</summary>
        public bool ShieldUp(float now)
        {
            if (!Open) return false;
            bool shield = Plan.Choice == DefenseChoice.Block || Plan.Choice == DefenseChoice.Parry;
            return shield && now >= ActAt && now < ReleaseAt;
        }

        /// <summary>True once, at the moment to act (step / swing / raise).</summary>
        public bool Due(float now)
        {
            if (!Open || Acted || now < ActAt) return false;
            Acted = true;
            return true;
        }

        /// <summary>Close after release; returns true on the closing tick.</summary>
        public bool Expire(float now)
        {
            if (!Open || now < ReleaseAt) return false;
            Open = false;
            return true;
        }

        public void Cancel() { Open = false; }
    }

    // ------------------------------------------------------------------------------------------------------------
    // A2.4 Fallback lanes
    // ------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Choose a viable fall-back lane before moving: 8 directions, each a straight step. Excluded: blocked by terrain/water/obstacle,
    /// or passing within PassClearance of a threat. Scored by how far the lane end is from the nearest threat (allowing for its
    /// approach), plus a cover bonus for ending near the partner or leader. No lane: stand and fight (a decision, not a blind run).
    /// </summary>
    public static class LanePlanner
    {
        public const int Directions = 8;
        public const float Step = 6f, PassClearance = 2.5f, ThreatLead = .8f;

        public static int Choose(P2 me, P2[] threats, P2[] threatVel, int threatCount, bool[] blocked, bool hasCover, P2 cover,
            float coverWeight, out float bestScore, out string why)
        {
            bestScore = float.NegativeInfinity;
            why = "no lane";
            int best = -1;
            for (int i = 0; i < Directions; i++)
            {
                float score = Score(i, me, threats, threatVel, threatCount, blocked, hasCover, cover, coverWeight);
                if (float.IsNaN(score)) continue;
                if (score > bestScore) { bestScore = score; best = i; }
            }
            if (best >= 0) why = hasCover && coverWeight > 0f ? "lane toward cover" : "open lane";
            return best;
        }

        /// <summary>One lane's score now, NaN when excluded (blocked, or it passes a threat). Used to re-check the lane in use.</summary>
        public static float Score(int i, P2 me, P2[] threats, P2[] threatVel, int threatCount, bool[] blocked, bool hasCover, P2 cover, float coverWeight)
        {
            if (i < 0 || i >= Directions || (blocked != null && i < blocked.Length && blocked[i])) return float.NaN;
            P2 end = me + Direction(i) * Step;
            float nearest = float.PositiveInfinity;
            for (int t = 0; t < threatCount; t++)
            {
                P2 future = threats[t] + (threatVel == null ? new P2(0f, 0f) : threatVel[t] * ThreatLead);
                if (SegmentDistance(me, end, threats[t]) < PassClearance && P2.Distance(me, threats[t]) > PassClearance) return float.NaN;
                nearest = Math.Min(nearest, P2.Distance(end, future));
            }
            float score = threatCount == 0 ? 0f : nearest;
            if (hasCover) score += coverWeight * (P2.Distance(me, cover) - P2.Distance(end, cover));
            return score;
        }

        public static P2 Direction(int i)
        {
            double a = i * Math.PI * 2d / Directions;
            return new P2((float)Math.Sin(a), (float)Math.Cos(a));
        }

        public static float SegmentDistance(P2 a, P2 b, P2 p)
        {
            P2 ab = b - a;
            float len2 = P2.Dot(ab, ab);
            if (len2 < 1e-6f) return P2.Distance(a, p);
            float t = Math.Max(0f, Math.Min(1f, P2.Dot(p - a, ab) / len2));
            return P2.Distance(a + ab * t, p);
        }

        /// <summary>Evade: the side step (left or right of the attacker line) that is free; prefers the side toward cover.</summary>
        public static int SideStep(P2 me, P2 attacker, bool[] blocked, bool hasCover, P2 cover)
        {
            P2 away = (me - attacker).Normalized;
            if (away.Length < .5f) away = new P2(0f, 1f);
            P2 left = away.Left, right = left * -1f;
            int li = Nearest(left), ri = Nearest(right);
            bool lFree = blocked == null || !blocked[li], rFree = blocked == null || !blocked[ri];
            if (lFree && rFree && hasCover) return P2.Distance(me + left, cover) <= P2.Distance(me + right, cover) ? li : ri;
            if (lFree) return li;
            if (rFree) return ri;
            return -1;
        }

        public static int Nearest(P2 dir)
        {
            int best = 0; float bestDot = float.NegativeInfinity;
            for (int i = 0; i < Directions; i++) { float d = P2.Dot(Direction(i), dir); if (d > bestDot) { bestDot = d; best = i; } }
            return best;
        }
    }

    // ------------------------------------------------------------------------------------------------------------
    // Arbitration priority (reported; the kernel schedules domains in this order)
    // ------------------------------------------------------------------------------------------------------------

    public enum FormidablePriority { Defend = 1, CoverPartner = 2, OpeningAreaControl = 3, Attack = 4, Reposition = 5, Observe = 6 }

}

// Living World C.O.R.E. — NPC kernel, pure decision layer.
//
// Nothing in this file touches Unity or Valheim. Every rule that decides
// "what is this NPC doing right now" lives here so it can be tested offline
// and so no gameplay module can quietly reinterpret it.
using System;
using System.Collections.Generic;

namespace LivingWorld.Core
{
    /// <summary>Exclusive write domains. Exactly one behavior may hold each at a time.</summary>
    [Flags]
    public enum NpcDomain
    {
        None = 0,
        Movement = 1,     // follow target, pace, encounter release
        Hands = 2,        // hand/back items, torch, weapon handoff
        Look = 4,         // explicit look direction
        Posture = 8,      // crouch, sit
        All = Movement | Hands | Look | Posture
    }

    /// <summary>The one top-level situation the NPC is in.</summary>
    public enum NpcPhase
    {
        Initializing,   // identity/appearance not complete
        AwaitingLeader, // no bound leader resolved
        Calm,           // travelling or settled with the leader
        Readiness,      // leader drew a weapon or native alert without a target
        Combat,         // native MonsterAI owns the fight
        CombatClear,    // no threat; verifying for the clear interval before standing down
        CombatRegroup,  // too far from the party during a fight; return, defend only locally
        Regroup         // fight is over; gather back into formation
    }

    /// <summary>Location/role context. Only FieldCompanion is implemented today.</summary>
    public enum NpcContext { FieldCompanion }

    /// <summary>All tunable numbers in one place. Acceptance parameters, not scattered constants.</summary>
    public static class Tuning
    {
        public const float DecisionInterval = .1f;

        // Combat clear: contract requires five uninterrupted quiet seconds.
        public const float ClearSeconds = 5f;
        public const float DamageMemorySeconds = 5f;

        // Party cohesion during combat (actor distances, never enemy range).
        public const int CohesionOutsideTicks = 10;
        public const float CohesionOuterLeader = 16f;
        public const float CohesionOuterParty = 18f;
        public const float CohesionPairLeaderFloor = 8f;
        public const float CohesionInnerLeader = 8f;
        public const float CohesionInnerParty = 12f;
        // While returning, a companion may still fight anything this close that is hurting it.
        public const float LocalDefenseRadius = 6f;
        // 1.0.5 live: companions left fights to rejoin the leader with enemies 2-6 m away.
        // An engaged enemy (it picked or hurt one of us) this close pins the companion to the fight.
        public const float EngagedPinRadius = 10f;
        public const float LocalDefenseMemory = 1.5f;

        // Regroup / catch-up.
        public const float RegroupArrival = 2.4f;
        public const float CatchUpEnter = 12f;
        public const float CatchUpRelease = 7f;

        // Torch.
        public const float TorchOnDelay = 3f;
        public const float TorchOffDelayForLight = 3f;
        public const float TorchAfterCombat = 5f;
        public const float BuiltLightEnter = 6f;
        public const float BuiltLightExit = 10f;

        // Awareness scan.
        public const float HostileScanInterval = .5f;
        public const float HostileScanRadius = 30f;
    }

    /// <summary>Plain facts sampled once per decision tick. No Unity types.</summary>
    public struct PhaseInputs
    {
        public bool AppearanceReady;
        public bool HasLeader;
        public bool LeaderArmed;
        public bool NativeAlert;
        public bool HasTarget;          // native creature or static target
        public bool DamagedRecently;    // within DamageMemorySeconds
        public bool AlertedHostileNear; // any alerted enemy inside the awareness radius
        public bool LocalThreat;        // an engaged enemy within EngagedPinRadius: stay and fight
        public float LeaderDistance;
        public float NearestPartyDistance; // < 0 when alone
        public float FormationDistance;    // distance to leased formation slot
    }

    /// <summary>
    /// Party cohesion latch. Separation is measured between actors only.
    /// Entering the latch starts one return; it clears only inside the inner area.
    /// Unlike 0.6.5, a returning actor may still defend itself against anything
    /// close enough to be hurting it.
    /// </summary>
    public sealed class CohesionLatch
    {
        int outsideTicks;
        public bool Returning { get; private set; }
        public bool JustEntered { get; private set; }
        public int OutsideTicks { get { return outsideTicks; } }

        public bool Observe(bool inCombat, float leaderDistance, float partyDistance)
        {
            return Observe(inCombat, leaderDistance, partyDistance, false);
        }

        /// <param name="pinned">an engaged enemy is close: no return starts, and a return under way ends.</param>
        public bool Observe(bool inCombat, float leaderDistance, float partyDistance, bool pinned)
        {
            JustEntered = false;
            if (pinned) { Returning = false; outsideTicks = 0; return false; }
            if (Returning)
            {
                bool partyInside = partyDistance < 0f || partyDistance <= Tuning.CohesionInnerParty;
                if (leaderDistance <= Tuning.CohesionInnerLeader && partyInside)
                {
                    Returning = false;
                    outsideTicks = 0;
                }
                return Returning;
            }
            if (!inCombat) { outsideTicks = 0; return false; }
            bool partySeparated = partyDistance >= 0f && partyDistance > Tuning.CohesionOuterParty &&
                leaderDistance > Tuning.CohesionPairLeaderFloor;
            bool outside = leaderDistance > Tuning.CohesionOuterLeader || partySeparated;
            outsideTicks = outside ? outsideTicks + 1 : 0;
            if (outsideTicks >= Tuning.CohesionOutsideTicks)
            {
                Returning = true;
                JustEntered = true;
                outsideTicks = 0;
            }
            return Returning;
        }

        public void Reset() { Returning = false; JustEntered = false; outsideTicks = 0; }
    }

    /// <summary>
    /// The phase arbiter. The only code that decides the NPC's top-level phase.
    /// </summary>
    public sealed class PhaseArbiter
    {
        readonly CohesionLatch cohesion = new CohesionLatch();
        bool inEncounter;
        float quietSeconds;
        float secondsSinceEncounter = float.PositiveInfinity;

        public NpcPhase Phase { get; private set; }
        public string Reason { get; private set; }
        public float QuietSeconds { get { return quietSeconds; } }
        public float SecondsSinceEncounter { get { return secondsSinceEncounter; } }
        public CohesionLatch Cohesion { get { return cohesion; } }

        public PhaseArbiter() { Phase = NpcPhase.Initializing; Reason = "starting"; }

        public NpcPhase Step(PhaseInputs s, float dt)
        {
            if (!s.AppearanceReady) return Set(NpcPhase.Initializing, "identity not ready");

            bool threat = s.HasTarget || s.DamagedRecently;
            bool returning = cohesion.Observe(inEncounter || threat, s.HasLeader ? s.LeaderDistance : 0f,
                s.NearestPartyDistance, s.LocalThreat);

            if (threat)
            {
                inEncounter = true;
                quietSeconds = 0f;
                secondsSinceEncounter = 0f;
            }

            if (inEncounter)
            {
                if (returning && !s.LocalThreat) return Set(NpcPhase.CombatRegroup, "party separation; returning");
                if (threat) return Set(NpcPhase.Combat, returning ? "local defense while returning" :
                    s.HasTarget ? "native target" : "taking damage");

                // Verified clear: five uninterrupted seconds with no target, no damage
                // and no alerted hostile in the awareness radius.
                // The NPC's own native alert flag can linger long after a fight; an
                // actual alerted enemy inside the awareness radius is the evidence.
                if (s.AlertedHostileNear) quietSeconds = 0f;
                else quietSeconds += dt;
                if (quietSeconds + 1e-3f < Tuning.ClearSeconds)
                    return Set(NpcPhase.CombatClear, s.AlertedHostileNear ? "alerted hostile still near" : "verifying clear");
                inEncounter = false;
                cohesion.Reset();
                quietSeconds = 0f;
                return Set(NpcPhase.Regroup, "verified clear");
            }

            if (!float.IsPositiveInfinity(secondsSinceEncounter)) secondsSinceEncounter += dt;
            if (!s.HasLeader) return Set(NpcPhase.AwaitingLeader, "no leader");
            if (s.LeaderArmed) return Set(NpcPhase.Readiness, "leader armed");
            if (s.NativeAlert) return Set(NpcPhase.Readiness, "native alert, no target");
            if (Phase == NpcPhase.Regroup && s.FormationDistance > Tuning.RegroupArrival)
                return Set(NpcPhase.Regroup, "returning to formation");
            return Set(NpcPhase.Calm, "calm");
        }

        NpcPhase Set(NpcPhase phase, string reason)
        {
            Phase = phase;
            Reason = reason;
            return phase;
        }
    }

    /// <summary>
    /// Grants domains in priority order. A behavior is granted only if it wants to run
    /// and none of its domains is already granted to a higher-priority behavior.
    /// All-or-nothing: a behavior never gets a partial claim.
    /// </summary>
    public static class DomainScheduler
    {
        public static void Resolve(bool[] wants, NpcDomain[] claims, bool[] granted)
        {
            if (wants.Length != claims.Length || granted.Length != claims.Length)
                throw new ArgumentException("Scheduler arrays must align.");
            NpcDomain taken = NpcDomain.None;
            for (int i = 0; i < claims.Length; i++)
            {
                granted[i] = wants[i] && (claims[i] & taken) == 0;
                if (granted[i]) taken |= claims[i];
            }
        }
    }

    /// <summary>
    /// Torch decision with hysteresis. Light conditions must hold for a few seconds
    /// before the torch changes; anything combat- or stealth-related removes it at once.
    /// </summary>
    public sealed class TorchPolicy
    {
        float eligibleFor;
        float lightIneligibleFor;
        public bool On { get; private set; }
        public string Reason { get; private set; }

        public bool Step(NpcPhase phase, bool dark, bool nearBuiltLight, bool leaderCrouching, bool swimming,
            float secondsSinceEncounter, float dt)
        {
            bool phaseOk = phase == NpcPhase.Calm || phase == NpcPhase.Regroup;
            bool hardBlock = !phaseOk || leaderCrouching || swimming || secondsSinceEncounter < Tuning.TorchAfterCombat;
            bool lightOk = dark && !nearBuiltLight;

            if (hardBlock)
            {
                On = false;
                eligibleFor = 0f;
                lightIneligibleFor = 0f;
                Reason = !phaseOk ? "phase " + phase : leaderCrouching ? "stealth" : swimming ? "swimming" : "after combat";
                return On;
            }
            if (On)
            {
                lightIneligibleFor = lightOk ? 0f : lightIneligibleFor + dt;
                if (lightIneligibleFor >= Tuning.TorchOffDelayForLight)
                {
                    On = false;
                    eligibleFor = 0f;
                    Reason = dark ? "near built light" : "daylight";
                }
                else Reason = "lit";
                return On;
            }
            eligibleFor = lightOk ? eligibleFor + dt : 0f;
            if (eligibleFor >= Tuning.TorchOnDelay)
            {
                On = true;
                lightIneligibleFor = 0f;
                Reason = "dark";
            }
            else Reason = lightOk ? "darkness settling" : dark ? "near built light" : "daylight";
            return On;
        }
    }

    /// <summary>Built-light proximity with enter/exit hysteresis.</summary>
    public static class BuiltLight
    {
        public static bool Near(bool previouslyNear, float nearestDistance)
        {
            return previouslyNear ? nearestDistance <= Tuning.BuiltLightExit : nearestDistance <= Tuning.BuiltLightEnter;
        }
    }

    /// <summary>Pure combat-hand rules (no Unity), so they are ledger-tested.</summary>
    public static class CombatHands
    {
        public enum Kind { None, OneHanded, Shield, Ranged, TwoHanded, Torch, Other }

        /// <summary>Bows are not chosen by native combat when the target is closer than this.</summary>
        public const float RangedMinimum = 8f;
        // AI handling for a player bow (verified against how RRRCore makes pillagers shoot:
        // range 25, aim cone 10, no bow draw). Draw is a player-input mechanic; the AI never
        // holds the button, so with draw on she never fires. Arrows still come from her quiver.
        public const float RangedAiRange = 25f;
        public const float RangedAimAngle = 10f;
        public const int QuiverSize = 40;

        public static bool IsRanged(Kind kind) { return kind == Kind.Ranged; }

        /// <summary>A shield belongs beside a one-handed weapon whose off hand is free.</summary>
        public static bool ShouldPairShield(Kind right, bool leftOccupied)
        {
            return right == Kind.OneHanded && !leftOccupied;
        }
    }

    /// <summary>One enemy as the block rule sees it this tick.</summary>
    public struct ThreatObservation
    {
        public float Distance;
        public bool Attacking;   // enemy is in an attack right now
        public bool TargetsMe;   // enemy AI's current target is this NPC
        public bool Ranged;      // projectile attack (bow, thrown rock, spit)
        public bool InFront;     // within the front arc this NPC can block
    }

    /// <summary>
    /// When to raise and lower a shield. Native Valheim only raises a block from player
    /// input (Player.SetControls); everything after the raise — animation, network sync,
    /// parry window, stamina math, stagger — is native. NPCs have no stamina limit, so this
    /// rule supplies the human limits: a reaction delay, a maximum hold, and a cooldown.
    /// Close quarters for melee; arrows and thrown rocks only when aimed at this NPC.
    /// </summary>
    public sealed class BlockPolicy
    {
        public const float MeleeRadius = 3.5f;
        public const float RangedRadius = 30f;
        public const float MaxHold = .9f;
        public const float Cooldown = .5f;

        readonly float reaction;
        float threatSince = -1f;
        float raisedAt = -1f;
        float loweredAt = float.NegativeInfinity;

        public bool Raised { get; private set; }
        public string Reason { get; private set; }
        public int Raises { get; private set; }
        public bool AgainstRanged { get; private set; }

        /// <param name="reactionSeconds">Per-person reaction time, 0.12 - 0.30 s.</param>
        public BlockPolicy(float reactionSeconds)
        {
            reaction = Math.Max(.12f, Math.Min(.30f, reactionSeconds));
            Reason = "idle";
        }

        public float Reaction { get { return reaction; } }

        public static float ReactionFor(int stableSeed)
        {
            return .12f + ((stableSeed & 0x7fffffff) % 19) * .01f;   // .12 - .30 s
        }

        public bool Step(float now, bool holdsShield, bool selfBusy, IList<ThreatObservation> threats)
        {
            int index;
            bool incoming = Incoming(threats, out index);
            if (!holdsShield || selfBusy)
            {
                Lower(now, !holdsShield ? "no shield in hand" : "own attack/draw/stagger");
                threatSince = -1f;
                return Raised;
            }
            if (Raised)
            {
                if (!incoming) { Lower(now, "threat over"); threatSince = -1f; }
                else if (now - raisedAt >= MaxHold) { Lower(now, "max hold"); threatSince = -1f; }
                else Reason = AgainstRanged ? "holding vs projectile" : "holding vs melee";
                return Raised;
            }
            if (!incoming) { threatSince = -1f; Reason = "no incoming attack"; return false; }
            if (threatSince < 0f) threatSince = now;
            if (now - loweredAt < Cooldown) { Reason = "cooldown"; return false; }
            if (now - threatSince < reaction) { Reason = "reacting"; return false; }
            Raised = true;
            raisedAt = now;
            Raises++;
            AgainstRanged = threats[index].Ranged;
            Reason = AgainstRanged ? "raised vs projectile" : "raised vs melee";
            return true;
        }

        static bool Incoming(IList<ThreatObservation> threats, out int index)
        {
            index = -1;
            if (threats == null) return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < threats.Count; i++)
            {
                ThreatObservation t = threats[i];
                if (!t.Attacking || !t.TargetsMe) continue;
                bool inRange = t.Ranged ? t.Distance <= RangedRadius : (t.Distance <= MeleeRadius && t.InFront);
                if (!inRange || t.Distance >= best) continue;
                best = t.Distance;
                index = i;
            }
            return index >= 0;
        }

        void Lower(float now, string why)
        {
            if (Raised) loweredAt = now;
            Raised = false;
            Reason = why;
        }
    }

    /// <summary>One enemy as the target rule sees it. Built only from native perception.</summary>
    public struct TargetCandidate
    {
        public int Id;               // stable per actor within a fight
        public float Distance;
        public bool CanSee;          // native BaseAI.CanSeeTarget
        public bool TargetsMe;
        public bool TargetsLeader;
        public bool TargetsPartner;  // another companion of the same leader
        public bool Ranged;          // shoots or throws (bow, rock, spit, fireball)
        public float Health01;       // native GetHealthPercentage
        public float PartnerDistance;// distance to the nearest companion of ours, -1 if none
        public bool Boss;
        public bool Alerted;         // its own native alert flag
        public bool Wildlife;        // AnimalsVeg faction (deer and the like)
        public bool HurtUs;          // recently damaged me or a partner
        public bool HurtMe;          // recently damaged me
        public bool Committed { get { return TargetsMe || TargetsLeader || TargetsPartner || HurtUs || HurtMe; } }
    }

    /// <summary>
    /// Who to fight. Same behavior for every companion; the weapon follows from distance
    /// (native EquipBestWeapon: bow 8-25 m, melee closer), never from a role.
    ///   1. Something on me (targets me within melee reach) or that just hit me: nearest of those.
    ///   2. Otherwise keep the current target while it is still engaged and in range.
    ///   3. Otherwise the nearest engaged enemy.
    /// Engaged = it has picked or hurt one of us; mid-fight also any alerted non-wildlife hostile;
    /// while the leader sneaks only what already committed. Nothing beyond MaxRange.
    /// </summary>
    public static class TargetPolicy
    {
        public const float MaxRange = 30f;
        public const float SelfDefenseRange = 4f;

        public static bool Engaged(TargetCandidate c, bool encounterActive, bool leaderSneaking)
        {
            if (c.Distance > MaxRange) return false;
            if (c.Committed) return true;
            if (leaderSneaking) return false;
            return encounterActive && c.Alerted && !c.Wildlife && c.CanSee;
        }

        static bool OnMe(TargetCandidate c) { return c.HurtMe || (c.TargetsMe && c.Distance <= SelfDefenseRange); }

        /// <returns>index of the choice, or -1 for "nothing to fight".</returns>
        public static int Choose(IList<TargetCandidate> all, int currentId, bool encounterActive, bool leaderSneaking,
            out string reason)
        {
            reason = "nothing to fight";
            if (all == null) return -1;
            int onMe = -1, current = -1, nearest = -1;
            for (int i = 0; i < all.Count; i++)
            {
                TargetCandidate c = all[i];
                if (!Engaged(c, encounterActive, leaderSneaking)) continue;
                if (OnMe(c) && (onMe < 0 || c.Distance < all[onMe].Distance)) onMe = i;
                if (c.Id == currentId) current = i;
                if (nearest < 0 || c.Distance < all[nearest].Distance) nearest = i;
            }
            if (onMe >= 0) { reason = "on me"; return onMe; }
            if (current >= 0) { reason = "keep current"; return current; }
            if (nearest >= 0) { reason = "nearest engaged"; return nearest; }
            return -1;
        }
    }
}

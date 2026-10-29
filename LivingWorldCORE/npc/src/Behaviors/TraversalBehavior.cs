using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Sole owner of calm movement. Native MonsterAI.Follow is the only path/steering
    /// writer; this module only places the invisible follow anchor and picks the pace.
    /// Formation math, anchor lead and pace rules are carried over unchanged from the
    /// 0.5.1/0.5.2 live-accepted traversal.
    /// </summary>
    public sealed class TraversalBehavior : NpcBehavior
    {
        const float HeadingMovementThreshold = .75f;
        const float FormationTurnDegreesPerSecond = 120f;
        const float NativeFollowArrivalDistance = 3f;
        const float MovingAnchorLead = 3.25f;

        GameObject anchor;
        Vector3 formationForward;
        bool formationFrameReady;
        int slot = -1;
        bool catchingUp;
        // 1.0.32.8 party spacing: changes only where the anchor is placed; Follow stays the sole movement writer.
        const float StandingArrival = 2.6f;
        const float PassAnchor = 3.6f;        // > Follow's 3 m stop radius
        const float BackOffAnchor = 3.2f;   // anchor this far past a standing spot so Follow's 3 m stop lands at it
        readonly PartySpacing spacing = new PartySpacing();
        readonly PartySpacingProbe probe = new PartySpacingProbe();
        readonly PartySpacingLog spacingLog = new PartySpacingLog();
        SpacingResult lastSpacing;

        public override string Name { get { return "traversal"; } }
        public override NpcDomain Claims { get { return NpcDomain.Movement; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Senses.HasLeader &&
                (f.Phase == NpcPhase.Calm || f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Regroup);
        }

        readonly GaitPolicy gait = new GaitPolicy();
        readonly SneakApproach sneakApproach = new SneakApproach();
        GaitChoice lastGait;

        public override void Enter(NpcFrame f)
        {
            gait.Reset(); lastGait = new GaitChoice(); sneakApproach.Reset();
            formationFrameReady = false;
            catchingUp = false;
            spacing.Reset();
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            Player leader = s.Leader;
            UpdateFormationFrame(leader, f.Dt);
            EnsureSlot(leader);

            NpcBlackboard board = f.Board;
            // Settled request: the settled guard (Calm) or, 1.0.32.18, the ready watch (Readiness) says where to stand.
            bool settled = board.SettledRequester != null && (f.Phase == NpcPhase.Calm || f.Phase == NpcPhase.Readiness) && !s.LeaderMoving;
            Vector3 slotPosition = FormationLeases.Position(leader, slot, formationForward);
            Vector3 destination = settled ? board.SettledDestination : slotPosition;
            Vector3 followAnchor = settled ? board.SettledFollowAnchor : FollowAnchorPosition(leader, destination);
            if (CorePlugin.PartySpacingEnabled) followAnchor = Spaced(f, leader, settled, destination, followAnchor);
            else lastSpacing = new SpacingResult { AvoidedId = -1 };
            anchor.transform.position = followAnchor;
            anchor.transform.rotation = Quaternion.LookRotation(formationForward, Vector3.up);
            Body.Follow(this, anchor);

            float slotDistance = Vector3.Distance(Body.Root.transform.position, slotPosition);
            board.FormationDistance = settled ? Vector3.Distance(Body.Root.transform.position, destination) : slotDistance;
            board.SlotDistance = slotDistance;
            board.FormationSlot = slot;

            catchingUp = s.LeaderDistance >= Tuning.CatchUpEnter || (catchingUp && s.LeaderDistance > Tuning.CatchUpRelease);
            // 1.0.32.20: far companions come up at the ordinary pace; they sneak only the last stretch near the leader
            bool sneak = sneakApproach.Step(s.LeaderCrouching && !s.Swimming && f.Phase != NpcPhase.Regroup, s.LeaderDistance);
            float anchorDistance = Vector3.Distance(Body.Root.transform.position, followAnchor);
            bool run = !settled && !sneak && (s.LeaderRunning || catchingUp ||
                ((f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Regroup) && (s.LeaderRunning || s.LeaderDistance > 7f)));
            Body.SetPace(this, run, catchingUp || s.LeaderDistance > 12f, s.LeaderSpeed, sneak);
            Body.SetRun(this, run && anchorDistance > NativeFollowArrivalDistance + .15f);
            // 1.0.32.10 gait: calm walking only; run, catch-up and sneak keep their pace rules
            if (CorePlugin.MatchGait && !run && !sneak && !settled && !catchingUp)
            {
                lastGait = gait.Decide(f.Dt, s.LeaderSpeed, s.LeaderWalkToggle, s.LeaderDistance, slotDistance, Body.NativeJogSpeed);
                Body.SetGait(this, lastGait);
            }
            else { gait.Reset(); lastGait = new GaitChoice(); }

            Status = (settled ? "settled guard " + (board.SettledHold ? "hold" : "approach") : "slot " + slot) +
                (sneak ? "; sneak" : run ? "; run" : "; walk") + (catchingUp ? "; catch-up" : "") + (lastGait.Matching ? (lastGait.Walk ? "; gait walk " : "; gait jog ") + lastGait.Speed.ToString("0.0") : "") +
                "; lease " + board.FormationDistance.ToString("0.0") + "m" + SpacingStatus();
        }

        public override void Exit(NpcFrame f, string reason)
        {
            FormationLeases.Release(this);
            slot = -1;
            f.Board.FormationDistance = -1f;
            f.Board.FormationSlot = -1;
            Status = "released: " + reason;
            try
            {
                if (anchor != null && Body.Ai != null && Body.Ai.GetFollowTarget() == anchor) Body.Follow(this, null);
                Body.RestorePace(this);
            }
            finally
            {
                // The anchor is HideAndDontSave: it must never outlive this tenure,
                // even if the actor is already being torn down.
                if (anchor != null) UnityEngine.Object.Destroy(anchor);
                anchor = null;
            }
        }

        /// <summary>1.0.32.8 party spacing. Standing: the spot keeps off the leader and the companion walks onto it
        /// (soft region, then holds). Any walk: a party member in the corridor bends the aim sideways; blocked = wait;
        /// in the leader's menu clearance = ease back. Returns only the anchor point.</summary>
        Vector3 Spaced(NpcFrame f, Player leader, bool settled, Vector3 destination, Vector3 followAnchor)
        {
            Vector3 self = Body.Root.transform.position;
            bool inMenu = PartySpacingProbe.LeaderInMenu(leader);
            bool standing = !settled && !f.Senses.LeaderMoving;
            P2 leaderFlat = PartySpacingProbe.Flat(leader.transform.position);
            // 1.0.32.17 (Brian live 2026-10-29: weapon drawn and standing, they "freeze ... like a generic NPC"): a standing
            // companion keeps the 1.0.32.7 anchor (its spot; native Follow settles inside 3 m and re-adjusts as the leader
            // shifts). Only the contact rules below may override it. The 1.0.32.8 exact-arrival anchor and soft-region Hold
            // parked them on the spot directly behind the leader (telemetry: 0.7 m from the spot at 2.8 m from the leader,
            // against 2.7 m from the spot at 6.5 m from the leader under 1.0.32.7).
            Vector3 standingAnchor = followAnchor;
            NpcKernel kernel = Body.Root.GetComponent<NpcKernel>();
            if (kernel != null) spacing.SelfId = kernel.GetInstanceID();
            probe.Gather(kernel, leader);
            SpacingResult r = spacing.Step(f.Now, f.Dt, PartySpacingProbe.Flat(self), f.Senses.OwnSpeed,
                PartySpacingProbe.Flat(followAnchor), PartySpacingProbe.Flat(destination), standing, inMenu,
                PartySpacingProbe.SlotSide(slot), slot, probe.Near, probe.Count, probe);
            lastSpacing = r;
            spacingLog.Write(kernel, probe, r, slot, f.Now);
            if (standing && r.Mode != SpacingMode.BackOff && !(r.Mode == SpacingMode.Wait && r.AvoidedId >= 0))
                return standingAnchor;                             // 1.0.32.7 standing behavior unless backing off a contact
            switch (r.Mode)
            {
                case SpacingMode.Hold:
                case SpacingMode.Wait:
                    return self;                                   // Follow stops inside its 3 m radius
                case SpacingMode.BackOff:
                    // straight out from the leader; anchored past Follow's 3 m stop so it really reaches the clearance
                    return new Vector3(r.Point.X + r.Shift.X * BackOffAnchor, self.y, r.Point.Z + r.Shift.Z * BackOffAnchor);
                case SpacingMode.Sidestep:
                {
                    // walk at the pass point; the anchor sits on that line at least PassAnchor out so Follow keeps walking
                    Vector3 toPass = new Vector3(r.Point.X - self.x, 0f, r.Point.Z - self.z);
                    float d = toPass.magnitude;
                    if (d < .05f) return followAnchor;
                    return self + toPass * (Mathf.Max(d, PassAnchor) / d) + Vector3.up * (followAnchor.y - self.y);
                }
                default:
                    return followAnchor + new Vector3(r.Shift.X, 0f, r.Shift.Z);   // zero unless easing back
            }
        }

        /// <summary>An anchor past the point along the walk, so native Follow's 3 m stop radius lands on the point.
        /// If that anchor is not on clear ground, the point itself (native stop short of it, as before 1.0.32.8).</summary>
        Vector3 Beyond(Vector3 self, Vector3 point)
        {
            Vector3 walk = point - self;
            walk.y = 0f;
            if (walk.magnitude <= PartySpacing.HoldEnter) return point;
            Vector3 beyond = point + walk.normalized * StandingArrival;
            return probe.Clear(PartySpacingProbe.Flat(point), PartySpacingProbe.Flat(beyond)) ? beyond : point;
        }

        string SpacingStatus()
        {
            if (!CorePlugin.PartySpacingEnabled) return "";
            SpacingResult r = lastSpacing;
            if (r.Mode == SpacingMode.Travel && r.Offset <= 0f) return "";
            return "; space " + r.Mode.ToString().ToLowerInvariant() +
                (r.Side != 0 ? " " + (r.Side > 0 ? "left" : "right") + " " + r.Offset.ToString("0.0") + "m" : "") +
                (r.AvoidedId >= 0 ? " around " + (r.AvoidedLeader ? "leader" : probe.NameOf(r.AvoidedId)) : "");
        }

        void EnsureSlot(Player leader)
        {
            if (slot < 0 || !FormationLeases.IsClaimedBy(this, leader, slot))
                slot = FormationLeases.Acquire(this, Body.Root.transform.position, leader, formationForward);
            if (anchor == null)
            {
                anchor = new GameObject("LWC follow anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
        }

        void UpdateFormationFrame(Player leader, float dt)
        {
            Vector3 velocity = leader.GetVelocity();
            velocity.y = 0f;
            if (!formationFrameReady)
            {
                Vector3 initial = velocity.magnitude >= HeadingMovementThreshold ? velocity.normalized : leader.transform.forward;
                initial.y = 0f;
                formationForward = initial.sqrMagnitude < .01f ? Vector3.forward : initial.normalized;
                formationFrameReady = true;
                return;
            }
            // Stationary turning does not rotate the frame (0.5.0 orbit/jitter fix).
            if (velocity.magnitude < HeadingMovementThreshold) return;
            formationForward = Vector3.RotateTowards(formationForward, velocity.normalized,
                FormationTurnDegreesPerSecond * Mathf.Deg2Rad * dt, 0f).normalized;
        }

        Vector3 FollowAnchorPosition(Player leader, Vector3 destination)
        {
            Vector3 velocity = leader.GetVelocity();
            velocity.y = 0f;
            if (velocity.magnitude < HeadingMovementThreshold) return destination;
            // MonsterAI.Follow stops inside a three-metre radius; lead the anchor by that
            // radius while travelling so the native arrival point is the leased slot.
            return destination + formationForward * MovingAnchorLead;
        }

        public float FormationHeading
        {
            get { return formationFrameReady ? Mathf.Atan2(formationForward.x, formationForward.z) * Mathf.Rad2Deg : 0f; }
        }
    }

    /// <summary>Dynamic formation leases: any companion may take any open slot.</summary>
    public static class FormationLeases
    {
        sealed class Lease { public long LeaderId; public int Slot; }

        static readonly Vector2[] LocalSlots = {
            new Vector2(-1.65f, -2.75f), new Vector2(1.65f, -2.75f),
            new Vector2(-2.55f, -1.05f), new Vector2(2.55f, -1.05f),
            new Vector2(-3.15f, -3.45f), new Vector2(3.15f, -3.45f)
        };
        static readonly Dictionary<object, Lease> Claims = new Dictionary<object, Lease>();

        public static int Acquire(object actor, Vector3 actorPosition, Player leader, Vector3 forward)
        {
            long leaderId = leader.GetPlayerID();
            Lease existing;
            if (Claims.TryGetValue(actor, out existing) && existing.LeaderId == leaderId) return existing.Slot;
            Claims.Remove(actor);
            int chosen = -1;
            float best = Single.PositiveInfinity;
            for (int slot = 0; slot < LocalSlots.Length; slot++)
            {
                if (Occupied(leaderId, slot)) continue;
                float distance = Vector3.Distance(actorPosition, Position(leader, slot, forward));
                if (distance < best) { best = distance; chosen = slot; }
            }
            if (chosen < 0) chosen = Claims.Count % LocalSlots.Length;
            Claims[actor] = new Lease { LeaderId = leaderId, Slot = chosen };
            return chosen;
        }

        public static bool IsClaimedBy(object actor, Player leader, int slot)
        {
            Lease lease;
            return actor != null && leader != null && Claims.TryGetValue(actor, out lease) &&
                lease.LeaderId == leader.GetPlayerID() && lease.Slot == slot;
        }

        /// <summary>Lateral offset of a slot (negative = left of the leader's travel direction).</summary>
        public static float LocalX(int slot) { return slot < 0 ? 0f : LocalSlots[Mathf.Clamp(slot, 0, LocalSlots.Length - 1)].x; }

        public static Vector3 Position(Player leader, int slot, Vector3 forward)
        {
            Vector2 local = LocalSlots[Mathf.Clamp(slot, 0, LocalSlots.Length - 1)];
            Vector3 flat = forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < .01f) flat = leader.transform.forward;
            flat.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flat).normalized;
            return leader.transform.position + right * local.x + flat * local.y;
        }

        public static void Release(object actor) { if (actor != null) Claims.Remove(actor); }

        static bool Occupied(long leaderId, int slot)
        {
            foreach (Lease lease in Claims.Values) if (lease.LeaderId == leaderId && lease.Slot == slot) return true;
            return false;
        }
    }
}

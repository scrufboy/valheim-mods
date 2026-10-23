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

        public override string Name { get { return "traversal"; } }
        public override NpcDomain Claims { get { return NpcDomain.Movement; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Senses.HasLeader &&
                (f.Phase == NpcPhase.Calm || f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Regroup);
        }

        public override void Enter(NpcFrame f)
        {
            formationFrameReady = false;
            catchingUp = false;
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            Player leader = s.Leader;
            UpdateFormationFrame(leader, f.Dt);
            EnsureSlot(leader);

            NpcBlackboard board = f.Board;
            bool settled = board.SettledRequester != null && f.Phase == NpcPhase.Calm && !s.LeaderMoving;
            Vector3 slotPosition = FormationLeases.Position(leader, slot, formationForward);
            Vector3 destination = settled ? board.SettledDestination : slotPosition;
            Vector3 followAnchor = settled ? board.SettledFollowAnchor : FollowAnchorPosition(leader, destination);
            anchor.transform.position = followAnchor;
            anchor.transform.rotation = Quaternion.LookRotation(formationForward, Vector3.up);
            Body.Follow(this, anchor);

            float slotDistance = Vector3.Distance(Body.Root.transform.position, slotPosition);
            board.FormationDistance = settled ? Vector3.Distance(Body.Root.transform.position, destination) : slotDistance;
            board.FormationSlot = slot;

            catchingUp = s.LeaderDistance >= Tuning.CatchUpEnter || (catchingUp && s.LeaderDistance > Tuning.CatchUpRelease);
            bool sneak = s.LeaderCrouching && !s.Swimming && f.Phase != NpcPhase.Regroup;
            float anchorDistance = Vector3.Distance(Body.Root.transform.position, followAnchor);
            bool run = !settled && !sneak && (s.LeaderRunning || catchingUp ||
                ((f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Regroup) && (s.LeaderRunning || s.LeaderDistance > 7f)));
            Body.SetPace(this, run, catchingUp || s.LeaderDistance > 12f, s.LeaderSpeed, sneak);
            Body.SetRun(this, run && anchorDistance > NativeFollowArrivalDistance + .15f);

            Status = (settled ? "settled guard " + (board.SettledHold ? "hold" : "approach") : "slot " + slot) +
                (sneak ? "; sneak" : run ? "; run" : "; walk") + (catchingUp ? "; catch-up" : "") +
                "; lease " + board.FormationDistance.ToString("0.0") + "m";
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

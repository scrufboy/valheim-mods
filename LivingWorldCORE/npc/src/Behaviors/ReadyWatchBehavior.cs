using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// 1.0.32.18 ready watch (Brian 2026-10-29). Weapon drawn (Readiness), leader standing, nothing targeted: the companion
    /// stands its ground at the ready. It watches the outside half on its side of the leader (the partner takes the other
    /// half), turns head and shoulders to a new bearing every few seconds or sweeps slowly across the half, and now and
    /// then shifts its footing a short way. Owns only Look; asks traversal (the movement owner) where to stand through the
    /// same blackboard request the settled guard uses. An alerted hostile takes the look first (AwarenessLook, higher
    /// priority); a target or the leader moving ends the watch.
    /// </summary>
    public sealed class ReadyWatchBehavior : NpcBehavior
    {
        const float FollowArrivalCompensation = 3.25f;
        const float ArrivalDistance = .35f;
        const float StepTimeout = 3f;
        const float LeaderMovedReset = .75f;
        const float KeepFromLeader = 2.2f, KeepFromMate = 1.7f;

        sealed class SideClaim { public long LeaderId; public int Side; }
        static readonly Dictionary<ReadyWatchBehavior, SideClaim> Sides = new Dictionary<ReadyWatchBehavior, SideClaim>();

        readonly Collider[] clearance = new Collider[32];
        readonly RaycastHit[] rayHits = new RaycastHit[16];
        ReadyWatchPolicy policy;
        WatchLook look;
        float lookStarted, nextStep, stepStarted;
        bool stepping, reacquiring;
        int side;
        Vector3 home, spot, leaderAtStart;
        Player leader;
        NpcBlackboard board;

        public override string Name { get { return "ready-watch"; } }
        public override NpcDomain Claims { get { return NpcDomain.Look; } }

        public override bool Wants(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            return CorePlugin.ReadyWatch && f.Phase == NpcPhase.Readiness && s.HasLeader && s.Leader != null &&
                !s.LeaderMoving && !s.LeaderSitting && !s.LeaderCrouching && s.Target == null && !s.HasStaticTarget &&
                !s.Swimming && ReadyWatchPolicy.ShouldWatch(Active, f.Board.SlotDistance, s.OwnSpeed, s.LeaderDistance);
        }

        public override void Enter(NpcFrame f)
        {
            if (policy == null) policy = new ReadyWatchPolicy(Body.Root.GetInstanceID());
            leader = f.Senses.Leader;
            board = f.Board;
            Begin(f);
        }

        void Begin(NpcFrame f)
        {
            home = spot = Body.Root.transform.position;
            leaderAtStart = leader.transform.position;
            side = ClaimSide();
            stepping = false;
            look = policy.NextLook(side);
            lookStarted = f.Now;
            nextStep = f.Now + policy.NextStepDelay();
            Request(spot, spot, true);
            Status = "ready watch " + (side > 0 ? "right" : "left") + " half";
        }

        public override void Tick(NpcFrame f)
        {
            leader = f.Senses.Leader;
            board = f.Board;
            if (leader == null) return;
            Humanoid human = Body.Human;
            if (reacquiring)
            {   // traversal is taking it back to its leased spot; resume the watch once settled there
                if (f.Board.SlotDistance >= 0f && f.Board.SlotDistance <= ReadyWatchPolicy.StartSlotDistance &&
                    f.Senses.OwnSpeed <= ReadyWatchPolicy.StartSpeed) { reacquiring = false; Begin(f); }
                else { Status = "re-acquiring leased spot"; return; }
            }
            if (Vector3.Distance(leader.transform.position, leaderAtStart) > LeaderMovedReset)
            {   // the leader shifted: give the spot back to traversal; the watch resumes once re-settled near him
                if (board != null) board.ClearSettled(this);
                reacquiring = true; stepping = false;
                Status = "leader moved; re-acquiring leased spot";
                return;
            }

            if (f.Now - lookStarted >= look.Seconds)
            {
                look = policy.NextLook(side);
                lookStarted = f.Now;
            }

            bool busy = human.IsDead() || !human.IsOnGround() || human.InAttack() || human.IsDrawingBow() || human.IsBlocking();
            Vector3 self = Body.Root.transform.position;
            if (stepping)
            {
                Vector3 off = self - spot; off.y = 0f;
                if (off.magnitude <= ArrivalDistance || f.Now - stepStarted > StepTimeout || busy)
                {
                    stepping = false;
                    spot = self;
                    Request(spot, spot, true);
                }
            }
            else if (!busy && f.Now >= nextStep)
            {
                nextStep = f.Now + policy.NextStepDelay();
                P2 to;
                if (policy.Step(new P2(self.x, self.z), new P2(home.x, home.z), out to))
                {
                    Vector3 point = new Vector3(to.X, self.y, to.Z);
                    Vector3 dir = point - self; dir.y = 0f;
                    if (dir.sqrMagnitude > .01f && Ground(ref point) && Clear(point) && ClearSegment(self, point) && Spaced(point))
                    {
                        Vector3 anchor = point + dir.normalized * FollowArrivalCompensation;
                        spot = point;
                        stepping = true;
                        stepStarted = f.Now;
                        Request(point, anchor, false);
                    }
                }
            }
            Status = "ready watch " + (side > 0 ? "right" : "left") + (stepping ? "; shifting footing" : "") +
                "; look " + ReadyWatchPolicy.At(look, f.Now - lookStarted).ToString("0");
        }

        public override void LateFrame(NpcFrame f)
        {
            if (leader == null) return;
            Vector3 facing = leader.transform.forward; facing.y = 0f;
            if (facing.sqrMagnitude < .01f) facing = Vector3.forward;
            float bearing = ReadyWatchPolicy.At(look, Time.time - lookStarted);
            Vector3 dir = Quaternion.AngleAxis(bearing, Vector3.up) * facing.normalized;
            Body.LookAt(this, Body.Root.transform.position + dir * 10f + Vector3.up * 1.4f);
        }

        public override void Exit(NpcFrame f, string reason)
        {
            Sides.Remove(this);
            if (board != null) board.ClearSettled(this);
            stepping = false; reacquiring = false;
            Status = "released: " + reason;
        }

        void Request(Vector3 destination, Vector3 anchor, bool hold)
        {
            if (board == null) return;
            if (board.SettledRequester != null && board.SettledRequester != this) return;
            board.SettledRequester = this;
            board.SettledDestination = destination;
            board.SettledFollowAnchor = anchor;
            board.SettledHold = hold;
        }

        int ClaimSide()
        {
            long id = leader.GetPlayerID();
            var stale = new List<ReadyWatchBehavior>();
            foreach (var kv in Sides) if (!kv.Key.Active || kv.Key == this) stale.Add(kv.Key);
            foreach (var k in stale) Sides.Remove(k);
            int partner = 0;
            foreach (var kv in Sides) if (kv.Value.LeaderId == id) { partner = kv.Value.Side; break; }
            Vector3 facing = leader.transform.forward; facing.y = 0f;
            Vector3 rel = Body.Root.transform.position - leader.transform.position; rel.y = 0f;
            float lateral = Vector3.Dot(rel, Vector3.Cross(Vector3.up, facing.normalized));   // + = right of the leader
            int chosen = ReadyWatchPolicy.ChooseSide(lateral, partner);
            Sides[this] = new SideClaim { LeaderId = id, Side = chosen };
            return chosen;
        }

        bool Spaced(Vector3 point)
        {
            Vector3 l = leader.transform.position;
            if (new Vector2(point.x - l.x, point.z - l.z).magnitude < KeepFromLeader) return false;
            foreach (NpcKernel k in NpcKernel.LiveSet)
            {
                if (k == null || k.Body == null || k.Body.Root == Body.Root || k.Body.Root == null) continue;
                Vector3 p = k.Body.Root.transform.position;
                if (new Vector2(point.x - p.x, point.z - p.z).magnitude < KeepFromMate) return false;
            }
            return true;
        }

        bool Ground(ref Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(point + Vector3.up * .9f, Vector3.down, rayHits, 1.8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            float closest = Single.PositiveInfinity; bool found = false;
            for (int i = 0; i < count && i < rayHits.Length; i++)
            {
                RaycastHit hit = rayHits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<Character>() != null || hit.normal.y < .8f || hit.distance >= closest) continue;
                closest = hit.distance; point = hit.point; found = true;
            }
            return found;
        }

        bool Clear(Vector3 point)
        {
            int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * .45f, point + Vector3.up * 1.45f, .35f, clearance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (count >= clearance.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider c = clearance[i]; clearance[i] = null;
                if (c == null || c.GetComponentInParent<Character>() == Body.Human) continue;
                if (c.GetComponentInParent<Aoe>() != null || c.GetComponentInParent<Smoke>() != null || !c.isTrigger) return false;
            }
            return !EffectArea.IsPointPlus025InsideBurningArea(point);
        }

        bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            if (d.sqrMagnitude < .01f) return true;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * .8f, d.normalized, rayHits, d.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count && i < rayHits.Length; i++)
                if (rayHits[i].collider != null && rayHits[i].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// When the leader stops, companions take turns stepping out to visibly sweep a
    /// sector, preferring the opposite of the last one covered and never the sector a
    /// teammate holds. Owns only Look; asks traversal (the movement owner) where to
    /// stand through the blackboard. Safety probes and sector logic carried over from 0.6.0.
    /// </summary>
    public sealed class SettledGuardBehavior : NpcBehavior
    {
        enum GuardPhase { Quiet, Approaching, Scanning, Cooldown }

        const float GuardDistance = 4.6f;
        const float FollowArrivalCompensation = 3.25f;
        const float ArrivalDistance = 1.25f;
        const float ScanSeconds = 6f;
        const float CoverageResetDistance = 4f;

        sealed class GuardClaim { public long LeaderId; public int Sector; }
        static readonly Dictionary<SettledGuardBehavior, GuardClaim> SectorClaims = new Dictionary<SettledGuardBehavior, GuardClaim>();

        readonly GuardCoverage coverage = new GuardCoverage();
        readonly Collider[] clearance = new Collider[64];
        readonly RaycastHit[] rayHits = new RaycastHit[32];
        readonly List<Vector3> probePath = new List<Vector3>();

        GuardPhase phase;
        Vector3 destination, followAnchor, leaderAtStart, coverageOrigin;
        bool coverageOriginReady;
        int sector = -1;
        float phaseStarted, settledSince, nextAttempt, nextValidation;
        Player leader;
        NpcBlackboard board;

        public int Sector { get { return sector; } }
        public string Phase { get { return phase.ToString(); } }

        public override string Name { get { return "settled-guard"; } }
        public override NpcDomain Claims { get { return NpcDomain.Look; } }

        public override bool Wants(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            return f.Phase == NpcPhase.Calm && s.HasLeader && !s.LeaderMoving && !s.LeaderSitting && !s.LeaderCrouching &&
                !s.NativeAlert && s.Target == null;
        }

        public override void Enter(NpcFrame f)
        {
            leader = f.Senses.Leader;
            board = f.Board;
            phase = GuardPhase.Quiet;
            phaseStarted = f.Now;
            settledSince = 0f;
            Status = "quiet before coverage";
        }

        public override void Tick(NpcFrame f)
        {
            leader = f.Senses.Leader;
            board = f.Board;
            Humanoid human = Body.Human;
            if (leader == null || human.IsDead() || !human.IsOnGround() || human.IsSwimming() || human.InAttack() ||
                human.IsDrawingBow() || human.IsBlocking())
            {
                Cancel("body busy");
                return;
            }

            if (phase == GuardPhase.Quiet || phase == GuardPhase.Cooldown)
            {
                if (!SettledAtFormation(f))
                {
                    settledSince = 0f;
                    Status = "waiting at formation lease";
                    return;
                }
                if (settledSince <= 0f) settledSince = f.Now;
                float delay = (f.Senses.LeaderBusy ? .75f : 2f) + StableStagger(f);
                if (f.Now < nextAttempt || f.Now - settledSince < delay)
                {
                    Status = f.Senses.LeaderBusy ? "busy-leader guard delay" : "settled guard delay";
                    return;
                }
                if (!BeginCoverage(f))
                {
                    nextAttempt = f.Now + 2f + StableStagger(f);
                    Status = "no safe reachable guard sector";
                }
                return;
            }

            if (Vector3.Distance(leader.transform.position, leaderAtStart) > .75f)
            {
                Cancel("leader moved");
                return;
            }

            if (phase == GuardPhase.Approaching)
            {
                if (f.Now >= nextValidation)
                {
                    nextValidation = f.Now + .5f;
                    if (!SafePoint(destination) || !SafePoint(followAnchor)) { Cancel("guard point lost safety"); return; }
                }
                Vector3 offset = Body.Root.transform.position - destination;
                offset.y = 0f;
                if (offset.magnitude <= ArrivalDistance && f.Senses.OwnSpeed <= .8f)
                {
                    phase = GuardPhase.Scanning;
                    phaseStarted = f.Now;
                    board.SettledHold = true;
                    Status = "scanning sector " + sector;
                    return;
                }
                if (f.Now - phaseStarted > 10f)
                {
                    Cancel("approach timed out");
                    nextAttempt = f.Now + 3f;
                    return;
                }
                Status = "approaching sector " + sector;
                return;
            }

            if (phase == GuardPhase.Scanning && f.Now - phaseStarted >= ScanSeconds)
            {
                coverage.Resolve(sector, f.Now);
                int completed = sector;
                ReleaseClaim();
                board.ClearSettled(this);
                phase = GuardPhase.Cooldown;
                sector = -1;
                settledSince = 0f;
                nextAttempt = f.Now + 4f + StableStagger(f);
                Status = "covered sector " + completed + "; opposite preferred next";
            }
        }

        public override void LateFrame(NpcFrame f)
        {
            if (phase != GuardPhase.Scanning || leader == null) return;
            Vector3 outward = destination - leader.transform.position;
            outward.y = 0f;
            if (outward.sqrMagnitude < .01f) outward = Body.Root.transform.forward;
            float elapsed = Time.time - phaseStarted;
            float angle = elapsed < 1.6f ? -48f : elapsed < 3.2f ? 0f : elapsed < 4.8f ? 48f : 0f;
            double radians = angle * Math.PI / 180d;
            float sine = (float)Math.Sin(radians), cosine = (float)Math.Cos(radians);
            Vector3 basis = outward.normalized;
            Vector3 scan = new Vector3(basis.x * cosine + basis.z * sine, 0f, basis.z * cosine - basis.x * sine);
            Body.LookAt(this, Body.Root.transform.position + scan * 8f + Vector3.up);
        }

        public override void Exit(NpcFrame f, string reason)
        {
            Cancel(reason);
            Status = "released: " + reason;
        }

        bool BeginCoverage(NpcFrame f)
        {
            if (!coverageOriginReady || Vector3.Distance(coverageOrigin, leader.transform.position) > CoverageResetDistance)
            {
                coverageOrigin = leader.transform.position;
                coverageOriginReady = true;
                coverage.Reset();
            }
            int rejected = UnavailableMask(leader);
            int offset = (Body.Root.GetInstanceID() & Int32.MaxValue) % GuardCoverage.SectorCount;
            Vector3 self = Body.Root.transform.position;
            for (int attempt = 0; attempt < GuardCoverage.SectorCount; attempt++)
            {
                int candidate = coverage.Choose(rejected, offset);
                if (candidate < 0) return false;
                double radians = GuardCoverage.Radians(candidate);
                Vector3 radial = new Vector3((float)Math.Sin(radians), 0f, (float)Math.Cos(radians));
                Vector3 point = leader.transform.position + radial * GuardDistance;
                Vector3 anchorPoint = point + radial * FollowArrivalCompensation;
                if (!Ground(ref point) || !Ground(ref anchorPoint) || !SafePoint(point) || !SafePoint(anchorPoint) ||
                    CrossesLeader(self, anchorPoint, leader.transform.position) || !ClearSegment(self, anchorPoint) ||
                    !FullPath(anchorPoint) || !AcquireClaim(leader, candidate))
                {
                    rejected |= 1 << candidate;
                    continue;
                }
                if (board.SettledRequester != null && board.SettledRequester != this) { ReleaseClaim(); return false; }
                board.SettledRequester = this;
                board.SettledDestination = point;
                board.SettledFollowAnchor = anchorPoint;
                board.SettledHold = false;
                sector = candidate;
                destination = point;
                followAnchor = anchorPoint;
                leaderAtStart = leader.transform.position;
                phase = GuardPhase.Approaching;
                phaseStarted = f.Now;
                nextValidation = 0f;
                Status = "leased sector " + sector;
                return true;
            }
            return false;
        }

        bool SettledAtFormation(NpcFrame f)
        {
            return f.Board.FormationDistance >= 0f && f.Board.FormationDistance <= 3.35f && f.Senses.OwnSpeed <= .8f;
        }

        float StableStagger(NpcFrame f)
        {
            int formationSlot = f.Board.FormationSlot;
            return formationSlot >= 0 ? formationSlot * .17f : ((Body.Root.GetInstanceID() & Int32.MaxValue) % 5) * .18f;
        }

        bool Ground(ref Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(point + Vector3.up * .9f, Vector3.down, rayHits, 1.8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == rayHits.Length) return false;
            float closest = Single.PositiveInfinity;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = rayHits[i];
                if (hit.collider == null || hit.collider.GetComponentInParent<Character>() != null ||
                    hit.normal.y < .8f || hit.distance >= closest) continue;
                closest = hit.distance;
                point = hit.point;
                found = true;
            }
            return found;
        }

        bool SafePoint(Vector3 point)
        {
            int count = Physics.OverlapCapsuleNonAlloc(point + Vector3.up * .45f, point + Vector3.up * 1.45f,
                .35f, clearance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (count == clearance.Length) return false;
            for (int i = 0; i < count; i++)
            {
                Collider candidate = clearance[i];
                clearance[i] = null;
                if (candidate == null || candidate.GetComponentInParent<Character>() == Body.Human) continue;
                if (candidate.GetComponentInParent<Aoe>() != null || candidate.GetComponentInParent<Smoke>() != null ||
                    !candidate.isTrigger) return false;
            }
            return !EffectArea.IsPointPlus025InsideBurningArea(point);
        }

        bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .01f) return true;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * .8f, delta.normalized, rayHits,
                delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == rayHits.Length) return false;
            for (int i = 0; i < count; i++)
                if (rayHits[i].collider != null && rayHits[i].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }

        bool FullPath(Vector3 target)
        {
            if (Pathfinding.instance == null) return false;
            probePath.Clear();
            return Pathfinding.instance.GetPath(Body.Root.transform.position, target, probePath, Body.Ai.m_pathAgentType,
                true, true, false) && probePath.Count > 0 && Vector3.Distance(probePath[probePath.Count - 1], target) <= .5f;
        }

        static bool CrossesLeader(Vector3 from, Vector3 to, Vector3 leaderPosition)
        {
            float x = to.x - from.x, z = to.z - from.z;
            float lengthSquared = x * x + z * z;
            if (lengthSquared < .01f) return false;
            float projection = Mathf.Clamp01(((leaderPosition.x - from.x) * x + (leaderPosition.z - from.z) * z) / lengthSquared);
            float nx = from.x + x * projection - leaderPosition.x;
            float nz = from.z + z * projection - leaderPosition.z;
            return nx * nx + nz * nz < 1.3225f;
        }

        static int UnavailableMask(Player currentLeader)
        {
            CleanupClaims();
            int mask = 0;
            long leaderId = currentLeader.GetPlayerID();
            foreach (KeyValuePair<SettledGuardBehavior, GuardClaim> pair in SectorClaims)
                if (pair.Value.LeaderId == leaderId) mask |= 1 << pair.Value.Sector;
            return mask;
        }

        bool AcquireClaim(Player currentLeader, int candidate)
        {
            CleanupClaims();
            long leaderId = currentLeader.GetPlayerID();
            foreach (KeyValuePair<SettledGuardBehavior, GuardClaim> pair in SectorClaims)
                if (pair.Key != this && pair.Value.LeaderId == leaderId && pair.Value.Sector == candidate) return false;
            SectorClaims[this] = new GuardClaim { LeaderId = leaderId, Sector = candidate };
            return true;
        }

        static void CleanupClaims()
        {
            var stale = new List<SettledGuardBehavior>();
            foreach (SettledGuardBehavior actor in SectorClaims.Keys) if (!actor.Active) stale.Add(actor);
            foreach (SettledGuardBehavior actor in stale) SectorClaims.Remove(actor);
        }

        void ReleaseClaim() { SectorClaims.Remove(this); }

        void Cancel(string reason)
        {
            ReleaseClaim();
            if (board != null) board.ClearSettled(this);
            sector = -1;
            phase = GuardPhase.Quiet;
            settledSince = 0f;
            Status = reason;
        }
    }
}

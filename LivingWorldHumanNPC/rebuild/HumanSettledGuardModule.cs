using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    [DefaultExecutionOrder(1200)]
    public sealed class HumanSettledGuardModule : MonoBehaviour
    {
        enum GuardPhase { Inactive, Quiet, Approaching, Scanning, Cooldown }

        const float GuardDistance = 4.6f;
        const float FollowArrivalCompensation = 3.25f;
        const float ArrivalDistance = 1.25f;
        const float ScanSeconds = 6f;
        const float CoverageResetDistance = 4f;
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo Look = typeof(BaseAI).GetMethod("LookAt", Native, null,
            new[] { typeof(Vector3) }, null);

        sealed class GuardClaim
        {
            internal long LeaderId;
            internal int Sector;
        }

        static readonly Dictionary<HumanSettledGuardModule, GuardClaim> Claims =
            new Dictionary<HumanSettledGuardModule, GuardClaim>();

        readonly GuardCoverageV2 coverage = new GuardCoverageV2();
        readonly Collider[] clearance = new Collider[64];
        readonly RaycastHit[] rayHits = new RaycastHit[32];
        readonly List<Vector3> probePath = new List<Vector3>();
        readonly object[] lookArgument = new object[1];

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        HumanTraversalModule traversal;
        Player leader;
        GuardPhase phase;
        Vector3 destination;
        Vector3 followAnchor;
        Vector3 leaderAtStart;
        Vector3 coverageOrigin;
        bool coverageOriginReady;
        bool eligible;
        int sector = -1;
        float phaseStarted;
        float settledSince;
        float nextAttempt;
        float nextValidation;
        float nextFaultLog;

        public string Status { get; private set; }
        public int Sector { get { return sector; } }
        public string Phase { get { return phase.ToString(); } }

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            traversal = GetComponent<HumanTraversalModule>();
            if (Look == null) throw new MissingMemberException("Valheim's native AI look boundary changed.");
            phase = GuardPhase.Inactive;
            Status = "inactive";
        }

        internal void Configure(HumanBehaviorState state, Player nextLeader)
        {
            bool nextEligible = state == HumanBehaviorState.IdleGuard && nextLeader != null;
            if (!nextEligible)
            {
                if (eligible || phase != GuardPhase.Inactive) Cancel("yielded to " + state);
                eligible = false;
                leader = nextLeader;
                return;
            }

            if (!eligible || leader != nextLeader || phase == GuardPhase.Inactive)
            {
                Cancel("new settled context");
                leader = nextLeader;
                eligible = true;
                phase = GuardPhase.Quiet;
                phaseStarted = Time.time;
                settledSince = 0f;
                Status = "quiet before coverage";
            }
        }

        void Update()
        {
            if (!eligible || leader == null) return;
            try { Tick(); }
            catch (Exception error)
            {
                Cancel("retained formation after guard fault");
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    HumanNpcPlugin.WarnStatic(name + " settled guard yielded safely: " + error.Message);
                }
            }
        }

        void Tick()
        {
            if (!Ready())
            {
                Cancel("guard interrupted by authority, motion, body state, or threat");
                return;
            }

            if (phase == GuardPhase.Quiet || phase == GuardPhase.Cooldown)
            {
                if (!SettledAtFormation())
                {
                    settledSince = 0f;
                    Status = "waiting at formation lease";
                    return;
                }
                if (settledSince <= 0f) settledSince = Time.time;
                float delay = LeaderBusy() ? .75f : 2f;
                delay += StableStagger();
                if (Time.time < nextAttempt || Time.time - settledSince < delay)
                {
                    Status = LeaderBusy() ? "busy-leader accelerated guard delay" : "settled guard delay";
                    return;
                }
                if (!BeginCoverage())
                {
                    nextAttempt = Time.time + 2f + StableStagger();
                    Status = "no safe reachable guard sector";
                }
                return;
            }

            if (Vector3.Distance(leader.transform.position, leaderAtStart) > .75f)
            {
                Cancel("leader displaced during optional guard action");
                return;
            }

            if (phase == GuardPhase.Approaching)
            {
                if (Time.time >= nextValidation)
                {
                    nextValidation = Time.time + .5f;
                    if (!SafePoint(destination) || !SafePoint(followAnchor))
                    {
                        Cancel("guard destination lost safety");
                        return;
                    }
                }
                Vector3 velocity = human.GetVelocity();
                velocity.y = 0f;
                Vector3 offset = transform.position - destination;
                offset.y = 0f;
                if (offset.magnitude <= ArrivalDistance && velocity.magnitude <= .8f)
                {
                    phase = GuardPhase.Scanning;
                    phaseStarted = Time.time;
                    traversal.SetSettledHold(this, true);
                    Status = "scanning sector " + sector;
                    return;
                }
                if (Time.time - phaseStarted > 10f)
                {
                    Cancel("guard approach timed out");
                    nextAttempt = Time.time + 3f;
                    return;
                }
                Status = "approaching sector " + sector;
                return;
            }

            if (phase == GuardPhase.Scanning && Time.time - phaseStarted >= ScanSeconds)
            {
                coverage.Resolve(sector, Time.time);
                int completed = sector;
                ReleaseClaim();
                traversal.ClearSettledDestination(this);
                phase = GuardPhase.Cooldown;
                sector = -1;
                settledSince = 0f;
                nextAttempt = Time.time + 4f + StableStagger();
                Status = "resolved sector " + completed + "; opposing sector now preferred";
            }
        }

        void LateUpdate()
        {
            if (!eligible || phase != GuardPhase.Scanning || leader == null || ai == null ||
                ai.GetTargetCreature() != null || ai.IsAlerted()) return;

            Vector3 outward = destination - leader.transform.position;
            outward.y = 0f;
            if (outward.sqrMagnitude < .01f) outward = transform.forward;
            float elapsed = Time.time - phaseStarted;
            float angle = elapsed < 1.6f ? -48f : elapsed < 3.2f ? 0f : elapsed < 4.8f ? 48f : 0f;
            double radians = angle * Math.PI / 180d;
            float sine = (float)Math.Sin(radians), cosine = (float)Math.Cos(radians);
            Vector3 basis = outward.normalized;
            Vector3 scan = new Vector3(basis.x * cosine + basis.z * sine, 0f,
                basis.z * cosine - basis.x * sine);
            lookArgument[0] = transform.position + scan * 8f + Vector3.up;
            Look.Invoke(ai, lookArgument);
        }

        bool BeginCoverage()
        {
            if (!coverageOriginReady || Vector3.Distance(coverageOrigin, leader.transform.position) > CoverageResetDistance)
            {
                coverageOrigin = leader.transform.position;
                coverageOriginReady = true;
                coverage.Reset();
            }

            int unavailable = UnavailableMask(leader);
            int rejected = unavailable;
            int offset = (GetInstanceID() & Int32.MaxValue) % GuardCoverageV2.SectorCount;
            for (int attempt = 0; attempt < GuardCoverageV2.SectorCount; attempt++)
            {
                int candidate = coverage.Choose(rejected, offset);
                if (candidate < 0) return false;
                double radians = GuardCoverageV2.Radians(candidate);
                Vector3 radial = new Vector3((float)Math.Sin(radians), 0f, (float)Math.Cos(radians));
                Vector3 point = leader.transform.position + radial * GuardDistance;
                Vector3 anchorPoint = point + radial * FollowArrivalCompensation;
                if (!Ground(ref point) || !Ground(ref anchorPoint) || !SafePoint(point) || !SafePoint(anchorPoint) ||
                    CrossesLeaderExclusion(transform.position, anchorPoint, leader.transform.position) ||
                    !ClearSegment(transform.position, anchorPoint) || !FullPath(anchorPoint) ||
                    !AcquireClaim(leader, candidate))
                {
                    rejected |= 1 << candidate;
                    continue;
                }
                if (!traversal.SetSettledDestination(this, point, anchorPoint))
                {
                    ReleaseClaim();
                    return false;
                }
                sector = candidate;
                destination = point;
                followAnchor = anchorPoint;
                leaderAtStart = leader.transform.position;
                phase = GuardPhase.Approaching;
                phaseStarted = Time.time;
                nextValidation = 0f;
                Status = "leased sector " + sector;
                return true;
            }
            return false;
        }

        bool Ready()
        {
            if (view == null || !view.IsValid() || !view.IsOwner() || ai == null || human == null ||
                traversal == null || leader == null || leader.IsDead() || human.IsDead() ||
                !human.IsOnGround() || human.IsSwimming() || human.IsFlying() || human.InAttack() ||
                human.IsDrawingBow() || human.IsBlocking() || ai.GetTargetCreature() != null || ai.IsAlerted()) return false;
            Vector3 velocity = leader.GetVelocity();
            return new Vector2(velocity.x, velocity.z).magnitude <= .35f;
        }

        bool SettledAtFormation()
        {
            Vector3 velocity = human.GetVelocity();
            velocity.y = 0f;
            return traversal.FormationTargetDistance >= 0f && traversal.FormationTargetDistance <= 3.35f &&
                velocity.magnitude <= .8f;
        }

        bool LeaderBusy()
        {
            return leader.GetCurrentCraftingStation() != null || leader.InPlaceMode() ||
                InventoryGui.IsVisible() || StoreGui.IsVisible() || Minimap.IsOpen();
        }

        float StableStagger()
        {
            int formationSlot = traversal == null ? -1 : traversal.FormationSlot;
            return formationSlot >= 0 ? formationSlot * .17f :
                ((GetInstanceID() & Int32.MaxValue) % 5) * .18f;
        }

        bool Ground(ref Vector3 point)
        {
            int count = Physics.RaycastNonAlloc(point + Vector3.up * .9f, Vector3.down, rayHits, 1.8f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == rayHits.Length) return false;
            float closest = Single.PositiveInfinity;
            bool found = false;
            for (int index = 0; index < count; index++)
            {
                RaycastHit hit = rayHits[index];
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
            for (int index = 0; index < count; index++)
            {
                Collider candidate = clearance[index];
                clearance[index] = null;
                if (candidate == null || candidate.GetComponentInParent<Character>() == human) continue;
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
            for (int index = 0; index < count; index++)
                if (rayHits[index].collider != null &&
                    rayHits[index].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }

        bool FullPath(Vector3 target)
        {
            if (Pathfinding.instance == null) return false;
            probePath.Clear();
            return Pathfinding.instance.GetPath(transform.position, target, probePath, ai.m_pathAgentType,
                true, true, false) && probePath.Count > 0 &&
                Vector3.Distance(probePath[probePath.Count - 1], target) <= .5f;
        }

        static bool CrossesLeaderExclusion(Vector3 from, Vector3 to, Vector3 leaderPosition)
        {
            float x = to.x - from.x, z = to.z - from.z;
            float lengthSquared = x * x + z * z;
            if (lengthSquared < .01f) return false;
            float projection = ((leaderPosition.x - from.x) * x + (leaderPosition.z - from.z) * z) / lengthSquared;
            projection = Mathf.Clamp01(projection);
            float nearestX = from.x + x * projection - leaderPosition.x;
            float nearestZ = from.z + z * projection - leaderPosition.z;
            return nearestX * nearestX + nearestZ * nearestZ < 1.3225f;
        }

        static int UnavailableMask(Player currentLeader)
        {
            CleanupClaims();
            int mask = 0;
            long leaderId = currentLeader.GetPlayerID();
            foreach (KeyValuePair<HumanSettledGuardModule, GuardClaim> pair in Claims)
                if (pair.Key != null && pair.Key.phase != GuardPhase.Inactive && pair.Value.LeaderId == leaderId)
                    mask |= 1 << pair.Value.Sector;
            return mask;
        }

        bool AcquireClaim(Player currentLeader, int candidate)
        {
            CleanupClaims();
            long leaderId = currentLeader.GetPlayerID();
            foreach (KeyValuePair<HumanSettledGuardModule, GuardClaim> pair in Claims)
                if (pair.Key != this && pair.Value.LeaderId == leaderId && pair.Value.Sector == candidate) return false;
            Claims[this] = new GuardClaim { LeaderId = leaderId, Sector = candidate };
            return true;
        }

        static void CleanupClaims()
        {
            var stale = new List<HumanSettledGuardModule>();
            foreach (HumanSettledGuardModule actor in Claims.Keys)
                if (actor == null || !actor.isActiveAndEnabled) stale.Add(actor);
            foreach (HumanSettledGuardModule actor in stale) Claims.Remove(actor);
        }

        void ReleaseClaim()
        {
            Claims.Remove(this);
        }

        void Cancel(string reason)
        {
            ReleaseClaim();
            if (traversal != null) traversal.ClearSettledDestination(this);
            sector = -1;
            phase = GuardPhase.Inactive;
            settledSince = 0f;
            Status = reason;
        }

        void OnDisable() { eligible = false; Cancel("disabled"); }
        void OnDestroy() { eligible = false; Cancel("destroyed"); }
    }
}

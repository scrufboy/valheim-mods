using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.CompanionActivities;
using ActivityFeedback = LivingWorld.CompanionActivities.Feedback;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Supplies an optional goal to HumanFollowPresence's sole MoveTo executor.
    // Update only cancels/cleans up: it never moves, turns or starts an action.
    public sealed class HumanCompanionAmbient : MonoBehaviour
    {
        const float Radius = 8f, ScanInterval = 2f, Arrival = 0.7f;
        const BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo LastPathTime = typeof(BaseAI).GetField("m_lastFindPathTime", Native);
        static readonly MethodInfo Look = typeof(BaseAI).GetMethod("LookAt", Native, null, new[] { typeof(Vector3) }, null);
        static long sessions;
        struct StationLease { internal HumanCompanionAmbient Actor; internal ActivityToken Token; }
        struct GuardLease { internal HumanCompanionAmbient Actor; internal ActivityToken Token; }
        static readonly Dictionary<CraftingStation, StationLease> Claims = new Dictionary<CraftingStation, StationLease>();
        static readonly Dictionary<string, GuardLease> GuardClaims = new Dictionary<string, GuardLease>();
        readonly Collider[] discovery = new Collider[192], clearance = new Collider[64];
        readonly RaycastHit[] rayHits = new RaycastHit[32];
        readonly List<CraftingStation> stations = new List<CraftingStation>(24);
        readonly List<Opportunity> opportunities = new List<Opportunity>(9);
        readonly List<Vector3> probePath = new List<Vector3>();
        readonly object[] lookArgument = new object[1];
        readonly Dictionary<string, CraftingStation> snapshot = new Dictionary<string, CraftingStation>();
        HumanCompanion companion;
        HumanCompanionPosture posture;
        HumanDoorTraversal doors;
        BaseAI ai;
        Humanoid body;
        ZNetView view;
        Scheduler scheduler;
        GuardCoveragePolicy coverage;
        Player sampledLeader;
        Player coverageLeader;
        Player guardLeader;
        Vector3 leaderSample, point, focus, stationSample;
        Vector3 coverageOrigin;
        CraftingStation station;
        ActivityToken lease;
        ActivityKind kind;
        ActivityPhase phase;
        float nextScan, sampledAt, nextValidation, lastMotion, lastFollow, bestDistance, lastProgress, guardScanStarted;
        int candidateCursor, craftKind, lookStep, guardSector = -1;
        bool haveSample, haveCoverageOrigin, safeWorkbench, safeForge, checkedAnimation, faulted, guardScanResolved;
        string status = "Ambient awaiting a quiet bound player";
        public string Status { get { return status; } }
        internal bool Active { get { return !lease.IsEmpty; } }
        internal bool Performing { get { return phase == ActivityPhase.Performing; } }

        void Cache()
        {
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (posture == null) posture = GetComponent<HumanCompanionPosture>();
            if (doors == null) doors = GetComponent<HumanDoorTraversal>();
            if (body == null) body = GetComponent<Humanoid>();
            if (ai == null) ai = GetComponent<BaseAI>();
            if (view == null) view = GetComponent<ZNetView>();
            if (coverage == null) coverage = new GuardCoveragePolicy();
            if (scheduler == null) scheduler = new Scheduler(++sessions, new ActivitySettings {
                Radius = Radius, CandidateBudget = 12, MinimumDwell = 15, MaximumDwell = 120,
                QuietDelay = 6, BusyQuietDelay = 1.5, Cooldown = 20, FailureRetry = 3, RecentWindow = 180, PassiveInterval = 35,
                StandingPauseCycles = 0, BusyPauseCycles = 0, MaximumContinuityExtensions = 1 });
        }
        bool Owner() { return view != null && view.IsValid() && view.IsOwner(); }
        bool Ready(Player leader)
        {
            return !faulted && isActiveAndEnabled && Owner() && companion != null && companion.PresenceLeader == leader &&
                leader != null && leader == Player.m_localPlayer && !leader.IsDead() && posture != null && body != null && ai != null &&
                !body.IsDead() && body.IsOnGround() && !body.IsSwimming() && !body.IsFlying() && !body.InAttack() &&
                !body.IsDrawingBow() && !body.IsBlocking();
        }
        bool LeaderMoved(Player leader)
        {
            if (!haveSample || sampledLeader != leader)
            { sampledLeader = leader; haveSample = true; leaderSample = leader.transform.position; lastMotion = Time.time; }
            if (Vector3.Distance(leaderSample, leader.transform.position) >= 0.15f || leader.GetVelocity().sqrMagnitude > 0.04f)
            { leaderSample = leader.transform.position; lastMotion = Time.time; }
            return Time.time - lastMotion < 0.35f;
        }
        static bool Sitting(Player leader)
        {
            var net = leader.GetComponent<ZNetView>();
            return leader.InEmote() && net != null && net.IsValid() && net.GetZDO().GetString(ZDOVars.s_emote, "") == "sit";
        }

        internal bool TryDestination(Player leader, bool doorCrossing, out Vector3 destination, out bool hold)
        {
            destination = new Vector3(); hold = false;
            try
            {
                Cache(); lastFollow = Time.time;
                if (!Ready(leader)) { Cancel("authority/order/threat or body state changed"); return false; }
                bool moving = LeaderMoved(leader);
                bool sitting = Sitting(leader);
                bool passage = doorCrossing || doors == null || !doors.MayRestNear(leader.gameObject);
                var currentStation = leader.GetCurrentCraftingStation();
                // Ready already proves this is the bound LOCAL player. Local
                // inventory/merchant UI therefore describes this leader only.
                bool busy = currentStation != null || leader.InPlaceMode() || InventoryGui.IsVisible() || StoreGui.IsVisible() ||
                    Minimap.IsOpen();
                var facts = new Context {
                    Now = Time.time, CandidateSnapshotAt = sampledAt, IsOwner = true, Ready = true, Alive = true,
                    Membership = Membership.Companion, LeaderPresent = true, LeaderAlive = true,
                    LeaderId = leader.GetPlayerID().ToString(System.Globalization.CultureInfo.InvariantCulture),
                    LeaderMoving = moving, PassageActive = passage,
                    LeaderActivity = leader.IsCrouching() ? LeaderActivity.Crouching : sitting ? LeaderActivity.Sitting :
                        busy ? LeaderActivity.Busy : LeaderActivity.Standing,
                    ExternalActionBusy = (!Active && Vector3.Distance(transform.position, leader.transform.position) > CompanionPresencePolicy.RunDistance) ||
                        (leader.InEmote() && !sitting),
                    Capabilities = Capabilities.Observe | Capabilities.Guard | Capabilities.AmbientCraft,
                    InterestGroup = currentStation == null ? "" : StationId(currentStation)
                };
                ActivityFeedback feedback = new ActivityFeedback();
                if (Active)
                {
                    float distance = Vector3.Distance(transform.position, point);
                    if (distance < bestDistance - 0.1f) { bestDistance = distance; lastProgress = Time.time; }
                    bool valid = Vector3.Distance(leader.transform.position, point) <= Radius;
                    if (Time.time >= nextValidation)
                    {
                        nextValidation = Time.time + 0.3f;
                        valid = valid && (station == null ? kind != ActivityKind.AmbientCraft && SafePoint(point) :
                            StationPermitted(station, leader) && Vector3.Distance(station.transform.position, stationSample) < 0.1f &&
                            SafePoint(point) && ClearSegment(transform.position, point));
                        if (!valid) { Cancel("committed target lost safety/access/occupancy"); return false; }
                    }
                    feedback = new ActivityFeedback { Token = lease, LeaseHeld = OwnsClaim(), TargetValid = valid,
                        Arrived = SettledAtPoint(distance),
                        Finished = guardScanResolved,
                        PathFailed = phase != ActivityPhase.Performing && Time.time - lastProgress > 8f };
                }
                else if (!moving && !passage && !sitting && !leader.IsCrouching() && !facts.ExternalActionBusy &&
                    Time.time >= nextScan && Time.time >= scheduler.NextDecisionAt)
                {
                    nextScan = Time.time + ScanInterval;
                    Discover(leader, busy); facts.CandidateSnapshotAt = sampledAt;
                }
                ActivityKind previousKind = kind;
                var decision = scheduler.Tick(facts, opportunities, feedback);
                Trace(decision, previousKind);
                if (!decision.ReleasedToken.IsEmpty) Release();
                status = decision.Reason + "; " + decision.Phase;
                if (decision.Control == Control.Begin)
                {
                    station = null;
                    if (decision.Opportunity.Kind == ActivityKind.AmbientCraft && !snapshot.TryGetValue(decision.Opportunity.Id, out station)) return false;
                    if (station != null && !Acquire(station, decision.Token)) { station = null; return false; }
                    int selectedGuardSector = decision.Opportunity.Kind == ActivityKind.GuardPosition ?
                        (int)Math.Round(decision.Opportunity.Yaw) : -1;
                    if (selectedGuardSector >= 0 && !AcquireGuard(leader, selectedGuardSector, decision.Token))
                    { if (station != null) Release(); return false; }
                    lease = decision.Token; kind = decision.Opportunity.Kind; phase = decision.Phase;
                    guardLeader = selectedGuardSector >= 0 ? leader : null;
                    guardSector = selectedGuardSector; guardScanStarted = 0f; guardScanResolved = false;
                    point = new Vector3(decision.Opportunity.X, decision.Opportunity.Y, decision.Opportunity.Z);
                    craftKind = station == null ? 0 : station.m_useAnimation;
                    stationSample = station == null ? point : station.transform.position;
                    focus = station == null ? point + (point - leader.transform.position).normalized * 2f : station.transform.position;
                    bestDistance = Vector3.Distance(transform.position, point); lastProgress = Time.time; nextValidation = 0f;
                    lookStep = 0;
                    ResetPath();
                }
                if (!Active) return false;
                phase = decision.Phase;
                hold = phase == ActivityPhase.Performing;
                destination = point;
                return true;
            }
            catch (Exception error) { faulted = true; Cancel("disabled after fault: " + error.Message); return false; }
        }

        bool SettledAtPoint(float distance)
        {
            if (distance > Arrival) return false;
            // Native character velocity includes vertical grounding correction.
            // That value can remain above the old full-vector threshold after
            // MoveTo has reached and stopped at its goal, trapping the activity
            // in Approaching/AwaitingLease forever. Arrival is horizontal intent.
            Vector3 velocity = body.GetVelocity(); velocity.y = 0f;
            return velocity.sqrMagnitude < 0.49f;
        }

        // Called by the one follow executor only AFTER it has stopped movement.
        internal void AfterStop()
        {
            if (!Active || !Performing || !Owner()) return;
            if (Look == null) { Cancel("native facing contract unavailable"); return; }
            if (kind == ActivityKind.GuardPosition)
            {
                if (guardScanStarted <= 0f)
                {
                    guardScanStarted = Time.time; lookStep = 0;
                }
                float scanElapsed = Time.time - guardScanStarted;
                int resolvedStep = scanElapsed < 1.6f ? 0 : scanElapsed < 3.2f ? 1 : scanElapsed < 4.8f ? 2 : 3;
                lookStep = resolvedStep;
                Vector3 outward = focus - point; outward.y = 0f;
                if (outward.sqrMagnitude < 0.01f) outward = transform.forward;
                float offset = lookStep == 0 ? -48f : lookStep == 1 ? 0f : lookStep == 2 ? 48f : 0f;
                Vector3 scanBase = outward.normalized;
                double radians = offset * Math.PI / 180.0;
                float sine = (float)Math.Sin(radians), cosine = (float)Math.Cos(radians);
                Vector3 scan = new Vector3(scanBase.x * cosine + scanBase.z * sine, 0f,
                    scanBase.z * cosine - scanBase.x * sine);
                lookArgument[0] = transform.position + scan * 5f + Vector3.up;
                Look.Invoke(ai, lookArgument);
                if (!guardScanResolved && scanElapsed >= 6.0f && coverage != null && guardSector >= 0)
                {
                    guardScanResolved = true;
                    coverage.Resolve(guardSector, Time.time);
                    var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
                    if (plugin != null) plugin.Report("GUARD_SCAN resolved=" + guardSector +
                        "; next=" + GuardCoveragePolicy.Opposite(guardSector) + "; trigger=coverage");
                }
                return;
            }
            if (kind == ActivityKind.Observe)
            {
                lookArgument[0] = focus + Vector3.up; Look.Invoke(ai, lookArgument); return;
            }
            lookArgument[0] = focus + Vector3.up; Look.Invoke(ai, lookArgument);
            if (kind != ActivityKind.AmbientCraft || posture.HasAmbientPose) return;
            Vector3 direction = focus - transform.position; direction.y = 0f;
            if (Vector3.Angle(transform.forward, direction) > 12f) return;
            if (!posture.BeginAmbientCraft(craftKind)) Cancel("native hand/pose lease unavailable");
        }
        void Discover(Player leader, bool busy)
        {
            opportunities.Clear(); snapshot.Clear(); stations.Clear(); sampledAt = Time.time;
            if (!checkedAnimation)
            {
                var animator = GetComponentInChildren<Animator>();
                safeWorkbench = AmbientAnimationContract.Supports(animator, 1);
                safeForge = AmbientAnimationContract.Supports(animator, 2); checkedAnimation = true;
            }
            // Do not schedule the actor's current location as an "activity".
            // That old begin/stop cycle had no visible purpose and produced
            // the repeated stationary twitch seen in the 0.3.4 test.
            DiscoverWalks(leader);
            if (!safeWorkbench && !safeForge) return;
            int count = Physics.OverlapSphereNonAlloc(leader.transform.position, Radius, discovery, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == discovery.Length) { Array.Clear(discovery, 0, discovery.Length); return; }
            for (int i = 0; i < count; ++i)
            {
                var candidate = discovery[i] == null ? null : discovery[i].GetComponentInParent<CraftingStation>();
                discovery[i] = null;
                if (candidate != null && !stations.Contains(candidate) && stations.Count < 24) stations.Add(candidate);
            }
            stations.Sort(delegate(CraftingStation a, CraftingStation b) { return a.GetInstanceID().CompareTo(b.GetInstanceID()); });
            int start = stations.Count == 0 ? 0 : candidateCursor % stations.Count, queries = 0;
            for (int n = 0; n < stations.Count && n < 8 && queries < 2; ++n)
            {
                int index = (start + n) % stations.Count; candidateCursor = index + 1;
                var candidate = stations[index];
                if (!StationPermitted(candidate, leader) || candidate == leader.GetCurrentCraftingStation() || ClaimedByOther(candidate)) continue;
                Vector3 usePoint = candidate.transform.position + candidate.transform.forward * 1.35f;
                if (!Ground(ref usePoint) || !SafePoint(usePoint) || !ClearSegment(transform.position, usePoint)) continue;
                ++queries;
                if (!FullPath(usePoint)) continue;
                string id = StationId(candidate); snapshot[id] = candidate;
                opportunities.Add(new Opportunity { Id = id, InterestGroup = id, Kind = ActivityKind.AmbientCraft,
                    Safe = true, Reachable = true, Permitted = true, Available = true, Duration = 60,
                    LeaderDistance = Vector3.Distance(leader.transform.position, usePoint), ActorDistance = Vector3.Distance(transform.position, usePoint),
                    X = usePoint.x, Y = usePoint.y, Z = usePoint.z });
            }
        }
        void DiscoverWalks(Player leader)
        {
            // A guard move answers a coverage deficit. A completed field scan
            // selects the opposing sector next; old-area coverage is discarded
            // after meaningful leader displacement. No random idle roll moves it.
            if (coverageLeader != leader || !haveCoverageOrigin || Vector3.Distance(coverageOrigin, leader.transform.position) > 8f)
            {
                coverageLeader = leader; coverageOrigin = leader.transform.position; haveCoverageOrigin = true;
                coverage.Reset();
            }
            int companionOffset = (GetInstanceID() & Int32.MaxValue) % GuardCoveragePolicy.SectorCount;
            int otherSector;
            int occupied = GuardUnavailableMask(leader, out otherSector);
            if (otherSector >= 0) coverage.Prefer(GuardCoveragePolicy.Opposite(otherSector));
            int rejected = occupied;
            for (int attempt = 0; attempt < GuardCoveragePolicy.SectorCount; ++attempt)
            {
                int sector = coverage.Choose(rejected, companionOffset);
                if (sector < 0) break;
                double angle = GuardCoveragePolicy.Radians(sector);
                float distance = 4.5f + (sector % 3) * 0.5f;
                Vector3 walk = leader.transform.position + new Vector3((float)Math.Sin(angle), 0f, (float)Math.Cos(angle)) * distance;
                if (!Ground(ref walk) || !SafePoint(walk) || CrossesLeaderExclusion(transform.position, walk, leader.transform.position) ||
                    !ClearSegment(transform.position, walk) || !FullPath(walk))
                { rejected |= 1 << sector; continue; }
                opportunities.Add(new Opportunity { Id = GuardKey(leader, sector), Kind = ActivityKind.GuardPosition,
                    Safe = true, Reachable = true, Permitted = true, Available = true,
                    Duration = 8, CompletionCooldown = 2.5,
                    LeaderDistance = Vector3.Distance(leader.transform.position, walk), ActorDistance = Vector3.Distance(transform.position, walk),
                    X = walk.x, Y = walk.y, Z = walk.z, Yaw = sector });
                break;
            }
        }

        static string GuardKey(Player leader, int sector)
        { return "guard:" + leader.GetPlayerID().ToString(System.Globalization.CultureInfo.InvariantCulture) + ":" + sector; }

        int GuardUnavailableMask(Player leader, out int otherSector)
        {
            int mask = 0; otherSector = -1;
            for (int sector = 0; sector < GuardCoveragePolicy.SectorCount; ++sector)
            {
                string key = GuardKey(leader, sector); GuardLease claim;
                if (!GuardClaims.TryGetValue(key, out claim)) continue;
                bool valid = claim.Actor != null && claim.Actor != this && claim.Actor.isActiveAndEnabled &&
                    claim.Actor.guardLeader == leader && claim.Actor.guardSector == sector &&
                    claim.Token.Equals(claim.Actor.lease);
                if (!valid) { GuardClaims.Remove(key); continue; }
                mask |= 1 << sector;
                if (otherSector < 0) otherSector = sector;
            }
            return mask;
        }

        bool AcquireGuard(Player leader, int sector, ActivityToken token)
        {
            if (leader == null || token.IsEmpty || sector < 0 || sector >= GuardCoveragePolicy.SectorCount) return false;
            string key = GuardKey(leader, sector); GuardLease claim;
            if (GuardClaims.TryGetValue(key, out claim) && claim.Actor != null && claim.Actor != this &&
                claim.Actor.isActiveAndEnabled && claim.Token.Equals(claim.Actor.lease)) return false;
            GuardClaims[key] = new GuardLease { Actor = this, Token = token };
            return true;
        }

        static bool CrossesLeaderExclusion(Vector3 from, Vector3 to, Vector3 leader)
        {
            float dx = to.x - from.x, dz = to.z - from.z;
            float lengthSquared = dx * dx + dz * dz;
            if (lengthSquared < 0.01f) return false;
            float t = ((leader.x - from.x) * dx + (leader.z - from.z) * dz) / lengthSquared;
            if (t < 0f) t = 0f; else if (t > 1f) t = 1f;
            float closestX = from.x + dx * t, closestZ = from.z + dz * t;
            float offsetX = closestX - leader.x, offsetZ = closestZ - leader.z;
            return offsetX * offsetX + offsetZ * offsetZ < 1.3225f;
        }
        bool StationPermitted(CraftingStation target, Player leader)
        {
            if (target == null || !target.isActiveAndEnabled || leader == null || leader != Player.m_localPlayer) return false;
            string name = target.gameObject.name;
            bool bench = (name == "piece_workbench" || name == "piece_workbench(Clone)") && target.m_useAnimation == 1 && safeWorkbench;
            bool forge = (name == "forge" || name == "forge(Clone)") && target.m_useAnimation == 2 && safeForge;
            if (!bench && !forge) return false;
            var piece = target.GetComponent<Piece>(); var net = target.GetComponent<ZNetView>();
            // Exclusive native authority + creator is deliberately conservative:
            // a private local lease cannot reserve another peer's station.
            if (piece == null || piece.GetCreator() != leader.GetPlayerID() || net == null || !net.IsValid() || !net.IsOwner() ||
                !PrivateArea.CheckAccess(target.transform.position, 0f, false, false)) return false;
            if (target.m_craftRequireFire) return false; // No fire/resource lifecycle borrowed.
            if (target.m_craftRequireRoof)
            {
                if (target.m_roofCheckPoint == null) return false;
                float cover; bool roof;
                Cover.GetCoverForPoint(target.m_roofCheckPoint.position, out cover, out roof, 0.5f);
                if (!roof || cover < 0.7f) return false;
            }
            return true;
        }
        static string StationId(CraftingStation target) { return "station:" + target.GetInstanceID().ToString(System.Globalization.CultureInfo.InvariantCulture); }
        bool Ground(ref Vector3 at)
        {
            int count = Physics.RaycastNonAlloc(at + Vector3.up * 0.75f, Vector3.down, rayHits, 1.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == rayHits.Length) return false;
            float best = Single.PositiveInfinity; Vector3 ground = at; bool found = false;
            for (int i = 0; i < count; ++i)
            {
                var h = rayHits[i];
                if (h.collider == null || h.collider.GetComponentInParent<Character>() != null || h.normal.y < 0.8f || h.distance >= best) continue;
                best = h.distance; ground = h.point; found = true;
            }
            if (found) at = ground;
            return found;
        }
        bool SafePoint(Vector3 at)
        {
            int count = Physics.OverlapCapsuleNonAlloc(at + Vector3.up * 0.45f, at + Vector3.up * 1.45f, 0.35f,
                clearance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
            if (count == clearance.Length) return false;
            for (int i = 0; i < count; ++i)
            {
                var c = clearance[i]; clearance[i] = null;
                if (c == null || c.GetComponentInParent<Character>() == body) continue;
                if (c.GetComponentInParent<Aoe>() != null || c.GetComponentInParent<Smoke>() != null) return false;
                if (!c.isTrigger) return false;
            }
            return !EffectArea.IsPointPlus025InsideBurningArea(at);
        }
        bool ClearSegment(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.01f) return true;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * 0.8f, delta.normalized, rayHits, delta.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == rayHits.Length) return false;
            for (int i = 0; i < count; ++i)
                if (rayHits[i].collider != null && rayHits[i].collider.GetComponentInParent<Character>() == null) return false;
            return true;
        }
        bool FullPath(Vector3 to)
        {
            if (Vector3.Distance(transform.position, to) < Arrival) return true;
            if (Pathfinding.instance == null) return false;
            probePath.Clear();
            return Pathfinding.instance.GetPath(transform.position, to, probePath, ai.m_pathAgentType, true, true, false) &&
                probePath.Count > 0 && Vector3.Distance(probePath[probePath.Count - 1], to) <= 0.5f;
        }
        bool ClaimedByOther(CraftingStation target)
        {
            StationLease claim;
            return Claims.TryGetValue(target, out claim) && claim.Actor != null && claim.Actor != this &&
                claim.Actor.isActiveAndEnabled && claim.Actor.Owner() && claim.Token.Equals(claim.Actor.lease);
        }
        Control lastTraceControl;
        Reason lastTraceReason;
        ActivityPhase lastTracePhase;
        bool haveTrace;
        void Trace(Decision decision, ActivityKind previousKind)
        {
            if (haveTrace && decision.Control == lastTraceControl && decision.Reason == lastTraceReason && decision.Phase == lastTracePhase) return;
            bool phaseChanged = !haveTrace || decision.Phase != lastTracePhase;
            haveTrace = true; lastTraceControl = decision.Control; lastTraceReason = decision.Reason; lastTracePhase = decision.Phase;
            bool useful = phaseChanged || decision.Control == Control.Begin || decision.Control == Control.Perform || decision.Control == Control.Complete ||
                decision.Reason == Reason.Extended || decision.Reason == Reason.PathFailed || decision.Reason == Reason.ApproachTimeout ||
                decision.Reason == Reason.TargetUnavailable || decision.Reason == Reason.NoCandidate;
            if (!useful) return;
            ActivityKind reported = decision.Opportunity.Kind != ActivityKind.None ? decision.Opportunity.Kind : previousKind;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("AMBIENT control=" + decision.Control + "; reason=" + decision.Reason +
                "; phase=" + decision.Phase + "; activity=" + reported + "; candidates=" + decision.CandidatesExamined);
        }
        bool Acquire(CraftingStation target, ActivityToken token)
        {
            if (token.IsEmpty || ClaimedByOther(target) || Claims.Count >= 64 && !Claims.ContainsKey(target)) return false;
            Claims[target] = new StationLease { Actor = this, Token = token }; return true;
        }
        bool OwnsClaim()
        {
            if (kind == ActivityKind.GuardPosition)
            {
                if (guardLeader == null || guardSector < 0) return false;
                GuardLease guardClaim;
                return GuardClaims.TryGetValue(GuardKey(guardLeader, guardSector), out guardClaim) &&
                    guardClaim.Actor == this && guardClaim.Token.Equals(lease);
            }
            if (kind != ActivityKind.AmbientCraft) return true;
            StationLease claim;
            return station != null && Claims.TryGetValue(station, out claim) && claim.Actor == this && claim.Token.Equals(lease);
        }
        void ResetPath() { if (ai != null && LastPathTime != null && Owner()) LastPathTime.SetValue(ai, Single.NegativeInfinity); }
        void Release()
        {
            StationLease claim;
            if (!ReferenceEquals(station, null) && Claims.TryGetValue(station, out claim) && claim.Actor == this && claim.Token.Equals(lease)) Claims.Remove(station);
            if (guardLeader != null && guardSector >= 0)
            {
                string key = GuardKey(guardLeader, guardSector); GuardLease guardClaim;
                if (GuardClaims.TryGetValue(key, out guardClaim) && guardClaim.Actor == this && guardClaim.Token.Equals(lease))
                    GuardClaims.Remove(key);
            }
            if (posture != null) posture.StopAmbient();
            station = null; guardLeader = null; guardSector = -1; guardScanStarted = 0f; guardScanResolved = false;
            lease = new ActivityToken(); kind = ActivityKind.None; phase = ActivityPhase.None;
            ResetPath();
        }
        internal void Cancel(string reason)
        {
            if (scheduler != null) scheduler.Tick(new Context { IsOwner = false }, null, new ActivityFeedback());
            if (Active) Release(); else if (posture != null && posture.HasAmbientPose) posture.StopAmbient();
            nextScan = Math.Max(nextScan, Time.time + 6f); status = reason;
        }
        void Update()
        {
            if (!Active) return;
            Player leader = companion == null ? null : companion.PresenceLeader;
            if (!Ready(leader) || leader.IsCrouching() || Sitting(leader) || LeaderMoved(leader) || Time.time - lastFollow > 0.5f)
                Cancel("optional activity interrupted; native order retained");
        }
        void OnDisable() { Cancel("disabled"); }
        void OnDestroy() { Release(); }
    }
}

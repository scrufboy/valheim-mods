using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    internal enum DoorTravelPurpose { None, PeacefulTransit, CombatPursuit }

    // A bounded travel aid, not a second movement/combat controller. It only
    // supplies a temporary approach/exit goal to native MoveTo while travelling
    // on an existing companion order. Native locomotion/pathfinding still runs.
    public sealed class HumanDoorTraversal : MonoBehaviour
    {
        const BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo CanInteract = typeof(Door).GetMethod("CanInteract", Native, null, Type.EmptyTypes, null);
        static readonly MethodInfo Open = typeof(Door).GetMethod("Open", Native, null, new[] { typeof(Vector3) }, null);
        static readonly FieldInfo LastPathTime = typeof(BaseAI).GetField("m_lastFindPathTime", Native);
        static readonly FieldInfo Path = typeof(BaseAI).GetField("m_path", Native);
        static readonly Dictionary<Door, HumanDoorTraversal> Claims = new Dictionary<Door, HumanDoorTraversal>();
        readonly RaycastHit[] hits = new RaycastHit[64];
        readonly Collider[] nearby = new Collider[256];
        readonly List<Door> candidates = new List<Door>();
        readonly List<Vector3> probePath = new List<Vector3>();
        HumanCompanion companion;
        BaseAI ai;
        Door door, retiredDoor;
        ZNetView doorView;
        Vector3 center, normal, lateral, approach, exit;
        float halfWidth, entrySign, deadline, nextScan, nextObservation, closeAt, retiredUntil;
        float lastMovement, lastProgress, bestDistance;
        int openedState;
        bool openedHere, disabledByFault;
        float nextDiscoveryReport, nextShortProbe;
        float nextMotionReport;
        Vector3 lastReportedPosition;
        bool haveMotionSample;
        bool shortBlocked;
        Vector3 shortProbeGoal;
        int candidateOffset;
        DoorTravelPhase phase;
        DoorTravelPurpose purpose;
        string status = "Idle: door travel follows existing orders";
        public string Status { get { return status; } }

        internal static void InstallPatches(Harmony harmony)
        {
            var move = typeof(BaseAI).GetMethod("MoveTo", Native, null,
                new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }, null);
            if (move == null || CanInteract == null || Open == null || LastPathTime == null || Path == null)
                throw new MissingMethodException("Native door travel contract changed.");
            harmony.Patch(move,
                prefix: new HarmonyMethod(typeof(HumanDoorTraversal).GetMethod("BeforeMove", BindingFlags.Static | BindingFlags.NonPublic)),
                postfix: new HarmonyMethod(typeof(HumanDoorTraversal).GetMethod("AfterMove", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        void Cache()
        {
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (ai == null) ai = GetComponent<BaseAI>();
        }
        bool Authorized()
        {
            Cache();
            return !disabledByFault && isActiveAndEnabled && companion != null && companion.MayTravelThroughDoors;
        }
        bool Access(Door target)
        {
            if (!Authorized() || target == null || !target.isActiveAndEnabled) return false;
            var view = target.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return false;
            // CheckAccess uses the local player's identity. Authorized explicitly
            // requires that this is the bound owner, never an unrelated player.
            return DoorTravelPolicy.Access(true, true, view.IsOwner(), target.m_keyItem != null,
                target.m_canNotBeClosed, target.m_checkGuardStone,
                !target.m_checkGuardStone || PrivateArea.CheckAccess(target.transform.position, 0f, false, false));
        }
        int State() { return doorView.GetZDO().GetInt(ZDOVars.s_state, 0); }

        bool Step(ref Vector3 destination, ref float stopDistance, ref bool run, out bool redirected)
        {
            redirected = false;
            if (!Authorized() || !companion.IsDoorTravelGoal(destination))
            { if (phase != DoorTravelPhase.None) Cancel("Travel interrupted", true); return true; }
            lastMovement = Time.time;
            if (phase == DoorTravelPhase.None && Time.time >= nextScan)
            {
                nextScan = Time.time + 1f;
                Discover(destination);
            }
            if (phase == DoorTravelPhase.None) return true;
            // An intentional change of direction cancels the door commitment.
            if ((phase == DoorTravelPhase.Approach || phase == DoorTravelPhase.Opening || phase == DoorTravelPhase.Crossing) &&
                Vector3.Dot(destination - center, normal) * entrySign > -0.3f)
            {
                if (!openedHere) { Cancel("Destination no longer crosses this door before opening", true); return true; }
                if (purpose == DoorTravelPurpose.CombatPursuit)
                { Cancel("Combat pursuit redirected; opened passage left available", false); return true; }
                // Keep the closing obligation on a reversal. If still in the
                // passage, return to the entry clearance before waiting to close.
                exit = center + normal * entrySign * 2f;
                phase = DoorTravelPhase.Closing; closeAt = Time.time + 0.8f; deadline = Time.time + DoorTravelPolicy.MaxIntentSeconds;
                ResetRoute(); Log("Redirect cleanup", "retaining opened door until clear or interrupted");
            }
            if (Time.time >= nextObservation)
            {
                nextObservation = Time.time + DoorTravelPolicy.ScanInterval;
                Observe();
            }
            if (phase == DoorTravelPhase.None) return true;
            if (phase == DoorTravelPhase.Closing || phase == DoorTravelPhase.ClosePending)
            {
                if (Horizontal(transform.position, exit) > 0.3f && phase == DoorTravelPhase.Closing)
                { destination = exit; stopDistance = 0.15f; run = false; redirected = true; return true; }
                ai.StopMoving(); return false;
            }
            if (phase == DoorTravelPhase.Opening)
            { ai.StopMoving(); return false; }
            destination = phase == DoorTravelPhase.Approach ? approach : exit;
            stopDistance = 0.15f; run = false; redirected = true;
            float distance = Horizontal(transform.position, destination);
            if (distance < bestDistance - 0.15f) { bestDistance = distance; lastProgress = Time.time; }
            if (Time.time - lastProgress > 5f)
            { Cancel("Native approach made no progress; intent released", true); redirected = false; return false; }
            return true;
        }

        void Discover(Vector3 destination)
        {
            // Only the first solid obstacle on the current travel line can
            // create door knowledge. Never scan a building and choose another
            // entrance the companion has not observed or reached.
            float blockerDistance;
            var direct = DirectBlockingDoor(transform.position, destination, out blockerDistance);
            if (direct == null) return;
            candidates.Clear();
            candidates.Add(direct);
            int probed = 0;
            for (int n = 0; n < candidates.Count && probed < 2; ++n)
            {
                var candidate = candidates[n];
                HumanDoorTraversal claimant;
                bool claimed = Claims.TryGetValue(candidate, out claimant) && claimant != null && claimant != this;
                if (!Access(candidate) || !CaptureGeometry(candidate)) continue;
                var view = candidate.GetComponent<ZNetView>();
                if (view.GetZDO().GetInt(ZDOVars.s_state, 0) != 0) continue;
                float actorSide = Vector3.Dot(transform.position - center, normal);
                float targetSide = Vector3.Dot(destination - center, normal);
                bool retired = candidate == retiredDoor && Time.time < retiredUntil;
                if (!DoorTravelPolicy.DirectBlocker(true, true, blockerDistance, true, claimed, retired) ||
                    !DoorTravelPolicy.RouteCandidate(actorSide, targetSide, Horizontal(transform.position, center),
                    center.y - transform.position.y, true, claimed, retired)) continue;
                entrySign = Mathf.Sign(actorSide);
                approach = center + normal * entrySign * 1.4f;
                exit = center - normal * entrySign * 2f;
                ++probed;
                // A nearby door alone is not a route. Both sides must connect
                // to the actor and intended destination, with no other wall in
                // the crossing. Probe paths never overwrite the AI's live path.
                if (SegmentBlocked(approach, exit, candidate) || !FullPath(transform.position, approach) ||
                    !FullPath(exit, destination)) continue;
                door = candidate; doorView = view; openedHere = false; openedState = 0;
                purpose = companion.HasAuthorizedCombat ? DoorTravelPurpose.CombatPursuit : DoorTravelPurpose.PeacefulTransit;
                Claims[door] = this;
                deadline = Time.time + DoorTravelPolicy.MaxIntentSeconds;
                phase = DoorTravelPhase.Approach; lastProgress = Time.time; bestDistance = Single.PositiveInfinity;
                ResetRoute();
                Log("Approach", "reachable entrance committed; native movement retained");
                return;
            }
            DiscoveryReport("Direct blocking door observed; routes checked=" + probed + "; no authorized complete entrance route");
        }

        Door DirectBlockingDoor(Vector3 from, Vector3 to, out float distance)
        {
            distance = Single.PositiveInfinity;
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < .01f) return null;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * .8f, delta.normalized, hits,
                delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return null;
            Collider first = null;
            for (int i = 0; i < count; ++i)
            {
                var collider = hits[i].collider;
                if (collider == null || collider.GetComponentInParent<Character>() != null) continue;
                if (hits[i].distance < distance) { distance = hits[i].distance; first = collider; }
            }
            return first == null ? null : first.GetComponentInParent<Door>();
        }

        bool FullPath(Vector3 from, Vector3 to)
        {
            // Some native path queries return no corners for coincident points.
            if (Vector3.Distance(from, to) < 0.25f && !SegmentBlocked(from, to, null)) return true;
            if (Pathfinding.instance == null) return false;
            probePath.Clear();
            bool complete = Pathfinding.instance.GetPath(from, to, probePath, ai.m_pathAgentType, true, true, false);
            float error = probePath.Count == 0 ? Single.PositiveInfinity : Vector3.Distance(probePath[probePath.Count - 1], to);
            return DoorTravelPolicy.FullLeg(complete, error, probePath.Count);
        }

        bool SegmentBlocked(Vector3 from, Vector3 to, Door allowedDoor)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.01f) return false;
            int count = Physics.RaycastNonAlloc(from + Vector3.up * 0.8f, delta.normalized, hits,
                delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (count == hits.Length) return true;
            for (int i = 0; i < count; ++i)
            {
                var collider = hits[i].collider;
                if (collider == null || collider.GetComponentInParent<Character>() != null) continue;
                if (allowedDoor != null && collider.GetComponentInParent<Door>() == allowedDoor) continue;
                return true;
            }
            return false;
        }

        void ResetRoute()
        {
            // Native FindPath otherwise reuses the previous goal for up to a
            // second, or five seconds when the new destination is nearby.
            if (ai != null) LastPathTime.SetValue(ai, Single.NegativeInfinity);
        }

        void DiscoveryReport(string reason)
        {
            status = reason;
            if (Time.time < nextDiscoveryReport) return;
            nextDiscoveryReport = Time.time + 10f;
            Log("Discovery", reason);
        }

        internal bool NeedsCloseFollow(GameObject target)
        {
            if (target == null || !Authorized()) return false;
            Vector3 goal = target.transform.position;
            if (!companion.IsDoorTravelGoal(goal)) return false;
            float distance = Vector3.Distance(transform.position, goal);
            if (distance >= 3f) return false;
            if (Time.time >= nextShortProbe || Vector3.Distance(shortProbeGoal, goal) > 0.75f)
            {
                nextShortProbe = Time.time + DoorTravelPolicy.ScanInterval;
                shortProbeGoal = goal;
                shortBlocked = SegmentBlocked(transform.position, goal, null);
            }
            bool crossing = phase != DoorTravelPhase.None;
            return DoorTravelPolicy.ContinueShortFollow(true, true, distance, crossing, shortBlocked);
        }

        internal bool MayRestNear(GameObject target)
        {
            // Share the bounded 0.3-second obstruction observation. Never sit
            // during a committed crossing or on the wrong side of a wall.
            if (target == null || !Authorized() || phase != DoorTravelPhase.None) return false;
            return !NeedsCloseFollow(target) && !shortBlocked;
        }

        bool CaptureGeometry(Door candidate)
        {
            normal = candidate.transform.forward; normal.y = 0f;
            if (normal.sqrMagnitude < 0.9f) return false; // Tilted/nonvertical doors unsupported.
            normal.Normalize(); lateral = Vector3.Cross(Vector3.up, normal);
            float min = Single.PositiveInfinity, max = Single.NegativeInfinity, bottom = Single.PositiveInfinity;
            Vector3 pivot = candidate.transform.position;
            foreach (var box in candidate.GetComponentsInChildren<BoxCollider>())
            {
                if (!box.enabled || box.isTrigger) continue;
                var x = box.transform.TransformVector(Vector3.right * box.size.x * 0.5f);
                var y = box.transform.TransformVector(Vector3.up * box.size.y * 0.5f);
                var z = box.transform.TransformVector(Vector3.forward * box.size.z * 0.5f);
                float width = Projection(x, y, z, lateral), depth = Projection(x, y, z, normal), height = Projection(x, y, z, Vector3.up);
                if (width < 0.45f || depth > 0.4f || height < 0.65f) continue; // Exclude posts, trim and threshold.
                Vector3 at = box.transform.TransformPoint(box.center);
                float offset = Vector3.Dot(at - pivot, lateral);
                min = Mathf.Min(min, offset - width); max = Mathf.Max(max, offset + width);
                bottom = Mathf.Min(bottom, at.y - height);
            }
            if (Single.IsInfinity(min) || max - min > 6f || Math.Abs(bottom - transform.position.y) > 1.5f) return false;
            halfWidth = (max - min) * 0.5f;
            center = pivot + lateral * (min + max) * 0.5f;
            center.y = bottom; // Bottom of closed leaf, not pivot at chest height.
            return true;
        }
        static float Projection(Vector3 x, Vector3 y, Vector3 z, Vector3 axis)
        { return Math.Abs(Vector3.Dot(x, axis)) + Math.Abs(Vector3.Dot(y, axis)) + Math.Abs(Vector3.Dot(z, axis)); }

        void Update()
        {
            try
            {
                MotionReport();
                if (phase == DoorTravelPhase.None || Time.time < nextObservation) return;
                nextObservation = Time.time + DoorTravelPolicy.ScanInterval;
                if (!Authorized() || ((phase == DoorTravelPhase.Approach || phase == DoorTravelPhase.Opening || phase == DoorTravelPhase.Crossing) && Time.time - lastMovement > 1f))
                { Cancel("Travel stopped or combat took priority", true); return; }
                Observe();
            }
            catch (Exception error) { Fault(error); }
        }

        void MotionReport()
        {
            // Lab-only observation, at most one sample per ten seconds. This is
            // not a movement controller and never changes the route or order.
            if (Time.time < nextMotionReport) return;
            nextMotionReport = Time.time + 10f;
            if (!Authorized()) { haveMotionSample = false; return; }
            var monster = ai as MonsterAI;
            var target = monster == null ? null : monster.GetFollowTarget();
            if (target == null) { haveMotionSample = false; return; }
            var route = Path.GetValue(ai) as List<Vector3>;
            int corners = route == null ? 0 : route.Count;
            float displacement = haveMotionSample ? Vector3.Distance(lastReportedPosition, transform.position) : -1f;
            float waypointDistance = corners == 0 ? -1f : Horizontal(transform.position, route[0]);
            float turn = corners == 0 ? 0f : Vector3.Angle(transform.forward, route[0] - transform.position);
            lastReportedPosition = transform.position; haveMotionSample = true;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report(String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "NAV phase={0}; leaderDistance={1:F2}; displacementSinceSample={2:F2}; corners={3}; nextCornerDistance={4:F2}; turn={5:F1}; avoidFire={6}",
                phase, Vector3.Distance(transform.position, target.transform.position), displacement, corners, waypointDistance, turn, ai.m_avoidFire));
        }
        void Observe()
        {
            bool valid = door != null && doorView != null && doorView.IsValid();
            if (!valid) { Cancel("Door unloaded or removed", false); return; }
            int state = State();
            // Once open has been acknowledged, a different state means somebody
            // intervened. Never take their newer decision back.
            if (openedState != 0 && phase != DoorTravelPhase.ClosePending && state != openedState)
            { Cancel("Door state changed externally", true); return; }
            Vector3 offset = transform.position - center;
            bool crossed = DoorTravelPolicy.HasCrossed(entrySign, Vector3.Dot(offset, normal), Vector3.Dot(offset, lateral), halfWidth);
            var input = new DoorTravelInput {
                Phase = phase, Authorized = Authorized(), DoorValid = valid, AccessAllowed = Access(door),
                Keyed = door.m_keyItem != null, CannotClose = door.m_canNotBeClosed,
                TimedOut = Time.time >= deadline, InUseRange = Horizontal(transform.position, center) <= DoorTravelPolicy.UseRange &&
                    Horizontal(transform.position, approach) <= 0.75f && !SegmentBlocked(transform.position, center, door),
                IsOpen = state != 0, CanInteract = (bool)CanInteract.Invoke(door, null), Crossed = crossed,
                OpenedHere = openedHere, CrossingClear = phase == DoorTravelPhase.Closing && CrossingClear(),
                CloseDelayElapsed = Time.time >= closeAt,
                WithinCloseRange = Horizontal(transform.position, center) <= DoorTravelPolicy.CloseRange
            };
            var action = DoorTravelPolicy.Evaluate(input);
            if (action == DoorTravelAction.Cancel || action == DoorTravelAction.Finish)
            {
                if (action == DoorTravelAction.Finish) Log("Closed", "native close acknowledged");
                Cancel(action + "; phase=" + input.Phase + "; authorized=" + input.Authorized +
                    "; access=" + input.AccessAllowed + "; timeout=" + input.TimedOut +
                    "; openedHere=" + input.OpenedHere + "; closeRange=" + input.WithinCloseRange +
                    "; doorwayClear=" + input.CrossingClear, true); return;
            }
            if (action == DoorTravelAction.Open)
            {
                // Commit phase BEFORE a native toggle. Never reissue while waiting.
                phase = DoorTravelPhase.Opening; deadline = Time.time + 4f;
                Open.Invoke(door, new object[] { transform.position - door.transform.position });
                openedHere = true; Log("Open requested", "waiting for native animation/state");
            }
            else if (action == DoorTravelAction.Cross && phase == DoorTravelPhase.Opening)
            {
                openedState = state; phase = DoorTravelPhase.Crossing;
                deadline = Time.time + DoorTravelPolicy.MaxIntentSeconds;
                bestDistance = Single.PositiveInfinity; lastProgress = Time.time;
                ResetRoute();
                Log("Crossing", "native movement through open doorway");
            }
            else if (phase == DoorTravelPhase.Crossing && crossed)
            {
                if (purpose == DoorTravelPurpose.CombatPursuit)
                {
                    Cancel("Combat passage complete; opened door left available for party pursuit", false);
                    return;
                }
                phase = DoorTravelPhase.Closing; closeAt = Time.time + 0.8f; deadline = Time.time + DoorTravelPolicy.MaxIntentSeconds;
                ResetRoute();
                Log("Crossed", "holding exit clearance until doorway clears and close completes");
            }
            else if (action == DoorTravelAction.Close)
            {
                phase = DoorTravelPhase.ClosePending; deadline = Time.time + 3f;
                Open.Invoke(door, new object[] { transform.position - door.transform.position });
                Log("Close requested", "doorway clear; native close pending");
            }
        }
        bool CrossingClear()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                // The operator has already reached its committed exit point;
                // its own capsule must not veto its close forever. Corpses do
                // not need convoy clearance either. Living companions/players
                // still keep the door open until they are safely through.
                if (character == null || character.gameObject == gameObject || character.IsDead()) continue;
                Vector3 offset = character.transform.position - center;
                if (DoorTravelPolicy.OccupiesDoorway(Vector3.Dot(offset, lateral), Vector3.Dot(offset, normal), offset.y, character.GetRadius(), halfWidth))
                    return false;
            }
            return true;
        }
        void Cancel(string reason, bool backoff)
        {
            if (phase != DoorTravelPhase.None) ResetRoute();
            if (phase != DoorTravelPhase.None && reason != DoorTravelAction.Finish.ToString() && LivingWorld.ManualTest.ManualTestPlugin.Instance != null)
                LivingWorld.ManualTest.ManualTestPlugin.Instance.Report("DOOR Released: " + reason);
            if (!ReferenceEquals(door, null))
            {
                HumanDoorTraversal claimant;
                if (Claims.TryGetValue(door, out claimant) && ReferenceEquals(claimant, this)) Claims.Remove(door);
                if (backoff) { retiredDoor = door; retiredUntil = Time.time + 8f; }
            }
            door = null; doorView = null; phase = DoorTravelPhase.None; purpose = DoorTravelPurpose.None; openedHere = false; openedState = 0;
            status = reason;
        }
        void Fault(Exception error)
        {
            Cancel("Door assistance disabled: " + error.Message, false);
            disabledByFault = true;
            LivingWorld.ManualTest.ManualTestPlugin.Instance.Report("DOOR FAIL: " + error.Message + "; native companion behavior retained.");
        }
        void Log(string stage, string details)
        {
            status = stage + ": " + details;
            LivingWorld.ManualTest.ManualTestPlugin.Instance.Report("DOOR " + status);
        }
        void OnDisable() { Cancel("Door assistance disabled", false); }
        void OnDestroy() { Cancel("Door assistance destroyed", false); }
        static float Horizontal(Vector3 a, Vector3 b) { a.y = 0; b.y = 0; return Vector3.Distance(a, b); }

        static bool BeforeMove(BaseAI __instance, ref Vector3 __1, ref float __2, ref bool __3, ref bool __result, out bool __state)
        {
            __state = false;
            var travel = __instance.GetComponent<HumanDoorTraversal>();
            if (travel == null) return true;
            Vector3 original = __1; float stop = __2; bool run = __3;
            try
            {
                if (travel.Step(ref __1, ref __2, ref __3, out __state)) return true;
                __1 = original; __2 = stop; __3 = run;
                __instance.StopMoving(); __result = false; return false;
            }
            catch (Exception error)
            {
                travel.Fault(error); __1 = original; __2 = stop; __3 = run; __state = false;
                return true; // A door fault must not disable the tested companion/combat AI.
            }
        }
        static void AfterMove(bool __state, ref bool __result)
        {
            if (__state) __result = false; // Reaching an intermediate point is not completing the original order.
        }
    }
}

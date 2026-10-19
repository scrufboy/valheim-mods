using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // One follow executor. Native MoveTo still owns paths, steering, obstacles
    // and locomotion. Native CharacterAnimEvent owns head IK and replication.
    // This component never turns the body to face a leader during travel.
    public sealed class HumanFollowPresence : MonoBehaviour
    {
        const BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo Move = typeof(BaseAI).GetMethod("MoveTo", Native, null,
            new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }, null);
        static readonly FieldInfo LastFindPathTime = typeof(BaseAI).GetField("m_lastFindPathTime", Native);
        readonly object[] moveArguments = new object[4];
        HumanCompanion companion;
        HumanDoorTraversal doors;
        HumanCompanionPosture posture;
        HumanCompanionAmbient ambient;
        BaseAI ai;
        Character body;
        CharacterAnimEvent animationEvents;
        Player currentLeader;
        Transform assignedLook, previousLook;
        Vector3 leaderSample;
        float lastActivity, nextReport;
        float lastCatchUpRepath = Single.NegativeInfinity;
        float normalRunSpeed;
        float previousLookWeight, previousBodyWeight;
        bool ownLook, faulted, catchingUp, ownsCatchUpSpeed;
        CompanionPace pace;
        static readonly List<HumanFollowPresence> Leases = new List<HumanFollowPresence>();
        Player leaseLeader;
        int leaseSlot = -1;
        string status = "Presence awaiting active follow order";
        public string Status { get { return status; } }

        internal static void InstallPatches(Harmony harmony)
        {
            var follow = typeof(BaseAI).GetMethod("Follow", Native, null, new[] { typeof(GameObject), typeof(float) }, null);
            var head = typeof(CharacterAnimEvent).GetMethod("UpdateHeadRotation", Native, null, new[] { typeof(float) }, null);
            if (follow == null || head == null || Move == null || LastFindPathTime == null)
                throw new MissingMethodException("Native human follow/head-look contract changed.");
            harmony.Patch(follow, prefix: new HarmonyMethod(typeof(HumanFollowPresence).GetMethod("BeforeFollow", BindingFlags.Static | BindingFlags.NonPublic)));
            harmony.Patch(head, prefix: new HarmonyMethod(typeof(HumanFollowPresence).GetMethod("BeforeHeadLook", BindingFlags.Static | BindingFlags.NonPublic)));
        }

        void Cache()
        {
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (doors == null) doors = GetComponent<HumanDoorTraversal>();
            if (posture == null) posture = GetComponent<HumanCompanionPosture>();
            if (ambient == null) ambient = GetComponent<HumanCompanionAmbient>();
            if (ai == null) ai = GetComponent<BaseAI>();
            if (body == null) body = GetComponent<Character>();
        }

        Player Leader()
        {
            Cache();
            return !faulted && isActiveAndEnabled && companion != null ? companion.PresenceLeader : null;
        }

        void ObserveLeader(Player leader)
        {
            if (currentLeader != leader)
            {
                ReleaseLease();
                ReleaseLook();
                currentLeader = leader; pace = CompanionPace.Stand;
                lastActivity = Time.time;
                if (leader != null) leaderSample = leader.transform.position;
            }
            if (leader != null && Vector3.Distance(leaderSample, leader.transform.position) >= CompanionPresencePolicy.LeaderMovementThreshold)
            { leaderSample = leader.transform.position; lastActivity = Time.time; }
        }

        bool Follow(GameObject target, float dt)
        {
            Player leader = Leader();
            ObserveLeader(leader);
            if (leader == null || target != leader.gameObject)
            { ReleaseCatchUpSpeed(); if (ambient != null) ambient.Cancel("follow target/order lost"); return false; }
            if (posture != null) posture.Refresh();
            bool crossing = doors != null && doors.NeedsCloseFollow(target);
            float leaderDistance = Vector3.Distance(transform.position, leader.transform.position);
            catchingUp = CompanionPresencePolicy.CatchUp(catchingUp, leaderDistance);
            SetCatchUpSpeed(catchingUp);
            if (catchingUp && ambient != null) ambient.Cancel("leader catch-up owns movement");
            Vector3 destination = leader.transform.position; bool ambientHold = false;
            bool optional = !catchingUp && ambient != null && ambient.TryDestination(leader, crossing, out destination, out ambientHold);
            bool formation = !catchingUp && !optional && !crossing && FormationEligible();
            if (formation) destination = FormationDestination(leader);
            else ReleaseLease();
            float distance = Vector3.Distance(transform.position, destination);
            // During a directly observed doorway transit the leader's real
            // position is the order anchor. A personal-space detour here makes
            // the door adapter reject its own goal before it can open the door.
            Vector3 safeDestination = crossing ? destination : RespectLeaderExclusion(leader, destination);
            if (optional) { ReleaseLook(); pace = ambientHold ? CompanionPace.Stand : CompanionPace.Walk; }
            else if (formation) pace = CompanionPresencePolicy.NextFormationPace(pace, distance);
            else pace = CompanionPresencePolicy.NextPace(pace, distance, crossing);
            if (posture != null && posture.Posture == CompanionPosture.Crouch && pace == CompanionPace.Run)
                pace = CompanionPace.Walk;
            if (posture != null && posture.Posture == CompanionPosture.Sit) pace = CompanionPace.Stand;
            if (pace == CompanionPace.Stand)
            { ai.StopMoving(); if (optional && ambientHold) ambient.AfterStop(); }
            else
            {
                lastActivity = Time.time;
                // A committed ambient point is an optional input to this same
                // executor; ordinary orders retain the actual leader position.
                moveArguments[0] = dt; moveArguments[1] = safeDestination;
                moveArguments[2] = optional ? 0.2f : 0f; moveArguments[3] = pace == CompanionPace.Run;
                bool completed = (bool)Move.Invoke(ai, moveArguments);
                // MoveTo returns true for both arrival and path failure. At
                // catch-up distance this cannot be arrival, so invalidate the
                // cached failed route at a bounded rate and let native pathing
                // solve again on the next update. No teleport or direct motion.
                if (catchingUp && completed && Time.time - lastCatchUpRepath >= CompanionPresencePolicy.CatchUpRepathSeconds)
                {
                    LastFindPathTime.SetValue(ai, Single.NegativeInfinity);
                    ai.ResetRandomMovement();
                    lastCatchUpRepath = Time.time;
                }
            }
            Report(leaderDistance, crossing);
            return true;
        }

        void SetCatchUpSpeed(bool active)
        {
            if (body == null) return;
            if (!active) { ReleaseCatchUpSpeed(); return; }
            if (!ownsCatchUpSpeed)
            {
                normalRunSpeed = body.m_runSpeed;
                ownsCatchUpSpeed = true;
            }
            body.m_runSpeed = CompanionPresencePolicy.CatchUpRunSpeed(normalRunSpeed);
        }

        void ReleaseCatchUpSpeed()
        {
            if (ownsCatchUpSpeed && body != null) body.m_runSpeed = normalRunSpeed;
            ownsCatchUpSpeed = false;
        }

        internal void ReleaseForCombat()
        {
            catchingUp = false;
            ReleaseLease();
            ReleaseCatchUpSpeed();
        }

        Vector3 RespectLeaderExclusion(Player leader, Vector3 destination)
        {
            if (leader == null || !CompanionPresencePolicy.PathEntersLeaderExclusion(
                transform.position.x, transform.position.z, destination.x, destination.z,
                leader.transform.position.x, leader.transform.position.z)) return destination;
            Vector3 outward = transform.position - leader.transform.position; outward.y = 0f;
            if (outward.sqrMagnitude < .01f)
            {
                outward = leader.transform.forward * -1f; outward.y = 0f;
                if (outward.sqrMagnitude < .01f) outward = new Vector3(0f, 0f, -1f);
            }
            outward = outward.normalized;
            Vector3 tangent = new Vector3(-outward.z, 0f, outward.x);
            float side = leaseSlot == 0 ? -1f : leaseSlot == 1 ? 1f : 1f;
            Vector3 detour = leader.transform.position + outward *
                (CompanionPresencePolicy.LeaderExclusionRadius + CompanionPresencePolicy.LeaderExclusionMargin) +
                tangent * side * .9f;
            detour.y = destination.y;
            return detour;
        }

        Vector3 FormationDestination(Player leader)
        {
            if (leader == null) return transform.position;
            AcquireLease(leader);
            if (leaseSlot < 0) return leader.transform.position;
            float side = leaseSlot == 0 ? -CompanionPresencePolicy.FormationSide : CompanionPresencePolicy.FormationSide;
            Vector3 forward = leader.transform.forward; forward.y = 0f;
            Vector3 right = leader.transform.right; right.y = 0f;
            if (forward.sqrMagnitude < .01f) forward = new Vector3(0f, 0f, 1f);
            if (right.sqrMagnitude < .01f) right = new Vector3(1f, 0f, 0f);
            return leader.transform.position - forward.normalized * CompanionPresencePolicy.FormationRear +
                right.normalized * side;
        }

        bool FormationEligible()
        {
            string identity = gameObject.name ?? "";
            return identity.IndexOf("HumanScout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                identity.IndexOf("HumanCleric", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        void AcquireLease(Player leader)
        {
            if (leaseLeader == leader && leaseSlot >= 0) return;
            ReleaseLease();
            bool leftFree = true, rightFree = true;
            for (int i = Leases.Count - 1; i >= 0; --i)
            {
                var other = Leases[i];
                if (other == null || !other.isActiveAndEnabled || other.leaseLeader == null || other.leaseSlot < 0)
                { Leases.RemoveAt(i); continue; }
                if (other.leaseLeader != leader) continue;
                if (other.leaseSlot == 0) leftFree = false;
                if (other.leaseSlot == 1) rightFree = false;
            }
            Vector3 forward = leader.transform.forward; forward.y = 0f;
            Vector3 right = leader.transform.right; right.y = 0f;
            if (forward.sqrMagnitude < .01f) forward = new Vector3(0f, 0f, 1f);
            if (right.sqrMagnitude < .01f) right = new Vector3(1f, 0f, 0f);
            Vector3 rear = leader.transform.position - forward.normalized * CompanionPresencePolicy.FormationRear;
            float leftDistance = Vector3.Distance(transform.position, rear - right.normalized * CompanionPresencePolicy.FormationSide);
            float rightDistance = Vector3.Distance(transform.position, rear + right.normalized * CompanionPresencePolicy.FormationSide);
            if (leftFree && rightFree) leaseSlot = leftDistance <= rightDistance ? 0 : 1;
            else if (leftFree) leaseSlot = 0;
            else if (rightFree) leaseSlot = 1;
            else leaseSlot = -1;
            if (leaseSlot >= 0) { leaseLeader = leader; Leases.Add(this); }
        }

        void ReleaseLease()
        {
            leaseLeader = null; leaseSlot = -1; Leases.Remove(this);
        }

        void Attention(CharacterAnimEvent events)
        {
            Player leader = Leader();
            ObserveLeader(leader);
            Character awareness = companion == null ? null : companion.AwarenessTarget;
            bool eligible = leader != null && leader.m_eye != null;
            // Social gaze never competes with combat aim. The companion's one
            // combat-owner state controls this handoff; animation details do not.
            bool combat = companion != null && companion.CombatOwnsActor;
            if (combat) { ReleaseLook(); return; }
            if (awareness != null)
            {
                AssignLook(events, awareness.transform, 0.9f, 0.25f);
                return;
            }
            if (ambient != null && ambient.Active) { ReleaseLook(); return; }
            if (!CompanionPresencePolicy.AttendLeader(eligible, combat, pace != CompanionPace.Stand, Time.time - lastActivity))
            { ReleaseLook(); return; }
            AssignLook(events, leader.m_eye, 0.75f, 0f);
        }

        void AssignLook(CharacterAnimEvent events, Transform target, float lookWeight, float bodyWeight)
        {
            if (target == null) { ReleaseLook(); return; }
            if (animationEvents != events || (ownLook && assignedLook != target))
            { ReleaseLook(); animationEvents = events; }
            if (!ownLook)
            {
                // Do not steal another system's explicit focus. Native combat
                // is handled above and remains free to choose its own target.
                if (events.m_lookAt != null) return;
                previousLook = events.m_lookAt;
                previousLookWeight = events.m_lookWeight;
                previousBodyWeight = events.m_bodyLookWeight;
                assignedLook = target; ownLook = true;
                events.m_lookAt = assignedLook;
                events.m_lookWeight = lookWeight;
                events.m_bodyLookWeight = bodyWeight;
            }
            // UpdateHeadRotation now runs normally: smooth head turn, native
            // clamp, native attack suppression, and owner-authored ZDO target.
            // Player rig has no eye bones; head facing is the supported gaze.
        }

        void ReleaseLook()
        {
            if (ownLook && animationEvents != null)
            {
                if (animationEvents.m_lookAt == assignedLook) animationEvents.m_lookAt = previousLook;
                animationEvents.m_lookWeight = previousLookWeight;
                animationEvents.m_bodyLookWeight = previousBodyWeight;
            }
            ownLook = false; assignedLook = null; previousLook = null;
        }

        void Report(float distance, bool crossing)
        {
            if (Time.time < nextReport) return;
            nextReport = Time.time + 10f;
            status = String.Format(CultureInfo.InvariantCulture,
                "pace={0}; leaderDistance={1:F2}; catchUp={2}; doorOverride={3}; leaderLook={4}; quietSeconds={5:F1}; crouching={6}; encumbered={7}",
                pace, distance, catchingUp, crossing, ownLook, Time.time - lastActivity, body.IsCrouching(), body.IsEncumbered());
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("PRESENCE " + status);
        }

        void Fault(Exception error)
        {
            ReleaseLook(); ReleaseCatchUpSpeed(); if (ambient != null) ambient.Cancel("presence fault"); faulted = true;
            status = "Presence disabled: " + error.Message + "; native follow retained";
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("PRESENCE FAIL: " + status);
        }
        void OnDisable() { ReleaseLease(); ReleaseLook(); ReleaseCatchUpSpeed(); if (ambient != null) ambient.Cancel("presence disabled"); currentLeader = null; pace = CompanionPace.Stand; catchingUp = false; }
        void OnDestroy() { ReleaseLease(); ReleaseLook(); ReleaseCatchUpSpeed(); if (ambient != null) ambient.Cancel("presence destroyed"); }

        static bool BeforeFollow(BaseAI __instance, GameObject __0, float __1)
        {
            var presence = __instance.GetComponent<HumanFollowPresence>();
            if (presence == null) return true;
            try { return !presence.Follow(__0, __1); }
            catch (Exception error) { presence.Fault(error); return true; }
        }

        static void BeforeHeadLook(CharacterAnimEvent __instance)
        {
            var presence = __instance.GetComponentInParent<HumanFollowPresence>();
            if (presence == null) return;
            try { presence.Attention(__instance); }
            catch (Exception error) { presence.Fault(error); }
        }
    }
}

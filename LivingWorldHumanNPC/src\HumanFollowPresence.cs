using System;
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
        float previousLookWeight, previousBodyWeight;
        bool ownLook, faulted;
        CompanionPace pace;
        string status = "Presence awaiting active follow order";
        public string Status { get { return status; } }

        internal static void InstallPatches(Harmony harmony)
        {
            var follow = typeof(BaseAI).GetMethod("Follow", Native, null, new[] { typeof(GameObject), typeof(float) }, null);
            var head = typeof(CharacterAnimEvent).GetMethod("UpdateHeadRotation", Native, null, new[] { typeof(float) }, null);
            if (follow == null || head == null || Move == null)
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
            { if (ambient != null) ambient.Cancel("follow target/order lost"); return false; }
            if (posture != null) posture.Refresh();
            Vector3 destination = FormationDestination(leader);
            float distance = Vector3.Distance(transform.position, destination);
            bool crossing = doors != null && doors.NeedsCloseFollow(target);
            if (crossing) destination = leader.transform.position;
            bool ambientHold = false;
            bool optional = ambient != null && ambient.TryDestination(leader, crossing, out destination, out ambientHold);
            if (!optional && crossing) destination = leader.transform.position;
            if (optional) { ReleaseLook(); pace = ambientHold ? CompanionPace.Stand : CompanionPace.Walk; }
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
                moveArguments[0] = dt; moveArguments[1] = destination;
                moveArguments[2] = optional ? 0.2f : 0f; moveArguments[3] = pace == CompanionPace.Run;
                Move.Invoke(ai, moveArguments);
            }
            Report(distance, crossing);
            return true;
        }

        Vector3 FormationDestination(Player leader)
        {
            if (leader == null) return transform.position;
            int stable = 17;
            string identity = gameObject.name ?? "";
            for (int i = 0; i < identity.Length; ++i) stable = unchecked(stable * 31 + identity[i]);
            bool archer = identity.IndexOf("Scout", StringComparison.OrdinalIgnoreCase) >= 0;
            bool cleric = identity.IndexOf("Cleric", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!archer && !cleric) return leader.transform.position;
            float side = CompanionPresencePolicy.FormationSideOffset(archer, cleric, stable);
            Vector3 forward = leader.transform.forward; forward.y = 0f;
            Vector3 right = leader.transform.right; right.y = 0f;
            if (forward.sqrMagnitude < .01f) forward = new Vector3(0f, 0f, 1f);
            if (right.sqrMagnitude < .01f) right = new Vector3(1f, 0f, 0f);
            return leader.transform.position - forward.normalized * CompanionPresencePolicy.FormationRear +
                right.normalized * side;
        }

        void Attention(CharacterAnimEvent events)
        {
            Player leader = Leader();
            ObserveLeader(leader);
            Character awareness = companion == null ? null : companion.AwarenessTarget;
            bool eligible = leader != null && leader.m_eye != null;
            // Social gaze never competes with combat aim. Include native attack,
            // draw and alert phases so a brief target transition cannot twist
            // the head back toward the leader between bow releases.
            bool combat = companion != null && companion.CombatReadinessActive;
            combat |= body != null && (body.InAttack() || body.IsDrawingBow() || body.IsBlocking());
            if (ambient != null && ambient.Active) { ReleaseLook(); return; }
            if (!combat && awareness != null)
            {
                AssignLook(events, awareness.transform, 0.9f, 0.25f);
                return;
            }
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
                "pace={0}; leaderDistance={1:F2}; doorOverride={2}; leaderLook={3}; quietSeconds={4:F1}; crouching={5}; encumbered={6}",
                pace, distance, crossing, ownLook, Time.time - lastActivity, body.IsCrouching(), body.IsEncumbered());
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("PRESENCE " + status);
        }

        void Fault(Exception error)
        {
            ReleaseLook(); if (ambient != null) ambient.Cancel("presence fault"); faulted = true;
            status = "Presence disabled: " + error.Message + "; native follow retained";
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("PRESENCE FAIL: " + status);
        }
        void OnDisable() { ReleaseLook(); if (ambient != null) ambient.Cancel("presence disabled"); currentLeader = null; pace = CompanionPace.Stand; }
        void OnDestroy() { ReleaseLook(); if (ambient != null) ambient.Cancel("presence destroyed"); }

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


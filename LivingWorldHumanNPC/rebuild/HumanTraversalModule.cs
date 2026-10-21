using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    [DefaultExecutionOrder(1000)]
    public sealed class HumanTraversalModule : MonoBehaviour
    {
        static readonly MethodInfo MoveTo = typeof(BaseAI).GetMethod("MoveTo",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(float), typeof(Vector3), typeof(float), typeof(bool) }, null);

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        Player leader;
        GameObject anchor;
        HumanBehaviorState state;
        int slot = -1;
        float baseSpeed;
        float baseWalkSpeed;
        float baseRunSpeed;
        float nextFaultLog;

        public int FormationSlot { get { return slot; } }
        public string Status { get; private set; }

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            if (MoveTo == null) throw new MissingMethodException("Valheim's native BaseAI.MoveTo contract changed.");
            if (human != null)
            {
                baseSpeed = human.m_speed;
                baseWalkSpeed = human.m_walkSpeed;
                baseRunSpeed = human.m_runSpeed;
            }
            Status = "inactive";
        }

        internal void Configure(HumanBehaviorState nextState, Player nextLeader)
        {
            state = nextState;
            leader = nextLeader;
            if (!OwnsTraversal(nextState) || nextLeader == null)
            {
                ReleaseFormation();
                RestorePace();
                Status = nextState == HumanBehaviorState.NativeCombat || nextState == HumanBehaviorState.CombatClear ?
                    "released to native combat" : "inactive";
                return;
            }

            EnsureFormation(nextLeader);
        }

        internal float GetFormationDistance(Player nextLeader)
        {
            if (nextLeader == null) return Single.PositiveInfinity;
            if (slot < 0 || !FormationLeasesV2.IsClaimedBy(this, nextLeader, slot))
                return Vector3.Distance(transform.position, nextLeader.transform.position);
            return Vector3.Distance(transform.position, FormationLeasesV2.Position(nextLeader, slot));
        }

        void LateUpdate()
        {
            if (!OwnsTraversal(state) || leader == null || ai == null || human == null || view == null ||
                !view.IsValid() || !view.IsOwner()) return;

            try
            {
                // Threat ownership changes immediately. The controller records the
                // formal transition on its next bounded decision tick.
                if (ai.GetTargetCreature() != null || ai.IsAlerted())
                {
                    ai.SetFollowTarget(null);
                    RestorePace();
                    Status = "yielding immediately to native threat";
                    return;
                }

                EnsureFormation(leader);
                Vector3 formation = FormationLeasesV2.Position(leader, slot);
                Vector3 destination = RespectLeaderExclusion(formation);
                anchor.transform.position = destination;
                anchor.transform.rotation = leader.transform.rotation;
                if (ai.GetFollowTarget() != anchor) ai.SetFollowTarget(anchor);

                float distance = Vector3.Distance(transform.position, destination);
                bool run = state == HumanBehaviorState.TraversalSprint || state == HumanBehaviorState.CatchUp ||
                    (state == HumanBehaviorState.CombatReadiness && leader.IsRunning()) ||
                    (state == HumanBehaviorState.Regroup && distance > 5f);
                ApplyPace(run, state == HumanBehaviorState.CatchUp);
                MoveTo.Invoke(ai, new object[] { Time.deltaTime, destination, 1.15f, run });
                Status = "slot=" + slot + "; " + (run ? "run" : "trot") + "; distance=" + distance.ToString("0.0");
            }
            catch (Exception error)
            {
                Status = "instruction retained after traversal fault";
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    HumanNpcPlugin.WarnStatic(name + " traversal instruction retained: " + error.Message);
                }
            }
        }

        void EnsureFormation(Player nextLeader)
        {
            if (slot < 0 || !FormationLeasesV2.IsClaimedBy(this, nextLeader, slot))
                slot = FormationLeasesV2.Acquire(this, nextLeader);
            if (anchor == null)
            {
                anchor = new GameObject("LWC traversal anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
            anchor.transform.position = FormationLeasesV2.Position(nextLeader, slot);
            anchor.transform.rotation = nextLeader.transform.rotation;
            if (ai != null && ai.GetFollowTarget() != anchor) ai.SetFollowTarget(anchor);
        }

        Vector3 RespectLeaderExclusion(Vector3 formation)
        {
            Vector3 start = transform.position;
            Vector3 center = leader.transform.position;
            Vector2 segment = new Vector2(formation.x - start.x, formation.z - start.z);
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < .01f) return formation;
            Vector2 toCenter = new Vector2(center.x - start.x, center.z - start.z);
            float t = Mathf.Clamp01(Vector2.Dot(toCenter, segment) / lengthSquared);
            Vector2 closest = new Vector2(start.x, start.z) + segment * t;
            float clearance = Vector2.Distance(closest, new Vector2(center.x, center.z));
            if (t <= .05f || t >= .95f || clearance >= 1.55f) return formation;

            float side = FormationLeasesV2.Side(slot);
            Vector3 detour = center + leader.transform.right * side * 2.35f - leader.transform.forward * .35f;
            float ground;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(detour, out ground)) detour.y = ground + .1f;
            return detour;
        }

        void ApplyPace(bool run, bool catchUp)
        {
            Vector3 velocity = leader == null ? Vector3.zero : leader.GetVelocity();
            float leaderSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            human.m_walkSpeed = baseWalkSpeed;
            human.m_speed = run ? baseSpeed : Mathf.Max(baseSpeed, leaderSpeed + .15f);
            if (run)
            {
                float margin = catchUp ? 1.25f : .5f;
                float cap = baseRunSpeed * (catchUp ? 1.35f : 1.2f);
                human.m_runSpeed = Mathf.Min(cap, Mathf.Max(baseRunSpeed, leaderSpeed + margin));
            }
            else
            {
                human.m_runSpeed = baseRunSpeed;
            }
        }

        void RestorePace()
        {
            if (human == null) return;
            human.m_speed = baseSpeed;
            human.m_walkSpeed = baseWalkSpeed;
            human.m_runSpeed = baseRunSpeed;
        }

        void ReleaseFormation()
        {
            FormationLeasesV2.Release(this);
            slot = -1;
            if (ai != null && anchor != null && ai.GetFollowTarget() == anchor) ai.SetFollowTarget(null);
            if (anchor != null)
            {
                Destroy(anchor);
                anchor = null;
            }
        }

        static bool OwnsTraversal(HumanBehaviorState value)
        {
            return value == HumanBehaviorState.IdleGuard || value == HumanBehaviorState.Traversal ||
                value == HumanBehaviorState.TraversalSprint || value == HumanBehaviorState.CatchUp ||
                value == HumanBehaviorState.CombatReadiness || value == HumanBehaviorState.Regroup;
        }

        void OnDisable()
        {
            RestorePace();
            ReleaseFormation();
        }

        void OnDestroy()
        {
            RestorePace();
            ReleaseFormation();
        }
    }

    internal static class FormationLeasesV2
    {
        sealed class Lease
        {
            internal long LeaderId;
            internal int Slot;
        }

        static readonly Vector2[] LocalSlots = {
            new Vector2(-1.65f, -2.75f), new Vector2(1.65f, -2.75f),
            new Vector2(-2.55f, -1.05f), new Vector2(2.55f, -1.05f),
            new Vector2(-3.15f, -3.45f), new Vector2(3.15f, -3.45f)
        };
        static readonly Dictionary<HumanTraversalModule, Lease> Claims = new Dictionary<HumanTraversalModule, Lease>();

        internal static int Acquire(HumanTraversalModule actor, Player leader)
        {
            Cleanup();
            long leaderId = leader.GetPlayerID();
            Lease existing;
            if (Claims.TryGetValue(actor, out existing) && existing.LeaderId == leaderId) return existing.Slot;
            Claims.Remove(actor);

            int chosen = -1;
            float best = Single.PositiveInfinity;
            for (int slot = 0; slot < LocalSlots.Length; slot++)
            {
                if (Occupied(leaderId, slot)) continue;
                float distance = Vector3.Distance(actor.transform.position, Position(leader, slot));
                if (distance < best) { best = distance; chosen = slot; }
            }
            if (chosen < 0) chosen = Claims.Count % LocalSlots.Length;
            Claims[actor] = new Lease { LeaderId = leaderId, Slot = chosen };
            return chosen;
        }

        internal static bool IsClaimedBy(HumanTraversalModule actor, Player leader, int slot)
        {
            Lease lease;
            return actor != null && leader != null && Claims.TryGetValue(actor, out lease) &&
                lease.LeaderId == leader.GetPlayerID() && lease.Slot == slot;
        }

        internal static Vector3 Position(Player leader, int slot)
        {
            int safe = Mathf.Clamp(slot, 0, LocalSlots.Length - 1);
            Vector2 local = LocalSlots[safe];
            return leader.transform.position + leader.transform.right * local.x + leader.transform.forward * local.y;
        }

        internal static float Side(int slot)
        {
            int safe = Mathf.Clamp(slot, 0, LocalSlots.Length - 1);
            return LocalSlots[safe].x < 0f ? -1f : 1f;
        }

        internal static void Release(HumanTraversalModule actor)
        {
            if (actor != null) Claims.Remove(actor);
        }

        static bool Occupied(long leaderId, int slot)
        {
            foreach (Lease lease in Claims.Values)
                if (lease.LeaderId == leaderId && lease.Slot == slot) return true;
            return false;
        }

        static void Cleanup()
        {
            var stale = new List<HumanTraversalModule>();
            foreach (HumanTraversalModule actor in Claims.Keys) if (actor == null) stale.Add(actor);
            foreach (HumanTraversalModule actor in stale) Claims.Remove(actor);
        }
    }
}

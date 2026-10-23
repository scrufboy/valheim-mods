using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    [DefaultExecutionOrder(1000)]
    public sealed class HumanTraversalModule : MonoBehaviour
    {
        const float HeadingMovementThreshold = .75f;
        const float FormationTurnDegreesPerSecond = 120f;
        const float NativeFollowArrivalDistance = 3f;
        const float MovingAnchorLead = 3.25f;

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        Player leader;
        GameObject anchor;
        HumanBehaviorState state;
        Vector3 formationForward;
        bool formationFrameReady;
        int slot = -1;
        float baseSpeed;
        float baseWalkSpeed;
        float baseRunSpeed;
        float nextFaultLog;
        HumanSettledGuardModule settledOwner;
        Vector3 settledDestination;
        Vector3 settledFollowAnchor;
        bool settledHold;

        public int FormationSlot { get { return slot; } }
        public string Status { get; private set; }
        public float FormationHeading
        {
            get { return formationFrameReady ? Mathf.Atan2(formationForward.x, formationForward.z) * Mathf.Rad2Deg : 0f; }
        }
        public float FormationTargetDistance
        {
            get
            {
                if (settledOwner != null && state == HumanBehaviorState.IdleGuard)
                    return Vector3.Distance(transform.position, settledDestination);
                return leader == null || slot < 0 ? -1f :
                    Vector3.Distance(transform.position, FormationLeasesV2.Position(leader, slot, formationForward));
            }
        }

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
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

            if (nextState == HumanBehaviorState.NativeCombat || nextState == HumanBehaviorState.CombatClear)
            {
                ClearSettledDestination(null);
                ReleaseFormation();
                RestorePace();
                if (ai != null) ai.SetFollowTarget(nextLeader == null ? null : nextLeader.gameObject);
                Status = nextLeader == null ? "native combat; leader unavailable" : "native combat; native leader tether";
                return;
            }

            if (!OwnsTraversal(nextState) || nextLeader == null)
            {
                ClearSettledDestination(null);
                ReleaseFormation();
                RestorePace();
                if (ai != null) ai.SetFollowTarget(null);
                Status = "inactive";
                return;
            }

            if (nextState != HumanBehaviorState.IdleGuard) ClearSettledDestination(null);

            EnsureFormationFrame(nextLeader);
            EnsureFormation(nextLeader);
        }

        internal float GetFormationDistance(Player nextLeader)
        {
            if (nextLeader == null) return Single.PositiveInfinity;
            EnsureFormationFrame(nextLeader);
            if (slot < 0 || !FormationLeasesV2.IsClaimedBy(this, nextLeader, slot))
                return Vector3.Distance(transform.position, nextLeader.transform.position);
            return Vector3.Distance(transform.position, FormationLeasesV2.Position(nextLeader, slot, formationForward));
        }

        void LateUpdate()
        {
            if (!OwnsTraversal(state) || leader == null || ai == null || human == null || view == null ||
                !view.IsValid() || !view.IsOwner()) return;

            try
            {
                // A real creature target outranks traversal immediately. Keeping
                // the native leader follow target does not drive locomotion while
                // MonsterAI has a target, but it gives native AI a party tether
                // and a deterministic return instead of unbounded idle wandering.
                if (ai.GetTargetCreature() != null)
                {
                    ReleaseFormation();
                    ai.SetFollowTarget(leader.gameObject);
                    RestorePace();
                    Status = "yielding immediately to native target; leader tether retained";
                    return;
                }

                UpdateFormationFrame(leader);
                EnsureFormation(leader);
                bool settled = settledOwner != null && state == HumanBehaviorState.IdleGuard;
                Vector3 destination = settled ? settledDestination :
                    FormationLeasesV2.Position(leader, slot, formationForward);
                Vector3 followAnchor = settled ? settledFollowAnchor : FollowAnchorPosition(leader, destination);
                anchor.transform.position = followAnchor;
                anchor.transform.rotation = Quaternion.LookRotation(formationForward, Vector3.up);
                if (ai.GetFollowTarget() != anchor) ai.SetFollowTarget(anchor);

                float leaseDistance = Vector3.Distance(transform.position, destination);
                float anchorDistance = Vector3.Distance(transform.position, followAnchor);
                float leaderDistance = Vector3.Distance(transform.position, leader.transform.position);
                bool run = !settled && (state == HumanBehaviorState.TraversalSprint || state == HumanBehaviorState.CatchUp ||
                    ((state == HumanBehaviorState.CombatReadiness || state == HumanBehaviorState.CombatRegroup ||
                        state == HumanBehaviorState.Regroup) &&
                        (leader.IsRunning() || leaderDistance > 7f)));
                ApplyPace(run, state == HumanBehaviorState.CatchUp || leaderDistance > 12f);

                // Native Follow remains the only movement/path writer. This only
                // selects its pace after it has supplied the movement direction.
                human.SetRun(run && anchorDistance > NativeFollowArrivalDistance + .15f);
                Status = (settled ? "settled guard" + (settledHold ? " scan" : " approach") : "slot=" + slot) +
                    "; native " + (run ? "run" : "trot") +
                    "; leaseDistance=" + leaseDistance.ToString("0.0") +
                    "; anchorDistance=" + anchorDistance.ToString("0.0") +
                    "; heading=" + FormationHeading.ToString("0");
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

        void EnsureFormationFrame(Player nextLeader)
        {
            if (formationFrameReady) return;
            Vector3 velocity = nextLeader.GetVelocity();
            velocity.y = 0f;
            Vector3 initial = velocity.magnitude >= HeadingMovementThreshold ? velocity.normalized : nextLeader.transform.forward;
            initial.y = 0f;
            formationForward = initial.sqrMagnitude < .01f ? Vector3.forward : initial.normalized;
            formationFrameReady = true;
        }

        void UpdateFormationFrame(Player nextLeader)
        {
            EnsureFormationFrame(nextLeader);
            Vector3 velocity = nextLeader.GetVelocity();
            velocity.y = 0f;
            if (velocity.magnitude < HeadingMovementThreshold) return;
            Vector3 desired = velocity.normalized;
            formationForward = Vector3.RotateTowards(formationForward, desired,
                FormationTurnDegreesPerSecond * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
        }

        void EnsureFormation(Player nextLeader)
        {
            if (slot < 0 || !FormationLeasesV2.IsClaimedBy(this, nextLeader, slot))
                slot = FormationLeasesV2.Acquire(this, nextLeader, formationForward);
            if (anchor == null)
            {
                anchor = new GameObject("LWC traversal anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
            Vector3 destination = FormationLeasesV2.Position(nextLeader, slot, formationForward);
            anchor.transform.position = settledOwner != null && state == HumanBehaviorState.IdleGuard ?
                settledFollowAnchor : FollowAnchorPosition(nextLeader, destination);
            anchor.transform.rotation = Quaternion.LookRotation(formationForward, Vector3.up);
            if (ai != null && ai.GetFollowTarget() != anchor) ai.SetFollowTarget(anchor);
        }

        Vector3 FollowAnchorPosition(Player nextLeader, Vector3 destination)
        {
            Vector3 velocity = nextLeader.GetVelocity();
            velocity.y = 0f;
            if (velocity.magnitude < HeadingMovementThreshold) return destination;

            // MonsterAI.Follow deliberately stops inside a three-metre radius.
            // Lead the invisible destination by that native radius while the
            // leader is travelling, so the actor's native arrival point is the
            // leased formation position instead of three metres behind it.
            return destination + formationForward * MovingAnchorLead;
        }

        internal bool SetSettledDestination(HumanSettledGuardModule owner, Vector3 destination, Vector3 followAnchor)
        {
            if (owner == null || state != HumanBehaviorState.IdleGuard || leader == null ||
                (settledOwner != null && settledOwner != owner)) return false;
            settledOwner = owner;
            settledDestination = destination;
            settledFollowAnchor = followAnchor;
            settledHold = false;
            return true;
        }

        internal void SetSettledHold(HumanSettledGuardModule owner, bool hold)
        {
            if (owner != null && settledOwner == owner) settledHold = hold;
        }

        internal void ClearSettledDestination(HumanSettledGuardModule owner)
        {
            if (owner != null && settledOwner != owner) return;
            settledOwner = null;
            settledHold = false;
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
                value == HumanBehaviorState.CombatReadiness || value == HumanBehaviorState.CombatRegroup ||
                value == HumanBehaviorState.Regroup;
        }

        void OnDisable()
        {
            RestorePace();
            ReleaseFormation();
            if (ai != null) ai.SetFollowTarget(null);
        }

        void OnDestroy()
        {
            RestorePace();
            ReleaseFormation();
            if (ai != null) ai.SetFollowTarget(null);
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

        internal static int Acquire(HumanTraversalModule actor, Player leader, Vector3 forward)
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
                float distance = Vector3.Distance(actor.transform.position, Position(leader, slot, forward));
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

        internal static Vector3 Position(Player leader, int slot, Vector3 forward)
        {
            int safe = Mathf.Clamp(slot, 0, LocalSlots.Length - 1);
            Vector2 local = LocalSlots[safe];
            Vector3 flatForward = forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < .01f) flatForward = leader.transform.forward;
            flatForward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, flatForward).normalized;
            return leader.transform.position + right * local.x + flatForward * local.y;
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

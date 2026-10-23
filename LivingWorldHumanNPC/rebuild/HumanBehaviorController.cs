using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    public sealed class HumanBehaviorController : MonoBehaviour
    {
        const float DecisionInterval = .1f;
        const int CombatClearScans = 50;
        const int CohesionOutsideScans = 10;
        const float CohesionOuterLeaderDistance = 16f;
        const float CohesionOuterPartyDistance = 18f;
        const float CohesionPairLeaderFloor = 8f;
        const float CohesionInnerLeaderDistance = 8f;
        const float CohesionInnerPartyDistance = 12f;
        static readonly HashSet<HumanBehaviorController> ActiveControllers =
            new HashSet<HumanBehaviorController>();
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo VisibleLeft = typeof(Humanoid).GetField("m_leftItem", Native);
        static readonly FieldInfo VisibleRight = typeof(Humanoid).GetField("m_rightItem", Native);
        static readonly FieldInfo NativeCreatureTarget = typeof(MonsterAI).GetField("m_targetCreature", Native);
        static readonly FieldInfo NativeStaticTarget = typeof(MonsterAI).GetField("m_targetStatic", Native);
        static readonly FieldInfo NativeAttackTimer = typeof(MonsterAI).GetField("m_timeSinceAttacking", Native);
        static readonly FieldInfo NativeTargetTimer = typeof(MonsterAI).GetField("m_updateTargetTimer", Native);
        static readonly MethodInfo SetNativeAlert = typeof(MonsterAI).GetMethod("SetAlerted", Native, null,
            new[] { typeof(bool) }, null);

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        HumanAppearanceV2 appearance;
        HumanCompanionV2 companion;
        HumanTraversalModule traversal;
        HumanEquipmentModule equipment;
        HumanSettledGuardModule settledGuard;
        HumanBehaviorStateMachine machine;
        CombatCohesionLatch cohesion;
        Character preparedTarget;
        float nextDecision;
        float nextFaultLog;
        int consecutiveFaults;

        public HumanBehaviorState State { get { return machine == null ? HumanBehaviorState.AwaitingAppearance : machine.State; } }
        public string Status { get; private set; }
        public int ClearScans { get { return machine == null ? 0 : machine.ClearScans; } }
        public int RequiredClearScans { get { return machine == null ? CombatClearScans : machine.RequiredClearScans; } }
        public string TraversalStatus { get { return traversal == null ? "missing" : traversal.Status; } }
        public string EquipmentStatus { get { return equipment == null ? "missing" : equipment.Status; } }
        public int FormationSlot { get { return traversal == null ? -1 : traversal.FormationSlot; } }
        public bool NativeAlert { get; private set; }
        public bool CommittedThreat { get; private set; }
        public bool LeaderArmed { get; private set; }
        public float LeaderDistance { get; private set; }
        public float LeaderSpeed { get; private set; }
        public float FormationHeading { get { return traversal == null ? 0f : traversal.FormationHeading; } }
        public float FormationTargetDistance { get { return traversal == null ? -1f : traversal.FormationTargetDistance; } }
        public int CombatBoundaryReleases { get; private set; }
        public int CombatBoundarySuppressions { get; private set; }
        public bool CombatRegroupActive { get { return cohesion != null && cohesion.Returning; } }
        public int CombatBoundaryOutsideScans { get { return cohesion == null ? 0 : cohesion.OutsideScans; } }
        public float NearestCompanionDistance { get; private set; }
        public string SettledGuardStatus { get { return settledGuard == null ? "missing" : settledGuard.Status; } }
        public int GuardSector { get { return settledGuard == null ? -1 : settledGuard.Sector; } }
        public string GuardPhase { get { return settledGuard == null ? "missing" : settledGuard.Phase; } }

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            appearance = GetComponent<HumanAppearanceV2>();
            companion = GetComponent<HumanCompanionV2>();
            traversal = GetComponent<HumanTraversalModule>();
            equipment = GetComponent<HumanEquipmentModule>();
            settledGuard = GetComponent<HumanSettledGuardModule>();
            if (VisibleLeft == null || VisibleRight == null || NativeCreatureTarget == null ||
                NativeStaticTarget == null || NativeAttackTimer == null || NativeTargetTimer == null ||
                SetNativeAlert == null)
                throw new MissingMemberException("Valheim's native hand or encounter-release boundary changed.");
            machine = new HumanBehaviorStateMachine(CombatClearScans);
            cohesion = new CombatCohesionLatch(CohesionOutsideScans, CohesionOuterLeaderDistance,
                CohesionOuterPartyDistance, CohesionPairLeaderFloor, CohesionInnerLeaderDistance,
                CohesionInnerPartyDistance);
            Status = "AwaitingAppearance";
        }

        void OnEnable()
        {
            ActiveControllers.Add(this);
        }

        void OnDisable()
        {
            ActiveControllers.Remove(this);
        }

        void OnDestroy()
        {
            ActiveControllers.Remove(this);
        }

        void Update()
        {
            if (Time.time < nextDecision) return;
            nextDecision = Time.time + DecisionInterval;

            try
            {
                Decide();
                consecutiveFaults = 0;
            }
            catch (Exception error)
            {
                machine.Retain();
                consecutiveFaults++;
                Status = machine.State + "; exact state retained after input fault " + consecutiveFaults;
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    HumanNpcPlugin.WarnStatic(name + " behavior input failed; retaining " + machine.State + ": " + error.Message);
                }
            }
        }

        void Decide()
        {
            bool ownerReady = view != null && view.IsValid() && view.IsOwner() && ai != null && human != null &&
                companion != null && traversal != null && equipment != null && settledGuard != null;
            if (!ownerReady)
            {
                machine.Step(new HumanBehaviorSignal { InputValid = false });
                Status = machine.State + "; native actor unavailable; state retained";
                return;
            }

            Player leader;
            bool hasLeader = companion.TryResolveLeader(out leader);
            Character target = ai.GetTargetCreature();
            Vector3 velocity = hasLeader ? leader.GetVelocity() : Vector3.zero;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float leaderDistance = hasLeader ? Vector3.Distance(transform.position, leader.transform.position) : -1f;
            float formationDistance = hasLeader ? traversal.GetFormationDistance(leader) : -1f;
            float nearestCompanion = hasLeader ? FindNearestCompanionDistance(leader) : -1f;
            NearestCompanionDistance = nearestCompanion;
            bool combatRegroup = hasLeader ? cohesion.Observe(target != null, leaderDistance, nearestCompanion) :
                cohesion.Retain();
            if (combatRegroup && target != null)
            {
                // A sustained actor separation starts one latched return. The
                // enemy's range from the leader is deliberately irrelevant:
                // an archer may shoot a distant target while remaining with the
                // party. During the latch, native reacquisition is suppressed
                // until this actor reaches the inner cohesion area.
                ReleaseNativeEncounter(ai);
                if (cohesion.JustEntered) CombatBoundaryReleases++;
                else CombatBoundarySuppressions++;
                target = null;
                preparedTarget = null;
            }
            bool committedThreat = target != null;
            bool nativeAlert = ai.IsAlerted();
            bool leaderArmed = hasLeader &&
                (IsVisibleWeapon(VisibleRight.GetValue(leader) as ItemDrop.ItemData) ||
                    IsVisibleWeapon(VisibleLeft.GetValue(leader) as ItemDrop.ItemData));
            NativeAlert = nativeAlert;
            CommittedThreat = committedThreat;
            LeaderArmed = leaderArmed;
            LeaderDistance = leaderDistance;
            LeaderSpeed = planarSpeed;

            HumanBehaviorState state = machine.Step(new HumanBehaviorSignal
            {
                InputValid = true,
                AppearanceReady = appearance != null && appearance.Ready,
                LeaderAvailable = hasLeader,
                LeaderMoving = planarSpeed > .35f,
                LeaderSprinting = hasLeader && leader.IsRunning() && planarSpeed > .35f,
                LeaderArmed = leaderArmed,
                NativeAlert = nativeAlert,
                CommittedThreat = committedThreat,
                CombatRegroup = combatRegroup,
                LeaderDistance = leaderDistance,
                FormationDistance = formationDistance
            });

            if (state == HumanBehaviorState.NativeCombat && target != null && preparedTarget != target)
            {
                // Run the game's own distance/attack-data weapon selector once
                // at target handoff. Living World does not select a bow, sword,
                // shield, attack, aim, or facing; subsequent choices remain in
                // MonsterAI's native update loop.
                human.EquipBestWeapon(target, null, null, null);
                preparedTarget = target;
            }
            else if (state != HumanBehaviorState.NativeCombat && state != HumanBehaviorState.CombatClear)
                preparedTarget = null;

            traversal.Configure(state, leader);
            equipment.Configure(state);
            settledGuard.Configure(state, leader);

            string detail = state.ToString();
            if (state == HumanBehaviorState.NativeCombat)
                detail += target == null ? "; alerted" : "; target=" + target.GetHoverName();
            else if (state == HumanBehaviorState.CombatClear)
                detail += "; clear=" + machine.ClearScans + "/" + machine.RequiredClearScans;
            else if (state == HumanBehaviorState.CombatRegroup)
                detail += "; cohesion return; leaderDistance=" + leaderDistance.ToString("0.0") +
                    "; partyDistance=" + nearestCompanion.ToString("0.0");
            else if (state == HumanBehaviorState.CombatReadiness)
                detail += "; reason=" + (nativeAlert ? "native alert" : "leader armed") +
                    "; leaderDistance=" + leaderDistance.ToString("0.0") + "; slot=" + traversal.FormationSlot;
            else if (hasLeader)
                detail += "; leaderDistance=" + leaderDistance.ToString("0.0") + "; slot=" + traversal.FormationSlot;
            Status = detail;
        }

        float FindNearestCompanionDistance(Player leader)
        {
            float nearest = Single.PositiveInfinity;
            long leaderId = leader.GetPlayerID();
            foreach (HumanBehaviorController other in ActiveControllers)
            {
                if (other == null || other == this || !other.isActiveAndEnabled || other.companion == null) continue;
                Player otherLeader;
                if (!other.companion.TryResolveLeader(out otherLeader) || otherLeader == null ||
                    otherLeader.GetPlayerID() != leaderId) continue;
                nearest = Mathf.Min(nearest, Vector3.Distance(transform.position, other.transform.position));
            }
            return Single.IsPositiveInfinity(nearest) ? -1f : nearest;
        }

        static void ReleaseNativeEncounter(MonsterAI nativeAi)
        {
            // MonsterAI.SetTarget(null) is deliberately a no-op in Valheim.
            // Use the complete transaction from MonsterAI.UpdateTarget's own
            // abandon branch so an out-of-bound target cannot survive and be
            // cleared again every controller tick.
            SetNativeAlert.Invoke(nativeAi, new object[] { false });
            NativeCreatureTarget.SetValue(nativeAi, null);
            NativeStaticTarget.SetValue(nativeAi, null);
            NativeAttackTimer.SetValue(nativeAi, 0f);
            NativeTargetTimer.SetValue(nativeAi, 5f);
        }

        static bool IsVisibleWeapon(ItemDrop.ItemData item)
        {
            // GetCurrentWeapon falls back to Valheim's invisible unarmed/fists
            // attack when both hands are stowed. Readiness is presentation
            // evidence, so only a real item currently visible in a hand counts.
            return item != null && item.m_dropPrefab != null && item.IsWeapon() &&
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch;
        }
    }
}

using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    public sealed class HumanBehaviorController : MonoBehaviour
    {
        const float DecisionInterval = .1f;
        const int CombatClearScans = 50;
        const float CombatLeaderRadius = 20f;
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo VisibleLeft = typeof(Humanoid).GetField("m_leftItem", Native);
        static readonly FieldInfo VisibleRight = typeof(Humanoid).GetField("m_rightItem", Native);
        static readonly MethodInfo ClearNativeTarget = typeof(MonsterAI).GetMethod("SetTarget", Native, null,
            new[] { typeof(Character) }, null);

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        HumanAppearanceV2 appearance;
        HumanCompanionV2 companion;
        HumanTraversalModule traversal;
        HumanEquipmentModule equipment;
        HumanBehaviorStateMachine machine;
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

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            appearance = GetComponent<HumanAppearanceV2>();
            companion = GetComponent<HumanCompanionV2>();
            traversal = GetComponent<HumanTraversalModule>();
            equipment = GetComponent<HumanEquipmentModule>();
            if (VisibleLeft == null || VisibleRight == null || ClearNativeTarget == null)
                throw new MissingMemberException("Valheim's native hand or combat-target boundary changed.");
            machine = new HumanBehaviorStateMachine(CombatClearScans);
            Status = "AwaitingAppearance";
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
                companion != null && traversal != null && equipment != null;
            if (!ownerReady)
            {
                machine.Step(new HumanBehaviorSignal { InputValid = false });
                Status = machine.State + "; native actor unavailable; state retained";
                return;
            }

            Player leader;
            bool hasLeader = companion.TryResolveLeader(out leader);
            Character target = ai.GetTargetCreature();
            if (target != null && hasLeader &&
                Vector3.Distance(target.transform.position, leader.transform.position) > CombatLeaderRadius)
            {
                // This is the sole outer party boundary. It does not choose a
                // target, steer, face, attack, or replace MonsterAI combat.
                // The fixed native spawn-point chase limit cannot follow a
                // travelling party, so retire only a target that has left the
                // leader-centred combat envelope.
                ClearNativeTarget.Invoke(ai, new object[] { null });
                CombatBoundaryReleases++;
                target = null;
            }
            bool committedThreat = target != null;
            bool nativeAlert = ai.IsAlerted();
            Vector3 velocity = hasLeader ? leader.GetVelocity() : Vector3.zero;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float leaderDistance = hasLeader ? Vector3.Distance(transform.position, leader.transform.position) : -1f;
            float formationDistance = hasLeader ? traversal.GetFormationDistance(leader) : -1f;
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
                LeaderDistance = leaderDistance,
                FormationDistance = formationDistance
            });

            traversal.Configure(state, leader);
            equipment.Configure(state);

            string detail = state.ToString();
            if (state == HumanBehaviorState.NativeCombat)
                detail += target == null ? "; alerted" : "; target=" + target.GetHoverName();
            else if (state == HumanBehaviorState.CombatClear)
                detail += "; clear=" + machine.ClearScans + "/" + machine.RequiredClearScans;
            else if (state == HumanBehaviorState.CombatReadiness)
                detail += "; reason=" + (nativeAlert ? "native alert" : "leader armed") +
                    "; leaderDistance=" + leaderDistance.ToString("0.0") + "; slot=" + traversal.FormationSlot;
            else if (hasLeader)
                detail += "; leaderDistance=" + leaderDistance.ToString("0.0") + "; slot=" + traversal.FormationSlot;
            Status = detail;
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

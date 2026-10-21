using System;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    public sealed class HumanBehaviorController : MonoBehaviour
    {
        const float DecisionInterval = .1f;
        const int CombatClearScans = 50;

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

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            appearance = GetComponent<HumanAppearanceV2>();
            companion = GetComponent<HumanCompanionV2>();
            traversal = GetComponent<HumanTraversalModule>();
            equipment = GetComponent<HumanEquipmentModule>();
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
            bool committedThreat = target != null || ai.IsAlerted();
            Vector3 velocity = hasLeader ? leader.GetVelocity() : Vector3.zero;
            float planarSpeed = new Vector2(velocity.x, velocity.z).magnitude;
            float leaderDistance = hasLeader ? Vector3.Distance(transform.position, leader.transform.position) : -1f;
            float formationDistance = hasLeader ? traversal.GetFormationDistance(leader) : -1f;
            ItemDrop.ItemData leaderWeapon = hasLeader ? leader.GetCurrentWeapon() : null;
            bool leaderArmed = leaderWeapon != null && leaderWeapon.IsWeapon() &&
                leaderWeapon.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Torch;

            HumanBehaviorState state = machine.Step(new HumanBehaviorSignal
            {
                InputValid = true,
                AppearanceReady = appearance != null && appearance.Ready,
                LeaderAvailable = hasLeader,
                LeaderMoving = planarSpeed > .35f,
                LeaderSprinting = hasLeader && leader.IsRunning() && planarSpeed > .35f,
                LeaderArmed = leaderArmed,
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
            else if (hasLeader)
                detail += "; leaderDistance=" + leaderDistance.ToString("0.0") + "; slot=" + traversal.FormationSlot;
            Status = detail;
        }
    }
}

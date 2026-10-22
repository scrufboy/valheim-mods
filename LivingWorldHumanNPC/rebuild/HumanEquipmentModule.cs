using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    [DefaultExecutionOrder(1100)]
    public sealed class HumanEquipmentModule : MonoBehaviour
    {
        enum EquipmentPosture { Neutral, Stowed, Drawn }

        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Left = typeof(Humanoid).GetField("m_leftItem", Native);
        static readonly FieldInfo Right = typeof(Humanoid).GetField("m_rightItem", Native);
        static readonly FieldInfo HiddenLeft = typeof(Humanoid).GetField("m_hiddenLeftItem", Native);
        static readonly FieldInfo HiddenRight = typeof(Humanoid).GetField("m_hiddenRightItem", Native);
        static readonly MethodInfo ShowHands = typeof(Humanoid).GetMethod("ShowHandItems", Native, null,
            new[] { typeof(bool), typeof(bool) }, null);

        Humanoid human;
        MonsterAI ai;
        VisEquipment visuals;
        EquipmentPosture desired;
        float nextFaultLog;

        public string Status { get; private set; }

        void Awake()
        {
            human = GetComponent<Humanoid>();
            ai = GetComponent<MonsterAI>();
            visuals = GetComponent<VisEquipment>();
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null || ShowHands == null)
                throw new MissingMemberException("Valheim's native hand-item stow contract changed.");
            if (visuals == null) throw new MissingComponentException("The native human has no VisEquipment adapter.");
            Status = "neutral";
        }

        internal void Configure(HumanBehaviorState state)
        {
            if (state == HumanBehaviorState.NativeCombat || state == HumanBehaviorState.CombatClear ||
                state == HumanBehaviorState.CombatReadiness)
                desired = EquipmentPosture.Drawn;
            else if (state == HumanBehaviorState.IdleGuard || state == HumanBehaviorState.Traversal ||
                state == HumanBehaviorState.TraversalSprint || state == HumanBehaviorState.CatchUp ||
                state == HumanBehaviorState.Regroup)
                desired = EquipmentPosture.Stowed;
            else
                desired = EquipmentPosture.Neutral;
        }

        void LateUpdate()
        {
            if (human == null || desired == EquipmentPosture.Neutral) return;
            try
            {
                // A native target can be acquired between bounded controller
                // decisions. Never let the previous calm posture stow a weapon
                // after native combat has already claimed the actor.
                EquipmentPosture effective = ai != null && (ai.GetTargetCreature() != null || ai.IsAlerted()) ?
                    EquipmentPosture.Drawn : desired;
                ItemDrop.ItemData left = Left.GetValue(human) as ItemDrop.ItemData;
                ItemDrop.ItemData right = Right.GetValue(human) as ItemDrop.ItemData;
                ItemDrop.ItemData hiddenLeft = HiddenLeft.GetValue(human) as ItemDrop.ItemData;
                ItemDrop.ItemData hiddenRight = HiddenRight.GetValue(human) as ItemDrop.ItemData;

                if (effective == EquipmentPosture.Stowed)
                {
                    if (left != null || right != null)
                    {
                        human.HideHandItems(false, true);
                        hiddenLeft = HiddenLeft.GetValue(human) as ItemDrop.ItemData;
                        hiddenRight = HiddenRight.GetValue(human) as ItemDrop.ItemData;
                    }
                    SyncBackVisuals(hiddenLeft, hiddenRight);
                    Status = "stowed on back=" + ItemName(hiddenLeft) + "/" + ItemName(hiddenRight);
                }
                else
                {
                    if (left == null && right == null && (hiddenLeft != null || hiddenRight != null))
                        ShowHands.Invoke(human, new object[] { false, true });
                    SyncBackVisuals(null, null);
                    Status = "drawn/native selection";
                }
            }
            catch (Exception error)
            {
                Status = "posture retained after equipment fault";
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    HumanNpcPlugin.WarnStatic(name + " equipment posture retained: " + error.Message);
                }
            }
        }

        void SyncBackVisuals(ItemDrop.ItemData left, ItemDrop.ItemData right)
        {
            // Humanoid.SetupVisEquipment intentionally updates hidden/back
            // slots only when Character.IsPlayer() is true. These actors are
            // native Player clones with the Player component removed, so the
            // native hide/show transaction preserves the correct item data and
            // animation but requires this presentation-only synchronization.
            visuals.SetLeftBackItem(ItemHash(left), left == null ? 0 : left.m_variant,
                left == null ? 0 : left.m_quality);
            visuals.SetRightBackItem(ItemHash(right), right == null ? 0 : right.m_quality);
        }

        static int ItemHash(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? 0 :
                item.m_dropPrefab.name.GetStableHashCode();
        }

        static string ItemName(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? "none" : item.m_dropPrefab.name;
        }
    }
}

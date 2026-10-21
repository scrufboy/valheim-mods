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
        EquipmentPosture desired;
        float nextFaultLog;

        public string Status { get; private set; }

        void Awake()
        {
            human = GetComponent<Humanoid>();
            ai = GetComponent<MonsterAI>();
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null || ShowHands == null)
                throw new MissingMemberException("Valheim's native hand-item stow contract changed.");
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
                    if (left != null || right != null) human.HideHandItems(false, true);
                    Status = "stowed last pair=" + ItemName(hiddenLeft ?? left) + "/" + ItemName(hiddenRight ?? right);
                }
                else
                {
                    if (left == null && right == null && (hiddenLeft != null || hiddenRight != null))
                        ShowHands.Invoke(human, new object[] { false, true });
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

        static string ItemName(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? "none" : item.m_dropPrefab.name;
        }
    }
}

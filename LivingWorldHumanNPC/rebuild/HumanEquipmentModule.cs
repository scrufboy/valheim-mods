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
        ZNetView view;
        EquipmentPosture desired;
        float nextFaultLog;
        int appliedLeft = Int32.MinValue;
        int appliedLeftVariant = Int32.MinValue;
        int appliedLeftQuality = Int32.MinValue;
        int appliedRight = Int32.MinValue;
        int appliedRightQuality = Int32.MinValue;
        int appliedBackLeft = Int32.MinValue;
        int appliedBackLeftVariant = Int32.MinValue;
        int appliedBackLeftQuality = Int32.MinValue;
        int appliedBackRight = Int32.MinValue;
        int appliedBackRightQuality = Int32.MinValue;

        public string Status { get; private set; }

        void Awake()
        {
            human = GetComponent<Humanoid>();
            ai = GetComponent<MonsterAI>();
            visuals = GetComponent<VisEquipment>();
            view = GetComponent<ZNetView>();
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null || ShowHands == null)
                throw new MissingMemberException("Valheim's native hand-item stow contract changed.");
            if (visuals == null) throw new MissingComponentException("The native human has no VisEquipment adapter.");
            if (view == null) throw new MissingComponentException("The native human has no ZNetView adapter.");
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
                    SyncPresentation(null, null, hiddenLeft, hiddenRight);
                    Status = "stowed on back=" + ItemName(hiddenLeft) + "/" + ItemName(hiddenRight);
                }
                else
                {
                    if (left == null && right == null && (hiddenLeft != null || hiddenRight != null))
                    {
                        ShowHands.Invoke(human, new object[] { false, true });
                        left = Left.GetValue(human) as ItemDrop.ItemData;
                        right = Right.GetValue(human) as ItemDrop.ItemData;
                    }
                    SyncPresentation(left, right, null, null);
                    Status = "drawn visuals=" + ItemName(left) + "/" + ItemName(right);
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

        void SyncPresentation(ItemDrop.ItemData left, ItemDrop.ItemData right,
            ItemDrop.ItemData backLeft, ItemDrop.ItemData backRight)
        {
            // Humanoid.SetupVisEquipment intentionally updates hidden/back
            // slots only when Character.IsPlayer() is true. These actors are
            // native Player clones with the Player component removed, so the
            // native hide/show transaction preserves the correct item data but
            // requires this presentation-only synchronization. Persisted
            // VisEquipment hand records can also outlive its local cache; write
            // the complete mutually-exclusive hand/back presentation together.
            int leftHash = ItemHash(left);
            int leftVariant = left == null ? 0 : left.m_variant;
            int leftQuality = left == null ? 0 : left.m_quality;
            int rightHash = ItemHash(right);
            int rightQuality = right == null ? 0 : right.m_quality;
            int backLeftHash = ItemHash(backLeft);
            int backLeftVariant = backLeft == null ? 0 : backLeft.m_variant;
            int backLeftQuality = backLeft == null ? 0 : backLeft.m_quality;
            int backRightHash = ItemHash(backRight);
            int backRightQuality = backRight == null ? 0 : backRight.m_quality;

            if (leftHash == appliedLeft && leftVariant == appliedLeftVariant && leftQuality == appliedLeftQuality &&
                rightHash == appliedRight && rightQuality == appliedRightQuality &&
                backLeftHash == appliedBackLeft && backLeftVariant == appliedBackLeftVariant &&
                backLeftQuality == appliedBackLeftQuality && backRightHash == appliedBackRight &&
                backRightQuality == appliedBackRightQuality) return;

            ZDO zdo = view.GetZDO();
            if (zdo == null || !view.IsOwner()) return;
            zdo.Set(ZDOVars.s_leftItem, leftHash, false);
            zdo.Set(ZDOVars.s_leftItemVariant, leftVariant, false);
            zdo.Set(ZDOVars.s_leftItemQuality, leftQuality, false);
            zdo.Set(ZDOVars.s_rightItem, rightHash, false);
            zdo.Set(ZDOVars.s_rightItemQuality, rightQuality, false);
            zdo.Set(ZDOVars.s_leftBackItem, backLeftHash, false);
            zdo.Set(ZDOVars.s_leftBackItemVariant, backLeftVariant, false);
            zdo.Set(ZDOVars.s_leftBackItemQuality, backLeftQuality, false);
            zdo.Set(ZDOVars.s_rightBackItem, backRightHash, false);
            zdo.Set(ZDOVars.s_rightBackItemQuality, backRightQuality, false);

            visuals.SetLeftItem(leftHash, leftVariant, leftQuality);
            visuals.SetRightItem(rightHash, rightQuality);
            visuals.SetLeftBackItem(backLeftHash, backLeftVariant, backLeftQuality);
            visuals.SetRightBackItem(backRightHash, backRightQuality);

            appliedLeft = leftHash;
            appliedLeftVariant = leftVariant;
            appliedLeftQuality = leftQuality;
            appliedRight = rightHash;
            appliedRightQuality = rightQuality;
            appliedBackLeft = backLeftHash;
            appliedBackLeftVariant = backLeftVariant;
            appliedBackLeftQuality = backLeftQuality;
            appliedBackRight = backRightHash;
            appliedBackRightQuality = backRightQuality;
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

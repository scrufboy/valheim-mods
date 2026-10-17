using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // One owner-side hand/readiness adapter for every composed human. It uses
    // Valheim's real hide/show lifecycle, including the native hip/equip cue.
    public sealed class HumanEquipmentReadiness : MonoBehaviour
    {
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Left = typeof(Humanoid).GetField("m_leftItem", Hidden);
        static readonly FieldInfo Right = typeof(Humanoid).GetField("m_rightItem", Hidden);
        static readonly FieldInfo HiddenLeft = typeof(Humanoid).GetField("m_hiddenLeftItem", Hidden);
        static readonly FieldInfo HiddenRight = typeof(Humanoid).GetField("m_hiddenRightItem", Hidden);
        static readonly MethodInfo ShowHands = typeof(Humanoid).GetMethod("ShowHandItems", Hidden, null, new[] { typeof(bool), typeof(bool) }, null);
        static readonly object[] ShowWithoutHipAnimation = { false, false };
        Humanoid body;
        ZNetView view;
        HumanCompanion companion;
        HumanCompanionPosture posture;
        HumanAppearance appearance;
        VisEquipment visuals;
        float nextDecision;
        int reported = -1;
        bool fault;

        internal static void InstallPatches(Harmony harmony)
        {
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null || ShowHands == null)
                throw new MissingMemberException("Native humanoid hand equipment members changed");
            var run = typeof(Character).GetMethod("GetRunSpeedFactor", Hidden, null, Type.EmptyTypes, null);
            if (run == null) throw new MissingMethodException("Character.GetRunSpeedFactor");
            harmony.Patch(run, postfix: new HarmonyMethod(typeof(HumanEquipmentReadiness), "AfterRunSpeedFactor"));
        }

        void Cache()
        {
            if (body == null) body = GetComponent<Humanoid>();
            if (view == null) view = GetComponent<ZNetView>();
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (posture == null) posture = GetComponent<HumanCompanionPosture>();
            if (appearance == null) appearance = GetComponent<HumanAppearance>();
            if (visuals == null) visuals = GetComponent<VisEquipment>();
        }

        bool Owner()
        {
            Cache();
            return !fault && view != null && view.IsValid() && view.IsOwner() && body != null && !body.IsDead() &&
                appearance != null && appearance.Initialized;
        }

        bool HandsSafe()
        {
            return Owner() && !body.InAttack() && !body.IsDrawingBow() && !body.IsBlocking() && !body.IsStaggering() &&
                (posture == null || (!posture.OwnsAmbientHands && !posture.HasAmbientPose));
        }

        static ItemDrop.ItemData Slot(FieldInfo field, Humanoid human)
        { return field.GetValue(human) as ItemDrop.ItemData; }

        static bool CombatItem(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return false;
            var type = item.m_shared.m_itemType;
            return type == ItemDrop.ItemData.ItemType.OneHandedWeapon || type == ItemDrop.ItemData.ItemType.Bow ||
                type == ItemDrop.ItemData.ItemType.Shield || type == ItemDrop.ItemData.ItemType.TwoHandedWeapon ||
                type == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft;
        }

        static bool VisibleCombat(Humanoid human)
        { return human != null && (CombatItem(Slot(Left, human)) || CombatItem(Slot(Right, human))); }

        static bool HiddenCombat(Humanoid human)
        { return human != null && (CombatItem(Slot(HiddenLeft, human)) || CombatItem(Slot(HiddenRight, human))); }

        static bool LeaderReady(Player leader)
        { return leader != null && !leader.IsDead() && VisibleCombat(leader); }

        internal bool WantsWeapons()
        {
            Cache();
            // A real encounter has priority, while an explicitly armed party
            // leader is the established readiness signal. Town etiquette is a
            // separate future rule and must not erase party mirroring.
            return Owner() && companion != null &&
                (companion.CombatReadinessActive || LeaderReady(companion.PartyLeader));
        }

        internal bool EnsureCombatReady()
        {
            if (!HandsSafe()) return false;
            if (!VisibleCombat(body) && !HiddenCombat(body)) RestoreBasePair(false);
            if (HiddenCombat(body)) ShowHands.Invoke(body, ShowWithoutHipAnimation);
            SyncBackVisuals(false);
            Report(1, "combat threat: weapons drawn without duplicate hip animation");
            return VisibleCombat(body) && !HiddenCombat(body);
        }

        // Utility equipment is allowed to borrow the hand, never to destroy the
        // persistent combat/stow presentation that existed before it.
        internal void RecoverFromUtility()
        {
            if (!HandsSafe() || VisibleCombat(body) || HiddenCombat(body)) return;
            RestoreBasePair(!WantsWeapons());
        }

        void RestoreBasePair(bool stow)
        {
            var items = body.GetInventory().GetAllItems();
            var sword = items.FirstOrDefault(item => item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == "SwordBronze");
            var shield = items.FirstOrDefault(item => item != null && item.m_dropPrefab != null && item.m_dropPrefab.name == "ShieldWood");
            if (sword == null || shield == null) throw new InvalidOperationException("Persistent sword/shield loadout missing");
            if (!sword.m_equipped && !body.EquipItem(sword, false)) throw new InvalidOperationException("Sword restoration refused");
            if (!shield.m_equipped && !body.EquipItem(shield, false)) throw new InvalidOperationException("Shield restoration refused");
            if (stow) body.HideHandItems(false, true);
        }

        bool EnsurePeacefulStow()
        {
            bool visible = VisibleCombat(body), hidden = HiddenCombat(body);
            var action = EquipmentPresentationPolicy.Stow(visible, hidden);
            if (action == StowAction.Complete) { SyncBackVisuals(true); return true; }
            if (action == StowAction.RestoreThenHide) RestoreBasePair(false);
            if (!body.HideHandItems(false, true)) return false;
            bool verified = EquipmentPresentationPolicy.VerifiedStowed(VisibleCombat(body), HiddenCombat(body));
            if (verified) SyncBackVisuals(true);
            return verified;
        }

        void SyncBackVisuals(bool stowed)
        {
            if (visuals == null) throw new InvalidOperationException("Human visual equipment adapter missing");
            var left = stowed ? Slot(HiddenLeft, body) : null;
            var right = stowed ? Slot(HiddenRight, body) : null;
            int leftHash = left == null || left.m_dropPrefab == null ? 0 : left.m_dropPrefab.name.GetStableHashCode();
            int rightHash = right == null || right.m_dropPrefab == null ? 0 : right.m_dropPrefab.name.GetStableHashCode();
            visuals.SetLeftBackItem(leftHash, left == null ? 0 : left.m_variant, left == null ? 0 : left.m_quality);
            visuals.SetRightBackItem(rightHash, right == null ? 0 : right.m_quality);
        }

        void Update()
        {
            if (Time.time < nextDecision) return;
            nextDecision = Time.time + .1f;
            try
            {
                if (!HandsSafe()) return;
                bool threat = companion != null && companion.CombatReadinessActive;
                bool show = WantsWeapons();
                if (show)
                {
                    var utility = GetComponent<HumanUtilityTorch>();
                    if (utility != null) utility.ReleaseForWeapons();
                    if (HiddenCombat(body)) ShowHands.Invoke(body, ShowWithoutHipAnimation);
                    SyncBackVisuals(false);
                    Report(1, "authorized combat threat: weapons drawn");
                }
                else
                {
                    if (EnsurePeacefulStow()) Report(0, "social/peaceful: native back-slot stow verified");
                    else Report(2, "STOW RETRY: native hide did not produce hidden combat equipment");
                }
            }
            catch (Exception error)
            {
                fault = true;
                var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
                if (plugin != null) plugin.Report("READINESS FAIL: " + error.Message + "; native equipment left untouched");
            }
        }

        void Report(int state, string message)
        {
            if (reported == state) return;
            reported = state;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("READINESS " + body.m_name + ": " + message);
        }

        static void AfterRunSpeedFactor(Character __instance, ref float __result)
        {
            var readiness = __instance == null ? null : __instance.GetComponent<HumanEquipmentReadiness>();
            var human = __instance as Humanoid;
            if (readiness == null || !readiness.isActiveAndEnabled || readiness.fault || human == null) return;
            float equipment = human.GetInventory().GetAllItems().Where(item => item != null && item.m_equipped && item.m_shared != null)
                .Sum(item => item.m_shared.m_movementModifier);
            // Match Player.GetRunSpeedFactor's native equipment term. Hidden
            // hand items are unequipped by HideHandItems, so stowing naturally
            // removes only their penalty while armor still counts.
            __result *= Mathf.Max(.1f, 1f + equipment * 1.5f);
        }
    }
}

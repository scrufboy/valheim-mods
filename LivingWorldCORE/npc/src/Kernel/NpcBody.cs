using System;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// The only place Living World writes native NPC state. Every writer names the
    /// domain it needs and the calling behavior; a caller that does not currently
    /// hold that domain is refused. This is what makes "the torch module re-stowed
    /// weapons during combat" impossible rather than merely unlikely.
    /// </summary>
    public sealed class NpcBody
    {
        public readonly GameObject Root;
        public readonly Humanoid Human;
        public readonly MonsterAI Ai;
        public readonly ZNetView View;
        public readonly VisEquipment Visuals;
        public readonly ZSyncAnimation Anim;
        readonly NpcKernel kernel;

        readonly float baseSpeed, baseWalkSpeed, baseRunSpeed;
        ItemDrop.ItemData torch;
        bool crouching, sitting;
        int appliedLeft = Int32.MinValue, appliedLeftVariant, appliedLeftQuality, appliedRight, appliedRightQuality;
        int appliedBackLeft, appliedBackLeftVariant, appliedBackLeftQuality, appliedBackRight, appliedBackRightQuality;
        readonly object[] lookArgs = new object[1];

        public int EncounterReleases { get; private set; }
        public bool Crouching { get { return crouching; } }
        public bool Sitting { get { return sitting; } }

        internal NpcBody(NpcKernel kernel, GameObject root)
        {
            this.kernel = kernel;
            Root = root;
            Human = root.GetComponent<Humanoid>();
            Ai = root.GetComponent<MonsterAI>();
            View = root.GetComponent<ZNetView>();
            Visuals = root.GetComponent<VisEquipment>();
            Anim = root.GetComponent<ZSyncAnimation>();
            if (Human != null)
            {
                baseSpeed = Human.m_speed;
                baseWalkSpeed = Human.m_walkSpeed;
                baseRunSpeed = Human.m_runSpeed;
            }
        }

        public bool IsOwner { get { return View != null && View.IsValid() && View.IsOwner(); } }

        void Require(NpcDomain domain, NpcBehavior caller)
        {
            if (!IsOwner) throw new InvalidOperationException("not the network owner");
            if (!kernel.Holds(caller, domain))
            {
                kernel.RecordViolation(caller, domain);
                throw new InvalidOperationException((caller == null ? "unknown" : caller.Name) +
                    " wrote " + domain + " without holding it");
            }
        }

        // ---------------- Read-only views (anyone) ----------------

        public ItemDrop.ItemData LeftItem { get { return NativeApi.LeftItem.GetValue(Human) as ItemDrop.ItemData; } }
        public ItemDrop.ItemData RightItem { get { return NativeApi.RightItem.GetValue(Human) as ItemDrop.ItemData; } }
        public ItemDrop.ItemData BackLeft { get { return NativeApi.HiddenLeftItem.GetValue(Human) as ItemDrop.ItemData; } }
        public ItemDrop.ItemData BackRight { get { return NativeApi.HiddenRightItem.GetValue(Human) as ItemDrop.ItemData; } }
        public bool TorchInHand
        {
            get
            {
                return torch != null && (ReferenceEquals(LeftItem, torch) || ReferenceEquals(RightItem, torch));
            }
        }

        // ---------------- Movement ----------------

        public void Follow(NpcBehavior caller, GameObject target)
        {
            Require(NpcDomain.Movement, caller);
            if (Ai.GetFollowTarget() != target) Ai.SetFollowTarget(target);
        }

        public void SetPace(NpcBehavior caller, bool run, bool catchUp, float leaderSpeed, bool sneak)
        {
            Require(NpcDomain.Movement, caller);
            Human.m_walkSpeed = baseWalkSpeed;
            if (sneak)
            {
                // Non-Player humans cannot natively crouch-walk; match the leader's sneak pace.
                float sneakSpeed = Mathf.Max(Human.m_crouchSpeed, leaderSpeed + .1f);
                Human.m_speed = Mathf.Min(baseSpeed, sneakSpeed);
                Human.m_runSpeed = Human.m_speed;
                Human.SetRun(false);
                return;
            }
            Human.m_speed = run ? baseSpeed : Mathf.Max(baseSpeed, leaderSpeed + .15f);
            if (run)
            {
                float margin = catchUp ? 1.25f : .5f;
                float cap = baseRunSpeed * (catchUp ? 1.35f : 1.2f);
                Human.m_runSpeed = Mathf.Min(cap, Mathf.Max(baseRunSpeed, leaderSpeed + margin));
            }
            else Human.m_runSpeed = baseRunSpeed;
        }

        public void SetRun(NpcBehavior caller, bool run)
        {
            Require(NpcDomain.Movement, caller);
            Human.SetRun(run);
        }

        public void RestorePace(NpcBehavior caller)
        {
            Require(NpcDomain.Movement, caller);
            RestorePaceUnchecked();
        }

        internal void RestorePaceUnchecked()
        {
            if (Human == null) return;
            Human.m_speed = baseSpeed;
            Human.m_walkSpeed = baseWalkSpeed;
            Human.m_runSpeed = baseRunSpeed;
        }

        /// <summary>Complete native encounter abandon (MonsterAI.UpdateTarget's own branch).</summary>
        public void ReleaseEncounter(NpcBehavior caller)
        {
            Require(NpcDomain.Movement, caller);
            NativeApi.SetAlerted.Invoke(Ai, new object[] { false });
            NativeApi.TargetCreature.SetValue(Ai, null);
            NativeApi.TargetStatic.SetValue(Ai, null);
            NativeApi.TimeSinceAttacking.SetValue(Ai, 0f);
            // 1 s, not 5: with targets chosen by our rule, a short pause is enough, and 5 s left a
            // companion unable to answer an enemy walking up to it (1.0.5: a 54-point hit).
            NativeApi.UpdateTargetTimer.SetValue(Ai, 1f);
            EncounterReleases++;
        }

        /// <summary>Let go of a target that is no longer worth fighting; native re-picks at once.</summary>
        public void DropTarget(NpcBehavior caller)
        {
            Require(NpcDomain.Movement, caller);
            NativeApi.TargetCreature.SetValue(Ai, null);
            NativeApi.UpdateTargetTimer.SetValue(Ai, 0f);
            TargetDrops++;
        }

        public int TargetDrops { get; private set; }

        // ---------------- Look ----------------

        public void LookAt(NpcBehavior caller, Vector3 point)
        {
            Require(NpcDomain.Look, caller);
            lookArgs[0] = point;
            NativeApi.LookAt.Invoke(Ai, lookArgs);
        }

        // ---------------- Posture ----------------

        public void SetCrouch(NpcBehavior caller, bool value)
        {
            Require(NpcDomain.Posture, caller);
            if (crouching == value) return;
            crouching = value;
            Anim.SetBool("crouching", value);
        }

        public void SetSit(NpcBehavior caller, bool value)
        {
            Require(NpcDomain.Posture, caller);
            if (sitting == value) return;
            sitting = value;
            Anim.SetBool("emote_sit", value);
            if (!value) Anim.SetTrigger("emote_stop");
        }

        // ---------------- Hands ----------------

        /// <summary>Weapons go to the back. Torch, if any, is put away first.</summary>
        public void Stow(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            DouseTorchCore();
            if (LeftItem != null || RightItem != null) Human.HideHandItems(false, true);
            RestoreShieldToBack();
            Publish(null, null, BackLeft, BackRight);
        }

        /// <summary>Weapons come off the back into the hands.</summary>
        public void Draw(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            DouseTorchCore();
            if (LeftItem == null && RightItem == null && (BackLeft != null || BackRight != null))
                NativeApi.ShowHandItems.Invoke(Human, new object[] { false, true });
            Publish(LeftItem, RightItem, null, null);
        }

        /// <summary>
        /// Torch in hand, weapons stay visibly on the back. EquipItem natively wipes the
        /// back slots, so they are captured first and restored after — this is the exact
        /// cause of every past "torch lost the stowed weapons" regression.
        /// </summary>
        public bool LightTorch(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            if (TorchInHand) { Publish(LeftItem, RightItem, BackLeft, BackRight); return true; }
            if (LeftItem != null || RightItem != null) Human.HideHandItems(false, true);
            ItemDrop.ItemData backLeft = BackLeft, backRight = BackRight;
            ItemDrop.ItemData item = EnsureTorchItem();
            if (item == null) return false;
            if (item.m_equipped && !TorchInHand) Human.UnequipItem(item, false);
            bool equipped = Human.EquipItem(item, false);
            NativeApi.HiddenLeftItem.SetValue(Human, backLeft);
            NativeApi.HiddenRightItem.SetValue(Human, backRight);
            if (!equipped || !TorchInHand)
            {
                DouseTorchCore();
                return false;
            }
            Publish(LeftItem, RightItem, backLeft, backRight);
            return true;
        }

        public void DouseTorch(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            DouseTorchCore();
            Publish(LeftItem, RightItem, BackLeft, BackRight);
        }

        /// <summary>Removes the torch from hand and inventory so native combat can never select it.</summary>
        void DouseTorchCore()
        {
            if (torch == null) return;
            if (torch.m_equipped || TorchInHand) Human.UnequipItem(torch, false);
            Inventory inventory = Human.GetInventory();
            if (inventory != null && inventory.ContainsItem(torch)) inventory.RemoveItem(torch);
            torch = null;
        }

        ItemDrop.ItemData EnsureTorchItem()
        {
            Inventory inventory = Human.GetInventory();
            if (inventory == null || ObjectDB.instance == null) return null;
            if (torch != null && inventory.ContainsItem(torch)) return torch;
            GameObject prefab = ObjectDB.instance.GetItemPrefab("Torch");
            ItemDrop drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            if (drop == null) return null;
            ItemDrop.ItemData item = drop.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = 1;
            // Actor-local copy of the shared data so this torch never wears out.
            // The global vanilla Torch definition is untouched.
            item.m_shared = (ItemDrop.ItemData.SharedData)NativeApi.CloneObject.Invoke(item.m_shared, null);
            item.m_shared.m_useDurability = false;
            if (!inventory.AddItem(item)) return null;
            torch = item;
            return torch;
        }

        /// <summary>Native combat picks its own weapon at target handoff. We only ask it to, once.</summary>
        public void EquipBestWeaponFor(NpcBehavior caller, Character target)
        {
            Require(NpcDomain.Hands, caller);
            DouseTorchCore();
            Human.EquipBestWeapon(target, null, null, null);
        }

        /// <summary>
        /// Keeps a one-handed weapon paired with the actor's shield during combat.
        /// Native EquipItem wipes the back slots and native weapon choice never re-equips
        /// a shield, so after the first weapon swap a guard fought sword-only for the rest
        /// of the 2026-10-23 session. Never interrupts an attack, block or bow draw.
        /// </summary>
        public bool PairShield(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            if (Human.InAttack() || Human.IsDrawingBow() || Human.IsBlocking()) return false;
            ItemDrop.ItemData right = RightItem, left = LeftItem;
            if (!CombatHands.ShouldPairShield(TypeOf(right), left != null)) return false;
            ItemDrop.ItemData shield = FindShield();
            if (shield == null) return false;
            return Human.EquipItem(shield, false);
        }

        /// <summary>
        /// Gives this actor's own bow copies a minimum engagement range. Native
        /// Humanoid.EquipBestWeapon skips any weapon whose m_aiAttackRangeMin exceeds the
        /// target distance (verified in IL), so up close the game itself picks the sword.
        /// The global bow definition is untouched.
        /// </summary>
        public int TuneRangedWeapons(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            Inventory inventory = Human.GetInventory();
            if (inventory == null) return 0;
            int tuned = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null || !CombatHands.IsRanged(TypeOf(item))) continue;
                ItemDrop.ItemData.SharedData s = item.m_shared;
                if (s.m_aiAttackRangeMin >= CombatHands.RangedMinimum && s.m_aiAttackRange >= CombatHands.RangedAiRange &&
                    s.m_attack != null && !s.m_attack.m_bowDraw) continue;
                // Actor-local copy: the shared prefab data (and the player's own bows) stay untouched.
                s = (ItemDrop.ItemData.SharedData)NativeApi.CloneObject.Invoke(s, null);
                if (s.m_attack != null)
                {
                    s.m_attack = s.m_attack.Clone();
                    s.m_attack.m_bowDraw = false;
                    s.m_attack.m_drawDurationMin = 0f;
                }
                s.m_aiAttackRange = CombatHands.RangedAiRange;
                s.m_aiAttackRangeMin = CombatHands.RangedMinimum;
                s.m_aiAttackMaxAngle = CombatHands.RangedAimAngle;
                item.m_shared = s;
                tuned++;
            }
            return tuned;
        }

        /// <summary>
        /// Keeps a bow-carrier's quiver at QuiverSize arrows of the type she already uses (wood if none).
        /// Called outside combat only; stand-in for resupply until camps/towns provide it.
        /// </summary>
        public int TopUpQuiver(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            Inventory inventory = Human.GetInventory();
            if (inventory == null) return 0;
            bool hasBow = false;
            int count = 0;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null) continue;
                if (CombatHands.IsRanged(TypeOf(item))) hasBow = true;
                if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
                {
                    count += item.m_stack;
                }
            }
            if (!hasBow || count >= CombatHands.QuiverSize) return 0;
            GameObject prefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab("ArrowWood");  // resupply is always basic arrows: skill, not gear
            if (prefab == null) return 0;
            int added = 0;
            while (count < CombatHands.QuiverSize)
            {
                int want = CombatHands.QuiverSize - count;
                if (!inventory.AddItem(prefab, want)) break;
                count += want;
                added += want;
            }
            return added;
        }

        public bool ShieldInHand
        {
            get
            {
                ItemDrop.ItemData left = LeftItem;
                return left != null && left.m_shared != null && left.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield;
            }
        }

        public int BlockRaises { get; private set; }

        /// <summary>Raise or lower the shield. Native code does the rest (animation, sync, parry, stamina, stagger).</summary>
        public void SetBlocking(NpcBehavior caller, bool raise)
        {
            Require(NpcDomain.Hands, caller);
            bool current = (bool)NativeApi.Blocking.GetValue(Human);
            if (current == raise) return;
            NativeApi.Blocking.SetValue(Human, raise);
            if (raise) BlockRaises++;
        }

        internal void LowerBlockUnchecked()
        {
            if (Human != null && NativeApi.Blocking != null && IsOwner) NativeApi.Blocking.SetValue(Human, false);
        }

        public int AmmoCount
        {
            get
            {
                Inventory inventory = Human.GetInventory();
                if (inventory == null) return 0;
                int count = 0;
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                    if (item != null && item.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo)
                        count += item.m_stack;
                return count;
            }
        }

        ItemDrop.ItemData FindShield()
        {
            Inventory inventory = Human.GetInventory();
            if (inventory == null) return null;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                if (item != null && item.m_shared != null && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
                    return item;
            return null;
        }

        // Saved actors that already lost the shield from their back get it back on the next stow.
        void RestoreShieldToBack()
        {
            if (BackLeft != null || LeftItem != null || RightItem != null) return;
            if (!CombatHands.ShouldPairShield(TypeOf(BackRight), false)) return;
            ItemDrop.ItemData shield = FindShield();
            if (shield != null && !shield.m_equipped) NativeApi.HiddenLeftItem.SetValue(Human, shield);
        }

        static CombatHands.Kind TypeOf(ItemDrop.ItemData item)
        {
            if (item == null || item.m_shared == null) return CombatHands.Kind.None;
            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon: return CombatHands.Kind.OneHanded;
                case ItemDrop.ItemData.ItemType.Shield: return CombatHands.Kind.Shield;
                case ItemDrop.ItemData.ItemType.Bow: return CombatHands.Kind.Ranged;
                case ItemDrop.ItemData.ItemType.Torch: return CombatHands.Kind.Torch;
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft: return CombatHands.Kind.TwoHanded;
                default: return CombatHands.Kind.Other;
            }
        }

        /// <summary>Publish whatever native combat currently holds without changing it.</summary>
        public void PublishObserved(NpcBehavior caller)
        {
            Require(NpcDomain.Hands, caller);
            Publish(LeftItem, RightItem, BackLeft, BackRight);
        }

        /// <summary>Kernel-only cleanup on teardown (no owner exists any more).</summary>
        internal void ReleaseAllUnchecked()
        {
            if (!IsOwner) return;
            DouseTorchCore();
            LowerBlockUnchecked();
            if (crouching) { crouching = false; Anim.SetBool("crouching", false); }
            if (sitting) { sitting = false; Anim.SetBool("emote_sit", false); Anim.SetTrigger("emote_stop"); }
            RestorePaceUnchecked();
        }

        // Humanoid.SetupVisEquipment updates back slots only for real Players. These
        // actors are Player clones with Player removed, so hand AND back presentation
        // are written together here (accepted in 0.6.1 / 0.6.3 live runs).
        void Publish(ItemDrop.ItemData left, ItemDrop.ItemData right, ItemDrop.ItemData backLeft, ItemDrop.ItemData backRight)
        {
            int leftHash = Hash(left), leftVariant = left == null ? 0 : left.m_variant, leftQuality = left == null ? 0 : left.m_quality;
            int rightHash = Hash(right), rightQuality = right == null ? 0 : right.m_quality;
            int bLeftHash = Hash(backLeft), bLeftVariant = backLeft == null ? 0 : backLeft.m_variant,
                bLeftQuality = backLeft == null ? 0 : backLeft.m_quality;
            int bRightHash = Hash(backRight), bRightQuality = backRight == null ? 0 : backRight.m_quality;
            if (leftHash == appliedLeft && leftVariant == appliedLeftVariant && leftQuality == appliedLeftQuality &&
                rightHash == appliedRight && rightQuality == appliedRightQuality && bLeftHash == appliedBackLeft &&
                bLeftVariant == appliedBackLeftVariant && bLeftQuality == appliedBackLeftQuality &&
                bRightHash == appliedBackRight && bRightQuality == appliedBackRightQuality) return;
            ZDO zdo = View.GetZDO();
            if (zdo == null) return;
            zdo.Set(ZDOVars.s_leftItem, leftHash, false);
            zdo.Set(ZDOVars.s_leftItemVariant, leftVariant, false);
            zdo.Set(ZDOVars.s_leftItemQuality, leftQuality, false);
            zdo.Set(ZDOVars.s_rightItem, rightHash, false);
            zdo.Set(ZDOVars.s_rightItemQuality, rightQuality, false);
            zdo.Set(ZDOVars.s_leftBackItem, bLeftHash, false);
            zdo.Set(ZDOVars.s_leftBackItemVariant, bLeftVariant, false);
            zdo.Set(ZDOVars.s_leftBackItemQuality, bLeftQuality, false);
            zdo.Set(ZDOVars.s_rightBackItem, bRightHash, false);
            zdo.Set(ZDOVars.s_rightBackItemQuality, bRightQuality, false);
            Visuals.SetLeftItem(leftHash, leftVariant, leftQuality);
            Visuals.SetRightItem(rightHash, rightQuality);
            Visuals.SetLeftBackItem(bLeftHash, bLeftVariant, bLeftQuality);
            Visuals.SetRightBackItem(bRightHash, bRightQuality);
            appliedLeft = leftHash; appliedLeftVariant = leftVariant; appliedLeftQuality = leftQuality;
            appliedRight = rightHash; appliedRightQuality = rightQuality;
            appliedBackLeft = bLeftHash; appliedBackLeftVariant = bLeftVariant; appliedBackLeftQuality = bLeftQuality;
            appliedBackRight = bRightHash; appliedBackRightQuality = bRightQuality;
        }

        static int Hash(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? 0 : item.m_dropPrefab.name.GetStableHashCode();
        }

        public static string ItemName(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? "none" : item.m_dropPrefab.name;
        }
    }
}

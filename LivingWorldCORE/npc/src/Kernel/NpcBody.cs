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

        static readonly System.Collections.Generic.Dictionary<Character, NpcBody> ByCharacter =
            new System.Collections.Generic.Dictionary<Character, NpcBody>();

        /// <summary>The companion body for a native character, or null (all other characters are untouched).</summary>
        internal static NpcBody Of(Character c)
        {
            NpcBody body;
            return c != null && ByCharacter.TryGetValue(c, out body) ? body : null;
        }

        internal void Unregister() { if (Human != null) ByCharacter.Remove(Human); }

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
                ByCharacter[Human] = this;
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

        // ---------------- Lab commands (no behavior domain; owner only) ----------------

        /// <summary>lwheal: full health and no lingering status effects (cold, poison, burning...).</summary>
        internal bool HealFull()
        {
            if (!IsOwner || Human == null || Human.IsDead()) return false;
            SEMan seman = Human.GetSEMan();
            if (seman != null) seman.RemoveAllStatusEffects(true);
            Human.Heal(Human.GetMaxHealth() - Human.GetHealth(), true);
            return true;
        }

        /// <summary>lwsummon: place the companion at a point (native transform sync carries it to peers).</summary>
        internal bool PlaceAt(Vector3 point, Quaternion facing)
        {
            if (!IsOwner || Human == null || Human.IsDead()) return false;
            Rigidbody rb = Human.GetComponent<Rigidbody>();
            if (rb != null) { rb.velocity = Vector3.zero; rb.position = point; }
            Human.transform.position = point;
            Human.transform.rotation = facing;
            return true;
        }

        // ---------------- Gear kits (lab: lwparty plains / lwgear; no behavior domain; owner only) ----------------

        public const string KitKey = "lw_kit", KitQualityKey = "lw_kit_quality";
        bool kitChecked;

        /// <summary>The recorded kit ("" = prefab default, iron).</summary>
        public string Kit { get { ZDO z = View == null || !View.IsValid() ? null : View.GetZDO(); return z == null ? "" : z.GetString(KitKey, ""); } }
        public int KitQuality { get { ZDO z = View == null || !View.IsValid() ? null : View.GetZDO(); return z == null ? 1 : z.GetInt(KitQualityKey, 1); } }

        /// <summary>
        /// Replace everything carried with a vanilla kit at a quality, armor worn, weapons on the back, and
        /// record it so it survives reloads. Returns "" or the reason it was refused.
        /// </summary>
        internal string Outfit(string kit, int quality)
        {
            if (!IsOwner || Human == null || Human.IsDead()) return "not loaded here";
            if (Human.InAttack()) return "mid-attack";
            string[] names = CompanionKits.Items(kit);
            if (names == null) return "unknown kit '" + kit + "' (iron, plains)";
            if (ObjectDB.instance == null) return "item database not ready";
            quality = CompanionKits.ClampQuality(quality);
            var prefabs = new GameObject[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                prefabs[i] = ObjectDB.instance.GetItemPrefab(names[i]);
                if (prefabs[i] == null || prefabs[i].GetComponent<ItemDrop>() == null) return "missing vanilla item " + names[i];
            }
            Inventory inventory = Human.GetInventory();
            if (inventory == null) return "no inventory";
            LowerBlockUnchecked();
            Human.UnequipAllItems();
            NativeApi.HiddenLeftItem.SetValue(Human, null);
            NativeApi.HiddenRightItem.SetValue(Human, null);
            torch = null;
            inventory.RemoveAll();
            ItemDrop.ItemData sword = null, shield = null;
            foreach (GameObject prefab in prefabs)
            {
                ItemDrop.ItemData item = prefab.GetComponent<ItemDrop>().m_itemData.Clone();
                item.m_dropPrefab = prefab;
                bool ammo = item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo;
                item.m_quality = ammo ? 1 : Mathf.Min(quality, Mathf.Max(1, item.m_shared.m_maxQuality));
                item.m_durability = item.GetMaxDurability();
                item.m_stack = ammo ? CombatHands.QuiverSize : 1;
                if (!inventory.AddItem(item)) return "inventory refused " + prefab.name;
                if (ammo) continue;
                if (!item.IsWeapon() && item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield) Human.EquipItem(item, false);
                else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield) shield = item;
                else if (!CombatHands.IsRanged(TypeOf(item))) sword = item;
            }
            // Weapons start on the back, as a fresh companion's do after its first stow.
            if (sword != null) Human.EquipItem(sword, false);
            if (shield != null) Human.EquipItem(shield, false);
            if (LeftItem != null || RightItem != null) Human.HideHandItems(false, true);
            Publish(null, null, BackLeft, BackRight);
            ZDO zdo = View.GetZDO();
            zdo.Set(KitKey, kit.ToLowerInvariant());
            zdo.Set(KitQualityKey, quality);
            kitChecked = true;
            return "";
        }

        /// <summary>Once per load: re-apply a recorded non-default kit (Valheim re-gives prefab items on load).</summary>
        internal void ReapplyKitOnce()
        {
            if (kitChecked || !IsOwner) return;
            kitChecked = true;
            string kit = Kit;
            if (kit.Length == 0 || kit == CompanionKits.Iron) return;
            string refused = Outfit(kit, KitQuality);
            if (refused.Length > 0) { kitChecked = false; CorePlugin.Warn(Human.GetHoverName() + " kit " + kit + " not re-applied: " + refused); }
        }

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
        public string LastWeaponPick { get; private set; }
        public int WeaponSwaps { get; private set; }

        /// <summary>
        /// Native-context writer: runs when MonsterAI asks Humanoid.EquipBestWeapon (once a second in
        /// combat). Answers with WeaponChoice instead of the native interval-and-random pick.
        /// Returns false to let native choose (no target, or no weapons we recognise).
        /// </summary>
        internal bool EquipForNativeSelection(Character target)
        {
            if (target == null || !IsOwner) return false;
            Inventory inventory = Human.GetInventory();
            if (inventory == null) return false;
            ItemDrop.ItemData bow = null, melee = null;
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (item == null || item.m_shared == null) continue;
                CombatHands.Kind kind = TypeOf(item);
                if (kind == CombatHands.Kind.Ranged && bow == null) bow = item;
                else if ((kind == CombatHands.Kind.OneHanded || kind == CombatHands.Kind.TwoHanded) && melee == null) melee = item;
            }
            if (bow == null && melee == null) return false;
            ItemDrop.ItemData right = RightItem, left = LeftItem;
            bool holdingBow = bow != null && (ReferenceEquals(right, bow) || ReferenceEquals(left, bow));
            bool holdingMelee = melee != null && ReferenceEquals(right, melee);
            float distance = Vector3.Distance(Root.transform.position, target.transform.position);
            WeaponChoice.Pick pick = WeaponChoice.Choose(bow != null, AmmoCount > 0, melee != null, holdingBow, holdingMelee,
                distance, target.IsFlying());
            LastWeaponPick = pick.ToString();
            ItemDrop.ItemData want = pick == WeaponChoice.Pick.Bow ? bow : pick == WeaponChoice.Pick.Melee ? melee : null;
            if (want != null && !Human.IsItemEquiped(want) && !Human.InAttack())
            {
                Human.EquipItem(want, true);
                WeaponSwaps++;
            }
            return true;
        }

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
            // Resupply the kit's own arrow (wood for the default kit); stand-in for camps/towns.
            GameObject prefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(CompanionKits.Arrow(Kit));
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

        // ---------------- Melee follow-ups: combo chain (1.0.17) and heavy attack (1.0.19) ----------------

        NpcBehavior chainOwner;
        [ThreadStatic] static Humanoid chainGate;   // the one actor whose HaveQueuedChain answers yes, for one call
        public int Chains { get; private set; }
        public int Heavies { get; private set; }
        public int LastChainLevel { get; private set; }
        public float LastChainDistance { get; private set; }
        public string LastHeavyReason { get; private set; }
        public float LastHeavyCost { get; private set; }
        public int LastHeavyClosing { get; private set; }
        public int LastHeavyInArea { get; private set; }
        bool wasInAttack, lastSwingWasComboEnd;
        float lastSwingEnded = float.NegativeInfinity;

        /// <summary>Combo position of the swing in progress (0 = first hit), or -1.</summary>
        public int CurrentChainLevel
        {
            get
            {
                Attack a = Human == null ? null : NativeApi.CurrentAttack.GetValue(Human) as Attack;
                return a == null ? -1 : (int)NativeApi.AttackChainLevel.GetValue(a);
            }
        }

        public bool CurrentAttackIsSecondary
        {
            get { return Human != null && (bool)NativeApi.CurrentAttackIsSecondary.GetValue(Human); }
        }

        /// <summary>The behavior holding the hands lets follow-ups happen (or stops them). Released on domain loss.</summary>
        public void AllowChains(NpcBehavior caller, bool allow)
        {
            Require(NpcDomain.Hands, caller);
            chainOwner = allow ? caller : null;
        }

        internal static bool ChainQueued(Humanoid human) { return human != null && ReferenceEquals(chainGate, human); }

        /// <summary>
        /// Every physics step from Humanoid.UpdateAttack. Mid-swing in the native chain window: a heavy
        /// if HeavyPolicy says so, else the next combo hit while it can land. Between swings: a heavy
        /// if HeavyPolicy says so (staggered target, crowd for an area heavy, finisher right after a
        /// combo's last hit). Returns 1 = combo hit started, 2 = heavy started, 0 = nothing.
        /// </summary>
        internal int TryFollowUp()
        {
            if (Human == null) return 0;
            bool inAttack = Human.InAttack();
            Attack current = NativeApi.CurrentAttack.GetValue(Human) as Attack;
            if (inAttack && current != null)
            {
                int level = (int)NativeApi.AttackChainLevel.GetValue(current);
                int levels = (int)NativeApi.AttackChainLevels.GetValue(current);
                lastSwingWasComboEnd = !CurrentAttackIsSecondary && levels > 1 && level >= levels - 1;
            }
            else if (wasInAttack) lastSwingEnded = Time.time;
            wasInAttack = inAttack;

            if (chainOwner == null) return 0;
            if (!IsOwner || !kernel.Holds(chainOwner, NpcDomain.Hands)) { chainOwner = null; return 0; }
            ItemDrop.ItemData weapon = Human.GetCurrentWeapon();
            if (weapon == null || weapon.m_shared == null || weapon.m_shared.m_attack == null) return 0;
            bool melee = weapon.m_shared.m_attack.m_attackType != Attack.AttackType.Projectile &&
                weapon.m_shared.m_skillType != Skills.SkillType.Bows;
            if (!melee) return 0;
            Character target = Ai.GetTargetCreature();
            bool alive = target != null && !target.IsDead();
            if (!alive) return 0;

            bool window = inAttack && current != null && (bool)NativeApi.CanStartChainAttack.Invoke(current, null);
            if (inAttack && !window) return 0;

            Vector3 offset = target.transform.position - Human.transform.position;
            float edge = Mathf.Max(0f, offset.magnitude - target.GetRadius());
            offset.y = 0f;
            float facing = offset.sqrMagnitude < .01f ? 0f : Vector3.Angle(Human.transform.forward, offset);

            // Heavy first: it is only ever chosen when it beats the alternative.
            Attack heavy = weapon.m_shared.m_secondaryAttack;
            bool hasHeavy = heavy != null && !String.IsNullOrEmpty(heavy.m_attackAnimation);
            HeavyPolicy.Shape shape = HeavyPolicy.ShapeOf(hasHeavy, hasHeavy && heavy.m_attackType == Attack.AttackType.Projectile,
                hasHeavy ? heavy.m_attackAngle : 0f, hasHeavy && heavy.m_attackType == Attack.AttackType.Area);
            if (shape != HeavyPolicy.Shape.None)
            {
                float cost = HeavyCost(heavy, weapon);
                int inArea, closing;
                CountEnemies(target, heavy.m_attackRange + HeavyPolicy.ReachSlack, out inArea, out closing);
                bool comboJustEnded = !inAttack && lastSwingWasComboEnd && Time.time - lastSwingEnded <= HeavyPolicy.FinisherWindow;
                HeavyPolicy.Why why = HeavyPolicy.Choose(shape, Stamina, cost, true, edge, heavy.m_attackRange, facing,
                    target.IsStaggering(), inArea, closing, comboJustEnded, heavy.m_damageMultiplier, heavy.m_staggerMultiplier);
                if (why != HeavyPolicy.Why.None && ChainPolicy.Affordable(Stamina, cost) && Start(target, true))
                {
                    Heavies++;
                    lastSwingWasComboEnd = false;
                    LastHeavyReason = why.ToString();
                    LastHeavyCost = cost;
                    LastHeavyClosing = closing;
                    LastHeavyInArea = inArea;
                    LastChainDistance = edge;
                    return 2;
                }
            }

            if (!window) return 0;
            float reach = weapon.m_shared.m_aiAttackRange;
            if (!ChainPolicy.Continue(true, true, true, CurrentAttackIsSecondary, true, edge, reach, facing,
                    weapon.m_shared.m_aiAttackMaxAngle, Stamina, weapon.m_shared.m_attack.m_attackStamina))
                return 0;
            if (!Start(target, false)) return 0;
            Chains++;
            LastChainLevel = CurrentChainLevel;
            LastChainDistance = edge;
            return 1;
        }

        bool Start(Character target, bool secondary)
        {
            chainGate = Human;
            try { return Human.StartAttack(target, secondary); }
            finally { chainGate = null; }
        }

        float HeavyCost(Attack heavy, ItemDrop.ItemData weapon)
        {
            // Attack.GetAttackStamina for a non-Player: equipment modifiers apply only to Player (isinst in IL)
            // and the skill factor is 0, so the cost is the attack's own number (status effects aside).
            return heavy.m_attackStamina;
        }

        void CountEnemies(Character target, float areaRadius, out int inArea, out int closing)
        {
            inArea = 0; closing = 0;
            Vector3 me = Human.transform.position;
            foreach (Character other in Character.GetAllCharacters())
            {
                if (other == null || other == Human || other.IsDead() || !BaseAI.IsEnemy(Human, other)) continue;
                float d = Vector3.Distance(other.transform.position, me) - other.GetRadius();
                if (d <= areaRadius) inArea++;
                if (other != target && d <= HeavyPolicy.ConvergeRadius) closing++;
            }
        }

        // ---------------- Stamina (1.0.19): the Player's rules for a companion ----------------

        float stamina = -1f, maxStamina, regenTimer;
        public float Stamina { get { EnsureStamina(); return stamina; } }
        public float MaxStamina { get { EnsureStamina(); return maxStamina; } }
        public int Exhaustions { get; private set; }

        void EnsureStamina()
        {
            if (stamina >= 0f) return;
            maxStamina = StaminaRules.Base + CorePlugin.FoodStamina;
            stamina = maxStamina;
        }

        internal bool HaveStaminaFor(float amount) { EnsureStamina(); return stamina > amount; }

        internal void SpendStamina(float amount)
        {
            if (amount <= 0f || float.IsNaN(amount)) return;
            EnsureStamina();
            bool had = stamina > 0f;
            stamina = Mathf.Max(0f, stamina - amount * Game.m_staminaRate);
            regenTimer = StaminaRules.RegenDelay;
            if (had && stamina <= 0f) Exhaustions++;
        }

        /// <summary>Player.UpdateStats regen, once per frame on the owner.</summary>
        internal void TickStamina(float dt)
        {
            EnsureStamina();
            maxStamina = StaminaRules.Base + CorePlugin.FoodStamina;
            regenTimer -= dt;
            if (stamina >= maxStamina) { stamina = maxStamina; return; }
            if (regenTimer > 0f || Human.InDodge() || Human.IsEncumbered()) return;
            float rate = StaminaRules.RegenPerSecond(stamina, maxStamina, Human.InAttack(), Human.IsBlocking());
            float mod = 1f;
            Human.GetSEMan().ModifyStaminaRegen(ref mod);
            stamina = Mathf.Min(maxStamina, stamina + rate * mod * dt * Game.m_staminaRegenRate);
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

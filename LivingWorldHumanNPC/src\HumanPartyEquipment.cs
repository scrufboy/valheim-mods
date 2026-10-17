using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Opt-in lab equipment adapter. No prefab/shared item definition is mutated.
    // Native Attack owns projectile creation, ammunition debit and hit resolution.
    public sealed class HumanPartyEquipment : MonoBehaviour
    {
        internal const string RecordKey = "lwc_party_equipment_v1";
        const string InventoryKey = "lwc_party_inventory_v1";
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo DrawTime = typeof(Humanoid).GetField("m_attackDrawTime", Hidden);
        static readonly FieldInfo Blocking = typeof(Character).GetField("m_blocking", Hidden);
        static readonly FieldInfo BlockTimer = typeof(Humanoid).GetField("m_blockTimer", Hidden);
        static readonly MethodInfo CurrentBlocker = typeof(Humanoid).GetMethod("GetCurrentBlocker", Hidden, null, Type.EmptyTypes, null);
        static readonly MethodInfo Look = typeof(BaseAI).GetMethod("LookAt", Hidden, null, new[] { typeof(Vector3) }, null);
        static readonly MethodInfo DoAttack = typeof(MonsterAI).GetMethod("DoAttack", Hidden, null, new[] { typeof(Character), typeof(bool) }, null);
        static readonly MethodInfo Copy = typeof(object).GetMethod("MemberwiseClone", Hidden);
        Humanoid body;
        MonsterAI ai;
        ZNetView view;
        HumanCompanion companion;
        ItemDrop.ItemData bow, sword, shield;
        Character cueTarget, blockTarget, riposteTarget;
        bool restored, fault, blocking, sawAttack, attemptedBlock, subscribed, ripostePending;
        float cueAt, blockExpires, riposteAt, riposteExpires, nextRiposteReport, nextBowReleaseReport;
        float weaponCommittedUntil;
        PartyWeapon committedWeapon;
        bool hasWeaponCommitment;
        string drawAnimation;
        public bool Ready { get { return isActiveAndEnabled && restored && !fault && view != null && view.IsValid() && view.IsOwner() && !body.IsDead(); } }
        public string Describe()
        {
            Cache();
            int arrows = body == null ? 0 : body.GetInventory().GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == "ArrowWood").Sum(i => i.m_stack);
            return "archerReady=" + Ready + "; equipmentFault=" + fault + "; woodArrows=" + arrows + "; blocking=" + blocking;
        }

        internal static void InstallPatches(Harmony harmony)
        {
            if (DrawTime == null || Blocking == null || BlockTimer == null || CurrentBlocker == null || Look == null || DoAttack == null || Copy == null)
                throw new MissingMemberException("Native party equipment members changed");
            var select = typeof(MonsterAI).GetMethod("SelectBestAttack", Hidden, null, new[] { typeof(Humanoid), typeof(float) }, null);
            var block = typeof(Humanoid).GetMethod("BlockAttack", Hidden, null, new[] { typeof(HitData), typeof(Character) }, null);
            if (select == null || block == null) throw new MissingMethodException("Native party combat boundary changed");
            harmony.Patch(select, prefix: new HarmonyMethod(typeof(HumanPartyEquipment), "BeforeSelect"));
            harmony.Patch(select, postfix: new HarmonyMethod(typeof(HumanPartyEquipment), "AfterSelect"));
            harmony.Patch(DoAttack,
                prefix: new HarmonyMethod(typeof(HumanPartyEquipment), "BeforeBowRelease"),
                postfix: new HarmonyMethod(typeof(HumanPartyEquipment), "AfterBowRelease"));
            harmony.Patch(block,
                prefix: new HarmonyMethod(typeof(HumanPartyEquipment), "BeforeNativeBlock"),
                postfix: new HarmonyMethod(typeof(HumanPartyEquipment), "AfterNativeBlock"));
        }

        void Cache()
        {
            if (body == null) body = GetComponent<Humanoid>();
            if (view == null) view = GetComponent<ZNetView>();
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (companion == null) companion = GetComponent<HumanCompanion>();
        }

        public void PrepareArcher()
        {
            Cache();
            LivingWorld.ManualTest.ManualTestPlugin.GuardWorld();
            if (view == null || !view.IsValid() || !view.IsOwner() || body.IsDead() ||
                !GetComponent<HumanAppearance>().Initialized || companion.HasAuthorizedCombat)
                throw new InvalidOperationException("Prepare requires a ready, owned, peaceful scout");
            string record = view.GetZDO().GetString(RecordKey, "");
            if (record != "") { if (record != "archer") throw new InvalidOperationException("Unknown party role preserved"); return; }
            // Resolve every native asset before committing a role or inventory.
            var bowPrefab = NativeItem("Bow");
            var ammoPrefab = NativeItem("ArrowWood");
            var inventory = body.GetInventory();
            if (!inventory.CanAddItem(bowPrefab, 1) || !inventory.CanAddItem(ammoPrefab, 60))
                throw new InvalidOperationException("Scout inventory lacks room for bow and 60 arrows");
            var before = new ZPackage(); inventory.Save(before);
            try
            {
                if (!inventory.AddItem(bowPrefab, 1) || !inventory.AddItem(ammoPrefab, 60))
                    throw new InvalidOperationException("Native equipment insertion refused");
                ValidateItems();
                SaveInventory();
                view.GetZDO().Set(RecordKey, "archer");
                restored = true; Subscribe();
                Log("archer prepared; 60 finite native arrows; repeated prepare never refills");
            }
            catch { RestoreInventory(new ZPackage(before.GetArray())); throw; }
        }

        static GameObject NativeItem(string name)
        {
            var prefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(name);
            if (prefab == null || prefab.GetComponent<ItemDrop>() == null)
                throw new InvalidOperationException("Required runtime item unavailable: " + name);
            return prefab;
        }

        ItemDrop.ItemData Find(string prefab)
        {
            return body.GetInventory().GetAllItems().FirstOrDefault(i => i.m_dropPrefab != null && i.m_dropPrefab.name == prefab);
        }

        void ValidateItems()
        {
            bow = Find("Bow"); sword = Find("SwordBronze"); shield = Find("ShieldWood");
            if (bow == null || sword == null || shield == null || !bow.m_shared.m_attack.m_bowDraw ||
                String.IsNullOrEmpty(bow.m_shared.m_attack.m_drawAnimationState) || bow.m_shared.m_attack.m_drawDurationMin <= 0)
                throw new InvalidOperationException("Native archer inventory/animation contract is incomplete");
            // AI range is adapter tuning on this actor's item, never global Bow data.
            bow.m_shared = (ItemDrop.ItemData.SharedData)Copy.Invoke(bow.m_shared, null);
            bow.m_shared.m_aiAttackRange = CompanionPolicy.AwarenessRange;
            bow.m_shared.m_aiAttackRangeMin = 6f;
            bow.m_shared.m_aiAttackMaxAngle = 10f;
            drawAnimation = bow.m_shared.m_attack.m_drawAnimationState;
            ai.m_viewRange = CompanionPolicy.AwarenessRange;
        }

        void Subscribe()
        {
            if (subscribed) return;
            body.GetInventory().m_onChanged += SaveInventory; subscribed = true;
        }
        void RestoreInventory(ZPackage package)
        {
            body.UnequipAllItems(); body.GetInventory().Load(package);
            foreach (var item in body.GetInventory().GetAllItems().ToArray())
                if (item.m_equipped) { item.m_equipped = false; body.EquipItem(item, false); }
        }

        void SaveInventory()
        {
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            var package = new ZPackage(); body.GetInventory().Save(package);
            view.GetZDO().Set(InventoryKey, package.GetBase64());
        }

        void Update()
        {
            try
            {
                Cache();
                if (view == null || !view.IsValid() || !view.IsOwner())
                {
                    restored = false;
                    if (subscribed && body != null) { body.GetInventory().m_onChanged -= SaveInventory; subscribed = false; }
                    return;
                }
                if (fault || body == null || body.IsDead()) { ReleaseControls(); return; }
                if (!restored)
                {
                    if (view.GetZDO().GetString(RecordKey, "") != "archer" || !GetComponent<HumanAppearance>().Initialized) return;
                    string saved = view.GetZDO().GetString(InventoryKey, "");
                    if (saved.Length == 0 || saved.Length > 65536) throw new InvalidOperationException("Missing/invalid party inventory; refusing refill");
                    RestoreInventory(new ZPackage(saved));
                    ValidateItems(); restored = true; Subscribe();
                }
                if (!Ready) return;
                var posture = GetComponent<HumanCompanionPosture>();
                if (posture != null && (posture.OwnsAmbientHands || posture.HasAmbientPose)) return;
                var target = companion.TargetCreature;
                if (target == null || target.IsDead()) { ReleaseControls(); return; }
                var readiness = GetComponent<HumanEquipmentReadiness>();
                if (readiness == null || !readiness.EnsureCombatReady()) return;
                if (ripostePending) { TryRiposte(); return; }
                ObserveBlock(target);
                AdvanceDraw(target, Mathf.Clamp(Time.deltaTime, 0f, .1f));
            }
            catch (Exception error) { fault = true; ReleaseControls(); Log("equipment fail-closed: " + error.Message); }
        }

        bool HasAmmo() { return body.GetInventory().GetAllItems().Any(i => i.m_shared.m_ammoType == bow.m_shared.m_ammoType && i != bow && i.m_stack > 0); }

        void Equip(bool ranged)
        {
            if (body.InAttack()) return;
            var desired = ranged ? bow : sword;
            bool coherent = ranged
                ? body.GetCurrentWeapon() == bow && bow.m_equipped && !shield.m_equipped && !sword.m_equipped
                : body.GetCurrentWeapon() == sword && sword.m_equipped && shield.m_equipped && !bow.m_equipped;
            if (coherent) return;
            ResetDraw();
            if (ranged)
            {
                SetBlock(false);
                Unequip(sword); Unequip(shield);
                if (!bow.m_equipped && !body.EquipItem(bow, true)) throw new InvalidOperationException("Native bow equip refused");
                if (body.GetCurrentWeapon() != bow || shield.m_equipped || sword.m_equipped)
                    throw new InvalidOperationException("Bow equipment state incoherent; refusing mixed-hand attack");
            }
            else
            {
                Unequip(bow);
                if (!sword.m_equipped && !body.EquipItem(sword, true)) throw new InvalidOperationException("Native sword equip refused");
                if (!shield.m_equipped && !body.EquipItem(shield, true)) throw new InvalidOperationException("Native shield equip refused");
                if (body.GetCurrentWeapon() != sword || !shield.m_equipped || bow.m_equipped)
                    throw new InvalidOperationException("Sword/shield equipment state incoherent");
            }
            if (body.GetCurrentWeapon() == desired)
            {
                var accepted = ranged ? PartyWeapon.Bow : PartyWeapon.SwordShield;
                if (!hasWeaponCommitment || committedWeapon != accepted)
                {
                    committedWeapon = accepted; hasWeaponCommitment = true;
                    weaponCommittedUntil = Time.time + 4f;
                }
                Log(ranged ? "native AI selected bow; hand state aligned" : "native AI selected sword; shield state aligned");
            }
        }

        void Unequip(ItemDrop.ItemData item)
        {
            if (item != null && item.m_equipped) body.UnequipItem(item, true);
        }

        bool NearbyThreat()
        {
            foreach (var character in Character.GetAllCharacters())
            {
                if (character == null || character.IsDead() || Vector3.Distance(transform.position, character.transform.position) > 6f) continue;
                var hostile = character.GetComponent<MonsterAI>();
                if (hostile != null && hostile.GetTargetCreature() == body && (ai.CanSeeTarget(character) || ai.CanHearTarget(character))) return true;
            }
            return false;
        }

        internal bool LaneClear(Character target)
        {
            if (target == null || !ai.CanSeeTarget(target)) return false;
            Vector3 start = body.GetCenterPoint(), end = target.GetCenterPoint();
            Vector3 line = end - start;
            if (line.sqrMagnitude < .01f) return false;
            foreach (var other in Character.GetAllCharacters())
            {
                if (other == null || other == body || other == target || other.IsDead()) continue;
                if (!other.IsPlayer() && other.GetComponent<HumanCompanion>() == null && !other.IsTamed()) continue;
                float along = Vector3.Dot(other.GetCenterPoint() - start, line) / line.sqrMagnitude;
                if (along > 0 && along < 1 && Vector3.Distance(other.GetCenterPoint(), start + line * along) < other.GetRadius() + .8f) return false;
            }
            return true;
        }

        void ObserveBlock(Character target)
        {
            bool perceived = ai.CanSeeTarget(target);
            bool attacking = perceived && target.InAttack();
            if (target != cueTarget) { cueTarget = target; sawAttack = false; attemptedBlock = false; SetBlock(false); }
            if (attacking && !sawAttack) cueAt = Time.time;
            bool facing = Vector3.Dot(transform.forward, (target.transform.position - transform.position).normalized) >= .5f;
            bool react = attacking && PartyCombatPolicy.ReactToAttack(perceived, facing, true,
                body.HaveStamina(10f), body.InAttack() || body.IsStaggering(), Vector3.Distance(transform.position, target.transform.position), Time.time - cueAt, 0f);
            if (react && !attemptedBlock)
            {
                attemptedBlock = true;
                Equip(false);
                if (shield.m_equipped)
                {
                    blockTarget = target; blockExpires = Time.time + .8f; SetBlock(true);
                    Log("shield response committed to observed close attack");
                }
            }
            if (blocking && ((!attacking && Time.time - cueAt >= .08f) || Time.time >= blockExpires))
            {
                SetBlock(false); blockTarget = null;
                Log(attacking ? "shield window expired without confirmed impact" : "shield released when attack ended without impact");
            }
            sawAttack = attacking;
            if (!attacking) attemptedBlock = false;
        }

        void SetBlock(bool value)
        {
            if (body == null || view == null || !view.IsValid() || !view.IsOwner()) return;
            Blocking.SetValue(body, value);
            if (value != blocking) { blocking = value; Log(value ? "shield raised after observed attack cue" : "shield released"); }
        }

        void AdvanceDraw(Character target, float dt)
        {
            if (body.GetCurrentWeapon() != bow || body.InAttack() || body.IsStaggering() || blocking || !HasAmmo() || !LaneClear(target) || !body.HaveStamina(1f))
            { ResetDraw(); return; }
            Look.Invoke(ai, new object[] { target.GetCenterPoint() });
            Vector3 direction = target.GetCenterPoint() - body.GetCenterPoint();
            direction.y = 0f;
            Vector3 facing = transform.forward; facing.y = 0f;
            // LookAt requests a turn; it does not complete that turn in this
            // frame. Never invoke the native release until the torso has
            // actually entered the bow's ten-degree firing cone.
            if (direction.sqrMagnitude < .01f || facing.sqrMagnitude < .01f ||
                Vector3.Dot(facing.normalized, direction.normalized) < Mathf.Cos(10f * Mathf.Deg2Rad))
            { ResetDraw(); return; }
            bool started = (bool)DoAttack.Invoke(ai, new object[] { target, false });
            if (started) Log("native MonsterAI bow attack started");
            else if (Time.time >= nextBowReleaseReport)
            {
                nextBowReleaseReport = Time.time + .5f;
                Log("bow waiting for native attack-ready gate");
            }
        }

        void ResetDraw()
        {
            if (body == null || view == null || !view.IsValid() || !view.IsOwner()) return;
            DrawTime.SetValue(body, 0f);
            var animation = GetComponent<ZSyncAnimation>();
            if (animation != null && !String.IsNullOrEmpty(drawAnimation)) animation.SetBool(drawAnimation, false);
        }

        internal bool AllowsWeapon(ItemDrop.ItemData item) { return Ready && (item == bow || item == sword); }
        internal bool MayAttack(Character target, bool release)
        {
            Cache();
            if (view != null && view.IsValid() && view.GetZDO().GetString(RecordKey, "") == "") return true;
            if (!Ready || blocking) return false;
            if (body.GetCurrentWeapon() != bow) return true;
            return LaneClear(target) && HasAmmo();
        }
        void OnBlockResolved(Character attacker, bool timedWindow)
        {
            if (!blocking) return;
            SetBlock(false); blockTarget = null;
            if (attacker == null || attacker.IsDead() || !companion.HasAuthorizedCombat || companion.TargetCreature != attacker)
            { Log("block succeeded; no legal counterattack target"); return; }
            riposteTarget = attacker; riposteAt = Time.time; riposteExpires = Time.time + 1.2f; nextRiposteReport = 0f; ripostePending = true;
            Log(timedWindow ? "timed block confirmed; immediate riposte queued" : "block confirmed; immediate return strike queued");
        }

        void TryRiposte()
        {
            SetBlock(false);
            if (riposteTarget == null || riposteTarget.IsDead() || !companion.HasAuthorizedCombat || companion.TargetCreature != riposteTarget ||
                Vector3.Distance(transform.position, riposteTarget.transform.position) > 4.5f || Time.time >= riposteExpires)
            { Log("riposte cancelled because target or timing was no longer valid"); ClearRiposte(); return; }
            if (Time.time < riposteAt || body.InAttack() || body.IsStaggering()) return;
            Equip(false);
            Look.Invoke(ai, new object[] { riposteTarget.GetCenterPoint() });
            bool started = (bool)DoAttack.Invoke(ai, new object[] { riposteTarget, false });
            if (started)
            {
                Log("native MonsterAI return strike started after confirmed block");
                ClearRiposte();
            }
            else if (Time.time >= nextRiposteReport)
            {
                nextRiposteReport = Time.time + .35f;
                Log("return strike waiting for native attack-ready gate");
            }
        }

        void ClearRiposte() { ripostePending = false; riposteTarget = null; }
        void ReleaseControls() { SetBlock(false); ResetDraw(); sawAttack = false; attemptedBlock = false; cueTarget = null; blockTarget = null; ClearRiposte(); }
        void OnDisable() { ReleaseControls(); if (subscribed && body != null) { body.GetInventory().m_onChanged -= SaveInventory; subscribed = false; } }
        void Log(string text) { Debug.Log("[LWC party] " + gameObject.name + ": " + text); }

        static bool BeforeSelect(MonsterAI __instance, ref ItemDrop.ItemData __result)
        {
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null || !equipment.Ready) return true;
            // Preserve Valheim's native EquipBestWeapon selection, as used by
            // RRR NPCs. Only make an empty bow ineligible for this actor.
            if (!equipment.HasAmmo()) equipment.bow.m_shared.m_aiAttackRangeMin = Single.MaxValue;
            else equipment.bow.m_shared.m_aiAttackRangeMin = 6f;
            return true;
        }

        static void AfterSelect(MonsterAI __instance, ref ItemDrop.ItemData __result)
        {
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null || !equipment.Ready || equipment.body.InAttack() || equipment.blocking) return;
            var target = equipment.companion.TargetCreature;
            if (target == null) return;
            float distance = Vector3.Distance(equipment.transform.position, target.transform.position);
            bool immediate = equipment.NearbyThreat();
            PartyWeapon desired = PartyCombatPolicy.Weapon(equipment.body.GetCurrentWeapon() == equipment.bow,
                equipment.HasAmmo(), equipment.LaneClear(target), immediate, distance, 0f, 2f);
            if (equipment.hasWeaponCommitment && Time.time < equipment.weaponCommittedUntil && !immediate)
                desired = equipment.committedWeapon;
            __result = desired == PartyWeapon.Bow ? equipment.bow : equipment.sword;
            equipment.Equip(desired == PartyWeapon.Bow);
        }

        [HarmonyPriority(Priority.Last)]
        static void BeforeBowRelease(MonsterAI __instance, Character __0, out bool __state)
        {
            __state = false;
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null || !equipment.Ready || equipment.bow == null) return;
            var attack = equipment.bow.m_shared == null ? null : equipment.bow.m_shared.m_attack;
            bool bridge = PartyCombatPolicy.BowReleaseBridge(equipment.Ready,
                equipment.companion != null && equipment.companion.HasAuthorizedCombat &&
                    equipment.companion.TargetCreature == __0,
                equipment.body.GetCurrentWeapon() == equipment.bow && attack != null && attack.m_bowDraw,
                true,
                equipment.LaneClear(__0), equipment.HasAmmo());
            if (!bridge) return;
            // Proven native-NPC bridge: StartAttack must see an ordinary attack,
            // while the shared per-actor bow setting is restored immediately in
            // the postfix. Native Attack still owns ammo and projectile creation.
            attack.m_bowDraw = false;
            __state = true;
            equipment.Log("bow release committed through native StartAttack");
        }

        static void AfterBowRelease(MonsterAI __instance, bool __state)
        {
            if (!__state) return;
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment != null && equipment.bow != null && equipment.bow.m_shared != null &&
                equipment.bow.m_shared.m_attack != null)
                equipment.bow.m_shared.m_attack.m_bowDraw = true;
        }

        static void BeforeNativeBlock(Humanoid __instance, Character __1, out bool __state)
        {
            __state = false;
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null || !equipment.Ready || !equipment.blocking) return;
            var blocker = CurrentBlocker.Invoke(__instance, null) as ItemDrop.ItemData;
            float timer = (float)BlockTimer.GetValue(__instance);
            __state = blocker != null && blocker.m_shared.m_timedBlockBonus > 1f && timer >= 0f && timer < .25f;
        }

        static void AfterNativeBlock(Humanoid __instance, Character __1, bool __state, bool __result)
        {
            if (!__result) return;
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment != null && equipment.Ready) equipment.OnBlockResolved(__1, __state);
        }
    }
}

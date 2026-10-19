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
        static readonly MethodInfo DoAttack = typeof(MonsterAI).GetMethod("DoAttack", Hidden, null, new[] { typeof(Character), typeof(bool) }, null);
        static readonly MethodInfo Copy = typeof(object).GetMethod("MemberwiseClone", Hidden);
        Humanoid body;
        MonsterAI ai;
        ZNetView view;
        HumanCompanion companion;
        ItemDrop.ItemData bow, sword, shield;
        float nextShieldReport;
        bool restored, fault, subscribed;
        public bool Ready { get { return isActiveAndEnabled && restored && !fault && view != null && view.IsValid() && view.IsOwner() && !body.IsDead(); } }
        public string Describe()
        {
            Cache();
            int arrows = body == null ? 0 : body.GetInventory().GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == "ArrowWood").Sum(i => i.m_stack);
            return "archerReady=" + Ready + "; equipmentFault=" + fault + "; woodArrows=" + arrows + "; nativeCombatOwner=true";
        }

        internal static void InstallPatches(Harmony harmony)
        {
            if (DoAttack == null || Copy == null)
                throw new MissingMemberException("Native party equipment members changed");
            var select = typeof(MonsterAI).GetMethod("SelectBestAttack", Hidden, null, new[] { typeof(Humanoid), typeof(float) }, null);
            if (select == null) throw new MissingMethodException("Native party combat boundary changed");
            // Native MonsterAI owns weapon selection during authorized combat.
            // Outside combat, returning no attack item is what permits its own
            // UpdateAI to reach Follow even when readiness is only visual.
            harmony.Patch(select,
                prefix: new HarmonyMethod(typeof(HumanPartyEquipment), "BeforeSelect"),
                postfix: new HarmonyMethod(typeof(HumanPartyEquipment), "RejectUtilitySelection"));
            harmony.Patch(DoAttack,
                prefix: new HarmonyMethod(typeof(HumanPartyEquipment), "BeforeBowRelease"),
                postfix: new HarmonyMethod(typeof(HumanPartyEquipment), "AfterBowRelease"));
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
                if (fault || body == null || body.IsDead()) return;
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
                if (target == null || target.IsDead()) return;
                var readiness = GetComponent<HumanEquipmentReadiness>();
                if (readiness == null || !readiness.EnsureCombatReady()) return;
                // Combat is intentionally hands-off here.  MonsterAI already
                // owns movement, facing, range choice and attack cadence using
                // the Souls of the Land values configured on this actor.  The
                // old parallel controller called LookAt/DoAttack and ran its own
                // block/riposte state machine, which could stall native combat.
            }
            catch (Exception error) { fault = true; Log("equipment fail-closed: " + error.Message); }
        }

        bool HasAmmo() { return body.GetInventory().GetAllItems().Any(i => i.m_shared.m_ammoType == bow.m_shared.m_ammoType && i != bow && i.m_stack > 0); }

        internal bool AllowsWeapon(ItemDrop.ItemData item) { return Ready && (item == bow || item == sword); }
        internal bool MayAttack(Character target, bool release)
        {
            Cache();
            if (view != null && view.IsValid() && view.GetZDO().GetString(RecordKey, "") == "") return true;
            if (!Ready) return false;
            if (body.GetCurrentWeapon() != bow) return true;
            return HasAmmo();
        }
        void OnDisable() { if (subscribed && body != null) { body.GetInventory().m_onChanged -= SaveInventory; subscribed = false; } }
        void Log(string text) { Debug.Log("[LWC party] " + gameObject.name + ": " + text); }

        static bool BeforeSelect(MonsterAI __instance, ref ItemDrop.ItemData __result)
        {
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null) return true;
            equipment.Cache();
            if (equipment.companion != null && !equipment.companion.HasAuthorizedCombat)
            {
                __result = null;
                return false;
            }
            if (!equipment.Ready) return true;
            // Preserve Valheim's native EquipBestWeapon selection, as used by
            // RRR NPCs. Only make an empty bow ineligible for this actor.
            if (!equipment.HasAmmo()) equipment.bow.m_shared.m_aiAttackRangeMin = Single.MaxValue;
            else equipment.bow.m_shared.m_aiAttackRangeMin = 6f;
            return true;
        }

        static void RejectUtilitySelection(MonsterAI __instance, ref ItemDrop.ItemData __result)
        {
            // Torches are traversal equipment. Even if native inventory timing
            // exposes one during the combat handoff, it cannot become an attack.
            var equipment = __instance.GetComponent<HumanPartyEquipment>();
            if (equipment == null) return;
            equipment.Cache();
            if (HumanUtilityTorch.IsUtilityTorch(__result)) { __result = null; return; }
            equipment.PairSelectedShield(__result);
        }

        void PairSelectedShield(ItemDrop.ItemData selected)
        {
            if (body == null || companion == null || selected == null || selected.m_shared == null) return;
            var available = body.GetInventory().GetAllItems().FirstOrDefault(item => item != null && item.m_shared != null &&
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield);
            bool pair = EquipmentPresentationPolicy.PairShield(companion.HasAuthorizedCombat,
                selected.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon,
                available != null, available != null && available.m_equipped);
            if (!pair) return;
            bool equipped = body.EquipItem(available, true);
            if (equipped && available.m_equipped) return;
            if (Time.time < nextShieldReport) return;
            nextShieldReport = Time.time + 2f;
            Log("native one-handed selection could not pair the owned shield; defense remains unavailable");
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
                true, equipment.HasAmmo());
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

    }
}

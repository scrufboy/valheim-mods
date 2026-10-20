// Compiles and executes the actual HumanIdentity AND HumanAppearance adapters
// against managed API doubles. This does not execute Unity or native rendering.
using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace UnityEngine
{
    public class Object { }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject == null ? null : gameObject.transform; } }
        public T GetComponentInParent<T>() where T : class { return gameObject == null ? null : gameObject.Get<T>(); }
    }
    public class GameObject
    {
        public readonly List<MonoBehaviour> components = new List<MonoBehaviour>();
        public readonly Transform transform;
        public bool activeInHierarchy = true;
        public GameObject() { transform = new Transform(); transform.gameObject = this; }
        public T Add<T>() where T : MonoBehaviour, new() { var value = new T(); value.gameObject = this; components.Add(value); return value; }
        public T Get<T>() where T : class { foreach (var value in components) if (value is T) return value as T; return null; }
        public T GetComponent<T>() where T : class { return Get<T>(); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class
        {
            var value = Get<T>(); return value == null ? new T[0] : new[] { value };
        }
    }
    public class Transform : Component
    {
        readonly HashSet<string> children = new HashSet<string>();
        public Transform parent;
        public void Add(string name) { children.Add(name); }
        public Transform Find(string name) { return children.Contains(name) ? new Transform() : null; }
        public bool IsChildOf(Transform parent) { return true; }
    }
    public class MonoBehaviour : Component
    {
        public bool enabled = true;
        public T GetComponent<T>() where T : class { return gameObject.Get<T>(); }
    }
    public class Material : Object { }
    public class Mesh : Object { }
    public class Renderer : MonoBehaviour
    {
        public Material[] sharedMaterials = { new Material() };
    }
    public class SkinnedMeshRenderer : Renderer
    {
        public Mesh sharedMesh = new Mesh();
        public Transform rootBone = new Transform();
    }
    public struct LOD { public Renderer[] renderers; }
    public class LODGroup : MonoBehaviour
    {
        public readonly List<Renderer> renderers = new List<Renderer>();
        public LOD[] GetLODs() { return new[] { new LOD { renderers = renderers.ToArray() } }; }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
    }
    public static class Time { public static float realtimeSinceStartup; }
}
public class ZDO
{
    public readonly Dictionary<string, string> data = new Dictionary<string, string>();
    public readonly List<string> writes = new List<string>();
    public int prefab = HumanRigPolicy.PrefabId.GetStableHashCode();
    public int GetPrefab() { return prefab; }
    public string GetString(string key, string fallback) { string value; return data.TryGetValue(key, out value) ? value : fallback; }
    public void Set(string key, string value) { data[key] = value; writes.Add(key); }
}
public class ZNetView : MonoBehaviour
{
    public bool owner = true, valid = true;
    public ZDO data = new ZDO();
    public bool IsOwner() { return owner; }
    public bool IsValid() { return valid; }
    public ZDO GetZDO() { return data; }
}
public class Shared { public string m_name = "Sword"; }
public class Item { public bool m_equipped = true; public Shared m_shared = new Shared(); }
public class Inventory
{
    public List<Item> items = new List<Item> { new Item() };
    public List<Item> GetAllItems() { return items; }
}
public class Humanoid : MonoBehaviour
{
    public string m_name = "Original Scout";
    public int equips;
    public Inventory inventory = new Inventory();
    public Inventory GetInventory() { return inventory; }
    public Item GetCurrentWeapon() { return inventory.items.Count == 0 ? null : inventory.items[0]; }
    public void EquipBestWeapon(object a, object b, object c, object d) { ++equips; }
    public bool IsPlayer() { return this is Player; }
}
public class Player : Humanoid { }
public class MonsterAI : MonoBehaviour { }
public class ItemDrop : MonoBehaviour
{
    public ItemData m_itemData = new ItemData();
    public class ItemData
    {
        public SharedData m_shared = new SharedData();
        public enum ItemType { None, Customization }
        public class SharedData { public ItemType m_itemType = ItemType.Customization; }
    }
}
public class VisEquipment : MonoBehaviour
{
    public class PlayerModel { public object m_mesh = new object(), m_baseMaterial = new object(); }
    public bool m_isPlayer = true;
    public Player m_playerComponent;
    public PlayerModel[] m_models = { new PlayerModel(), new PlayerModel() };
    public int writes, refreshes, model = -1, hair = -1, beard = -1, loseOwnerAfter;
    public SkinnedMeshRenderer m_bodyModel;
    public GameObject m_hairItemInstance, m_beardItemInstance;
    public int m_currentHairItemHash, m_currentBeardItemHash;
    public Vector3 skin, tint;
    void Write() { ++writes; if (loseOwnerAfter == writes) GetComponent<ZNetView>().owner = false; }
    public void SetModel(int value) { Write(); model = value; }
    public void SetSkinColor(Vector3 value) { Write(); skin = value; }
    public void SetHairColor(Vector3 value) { Write(); tint = value; }
    void BuildAttachment(ref GameObject instance, int value)
    {
        if (value == 0) { instance = null; return; }
        instance = new GameObject(); var renderer = instance.Add<SkinnedMeshRenderer>();
        instance.transform.parent = gameObject.transform;
        renderer.rootBone.gameObject = gameObject;
        var lod = gameObject.Get<LODGroup>(); if (lod != null) lod.renderers.Add(renderer);
    }
    public void SetHairItem(int value) { Write(); hair = value; m_currentHairItemHash = value; BuildAttachment(ref m_hairItemInstance, value); }
    public void SetBeardItem(int value) { Write(); beard = value; m_currentBeardItemHash = value; BuildAttachment(ref m_beardItemInstance, value); }
    void UpdateVisuals() { ++refreshes; }
}
public class ZNetScene
{
    public static ZNetScene instance = new ZNetScene();
    public bool assets = true;
    public Action onLookup;
    public GameObject GetPrefab(string name) { if (onLookup != null) onLookup(); return assets ? new GameObject() : null; }
}
public class ObjectDB
{
    public static ObjectDB instance = new ObjectDB();
    public bool assets = true;
    public Action onLookup;
    public GameObject GetItemPrefab(string name)
    {
        if (onLookup != null) onLookup();
        if (!assets) return null;
        var prefab = new GameObject(); prefab.Add<ItemDrop>(); prefab.transform.Add("attach"); return prefab;
    }
}
public static class Hash
{
    public static int GetStableHashCode(this string value) { unchecked { int hash = 7; foreach (char c in value) hash = hash * 31 + c; return hash; } }
}
namespace LivingWorld.HumanNpc
{
    public class HumanCompositionIdentity : MonoBehaviour { public bool IsCompositionReady = true; public string Status = "Test composition"; }
    public static class NativeHumanFactory { public static void Validate(GameObject root, bool live) { } }
}
namespace LivingWorld.ManualTest
{
    public class ManualTestPlugin
    {
        public static ManualTestPlugin Instance;
        public void Report(string value) { }
    }
}
static class IdentityAdapterHarness
{
    const string Seed = "0123456789abcdef0123456789abcdef";
    static readonly string[] HairChoices = { "Hair1", "Hair2", "Hair3", "Hair4" };
    static readonly string[] BeardChoices = { "", "Beard1", "Beard2", "Beard3" };
    static int checks;
    static void Check(bool condition, string label) { ++checks; if (!condition) throw new Exception(label); }
    static void Call(object instance, string method)
    { instance.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(instance, null); }
    static void Refuse(Action action, string label)
    { bool refused = false; try { action(); } catch (InvalidOperationException) { refused = true; } Check(refused, label); }
    static GameObject Actor()
    {
        ZNetScene.instance = new ZNetScene(); ObjectDB.instance = new ObjectDB(); Time.realtimeSinceStartup = 0;
        var actor = new GameObject(); var view = actor.Add<ZNetView>();
        view.data.data[HumanMigrationPolicy.OriginKey] = HumanMigrationPolicy.FreshOrigin;
        view.data.data[HumanMigrationPolicy.CompositionKey] = HumanMigrationPolicy.CompositionRevision;
        view.data.data[HumanProfile.RequiredKey] = "1";
        actor.Add<Humanoid>(); actor.Add<MonsterAI>(); var vis = actor.Add<VisEquipment>();
        vis.m_bodyModel = actor.Add<SkinnedMeshRenderer>(); vis.m_bodyModel.transform.parent = actor.transform; actor.Add<LODGroup>();
        actor.Add<HumanCompositionIdentity>(); actor.Add<HumanIdentity>();
        var appearance = actor.Add<HumanAppearance>(); appearance.RequiredItems = new[] { "Sword" }; appearance.WeaponName = "Sword";
        Call(appearance, "Start"); return actor;
    }
    static void Tick(GameObject actor) { Call(actor.Get<HumanIdentity>(), "Update"); Call(actor.Get<HumanAppearance>(), "Update"); }
    public static int Main()
    {
        try
        {
            HumanProfile profile;
            var actor = Actor(); var id = actor.Get<HumanIdentity>(); var view = actor.Get<ZNetView>(); var appearance = actor.Get<HumanAppearance>(); var vis = actor.Get<VisEquipment>();
            Check(id.Read(out profile) == HumanProfileState.Refused && profile == null, "required fresh profile absent remains gated");
            Call(id, "Update"); Check(view.data.writes.Count == 0, "identity lifecycle cannot create missing record");
            id.InitializeFresh(Seed, HairChoices, BeardChoices);
            string saved = view.data.GetString(HumanProfile.RecordKey, "");
            Check(view.data.writes.Count == 1 && view.data.writes[0] == HumanProfile.RecordKey, "one explicit identity record write");
            Check(id.Read(out profile) == HumanProfileState.Ready && profile.Seed == Seed && profile.InitialRole == "unassigned", "created profile readable");
            id.InitializeFresh("invalid new seed ignored", HairChoices, BeardChoices);
            Check(view.data.writes.Count == 1 && view.data.GetString(HumanProfile.RecordKey, "") == saved, "idempotence before visual commit ignores seed without reroll");
            Tick(actor);
            Check(appearance.Initialized && vis.writes == 5 && vis.refreshes == 2, "actual appearance adapter refreshes the materialized native profile twice");
            Check(vis.model == (int)profile.Sex && vis.hair == profile.Hair.GetStableHashCode() && vis.beard == (profile.Beard == "" ? 0 : profile.Beard.GetStableHashCode()), "profile model/hair/beard used");
            Check(Math.Abs(vis.skin.x - profile.SkinR / 255f) < .00001 && Math.Abs(vis.tint.z - profile.HairB / 255f) < .00001, "persisted colors used");
            Check(actor.Get<Humanoid>().m_name == profile.Name, "stable name hydrated");
            id.InitializeFresh("another ignored seed", HairChoices, BeardChoices); Tick(actor);
            Check(view.data.writes.Count == 2 && vis.writes == 5 && vis.refreshes == 2, "after commit repeated init/tick has no persistent or visual writes");

            string femaleSeed = null;
            for (int i = 0; i < 100; ++i)
            {
                string candidate = i.ToString("x32", System.Globalization.CultureInfo.InvariantCulture);
                if (HumanProfile.Generate(candidate, HairChoices, BeardChoices).Sex == HumanSex.Female) { femaleSeed = candidate; break; }
            }
            Check(femaleSeed != null, "female seed available");
            var femaleActor = Actor(); femaleActor.Get<HumanIdentity>().InitializeFresh(femaleSeed, HairChoices, BeardChoices); Tick(femaleActor);
            Check(femaleActor.Get<HumanAppearance>().Initialized && femaleActor.Get<VisEquipment>().model == 1 && femaleActor.Get<VisEquipment>().beard == 0,
                "actual adapter selects native female model without beard");

            actor = Actor(); view = actor.Get<ZNetView>(); vis = actor.Get<VisEquipment>();
            view.data.data[HumanProfile.RecordKey] = saved;
            view.data.data[HumanMigrationPolicy.VisualKey] = HumanRigPolicy.Revision;
            // A loaded native VisEquipment has already reconstructed its saved
            // customization instances before this adapter verifies them. Model
            // that replication without counting it as an adapter write.
            vis.SetHairItem(profile.Hair.GetStableHashCode());
            vis.SetBeardItem(profile.Beard == "" ? 0 : profile.Beard.GetStableHashCode());
            vis.writes = 0; vis.refreshes = 0;
            Tick(actor);
            Check(actor.Get<HumanAppearance>().Initialized && vis.writes == 0 && view.data.writes.Count == 0, "loaded profile never rerolls or rewrites existing appearance");
            Check(actor.Get<Humanoid>().m_name == profile.Name, "loaded name restored");

            actor = Actor(); view = actor.Get<ZNetView>(); vis = actor.Get<VisEquipment>();
            view.data.data.Remove(HumanProfile.RequiredKey);
            view.data.data[HumanMigrationPolicy.VisualKey] = HumanRigPolicy.Revision;
            view.data.data["livingworld_human_order"] = "saved follow order";
            Tick(actor);
            Check(actor.Get<HumanAppearance>().Initialized && vis.writes == 0 && view.data.writes.Count == 0 && actor.Get<Humanoid>().m_name == "Original Scout", "saved legacy appearance/name preserved");
            Check(view.data.GetString("livingworld_human_order", "") == "saved follow order", "legacy order remains untouched");
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "saved legacy cannot be randomized");

            actor = Actor(); actor.Get<ZNetView>().data.data.Remove(HumanProfile.RequiredKey); Tick(actor); vis = actor.Get<VisEquipment>();
            Check(vis.model == 0 && vis.hair == 0 && vis.beard == 0 && Math.Abs(vis.skin.x - .90f) < .00001,
                "unprofiled legacy path uses native empty customization, never a guessed asset");

            foreach (string malformed in new[] { "broken", "2" + saved.Substring(1), new String('x', 257) })
            {
                actor = Actor(); view = actor.Get<ZNetView>(); view.data.data[HumanProfile.RecordKey] = malformed;
                Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "corruption cannot be replaced");
                Tick(actor);
                Check(!actor.Get<HumanAppearance>().Initialized && !actor.Get<MonsterAI>().enabled && actor.Get<VisEquipment>().writes == 0,
                    "malformed profile fail closed before visual changes");
                Check(view.data.writes.Count == 0 && view.data.GetString(HumanProfile.RecordKey, "") == malformed, "malformed bytes preserved");
            }
            actor = Actor(); view = actor.Get<ZNetView>(); view.owner = false;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "remote cannot initialize");
            view.data.data[HumanProfile.RecordKey] = saved; Tick(actor);
            Check(view.data.writes.Count == 0 && actor.Get<VisEquipment>().writes == 0, "remote load no persistent/visual writes");

            actor = Actor(); view = actor.Get<ZNetView>();
            ObjectDB.instance.onLookup = () => view.owner = false;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "authority lost during asset lookup refuses commit");
            Check(view.data.writes.Count == 0, "authority loss writes zero records");

            actor = Actor(); view = actor.Get<ZNetView>(); actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices);
            vis = actor.Get<VisEquipment>(); vis.loseOwnerAfter = 1; Tick(actor);
            Check(vis.writes == 1 && view.data.GetString(HumanMigrationPolicy.VisualKey, "") == "", "appearance loses authority without subsequent writes/commit");
            view.owner = true; vis.loseOwnerAfter = 0; Tick(actor);
            Check(actor.Get<HumanAppearance>().Initialized && vis.writes == 6 && view.data.GetString(HumanProfile.RecordKey, "") == saved,
                "new authority completes same saved appearance without reroll");

            actor = Actor(); view = actor.Get<ZNetView>(); view.data.prefab = -1;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "foreign prefab refused"); Check(view.data.writes.Count == 0, "foreign prefab no writes");
            actor = Actor(); actor.Add<Player>(); Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "Player root refused");
            actor = Actor(); actor.Get<HumanCompositionIdentity>().IsCompositionReady = false;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "unknown composition refused");
            actor = Actor(); actor.Get<ZNetView>().data.data[HumanMigrationPolicy.OriginKey] = HumanMigrationPolicy.LegacyOrigin;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "migrated legacy origin refused");
            actor = Actor(); ObjectDB.instance.assets = false;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "unavailable hair assets refused");
            Check(actor.Get<ZNetView>().data.writes.Count == 0, "unavailable assets no partial identity");
            actor = Actor(); actor.Get<VisEquipment>().m_models = new VisEquipment.PlayerModel[0];
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "unavailable human models refused");

            actor = Actor(); id = actor.Get<HumanIdentity>(); view = actor.Get<ZNetView>();
            id.InitializeFresh(Seed, HairChoices, BeardChoices); Tick(actor);
            actor.Get<Humanoid>().m_name = "Display changed after hydration";
            for (int i = 0; i < 100; ++i) Call(id, "Update");
            Check(actor.Get<Humanoid>().m_name == "Display changed after hydration", "no per-frame display-name rewrite");
            Time.realtimeSinceStartup = .5f; Call(id, "Update");
            Check(actor.Get<Humanoid>().m_name == profile.Name, "hydration cadence repairs a role/display overwrite from immutable identity");
            view.data.data[HumanProfile.RecordKey] = "broken";
            view.owner = false; Call(id, "Update");
            Check(!actor.Get<HumanAppearance>().Initialized && view.data.writes.Count == 2, "owner boundary immediately detects bad saved data without persistence");

            actor = Actor(); id = actor.Get<HumanIdentity>(); view = actor.Get<ZNetView>();
            id.InitializeFresh(Seed, HairChoices, BeardChoices); Tick(actor);
            view.data.data.Remove(HumanProfile.RecordKey);
            Time.realtimeSinceStartup = .5f; Call(id, "Update");
            Check(id.Read(out profile) == HumanProfileState.Refused && !actor.Get<HumanAppearance>().Initialized,
                "vanished observed identity fails closed instead of becoming legacy");

            // A failed fresh spawn may remain in persistence while native
            // deletion is queued or authority unavailable. Reload sees the
            // requirement flag even though all runtime caches are new.
            actor = Actor(); view = actor.Get<ZNetView>(); ObjectDB.instance.assets = false;
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "failed fresh asset validation");
            var incompleteSave = new Dictionary<string, string>(view.data.data);
            actor = Actor(); view = actor.Get<ZNetView>();
            view.data.data.Clear(); foreach (var pair in incompleteSave) view.data.data.Add(pair.Key, pair.Value);
            Tick(actor);
            Check(!actor.Get<HumanAppearance>().Initialized && !actor.Get<MonsterAI>().enabled && actor.Get<VisEquipment>().writes == 0,
                "unconfirmed cleanup then reload cannot activate unprofiled fresh actor");
            Check(view.data.writes.Count == 0 && !view.data.data.ContainsKey(HumanProfile.RecordKey) && view.data.GetString(HumanProfile.RequiredKey, "") == "1",
                "pending fresh record preserved across reload without generation or appearance writes");
            actor = Actor(); view = actor.Get<ZNetView>(); view.data.data[HumanProfile.RequiredKey] = "2";
            Refuse(() => actor.Get<HumanIdentity>().InitializeFresh(Seed, HairChoices, BeardChoices), "unknown required-profile flag refused");
            Tick(actor); Check(!actor.Get<HumanAppearance>().Initialized && view.data.writes.Count == 0, "unknown requirement stays fail closed");
            Console.WriteLine("IdentityAdapterHarness PASS: " + checks + " assertions executing actual identity and appearance adapters with API doubles; Unity runtime NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

// Compiles with a mechanical extraction of the real ManualTestPlugin.Spawn.
// No Unity execution, profile installation or live save mutations occur.
using System;
using System.Collections.Generic;
using LivingWorld.HumanNpc;
using LivingWorld.ManualTest;
using UnityEngine;

static class SpawnFixture
{
    internal static readonly List<string> Events = new List<string>();
    internal static bool FailIdentity, LoseOwner, ThrowInstantiate, FailDestroy, ConfirmRemoval = true, ChangeScene;
    internal static GameObject Created;
    internal static void Reset()
    {
        Events.Clear(); FailIdentity = LoseOwner = ThrowInstantiate = FailDestroy = ChangeScene = false; ConfirmRemoval = true;
        Created = null; Character.Characters.Clear();
        ZNetScene.instance = new ZNetScene(); ZoneSystem.instance = new ZoneSystem(); ZDOMan.instance = new ZDOMan();
        ObjectDB.instance = new ObjectDB();
        Player.m_localPlayer = new GameObject().Add<Player>();
        ZoneSystem.instance.keys.Add("lab");
    }
    internal static GameObject Allocate()
    {
        Events.Add("instantiate");
        var actor = new GameObject { name = "Human(Clone)" }; Created = actor;
        actor.Add<Humanoid>(); actor.Add<MonsterAI>(); actor.Add<HumanAppearance>(); actor.Add<HumanCompanion>();
        actor.Add<HumanIdentity>(); actor.Add<HumanCompositionIdentity>();
        var view = actor.Add<ZNetView>();
        ZDOMan.instance.records.Add(view.data.m_uid, view.data); ZNetScene.instance.instances.Add(view.data.m_uid, actor);
        if (ThrowInstantiate) throw new InvalidOperationException("allocation failed after creation");
        return actor;
    }
}
namespace UnityEngine
{
    public class Object
    {
        public static GameObject Instantiate(GameObject source, Vector3 position, Quaternion rotation) { return SpawnFixture.Allocate(); }
    }
    public class GameObject : Object
    {
        public string name = "";
        public Transform transform = new Transform();
        readonly List<MonoBehaviour> components = new List<MonoBehaviour>();
        public T Add<T>() where T : MonoBehaviour, new() { var value = new T { gameObject = this }; components.Add(value); return value; }
        public T GetComponent<T>() where T : class { foreach (var value in components) if (value is T) return value as T; return null; }
    }
    public class MonoBehaviour : Object
    {
        public GameObject gameObject;
        public bool enabled = true;
        public Transform transform { get { return gameObject.transform; } }
        public string name { get { return gameObject.name; } }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public class Transform
    {
        public Vector3 position, forward; public Quaternion rotation;
        public Transform Find(string name) { return name == "attach" ? new Transform() : null; }
    }
    public struct Quaternion { }
    public struct Vector3
    {
        public static Vector3 operator +(Vector3 a, Vector3 b) { return a; }
        public static Vector3 operator *(Vector3 a, float b) { return a; }
    }
}
public struct ZDOID
{
    public int value;
    public static readonly ZDOID None = new ZDOID();
    public static bool operator ==(ZDOID a, ZDOID b) { return a.value == b.value; }
    public static bool operator !=(ZDOID a, ZDOID b) { return !(a == b); }
    public override bool Equals(object value) { return value is ZDOID && this == (ZDOID)value; }
    public override int GetHashCode() { return value; }
}
public class ZDO
{
    public ZDOID m_uid = new ZDOID { value = 17 };
    public Dictionary<string, string> fields = new Dictionary<string, string>();
    public void Set(string key, string value) { fields[key] = value; if (key == HumanProfile.RequiredKey) SpawnFixture.Events.Add("profile:required"); }
}
public class ZNetView : MonoBehaviour
{
    public bool owner = true, valid = true;
    public ZDO data = new ZDO();
    public bool IsValid() { return valid; }
    public bool IsOwner() { return owner; }
    public ZDO GetZDO() { return data; }
}
public class ZDOMan
{
    public static ZDOMan instance;
    public readonly Dictionary<ZDOID, ZDO> records = new Dictionary<ZDOID, ZDO>();
    public ZDO GetZDO(ZDOID id) { ZDO data; return records.TryGetValue(id, out data) ? data : null; }
}
public class ZNetScene
{
    public static ZNetScene instance;
    public GameObject prefab = new GameObject();
    public readonly Dictionary<ZDOID, GameObject> instances = new Dictionary<ZDOID, GameObject>();
    public readonly List<GameObject> destroyed = new List<GameObject>();
    public GameObject GetPrefab(string name) { return prefab; }
    public GameObject FindInstance(ZDOID id) { GameObject actor; return instances.TryGetValue(id, out actor) ? actor : null; }
    public void Destroy(GameObject actor)
    {
        SpawnFixture.Events.Add("destroy"); destroyed.Add(actor);
        if (SpawnFixture.FailDestroy) throw new InvalidOperationException("destroy failed");
        var view = actor.GetComponent<ZNetView>(); instances.Remove(view.data.m_uid); view.valid = false;
        if (SpawnFixture.ConfirmRemoval) ZDOMan.instance.records.Remove(view.data.m_uid);
    }
}
public class ZoneSystem
{
    public static ZoneSystem instance;
    public readonly HashSet<string> keys = new HashSet<string>();
    public bool GetGlobalKey(string key) { return keys.Contains(key); }
    public void SetGlobalKey(string key) { SpawnFixture.Events.Add("guard:set"); keys.Add(key); }
    public void RemoveGlobalKey(string key) { SpawnFixture.Events.Add("guard:remove"); keys.Remove(key); }
}
public class Character : MonoBehaviour
{
    public static readonly List<Character> Characters = new List<Character>();
    public static List<Character> GetAllCharacters() { return Characters; }
}
public class Humanoid : Character
{
    public void EquipBestWeapon(object a, object b, object c, object d) { SpawnFixture.Events.Add("equip"); }
}
public class Player : Humanoid { public static Player m_localPlayer; }
public class MonsterAI : MonoBehaviour { }
public class VisEquipment : MonoBehaviour { }
public class ItemDrop : MonoBehaviour
{
    public ItemData m_itemData = new ItemData();
    public class ItemData
    {
        public SharedData m_shared = new SharedData();
        public enum ItemType { Customization }
        public class SharedData { public ItemType m_itemType = ItemType.Customization; }
    }
}
public class PrefabManager
{
    public static PrefabManager Instance = new PrefabManager();
    public GameObject GetPrefab(string name)
    {
        var player = new GameObject { name = name };
        player.Add<VisEquipment>();
        return player;
    }
}
public class ObjectDB
{
    public static ObjectDB instance;
    public List<ItemDrop> GetAllItems(ItemDrop.ItemData.ItemType type, string search)
    {
        var item = new GameObject { name = search + "_Native" }.Add<ItemDrop>();
        return new List<ItemDrop> { item };
    }
    public GameObject GetItemPrefab(string name)
    {
        int value;
        string suffix = name.StartsWith("Hair") ? name.Substring(4) : name.StartsWith("Beard") ? name.Substring(5) : "";
        if (!Int32.TryParse(suffix, out value) || value < 1 || value > (name.StartsWith("Hair") ? 4 : 3)) return null;
        return new GameObject { name = name }.Add<ItemDrop>().gameObject;
    }
}
namespace LivingWorld.HumanNpc
{
    public static class HumanProfile { public const string RequiredKey = "livingworld_human_identity_required"; }
    public class HumanAppearance : MonoBehaviour { public bool Initialized; }
    public class HumanCompanion : MonoBehaviour { }
    public class HumanCompositionIdentity : MonoBehaviour
    { public void InitializeFresh(Player player) { SpawnFixture.Events.Add("composition"); } }
    public class HumanIdentity : MonoBehaviour
    {
        public void InitializeFresh(string seed, string[] availableHair, string[] availableBeards)
        {
            SpawnFixture.Events.Add("identity");
            if (SpawnFixture.LoseOwner) GetComponent<ZNetView>().owner = false;
            if (SpawnFixture.ChangeScene) ZNetScene.instance = new ZNetScene();
            if (SpawnFixture.FailIdentity || SpawnFixture.LoseOwner || SpawnFixture.ChangeScene)
                throw new InvalidOperationException("identity refused");
        }
    }
    public static class NativeHumanFactory { public static void Validate(GameObject actor, bool live) { SpawnFixture.Events.Add("validate"); } }
}
namespace LivingWorld.ManualTest
{
    public sealed partial class ManualTestPlugin
    {
        const string LabBuiltKey = "lab", NpcSpawnedKey = "spawned", NpcId = "Human", LabTag = "lab-tag", NpcKey = "npc-key";
        bool registered = true;
        GameObject registeredPrefab = ZNetScene.instance.prefab;
        GameObject npc;
        internal GameObject Existing;
        internal readonly List<string> reports = new List<string>();
        void FindNpc() { if (npc == null) npc = Existing; }
        static Vector3 GroundPoint(Vector3 value) { return value; }
        static void RequireClear(Vector3 value, float radius) { }
        static void GuardOwner(GameObject value)
        { if (!value.GetComponent<ZNetView>().IsOwner()) throw new InvalidOperationException("owner required"); }
        void Report(string value) { reports.Add(value); }
        internal void Run() { Spawn(); }
    }
}
static class FreshSpawnTransactionHarness
{
    static int checks;
    static void Check(bool condition, string name) { ++checks; if (!condition) throw new Exception(name); }
    static string Refuse(ManualTestPlugin plugin)
    { try { plugin.Run(); } catch (InvalidOperationException error) { return error.Message; } throw new Exception("Expected refusal"); }
    static void Frozen(GameObject actor)
    {
        Check(!actor.GetComponent<HumanAppearance>().enabled && !actor.GetComponent<HumanAppearance>().Initialized,
            "failed fresh appearance cannot enter legacy fallback");
        Check(!actor.GetComponent<HumanCompanion>().enabled && !actor.GetComponent<MonsterAI>().enabled,
            "failed actor has no companion/native AI execution");
    }
    public static int Main()
    {
        try
        {
            SpawnFixture.Reset(); var plugin = new ManualTestPlugin(); plugin.Run();
            Check(SpawnFixture.Events.IndexOf("profile:required") < SpawnFixture.Events.IndexOf("composition"), "persist profile requirement before composition commit");
            Check(SpawnFixture.Events.IndexOf("composition") < SpawnFixture.Events.IndexOf("identity"), "composition before explicit identity");
            Check(SpawnFixture.Events.IndexOf("identity") < SpawnFixture.Events.IndexOf("equip"), "profile before success equipment");
            Check(!ZoneSystem.instance.GetGlobalKey("spawned") && ZNetScene.instance.destroyed.Count == 0, "successful fresh actor preserved without permanent spawn lock");

            SpawnFixture.Reset(); SpawnFixture.FailIdentity = true; plugin = new ManualTestPlugin(); var scene = ZNetScene.instance;
            string reason = Refuse(plugin); Frozen(SpawnFixture.Created);
            Check(scene.destroyed.Count == 1 && System.Object.ReferenceEquals(scene.destroyed[0], SpawnFixture.Created), "only exact newly created actor rolled back");
            Check(!ZoneSystem.instance.GetGlobalKey("spawned"), "failed identity never commits world guard");
            Check(!SpawnFixture.Events.Contains("equip"), "failed identity cannot reach success equipment");

            SpawnFixture.Reset(); SpawnFixture.FailIdentity = true; SpawnFixture.ConfirmRemoval = false; plugin = new ManualTestPlugin(); scene = ZNetScene.instance;
            reason = Refuse(plugin); Frozen(SpawnFixture.Created);
            Check(scene.FindInstance(new ZDOID { value = 17 }) == null && ZDOMan.instance.GetZDO(new ZDOID { value = 17 }) != null,
                "simulated native queued destruction leaves persistence pending");
            Check(!ZoneSystem.instance.GetGlobalKey("spawned") && reason.Contains("No persistent spawn lock"), "scene-only disappearance cannot strand a false world guard");
            Check(ZDOMan.instance.GetZDO(new ZDOID { value = 17 }).fields[HumanProfile.RequiredKey] == "1", "unconfirmed rollback retains persisted profile requirement");

            SpawnFixture.Reset(); SpawnFixture.LoseOwner = true; plugin = new ManualTestPlugin(); scene = ZNetScene.instance;
            Refuse(plugin); Frozen(SpawnFixture.Created);
            Check(scene.destroyed.Count == 0 && !ZoneSystem.instance.GetGlobalKey("spawned"), "authority loss never claims/deletes record or commits guard");

            SpawnFixture.Reset(); SpawnFixture.ChangeScene = true; plugin = new ManualTestPlugin(); scene = ZNetScene.instance;
            Refuse(plugin); Frozen(SpawnFixture.Created);
            Check(scene.destroyed.Count == 0 && ZNetScene.instance.destroyed.Count == 0 && !ZoneSystem.instance.GetGlobalKey("spawned"), "changed scene cannot receive rollback destruction or guard commit");

            SpawnFixture.Reset(); SpawnFixture.FailIdentity = SpawnFixture.FailDestroy = true; plugin = new ManualTestPlugin();
            reason = Refuse(plugin); Frozen(SpawnFixture.Created);
            Check(!ZoneSystem.instance.GetGlobalKey("spawned") && reason.Contains("rollback failed"), "cleanup exception preserves original refusal without false guard");

            SpawnFixture.Reset(); SpawnFixture.ThrowInstantiate = true; plugin = new ManualTestPlugin(); scene = ZNetScene.instance;
            Refuse(plugin);
            Check(scene.destroyed.Count == 0 && !ZoneSystem.instance.GetGlobalKey("spawned"), "allocation failure without returned instance has no broad cleanup or guard commit");

            SpawnFixture.Reset(); plugin = new ManualTestPlugin(); var existing = SpawnFixture.Allocate(); SpawnFixture.Events.Clear(); plugin.Existing = existing;
            plugin.Run();
            Check(SpawnFixture.Events.Count == 0 && ZNetScene.instance.destroyed.Count == 0 && existing.GetComponent<MonsterAI>().enabled,
                "existing living scout untouched without allocation, marker mutation, or rollback");
            Console.WriteLine("FreshSpawnTransaction PASS: " + checks + " assertions executing production Spawn method against managed API doubles; Unity runtime NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

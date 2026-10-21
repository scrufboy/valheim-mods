// Executes the production PrepareParty transaction against managed API doubles.
using System;
using System.Collections.Generic;
using System.Linq;
using LivingWorld.HumanNpc;
using LivingWorld.ManualTest;
using UnityEngine;

static class PartyFixture
{
    internal static readonly List<string> Events = new List<string>();
    internal static readonly List<GameObject> Actors = new List<GameObject>();
    internal static void Reset()
    {
        Events.Clear(); Actors.Clear(); Character.All.Clear();
        ZoneSystem.instance = new ZoneSystem(); ZNetScene.instance = new ZNetScene();
        Player.m_localPlayer = new GameObject().AddComponent<Player>();
    }
    internal static GameObject NewActor(bool support)
    {
        var actor = new GameObject();
        var body = support ? (Character)actor.AddComponent<Humanoid>() : actor.AddComponent<Humanoid>();
        Character.All.Add(body);
        actor.AddComponent<ZNetView>(); actor.AddComponent<HumanAppearance>();
        actor.AddComponent<HumanBehaviorInstaller>(); actor.AddComponent<HumanCompositionIdentity>();
        actor.AddComponent<HumanIdentity>();
        Actors.Add(actor); Events.Add(support ? "allocate:support" : "allocate:scout");
        return actor;
    }
    internal static void Approve(GameObject actor)
    {
        actor.GetComponent<HumanAppearance>().Initialized = true;
        actor.GetComponent<HumanAppearance>().State = "complete";
        actor.GetComponent<HumanBehaviorInstaller>().Installed = true;
        actor.GetComponent<HumanBehaviorInstaller>().Status = "installed";
        actor.GetComponent<HumanCompositionIdentity>().IsReady = true;
        actor.AddComponent<HumanPartyEquipment>(); actor.AddComponent<HumanCompanion>();
    }
}

namespace UnityEngine
{
    public class Object
    {
        public static GameObject Instantiate(GameObject prefab, Vector3 point, Quaternion rotation)
        { return PartyFixture.NewActor(true); }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject.transform; } }
        public T GetComponent<T>() where T : class { return gameObject.GetComponent<T>(); }
    }
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        readonly List<Component> components = new List<Component>();
        public Transform transform = new Transform();
        public T AddComponent<T>() where T : Component, new() { var value = new T(); value.gameObject = this; components.Add(value); return value; }
        public T GetComponent<T>() where T : class { foreach (var item in components) if (item is T) return item as T; return null; }
    }
    public class Transform { public Vector3 position; public Quaternion rotation; }
    public struct Vector3 { }
    public struct Quaternion { }
}

public class ZDO
{
    readonly Dictionary<string, string> values = new Dictionary<string, string>();
    public void Set(string key, string value) { values[key] = value; }
    public string GetString(string key, string fallback) { string value; return values.TryGetValue(key, out value) ? value : fallback; }
}
public class ZNetView : MonoBehaviour
{
    readonly ZDO zdo = new ZDO();
    public bool IsValid() { return true; }
    public bool IsOwner() { return true; }
    public ZDO GetZDO() { return zdo; }
}
public class Character : MonoBehaviour
{
    public static readonly List<Character> All = new List<Character>();
    public static List<Character> GetAllCharacters() { return All; }
    public bool IsDead() { return false; }
}
public class Humanoid : Character
{
    public void EquipBestWeapon(object a, object b, object c, object d) { PartyFixture.Events.Add("equip"); }
}
public class MonsterAI : MonoBehaviour { public bool enabled = true; }
public class Player : Humanoid
{
    public static Player m_localPlayer;
}
public class ZoneSystem
{
    public static ZoneSystem instance;
    public void RemoveGlobalKey(string key) { PartyFixture.Events.Add("guard:remove"); }
}
public class ZNetScene
{
    public static ZNetScene instance;
    public GameObject prefab = new GameObject();
    public GameObject GetPrefab(string id) { return prefab; }
    public void Destroy(GameObject actor) { PartyFixture.Events.Add("destroy"); }
}

namespace LivingWorld.HumanNpc
{
    public static class HumanProfile { public const string RequiredKey = "profile"; }
    public class HumanAppearance : MonoBehaviour
    {
        public bool Initialized; public string State = "pending";
        public void StageFreshProfile() { PartyFixture.Events.Add("appearance:stage"); }
    }
    public class HumanBehaviorInstaller : MonoBehaviour { public bool Installed; public string Status = "pending"; }
    public class HumanCompositionIdentity : MonoBehaviour
    {
        public bool IsReady;
        public string Status { get { return IsReady ? "ready" : "pending"; } }
        public void InitializeFreshBase(Player player) { PartyFixture.Events.Add("base-profile"); }
        public bool InitializeOrders(Player player) { PartyFixture.Events.Add("orders"); return IsReady; }
    }
    public class HumanIdentity : MonoBehaviour
    { public void InitializeFresh(string seed, string[] hair, string[] beard) { PartyFixture.Events.Add("identity"); } }
    public class HumanPartyEquipment : MonoBehaviour
    { public void PrepareArcher() { PartyFixture.Events.Add("archer"); } }
    public class HumanCompanion : MonoBehaviour
    { public bool TryIssueFollow(Player player) { PartyFixture.Events.Add("follow"); return true; } }
    public class HumanCleric : MonoBehaviour
    {
        public const string RoleKey = "role";
        public static bool IsCleric(GameObject actor)
        { var view = actor == null ? null : actor.GetComponent<ZNetView>(); return view != null && view.GetZDO().GetString(RoleKey, "") == "1"; }
    }
    public static class NativeHumanFactory { public static void Validate(GameObject actor, bool live) { PartyFixture.Events.Add("validate"); } }
}

namespace LivingWorld.ManualTest
{
    public sealed partial class ManualTestPlugin : MonoBehaviour
    {
        const string NpcId = "human", LabTag = "lab", NpcKey = "npc";
        bool registered = true;
        GameObject registeredPrefab = ZNetScene.instance.prefab;
        GameObject npc;
        internal readonly List<string> Reports = new List<string>();
        void FindNpc() { }
        void Spawn()
        {
            npc = PartyFixture.NewActor(false);
            npc.GetComponent<HumanCompositionIdentity>().InitializeFreshBase(Player.m_localPlayer);
            npc.GetComponent<HumanIdentity>().InitializeFresh("scout", new[] { "Hair1" }, new[] { "", "Beard1" });
        }
        static void GuardOwner(GameObject actor) { if (!actor.GetComponent<ZNetView>().IsOwner()) throw new InvalidOperationException("owner"); }
        static string[] NativeAppearanceChoices(string type, bool empty) { return empty ? new[] { "", "Beard1" } : new[] { "Hair1" }; }
        static Vector3 FindPartySpawnPoint() { return new Vector3(); }
        void Report(string message) { Reports.Add(message); PartyFixture.Events.Add("report:" + message); }
        internal void Run() { PrepareParty(); }
    }
}

static class PartySpawnTransactionHarness
{
    static int checks;
    static void Check(bool condition, string message) { ++checks; if (!condition) throw new Exception(message); }
    static bool Has(string prefix) { return PartyFixture.Events.Any(value => value.StartsWith(prefix, StringComparison.Ordinal)); }
    public static int Main()
    {
        try
        {
            PartyFixture.Reset(); var plugin = new ManualTestPlugin();
            try { plugin.Run(); } catch (InvalidOperationException error) { Check(error.Message.StartsWith("Prepare requires a ready"), "first pass waits internally"); }
            Check(PartyFixture.Actors.Count == 2 && PartyFixture.Actors.Count(a => HumanCleric.IsCleric(a)) == 1,
                "one command pass allocates exactly scout and support");
            Check(PartyFixture.Events.Count(value => value == "identity") == 2 && PartyFixture.Events.Count(value => value == "base-profile") == 2,
                "both independent random profile transactions begin in the same pass");
            Check(PartyFixture.Events.Count(value => value == "appearance:stage") == 1,
                "support is staged in the extracted party transaction; scout staging is covered by the spawn transaction");
            Check(!Has("follow") && !Has("archer") && !Has("report:Party ready"),
                "no partial actor is ordered or reported ready");
            foreach (var actor in PartyFixture.Actors) PartyFixture.Approve(actor);
            plugin.Run();
            Check(PartyFixture.Actors.Count == 2, "internal retry does not duplicate either actor");
            Check(PartyFixture.Events.Count(value => value == "orders") == 2 && PartyFixture.Events.Count(value => value == "follow") == 2,
                "both actors receive orders and follow together");
            Check(PartyFixture.Events.Count(value => value == "archer") == 1 && Has("report:Party ready: two complete"),
                "success is reported only for the complete two-person party");
            Console.WriteLine("PartySpawnTransactionHarness PASS: " + checks + " assertions executing production PrepareParty; one command, two parallel base humans, one joint ready boundary; Unity runtime NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

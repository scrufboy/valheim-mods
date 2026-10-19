using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.HumanNpc;

// Executable managed API doubles: actual HumanResources source runs below.
// This tests adapter transitions and scope, not Unity/Harmony execution.
namespace UnityEngine
{
    public class GameObject
    {
        readonly Dictionary<Type, MonoBehaviour> components = new Dictionary<Type, MonoBehaviour>();
        public T Add<T>() where T : MonoBehaviour, new()
        { var value = new T { gameObject = this }; components.Add(typeof(T), value); return value; }
        public T Get<T>() where T : class
        { foreach (var value in components.Values) if (value is T) return value as T; return null; }
    }
    public class MonoBehaviour
    {
        public GameObject gameObject;
        public bool isActiveAndEnabled = true;
        public T GetComponent<T>() where T : class { return gameObject.Get<T>(); }
    }
    public static class Time { public static float deltaTime; }
}
namespace HarmonyLib
{
    public class HarmonyMethod { public HarmonyMethod(MethodInfo method) { if (method == null) throw new Exception("Missing handler"); } }
    public class Harmony
    {
        public int Count;
        public void Patch(MethodInfo method, HarmonyMethod prefix)
        { if (method.DeclaringType != typeof(Character)) throw new Exception("Wrong patch scope"); Count++; }
    }
}
public class ZDO
{
    public readonly Dictionary<string, string> Data = new Dictionary<string, string>();
    public int Writes;
    public string GetString(string key, string fallback) { string value; return Data.TryGetValue(key, out value) ? value : fallback; }
    public void Set(string key, string value) { Data[key] = value; Writes++; }
}
public class ZNetView : UnityEngine.MonoBehaviour
{
    public bool Valid = true, Owner = true;
    public ZDO Data = new ZDO();
    public bool IsValid() { return Valid; } public bool IsOwner() { return Owner; } public ZDO GetZDO() { return Data; }
}
public class SEMan { public float Modifier = 1; public void ModifyStaminaRegen(ref float value) { value *= Modifier; } }
public class Character : UnityEngine.MonoBehaviour
{
    public bool Dead, Attacking, Dodging, Encumbered, Swimming, Blocking, m_wallRunning;
    public bool Ground = true;
    public SEMan Effects = new SEMan();
    public bool IsDead() { return Dead; } public bool InAttack() { return Attacking; }
    public bool InDodge() { return Dodging; } public bool IsEncumbered() { return Encumbered; }
    public bool IsSwimming() { return Swimming; } public bool IsOnGround() { return Ground; }
    public bool IsBlocking() { return Blocking; } public SEMan GetSEMan() { return Effects; }
    public bool IsWallRunning() { return m_wallRunning; }
    public virtual bool HaveStamina(float value) { return true; }
    public virtual void UseStamina(float value) { }
    public virtual void AddStamina(float value) { }
    public virtual float GetMaxStamina() { return 0; }
    public virtual float GetStaminaPercentage() { return 1; }
}
public class Humanoid : Character { }
public class Player : Humanoid
{
    public float m_baseStamina = 50, m_staminaRegen = 5, m_staminaRegenTimeMultiplier = 1, m_staminaRegenDelay = 1;
}
public static class Game { public static float m_staminaRate = 1, m_staminaRegenRate = 1; }
namespace LivingWorld.HumanNpc
{
    public class HumanAppearance : UnityEngine.MonoBehaviour { public bool Initialized = true; }
    public class HumanCompositionIdentity : UnityEngine.MonoBehaviour { public bool IsReady = true; }
}
namespace LivingWorld.ManualTest
{
    public class ManualTestPlugin { public static ManualTestPlugin Instance; public void Report(string message) { } }
}
static class ResourceAdapterHarness
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static object Call(object instance, string method, params object[] args)
    { return typeof(HumanResources).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).Invoke(instance, args); }
    static HumanResources Make(out Humanoid body, out ZNetView view, ZDO saved)
    {
        var obj = new UnityEngine.GameObject(); body = obj.Add<Humanoid>(); view = obj.Add<ZNetView>();
        if (saved != null) view.Data = saved;
        obj.Add<HumanAppearance>(); obj.Add<HumanCompositionIdentity>();
        var resource = obj.Add<HumanResources>(); resource.Configure(new Player()); return resource;
    }
    static HumanResourcePool State(ZNetView view)
    {
        HumanResourcePool pool;
        if (!HumanResourcePool.TryRestore(view.Data.GetString(HumanResources.RecordKey, ""), 50, out pool)) throw new Exception("No valid saved pool");
        return pool;
    }
    static bool Have(Character body, float cost)
    { var args = new object[] { body, cost, false }; Check(!(bool)Call(null, "BeforeHave", args), "our method intercepted"); return (bool)args[2]; }
    static int Main()
    {
        var harmony = new HarmonyLib.Harmony(); HumanResources.InstallPatches(harmony);
        Check(harmony.Count == 5, "exact five character hooks");
        Humanoid body; ZNetView view;
        var provider = Make(out body, out view, null);
        Check(Have(body, 49), "ready owned NPC initialized");
        Check(view.Data.Writes == 1 && State(view).Current == 50, "single initial persistence");
        Call(null, "BeforeUse", body, 20f);
        Check(State(view).Current == 30 && view.Data.Writes == 2, "native call debits once and saves immediately");
        Check(Have(body, 29) && !Have(body, 30), "strict native boundary in adapter");
        int writes = view.Data.Writes;
        Call(null, "BeforeUse", body, float.NaN);
        Check(view.Data.Writes == writes, "malformed call no write");
        var save = view.Data;
        Call(provider, "OnDisable");
        provider = Make(out body, out view, save);
        Check(!Have(body, 30) && State(view).Current == 30 && State(view).RecoveryDelay == 1, "reload does not refill or skip delay");
        UnityEngine.Time.deltaTime = .25f;
        for (int i = 0; i < 4; i++) Call(provider, "Update");
        Check(State(view).Current == 30, "first second pays saved recovery delay");
        body.Attacking = true;
        for (int i = 0; i < 4; i++) Call(provider, "Update");
        Check(State(view).Current == 30, "native attack prevents regen");
        body.Attacking = false;
        for (int i = 0; i < 4; i++) Call(provider, "Update");
        Check(State(view).Current > 30 && State(view).Current < 50, "loaded safe recovery persisted");
        Call(null, "BeforeUse", body, 100f);
        Check(!Have(body, 0), "exhausted is not able to block");
        Call(null, "BeforeAdd", body, 15f);
        Check(State(view).Current == 15, "native block return enters same provider");
        view.Owner = false; writes = view.Data.Writes;
        Call(provider, "Update"); Call(null, "BeforeUse", body, 10f); Call(null, "BeforeAdd", body, 10f);
        Check(view.Data.Writes == writes && State(view).Current == 15, "remote writes denied");
        Check(Have(body, 14), "remote reads replicated saved pool");
        view.Data.Set(HumanResources.RecordKey, "1|50|7|0.5"); view.Owner = true;
        Check(!Have(body, 10) && State(view).Current == 7, "authority reacquire reloads authoritative record");
        body.Dead = true; writes = view.Data.Writes;
        Call(null, "BeforeAdd", body, 50f); Call(provider, "Update");
        Check(view.Data.Writes == writes && !Have(body, 0), "dead cannot recover");
        var malformed = new ZDO(); malformed.Set(HumanResources.RecordKey, "future-version");
        provider = Make(out body, out view, malformed); writes = view.Data.Writes;
        Check(!Have(body, 0) && view.Data.Writes == writes && view.Data.GetString(HumanResources.RecordKey, "") == "future-version", "unknown data retained; no reset");
        provider = Make(out body, out view, null);
        body.GetComponent<HumanAppearance>().Initialized = false;
        Check(!Have(body, 0) && view.Data.Writes == 0, "no pool before composed human is ready");
        var other = new UnityEngine.GameObject().Add<Humanoid>();
        Check((bool)Call(null, "BeforeUse", other, 10f), "ordinary NPC untouched");
        var humanPlayer = new UnityEngine.GameObject().Add<Player>(); humanPlayer.gameObject.Add<HumanResources>();
        Check((bool)Call(null, "BeforeUse", humanPlayer, 10f), "real player untouched even with misplaced provider");
        Console.WriteLine("ResourceAdapterHarness PASS: " + checks + " actual adapter checks; Unity/Harmony runtime NOT RUN.");
        return 0;
    }
}

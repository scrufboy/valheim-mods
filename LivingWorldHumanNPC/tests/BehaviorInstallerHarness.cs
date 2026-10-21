using System;
using System.Collections.Generic;
using System.Reflection;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace UnityEngine
{
    public class Component { public GameObject gameObject; public T GetComponent<T>() where T : Component { return gameObject.GetComponent<T>(); } }
    public class MonoBehaviour : Component { }
    public class GameObject
    {
        readonly List<Component> components = new List<Component>();
        public int Count { get { return components.Count; } }
        public T AddComponent<T>() where T : Component, new() { var value = new T(); value.gameObject = this; components.Add(value); return value; }
        public T GetComponent<T>() where T : Component { foreach (var item in components) if (item is T) return (T)item; return null; }
        public int CountOf<T>() where T : Component { int count = 0; foreach (var item in components) if (item is T) ++count; return count; }
    }
}

public class Player : MonoBehaviour
{
    public float m_baseStamina = 80, m_staminaRegen = 7, m_staminaRegenTimeMultiplier = 1.4f, m_staminaRegenDelay = 1;
}
public class MonsterAI : MonoBehaviour
{
    bool active = true;
    public int EnabledAtCount = -1;
    public bool enabled { get { return active; } set { active = value; if (value && gameObject != null) EnabledAtCount = gameObject.Count; } }
}

namespace LivingWorld.HumanNpc
{
    public class HumanAppearance : MonoBehaviour { public bool Initialized; }
    public class HumanCompanion : MonoBehaviour { }
    public class HumanDoorTraversal : MonoBehaviour { }
    public class HumanFollowPresence : MonoBehaviour { }
    public class HumanCompanionPosture : MonoBehaviour { }
    public class HumanEnvironmentStatus : MonoBehaviour { }
    public class HumanPartyConfidence : MonoBehaviour { }
    public class HumanCompanionAmbient : MonoBehaviour { }
    public class HumanEquipmentReadiness : MonoBehaviour { }
    public class HumanCombatDefense : MonoBehaviour { }
    public class HumanPartyEquipment : MonoBehaviour { }
    public class HumanCleric : MonoBehaviour { }
    public class HumanUtilityTorch : MonoBehaviour { }
    public class HumanResources : MonoBehaviour
    {
        public float Capacity, Recovery, Bonus, Delay;
        internal void Configure(float capacity, float recovery, float bonus, float delay)
        { Capacity = capacity; Recovery = recovery; Bonus = bonus; Delay = delay; }
    }
}

namespace LivingWorld.ManualTest
{
    public class ManualTestPlugin
    {
        public static ManualTestPlugin Instance;
        public readonly List<string> Reports = new List<string>();
        public void Report(string message) { Reports.Add(message); }
    }
}

static class BehaviorInstallerHarness
{
    static int checks;
    static void Check(bool value, string message) { ++checks; if (!value) throw new Exception(message); }
    static void Tick(HumanBehaviorInstaller installer)
    { typeof(HumanBehaviorInstaller).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(installer, null); }
    static GameObject Actor(out HumanAppearance appearance, out MonsterAI ai, out HumanBehaviorInstaller installer)
    {
        var actor = new GameObject();
        appearance = actor.AddComponent<HumanAppearance>();
        ai = actor.AddComponent<MonsterAI>(); ai.enabled = false;
        installer = actor.AddComponent<HumanBehaviorInstaller>();
        installer.Configure(new GameObject().AddComponent<Player>());
        return actor;
    }
    static void CompleteSet(GameObject actor)
    {
        Check(actor.CountOf<HumanCompanion>() == 1 && actor.CountOf<HumanDoorTraversal>() == 1 &&
            actor.CountOf<HumanFollowPresence>() == 1 && actor.CountOf<HumanCompanionPosture>() == 1 &&
            actor.CountOf<HumanResources>() == 1 && actor.CountOf<HumanEnvironmentStatus>() == 1 &&
            actor.CountOf<HumanPartyConfidence>() == 1 && actor.CountOf<HumanCompanionAmbient>() == 1 &&
            actor.CountOf<HumanEquipmentReadiness>() == 1 && actor.CountOf<HumanCombatDefense>() == 1 &&
            actor.CountOf<HumanPartyEquipment>() == 1 && actor.CountOf<HumanCleric>() == 1 &&
            actor.CountOf<HumanUtilityTorch>() == 1, "exactly one complete behavior set");
    }
    public static int Main()
    {
        try
        {
            HumanAppearance appearance; MonsterAI ai; HumanBehaviorInstaller installer;
            var actor = Actor(out appearance, out ai, out installer);
            Tick(installer);
            Check(!installer.Installed && !ai.enabled && actor.GetComponent<HumanCompanion>() == null,
                "appearance gate keeps AI and every behavior dormant");
            appearance.Initialized = true; Tick(installer);
            Check(installer.Installed && ai.enabled, "complete appearance installs behavior and enables AI");
            CompleteSet(actor);
            Check(ai.EnabledAtCount == actor.Count, "AI enabled after the final behavior component");
            int count = actor.Count; Tick(installer); Tick(installer);
            Check(actor.Count == count, "repeated update cannot duplicate behavior");
            var resources = actor.GetComponent<HumanResources>();
            Check(resources.Capacity == 80 && resources.Recovery == 7 && Math.Abs(resources.Bonus - 1.4f) < .001f && resources.Delay == 1,
                "native stamina values cross the staging boundary");

            actor = Actor(out appearance, out ai, out installer);
            actor.AddComponent<HumanCompanion>(); appearance.Initialized = true; Tick(installer);
            Check(!installer.Installed && !ai.enabled && installer.Status.StartsWith("FAIL behavior installation"),
                "pre-attached behavior fails closed with AI dormant");
            Console.WriteLine("BehaviorInstallerHarness PASS: " + checks + " assertions; appearance-first behavior staging executed against API doubles; Unity runtime NOT RUN.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

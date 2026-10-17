using System;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("local.popvillages.discovery", "PoP Village Discovery", "0.1.1")]
public class VillageDiagnostics : BaseUnityPlugin
{
    internal static BepInEx.Logging.ManualLogSource Log;
    Harmony patches;
    long lastWorld;
    void Awake()
    {
        Log = Logger;
        patches = new Harmony("local.popvillages.discovery");
        patches.PatchAll(typeof(VillageDiagnostics).Assembly);
        InvokeRepeating("Snapshot", 15f, 30f);
        Logger.LogInfo("Read-only village diagnostics active; no spawn or save changes.");
    }
    void OnDestroy() { if (patches != null) patches.UnpatchSelf(); }
    void Snapshot()
    {
        if (!Player.m_localPlayer) return;
        if (ZNet.instance && ZoneSystem.instance && lastWorld != ZNet.instance.GetWorldUID())
        {
            lastWorld = ZNet.instance.GetWorldUID();
            Log.LogInfo("WORLD FLAGS world=" + ZNet.instance.GetWorldName() + " uid=" + lastWorld + " Elder=" + ZoneSystem.instance.GetGlobalKey("defeated_gdking") + " keys=" + string.Join(",", ZoneSystem.instance.GetGlobalKeys()));
            foreach (var method in Harmony.GetAllPatchedMethods().Where(m => m.DeclaringType == typeof(SpawnSystem) || m.DeclaringType == typeof(ZoneSystem)))
            {
                var info = Harmony.GetPatchInfo(method);
                Log.LogInfo("SPAWN PATCH method=" + method.Name + " owners=" + string.Join(",", info.Owners));
            }
        }
        var pos = Player.m_localPlayer.transform.position;
        var spawners = UnityEngine.Object.FindObjectsOfType<CreatureSpawner>();
        foreach (var s in spawners.Where(s => Vector3.Distance(s.transform.position, pos) < 100f))
        {
            var prefab = AccessTools.Field(typeof(CreatureSpawner), "m_creaturePrefab").GetValue(s) as GameObject;
            Log.LogInfo("LOCAL name=" + s.name + " pos=" + s.transform.position + " creature=" + (prefab ? prefab.name : "null"));
        }
        foreach (var s in UnityEngine.Object.FindObjectsOfType<SpawnArea>().Where(s => Vector3.Distance(s.transform.position, pos) < 100f))
            Log.LogInfo("SPAWNAREA name=" + s.name + " pos=" + s.transform.position);
        foreach (var c in UnityEngine.Object.FindObjectsOfType<Character>().Where(c => Vector3.Distance(c.transform.position, pos) < 100f && (c.name.StartsWith("Draugr") || c.name.StartsWith("RRRN_popvillages"))))
            Log.LogInfo("RESIDENT name=" + c.name + " pos=" + c.transform.position);
    }
    [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
    class SpawnTrace
    {
        static void Postfix(CreatureSpawner __instance)
        {
            if (!Player.m_localPlayer || Vector3.Distance(__instance.transform.position, Player.m_localPlayer.transform.position) > 100f) return;
            Log.LogInfo("LOCAL SPAWN source=" + __instance.name + " pos=" + __instance.transform.position);
        }
    }
    [HarmonyPatch(typeof(ZNetView), "Destroy")]
    class DestroyTrace
    {
        static void Prefix(ZNetView __instance)
        {
            if (!__instance.GetComponent<SpawnArea>()) return;
            Log.LogInfo("SPAWNAREA DESTROY name=" + __instance.name + " pos=" + __instance.transform.position + " caller=" + Environment.StackTrace);
        }
    }
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    class RuleTrace
    {
        [HarmonyPriority(Priority.First)]
        static void Prefix(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
        {
            if (!critter.m_prefab || !critter.m_prefab.name.StartsWith("Draugr")) return;
            Log.LogInfo("DRAUGR ATTEMPT rule=" + critter.m_name + " requiredKey=" + critter.m_requiredGlobalKey + " Elder=" + ZoneSystem.instance.GetGlobalKey("defeated_gdking") + " requiredKeyMet=" + (string.IsNullOrEmpty(critter.m_requiredGlobalKey) || ZoneSystem.instance.GetGlobalKey(critter.m_requiredGlobalKey)) + " night=" + EnvMan.IsNight() + " weather=" + EnvMan.instance.GetCurrentEnvironment().m_name + " biomes=" + critter.m_biome + " event=" + eventSpawner + " pos=" + spawnPoint + " caller=" + Environment.StackTrace);
        }
    }
}

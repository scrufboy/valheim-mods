using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("local.popvillages.reclaim", "PoP Village Reclaim", "0.1.2")]
[BepInDependency("asharppen.valheim.spawn_that")]
[BepInDependency("com.alexanderstrada.rrrcore")]
public class VillageReclaim : BaseUnityPlugin
{
    internal static BepInEx.Logging.ManualLogSource Log;
    static readonly Dictionary<CreatureSpawner, GameObject> Villagers = new Dictionary<CreatureSpawner, GameObject>();
    static readonly Dictionary<Vector3, float> QuietSince = new Dictionary<Vector3, float>();
    static readonly Dictionary<CreatureSpawner, Vector3> Centers = new Dictionary<CreatureSpawner, Vector3>();
    const string Cleared = "pop_reclaim_v1_cleared";
    const string Encounter = "pop_reclaim_v1_encounter";
    const string WorldSpawn = "pop_reclaim_world_draugr";
    static readonly HashSet<string> SpawnRulesLogged = new HashSet<string>();
    Harmony patches;
    void Awake() { Log = Logger; patches = new Harmony("local.popvillages.reclaim"); patches.PatchAll(); }
    void OnDestroy() { if (patches != null) patches.UnpatchSelf(); Villagers.Clear(); QuietSince.Clear(); Centers.Clear(); }
    static object Field(object o, string name) { return AccessTools.Field(o.GetType(), name).GetValue(o); }
    static void Set(object o, string name, object value) { AccessTools.Field(o.GetType(), name).SetValue(o, value); }
    static ZDO Data(CreatureSpawner s) { var v = s.GetComponent<ZNetView>(); return v && v.IsValid() ? v.GetZDO() : null; }
    static bool Near(Vector3 a, Vector3 b) { var d = a-b; return d.x*d.x+d.z*d.z <= 80f*80f && Math.Abs(d.y) < 50f; }
    static bool TryCenter(CreatureSpawner s, out Vector3 center)
    {
        if (Centers.TryGetValue(s, out center)) return true;
        ZoneSystem.LocationInstance village;
        if (!ZoneSystem.instance || !ZoneSystem.instance.FindClosestLocation("WoodVillage1", s.transform.position, out village)) return false;
        center = village.m_position;
        if (!Near(s.transform.position, center)) return false;
        Centers[s] = center;
        Log.LogInfo("Village registered: WoodVillage1 center=" + center + " spawner=" + s.name);
        return true;
    }
    static bool Prepare(CreatureSpawner s)
    {
        GameObject villager;
        if (!Villagers.TryGetValue(s, out villager))
        {
            villager = Field(s, "m_creaturePrefab") as GameObject;
            if (!villager || !villager.name.StartsWith("RRRN_popvillagesHuman")) return true;
            Villagers[s] = villager;
        }
        var data = Data(s);
        if (data == null) return false;
        Vector3 center;
        if (!TryCenter(s, out center)) return false;
        var viewOwner = s.GetComponent<ZNetView>();
        if (!viewOwner.IsOwner()) return true;
        var peers = Villagers.Keys.Where(p => p && Centers.ContainsKey(p) && Centers[p] == center).ToArray();
        bool unlocked = data.GetBool(Cleared, false) || peers.Any(p => Data(p) != null && Data(p).GetBool(Cleared, false));
        if (!unlocked)
        {
            bool hostile = Character.GetAllCharacters().Any(c => c && c.name.StartsWith("Draugr") && Near(c.transform.position, center) && !IsWorldDraugr(c));
            bool piles = UnityEngine.Object.FindObjectsOfType<SpawnArea>().Any(p => p && p.name.StartsWith("Spawner_DraugrPile") && Near(p.transform.position, center));
            bool started = data.GetBool(Encounter, false) || peers.Any(p => Data(p) != null && Data(p).GetBool(Encounter, false));
            bool present = Player.m_localPlayer && Vector3.Distance(Player.m_localPlayer.transform.position, center) < 50f;
            if (hostile || piles || !started || !present) QuietSince.Remove(center);
            else
            {
                float since;
                if (!QuietSince.TryGetValue(center, out since)) { QuietSince[center] = Time.time; Log.LogInfo("Village clear candidate at " + center + "; waiting 180 seconds."); }
                else if (Time.time-since >= 180f) { unlocked = true; Log.LogInfo("Village reclaimed at " + center); }
            }
        }
        if (unlocked)
        {
            var view = s.GetComponent<ZNetView>();
            if (view.IsOwner()) data.Set(Cleared, true);
            Set(s, "m_creaturePrefab", villager);
            Set(s, "m_respawnTimeMinuts", 3f);
            return true;
        }
        string original = s.name.StartsWith("Spawner_Draugr_Elite") ? "Draugr_Elite" : s.name.StartsWith("Spawner_Draugr_Ranged") ? "Draugr_Ranged" : "Draugr";
        var prefab = ZNetScene.instance ? ZNetScene.instance.GetPrefab(original) : null;
        if (!prefab) return false;
        Set(s, "m_creaturePrefab", prefab);
        Set(s, "m_respawnTimeMinuts", 0f);
        return true;
    }
    [HarmonyPatch(typeof(CreatureSpawner), "UpdateSpawner")]
    class Gate
    {
        [HarmonyPriority(Priority.Last)]
        static bool Prefix(CreatureSpawner __instance)
        {
            try { return Prepare(__instance); }
            catch (Exception e) { Log.LogError("Village gate failed; spawn blocked: " + e); return false; }
        }
    }
    [HarmonyPatch(typeof(CreatureSpawner), "Spawn")]
    class SpawnRecord
    {
        static bool Prefix(CreatureSpawner __instance)
        {
            if (!Villagers.ContainsKey(__instance)) return true;
            var data = Data(__instance);
            return data != null && (data.GetBool(Cleared, false) || !data.GetBool(Encounter, false));
        }
        [HarmonyPriority(Priority.Last)]
        static void Postfix(CreatureSpawner __instance, ZNetView __result)
        {
            if (!__result || !Villagers.ContainsKey(__instance)) return;
            var data = Data(__instance);
            var creature = __result.GetComponent<Character>();
            if (!creature) return;
            if (__result.name.StartsWith("Draugr")) { if(data != null) data.Set(Encounter, true); Set(creature, "m_faction", Character.Faction.Undead); }
            else if (__result.name.StartsWith("RRRN_popvillages"))
            {
                Set(creature, "m_faction", Character.Faction.Players);
                Log.LogInfo("Reclaimed village civilian spawned: " + __result.name);
            }
        }
    }
    static bool IsWorldDraugr(Character c)
    {
        var v = c.GetComponent<ZNetView>();
        return v && v.IsValid() && v.GetZDO().GetBool(WorldSpawn, false);
    }
    [HarmonyPatch(typeof(SpawnSystem), "Spawn")]
    class WorldDraugrGate
    {
        static bool Prefix(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner, out bool __state)
        {
            __state = false;
            if (!critter.m_prefab || !critter.m_prefab.name.StartsWith("Draugr") || eventSpawner) return true;
            ZoneSystem.LocationInstance village;
            bool inside = ZoneSystem.instance && ZoneSystem.instance.FindClosestLocation("WoodVillage1", spawnPoint, out village) && Near(spawnPoint, village.m_position);
            string rule = critter.m_prefab.name + "/" + critter.m_biome + "/" + inside;
            if (SpawnRulesLogged.Add(rule)) Log.LogInfo("Draugr world-spawn rule: prefab=" + critter.m_prefab.name + " allowedBiomes=" + critter.m_biome + " point=" + spawnPoint + " villageBlocked=" + inside);
            __state = !inside;
            return !inside;
        }
        static void Postfix(SpawnSystem.SpawnData critter, Vector3 spawnPoint, bool eventSpawner, bool __state)
        {
            if (!__state) return;
            foreach (var c in Character.GetAllCharacters().Where(c => c && c.name.StartsWith(critter.m_prefab.name + "(Clone)") && Vector3.Distance(c.transform.position, spawnPoint) < 0.1f))
            {
                var v = c.GetComponent<ZNetView>();
                if (v && v.IsValid() && v.IsOwner()) v.GetZDO().Set(WorldSpawn, true);
            }
        }
    }
    [HarmonyPatch(typeof(ZNetScene), "OnDestroy")]
    class WorldExit
    {
        static void Prefix() { Villagers.Clear(); QuietSince.Clear(); Centers.Clear(); SpawnRulesLogged.Clear(); }
    }
}

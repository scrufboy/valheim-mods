using System;
using System.Linq;
using System.Reflection;
using BepInEx;
using RagnarsRokare.MobAI;
using UnityEngine;
using HarmonyLib;

[BepInPlugin("local.popvillages.workers.bridge", "PoP Worker Registration", "0.1.2")]
[BepInDependency("som.horem.FriendliesAI")]
public class WorkerBridge : BaseUnityPlugin
{
    MethodInfo attach;
    static BepInEx.Logging.ManualLogSource WorkLog;
    static System.Collections.Generic.HashSet<string> FailedWorkers = new System.Collections.Generic.HashSet<string>();
    Harmony patches;
    System.Collections.Generic.Dictionary<string,string> statuses = new System.Collections.Generic.Dictionary<string,string>();
    void Awake()
    {
        WorkLog=Logger;
        patches=new Harmony("local.popvillages.workers.bridge"); patches.PatchAll(typeof(WorkerBridge).Assembly);
        attach = Assembly.Load("FriendliesAI").GetType("FriendliesAI.Plugin+Character_Awake_Patch").GetMethod("Postfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        InvokeRepeating("RegisterWorkers", 5f, 10f);
    }
    void RegisterWorkers()
    {
        foreach(var c in Character.GetAllCharacters().Where(c => c && c.IsTamed() && (c.name.StartsWith("RRRN_popvillagesHumanM01(") || c.name.StartsWith("RRRN_popvillagesHumanF01(") || c.name.StartsWith("RRRN_popvillagesHumanM02("))))
        {
            var v=c.GetComponent<ZNetView>();
            if (!v || !v.IsValid() || !v.IsOwner()) continue;
            var id=v.GetZDO().GetString("RR_CharId", "");
            if(FailedWorkers.Contains(id))continue;
            if (!string.IsNullOrEmpty(id) && MobManager.IsRegisteredMob(id))
            {
                var status=v.GetZDO().GetString("RR_AiStatus", "");
                string prior;
                if(!statuses.TryGetValue(id,out prior) || prior!=status) {statuses[id]=status; Logger.LogInfo("Worker activity: " + c.name + " status=" + status + " liveAI=" + MobManager.IsAliveMob(id));}
                continue;
            }
            try {
                attach.Invoke(null, new object[]{c,v});
                id=v.GetZDO().GetString("RR_CharId", "");
                if(MobManager.IsRegisteredMob(id)) Logger.LogInfo("Worker registered: " + c.name);
                else Logger.LogWarning("Worker registration deferred: " + c.name);
            } catch(Exception e) { Logger.LogError("Worker registration failed: " + e); }
        }
    }
    void OnDestroy(){if(patches!=null)patches.UnpatchSelf();}
    [HarmonyPatch(typeof(Tameable), "GetHoverText")]
    class WorkerStatus
    {
        static void Postfix(Tameable __instance, ref string __result)
        {
            var v=__instance.GetComponent<ZNetView>();
            if(!v || !v.IsValid())return;
            var id=v.GetZDO().GetString("RR_CharId", "");
            if(string.IsNullOrEmpty(id) || !MobManager.IsRegisteredMob(id))return;
            var status=v.GetZDO().GetString("RR_AiStatus", "");
            if(!string.IsNullOrEmpty(status))__result += "\n"+status;
        }
    }
    [HarmonyPatch(typeof(WearNTear), "Repair")]
    class RepairLog
    {
        static void Postfix(WearNTear __instance, bool __result)
        {
            if(__result && Environment.StackTrace.Contains("FriendliesAI.NpcAI")) WorkLog.LogInfo("Worker repair completed: " + __instance.name + " pos=" + __instance.transform.position);
        }
    }
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    class WorkerFailureRecovery
    {
        static Exception Finalizer(MonsterAI __instance, Exception __exception)
        {
            if(__exception==null)return null;
            var v=__instance.GetComponent<ZNetView>();
            if(!v || !v.IsValid())return __exception;
            var id=v.GetZDO().GetString("RR_CharId", "");
            if(string.IsNullOrEmpty(id) || !MobManager.IsRegisteredMob(id))return __exception;
            FailedWorkers.Add(id);
            MobManager.UnregisterMob(id);
            WorkLog.LogError("Worker AI disabled for this NPC after failure; native AI resumes next update: " + __instance.name + " " + __exception);
            return null;
        }
    }
    [HarmonyPatch(typeof(ZNetScene), "OnDestroy")]
    class WorkerWorldExit
    {
        static void Prefix(){FailedWorkers.Clear();}
    }
}

// Living World C.O.R.E. - RRR actor lifecycle diagnostic.
//
// Two parts with different safety classes:
//   1. Observer hooks (General.Enabled, default false). With Enabled=false no Harmony patch is applied.
//      Hooks only read and log. Every hook body is wrapped; a diagnostic fault is logged once per
//      hook/fault class as "RRR-LIFE DIAG-FAULT" and never escapes. Every finalizer returns the
//      incoming exception unchanged, even when its own inspection fails.
//   2. Fixture commands (Fixture.EnableFixtureCommands, default false, separate switch). These MUTATE
//      the world: they instantiate one allowlisted actor and remove it through ZNetScene.Destroy.
//      Single-player only, exact allowlist, one fixture at a time, distance clamped.
//
// Tracking is fixed to three actors: RRRN_WoodsSoul (working reference), RRRN_NinoBosque, RRRN_OrcBrute.
// Registration history is kept only for ZNetViews whose own prefab or root prefab is one of them.
// A RemoveObjects failure still reports any bad scene entry generically, even outside that set.
//
// Native facts relied on (assembly_valheim metadata / IL call lists, SOURCE-INSPECTED):
//   * ZNetView.Awake registers via ZNetScene.AddInstance(m_zdo).
//   * ZNetView.OnDestroy does not unregister; only ZNetScene.Destroy / OnZDODestroyed /
//     RemoveObjects / Shutdown call ResetZDO and remove the m_instances entry.
// Unity may defer destruction, so the stack captured in ZNetView.OnDestroy is the destruction
// callback path. It does not necessarily contain the original caller of Object.Destroy.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.RRRLifecycleDiag
{
    [BepInPlugin(Guid, "LWC RRR Lifecycle Diagnostic", Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "livingworld.core.rrrlifecyclediag";
        public const string Version = "0.2.0";

        // Exact tracked set and fixture allowlist (the same three names). Read by the contract check.
        public static readonly string[] TrackedActors = { "RRRN_WoodsSoul", "RRRN_NinoBosque", "RRRN_OrcBrute" };

        public const float FixtureDefaultDistance = 12f;
        public const float FixtureMinDistance = 8f;
        public const float FixtureMaxDistance = 30f;
        public const int FixtureDefaultSeed = 1337;

        // Hook table: "Type|Method|prefix|postfix|finalizer" ("-" = none). Read by the contract check.
        public static readonly string[] HookTable =
        {
            "ZNetView|Awake|-|ViewAwakePostfix|-",
            "ZNetView|OnDestroy|ViewOnDestroyPrefix|-|-",
            "ZNetScene|Destroy|SceneDestroyPrefix|-|-",
            "ZNetScene|OnZDODestroyed|SceneZdoDestroyedPrefix|-|-",
            "ZNetScene|RemoveObjects|-|-|RemoveObjectsFinalizer",
            "ZNetScene|OnDestroy|SceneOnDestroyPrefix|-|-",
            "Character|Awake|-|CharacterAwakePostfix|CharacterAwakeFinalizer",
            "BaseAI|Awake|-|BaseAIAwakePostfix|BaseAIAwakeFinalizer",
            "Humanoid|Start|HumanoidStartPrefix|HumanoidStartPostfix|HumanoidStartFinalizer",
            "Humanoid|GiveDefaultItems|-|GiveDefaultItemsPostfix|GiveDefaultItemsFinalizer",
            "Character|OnDestroy|CharacterOnDestroyPrefix|-|-",
            "VisEquipment|AttachItem|-|AttachItemPostfix|AttachItemFinalizer",
            "Player|OnSpawned|-|PlayerSpawnedPostfix|-",
            "Terminal|InitTerminal|-|InitTerminalPostfix|-",
        };

        const int MaxFaultLines = 40;

        static ManualLogSource log;
        static ConfigEntry<bool> cfgEnabled;
        static ConfigEntry<bool> cfgAutoAudit;
        static ConfigEntry<int> cfgStackDepth;
        static ConfigEntry<bool> cfgFixtures;

        static readonly HashSet<string> tracked = new HashSet<string>(TrackedActors, StringComparer.Ordinal);

        // Per-world state (cleared by ResetWorld).
        static readonly Dictionary<int, ViewInfo> views = new Dictionary<int, ViewInfo>();
        static readonly HashSet<string> nestedSeen = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> spawnStackSeen = new HashSet<string>(StringComparer.Ordinal);
        static int removeErrors;
        static float removeWindowStart = -1f;
        static float lastRemoveScan = -10f;
        static bool audited;
        static int worldScene;

        // Fixture state survives a reset only long enough to be reported.
        static ZNetView fixtureView;
        static string fixturePrefab;
        static string fixtureZdo;
        static int fixtureSeed;
        static bool fixtureSeedPending;
        static bool seedApplied;
        static UnityEngine.Random.State seedSavedState;

        // Process state.
        static readonly HashSet<string> faultSeen = new HashSet<string>(StringComparer.Ordinal);
        static int faultLines;
        static System.Reflection.FieldInfo zdoField;
        static System.Reflection.FieldInfo instancesField;

        sealed class ViewInfo
        {
            public string Prefab;
            public string Root;
            public string Path;
            public float Time;
        }

        void Awake()
        {
            log = Logger;
            cfgEnabled = Config.Bind("General", "Enabled", false,
                "Observer hooks. Off by default. When false, no hooks are installed and no command is registered.");
            cfgAutoAudit = Config.Bind("General", "AutoAudit", true,
                "Static prefab audit once per world, on the local player's first spawn. Reads prefabs only.");
            cfgStackDepth = Config.Bind("General", "StackDepth", 14, "Frames kept when a stack is logged (4-40).");
            cfgFixtures = Config.Bind("Fixture", "EnableFixtureCommands", false,
                "Separate switch. Enables 'lwc_rrrdiag fixture', which SPAWNS one allowlisted actor. Single-player only.");

            if (!cfgEnabled.Value)
            {
                log.LogInfo("RRR-LIFE disabled (General.Enabled=false). No hooks installed.");
                return;
            }

            zdoField = AccessTools.Field(typeof(ZNetView), "m_zdo");
            instancesField = AccessTools.Field(typeof(ZNetScene), "m_instances");
            if (zdoField == null || zdoField.FieldType != typeof(ZDO)
                || instancesField == null || instancesField.FieldType != typeof(Dictionary<ZDO, ZNetView>))
            {
                log.LogError("RRR-LIFE native fields changed (ZNetView.m_zdo / ZNetScene.m_instances). Not arming.");
                return;
            }

            Harmony h = new Harmony(Guid);
            int ok = 0;
            foreach (string row in HookTable)
                if (ApplyHook(h, row)) ok++;
            log.LogInfo("RRR-LIFE armed " + ok + "/" + HookTable.Length + " hooks. Tracking: "
                + string.Join(",", TrackedActors) + ". Fixture commands: "
                + (cfgFixtures.Value ? "ENABLED (mutating)" : "disabled"));
        }

        static bool ApplyHook(Harmony h, string row)
        {
            string[] p = row.Split('|');
            try
            {
                Type type = AccessTools.TypeByName(p[0]);
                System.Reflection.MethodInfo target = type == null ? null : AccessTools.Method(type, p[1]);
                if (target == null) { log.LogWarning("RRR-LIFE hook missing: " + p[0] + "." + p[1]); return false; }
                h.Patch(target,
                    prefix: Handler(p[2]),
                    postfix: Handler(p[3]),
                    finalizer: Handler(p[4]));
                return true;
            }
            catch (Exception ex)
            {
                log.LogWarning("RRR-LIFE hook failed: " + p[0] + "." + p[1] + " " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        static HarmonyMethod Handler(string name)
        {
            return name == "-" ? null : new HarmonyMethod(typeof(Plugin), name);
        }

        // ---------- fault containment ----------

        static void Fault(string hook, Exception ex)
        {
            try
            {
                string key = hook + "|" + ex.GetType().FullName;
                if (faultLines >= MaxFaultLines || !faultSeen.Add(key)) return;
                faultLines++;
                log.LogWarning("RRR-LIFE DIAG-FAULT hook=" + hook + " " + ex.GetType().Name + ": " + ex.Message
                    + " at " + FirstFrame(ex) + (faultLines == MaxFaultLines ? " (further faults suppressed)" : ""));
            }
            catch
            {
                // Logging itself failed; nothing further is attempted.
            }
        }

        // ---------- identity helpers ----------

        static ZDO ZdoOf(ZNetView view) { return (ZDO)zdoField.GetValue(view); }

        static Dictionary<ZDO, ZNetView> Instances(ZNetScene scene)
        {
            return (Dictionary<ZDO, ZNetView>)instancesField.GetValue(scene);
        }

        static string PrefabOf(GameObject go)
        {
            return go == null ? "<null>" : Utils.GetPrefabName(go);
        }

        static bool IsTracked(string prefab) { return prefab != null && tracked.Contains(prefab); }

        static string ZdoText(ZDO zdo)
        {
            return zdo == null ? "zdo=<null>" : "zdo=" + zdo.m_uid.ToString() + " prefabHash=" + zdo.GetPrefab();
        }

        static string PathOf(Transform t)
        {
            StringBuilder sb = new StringBuilder();
            while (t != null)
            {
                if (sb.Length > 0) sb.Insert(0, "/");
                sb.Insert(0, t.name);
                t = t.parent;
            }
            return sb.ToString();
        }

        static string Stack()
        {
            StackTrace st = new StackTrace(2, false);
            StringBuilder sb = new StringBuilder();
            int n = Math.Min(st.FrameCount, Mathf.Clamp(cfgStackDepth.Value, 4, 40));
            for (int i = 0; i < n; ++i)
            {
                System.Reflection.MethodBase m = st.GetFrame(i).GetMethod();
                if (m == null) continue;
                sb.Append(" <- ").Append(m.DeclaringType == null ? "?" : m.DeclaringType.FullName).Append(".").Append(m.Name);
            }
            return sb.ToString();
        }

        static string FirstFrame(Exception ex)
        {
            string s = ex == null ? null : ex.StackTrace;
            if (string.IsNullOrEmpty(s)) return "?";
            int nl = s.IndexOf('\n');
            return (nl > 0 ? s.Substring(0, nl) : s).Trim();
        }

        static string Who(Component c)
        {
            if (c == null) return "<destroyed>";
            GameObject root = c.transform.root.gameObject;
            return PrefabOf(root) + " id=" + root.GetInstanceID();
        }

        static bool RootTracked(Component c)
        {
            return c != null && IsTracked(PrefabOf(c.transform.root.gameObject));
        }

        static void Line(string text)
        {
            log.LogInfo("RRR-LIFE f=" + Time.frameCount + " t=" + Time.time.ToString("F2", CultureInfo.InvariantCulture) + " " + text);
        }

        static bool StillRegistered(ZNetView view, ZDO zdo)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || zdo == null) return false;
            ZNetView registered;
            return Instances(scene).TryGetValue(zdo, out registered) && ReferenceEquals(registered, view);
        }

        // ---------- per-world reset ----------

        static void ResetWorld(string reason)
        {
            if (fixturePrefab != null)
            {
                bool alive = fixtureView != null;
                Line("FIXTURE ABANDONED at " + reason + " prefab=" + fixturePrefab + " " + fixtureZdo
                    + " alive=" + alive + " (not removed via lwc_rrrdiag remove)");
                fixtureView = null;
                fixturePrefab = null;
                fixtureZdo = null;
                fixtureSeedPending = false;
            }
            int n = views.Count;
            views.Clear();
            nestedSeen.Clear();
            spawnStackSeen.Clear();
            removeErrors = 0;
            removeWindowStart = -1f;
            lastRemoveScan = -10f;
            audited = false;
            worldScene = 0;
            Line("RESET reason=" + reason + " clearedViews=" + n);
        }

        static void SceneOnDestroyPrefix(ZNetScene __instance)
        {
            try { ResetWorld("world-teardown"); }
            catch (Exception ex) { Fault("ZNetScene.OnDestroy", ex); }
        }

        // ---------- network registration ----------

        static void ViewAwakePostfix(ZNetView __instance)
        {
            try
            {
                if (__instance == null) return;
                GameObject go = __instance.gameObject;
                string prefab = PrefabOf(go);
                string rootPrefab = PrefabOf(go.transform.root.gameObject);
                if (!IsTracked(prefab) && !IsTracked(rootPrefab)) return;

                bool nested = go.transform.parent != null;
                ZDO zdo = ZdoOf(__instance);
                ViewInfo info = new ViewInfo();
                info.Prefab = prefab;
                info.Root = rootPrefab;
                info.Path = nested ? PathOf(go.transform) : prefab;
                info.Time = Time.time;
                views[__instance.GetInstanceID()] = info;

                if (nested)
                {
                    if (nestedSeen.Add(rootPrefab + "|" + info.Path))
                        Line("NESTED-ZNETVIEW root=" + rootPrefab + " path=" + info.Path + " " + ZdoText(zdo)
                            + " view=" + __instance.GetInstanceID() + " registeredBy" + Stack());
                    return;
                }
                bool first = spawnStackSeen.Add(prefab);
                Line("REGISTER prefab=" + prefab + " view=" + __instance.GetInstanceID() + " go=" + go.GetInstanceID()
                    + " " + ZdoText(zdo) + " registered=" + StillRegistered(__instance, zdo)
                    + " active=" + go.activeInHierarchy
                    + " pos=" + go.transform.position.ToString("F1")
                    + (first ? " instantiatedBy" + Stack() : ""));
            }
            catch (Exception ex) { Fault("ZNetView.Awake", ex); }
        }

        static void ViewOnDestroyPrefix(ZNetView __instance)
        {
            try
            {
                int id = __instance.GetInstanceID();
                ViewInfo info;
                bool known = views.TryGetValue(id, out info);
                ZDO zdo = ZdoOf(__instance);
                bool leak = StillRegistered(__instance, zdo);
                if (leak && (known || RootTracked(__instance)))
                {
                    // Destruction callback path only; Unity may have deferred the original Object.Destroy.
                    Line("LEAK destroyed-while-registered prefab=" + (known ? info.Prefab : PrefabOf(__instance.gameObject))
                        + " root=" + (known ? info.Root : PrefabOf(__instance.transform.root.gameObject))
                        + " view=" + id + " " + ZdoText(zdo)
                        + " age=" + (known ? (Time.time - info.Time).ToString("F2", CultureInfo.InvariantCulture) : "?")
                        + " destroyCallbackPath" + Stack());
                }
                else if (known && !leak)
                {
                    Line("UNREGISTERED-DESTROY prefab=" + info.Prefab + " view=" + id + " " + ZdoText(zdo)
                        + " age=" + (Time.time - info.Time).ToString("F2", CultureInfo.InvariantCulture));
                }
                if (known && !leak) views.Remove(id);
            }
            catch (Exception ex) { Fault("ZNetView.OnDestroy", ex); }
        }

        static void SceneDestroyPrefix(GameObject go)
        {
            try
            {
                if (go == null) return;
                string prefab = PrefabOf(go);
                if (!IsTracked(prefab)) return;
                ZNetView view = go.GetComponent<ZNetView>();
                Line("NET-DESTROY prefab=" + prefab + " go=" + go.GetInstanceID() + " "
                    + (view == null ? "no-znetview" : ZdoText(ZdoOf(view))) + " caller" + Stack());
            }
            catch (Exception ex) { Fault("ZNetScene.Destroy", ex); }
        }

        static void SceneZdoDestroyedPrefix(ZNetScene __instance, ZDO zdo)
        {
            try
            {
                if (zdo == null) return;
                ZNetView view;
                if (!Instances(__instance).TryGetValue(zdo, out view)) return;
                bool known = (object)view != null && views.ContainsKey(view.GetInstanceID());
                bool destroyed = (object)view != null && view == null;
                if (known || destroyed)
                    Line("ZDO-DESTROYED " + ZdoText(zdo) + " view=" + ((object)view == null ? "<none>" : DescribeView(view)));
            }
            catch (Exception ex) { Fault("ZNetScene.OnZDODestroyed", ex); }
        }

        static string DescribeView(ZNetView view)
        {
            int id = view.GetInstanceID();
            ViewInfo info;
            if (views.TryGetValue(id, out info))
                return info.Prefab + " root=" + info.Root + " path=" + info.Path + " view=" + id + (view == null ? " DESTROYED" : "");
            if (view == null) return "<untracked, destroyed> view=" + id;
            return "<untracked> " + PrefabOf(view.gameObject) + " root=" + PrefabOf(view.transform.root.gameObject) + " view=" + id;
        }

        static Exception RemoveObjectsFinalizer(ZNetScene __instance, Exception __exception)
        {
            if (__exception == null) return null;
            try
            {
                float now = Time.time;
                if (removeWindowStart < 0f || now - removeWindowStart > 5f)
                {
                    if (removeErrors > 0) Line("REMOVEOBJECTS-ERRORS count=" + removeErrors + " window=5s");
                    removeErrors = 0;
                    removeWindowStart = now;
                }
                removeErrors++;
                if (now - lastRemoveScan >= 1f)
                {
                    lastRemoveScan = now;
                    ScanInstances(__instance, __exception);
                }
            }
            catch (Exception ex) { Fault("ZNetScene.RemoveObjects", ex); }
            return __exception;
        }

        static void ScanInstances(ZNetScene scene, Exception ex)
        {
            int bad = 0;
            foreach (KeyValuePair<ZDO, ZNetView> kv in Instances(scene))
            {
                ZNetView view = kv.Value;
                string why = null;
                if ((object)view == null) why = "null-view";
                else if (view == null) why = "view-destroyed-outside-ZNetScene";
                else
                {
                    ZDO z = ZdoOf(view);
                    if (z == null) why = "view-zdo-null";
                    else if (!ReferenceEquals(z, kv.Key)) why = "view-zdo-mismatch";
                }
                if (why == null) continue;
                if (++bad <= 12)
                    Line("BAD-INSTANCE " + why + " key=" + ZdoText(kv.Key) + " "
                        + ((object)view == null ? "" : DescribeView(view)));
            }
            Line("SCAN bad=" + bad + " cause=" + ex.GetType().Name + ": " + ex.Message + " at " + FirstFrame(ex));
        }

        // ---------- actor stages ----------

        static void Stage(Component c, string stage, Exception ex)
        {
            if (!RootTracked(c)) return;
            if (ex == null) Line("STAGE " + stage + " ok " + Who(c));
            else Line("STAGE " + stage + " THREW " + Who(c) + " " + ex.GetType().Name + ": " + ex.Message + " at " + FirstFrame(ex));
        }

        static void CharacterAwakePostfix(Character __instance)
        {
            try { Stage(__instance, "Character.Awake", null); }
            catch (Exception ex) { Fault("Character.Awake", ex); }
        }
        static Exception CharacterAwakeFinalizer(Character __instance, Exception __exception)
        {
            try { if (__exception != null) Stage(__instance, "Character.Awake", __exception); }
            catch (Exception ex) { Fault("Character.Awake.finalizer", ex); }
            return __exception;
        }

        static void BaseAIAwakePostfix(BaseAI __instance)
        {
            try { Stage(__instance, "BaseAI.Awake", null); }
            catch (Exception ex) { Fault("BaseAI.Awake", ex); }
        }
        static Exception BaseAIAwakeFinalizer(BaseAI __instance, Exception __exception)
        {
            try { if (__exception != null) Stage(__instance, "BaseAI.Awake", __exception); }
            catch (Exception ex) { Fault("BaseAI.Awake.finalizer", ex); }
            return __exception;
        }

        // Fixture only: Humanoid.Start picks random gear one frame after Instantiate, so the fixture seed is
        // re-applied for that one call and the global Random state is restored in the finalizer.
        static void HumanoidStartPrefix(Humanoid __instance)
        {
            try
            {
                if (!fixtureSeedPending || fixtureView == null || __instance == null
                    || __instance.gameObject != fixtureView.gameObject) return;
                fixtureSeedPending = false;
                seedSavedState = UnityEngine.Random.state;
                seedApplied = true;
                UnityEngine.Random.InitState(fixtureSeed);
            }
            catch (Exception ex) { Fault("Humanoid.Start.prefix", ex); }
        }

        static void HumanoidStartPostfix(Humanoid __instance)
        {
            try { Stage(__instance, "Humanoid.Start", null); }
            catch (Exception ex) { Fault("Humanoid.Start", ex); }
        }
        static Exception HumanoidStartFinalizer(Humanoid __instance, Exception __exception)
        {
            try
            {
                if (seedApplied)
                {
                    seedApplied = false;
                    UnityEngine.Random.state = seedSavedState;
                }
            }
            catch (Exception ex) { Fault("Humanoid.Start.seedRestore", ex); }
            try { if (__exception != null) Stage(__instance, "Humanoid.Start", __exception); }
            catch (Exception ex) { Fault("Humanoid.Start.finalizer", ex); }
            return __exception;
        }

        static void GiveDefaultItemsPostfix(Humanoid __instance)
        {
            try { Stage(__instance, "Humanoid.GiveDefaultItems", null); }
            catch (Exception ex) { Fault("Humanoid.GiveDefaultItems", ex); }
        }
        static Exception GiveDefaultItemsFinalizer(Humanoid __instance, Exception __exception)
        {
            try { if (__exception != null) Stage(__instance, "Humanoid.GiveDefaultItems", __exception); }
            catch (Exception ex) { Fault("Humanoid.GiveDefaultItems.finalizer", ex); }
            return __exception;
        }

        static void CharacterOnDestroyPrefix(Character __instance)
        {
            try
            {
                if (!RootTracked(__instance)) return;
                ZNetView view = __instance.GetComponent<ZNetView>();
                ZDO z = view == null ? null : ZdoOf(view);
                Line("CHARACTER-DESTROY " + Who(__instance) + " "
                    + (view == null ? "no-znetview" : ZdoText(z) + " registered=" + StillRegistered(view, z)));
            }
            catch (Exception ex) { Fault("Character.OnDestroy", ex); }
        }

        static void AttachItemPostfix(VisEquipment __instance, int itemHash, GameObject __result)
        {
            try
            {
                if (!RootTracked(__instance)) return;
                GameObject itemPrefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(itemHash);
                int nets = __result == null ? 0 : __result.GetComponentsInChildren<ZNetView>(true).Length;
                Line("ATTACH " + Who(__instance) + " item=" + (itemPrefab != null ? itemPrefab.name : "hash" + itemHash)
                    + " result=" + (__result == null ? "<null>" : __result.name) + " znetviews=" + nets);
            }
            catch (Exception ex) { Fault("VisEquipment.AttachItem", ex); }
        }

        static Exception AttachItemFinalizer(VisEquipment __instance, int itemHash, Exception __exception)
        {
            if (__exception == null) return null;
            try
            {
                if (RootTracked(__instance))
                    Line("ATTACH THREW " + Who(__instance) + " itemHash=" + itemHash + " "
                        + __exception.GetType().Name + ": " + __exception.Message + " at " + FirstFrame(__exception));
            }
            catch (Exception ex) { Fault("VisEquipment.AttachItem.finalizer", ex); }
            return __exception;
        }

        // ---------- static audit ----------

        static void PlayerSpawnedPostfix(Player __instance)
        {
            try
            {
                if (__instance == null || __instance != Player.m_localPlayer) return;
                int scene = ZNetScene.instance == null ? 0 : ZNetScene.instance.GetInstanceID();
                if (worldScene != 0 && worldScene != scene) ResetWorld("new-world-spawn");
                worldScene = scene;
                if (audited || !cfgAutoAudit.Value) return;
                audited = true;
                Audit(null);
            }
            catch (Exception ex) { Fault("Player.OnSpawned", ex); }
        }

        static void Audit(string only)
        {
            if (ZNetScene.instance == null) { Line("AUDIT skipped: no ZNetScene"); return; }
            foreach (string name in TrackedActors)
            {
                if (only != null && only != name) continue;
                try { AuditActor(name); }
                catch (Exception ex) { Fault("Audit." + name, ex); }
            }
        }

        static void AuditActor(string name)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null) { Line("AUDIT " + name + " NOT-REGISTERED in ZNetScene"); return; }
            ZNetView[] nets = prefab.GetComponentsInChildren<ZNetView>(true);
            int missing = 0;
            foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                foreach (Component c in t.GetComponents<Component>())
                    if (c == null) { missing++; Line("AUDIT " + name + " MISSING-SCRIPT at " + PathOf(t)); }
            Line("AUDIT " + name + " active=" + prefab.activeSelf + " znetviews=" + nets.Length + " missingScripts=" + missing);
            foreach (ZNetView n in nets)
                if (n.transform != prefab.transform) Line("AUDIT " + name + " NESTED-ZNETVIEW " + PathOf(n.transform));

            Humanoid hum = prefab.GetComponent<Humanoid>();
            if (hum == null) { Line("AUDIT " + name + " no Humanoid"); return; }
            AuditItems(name, "default", hum.m_defaultItems);
            AuditItems(name, "randomWeapon", hum.m_randomWeapon);
            AuditItems(name, "randomArmor", hum.m_randomArmor);
            AuditItems(name, "randomShield", hum.m_randomShield);
            if (hum.m_randomSets != null)
                for (int i = 0; i < hum.m_randomSets.Length; ++i)
                {
                    Humanoid.ItemSet set = hum.m_randomSets[i];
                    AuditItems(name, "set" + i + ":" + (set == null ? "<null>" : set.m_name), set == null ? null : set.m_items);
                }
            if (hum.m_randomItems != null)
                for (int i = 0; i < hum.m_randomItems.Length; ++i)
                    AuditItems(name, "randomItem" + i,
                        new GameObject[] { hum.m_randomItems[i] == null ? null : hum.m_randomItems[i].m_prefab });
        }

        static void AuditItems(string owner, string slot, GameObject[] items)
        {
            if (items == null) { Line("AUDIT " + owner + " " + slot + " array=<null>"); return; }
            for (int i = 0; i < items.Length; ++i)
            {
                GameObject item = items[i];
                if (item == null) { Line("AUDIT " + owner + " " + slot + "[" + i + "] NULL-ITEM"); continue; }
                ItemDrop drop = item.GetComponent<ItemDrop>();
                bool inDb = ObjectDB.instance != null && ObjectDB.instance.GetItemPrefab(item.name) != null;
                int childNets = 0;
                foreach (ZNetView n in item.GetComponentsInChildren<ZNetView>(true))
                    if (n.transform != item.transform) childNets++;
                int missing = 0;
                foreach (Transform t in item.GetComponentsInChildren<Transform>(true))
                    foreach (Component c in t.GetComponents<Component>())
                        if (c == null) missing++;
                string type = drop == null || drop.m_itemData == null || drop.m_itemData.m_shared == null
                    ? "no-ItemDrop" : drop.m_itemData.m_shared.m_itemType.ToString();
                Line("AUDIT " + owner + " " + slot + "[" + i + "] " + item.name + " type=" + type
                    + " inObjectDB=" + inDb + " childZNetViews=" + childNets + " missingScripts=" + missing);
            }
        }

        // ---------- console ----------

        static void InitTerminalPostfix()
        {
            try
            {
                new Terminal.ConsoleCommand("lwc_rrrdiag",
                    "audit [actor] | scan | fixture <actor> [dist] [seed] | remove  (RRR lifecycle diagnostic)",
                    new Terminal.ConsoleEvent(Command), false, false, false, false, false, false, null, false, false, false);
            }
            catch (Exception ex) { Fault("Terminal.InitTerminal", ex); }
        }

        static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            try { if (args != null && args.Context != null) args.Context.AddString(text); } catch { }
            try { Line("CMD " + text); } catch { }
        }

        static void Command(Terminal.ConsoleEventArgs args)
        {
            try
            {
                string[] a = args.Args;
                string verb = a.Length > 1 ? a[1] : "";
                if (verb == "audit")
                {
                    string only = a.Length > 2 ? a[2] : null;
                    if (only != null && !tracked.Contains(only)) { Say(args, "audit: not a tracked actor: " + only); return; }
                    Audit(only);
                    return;
                }
                if (verb == "scan")
                {
                    if (ZNetScene.instance != null) ScanInstances(ZNetScene.instance, new Exception("manual scan"));
                    return;
                }
                if (verb == "remove") { RemoveFixture(args); return; }
                if (verb == "fixture") { Fixture(args); return; }
                Say(args, "lwc_rrrdiag audit [actor] | scan | fixture <actor> [dist " + FixtureMinDistance + "-"
                    + FixtureMaxDistance + "] [seed] | remove");
            }
            catch (Exception ex) { Fault("lwc_rrrdiag", ex); }
        }

        static string SinglePlayerRefusal()
        {
            ZNet net = ZNet.instance;
            if (net == null) return "no ZNet";
            if (!net.IsServer() || net.IsDedicated()) return "not a local single-player host";
            if (net.GetPeers().Count > 0) return "peers connected (" + net.GetPeers().Count + ")";
            if (Player.GetAllPlayers().Count > 1) return "more than one player present";
            return null;
        }

        public static float ParseDistance(string text, out bool fellBack)
        {
            fellBack = false;
            if (text == null) return FixtureDefaultDistance;
            float d;
            if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                || float.IsNaN(d) || float.IsInfinity(d))
            {
                fellBack = true;
                return FixtureDefaultDistance;
            }
            return Mathf.Clamp(d, FixtureMinDistance, FixtureMaxDistance);
        }

        static void Fixture(Terminal.ConsoleEventArgs args)
        {
            string[] a = args.Args;
            if (!cfgFixtures.Value) { Say(args, "fixture refused: Fixture.EnableFixtureCommands=false"); return; }
            if (a.Length < 3 || !tracked.Contains(a[2]))
            {
                Say(args, "fixture refused: actor must be exactly one of " + string.Join(", ", TrackedActors));
                return;
            }
            if (fixturePrefab != null)
            {
                Say(args, "fixture refused: " + fixturePrefab + " still active; run 'lwc_rrrdiag remove' first");
                return;
            }
            string refusal = SinglePlayerRefusal();
            if (refusal != null) { Say(args, "fixture refused: " + refusal); return; }
            Player p = Player.m_localPlayer;
            if (p == null || ZNetScene.instance == null) { Say(args, "fixture refused: no local player / ZNetScene"); return; }

            string name = a[2];
            bool fellBack;
            float dist = ParseDistance(a.Length > 3 ? a[3] : null, out fellBack);
            if (fellBack) Say(args, "distance '" + a[3] + "' invalid; using " + FixtureDefaultDistance + " m");
            int seed = FixtureDefaultSeed;
            if (a.Length > 4 && !int.TryParse(a[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out seed))
            {
                seed = FixtureDefaultSeed;
                Say(args, "seed '" + a[4] + "' invalid; using " + FixtureDefaultSeed);
            }

            GameObject prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null) { Say(args, "fixture refused: " + name + " NOT-REGISTERED"); return; }

            Vector3 fwd = p.transform.forward; fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
            fwd.Normalize();
            Vector3 pos = p.transform.position + fwd * dist;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(pos, out h)) pos.y = h + 0.5f;

            Line("FIXTURE spawn prefab=" + name + " seed=" + seed + " dist=" + dist.ToString("F1", CultureInfo.InvariantCulture)
                + " pos=" + pos.ToString("F1"));
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            GameObject go = null;
            try
            {
                UnityEngine.Random.InitState(seed);
                go = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.LookRotation(-fwd));
            }
            catch (Exception ex)
            {
                Line("FIXTURE spawn THREW " + ex.GetType().Name + ": " + ex.Message + " at " + FirstFrame(ex));
            }
            finally
            {
                UnityEngine.Random.state = saved;
            }
            if (go == null) { Line("FIXTURE result=<null>"); return; }
            ZNetView view = go.GetComponent<ZNetView>();
            ZDO z = view == null ? null : ZdoOf(view);
            fixtureView = view;
            fixturePrefab = name;
            fixtureSeed = seed;
            fixtureSeedPending = view != null;
            fixtureZdo = ZdoText(z);
            Line("FIXTURE result go=" + go.GetInstanceID() + " " + (view == null ? "no-znetview" : fixtureZdo
                + " registered=" + StillRegistered(view, z)));
            Say(args, "fixture " + name + " spawned at " + dist.ToString("F1", CultureInfo.InvariantCulture)
                + " m; remove it with 'lwc_rrrdiag remove' before the next one");
        }

        // Available whenever a fixture exists, regardless of EnableFixtureCommands.
        static void RemoveFixture(Terminal.ConsoleEventArgs args)
        {
            if (fixturePrefab == null) { Say(args, "remove: no diagnostic fixture active"); return; }
            string prefab = fixturePrefab;
            ZNetView view = fixtureView;
            if (view == null)
            {
                Line("FIXTURE remove prefab=" + prefab + " " + fixtureZdo + " already-destroyed (outside lwc_rrrdiag)");
            }
            else
            {
                ZDO z = ZdoOf(view);
                Line("FIXTURE remove prefab=" + prefab + " " + ZdoText(z) + " registered=" + StillRegistered(view, z)
                    + " via ZNetScene.Destroy");
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(view.gameObject);
            }
            fixtureView = null;
            fixturePrefab = null;
            fixtureZdo = null;
            fixtureSeedPending = false;
            Say(args, "fixture " + prefab + " removed");
        }
    }
}

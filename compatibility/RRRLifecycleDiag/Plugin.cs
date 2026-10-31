// Living World C.O.R.E. - RRR actor lifecycle diagnostic.
//
// Off by default. When Enabled=false no Harmony patch is applied and the plugin
// does nothing but read its config. It never changes game behaviour: every hook
// is an observer; finalizers return the original exception unchanged.
//
// Purpose: name the exact stage at which RRRN_NinoBosque / RRRN_OrcBrute fail,
// compared with RRRN_WoodsSoul, and identify the ZNetView that ZNetScene.RemoveObjects
// trips over (the 263/299-per-burst NullReferenceException).
//
// Native facts this instrument relies on (from assembly_valheim metadata, IL call lists):
//   * ZNetView.Awake registers via ZNetScene.AddInstance(m_zdo).
//   * Only ZNetScene.Destroy / OnZDODestroyed / RemoveObjects / Shutdown unregister
//     (they ResetZDO then Remove from m_instances).
//   * ZNetView.OnDestroy does NOT unregister.
//   * RemoveObjects reads view.GetZDO().TempRemoveEarmark for every registered view,
//     then for removed ones ResetZDO -> view.gameObject -> Destroy -> m_instances.Remove.
// So an RemoveObjects NRE means a registered view whose m_zdo is null, or a registered
// view whose GameObject was destroyed outside ZNetScene. Both are logged with names here.
//
// Log prefix: "RRR-LIFE". One line per event; stacks are trimmed.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public const string Version = "0.1.0";

        static ManualLogSource log;
        static ConfigEntry<bool> cfgEnabled;
        static ConfigEntry<string> cfgPrefabs;
        static ConfigEntry<bool> cfgTrackAllRrrn;
        static ConfigEntry<int> cfgStackDepth;
        static ConfigEntry<bool> cfgAutoAudit;

        static readonly HashSet<string> tracked = new HashSet<string>(StringComparer.Ordinal);
        static readonly Dictionary<int, ViewInfo> views = new Dictionary<int, ViewInfo>();
        static readonly HashSet<string> nestedSeen = new HashSet<string>(StringComparer.Ordinal);
        static readonly HashSet<string> spawnStackSeen = new HashSet<string>(StringComparer.Ordinal);
        static readonly List<ZNetView> fixtures = new List<ZNetView>();

        static System.Reflection.FieldInfo zdoField;
        static System.Reflection.FieldInfo instancesField;

        static ZDO zdoRef(ZNetView view) { return (ZDO)zdoField.GetValue(view); }
        static Dictionary<ZDO, ZNetView> instancesRef(ZNetScene scene)
        { return (Dictionary<ZDO, ZNetView>)instancesField.GetValue(scene); }

        static int removeErrors;
        static float removeWindowStart = -1f;
        static float lastRemoveScan = -10f;
        static bool audited;

        sealed class ViewInfo
        {
            public string Prefab;
            public string Root;
            public string Path;
            public string Zdo;
            public int Frame;
            public float Time;
            public bool Tracked;
            public bool Nested;
        }

        void Awake()
        {
            log = Logger;
            cfgEnabled = Config.Bind("General", "Enabled", false,
                "Off by default. When false, no hooks are installed.");
            cfgPrefabs = Config.Bind("General", "Prefabs",
                "RRRN_NinoBosque,RRRN_OrcBrute,RRRN_WoodsSoul",
                "Comma-separated root prefab names to trace in full.");
            cfgTrackAllRrrn = Config.Bind("General", "TrackAllRRRN", false,
                "Also trace every root prefab whose name starts with RRRN_.");
            cfgStackDepth = Config.Bind("General", "StackDepth", 14,
                "Frames kept when a stack trace is logged.");
            cfgAutoAudit = Config.Bind("General", "AutoAudit", true,
                "Run the static prefab audit once, on first local player spawn.");

            if (!cfgEnabled.Value)
            {
                log.LogInfo("RRR-LIFE disabled (Enabled=false). No hooks installed.");
                return;
            }

            foreach (string raw in cfgPrefabs.Value.Split(','))
            {
                string name = raw.Trim();
                if (name.Length > 0) tracked.Add(name);
            }

            zdoField = AccessTools.Field(typeof(ZNetView), "m_zdo");
            instancesField = AccessTools.Field(typeof(ZNetScene), "m_instances");
            if (zdoField == null || instancesField == null)
            {
                log.LogError("RRR-LIFE native fields changed (m_zdo / m_instances). Not arming.");
                return;
            }

            Harmony h = new Harmony(Guid);
            Patch(h, typeof(ZNetView), "Awake", null, "ViewAwakePostfix", null);
            Patch(h, typeof(ZNetView), "OnDestroy", "ViewOnDestroyPrefix", null, null);
            Patch(h, typeof(ZNetScene), "Destroy", "SceneDestroyPrefix", null, null);
            Patch(h, typeof(ZNetScene), "OnZDODestroyed", "SceneZdoDestroyedPrefix", null, null);
            Patch(h, typeof(ZNetScene), "RemoveObjects", null, null, "RemoveObjectsFinalizer");
            Patch(h, typeof(Character), "Awake", null, "StagePostfix_CharacterAwake", "StageFinalizer_CharacterAwake");
            Patch(h, typeof(BaseAI), "Awake", null, "StagePostfix_BaseAIAwake", "StageFinalizer_BaseAIAwake");
            Patch(h, typeof(Humanoid), "Start", null, "StagePostfix_HumanoidStart", "StageFinalizer_HumanoidStart");
            Patch(h, typeof(Humanoid), "GiveDefaultItems", null, "StagePostfix_GiveDefaultItems", "StageFinalizer_GiveDefaultItems");
            Patch(h, typeof(Character), "OnDestroy", "CharacterOnDestroyPrefix", null, null);
            Patch(h, typeof(VisEquipment), "AttachItem", null, "AttachItemPostfix", "AttachItemFinalizer");
            Patch(h, typeof(Player), "OnSpawned", null, "PlayerSpawnedPostfix", null);
            Patch(h, typeof(Terminal), "InitTerminal", null, "InitTerminalPostfix", null);
            log.LogInfo("RRR-LIFE armed. Tracking: " + string.Join(",", new List<string>(tracked).ToArray())
                + (cfgTrackAllRrrn.Value ? " +all RRRN_*" : ""));
        }

        static void Patch(Harmony h, Type type, string method, string prefix, string postfix, string finalizer)
        {
            try
            {
                System.Reflection.MethodInfo target = AccessTools.Method(type, method);
                if (target == null) { log.LogWarning("RRR-LIFE hook missing: " + type.Name + "." + method); return; }
                h.Patch(target,
                    prefix: prefix == null ? null : new HarmonyMethod(typeof(Plugin), prefix),
                    postfix: postfix == null ? null : new HarmonyMethod(typeof(Plugin), postfix),
                    finalizer: finalizer == null ? null : new HarmonyMethod(typeof(Plugin), finalizer));
            }
            catch (Exception ex)
            {
                log.LogWarning("RRR-LIFE hook failed: " + type.Name + "." + method + " " + ex.Message);
            }
        }

        // ---------- identity helpers ----------

        static string PrefabOf(GameObject go)
        {
            if (go == null) return "<null>";
            return Utils.GetPrefabName(go);
        }

        static bool IsTracked(string prefab)
        {
            if (prefab == null) return false;
            if (tracked.Contains(prefab)) return true;
            return cfgTrackAllRrrn.Value && prefab.StartsWith("RRRN_", StringComparison.Ordinal);
        }

        static string ZdoText(ZDO zdo)
        {
            if (zdo == null) return "zdo=<null>";
            return "zdo=" + zdo.m_uid.ToString() + " prefabHash=" + zdo.GetPrefab();
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
            int n = Math.Min(st.FrameCount, cfgStackDepth.Value);
            for (int i = 0; i < n; ++i)
            {
                System.Reflection.MethodBase m = st.GetFrame(i).GetMethod();
                if (m == null) continue;
                sb.Append(" <- ");
                sb.Append(m.DeclaringType == null ? "?" : m.DeclaringType.FullName);
                sb.Append(".");
                sb.Append(m.Name);
            }
            return sb.ToString();
        }

        static string Who(Component c)
        {
            if (c == null) return "<destroyed>";
            Transform root = c.transform.root;
            return PrefabOf(root.gameObject) + " id=" + root.gameObject.GetInstanceID();
        }

        static bool RootTracked(Component c)
        {
            return c != null && IsTracked(PrefabOf(c.transform.root.gameObject));
        }

        static void Line(string text)
        {
            log.LogInfo("RRR-LIFE f=" + Time.frameCount + " t=" + Time.time.ToString("F2") + " " + text);
        }

        // ---------- network registration ----------

        static void ViewAwakePostfix(ZNetView __instance)
        {
            if (__instance == null) return;
            GameObject go = __instance.gameObject;
            Transform root = go.transform.root;
            bool nested = go.transform.parent != null;
            string prefab = PrefabOf(go);
            string rootPrefab = PrefabOf(root.gameObject);
            bool isTracked = IsTracked(prefab) || IsTracked(rootPrefab);
            if (!isTracked && !nested) return;

            ZDO zdo = zdoRef(__instance);
            ViewInfo info = new ViewInfo();
            info.Prefab = prefab;
            info.Root = rootPrefab;
            info.Path = nested ? PathOf(go.transform) : prefab;
            info.Zdo = ZdoText(zdo);
            info.Frame = Time.frameCount;
            info.Time = Time.time;
            info.Tracked = isTracked;
            info.Nested = nested;
            views[__instance.GetInstanceID()] = info;

            if (nested)
            {
                // A ZNetView below the root registers its own ZDO and is not removed when the
                // parent is destroyed by anything other than ZNetScene. Report once per path.
                string key = rootPrefab + "|" + info.Path;
                if (nestedSeen.Add(key))
                    Line("NESTED-ZNETVIEW root=" + rootPrefab + " path=" + info.Path + " " + info.Zdo
                        + " view=" + __instance.GetInstanceID() + Stack());
            }
            if (isTracked)
            {
                bool firstForPrefab = spawnStackSeen.Add(prefab);
                Line("REGISTER prefab=" + prefab + " view=" + __instance.GetInstanceID() + " go="
                    + go.GetInstanceID() + " " + info.Zdo + " active=" + go.activeInHierarchy
                    + " pos=" + go.transform.position.ToString("F1")
                    + (firstForPrefab ? " spawnedBy" + Stack() : ""));
            }
        }

        static bool StillRegistered(ZNetView view, ZDO zdo)
        {
            ZNetScene scene = ZNetScene.instance;
            if (scene == null || zdo == null) return false;
            ZNetView registered;
            return instancesRef(scene).TryGetValue(zdo, out registered) && ReferenceEquals(registered, view);
        }

        static void ViewOnDestroyPrefix(ZNetView __instance)
        {
            ViewInfo info;
            int id = __instance.GetInstanceID();
            bool known = views.TryGetValue(id, out info);
            ZDO zdo = zdoRef(__instance);
            bool leak = StillRegistered(__instance, zdo);
            if (leak)
            {
                Line("LEAK destroyed-while-registered prefab=" + (known ? info.Prefab : PrefabOf(__instance.gameObject))
                    + " root=" + (known ? info.Root : PrefabOf(__instance.transform.root.gameObject))
                    + " view=" + id + " " + ZdoText(zdo) + " age=" + (known ? (Time.time - info.Time).ToString("F2") : "?")
                    + Stack());
            }
            else if (known && info.Tracked)
            {
                Line("UNREGISTERED-DESTROY prefab=" + info.Prefab + " view=" + id + " " + ZdoText(zdo)
                    + " age=" + (Time.time - info.Time).ToString("F2"));
            }
            if (!leak) views.Remove(id);
        }

        static void SceneDestroyPrefix(GameObject go)
        {
            if (go == null) return;
            string prefab = PrefabOf(go);
            if (!IsTracked(prefab)) return;
            ZNetView view = go.GetComponent<ZNetView>();
            Line("NET-DESTROY prefab=" + prefab + " go=" + go.GetInstanceID()
                + " " + (view == null ? "no-znetview" : ZdoText(zdoRef(view))) + Stack());
        }

        static void SceneZdoDestroyedPrefix(ZNetScene __instance, ZDO zdo)
        {
            if (zdo == null) return;
            ZNetView view;
            if (!instancesRef(__instance).TryGetValue(zdo, out view)) return;
            ViewInfo info;
            bool known = view != null && views.TryGetValue(view.GetInstanceID(), out info) && info.Tracked;
            if (known || (object)view != null && view == null)
                Line("ZDO-DESTROYED " + ZdoText(zdo) + " view=" + ((object)view == null ? "<none>" : DescribeView(view)));
        }

        static string DescribeView(ZNetView view)
        {
            ViewInfo info;
            int id = view.GetInstanceID();
            string name = views.TryGetValue(id, out info) ? info.Prefab + " root=" + info.Root + " path=" + info.Path
                : (view == null ? "<unrecorded,destroyed>" : PrefabOf(view.gameObject));
            return name + " view=" + id + (view == null ? " DESTROYED" : "");
        }

        static Exception RemoveObjectsFinalizer(ZNetScene __instance, Exception __exception)
        {
            if (__exception == null) return null;
            float now = Time.time;
            if (removeWindowStart < 0f || now - removeWindowStart > 5f)
            {
                if (removeErrors > 0)
                    Line("REMOVEOBJECTS-ERRORS count=" + removeErrors + " window=5s");
                removeErrors = 0;
                removeWindowStart = now;
            }
            removeErrors++;
            if (now - lastRemoveScan >= 1f)
            {
                lastRemoveScan = now;
                ScanInstances(__instance, __exception);
            }
            return __exception; // never swallowed
        }

        static void ScanInstances(ZNetScene scene, Exception ex)
        {
            int bad = 0;
            foreach (KeyValuePair<ZDO, ZNetView> kv in instancesRef(scene))
            {
                ZNetView view = kv.Value;
                string why = null;
                if ((object)view == null) why = "null-view";
                else if (view == null) why = "view-destroyed-outside-ZNetScene";
                else
                {
                    ZDO z = zdoRef(view);
                    if (z == null) why = "view-zdo-null";
                    else if (!ReferenceEquals(z, kv.Key)) why = "view-zdo-mismatch";
                }
                if (why == null) continue;
                if (++bad <= 12)
                    Line("BAD-INSTANCE " + why + " key=" + ZdoText(kv.Key) + " "
                        + ((object)view == null ? "" : DescribeView(view)));
            }
            Line("REMOVEOBJECTS-NRE bad=" + bad + " first=" + ex.GetType().Name + ": " + ex.Message
                + " at " + FirstFrame(ex));
        }

        static string FirstFrame(Exception ex)
        {
            string s = ex.StackTrace ?? "";
            int nl = s.IndexOf('\n');
            return (nl > 0 ? s.Substring(0, nl) : s).Trim();
        }

        // ---------- actor stages ----------

        static void Stage(Component c, string stage, Exception ex)
        {
            if (!RootTracked(c)) return;
            if (ex == null) Line("STAGE " + stage + " ok " + Who(c));
            else Line("STAGE " + stage + " THREW " + Who(c) + " " + ex.GetType().Name + ": " + ex.Message + " at " + FirstFrame(ex));
        }

        static void StagePostfix_CharacterAwake(Character __instance) { Stage(__instance, "Character.Awake", null); }
        static Exception StageFinalizer_CharacterAwake(Character __instance, Exception __exception)
        { if (__exception != null) Stage(__instance, "Character.Awake", __exception); return __exception; }

        static void StagePostfix_BaseAIAwake(BaseAI __instance) { Stage(__instance, "BaseAI.Awake", null); }
        static Exception StageFinalizer_BaseAIAwake(BaseAI __instance, Exception __exception)
        { if (__exception != null) Stage(__instance, "BaseAI.Awake", __exception); return __exception; }

        static void StagePostfix_HumanoidStart(Humanoid __instance) { Stage(__instance, "Humanoid.Start", null); }
        static Exception StageFinalizer_HumanoidStart(Humanoid __instance, Exception __exception)
        { if (__exception != null) Stage(__instance, "Humanoid.Start", __exception); return __exception; }

        static void StagePostfix_GiveDefaultItems(Humanoid __instance) { Stage(__instance, "Humanoid.GiveDefaultItems", null); }
        static Exception StageFinalizer_GiveDefaultItems(Humanoid __instance, Exception __exception)
        { if (__exception != null) Stage(__instance, "Humanoid.GiveDefaultItems", __exception); return __exception; }

        static void CharacterOnDestroyPrefix(Character __instance)
        {
            if (!RootTracked(__instance)) return;
            ZNetView view = __instance.GetComponent<ZNetView>();
            Line("CHARACTER-DESTROY " + Who(__instance) + " "
                + (view == null ? "no-znetview" : ZdoText(zdoRef(view)) + " registered=" + StillRegistered(view, zdoRef(view))));
        }

        static void AttachItemPostfix(VisEquipment __instance, int itemHash, GameObject __result)
        {
            if (!RootTracked(__instance)) return;
            string itemName = "hash" + itemHash;
            GameObject itemPrefab = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(itemHash);
            if (itemPrefab != null) itemName = itemPrefab.name;
            int nets = __result == null ? 0 : __result.GetComponentsInChildren<ZNetView>(true).Length;
            Line("ATTACH " + Who(__instance) + " item=" + itemName
                + " result=" + (__result == null ? "<null>" : __result.name) + " znetviews=" + nets);
        }

        static Exception AttachItemFinalizer(VisEquipment __instance, int itemHash, Exception __exception)
        {
            if (__exception != null && RootTracked(__instance))
                Line("ATTACH THREW " + Who(__instance) + " itemHash=" + itemHash + " "
                    + __exception.GetType().Name + ": " + __exception.Message + " at " + FirstFrame(__exception));
            return __exception;
        }

        // ---------- static audit ----------

        static void PlayerSpawnedPostfix(Player __instance)
        {
            if (audited || !cfgAutoAudit.Value || __instance != Player.m_localPlayer) return;
            audited = true;
            Audit(null);
        }

        static IEnumerable<string> AuditTargets(string only)
        {
            if (!string.IsNullOrEmpty(only)) { yield return only; yield break; }
            foreach (string name in tracked) yield return name;
        }

        static void Audit(string only)
        {
            if (ZNetScene.instance == null) { Line("AUDIT skipped: no ZNetScene"); return; }
            foreach (string name in AuditTargets(only))
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab == null) { Line("AUDIT " + name + " NOT-REGISTERED in ZNetScene"); continue; }
                ZNetView[] nets = prefab.GetComponentsInChildren<ZNetView>(true);
                int missing = 0;
                foreach (Transform t in prefab.GetComponentsInChildren<Transform>(true))
                    foreach (Component c in t.GetComponents<Component>())
                        if (c == null) { missing++; Line("AUDIT " + name + " MISSING-SCRIPT at " + PathOf(t)); }
                Line("AUDIT " + name + " active=" + prefab.activeSelf + " znetviews=" + nets.Length
                    + " missingScripts=" + missing);
                foreach (ZNetView n in nets)
                    if (n.transform != prefab.transform) Line("AUDIT " + name + " NESTED-ZNETVIEW " + PathOf(n.transform));

                Humanoid hum = prefab.GetComponent<Humanoid>();
                if (hum == null) { Line("AUDIT " + name + " no Humanoid"); continue; }
                AuditItems(name, "default", hum.m_defaultItems);
                AuditItems(name, "randomWeapon", hum.m_randomWeapon);
                AuditItems(name, "randomArmor", hum.m_randomArmor);
                AuditItems(name, "randomShield", hum.m_randomShield);
                if (hum.m_randomSets != null)
                    for (int i = 0; i < hum.m_randomSets.Length; ++i)
                        AuditItems(name, "set" + i + ":" + (hum.m_randomSets[i] == null ? "<null>" : hum.m_randomSets[i].m_name),
                            hum.m_randomSets[i] == null ? null : hum.m_randomSets[i].m_items);
                if (hum.m_randomItems != null)
                    for (int i = 0; i < hum.m_randomItems.Length; ++i)
                        AuditItems(name, "randomItem" + i,
                            new GameObject[] { hum.m_randomItems[i] == null ? null : hum.m_randomItems[i].m_prefab });
            }
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

        // ---------- console: isolated fixture ----------

        static void InitTerminalPostfix()
        {
            new Terminal.ConsoleCommand("lwc_rrrdiag",
                "[audit [prefab] | fixture <prefab> [dist] [seed] | remove | scan] RRR lifecycle diagnostic",
                new Terminal.ConsoleEvent(Command), false, false, false, false, false, false, null, false, false, false);
        }

        static void Command(Terminal.ConsoleEventArgs args)
        {
            string verb = args.Args.Length > 1 ? args.Args[1] : "";
            if (verb == "audit") { Audit(args.Args.Length > 2 ? args.Args[2] : null); return; }
            if (verb == "scan")
            {
                if (ZNetScene.instance != null) ScanInstances(ZNetScene.instance, new Exception("manual scan"));
                return;
            }
            if (verb == "remove") { RemoveFixtures(); return; }
            if (verb == "fixture" && args.Args.Length > 2) { Fixture(args); return; }
            args.Context.AddString("lwc_rrrdiag audit [prefab] | fixture <prefab> [dist=12] [seed=1337] | remove | scan");
        }

        static void Fixture(Terminal.ConsoleEventArgs args)
        {
            string name = args.Args[2];
            float dist = 12f;
            int seed = 1337;
            if (args.Args.Length > 3) float.TryParse(args.Args[3], out dist);
            if (args.Args.Length > 4) int.TryParse(args.Args[4], out seed);
            Player p = Player.m_localPlayer;
            if (p == null || ZNetScene.instance == null) { args.Context.AddString("no player / ZNetScene"); return; }
            GameObject prefab = ZNetScene.instance.GetPrefab(name);
            if (prefab == null) { args.Context.AddString(name + " not registered"); Line("FIXTURE " + name + " NOT-REGISTERED"); return; }
            tracked.Add(name);
            Vector3 fwd = p.transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 pos = p.transform.position + fwd * dist;
            float h;
            if (ZoneSystem.instance != null && ZoneSystem.instance.GetGroundHeight(pos, out h)) pos.y = h + 0.5f;
            UnityEngine.Random.State saved = UnityEngine.Random.state;
            UnityEngine.Random.InitState(seed);
            Line("FIXTURE spawn prefab=" + name + " seed=" + seed + " pos=" + pos.ToString("F1"));
            GameObject go = null;
            try
            {
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
            if (view != null) fixtures.Add(view);
            Line("FIXTURE result go=" + go.GetInstanceID() + " " + (view == null ? "no-znetview" : ZdoText(zdoRef(view))
                + " registered=" + StillRegistered(view, zdoRef(view))));
            args.Context.AddString("fixture " + name + " spawned; see LogOutput RRR-LIFE");
        }

        static void RemoveFixtures()
        {
            int n = 0;
            foreach (ZNetView view in fixtures)
            {
                if (view == null) { Line("FIXTURE remove: view already destroyed"); continue; }
                ZDO z = zdoRef(view);
                Line("FIXTURE remove " + PrefabOf(view.gameObject) + " " + ZdoText(z) + " registered=" + StillRegistered(view, z));
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(view.gameObject);
                n++;
            }
            fixtures.Clear();
            Line("FIXTURE removed=" + n + " via ZNetScene.Destroy");
        }
    }
}

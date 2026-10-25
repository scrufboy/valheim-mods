using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Core
{
    // Same GUID as the 0.6.x Human NPC lab so this cleanly replaces it in the profile.
    // Same prefab names and saved keys so existing companions load straight into the new core.
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class CorePlugin : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.humannpc";
        public const string Name = "Living World C.O.R.E. NPC";
        public const string Version = "1.0.11";
        internal const string GuardPrefab = "LWC_HumanGuard_v2";
        internal const string ArcherPrefab = "LWC_HumanArcher_v2";
        internal const string LegacyPrefab = "LWC_HumanScout_v1";

        internal static CorePlugin Instance;
        bool registered;
        bool partyBusy;
        Terminal.ConsoleCommand partyCommand, statusCommand;

        public static bool Registered { get { return Instance != null && Instance.registered; } }

        // Feature switches so one change can be live-tested at a time (BepInEx config file).
        static BepInEx.Configuration.ConfigEntry<bool> shieldBlocking;
        public static bool ShieldBlocking { get { return shieldBlocking != null && shieldBlocking.Value; } }

        void Awake()
        {
            Instance = this;
            shieldBlocking = Config.Bind("Combat", "ShieldBlocking", false,
                "Experimental: companions raise shields against close melee and aimed projectiles.");
            // One hook: companions answer native MonsterAI's own "which enemy?" question.
            var harmony = new HarmonyLib.Harmony(Guid);
            harmony.Patch(NativeApi.FindEnemy,
                postfix: new HarmonyLib.HarmonyMethod(typeof(FindEnemyHook).GetMethod("Postfix")));
            // Companions get the Player's armor rules (Valheim counts worn armor only for Player).
            harmony.Patch(NativeApi.GetBodyArmor,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("BodyArmorPostfix")));
            harmony.Patch(NativeApi.ApplyArmorDamageMods,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("ArmorModsPostfix")));
            harmony.Patch(NativeApi.RpcDamage,
                transpiler: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("RpcDamageTranspiler")));
            Report(ArmorParity.ArmorGatesPatched == 1 ? "Companion armor applies to damage (Player rule)." :
                "WARNING: companion armor gate not patched.");
            Report("Companion damage parity: stagger x2 " + (ArmorParity.StaggerGatesPatched == 1 ? "off" : "NOT PATCHED") +
                ", backstab " + (ArmorParity.BackstabGatesPatched == 1 ? "off" : "NOT PATCHED") + " (Player rules).");
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
            partyCommand = new Terminal.ConsoleCommand("lwparty",
                "Living World: create your two companions (lwparty new = replace the current lab pair)",
                new Terminal.ConsoleEvent(OnParty));
            statusCommand = new Terminal.ConsoleCommand("lwstatus", "Living World: show each companion's phase and owners",
                new Terminal.ConsoleEvent(OnStatus));
            string missing = NativeApi.MissingMembers();
            Report(Version + " loaded. Commands: lwparty, lwparty new, lwstatus." +
                (missing.Length == 0 ? "" : " NATIVE API MISSING: " + missing));
        }

        void RegisterPrefabs()
        {
            if (registered) return;
            try
            {
                GameObject player = RequirePrefab("Player", false);
                // No helmet: it hides native hair (0.4.0 lesson).
                // Same kit, same behavior: the weapon follows from distance, not from a role.
                GameObject[] guardItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "BowFineWood", "ArrowWood", "SwordIron", "ShieldBanded");
                GameObject[] archerItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "BowFineWood", "ArrowWood", "SwordIron", "ShieldBanded");
                PrefabManager.Instance.AddPrefab(new CustomPrefab(NpcFactory.Create(GuardPrefab, player, guardItems, "guard"), false));
                PrefabManager.Instance.AddPrefab(new CustomPrefab(NpcFactory.Create(ArcherPrefab, player, archerItems, "archer"), false));
                registered = true;
                Report("Registered guard and archer native humans.");
            }
            catch (Exception error)
            {
                Logger.LogError(error);
                Report("REGISTRATION FAILED: " + error.Message);
            }
        }

        GameObject[] Items(params string[] names) { return names.Select(n => RequirePrefab(n, true)).ToArray(); }

        static GameObject RequirePrefab(string name, bool item)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            if (prefab == null || (item && prefab.GetComponent<ItemDrop>() == null))
                throw new InvalidOperationException("Required native prefab is unavailable: " + name);
            return prefab;
        }

        // ---------------- lwparty ----------------

        void OnParty(Terminal.ConsoleEventArgs args)
        {
            if (partyBusy) { Report("lwparty is already running."); return; }
            bool replace = args.Length > 1 && args[1].Equals("new", StringComparison.OrdinalIgnoreCase);
            if (!replace && Player.m_localPlayer != null)
            {
                long me = Player.m_localPlayer.GetPlayerID();
                int mine = NpcKernel.All.Count(k => k != null && k.GetComponent<NpcLeaderBinding>() != null &&
                    k.GetComponent<NpcLeaderBinding>().LeaderId == me);
                if (mine > 0)
                {
                    Report("You already have " + mine + " companion(s) loaded. Use 'lwparty new' to replace them.");
                    return;
                }
            }
            StartCoroutine(RunParty(replace));
        }

        IEnumerator RunParty(bool replace)
        {
            partyBusy = true;
            var created = new List<GameObject>();
            Exception failure = null;
            IEnumerator transaction = PartyTransaction(created, replace);
            while (true)
            {
                object yielded = null;
                bool advanced;
                try
                {
                    advanced = transaction.MoveNext();
                    if (advanced) yielded = transaction.Current;
                }
                catch (Exception error) { failure = error; break; }
                if (!advanced) break;
                yield return yielded;
            }
            if (failure != null)
            {
                foreach (GameObject actor in created)
                {
                    if (actor == null) continue;
                    ZNetView view = actor.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) ZNetScene.instance.Destroy(actor);
                    else UnityEngine.Object.Destroy(actor);
                }
                Logger.LogError(failure);
                Report("PARTY FAILED: " + failure.Message + " No partial party was kept.");
            }
            partyBusy = false;
        }

        IEnumerator PartyTransaction(List<GameObject> created, bool replace)
        {
            RequireWorld();
            if (!registered) throw new InvalidOperationException("Companion prefabs are not registered (see startup log).");
            int removed = 0;
            if (replace)
            {
                foreach (string prefab in new[] { LegacyPrefab, GuardPrefab, ArcherPrefab })
                {
                    int count = 0;
                    yield return StartCoroutine(RemovePrefabRecords(prefab, value => count = value));
                    removed += count;
                }
                yield return null;
                yield return null;
            }

            Player leader = Player.m_localPlayer;
            string generation = System.Guid.NewGuid().ToString("N");
            Vector3[] points = FindSpawnPair(leader);
            Spawn(GuardPrefab, points[0], leader.transform.rotation, created);
            Spawn(ArcherPrefab, points[1], leader.transform.rotation, created);

            float deadline = Time.realtimeSinceStartup + 10f;
            while (created.Any(a => a != null && !a.GetComponent<NpcAppearance>().Ready) && Time.realtimeSinceStartup < deadline)
                yield return null;
            foreach (GameObject actor in created)
            {
                if (actor == null) throw new InvalidOperationException("A new companion vanished during creation.");
                NpcAppearance appearance = actor.GetComponent<NpcAppearance>();
                if (!appearance.Ready) throw new InvalidOperationException(appearance.Status);
            }
            foreach (GameObject actor in created) actor.GetComponent<NpcLeaderBinding>().Bind(leader, generation);
            Report("PARTY READY: two companions bound to you" + (replace ? "; replaced " + removed + " old record(s)." : "."));
        }

        static void Spawn(string prefabId, Vector3 position, Quaternion rotation, List<GameObject> created)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabId);
            if (prefab == null) throw new InvalidOperationException("Registered prefab not found: " + prefabId);
            GameObject actor = UnityEngine.Object.Instantiate(prefab, position, rotation);
            created.Add(actor); // tracked before validation so a failure here is still rolled back
            ZNetView view = actor.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
                throw new InvalidOperationException("New companion did not receive local network ownership.");
            NpcFactory.Validate(actor);
        }

        IEnumerator RemovePrefabRecords(string prefabName, Action<int> completed)
        {
            var records = new List<ZDO>();
            int index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefabName, records, ref index)) yield return null;
            int removed = 0;
            foreach (ZDO zdo in records.ToArray())
            {
                if (zdo == null || !zdo.IsValid()) continue;
                GameObject loaded = ZNetScene.instance.FindInstance(zdo.m_uid);
                if (loaded != null)
                {
                    ZNetView view = loaded.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) { ZNetScene.instance.Destroy(loaded); removed++; }
                }
                else { ZDOMan.instance.DestroyZDO(zdo); removed++; }
            }
            completed(removed);
        }

        static Vector3[] FindSpawnPair(Player player)
        {
            Vector3[] offsets = {
                -player.transform.right * 2.5f - player.transform.forward * 2f,
                player.transform.right * 2.5f - player.transform.forward * 2f,
                -player.transform.right * 3.5f, player.transform.right * 3.5f
            };
            var points = new List<Vector3>();
            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = player.transform.position + offset;
                float height;
                if (!ZoneSystem.instance.GetGroundHeight(candidate, out height)) continue;
                candidate.y = height + .15f;
                bool blocked = Physics.OverlapCapsule(candidate + Vector3.up * .5f, candidate + Vector3.up * 1.7f, .65f)
                    .Any(c => !c.isTrigger && !(c is TerrainCollider) && c.GetComponent<Heightmap>() == null);
                if (!blocked) points.Add(candidate);
                if (points.Count == 2) return points.ToArray();
            }
            throw new InvalidOperationException("Stand on open loaded ground so two companions can appear beside you.");
        }

        // ---------------- lwstatus ----------------

        void OnStatus(Terminal.ConsoleEventArgs args)
        {
            Report("STATUS " + Version + ": registered=" + registered + "; companions loaded=" + NpcKernel.All.Count());
            foreach (NpcKernel kernel in NpcKernel.All)
            {
                if (kernel == null) continue;
                NpcAppearance appearance = kernel.GetComponent<NpcAppearance>();
                NpcWetStatus wet = kernel.GetComponent<NpcWetStatus>();
                Character character = kernel.GetComponent<Character>();
                Report((appearance == null ? kernel.name : appearance.Status) + " | " + kernel.Describe() +
                    " | hp=" + (character == null ? 0f : character.GetHealth()).ToString("0") +
                    (wet != null && wet.Wet ? " | wet" : ""));
            }
        }

        static void RequireWorld()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null || ZNetScene.instance == null ||
                ZDOMan.instance == null || ZoneSystem.instance == null)
                throw new InvalidOperationException("Enter a world and wait until you are standing on loaded terrain.");
            if (!ZNet.instance.IsServer())
                throw new InvalidOperationException("lwparty currently requires local/server authority.");
        }

        // ---------------- reporting ----------------

        public static void Report(string message)
        {
            if (Instance == null) return;
            Instance.Logger.LogInfo("[LWC] " + message);
            if (global::Console.instance != null) global::Console.instance.Print("[LWC] " + message);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, message, 0, null, false);
        }

        public static void Warn(string message)
        {
            if (Instance != null) Instance.Logger.LogWarning("[LWC] " + message);
        }

        void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefabs;
            Instance = null;
        }
    }
}

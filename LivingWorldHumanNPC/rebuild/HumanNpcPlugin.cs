using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class HumanNpcPlugin : BaseUnityPlugin
    {
        internal const string Guid = "local.livingworld.humannpc";
        internal const string Name = "Living World Human NPC";
        internal const string Version = "0.5.1";
        internal const string GuardPrefab = "LWC_HumanGuard_v2";
        internal const string ArcherPrefab = "LWC_HumanArcher_v2";
        internal const string LegacyPrefab = "LWC_HumanScout_v1";

        internal static HumanNpcPlugin Instance;
        bool registered;
        bool partyBusy;
        Terminal.ConsoleCommand partyCommand;
        Terminal.ConsoleCommand statusCommand;

        void Awake()
        {
            Instance = this;
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
            partyCommand = new Terminal.ConsoleCommand("lwparty",
                "Lab command: replace stale test companions with exactly two fresh native humans",
                new Terminal.ConsoleEvent(OnParty));
            statusCommand = new Terminal.ConsoleCommand("lwstatus", "Show the exact Living World v2 party state",
                new Terminal.ConsoleEvent(OnStatus));
            Report("0.5.1 governed companion movement loaded. Commands: lwparty, lwstatus.");
        }

        void RegisterPrefabs()
        {
            if (registered) return;
            try
            {
                GameObject player = RequirePrefab("Player", false);
                // The acceptance actors must visibly prove their generated hair
                // and beard. HelmetIron deliberately hides native hair, so it
                // cannot be part of this foundation loadout.
                GameObject[] guardItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "SwordIron", "ShieldBanded");
                GameObject[] archerItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "BowFineWood", "ArrowFire", "SwordIron", "ShieldBanded");
                GameObject guard = NativeHumanFactoryV2.Create(GuardPrefab, player, guardItems, "guard");
                GameObject archer = NativeHumanFactoryV2.Create(ArcherPrefab, player, archerItems, "archer");
                PrefabManager.Instance.AddPrefab(new CustomPrefab(guard, false));
                PrefabManager.Instance.AddPrefab(new CustomPrefab(archer, false));
                registered = true;
                Report("Registered two self-contained native Player-clone Humanoid definitions.");
            }
            catch (Exception error)
            {
                Logger.LogError(error);
                Report("REGISTRATION FAILED: " + error.Message);
            }
        }

        GameObject[] Items(params string[] names)
        {
            return names.Select(name => RequirePrefab(name, true)).ToArray();
        }

        static GameObject RequirePrefab(string name, bool item)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            if (prefab == null || (item && prefab.GetComponent<ItemDrop>() == null))
                throw new InvalidOperationException("Required native prefab is unavailable: " + name);
            return prefab;
        }

        void OnParty(Terminal.ConsoleEventArgs args)
        {
            if (partyBusy) { Report("lwparty is already replacing the party."); return; }
            StartCoroutine(ReplaceParty());
        }

        IEnumerator ReplaceParty()
        {
            partyBusy = true;
            var created = new List<GameObject>();
            Exception failure = null;
            IEnumerator transaction = ReplacePartyTransaction(created);
            while (true)
            {
                object yielded = null;
                bool advanced = false;
                try
                {
                    advanced = transaction.MoveNext();
                    if (advanced) yielded = transaction.Current;
                }
                catch (Exception error)
                {
                    failure = error;
                    break;
                }
                if (!advanced) break;
                yield return yielded;
            }

            if (failure != null)
            {
                foreach (GameObject actor in created)
                    if (actor != null && actor.GetComponent<ZNetView>() != null && actor.GetComponent<ZNetView>().IsOwner())
                        ZNetScene.instance.Destroy(actor);
                Logger.LogError(failure);
                Report("PARTY FAILED: " + failure.Message + ". No partial party was retained.");
            }
            partyBusy = false;
        }

        IEnumerator ReplacePartyTransaction(List<GameObject> created)
        {
            RequireWorld();
            if (!registered) throw new InvalidOperationException("Native human prefabs are not registered yet.");
            int removed = 0;
            foreach (string prefab in new[] { LegacyPrefab, GuardPrefab, ArcherPrefab })
            {
                int count = 0;
                yield return StartCoroutine(RemovePrefabRecords(prefab, value => count = value));
                removed += count;
            }
            yield return null;
            yield return null;

            Player leader = Player.m_localPlayer;
            string generation = System.Guid.NewGuid().ToString("N");
            Vector3[] points = FindSpawnPair(leader);
            created.Add(InstantiateRegistered(GuardPrefab, points[0], leader.transform.rotation));
            created.Add(InstantiateRegistered(ArcherPrefab, points[1], leader.transform.rotation));

            float deadline = Time.realtimeSinceStartup + 10f;
            while (created.Any(actor => actor != null && !actor.GetComponent<HumanAppearanceV2>().Ready) &&
                Time.realtimeSinceStartup < deadline) yield return null;

            foreach (GameObject actor in created)
            {
                if (actor == null) throw new InvalidOperationException("A newly created party member vanished.");
                HumanAppearanceV2 appearance = actor.GetComponent<HumanAppearanceV2>();
                if (!appearance.Ready) throw new InvalidOperationException(appearance.Status);
            }

            foreach (GameObject actor in created) actor.GetComponent<HumanCompanionV2>().Bind(leader, generation);
            Report("LAB PARTY READY: exactly two visible randomized humans are bound; stale records removed=" + removed + ".");
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
                    if (view != null && view.IsValid() && view.IsOwner())
                    { ZNetScene.instance.Destroy(loaded); removed++; }
                }
                else
                {
                    ZDOMan.instance.DestroyZDO(zdo);
                    removed++;
                }
            }
            completed(removed);
        }

        static GameObject InstantiateRegistered(string prefabId, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabId);
            if (prefab == null) throw new InvalidOperationException("Registered prefab not found in ZNetScene: " + prefabId);
            GameObject actor = UnityEngine.Object.Instantiate(prefab, position, rotation);
            ZNetView view = actor.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
                throw new InvalidOperationException("New human did not receive local network ownership.");
            NativeHumanFactoryV2.Validate(actor);
            return actor;
        }

        static Vector3[] FindSpawnPair(Player player)
        {
            Vector3[] offsets = {
                -player.transform.right * 2.5f - player.transform.forward * 2f,
                player.transform.right * 2.5f - player.transform.forward * 2f,
                -player.transform.right * 3.5f,
                player.transform.right * 3.5f
            };
            var points = new List<Vector3>();
            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = player.transform.position + offset;
                float height;
                if (!ZoneSystem.instance.GetGroundHeight(candidate, out height)) continue;
                candidate.y = height + .15f;
                bool blocked = Physics.OverlapCapsule(candidate + Vector3.up * .5f, candidate + Vector3.up * 1.7f, .65f)
                    .Any(collider => !collider.isTrigger && !(collider is TerrainCollider) && collider.GetComponent<Heightmap>() == null);
                if (!blocked) points.Add(candidate);
                if (points.Count == 2) return points.ToArray();
            }
            throw new InvalidOperationException("Stand on open loaded ground so two companions can appear beside you.");
        }

        void OnStatus(Terminal.ConsoleEventArgs args)
        {
            var party = Character.GetAllCharacters().Where(character => character != null &&
                character.GetComponent<HumanAppearanceV2>() != null).ToArray();
            Report("STATUS 0.5.1: registered=" + registered + "; partyBusy=" + partyBusy + "; loadedV2=" + party.Length + ".");
            foreach (Character character in party)
            {
                HumanAppearanceV2 appearance = character.GetComponent<HumanAppearanceV2>();
                HumanBehaviorController behavior = character.GetComponent<HumanBehaviorController>();
                Report(appearance.Status + "; behavior=" + (behavior == null ? "missing" : behavior.Status) +
                    "; traversal=" + (behavior == null ? "missing" : behavior.TraversalStatus) +
                    "; equipment=" + (behavior == null ? "missing" : behavior.EquipmentStatus) +
                    "; health=" + character.GetHealth().ToString("0") + ".");
            }
        }

        static void RequireWorld()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null || ZNetScene.instance == null ||
                ZDOMan.instance == null || ZoneSystem.instance == null)
                throw new InvalidOperationException("Enter a local world and wait until your player is standing on loaded terrain.");
            if (!ZNet.instance.IsServer()) throw new InvalidOperationException("The 0.5.1 lab party command currently requires local/server authority.");
        }

        internal static void ReportStatic(string message)
        {
            if (Instance != null) Instance.Report(message);
        }

        internal static void WarnStatic(string message)
        {
            if (Instance != null) Instance.Logger.LogWarning("[LWC HUMAN] " + message);
        }

        internal void Report(string message)
        {
            Logger.LogInfo("[LWC HUMAN] " + message);
            if (Console.instance != null) Console.instance.Print("[LWC HUMAN] " + message);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, message, 0, null, false);
        }

        void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefabs;
            Instance = null;
        }
    }
}

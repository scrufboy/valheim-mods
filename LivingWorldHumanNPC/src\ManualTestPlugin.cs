using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using HarmonyLib;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace LivingWorld.ManualTest
{
    [BepInPlugin("local.livingworld.humannpc", "Living World Human NPC Lab", "0.3.43")]
    [BepInIncompatibility("local.livingworld.manualtest")]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class ManualTestPlugin : BaseUnityPlugin
    {
        internal const string NpcId = HumanRigPolicy.PrefabId;
        internal const string LabTag = "livingworld_human_lab_v1";
        internal const string LabBuiltKey = "lwc_human_lab_v1_built";
        internal const string NpcKey = "livingworld_human_npc_v1";
        internal const string NpcSpawnedKey = "lwc_human_scout_v1_spawned";
        internal static ManualTestPlugin Instance;
        GameObject registeredPrefab;
        Harmony harmony;
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly MethodInfo HavePath = typeof(BaseAI).GetMethod("HavePath", Native, null, new[] { typeof(Vector3) }, null);
        GameObject npc, enemy;
        Character victim;
        Vector3 goal;
        float deadline, initialDistance, damage;
        bool pathSeen, targetSeen, registered, unstableMotion, targetDied, foreignDamage;
        string active = "idle";
        string expectedWeaponName;
        string pendingResult;
        ZDOID cleanupUid;
        ZDOID targetUid;
        bool targetTracked;
        bool partyPreparing;
        ZNetScene checkScene;
        long checkWorldUid;
        Terminal.ConsoleCommand command;
        Terminal.ConsoleCommand partyCommand;
        Terminal.ConsoleCommand commandsCommand;
        ManualPartyLab partyLab;

        void Awake()
        {
            Instance = this;
            harmony = new Harmony("local.livingworld.humannpc.companion");
            HumanCompanion.InstallPatches(harmony);
            HumanDoorTraversal.InstallPatches(harmony);
            HumanFollowPresence.InstallPatches(harmony);
            HumanAwarenessHud.InstallPatches(harmony);
            HumanCompanionPosture.InstallPatches(harmony);
            HumanEquipmentReadiness.InstallPatches(harmony);
            HumanResources.InstallPatches(harmony);
            HumanPartyEquipment.InstallPatches(harmony);
            ManualPartyLab.Install(harmony);
            partyLab = gameObject.AddComponent<ManualPartyLab>();
            PrefabManager.OnVanillaPrefabsAvailable += Register;
            command = new Terminal.ConsoleCommand("lwtest", "Human NPC commands; type lwtest help or press Tab after lwtest",
                new Terminal.ConsoleEvent(OnCommand), optionsFetcher: LwTestOptions);
            partyCommand = new Terminal.ConsoleCommand("lwparty", "Create or recover a ready companion party and start following",
                new Terminal.ConsoleEvent(OnPartyCommand));
            commandsCommand = new Terminal.ConsoleCommand("lwcommands", "Show every Living World Human NPC console command",
                new Terminal.ConsoleEvent(OnCommands));
            Report("READY. Type lwtest help in F5. Nothing is spawned automatically. Requires disposable world " +
                ManualAcceptance.WorldName + " and character " + ManualAcceptance.ProfileName + ".");
        }

        static List<string> LwTestOptions()
        {
            return new List<string> {
                "help", "status", "setup", "spawn", "migrate", "follow", "hold", "move", "combat", "stop", "cleanup",
                "party prepare", "party status", "party locate", "party recover",
                "wave boarneck", "wave greydwarf", "wave go", "wave status", "wave stop"
            };
        }

        void OnCommands(Terminal.ConsoleEventArgs args) { Help(); }

        void OnPartyCommand(Terminal.ConsoleEventArgs args)
        {
            if (partyPreparing) { Report("PARTY preparation already in progress; wait for completion."); return; }
            StartCoroutine(PreparePartyTransaction());
        }

        IEnumerator PreparePartyTransaction()
        {
            partyPreparing = true;
            float expires = Time.realtimeSinceStartup + 12f;
            while (true)
            {
                Exception failure = null;
                try
                {
                    GuardWorld();
                    if (active != "idle" || partyLab.HasEncounter)
                        throw new InvalidOperationException("Stop the current measured encounter before changing the party");
                    PrepareParty();
                }
                catch (Exception error) { failure = error; }
                if (failure == null) { partyPreparing = false; yield break; }
                bool initializing = failure.Message.StartsWith("Prepare requires a ready", StringComparison.Ordinal) ||
                    failure.Message.IndexOf("initialization", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    failure.Message.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0;
                if (initializing && Time.realtimeSinceStartup < expires) { yield return null; continue; }
                Report("PARTY refused: " + failure.Message); Logger.LogError(failure);
                partyPreparing = false; yield break;
            }
        }

        void Register()
        {
            if (registered) return;
            GameObject clone = null;
            try
            {
                if (HavePath == null) throw new MissingMethodException("Native movement contracts changed.");
                if (PrefabManager.Instance.GetPrefab(NpcId) != null) throw new InvalidOperationException("Test prefab identity already exists.");
                var playerSource = PrefabManager.Instance.GetPrefab("Player");
                if (playerSource == null) throw new InvalidOperationException("Native human visual source unavailable.");
                var outfit = new[] { "ArmorLeatherChest", "ArmorLeatherLegs", "SwordBronze", "ShieldWood" }
                    .Select(name => PrefabManager.Instance.GetPrefab(name)).ToArray();
                if (outfit.Any(item => item == null || item.GetComponent<ItemDrop>() == null))
                    throw new InvalidOperationException("Required native human clothing/weapon/shield unavailable.");
                var weapon = outfit[2];
                if (String.IsNullOrEmpty(weapon.GetComponent<ItemDrop>().m_itemData.m_shared.m_attack.m_attackAnimation))
                    throw new InvalidOperationException("Native sword attack animation missing.");
                expectedWeaponName = weapon.GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
                clone = NativeHumanFactory.Create(NpcId, playerSource, outfit);
                clone.AddComponent<ManualNpcIdentity>();
                NativeHumanFactory.Validate(clone, false);
                var containerProperty = typeof(PrefabManager).GetProperty("PrefabContainer", Native);
                var prefabContainer = containerProperty == null ? null : containerProperty.GetValue(PrefabManager.Instance, null) as GameObject;
                if (prefabContainer == null || prefabContainer.activeInHierarchy || clone.activeInHierarchy)
                    throw new InvalidOperationException("Native prefab registration must stay inactive until a network instance is created.");
                PrefabManager.Instance.AddPrefab(new CustomPrefab(clone, false));
                if (clone.activeInHierarchy || clone.transform.parent != prefabContainer.transform)
                    throw new InvalidOperationException("Prefab registration did not retain the inactive definition container.");
                registeredPrefab = clone;
                registered = true;
                Report("REGISTERED exactly one independent NPC: " + NpcId + "; native weapon=" + weapon.name +
                    "; body=native human visual; runtime appearance/movement/combat NOT yet certified; jobs pending.");
            }
            catch (Exception error)
            {
                if (clone != null) UnityEngine.Object.Destroy(clone);
                Report("FAIL registration: " + error.Message);
                Logger.LogError(error);
            }
        }

        void OnCommand(Terminal.ConsoleEventArgs args)
        {
            var verb = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "help";
            try
            {
                if (verb == "help") { Help(); return; }
                if (verb == "status") { Status(); return; }
                GuardWorld();
                if (verb == "wave")
                {
                    if (active != "idle") throw new InvalidOperationException("End the old measured check with lwtest stop before using party waves");
                    partyLab.Command(args.Args.Length > 2 ? args.Args[2].ToLowerInvariant() : "status"); return;
                }
                if (verb == "party")
                {
                    if (active != "idle") throw new InvalidOperationException("Stop the existing measured check first");
                    string action = args.Args.Length > 2 ? args.Args[2].ToLowerInvariant() : "prepare";
                    if (action == "prepare") PrepareParty();
                    else if (action == "status") { Status(); partyLab.Command("status"); Report("Cleric loaded=" + Character.GetAllCharacters().Any(c => c != null && HumanCleric.IsCleric(c.gameObject))); }
                    else if (action == "locate") LocateParty();
                    else if (action == "recover") RecoverParty();
                    else throw new ArgumentException("party prepare | status | locate | recover");
                    return;
                }
                if (verb == "stop") { EndCheck("STOP requested."); return; }
                if (partyLab.HasEncounter) throw new InvalidOperationException("Party encounter active. Use lwtest wave stop; other lab mutations are blocked until it finishes");
                if (verb == "cleanup") { Cleanup(); return; }
                if (active != "idle") throw new InvalidOperationException("Check running: " + active + ". Use lwtest stop first.");
                if (enemy != null || targetTracked) throw new InvalidOperationException("A target still needs cleanup. Use lwtest stop before another action.");
                if (verb == "setup") { Setup(); return; }
                if (verb == "spawn") { Spawn(); return; }
                FindNpc();
                if (npc == null) throw new InvalidOperationException("No loaded test scout. Use lwtest spawn near the lab.");
                GuardOwner(npc);
                var composition = npc.GetComponent<HumanCompositionIdentity>();
                if (verb == "migrate")
                {
                    if (composition == null || !composition.TryMigrate(Player.m_localPlayer))
                        throw new InvalidOperationException(composition == null ? "Composition identity missing" : composition.Status);
                    Report("MIGRATION committed explicit legacy compatibility; same saved scout retained. " + composition.Status);
                    return;
                }
                var human = npc.GetComponent<HumanAppearance>();
                if (human == null || !human.Initialized) throw new InvalidOperationException("Human initialization not ready: " + (human == null ? "missing adapter" : human.State));
                if (verb == "follow") { if (!npc.GetComponent<HumanCompanion>().TryIssueFollow(Player.m_localPlayer)) throw new InvalidOperationException("Follow order refused; inspect status."); Report("FOLLOW saved by stable leader identity; native movement enabled."); return; }
                if (verb == "hold") { Hold(); Report("HOLD saved at current point; nearby defense remains available."); return; }
                if (verb == "move") { BeginMovement(); return; }
                if (verb == "combat") { BeginCombat(); return; }
                throw new ArgumentException("Unknown action. Type lwtest help.");
            }
            catch (Exception error)
            {
                if ((enemy != null || targetTracked) && active != "cleanup") EndCheck("FAIL command: " + error.Message);
                else Report("FAIL / refused: " + error.Message);
                Logger.LogError(error);
            }
        }

        internal static void GuardWorld()
        {
            if (ZNet.instance == null || Game.instance == null || Player.m_localPlayer == null || ZoneSystem.instance == null || ZNetScene.instance == null)
                throw new InvalidOperationException("Enter the disposable world and wait until the player is standing on the ground.");
            if (!ManualAcceptance.WorldAllowed(ZNet.instance.GetWorldName(), Game.instance.GetPlayerProfile().GetFilename(),
                ZNet.instance.IsServer(), ZNet.IsOpenServer(), ZNet.instance.GetPeers().Count))
                throw new InvalidOperationException("Commands require local world " + ManualAcceptance.WorldName + ", character " +
                    ManualAcceptance.ProfileName + ", Start Server OFF, and no remote peers.");
        }

        void Setup()
        {
            if (ZoneSystem.instance.GetGlobalKey(LabBuiltKey))
            {
                Report("Lab was already built in this world. Setup will not duplicate it. Return to the original site; use lwtest spawn there.");
                return;
            }
            var origin = Player.m_localPlayer.transform.position + Vector3.forward * 12f;
            ManualLabBuilder.ValidateSite(origin);
            var parts = ManualLabBuilder.Build(origin, Report);
            ZoneSystem.instance.SetGlobalKey(LabBuiltKey);
            Report("SETUP created " + parts.Count + " native pieces. World is marked built. Walk around and inspect stability, doors, smoke, storage and stations.");
            Report("NPC cooking, repair, sleep, guard jobs and door traversal are PENDING. Type lwtest spawn to begin native NPC tests.");
        }

        void Spawn()
        {
            // Resolve the catalogs Valheim actually loaded before allocating or
            // writing any persistent scout state. No guessed prefab names.
            string[] availableHair = NativeAppearanceChoices("Hair");
            string[] availableBeards = NativeAppearanceChoices("Beard");
            Report("APPEARANCE catalog verified from ObjectDB customization items: hair=" + (availableHair.Length - 1) +
                "; beard=" + (availableBeards.Length - 1) + ".");
            FindNpc();
            if (npc != null) { Report("One scout already exists nearby; no duplicate spawned."); return; }
            // This is a disposable combat lab, not a population authority. An
            // old world marker, dead actor, or unloaded/lost actor must never
            // lock the tester out of creating a usable local replacement.
            ZoneSystem.instance.RemoveGlobalKey(NpcSpawnedKey);
            var prefab = ZNetScene.instance.GetPrefab(NpcId);
            if (!registered || prefab == null || !ReferenceEquals(registeredPrefab, prefab))
                throw new InvalidOperationException("Test NPC did not resolve identically in Jotunn and ZNetScene. Read LogOutput.log.");
            var at = GroundPoint(Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward * 4f);
            RequireClear(at, 1f);
            var spawnScene = ZNetScene.instance;
            var spawnZone = ZoneSystem.instance;
            GameObject created = null;
            ZDOID createdId = ZDOID.None;
            bool haveCreatedId = false;
            try
            {
                created = UnityEngine.Object.Instantiate(prefab, at, Player.m_localPlayer.transform.rotation);
                npc = created;
                var createdView = created.GetComponent<ZNetView>();
                if (createdView != null && createdView.IsValid())
                { createdId = createdView.GetZDO().m_uid; haveCreatedId = true; }
                GuardOwner(created);
                NativeHumanFactory.Validate(created, true);
                GuardOwner(created);
                // This requirement survives a failed rollback/reload. Only this
                // newly allocated actor receives it; legacy saved scouts do not.
                createdView.GetZDO().Set(HumanProfile.RequiredKey, "1");
                createdView.GetZDO().Set(LabTag, "npc");
                createdView.GetZDO().Set(NpcKey, NpcId);
                created.GetComponent<HumanCompositionIdentity>().InitializeFresh(Player.m_localPlayer);
                created.GetComponent<HumanIdentity>().InitializeFresh(Guid.NewGuid().ToString("N"), availableHair, availableBeards);
                created.GetComponent<Humanoid>().EquipBestWeapon(null, null, null, null);
                Report("SPAWN created " + NpcId + " with native human body; id=" + createdId + ". Wait for initialization, then inspect clothing/animations; type lwtest status.");
            }
            catch (Exception error)
            {
                string rollback = "No persistent spawn lock was written";
                try
                {
                    if (created != null)
                    {
                        // This exact new actor must not enter the legacy fallback
                        // after a profile failure. It stays visible to explicit
                        // cleanup if authority is unavailable; no saved scout is
                        // discovered, changed or claimed by this rollback.
                        var appearance = created.GetComponent<HumanAppearance>();
                        if (appearance != null) { appearance.Initialized = false; appearance.enabled = false; }
                        var companion = created.GetComponent<HumanCompanion>();
                        if (companion != null) companion.enabled = false;
                        var ai = created.GetComponent<MonsterAI>();
                        if (ai != null) ai.enabled = false;
                        var failedView = created.GetComponent<ZNetView>();
                        if (ReferenceEquals(ZNetScene.instance, spawnScene) && failedView != null && failedView.IsValid() &&
                            failedView.IsOwner() && haveCreatedId && failedView.GetZDO().m_uid == createdId)
                        {
                            spawnScene.Destroy(created);
                            npc = null;
                            // Native DestroyZDO queues persistence removal. A
                            // vanished scene object alone is insufficient proof.
                            if (ReferenceEquals(ZoneSystem.instance, spawnZone) && ZDOMan.instance != null &&
                                spawnScene.FindInstance(createdId) == null && ZDOMan.instance.GetZDO(createdId) == null)
                            {
                                rollback = "Exact failed instance and its saved record were confirmed removed";
                            }
                        }
                    }
                }
                catch (Exception cleanupError)
                { rollback += "; rollback failed: " + cleanupError.Message; }
                throw new InvalidOperationException("Fresh human spawn failed: " + error.Message + ". " + rollback + ".", error);
            }
        }

        internal static string[] NativeAppearanceChoices(string search)
        {
            if (ObjectDB.instance == null)
                throw new InvalidOperationException("Native appearance catalogs are not ready.");
            var result = new List<string> { "" };
            var items = ObjectDB.instance.GetAllItems(ItemDrop.ItemData.ItemType.Customization, search);
            if (items != null)
                foreach (var item in items)
                {
                    if (item == null || item.gameObject == null) continue;
                    string name = item.gameObject.name;
                    var prefab = String.IsNullOrEmpty(name) ? null : ObjectDB.instance.GetItemPrefab(name);
                    if (prefab == null || prefab.GetComponent<ItemDrop>() == null || result.Contains(name)) continue;
                    result.Add(name);
                }
            result.Sort(StringComparer.Ordinal);
            if (result.Count == 1)
                throw new InvalidOperationException("Valheim exposed no registered native " + search.ToLowerInvariant() + " assets.");
            return result.ToArray();
        }

        void FindNpc()
        {
            if (npc != null) return;
            npc = Character.GetAllCharacters().Where(c => c != null && !c.IsDead() && c.GetComponent<ManualNpcIdentity>() != null && !HumanCleric.IsCleric(c.gameObject))
                .Select(c => c.gameObject).FirstOrDefault();
        }
        void Hold()
        {
            if (npc == null) return;
            GuardOwner(npc);
            if (!npc.GetComponent<HumanCompanion>().TryIssueHold()) throw new InvalidOperationException("Hold order refused; inspect status.");
        }
        void PrepareParty()
        {
            FindNpc();
            if (npc == null) { Spawn(); FindNpc(); }
            if (npc == null) throw new InvalidOperationException("Scout creation did not produce a loaded person");
            GuardOwner(npc);
            npc.GetComponent<HumanPartyEquipment>().PrepareArcher();
            if (!npc.GetComponent<HumanCompanion>().TryIssueFollow(Player.m_localPlayer)) throw new InvalidOperationException("Scout follow not ready");
            var cleric = Character.GetAllCharacters().FirstOrDefault(c => c != null && HumanCleric.IsCleric(c.gameObject));
            if (cleric != null && !cleric.IsDead())
            {
                if (!cleric.GetComponent<HumanCompanion>().TryIssueFollow(Player.m_localPlayer)) throw new InvalidOperationException("Cleric initialization still pending; wait, then repeat prepare");
                Report("Party prepared: existing archer and cleric follow you. No refill, resurrection or player equipment changes."); return;
            }
            const string guard = "lwc_human_cleric_v1_spawned";
            ZoneSystem.instance.RemoveGlobalKey(guard);
            var prefab = ZNetScene.instance.GetPrefab(NpcId);
            if (!registered || prefab == null || !ReferenceEquals(prefab, registeredPrefab)) throw new InvalidOperationException("Human prefab registration incomplete");
            string[] hair = NativeAppearanceChoices("Hair"), beards = NativeAppearanceChoices("Beard");
            var at = FindPartySpawnPoint();
            GameObject created = null;
            try
            {
                created = Instantiate(prefab, at, Player.m_localPlayer.transform.rotation);
                GuardOwner(created); NativeHumanFactory.Validate(created, true);
                var zdo = created.GetComponent<ZNetView>().GetZDO();
                zdo.Set(HumanProfile.RequiredKey, "1"); zdo.Set(HumanCleric.RoleKey, "1");
                zdo.Set(LabTag, "npc"); zdo.Set(NpcKey, NpcId);
                created.GetComponent<HumanCompositionIdentity>().InitializeFresh(Player.m_localPlayer);
                created.GetComponent<HumanIdentity>().InitializeFresh(Guid.NewGuid().ToString("N"), hair, beards);
                created.GetComponent<Humanoid>().EquipBestWeapon(null, null, null, null);
                if (!created.GetComponent<HumanCompanion>().TryIssueFollow(Player.m_localPlayer))
                    throw new InvalidOperationException("New companion was created but follow initialization was refused");
                Report("Party ready: archer and support companion follow you. No refill, resurrection or raid was triggered.");
            }
            catch (Exception error)
            {
                // Appearance finishes through its bounded native lifecycle after
                // the fresh identity transaction. Preserve that valid actor while
                // the outer coroutine retries; otherwise every retry destroys the
                // support member before it can ever become command-ready.
                bool initializationPending = error.Message.IndexOf("follow initialization", StringComparison.OrdinalIgnoreCase) >= 0;
                if (created != null && !initializationPending)
                {
                    var view = created.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) ZNetScene.instance.Destroy(created);
                    else { var ai = created.GetComponent<MonsterAI>(); if (ai != null) ai.enabled = false; }
                }
                throw;
            }
        }

        Vector3 FindPartySpawnPoint()
        {
            var player = Player.m_localPlayer;
            Vector3[] directions = {
                -player.transform.right, player.transform.right, -player.transform.forward, player.transform.forward,
                (-player.transform.right - player.transform.forward).normalized,
                (player.transform.right - player.transform.forward).normalized,
                (-player.transform.right + player.transform.forward).normalized,
                (player.transform.right + player.transform.forward).normalized
            };
            foreach (float radius in new[] { 3f, 4.5f, 6f })
                foreach (Vector3 direction in directions)
                    try
                    {
                        Vector3 point = GroundPoint(player.transform.position + direction * radius);
                        RequireClear(point, 1f);
                        return point;
                    }
                    catch (InvalidOperationException) { }
            throw new InvalidOperationException("No clear companion spawn point within 6m. Move to open ground and retry.");
        }

        Character[] LoadedParty()
        {
            return Character.GetAllCharacters().Where(c => c != null && !c.IsDead() &&
                c.GetComponent<HumanCompanion>() != null).ToArray();
        }

        void LocateParty()
        {
            var player = Player.m_localPlayer;
            var members = LoadedParty();
            if (members.Length == 0) throw new InvalidOperationException("No living loaded companions found; nothing was changed");
            foreach (var member in members)
            {
                Vector3 delta = member.transform.position - player.transform.position;
                float distance = delta.magnitude;
                float bearing = Mathf.Atan2(Vector3.Dot(delta, player.transform.right),
                    Vector3.Dot(delta, player.transform.forward)) * Mathf.Rad2Deg;
                Report("LOCATE " + member.GetComponent<Humanoid>().m_name + ": distance=" + distance.ToString("F1") +
                    "m; relativeBearing=" + bearing.ToString("F0") + "deg (0 ahead, +right, -left, 180 behind); world=" +
                    member.transform.position.ToString("F1") + "; owner=" + member.GetComponent<ZNetView>().IsOwner());
            }
        }

        void RecoverParty()
        {
            var player = Player.m_localPlayer;
            var members = LoadedParty();
            if (members.Length == 0) throw new InvalidOperationException("No living loaded companions found; nothing was changed");
            if (members.Length > 8) throw new InvalidOperationException("Recovery refused: unexpected companion count " + members.Length);
            for (int i = 0; i < members.Length; ++i)
            {
                var member = members[i];
                var view = member.GetComponent<ZNetView>();
                if (view == null || !view.IsValid() || !view.IsOwner())
                    throw new InvalidOperationException("Recovery refused: local ownership unavailable for " + member.GetComponent<Humanoid>().m_name);
            }
            for (int i = 0; i < members.Length; ++i)
            {
                var member = members[i];
                float side = (i % 2 == 0 ? -1f : 1f) * (2f + (i / 2) * 1.5f);
                Vector3 at = GroundPoint(player.transform.position - player.transform.forward * 3f + player.transform.right * side);
                var ai = member.GetComponent<MonsterAI>();
                if (ai != null) { ai.SetFollowTarget(null); ai.ResetPatrolPoint(); ai.ResetRandomMovement(); ai.StopMoving(); }
                var rigidbody = member.GetComponent<Rigidbody>();
                if (rigidbody != null) { rigidbody.linearVelocity = Vector3.zero; rigidbody.angularVelocity = Vector3.zero; rigidbody.position = at; rigidbody.rotation = player.transform.rotation; }
                member.transform.SetPositionAndRotation(at, player.transform.rotation);
                member.GetComponent<ZNetView>().GetZDO().SetPosition(at);
                if (!member.GetComponent<HumanCompanion>().TryIssueHold())
                    throw new InvalidOperationException("Recovered companion could not enter Hold: " + member.GetComponent<Humanoid>().m_name);
                Report("RECOVERED existing " + member.GetComponent<Humanoid>().m_name + " to " + at.ToString("F1") + "; Hold saved; identity, inventory and health preserved");
            }
        }
        void BeginMovement()
        {
            goal = Player.m_localPlayer.transform.position;
            initialDistance = Horizontal(npc.transform.position, goal);
            if (initialDistance < 6f || initialDistance > 20f)
                throw new InvalidOperationException("First walk 6-20 metres from the scout on open ground, then use lwtest move and stand still.");
            if (!npc.GetComponent<Character>().IsOnGround()) throw new InvalidOperationException("Wait for the scout to settle on the ground.");
            pathSeen = false; unstableMotion = false;
            if (!npc.GetComponent<HumanCompanion>().BeginTemporaryFollow(Player.m_localPlayer))
                throw new InvalidOperationException("Temporary movement check refused; saved order is unchanged.");
            active = "movement"; deadline = Time.realtimeSinceStartup + 35f;
            CaptureCheckContext();
            Report("MOVE started (35 seconds). Close F5 and stay still. Requires native path, grounded progress and arrival within 3.75 m.");
        }
        void BeginCombat()
        {
            if (!npc.GetComponent<Character>().IsOnGround()) throw new InvalidOperationException("Scout must be grounded before combat.");
            var at = GroundPoint(npc.transform.position + Vector3.right * 5f);
            RequireClear(at, 1f);
            partyLab.CaptureProtection();
            var prefab = ZNetScene.instance.GetPrefab("Greydwarf");
            if (prefab == null) throw new InvalidOperationException("Native Greydwarf prefab unavailable.");
            enemy = UnityEngine.Object.Instantiate(prefab, at, Quaternion.identity);
            GuardOwner(enemy);
            targetUid = enemy.GetComponent<ZNetView>().GetZDO().m_uid;
            targetTracked = true;
            CaptureCheckContext();
            enemy.GetComponent<ZNetView>().GetZDO().Set(LabTag, "enemy");
            var drop = enemy.GetComponent<CharacterDrop>();
            if (drop != null) drop.m_drops = new List<CharacterDrop.Drop>();
            victim = enemy.GetComponent<Character>();
            victim.m_onDamaged += OnDamage;
            victim.m_onDeath += OnTargetDeath;
            damage = 0f; targetSeen = false; targetDied = false; foreignDamage = false; active = "combat";
            if (!npc.GetComponent<HumanCompanion>().TryAuthorizeCombatTarget(victim))
                throw new InvalidOperationException("Explicit test target authorization refused.");
            CaptureCheckContext();
            deadline = Time.realtimeSinceStartup + 35f;
            Report("COMBAT started: one Greydwarf, 35-second observation. Close F5 and stand back; do not attack it. A first hit alone does not pass. Target cleanup occurs after observed death, failure or stop.");
        }
        void OnDamage(float amount, Character attacker)
        {
            if (amount > 0f && npc != null && attacker == npc.GetComponent<Character>()) damage += amount;
            else if (amount > 0f) foreignDamage = true;
        }
        void OnTargetDeath() { targetDied = true; }

        void Update()
        {
            if (active == "idle") return;
            if (active == "cleanup")
            {
                if (ZNetScene.instance != checkScene || ZNet.instance == null || ZNet.instance.GetWorldUID() != checkWorldUid || Player.m_localPlayer == null)
                {
                    active = "idle";
                    Report("FAIL / interrupted: world or scene changed before cleanup confirmation. Previous result is unconfirmed.");
                    return;
                }
                if (ZNetScene.instance != null && ZNetScene.instance.FindInstance(cleanupUid) == null)
                {
                    enemy = null; targetTracked = false; active = "idle";
                    Report(pendingResult + " Target deregistration confirmed.");
                }
                else if (Time.realtimeSinceStartup >= deadline)
                {
                    active = "idle";
                    Report("FAIL cleanup: target deregistration was not confirmed. Use lwtest stop; do not start a second target.");
                }
                return;
            }
            try
            {
                GuardWorld(); GuardOwner(npc);
                if (ZNetScene.instance != checkScene || ZNet.instance.GetWorldUID() != checkWorldUid)
                    throw new InvalidOperationException("Test world/scene changed during observation.");
                if (Time.realtimeSinceStartup >= deadline) { EndCheck("FAIL " + active + ": 35-second deadline; inspect obstacles, weapon and log."); return; }
                var c = npc.GetComponent<Character>();
                if (active == "movement")
                {
                    var drift = Horizontal(Player.m_localPlayer.transform.position, goal);
                    if (drift > 1.5f) { EndCheck("FAIL movement: player moved after the goal was captured. Repeat while standing still."); return; }
                    pathSeen |= (bool)HavePath.Invoke(npc.GetComponent<BaseAI>(), new object[] { goal });
                    unstableMotion |= !c.IsOnGround() && Math.Abs(c.GetVelocity().y) > 1f;
                    var remaining = Horizontal(npc.transform.position, goal);
                    if (!unstableMotion && ManualAcceptance.MovementPassed(pathSeen, c.IsOnGround(), c.GetVelocity().y, initialDistance, remaining, drift))
                        EndCheck("PASS movement: native path=true, grounded arrival; horizontal progress=" + (initialDistance - remaining).ToString("F2") + "m, remaining=" + remaining.ToString("F2") + "m.");
                }
                else if (active == "combat")
                {
                    targetSeen |= victim != null && npc.GetComponent<MonsterAI>().GetTargetCreature() == victim;
                    var weapon = npc.GetComponent<Humanoid>().GetCurrentWeapon();
                    if (ManualAcceptance.FullCombatPassed(targetSeen, damage, HasExpectedWeapon(), targetDied, foreignDamage))
                        EndCheck("PASS observed combat: target death callback observed; NPC-attributed native damage=" + damage.ToString("F2") + "; no other damage observed; weapon=" + weapon.m_shared.m_name + ". Broader combat quality is not certified.");
                    else if (targetDied || enemy == null) EndCheck("FAIL combat: death/disappearance lacked complete unassisted scout evidence; attributed=" + damage.ToString("F2") + ", deathObserved=" + targetDied + ", otherDamage=" + foreignDamage + ".");
                }
            }
            catch (Exception error) { EndCheck("FAIL " + active + ": " + error.Message); }
        }
        void EndCheck(string result)
        {
            if (active == "cleanup") { Report("Target cleanup is awaiting confirmation."); return; }
            active = "idle";
            if (victim != null) { victim.m_onDamaged -= OnDamage; victim.m_onDeath -= OnTargetDeath; }
            victim = null;
            try
            {
                if (npc != null) { var companion = npc.GetComponent<HumanCompanion>(); companion.StopCombat(); companion.EndTemporaryOrder(); }
            }
            catch (Exception error) { Report("Order restore failed: " + error.Message); }
            if (enemy != null || targetTracked)
            {
                try
                {
                    if (!targetTracked) CaptureCheckContext();
                    if (ZNetScene.instance == null || ZNetScene.instance != checkScene || ZNet.instance == null || ZNet.instance.GetWorldUID() != checkWorldUid)
                        throw new InvalidOperationException("Target's original world/scene is unavailable.");
                    if (enemy != null)
                    {
                        GuardOwner(enemy);
                        targetUid = enemy.GetComponent<ZNetView>().GetZDO().m_uid;
                        targetTracked = true;
                        ZNetScene.instance.Destroy(enemy);
                    }
                    cleanupUid = targetUid;
                    pendingResult = result;
                    active = "cleanup";
                    deadline = Time.realtimeSinceStartup + 5f;
                }
                catch (Exception error)
                {
                    Report("FAIL cleanup (result withheld): " + error.Message + ". Target retained for lwtest stop.");
                    return;
                }
            }
            if (active != "cleanup") Report(result);
        }
        void Cleanup()
        {
            if (active != "idle" || enemy != null || targetTracked)
            {
                EndCheck("Checks stopped for cleanup. Repeat lwtest cleanup after target removal.");
                return;
            }
            var marked = UnityEngine.Object.FindObjectsOfType<ZNetView>().Where(v => v != null && v.IsValid() && v.GetZDO().GetString(LabTag, "") != "").ToArray();
            foreach (var view in marked) GuardOwner(view.gameObject);
            foreach (var view in marked) ZNetScene.instance.Destroy(view.gameObject);
            // This command is an explicit destructive reset confined to the
            // disposable lab world. Clear the transaction marker even when a
            // prior failed build already removed its scene object.
            ZoneSystem.instance.RemoveGlobalKey(NpcSpawnedKey);
            ZoneSystem.instance.RemoveGlobalKey("lwc_human_cleric_v1_spawned");
            npc = null;
            Report("CLEANUP removed " + marked.Length + " loaded test-tagged objects and reset the scout spawn marker. Setup marker remains.");
        }
        void Status()
        {
            FindNpc();
            Report("STATUS human plugin=" + Info.Metadata.Version + "; registered=" + registered + "; check=" + active + "; scoutLoaded=" + (npc != null) + ".");
            if (npc != null)
            {
                var humanoid = npc.GetComponent<Humanoid>();
                var human = npc.GetComponent<HumanAppearance>();
                Report("Human: " + (human == null ? "adapter missing" : human.State));
                Report("Composition: " + npc.GetComponent<HumanCompositionIdentity>().Status);
                Report("Companion: " + npc.GetComponent<HumanCompanion>().Status);
                Report("Door travel: " + npc.GetComponent<HumanDoorTraversal>().Status);
                Report("Companion presence: " + npc.GetComponent<HumanFollowPresence>().Status);
                Report("Resources: " + npc.GetComponent<HumanResources>().Describe());
                Report("Identity: " + npc.GetComponent<HumanIdentity>().Status);
                Report("Party confidence: " + npc.GetComponent<HumanPartyConfidence>().Describe());
                Report("Companion activity: " + npc.GetComponent<HumanCompanionAmbient>().Status);
                Report("Scout health=" + humanoid.GetHealth() + "; grounded=" + humanoid.IsOnGround() + "; weapon=" +
                    (humanoid.GetCurrentWeapon() == null ? "none" : humanoid.GetCurrentWeapon().m_shared.m_name) + "; expectedNativeWeaponEquipped=" + HasExpectedWeapon() + ".");
            }
        }
        bool HasExpectedWeapon()
        {
            if (npc == null || String.IsNullOrEmpty(expectedWeaponName)) return false;
            var humanoid = npc.GetComponent<Humanoid>();
            var item = humanoid.GetCurrentWeapon();
            return item != null && item.m_equipped && item.m_shared.m_name == expectedWeaponName &&
                humanoid.GetInventory().GetAllItems().Contains(item);
        }
        void CaptureCheckContext()
        {
            checkScene = ZNetScene.instance;
            checkWorldUid = ZNet.instance == null ? 0L : ZNet.instance.GetWorldUID();
        }
        static void GuardOwner(GameObject value)
        {
            if (value == null) throw new InvalidOperationException("Fixture instance is missing.");
            var view = value.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) throw new InvalidOperationException("Fixture lacks local authority; ownership will not be claimed.");
        }
        internal static Vector3 GroundPoint(Vector3 at)
        {
            float height;
            if (!ZoneSystem.instance.GetGroundHeight(at, out height) || height < ZoneSystem.instance.m_waterLevel + 0.5f)
                throw new InvalidOperationException("Choose dry loaded terrain.");
            at.y = height + 0.15f;
            return at;
        }
        internal static void RequireClear(Vector3 at, float radius)
        {
            foreach (var c in Physics.OverlapCapsule(at + Vector3.up * 0.5f, at + Vector3.up * 1.7f, radius))
                if (!c.isTrigger && !(c is TerrainCollider) && c.GetComponent<Heightmap>() == null)
                    throw new InvalidOperationException("Spawn point is obstructed. Face open ground and retry.");
        }
        static float Horizontal(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }
        internal void Report(string text)
        {
            Logger.LogInfo("[LWC LAB] " + text);
            if (Console.instance != null) Console.instance.Print("[LWC LAB] " + text);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text, 0, null, false);
        }
        void Help()
        {
            Report("DISCOVERY: type lwcommands for this list. Type lwtest then press Tab to cycle every supported action. lwtest, lwparty and lwcommands also appear in normal console completion.");
            Report("PARTY: lwparty | lwtest party prepare | lwtest party status | lwtest party locate | lwtest party recover.");
            Report("WAVES: lwtest wave boarneck | lwtest wave greydwarf | lwtest wave go | lwtest wave status | lwtest wave stop. Arming never starts a wave; go starts after 10 seconds. Never use cleanup to stop waves.");
            Report("ORDERS/TESTS: lwtest follow | hold | move | combat | stop | status. WORLD/LIFECYCLE: lwtest setup | spawn | migrate | cleanup | help.");
            Report("Use existing character " + ManualAcceptance.ProfileName + " and world " + ManualAcceptance.WorldName + "; leave Start Server off. Do not rebuild the existing lab.");
            Report("Existing scout: inspect status; only legacy 0.2.x requires lwtest migrate. follow/hold persist orders. move is a temporary 6-20m check. combat is the old single-enemy measurement, not the new wave test. stop ends that measurement; wave stop ends waves. spawn creates a replacement beside you whenever no living scout is loaded. WARNING: cleanup removes loaded marked lab objects and companions.");
            Report("0.3.19 separates peaceful door etiquette from combat pursuit: peaceful companions close what they open; combat pursuit opens, crosses and leaves the passage available to the party. Retreat/breach tactics remain separate future behaviors. It remains a lab candidate, not certified gameplay.");
        }
        void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= Register;
            if (victim != null) { victim.m_onDamaged -= OnDamage; victim.m_onDeath -= OnTargetDeath; }
            if (harmony != null) harmony.UnpatchSelf();
            Instance = null;
        }
    }

    public sealed class ManualNpcIdentity : MonoBehaviour
    {
        // Marker only. Saved metadata is inspected, never blessed automatically.
    }
}


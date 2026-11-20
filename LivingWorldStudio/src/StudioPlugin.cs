using System;
using System.IO;
using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Studio
{
    [BepInPlugin(Id, "Living World Studio Lab", "0.1.0")]
    [BepInDependency(Jotunn.Main.ModGuid)]
    public sealed class StudioPlugin : BaseUnityPlugin
    {
        public const string Id = "com.scrufboy.livingworldcore.studio";
        const string DoorId = "LWC_StudioDoor", ReturnKey = "lwc_studio_return_v1";
        const string LabName = "LWC_StudioLab_v1";
        internal static StudioPlugin Instance;
        StudioJournal journal;
        StudioRecord record;
        StudioRoom room;
        IDisposable environment;
        StudioRenderScope renderScope;
        Player observedPlayer;
        long observedWorld;
        bool checkedRecovery, registered, markers = true;
        float deadline, nextRetry, recoveryRetry, nextError;
        string lastError;
        string palette = "green";
        int selectedProp, selectedPad, yawStep, selectedScale;
        static readonly float[] Scales = { 1f, .75f, .5f, .25f };
        Terminal.ConsoleCommand command;
        static readonly string[] Scenery = { "Bush01", "Bush02", "RaspberryBush", "BlueberryBush", "piece_workbench", "piece_chair", "piece_bench01", "piece_table", "piece_chest_wood", "woodwall", "wood_pole2", "Birch1", "Beech1", "FirTree", "PineTree" };

        void Awake()
        {
            Instance = this;
            journal = new StudioJournal(Path.Combine(Paths.ConfigPath, "LivingWorldStudio", "returns"));
            PrefabManager.OnVanillaPrefabsAvailable += RegisterDoor;
            command = new Terminal.ConsoleCommand("lwstudio", "Studio lab: status, recover, exit, green, blue, neutral, clear. Door and room consoles use ordinary Interact.",
                new Terminal.ConsoleEvent(args => Run(() => Command(args.Args))));
            Logger.LogInfo("Studio Lab 0.1.0: independent player/scenery prototype; no companion transfers. Disposable world AND character LWC_StudioLab_v1; Start Server OFF.");
        }

        void RegisterDoor()
        {
            if (registered) return;
            try
            {
                var config = new PieceConfig { Name = "Portrait Hall Door (Studio Lab)", Description = "Photography lab entrance. Disposable studio world only; no NPC transfer.", PieceTable = "Hammer", Category = "Misc" };
                config.AddRequirement("Wood", 10, true);
                var piece = new CustomPiece(DoorId, "wood_door", config);
                var native = piece.PiecePrefab.GetComponent<Door>();
                if (native != null) UnityEngine.Object.DestroyImmediate(native);
                piece.PiecePrefab.AddComponent<StudioControl>().Action = "enter";
                if (!PieceManager.Instance.AddPiece(piece)) throw new InvalidOperationException("Door registration was refused.");
                registered = true;
                PrefabManager.OnVanillaPrefabsAvailable -= RegisterDoor;
            }
            catch (Exception error) { Logger.LogError("Studio entrance unavailable: " + error); }
        }

        internal void Use(string action)
        {
            Run(() =>
            {
                if (action == "enter") { Enter(); return; }
                if (action == "exit" || action == "recover") { if (record == null) LoadRecovery(); BeginReturn(); return; }
                RequireActive();
                if (action == "green" || action == "blue" || action == "neutral") { room.SetPalette(action); palette = action; Say("Backdrop: " + action); }
                else if (action == "select") { selectedProp = (selectedProp + 1) % Scenery.Length; Say("Scenery: " + Scenery[selectedProp] + " (visual copy only)"); }
                else if (action == "pad") { selectedPad = (selectedPad + 1) % 16; Say("Placement pad " + (selectedPad + 1)); }
                else if (action == "rotate") { yawStep = (yawStep + 1) % 8; Say("Scenery yaw " + yawStep * 45 + " degrees"); }
                else if (action == "scale") { selectedScale = (selectedScale + 1) % Scales.Length; Say("Scenery scale " + Scales[selectedScale]); }
                else if (action == "place") { room.PlaceProp(Scenery[selectedProp], Pad(selectedPad), yawStep * 45f, Scales[selectedScale]); Say("Placed decorative " + Scenery[selectedProp]); }
                else if (action == "clear") { room.ClearProps(); Say("Decorative scenery cleared."); }
                else if (action == "pads") { markers = !markers; room.SetMarkersVisible(markers); Say("Pad markers " + (markers ? "visible" : "hidden")); }
            });
        }

        internal string Describe(string action)
        {
            switch (action)
            {
                case "enter": return "Portrait Hall Door — enter studio lab (player only)";
                case "exit": return "Exit studio — return to recorded origin";
                case "select": return "Choose scenery: " + Scenery[selectedProp] + " — interact for next";
                case "pad": return "Placement pad " + (selectedPad + 1) + "/16 — interact for next";
                case "rotate": return "Scenery yaw " + yawStep * 45 + "° — interact for next";
                case "scale": return "Scenery scale " + Scales[selectedScale] + " — interact for next";
                case "place": return "Place " + Scenery[selectedProp] + " at pad " + (selectedPad + 1) + " (decorative only)";
                case "clear": return "Clear all decorative scenery";
                case "pads": return "Show/hide subject pad markers for screenshots";
                default: return "Backdrop: " + action;
            }
        }

        static Vector3 Pad(int i) { return new Vector3((i % 4 - 1.5f) * 3f, .02f, 1f + (i / 4) * 2.5f); }

        void GuardLab()
        {
            if (ZNet.instance == null || Game.instance == null || Player.m_localPlayer == null || ZNetScene.instance == null)
                throw new InvalidOperationException("Wait for a loaded local player and world.");
            if (ZNet.instance.GetWorldName() != LabName || !String.Equals(Game.instance.GetPlayerProfile().GetFilename(), LabName, StringComparison.OrdinalIgnoreCase) ||
                !ZNet.instance.IsServer() || ZNet.IsOpenServer() || ZNet.instance.GetPeers().Count != 0)
                throw new InvalidOperationException("This first lab build requires world AND character LWC_StudioLab_v1, Start Server OFF and no peers. Production worlds and multiplayer are disabled.");
        }

        void Enter()
        {
            GuardLab();
            var player = Player.m_localPlayer;
            if (!checkedRecovery) throw new InvalidOperationException("Recovery check is still pending.");
            var previous = journal.Load(ZNet.instance.GetWorldUID(), player.GetPlayerID());
            if (record != null || (previous != null && previous.Phase != "Restored") || player.m_customData.ContainsKey(ReturnKey))
                throw new InvalidOperationException("A saved return transaction exists. Use recovery before entering again.");
            if (player.IsDead() || player.IsTeleporting() || !player.IsOnGround()) throw new InvalidOperationException("Stand on safe ground before entering.");
            // Existing follow/presence systems may catch up after teleport. Do not move a player
            // away from loaded AI actors while no photo suspension contract exists.
            foreach (var component in UnityEngine.Object.FindObjectsOfType<MonoBehaviour>())
                if (component != null && component.GetType().Name == "HumanCompanion")
                    throw new InvalidOperationException("A Living World companion is loaded. This lab has no actor suspension adapter; use a separate empty photography world.");
            var at = player.transform.position;
            if (!ClearLanding(player, at)) throw new InvalidOperationException("Origin must have clear standing space and a solid floor.");
            var rotation = player.transform.rotation;
            var destination = new Vector3(at.x, 5000f, at.z);
            if (Physics.OverlapBox(destination + Vector3.up * 6f, new Vector3(18f, 8f, 18f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Length != 0)
                throw new InvalidOperationException("The experimental studio volume is obstructed. No movement performed.");
            try
            {
                BuildRoom(destination);
                record = new StudioRecord { World = ZNet.instance.GetWorldUID(), Player = player.GetPlayerID(), Session = Guid.NewGuid().ToString("N"), Phase = "Prepared",
                    X = at.x, Y = at.y, Z = at.z, Qx = rotation.x, Qy = rotation.y, Qz = rotation.z, Qw = rotation.w,
                    RoomX = destination.x, RoomY = destination.y, RoomZ = destination.z };
                Persist();
                record.Phase = "Entering"; Persist();
                deadline = Time.realtimeSinceStartup + 40f;
                if (!player.TeleportTo(room.Landing, Quaternion.identity, false)) BeginReturn();
                else Say("Entering studio. Original position and rotation recorded before movement.");
            }
            catch
            {
                // Keep room + durable record if any write/movement was attempted. Recovery is idempotent.
                if (record == null) DisposeRoom();
                throw;
            }
        }

        void BuildRoom(Vector3 at)
        {
            if (room != null) return;
            room = StudioRoom.Build(at);
            palette = "green";
            markers = true;
            try
            {
                room.CreateExitDoor().AddComponent<StudioControl>().Action = "exit";
                environment = StudioEnvironment.Attach(at + Vector3.up * 6f);
                var actions = new[] { "exit", "green", "blue", "neutral", "select", "pad", "rotate", "scale", "place", "clear", "pads" };
                for (int i = 0; i < actions.Length; ++i)
                    room.CreateControl(actions[i], new Vector3(-13f + i * 2.6f, .5f, -14f)).AddComponent<StudioControl>().Action = actions[i];
            }
            catch { DisposeRoom(); throw; }
        }

        void Persist()
        {
            journal.Save(record); // Independent flushed journal is authoritative; native save returns no success ack.
            Player.m_localPlayer.m_customData[ReturnKey] = StudioJournal.Encode(record);
            if (Game.instance != null) Game.instance.SavePlayerProfile(false);
        }

        void BeginReturn()
        {
            if (record == null) { Say("No active studio return record."); return; }
            if (ZNet.instance == null || Player.m_localPlayer == null || record.World != ZNet.instance.GetWorldUID() || record.Player != Player.m_localPlayer.GetPlayerID())
                throw new InvalidOperationException("Return record belongs to another world or character. It is retained.");
            record.Phase = "Returning";
            // Any previously durable phase means recover to this same origin. A failed
            // phase update must not block a safe return from a reconstructed session.
            try { Persist(); } catch (Exception error) { ReportError(error); }
            nextRetry = 0f;
            Say("Returning to recorded origin. Obstructed origins remain pending; the room and return record are retained.");
        }

        void Update()
        {
            var player = Player.m_localPlayer;
            if (player == null || ZNet.instance == null || ZNetScene.instance == null)
            {
                if (observedPlayer != null || room != null) ResetScene();
                return;
            }
            if (player != observedPlayer || observedWorld != ZNet.instance.GetWorldUID())
            {
                ResetScene(); observedPlayer = player; observedWorld = ZNet.instance.GetWorldUID();
            }
            if (!checkedRecovery && Time.realtimeSinceStartup >= recoveryRetry)
            {
                recoveryRetry = Time.realtimeSinceStartup + 5f;
                Run(LoadRecovery);
            }
            if (record == null) return;
            Run(() =>
            {
                if (record.Phase == "Prepared") { BeginReturn(); return; }
                if (record.Phase == "Entering")
                {
                    if (!player.IsTeleporting() && Vector3.Distance(player.transform.position, room.Landing) < 2f)
                    { record.Phase = "Active"; Persist(); Say("Studio ready. Walk to a timber console and use ordinary Interact. Green, blue, neutral; scenery selection, pad, rotation, place and clear."); }
                    else if (Time.realtimeSinceStartup > deadline && !player.IsTeleporting()) BeginReturn();
                }
                if (record.Phase == "Active" && (player.transform.position.y < record.RoomY - 2f || Math.Abs(player.transform.position.x - record.RoomX) > 18f || Math.Abs(player.transform.position.z - record.RoomZ) > 18f)) BeginReturn();
                if (record.Phase == "Returning") ReturnTick(player);
                UpdateRenderScope();
            });
        }

        void LoadRecovery()
        {
            var player = Player.m_localPlayer;
            if (player == null || ZNet.instance == null) throw new InvalidOperationException("Recovery waits for a loaded player/world.");
            long world = ZNet.instance.GetWorldUID();
            var saved = journal.Load(world, player.GetPlayerID());
            string mirror;
            if (saved == null && player.m_customData.TryGetValue(ReturnKey, out mirror)) saved = StudioJournal.Decode(mirror);
            if (saved == null) { checkedRecovery = true; return; }
            if (saved.World != world || saved.Player != player.GetPlayerID()) throw new InvalidOperationException("Return mirror is from another world/character; no movement performed.");
            if (saved.Phase == "Restored")
            {
                // A completed disk tombstone fences a stale character-save mirror.
                // Native SavePlayerProfile exposes no successful flush acknowledgement.
                if (player.m_customData.TryGetValue(ReturnKey, out mirror))
                {
                    var stale = StudioJournal.Decode(mirror);
                    if (stale.Session != saved.Session) throw new InvalidOperationException("Return mirror conflicts with the completed session; records retained for inspection.");
                    player.m_customData.Remove(ReturnKey);
                    if (Game.instance != null) Game.instance.SavePlayerProfile(false);
                }
                checkedRecovery = true; return;
            }
            record = saved;
            BeginReturn(); // Rebuild is useful but is never a prerequisite to safe return.
            checkedRecovery = true;
            try { BuildRoom(new Vector3(record.RoomX, record.RoomY, record.RoomZ)); }
            catch (Exception error) { ReportError(error); }
        }

        void ReturnTick(Player player)
        {
            if (player.IsDead() || player.IsTeleporting()) return;
            var origin = new Vector3(record.X, record.Y, record.Z);
            var rotation = new Quaternion(record.Qx, record.Qy, record.Qz, record.Qw);
            if (player.IsOnGround() && ClearLanding(player, origin) && Vector3.Distance(player.transform.position, origin) < .15f && Quaternion.Angle(player.transform.rotation, rotation) < 2f)
            {
                // Arrival must be real before any journal cleanup.
                record.Phase = "Restored";
                journal.Save(record); // Keep one durable completed-session fence per world/character.
                player.m_customData.Remove(ReturnKey);
                try { if (Game.instance != null) Game.instance.SavePlayerProfile(false); }
                catch (Exception error) { ReportError(error); } // Completion fence prevents replay of a stale mirror.
                record = null;
                DisposeRoom(); Say("Returned to exact origin and rotation. Studio scenery cleaned up."); return;
            }
            if (Time.realtimeSinceStartup < nextRetry) return;
            nextRetry = Time.realtimeSinceStartup + 5f;
            // Native distant teleport loads the origin zone before solid-floor placement.
            // Once loaded, blocked capsules fail closed rather than choosing a secret offset.
            if (!ClearLanding(player, origin)) { Say("Exact origin is not yet clear/loaded. Recovery remains pending; retrying. Do not remove the mod."); return; }
            // Native non-distant mode loads the area and reverts if no floor is found;
            // distant mode may invent a fallback Y after its timeout, which is forbidden here.
            player.TeleportTo(origin, rotation, false);
        }

        static bool ClearLanding(Player player, Vector3 at)
        {
            foreach (var collider in Physics.OverlapCapsule(at + Vector3.up * .55f, at + Vector3.up * 1.5f, .35f, ~0, QueryTriggerInteraction.Ignore))
                if (!collider.transform.IsChildOf(player.transform)) return false;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * .25f, Vector3.down, .8f, ~0, QueryTriggerInteraction.Ignore))
                if (!hit.collider.transform.IsChildOf(player.transform) && hit.normal.y > .6f) return true;
            return false;
        }

        void UpdateRenderScope()
        {
            var camera = Camera.main;
            if (camera == null) return;
            if (renderScope == null || renderScope.gameObject != camera.gameObject)
            {
                if (renderScope != null) { renderScope.Active = false; renderScope.Restore(); UnityEngine.Object.Destroy(renderScope); }
                renderScope = camera.gameObject.AddComponent<StudioRenderScope>();
            }
            renderScope.Active = record != null && room != null && Math.Abs(Player.m_localPlayer.transform.position.y - record.RoomY) < 20f;
            renderScope.ChromaColor = palette == "blue" ? Color.blue : palette == "neutral" ? new Color(.502f, .502f, .502f) : Color.green;
        }

        void Command(string[] args)
        {
            if (args.Length < 2 || args[1] == "status") { Say("Studio Lab 0.1.0; door=" + registered + "; state=" + (record == null ? "outside" : record.Phase) + "; player/scenery only; no companion/multiplayer support."); return; }
            string action = args[1];
            if (action != "recover" && action != "exit" && action != "green" && action != "blue" && action != "neutral" && action != "clear") throw new InvalidOperationException("Use studio door/consoles, or lwstudio status|recover|exit|green|blue|neutral|clear.");
            Use(action);
        }
        void RequireActive()
        {
            if (record == null || record.Phase != "Active" || room == null) throw new InvalidOperationException("Enter the studio before using its controls.");
        }
        void Run(Action action) { try { action(); } catch (Exception error) { ReportError(error); } }
        void ReportError(Exception error)
        {
            string message = error.GetType().FullName + ": " + error.Message;
            if (lastError == message && Time.realtimeSinceStartup < nextError) return;
            lastError = message; nextError = Time.realtimeSinceStartup + 5f;
            Logger.LogError(error); Say("Studio: " + error.Message);
        }
        void Say(string text)
        {
            Logger.LogInfo(text);
            if (Console.instance != null) Console.instance.Print(text);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, text, 0, null, false);
        }
        void DisposeRoom()
        {
            if (environment != null) { environment.Dispose(); environment = null; }
            if (renderScope != null) { renderScope.Active = false; renderScope.Restore(); UnityEngine.Object.Destroy(renderScope); renderScope = null; }
            if (room != null) { room.Dispose(); room = null; }
        }
        void ResetScene() { DisposeRoom(); record = null; observedPlayer = null; observedWorld = 0; checkedRecovery = false; recoveryRetry = 0; }
        void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterDoor;
            DisposeRoom(); Instance = null; // Durable return record intentionally survives shutdown.
        }
    }

    public sealed class StudioControl : MonoBehaviour, Hoverable, Interactable
    {
        public string Action = "enter";
        public string GetHoverName() { return "Living World Studio"; }
        public float GetHoverOffset() { return 0f; }
        public string GetHoverText() { return StudioPlugin.Instance == null ? "Studio unavailable" : StudioPlugin.Instance.Describe(Action) + "\n[<color=yellow><b>$KEY_Use</b></color>] Use"; }
        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold || user != Player.m_localPlayer || StudioPlugin.Instance == null) return false;
            StudioPlugin.Instance.Use(Action); return true;
        }
        public bool UseItem(Humanoid user, ItemDrop.ItemData item) { return false; }
    }
}

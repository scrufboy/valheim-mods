using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace Scrufboy.LivingWorldCORE.Tooler
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("local.livingworld.humannpc", BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class LivingWorldCoreToolerPlugin : BaseUnityPlugin
    {
        public const string Guid = "com.scrufboy.livingworldcore.tooler";
        public const string Name = "Living World C.O.R.E. Tooler";
        public const string Version = "2.0.0";

        ConfigEntry<KeyboardShortcut> toggleKey;
        ConfigEntry<float> uiScale;
        Rect window = new Rect(60, 60, 760, 650);
        Vector2 scroll;
        bool visible;
        int tab;
        string lastAction = "Ready. Open a disposable Lab world, then choose an action.";
        string customCommand = "lwtest status";
        string spawnName = "Greydwarf", spawnAmount = "1", spawnLevel = "1";
        string itemName = "Wood", itemAmount = "50", itemQuality = "1";
        string coordinates = "0 0";
        string environmentName = "Clear";
        string terrainRadius = "8";
        string pendingDanger;
        float pendingUntil;
        GUIStyle windowStyle, titleStyle, sectionStyle, buttonStyle, statusStyle, warningStyle;
        Texture2D windowBackground, panelBackground;

        static readonly string[] Tabs = { "Overview", "Lifecycle", "Community", "Combat", "Party", "Raids", "Player", "Items / Spawn", "World", "Terrain", "Map / Admin", "Console" };

        void Awake()
        {
            toggleKey = Config.Bind("Interface", "ToggleKey", new KeyboardShortcut(KeyCode.Insert), "Open or close the Living World C.O.R.E. Tooler. Ignored while typing in chat, the console, or a text field.");
            uiScale = Config.Bind("Interface", "Scale", 1f, new ConfigDescription("Tooler scale.", new AcceptableValueRange<float>(0.75f, 1.5f)));
            Logger.LogInfo(Name + " " + Version + " loaded. Press " + toggleKey.Value + " to open the GUI; rebind Interface.ToggleKey in the generated config if needed.");
        }

        void Update()
        {
            if (toggleKey.Value.IsDown() && !IsTyping()) SetVisible(!visible);
            if (visible && Input.GetKeyDown(KeyCode.Escape)) SetVisible(false);
        }

        void SetVisible(bool value)
        {
            visible = value;
            if (!visible) pendingDanger = null;
        }

        bool IsTyping()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            if (Console.IsVisible() || TextInput.IsVisible()) return true;
            return visible && GUI.GetNameOfFocusedControl() == "LivingWorldCommandField";
        }

        void OnGUI()
        {
            if (!visible) return;
            EnsureStyles();
            float scale = uiScale.Value;
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            window = GUI.Window(GetInstanceID(), window, DrawWindow, Name + "  v" + Version, windowStyle);
            GUI.matrix = old;
        }

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            windowBackground = SolidTexture(new Color(0.055f, 0.065f, 0.075f, 1f));
            panelBackground = SolidTexture(new Color(0.10f, 0.115f, 0.13f, 1f));
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = windowBackground;
            windowStyle.onNormal.background = windowBackground;
            windowStyle.padding = new RectOffset(14, 14, 24, 12);
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            sectionStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            buttonStyle = new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleLeft, wordWrap = true, fixedHeight = 42 };
            statusStyle = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true, padding = new RectOffset(10, 10, 8, 8) };
            statusStyle.normal.background = panelBackground;
            warningStyle = new GUIStyle(statusStyle); warningStyle.normal.textColor = new Color(1f, .75f, .25f);
        }

        static Texture2D SolidTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        void OnDestroy()
        {
            if (windowBackground != null) Destroy(windowBackground);
            if (panelBackground != null) Destroy(panelBackground);
        }

        void DrawWindow(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Living World Lab", titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(70))) SetVisible(false);
            GUILayout.EndHorizontal();

            DrawEnvironment();
            DrawTabRow(0, 6);
            DrawTabRow(6, Tabs.Length);

            scroll = GUILayout.BeginScrollView(scroll);
            if (tab == 0) Overview();
            else if (tab == 1) Lifecycle();
            else if (tab == 2) Community();
            else if (tab == 3) Combat();
            else if (tab == 4) Party();
            else if (tab == 5) Raids();
            else if (tab == 6) PlayerTools();
            else if (tab == 7) ItemsAndSpawn();
            else if (tab == 8) WorldTools();
            else if (tab == 9) TerrainTools();
            else if (tab == 10) MapAndAdmin();
            else ConsolePage();
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.Label(lastAction, statusStyle, GUILayout.MinHeight(48));
            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        void DrawTabRow(int first, int end)
        {
            GUILayout.BeginHorizontal();
            for (int i = first; i < end; i++)
                if (GUILayout.Toggle(tab == i, Tabs[i], GUI.skin.button, GUILayout.MinWidth(105))) tab = i;
            GUILayout.EndHorizontal();
        }

        void DrawEnvironment()
        {
            bool game = ZNet.instance != null && Player.m_localPlayer != null;
            string mode = !game ? "Not in a world" : (ZNet.instance.IsServer() ? (ZNet.IsOpenServer() ? "Server/open world" : "Local/server authority") : "Connected client");
            int peers = ZNet.instance == null ? 0 : ZNet.instance.GetPeers().Count;
            GUILayout.Label("State: " + mode + "  |  peers: " + peers + "  |  command target: local game console", game ? statusStyle : warningStyle);
            GUILayout.Label("Toggle: " + toggleKey.Value + " (rebind in BepInEx/config/local.livingworld.labtooler.cfg). The shortcut is ignored while typing.", statusStyle);
        }

        void Overview()
        {
            Heading("Fast test loop");
            Row(new ActionDef("Prepare / recover party", "lwparty", "Creates or recovers the ready four-person party and starts follow."),
                new ActionDef("Party status", "lwtest party status", "Shows party, cleric, and encounter state."));
            Row(new ActionDef("Bring party to me", "lwtest party recover", "Recovers the saved party beside the tester."),
                new ActionDef("Locate party", "lwtest party locate", "Reports where saved party members are."));
            Row(new ActionDef("Stop measured test", "lwtest stop", "Ends the old single-target measurement."),
                new ActionDef("Stop raid/wave", "lwtest wave stop", "Ends the active specialized wave."));
            Heading("Important");
            GUILayout.Label("The current Human NPC Lab intentionally accepts mutations only in its disposable local Lab world/profile with Start Server OFF and no remote peers. This interface preserves those safeguards and reports refusals through the existing command system.", warningStyle);
        }

        void Lifecycle()
        {
            Heading("World and NPC lifecycle");
            Row(new ActionDef("Build lab once", "lwtest setup", "Builds native Lab pieces; never duplicates a marked Lab."),
                new ActionDef("Spawn scout", "lwtest spawn", "Creates a replacement only when no living loaded scout is available."));
            Row(new ActionDef("Status / diagnostics", "lwtest status", "Shows loaded NPC, ownership, behavior, and current test state."),
                new ActionDef("Show commands", "lwcommands", "Prints the Human NPC build's authoritative command list."));
            DangerButton("Advanced: migrate legacy 0.2.x scout", "lwtest migrate", "Compatibility-only operation; not part of an ordinary test loop.");
            DangerButton("Clean up marked Lab objects", "lwtest cleanup", "Removes loaded marked Lab objects and companions. This is intentionally two-step.");
            GUILayout.Label("Spawn creates a new person. If an existing companion is merely unloaded or lost, spawning can create an unintended replacement; use Locate and Emergency Recover first.", warningStyle);
            GUILayout.Label("Registration is owned by the Human NPC plugin during Valheim/Jotunn startup. It is not safe to hot-deregister a live network prefab. Restart the profile after changing mod registration.", warningStyle);
        }

        void Community()
        {
            Heading("Community living / casual behavior");
            Row(new ActionDef("Follow tester", "lwtest follow", "Saves the follow order using stable leader identity."),
                new ActionDef("Hold here", "lwtest hold", "Saves hold position; nearby defense remains enabled."));
            Row(new ActionDef("Movement check", "lwtest move", "Runs the measured 6-20m navigation check."),
                new ActionDef("Stop current check", "lwtest stop", "Stops the current movement/combat measurement."));
            Row(new ActionDef("Party ready + follow", "lwparty", "One-button repeatable party preparation."),
                new ActionDef("Recover to tester", "lwtest party recover", "Returns/replaces the ready party through the Lab transaction."));
            GUILayout.Label("Cooking, repair, sleep, guard jobs, trading, occupations, and full town simulation are not console actions in 0.3.32. They are shown as unavailable instead of inventing destructive or false commands.", warningStyle);
        }

        void Combat()
        {
            Heading("Measured combat");
            Row(new ActionDef("Single-enemy combat", "lwtest combat", "Runs the older measured combat test."),
                new ActionDef("Combat status", "lwtest status", "Shows target, damage, motion, and current result."));
            Row(new ActionDef("Stop single test", "lwtest stop", "Safely ends the measured check."),
                new ActionDef("Prepare full party", "lwparty", "Ensures the party is available before a wave."));
        }

        void Party()
        {
            Heading("Party control and recovery");
            Row(new ActionDef("Prepare", "lwtest party prepare", "Creates or recovers the party."),
                new ActionDef("Status", "lwtest party status", "Shows party and encounter state."));
            Row(new ActionDef("Locate", "lwtest party locate", "Reports the party location."),
                new ActionDef("Recover / teleport to me", "lwtest party recover", "Uses the official party recovery transaction."));
            Row(new ActionDef("One-button party", "lwparty", "Prepare/recover and follow."),
                new ActionDef("Follow", "lwtest follow", "Orders the loaded scout to follow."));
        }

        void Raids()
        {
            Heading("Specialized Living World waves");
            Row(new ActionDef("Arm Boar + Neck", "lwtest wave boarneck", "Arms the close-range mixed wave; does not start it."),
                new ActionDef("Arm Greydwarf", "lwtest wave greydwarf", "Arms the greydwarf wave; does not start it."));
            Row(new ActionDef("GO — 10 second countdown", "lwtest wave go", "Starts the armed wave after the built-in countdown."),
                new ActionDef("Wave status", "lwtest wave status", "Reports wave/party state."));
            DangerButton("Stop active wave", "lwtest wave stop", "Stops the specialized wave. Use this instead of cleanup.");
            Heading("Vanilla raid console");
            GUILayout.Label("These start Valheim's real native raid events through the installed Server Devcommands permission path.", statusStyle);
            Row(new ActionDef("Eikthyr army", "event army_eikthyr", "Real native raid event."),
                new ActionDef("Elder army", "event army_theelder", "Real native raid event."));
            Row(new ActionDef("Bonemass army", "event army_bonemass", "Real native raid event."),
                new ActionDef("Moder army", "event army_moder", "Real native raid event."));
            Row(new ActionDef("Fuling army", "event army_goblin", "Real native raid event."),
                new ActionDef("Seeker army", "event army_seekers", "Real native raid event."));
            Row(new ActionDef("Gjall army", "event army_gjall", "Real native raid event."),
                new ActionDef("Charred army", "event army_charred", "Real native raid event."));
            Row(new ActionDef("Troll raid", "event foresttrolls", "Real native troll event."),
                new ActionDef("Skeleton raid", "event skeletons", "Real native skeleton event."));
            Row(new ActionDef("Blob raid", "event blobs", "Real native blob event."),
                new ActionDef("Wolf raid", "event wolves", "Real native wolf event."));
            Row(new ActionDef("Bat raid", "event bats", "Real native bat event."),
                new ActionDef("Surtling raid", "event surtling", "Real native surtling event."));
            Row(new ActionDef("Random eligible real raid", "randomevent", "Starts an eligible native event."),
                new ActionDef("Stop real raid", "stopevent", "Stops the event timer; already-spawned enemies may remain."));
            GUILayout.Label("Named event availability still depends on the installed Valheim version. A rejected native ID is reported directly in the console instead of being disguised as success.", warningStyle);
        }

        void Danger()
        {
            Heading("Destructive controls");
            GUILayout.Label("Every destructive operation requires a second click within five seconds. Commands still pass through Valheim's permission checks and the Lab's disposable-world guard.", warningStyle);
            DangerButton("Kill nearby enemies (Valheim killall)", "killall", "Vanilla command; does not mean players. Requires the game's normal permission/devcommand state.");
            DangerButton("Kill this tester", "killme", "Kills only the local player through Valheim's native console command, when allowed.");
            DangerButton("Clean up marked Lab content", "lwtest cleanup", "Removes marked loaded Lab objects and companions.");
            GUILayout.Label("Kill-all-players is deliberately not forged here: the current Lab forbids remote peers, and Valheim exposes no certified Living World server action for it. A future multiplayer-authority build must add an authenticated server RPC before that button can be enabled.", warningStyle);
        }

        void PlayerTools()
        {
            Heading("Player modes");
            GUILayout.Label("These use Valheim/Server Devcommands instead of permanent Harmony patches, so nothing scans or rewrites the player every frame.", statusStyle);
            Row(new ActionDef("God mode toggle", "god", "Toggle native god mode."),
                new ActionDef("Ghost mode toggle", "ghost", "Toggle native enemy-ignore mode."));
            Row(new ActionDef("Fly mode toggle", "fly", "Toggle native flight."),
                new ActionDef("No build cost toggle", "nocost", "Toggle native no-placement-cost mode."));
            Row(new ActionDef("Heal player", "heal", "Use the native heal command."),
                new ActionDef("Clear status / food", "puke", "Native food/status reset."));
            Heading("Player testing");
            DangerButton("Kill this tester", "killme", "Native player-death test with confirmation.");
            Row(new ActionDef("Raise run skill +10", "raiseskill Run 10", "Native skill command."),
                new ActionDef("Raise jump skill +10", "raiseskill Jump 10", "Native skill command."));
        }

        void ItemsAndSpawn()
        {
            Heading("Spawn any loaded prefab");
            GUILayout.Label("Unlike the removed Tooler, this does not crawl unfinished/internal item definitions at startup. It only resolves the prefab you request when you press Spawn.", statusStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab", GUILayout.Width(55)); spawnName = GUILayout.TextField(spawnName, GUILayout.Width(250));
            GUILayout.Label("Amount", GUILayout.Width(55)); spawnAmount = GUILayout.TextField(spawnAmount, GUILayout.Width(60));
            GUILayout.Label("Level", GUILayout.Width(40)); spawnLevel = GUILayout.TextField(spawnLevel, GUILayout.Width(60));
            if (GUILayout.Button("Spawn", GUILayout.Height(28))) Execute("spawn " + SafeToken(spawnName) + " " + SafeNumber(spawnAmount, "1") + " " + SafeNumber(spawnLevel, "1"));
            GUILayout.EndHorizontal();
            Heading("Item/resource giver");
            GUILayout.Label("Items are spawned at the tester through Valheim's authoritative prefab command; pick them up normally. Invalid names produce one console refusal—not hundreds of startup errors.", statusStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", GUILayout.Width(55)); itemName = GUILayout.TextField(itemName, GUILayout.Width(250));
            GUILayout.Label("Amount", GUILayout.Width(55)); itemAmount = GUILayout.TextField(itemAmount, GUILayout.Width(60));
            GUILayout.Label("Quality", GUILayout.Width(45)); itemQuality = GUILayout.TextField(itemQuality, GUILayout.Width(60));
            if (GUILayout.Button("Give / spawn", GUILayout.Height(28))) Execute("spawn " + SafeToken(itemName) + " " + SafeNumber(itemAmount, "1") + " " + SafeNumber(itemQuality, "1"));
            GUILayout.EndHorizontal();
            Row(new ActionDef("Wood ×50", "spawn Wood 50", "Common building resource."),
                new ActionDef("Stone ×50", "spawn Stone 50", "Common building resource."));
            Row(new ActionDef("Fine wood ×50", "spawn FineWood 50", "Common building resource."),
                new ActionDef("Core wood ×50", "spawn RoundLog 50", "Common building resource."));
            Row(new ActionDef("Food test bundle", "spawn CookedMeat 10", "Spawn ten cooked meats."),
                new ActionDef("Arrows ×100", "spawn ArrowWood 100", "Spawn basic arrows."));
        }

        void WorldTools()
        {
            Heading("Time and weather");
            Row(new ActionDef("Morning", "tod 0.25", "Set native time of day."), new ActionDef("Noon", "tod 0.5", "Set native time of day."));
            Row(new ActionDef("Evening", "tod 0.75", "Set native time of day."), new ActionDef("Reset time", "tod -1", "Return to normal time flow."));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Environment", GUILayout.Width(90)); environmentName = GUILayout.TextField(environmentName, GUILayout.Width(220));
            if (GUILayout.Button("Force environment", GUILayout.Height(28))) Execute("env " + SafeToken(environmentName));
            if (GUILayout.Button("Reset environment", GUILayout.Height(28))) Execute("resetenv");
            GUILayout.EndHorizontal();
            Row(new ActionDef("Skip one hour", "skiptime 3600", "Advance world time."),
                new ActionDef("Save world", "save", "Request a normal world save."));
            Heading("Location generation");
            DangerButton("Generate missing locations", "genloc", "Native world-generation maintenance command.");
        }

        void TerrainTools()
        {
            Heading("Terrain shaping");
            GUILayout.Label("These invoke Server Devcommands' maintained terrain commands. They run only on demand and do not install the old Tooler's persistent terrain hooks.", statusStyle);
            GUILayout.BeginHorizontal(); GUILayout.Label("Radius", GUILayout.Width(55)); terrainRadius = GUILayout.TextField(terrainRadius, GUILayout.Width(70)); GUILayout.EndHorizontal();
            Row(new ActionDef("Level ground", "terrain_level " + SafeNumber(terrainRadius, "8"), "Level nearby terrain."),
                new ActionDef("Smooth ground", "terrain_smooth " + SafeNumber(terrainRadius, "8"), "Smooth nearby terrain."));
            Row(new ActionDef("Raise ground", "terrain_raise " + SafeNumber(terrainRadius, "8"), "Raise nearby terrain."),
                new ActionDef("Lower ground", "terrain_lower " + SafeNumber(terrainRadius, "8"), "Lower nearby terrain."));
            DangerButton("Reset nearby terrain", "terrain_reset " + SafeNumber(terrainRadius, "8"), "Reset terrain modifications in the selected radius.");
            GUILayout.Label("The command and radius are shown on every button. If the installed Server Devcommands version renames a command, the console reports it without damaging terrain or flooding startup logs.", warningStyle);
        }

        void MapAndAdmin()
        {
            Heading("Map and travel");
            Row(new ActionDef("Explore entire map", "exploremap", "Native map reveal."),
                new ActionDef("Reset explored map", "resetmap", "Native map reset."));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Goto X Z", GUILayout.Width(70)); coordinates = GUILayout.TextField(coordinates, GUILayout.Width(220));
            if (GUILayout.Button("Teleport", GUILayout.Height(28))) Execute("goto " + SafeCoordinates(coordinates));
            if (GUILayout.Button("Show position", GUILayout.Height(28))) Execute("pos");
            GUILayout.EndHorizontal();
            Heading("World cleanup and creature controls");
            Row(new ActionDef("Tame nearby", "tame", "Native nearby tame operation."),
                new ActionDef("List players", "players", "Show connected-player information."));
            DangerButton("Kill nearby enemies", "killall", "Native enemy cleanup; confirmation required.");
            DangerButton("Remove ground drops", "removedrops", "Native dropped-item cleanup; confirmation required.");
            Heading("Diagnostics");
            Row(new ActionDef("Ping", "ping", "Show connection latency."),
                new ActionDef("Server information", "info", "Show current server/world information."));
        }

        static string SafeToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "INVALID";
            return value.Trim().Replace(" ", "").Replace(";", "").Replace("\n", "").Replace("\r", "");
        }

        static string SafeNumber(string value, string fallback)
        {
            int number; return Int32.TryParse(value, out number) ? number.ToString() : fallback;
        }

        static string SafeCoordinates(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return "0 0";
            return value.Trim().Replace(";", "").Replace("\n", " ").Replace("\r", " ");
        }

        void ConsolePage()
        {
            Heading("Command bridge");
            GUILayout.Label("This runs the exact text through Valheim's live console dispatcher, so newly added Living World commands can be used without rebuilding the Tooler.");
            GUI.SetNextControlName("LivingWorldCommandField");
            customCommand = GUILayout.TextField(customCommand ?? "", GUILayout.Height(30));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run command", GUILayout.Height(38))) Execute(customCommand);
            if (GUILayout.Button("Show all Living World commands", GUILayout.Height(38))) Execute("lwcommands");
            GUILayout.EndHorizontal();
            Heading("Examples");
            GUILayout.Label("lwtest status     |     lwtest party recover     |     lwtest wave status     |     event <event-id>", statusStyle);
        }

        void Heading(string text) { GUILayout.Space(8); GUILayout.Label(text, sectionStyle); }

        void Row(ActionDef left, ActionDef right)
        {
            GUILayout.BeginHorizontal(); DrawAction(left); DrawAction(right); GUILayout.EndHorizontal();
        }

        void DrawAction(ActionDef action)
        {
            GUILayout.BeginVertical(GUILayout.Width(355));
            if (GUILayout.Button(action.Label + "\n" + action.Command, buttonStyle)) Execute(action.Command);
            GUILayout.Label(action.Help, GUI.skin.label);
            GUILayout.EndVertical();
        }

        void DangerButton(string label, string command, string help)
        {
            bool armed = pendingDanger == command && Time.realtimeSinceStartup <= pendingUntil;
            string caption = armed ? "CONFIRM: " + label : label;
            if (GUILayout.Button(caption + "\n" + command, buttonStyle))
            {
                if (!armed) { pendingDanger = command; pendingUntil = Time.realtimeSinceStartup + 5f; lastAction = "Confirmation armed for five seconds: " + label; }
                else { pendingDanger = null; Execute(command); }
            }
            GUILayout.Label(help);
        }

        void Execute(string command)
        {
            if (String.IsNullOrWhiteSpace(command)) { lastAction = "No command entered."; return; }
            if (Console.instance == null) { lastAction = "Cannot run `" + command + "`: Valheim console dispatcher is not ready."; return; }
            try
            {
                Console.instance.TryRunCommand(command.Trim(), false, true);
                lastAction = "Sent live: " + command.Trim() + ". Check the top-left message, F5 console, and LogOutput.log for the authoritative result.";
                Logger.LogInfo("Tooler sent command: " + command.Trim());
            }
            catch (Exception error)
            {
                lastAction = "Command failed before dispatch: " + error.Message;
                Logger.LogError(error);
            }
        }

        sealed class ActionDef
        {
            public readonly string Label, Command, Help;
            public ActionDef(string label, string command, string help) { Label = label; Command = command; Help = help; }
        }
    }
}


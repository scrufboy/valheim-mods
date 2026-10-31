using System;
using UnityEngine;

namespace Scrufboy.LivingWorld.Tooler
{
    internal static class LivingWorldPanel
    {
        // Tooler 2.6.1: 14 Living World pages -> 7. Every control appears exactly once; destructive
        // actions stay on their own page behind the two-click confirmation. The four original
        // Tooler pages (Player, Entities, Terrain Shaper, Misc) are untouched.
        private static readonly ToolerPage[] Pages = LivingWorldToolerModules.Create(
            Companions,
            Raids,
            PlayerTools,
            Spawn,
            WorldTools,
            BuildTools,
            Admin);

        private static Vector2 scroll;
        private static int tab;
        private static string lastAction = "Ready. Choose an action.";
        private static string customCommand = "lwstatus";
        private static string spawnName = "Greydwarf";
        private static string spawnAmount = "1";
        private static string spawnLevel = "1";
        private static string itemName = "Wood";
        private static string itemAmount = "50";
        private static string itemQuality = "1";
        private static string coordinates = "0 0";
        private static string environmentName = "Clear";
        private static string terrainRadius = "8";
        private static string pendingDanger;
        private static float pendingUntil;
        private static GUIStyle sectionStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle statusStyle;
        private static GUIStyle warningStyle;

        internal static void SetStatus(string text)
        {
            lastAction = text ?? String.Empty;
        }

        public static void DisplayGUI()
        {
            EnsureStyles();
            DrawEnvironment();
            DrawTabRow(0, Pages.Length);

            float availableHeight = Mathf.Clamp(Screen.height - 340f, 280f, 560f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(availableHeight));
            Pages[Mathf.Clamp(tab, 0, Pages.Length - 1)].Draw();
            GUILayout.EndScrollView();

            GUILayout.Space(4);
            GUILayout.Label(lastAction, statusStyle, GUILayout.MinHeight(48));
        }

        private static void EnsureStyles()
        {
            if (sectionStyle != null)
            {
                return;
            }

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold
            };
            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                fixedHeight = 42
            };
            statusStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                padding = new RectOffset(10, 10, 8, 8)
            };
            warningStyle = new GUIStyle(statusStyle);
            warningStyle.normal.textColor = new Color(1f, 0.75f, 0.25f);
        }

        private static void DrawTabRow(int first, int end)
        {
            GUILayout.BeginHorizontal();
            for (int index = first; index < end; index++)
            {
                if (GUILayout.Toggle(tab == index, Pages[index].Label, GUI.skin.button, GUILayout.MinWidth(96)))
                {
                    tab = index;
                }
            }
            GUILayout.EndHorizontal();
        }

        private static void DrawEnvironment()
        {
            bool inWorld = ZNet.instance != null && Player.m_localPlayer != null;
            string mode = !inWorld
                ? "Not in a world"
                : (ZNet.instance.IsServer()
                    ? (ZNet.IsOpenServer() ? "Server/open world" : "Local/server authority")
                    : "Connected client");
            int peers = ZNet.instance == null ? 0 : ZNet.instance.GetPeers().Count;
            GUILayout.Label("State: " + mode + "  |  peers: " + peers + "  |  command target: local game console", inWorld ? statusStyle : warningStyle);
        }

        // ---------------- 1. Companions ----------------
        private static void Companions()
        {
            Heading("Party");
            Row(new ActionDef("Create companion pair", "lwparty", "Creates your two companions if you have none loaded."),
                new ActionDef("Status", "lwstatus", "Each companion's phase, reason and who owns movement, hands, look and posture."));
            Row(new ActionDef("Replace pair", "lwparty new", "Replaces your loaded companions with a fresh pair."),
                new ActionDef("Heal companions", "lwheal", "Full health; cold, poison, burning and other effects cleared."));
            Row(new ActionDef("Bring companions to me", "lwsummon", "Places loaded companions beside you."),
                new ActionDef("Go to my companion", "lwgoto", "Teleports you to your nearest loaded companion."));
            DangerButton("Remove ALL companions", "lwparty clean", "Removes every companion record in this world, loaded or not. For clean test starts.");
            Heading("Orders");
            GUILayout.Label("Follow / hold / wait orders arrive with the companion orders module; buttons will appear here.", statusStyle);
        }

        // ---------------- 2. Raids ----------------
        private static void Raids()
        {
            Heading("Current FormidableAI training raids");
            Row(new ActionDef("Formidable Arena", "event lwc_formidable_arena", "Starts the current Elder-pattern mixed Souls raid at the configured long-range ring."),
                new ActionDef("Formidable Souls Swamp", "event lwc_formidable_souls_swamp", "Starts the current Elder-pattern swamp Souls raid at the configured long-range ring."));
            Row(new ActionDef("STOP TRAINING RAID", "stopevent", "Stops the active event timer; enemies already spawned may remain."),
                new ActionDef("Noon", "tod 0.5", "Sets daylight for the standard awareness and initiative stress run."));
            Heading("Vanilla raids (biome order)");
            Row(new ActionDef("Eikthyr army", "event army_eikthyr", "Meadows."),
                new ActionDef("Elder army", "event army_theelder", "Black Forest."));
            Row(new ActionDef("Bonemass army", "event army_bonemass", "Swamp."),
                new ActionDef("Moder army", "event army_moder", "Mountains (Drakes)."));
            Row(new ActionDef("Fuling army", "event army_goblin", "Plains."),
                new ActionDef("Seeker army", "event army_seekers", "Mistlands."));
            Row(new ActionDef("Gjall army", "event army_gjall", "Mistlands."),
                new ActionDef("Charred army", "event army_charred", "Ashlands."));
            Row(new ActionDef("Troll raid", "event foresttrolls", "Native troll event."),
                new ActionDef("Skeleton raid", "event skeletons", "Native skeleton event."));
            Row(new ActionDef("Blob raid", "event blobs", "Native blob event."),
                new ActionDef("Wolf raid", "event wolves", "Native wolf event."));
            Row(new ActionDef("Bat raid", "event bats", "Native bat event."),
                new ActionDef("Surtling raid", "event surtling", "Native surtling event."));
            Row(new ActionDef("Random eligible raid", "randomevent", "Starts an eligible native event."),
                new ActionDef("STOP RAID", "stopevent", "Stops the event timer; enemies already spawned may remain."));
            Heading("Souls of the Land matchups");
            Row(new ActionDef("Pillagers - 2 to 3", "event lwc_souls_pillagers", "Bounded RandomEvent with RRRN_Pillager. Needs the Souls/RRRCore stack."),
                new ActionDef("Krauguk Legion - 2", "event lwc_krauguk_legion", "Bounded RandomEvent with two RRRN_OrcBrute. Needs the Souls/RRRCore stack."));
        }

        // ---------------- 3. Player ----------------
        private static void PlayerTools()
        {
            Heading("Modes");
            Row(new ActionDef("God mode", "god", "Toggle native god mode."),
                new ActionDef("Ghost mode", "ghost", "Toggle: enemies ignore you."));
            Row(new ActionDef("Fly", "fly", "Toggle native flight."),
                new ActionDef("No build cost", "nocost", "Toggle no placement cost."));
            Heading("Body");
            Row(new ActionDef("Heal me", "heal", "Native heal."),
                new ActionDef("Clear food and status", "puke", "Native food and status reset."));
            Row(new ActionDef("Run skill +10", "raiseskill Run 10", "Native skill command."),
                new ActionDef("Jump skill +10", "raiseskill Jump 10", "Native skill command."));
            DangerButton("Kill me", "killme", "Native player-death test.");
        }

        // ---------------- 4. Spawn ----------------
        private static void Spawn()
        {
            Heading("Spawn creature or prefab");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Prefab", GUILayout.Width(55));
            spawnName = GUILayout.TextField(spawnName, GUILayout.Width(250));
            GUILayout.Label("Amount", GUILayout.Width(55));
            spawnAmount = GUILayout.TextField(spawnAmount, GUILayout.Width(60));
            GUILayout.Label("Level", GUILayout.Width(40));
            spawnLevel = GUILayout.TextField(spawnLevel, GUILayout.Width(60));
            if (GUILayout.Button("Spawn", GUILayout.Height(28)))
            {
                Execute("spawn " + SafeToken(spawnName) + " " + SafeNumber(spawnAmount, "1") + " " + SafeNumber(spawnLevel, "1"));
            }
            GUILayout.EndHorizontal();

            Heading("Give item");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", GUILayout.Width(55));
            itemName = GUILayout.TextField(itemName, GUILayout.Width(250));
            GUILayout.Label("Amount", GUILayout.Width(55));
            itemAmount = GUILayout.TextField(itemAmount, GUILayout.Width(60));
            GUILayout.Label("Quality", GUILayout.Width(45));
            itemQuality = GUILayout.TextField(itemQuality, GUILayout.Width(60));
            if (GUILayout.Button("Give", GUILayout.Height(28)))
            {
                Execute("spawn " + SafeToken(itemName) + " " + SafeNumber(itemAmount, "1") + " " + SafeNumber(itemQuality, "1"));
            }
            GUILayout.EndHorizontal();
            Row(new ActionDef("Wood x50", "spawn Wood 50", "Building resource."),
                new ActionDef("Stone x50", "spawn Stone 50", "Building resource."));
            Row(new ActionDef("Fine wood x50", "spawn FineWood 50", "Building resource."),
                new ActionDef("Core wood x50", "spawn RoundLog 50", "Building resource."));
            Row(new ActionDef("Cooked meat x10", "spawn CookedMeat 10", "Food."),
                new ActionDef("Wood arrows x100", "spawn ArrowWood 100", "Ammunition."));
        }

        // ---------------- 5. World ----------------
        private static void WorldTools()
        {
            Heading("Time");
            Row(new ActionDef("Midnight", "tod 0", "Set time to midnight."),
                new ActionDef("Morning", "tod 0.25", "Set time to morning."));
            Row(new ActionDef("Noon", "tod 0.5", "Set time to noon."),
                new ActionDef("Evening", "tod 0.75", "Set time to evening."));
            Row(new ActionDef("Normal time flow", "tod -1", "Return to normal time flow."),
                new ActionDef("Skip one hour", "skiptime 3600", "Advance world time by one hour."));
            Heading("Weather");
            Row(new ActionDef("Clear", "env Clear", "Force clear weather."),
                new ActionDef("Rain", "env Rain", "Force rain."));
            Row(new ActionDef("Thunderstorm", "env ThunderStorm", "Force a thunderstorm."),
                new ActionDef("Fog", "env Fog", "Force fog."));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Other", GUILayout.Width(55));
            environmentName = GUILayout.TextField(environmentName, GUILayout.Width(220));
            if (GUILayout.Button("Force", GUILayout.Height(28))) Execute("env " + SafeToken(environmentName));
            if (GUILayout.Button("Back to normal weather", GUILayout.Height(28))) Execute("resetenv");
            GUILayout.EndHorizontal();
            Heading("Map and travel");
            Row(new ActionDef("Reveal map", "exploremap", "Native map reveal."),
                new ActionDef("Reset explored map", "resetmap", "Native map reset."));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Goto X Z", GUILayout.Width(70));
            coordinates = GUILayout.TextField(coordinates, GUILayout.Width(220));
            if (GUILayout.Button("Teleport", GUILayout.Height(28))) Execute("goto " + SafeCoordinates(coordinates));
            if (GUILayout.Button("Show position", GUILayout.Height(28))) Execute("pos");
            GUILayout.EndHorizontal();
            Heading("World file");
            Row(new ActionDef("Save world", "save", "Request a normal world save."),
                new ActionDef("Skip to morning", "tod 0.3", "Jump to early morning."));
            DangerButton("Generate missing locations", "genloc", "Native world-generation maintenance.");
        }

        // ---------------- 6. Build ----------------
        private static void BuildTools()
        {
            Heading("Terrain around you");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Radius", GUILayout.Width(55));
            terrainRadius = GUILayout.TextField(terrainRadius, GUILayout.Width(70));
            GUILayout.EndHorizontal();
            Row(new ActionDef("Level ground", "terrain_level " + SafeNumber(terrainRadius, "8"), "Level nearby terrain."),
                new ActionDef("Smooth ground", "terrain_smooth " + SafeNumber(terrainRadius, "8"), "Smooth nearby terrain."));
            Row(new ActionDef("Raise ground", "terrain_raise " + SafeNumber(terrainRadius, "8"), "Raise nearby terrain."),
                new ActionDef("Lower ground", "terrain_lower " + SafeNumber(terrainRadius, "8"), "Lower nearby terrain."));
            DangerButton("Reset nearby terrain", "terrain_reset " + SafeNumber(terrainRadius, "8"), "Undo terrain changes in the radius.");
            Heading("Site Survey");
            SiteSurveyPanel.DisplayGUI();
        }

        // ---------------- 7. Admin ----------------
        private static void Admin()
        {
            Heading("Server");
            Row(new ActionDef("List players", "players", "Connected-player information."),
                new ActionDef("Server information", "info", "Current server and world information."));
            Row(new ActionDef("Ping", "ping", "Connection latency."),
                new ActionDef("Tame nearby", "tame", "Native nearby tame."));
            Heading("Cleanup");
            DangerButton("Kill nearby enemies", "killall", "Native enemy cleanup.");
            DangerButton("Remove ground drops", "removedrops", "Native dropped-item cleanup.");
            Heading("Console");
            GUILayout.Label("Runs any text through Valheim's console, so new commands work without rebuilding this panel.", statusStyle);
            GUI.SetNextControlName("LivingWorldToolerCommand");
            customCommand = GUILayout.TextField(customCommand ?? String.Empty, GUILayout.Height(30));
            if (GUILayout.Button("Run command", GUILayout.Height(38))) Execute(customCommand);
            GUILayout.Label("Companion commands: lwparty | lwparty new | lwparty clean | lwstatus | lwheal | lwsummon | lwgoto", statusStyle);
        }

        private static void Heading(string text)
        {
            GUILayout.Space(8);
            GUILayout.Label(text, sectionStyle);
        }

        private static void Row(ActionDef left, ActionDef right)
        {
            GUILayout.BeginHorizontal();
            DrawAction(left);
            DrawAction(right);
            GUILayout.EndHorizontal();
        }

        private static void DrawAction(ActionDef action)
        {
            GUILayout.BeginVertical(GUILayout.Width(355));
            if (GUILayout.Button(action.Label + "\n" + action.Command, buttonStyle))
            {
                Execute(action.Command);
            }
            GUILayout.Label(action.Help, GUI.skin.label);
            GUILayout.EndVertical();
        }

        private static void DangerButton(string label, string command, string help)
        {
            bool armed = pendingDanger == command && Time.realtimeSinceStartup <= pendingUntil;
            string caption = armed ? "CONFIRM: " + label : label;
            if (GUILayout.Button(caption + "\n" + command, buttonStyle))
            {
                if (!armed)
                {
                    pendingDanger = command;
                    pendingUntil = Time.realtimeSinceStartup + 5f;
                    lastAction = "Confirmation armed for five seconds: " + label;
                }
                else
                {
                    pendingDanger = null;
                    Execute(command);
                }
            }
            GUILayout.Label(help);
        }

        private static void Execute(string command)
        {
            command = (command ?? String.Empty).Trim();
            if (command.Length == 0)
            {
                lastAction = "No command entered.";
                return;
            }
            if (global::Console.instance == null)
            {
                lastAction = "Cannot run `" + command + "`: Valheim's command dispatcher is not ready.";
                return;
            }

            try
            {
                global::Console.instance.TryRunCommand(command, false, true);
                lastAction = "Sent live: " + command + ". Check the top-left message, F5 console, and LogOutput.log for the authoritative result.";
            }
            catch (Exception error)
            {
                lastAction = "Command failed before dispatch: " + error.Message;
            }
        }

        private static string SafeToken(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "INVALID";
            }
            return value.Trim().Replace(" ", "").Replace(";", "").Replace("\n", "").Replace("\r", "");
        }

        private static string SafeNumber(string value, string fallback)
        {
            int number;
            return Int32.TryParse(value, out number) ? number.ToString() : fallback;
        }

        private static string SafeCoordinates(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                return "0 0";
            }
            return value.Trim().Replace(";", "").Replace("\n", " ").Replace("\r", " ");
        }

        private sealed class ActionDef
        {
            public readonly string Label;
            public readonly string Command;
            public readonly string Help;

            public ActionDef(string label, string command, string help)
            {
                Label = label;
                Command = command;
                Help = help;
            }
        }
    }
}

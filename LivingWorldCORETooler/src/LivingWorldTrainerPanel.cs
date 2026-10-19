using System;
using UnityEngine;

namespace Scrufboy.LivingWorld.Tooler
{
    internal static class LivingWorldPanel
    {
        private static readonly string[] Tabs =
        {
            "Overview", "Lifecycle", "Community", "Combat", "Party", "Raids", "Player",
            "Items / Spawn", "World", "Terrain", "Map / Admin", "Danger", "Console"
        };

        private static Vector2 scroll;
        private static int tab;
        private static string lastAction = "Ready. Choose an action.";
        private static string customCommand = "lwtest status";
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

        public static void DisplayGUI()
        {
            EnsureStyles();
            DrawEnvironment();
            DrawTabRow(0, 7);
            DrawTabRow(7, Tabs.Length);

            float availableHeight = Mathf.Clamp(Screen.height - 340f, 280f, 560f);
            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(availableHeight));
            switch (tab)
            {
                case 0: Overview(); break;
                case 1: Lifecycle(); break;
                case 2: Community(); break;
                case 3: Combat(); break;
                case 4: Party(); break;
                case 5: Raids(); break;
                case 6: PlayerTools(); break;
                case 7: ItemsAndSpawn(); break;
                case 8: WorldTools(); break;
                case 9: TerrainTools(); break;
                case 10: MapAndAdmin(); break;
                case 11: Danger(); break;
                default: ConsolePage(); break;
            }
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
                if (GUILayout.Toggle(tab == index, Tabs[index], GUI.skin.button, GUILayout.MinWidth(96)))
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

        private static void Overview()
        {
            Heading("Fast test loop");
            Row(new ActionDef("Prepare / recover party", "lwparty", "Creates or recovers the ready party and starts follow."),
                new ActionDef("Party status", "lwtest party status", "Shows party, cleric, and encounter state."));
            Row(new ActionDef("Bring party to me", "lwtest party recover", "Recovers the saved party beside the tester."),
                new ActionDef("Locate party", "lwtest party locate", "Reports where saved party members are."));
            Row(new ActionDef("Stop measured test", "lwtest stop", "Ends the single-target measurement."),
                new ActionDef("Stop raid / wave", "lwtest wave stop", "Ends the active specialized wave."));
            Heading("Full trainer surface");
            GUILayout.Label("The original Player, Entities, Terrain Shaper, and Misc pages remain intact above. These Living World pages restore the additional lifecycle, raid, player, spawn, time, environment, terrain, map, administration, danger-confirmation, and command controls.", statusStyle);
        }

        private static void Lifecycle()
        {
            Heading("World and NPC lifecycle");
            Row(new ActionDef("Build lab once", "lwtest setup", "Builds native Lab pieces; never duplicates a marked Lab."),
                new ActionDef("Spawn scout", "lwtest spawn", "Creates a replacement only when no living loaded scout is available."));
            Row(new ActionDef("Status / diagnostics", "lwtest status", "Shows loaded NPC, ownership, behavior, and current test state."),
                new ActionDef("Show commands", "lwcommands", "Prints the Human NPC build's authoritative command list."));
            DangerButton("Advanced: migrate legacy scout", "lwtest migrate", "Compatibility-only operation; not part of an ordinary test loop.");
            DangerButton("Clean up marked Lab objects", "lwtest cleanup", "Removes loaded marked Lab objects and companions. This is intentionally two-step.");
            GUILayout.Label("If a companion is merely unloaded or lost, use Locate and Recover before Spawn so an unintended replacement is not created.", warningStyle);
        }

        private static void Community()
        {
            Heading("Community living / casual behavior");
            Row(new ActionDef("Follow tester", "lwtest follow", "Saves the follow order using stable leader identity."),
                new ActionDef("Hold here", "lwtest hold", "Saves hold position; nearby defense remains enabled."));
            Row(new ActionDef("Movement check", "lwtest move", "Runs the measured navigation check."),
                new ActionDef("Stop current check", "lwtest stop", "Stops the current movement or combat measurement."));
            Row(new ActionDef("Party ready + follow", "lwparty", "One-button repeatable party preparation."),
                new ActionDef("Recover to tester", "lwtest party recover", "Returns or replaces the ready party through the Lab transaction."));
        }

        private static void Combat()
        {
            Heading("Measured combat");
            Row(new ActionDef("Single-enemy combat", "lwtest combat", "Runs the measured combat test."),
                new ActionDef("Combat status", "lwtest status", "Shows target, damage, motion, and current result."));
            Row(new ActionDef("Stop single test", "lwtest stop", "Safely ends the measured check."),
                new ActionDef("Prepare full party", "lwparty", "Ensures the party is available before a wave."));
        }

        private static void Party()
        {
            Heading("Party control and recovery");
            Row(new ActionDef("Prepare", "lwtest party prepare", "Creates or recovers the party."),
                new ActionDef("Status", "lwtest party status", "Shows party and encounter state."));
            Row(new ActionDef("Locate", "lwtest party locate", "Reports the party location."),
                new ActionDef("Recover / teleport to me", "lwtest party recover", "Uses the official party recovery transaction."));
            Row(new ActionDef("One-button party", "lwparty", "Prepare, recover, and follow."),
                new ActionDef("Follow", "lwtest follow", "Orders the loaded party to follow."));
            Row(new ActionDef("Hold", "lwtest hold", "Orders the loaded party to hold."),
                new ActionDef("All Living World commands", "lwcommands", "Prints the current command catalog."));
        }

        private static void Raids()
        {
            Heading("Specialized Living World waves");
            Row(new ActionDef("Arm Boar + Neck", "lwtest wave boarneck", "Arms the close-range mixed wave; does not start it."),
                new ActionDef("Arm Greydwarf", "lwtest wave greydwarf", "Arms the greydwarf wave; does not start it."));
            Row(new ActionDef("GO — 10 second countdown", "lwtest wave go", "Starts the armed wave after the built-in countdown."),
                new ActionDef("Wave status", "lwtest wave status", "Reports wave and party state."));
            DangerButton("Stop active wave", "lwtest wave stop", "Stops the specialized wave. Use this instead of cleanup.");

            Heading("Vanilla raid console");
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
            Row(new ActionDef("Random eligible raid", "randomevent", "Starts an eligible native event."),
                new ActionDef("Stop real raid", "stopevent", "Stops the event timer; existing enemies may remain."));
        }

        private static void PlayerTools()
        {
            Heading("Player modes");
            Row(new ActionDef("God mode toggle", "god", "Toggle native god mode."),
                new ActionDef("Ghost mode toggle", "ghost", "Toggle native enemy-ignore mode."));
            Row(new ActionDef("Fly mode toggle", "fly", "Toggle native flight."),
                new ActionDef("No build cost toggle", "nocost", "Toggle native no-placement-cost mode."));
            Row(new ActionDef("Heal player", "heal", "Use the native heal command."),
                new ActionDef("Clear status / food", "puke", "Native food and status reset."));
            Heading("Player testing");
            Row(new ActionDef("Raise run skill +10", "raiseskill Run 10", "Native skill command."),
                new ActionDef("Raise jump skill +10", "raiseskill Jump 10", "Native skill command."));
            DangerButton("Kill this tester", "killme", "Native player-death test with confirmation.");
            GUILayout.Label("Inventory weight, stamina, durability, placement-cost repair, teleport, and related persistent toggles remain on the original Player page above; they were not replaced by these shortcuts.", statusStyle);
        }

        private static void ItemsAndSpawn()
        {
            Heading("Spawn any loaded prefab");
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

            Heading("Item / resource giver");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Item", GUILayout.Width(55));
            itemName = GUILayout.TextField(itemName, GUILayout.Width(250));
            GUILayout.Label("Amount", GUILayout.Width(55));
            itemAmount = GUILayout.TextField(itemAmount, GUILayout.Width(60));
            GUILayout.Label("Quality", GUILayout.Width(45));
            itemQuality = GUILayout.TextField(itemQuality, GUILayout.Width(60));
            if (GUILayout.Button("Give / spawn", GUILayout.Height(28)))
            {
                Execute("spawn " + SafeToken(itemName) + " " + SafeNumber(itemAmount, "1") + " " + SafeNumber(itemQuality, "1"));
            }
            GUILayout.EndHorizontal();

            Row(new ActionDef("Wood ×50", "spawn Wood 50", "Common building resource."),
                new ActionDef("Stone ×50", "spawn Stone 50", "Common building resource."));
            Row(new ActionDef("Fine wood ×50", "spawn FineWood 50", "Common building resource."),
                new ActionDef("Core wood ×50", "spawn RoundLog 50", "Common building resource."));
            Row(new ActionDef("Food test bundle", "spawn CookedMeat 10", "Spawn ten cooked meats."),
                new ActionDef("Arrows ×100", "spawn ArrowWood 100", "Spawn basic arrows."));
            GUILayout.Label("The original Entities page above also retains its searchable valid item and prefab catalogs.", statusStyle);
        }

        private static void WorldTools()
        {
            Heading("Time of day");
            Row(new ActionDef("Midnight", "tod 0", "Set native time to midnight."),
                new ActionDef("Morning", "tod 0.25", "Set native time to morning."));
            Row(new ActionDef("Noon", "tod 0.5", "Set native time to noon."),
                new ActionDef("Evening", "tod 0.75", "Set native time to evening."));
            Row(new ActionDef("Reset time", "tod -1", "Return to normal time flow."),
                new ActionDef("Skip one hour", "skiptime 3600", "Advance world time by one hour."));

            Heading("Environment / weather");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Environment", GUILayout.Width(90));
            environmentName = GUILayout.TextField(environmentName, GUILayout.Width(220));
            if (GUILayout.Button("Force environment", GUILayout.Height(28)))
            {
                Execute("env " + SafeToken(environmentName));
            }
            if (GUILayout.Button("Reset environment", GUILayout.Height(28)))
            {
                Execute("resetenv");
            }
            GUILayout.EndHorizontal();
            Row(new ActionDef("Clear", "env Clear", "Force clear weather."),
                new ActionDef("Rain", "env Rain", "Force rain."));
            Row(new ActionDef("Thunderstorm", "env ThunderStorm", "Force a thunderstorm."),
                new ActionDef("Fog", "env Fog", "Force fog."));
            Row(new ActionDef("Reset environment", "resetenv", "Return weather to the world cycle."),
                new ActionDef("Save world", "save", "Request a normal world save."));

            Heading("Location generation");
            DangerButton("Generate missing locations", "genloc", "Native world-generation maintenance command.");
        }

        private static void TerrainTools()
        {
            Heading("Terrain shaping");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Radius", GUILayout.Width(55));
            terrainRadius = GUILayout.TextField(terrainRadius, GUILayout.Width(70));
            GUILayout.EndHorizontal();
            Row(new ActionDef("Level ground", "terrain_level " + SafeNumber(terrainRadius, "8"), "Level nearby terrain."),
                new ActionDef("Smooth ground", "terrain_smooth " + SafeNumber(terrainRadius, "8"), "Smooth nearby terrain."));
            Row(new ActionDef("Raise ground", "terrain_raise " + SafeNumber(terrainRadius, "8"), "Raise nearby terrain."),
                new ActionDef("Lower ground", "terrain_lower " + SafeNumber(terrainRadius, "8"), "Lower nearby terrain."));
            DangerButton("Reset nearby terrain", "terrain_reset " + SafeNumber(terrainRadius, "8"), "Reset terrain modifications in the selected radius.");
            GUILayout.Label("The original Terrain Shaper page remains available above for its native Tooler controls.", statusStyle);
        }

        private static void MapAndAdmin()
        {
            Heading("Map and travel");
            Row(new ActionDef("Explore entire map", "exploremap", "Native map reveal."),
                new ActionDef("Reset explored map", "resetmap", "Native map reset."));
            GUILayout.BeginHorizontal();
            GUILayout.Label("Goto X Z", GUILayout.Width(70));
            coordinates = GUILayout.TextField(coordinates, GUILayout.Width(220));
            if (GUILayout.Button("Teleport", GUILayout.Height(28)))
            {
                Execute("goto " + SafeCoordinates(coordinates));
            }
            if (GUILayout.Button("Show position", GUILayout.Height(28)))
            {
                Execute("pos");
            }
            GUILayout.EndHorizontal();

            Heading("World cleanup and creature controls");
            Row(new ActionDef("Tame nearby", "tame", "Native nearby tame operation."),
                new ActionDef("List players", "players", "Show connected-player information."));
            DangerButton("Kill nearby enemies", "killall", "Native enemy cleanup; confirmation required.");
            DangerButton("Remove ground drops", "removedrops", "Native dropped-item cleanup; confirmation required.");

            Heading("Diagnostics");
            Row(new ActionDef("Ping", "ping", "Show connection latency."),
                new ActionDef("Server information", "info", "Show current server and world information."));
        }

        private static void Danger()
        {
            Heading("Destructive controls");
            GUILayout.Label("Every destructive operation requires a second click within five seconds. Commands still pass through Valheim's permission checks and Living World's own safeguards.", warningStyle);
            DangerButton("Kill nearby enemies", "killall", "Native nearby enemy cleanup.");
            DangerButton("Kill this tester", "killme", "Kills only the local player when allowed.");
            DangerButton("Clean up marked Lab content", "lwtest cleanup", "Removes marked loaded Lab objects and companions.");
            DangerButton("Remove ground drops", "removedrops", "Removes loose dropped items through the native command.");
        }

        private static void ConsolePage()
        {
            Heading("Command bridge");
            GUILayout.Label("Runs the exact text through Valheim's live console dispatcher so newly added Living World or installed dev commands can be used without rebuilding this interface.", statusStyle);
            GUI.SetNextControlName("LivingWorldToolerCommand");
            customCommand = GUILayout.TextField(customCommand ?? String.Empty, GUILayout.Height(30));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run command", GUILayout.Height(38)))
            {
                Execute(customCommand);
            }
            if (GUILayout.Button("Show all Living World commands", GUILayout.Height(38)))
            {
                Execute("lwcommands");
            }
            GUILayout.EndHorizontal();
            Heading("Examples");
            GUILayout.Label("lwtest status  |  lwtest party recover  |  lwtest wave status  |  event <event-id>  |  tod 0  |  env Rain", statusStyle);
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

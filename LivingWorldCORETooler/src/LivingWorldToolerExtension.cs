using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using ValheimTooler;
using ValheimTooler.Core;
using ValheimTooler.Utils;

[assembly: AssemblyTitle("Living World C.O.R.E. Tooler")]
[assembly: AssemblyDescription("Original ValheimTooler interface with Living World integration")]
[assembly: AssemblyCompany("Brian / scrufboy")]
[assembly: AssemblyProduct("Living World C.O.R.E.")]
[assembly: AssemblyCopyright("Living World integration Copyright Brian / scrufboy")]
[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

namespace Scrufboy.LivingWorld.Tooler
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.github.Astropilot.ValheimTooler", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class LivingWorldToolerExtension : BaseUnityPlugin
    {
        public const string PluginGuid = "com.scrufboy.livingworld.tooler";
        public const string PluginName = "Brian's Living World C.O.R.E. Tooler";
        public const string PluginVersion = "2.1.0";

        private Harmony harmony;

        private void Awake()
        {
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded into the original ValheimTooler interface.");
        }

        private void OnDestroy()
        {
            if (harmony != null)
            {
                harmony.UnpatchSelf();
            }
        }
    }

    internal static class OriginalWindowExtension
    {
        private static readonly string[] ToolbarChoices =
        {
            "$vt_toolbar_player",
            "$vt_toolbar_entities",
            "$vt_toolbar_terrain_shaper",
            "$vt_toolbar_misc",
            "Living World"
        };

        private static int selectedTab;

        [HarmonyPatch(typeof(EntryPoint), "ValheimToolerWindow")]
        private static class EntryPointWindowPatch
        {
            private static bool Prefix(int windowID)
            {
                GUILayout.Space(10);
                selectedTab = GUILayout.Toolbar(
                    selectedTab,
                    ToolbarChoices.Select(LocalizeToolbarLabel).ToArray());

                switch (selectedTab)
                {
                    case 0:
                        PlayerHacks.DisplayGUI();
                        break;
                    case 1:
                        EntitiesItemsHacks.DisplayGUI();
                        break;
                    case 2:
                        TerrainShaper.DisplayGUI();
                        break;
                    case 3:
                        MiscHacks.DisplayGUI();
                        break;
                    case 4:
                        LivingWorldPanel.DisplayGUI();
                        break;
                }

                GUI.DragWindow();
                return false;
            }

            private static string LocalizeToolbarLabel(string label)
            {
                return label.Length > 0 && label[0] == '$'
                    ? VTLocalization.instance.Localize(label)
                    : label;
            }
        }

        // Valheim 1.0 exposes unfinished/internal ObjectDB entries whose sprites cannot
        // be rendered. The compatibility fork correctly skips them, but reporting every
        // rejected entry floods the log. Keep the catalog and skip behavior; remove only
        // the per-entry informational spam.
        [HarmonyPatch(typeof(ItemGiver), "Start")]
        private static class QuietRejectedCatalogEntriesPatch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    MethodInfo method = instruction.operand as MethodInfo;
                    if (method != null && method.DeclaringType != null && method.DeclaringType.Name == "ZLog" && method.Name == "Log")
                    {
                        instruction.opcode = OpCodes.Pop;
                        instruction.operand = null;
                    }

                    yield return instruction;
                }
            }
        }
    }

    internal static class LivingWorldPanel
    {
        private static readonly string[] Presets =
        {
            "lwparty",
            "lwtest party status",
            "lwtest party locate",
            "lwtest party recover",
            "lwtest follow",
            "lwtest hold",
            "lwtest status",
            "lwcommands",
            "lwtest wave boarneck",
            "lwtest wave greydwarf",
            "lwtest wave go",
            "lwtest wave status",
            "lwtest wave stop"
        };

        private static int presetIndex;
        private static string command = Presets[0];
        private static string status = "Ready.";

        public static void DisplayGUI()
        {
            GUILayout.BeginHorizontal();
            DrawPartyColumn();
            DrawCombatColumn();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical("Living World command", GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Space(EntryPoint.s_boxSpacing);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Preset", GUILayout.ExpandWidth(false));
            int newIndex = GUILayout.SelectionGrid(presetIndex, Presets, 3, GUI.skin.button);
            if (newIndex != presetIndex)
            {
                presetIndex = newIndex;
                command = Presets[presetIndex];
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUI.SetNextControlName("LivingWorldToolerCommand");
            command = GUILayout.TextField(command ?? String.Empty, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Run", GUILayout.Width(90)))
            {
                Run(command);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label(status);
            GUILayout.EndVertical();
        }

        private static void DrawPartyColumn()
        {
            GUILayout.BeginVertical("Companion party", GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Space(EntryPoint.s_boxSpacing);
            Button("Create / recover full party", "lwparty");
            Button("Party status", "lwtest party status");
            Button("Locate party", "lwtest party locate");
            Button("Bring party to me", "lwtest party recover");
            Button("Follow", "lwtest follow");
            Button("Hold", "lwtest hold");
            GUILayout.EndVertical();
        }

        private static void DrawCombatColumn()
        {
            GUILayout.BeginVertical("Combat tests", GUI.skin.box, GUILayout.ExpandWidth(true));
            GUILayout.Space(EntryPoint.s_boxSpacing);
            Button("Arm Boar + Neck wave", "lwtest wave boarneck");
            Button("Arm Greydwarf wave", "lwtest wave greydwarf");
            Button("Start armed wave", "lwtest wave go");
            Button("Wave status", "lwtest wave status");
            Button("Stop wave", "lwtest wave stop");
            Button("All Living World commands", "lwcommands");
            GUILayout.EndVertical();
        }

        private static void Button(string label, string text)
        {
            if (GUILayout.Button(label))
            {
                command = text;
                presetIndex = Array.IndexOf(Presets, text);
                if (presetIndex < 0)
                {
                    presetIndex = 0;
                }
                Run(text);
            }
        }

        private static void Run(string text)
        {
            text = (text ?? String.Empty).Trim();
            if (text.Length == 0)
            {
                status = "No command entered.";
                return;
            }

            if (global::Console.instance == null)
            {
                status = "Valheim's command dispatcher is not ready.";
                return;
            }

            try
            {
                global::Console.instance.TryRunCommand(text, false, true);
                status = "Sent: " + text;
            }
            catch (Exception error)
            {
                status = "Command failed: " + error.Message;
            }
        }
    }
}

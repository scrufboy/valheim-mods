using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Extensions;

namespace LivingWorld.FactionRuntime
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInDependency("com.jotunn.jotunn", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class LivingWorldFactionRuntimePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.livingworld.factionrelations.compat";
        public const string PluginName = "Living World Faction Relations Compatibility";
        public const string PluginVersion = "0.1.0";

        internal const string DefaultAssignments =
            "RRRN_Pillager=LWC.Pillagers;RRRN_OrcBrute=LWC.Eitrherjar;Krauguk=LWC.Eitrherjar";
        internal const string DefaultRelations =
            "LWC.Pillagers<->native:Undead=Hostile;" +
            "LWC.Pillagers<->LWC.Eitrherjar=Hostile;" +
            "LWC.Eitrherjar<->native:Undead=Hostile;" +
            "LWC.Eitrherjar<->native:PlainsMonsters=Hostile";
        internal const string ManagedFactionZdoKey = "lwc_managed_faction_v1";

        static ConfigEntry<bool> enabledEntry;
        static ConfigEntry<string> assignmentsEntry;
        static ConfigEntry<string> relationsEntry;
        static ManualLogSource log;
        static volatile RelationRegistry registry = RelationRegistry.Empty;
        Harmony harmony;

        void Awake()
        {
            log = Logger;
            enabledEntry = Config.BindConfig("General", "Enabled", true,
                "Server-authored synchronized managed faction relation overrides.", true);
            assignmentsEntry = Config.BindConfig("Registry", "PrefabAssignments", DefaultAssignments,
                "Semicolon-separated exact prefab=faction assignments.", true);
            relationsEntry = Config.BindConfig("Registry", "Relations", DefaultRelations,
                "Semicolon-separated A->B or A<->B relations; native:Name is a compatibility selector.", true);
            enabledEntry.SettingChanged += OnConfigChanged;
            assignmentsEntry.SettingChanged += OnConfigChanged;
            relationsEntry.SettingChanged += OnConfigChanged;
            RebuildRegistry();
            harmony = new Harmony(PluginGuid);
            harmony.PatchAll(typeof(LivingWorldFactionRuntimePlugin).Assembly);
            Logger.LogInfo(PluginName + " " + PluginVersion + " loaded; no profile or native faction mutation performed");
        }

        void OnDestroy()
        {
            if (harmony != null) harmony.UnpatchSelf();
        }

        static void OnConfigChanged(object sender, EventArgs args) { RebuildRegistry(); }

        static void RebuildRegistry()
        {
            try
            {
                RelationRegistry next = RelationRegistry.Parse(assignmentsEntry.Value, relationsEntry.Value);
                registry = next;
                log.LogInfo("Validated faction registry: " + next.AssignmentCount + " exact assignments, " +
                    next.RelationCount + " directed rules; unknown pairs use vanilla");
            }
            catch (Exception ex)
            {
                log.LogError("Rejected synchronized faction registry; retaining last valid registry: " + ex.Message);
            }
        }

        internal static bool TryOverride(Character first, Character second, out bool enemy)
        {
            enemy = false;
            if (enabledEntry == null || !enabledEntry.Value || first == null || second == null || first == second) return false;
            if (first.IsPlayer() || second.IsPlayer() || first.IsTamed() || second.IsTamed()) return false;
            if (CharacterSafety.SameExplicitGroup(first, second)) return false;
            RelationDisposition disposition;
            if (!registry.TryResolve(first, second, out disposition)) return false;
            enemy = disposition == RelationDisposition.Hostile;
            return true;
        }

        internal static void AssignPersistentIdentity(Character character)
        {
            if (character == null || ZNet.instance == null) return;
            ZNetView view = character.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner()) return;
            string faction;
            if (!registry.TryGetManagedFaction(CharacterSafety.PrefabName(character), out faction)) return;
            ZDO zdo = view.GetZDO();
            if (zdo == null) return;
            string existing = zdo.GetString(ManagedFactionZdoKey, "");
            if (existing == faction) return;
            if (!String.IsNullOrEmpty(existing) && registry.KnowsManagedFaction(existing))
            {
                log.LogWarning("Preserving existing managed faction " + existing + " on " + CharacterSafety.PrefabName(character));
                return;
            }
            zdo.Set(ManagedFactionZdoKey, faction);
        }

        [HarmonyPatch(typeof(BaseAI), "IsEnemy", new Type[] { typeof(Character), typeof(Character) })]
        static class BaseAIIsEnemyPatch
        {
            [HarmonyPriority(Priority.First)]
            static bool Prefix(Character __0, Character __1, ref bool __result)
            {
                bool enemy;
                if (!TryOverride(__0, __1, out enemy)) return true;
                __result = enemy;
                return false;
            }
        }

        [HarmonyPatch(typeof(Character), "Start")]
        static class CharacterStartPatch
        {
            static void Postfix(Character __instance) { AssignPersistentIdentity(__instance); }
        }
    }

    internal enum RelationDisposition { Friendly, Hostile }

    internal sealed class ActorFactionFacts
    {
        public string ManagedFaction;
        public string NativeFaction;
    }

    internal sealed class RelationRegistry
    {
        readonly Dictionary<string, string> assignments;
        readonly Dictionary<string, RelationDisposition> relations;
        readonly HashSet<string> managedFactions;
        public static readonly RelationRegistry Empty = new RelationRegistry(
            new Dictionary<string, string>(StringComparer.Ordinal),
            new Dictionary<string, RelationDisposition>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal));

        RelationRegistry(Dictionary<string, string> assignmentsValue,
            Dictionary<string, RelationDisposition> relationsValue, HashSet<string> managedFactionsValue)
        { assignments = assignmentsValue; relations = relationsValue; managedFactions = managedFactionsValue; }

        public int AssignmentCount { get { return assignments.Count; } }
        public int RelationCount { get { return relations.Count; } }

        public static RelationRegistry Parse(string assignmentText, string relationText)
        {
            Dictionary<string, string> assignments = new Dictionary<string, string>(StringComparer.Ordinal);
            HashSet<string> managed = new HashSet<string>(StringComparer.Ordinal);
            foreach (string entry in Split(assignmentText))
            {
                string[] pair = entry.Split(new char[] { '=' }, 2);
                if (pair.Length != 2) throw new FormatException("Invalid assignment: " + entry);
                string prefab = CleanId(pair[0], "prefab"), faction = CleanId(pair[1], "faction");
                if (prefab.StartsWith("native:", StringComparison.Ordinal) || !assignments.TryAddCompat(prefab, faction))
                    throw new FormatException("Duplicate or reserved prefab assignment: " + prefab);
                managed.Add(faction);
            }

            Dictionary<string, RelationDisposition> relations = new Dictionary<string, RelationDisposition>(StringComparer.Ordinal);
            foreach (string entry in Split(relationText))
            {
                string[] valuePair = entry.Split(new char[] { '=' }, 2);
                if (valuePair.Length != 2) throw new FormatException("Invalid relation: " + entry);
                RelationDisposition disposition;
                if (!Enum.TryParse(valuePair[1].Trim(), true, out disposition))
                    throw new FormatException("Disposition must be Friendly or Hostile: " + entry);
                string expression = valuePair[0].Trim();
                int symmetric = expression.IndexOf("<->", StringComparison.Ordinal);
                int directed = expression.IndexOf("->", StringComparison.Ordinal);
                bool both = symmetric >= 0;
                int split = both ? symmetric : directed;
                int width = both ? 3 : 2;
                if (split <= 0 || split + width >= expression.Length) throw new FormatException("Invalid relation arrow: " + entry);
                string left = CleanSelector(expression.Substring(0, split));
                string right = CleanSelector(expression.Substring(split + width));
                if (!left.StartsWith("native:", StringComparison.Ordinal)) managed.Add(left);
                if (!right.StartsWith("native:", StringComparison.Ordinal)) managed.Add(right);
                AddRelation(relations, left, right, disposition);
                if (both) AddRelation(relations, right, left, disposition);
            }
            return new RelationRegistry(assignments, relations, managed);
        }

        public bool TryResolve(Character first, Character second, out RelationDisposition disposition)
        {
            ActorFactionFacts a = Facts(first), b = Facts(second);
            return TryResolveFacts(a.ManagedFaction, a.NativeFaction, b.ManagedFaction, b.NativeFaction, out disposition);
        }

        ActorFactionFacts Facts(Character actor)
        {
            string managed = CharacterSafety.PersistentManagedFaction(actor, this);
            if (managed == null) assignments.TryGetValue(CharacterSafety.PrefabName(actor), out managed);
            return new ActorFactionFacts { ManagedFaction = managed, NativeFaction = actor.GetFaction().ToString() };
        }

        internal bool TryGetManagedFaction(string prefab, out string faction)
        { return assignments.TryGetValue(prefab, out faction); }

        internal bool KnowsManagedFaction(string faction) { return managedFactions.Contains(faction); }

        internal bool TryResolveFacts(string firstManaged, string firstNative, string secondManaged,
            string secondNative, out RelationDisposition disposition)
        {
            if (firstManaged != null && firstManaged == secondManaged)
            { disposition = RelationDisposition.Friendly; return false; }
            ActorFactionFacts first = new ActorFactionFacts { ManagedFaction = firstManaged, NativeFaction = firstNative };
            ActorFactionFacts second = new ActorFactionFacts { ManagedFaction = secondManaged, NativeFaction = secondNative };
            return relations.TryGetValue(SelectorKey(first) + ">" + SelectorKey(second), out disposition);
        }

        static string SelectorKey(ActorFactionFacts facts)
        { return facts.ManagedFaction ?? "native:" + facts.NativeFaction; }

        static void AddRelation(Dictionary<string, RelationDisposition> values, string left, string right,
            RelationDisposition disposition)
        {
            string key = left + ">" + right;
            if (values.ContainsKey(key)) throw new FormatException("Duplicate directed relation: " + key);
            values.Add(key, disposition);
        }

        static string CleanSelector(string value)
        {
            string result = CleanId(value, "selector");
            if (result == "native:") throw new FormatException("Native selector needs a faction name");
            return result;
        }

        static string CleanId(string value, string kind)
        {
            string result = value == null ? "" : value.Trim();
            if (result.Length == 0 || result.Length > 128 || result.IndexOfAny(new char[] { ';', '=', '>', '<' }) >= 0)
                throw new FormatException("Invalid " + kind + " ID");
            return result;
        }

        static IEnumerable<string> Split(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) yield break;
            foreach (string value in text.Split(';'))
                if (!String.IsNullOrWhiteSpace(value)) yield return value.Trim();
        }
    }

    internal static class DictionaryCompatibility
    {
        public static bool TryAddCompat<TKey, TValue>(this Dictionary<TKey, TValue> values, TKey key, TValue value)
        {
            if (values.ContainsKey(key)) return false;
            values.Add(key, value); return true;
        }
    }

    internal static class CharacterSafety
    {
        public static bool SameExplicitGroup(Character a, Character b)
        {
            string first = a.GetGroup();
            string second = b.GetGroup();
            return !String.IsNullOrEmpty(first) && String.Equals(first, second, StringComparison.Ordinal);
        }

        public static string PrefabName(Character actor)
        {
            ZNetView view = actor.GetComponent<ZNetView>();
            if (view != null && view.IsValid() && ZNetScene.instance != null)
            {
                ZDO zdo = view.GetZDO();
                if (zdo != null)
                {
                    UnityEngine.GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());
                    if (prefab != null) return prefab.name;
                }
            }
            string value = actor.gameObject == null ? actor.name : actor.gameObject.name;
            const string clone = "(Clone)";
            if (value != null && value.EndsWith(clone, StringComparison.Ordinal))
                value = value.Substring(0, value.Length - clone.Length);
            return value == null ? "" : value.Trim();
        }

        public static string PersistentManagedFaction(Character actor, RelationRegistry registry)
        {
            ZNetView view = actor.GetComponent<ZNetView>();
            if (view == null || !view.IsValid()) return null;
            ZDO zdo = view.GetZDO();
            if (zdo == null) return null;
            string value = zdo.GetString(LivingWorldFactionRuntimePlugin.ManagedFactionZdoKey, "");
            return registry.KnowsManagedFaction(value) ? value : null;
        }
    }
}

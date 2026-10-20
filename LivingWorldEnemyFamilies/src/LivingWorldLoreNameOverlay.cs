using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace LivingWorld.RegionalPeoples
{
    [BepInPlugin(Guid, Name, Version)]
    public sealed class LivingWorldLoreNameOverlay : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.lorenames";
        public const string Name = "Living World Lore Name Overlay";
        public const string Version = "0.1.0";

        sealed class Mapping
        {
            public string Prefab;
            public string Previous;
            public ConfigEntry<string> Name;
            public bool Verified;
        }

        ConfigEntry<bool> overlayEnabled;
        readonly List<Mapping> mappings = new List<Mapping>();
        ZNetScene scene;
        float nextAttempt;

        void Awake()
        {
            overlayEnabled = Config.Bind("General", "Enabled", true, "Apply configurable Living World display names without changing prefab IDs or source JSON.");
            mappings.Add(Bind("RRRN_Neckman", "Swamp Neckman", "Fen Draugr"));
            mappings.Add(Bind("RRRN_Servant", "Corrupted Servant", "Myrkvidr Wanderer"));
            mappings.Add(Bind("RRRN_OrcBrute", "Krauguk Legions", "Eitrherjar Legionary"));
            Logger.LogInfo("Lore-name overlay loaded; waiting for RRR prefab registration.");
        }

        Mapping Bind(string prefab, string previous, string proposed)
        {
            return new Mapping {
                Prefab = prefab,
                Previous = previous,
                Name = Config.Bind("Display Names", prefab, proposed, "Display name only. Internal prefab ID, faction, stats, equipment, attacks, biomes, and spawning remain unchanged.")
            };
        }

        void Update()
        {
            if (!overlayEnabled.Value || Time.unscaledTime < nextAttempt) return;
            nextAttempt = Time.unscaledTime + 1f;
            if (ZNetScene.instance == null) return;
            if (scene != ZNetScene.instance) {
                scene = ZNetScene.instance;
                foreach (var mapping in mappings) mapping.Verified = false;
            }
            foreach (var mapping in mappings) Apply(mapping);
        }

        void Apply(Mapping mapping)
        {
            if (mapping.Verified) return;
            var prefab = scene.GetPrefab(mapping.Prefab);
            if (prefab == null) return;
            var character = prefab.GetComponent<Character>();
            if (character == null) {
                Logger.LogWarning("Lore mapping found prefab without Character: " + mapping.Prefab);
                mapping.Verified = true;
                return;
            }
            var configured = (mapping.Name.Value ?? "").Trim();
            if (configured.Length == 0) {
                Logger.LogWarning("Lore mapping disabled by blank name: " + mapping.Prefab);
                mapping.Verified = true;
                return;
            }
            character.m_name = configured;
            mapping.Verified = character.m_name == configured;
            Logger.LogInfo(string.Format("Lore mapping {0}: '{1}' -> '{2}', verified={3}", mapping.Prefab, mapping.Previous, configured, mapping.Verified));
        }
    }
}

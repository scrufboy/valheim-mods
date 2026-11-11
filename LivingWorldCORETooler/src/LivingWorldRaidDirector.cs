using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;

namespace Scrufboy.LivingWorld.Tooler
{
    internal sealed class LivingWorldRaidCandidate
    {
        public readonly string Label;
        public readonly string EventName;
        public readonly string PrefabName;
        public readonly string PluginGuid;
        public readonly string Readiness;

        public LivingWorldRaidCandidate(string label, string eventName, string prefabName, string pluginGuid, string readiness)
        {
            Label = label;
            EventName = eventName;
            PrefabName = prefabName;
            PluginGuid = pluginGuid;
            Readiness = readiness;
        }
    }

    internal static class LivingWorldRaidDirector
    {
        internal static readonly LivingWorldRaidCandidate[] Candidates =
        {
            new LivingWorldRaidCandidate("NekMyn Brute", "lwc_reveal_nekmyn_brute", "LWC_NekMynBrute", "lwc.nekmyn.brute", "candidate; requires a successful live registration"),
            new LivingWorldRaidCandidate("Draugr Behemoth — club", "lwc_reveal_behemoth_club", "LWC_UndeadColossus_Test", "lwc.creatures.undeadcolossus", "registered; reveal candidate"),
            new LivingWorldRaidCandidate("Draugr Behemoth — unarmed", "lwc_reveal_behemoth_unarmed", "LWC_UndeadColossus_Unarmed", "lwc.creatures.undeadcolossus.polish", "registered; gameplay validation pending"),
            new LivingWorldRaidCandidate("NekMyn Bowman", "lwc_reveal_nekmyn_bowman", "LWC_NeckMyn_Bowman_Recovery_QA", "lwc.neckmyn.bowman.recovery.qa", "QA-only; final acceptance false"),
            new LivingWorldRaidCandidate("NekMyn Spearman", "lwc_reveal_nekmyn_spearman", "LWC_NekMyn_Spearman", "lwc.nekmyn.spearman", "candidate; source registration required"),
            new LivingWorldRaidCandidate("NekMyn Unarmed / commoner", "lwc_reveal_nekmyn_commoner", "RRRN_Neckman", "com.alexanderstrada.rrrcore", "legacy Souls commoner; not a new specialized runtime"),
            new LivingWorldRaidCandidate("NekMyn Shaman", "lwc_reveal_nekmyn_shaman", "LWC_NekMyn_Shaman", "lwc.nekmyn.shaman.combatant", "candidate; current combat contract may refuse registration"),
            new LivingWorldRaidCandidate("Formidable Surtling", "lwc_reveal_formidable_surtling", "Surtling", "lwc.creatures.formidablesurtling", "native Surtling identity with installed Formidable replacement"),
            new LivingWorldRaidCandidate("Sunken Marauder 0.3.7", "lwc_reveal_sunken_marauder", "LWC_MarshMarauder_Prototype", "lwc.creatures.marshmarauder", "0.3.7 candidate; current registration failure must be resolved")
        };

        private static LivingWorldRaidCandidate active;
        private static Vector3 target;
        private static int observed;
        private static int wave;
        private static DateTime requestedUtc;

        internal static bool IsAvailable(LivingWorldRaidCandidate candidate, out string reason)
        {
            if (!IsPrefabAvailable(candidate, out reason))
                return false;
            if (RandEventSystem.instance == null || !RandEventSystem.instance.HaveEvent(candidate.EventName))
            {
                reason = "raid definition is not registered: " + candidate.EventName;
                return false;
            }
            reason = "ready";
            return true;
        }

        internal static bool IsPrefabAvailable(LivingWorldRaidCandidate candidate, out string reason)
        {
            if (candidate == null)
            {
                reason = "candidate is missing";
                return false;
            }
            if (!Chainloader.PluginInfos.ContainsKey(candidate.PluginGuid))
            {
                reason = "required module is not loaded: " + candidate.PluginGuid;
                return false;
            }
            if (ZNetScene.instance == null)
            {
                reason = "world prefab registry is not ready";
                return false;
            }
            GameObject prefab = ZNetScene.instance.GetPrefab(candidate.PrefabName);
            if (prefab == null || !String.Equals(prefab.name, candidate.PrefabName, StringComparison.Ordinal))
            {
                reason = "exact prefab is not registered: " + candidate.PrefabName;
                return false;
            }
            reason = "ready";
            return true;
        }

        internal static bool Start(LivingWorldRaidCandidate candidate, int minDistance, int maxDistance, int approachSeconds, int count, int intervalSeconds, int waves, out string result)
        {
            string reason;
            if (!IsAvailable(candidate, out reason))
            {
                result = "REFUSED — " + candidate.Label + ": " + reason + ". No substitute was spawned.";
                Telemetry(candidate, null, Vector3.zero, 0f, 0, "local-player-party", reason);
                return false;
            }
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                result = "REFUSED — local player is unavailable.";
                return false;
            }

            RandomEvent raid = RandEventSystem.instance.m_events.FirstOrDefault(x => x != null && x.m_name == candidate.EventName);
            if (raid == null)
            {
                result = "REFUSED — exact raid record disappeared before launch.";
                return false;
            }
            GameObject exact = ZNetScene.instance.GetPrefab(candidate.PrefabName);
            List<SpawnSystem.SpawnData> slots = raid.m_spawn.Where(x => x != null && x.m_prefab != null && x.m_prefab.name == candidate.PrefabName).ToList();
            if (slots.Count != 1 || exact == null || slots[0].m_prefab != exact)
            {
                result = "REFUSED — raid-to-prefab registration is not exact for " + candidate.PrefabName + ".";
                Telemetry(candidate, slots.Count == 1 && slots[0].m_prefab != null ? slots[0].m_prefab.name : null, Vector3.zero, 0f, 0, "local-player-party", "raid-prefab-mismatch");
                return false;
            }

            SpawnSystem.SpawnData slot = slots[0];
            slot.m_spawnRadiusMin = Mathf.Clamp(minDistance, 40, 250);
            slot.m_spawnRadiusMax = Mathf.Clamp(maxDistance, (int)slot.m_spawnRadiusMin, 300);
            slot.m_groupSizeMin = Mathf.Clamp(count, 1, 20);
            slot.m_groupSizeMax = slot.m_groupSizeMin;
            slot.m_maxSpawned = Mathf.Clamp(count * waves, count, 60);
            slot.m_spawnInterval = Mathf.Clamp(intervalSeconds, 1, 300);
            slot.m_spawnChance = 100f;
            slot.m_huntPlayer = true;
            slot.m_canSpawnCloseToPlayer = false;
            raid.m_duration = Mathf.Clamp(approachSeconds + (waves - 1) * intervalSeconds, 15, 1800);

            active = candidate;
            target = player.transform.position;
            observed = 0;
            wave = 0;
            requestedUtc = DateTime.UtcNow;
            RandEventSystem.instance.SetRandomEventByName(candidate.EventName, target);
            result = "STARTED — " + candidate.Label + " at " + slot.m_spawnRadiusMin.ToString("0", CultureInfo.InvariantCulture) + "–" + slot.m_spawnRadiusMax.ToString("0", CultureInfo.InvariantCulture) + " m; count " + count + ", interval " + intervalSeconds + " s, waves " + waves + ", approach window " + approachSeconds + " s.";
            Telemetry(candidate, candidate.PrefabName, target, 0f, 0, "local-player-party", null);
            return true;
        }

        internal static void Stop()
        {
            if (RandEventSystem.instance != null)
                RandEventSystem.instance.ResetRandomEvent();
            active = null;
        }

        internal static void Observe(Character character)
        {
            if (active == null || character == null || character.gameObject == null)
                return;
            string actual = character.gameObject.name.Replace("(Clone)", String.Empty).Trim();
            if (!String.Equals(actual, active.PrefabName, StringComparison.Ordinal))
                return;
            observed++;
            wave++;
            Vector3 position = character.transform.position;
            float distance = Vector3.Distance(new Vector3(position.x, 0f, position.z), new Vector3(target.x, 0f, target.z));
            Telemetry(active, actual, position, distance, wave, "local-player-party", null);
        }

        private static void Telemetry(LivingWorldRaidCandidate candidate, string actualPrefab, Vector3 point, float distance, int waveNumber, string party, string failure)
        {
            try
            {
                string directory = Path.Combine(Paths.ConfigPath, "LivingWorld", "Tooler", "RaidTelemetry");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "raid-" + DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".jsonl");
                string line = "{\"utc\":\"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\",\"requestUtc\":\"" + requestedUtc.ToString("o", CultureInfo.InvariantCulture) + "\",\"event\":\"" + Json(candidate == null ? null : candidate.EventName) + "\",\"requestedPrefab\":\"" + Json(candidate == null ? null : candidate.PrefabName) + "\",\"actualPrefab\":\"" + Json(actualPrefab) + "\",\"spawnPoint\":{\"x\":" + point.x.ToString("R", CultureInfo.InvariantCulture) + ",\"y\":" + point.y.ToString("R", CultureInfo.InvariantCulture) + ",\"z\":" + point.z.ToString("R", CultureInfo.InvariantCulture) + "},\"distance\":" + distance.ToString("R", CultureInfo.InvariantCulture) + ",\"wave\":" + waveNumber + ",\"targetParty\":\"" + Json(party) + "\",\"failureReason\":\"" + Json(failure) + "\"}";
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
        }

        private static string Json(string value)
        {
            return (value ?? String.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }

    [HarmonyPatch(typeof(Character), "Awake")]
    internal static class LivingWorldRaidSpawnTelemetryPatch
    {
        private static void Postfix(Character __instance)
        {
            LivingWorldRaidDirector.Observe(__instance);
        }
    }
}

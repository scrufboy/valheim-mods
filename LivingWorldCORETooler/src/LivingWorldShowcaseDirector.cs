using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace Scrufboy.LivingWorld.Tooler
{
    internal sealed class LivingWorldShowcaseMarker : MonoBehaviour
    {
        internal string RunId;
        internal string CohortId;
        internal string RequestedPrefab;
        internal int Wave;
        internal bool ArrivalLogged;
        internal bool CompanionReinforcement;
    }

    internal sealed class LivingWorldShowcaseCohort
    {
        internal string Id;
        internal LivingWorldRaidCandidate Candidate;
        internal int Count;
        internal int MinDistance;
        internal int MaxDistance;
        internal float ApproachDelay;
        internal float Interval;
        internal int Waves;
        internal int NextWave;
        internal float NextAt;
        internal string TeamIntent;
        internal bool Complete;
    }

    internal static class LivingWorldShowcaseDirector
    {
        private static readonly List<LivingWorldShowcaseCohort> cohorts = new List<LivingWorldShowcaseCohort>();
        private static readonly List<LivingWorldShowcaseMarker> owned = new List<LivingWorldShowcaseMarker>();
        private static string runId;
        private static int cohortSerial;
        private static string lastAction = "Showcase idle.";
        internal static bool ExplicitlyEnabled { get; set; }

        internal static string Status { get { return lastAction; } }
        internal static bool Running { get { return !String.IsNullOrEmpty(runId); } }

        internal static bool Queue(LivingWorldRaidCandidate candidate, int minDistance, int maxDistance, int approachDelay, int count, int intervalSeconds, int waves, string action, out string result)
        {
            if (!ExplicitlyEnabled)
            {
                result = "REFUSED — Showcase Director is disabled. An authorized administrator must explicitly enable the private testing bridge.";
                Write("cohort_refused", action, candidate, null, Vector3.zero, 0f, 0, "showcase-explicit-enablement-required");
                return false;
            }
            string reason = "exact prefab is not registered";
            if (!LivingWorldRaidDirector.IsPrefabAvailable(candidate, out reason))
            {
                result = "REFUSED — " + candidate.Label + ": " + reason + ". No substitute was spawned.";
                Write("cohort_refused", action, candidate, null, Vector3.zero, 0f, 0, reason);
                return false;
            }
            if (Player.m_localPlayer == null || ZNet.instance == null || !ZNet.instance.IsServer())
            {
                result = "REFUSED — Showcase Director requires local host/server-administrator authority; unprivileged remote clients are not permitted.";
                Write("cohort_refused", action, candidate, null, Vector3.zero, 0f, 0, "local-host-or-server-admin-authority-required");
                return false;
            }
            if (String.IsNullOrEmpty(runId))
            {
                runId = DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
                cohortSerial = 0;
                Write("run_started", action, candidate, null, Player.m_localPlayer.transform.position, 0f, 0, null);
            }
            LivingWorldShowcaseCohort cohort = new LivingWorldShowcaseCohort
            {
                Id = "cohort-" + (++cohortSerial).ToString("D3", CultureInfo.InvariantCulture),
                Candidate = candidate,
                Count = Mathf.Clamp(count, 1, 20),
                MinDistance = Mathf.Clamp(minDistance, 40, 250),
                MaxDistance = Mathf.Clamp(maxDistance, minDistance, 300),
                ApproachDelay = Mathf.Clamp(approachDelay, 0, 1800),
                Interval = Mathf.Clamp(intervalSeconds, 1, 300),
                Waves = Mathf.Clamp(waves, 1, 20),
                NextWave = 1,
                NextAt = Time.realtimeSinceStartup + Mathf.Clamp(approachDelay, 0, 1800),
                TeamIntent = "preserve-prefab-faction; hostile approach to local-player-party"
            };
            cohorts.Add(cohort);
            result = "QUEUED " + candidate.Label + " as " + cohort.Id + " in showcase run " + runId + ".";
            lastAction = result;
            Write("cohort_queued", action, candidate, null, Player.m_localPlayer.transform.position, 0f, 0, null, cohort.Id);
            return true;
        }

        internal static void Tick()
        {
            if (String.IsNullOrEmpty(runId) || Player.m_localPlayer == null)
                return;
            float now = Time.realtimeSinceStartup;
            foreach (LivingWorldShowcaseCohort cohort in cohorts.ToArray())
            {
                if (cohort.Complete || now < cohort.NextAt)
                    continue;
                SpawnWave(cohort);
                cohort.NextWave++;
                if (cohort.NextWave > cohort.Waves)
                    cohort.Complete = true;
                else
                    cohort.NextAt = now + cohort.Interval;
            }
            foreach (LivingWorldShowcaseMarker marker in owned.ToArray())
            {
                if (marker == null) { owned.Remove(marker); continue; }
                if (!marker.ArrivalLogged && Vector3.Distance(marker.transform.position, Player.m_localPlayer.transform.position) <= 30f)
                {
                    marker.ArrivalLogged = true;
                    WriteMarker("arrival", "runtime", marker, null);
                }
            }
        }

        internal static void NextWave(string action)
        {
            float now = Time.realtimeSinceStartup;
            foreach (LivingWorldShowcaseCohort cohort in cohorts.Where(x => !x.Complete))
                cohort.NextAt = now;
            lastAction = "Next showcase wave released.";
            Write("next_wave", action, null, null, Player.m_localPlayer == null ? Vector3.zero : Player.m_localPlayer.transform.position, 0f, 0, null);
        }

        internal static void Stop(string action)
        {
            string stoppedRun = runId;
            int removed = 0;
            foreach (LivingWorldShowcaseMarker marker in owned.ToArray())
            {
                if (marker == null) continue;
                WriteMarker("cleanup", action, marker, "showcase-stop");
                if (ZNetScene.instance != null) ZNetScene.instance.Destroy(marker.gameObject);
                else UnityEngine.Object.Destroy(marker.gameObject);
                removed++;
            }
            owned.Clear();
            cohorts.Clear();
            runId = null;
            lastAction = "Showcase stopped; removed " + removed + " Tooler-owned test instances from " + (stoppedRun ?? "no run") + ". Native raid state was not changed.";
        }

        internal static void Reinforce(string action)
        {
            LivingWorldRaidCandidate test = new LivingWorldRaidCandidate("Showcase companion reinforcement", "", "LWC_ShowcaseCompanion_Test", "local.livingworld.humannpc", "dedicated noncanonical test prefab required");
            string result;
            if (!Queue(test, 8, 12, 0, 2, 5, 1, action, out result))
                lastAction = result + " Canonical companions were not touched.";
            else
            {
                LivingWorldShowcaseCohort cohort = cohorts[cohorts.Count - 1];
                cohort.TeamIntent = "showcase-companion reinforcement; noncanonical test provenance";
                lastAction = result;
            }
        }

        internal static void Recall(string action)
        {
            if (Player.m_localPlayer == null) return;
            int moved = 0;
            foreach (LivingWorldShowcaseMarker marker in owned.Where(x => x != null && x.CompanionReinforcement))
            {
                Vector3 offset = Quaternion.Euler(0f, moved * 55f, 0f) * Vector3.forward * 4f;
                marker.transform.position = Player.m_localPlayer.transform.position + offset;
                moved++;
                WriteMarker("reinforcement_recall", action, marker, null);
            }
            lastAction = "Recalled " + moved + " showcase-only companion test instances. Canonical companions were not touched.";
        }

        internal static void Death(Character character)
        {
            LivingWorldShowcaseMarker marker = character == null ? null : character.GetComponent<LivingWorldShowcaseMarker>();
            if (marker != null) WriteMarker("death", "runtime", marker, null);
        }

        private static void SpawnWave(LivingWorldShowcaseCohort cohort)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(cohort.Candidate.PrefabName);
            string reason = "exact prefab is not registered";
            if (prefab == null || !LivingWorldRaidDirector.IsPrefabAvailable(cohort.Candidate, out reason))
            {
                cohort.Complete = true;
                Write("wave_refused", "scheduler", cohort.Candidate, null, Vector3.zero, 0f, cohort.NextWave, reason, cohort.Id);
                return;
            }
            for (int index = 0; index < cohort.Count; index++)
            {
                Vector3 point;
                if (!FindSpawnPoint(Player.m_localPlayer.transform.position, cohort.MinDistance, cohort.MaxDistance, cohort.Id.GetHashCode() + cohort.NextWave * 101 + index, out point))
                {
                    Write("spawn_refused", "scheduler", cohort.Candidate, null, Vector3.zero, 0f, cohort.NextWave, "no-nearby-spawnable-area", cohort.Id);
                    continue;
                }
                GameObject actor = UnityEngine.Object.Instantiate(prefab, point, Quaternion.LookRotation(Player.m_localPlayer.transform.position - point));
                LivingWorldShowcaseMarker marker = actor.AddComponent<LivingWorldShowcaseMarker>();
                marker.RunId = runId;
                marker.CohortId = cohort.Id;
                marker.RequestedPrefab = cohort.Candidate.PrefabName;
                marker.Wave = cohort.NextWave;
                marker.CompanionReinforcement = cohort.Candidate.PrefabName == "LWC_ShowcaseCompanion_Test";
                MonsterAI ai = actor.GetComponent<MonsterAI>();
                if (ai != null && !marker.CompanionReinforcement) ai.m_enableHuntPlayer = true;
                owned.Add(marker);
                float distance = HorizontalDistance(point, Player.m_localPlayer.transform.position);
                Write("spawn", "scheduler", cohort.Candidate, actor.name.Replace("(Clone)", String.Empty).Trim(), point, distance, cohort.NextWave, null, cohort.Id);
            }
        }

        private static bool FindSpawnPoint(Vector3 center, int minimum, int maximum, int seed, out Vector3 point)
        {
            point = Vector3.zero;
            if (ZoneSystem.instance == null) return false;
            System.Random random = new System.Random(seed);
            for (int attempt = 0; attempt < 72; attempt++)
            {
                float angle = (float)(random.NextDouble() * Math.PI * 2.0);
                float distance = minimum + (float)random.NextDouble() * (maximum - minimum);
                Vector3 probe = center + new Vector3(Mathf.Sin(angle) * distance, 0f, Mathf.Cos(angle) * distance);
                float ground;
                if (!ZoneSystem.instance.GetGroundHeight(probe, out ground)) continue;
                if (ground <= ZoneSystem.instance.m_waterLevel + 0.5f) continue;
                RaycastHit hit;
                Vector3 above = new Vector3(probe.x, ground + 20f, probe.z);
                if (!Physics.Raycast(above, Vector3.down, out hit, 45f, Physics.DefaultRaycastLayers)) continue;
                if (Vector3.Dot(hit.normal, Vector3.up) < 0.72f) continue;
                if (Physics.CheckSphere(hit.point + Vector3.up * 1.2f, 0.8f, Physics.DefaultRaycastLayers)) continue;
                point = hit.point + Vector3.up * 0.1f;
                return true;
            }
            return false;
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
        }

        private static void WriteMarker(string kind, string action, LivingWorldShowcaseMarker marker, string failure)
        {
            LivingWorldRaidCandidate candidate = LivingWorldRaidDirector.Candidates.FirstOrDefault(x => x.PrefabName == marker.RequestedPrefab);
            Write(kind, action, candidate, marker.gameObject.name.Replace("(Clone)", String.Empty).Trim(), marker.transform.position, Player.m_localPlayer == null ? 0f : HorizontalDistance(marker.transform.position, Player.m_localPlayer.transform.position), marker.Wave, failure, marker.CohortId, marker.RunId);
        }

        private static void Write(string kind, string action, LivingWorldRaidCandidate candidate, string actual, Vector3 point, float distance, int wave, string failure, string cohortId = "", string forcedRun = null)
        {
            try
            {
                string directory = Path.Combine(Paths.ConfigPath, "LivingWorld", "Tooler", "ShowcaseTelemetry");
                Directory.CreateDirectory(directory);
                string nativeEvent = RandEventSystem.instance != null && RandEventSystem.instance.GetActiveEvent() != null ? RandEventSystem.instance.GetActiveEvent().m_name : String.Empty;
                string line = "{\"utc\":\"" + DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + "\",\"kind\":\"" + Json(kind) + "\",\"streamDeckAction\":\"" + Json(action) + "\",\"runId\":\"" + Json(forcedRun ?? runId) + "\",\"cohortId\":\"" + Json(cohortId) + "\",\"nativeEvent\":\"" + Json(nativeEvent) + "\",\"requestedPrefab\":\"" + Json(candidate == null ? null : candidate.PrefabName) + "\",\"actualPrefab\":\"" + Json(actual) + "\",\"spawnPoint\":{\"x\":" + point.x.ToString("R", CultureInfo.InvariantCulture) + ",\"y\":" + point.y.ToString("R", CultureInfo.InvariantCulture) + ",\"z\":" + point.z.ToString("R", CultureInfo.InvariantCulture) + "},\"distance\":" + distance.ToString("R", CultureInfo.InvariantCulture) + ",\"wave\":" + wave + ",\"targetParty\":\"local-player-party\",\"failureReason\":\"" + Json(failure) + "\"}";
                File.AppendAllText(Path.Combine(directory, "showcase-" + DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".jsonl"), line + Environment.NewLine);
            }
            catch { }
        }

        private static string Json(string value)
        {
            return (value ?? String.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }

    [HarmonyPatch(typeof(Character), "OnDeath")]
    internal static class LivingWorldShowcaseDeathPatch
    {
        private static void Prefix(Character __instance) { LivingWorldShowcaseDirector.Death(__instance); }
    }
}

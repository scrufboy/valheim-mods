using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using LivingWorld.HumanNpcV2;
using UnityEngine;

namespace LivingWorld.TelemetryV2
{
    [BepInPlugin(Guid, "Living World Lab Telemetry", Version)]
    [BepInDependency("local.livingworld.humannpc", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class TelemetryV2 : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.telemetry";
        public const string Version = "0.6.1";
        const int Schema = 7;
        const float SampleSeconds = .5f;
        const long MaxBytes = 64L * 1024L * 1024L;
        const int MaxRecords = 200000;
        const int MaxRecordsPerSecond = 50;
        static readonly BindingFlags Native = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Left = typeof(Humanoid).GetField("m_leftItem", Native);
        static readonly FieldInfo Right = typeof(Humanoid).GetField("m_rightItem", Native);
        static readonly FieldInfo HiddenLeft = typeof(Humanoid).GetField("m_hiddenLeftItem", Native);
        static readonly FieldInfo HiddenRight = typeof(Humanoid).GetField("m_hiddenRightItem", Native);

        readonly Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, float> health = new Dictionary<string, float>(StringComparer.Ordinal);
        StreamWriter writer;
        string sessionId;
        string outputPath;
        float started;
        float nextSample;
        float nextFlush;
        int records;
        int secondBucket;
        int recordsThisSecond;
        int dropped;
        int segment;
        long bytes;
        bool active;
        bool fault;
        bool sawWorld;

        void Awake()
        {
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null)
                throw new MissingMemberException("Valheim's native hand-item observation fields changed.");
            Logger.LogInfo("Read-only 0.6.1 back-stow telemetry armed; no gameplay methods are patched.");
        }

        void Update()
        {
            bool worldReady = ZNet.instance != null && Player.m_localPlayer != null && Game.instance != null;
            if (!worldReady)
            {
                if (active) StopSession("world-exit");
                if (sawWorld) { segment = 0; sawWorld = false; fault = false; }
                return;
            }

            sawWorld = true;
            if (!active && !fault) { StartSession(); return; }
            if (!active || fault) return;

            try
            {
                float now = Time.realtimeSinceStartup;
                if (records >= MaxRecords || bytes >= MaxBytes)
                {
                    StopSession("automatic-file-rotation");
                    segment++;
                    StartSession();
                    return;
                }
                if (now >= nextSample)
                {
                    nextSample = now + SampleSeconds;
                    Sample(false);
                }
                if (now >= nextFlush)
                {
                    nextFlush = now + 2f;
                    writer.Flush();
                }
            }
            catch (Exception error)
            {
                fault = true;
                try { Emit("RecorderFault", "", "error", error.Message); writer.Flush(); } catch { }
                Logger.LogError("Telemetry stopped without changing gameplay: " + error);
            }
        }

        void StartSession()
        {
            if (active || ZNet.instance == null || Player.m_localPlayer == null || Game.instance == null) return;
            string directory = Path.Combine(Paths.BepInExRootPath, "LivingWorldTelemetry");
            Directory.CreateDirectory(directory);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string label = SafeName(ZNet.instance.GetWorldName());
            outputPath = Path.Combine(directory, "LWC-" + stamp + "-" + label + "-part" + segment.ToString("D2", CultureInfo.InvariantCulture) + ".jsonl");
            writer = new StreamWriter(new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            sessionId = System.Guid.NewGuid().ToString("N");
            started = Time.realtimeSinceStartup;
            nextSample = started;
            nextFlush = started + 2f;
            records = secondBucket = recordsThisSecond = dropped = 0;
            bytes = 0;
            states.Clear();
            health.Clear();
            active = true;
            Emit("RunStart", "", "telemetryVersion", Version, "npcBuild", NpcBuild(), "world", ZNet.instance.GetWorldName(),
                "profile", Game.instance.GetPlayerProfile().GetFilename(), "player", Player.m_localPlayer.GetPlayerName(),
                "sampleSeconds", SampleSeconds, "observationOnly", true);
            Sample(true);
            writer.Flush();
            Logger.LogInfo("Automatic read-only telemetry recording: " + outputPath);
        }

        void StopSession(string reason)
        {
            if (!active) return;
            try
            {
                Emit("RunSummary", "", "reason", reason, "records", records, "dropped", dropped,
                    "bytes", bytes, "durationSeconds", Time.realtimeSinceStartup - started);
                writer.Flush();
            }
            finally
            {
                writer.Dispose();
                writer = null;
                active = false;
            }
            Logger.LogInfo("Automatic telemetry closed: records=" + records + "; dropped=" + dropped + "; file=" + outputPath);
        }

        void Sample(bool initial)
        {
            Character[] actors = Character.GetAllCharacters().Where(character => character != null &&
                character.GetComponent<HumanCompanionV2>() != null).ToArray();
            foreach (Character actor in actors) SampleActor(actor, actors, initial);
            Player player = Player.m_localPlayer;
            if (player != null)
            {
                Vector3 velocity = player.GetVelocity();
                Vector3 forward = player.transform.forward;
                Emit("LeaderSample", Id(player),
                    "x", player.transform.position.x, "y", player.transform.position.y, "z", player.transform.position.z,
                    "vx", velocity.x, "vy", velocity.y, "vz", velocity.z,
                    "speed", new Vector2(velocity.x, velocity.z).magnitude,
                    "forwardX", forward.x, "forwardZ", forward.z,
                    "running", player.IsRunning(), "crouched", player.IsCrouching(),
                    "right", Item(Right, player), "left", Item(Left, player),
                    "currentWeapon", Item(player.GetCurrentWeapon()), "wet", Wet(player));
                EmitChanged("player.environment", "Observation", Id(player), "wet=" + Wet(player),
                    "wet", Wet(player), "health", player.GetHealth(), "x", player.transform.position.x,
                    "y", player.transform.position.y, "z", player.transform.position.z);
            }
        }

        void SampleActor(Character actor, Character[] party, bool initial)
        {
            string id = Id(actor);
            Humanoid human = actor as Humanoid;
            MonsterAI ai = actor.GetComponent<MonsterAI>();
            HumanCompanionV2 companion = actor.GetComponent<HumanCompanionV2>();
            HumanBehaviorController behavior = actor.GetComponent<HumanBehaviorController>();
            HumanAppearanceV2 appearance = actor.GetComponent<HumanAppearanceV2>();
            Character target = ai == null ? null : ai.GetTargetCreature();
            GameObject follow = ai == null ? null : ai.GetFollowTarget();
            Player leader = Player.m_localPlayer;
            Vector3 offset = leader == null ? Vector3.zero : actor.transform.position - leader.transform.position;
            float side = leader == null ? 0f : Vector3.Dot(offset, leader.transform.right);
            float forward = leader == null ? 0f : Vector3.Dot(offset, leader.transform.forward);
            float nearest = Single.PositiveInfinity;
            foreach (Character other in party)
                if (other != actor) nearest = Mathf.Min(nearest, Vector3.Distance(actor.transform.position, other.transform.position));

            string left = Item(Left, human);
            string right = Item(Right, human);
            string hiddenLeft = Item(HiddenLeft, human);
            string hiddenRight = Item(HiddenRight, human);
            string targetId = Id(target);
            string state = String.Join("|", new[] {
                appearance == null ? "appearance-missing" : appearance.Status,
                companion == null ? "companion-missing" : companion.State,
                behavior == null ? "behavior-missing" : behavior.Status,
                behavior == null ? "traversal-missing" : behavior.TraversalStatus,
                behavior == null ? "equipment-missing" : behavior.EquipmentStatus,
                behavior == null ? "guard-missing" : behavior.SettledGuardStatus,
                behavior == null ? "boundary-missing" : behavior.CombatBoundaryReleases.ToString(CultureInfo.InvariantCulture),
                ai == null ? "ai-missing" : ai.IsAlerted().ToString(), targetId,
                left, right, hiddenLeft, hiddenRight, Wet(actor).ToString()
            });
            EmitChanged("actor." + id, "ActorState", id, state,
                "initial", initial, "name", human == null ? actor.name : human.m_name,
                "role", appearance == null ? "" : appearance.Role,
                "female", appearance != null && appearance.Female,
                "hair", appearance == null ? "" : appearance.HairName,
                "beard", appearance == null ? "" : appearance.BeardName,
                "appearanceReady", appearance != null && appearance.Ready,
                "appearanceStatus", appearance == null ? "missing" : appearance.Status,
                "companionState", companion == null ? "missing" : companion.State,
                "legacyTameCleared", companion != null && companion.LegacyTameCleared,
                "behaviorState", behavior == null ? "missing" : behavior.State.ToString(),
                "behaviorStatus", behavior == null ? "missing" : behavior.Status,
                "formationSlot", behavior == null ? -1 : behavior.FormationSlot,
                "formationHeading", behavior == null ? 0f : behavior.FormationHeading,
                "formationTargetDistance", behavior == null ? -1f : behavior.FormationTargetDistance,
                "combatClearScans", behavior == null ? -1 : behavior.ClearScans,
                "combatClearRequired", behavior == null ? -1 : behavior.RequiredClearScans,
                "traversalStatus", behavior == null ? "missing" : behavior.TraversalStatus,
                "equipmentStatus", behavior == null ? "missing" : behavior.EquipmentStatus,
                "settledGuardStatus", behavior == null ? "missing" : behavior.SettledGuardStatus,
                "guardSector", behavior == null ? -1 : behavior.GuardSector,
                "guardPhase", behavior == null ? "missing" : behavior.GuardPhase,
                "nativeAlert", behavior != null && behavior.NativeAlert,
                "committedThreat", behavior != null && behavior.CommittedThreat,
                "leaderArmed", behavior != null && behavior.LeaderArmed,
                "controllerLeaderDistance", behavior == null ? -1f : behavior.LeaderDistance,
                "controllerLeaderSpeed", behavior == null ? -1f : behavior.LeaderSpeed,
                "combatBoundaryReleases", behavior == null ? -1 : behavior.CombatBoundaryReleases,
                "alerted", ai != null && ai.IsAlerted(), "target", targetId,
                "followTarget", follow == null ? "" : follow.name,
                "tamed", actor.IsTamed(),
                "health", actor.GetHealth(), "maxHealth", actor.GetMaxHealth(), "wet", Wet(actor),
                "left", left, "right", right, "stowedLeft", hiddenLeft, "stowedRight", hiddenRight,
                "weapon", human == null ? "" : Item(human.GetCurrentWeapon()));

            Vector3 position = actor.transform.position;
            Vector3 velocity = actor.GetVelocity();
            Emit("PositionSample", id,
                "x", position.x, "y", position.y, "z", position.z,
                "vx", velocity.x, "vy", velocity.y, "vz", velocity.z, "speed", velocity.magnitude,
                "leaderSide", side, "leaderForward", forward, "leaderDistance", leader == null ? -1f : offset.magnitude,
                "nearestCompanion", Single.IsPositiveInfinity(nearest) ? -1f : nearest,
                "targetDistance", target == null ? -1f : Vector3.Distance(position, target.transform.position),
                "followDistance", follow == null ? -1f : Vector3.Distance(position, follow.transform.position));

            float before;
            if (health.TryGetValue(id, out before) && Math.Abs(before - actor.GetHealth()) > .001f)
                Emit("HealthChange", id, "before", before, "after", actor.GetHealth(), "delta", actor.GetHealth() - before,
                    "target", targetId, "weapon", human == null ? "" : Item(human.GetCurrentWeapon()));
            health[id] = actor.GetHealth();
        }

        void EmitChanged(string key, string category, string actor, string fingerprint, params object[] fields)
        {
            string previous;
            if (states.TryGetValue(key, out previous) && previous == fingerprint) return;
            states[key] = fingerprint;
            Emit(category, actor, fields);
        }

        void Emit(string category, string actor, params object[] fields)
        {
            bool final = category == "RunSummary" || category == "RecorderFault";
            if (!active || writer == null || (!final && (records >= MaxRecords || bytes >= MaxBytes))) { dropped++; return; }
            int second = Mathf.FloorToInt(Time.realtimeSinceStartup - started);
            if (second != secondBucket) { secondBucket = second; recordsThisSecond = 0; }
            if (!final && recordsThisSecond >= MaxRecordsPerSecond) { dropped++; return; }
            recordsThisSecond++;
            var text = new StringBuilder(512);
            text.Append('{');
            Add(text, "schema", Schema, false);
            Add(text, "session", sessionId, true);
            Add(text, "category", category, true);
            Add(text, "wallUtc", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture), true);
            Add(text, "elapsed", Time.realtimeSinceStartup - started, true);
            if (!String.IsNullOrEmpty(actor)) Add(text, "actor", actor, true);
            for (int index = 0; fields != null && index + 1 < fields.Length; index += 2)
                Add(text, Convert.ToString(fields[index], CultureInfo.InvariantCulture), fields[index + 1], true);
            text.Append('}');
            string line = text.ToString();
            writer.WriteLine(line);
            bytes += Encoding.UTF8.GetByteCount(line) + 1;
            records++;
        }

        static void Add(StringBuilder text, string key, object value, bool comma)
        {
            if (comma) text.Append(',');
            text.Append('"').Append(Escape(key)).Append("\":");
            if (value == null) { text.Append("null"); return; }
            if (value is bool) { text.Append((bool)value ? "true" : "false"); return; }
            if (value is float) { text.Append(((float)value).ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is double) { text.Append(((double)value).ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is int || value is long) { text.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            text.Append('"').Append(Escape(Convert.ToString(value, CultureInfo.InvariantCulture))).Append('"');
        }

        static string Escape(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }

        static string SafeName(string value)
        {
            string safe = new string((value ?? "lab").Where(character => Char.IsLetterOrDigit(character) ||
                character == '-' || character == '_').Take(40).ToArray());
            return safe.Length == 0 ? "lab" : safe;
        }

        static string Id(Character actor) { return actor == null ? "" : actor.GetZDOID().ToString(); }
        static bool Wet(Character actor)
        {
            return actor != null && actor.GetSEMan() != null && actor.GetSEMan().HaveStatusEffect("Wet".GetStableHashCode());
        }
        static string Item(FieldInfo field, Humanoid human)
        {
            return human == null ? "" : Item(field.GetValue(human) as ItemDrop.ItemData);
        }
        static string Item(ItemDrop.ItemData item)
        {
            return item == null || item.m_dropPrefab == null ? "" : item.m_dropPrefab.name;
        }
        static string NpcBuild()
        {
            PluginInfo info;
            return Chainloader.PluginInfos.TryGetValue("local.livingworld.humannpc", out info) ?
                info.Metadata.Version.ToString() : "missing";
        }

        void OnDestroy()
        {
            if (active) StopSession("plugin-destroyed");
        }
    }
}

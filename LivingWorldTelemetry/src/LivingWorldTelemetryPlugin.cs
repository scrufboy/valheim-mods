using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using LivingWorld.HumanNpc;
using UnityEngine;

namespace LivingWorld.Telemetry
{
    [BepInPlugin(Guid, "Living World Lab Telemetry", Version)]
    [BepInDependency("local.livingworld.humannpc", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class LivingWorldTelemetryPlugin : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.telemetry";
        public const string Version = "0.2.2";
        const int Schema = 2;
        const long MaxBytes = 64L * 1024L * 1024L;
        const int MaxRecords = 200000;
        const int MaxRecordsPerSecond = 50;
        const float SampleSeconds = .5f;
        static readonly BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly FieldInfo Left = typeof(Humanoid).GetField("m_leftItem", Any);
        static readonly FieldInfo Right = typeof(Humanoid).GetField("m_rightItem", Any);
        static readonly FieldInfo HiddenLeft = typeof(Humanoid).GetField("m_hiddenLeftItem", Any);
        static readonly FieldInfo HiddenRight = typeof(Humanoid).GetField("m_hiddenRightItem", Any);
        static readonly FieldInfo ClericPatient = typeof(HumanCleric).GetField("patient", Any);
        static readonly FieldInfo BlockTimer = typeof(Humanoid).GetField("m_blockTimer", Any);
        static readonly MethodInfo CurrentBlocker = typeof(Humanoid).GetMethod("GetCurrentBlocker", Any, null, Type.EmptyTypes, null);
        static LivingWorldTelemetryPlugin current;
        [ThreadStatic] static Character healthTarget;
        [ThreadStatic] static float healthBefore;
        readonly Dictionary<string, string> states = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, float> lastHealth = new Dictionary<string, float>(StringComparer.Ordinal);
        Harmony harmony;
        StreamWriter writer;
        string sessionId, label, outputPath;
        float startedRealtime, nextSample, nextFlush;
        int recordCount, secondBucket, recordsThisSecond, dropped;
        int segment;
        long bytes;
        bool active, fault, sawWorld;

        void Awake()
        {
            current = this;
            harmony = new Harmony(Guid);
            InstallPatches();
            Logger.LogInfo("Automatic telemetry armed. Recording starts when a world is ready and stops on world exit.");
        }

        void InstallPatches()
        {
            if (Left == null || Right == null || HiddenLeft == null || HiddenRight == null || ClericPatient == null || BlockTimer == null || CurrentBlocker == null)
                throw new MissingMemberException("Required observed NPC fields changed");
            Patch(typeof(MonsterAI).GetMethod("SelectBestAttack", Any, null,
                new[] { typeof(Humanoid), typeof(float) }, null), null, "WeaponSelectionPostfix");
            Patch(typeof(HumanCleric).GetMethod("Cancel", Any), "ClericCancelPrefix", null);
            Patch(typeof(Character).GetMethod("RPC_Damage", Any, null, new[] { typeof(long), typeof(HitData) }, null), "DamagePrefix", "DamagePostfix");
            Patch(typeof(Character).GetMethod("Heal", Any, null, new[] { typeof(float), typeof(bool) }, null), "HealPrefix", "HealPostfix");
            Patch(typeof(Humanoid).GetMethod("StartAttack", Any, null, new[] { typeof(Character), typeof(bool) }, null), "AttackStartPrefix", "AttackStartPostfix");
            Patch(typeof(Humanoid).GetMethod("OnAttackTrigger", Any, null, Type.EmptyTypes, null), "AttackTriggerPrefix", null);
            Patch(typeof(Humanoid).GetMethod("BlockAttack", Any, null, new[] { typeof(HitData), typeof(Character) }, null), "BlockPrefix", "BlockPostfix");
        }

        void Patch(MethodInfo method, string prefix, string postfix)
        {
            if (method == null) throw new MissingMethodException("Telemetry observation boundary changed");
            var type = typeof(LivingWorldTelemetryPlugin);
            harmony.Patch(method,
                prefix == null ? null : new HarmonyMethod(type.GetMethod(prefix, Any)),
                postfix == null ? null : new HarmonyMethod(type.GetMethod(postfix, Any)));
        }

        void StartSession()
        {
            if (active) return;
            if (ZNet.instance == null || Player.m_localPlayer == null || Game.instance == null)
                return;
            label = SafeName(ZNet.instance.GetWorldName());
            sessionId = System.Guid.NewGuid().ToString("N");
            string directory = Path.Combine(Paths.BepInExRootPath, "LivingWorldTelemetry");
            Directory.CreateDirectory(directory);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            outputPath = Path.Combine(directory, "LWC-" + stamp + "-" + label + "-part" + segment.ToString("D2", CultureInfo.InvariantCulture) + ".jsonl");
            writer = new StreamWriter(new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            writer.AutoFlush = false;
            startedRealtime = Time.realtimeSinceStartup; nextSample = startedRealtime; nextFlush = startedRealtime + 2f;
            recordCount = secondBucket = recordsThisSecond = dropped = 0; bytes = 0;
            states.Clear(); lastHealth.Clear(); active = true;
            Emit("RunStart", null,
                "telemetryVersion", Version, "label", label, "world", ZNet.instance.GetWorldName(),
                "profile", Game.instance.GetPlayerProfile().GetFilename(), "player", Player.m_localPlayer.GetPlayerName(),
                "npcBuild", NpcBuild(), "sampleSeconds", SampleSeconds, "maxBytes", MaxBytes, "maxRecords", MaxRecords);
            Sample(true);
            writer.Flush();
            Logger.LogInfo("Automatic telemetry recording: " + outputPath);
        }

        void StopSession(string reason)
        {
            if (!active) return;
            try
            {
                Emit("RunSummary", null, "reason", reason, "records", recordCount, "dropped", dropped,
                    "bytes", bytes, "durationSeconds", Time.realtimeSinceStartup - startedRealtime);
                writer.Flush();
            }
            finally
            {
                writer.Dispose(); writer = null; active = false;
            }
            Logger.LogInfo("Automatic telemetry closed: records=" + recordCount + "; dropped=" + dropped + "; file=" + outputPath);
        }

        void Update()
        {
            bool ready = ZNet.instance != null && Player.m_localPlayer != null && Game.instance != null;
            if (!ready)
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
                if (recordCount >= MaxRecords || bytes >= MaxBytes)
                {
                    StopSession("automatic-file-rotation"); segment++; StartSession(); return;
                }
                if (now >= nextSample) { nextSample = now + SampleSeconds; Sample(false); }
                if (now >= nextFlush) { nextFlush = now + 2f; writer.Flush(); }
            }
            catch (Exception error)
            {
                fault = true;
                try { Emit("RecorderFault", null, "error", error.Message); writer.Flush(); } catch { }
                Logger.LogError(error);
                Logger.LogError("Telemetry stopped writing after error: " + error.Message);
            }
        }

        void Sample(bool initial)
        {
            var actors = Character.GetAllCharacters().Where(c => c != null && c.GetComponent<HumanCompanion>() != null).ToArray();
            foreach (var actor in actors) SampleActor(actor, actors, initial);
            if (Player.m_localPlayer != null)
                EmitChanged("player.environment", "Observation", Id(Player.m_localPlayer),
                    "wet=" + Wet(Player.m_localPlayer), "wet", Wet(Player.m_localPlayer), "health", Player.m_localPlayer.GetHealth());
        }

        void SampleActor(Character actor, Character[] party, bool initial)
        {
            string id = Id(actor);
            var human = actor as Humanoid;
            var companion = actor.GetComponent<HumanCompanion>();
            var equipment = actor.GetComponent<HumanPartyEquipment>();
            var cleric = actor.GetComponent<HumanCleric>();
            var door = actor.GetComponent<HumanDoorTraversal>();
            var presence = actor.GetComponent<HumanFollowPresence>();
            var ambient = actor.GetComponent<HumanCompanionAmbient>();
            var posture = actor.GetComponent<HumanCompanionPosture>();
            var target = companion.TargetCreature;
            var leader = Player.m_localPlayer;
            Vector3 offset = leader == null ? Vector3.zero : actor.transform.position - leader.transform.position;
            float side = leader == null ? 0 : Vector3.Dot(offset, leader.transform.right);
            float forward = leader == null ? 0 : Vector3.Dot(offset, leader.transform.forward);
            float nearest = Single.PositiveInfinity;
            foreach (var other in party) if (other != actor) nearest = Mathf.Min(nearest, Vector3.Distance(actor.transform.position, other.transform.position));
            int arrows = human == null ? 0 : human.GetInventory().GetAllItems().Where(i => i.m_dropPrefab != null && i.m_dropPrefab.name == "ArrowWood").Sum(i => i.m_stack);
            string role = IsCleric(actor) ? "cleric" : equipment != null && equipment.Ready ? "archer" : "companion";
            string left = Item(Left, human), right = Item(Right, human), hiddenLeft = Item(HiddenLeft, human), hiddenRight = Item(HiddenRight, human);
            string targetId = target == null ? "" : Id(target);
            string state = String.Join("|", new[] { role, Wet(actor).ToString(), left, right, hiddenLeft, hiddenRight,
                companion.HasAuthorizedCombat.ToString(), targetId, door.Status, posture.Posture.ToString(), cleric.Casting.ToString() });
            EmitChanged("actor." + id, "ActorState", id, state,
                "initial", initial, "name", human == null ? actor.name : human.m_name, "role", role,
                "health", actor.GetHealth(), "maxHealth", actor.GetMaxHealth(), "wet", Wet(actor),
                "left", left, "right", right, "stowedLeft", hiddenLeft, "stowedRight", hiddenRight,
                "weapon", human == null ? "" : Item(human.GetCurrentWeapon()), "arrows", arrows,
                "combatAuthorized", companion.HasAuthorizedCombat, "target", targetId,
                "door", door.Status, "posture", posture.Posture.ToString(), "presence", presence.Status,
                "ambient", ambient.Status, "equipment", equipment.Describe(), "cleric", cleric.Describe());
            Vector3 position = actor.transform.position, velocity = actor.GetVelocity();
            Emit("PositionSample", id,
                "x", position.x, "y", position.y, "z", position.z, "vx", velocity.x, "vy", velocity.y, "vz", velocity.z,
                "leaderSide", side, "leaderForward", forward, "leaderDistance", leader == null ? -1 : offset.magnitude,
                "nearestCompanion", Single.IsPositiveInfinity(nearest) ? -1 : nearest, "targetDistance", target == null ? -1 : Vector3.Distance(position, target.transform.position));
            float prior;
            if (lastHealth.TryGetValue(id, out prior) && Math.Abs(prior - actor.GetHealth()) > .001f)
                Emit("HealthChangeObserved", id, "before", prior, "after", actor.GetHealth(), "source", "unknown-or-native-hook-missed");
            lastHealth[id] = actor.GetHealth();
            if (cleric != null && IsCleric(actor)) SampleHealAssessment(actor, cleric, party);
        }

        void SampleHealAssessment(Character caster, HumanCleric cleric, Character[] party)
        {
            foreach (var candidate in party.Concat(new Character[] { Player.m_localPlayer }).Where(c => c != null).Distinct())
            {
                var view = candidate.GetComponent<ZNetView>();
                float distance = Vector3.Distance(caster.transform.position, candidate.transform.position);
                bool visible = candidate == caster || !Physics.Linecast(caster.GetCenterPoint(), candidate.GetCenterPoint(),
                    LayerMask.GetMask("Default", "static_solid", "piece", "terrain"), QueryTriggerInteraction.Ignore);
                bool eligible = !candidate.IsDead() && view != null && view.IsValid() && view.IsOwner() && visible && distance <= 10f &&
                    candidate.GetHealth() > 0 && candidate.GetHealth() < candidate.GetMaxHealth() * .85f;
                string key = Id(caster) + ".heal." + Id(candidate);
                string state = eligible + "|" + visible + "|" + (distance <= 10f) + "|" + (candidate.GetHealth() < candidate.GetMaxHealth() * .85f);
                EmitChanged(key, "HealAssessment", Id(caster), state,
                    "patient", Id(candidate), "eligible", eligible, "visible", visible, "distance", distance,
                    "health", candidate.GetHealth(), "maxHealth", candidate.GetMaxHealth(), "owned", view != null && view.IsValid() && view.IsOwner());
            }
            var patient = ClericPatient.GetValue(cleric) as Character;
            EmitChanged(Id(caster) + ".cast", "HealPhase", Id(caster), patient == null ? "idle" : "casting:" + Id(patient),
                "phase", patient == null ? "idle" : "casting", "patient", patient == null ? "" : Id(patient));
        }

        void EmitChanged(string key, string category, string actor, string fingerprint, params object[] fields)
        {
            string prior;
            if (states.TryGetValue(key, out prior) && prior == fingerprint) return;
            states[key] = fingerprint; Emit(category, actor, fields);
        }

        void Emit(string category, string actor, params object[] fields)
        {
            bool final = category == "RunSummary" || category == "RecorderFault";
            if (!active || writer == null || fault || (!final && (recordCount >= MaxRecords || bytes >= MaxBytes))) { dropped++; return; }
            int second = Mathf.FloorToInt(Time.realtimeSinceStartup - startedRealtime);
            if (second != secondBucket) { secondBucket = second; recordsThisSecond = 0; }
            if (!final && recordsThisSecond >= MaxRecordsPerSecond) { dropped++; return; }
            recordsThisSecond++;
            var text = new StringBuilder(512);
            text.Append('{'); Add(text, "schema", Schema, false); Add(text, "session", sessionId, true);
            Add(text, "category", category, true); Add(text, "wallUtc", DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture), true);
            Add(text, "elapsed", Time.realtimeSinceStartup - startedRealtime, true);
            if (!String.IsNullOrEmpty(actor)) Add(text, "actor", actor, true);
            for (int i = 0; fields != null && i + 1 < fields.Length; i += 2) Add(text, Convert.ToString(fields[i], CultureInfo.InvariantCulture), fields[i + 1], true);
            text.Append('}'); string line = text.ToString(); writer.WriteLine(line);
            bytes += Encoding.UTF8.GetByteCount(line) + 1; recordCount++;
        }

        static void Add(StringBuilder text, string key, object value, bool comma)
        {
            if (comma) text.Append(','); text.Append('"').Append(Escape(key)).Append("\":");
            if (value == null) { text.Append("null"); return; }
            if (value is bool) { text.Append((bool)value ? "true" : "false"); return; }
            if (value is float) { text.Append(((float)value).ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is double) { text.Append(((double)value).ToString("R", CultureInfo.InvariantCulture)); return; }
            if (value is int || value is long) { text.Append(Convert.ToString(value, CultureInfo.InvariantCulture)); return; }
            text.Append('"').Append(Escape(Convert.ToString(value, CultureInfo.InvariantCulture))).Append('"');
        }

        static string Escape(string value)
        { return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n"); }
        static string SafeName(string value)
        {
            var safe = new string((value ?? "lab").Where(c => Char.IsLetterOrDigit(c) || c == '-' || c == '_').Take(40).ToArray());
            return safe.Length == 0 ? "lab" : safe;
        }
        static string Id(Character actor) { return actor == null ? "" : actor.GetZDOID().ToString(); }
        static bool Wet(Character actor) { return actor != null && actor.GetSEMan() != null && actor.GetSEMan().HaveStatusEffect("Wet".GetStableHashCode()); }
        static bool IsCleric(Character actor)
        {
            var view = actor == null ? null : actor.GetComponent<ZNetView>();
            return view != null && view.IsValid() && view.GetZDO().GetString("lwc_cleric_v1", "") == "1";
        }
        static string Item(FieldInfo field, Humanoid human) { return human == null ? "" : Item(field.GetValue(human) as ItemDrop.ItemData); }
        static string Item(ItemDrop.ItemData item) { return item == null || item.m_dropPrefab == null ? "" : item.m_dropPrefab.name; }
        static ItemDrop.ItemData Blocker(Humanoid human) { return human == null ? null : CurrentBlocker.Invoke(human, null) as ItemDrop.ItemData; }
        static bool Tracked(Character actor) { return actor != null && (actor == Player.m_localPlayer || actor.GetComponent<HumanCompanion>() != null); }
        static string NpcBuild()
        {
            PluginInfo info;
            return Chainloader.PluginInfos.TryGetValue("local.livingworld.humannpc", out info) ? info.Metadata.Version.ToString() : "missing";
        }
        static void WeaponSelectionPostfix(MonsterAI __instance, ItemDrop.ItemData __result)
        {
            if (current == null || !current.active || __instance == null ||
                __instance.GetComponent<HumanCompanion>() == null) return;
            string id = Id(__instance.GetComponent<Character>());
            current.EmitChanged("weapon.selection." + id, "WeaponSelection", id, Item(__result),
                "weapon", Item(__result));
        }
        static void ClericCancelPrefix(HumanCleric __instance, string why)
        {
            if (current != null && current.active) current.Emit("HealPhase", Id(__instance.GetComponent<Character>()), "phase", "cancel", "reason", why);
        }
        static void DamagePrefix(Character __instance, long sender, HitData hit)
        {
            healthTarget = null;
            if (current == null || !current.active || !Tracked(__instance)) return;
            healthTarget = __instance; healthBefore = __instance.GetHealth();
        }
        static void DamagePostfix(Character __instance, long sender, HitData hit)
        {
            if (current == null || !current.active || healthTarget != __instance) return;
            float after = __instance.GetHealth(), before = healthBefore; healthTarget = null;
            if (before <= after) return;
            var attacker = hit == null ? null : hit.GetAttacker();
            current.Emit("Damage", Id(__instance), "before", before, "after", after, "amount", before - after,
                "raw", hit == null ? 0 : hit.GetTotalDamage(), "attacker", attacker == null ? "unknown" : Id(attacker));
        }
        static void HealPrefix(Character __instance, float hp, bool showText)
        {
            healthTarget = null;
            if (current == null || !current.active || !Tracked(__instance)) return;
            healthTarget = __instance; healthBefore = __instance.GetHealth();
        }
        static void HealPostfix(Character __instance, float hp, bool showText)
        {
            if (current == null || !current.active || healthTarget != __instance) return;
            float after = __instance.GetHealth(), before = healthBefore; healthTarget = null;
            if (after > before) current.Emit("HealthGainObserved", Id(__instance), "source", "Character.Heal", "before", before, "after", after, "amount", after - before);
        }
        static void AttackStartPrefix(Humanoid __instance, Character target, bool secondaryAttack)
        {
            if (current == null || !current.active || __instance.GetComponent<HumanCompanion>() == null) return;
            current.Emit("AttackAttempt", Id(__instance), "phase", "requested", "target", Id(target),
                "weapon", Item(__instance.GetCurrentWeapon()), "secondary", secondaryAttack, "blocking", __instance.IsBlocking());
        }
        static void AttackStartPostfix(Humanoid __instance, Character target, bool secondaryAttack, bool __result)
        {
            if (current == null || !current.active || __instance.GetComponent<HumanCompanion>() == null) return;
            current.Emit("AttackAttempt", Id(__instance), "phase", "result", "target", Id(target),
                "weapon", Item(__instance.GetCurrentWeapon()), "started", __result);
        }
        static void AttackTriggerPrefix(Humanoid __instance)
        {
            if (current == null || !current.active || !Tracked(__instance)) return;
            current.Emit("AttackTrigger", Id(__instance), "weapon", Item(__instance.GetCurrentWeapon()));
        }
        static void BlockPrefix(Humanoid __instance, HitData hit, Character attacker, out float __state)
        {
            __state = -1f;
            if (current == null || !current.active || !Tracked(__instance)) return;
            __state = (float)BlockTimer.GetValue(__instance);
            current.Emit("BlockAttempt", Id(__instance), "phase", "impact", "attacker", Id(attacker),
                "blockTimer", __state, "blocker", Item(Blocker(__instance)), "isBlocking", __instance.IsBlocking(),
                "rawDamage", hit == null ? 0f : hit.GetTotalDamage());
        }
        static void BlockPostfix(Humanoid __instance, HitData hit, Character attacker, float __state, bool __result)
        {
            if (current == null || !current.active || !Tracked(__instance)) return;
            var blocker = Blocker(__instance);
            bool timedWindow = blocker != null && blocker.m_shared != null && blocker.m_shared.m_timedBlockBonus > 1f && __state >= 0f && __state < .25f;
            current.Emit("BlockResult", Id(__instance), "attacker", Id(attacker), "success", __result,
                "timedWindow", timedWindow, "blockTimer", __state, "health", __instance.GetHealth());
        }
        void OnDestroy()
        {
            if (active) StopSession("plugin-destroyed");
            if (harmony != null) harmony.UnpatchSelf();
            if (current == this) current = null;
        }
    }
}

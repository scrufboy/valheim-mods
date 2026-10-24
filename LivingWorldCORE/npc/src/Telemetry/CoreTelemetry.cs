using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using LivingWorld.Core;
using UnityEngine;

namespace LivingWorld.Telemetry
{
    /// <summary>
    /// Read-only session recorder. Writes JSONL under BepInEx/LivingWorldTelemetry.
    /// It reads public kernel state only and holds no domain, so it cannot change
    /// what an NPC does. tools/analyze_session.py turns a file into a pass/fail report.
    /// </summary>
    [BepInPlugin(Guid, "Living World C.O.R.E. Telemetry", Version)]
    [BepInDependency(CorePlugin.Guid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class CoreTelemetry : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.telemetry";
        public const string Version = CorePlugin.Version;
        const int Schema = 11; // 1.0.1 adds fields only; schema-10 readers still parse it
        const float SampleSeconds = .5f;
        const long MaxBytes = 64L * 1024L * 1024L;

        readonly Dictionary<NpcKernel, NpcPhase> lastPhase = new Dictionary<NpcKernel, NpcPhase>();
        readonly Dictionary<NpcKernel, int> lastViolations = new Dictionary<NpcKernel, int>();
        readonly Dictionary<NpcKernel, bool> lastTorch = new Dictionary<NpcKernel, bool>();
        readonly StringBuilder line = new StringBuilder(1024);
        StreamWriter writer;
        float started, nextSample, nextFlush;
        long bytes;
        int records, segment;
        bool active, fault, sawWorld;

        void Update()
        {
            bool world = ZNet.instance != null && Player.m_localPlayer != null && Game.instance != null;
            if (!world)
            {
                if (active) Stop("world-exit");
                if (sawWorld) { segment = 0; sawWorld = false; fault = false; }
                return;
            }
            sawWorld = true;
            if (fault) return;
            try
            {
                if (!active) { BeginSession(); return; }
                ObserveEdges();
                float now = Time.realtimeSinceStartup;
                if (now >= nextSample) { nextSample = now + SampleSeconds; Sample(); }
                if (now >= nextFlush) { nextFlush = now + 2f; writer.Flush(); }
                if (bytes >= MaxBytes) { Stop("rotation"); segment++; }
            }
            catch (Exception error)
            {
                fault = true;
                Logger.LogError("Telemetry stopped (gameplay unaffected): " + error);
                try { if (writer != null) { writer.Flush(); writer.Dispose(); } } catch { }
                writer = null;
                active = false;
            }
        }

        // Not named Start: Unity calls a MonoBehaviour's Start() in the main menu (1.0.0 bug).
        void BeginSession()
        {
            string directory = Path.Combine(Paths.BepInExRootPath, "LivingWorldTelemetry");
            Directory.CreateDirectory(directory);
            string stamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string path = Path.Combine(directory, "LWC-" + stamp + "-" + Safe(ZNet.instance.GetWorldName()) + "-part" +
                segment.ToString("D2", CultureInfo.InvariantCulture) + ".jsonl");
            writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read), new UTF8Encoding(false));
            started = Time.realtimeSinceStartup;
            nextSample = started;
            nextFlush = started + 2f;
            bytes = 0;
            records = 0;
            lastPhase.Clear();
            lastViolations.Clear();
            lastTorch.Clear();
            lastBlock.Clear();
            lastSwitches.Clear();
            active = true;
            if (!subscribed) { NpcKernel.Removed += OnRemoved; subscribed = true; }
            Begin("RunStart");
            Str("core", CorePlugin.Version); Num("schema", Schema); Str("world", ZNet.instance.GetWorldName());
            Str("player", Player.m_localPlayer.GetPlayerName()); Bool("registered", CorePlugin.Registered);
            End();
            Logger.LogInfo("Recording read-only telemetry: " + path);
        }

        void Stop(string reason)
        {
            if (writer != null)
            {
                Begin("RunStop"); Str("reason", reason); Num("records", records); End();
                writer.Flush();
                writer.Dispose();
            }
            writer = null;
            active = false;
        }

        void ObserveEdges()
        {
            foreach (NpcKernel kernel in NpcKernel.All)
            {
                if (kernel == null) continue;
                NpcPhase phase = kernel.Frame.Phase;
                NpcPhase previous;
                if (!lastPhase.TryGetValue(kernel, out previous) || previous != phase)
                {
                    lastPhase[kernel] = phase;
                    Begin("Phase"); Actor(kernel); Str("from", previous.ToString()); Str("to", phase.ToString());
                    Str("reason", kernel.Arbiter.Reason); End();
                }
                int violations;
                if (!lastViolations.TryGetValue(kernel, out violations)) violations = 0;
                if (kernel.DomainViolations != violations)
                {
                    lastViolations[kernel] = kernel.DomainViolations;
                    Begin("DomainViolation"); Actor(kernel); Num("count", kernel.DomainViolations);
                    Str("last", kernel.LastViolation); End();
                }
                NativeCombatBehavior combat = Combat(kernel);
                bool blockUp = combat != null && combat.Block != null && combat.Block.Raised;
                bool wasUp;
                bool known = lastBlock.TryGetValue(kernel, out wasUp);
                if (!known || wasUp != blockUp)
                {
                    lastBlock[kernel] = blockUp;
                    if (known || blockUp)
                    {
                        Begin(blockUp ? "BlockRaise" : "BlockLower"); Actor(kernel);
                        Str("reason", combat == null || combat.Block == null ? "" : combat.Block.Reason);
                        Str("threat", combat == null ? "" : combat.BlockThreat);
                        Num("hp", kernel.Body.Human.GetHealth());
                        End();
                    }
                }
                int switches = combat == null ? 0 : combat.TargetSwitches, seenSwitches;
                if (lastSwitches.TryGetValue(kernel, out seenSwitches) && switches > seenSwitches)
                {
                    Character t = kernel.Body.Ai.GetTargetCreature();
                    Begin("TargetChoice"); Actor(kernel);
                    Str("reason", combat.TargetReason);
                    Str("target", t == null ? "" : t.GetHoverName());
                    Num("dist", t == null ? -1f : Vector3.Distance(kernel.transform.position, t.transform.position));
                    End();
                }
                lastSwitches[kernel] = switches;
                bool torch = kernel.Body != null && kernel.Body.TorchInHand;
                bool wasTorch;
                if (!lastTorch.TryGetValue(kernel, out wasTorch) || wasTorch != torch)
                {
                    lastTorch[kernel] = torch;
                    Begin("Torch"); Actor(kernel); Bool("lit", torch); Str("phase", phase.ToString()); End();
                }
            }
        }

        void Sample()
        {
            Player leader = Player.m_localPlayer;
            Vector3 lp = leader.transform.position;
            Begin("Leader");
            Num("x", lp.x); Num("z", lp.z); Bool("crouch", leader.IsCrouching()); Bool("sit", leader.IsSitting());
            Bool("running", leader.IsRunning()); Num("hp", leader.GetHealth());
            End();

            foreach (NpcKernel kernel in NpcKernel.All)
            {
                if (kernel == null || kernel.Body == null) continue;
                NpcBody body = kernel.Body;
                NpcSenses s = kernel.Frame.Senses;
                Vector3 p = kernel.transform.position;
                NpcWetStatus wet = kernel.GetComponent<NpcWetStatus>();
                Begin("Npc"); Actor(kernel);
                Num("x", p.x); Num("z", p.z);
                Str("phase", kernel.Frame.Phase.ToString()); Str("reason", kernel.Arbiter.Reason);
                Str("move", kernel.OwnerName(NpcDomain.Movement)); Str("hands", kernel.OwnerName(NpcDomain.Hands));
                Str("look", kernel.OwnerName(NpcDomain.Look)); Str("pose", kernel.OwnerName(NpcDomain.Posture));
                Num("leaderDist", s.LeaderDistance); Num("partyDist", s.NearestPartyDistance);
                Str("target", s.Target == null ? "" : s.Target.GetHoverName());
                Num("targetDist", s.Target == null ? -1f : Vector3.Distance(p, s.Target.transform.position));
                Bool("alerted", s.NativeAlert); Bool("staticTarget", s.HasStaticTarget);
                Num("sinceHit", Mathf.Min(s.SecondsSinceDamaged, 9999f));
                Str("hostile", s.NearestAlertedHostile == null ? "" : s.NearestAlertedHostile.GetHoverName());
                Num("hp", body.Human.GetHealth());
                Str("handL", NpcBody.ItemName(body.LeftItem)); Str("handR", NpcBody.ItemName(body.RightItem));
                Str("backL", NpcBody.ItemName(body.BackLeft)); Str("backR", NpcBody.ItemName(body.BackRight));
                Bool("torch", body.TorchInHand); Bool("torchWanted", kernel.Frame.Board.TorchWanted);
                Bool("dark", s.Dark); Bool("builtLight", s.NearBuiltLight);
                Bool("crouch", body.Crouching); Bool("sit", body.Sitting); Bool("swim", s.Swimming);
                Bool("wet", wet != null && wet.Wet);
                Num("slot", kernel.Frame.Board.FormationSlot); Num("leaseDist", kernel.Frame.Board.FormationDistance);
                Num("releases", body.EncounterReleases); Num("violations", kernel.DomainViolations); Num("faults", kernel.Faults);
                Num("clearQuiet", kernel.Arbiter.QuietSeconds);
                Bool("attacking", body.Human.InAttack()); Bool("drawingBow", body.Human.IsDrawingBow());
                Bool("blocking", body.Human.IsBlocking()); Num("ammo", body.AmmoCount);
                Bool("shieldInHand", body.ShieldInHand); Num("blockRaises", body.BlockRaises);
                NativeCombatBehavior combat = Combat(kernel);
                Str("block", combat == null || combat.Block == null ? "" : combat.Block.Reason);
                Str("blockThreat", combat == null ? "" : combat.BlockThreat);
                Str("targetReason", combat == null ? "" : combat.TargetReason ?? "");
                Num("targetSwitches", combat == null ? 0 : combat.TargetSwitches);
                Bool("targetRanged", s.Target != null && TargetIsRanged(s.Target));
                Str("targetsWho", s.Target == null ? "" : TargetOf(s.Target, kernel));
                End();
            }
        }

        static bool TargetIsRanged(Character c)
        {
            Humanoid h = c as Humanoid;
            ItemDrop.ItemData w = h == null ? null : h.GetCurrentWeapon();
            return w != null && w.m_shared != null && w.m_shared.m_attack != null &&
                w.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile;
        }

        static string TargetOf(Character enemy, NpcKernel kernel)
        {
            MonsterAI m = enemy.GetBaseAI() as MonsterAI;
            Character t = m == null ? null : m.GetTargetCreature();
            if (t == null) return "none";
            if (ReferenceEquals(t, kernel.Body.Human)) return "me";
            if (t is Player) return "leader";
            return t.GetComponent<NpcKernel>() != null ? "partner" : "other";
        }

        bool subscribed;
        readonly Dictionary<NpcKernel, bool> lastBlock = new Dictionary<NpcKernel, bool>();
        readonly Dictionary<NpcKernel, int> lastSwitches = new Dictionary<NpcKernel, int>();

        static NativeCombatBehavior Combat(NpcKernel kernel)
        {
            foreach (NpcBehavior b in kernel.Behaviors) { NativeCombatBehavior c = b as NativeCombatBehavior; if (c != null) return c; }
            return null;
        }

        void OnRemoved(NpcKernel kernel, string reason)
        {
            if (!active || writer == null || kernel == null) return;
            Character c = kernel.GetComponent<Character>();
            Begin("Removed"); Actor(kernel); Str("reason", reason); Str("phase", kernel.Frame.Phase.ToString());
            Num("hp", c == null ? 0f : c.GetHealth()); Str("lastAttacker", kernel.LastAttackerName ?? "");
            End();
            writer.Flush();
        }

        void Actor(NpcKernel kernel)
        {
            Character c = kernel.GetComponent<Character>();
            Str("npc", c == null ? kernel.name : c.m_name);
            NpcAppearance a = kernel.GetComponent<NpcAppearance>();
            Str("role", a == null ? "" : a.Role);
        }

        void Begin(string type)
        {
            line.Length = 0;
            line.Append("{\"t\":").Append((Time.realtimeSinceStartup - started).ToString("0.000", CultureInfo.InvariantCulture));
            line.Append(",\"type\":\"").Append(type).Append('"');
        }

        void End()
        {
            line.Append('}');
            string text = line.ToString();
            writer.WriteLine(text);
            bytes += text.Length + 1;
            records++;
        }

        void Str(string key, string value)
        {
            line.Append(",\"").Append(key).Append("\":\"");
            foreach (char c in value ?? "")
            {
                if (c == '"' || c == '\\') line.Append('\\').Append(c);
                else if (c < ' ') line.Append(' ');
                else line.Append(c);
            }
            line.Append('"');
        }

        void Num(string key, float value)
        {
            line.Append(",\"").Append(key).Append("\":");
            if (float.IsNaN(value) || float.IsInfinity(value)) line.Append("null");
            else line.Append(value.ToString("0.###", CultureInfo.InvariantCulture));
        }

        void Bool(string key, bool value) { line.Append(",\"").Append(key).Append("\":").Append(value ? "true" : "false"); }

        static string Safe(string value)
        {
            var sb = new StringBuilder();
            foreach (char c in value ?? "world") sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
            return sb.ToString();
        }

        void OnDestroy()
        {
            if (subscribed) NpcKernel.Removed -= OnRemoved;
            if (active) Stop("shutdown");
        }
    }
}

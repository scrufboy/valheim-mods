using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using BepInEx;
using LivingWorld.Core;
using LivingWorld.FormidableAI.Shadow;
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
        const int Schema = 13; // 13: A1 awareness shadow (Contact, Group, Initiative, AwarenessBudget); 12: FormidableShadow
        const float SampleSeconds = .5f;
        const long MaxBytes = 64L * 1024L * 1024L;

        readonly Dictionary<NpcKernel, NpcPhase> lastPhase = new Dictionary<NpcKernel, NpcPhase>();
        readonly Dictionary<NpcKernel, int> lastViolations = new Dictionary<NpcKernel, int>();
        readonly Dictionary<NpcKernel, bool> lastTorch = new Dictionary<NpcKernel, bool>();
        readonly Dictionary<NpcKernel, long> lastFormidableSequence = new Dictionary<NpcKernel, long>();
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
            lastFormidableSequence.Clear();
            lastBlock.Clear();
            lastSwitches.Clear();
            lastFallBack.Clear();
            active = true;
            if (!subscribed)
            {
                NpcKernel.Removed += OnRemoved; NpcKernel.Damaged += OnDamaged;
                CompanionCombatVoice.Spoke += OnSpoke; StrikeHook.Landed += OnStrike; MeleeChainHook.Chained += OnChain; MeleeChainHook.Heavy += OnHeavy; WarCry.Cried += OnWarCry;
                subscribed = true;
            }
            Begin("RunStart");
            Str("core", CorePlugin.Version); Num("schema", Schema); Str("world", ZNet.instance.GetWorldName());
            Str("player", Player.m_localPlayer.GetPlayerName()); Bool("registered", CorePlugin.Registered);
            Bool("cfgShieldBlocking", CorePlugin.ShieldBlocking); Num("cfgFoodStamina", CorePlugin.FoodStamina);
            End();
            Logger.LogInfo("Recording read-only telemetry: " + path);
        }

        void Stop(string reason)
        {
            if (writer != null)
            {
                // Session stop / world exit: publish open initiative timelines (target-only and still-active included) and
                // drain them before the file closes. A size rotation keeps timelines open for the next segment.
                try
                {
                    if (reason != "rotation") AwarenessGroup.FlushAll("session stop: " + reason);
                    ObserveAwareness();
                }
                catch (Exception e) { Logger.LogWarning("awareness flush at stop: " + e.Message); }
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
                bool fb = kernel.FallBack.Active, wasFb;
                if (lastFallBack.TryGetValue(kernel, out wasFb) && wasFb != fb)
                {
                    Begin(fb ? "FallBackStart" : "FallBackEnd"); Actor(kernel);
                    Str("outcome", fb ? "" : kernel.FallBack.LastOutcome);
                    Num("count", kernel.FallBack.Count);
                    Num("hp", kernel.Body.Human.GetHealth());
                    End();
                }
                lastFallBack[kernel] = fb;
                TargetSelector sel = kernel.Targeting;
                int switches = sel == null ? 0 : sel.Picks, seenSwitches;
                if (lastSwitches.TryGetValue(kernel, out seenSwitches) && switches > seenSwitches)
                {
                    Character t = kernel.Body.Ai.GetTargetCreature();
                    Begin("TargetChoice"); Actor(kernel);
                    Str("reason", sel.Reason);
                    Str("target", sel.LastChoiceName);
                    Num("targetId", sel.LastChoiceId);
                    Str("targetPrefab", t == null ? "" : global::Utils.GetPrefabName(t.gameObject));
                    Num("targetLevel", t == null ? 0 : t.GetLevel());
                    Num("dist", t == null ? -1f : Vector3.Distance(kernel.transform.position, t.transform.position));
                    Num("switches", sel.Switches);
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
                Bool("shieldInHand", body.ShieldInHand); Num("blockRaises", body.BlockRaises); Num("chains", body.Chains); Num("heavies", body.Heavies); Num("heavyRolls", body.HeavyRolls); Bool("partnerDefense", kernel.PartnerDefense); Num("stamina", body.Stamina); Num("maxStamina", body.MaxStamina); Num("exhaustions", body.Exhaustions); Str("kit", body.Kit.Length == 0 ? "iron" : body.Kit); Num("kitQ", body.KitQuality);
                NativeCombatBehavior combat = Combat(kernel);
                Str("block", combat == null || combat.Block == null ? "" : combat.Block.Reason);
                Str("blockThreat", combat == null ? "" : combat.BlockThreat);
                Str("targetReason", kernel.Targeting == null ? "" : kernel.Targeting.Reason);
                Num("targetSwitches", kernel.Targeting == null ? 0 : kernel.Targeting.Switches);
                Bool("fallBack", kernel.FallBack.Active); Str("fallBackReason", kernel.FallBack.Reason);
                Bool("targetWildlife", s.Target != null && s.Target.GetFaction() == Character.Faction.AnimalsVeg);
                Bool("targetRanged", s.Target != null && TargetIsRanged(s.Target));
                Str("targetsWho", s.Target == null ? "" : TargetOf(s.Target, kernel));
                Num("targetId", s.Target == null ? 0 : s.Target.GetInstanceID());
                Bool("targetFlying", s.Target != null && s.Target.IsFlying());
                Str("weaponPick", body.LastWeaponPick ?? ""); Num("weaponSwaps", body.WeaponSwaps);
                Num("targetPicks", kernel.Targeting == null ? 0 : kernel.Targeting.Picks);
                End();
                ObserveFormidable(kernel);
            }
            ObserveAwareness();
        }

        // ---- A1 awareness shadow (schema 13): drained from bounded per-party rings, each record written once ----
        readonly Dictionary<long, long> lastContactSeq = new Dictionary<long, long>();
        readonly Dictionary<long, long> lastGroupSeq = new Dictionary<long, long>();
        readonly Dictionary<long, long> lastBudgetSeq = new Dictionary<long, long>();
        readonly ContactTransition[] transitionBuf = new ContactTransition[128];
        readonly Dictionary<long, long> lastInitiativeSeq = new Dictionary<long, long>();
        readonly InitiativeRecord[] initiativeBuf = new InitiativeRecord[32];
        static readonly string[,] MilestoneKeys = {
            { "tEnvelope", "dEnvelope", "ttcEnvelope", "ubEnvelope", "byEnvelope" },
            { "tEvidence", "dEvidence", "ttcEvidence", "ubEvidence", "byEvidence" },
            { "tNoticed", "dNoticed", "ttcNoticed", "ubNoticed", "byNoticed" },
            { "tAlerted", "dAlerted", "ttcAlerted", "ubAlerted", "byAlerted" },
            { "tCommitted", "dCommitted", "ttcCommitted", "ubCommitted", "byCommitted" },
            { "tMemberTarget", "dMemberTarget", "ttcMemberTarget", "ubMemberTarget", "byMemberTarget" },
            { "tResponseReady", "dResponseReady", "ttcResponseReady", "ubResponseReady", "byResponseReady" },
            { "tMemberAction", "dMemberAction", "ttcMemberAction", "ubMemberAction", "byMemberAction" } };


        void ObserveAwareness()
        {
            foreach (AwarenessGroup party in AwarenessGroup.All)
            {
                long seen;
                if (!lastContactSeq.TryGetValue(party.AwarenessGroupId, out seen)) seen = -1;
                long tlost;
                int n = party.Book.TransitionsAfter(seen, transitionBuf, out tlost);
                if (tlost > 0) { Begin("ContactGap"); Str("group", party.Kind); Num("lost", tlost); End(); if (n == 0) seen += tlost; }
                for (int i = 0; i < n; i++)
                {
                    ContactTransition t = transitionBuf[i];
                    Begin("Contact"); Num("gameT", t.Time); Num("id", t.Id); Str("family", t.Family);
                    Str("from", t.From.ToString()); Str("to", t.To.ToString()); Str("source", t.Source.ToString());
                    Str("group", party.Kind); Str("observer", party.SensorName(t.Observer)); Num("dist", t.Distance); Num("closing", t.Closing); Num("ttc", t.TimeToContact);
                    Bool("alertedObserved", t.Alerted); Str("targetsWhom", party.MemberName(t.TargetsWhom)); Str("evidence", t.Evidence);
                    Bool("dark", party.Dark); Str("weather", party.Weather); Str("biome", party.Biome); Num("noticeRange", party.NoticeRange);
                    Str("posture", party.Posture.ToString());
                    End();
                    seen = t.Sequence;
                }
                lastContactSeq[party.AwarenessGroupId] = seen;

                long gseen;
                if (!lastGroupSeq.TryGetValue(party.AwarenessGroupId, out gseen)) gseen = -1;
                long gstart = Math.Max(gseen + 1, party.GroupSequence - party.GroupRing.Length);
                for (long q = gstart; q < party.GroupSequence; q++)
                {
                    GroupSnapshot g = party.GroupRing[q % party.GroupRing.Length];
                    Begin("Group"); Num("gameT", g.Time); Num("id", g.Id); Num("members", g.Members); Num("alerted", g.Alerted);
                    Num("committedMembers", g.CommittedMembers); Bool("committed", g.Committed); Num("dist", g.Distance);
                    Num("closing", g.Closing); Num("ttc", g.TimeToContact); Num("radius", g.Radius);
                    Num("poisonKnownSusceptible", g.SusceptibleKnown); Num("poisonKnownImmune", g.ImmuneKnown); Num("poisonUnknown", g.Unknown);
                    End();
                    gseen = q;
                }
                lastGroupSeq[party.AwarenessGroupId] = gseen;

                long iseen;
                if (!lastInitiativeSeq.TryGetValue(party.AwarenessGroupId, out iseen)) iseen = -1;
                long ilost;
                int ni = party.Initiative.After(iseen, initiativeBuf, out ilost);
                if (ilost > 0) { Begin("InitiativeGap"); Str("group", party.Kind); Num("lost", ilost); End(); }
                for (int i = 0; i < ni; i++)
                {
                    InitiativeRecord rec = initiativeBuf[i];
                    Begin("Initiative"); Str("group", party.Kind); Num("seq", rec.Sequence); Num("id", rec.Id); Str("family", rec.Family); Str("outcome", rec.Outcome);
                    for (int m = 0; m < InitiativeRecord.Count; m++)
                    {
                        MilestoneMark mk = rec.Marks[m];
                        if (!mk.Set) continue;
                        Num(MilestoneKeys[m, 0], mk.Time); Num(MilestoneKeys[m, 1], mk.Distance); Num(MilestoneKeys[m, 2], mk.TimeToContact);
                        if (mk.UpperBound) Bool(MilestoneKeys[m, 3], true);
                        if (!string.IsNullOrEmpty(mk.By)) Str(MilestoneKeys[m, 4], mk.By);
                    }
                    End();
                    iseen = rec.Sequence;
                }
                if (ilost > 0 && ni == 0) iseen += ilost;
                lastInitiativeSeq[party.AwarenessGroupId] = iseen;

                long bseen;
                if (!lastBudgetSeq.TryGetValue(party.AwarenessGroupId, out bseen)) bseen = 0;
                if (party.BudgetSequence != bseen)
                {
                    lastBudgetSeq[party.AwarenessGroupId] = party.BudgetSequence;
                    Begin("AwarenessBudget"); Num("contacts", party.Book.Count); Num("groups", party.Groups.Count);
                    Num("raysPerSec", party.RaysPerSecond); Num("raysDeferred", party.RaysDeferred);
                    for (int o = 0; o < party.SensorCount; o++) Num("rays_" + o, party.RaysBy(o));
                    Num("observers", party.SensorCount); Num("members", party.MemberCount); Str("group", party.Kind); Num("cpuMsPerSec", party.CpuMsPerSecond); Num("allocKbPerSec", party.AllocKbPerSecond);
                    Num("stepsPerSec", party.StepsPerSecond); Num("evicted", party.Book.Evicted); Num("refused", party.Book.Refused);
                    Num("readerFormidable", party.ReaderFormidable); Num("readerMonsterAI", party.ReaderMonster);
                    Num("readerBaseAI", party.ReaderBaseAI); Num("readerVisibleOnly", party.ReaderVisible);
                    Num("initiativeOpen", party.Initiative.Open); Num("initiativePublished", party.Initiative.Published); Num("initiativeEvicted", party.Initiative.Evicted);
                    Num("candidatesDropped", party.CandidatesDropped); Num("groupOverflow", party.Groups.Overflow); Num("faults", party.Faults);
                    Str("posture", party.Posture.ToString()); Num("noticeRange", party.NoticeRange); Bool("dark", party.Dark);
                    Str("weather", party.Weather); Str("biome", party.Biome);
                    End();
                }
            }
        }

        void ObserveFormidable(NpcKernel kernel)
        {
            FormidableShadowBehavior observer = null;
            foreach (NpcBehavior behavior in kernel.Behaviors)
            {
                observer = behavior as FormidableShadowBehavior;
                if (observer != null) break;
            }
            if (observer == null || !observer.Active) return;

            long seen;
            if (!lastFormidableSequence.TryGetValue(kernel, out seen)) seen = -1;
            IList<ShadowTraceEntry> entries = observer.Snapshot();
            for (int i = 0; i < entries.Count; i++)
            {
                ShadowTraceEntry entry = entries[i];
                if (entry.Sequence <= seen) continue;
                ShadowComparison comparison = entry.Comparison;
                NativeObservation native = comparison.Native;
                FormidablePrediction prediction = comparison.Prediction;
                Begin("FormidableShadow"); Actor(kernel);
                Num("shadowSeq", entry.Sequence);
                Str("mismatch", comparison.Mismatch.ToString());
                Num("timingDelta", comparison.TimingDeltaSeconds);
                Str("domain", native == null ? (prediction == null ? "" : prediction.Domain.ToString()) : native.Domain.ToString());
                Str("nativeOwner", native == null ? "" : native.Owner.ToString());
                Str("predictedOwner", prediction == null ? "" : prediction.ExpectedOwner.ToString());
                Str("nativeAction", native == null ? "" : native.Action.ToString());
                Str("predictedAction", prediction == null ? "" : prediction.Action.ToString());
                Str("target", prediction == null ? (native == null ? "" : native.TargetIdentity) : prediction.TargetIdentity);
                Str("attack", prediction == null ? (native == null ? "" : native.AttackIdentity) : prediction.AttackIdentity);
                Str("predictionReason", prediction == null ? "" : prediction.Reason);
                Str("profileJob", observer.ProfileJob);
                Str("profileClass", observer.ProfileClass);
                Str("profileWeapons", observer.ProfileWeapons);
                Str("profileSurvival", observer.ProfileSurvival);
                End();
                seen = entry.Sequence;
            }
            lastFormidableSequence[kernel] = seen;
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
        readonly Dictionary<NpcKernel, bool> lastFallBack = new Dictionary<NpcKernel, bool>();

        static NativeCombatBehavior Combat(NpcKernel kernel)
        {
            foreach (NpcBehavior b in kernel.Behaviors) { NativeCombatBehavior c = b as NativeCombatBehavior; if (c != null) return c; }
            return null;
        }

        void OnDamaged(NpcKernel kernel, float damage, Character attacker)
        {
            if (!active || writer == null || kernel == null || kernel.Body == null) return;
            Begin("Damage"); Actor(kernel);
            Num("amount", damage); Num("hp", kernel.Body.Human.GetHealth());
            Str("attacker", attacker == null ? "" : attacker.GetHoverName());
            Str("attackerPrefab", attacker == null ? "" : global::Utils.GetPrefabName(attacker.gameObject));
            Num("attackerId", attacker == null ? 0 : attacker.GetInstanceID());
            Num("attackerLevel", attacker == null ? 0 : attacker.GetLevel());
            Humanoid ah = attacker as Humanoid;
            ItemDrop.ItemData aw = ah == null ? null : ah.GetCurrentWeapon();
            Str("attackerWeapon", aw == null || aw.m_dropPrefab == null ? (aw == null ? "" : aw.m_shared.m_name) : aw.m_dropPrefab.name);
            Num("armor", kernel.Body.Human.GetBodyArmor());
            Num("dist", attacker == null ? -1f : Vector3.Distance(kernel.transform.position, attacker.transform.position));
            Str("phase", kernel.Frame.Phase.ToString());
            Bool("blocking", kernel.Body.Human.IsBlocking());
            End();
        }

        void OnSpoke(NpcKernel kernel, string line)
        {
            if (!active || writer == null || kernel == null) return;
            Begin("Voice"); Actor(kernel); Str("line", line); Str("phase", kernel.Frame.Phase.ToString()); End();
        }

        void OnStrike(Character attacker, Character target, float damage, bool killed)
        {
            if (!active || writer == null || attacker == null || target == null) return;
            NpcKernel k = attacker.GetComponent<NpcKernel>();
            if (k == null) return;
            Begin("Strike"); Actor(k);
            Str("target", target.GetHoverName()); Str("targetPrefab", global::Utils.GetPrefabName(target.gameObject));
            Num("targetId", target.GetInstanceID()); Num("amount", damage); Bool("killed", killed);
            ItemDrop.ItemData w = k.Body.Human.GetCurrentWeapon();
            Str("weapon", w == null || w.m_dropPrefab == null ? "" : w.m_dropPrefab.name);
            Num("dist", Vector3.Distance(attacker.transform.position, target.transform.position));
            Num("chainLevel", k.Body.CurrentChainLevel); Bool("heavy", k.Body.CurrentAttackIsSecondary);
            Bool("targetStaggered", target.IsStaggering());
            End();
        }

        void OnChain(NpcKernel kernel, int level, float edge)
        {
            if (!active || writer == null || kernel == null) return;
            Begin("Chain"); Actor(kernel); Num("level", level); Num("edge", edge);
            ItemDrop.ItemData w = kernel.Body.Human.GetCurrentWeapon();
            Str("weapon", w == null || w.m_dropPrefab == null ? "" : w.m_dropPrefab.name);
            Character t = kernel.Body.Ai.GetTargetCreature();
            Str("targetPrefab", t == null ? "" : global::Utils.GetPrefabName(t.gameObject));
            End();
        }

        void OnHeavy(NpcKernel kernel)
        {
            if (!active || writer == null || kernel == null) return;
            NpcBody b = kernel.Body;
            Begin("Heavy"); Actor(kernel); Str("why", b.LastHeavyReason ?? ""); Num("cost", b.LastHeavyCost);
            Num("stamina", b.Stamina); Num("closing", b.LastHeavyClosing); Num("inArea", b.LastHeavyInArea);
            Num("edge", b.LastChainDistance);
            ItemDrop.ItemData w = b.Human.GetCurrentWeapon();
            Str("weapon", w == null || w.m_dropPrefab == null ? "" : w.m_dropPrefab.name);
            Character t = b.Ai.GetTargetCreature();
            Str("targetPrefab", t == null ? "" : global::Utils.GetPrefabName(t.gameObject));
            Bool("targetStaggered", t != null && t.IsStaggering());
            End();
        }

        void OnWarCry(NpcKernel kernel, string raid)
        {
            if (!active || writer == null || kernel == null) return;
            Begin("WarCry"); Actor(kernel); Str("raid", raid ?? ""); Str("phase", kernel.Frame.Phase.ToString()); End();
        }

        void OnRemoved(NpcKernel kernel, string reason)
        {
            if (!active || writer == null || kernel == null) return;
            Character c = kernel.GetComponent<Character>();
            Begin("Removed"); Actor(kernel); Str("reason", reason); Str("phase", kernel.Frame.Phase.ToString());
            Num("hp", reason == "death" ? kernel.LastHealth : c == null ? 0f : c.GetHealth());
            Str("lastAttacker", kernel.LastAttackerName ?? "");
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
            if (subscribed)
            {
                NpcKernel.Removed -= OnRemoved; NpcKernel.Damaged -= OnDamaged;
                CompanionCombatVoice.Spoke -= OnSpoke; StrikeHook.Landed -= OnStrike; MeleeChainHook.Chained -= OnChain; MeleeChainHook.Heavy -= OnHeavy; WarCry.Cried -= OnWarCry;
            }
            if (active) Stop("shutdown");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Core
{
    // Same GUID as the 0.6.x Human NPC lab so this cleanly replaces it in the profile.
    // Same prefab names and saved keys so existing companions load straight into the new core.
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    public sealed class CorePlugin : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.humannpc";
        public const string Name = "Living World C.O.R.E. NPC";
        public const string Version = "1.0.32.16";
        internal const string GuardPrefab = "LWC_HumanGuard_v2";
        internal const string ArcherPrefab = "LWC_HumanArcher_v2";
        internal const string LegacyPrefab = "LWC_HumanScout_v1";

        internal static CorePlugin Instance;
        bool registered;
        bool partyBusy;
        Terminal.ConsoleCommand partyCommand, statusCommand, healCommand, summonCommand, gotoCommand, gearCommand, bombsCommand, bodyAuditionCommand;

        public static bool Registered { get { return Instance != null && Instance.registered; } }

        // Feature switches so one change can be live-tested at a time (BepInEx config file).
        static BepInEx.Configuration.ConfigEntry<bool> shieldBlocking, companionBombs, devBombLog, bombsEndless, bodyVoice;
        static BepInEx.Configuration.ConfigEntry<float> bombCooldown;
        static BepInEx.Configuration.ConfigEntry<bool> bodyBurning, bodyFrost, bodySwim, bodyExhaustion, bodyEnvRecover;
        static BepInEx.Configuration.ConfigEntry<float> bodySwimSeconds;
        static BepInEx.Configuration.ConfigEntry<bool> partyCloudCompanions, partyCloudPlayer, hazardAwareness, devHazardLog;
        static BepInEx.Configuration.ConfigEntry<float> partyCloudFraction;
        public static bool PartyCloudCompanions { get { return partyCloudCompanions == null || partyCloudCompanions.Value; } }
        public static bool PartyCloudPlayer { get { return partyCloudPlayer == null || partyCloudPlayer.Value; } }
        public static float PartyCloudFraction { get { return partyCloudFraction == null ? .35f : Mathf.Clamp01(partyCloudFraction.Value); } }
        public static bool HazardAwareness { get { return hazardAwareness == null || hazardAwareness.Value; } }
        public static bool DevHazardLog { get { return devHazardLog != null && devHazardLog.Value; } }
        public static bool BodyBurning { get { return bodyBurning == null || bodyBurning.Value; } }
        public static bool BodyFrost { get { return bodyFrost == null || bodyFrost.Value; } }
        public static bool BodySwim { get { return bodySwim == null || bodySwim.Value; } }
        public static bool BodyExhaustion { get { return bodyExhaustion == null || bodyExhaustion.Value; } }
        public static bool BodyEnvRecover { get { return bodyEnvRecover == null || bodyEnvRecover.Value; } }
        public static float BodySwimSeconds { get { return bodySwimSeconds == null ? 20f : Mathf.Clamp(bodySwimSeconds.Value, 5f, 120f); } }
        static BepInEx.Configuration.ConfigEntry<float> foodStamina, companionVoiceGain;
        static BepInEx.Configuration.ConfigEntry<bool> devVoiceLog;
        static BepInEx.Configuration.ConfigEntry<bool> partySpacing, devSpacingLog, matchGait;
        public static bool PartySpacingEnabled { get { return partySpacing == null || partySpacing.Value; } }
        public static bool DevSpacingLog { get { return devSpacingLog != null && devSpacingLog.Value; } }
        public static bool MatchGait { get { return matchGait == null || matchGait.Value; } }
        public static float CompanionVoiceGain { get { return companionVoiceGain == null ? 1f : Mathf.Clamp(companionVoiceGain.Value, 0f, 2f); } }
        public static bool DevVoiceLog { get { return devVoiceLog != null && devVoiceLog.Value; } }
        public static float FoodStamina { get { return foodStamina == null ? 100f : Mathf.Max(0f, foodStamina.Value); } }
        public static bool ShieldBlocking { get { return shieldBlocking != null && shieldBlocking.Value; } }
        public static bool CompanionBombs { get { return companionBombs == null || companionBombs.Value; } }
        public static bool DevBombLog { get { return devBombLog != null && devBombLog.Value; } }
        public static bool BodyVoiceEnabled { get { return bodyVoice == null || bodyVoice.Value; } }
        public static bool BombsEndless { get { return bombsEndless == null || bombsEndless.Value; } }
        public static float BombCooldown { get { return bombCooldown == null ? 10f : Mathf.Clamp(bombCooldown.Value, 2f, 120f); } }

        void Awake()
        {
            Instance = this;
            companionVoiceGain = Config.Bind("Voice", "CompanionVoiceGain", 1f,
                "One playback gain for both companion voice sets (Gamemaster Male B / Female A) over the game's 0.5 voice template. Clips are not normalized. 1.0 = template level.");
            devVoiceLog = Config.Bind("Voice", "DevVoiceLog", false,
                "Development only: log every companion line (who, gender, line, exact clip, trigger, fallback).");
            partySpacing = Config.Bind("Movement", "PartySpacing", true,
                "Companions keep personal space from the leader and each other: soft standing areas, sidesteps around party members, never pushing the leader (wider clearance while he is in a menu). Off = 1.0.32.7 movement.");
            devSpacingLog = Config.Bind("Movement", "DevSpacingLog", false,
                "Development only: log SPACE-ENTER/RELEASE/WAIT/BLOCKED/BACKOFF/CONTACT/SUMMARY lines for party spacing.");
            matchGait = Config.Bind("Movement", "MatchGait", true,
                "Companions match the leader's walking pace: they walk when he walks slowly (or has walk toggled) and jog no faster than needed, catching up only when they fall behind. Off = 1.0.32.9 pace.");
            foodStamina = Config.Bind("Combat", "CompanionFoodStamina", 100f,
                "Heavy-attack stamina a companion gets from food on top of the Player base 50 (only heavy attacks spend it).");
            shieldBlocking = Config.Bind("Combat", "ShieldBlocking", false,
                "Experimental: companions raise shields against close melee and aimed projectiles.");
            companionBombs = Config.Bind("Combat", "CompanionBombs", true,
                "Companions throw bombs the Souls of the Land Pillager way (Greydwarf throw, ooze bomb, overhead swing) at the best enemy cluster 6-13.5 m out, favouring groups coming at the party, aimed where they are headed. Never if you or another companion would be within 5.5 m of the landing. Off = never throw.");
            bodyVoice = Config.Bind("BodyVoice", "Enabled", true,
                "Body-reaction voice addon (separate from the locked combat voice): companions cough when poison or gas first takes hold, again every 4-7 s while it lasts, and catch their breath when it clears. Combat lines always take priority. Clips: voices/body/<male|female>/<onset|repeat|recover>/*.wav.");
            bodyBurning = Config.Bind("BodyVoice", "Burning", true,
                "1.0.32.14 candidate: a short pain cry when the native Burning status takes hold (fire hits, lava), a strain every 6-9 s while it lasts. Never per damage tick, never for standing near a fire. Clips: voices/body/<g>/burning_onset, burning_repeat.");
            bodyFrost = Config.Bind("BodyVoice", "Frost", true,
                "1.0.32.14 candidate: a cold breath when the native Frost status (frost hits) takes hold, again every 8-12 s while it lasts. Clips: voices/body/<g>/frost. Weather Cold/Freezing are Player-only statuses in Valheim and do not reach companions.");
            bodySwim = Config.Bind("BodyVoice", "SwimBreathless", true,
                "1.0.32.14 candidate: one catch-breath when a companion comes out of a long continuous swim (SwimSeconds). Being wet or a short swim says nothing. Clips: voices/body/<g>/swim.");
            bodySwimSeconds = Config.Bind("BodyVoice", "SwimSeconds", 20f,
                "Continuous swim time before leaving the water earns a catch-breath (5-120).");
            bodyExhaustion = Config.Bind("BodyVoice", "Exhaustion", true,
                "1.0.32.14 candidate: one strained breath when a companion's heavy-attack stamina runs out, at most every 45 s. Clips: voices/body/<g>/exhaustion.");
            bodyEnvRecover = Config.Bind("BodyVoice", "EnvironmentRecover", true,
                "1.0.32.14 candidate: one quiet exhale when burning or frost ends after at least 3 s, at most one per 30 s. Clips: voices/body/<g>/env_recover. Poison keeps its own recovery pool.");
            partyCloudCompanions = Config.Bind("Hazards", "PartyCloudCompanions", true,
                "1.0.32.15: a lingering bomb cloud (poison/fire) thrown by you or a companion also reaches companions, at PartyCloudFraction of its damage, through the game's own poison/burning. Off = the game's rule (the thrower and same-named characters are never hit).");
            partyCloudPlayer = Config.Bind("Hazards", "PartyCloudPlayer", true,
                "1.0.32.15: your own bomb cloud and your companions' clouds also reach you, at PartyCloudFraction. Off = the game's rule (your own Ooze/Bile cloud never hurts you).");
            partyCloudFraction = Config.Bind("Hazards", "PartyCloudFraction", .35f,
                "Share of a party cloud's damage a party member takes (0-1). Enemy clouds always do full damage.");
            hazardAwareness = Config.Bind("Hazards", "HazardAwareness", true,
                "1.0.32.15: companions know where the party's clouds are and see enemy clouds like a player would; one who ends up inside a harmful cloud steps out (the fall-back mover), then fights on.");
            devHazardLog = Config.Bind("Hazards", "DevHazardLog", true,
                "Development: log CLOUD-EXPOSE (a party member hit by a party cloud) and HAZARD-ESCAPE begin/end.");
            bombsEndless = Config.Bind("Combat", "BombsEndless", true,
                "Test mode: bombs never run out (lwbombs is not needed). False = each companion carries the lwbombs reserve.");
            bombCooldown = Config.Bind("Combat", "BombCooldown", 10f,
                "Seconds between one companion's throws (Souls Pillager: 16).");
            devBombLog = Config.Bind("Combat", "DevBombLog", false,
                "Development only: log why a companion carrying bombs did not throw (BOMB-SKIP), at most every 3 s.");
            // One hook: companions answer native MonsterAI's own "which enemy?" question.
            var harmony = new HarmonyLib.Harmony(Guid);
            harmony.Patch(NativeApi.FindEnemy,
                postfix: new HarmonyLib.HarmonyMethod(typeof(FindEnemyHook).GetMethod("Postfix")));
            // Companions get the Player's armor rules (Valheim counts worn armor only for Player).
            harmony.Patch(NativeApi.GetBodyArmor,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("BodyArmorPostfix")));
            harmony.Patch(NativeApi.ApplyArmorDamageMods,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("ArmorModsPostfix")));
            harmony.Patch(NativeApi.EquipBestWeapon,
                prefix: new HarmonyLib.HarmonyMethod(typeof(WeaponChoiceHook).GetMethod("Prefix")));
            harmony.Patch(NativeApi.ApplyDamage,
                postfix: new HarmonyLib.HarmonyMethod(typeof(StrikeHook).GetMethod("Postfix")));
            harmony.Patch(NativeApi.RpcDamage,
                transpiler: new HarmonyLib.HarmonyMethod(typeof(ArmorParity).GetMethod("RpcDamageTranspiler")));
            // Chained melee: companions continue a combo inside the swing's native chain window (Player rule).
            harmony.Patch(NativeApi.HaveQueuedChain,
                postfix: new HarmonyLib.HarmonyMethod(typeof(MeleeChainHook).GetMethod("QueuedPostfix")));
            harmony.Patch(NativeApi.HumanoidStartAttack,
                postfix: new HarmonyLib.HarmonyMethod(typeof(MeleeChainHook).GetMethod("StartAttackPostfix")));
            harmony.Patch(NativeApi.UpdateAttack,
                postfix: new HarmonyLib.HarmonyMethod(typeof(MeleeChainHook).GetMethod("UpdatePostfix")));
            // 1.0.32.4 voice (observes only; never changes the attack or the stagger).
            harmony.Patch(NativeApi.AttackTrigger,
                postfix: new HarmonyLib.HarmonyMethod(typeof(CompanionCombatVoice).GetMethod("AttackTriggerPostfix")));
            // 1.0.32.12 throws: only the Living World throw item with a pending aim (ThrowAimHook.Mine).
            harmony.Patch(NativeApi.FireProjectileBurst,
                prefix: new HarmonyLib.HarmonyMethod(typeof(ThrowAimHook).GetMethod("FirePrefix")));
            harmony.Patch(NativeApi.GetProjectileSpawnPoint,
                postfix: new HarmonyLib.HarmonyMethod(typeof(ThrowAimHook).GetMethod("SpawnPostfix")));
            // 1.0.32.15 party clouds: reduced own/party exposure and the live cloud list for hazard awareness.
            harmony.Patch(NativeApi.AoeAwake, postfix: new HarmonyLib.HarmonyMethod(typeof(PartyClouds).GetMethod("AwakePostfix")));
            harmony.Patch(NativeApi.AoeShouldHit, prefix: new HarmonyLib.HarmonyMethod(typeof(PartyClouds).GetMethod("ShouldHitPrefix")),
                finalizer: new HarmonyLib.HarmonyMethod(typeof(PartyClouds).GetMethod("ShouldHitFinalizer")));
            harmony.Patch(NativeApi.AoeOnHit, prefix: new HarmonyLib.HarmonyMethod(typeof(PartyClouds).GetMethod("OnHitPrefix")),
                finalizer: new HarmonyLib.HarmonyMethod(typeof(PartyClouds).GetMethod("OnHitFinalizer")));
            harmony.Patch(NativeApi.RpcStagger,
                prefix: new HarmonyLib.HarmonyMethod(typeof(CompanionCombatVoice).GetMethod("RpcStaggerPrefix")));
            harmony.Patch(NativeApi.CharacterOnDeath,
                prefix: new HarmonyLib.HarmonyMethod(typeof(CompanionCombatVoice).GetMethod("DeathPrefix")),
                finalizer: new HarmonyLib.HarmonyMethod(typeof(CompanionCombatVoice).GetMethod("DeathFinalizer")));
            Report(ArmorParity.ArmorGatesPatched == 1 ? "Companion armor applies to damage (Player rule)." :
                "WARNING: companion armor gate not patched.");
            Report("Companion damage parity: stagger x2 " + (ArmorParity.StaggerGatesPatched == 1 ? "off" : "NOT PATCHED") +
                ", backstab " + (ArmorParity.BackstabGatesPatched == 1 ? "off" : "NOT PATCHED") + " (Player rules).");
            PrefabManager.OnVanillaPrefabsAvailable += RegisterPrefabs;
            partyCommand = new Terminal.ConsoleCommand("lwparty",
                "Living World: create your two companions (lwparty new = replace the current lab pair)",
                new Terminal.ConsoleEvent(OnParty));
            statusCommand = new Terminal.ConsoleCommand("lwstatus", "Living World: show each companion's phase and owners",
                new Terminal.ConsoleEvent(OnStatus));
            healCommand = new Terminal.ConsoleCommand("lwheal", "Living World: heal your companions and clear their status effects",
                new Terminal.ConsoleEvent(args => OnHeal()));
            summonCommand = new Terminal.ConsoleCommand("lwsummon", "Living World: bring your companions to you",
                new Terminal.ConsoleEvent(args => OnSummon()));
            gearCommand = new Terminal.ConsoleCommand("lwgear", "Living World: re-outfit your companions (lwgear iron|plains [quality 1-4])",
                new Terminal.ConsoleEvent(OnGear));
            bombsCommand = new Terminal.ConsoleCommand("lwbombs", "Living World (lab): give each companion bombs (lwbombs [count 0-20] [ooze|bile])",
                new Terminal.ConsoleEvent(OnBombs));
            bodyAuditionCommand = new Terminal.ConsoleCommand("lwbodyaudition",
                "Living World (lab): play every clip of one body pool on a companion, 3 s apart (lwbodyaudition list | <pool> [male|female])",
                new Terminal.ConsoleEvent(args => BodyReactionVoice.Audition(args.Args)));
            gotoCommand = new Terminal.ConsoleCommand("lwgoto", "Living World: teleport yourself to your nearest companion",
                new Terminal.ConsoleEvent(args => OnGoto()));
            string missing = NativeApi.MissingMembers();
            Report(Version + " loaded. Commands: lwparty, lwparty new, lwparty plains [q], lwparty clean, lwgear iron|plains [q], lwbombs [n] [ooze|bile], lwbodyaudition, lwstatus, lwheal, lwsummon, lwgoto." +
                (missing.Length == 0 ? "" : " NATIVE API MISSING: " + missing));
        }

        /// <summary>1.0.32.12: the Souls of the Land Pillager bomb, rebuilt on our own clone so vanilla
        /// Greydwarf_throw and the player's bombs are untouched: Greydwarf_throw attack, oozebomb projectile,
        /// mace_secondary swing, sfx_unarmed_swing, pierce 5 + fire 25 on the projectile hit.</summary>
        void RegisterThrowItem()
        {
            GameObject projectile = PrefabManager.Instance.GetPrefab("oozebomb_projectile");
            if (projectile == null) { Report("THROW item not registered: oozebomb_projectile missing."); return; }
            var item = new CustomItem(ThrowAimHook.ThrowItem, "Greydwarf_throw");
            if (item.ItemDrop == null) { Report("THROW item not registered: Greydwarf_throw missing."); return; }
            ItemDrop.ItemData.SharedData shared = item.ItemDrop.m_itemData.m_shared;
            shared.m_name = "Living World throw";
            shared.m_attack.m_attackAnimation = "mace_secondary";
            shared.m_attack.m_attackProjectile = projectile;
            shared.m_damages = new HitData.DamageTypes { m_pierce = 5f, m_fire = 25f };
            GameObject swing = PrefabManager.Instance.GetPrefab("sfx_unarmed_swing");
            if (swing != null) shared.m_attack.m_startEffect = new EffectList { m_effectPrefabs = new[] { new EffectList.EffectData { m_prefab = swing, m_enabled = true } } };
            ItemManager.Instance.AddItem(item);
            Report("THROW item " + ThrowAimHook.ThrowItem + ": Greydwarf_throw + oozebomb_projectile, anim mace_secondary, vel " +
                shared.m_attack.m_projectileVel + ", spread " + shared.m_attack.m_projectileAccuracy + "/" + shared.m_attack.m_projectileAccuracyMin + ".");
        }

        void RegisterPrefabs()
        {
            if (registered) return;
            try
            {
                CompanionCombatVoice.Build();
                Report("Companion voice (Gamemaster Human Vocalizations; no enemy recordings): male (Male B) " + (CompanionCombatVoice.MaleSummary ?? "?") + ".");
                Report("Companion voice: female (Female A) " + (CompanionCombatVoice.FemaleSummary ?? "?") + ".");
                BodyReactionVoice.Build();
                Report("Body-reaction voice addon: " + (BodyReactionVoice.Summary ?? "?") + ".");
                GameObject player = RequirePrefab("Player", false);
                // No helmet: it hides native hair (0.4.0 lesson).
                // Same kit, same behavior: the weapon follows from distance, not from a role.
                GameObject[] guardItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "BowFineWood", "ArrowWood", "SwordIron", "ShieldBanded");
                GameObject[] archerItems = Items("ArmorIronChest", "ArmorIronLegs", "CapeDeerHide", "BowFineWood", "ArrowWood", "SwordIron", "ShieldBanded");
                PrefabManager.Instance.AddPrefab(new CustomPrefab(NpcFactory.Create(GuardPrefab, player, guardItems, "guard"), false));
                PrefabManager.Instance.AddPrefab(new CustomPrefab(NpcFactory.Create(ArcherPrefab, player, archerItems, "archer"), false));
                RegisterThrowItem();
                registered = true;
                Report("Registered guard and archer native humans.");
                try { CombatDataReport.Write(Report); }
                catch (Exception e) { Report("DATA report failed: " + e.Message); }
            }
            catch (Exception error)
            {
                Logger.LogError(error);
                Report("REGISTRATION FAILED: " + error.Message);
            }
        }

        GameObject[] Items(params string[] names) { return names.Select(n => RequirePrefab(n, true)).ToArray(); }

        static GameObject RequirePrefab(string name, bool item)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            if (prefab == null || (item && prefab.GetComponent<ItemDrop>() == null))
                throw new InvalidOperationException("Required native prefab is unavailable: " + name);
            return prefab;
        }

        // ---------------- lwparty ----------------

        void OnParty(Terminal.ConsoleEventArgs args)
        {
            if (partyBusy) { Report("lwparty is already running."); return; }
            if (args.Length > 1 && args[1].Equals("clean", StringComparison.OrdinalIgnoreCase)) { StartCoroutine(RunClean()); return; }
            // lwparty plains [quality 1-4]: a fresh pair in vanilla Plains gear (replaces the current pair).
            string kit = null;
            int quality = 1;
            if (args.Length > 1 && CompanionKits.Items(args[1]) != null)
            {
                kit = args[1].ToLowerInvariant();
                if (args.Length > 2) int.TryParse(args[2], out quality);
                quality = CompanionKits.ClampQuality(quality);
            }
            bool replace = kit != null || (args.Length > 1 && args[1].Equals("new", StringComparison.OrdinalIgnoreCase));
            if (!replace && Player.m_localPlayer != null)
            {
                long me = Player.m_localPlayer.GetPlayerID();
                int mine = NpcKernel.All.Count(k => k != null && k.GetComponent<NpcLeaderBinding>() != null &&
                    k.GetComponent<NpcLeaderBinding>().LeaderId == me);
                if (mine > 0)
                {
                    Report("You already have " + mine + " companion(s) loaded. Use 'lwparty new' to replace them.");
                    return;
                }
            }
            StartCoroutine(RunParty(replace, kit, quality));
        }

        IEnumerator RunParty(bool replace, string kit, int quality)
        {
            partyBusy = true;
            var created = new List<GameObject>();
            Exception failure = null;
            IEnumerator transaction = PartyTransaction(created, replace, kit, quality);
            while (true)
            {
                object yielded = null;
                bool advanced;
                try
                {
                    advanced = transaction.MoveNext();
                    if (advanced) yielded = transaction.Current;
                }
                catch (Exception error) { failure = error; break; }
                if (!advanced) break;
                yield return yielded;
            }
            if (failure != null)
            {
                foreach (GameObject actor in created)
                {
                    if (actor == null) continue;
                    ZNetView view = actor.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) ZNetScene.instance.Destroy(actor);
                    else UnityEngine.Object.Destroy(actor);
                }
                Logger.LogError(failure);
                Report("PARTY FAILED: " + failure.Message + " No partial party was kept.");
            }
            partyBusy = false;
        }

        IEnumerator PartyTransaction(List<GameObject> created, bool replace, string kit, int quality)
        {
            RequireWorld();
            if (!registered) throw new InvalidOperationException("Companion prefabs are not registered (see startup log).");
            int removed = 0;
            if (replace)
            {
                foreach (string prefab in new[] { LegacyPrefab, GuardPrefab, ArcherPrefab })
                {
                    int count = 0;
                    yield return StartCoroutine(RemovePrefabRecords(prefab, value => count = value));
                    removed += count;
                }
                yield return null;
                yield return null;
            }

            Player leader = Player.m_localPlayer;
            string generation = System.Guid.NewGuid().ToString("N");
            Vector3[] points = FindSpawnPair(leader);
            Spawn(GuardPrefab, points[0], leader.transform.rotation, created);
            Spawn(ArcherPrefab, points[1], leader.transform.rotation, created);

            float deadline = Time.realtimeSinceStartup + 10f;
            while (created.Any(a => a != null && !a.GetComponent<NpcAppearance>().Ready) && Time.realtimeSinceStartup < deadline)
                yield return null;
            foreach (GameObject actor in created)
            {
                if (actor == null) throw new InvalidOperationException("A new companion vanished during creation.");
                NpcAppearance appearance = actor.GetComponent<NpcAppearance>();
                if (!appearance.Ready) throw new InvalidOperationException(appearance.Status);
            }
            foreach (GameObject actor in created) actor.GetComponent<NpcLeaderBinding>().Bind(leader, generation);
            string gear = "";
            if (kit != null)
            {
                foreach (GameObject actor in created)
                {
                    string refused = actor.GetComponent<NpcKernel>().Body.Outfit(kit, quality);
                    if (refused.Length > 0) throw new InvalidOperationException("outfitting failed: " + refused);
                }
                gear = " Gear: " + kit + " q" + quality + ".";
            }
            Report("PARTY READY: two companions bound to you" + (replace ? "; replaced " + removed + " old record(s)." : ".") + gear);
        }

        static void Spawn(string prefabId, Vector3 position, Quaternion rotation, List<GameObject> created)
        {
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabId);
            if (prefab == null) throw new InvalidOperationException("Registered prefab not found: " + prefabId);
            GameObject actor = UnityEngine.Object.Instantiate(prefab, position, rotation);
            created.Add(actor); // tracked before validation so a failure here is still rolled back
            ZNetView view = actor.GetComponent<ZNetView>();
            if (view == null || !view.IsValid() || !view.IsOwner())
                throw new InvalidOperationException("New companion did not receive local network ownership.");
            NpcFactory.Validate(actor);
        }

        /// <summary>lwparty clean: remove every companion record in the world, loaded or not.</summary>
        IEnumerator RunClean()
        {
            if (partyBusy) { Report("lwparty is already running."); yield break; }
            partyBusy = true;
            int removed = 0;
            foreach (string prefab in new[] { LegacyPrefab, GuardPrefab, ArcherPrefab })
            {
                int count = 0;
                yield return StartCoroutine(RemovePrefabRecords(prefab, value => count = value));
                removed += count;
            }
            partyBusy = false;
            Report("CLEAN: removed " + removed + " companion record(s) everywhere in this world.");
        }

        static List<NpcKernel> MyCompanions()
        {
            var mine = new List<NpcKernel>();
            if (Player.m_localPlayer == null) return mine;
            long me = Player.m_localPlayer.GetPlayerID();
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (k == null || k.Body == null) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (b != null && b.LeaderId == me) mine.Add(k);
            }
            return mine;
        }

        /// <summary>lwgear iron|plains [quality 1-4]: re-outfit the loaded companions (not mid-fight).</summary>
        void OnGear(Terminal.ConsoleEventArgs args)
        {
            if (args.Length < 2 || CompanionKits.Items(args[1]) == null) { Report("Usage: lwgear iron|plains [quality 1-4]"); return; }
            int quality = 1;
            if (args.Length > 2) int.TryParse(args[2], out quality);
            quality = CompanionKits.ClampQuality(quality);
            int done = 0;
            var refusals = new List<string>();
            foreach (NpcKernel k in MyCompanions())
            {
                NpcPhase phase = k.Frame.Phase;
                if (phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup)
                { refusals.Add(k.Body.Human.GetHoverName() + ": in combat"); continue; }
                string refused = k.Body.Outfit(args[1], quality);
                if (refused.Length == 0) done++; else refusals.Add(k.Body.Human.GetHoverName() + ": " + refused);
            }
            Report("GEAR: " + done + " companion(s) now in " + args[1].ToLowerInvariant() + " q" + quality + "." +
                (refusals.Count > 0 ? " Refused: " + String.Join("; ", refusals.ToArray()) : ""));
        }

        /// <summary>lwbombs [count] [ooze|bile]: lab grant of a finite bomb reserve to each loaded companion.</summary>
        void OnBombs(Terminal.ConsoleEventArgs args)
        {
            int count = 3;
            if (args.Length > 1 && !int.TryParse(args[1], out count)) { Report("Usage: lwbombs [count 0-20] [ooze|bile]"); return; }
            string kind = args.Length > 2 && args[2].ToLowerInvariant() == "bile" ? "BombBile" : "BombOoze";
            int done = 0; var refusals = new List<string>();
            foreach (NpcKernel k in MyCompanions())
            {
                string refused = k.Body.SetBombs(count, kind);
                if (refused.Length == 0) done++; else refusals.Add(k.Body.Human.GetHoverName() + ": " + refused);
            }
            Report("BOMBS: " + done + " companion(s) now carry " + Mathf.Clamp(count, 0, 20) + " " + kind + "." +
                (refusals.Count > 0 ? " Refused: " + String.Join("; ", refusals.ToArray()) : ""));
        }

        float nextCloudPrune;
        void Update()
        {
            BodyReactionVoice.Tick();
            if (Time.time >= nextCloudPrune) { nextCloudPrune = Time.time + 2f; PartyClouds.Prune(); }
        }

        void OnHeal()
        {
            int healed = MyCompanions().Count(k => k.Body.HealFull());
            Report("HEAL: " + healed + " companion(s) restored to full health, status effects cleared.");
        }

        void OnSummon()
        {
            Player me = Player.m_localPlayer;
            if (me == null) { Report("Not in a world."); return; }
            List<NpcKernel> mine = MyCompanions();
            int placed = 0;
            for (int i = 0; i < mine.Count; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 150f + 60f * i, 0f) * me.transform.forward * 2.5f;
                Vector3 point = me.transform.position + offset;
                float height;
                if (ZoneSystem.instance.GetGroundHeight(point, out height)) point.y = height + .1f;
                if (mine[i].Body.PlaceAt(point, me.transform.rotation)) placed++;
            }
            Report("SUMMON: " + placed + " of " + mine.Count + " loaded companion(s) brought to you. (Companions in unloaded areas are not reachable; use lwgoto.)");
        }

        void OnGoto()
        {
            Player me = Player.m_localPlayer;
            if (me == null) { Report("Not in a world."); return; }
            NpcKernel nearest = MyCompanions().OrderBy(k => Vector3.Distance(k.transform.position, me.transform.position)).FirstOrDefault();
            if (nearest == null) { Report("No loaded companion to go to."); return; }
            Vector3 target = nearest.transform.position - nearest.transform.forward * 2f;
            me.TeleportTo(target, nearest.transform.rotation, false);
            Report("GOTO: teleporting to " + nearest.Body.Human.GetHoverName() + ".");
        }

        IEnumerator RemovePrefabRecords(string prefabName, Action<int> completed)
        {
            var records = new List<ZDO>();
            int index = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefabName, records, ref index)) yield return null;
            int removed = 0;
            foreach (ZDO zdo in records.ToArray())
            {
                if (zdo == null || !zdo.IsValid()) continue;
                GameObject loaded = ZNetScene.instance.FindInstance(zdo.m_uid);
                if (loaded != null)
                {
                    ZNetView view = loaded.GetComponent<ZNetView>();
                    if (view != null && view.IsValid() && view.IsOwner()) { ZNetScene.instance.Destroy(loaded); removed++; }
                }
                else { ZDOMan.instance.DestroyZDO(zdo); removed++; }
            }
            completed(removed);
        }

        static Vector3[] FindSpawnPair(Player player)
        {
            Vector3[] offsets = {
                -player.transform.right * 2.5f - player.transform.forward * 2f,
                player.transform.right * 2.5f - player.transform.forward * 2f,
                -player.transform.right * 3.5f, player.transform.right * 3.5f
            };
            var points = new List<Vector3>();
            foreach (Vector3 offset in offsets)
            {
                Vector3 candidate = player.transform.position + offset;
                float height;
                if (!ZoneSystem.instance.GetGroundHeight(candidate, out height)) continue;
                candidate.y = height + .15f;
                bool blocked = Physics.OverlapCapsule(candidate + Vector3.up * .5f, candidate + Vector3.up * 1.7f, .65f)
                    .Any(c => !c.isTrigger && !(c is TerrainCollider) && c.GetComponent<Heightmap>() == null);
                if (!blocked) points.Add(candidate);
                if (points.Count == 2) return points.ToArray();
            }
            throw new InvalidOperationException("Stand on open loaded ground so two companions can appear beside you.");
        }

        // ---------------- lwstatus ----------------

        void OnStatus(Terminal.ConsoleEventArgs args)
        {
            Report("STATUS " + Version + ": registered=" + registered + "; companions loaded=" + NpcKernel.All.Count());
            foreach (NpcKernel kernel in NpcKernel.All)
            {
                if (kernel == null) continue;
                NpcAppearance appearance = kernel.GetComponent<NpcAppearance>();
                NpcWetStatus wet = kernel.GetComponent<NpcWetStatus>();
                Character character = kernel.GetComponent<Character>();
                Report((appearance == null ? kernel.name : appearance.Status) + " | " + kernel.Describe() +
                    " | hp=" + (character == null ? 0f : character.GetHealth()).ToString("0") +
                    (wet != null && wet.Wet ? " | wet" : ""));
            }
        }

        static void RequireWorld()
        {
            if (Player.m_localPlayer == null || ZNet.instance == null || ZNetScene.instance == null ||
                ZDOMan.instance == null || ZoneSystem.instance == null)
                throw new InvalidOperationException("Enter a world and wait until you are standing on loaded terrain.");
            if (!ZNet.instance.IsServer())
                throw new InvalidOperationException("lwparty currently requires local/server authority.");
        }

        // ---------------- reporting ----------------

        public static void Report(string message)
        {
            if (Instance == null) return;
            Instance.Logger.LogInfo("[LWC] " + message);
            if (global::Console.instance != null) global::Console.instance.Print("[LWC] " + message);
            if (Player.m_localPlayer != null) Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, message, 0, null, false);
        }

        public static void Warn(string message)
        {
            if (Instance != null) Instance.Logger.LogWarning("[LWC] " + message);
        }

        void OnDestroy()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= RegisterPrefabs;
            Instance = null;
        }
    }
}

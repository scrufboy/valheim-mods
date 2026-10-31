using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MagicaCloth2;
using UnityEngine;

namespace LivingWorld.RRRCoreCompat
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.alexanderstrada.rrrcore", BepInDependency.DependencyFlags.HardDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "local.livingworld.rrrcore.compat";
        public const string Name = "Living World RRRCore current-game compatibility";
        public const string Version = "1.0.0";

        static readonly string[] SoulsPrefabs = {
            "RRRN_Pillager", "RRRN_OrcBrute", "RRRN_WoodsSoul",
            "RRRN_Neckman", "RRRN_NinoBosque", "RRRN_Servant"
        };

        static ManualLogSource log;
        static Type npcType;
        static MethodInfo designCompHumanoid;
        static MethodInfo designSfxDeepen;

        void Awake()
        {
            log = Logger;
            npcType = AccessTools.TypeByName("RRRCore.npc.Npc");
            Type loaderType = AccessTools.TypeByName("RRRCore.RRRPrefabLoader");
            if (npcType == null || loaderType == null)
                throw new InvalidOperationException("RRRCore 3.1.5 types are unavailable.");

            MethodInfo designNpcBase = AccessTools.Method(npcType, "DesignNpcBase");
            designCompHumanoid = AccessTools.Method(npcType, "DesignCompHumanoid");
            designSfxDeepen = AccessTools.Method(npcType, "DesignSfxDeepen");
            MethodInfo load = AccessTools.Method(loaderType, "Load");
            if (designNpcBase == null || designCompHumanoid == null || designSfxDeepen == null || load == null)
                throw new MissingMethodException("RRRCore 3.1.5 prefab construction methods changed.");

            Harmony harmony = new Harmony(Guid);
            harmony.Patch(designNpcBase,
                prefix: new HarmonyMethod(typeof(Plugin), "DesignNpcBasePrefix"));
            harmony.Patch(load,
                postfix: new HarmonyMethod(typeof(Plugin), "PrefabLoadPostfix"));
            Logger.LogInfo("RRRCore 3.1.5 current-Player prefab compatibility armed.");
        }

        static bool DesignNpcBasePrefix(GameObject clone, bool sfxNoAlert, bool sfxNoIdle,
            bool sfxNoHit, bool sfxNoDeath)
        {
            try
            {
                BuildCurrentNpcBase(clone, sfxNoAlert, sfxNoIdle, sfxNoHit, sfxNoDeath);
                log.LogInfo("RRR_NPC base rebuilt from the current Player prefab graph.");
                return false;
            }
            catch (Exception ex)
            {
                log.LogError("RRR_NPC current-game construction failed: " + ex);
                throw;
            }
        }

        static void BuildCurrentNpcBase(GameObject clone, bool sfxNoAlert, bool sfxNoIdle,
            bool sfxNoHit, bool sfxNoDeath)
        {
            if (clone == null) throw new ArgumentNullException("clone");
            if (ZNetScene.instance == null) throw new InvalidOperationException("ZNetScene is unavailable.");
            GameObject player = ZNetScene.instance.GetPrefab("Player");
            if (player == null) throw new InvalidOperationException("Current Player prefab is unavailable.");

            while (clone.transform.childCount > 0)
                UnityEngine.Object.DestroyImmediate(clone.transform.GetChild(0).gameObject);

            for (int i = 0; i < player.transform.childCount; ++i)
            {
                GameObject child = UnityEngine.Object.Instantiate<GameObject>(
                    player.transform.GetChild(i).gameObject, clone.transform);
                child.name = TrimCloneTag(child.name);
            }

            VisEquipment cloneVisuals = Require<VisEquipment>(clone, "clone VisEquipment");
            VisEquipment playerVisuals = Require<VisEquipment>(player, "Player VisEquipment");
            cloneVisuals.m_bodyModel = MapComponent<SkinnedMeshRenderer>(
                playerVisuals.m_bodyModel, player.transform, clone.transform, "body model");
            cloneVisuals.m_leftHand = MapTransform(playerVisuals.m_leftHand, player.transform, clone.transform, "left hand");
            cloneVisuals.m_rightHand = MapTransform(playerVisuals.m_rightHand, player.transform, clone.transform, "right hand");
            cloneVisuals.m_helmet = MapTransform(playerVisuals.m_helmet, player.transform, clone.transform, "helmet");
            cloneVisuals.m_backShield = MapTransform(playerVisuals.m_backShield, player.transform, clone.transform, "back shield");
            cloneVisuals.m_backMelee = MapTransform(playerVisuals.m_backMelee, player.transform, clone.transform, "back melee");
            cloneVisuals.m_backTwohandedMelee = MapTransform(playerVisuals.m_backTwohandedMelee, player.transform, clone.transform, "back two-handed");
            cloneVisuals.m_backBow = MapTransform(playerVisuals.m_backBow, player.transform, clone.transform, "back bow");
            cloneVisuals.m_backTool = MapTransform(playerVisuals.m_backTool, player.transform, clone.transform, "back tool");
            cloneVisuals.m_backAtgeir = MapTransform(playerVisuals.m_backAtgeir, player.transform, clone.transform, "back atgeir");
            cloneVisuals.m_clothColliders = new List<ColliderComponent>(clone.GetComponentsInChildren<ColliderComponent>(true));
            cloneVisuals.m_models = playerVisuals.m_models;
            cloneVisuals.m_isPlayer = true;

            CapsuleCollider cloneCapsule = Require<CapsuleCollider>(clone, "clone CapsuleCollider");
            CapsuleCollider playerCapsule = Require<CapsuleCollider>(player, "Player CapsuleCollider");
            cloneCapsule.center = playerCapsule.center;
            cloneCapsule.radius = playerCapsule.radius;
            cloneCapsule.height = playerCapsule.height;
            Require<Rigidbody>(clone, "clone Rigidbody").interpolation =
                Require<Rigidbody>(player, "Player Rigidbody").interpolation;

            ZSyncAnimation cloneSync = Require<ZSyncAnimation>(clone, "clone ZSyncAnimation");
            ZSyncAnimation playerSync = Require<ZSyncAnimation>(player, "Player ZSyncAnimation");
            cloneSync.m_syncBools = new List<string>(playerSync.m_syncBools);
            cloneSync.m_syncFloats = new List<string>(playerSync.m_syncFloats);
            cloneSync.m_syncInts = new List<string>(playerSync.m_syncInts);

            FootStep cloneFoot = Require<FootStep>(clone, "clone FootStep");
            FootStep playerFoot = Require<FootStep>(player, "Player FootStep");
            cloneFoot.m_footstepCullDistance = playerFoot.m_footstepCullDistance;
            cloneFoot.m_effects.Clear();
            foreach (FootStep.StepEffect effect in playerFoot.m_effects) cloneFoot.m_effects.Add(effect);
            if (cloneFoot.m_feet == null || cloneFoot.m_feet.Length < playerFoot.m_feet.Length)
                cloneFoot.m_feet = new Transform[playerFoot.m_feet.Length];
            for (int i = 0; i < playerFoot.m_feet.Length; ++i)
                cloneFoot.m_feet[i] = MapTransform(playerFoot.m_feet[i], player.transform, clone.transform, "foot " + i);

            Humanoid playerHuman = Require<Humanoid>(player, "Player Humanoid");
            Transform eye = MapTransform(playerHuman.m_eye, player.transform, clone.transform, "eye");
            Invoke(designCompHumanoid, clone, player, eye.gameObject, sfxNoHit, sfxNoDeath);

            Character character = Require<Character>(clone, "clone Character");
            character.m_name = "Human";
            character.m_health = 100f;
            character.m_faction = Character.Faction.Players;
            MonsterAI ai = Require<MonsterAI>(clone, "clone MonsterAI");
            ai.m_pathAgentType = Pathfinding.AgentType.Humanoid;
            ai.m_alertedEffects = sfxNoAlert ? null : SingleEffect(DesignSound("sfx_goblin_alerted", "RRR_NPC_sfx_alerted"));
            ai.m_idleSound = sfxNoIdle ? null : SingleEffect(DesignSound("sfx_goblin_idle", "RRR_NPC_sfx_idle"));

            CharacterDrop drops = Require<CharacterDrop>(clone, "clone CharacterDrop");
            drops.m_drops.Clear();
            ConfigureCustomization(clone);
        }

        static void ConfigureCustomization(GameObject clone)
        {
            Type type = AccessTools.TypeByName("RRRCore.RRRNpcCustomization");
            if (type == null) throw new TypeLoadException("RRRCore.RRRNpcCustomization");
            Component custom = clone.AddComponent(type);
            SetField(custom, "IsFemale", false);
            List<string> beards = new List<string> { "BeardNone" };
            for (int i = 1; i <= 10; ++i) beards.Add("Beard" + i);
            List<string> hair = new List<string> { "HairNone" };
            for (int i = 1; i <= 14; ++i) hair.Add("Hair" + i);
            SetField(custom, "BeardItems", beards);
            SetField(custom, "HairItems", hair);
            SetField(custom, "HairColorMin", new Color(1f, .5f, .3f));
            SetField(custom, "HairColorMax", new Color(1f, .9f, .7f));
            SetField(custom, "SkinColorMin", new Color(.5f, .5f, .5f));
            SetField(custom, "SkinColorMax", new Color(1f, 1f, 1f));
            SetField(custom, "HairValueMin", .1f);
            SetField(custom, "HairValueMax", 1f);
        }

        static void SetField(Component instance, string name, object value)
        {
            FieldInfo field = AccessTools.Field(instance.GetType(), name);
            if (field == null) throw new MissingFieldException(instance.GetType().FullName, name);
            field.SetValue(instance, value);
        }

        static EffectList SingleEffect(GameObject prefab)
        {
            return new EffectList {
                m_effectPrefabs = new[] {
                    new EffectList.EffectData { m_prefab = prefab, m_enabled = true }
                }
            };
        }

        static GameObject DesignSound(string source, string name)
        {
            return (GameObject)Invoke(designSfxDeepen, source, name);
        }

        static object Invoke(MethodInfo method, params object[] args)
        {
            try { return method.Invoke(null, args); }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException != null) throw ex.InnerException;
                throw;
            }
        }

        static T Require<T>(GameObject root, string label) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component == null) throw new InvalidOperationException("Missing " + label + ".");
            return component;
        }

        static T MapComponent<T>(T source, Transform sourceRoot, Transform targetRoot, string label) where T : Component
        {
            if (source == null) throw new InvalidOperationException("Current Player has no " + label + ".");
            Transform mapped = MapTransform(source.transform, sourceRoot, targetRoot, label);
            T component = mapped.GetComponent<T>();
            if (component == null) throw new InvalidOperationException("Mapped " + label + " has no " + typeof(T).Name + ".");
            return component;
        }

        static Transform MapTransform(Transform source, Transform sourceRoot, Transform targetRoot, string label)
        {
            if (source == null) throw new InvalidOperationException("Current Player has no " + label + " transform.");
            string path = RelativePath(source, sourceRoot);
            Transform mapped = path.Length == 0 ? targetRoot : targetRoot.Find(path);
            if (mapped == null) throw new InvalidOperationException("Unable to map " + label + " at " + path + ".");
            return mapped;
        }

        static string RelativePath(Transform node, Transform root)
        {
            List<string> parts = new List<string>();
            Transform cursor = node;
            while (cursor != null && cursor != root)
            {
                parts.Add(cursor.name);
                cursor = cursor.parent;
            }
            if (cursor != root) throw new InvalidOperationException("Transform is outside the Player prefab.");
            parts.Reverse();
            return String.Join("/", parts.ToArray());
        }

        static string TrimCloneTag(string name)
        {
            const string tag = "(Clone)";
            return name != null && name.EndsWith(tag, StringComparison.Ordinal)
                ? name.Substring(0, name.Length - tag.Length) : name;
        }

        static void PrefabLoadPostfix()
        {
            if (ZNetScene.instance == null)
            {
                log.LogError("SOULS-PREFAB verification failed: ZNetScene unavailable.");
                return;
            }
            int ready = 0;
            foreach (string name in SoulsPrefabs)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab == null)
                    log.LogError("SOULS-PREFAB MISSING " + name);
                else
                {
                    ++ready;
                    Humanoid human = prefab.GetComponent<Humanoid>();
                    MonsterAI ai = prefab.GetComponent<MonsterAI>();
                    log.LogInfo("SOULS-PREFAB READY " + name +
                        " humanoid=" + (human != null) + " monsterAI=" + (ai != null));
                }
            }
            log.LogInfo("SOULS-PREFAB SUMMARY " + ready + "/" + SoulsPrefabs.Length + " ready.");
        }
    }
}


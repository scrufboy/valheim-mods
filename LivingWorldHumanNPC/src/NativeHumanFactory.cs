using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Fresh, explicitly configured human NPC component composition. The native
    // Player prefab supplies one internally consistent human visual/equipment
    // graph. Its Player controller is removed while the clone is still beneath
    // Jotunn's inactive prefab container, before Unity can run its lifecycle.
    internal static class NativeHumanFactory
    {
        internal static GameObject Create(string id, GameObject player, GameObject[] outfit)
        {
            HumanRigPolicy.Require(!String.IsNullOrEmpty(id) && player != null, "Human identity and native visual source required.");
            HumanRigPolicy.Require(outfit != null && outfit.Length > 0, "Native human outfit required.");
            var root = PrefabManager.Instance.CreateClonedPrefab(id, player);
            HumanRigPolicy.Require(root != null && !root.activeInHierarchy,
                "Native Player clone must remain beneath Jotunn's inactive prefab container.");
            try
            {
                var sourcePlayer = root.GetComponent<Player>();
                var body = root.GetComponent<Rigidbody>();
                var view = root.GetComponent<ZNetView>();
                var transformSync = root.GetComponent<ZSyncTransform>();
                var animationSync = root.GetComponent<ZSyncAnimation>();
                var vis = root.GetComponent<VisEquipment>();
                HumanRigPolicy.Require(sourcePlayer != null && body != null && view != null &&
                    transformSync != null && animationSync != null && vis != null,
                    "Native Player clone is missing its human body, network or visual graph.");

                // Add and populate the NPC Humanoid while the cloned Player is
                // still available as a same-object serialized source. Runtime
                // caches, delegates and inventory are deliberately not copied.
                var human = root.AddComponent<Humanoid>();
                CopySerializedHumanFields(sourcePlayer, human);
                var ai = root.AddComponent<MonsterAI>();

                Remove<PlayerController>(root);
                Remove<Talker>(root);
                Remove<Skills>(root);
                UnityEngine.Object.DestroyImmediate(sourcePlayer);
                HumanRigPolicy.Require(root.GetComponent<Player>() == null && root.GetComponents<Character>().Length == 1,
                    "Player controller removal did not leave exactly one NPC Humanoid.");

                view.m_persistent = true; view.m_distant = false;
                view.m_type = ZDO.ObjectType.Prioritized; view.m_syncInitialScale = false;
                transformSync.m_syncPosition = true; transformSync.m_syncRotation = true;
                transformSync.m_syncScale = false; transformSync.m_syncBodyVelocity = true;
                transformSync.m_characterParentSync = true;
                HumanNpcSettings.Configure(human, ai, body);
                ConfigureNativeVisual(root, human, vis, animationSync, outfit);
                root.AddComponent<HumanCompositionIdentity>();
                root.AddComponent<HumanCompanion>();
                root.AddComponent<HumanDoorTraversal>();
                root.AddComponent<HumanFollowPresence>();
                root.AddComponent<HumanCompanionPosture>();
                root.AddComponent<HumanResources>().Configure(player.GetComponent<Player>());
                root.AddComponent<HumanEnvironmentStatus>();
                root.AddComponent<HumanIdentity>();
                root.AddComponent<HumanPartyConfidence>();
                root.AddComponent<HumanCompanionAmbient>();
                root.AddComponent<HumanEquipmentReadiness>();
                root.AddComponent<HumanCombatDefense>();
                root.AddComponent<HumanPartyEquipment>();
                root.AddComponent<HumanCleric>();
                root.AddComponent<HumanUtilityTorch>();
                Validate(root, false);
                return root;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        static void CopySerializedHumanFields(Player source, Humanoid target)
        {
            for (Type type = typeof(Humanoid); type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (field.IsStatic || field.IsInitOnly || field.IsNotSerialized) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), true)) continue;
                    field.SetValue(target, field.GetValue(source));
                }
            }
        }

        static void ConfigureNativeVisual(GameObject root, Humanoid human, VisEquipment vis,
            ZSyncAnimation sync, GameObject[] outfit)
        {
            var visual = root.transform.Find("Visual");
            HumanRigPolicy.Require(visual != null, "Native human Visual root missing from Player clone.");
            ValidateVisualSource(visual.gameObject, vis);
            vis.m_models = CloneModels(vis.m_models);
            vis.m_clothColliders = new List<MagicaCloth2.ColliderComponent>(vis.m_clothColliders);
            vis.m_isPlayer = true; // body models, armor textures and hair, NOT Player.IsPlayer().
            vis.m_playerComponent = null;
            vis.m_nViewOverride = null;
            vis.m_randomModelIndex = false;
            vis.m_npcHairChance = 0f; vis.m_npcBeardChance = 0f;
            vis.m_isArmorStand = false;

            human.m_nViewOverride = null;
            ValidateNativeWeapon(human.m_unarmedWeapon == null ? null : human.m_unarmedWeapon.gameObject);
            ValidateNativeWeapon(outfit.Single(item => item.name == "SwordBronze"));
            human.m_defaultItems = (GameObject[])outfit.Clone();
            human.m_randomWeapon = new GameObject[0]; human.m_randomArmor = new GameObject[0];
            human.m_randomShield = new GameObject[0]; human.m_randomSets = new Humanoid.ItemSet[0];
            human.m_randomItems = new Humanoid.RandomItem[0];
            human.m_deathEffects = new EffectList(); // Corpse/tombstone lifecycle is not copied.
            human.m_hitEffects = new EffectList();
            human.m_deathAnimation = true;

            sync.m_syncBools = new List<string>(sync.m_syncBools);
            if (!sync.m_syncBools.Contains("emote_sit")) sync.m_syncBools.Add("emote_sit");
            if (!sync.m_syncBools.Contains("crouching")) sync.m_syncBools.Add("crouching");
            sync.m_syncFloats = new List<string>(sync.m_syncFloats);
            sync.m_syncInts = new List<string>(sync.m_syncInts);
            if (!sync.m_syncInts.Contains("crafting")) sync.m_syncInts.Add("crafting");
            var foot = root.GetComponent<FootStep>();
            HumanRigPolicy.Require(foot != null, "Native human footstep graph missing from Player clone.");
            foot.m_effects = CloneFootEffects(foot.m_effects);
            foreach (var animator in root.GetComponentsInChildren<Animator>(true))
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;

            var appearance = root.AddComponent<HumanAppearance>();
            appearance.RequiredItems = outfit.Select(item => item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name).ToArray();
            appearance.WeaponName = outfit.Single(item => item.name == "SwordBronze").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
        }

        static void Remove<T>(GameObject root) where T : Component
        {
            var component = root.GetComponent<T>();
            if (component != null) UnityEngine.Object.DestroyImmediate(component);
        }

        static void ValidateNativeWeapon(GameObject prefab)
        {
            var drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            HumanRigPolicy.Require(drop != null, "Native human weapon item is missing.");
            var shared = drop.m_itemData.m_shared;
            foreach (var attack in new[] { shared.m_attack, shared.m_secondaryAttack })
                HumanRigPolicy.Require(attack != null && !attack.m_hitFriendly && !attack.m_attackKillsSelf,
                    "Native human attack must respect faction filtering and safe cancellation.");
        }

        static void ValidateVisualSource(GameObject visual, VisEquipment vis)
        {
            HumanRigPolicy.Require(visual.GetComponentsInChildren<Player>(true).Length == 0 && visual.GetComponentsInChildren<ZNetView>(true).Length == 0,
                "Human visual contains a controller or network identity.");
            foreach (var script in visual.GetComponentsInChildren<MonoBehaviour>(true))
            {
                HumanRigPolicy.Require(script != null, "Human visual contains a missing script.");
                var name = script.GetType().FullName;
                HumanRigPolicy.Require(name == "CharacterAnimEvent" || name == "AnimationEffect" ||
                    name == "MagicaCloth2.MagicaSphereCollider" || name == "MagicaCloth2.MagicaCapsuleCollider", "Unreviewed native visual behaviour: " + name);
            }
            var animators = visual.GetComponentsInChildren<Animator>(true);
            HumanRigPolicy.Require(animators.Length == 1 && animators[0].GetComponent<CharacterAnimEvent>() != null, "Human animator/event receiver topology changed.");
            HumanRigPolicy.Require(animators[0].avatar != null && animators[0].avatar.isValid && animators[0].avatar.isHuman && animators[0].runtimeAnimatorController != null,
                "Human avatar/controller is missing or invalid.");
            HumanRigPolicy.Require(vis.m_bodyModel != null && vis.m_bodyModel.sharedMesh != null && vis.m_bodyModel.sharedMaterials.Length >= 2,
                "Human body mesh requires native skin and hair materials.");
            HumanRigPolicy.ValidateBoneNames(vis.m_bodyModel.bones.Select(b => b == null ? null : b.name));
        }

        internal static void Validate(GameObject root, bool live)
        {
            HumanRigPolicy.Require(root != null && root.GetComponents<Character>().Length == 1 && root.GetComponent<Humanoid>() != null, "Exactly one native Humanoid required.");
            HumanRigPolicy.Require(root.GetComponents<BaseAI>().Length == 1 && root.GetComponent<MonsterAI>() != null, "Exactly one native AI required.");
            var view = root.GetComponent<ZNetView>();
            HumanRigPolicy.Require(view != null && (!live || (view.IsValid() && view.GetZDO() != null)), "Native network view missing/invalid.");
            HumanRigPolicy.Require(root.GetComponent<ZSyncTransform>() != null && root.GetComponent<ZSyncAnimation>() != null && root.GetComponent<Rigidbody>() != null && root.GetComponent<CapsuleCollider>() != null, "Native movement/synchronization components missing.");
            HumanRigPolicy.Require(root.GetComponentsInChildren<Player>(true).Length == 0, "Player lifecycle found on human NPC.");
            HumanRigPolicy.Require(root.GetComponentsInChildren<ZNetView>(true).Length == 1, "Human NPC has more than one network identity.");
            var visual = root.transform.Find("Visual");
            var vis = root.GetComponent<VisEquipment>();
            HumanRigPolicy.Require(visual != null && vis != null && vis.m_isPlayer && vis.m_playerComponent == null, "Human visual mode/linkage is invalid.");
            HumanRigPolicy.Require(vis.m_bodyModel != null && vis.m_bodyModel.sharedMesh != null && vis.m_bodyModel.sharedMaterials.Length >= 2, "Human body renderer invalid.");
            HumanRigPolicy.ValidateBoneNames(vis.m_bodyModel.bones.Select(b => b == null ? null : b.name));
            var bodyAnimator = visual.GetComponent<Animator>();
            HumanRigPolicy.Require(bodyAnimator != null && bodyAnimator.GetComponent<CharacterAnimEvent>() != null && bodyAnimator.avatar != null && bodyAnimator.avatar.isValid && bodyAnimator.avatar.isHuman && bodyAnimator.runtimeAnimatorController != null, "Human animation graph invalid.");
            HumanRigPolicy.Require(root.GetComponentsInChildren<Animator>(true).Length == 1, "Multiple body animators.");
            foreach (var mesh in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                HumanRigPolicy.Require(mesh.rootBone == null || mesh.rootBone.IsChildOf(visual), "Renderer root escaped human rig.");
                foreach (var bone in mesh.bones) HumanRigPolicy.Require(bone != null && bone.IsChildOf(visual), "Renderer bone escaped human rig.");
            }
            foreach (var hand in new[] { vis.m_leftHand, vis.m_rightHand, vis.m_helmet, vis.m_backShield, vis.m_backMelee, vis.m_backTwohandedMelee, vis.m_backBow, vis.m_backTool, vis.m_backAtgeir })
                HumanRigPolicy.Require(hand != null && hand.IsChildOf(visual), "Equipment attachment escaped human rig.");
            HumanRigPolicy.Require(root.GetComponent<Character>().m_eye != null && root.GetComponent<Character>().m_eye.IsChildOf(root.transform), "NPC eye is not local.");
        }

        // Pure managed-container copies. Unity asset references are deliberately
        // preserved, never instantiated or mutated by these helpers.
        internal static VisEquipment.PlayerModel[] CloneModels(VisEquipment.PlayerModel[] source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var result = new VisEquipment.PlayerModel[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var model = source[i];
                HumanRigPolicy.Require(model != null, "Native human model record is null at index " + i);
                result[i] = new VisEquipment.PlayerModel { m_mesh = model.m_mesh, m_baseMaterial = model.m_baseMaterial };
            }
            return result;
        }

        internal static List<FootStep.StepEffect> CloneFootEffects(List<FootStep.StepEffect> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var result = new List<FootStep.StepEffect>(source.Count);
            for (int i = 0; i < source.Count; i++)
            {
                var effect = source[i];
                HumanRigPolicy.Require(effect != null, "Native human footstep record is null at index " + i);
                result.Add(new FootStep.StepEffect {
                    m_name = effect.m_name, m_motionType = effect.m_motionType, m_material = effect.m_material,
                    m_effectPrefabs = effect.m_effectPrefabs == null ? new GameObject[0] : (GameObject[])effect.m_effectPrefabs.Clone()
                });
            }
            return result;
        }

    }
}

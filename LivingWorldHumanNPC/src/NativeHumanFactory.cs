using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Fresh, explicitly configured human NPC component composition. Only the
    // human visual subtree is instantiated; no character/template root is cloned.
    internal static class NativeHumanFactory
    {
        static readonly string[] VisualFields = {
            "m_bodyModel", "m_leftHand", "m_rightHand", "m_helmet", "m_backShield",
            "m_backMelee", "m_backTwohandedMelee", "m_backBow", "m_backTool", "m_backAtgeir",
            "m_useAllTrails", "m_overrideHairMaterial", "m_disableHairShadows"
        };
        static GameObject definitions;

        internal static GameObject Create(string id, GameObject player, GameObject[] outfit)
        {
            HumanRigPolicy.Require(!String.IsNullOrEmpty(id) && player != null, "Human identity and native visual source required.");
            if (definitions == null)
            {
                definitions = new GameObject("LivingWorld_HumanDefinitions");
                definitions.SetActive(false);
                UnityEngine.Object.DontDestroyOnLoad(definitions);
            }
            var root = new GameObject(id);
            root.SetActive(false); // NO MonoBehaviour exists before this point.
            root.transform.SetParent(definitions.transform, false);
            try
            {
                HumanRigPolicy.Require(!root.activeInHierarchy, "Human definition parent must remain inactive.");
                var characterLayer = LayerMask.NameToLayer("character");
                HumanRigPolicy.Require(characterLayer >= 0, "Native character physics layer missing.");
                root.layer = characterLayer;
                root.AddComponent<CapsuleCollider>();
                var body = root.AddComponent<Rigidbody>();
                var view = root.AddComponent<ZNetView>();
                view.m_persistent = true; view.m_distant = false;
                view.m_type = ZDO.ObjectType.Prioritized; view.m_syncInitialScale = false;
                var transformSync = root.AddComponent<ZSyncTransform>();
                transformSync.m_syncPosition = true; transformSync.m_syncRotation = true;
                transformSync.m_syncScale = false; transformSync.m_syncBodyVelocity = true;
                transformSync.m_characterParentSync = true;
                root.AddComponent<ZSyncAnimation>();
                var human = root.AddComponent<Humanoid>();
                var ai = root.AddComponent<MonsterAI>();
                HumanNpcSettings.Configure(human, ai, body);
                ApplyVisual(root, player, outfit);
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
                root.SetActive(true); // Inactive parent: native Awake still has NOT run.
                return root;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        static void ApplyVisual(GameObject clone, GameObject player, GameObject[] outfit)
        {
            var playerCharacter = player.GetComponent<Player>();
            var playerVis = player.GetComponent<VisEquipment>();
            var originalVisual = player.transform.Find("Visual");
            HumanRigPolicy.Require(playerCharacter != null && playerVis != null && originalVisual != null, "Native human visual topology changed.");
            ValidateVisualSource(originalVisual.gameObject, playerVis);
            string sourceBefore = SourceFingerprint(player);

            var humanVisual = UnityEngine.Object.Instantiate(originalVisual.gameObject, clone.transform, false);
            humanVisual.name = "Visual_HumanStaged";
            var mapped = new Dictionary<UnityEngine.Object, UnityEngine.Object>();
            MapTree(originalVisual, humanVisual.transform, mapped);
            mapped[player] = clone; mapped[player.transform] = clone.transform;
            mapped[player.GetComponent<ZNetView>()] = clone.GetComponent<ZNetView>();

            var vis = clone.AddComponent<VisEquipment>();
            foreach (string name in VisualFields)
            {
                var field = typeof(VisEquipment).GetField(name, BindingFlags.Instance | BindingFlags.Public);
                HumanRigPolicy.Require(field != null, "Native visual field changed: " + name);
                field.SetValue(vis, CopyMapped(field.GetValue(playerVis), mapped));
            }
            vis.m_models = CloneModels(playerVis.m_models);
            vis.m_clothColliders = playerVis.m_clothColliders.Select(collider => (MagicaCloth2.ColliderComponent)CopyMapped(collider, mapped)).ToList();
            vis.m_isPlayer = true; // body models, armor textures and hair, NOT Player.IsPlayer().
            vis.m_playerComponent = null;
            vis.m_nViewOverride = null;
            vis.m_randomModelIndex = false;
            vis.m_npcHairChance = 0f; vis.m_npcBeardChance = 0f;
            vis.m_isArmorStand = false;
            humanVisual.name = "Visual";
            humanVisual.SetActive(true);

            var humanoid = clone.GetComponent<Humanoid>();
            humanoid.m_nViewOverride = null;
            humanoid.m_unarmedWeapon = playerCharacter.m_unarmedWeapon;
            ValidateNativeWeapon(humanoid.m_unarmedWeapon == null ? null : humanoid.m_unarmedWeapon.gameObject);
            ValidateNativeWeapon(outfit.Single(item => item.name == "SwordBronze"));
            humanoid.m_defaultItems = (GameObject[])outfit.Clone();
            humanoid.m_randomWeapon = new GameObject[0]; humanoid.m_randomArmor = new GameObject[0];
            humanoid.m_randomShield = new GameObject[0]; humanoid.m_randomSets = new Humanoid.ItemSet[0];
            humanoid.m_randomItems = new Humanoid.RandomItem[0];
            humanoid.m_deathEffects = new EffectList(); // Corpse/tombstone lifecycle is not copied.
            humanoid.m_hitEffects = new EffectList();
            humanoid.m_deathAnimation = true;
            humanoid.m_eye = RemapEye(playerCharacter.m_eye, player.transform, clone.transform, mapped);

            var capsule = clone.GetComponent<CapsuleCollider>();
            var sourceCapsule = player.GetComponent<CapsuleCollider>();
            HumanRigPolicy.Require(capsule != null && sourceCapsule != null, "Native body capsule missing.");
            capsule.center = sourceCapsule.center; capsule.radius = sourceCapsule.radius;
            capsule.height = sourceCapsule.height; capsule.direction = sourceCapsule.direction;
            capsule.sharedMaterial = sourceCapsule.sharedMaterial;
            capsule.isTrigger = false;

            var sync = clone.GetComponent<ZSyncAnimation>();
            var sourceSync = player.GetComponent<ZSyncAnimation>();
            HumanRigPolicy.Require(sync != null && sourceSync != null, "Native animation synchronization missing.");
            sync.m_syncBools = new List<string>(sourceSync.m_syncBools);
            if (!sync.m_syncBools.Contains("emote_sit")) sync.m_syncBools.Add("emote_sit");
            if (!sync.m_syncBools.Contains("crouching")) sync.m_syncBools.Add("crouching");
            sync.m_syncFloats = new List<string>(sourceSync.m_syncFloats);
            sync.m_syncInts = new List<string>(sourceSync.m_syncInts);
            if (!sync.m_syncInts.Contains("crafting")) sync.m_syncInts.Add("crafting");
            sync.m_smoothCharacterSpeeds = sourceSync.m_smoothCharacterSpeeds;
            ReplaceFootStep(clone, player, mapped);
            // Preserve native Player Animator controller/avatar/update mode and root-motion
            // setting exactly; no hand-authored animation playback or pose forcing.
            var appearance = clone.AddComponent<HumanAppearance>();
            appearance.RequiredItems = outfit.Select(item => item.GetComponent<ItemDrop>().m_itemData.m_shared.m_name).ToArray();
            appearance.WeaponName = outfit.Single(item => item.name == "SwordBronze").GetComponent<ItemDrop>().m_itemData.m_shared.m_name;
            HumanRigPolicy.Require(sourceBefore == SourceFingerprint(player), "The human visual source changed during construction.");
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

        static Transform RemapEye(Transform eye, Transform sourceRoot, Transform targetRoot, Dictionary<UnityEngine.Object, UnityEngine.Object> mapped)
        {
            HumanRigPolicy.Require(eye != null && eye.IsChildOf(sourceRoot), "Native eye is missing or external.");
            UnityEngine.Object replacement;
            if (mapped.TryGetValue(eye, out replacement)) return (Transform)replacement;
            var own = new GameObject("HumanEye").transform;
            own.SetParent(targetRoot, false);
            own.localPosition = sourceRoot.InverseTransformPoint(eye.position);
            own.localRotation = Quaternion.Inverse(sourceRoot.rotation) * eye.rotation;
            return own;
        }

        static void ReplaceFootStep(GameObject clone, GameObject player, Dictionary<UnityEngine.Object, UnityEngine.Object> mapped)
        {
            var original = player.GetComponent<FootStep>();
            HumanRigPolicy.Require(original != null, "Native human footstep source missing.");
            var feet = clone.AddComponent<FootStep>();
            feet.m_footlessFootsteps = original.m_footlessFootsteps;
            feet.m_footlessTriggerDistance = original.m_footlessTriggerDistance;
            feet.m_footstepCullDistance = original.m_footstepCullDistance;
            feet.m_feet = original.m_feet.Select(f => (Transform)CopyMapped(f, mapped)).ToArray();
            feet.m_effects = CloneFootEffects(original.m_effects);
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

        static void MapTree(Transform source, Transform target, Dictionary<UnityEngine.Object, UnityEngine.Object> mapped)
        {
            mapped[source] = target; mapped[source.gameObject] = target.gameObject;
            var originals = source.GetComponents<Component>(); var copies = target.GetComponents<Component>();
            HumanRigPolicy.Require(originals.Length == copies.Length && source.childCount == target.childCount, "Visual clone topology mismatch.");
            for (int i = 0; i < originals.Length; i++) { HumanRigPolicy.Require(originals[i] != null && copies[i] != null, "Missing visual component."); mapped[originals[i]] = copies[i]; }
            for (int i = 0; i < source.childCount; i++) MapTree(source.GetChild(i), target.GetChild(i), mapped);
        }

        static object CopyMapped(object value, Dictionary<UnityEngine.Object, UnityEngine.Object> mapped)
        {
            if (value == null) return null;
            var native = value as UnityEngine.Object;
            if (native != null)
            {
                UnityEngine.Object replacement;
                if (mapped.TryGetValue(native, out replacement)) return replacement;
                HumanRigPolicy.Require(!(native is Component) && !(native is GameObject), "Unmapped source hierarchy reference: " + native.name);
                return native; // Read-only mesh/material asset; never modified by this factory.
            }
            var type = value.GetType();
            if (type.IsValueType || type == typeof(string)) return value;
            throw new InvalidOperationException("Unreviewed visual configuration type: " + type.FullName);
        }

        static string SourceFingerprint(GameObject player)
        {
            var vis = player.GetComponent<VisEquipment>();
            return player.GetInstanceID() + ":" + player.GetComponentsInChildren<Component>(true).Length + ":" +
                vis.m_bodyModel.GetInstanceID() + ":" + vis.m_isPlayer + ":" + vis.m_models.Length + ":" +
                String.Join(",", vis.m_bodyModel.bones.Select(b => b.GetInstanceID().ToString()).ToArray());
        }
    }
}

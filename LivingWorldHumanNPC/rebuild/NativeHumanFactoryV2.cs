using System;
using System.Reflection;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    internal static class NativeHumanFactoryV2
    {
        internal static GameObject Create(string prefabId, GameObject playerPrefab, GameObject[] defaultItems, string role)
        {
            if (String.IsNullOrEmpty(prefabId)) throw new ArgumentException("Prefab identity is required.", "prefabId");
            if (playerPrefab == null || playerPrefab.GetComponent<Player>() == null)
                throw new InvalidOperationException("The native Player prefab is unavailable.");
            if (defaultItems == null || defaultItems.Length == 0)
                throw new InvalidOperationException("A native equipment set is required.");

            GameObject root = PrefabManager.Instance.CreateClonedPrefab(prefabId, playerPrefab);
            if (root == null || root.activeInHierarchy)
                throw new InvalidOperationException("The native Player clone was not staged in Jotunn's inactive prefab container.");

            try
            {
                Player player = root.GetComponent<Player>();
                Humanoid human = root.AddComponent<Humanoid>();

                // A Player is a Humanoid. Copy its complete Unity-serialized
                // Humanoid/Character state before removing Player; reconstructing
                // only selected public fields produced the broken 0.3.x actors.
                CopySerializedHumanState(player, human);

                Remove<PlayerController>(root);
                Remove<Talker>(root);
                Remove<Skills>(root);
                UnityEngine.Object.DestroyImmediate(player);

                if (root.GetComponents<Character>().Length != 1 || root.GetComponent<Player>() != null)
                    throw new InvalidOperationException("The clone did not resolve to exactly one non-Player Humanoid.");

                ConfigureCharacter(human, defaultItems);
                ConfigureNetwork(root);
                ConfigureVisuals(root);
                ConfigureAi(root.AddComponent<MonsterAI>());

                HumanAppearanceV2 identity = root.AddComponent<HumanAppearanceV2>();
                identity.Role = role;
                HumanCompanionV2 companion = root.AddComponent<HumanCompanionV2>();
                companion.Role = role;
                root.AddComponent<HumanTraversalModule>();
                root.AddComponent<HumanEquipmentModule>();
                root.AddComponent<HumanBehaviorController>();

                Validate(root);
                return root;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }

        internal static void CopySerializedHumanState(Player source, Humanoid destination)
        {
            if (source == null || destination == null) throw new ArgumentNullException(source == null ? "source" : "destination");
            Type type = typeof(Humanoid);
            while (type != null && type != typeof(MonoBehaviour))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                foreach (FieldInfo field in fields)
                {
                    bool serialized = (field.IsPublic && !field.IsNotSerialized) ||
                        field.IsDefined(typeof(SerializeField), false);
                    if (serialized && !field.IsInitOnly) field.SetValue(destination, field.GetValue(source));
                }
                type = type.BaseType;
            }
        }

        static void ConfigureCharacter(Humanoid human, GameObject[] defaultItems)
        {
            human.m_name = "Living World Companion";
            human.m_group = "LivingWorldCompanions";
            human.m_faction = Character.Faction.Players;
            human.m_boss = false;
            human.m_health = 250f;
            human.m_regenAllHPTime = 180f;
            human.m_turnSpeed = 300f;
            human.m_runTurnSpeed = 300f;
            human.m_aiCannotTargetOthers = false;
            human.m_canSwim = true;
            human.m_tolerateWater = true;
            human.m_defaultItems = (GameObject[])defaultItems.Clone();
            human.m_randomWeapon = new GameObject[0];
            human.m_randomArmor = new GameObject[0];
            human.m_randomShield = new GameObject[0];
            human.m_randomItems = new Humanoid.RandomItem[0];
            human.m_randomSets = new Humanoid.ItemSet[0];
        }

        static void ConfigureNetwork(GameObject root)
        {
            ZNetView view = root.GetComponent<ZNetView>();
            ZSyncTransform sync = root.GetComponent<ZSyncTransform>();
            if (view == null || sync == null || root.GetComponent<ZSyncAnimation>() == null)
                throw new InvalidOperationException("The native human network graph is incomplete.");
            view.m_persistent = true;
            view.m_distant = false;
            view.m_type = ZDO.ObjectType.Prioritized;
            sync.m_syncPosition = true;
            sync.m_syncRotation = true;
            sync.m_syncBodyVelocity = true;
            sync.m_characterParentSync = true;
        }

        static void ConfigureVisuals(GameObject root)
        {
            VisEquipment visuals = root.GetComponent<VisEquipment>();
            if (visuals == null || root.transform.Find("Visual") == null)
                throw new InvalidOperationException("The native human visual graph is incomplete.");
            visuals.m_randomModelIndex = false;
            visuals.m_hairPrefabCount = 0;
            visuals.m_beardPrefabCount = 0;
            foreach (Animator animator in root.GetComponentsInChildren<Animator>(true))
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        }

        static void ConfigureAi(MonsterAI ai)
        {
            ai.m_pathAgentType = Pathfinding.AgentType.Humanoid;
            ai.m_viewRange = 30f;
            ai.m_viewAngle = 90f;
            ai.m_hearRange = 9999f;
            ai.m_alertRange = 20f;
            ai.m_randomCircleInterval = 2f;
            ai.m_randomMoveInterval = 30f;
            ai.m_randomMoveRange = 3f;
            ai.m_moveMinAngle = 60f;
            ai.m_smoothMovement = true;
            ai.m_avoidFire = false;
            ai.m_afraidOfFire = false;
            ai.m_avoidWater = false;
            ai.m_avoidLava = true;
            ai.m_avoidLavaFlee = true;
            ai.m_skipLavaTargets = true;
            ai.m_fleeInLava = true;
            ai.m_fleeIfHurtWhenTargetCantBeReached = true;
            ai.m_fleeIfNotAlerted = false;
            ai.m_fleeIfLowHealth = 0f;
            ai.m_enableHuntPlayer = false;
            ai.m_attackPlayerObjects = false;
            ai.m_circulateWhileCharging = true;
            ai.m_circulateWhileChargingFlying = false;
            ai.m_interceptTimeMin = 0f;
            ai.m_interceptTimeMax = 2f;
            // The native chase distance is measured from the actor's original
            // spawn point, not from its moving leader. A travelling companion
            // therefore disables that obsolete fixed-origin limit and uses the
            // native tamed follow-target alert range as its moving combat bound.
            ai.m_maxChaseDistance = 0f;
            ai.m_minAttackInterval = 0f;
            ai.m_circleTargetInterval = 8f;
            ai.m_circleTargetDuration = 6f;
            ai.m_circleTargetDistance = 8f;
            ai.m_sleeping = false;
            ai.m_noiseWakeup = false;
        }

        static void Remove<T>(GameObject root) where T : Component
        {
            T component = root.GetComponent<T>();
            if (component != null) UnityEngine.Object.DestroyImmediate(component);
        }

        internal static void Validate(GameObject root)
        {
            if (root == null || root.GetComponents<Character>().Length != 1 || root.GetComponent<Humanoid>() == null)
                throw new InvalidOperationException("Exactly one native Humanoid is required.");
            if (root.GetComponents<BaseAI>().Length != 1 || root.GetComponent<MonsterAI>() == null)
                throw new InvalidOperationException("Exactly one native MonsterAI is required.");
            if (root.GetComponent<Player>() != null || root.GetComponent<PlayerController>() != null ||
                root.GetComponent<Talker>() != null || root.GetComponent<Skills>() != null)
                throw new InvalidOperationException("Player-only behavior survived the human factory.");
            if (root.GetComponent<ZNetView>() == null || root.GetComponent<VisEquipment>() == null ||
                root.GetComponent<ZSyncTransform>() == null || root.GetComponent<ZSyncAnimation>() == null ||
                root.GetComponent<Rigidbody>() == null || root.GetComponent<CapsuleCollider>() == null)
                throw new InvalidOperationException("The native network, visual, animation, or physics graph is incomplete.");
            if (root.GetComponent<HumanAppearanceV2>() == null || root.GetComponent<HumanCompanionV2>() == null ||
                root.GetComponent<HumanBehaviorController>() == null || root.GetComponent<HumanTraversalModule>() == null ||
                root.GetComponent<HumanEquipmentModule>() == null)
                throw new InvalidOperationException("The modular identity, binding, behavior, traversal, or equipment component is missing.");
        }
    }
}

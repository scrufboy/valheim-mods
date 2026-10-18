using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    internal static class HumanNpcSettings
    {
        internal static void ConfigureAlertVoice(MonsterAI ai, GameObject nativeHumanVoice)
        {
            if (ai == null || nativeHumanVoice == null)
                throw new InvalidOperationException("Native human alert voice unavailable.");
            var voices = new EffectList();
            voices.m_effectPrefabs = new[] { new EffectList.EffectData {
                m_prefab = nativeHumanVoice, m_enabled = true, m_variant = -1
            } };
            ai.m_alertedEffects = voices;
        }

        // Product-owned values. No creature or Player behaviour settings are copied.
        // Human walking/swimming scales deliberately match the authored human rig.
        internal static void Configure(Humanoid h, MonsterAI ai, Rigidbody body)
        {
            body.mass = 10f; body.useGravity = true; body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            h.m_name = "LWC Human Scout"; h.m_group = "LivingWorldCompanions";
            h.m_faction = Character.Faction.Players; h.m_health = 200f;
            h.m_boss = false; h.m_bossEvent = ""; h.m_defeatSetGlobalKey = ""; h.m_dreamCinematic = "";
            h.m_speed = 4f; h.m_walkSpeed = 1.6f; h.m_runSpeed = 7f; h.m_acceleration = 0.8f;
            h.m_crouchSpeed = 2f; h.m_turnSpeed = 300f; h.m_runTurnSpeed = 300f;
            h.m_jumpForce = 8f; h.m_jumpForceForward = 2f; h.m_airControl = 0.1f;
            h.m_flying = false; h.m_canSwim = true; h.m_swimDepth = 1.5f;
            h.m_swimSpeed = 2f; h.m_swimTurnSpeed = 100f; h.m_swimAcceleration = 0.05f;
            h.m_tolerateWater = true; h.m_tolerateFire = false; h.m_tolerateSmoke = false;
            h.m_tolerateTar = false; h.m_hideHud = false; h.m_aiSkipTarget = false;
            h.m_aiCannotTargetOthers = false; h.m_disableWhileSleeping = false;
            h.m_weakSpots = new WeakSpot[0];
            h.m_defaultItems = new GameObject[0]; h.m_randomWeapon = new GameObject[0];
            h.m_randomArmor = new GameObject[0]; h.m_randomShield = new GameObject[0];
            h.m_randomSets = new Humanoid.ItemSet[0]; h.m_randomItems = new Humanoid.RandomItem[0];
            h.m_hitEffects = new EffectList(); h.m_deathEffects = new EffectList();
            ai.m_pathAgentType = Pathfinding.AgentType.Humanoid;
            ai.m_viewRange = 30f; ai.m_viewAngle = 90f; ai.m_hearRange = 9999f;
            ai.m_mistVision = false; ai.m_moveMinAngle = 90f; ai.m_smoothMovement = true;
            ai.m_serpentMovement = false; ai.m_jumpInterval = 0f;
            ai.m_randomMoveInterval = 30f; ai.m_randomMoveRange = 3f; ai.m_randomCircleInterval = 2f;
            ai.m_randomFly = false; ai.m_chanceToTakeoff = 0f;
            // Native AvoidFire pre-empts Follow and chooses random circling even
            // when afraidOfFire is false. Humans must approach homes/hearths.
            // This does NOT change actual fire/smoke damage or tolerance.
            ai.m_avoidFire = false; ai.m_afraidOfFire = false; ai.m_avoidWater = true;
            ai.m_avoidLava = true; ai.m_skipLavaTargets = true; ai.m_avoidLavaFlee = true;
            ai.m_aggravatable = false; ai.m_passiveAggresive = false;
            ai.m_spawnMessage = ""; ai.m_deathMessage = ""; ai.m_alertedMessage = "";
            ai.m_idleSound = new EffectList(); ai.m_alertedEffects = new EffectList();
            ai.m_idleSoundChance = 0f; ai.m_idleSoundInterval = 30f;
            ai.m_canBeAlerted = true; ai.m_alertRange = 20f;
            ai.m_enableHuntPlayer = false; ai.m_attackPlayerObjects = false;
            ai.m_maxChaseDistance = 300f; ai.m_minAttackInterval = 0f;
            ai.m_circulateWhileCharging = true; ai.m_circulateWhileChargingFlying = false;
            // Native short lateral reposition windows; these are human-owned
            // candidate settings, not inherited template tuning. A nonzero
            // waypoint interval prevents choosing a new direction every frame.
            ai.m_circleTargetDistance = 8f; ai.m_circleTargetDuration = 6f; ai.m_circleTargetInterval = 8f;
            ai.m_interceptTimeMin = 0f; ai.m_interceptTimeMax = 2f;
            ai.m_fleeIfHurtWhenTargetCantBeReached = true; ai.m_fleeIfNotAlerted = false;
            ai.m_fleeIfLowHealth = 0f; ai.m_fleeInLava = true;
            ai.m_sleeping = false; ai.m_noiseWakeup = false; ai.m_fallAsleepDistance = 0f;
            ai.m_wakeupEffects = new EffectList(); ai.m_sleepEffects = new EffectList();
            ai.m_avoidLand = false; ai.m_consumeItems = new List<ItemDrop>();
        }
    }
}

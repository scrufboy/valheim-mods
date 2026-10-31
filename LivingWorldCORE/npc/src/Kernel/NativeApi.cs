// Every private Valheim member Living World touches, in one place.
// tools/native_gate.py checks this exact list against the installed
// assembly_valheim.dll before a build is allowed to ship.
using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.Core
{
    internal static class NativeApi
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // Humanoid hands. Verified in IL (Valheim 1.0.20):
        //  - EquipItem clears BOTH m_hiddenLeftItem and m_hiddenRightItem on every path.
        //  - UnequipItem clears a hidden slot only when that exact item is the hidden one.
        //  - HideHandItems moves hand items into the hidden (back) slots.
        //  - ShowHandItems re-equips the hidden items.
        internal static readonly FieldInfo LeftItem = Field(typeof(Humanoid), "m_leftItem");
        internal static readonly FieldInfo RightItem = Field(typeof(Humanoid), "m_rightItem");
        internal static readonly FieldInfo HiddenLeftItem = Field(typeof(Humanoid), "m_hiddenLeftItem");
        internal static readonly FieldInfo HiddenRightItem = Field(typeof(Humanoid), "m_hiddenRightItem");
        internal static readonly MethodInfo ShowHandItems = Method(typeof(Humanoid), "ShowHandItems", typeof(bool), typeof(bool));

        // MonsterAI encounter state. SetTarget(null) is a native no-op, so release
        // uses the same fields MonsterAI.UpdateTarget clears in its own abandon branch.
        internal static readonly FieldInfo TargetCreature = Field(typeof(MonsterAI), "m_targetCreature");
        internal static readonly FieldInfo TargetStatic = Field(typeof(MonsterAI), "m_targetStatic");
        internal static readonly FieldInfo TimeSinceAttacking = Field(typeof(MonsterAI), "m_timeSinceAttacking");
        internal static readonly FieldInfo UpdateTargetTimer = Field(typeof(MonsterAI), "m_updateTargetTimer");
        internal static readonly MethodInfo FindEnemy = Method(typeof(BaseAI), "FindEnemy");
        // Worn armor (read-only) and the two armor rules Valheim implements only for Player.
        internal static readonly FieldInfo ChestItem = Field(typeof(Humanoid), "m_chestItem");
        internal static readonly FieldInfo LegItem = Field(typeof(Humanoid), "m_legItem");
        internal static readonly FieldInfo HelmetItem = Field(typeof(Humanoid), "m_helmetItem");
        internal static readonly FieldInfo ShoulderItem = Field(typeof(Humanoid), "m_shoulderItem");
        internal static readonly MethodInfo GetBodyArmor = typeof(Character).GetMethod("GetBodyArmor", Instance, null, Type.EmptyTypes, null);
        internal static readonly MethodInfo ApplyArmorDamageMods = typeof(Character).GetMethod("ApplyArmorDamageMods", Instance);
        internal static readonly MethodInfo EquipBestWeapon = typeof(Humanoid).GetMethod("EquipBestWeapon", Instance);
        internal static readonly MethodInfo ApplyDamage = typeof(Character).GetMethod("ApplyDamage", Instance);
        internal static readonly MethodInfo RpcDamage = Method(typeof(Character), "RPC_Damage", typeof(long), typeof(HitData));
        internal static readonly MethodInfo SetAlerted = Method(typeof(MonsterAI), "SetAlerted", typeof(bool));
        internal static readonly MethodInfo LookAt = Method(typeof(BaseAI), "LookAt", typeof(Vector3));

        // Blocking. Only Player.SetControls ever sets this (verified in IL), which is why NPCs
        // never block. Humanoid.UpdateBlock/IsBlocking/BlockAttack handle everything after it.
        internal static readonly FieldInfo Blocking = Field(typeof(Character), "m_blocking");

        // Chained melee. Player continues a combo by calling StartAttack inside the swing's chain window;
        // Humanoid.StartAttack allows that mid-swing only if HaveQueuedChain() (false for every
        // non-Player). Attack.Start then advances the level because the previous attack is the same
        // animation and m_timeSinceLastAttack is 0 while InAttack (verified in IL, 1.0.20).
        internal static readonly FieldInfo CurrentAttack = Field(typeof(Humanoid), "m_currentAttack");
        internal static readonly FieldInfo CurrentAttackIsSecondary = Field(typeof(Humanoid), "m_currentAttackIsSecondary");
        internal static readonly FieldInfo AttackChainLevel = Field(typeof(Attack), "m_currentAttackCainLevel");
        internal static readonly FieldInfo AttackChainLevels = Field(typeof(Attack), "m_attackChainLevels");
        internal static readonly MethodInfo CanStartChainAttack = Method(typeof(Attack), "CanStartChainAttack");
        internal static readonly MethodInfo HaveQueuedChain = Method(typeof(Humanoid), "HaveQueuedChain");
        internal static readonly MethodInfo UpdateAttack = Method(typeof(Humanoid), "UpdateAttack", typeof(float));
        internal static readonly MethodInfo HumanoidStartAttack = Method(typeof(Humanoid), "StartAttack", typeof(Character), typeof(bool));
        // 1.0.32.4 voice: heavy exertion at the committed attack frame; one exertion per attack; stagger pain on entry.
        internal static readonly MethodInfo AttackTrigger = Method(typeof(Attack), "OnAttackTrigger");
        internal static readonly FieldInfo AttackCharacter = Field(typeof(Attack), "m_character");
        // 1.0.32.12 throws: aim the companion's own throw attack at a planned point (verified in IL, 1.0.20:
        // FireProjectileBurst reads velocity, random-velocity and launch angle, then GetProjectileSpawnPoint gives aimDir).
        internal static readonly FieldInfo AttackWeapon = Field(typeof(Attack), "m_weapon");
        internal static readonly MethodInfo FireProjectileBurst = Method(typeof(Attack), "FireProjectileBurst");
        internal static readonly MethodInfo GetProjectileSpawnPoint = typeof(Attack).GetMethod("GetProjectileSpawnPoint", Instance, null,
            new[] { typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType() }, null);
        internal static readonly MethodInfo RpcStagger = Method(typeof(Character), "RPC_Stagger", typeof(long), typeof(Vector3));
        // 1.0.32.6 voice: death-line dev log (observes only).
        internal static readonly MethodInfo CharacterOnDeath = Method(typeof(Character), "OnDeath");

        // 1.0.32.15 party clouds (verified in decompile, Valheim 1.0.21): Aoe.ShouldHit picks targets with the cloud's own
        // m_hitOwner / m_hitSame / m_hitFriendly; Aoe.OnHit builds the HitData from m_damage; m_owner is the thrower.
        internal static readonly FieldInfo AoeOwner = Field(typeof(Aoe), "m_owner");
        internal static readonly MethodInfo AoeAwake = Method(typeof(Aoe), "Awake");
        internal static readonly MethodInfo AoeShouldHit = Method(typeof(Aoe), "ShouldHit", typeof(Collider));
        internal static readonly MethodInfo AoeOnHit = Method(typeof(Aoe), "OnHit", typeof(Collider), typeof(Vector3));

        // Visual identity.
        internal static readonly FieldInfo HairInstance = Field(typeof(VisEquipment), "m_hairItemInstance");
        internal static readonly FieldInfo BeardInstance = Field(typeof(VisEquipment), "m_beardItemInstance");
        internal static readonly FieldInfo CurrentHairHash = Field(typeof(VisEquipment), "m_currentHairItemHash");
        internal static readonly FieldInfo CurrentBeardHash = Field(typeof(VisEquipment), "m_currentBeardItemHash");
        internal static readonly FieldInfo VisualObject = Field(typeof(VisEquipment), "m_visual");
        internal static readonly FieldInfo RequestedModel = Field(typeof(VisEquipment), "m_modelIndex");
        internal static readonly FieldInfo RequestedSkinColor = Field(typeof(VisEquipment), "m_skinColor");
        internal static readonly FieldInfo RequestedHairColor = Field(typeof(VisEquipment), "m_hairColor");
        internal static readonly FieldInfo RequestedHairHash = Field(typeof(VisEquipment), "m_hairItem");
        internal static readonly FieldInfo RequestedBeardHash = Field(typeof(VisEquipment), "m_beardItem");
        internal static readonly MethodInfo UpdateVisuals = Method(typeof(VisEquipment), "UpdateVisuals");
        internal static readonly MethodInfo SetHairEquipped = Method(typeof(VisEquipment), "SetHairEquipped", typeof(int));
        internal static readonly MethodInfo SetBeardEquipped = Method(typeof(VisEquipment), "SetBeardEquipped", typeof(int));
        internal static readonly MethodInfo UpdateLodgroup = Method(typeof(VisEquipment), "UpdateLodgroup");
        internal static readonly MethodInfo UpdateColors = Method(typeof(VisEquipment), "UpdateColors");

        // Wet presentation: native Wet is authored player-only for its VFX.
        internal static readonly MethodInfo TriggerStartEffects =
            typeof(StatusEffect).GetMethod("TriggerStartEffects", Instance, null, Type.EmptyTypes, null);

        internal static readonly MethodInfo CloneObject =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        internal static string MissingMembers()
        {
            var missing = new System.Collections.Generic.List<string>();
            foreach (FieldInfo field in typeof(NativeApi).GetFields(BindingFlags.Static | BindingFlags.NonPublic))
                if (field.GetValue(null) == null) missing.Add(field.Name);
            return missing.Count == 0 ? "" : String.Join(", ", missing.ToArray());
        }

        static FieldInfo Field(Type type, string name)
        {
            FieldInfo field = type.GetField(name, Instance);
            return field != null && !field.IsStatic && !field.IsLiteral ? field : null;
        }

        static MethodInfo Method(Type type, string name, params Type[] parameters)
        {
            return type.GetMethod(name, Instance, null, parameters, null);
        }
    }
}

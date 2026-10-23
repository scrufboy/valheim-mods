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
        internal static readonly MethodInfo SetAlerted = Method(typeof(MonsterAI), "SetAlerted", typeof(bool));
        internal static readonly MethodInfo LookAt = Method(typeof(BaseAI), "LookAt", typeof(Vector3));

        // Visual identity.
        internal static readonly FieldInfo HairInstance = Field(typeof(VisEquipment), "m_hairItemInstance");
        internal static readonly FieldInfo BeardInstance = Field(typeof(VisEquipment), "m_beardItemInstance");
        internal static readonly FieldInfo CurrentHairHash = Field(typeof(VisEquipment), "m_currentHairItemHash");
        internal static readonly FieldInfo CurrentBeardHash = Field(typeof(VisEquipment), "m_currentBeardItemHash");

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

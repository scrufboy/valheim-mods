using System.Reflection;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Verified in assembly_valheim (1.0.20): Character.GetBodyArmor returns 0 and
    /// Character.ApplyArmorDamageMods does nothing; only Player overrides them to count worn
    /// armor (chest, legs, helmet, shoulder + status-effect armor mods) and the armor's damage
    /// resistances. Companions are Humanoids, so every 1.0.x companion fought with armor 0
    /// while visibly wearing iron. These postfixes give companions exactly the Player rules,
    /// nothing more. Reads only; returns values to native damage code.
    /// </summary>
    public static class ArmorParity
    {
        static readonly FieldInfo[] Slots = { NativeApi.ChestItem, NativeApi.LegItem, NativeApi.HelmetItem, NativeApi.ShoulderItem };

        static bool IsCompanion(Character c) { return c != null && !(c is Player) && c.GetComponent<NpcKernel>() != null; }

        public static void BodyArmorPostfix(Character __instance, ref float __result)
        {
            if (!IsCompanion(__instance)) return;
            float armor = 0f;
            foreach (FieldInfo slot in Slots)
            {
                ItemDrop.ItemData item = slot.GetValue(__instance) as ItemDrop.ItemData;
                if (item != null) armor += item.GetArmor();
            }
            SEMan seman = __instance.GetSEMan();
            if (seman != null) seman.ApplyArmorMods(ref armor);
            __result = armor;
        }

        public static void ArmorModsPostfix(Character __instance, ref HitData.DamageModifiers __0)
        {
            if (!IsCompanion(__instance)) return;
            foreach (FieldInfo slot in Slots)
            {
                ItemDrop.ItemData item = slot.GetValue(__instance) as ItemDrop.ItemData;
                if (item != null && item.m_shared != null) __0.Apply(item.m_shared.m_damageModifiers);
            }
        }
    }
}

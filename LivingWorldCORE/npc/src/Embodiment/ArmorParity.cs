using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
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

        /// <summary>Replaces the single IsPlayer() gate in front of armor in Character.RPC_Damage.</summary>
        public static bool CountsArmor(Character c) { return c.IsPlayer() || IsCompanion(c); }

        public static int ArmorGatesPatched { get; private set; }
        public static int StaggerGatesPatched { get; private set; }
        public static int BackstabGatesPatched { get; private set; }

        /// <summary>Backstab gate: "target AI not alerted". The player has no AI and is never backstabbed.</summary>
        public static bool AlertedForDamage(BaseAI ai)
        {
            if (ai.IsAlerted()) return true;
            return ai.GetComponent<NpcKernel>() != null;
        }

        /// <summary>
        /// Character.RPC_Damage (1.0.20): resistances, then `if (IsPlayer()) { ApplyArmor(GetBodyArmor());
        /// DamageArmorDurability(hit); }`. 1.0.9 live: companions read armor 29 but a Draugr axe still
        /// did 45.5 (should be ~18) because that gate skipped them. Swap exactly that one call -- the
        /// IsPlayer immediately followed by GetBodyArmor -- for CountsArmor. Anything else unchanged;
        /// if the shape is not found exactly once, the method is left untouched and the log says so.
        /// </summary>
        public static IEnumerable<CodeInstruction> RpcDamageTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);
            MethodInfo isPlayer = AccessTools.Method(typeof(Character), "IsPlayer");
            MethodInfo counts = AccessTools.Method(typeof(ArmorParity), "CountsArmor");
            var hits = new List<int>();
            for (int i = 0; i < code.Count; i++)
            {
                if (!code[i].Calls(isPlayer)) continue;
                for (int j = i + 1; j <= i + 4 && j < code.Count; j++)
                    if (code[j].Calls(NativeApi.GetBodyArmor)) { hits.Add(i); break; }
            }
            if (hits.Count != 1)
            {
                CorePlugin.Warn("armor gate not found exactly once in Character.RPC_Damage (" + hits.Count + "); companion armor will not reduce damage");
                return code;
            }
            code[hits[0]].opcode = OpCodes.Call;
            code[hits[0]].operand = counts;
            ArmorGatesPatched = 1;

            // 1.0.10 live: killing blows of 288-618 from 45-60 hits. Same method, two more player-only
            // rules in RPC_Damage: staggered non-players take x2, and an unalerted AI takes the weapon's
            // backstab bonus. Companions get the Player answer at exactly those two gates.
            MethodInfo staggering = AccessTools.Method(typeof(Character), "IsStaggering");
            var stagger = new List<int>();
            for (int i = 0; i < code.Count; i++)
            {
                if (!code[i].Calls(staggering)) continue;
                for (int j = i + 1; j <= i + 4 && j < code.Count; j++)
                    if (code[j].Calls(isPlayer)) { stagger.Add(j); break; }
            }
            if (stagger.Count == 1) { code[stagger[0]].opcode = OpCodes.Call; code[stagger[0]].operand = counts; StaggerGatesPatched = 1; }
            else CorePlugin.Warn("stagger gate not found exactly once (" + stagger.Count + ")");

            MethodInfo isAlerted = AccessTools.Method(typeof(BaseAI), "IsAlerted");
            var alert = new List<int>();
            for (int i = 0; i < code.Count; i++) if (code[i].Calls(isAlerted)) alert.Add(i);
            if (alert.Count == 1)
            {
                code[alert[0]].opcode = OpCodes.Call;
                code[alert[0]].operand = AccessTools.Method(typeof(ArmorParity), "AlertedForDamage");
                BackstabGatesPatched = 1;
            }
            else CorePlugin.Warn("backstab gate not found exactly once (" + alert.Count + ")");
            return code;
        }

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

using System;
using System.Text;
using Jotunn.Managers;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Read-only: writes the game's real numbers for each weapon family and gear tier to the log at
    /// startup ("[LWC] DATA ..."), so heavy-attack, stamina and block rules are built from the
    /// installed game's values, not remembered ones. Also proves every kit item name exists.
    /// </summary>
    internal static class CombatDataReport
    {
        static readonly string[] Weapons =
        {
            "SwordIron", "SwordBlackmetal", "AxeIron", "AxeBlackMetal", "MaceIron", "MaceNeedle", "KnifeBlackMetal",
            "AtgeirBronze", "AtgeirIron", "AtgeirBlackmetal", "Battleaxe", "SledgeIron", "SledgeStagbreaker",
            "SpearBronze", "SpearElderbark", "BowFineWood", "BowHuntsman", "BowDraugrFang", "ArrowWood", "ArrowNeedle",
            "ShieldBanded", "ShieldSilver", "ShieldBlackmetal", "ShieldBlackmetalTower", "ShieldSerpentscale"
        };
        static readonly string[] Armor =
        {
            "ArmorIronChest", "ArmorIronLegs", "HelmetIron", "ArmorWolfChest", "ArmorWolfLegs", "HelmetDrake",
            "ArmorPaddedCuirass", "ArmorPaddedGreaves", "HelmetPadded", "CapeDeerHide", "CapeWolf", "CapeLinen", "CapeLox"
        };

        internal static void Write(Action<string> log)
        {
            GameObject player = PrefabManager.Instance.GetPrefab("Player");
            Player p = player == null ? null : player.GetComponent<Player>();
            if (p != null)
                log(string.Format("DATA player: baseStamina {0} regen {1} regenTimeMult {2} regenDelay {3} run {4} dodge {5} jump {6} blockDrain {7} perfectBlockDrain {8}",
                    p.m_baseStamina, p.m_staminaRegen, p.m_staminaRegenTimeMultiplier, p.m_staminaRegenDelay, p.m_runStaminaDrain,
                    p.m_dodgeStaminaUsage, p.m_jumpStaminaUsage, p.m_blockStaminaDrain, p.m_perfectBlockStaminaDrain));
            foreach (string name in Weapons) log(Describe(name, true));
            foreach (string name in Armor) log(Describe(name, false));
        }

        static string Describe(string name, bool weapon)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            ItemDrop drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            if (drop == null) return "DATA " + name + ": MISSING";
            ItemDrop.ItemData.SharedData s = drop.m_itemData.m_shared;
            var b = new StringBuilder("DATA " + name + ": " + s.m_itemType + " skill " + s.m_skillType + " maxQ " + s.m_maxQuality);
            if (!weapon)
            {
                b.Append(" armor " + s.m_armor + " +" + s.m_armorPerLevel + "/q; mods " + Mods(s));
                b.Append(" moveMod " + s.m_movementModifier);
                return b.ToString();
            }
            b.Append(" dmg [" + Dmg(s.m_damages) + "] +[" + Dmg(s.m_damagesPerLevel) + "]/q force " + s.m_attackForce +
                " backstab " + s.m_backstabBonus + " block " + s.m_blockPower + " +" + s.m_blockPowerPerLevel +
                "/q deflect " + s.m_deflectionForce + " parryX " + s.m_timedBlockBonus + " moveMod " + s.m_movementModifier +
                " aiRange " + s.m_aiAttackRange + " aiAngle " + s.m_aiAttackMaxAngle + " interval " + s.m_aiAttackInterval);
            b.Append(" | P " + Atk(s.m_attack) + " | S " + Atk(s.m_secondaryAttack));
            return b.ToString();
        }

        static string Atk(Attack a)
        {
            if (a == null || String.IsNullOrEmpty(a.m_attackAnimation)) return "none";
            return string.Format("{0} anim {1} chain {2} stam {3} dmgX {4} staggerX {5} forceX {6} lastChainX {7} range {8} angle {9} rayW {10} speed {11}",
                a.m_attackType, a.m_attackAnimation, a.m_attackChainLevels, a.m_attackStamina, a.m_damageMultiplier,
                a.m_staggerMultiplier, a.m_forceMultiplier, a.m_lastChainDamageMultiplier, a.m_attackRange, a.m_attackAngle,
                a.m_attackRayWidth, a.m_speedFactor);
        }

        static string Dmg(HitData.DamageTypes d)
        {
            var b = new StringBuilder();
            Add(b, "blunt", d.m_blunt); Add(b, "slash", d.m_slash); Add(b, "pierce", d.m_pierce); Add(b, "fire", d.m_fire);
            Add(b, "frost", d.m_frost); Add(b, "lightning", d.m_lightning); Add(b, "poison", d.m_poison); Add(b, "spirit", d.m_spirit);
            return b.ToString().Trim();
        }

        static void Add(StringBuilder b, string k, float v) { if (v != 0f) b.Append(k + " " + v + " "); }

        static string Mods(ItemDrop.ItemData.SharedData s)
        {
            if (s.m_damageModifiers == null || s.m_damageModifiers.Count == 0) return "none";
            var b = new StringBuilder();
            foreach (HitData.DamageModPair m in s.m_damageModifiers) b.Append(m.m_type + "=" + m.m_modifier + " ");
            return b.ToString().Trim();
        }
    }
}

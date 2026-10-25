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

        static readonly string[] Foods =
        {
            "QueensJam", "Sausages", "TurnipStew", "ShocklateSmoothie", "BlackSoup", "SerpentStew", "WolfMeatSkewer",
            "OnionSoup", "Eyescream", "LoxPie", "FishWraps", "BloodPudding", "Bread", "MeadStaminaMinor", "MeadStaminaMedium"
        };

        static readonly string[] Throws = { "BombOoze", "BombBile", "Greydwarf_throw", "GoblinShaman_attack_fireball" };

        // Bombs and AI throws: the attack, its projectile, and the blast it leaves (who it can hurt).
        static string Throw(string name)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            ItemDrop drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            if (drop == null) return "DATA throw " + name + ": MISSING";
            ItemDrop.ItemData.SharedData s = drop.m_itemData.m_shared;
            Attack a = s.m_attack;
            var b = new StringBuilder("DATA throw " + name + ": " + s.m_itemType + " skill " + s.m_skillType + " dmg [" + Dmg(s.m_damages) +
                "] aiRange " + s.m_aiAttackRange + " aiMin " + s.m_aiAttackRangeMin + " interval " + s.m_aiAttackInterval);
            if (a == null) return b.Append(" no attack").ToString();
            b.Append(" | " + a.m_attackType + " anim " + a.m_attackAnimation + " stam " + a.m_attackStamina + " vel " + a.m_projectileVel +
                " velMin " + a.m_projectileVelMin + " acc " + a.m_projectileAccuracy + " accMin " + a.m_projectileAccuracyMin +
                " launchAngle " + a.m_launchAngle + " ammo " + s.m_ammoType);
            GameObject projectile = a.m_attackProjectile;
            Projectile p = projectile == null ? null : projectile.GetComponent<Projectile>();
            if (p == null) return b.Append(" | projectile " + (projectile == null ? "none" : projectile.name + " (no Projectile)")).ToString();
            b.Append(" | projectile " + projectile.name + " gravity " + p.m_gravity + " aoe " + p.m_aoe + " hitFriendly " + p.m_hitFriendly +
                " noDamageFriendly " + p.m_noDamageFriendly + " hitOwner " + p.m_hitOwner + " dmg [" + Dmg(p.m_damage) + "]");
            GameObject spawn = p.m_spawnOnHit;
            Aoe aoe = spawn == null ? null : spawn.GetComponent<Aoe>();
            if (spawn != null && aoe == null) aoe = spawn.GetComponentInChildren<Aoe>();
            if (aoe != null)
                b.Append(" | blast " + spawn.name + " radius " + aoe.m_radius + " ttl " + aoe.m_ttl + " interval " + aoe.m_hitInterval +
                    " dmg [" + Dmg(aoe.m_damage) + "] hitFriendly " + aoe.m_hitFriendly + " hitOwner " + aoe.m_hitOwner +
                    " hitSame " + aoe.m_hitSame + " hitEnemy " + aoe.m_hitEnemy + " se " + (aoe.m_statusEffect ?? ""));
            else b.Append(" | blast " + (spawn == null ? "none" : spawn.name + " (no Aoe)"));
            return b.ToString();
        }

        static string Food(string name)
        {
            GameObject prefab = PrefabManager.Instance.GetPrefab(name);
            ItemDrop drop = prefab == null ? null : prefab.GetComponent<ItemDrop>();
            if (drop == null) return "DATA food " + name + ": MISSING";
            ItemDrop.ItemData.SharedData s = drop.m_itemData.m_shared;
            return "DATA food " + name + ": health " + s.m_food + " stamina " + s.m_foodStamina + " regen " + s.m_foodRegen +
                " burn " + s.m_foodBurnTime + " type " + s.m_itemType;
        }

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
            foreach (string name in Foods) log(Food(name));
            foreach (string name in Throws) log(Throw(name));
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

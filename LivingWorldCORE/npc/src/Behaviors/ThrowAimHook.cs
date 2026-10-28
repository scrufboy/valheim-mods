using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// 1.0.32.12: a companion's planned throw lands where it was planned. Applies only to the Living World throw item
    /// in a companion's hands with a pending aim; every other attack in the game is untouched. The native spread
    /// (projectile accuracy) is applied after this, so throws still miss like the Souls Pillager's.
    /// </summary>
    public static class ThrowAimHook
    {
        public const string ThrowItem = "LWC_BombThrow";
        public struct Aim { public Vector3 Point; public float Speed; }
        static readonly Dictionary<Humanoid, Aim> pending = new Dictionary<Humanoid, Aim>();

        public static void Set(Humanoid who, Vector3 point) { pending[who] = new Aim { Point = point, Speed = ThrowPlanner.Speed }; }
        public static void Clear(Humanoid who) { pending.Remove(who); }

        static bool Mine(Attack attack, out Humanoid who, out Aim aim)
        {
            who = NativeApi.AttackCharacter.GetValue(attack) as Humanoid; aim = default(Aim);
            if (who == null || !pending.TryGetValue(who, out aim)) return false;
            var weapon = NativeApi.AttackWeapon.GetValue(attack) as ItemDrop.ItemData;
            return weapon != null && weapon.m_dropPrefab != null && weapon.m_dropPrefab.name == ThrowItem;
        }

        public static void FirePrefix(Attack __instance)
        {
            Humanoid who; Aim aim;
            if (!Mine(__instance, out who, out aim)) return;
            __instance.m_launchAngle = 0f;                 // our elevation replaces the fixed launch angle
            __instance.m_randomVelocity = false;
            __instance.m_projectileVel = aim.Speed;
            __instance.m_projectileVelMin = aim.Speed;
        }

        public static void SpawnPostfix(Attack __instance, ref Vector3 spawnPoint, ref Vector3 aimDir)
        {
            Humanoid who; Aim aim;
            if (!Mine(__instance, out who, out aim)) return;
            Vector3 to = aim.Point - spawnPoint;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            float d = flat.magnitude;
            if (d < .5f) return;
            float e = ThrowPlanner.Elevation(d, to.y);
            if (float.IsNaN(e)) e = Mathf.PI / 4f;            // out of reach: the longest throw
            aimDir = (flat / d) * Mathf.Cos(e) + Vector3.up * Mathf.Sin(e);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// 1.0.32.15 party clouds (Brian 2026-10-29). A lingering bomb cloud (poison or fire over time) thrown by the leader
    /// or a companion now also reaches the party, at a reduced fraction of its damage, through the game's own Aoe hit and
    /// the native SE_Poison / SE_Burning. Enemy clouds are untouched (full effect, native). Only for the duration of one
    /// native hit decision does this flip the cloud's own owner/same/friendly switches, and only for a party target; the
    /// damage scale is applied to that one hit and restored. No second damage writer, no new status.
    ///
    /// It also keeps a short list of live harmful clouds so companions know where poison is (HazardAwareness).
    /// </summary>
    public static class PartyClouds
    {
        public struct ShouldHitState { public bool Flipped, Owner, Same, Friendly; }
        public struct HitState { public bool Scaled; public HitData.DamageTypes Damage; }

        static readonly HashSet<Aoe> live = new HashSet<Aoe>();
        static float nextExposeLog;
        public static int Exposures { get; private set; }
        internal static IEnumerable<Aoe> Live { get { return live; } }

        public static void AwakePostfix(Aoe __instance)
        {
            if (__instance != null) live.Add(__instance);      // damage may be set later by Setup; judged at use time
        }

        internal static void Prune() { live.RemoveWhere(a => a == null || !a.isActiveAndEnabled); }

        internal static Character Owner(Aoe aoe)
        {
            return NativeApi.AoeOwner == null || aoe == null ? null : NativeApi.AoeOwner.GetValue(aoe) as Character;
        }

        internal static CloudActor Kind(Character c)
        {
            if (c == null) return CloudActor.Other;
            if (c is Player) return CloudActor.Player;
            return c.GetComponent<NpcKernel>() != null ? CloudActor.Companion : CloudActor.Other;
        }

        static float FractionFor(Aoe aoe, Character target)
        {
            if (aoe == null || target == null || !CloudExposure.Harmful(aoe.m_damage.m_poison, aoe.m_damage.m_fire, aoe.m_ttl)) return 0f;
            Character owner = Owner(aoe);
            if (owner == null) return 0f;
            return CloudExposure.Fraction(Kind(owner), Kind(target), owner == target, target == Player.m_localPlayer,
                CorePlugin.PartyCloudCompanions, CorePlugin.PartyCloudPlayer, CorePlugin.PartyCloudFraction);
        }

        static Character TargetOf(Collider collider)
        {
            GameObject go = collider == null ? null : Projectile.FindHitObject(collider);
            return go == null ? null : go.GetComponent<Character>();
        }

        public static void ShouldHitPrefix(Aoe __instance, Collider collider, out ShouldHitState __state)
        {
            __state = new ShouldHitState();
            if (FractionFor(__instance, TargetOf(collider)) <= 0f) return;
            __state.Flipped = true;
            __state.Owner = __instance.m_hitOwner; __state.Same = __instance.m_hitSame; __state.Friendly = __instance.m_hitFriendly;
            __instance.m_hitOwner = true; __instance.m_hitSame = true; __instance.m_hitFriendly = true;
        }

        public static Exception ShouldHitFinalizer(Aoe __instance, ShouldHitState __state, Exception __exception)
        {
            if (__state.Flipped && __instance != null)
            {
                __instance.m_hitOwner = __state.Owner; __instance.m_hitSame = __state.Same; __instance.m_hitFriendly = __state.Friendly;
            }
            return __exception;
        }

        public static void OnHitPrefix(Aoe __instance, Collider collider, out HitState __state)
        {
            __state = new HitState();
            Character target = TargetOf(collider);
            float f = FractionFor(__instance, target);
            if (f <= 0f) return;
            __state.Scaled = true;
            __state.Damage = __instance.m_damage;
            HitData.DamageTypes d = __instance.m_damage;
            d.Modify(f);
            __instance.m_damage = d;
            Exposures++;
            if (CorePlugin.DevHazardLog && Time.time >= nextExposeLog)
            {
                nextExposeLog = Time.time + 1f;
                Character owner = Owner(__instance);
                CorePlugin.Report("CLOUD-EXPOSE " + target.GetHoverName() + " in " + (owner == null ? "?" : owner.GetHoverName()) +
                    "'s cloud x" + f.ToString("0.00") + " poison=" + d.m_poison.ToString("0.0") + " fire=" + d.m_fire.ToString("0.0") +
                    " t=" + Time.time.ToString("0.00"));
            }
        }

        public static Exception OnHitFinalizer(Aoe __instance, HitState __state, Exception __exception)
        {
            if (__state.Scaled && __instance != null) __instance.m_damage = __state.Damage;
            return __exception;
        }
    }

    /// <summary>
    /// What a companion knows about clouds: its party's clouds always, any other harmful cloud it can see (player
    /// parity: a player sees the cloud). Read-only; the fall-back mover does the stepping out.
    /// </summary>
    public sealed class HazardAwareness
    {
        const float SightRange = 25f;
        static int viewMask;
        readonly HazardEscapePolicy policy = new HazardEscapePolicy();
        public bool Escaping { get { return policy.Active; } }
        public Vector3 From { get; private set; }
        public int Escapes { get; private set; }

        public bool Step(NpcKernel kernel, Humanoid me, float now)
        {
            if (!CorePlugin.HazardAwareness || me == null) return false;
            if (viewMask == 0) viewMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "viewblock", "vehicle");
            bool dead = me.IsDead();
            Aoe best = null; float bestD = float.PositiveInfinity, bestR = 0f;
            Vector3 at = me.transform.position;
            foreach (Aoe a in PartyClouds.Live)
            {
                if (a == null || !a.isActiveAndEnabled || !CloudExposure.Harmful(a.m_damage.m_poison, a.m_damage.m_fire, a.m_ttl)) continue;
                Vector3 c = a.transform.position;
                if (Mathf.Abs(c.y - at.y) > a.m_radius + 2f) continue;
                float d = new Vector2(c.x - at.x, c.z - at.z).magnitude;
                if (d > SightRange || d - a.m_radius >= bestD - bestR) continue;
                if (!Hurts(a, me) || !Knows(a, me, c, d)) continue;
                best = a; bestD = d; bestR = a.m_radius;
            }
            bool was = policy.Active;
            bool active = policy.Step(now, best == null ? float.PositiveInfinity : bestD, bestR, dead);
            if (active && best != null) From = best.transform.position;
            if (active && !was)
            {
                Escapes++;
                if (CorePlugin.DevHazardLog)
                {
                    Character owner = PartyClouds.Owner(best);
                    CorePlugin.Report("HAZARD-ESCAPE " + me.GetHoverName() + " begin cloud=" + best.name.Replace("(Clone)", "") + " owner=" +
                        (owner == null ? "none" : owner.GetHoverName()) + " dist=" + bestD.ToString("0.0") + " r=" + bestR.ToString("0.0") + " t=" + now.ToString("0.00"));
                }
            }
            if (!active && was && CorePlugin.DevHazardLog)
                CorePlugin.Report("HAZARD-ESCAPE " + me.GetHoverName() + " end " + policy.Outcome + " t=" + now.ToString("0.00"));
            return active;
        }

        static bool Hurts(Aoe a, Humanoid me)
        {
            Character owner = PartyClouds.Owner(a);
            CloudActor k = PartyClouds.Kind(owner);
            if (k != CloudActor.Other)                                   // party cloud: only if party exposure is on
                return CloudExposure.Fraction(k, CloudActor.Companion, owner == me, false, CorePlugin.PartyCloudCompanions, false, CorePlugin.PartyCloudFraction) > 0f;
            return owner == null || BaseAI.IsEnemy(owner, me) || a.m_hitFriendly;   // enemy, ownerless or hits-all cloud
        }

        static bool Knows(Aoe a, Humanoid me, Vector3 centre, float distance)
        {
            if (PartyClouds.Kind(PartyClouds.Owner(a)) != CloudActor.Other) return true;   // the party knows its own poison
            if (distance <= a.m_radius + 1f) return true;                                    // standing in it
            Vector3 eye = me.m_eye != null ? me.m_eye.position : me.transform.position + Vector3.up * 1.6f;
            Vector3 to = centre + Vector3.up * .5f - eye;
            return !Physics.Raycast(eye, to.normalized, to.magnitude, viewMask);
        }
    }
}

using System;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Supplies the player-only rain exposure step to a non-Player human root.
    // The native Wet StatusEffect owns visuals, duration and stamina modifiers.
    // Native Character liquid contact remains independent and untouched.
    public sealed class HumanEnvironmentStatus : MonoBehaviour
    {
        const float Interval = 1f;
        static readonly MethodInfo TriggerStartEffects = typeof(StatusEffect).GetMethod("TriggerStartEffects",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Humanoid body;
        ZNetView view;
        float elapsed;
        bool reportedWet;
        bool wetPresentationReady;

        void Awake()
        {
            body = GetComponent<Humanoid>();
            view = GetComponent<ZNetView>();
        }

        void Update()
        {
            if (body == null || view == null || !view.IsValid() || !view.IsOwner() || body.IsDead()) return;
            elapsed += Time.deltaTime;
            if (elapsed < Interval) return;
            elapsed = 0f;

            bool raining = EnvMan.instance != null && EnvMan.IsWet();
            float cover;
            bool underRoof;
            Cover.GetCoverForPoint(body.GetCenterPoint(), out cover, out underRoof, 0.5f);
            bool shielded = ShieldGenerator.IsInsideShield(body.GetCenterPoint());
            if (raining && !underRoof && !shielded)
                body.GetSEMan().AddStatusEffect(SEMan.s_statusEffectWet, true, 0, 0f, -1);

            StatusEffect wetEffect = body.GetSEMan().GetStatusEffect(SEMan.s_statusEffectWet);
            bool wet = wetEffect != null;
            // Native Wet is authored with effectsOnlyOnPlayer=true. These actors
            // intentionally use a Humanoid root, so the real Wet stats apply but
            // its networked vfx_Wet start effect is skipped. Change only this
            // actor's cloned runtime status and replay its native start effect.
            if (wet && !wetPresentationReady)
            {
                if (wetEffect.m_effectsOnlyOnPlayer)
                {
                    if (TriggerStartEffects == null)
                        throw new MissingMethodException("StatusEffect.TriggerStartEffects");
                    wetEffect.m_effectsOnlyOnPlayer = false;
                    TriggerStartEffects.Invoke(wetEffect, null);
                }
                wetPresentationReady = !wetEffect.m_effectsOnlyOnPlayer;
            }
            if (!wet) wetPresentationReady = false;
            if (wet == reportedWet) return;
            reportedWet = wet;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("ENV " + body.m_name + ": wet=" + wet +
                "; rain=" + raining + "; roof=" + underRoof + "; shield=" + shielded +
                "; nativeDrip=" + wetPresentationReady);
        }
    }
}


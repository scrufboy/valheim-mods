using System;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Supplies the player-only rain exposure step to a non-Player human root.
    // The native Wet StatusEffect owns visuals, duration and stamina modifiers.
    // Native Character liquid contact remains independent and untouched.
    public sealed class HumanEnvironmentStatus : MonoBehaviour
    {
        const float Interval = 1f;
        Humanoid body;
        ZNetView view;
        float elapsed;
        bool reportedWet;

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

            bool wet = body.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectWet);
            if (wet == reportedWet) return;
            reportedWet = wet;
            var plugin = LivingWorld.ManualTest.ManualTestPlugin.Instance;
            if (plugin != null) plugin.Report("ENV " + body.m_name + ": wet=" + wet +
                "; rain=" + raining + "; roof=" + underRoof + "; shield=" + shielded);
        }
    }
}


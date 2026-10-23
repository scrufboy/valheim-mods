using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Supplies the player-only rain exposure step to a non-Player human. The native
    /// Wet status effect owns duration, modifiers and visuals. This is embodiment, not
    /// behavior: it writes no movement, hands, look or posture, so it holds no domain.
    /// Ported from the 0.3.45 adapter.
    /// </summary>
    public sealed class NpcWetStatus : MonoBehaviour
    {
        const float Interval = 1f;
        Humanoid body;
        ZNetView view;
        float elapsed;
        bool presentationReady;

        public bool Wet { get; private set; }
        public bool Raining { get; private set; }
        public bool UnderRoof { get; private set; }

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

            Raining = EnvMan.instance != null && EnvMan.IsWet();
            float cover;
            bool underRoof;
            Cover.GetCoverForPoint(body.GetCenterPoint(), out cover, out underRoof, .5f);
            UnderRoof = underRoof;
            bool shielded = ShieldGenerator.IsInsideShield(body.GetCenterPoint());
            if (Raining && !underRoof && !shielded)
                body.GetSEMan().AddStatusEffect(SEMan.s_statusEffectWet, true, 0, 0f);

            StatusEffect wet = body.GetSEMan().GetStatusEffect(SEMan.s_statusEffectWet);
            Wet = wet != null;
            // Native Wet is authored effectsOnlyOnPlayer=true; replay its start effect on
            // this actor's own runtime copy so the drip VFX shows. Global definition untouched.
            if (Wet && !presentationReady && wet.m_effectsOnlyOnPlayer && NativeApi.TriggerStartEffects != null)
            {
                wet.m_effectsOnlyOnPlayer = false;
                NativeApi.TriggerStartEffects.Invoke(wet, null);
            }
            presentationReady = Wet && (wet == null || !wet.m_effectsOnlyOnPlayer);
        }
    }
}

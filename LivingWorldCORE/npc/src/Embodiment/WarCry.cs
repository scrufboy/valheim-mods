using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Once per raid: when a raid starts and the leader is inside it, each companion roars (vanilla Roar
    /// emote + Berserker taunt) a moment apart, if WarCryPolicy allows it. Waits up to Window seconds for
    /// a clear moment; otherwise skipped. Decides nothing about combat.
    /// </summary>
    public static class WarCry
    {
        const float Window = 10f;
        static readonly Dictionary<int, State> States = new Dictionary<int, State>();
        public static event System.Action<NpcKernel, string> Cried;

        sealed class State { public object Raid; public float DueAt, Until; public bool Pending; }

        internal static void Tick(NpcKernel kernel)
        {
            if (kernel == null || kernel.Body == null || kernel.Body.Human == null || RandEventSystem.instance == null) return;
            Player leader = kernel.Frame.Senses.Leader;
            RandomEvent raid = RandEventSystem.instance.GetActiveEvent();
            int id = kernel.GetInstanceID();
            State st;
            if (!States.TryGetValue(id, out st)) { st = new State(); States[id] = st; }
            if (raid == null) { st.Raid = null; st.Pending = false; return; }
            if (!ReferenceEquals(st.Raid, raid))
            {
                st.Raid = raid;
                // RandEventSystem.IsInsideRandomEventArea's own rule (private; verified in IL): below 3000 m, XZ inside range.
                Vector3 p = leader == null ? Vector3.zero : leader.transform.position;
                bool near = leader != null && p.y <= 3000f && global::Utils.DistanceXZ(p, raid.m_pos) < raid.m_eventRange;
                st.Pending = near;
                st.DueAt = Time.time + Random.Range(0f, WarCryPolicy.PartnerDelayMax);
                st.Until = Time.time + Window;
            }
            if (!st.Pending || Time.time < st.DueAt) return;
            if (Time.time > st.Until) { st.Pending = false; return; }
            NpcBody body = kernel.Body;
            float nearest = Mathf.Min(kernel.NearestEngagedDistance, kernel.Frame.Senses.NearestAlertedHostileDistance);
            bool moving = body.Human.GetVelocity().magnitude > 1.5f;
            if (!WarCryPolicy.Allowed(true, nearest, body.Human.InAttack(), kernel.Frame.FallingBack, moving)) return;
            st.Pending = false;
            body.PlayEmote("roar");
            CompanionCombatVoice.SayWarCry(kernel);
            System.Action<NpcKernel, string> cried = Cried;
            if (cried != null) cried(kernel, raid.m_name);
        }
    }
}

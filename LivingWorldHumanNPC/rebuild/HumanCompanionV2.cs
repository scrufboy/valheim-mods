using System;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    public sealed class HumanCompanionV2 : MonoBehaviour
    {
        internal const string LeaderKey = "lwc_human_v2_leader";
        internal const string GenerationKey = "lwc_human_v2_party";

        public string Role = "guard";
        public string State
        {
            get
            {
                HumanBehaviorController controller = GetComponent<HumanBehaviorController>();
                return controller == null ? "behavior controller missing" : controller.Status;
            }
        }

        ZNetView view;
        Player leader;
        long leaderId;
        bool legacyTameChecked;

        public bool LegacyTameCleared { get; private set; }

        void Awake()
        {
            view = GetComponent<ZNetView>();
        }

        internal void Bind(Player player, string generation)
        {
            if (player == null || view == null || !view.IsValid() || !view.IsOwner())
                throw new InvalidOperationException("A locally owned living leader is required.");
            leaderId = player.GetPlayerID();
            leader = player;
            view.GetZDO().Set(LeaderKey, leaderId);
            view.GetZDO().Set(GenerationKey, generation);
            ClearLegacyTameState();
        }

        internal bool TryResolveLeader(out Player resolved)
        {
            if (view == null || !view.IsValid())
            {
                resolved = null;
                return false;
            }
            if (leaderId == 0L) leaderId = view.GetZDO().GetLong(LeaderKey, 0L);
            if (leader == null || leader.IsDead() || leader.GetPlayerID() != leaderId)
                leader = leaderId == 0L ? null : Player.GetPlayer(leaderId);
            ClearLegacyTameState();
            resolved = leader;
            return resolved != null;
        }

        void ClearLegacyTameState()
        {
            if (legacyTameChecked || view == null || !view.IsValid() || !view.IsOwner()) return;
            Humanoid human = GetComponent<Humanoid>();
            if (human == null) throw new InvalidOperationException("The companion's native Humanoid is missing.");
            if (human.IsTamed())
            {
                // 0.5.1 persisted this bit while borrowing a native pet range
                // rule. Human companions do not implement Tameable's feeding or
                // interaction lifecycle, so migrate only that obsolete save bit.
                human.SetTamed(false);
                LegacyTameCleared = true;
                HumanNpcPlugin.ReportStatic("Migrated " + human.m_name + " from the obsolete tame flag.");
            }
            legacyTameChecked = true;
        }
    }
}

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
            resolved = leader;
            return resolved != null;
        }
    }
}

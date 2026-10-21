using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LivingWorld.HumanNpcV2
{
    public sealed class HumanCompanionV2 : MonoBehaviour
    {
        internal const string LeaderKey = "lwc_human_v2_leader";
        internal const string GenerationKey = "lwc_human_v2_party";
        static readonly MethodInfo ShowHands = typeof(Humanoid).GetMethod("ShowHandItems",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(bool), typeof(bool) }, null);

        public string Role = "guard";
        public string State { get; private set; }

        ZNetView view;
        MonsterAI ai;
        Humanoid human;
        HumanAppearanceV2 appearance;
        Player leader;
        GameObject anchor;
        long leaderId;
        int slot = -1;
        int consecutiveFaults;
        float nextFaultLog;
        float nextTick;
        readonly CombatStateLatch combatState = new CombatStateLatch(25);

        void Awake()
        {
            view = GetComponent<ZNetView>();
            ai = GetComponent<MonsterAI>();
            human = GetComponent<Humanoid>();
            appearance = GetComponent<HumanAppearanceV2>();
            State = "awaiting party order";
        }

        internal void Bind(Player player, string generation)
        {
            if (player == null || view == null || !view.IsValid() || !view.IsOwner())
                throw new InvalidOperationException("A locally owned living leader is required.");
            leaderId = player.GetPlayerID();
            leader = player;
            view.GetZDO().Set(LeaderKey, leaderId);
            view.GetZDO().Set(GenerationKey, generation);
            AcquireFormation();
            State = "following " + player.GetPlayerName();
        }

        void Update()
        {
            if (Time.time < nextTick) return;
            nextTick = Time.time + .2f;

            try
            {
                TickState();
                consecutiveFaults = 0;
            }
            catch (Exception exception)
            {
                combatState.Retain();
                consecutiveFaults++;
                State = "state retained; retry " + consecutiveFaults;
                if (Time.time >= nextFaultLog)
                {
                    nextFaultLog = Time.time + 5f;
                    HumanNpcPlugin.WarnStatic(name + " companion tick failed; retaining state and retrying: " + exception.Message);
                }
            }
        }

        void TickState()
        {
            if (view == null || !view.IsValid() || !view.IsOwner() || ai == null || human == null)
            {
                State = "native actor unavailable; state retained";
                return;
            }
            if (appearance == null || !appearance.Ready)
            {
                State = "appearance verification pending; state retained";
                return;
            }

            if (leaderId == 0L) leaderId = view.GetZDO().GetLong(LeaderKey, 0L);
            if (leader == null || leader.IsDead() || leader.GetPlayerID() != leaderId)
                leader = leaderId == 0L ? null : Player.GetPlayer(leaderId);
            if (leader == null)
            {
                State = "leader reacquisition pending; state retained";
                return;
            }

            Character target = ai.GetTargetCreature();
            bool nativeCombat = target != null || ai.IsAlerted();
            bool wasCombat = combatState.Active;
            bool combatOwned = combatState.Observe(nativeCombat);
            if (nativeCombat)
            {
                if (!wasCombat)
                {
                    ReleaseFormation();
                    ai.SetFollowTarget(null);
                    if (ShowHands != null) ShowHands.Invoke(human, new object[] { false, true });
                }
                State = "native combat" + (target == null ? " alert" : " vs " + target.GetHoverName());
                return;
            }

            if (combatOwned)
            {
                State = "native combat clearing " + combatState.ClearScans + "/" + combatState.RequiredClearScans;
                return;
            }
            if (slot < 0 || anchor == null) AcquireFormation();
            if (anchor == null) { ai.SetFollowTarget(leader.gameObject); State = "following leader directly"; return; }
            anchor.transform.position = FormationLeases.Position(leader, slot);
            anchor.transform.rotation = leader.transform.rotation;
            if (ai.GetFollowTarget() != anchor) ai.SetFollowTarget(anchor);
            if (human.GetCurrentWeapon() != null) human.HideHandItems(false, true);
            State = "formation slot " + slot;
        }

        void AcquireFormation()
        {
            if (leader == null) return;
            slot = FormationLeases.Acquire(this, leader);
            if (anchor == null)
            {
                anchor = new GameObject("LWC formation anchor");
                anchor.hideFlags = HideFlags.HideAndDontSave;
            }
            anchor.transform.position = FormationLeases.Position(leader, slot);
        }

        void ReleaseFormation()
        {
            FormationLeases.Release(this);
            slot = -1;
            if (anchor != null) { Destroy(anchor); anchor = null; }
        }

        void OnDestroy() { ReleaseFormation(); }
    }

    internal static class FormationLeases
    {
        static readonly Dictionary<HumanCompanionV2, int> Claims = new Dictionary<HumanCompanionV2, int>();

        internal static int Acquire(HumanCompanionV2 actor, Player leader)
        {
            Cleanup();
            int existing;
            if (Claims.TryGetValue(actor, out existing)) return existing;
            Vector3 left = Position(leader, 0);
            Vector3 right = Position(leader, 1);
            int first = Vector3.Distance(actor.transform.position, left) <= Vector3.Distance(actor.transform.position, right) ? 0 : 1;
            if (Occupied(first)) first = 1 - first;
            Claims[actor] = first;
            return first;
        }

        internal static Vector3 Position(Player leader, int slot)
        {
            float side = slot == 0 ? -2.1f : 2.1f;
            return leader.transform.position + leader.transform.right * side - leader.transform.forward * 2.6f;
        }

        internal static void Release(HumanCompanionV2 actor) { Claims.Remove(actor); }

        static bool Occupied(int slot)
        {
            foreach (KeyValuePair<HumanCompanionV2, int> claim in Claims)
                if (claim.Key != null && claim.Value == slot) return true;
            return false;
        }

        static void Cleanup()
        {
            var dead = new List<HumanCompanionV2>();
            foreach (HumanCompanionV2 actor in Claims.Keys) if (actor == null) dead.Add(actor);
            foreach (HumanCompanionV2 actor in dead) Claims.Remove(actor);
        }
    }
}

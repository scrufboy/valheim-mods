using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Answers native MonsterAI's own question "which enemy?" (BaseAI.FindEnemy, asked every
    /// 2 s, or 6 s when no player is within 50 m, and never mid-attack). Verified in the
    /// installed IL: a null answer keeps the current target. So this is the single place a
    /// companion's target is chosen; nothing overwrites the game's choice afterwards.
    /// Reads only. Writes nothing native.
    /// </summary>
    public sealed class TargetSelector
    {
        const float HurtMemorySeconds = 8f;
        readonly NpcKernel kernel;
        readonly List<TargetCandidate> candidates = new List<TargetCandidate>(16);
        readonly List<Character> actors = new List<Character>(16);
        readonly List<NpcKernel> partners = new List<NpcKernel>(4);

        public string Reason { get; private set; }
        public int Switches { get; private set; }
        public int Decisions { get; private set; }
        public string LastChoiceName { get; private set; }

        public TargetSelector(NpcKernel kernel) { this.kernel = kernel; Reason = ""; LastChoiceName = ""; }

        public Character Choose()
        {
            Decisions++;
            NpcBody body = kernel.Body;
            Humanoid me = body.Human;
            NpcSenses s = kernel.Frame.Senses;
            Player leader = s.Leader;
            NpcPhase phase = kernel.Arbiter.Phase;
            bool encounter = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            CollectPartners(leader);
            Build(me, leader);
            Character current = body.Ai.GetTargetCreature();
            int currentId = current == null || current.IsDead() ? -1 : current.GetInstanceID();
            string reason;
            int pick = TargetPolicy.Choose(RoleOf(), candidates, currentId, encounter, s.LeaderCrouching,
                CorePlugin.TargetPriority, out reason);
            Reason = reason;
            if (pick < 0) return null;
            Character chosen = actors[pick];
            if (!ReferenceEquals(chosen, current)) { Switches++; LastChoiceName = chosen.GetHoverName(); }
            return chosen;
        }

        CombatRole RoleOf()
        {
            NpcAppearance a = kernel.GetComponent<NpcAppearance>();
            return a != null && a.Role == "archer" ? CombatRole.Archer : CombatRole.Guard;
        }

        void CollectPartners(Player leader)
        {
            partners.Clear();
            if (leader == null) return;
            long leaderId = leader.GetPlayerID();
            foreach (NpcKernel k in NpcKernel.All)
            {
                if (k == null || k == kernel || k.Body == null) continue;
                NpcLeaderBinding b = k.GetComponent<NpcLeaderBinding>();
                if (b != null && b.LeaderId == leaderId) partners.Add(k);
            }
        }

        void Build(Humanoid me, Player leader)
        {
            candidates.Clear();
            actors.Clear();
            Vector3 position = me.transform.position;
            Character hurtMe = kernel.RecentAttacker(HurtMemorySeconds);
            foreach (Character other in Character.GetAllCharacters())
            {
                if (candidates.Count == 16) break;
                if (other == null || other == me || other.IsDead() || other.m_aiSkipTarget) continue;
                if (!BaseAI.IsEnemy(me, other)) continue;
                float distance = Vector3.Distance(position, other.transform.position);
                if (distance > TargetPolicy.MaxRange) continue;
                BaseAI theirAi = other.GetBaseAI();
                if (theirAi != null && theirAi.IsSleeping()) continue;
                if (!kernel.Body.Ai.CanSenseTarget(other)) continue;
                MonsterAI monster = theirAi as MonsterAI;
                Character theirTarget = monster == null ? null : monster.GetTargetCreature();
                bool targetsPartner = false, hurtUs = ReferenceEquals(other, hurtMe);
                float partnerDistance = -1f;
                foreach (NpcKernel p in partners)
                {
                    Humanoid ph = p.Body.Human;
                    if (ph == null) continue;
                    float d = Vector3.Distance(ph.transform.position, other.transform.position);
                    if (partnerDistance < 0f || d < partnerDistance) partnerDistance = d;
                    if (ReferenceEquals(theirTarget, ph)) targetsPartner = true;
                    if (ReferenceEquals(p.RecentAttacker(HurtMemorySeconds), other)) hurtUs = true;
                }
                Humanoid humanoid = other as Humanoid;
                ItemDrop.ItemData weapon = humanoid == null ? null : humanoid.GetCurrentWeapon();
                candidates.Add(new TargetCandidate
                {
                    Id = other.GetInstanceID(),
                    Distance = distance,
                    CanSee = kernel.Body.Ai.CanSeeTarget(other),
                    TargetsMe = ReferenceEquals(theirTarget, me),
                    TargetsLeader = leader != null && ReferenceEquals(theirTarget, leader),
                    TargetsPartner = targetsPartner,
                    Ranged = weapon != null && weapon.m_shared != null && weapon.m_shared.m_attack != null &&
                        weapon.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile,
                    Health01 = other.GetHealthPercentage(),
                    PartnerDistance = partnerDistance,
                    Boss = other.IsBoss(),
                    Alerted = theirAi != null && theirAi.IsAlerted(),
                    Wildlife = other.GetFaction() == Character.Faction.AnimalsVeg,
                    HurtUs = hurtUs
                });
                actors.Add(other);
            }
        }
    }

    /// <summary>Harmony postfix on BaseAI.FindEnemy, companions only. Replaces the return value.</summary>
    public static class FindEnemyHook
    {
        public static void Postfix(BaseAI __instance, ref Character __result)
        {
            NpcKernel kernel = __instance == null ? null : __instance.GetComponent<NpcKernel>();
            if (kernel == null || kernel.Body == null || kernel.Targeting == null) return;
            try { __result = kernel.Targeting.Choose(); }
            catch (System.Exception e)
            {
                // Never break native combat: on any fault keep the game's own answer.
                kernel.ReportFault("target selection", e);
            }
        }
    }
}

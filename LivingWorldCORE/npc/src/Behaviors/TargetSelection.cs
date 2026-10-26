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
        public int Picks { get; private set; }          // every new target, including the first after a kill
        public int LastChoiceId { get; private set; }
        public int Decisions { get; private set; }
        public string LastChoiceName { get; private set; }

        public TargetSelector(NpcKernel kernel) { this.kernel = kernel; Reason = ""; LastChoiceName = ""; }

        public Character Choose()
        {
            Decisions++;
            if (kernel.Frame.FallingBack) { Reason = "falling back"; return null; }
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
            int pick = TargetPolicy.Choose(candidates, currentId, encounter, s.LeaderCrouching, s.NearestPartyDistance, out reason);
            Reason = reason;
            if (pick < 0) return null;
            Character chosen = actors[pick];
            if (!ReferenceEquals(chosen, current))
            {
                if (currentId >= 0) Switches++;   // live target -> different target; a fresh pick is not a switch
                Picks++;
                try { NpcVoice.OnNewTarget(kernel, chosen); } catch (System.Exception e) { CorePlugin.Warn("voice target: " + e.Message); }
                LastChoiceName = chosen.GetHoverName();
                LastChoiceId = chosen.GetInstanceID();
            }
            return chosen;
        }

        /// <summary>Split from my partner (Sparta distance) and an engaged enemy is on them.</summary>
        public bool PartnerUnderAttack()
        {
            NpcSenses s = kernel.Frame.Senses;
            if (s.NearestPartyDistance < TargetPolicy.SplitDistance) return false;
            NpcPhase phase = kernel.Arbiter.Phase;
            bool encounter = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            CollectPartners(s.Leader);
            Build(kernel.Body.Human, s.Leader);
            for (int i = 0; i < candidates.Count; i++)
                if (TargetPolicy.OnPartner(candidates[i]) && TargetPolicy.Engaged(candidates[i], encounter, s.LeaderCrouching)) return true;
            return false;
        }

        /// <summary>Nearest engaged enemy (same rule as target choice), or null.</summary>
        public Character NearestEngaged(out float distance)
        {
            distance = float.PositiveInfinity;
            NpcPhase phase = kernel.Arbiter.Phase;
            bool encounter = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            CollectPartners(kernel.Frame.Senses.Leader);
            Build(kernel.Body.Human, kernel.Frame.Senses.Leader);
            Character best = null;
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Distance < distance && TargetPolicy.Engaged(candidates[i], encounter, kernel.Frame.Senses.LeaderCrouching))
                {
                    distance = candidates[i].Distance;
                    best = actors[i];
                }
            return best;
        }

        public bool AnyEngagedWithin(float radius)
        {
            NpcPhase phase = kernel.Arbiter.Phase;
            bool encounter = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            CollectPartners(kernel.Frame.Senses.Leader);
            Build(kernel.Body.Human, kernel.Frame.Senses.Leader);
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Distance <= radius && TargetPolicy.Engaged(candidates[i], encounter, kernel.Frame.Senses.LeaderCrouching))
                    return true;
            return false;
        }

        /// <summary>Is this target still one the rule would fight? (Drop it otherwise; do not chase it.)</summary>
        public bool StillEngaged(Character target)
        {
            if (target == null || target.IsDead()) return false;
            NpcPhase phase = kernel.Arbiter.Phase;
            bool encounter = phase == NpcPhase.Combat || phase == NpcPhase.CombatClear || phase == NpcPhase.CombatRegroup;
            CollectPartners(kernel.Frame.Senses.Leader);
            Build(kernel.Body.Human, kernel.Frame.Senses.Leader);
            int id = target.GetInstanceID();
            for (int i = 0; i < candidates.Count; i++)
                if (candidates[i].Id == id) return TargetPolicy.Engaged(candidates[i], encounter, kernel.Frame.Senses.LeaderCrouching);
            return false;   // out of range or no longer sensed
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
                if (distance > TargetPolicy.PartnerReach) continue;
                BaseAI theirAi = other.GetBaseAI();
                if (theirAi != null && theirAi.IsSleeping()) continue;
                MonsterAI monster = theirAi as MonsterAI;
                Character theirTarget = monster == null ? null : monster.GetTargetCreature();
                bool targetsPartner = false, hurtUs = false;
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
                // Beyond my own range only the enemy on my partner counts (the partner "calls" it, Sparta 1.0.23);
                // everything else still has to be within my range and sensed by me.
                bool onPartner = targetsPartner && partnerDistance >= 0f && partnerDistance <= TargetPolicy.PartnerDefenseRadius;
                if (distance > TargetPolicy.MaxRange && !onPartner) continue;
                if (!onPartner && !kernel.Body.Ai.CanSenseTarget(other)) continue;
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
                    HurtUs = hurtUs,
                    HurtMe = ReferenceEquals(other, hurtMe)
                });
                actors.Add(other);
            }
        }
    }

    /// <summary>Harmony prefix on Humanoid.EquipBestWeapon, companions only.</summary>
    public static class WeaponChoiceHook
    {
        public static bool Prefix(Humanoid __instance, Character targetCreature)
        {
            NpcKernel kernel = __instance == null ? null : __instance.GetComponent<NpcKernel>();
            if (kernel == null || kernel.Body == null) return true;
            try { return !kernel.Body.EquipForNativeSelection(targetCreature); }
            catch (System.Exception e) { kernel.ReportFault("weapon choice", e); return true; }
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

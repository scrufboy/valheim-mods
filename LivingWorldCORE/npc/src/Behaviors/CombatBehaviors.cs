using System.Collections.Generic;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Full native combat. Holds every domain so nothing else can touch the actor,
    /// and does almost nothing itself: native MonsterAI/Humanoid own target, facing,
    /// pursuit, weapon choice, attacks, blocking and damage.
    /// </summary>
    public sealed class NativeCombatBehavior : NpcBehavior
    {
        Character prepared;
        BlockPolicy block;
        Character blockThreat;
        readonly List<ThreatObservation> threats = new List<ThreatObservation>(8);
        readonly List<Character> threatActors = new List<Character>(8);

        public BlockPolicy Block { get { return block; } }
        public string BlockThreat { get { return blockThreat == null ? "" : blockThreat.GetHoverName(); } }

        public override string Name { get { return "combat"; } }
        public override NpcDomain Claims { get { return NpcDomain.All; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Phase == NpcPhase.Combat || f.Phase == NpcPhase.CombatClear;
        }

        public override void Enter(NpcFrame f)
        {
            prepared = null;
            Body.RestorePace(this);
            Body.TuneRangedWeapons(this);
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            // Native leader tether: does not drive locomotion while MonsterAI has a
            // target, but gives it a deterministic return instead of idle wandering.
            Body.Follow(this, s.Leader == null ? null : s.Leader.gameObject);
            if (s.Target != null && s.Target != prepared)
            {
                // Valheim's own distance/attack-data selector, once per new target.
                Body.EquipBestWeaponFor(this, s.Target);
                prepared = s.Target;
            }
            // Native choice may swap bow -> sword at any time; keep the shield paired.
            Body.PairShield(this);
            if (CorePlugin.ShieldBlocking) StepBlock(f); else if (block != null && block.Raised) Body.SetBlocking(this, false);
            Body.PublishObserved(this);
            Status = f.Phase == NpcPhase.CombatClear
                ? "verifying clear " + f.Senses.SecondsSinceDamaged.ToString("0.0") + "s since hit"
                : s.Target != null ? "native target " + s.Target.GetHoverName() : "native combat (" + f.Phase + ")";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            prepared = null;
            Body.SetBlocking(this, false);
            blockThreat = null;
            Status = "released: " + reason;
        }

        // Shield discipline inside native combat: same owner, same tick, no extra loop.
        void StepBlock(NpcFrame f)
        {
            if (block == null) block = new BlockPolicy(BlockPolicy.ReactionFor(Body.Root.name.GetStableHashCode() ^ Body.Root.GetInstanceID()));
            Humanoid me = Body.Human;
            bool busy = me.InAttack() || me.IsDrawingBow() || me.IsStaggering();
            ObserveThreats();
            bool raise = block.Step(f.Now, Body.ShieldInHand, busy, threats);
            if (raise)
            {
                int best = -1;
                float d = float.PositiveInfinity;
                for (int i = 0; i < threats.Count; i++)
                    if (threats[i].Attacking && threats[i].TargetsMe && threats[i].Distance < d) { d = threats[i].Distance; best = i; }
                blockThreat = best >= 0 ? threatActors[best] : blockThreat;
            }
            else blockThreat = null;
            Body.SetBlocking(this, raise);
        }

        // Turn to face the projectile shooter while the shield is up (native block needs a frontal hit).
        public override void LateFrame(NpcFrame f)
        {
            if (block != null && block.Raised && block.AgainstRanged && blockThreat != null)
                Body.LookAt(this, blockThreat.GetCenterPoint());
        }

        void ObserveThreats()
        {
            threats.Clear();
            threatActors.Clear();
            Humanoid me = Body.Human;
            Vector3 position = me.transform.position;
            Vector3 forward = me.transform.forward;
            foreach (Character other in Character.GetAllCharacters())
            {
                if (threats.Count == 8) break;
                if (other == null || other == me || other.IsDead()) continue;
                Vector3 offset = other.transform.position - position;
                float distance = offset.magnitude;
                if (distance > BlockPolicy.RangedRadius || !BaseAI.IsEnemy(me, other)) continue;
                BaseAI ai = other.GetBaseAI();
                MonsterAI monster = ai as MonsterAI;
                Humanoid humanoid = other as Humanoid;
                ItemDrop.ItemData weapon = humanoid == null ? null : humanoid.GetCurrentWeapon();
                bool ranged = weapon != null && weapon.m_shared != null && weapon.m_shared.m_attack != null &&
                    weapon.m_shared.m_attack.m_attackType == Attack.AttackType.Projectile;
                offset.y = 0f;
                threats.Add(new ThreatObservation
                {
                    Distance = distance,
                    Attacking = other.InAttack() || (humanoid != null && humanoid.IsDrawingBow()),
                    TargetsMe = monster != null && monster.GetTargetCreature() == me,
                    Ranged = ranged,
                    InFront = offset.sqrMagnitude < .01f || Vector3.Angle(forward, offset) <= 70f
                });
                threatActors.Add(other);
            }
        }
    }

    /// <summary>
    /// The companion got too far from the party mid-fight. Drop out-of-area pursuit
    /// once, run back, and keep suppressing distant reacquisition until inside the
    /// inner area. Anything close enough to be hurting it switches the arbiter back
    /// to Combat for local defense — that check lives in the arbiter, not here.
    /// </summary>
    public sealed class CombatRegroupBehavior : NpcBehavior
    {
        public int Suppressions { get; private set; }

        public override string Name { get { return "combat-regroup"; } }
        public override NpcDomain Claims { get { return NpcDomain.All; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Phase == NpcPhase.CombatRegroup && f.Senses.HasLeader;
        }

        public override void Enter(NpcFrame f)
        {
            Body.ReleaseEncounter(this);
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            if (s.Target != null || s.HasStaticTarget)
            {
                Body.ReleaseEncounter(this);
                Suppressions++;
            }
            Body.Follow(this, s.Leader.gameObject);
            Body.SetPace(this, true, true, s.LeaderSpeed, false);
            Body.SetRun(this, true);
            Body.PublishObserved(this);
            Status = "returning; leader " + s.LeaderDistance.ToString("0.0") + "m, party " +
                s.NearestPartyDistance.ToString("0.0") + "m";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            Body.RestorePace(this);
            Status = "released: " + reason;
        }
    }

    /// <summary>Readiness: face the nearest alerted hostile without attacking or pursuing.</summary>
    public sealed class AwarenessLookBehavior : NpcBehavior
    {
        public override string Name { get { return "awareness"; } }
        public override NpcDomain Claims { get { return NpcDomain.Look; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Phase == NpcPhase.Readiness && f.Senses.NearestAlertedHostile != null;
        }

        public override void Tick(NpcFrame f)
        {
            Character hostile = f.Senses.NearestAlertedHostile;
            Status = hostile == null ? "watching" : "watching " + hostile.GetHoverName() + " at " +
                f.Senses.NearestAlertedHostileDistance.ToString("0") + "m";
        }

        public override void LateFrame(NpcFrame f)
        {
            Character hostile = f.Senses.NearestAlertedHostile;
            if (hostile != null) Body.LookAt(this, hostile.GetCenterPoint());
        }
    }
}

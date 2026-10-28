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
        float nextEngagedCheck, nextRepick;
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
            // Same rule that chose the target decides when to let it go: no chasing a fleeing
            // creature out of range, no fighting something that has stopped being a threat.
            if (s.Target != null && f.Now >= nextEngagedCheck)
            {
                nextEngagedCheck = f.Now + 1f;
                NpcKernel k = Body.Root.GetComponent<NpcKernel>();
                if (k != null && k.Targeting != null && !k.Targeting.StillEngaged(s.Target))
                {
                    Body.DropTarget(this);
                    prepared = null;
                }
            }
            // No dead time between targets: after a kill (or anything that empties the slot) the game
            // waits up to 2 s for its next re-pick. With an engaged enemy near, ask for it now.
            // 1.0.7 live: 13% of fighting time targetless with enemies near, 7 of 50 hits taken then.
            if ((s.Target == null || s.Target.IsDead()) && !s.HasStaticTarget && f.Now >= nextRepick)
            {
                NpcKernel k = Body.Root.GetComponent<NpcKernel>();
                if (k != null && k.NearestEngagedDistance <= TargetPolicy.MaxRange)
                {
                    Body.DropTarget(this);
                    nextRepick = f.Now + .5f;
                }
            }
            if (s.Target != null && s.Target != prepared)
            {
                // Valheim's own distance/attack-data selector, once per new target.
                Body.EquipBestWeaponFor(this, s.Target);
                prepared = s.Target;
            }
            // Combos continue while this behavior holds the hands (1.0.17); FallBack taking the hands ends them.
            Body.AllowChains(this, true);
            // 1.0.32.11: one bounded special action - a finite bomb thrown by the native attack.
            if (CorePlugin.CompanionBombs) StepBomb(f);
            // Native choice may swap bow -> sword at any time; keep the shield paired.
            if (!Body.Throwing) Body.PairShield(this);
            if (CorePlugin.ShieldBlocking) StepBlock(f); else if (block != null && block.Raised) Body.SetBlocking(this, false);
            Body.PublishObserved(this);
            Status = f.Phase == NpcPhase.CombatClear
                ? "verifying clear " + f.Senses.SecondsSinceDamaged.ToString("0.0") + "s since hit"
                : s.Target != null ? "native target " + s.Target.GetHoverName() : "native combat (" + f.Phase + ")";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            prepared = null;
            if (Body.Throwing && Body.IsOwner) Body.FinishBomb(this);
            Body.SetBlocking(this, false);
            blockThreat = null;
            Status = "released: " + reason;
        }

        // ---- 1.0.32.11 bomb throw: decide with BombPolicy, execute with the native attack, finish and restore.
        readonly BombPolicy bombs = new BombPolicy();
        float nextBombCheck, nextBombSkipLog;

        void StepBomb(NpcFrame f)
        {
            if (Body.Throwing)
            {
                if (Body.FinishBomb(this)) CorePlugin.Report("BOMB-DONE " + Body.Human.GetHoverName() + " left=" + Body.BombReserve + " t=" + f.Now.ToString("0.00"));
                return;
            }
            if (f.Now < nextBombCheck) return;
            nextBombCheck = f.Now + .25f;
            Character target = f.Senses.Target;
            Humanoid me = Body.Human;
            var sit = new BombSituation
            {
                Reserve = Body.BombReserve,
                TargetDistance = target == null || target.IsDead() ? -1f : Vector3.Distance(me.transform.position, target.transform.position),
                TargetVisible = target != null && Body.Ai != null && Body.Ai.CanSeeTarget(target),
                Busy = me.InAttack() || me.IsDrawingBow() || me.IsStaggering() || me.IsBlocking(),
                NearestAllyToTarget = float.PositiveInfinity,
                TargetMaxHealth = target == null ? 0f : target.GetMaxHealth(),
            };
            sit.SelfToTarget = sit.TargetDistance;
            if (target != null)
            {
                Vector3 to = target.transform.position - me.transform.position; to.y = 0f;
                sit.FacingAngle = to.sqrMagnitude < 1e-4f ? 0f : Vector3.Angle(new Vector3(me.transform.forward.x, 0f, me.transform.forward.z), to);
            }
            if (target != null && sit.Reserve > 0)
            {
                Vector3 at = target.transform.position;
                foreach (Character c in Character.GetAllCharacters())
                {
                    if (c == null || c == me || c.IsDead()) continue;
                    float d = Vector3.Distance(c.transform.position, at);
                    bool ally = c.IsPlayer() || c.GetComponent<NpcKernel>() != null || (!BaseAI.IsEnemy(me, c) && c.IsTamed());
                    if (ally) { if (d < sit.NearestAllyToTarget) sit.NearestAllyToTarget = d; }
                    else if (d <= BombPolicy.BlastRadius && BaseAI.IsEnemy(me, c)) sit.HostilesNearTarget++;
                }
            }
            if (bombs.Decide(f.Now, sit))
            {
                if (Body.ThrowBomb(this, target))
                {
                    bombs.Started(f.Now);
                    CorePlugin.Report("BOMB-THROW " + me.GetHoverName() + " at=" + target.GetHoverName() + " dist=" + sit.TargetDistance.ToString("0.0") +
                        " hostiles=" + sit.HostilesNearTarget + " allyToTarget=" + (float.IsInfinity(sit.NearestAllyToTarget) ? "none" : sit.NearestAllyToTarget.ToString("0.0")) +
                        " kind=" + Body.BombKind + " left=" + Body.BombReserve + " t=" + f.Now.ToString("0.00"));
                }
                else { bombs.Failed(f.Now); CorePlugin.Report("BOMB-REFUSED " + me.GetHoverName() + " native attack did not start t=" + f.Now.ToString("0.00")); }
            }
            else if (CorePlugin.DevBombLog && sit.Reserve > 0 && target != null && f.Now >= nextBombSkipLog && bombs.LastReason != "cooldown" && bombs.LastReason != "busy")
            {
                nextBombSkipLog = f.Now + 3f;
                CorePlugin.Report("BOMB-SKIP " + me.GetHoverName() + " reason=" + bombs.LastReason + " dist=" + sit.TargetDistance.ToString("0.0") +
                    " hostiles=" + sit.HostilesNearTarget + " t=" + f.Now.ToString("0.00"));
            }
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

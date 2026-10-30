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
            if (f.Phase == NpcPhase.Combat || f.Phase == NpcPhase.CombatClear) return true;
            // Brian 2026-10-30: the companion's own sight decides, not the leader's weapon. A visible, valid group may
            // be opened on from Calm or Readiness alike; only the leader's sneak (stealth intent) holds the throw.
            if (!CorePlugin.CompanionBombs || (f.Phase != NpcPhase.Readiness && f.Phase != NpcPhase.Calm) ||
                f.Senses.LeaderCrouching || Body == null || Body.Human == null) return false;
            // Keep ownership until the native throw animation has delivered and
            // FinishBomb has restored the real loadout.
            if (Body.Throwing) return true;
            if (Body.Human.InAttack() || Body.Human.IsDrawingBow() || Body.Human.IsStaggering() || Body.Human.IsBlocking()) return false;
            cadence.Cooldown = CorePlugin.BombCooldown;
            if (!cadence.Ready(f.Now) || (!CorePlugin.BombsEndless && Body.BombReserve <= 0)) return false;
            // Not yet granted: look for an opening at the same 0.2 s cadence StepBomb uses (bounded raycasts).
            if (!Active) { if (f.Now < nextOpeningScan) return false; nextOpeningScan = f.Now + .2f; }
            Humanoid me = Body.Human;
            Vector3 at = me.transform.position;
            int nh = 0, nf = 0;
            P2 party = new P2(at.x, at.z); int partyN = 1;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == me || c.IsDead()) continue;
                if (Vector3.Distance(c.transform.position, at) > 30f) continue;
                bool friend = c.IsPlayer() || c.GetComponent<NpcKernel>() != null || !BaseAI.IsEnemy(me, c);
                if (friend)
                {
                    if (c.IsPlayer() || c.GetComponent<NpcKernel>() != null) { party = party + new P2(c.transform.position.x, c.transform.position.z); partyN++; }
                    if (nf < friendBuf.Length) friendBuf[nf++] = BodyOf(c);
                }
                else if (nh < hostileBuf.Length && Body.Ai != null && Body.Ai.CanSeeTarget(c)) { hostileActors[nh] = c; hostileBuf[nh++] = BodyOf(c); }
            }
            party = party * (1f / partyN);
            Vector3 fw = me.transform.forward;
            ThrowPlan plan = ThrowPlanner.Plan(new P2(at.x, at.z), at.y, new P2(fw.x, fw.z), party, hostileBuf, nh, friendBuf, nf);
            if (!plan.Throw) return false;
            Character target = null; float best = float.PositiveInfinity;
            for (int i = 0; i < nh; i++)
            {
                Vector3 p = hostileActors[i].transform.position;
                float dx = p.x - plan.Aim.X, dz = p.z - plan.Aim.Z, d2 = dx * dx + dz * dz;
                if (d2 < best) { best = d2; target = hostileActors[i]; }
            }
            if (target == null) return false;
            return true;
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

        // ---- 1.0.32.12 bomb throw: Souls Pillager throw, player-like decision (ThrowPlanner), native execution.
        readonly ThrowCadence cadence = new ThrowCadence();
        float nextBombCheck, nextBombSkipLog, nextOpeningScan;
        bool aiming; Vector3 aimPoint; float aimSince; ThrowPlan aimPlan;
        readonly ThrowBody[] hostileBuf = new ThrowBody[24];
        readonly Character[] hostileActors = new Character[24];
        readonly ThrowBody[] friendBuf = new ThrowBody[16];

        static ThrowBody BodyOf(Character c)
        {
            Vector3 p = c.transform.position, v = c.GetVelocity();
            // 1.0.32.21: the creature's own native poison modifier (the thrown bomb's cloud is ooze poison)
            HitData.DamageModifier poison = c.GetDamageModifiers().m_poison;
            bool immune = poison == HitData.DamageModifier.Immune || poison == HitData.DamageModifier.Ignore;
            return new ThrowBody { Position = new P2(p.x, p.z), Velocity = new P2(v.x, v.z), Height = p.y, MaxHealth = c.GetMaxHealth(), Immune = immune };
        }

        void StepBomb(NpcFrame f)
        {
            Humanoid me = Body.Human;
            if (Body.Throwing)
            {
                if (Body.FinishBomb(this)) CorePlugin.Report("BOMB-DONE " + me.GetHoverName() + (CorePlugin.BombsEndless ? "" : " left=" + Body.BombReserve) + " t=" + f.Now.ToString("0.00"));
                return;
            }
            cadence.Cooldown = CorePlugin.BombCooldown;
            if (!cadence.Ready(f.Now)) { aiming = false; return; }
            if (!CorePlugin.BombsEndless && Body.BombReserve <= 0) { aiming = false; return; }
            if (f.Now < nextBombCheck) return;
            nextBombCheck = f.Now + .2f;
            bool busy = me.InAttack() || me.IsDrawingBow() || me.IsStaggering() || me.IsBlocking();
            if (busy) { aiming = false; return; }

            Character target; ThrowPlan plan;
            {
                Vector3 at = me.transform.position;
                int nh = 0, nf = 0;
                P2 party = new P2(at.x, at.z); int partyN = 1;
                foreach (Character c in Character.GetAllCharacters())
                {
                    if (c == null || c == me || c.IsDead()) continue;
                    if (Vector3.Distance(c.transform.position, at) > 30f) continue;
                    bool friend = c.IsPlayer() || c.GetComponent<NpcKernel>() != null || !BaseAI.IsEnemy(me, c);
                    if (friend)
                    {
                        if (c.IsPlayer() || c.GetComponent<NpcKernel>() != null) { party = party + new P2(c.transform.position.x, c.transform.position.z); partyN++; }
                        if (nf < friendBuf.Length) friendBuf[nf++] = BodyOf(c);
                    }
                    else if (nh < hostileBuf.Length && Body.Ai != null && Body.Ai.CanSeeTarget(c)) { hostileActors[nh] = c; hostileBuf[nh++] = BodyOf(c); }
                }
                party = party * (1f / partyN);
                Vector3 fw = me.transform.forward;
                plan = ThrowPlanner.Plan(new P2(at.x, at.z), at.y, new P2(fw.x, fw.z), party, hostileBuf, nh, friendBuf, nf);
                target = null; float best = float.PositiveInfinity;
                if (plan.Throw)
                {
                    for (int i = 0; i < nh; i++)
                    {
                        Vector3 p = hostileActors[i].transform.position;
                        float dx = p.x - plan.Aim.X, dz = p.z - plan.Aim.Z, d2 = dx * dx + dz * dz;
                        if (d2 < best) { best = d2; target = hostileActors[i]; }
                    }
                }
                if (!plan.Throw || target == null)
                {
                    aiming = false;
                    if (CorePlugin.DevBombLog && f.Now >= nextBombSkipLog)
                    {
                        nextBombSkipLog = f.Now + 3f;
                        CorePlugin.Report("BOMB-SKIP " + me.GetHoverName() + " reason=" + plan.Reason + " hostilesSeen=" + nh + " t=" + f.Now.ToString("0.00"));
                    }
                    return;
                }
            }
            aimPlan = plan;
            aimPoint = new Vector3(plan.Aim.X, plan.AimHeight, plan.Aim.Z);
            if (!aiming) { aiming = true; aimSince = f.Now; }
            if (plan.TurnNeeded > ThrowPlanner.MaxTurn)
            {
                if (f.Now - aimSince > 2f) { aiming = false; cadence.Failed(f.Now); }   // could not turn in time
                return;                                                                 // LateFrame turns to the aim
            }
            aiming = false;
            if (Body.ThrowBomb(this, target, aimPoint))
            {
                cadence.Started(f.Now);
                CorePlugin.Report("BOMB-THROW " + me.GetHoverName() + " mode=" + (f.Phase == NpcPhase.Readiness || f.Phase == NpcPhase.Calm ? "opening-area-control phase=" + f.Phase : "in-combat-area-control") + " cluster=" + plan.Members + " poisonable=" + plan.Susceptible + " incoming=" + plan.Incoming +
                    " dist=" + plan.Distance.ToString("0.0") + " lead=" + plan.Lead.ToString("0.0") + " elev=" + (plan.Elevation * Mathf.Rad2Deg).ToString("0") +
                    " friendClear=" + (float.IsInfinity(plan.FriendClearance) ? "none" : plan.FriendClearance.ToString("0.0")) +
                    (CorePlugin.BombsEndless ? " endless" : " left=" + Body.BombReserve) + " t=" + f.Now.ToString("0.00"));
            }
            else { cadence.Failed(f.Now); CorePlugin.Report("BOMB-REFUSED " + me.GetHoverName() + " native attack did not start t=" + f.Now.ToString("0.00")); }
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
            if (aiming && !Body.Throwing) { Body.LookAt(this, aimPoint); return; }   // 1.0.32.12: face the throw
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

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

        NpcKernel kernel;
        float nextWeapon;

        public override void Enter(NpcFrame f)
        {
            prepared = null;
            if (kernel == null) kernel = Body.Root.GetComponent<NpcKernel>();
            // A window left open by an evade is judged when combat resumes; one from an earlier fight is dropped unjudged.
            if (defense.Open && f.Now > defense.ReleaseAt + 1f) { defense.Cancel(); windowAttacker = null; }
            Body.RestorePace(this);
            Body.TuneRangedWeapons(this, kernel == null || !CorePlugin.FormidableClasses ? CombatHands.RangedMinimum : kernel.Doctrine.MeleeUnder);
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
            // A2 (3e5ac3c): the answer is taken at once - the initiative contact, else TargetPolicy's own pick - instead of
            // dropping the slot and waiting for the native re-pick (2026-10-31: still 10% targetless with a hostile near).
            if ((s.Target == null || s.Target.IsDead()) && !s.HasStaticTarget)
            {
                Character pick = null;
                bool fromInitiative = false;
                if (CorePlugin.FormidableInitiative && kernel != null)
                {
                    if (kernel.InitiativeTarget != null) { pick = kernel.InitiativeTarget; fromInitiative = true; }
                    else if (kernel.Targeting != null && kernel.NearestEngagedDistance <= TargetPolicy.MaxRange && f.Now >= nextRepick)
                    {
                        nextRepick = f.Now + .25f;
                        pick = kernel.Targeting.Choose();
                    }
                }
                if (pick != null && Vector3.Distance(pick.transform.position, Body.Root.transform.position) <= TargetPolicy.MaxRange)
                {
                    if (Body.Engage(this, pick)) { kernel.NoteEngage(pick, fromInitiative); prepared = null; }
                }
                else if (!CorePlugin.FormidableInitiative && f.Now >= nextRepick)
                {
                    NpcKernel k = Body.Root.GetComponent<NpcKernel>();
                    if (k != null && k.NearestEngagedDistance <= TargetPolicy.MaxRange)
                    {
                        Body.DropTarget(this);
                        nextRepick = f.Now + .5f;
                    }
                }
            }
            if (s.Target != null && s.Target != prepared)
            {
                // Valheim's own distance/attack-data selector, once per new target.
                Body.EquipBestWeaponFor(this, s.Target);
                prepared = s.Target;
                nextWeapon = f.Now + .25f;
            }
            else if (CorePlugin.FormidableClasses && s.Target != null && f.Now >= nextWeapon && !Body.Throwing && !Body.Human.InAttack() && !defense.Open)
            {
                // A2.2: the class weapon band is re-checked while the fight moves (not only at the native ~1 s ask).
                nextWeapon = f.Now + .25f;
                Body.RefreshWeapon(this, s.Target);
            }
            // Combos continue while this behavior holds the hands (1.0.17); FallBack taking the hands ends them.
            Body.AllowChains(this, true);
            // 1.0.32.11: one bounded special action - a finite bomb thrown by the native attack.
            if (CorePlugin.CompanionBombs) StepBomb(f);
            // Native choice may swap bow -> sword at any time; keep the shield paired.
            if (!Body.Throwing) Body.PairShield(this);
            if (CorePlugin.FormidableDefense) StepDefense(f);
            else if (CorePlugin.ShieldBlocking) StepBlock(f); else if (block != null && block.Raised) Body.SetBlocking(this, false);
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
            guardUp = false;
            blockThreat = null;
            // An evade hands the body to the fall-back mover: that window stays open and is judged when combat resumes.
            if (defense.Open && defense.Plan.Choice != DefenseChoice.Evade) { CloseWindow(f, "released: " + reason); defense.Cancel(); }
            Status = "released: " + reason;
        }

        // ---- 1.0.32.12 bomb throw: Souls Pillager throw, player-like decision (ThrowPlanner), native execution.
        readonly ThrowCadence cadence = new ThrowCadence();
        float nextBombCheck, nextBombSkipLog;
        bool aiming; Vector3 aimPoint; float aimSince; ThrowPlan aimPlan;
        readonly ThrowBody[] hostileBuf = new ThrowBody[24];
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
            Character target = f.Senses.Target;
            if (busy || target == null) { aiming = false; return; }

            Vector3 at = me.transform.position;
            int nh = 0, nf = 0;
            P2 party = new P2(at.x, at.z); int partyN = 1;
            foreach (Character c in Character.GetAllCharacters())
            {
                if (c == null || c == me || c.IsDead()) continue;
                float d = Vector3.Distance(c.transform.position, at);
                if (d > 30f) continue;
                bool friend = c.IsPlayer() || c.GetComponent<NpcKernel>() != null || !BaseAI.IsEnemy(me, c);
                if (friend)
                {
                    if (c.IsPlayer() || c.GetComponent<NpcKernel>() != null) { party = party + new P2(c.transform.position.x, c.transform.position.z); partyN++; }
                    if (nf < friendBuf.Length) friendBuf[nf++] = BodyOf(c);
                }
                else if (nh < hostileBuf.Length && Body.Ai != null && Body.Ai.CanSeeTarget(c)) hostileBuf[nh++] = BodyOf(c);
            }
            party = party * (1f / partyN);
            Vector3 fw = me.transform.forward;
            ThrowPlan plan = ThrowPlanner.Plan(new P2(at.x, at.z), at.y, new P2(fw.x, fw.z), party, hostileBuf, nh, friendBuf, nf);
            if (!plan.Throw)
            {
                aiming = false;
                if (CorePlugin.DevBombLog && f.Now >= nextBombSkipLog)
                {
                    nextBombSkipLog = f.Now + 3f;
                    CorePlugin.Report("BOMB-SKIP " + me.GetHoverName() + " reason=" + plan.Reason + " hostilesSeen=" + nh + " t=" + f.Now.ToString("0.00"));
                }
                return;
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
                CorePlugin.Report("BOMB-THROW " + me.GetHoverName() + " cluster=" + plan.Members + " poisonable=" + plan.Susceptible + " incoming=" + plan.Incoming +
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
            // A2.3: face the attacker of the open defense window while the shield is up (a block is frontal).
            if (defense.Open && windowAttacker != null && !windowAttacker.IsDead() && defense.ShieldUp(f.Now))
            {
                Body.LookAt(this, windowAttacker.GetCenterPoint());
                return;
            }
            if (block != null && block.Raised && block.AgainstRanged && blockThreat != null)
                Body.LookAt(this, blockThreat.GetCenterPoint());
        }

        // ---- A2.3 strike-window defense (3e5ac3c): one decision per incoming strike, timed to its predicted impact ----
        public static readonly StrikeTimingBook Timings = new StrikeTimingBook();
        readonly DefenseWindow defense = new DefenseWindow();
        Character windowAttacker;
        float windowDamageBefore, reaction = -1f;
        public DefenseWindow Defense { get { return defense; } }
        public int Opportunities { get; private set; }
        public string LastDefense { get; private set; }
        public float LastImpactIn { get; private set; }

        /// <summary>One defense record per window, consumed by telemetry (bounded).</summary>
        public struct DefenseRecord
        {
            public float Time, ImpactIn, ActLead, Damage, Stamina, Health;
            public string Attacker, Attack, Choice, Why, Outcome;
            public bool Learned, AttackerStaggered;
        }
        public readonly DefenseRecord[] DefenseRing = new DefenseRecord[32];
        public long DefenseSequence { get; private set; }
        DefenseRecord pending;
        float damageAtOpen;

        void StepDefense(NpcFrame f)
        {
            Humanoid me = Body.Human;
            if (reaction < 0f) reaction = BlockPolicy.ReactionFor(Body.Root.name.GetStableHashCode() ^ Body.Root.GetInstanceID());
            ClassDoctrine doctrine = kernel == null ? ClassDoctrines.For(TrainedClass.Warden) : kernel.Doctrine;
            ItemDrop.ItemData weapon = me.GetCurrentWeapon();
            bool meleeInHand = weapon != null && weapon.m_shared != null && weapon.m_shared.m_attack != null &&
                weapon.m_shared.m_attack.m_attackType != Attack.AttackType.Projectile && weapon.m_shared.m_skillType != Skills.SkillType.Bows &&
                weapon.m_dropPrefab != null;
            var cap = new DefenseCapability
            {
                ShieldInHand = Body.ShieldInHand,
                BusyOwnAttack = me.InAttack() || me.IsDrawingBow() || me.IsStaggering() || Body.Throwing,
                OwnReach = meleeInHand ? weapon.m_shared.m_aiAttackRange : 0f,
                OwnWindup = DefenseArbiter.WindupPrior,
                Health01 = me.GetHealthPercentage(),
                Reaction = reaction
            };
            Vector3 at = me.transform.position, forward = me.transform.forward;
            // One window at a time: while one is open, no new strike is planned (and nothing is allocated or ray-cast for it).
            if (!defense.Open)
            foreach (Character other in Character.GetAllCharacters())
            {
                if (other == null || other == me || other.IsDead() || !other.InAttack()) continue;
                Humanoid h = other as Humanoid;
                if (h == null) continue;
                Vector3 offset = other.transform.position - at;
                float distance = offset.magnitude;
                if (distance > 40f || !BaseAI.IsEnemy(me, other)) continue;
                float elapsed; bool secondary;
                Attack attack = NpcBody.AttackOf(h, out elapsed, out secondary);
                if (attack == null) continue;
                MonsterAI ai = other.GetBaseAI() as MonsterAI;
                bool targetsMe = ai != null && ReferenceEquals(ai.GetTargetCreature(), me);
                Vector3 back = -offset; back.y = 0f;
                Vector3 theirForward = other.transform.forward; theirForward.y = 0f;
                bool facingMe = back.sqrMagnitude < .01f || Vector3.Angle(theirForward, back) <= 45f;
                bool ranged = attack.m_attackType == Attack.AttackType.Projectile;
                var obs = new StrikeObservation
                {
                    AttackerId = other.GetInstanceID(),
                    Distance = Mathf.Max(0f, distance - other.GetRadius() - me.GetRadius()),
                    Reach = attack.m_attackRange,
                    AimedAtMe = targetsMe || (facingMe && !ranged),
                    Ranged = ranged, Elapsed = elapsed,
                    ProjectileSpeed = attack.m_projectileVel, Heavy = secondary
                };
                if (!DefenseArbiter.Threatens(obs)) continue;   // filtered before any string or ray work
                string key = global::Utils.GetPrefabName(other.gameObject) + ":" + attack.m_attackAnimation + (secondary ? ":2" : "");
                bool learned;
                obs.Windup = Timings.Windup(key, secondary, out learned);
                obs.WindupLearned = learned;
                Vector3 side = Vector3.zero;
                cap.LaneFree = EvadeLane(at, other.transform.position, out side);
                DefensePlan plan = DefenseArbiter.Plan(obs, cap, doctrine);
                if (!defense.Offer(f.Now, f.Now - elapsed, plan)) continue;
                Opportunities++;
                windowAttacker = other;
                evadeSide = side;
                damageAtOpen = kernel == null ? 0f : (float)kernel.DamageTaken;
                pending = new DefenseRecord
                {
                    Time = f.Now, ImpactIn = plan.ImpactIn, ActLead = plan.ImpactIn - plan.ActIn, Attacker = other.GetHoverName(), Attack = key,
                    Choice = plan.Choice.ToString(), Why = plan.Why, Learned = learned, Stamina = Body.Stamina, Health = me.GetHealth()
                };
                LastDefense = plan.Choice + " (" + plan.Why + ")";
                LastImpactIn = plan.ImpactIn;
            }

            if (defense.Open && defense.Due(f.Now))
            {
                switch (defense.Plan.Choice)
                {
                    case DefenseChoice.Interrupt:
                        if (!Facing(windowAttacker)) pending.Why += "; not facing, no swing";
                        else if (!Body.StrikeNow(this, windowAttacker, false)) pending.Why += "; swing refused";
                        break;
                    case DefenseChoice.Evade:
                        if (kernel != null) kernel.RequestEvade(evadeSide, .7f);
                        break;
                }
            }
            // An unlearned (prior-timed) block/parry stays up while that attacker is still swinging, up to 1.5 s:
            // slower attacks than the 0.55 s prior must not land just after the guard drops.
            if (defense.Open && !pending.Learned && windowAttacker != null && !windowAttacker.IsDead() && windowAttacker.InAttack() &&
                (defense.Plan.Choice == DefenseChoice.Block || defense.Plan.Choice == DefenseChoice.Parry) && f.Now < defense.OpenedAt + 1.5f)
                defense.ExtendRelease(f.Now + .15f);
            bool up = defense.ShieldUp(f.Now) && Body.ShieldInHand;
            if (up != guardUp) { Body.SetBlocking(this, up); guardUp = up; }
            if (defense.Open && defense.Expire(f.Now)) CloseWindow(f, null);
        }

        /// <summary>A swing only lands in front: interrupt/counter need the attacker within 45 degrees of my facing.</summary>
        bool Facing(Character c)
        {
            if (c == null) return false;
            Vector3 to = c.transform.position - Body.Human.transform.position; to.y = 0f;
            Vector3 f = Body.Human.transform.forward; f.y = 0f;
            return to.sqrMagnitude < .01f || Vector3.Angle(f, to) <= 45f;
        }

        Vector3 evadeSide;
        bool guardUp;
        readonly bool[] evadeBlocked = new bool[LanePlanner.Directions];

        /// <summary>A side-step lane (left or right of the attacker line) that is standable and clear for 3 m.</summary>
        bool EvadeLane(Vector3 me, Vector3 attacker, out Vector3 side)
        {
            side = Vector3.zero;
            if (!CorePlugin.FormidableLanes) return false;
            P2 m = new P2(me.x, me.z), a = new P2(attacker.x, attacker.z);
            for (int i = 0; i < evadeBlocked.Length; i++) evadeBlocked[i] = false;
            P2 away = (m - a).Normalized;
            int l = LanePlanner.Nearest(away.Left), r = LanePlanner.Nearest(away.Left * -1f);
            evadeBlocked[l] = !CombatLanes.Clear(me, LanePlanner.Direction(l), 3f);
            evadeBlocked[r] = !CombatLanes.Clear(me, LanePlanner.Direction(r), 3f);
            int pickLane = LanePlanner.SideStep(m, a, evadeBlocked, false, m);
            if (pickLane < 0) return false;
            P2 d = LanePlanner.Direction(pickLane);
            side = new Vector3(d.X, 0f, d.Z);
            return true;
        }

        void CloseWindow(NpcFrame f, string forced)
        {
            float taken = kernel == null ? 0f : (float)kernel.DamageTaken - damageAtOpen;
            bool staggered = windowAttacker != null && !windowAttacker.IsDead() && windowAttacker.IsStaggering();
            DefenseChoice c = defense.Plan.Choice;
            string outcome = forced ?? (taken > 0f ? (c == DefenseChoice.Block || c == DefenseChoice.Parry ? "hit through guard" : "hit")
                : staggered ? "attacker staggered" : "no damage");
            pending.Damage = taken;
            pending.Outcome = outcome;
            pending.AttackerStaggered = staggered;
            DefenseRing[DefenseSequence % DefenseRing.Length] = pending;
            DefenseSequence++;
            // Counter: one swing right after a defended strike, if the attacker is in reach (not a loop: one per window).
            bool defended = forced == null && taken <= 0f && (c == DefenseChoice.Block || c == DefenseChoice.Parry || c == DefenseChoice.Evade);
            if (windowAttacker != null && !windowAttacker.IsDead())
            {
                Humanoid me = Body.Human;
                ItemDrop.ItemData weapon = me.GetCurrentWeapon();
                float reach = weapon != null && weapon.m_shared != null && weapon.m_dropPrefab != null &&
                    weapon.m_shared.m_skillType != Skills.SkillType.Bows ? weapon.m_shared.m_aiAttackRange : 0f;
                float d = Mathf.Max(0f, Vector3.Distance(windowAttacker.transform.position, me.transform.position) - windowAttacker.GetRadius() - me.GetRadius());
                if (DefenseArbiter.Counter(defended, staggered, d, reach, me.InAttack() || me.IsDrawingBow()) && Facing(windowAttacker) &&
                    Body.StrikeNow(this, windowAttacker, true))
                    DefenseRing[(DefenseSequence - 1) % DefenseRing.Length].Outcome = outcome + "; countered";
            }
            windowAttacker = null;
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
            return f.Phase == NpcPhase.Readiness && (f.Senses.NearestAlertedHostile != null || f.FocusContact != null);
        }

        // A2.1: the contact this member is answering (shared awareness) takes the look first; else the nearest alerted hostile.
        static Character Watched(NpcFrame f)
        {
            Character c = f.FocusContact;
            return c != null && !c.IsDead() ? c : f.Senses.NearestAlertedHostile;
        }

        public override void Tick(NpcFrame f)
        {
            Character hostile = Watched(f);
            Status = hostile == null ? "watching" : "watching " + hostile.GetHoverName() + " at " +
                Vector3.Distance(Body.Root.transform.position, hostile.transform.position).ToString("0") + "m";
        }

        public override void LateFrame(NpcFrame f)
        {
            Character hostile = Watched(f);
            if (hostile != null) Body.LookAt(this, hostile.GetCenterPoint());
        }
    }
}

using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Human-specific decisions composed around native MonsterAI movement and
    // native Humanoid attacks. No creature prefab, Player lifecycle or broad
    // Humanoid patch participates. All prefixes return normally for other actors.
    public sealed class HumanCompanion : MonoBehaviour
    {
        static readonly BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly FieldInfo TargetField = RequiredField(typeof(MonsterAI), "m_targetCreature");
        static readonly FieldInfo StaticTargetField = RequiredField(typeof(MonsterAI), "m_targetStatic");
        static readonly FieldInfo LastPositionField = RequiredField(typeof(MonsterAI), "m_lastKnownTargetPos");
        static readonly FieldInfo BeenAtPositionField = RequiredField(typeof(MonsterAI), "m_beenAtLastPos");
        static readonly FieldInfo AttackTimerField = RequiredField(typeof(MonsterAI), "m_timeSinceAttacking");
        static readonly FieldInfo CurrentAttackField = RequiredField(typeof(Humanoid), "m_currentAttack");
        static readonly FieldInfo AttackCharacterField = RequiredField(typeof(Attack), "m_character");
        static readonly MethodInfo PatrolMethod = RequiredMethod(typeof(BaseAI), "SetPatrolPoint", typeof(Vector3));
        static readonly MethodInfo AlertMethod = RequiredMethod(typeof(BaseAI), "SetAlerted", typeof(bool));
        static readonly MethodInfo TargetInfoMethod = RequiredMethod(typeof(BaseAI), "SetTargetInfo", typeof(ZDOID));
        static bool patchesReady;

        ZNetView view;
        Humanoid body;
        MonsterAI ai;
        HumanCompositionIdentity composition;
        HumanAppearance appearance;
        CompanionOrderRecord order;
        string observedRecord;
        Player observedLeader;
        Player temporaryLeader;
        bool temporaryFollow;
        Character authorizedTarget;
        Character awarenessTarget;
        bool awarenessAlarmed;
        Character retiredPursuitTarget;
        Vector3 combatOrigin;
        float lastNearbyThreat;
        float lastSensed;
        float lastAttributedDamage = Single.NegativeInfinity;
        float nextThreatScan;
        float encounterEvidenceUntil;
        float encounterClearSince;
        bool encounterOwned;
        bool encounterClearObserved;
        bool recoveryDefense;
        int encounterClearScans;
        CompanionMotion activeMotion = CompanionMotion.Invalid;
        Vector3 waitAnchor;
        bool haveWaitAnchor;
        bool hadAuthority;
        bool localFault;
        bool patrolApplied;
        string status = "Companion awaiting explicit initialization";

        public string Status { get { return status; } }
        public bool HasAuthorizedCombat { get { return authorizedTarget != null; } }
        public bool CombatReadinessActive
        { get { return authorizedTarget != null || encounterOwned; } }
        // Single ownership boundary consumed by every non-combat subsystem.
        // Native attack/draw/block details are consequences of combat, never
        // separate votes that can independently enter or exit this state.
        public bool CombatOwnsActor { get { return CombatReadinessActive; } }
        public bool CompanionBehaviorAllowed { get { return !CombatOwnsActor; } }
        public bool AreaClearVerified
        {
            // Awareness and curiosity are peaceful states. Only an owned combat
            // encounter withholds the verified-clear signal from utility hands,
            // formation and ambient companion behavior.
            get { return authorizedTarget == null && !encounterOwned && encounterClearObserved; }
        }
        public Character TargetCreature { get { return authorizedTarget; } }
        internal Character AwarenessTarget { get { return awarenessTarget; } }
        internal bool AwarenessAlarmed { get { return awarenessTarget != null && awarenessAlarmed; } }
        internal bool AwarenessActive { get { return awarenessTarget != null; } }
        bool RangedParty { get { var equipment = GetComponent<HumanPartyEquipment>(); return equipment != null && equipment.Ready; } }
        float DetectionLeash { get { return CompanionPolicy.AwarenessRange; } }

        internal Player PresenceLeader
        {
            get
            {
                var cleric = GetComponent<HumanCleric>();
                if (cleric != null && cleric.Casting) return null;
                // Read-only view of the existing authority/order decision. This
                // does not issue orders, reacquire targets or change combat.
                return Ready() && activeMotion == CompanionMotion.Follow && !CombatReadinessActive &&
                    !ai.IsAlerted() && observedLeader != null && !observedLeader.IsDead() &&
                    ai.GetFollowTarget() == observedLeader.gameObject ? observedLeader : null;
            }
        }

        // Party identity/readiness must survive combat state transitions. This
        // deliberately does not require peaceful presence or native alert state.
        internal Player PartyLeader
        {
            get
            {
                return Ready() && activeMotion == CompanionMotion.Follow && observedLeader != null &&
                    !observedLeader.IsDead() ? observedLeader : null;
            }
        }

        internal bool MayTravelThroughDoors
        {
            get
            {
                bool orderedTravel = authorizedTarget == null && !ai.IsAlerted() &&
                    (activeMotion == CompanionMotion.Follow || activeMotion == CompanionMotion.Hold);
                bool combatTravel = authorizedTarget != null && !authorizedTarget.IsDead();
                return Ready() && (orderedTravel || combatTravel) &&
                    Player.m_localPlayer != null && !Player.m_localPlayer.IsDead() &&
                    Player.m_localPlayer.GetPlayerID() == order.OwnerId;
            }
        }
        internal bool IsDoorTravelGoal(Vector3 point)
        {
            if (!MayTravelThroughDoors) return false;
            // Combat is allowed to follow its already-authorized target through
            // entrances; this does not broaden target selection. Peaceful travel
            // remains tied to the saved companion order anchor.
            if (authorizedTarget != null && !authorizedTarget.IsDead())
                return Vector3.Distance(point, authorizedTarget.transform.position) < 2f;
            return Vector3.Distance(point, OrderAnchor()) < .75f;
        }

        // Must be installed before any candidate prefab can become active. Exact
        // member existence/signatures are checked here, before Harmony mutation.
        public static void InstallPatches(Harmony harmony)
        {
            if (harmony == null) throw new ArgumentNullException("harmony");
            if (patchesReady) return;
            MethodInfo update = RequiredMethod(typeof(MonsterAI), "UpdateAI", typeof(float));
            MethodInfo target = RequiredMethod(typeof(MonsterAI), "UpdateTarget", typeof(Humanoid), typeof(float),
                typeof(bool).MakeByRefType(), typeof(bool).MakeByRefType());
            MethodInfo damaged = RequiredMethod(typeof(MonsterAI), "OnDamaged", typeof(float), typeof(Character));
            MethodInfo setTarget = RequiredMethod(typeof(MonsterAI), "SetTarget", typeof(Character));
            MethodInfo attack = RequiredMethod(typeof(MonsterAI), "DoAttack", typeof(Character), typeof(bool));
            MethodInfo trigger = RequiredMethod(typeof(Attack), "OnAttackTrigger");
            Patch(harmony, update, "BeforeUpdate");
            harmony.Patch(target,
                postfix: new HarmonyMethod(typeof(HumanCompanion).GetMethod("AfterTarget", BindingFlags.Static | BindingFlags.NonPublic)));
            Patch(harmony, damaged, "BeforeDamaged"); Patch(harmony, setTarget, "BeforeSetTarget");
            Patch(harmony, attack, "BeforeAttack"); Patch(harmony, trigger, "BeforeAttackTrigger");
            patchesReady = true;
        }

        static void Patch(Harmony harmony, MethodInfo original, string prefix)
        { harmony.Patch(original, prefix: new HarmonyMethod(typeof(HumanCompanion).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic))); }

        static FieldInfo RequiredField(Type type, string name)
        {
            var field = type.GetField(name, Hidden);
            if (field == null) throw new MissingFieldException(type.FullName, name);
            return field;
        }

        static MethodInfo RequiredMethod(Type type, string name, params Type[] arguments)
        {
            var method = type.GetMethod(name, Hidden | BindingFlags.Public, null, arguments, null);
            if (method == null) throw new MissingMethodException(type.FullName, name);
            return method;
        }

        void Cache()
        {
            if (view == null) view = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (composition == null) composition = GetComponent<HumanCompositionIdentity>();
            if (appearance == null) appearance = GetComponent<HumanAppearance>();
        }

        bool Owner()
        { Cache(); return view != null && view.IsValid() && view.IsOwner(); }

        bool Ready()
        {
            return patchesReady && !localFault && isActiveAndEnabled && Owner() && body != null && ai != null && !body.IsDead() &&
                !ai.HuntPlayer() && !ai.DespawnInDay() && !ai.IsEventCreature() && !ai.IsSleeping() &&
                composition != null && composition.IsReady && appearance != null && appearance.Initialized && ReadOrder();
        }

        bool ReadOrder()
        {
            if (view == null || !view.IsValid()) return false;
            string record = view.GetZDO().GetString(CompanionPolicy.RecordKey, "");
            if (record != observedRecord)
            {
                observedRecord = record; patrolApplied = false;
                CompanionPolicy.TryRead(record, out order);
            }
            return CompanionPolicy.Valid(order);
        }

        public bool InitializeOrders(Player owner)
        {
            Cache();
            if (owner == null || owner.GetPlayerID() == 0 || !Owner() || composition == null || !composition.IsCompositionReady)
            { status = "Order initialization refused: owner/composition not ready"; return false; }
            string existing = view.GetZDO().GetString(CompanionPolicy.RecordKey, "");
            if (!String.IsNullOrEmpty(existing))
            {
                CompanionOrderRecord current;
                bool same = CompanionPolicy.TryRead(existing, out current) && current.OwnerId == owner.GetPlayerID();
                status = same ? "Existing companion orders preserved" : "Unknown/conflicting orders preserved; passive";
                return same;
            }
            Vector3 point = transform.position;
            var initial = new CompanionOrderRecord { OwnerId = owner.GetPlayerID(), LeaderId = 0,
                Order = CompanionOrder.Hold, X = point.x, Y = point.y, Z = point.z };
            SaveOrder(initial); status = "Persisted initial hold; native initialization still required"; return true;
        }

        void SaveOrder(CompanionOrderRecord next)
        {
            if (!Owner()) throw new InvalidOperationException("Companion order write requires ZDO ownership.");
            string encoded = CompanionPolicy.Write(next);
            view.GetZDO().Set(CompanionPolicy.RecordKey, encoded);
            observedRecord = encoded; order = next; patrolApplied = false;
        }

        bool MayCommand(Player player)
        {
            return Ready() && player != null && player == Player.m_localPlayer && !player.IsDead() &&
                CompanionPolicy.MayIssue(order, player.GetPlayerID(), true, true, true);
        }

        public bool TryIssueFollow(Player leader)
        {
            if (!MayCommand(leader))
            { status = "Follow refused: ready owned companion and bound living player required"; return false; }
            temporaryFollow = false; temporaryLeader = null;
            Vector3 point = transform.position;
            SaveOrder(new CompanionOrderRecord { OwnerId = order.OwnerId, LeaderId = leader.GetPlayerID(),
                Order = CompanionOrder.Follow, X = point.x, Y = point.y, Z = point.z });
            StopCombat(); ReconcileLeader(); status = "Follow persisted by stable player ID; reconnect resumes within 64m"; return true;
        }

        public bool TryIssueHold()
        {
            if (!MayCommand(Player.m_localPlayer)) { status = "Hold refused: ready companion and bound living local owner required"; return false; }
            temporaryFollow = false; temporaryLeader = null;
            Vector3 point = transform.position;
            SaveOrder(new CompanionOrderRecord { OwnerId = order.OwnerId, LeaderId = 0,
                Order = CompanionOrder.Hold, X = point.x, Y = point.y, Z = point.z });
            StopCombat(); ReconcileLeader(); status = "Hold persisted at current position"; return true;
        }

        public bool TryAuthorizeCombatTarget(Character target)
        {
            if (!MayCommand(Player.m_localPlayer)) { status = "Explicit combat refused: bound living local owner required"; return false; }
            return Authorize(target, true, 0f);
        }

        // Measured movement may temporarily borrow native follow without changing
        // the companion's saved order. Reload never resurrects a test override.
        public bool BeginTemporaryFollow(Player leader)
        {
            if (!MayCommand(leader)) return false;
            StopCombat(); temporaryFollow = true; temporaryLeader = leader; ReconcileLeader();
            status = "Temporary measured follow; persistent order unchanged"; return true;
        }

        public void EndTemporaryOrder()
        {
            temporaryFollow = false; temporaryLeader = null; patrolApplied = false;
            if (Ready()) ReconcileLeader();
        }

        bool Authorize(Character target, bool explicitOrder, float damage)
        {
            if (!Ready() || target == null) return false;
            ReconcileLeader();
            if (!OrderActive()) return false;
            bool enemy = BaseAI.IsEnemy(body, target);
            bool activeThreat = IsActiveThreat(target);
            if (!CompanionPolicy.MayAuthorize(true, !target.IsDead(), target == body, target.IsPlayer(), target.IsTamed(),
                target.GetComponent<HumanCompanion>() != null, enemy, explicitOrder, damage, activeThreat)) return false;
            Vector3 anchor = OrderAnchor();
            float targetFromNpc = Vector3.Distance(target.transform.position, transform.position);
            float targetFromAnchor = Vector3.Distance(target.transform.position, anchor);
            float npcFromAnchor = Vector3.Distance(transform.position, anchor);
            bool localDefense = LocalSelfDefense(target, damage, targetFromNpc);
            bool livingPartyMember;
            float partyDistance = NearestLivingPartyMember(out livingPartyMember);
            bool recoveryRequired = CompanionPolicy.MustEnterCombatRecovery(npcFromAnchor,
                livingPartyMember, partyDistance);
            if (targetFromNpc > DetectionLeash || (!localDefense &&
                (targetFromAnchor > DetectionLeash || npcFromAnchor > CompanionPolicy.NpcLeash || recoveryRequired))) return false;
            bool nearby = CompanionPolicy.RefreshPursuit(activeThreat, damage > 0 ? 0 : Single.PositiveInfinity,
                Vector3.Distance(target.transform.position, transform.position));
            if (!CompanionPolicy.MayReacquireRetired(retiredPursuitTarget == target, explicitOrder, nearby || activeThreat)) return false;
            if (authorizedTarget == target)
            {
                if (CompanionPolicy.Finite(damage) && damage > 0) lastAttributedDamage = Time.time;
                if (localDefense && (npcFromAnchor > CompanionPolicy.TacticalNpcLeash || recoveryRequired) && !recoveryDefense)
                {
                    recoveryDefense = true;
                    combatOrigin = transform.position;
                }
                RefreshNearbyThreat(); return AuthorizationValid(target);
            }
            if (authorizedTarget != null) return false;
            BeginCombatOwnership(target, localDefense &&
                (npcFromAnchor > CompanionPolicy.TacticalNpcLeash || recoveryRequired),
                damage, explicitOrder ? "Explicit target handed to native combat" : "Party threat handed to native combat");
            SetNativeCombatTarget(target);
            return true;
        }

        void BeginCombatOwnership(Character target, bool recovery, float damage, string reason)
        {
            ReleaseOptionalActivity();
            bool continuingRecovery = recovery && recoveryDefense;
            authorizedTarget = target;
            if (!continuingRecovery) combatOrigin = transform.position;
            recoveryDefense = recovery;
            lastNearbyThreat = Time.time; lastSensed = Time.time;
            encounterOwned = true; encounterClearObserved = false; encounterClearScans = 0;
            encounterEvidenceUntil = Time.time + 1f;
            encounterClearSince = Single.NegativeInfinity;
            lastAttributedDamage = CompanionPolicy.Finite(damage) && damage > 0f ? Time.time : Single.NegativeInfinity;
            retiredPursuitTarget = null;
            // This is the single companion-to-combat handoff. After it, native
            // MonsterAI owns target movement, facing, weapon cadence and attacks.
            var cleric = GetComponent<HumanCleric>();
            if (cleric != null) cleric.InterruptForCombat();
            var presence = GetComponent<HumanFollowPresence>();
            if (presence != null) presence.ReleaseForCombat();
            var torch = GetComponent<HumanUtilityTorch>();
            if (torch != null) torch.ReleaseForWeapons();
            status = reason;
        }

        void SetNativeCombatTarget(Character target)
        {
            ai.SetFollowTarget(null);
            TargetField.SetValue(ai, target); StaticTargetField.SetValue(ai, null);
            LastPositionField.SetValue(ai, target.transform.position); BeenAtPositionField.SetValue(ai, false);
            TargetInfoMethod.Invoke(ai, new object[] { target.GetZDOID() });
            AlertMethod.Invoke(ai, new object[] { true });
        }

        bool LegalCombatTarget(Character target)
        {
            if (!Ready() || target == null || target.IsDead() || target == body || target.IsPlayer() ||
                target.IsTamed() || target.GetComponent<HumanCompanion>() != null || !OrderActive()) return false;
            return BaseAI.IsEnemy(body, target);
        }

        bool NativeTargetAllowed(Character target)
        {
            if (!LegalCombatTarget(target)) return false;
            Vector3 anchor = OrderAnchor();
            float targetFromNpc = Vector3.Distance(target.transform.position, transform.position);
            float targetFromAnchor = Vector3.Distance(target.transform.position, anchor);
            float npcFromAnchor = Vector3.Distance(transform.position, anchor);
            bool localDefense = LocalSelfDefense(target, 0f, targetFromNpc);
            bool livingPartyMember;
            float partyDistance = NearestLivingPartyMember(out livingPartyMember);
            bool recoveryRequired = CompanionPolicy.MustEnterCombatRecovery(npcFromAnchor,
                livingPartyMember, partyDistance);
            // Stealth is the one peaceful veto. A crouched leader permits native
            // awareness but not an unprovoked attack; a hostile already committed
            // to the protected party still crosses into combat immediately.
            bool stealthHold = ProtectingOwner() && observedLeader.IsCrouching();
            bool activeThreat = IsActiveThreat(target);
            bool passiveWildlife = target.GetFaction() == Character.Faction.AnimalsVeg;
            bool alreadyCommitted = target == authorizedTarget;
            return (alreadyCommitted || localDefense || !recoveryRequired) &&
                CompanionPolicy.MayAdoptNativeSelection(stealthHold, activeThreat, passiveWildlife, localDefense,
                    targetFromNpc, targetFromAnchor, npcFromAnchor);
        }

        bool AdoptNativeTarget(Character target, bool sensed, string reason)
        {
            if (!NativeTargetAllowed(target)) return false;
            if (authorizedTarget != target)
            {
                float npcFromAnchor = Vector3.Distance(transform.position, OrderAnchor());
                bool livingPartyMember;
                float partyDistance = NearestLivingPartyMember(out livingPartyMember);
                bool recoveryRequired = CompanionPolicy.MustEnterCombatRecovery(npcFromAnchor,
                    livingPartyMember, partyDistance);
                bool recovery = LocalSelfDefense(target, 0f,
                    Vector3.Distance(target.transform.position, transform.position)) &&
                    (npcFromAnchor > CompanionPolicy.TacticalNpcLeash || recoveryRequired);
                BeginCombatOwnership(target, recovery, 0f, reason);
            }
            if (sensed)
            {
                lastSensed = Time.time;
                lastNearbyThreat = Time.time;
                MarkEncounterEvidence(1f);
            }
            return true;
        }

        void ObserveAttributedDamage(float damage, Character attacker)
        {
            if (!CompanionPolicy.Finite(damage) || damage <= 0f || !LegalCombatTarget(attacker)) return;
            float distance = Vector3.Distance(transform.position, attacker.transform.position);
            if (distance > DetectionLeash) return;
            lastAttributedDamage = Time.time;
            lastNearbyThreat = Time.time;
            // Damage from a second hostile proves that the encounter is still live,
            // but it must not pretend the currently committed target was re-sensed.
            // Native MonsterAI remains free to finish that target before reacquiring.
            if (authorizedTarget == null || authorizedTarget == attacker)
                lastSensed = Time.time;
            encounterOwned = true;
            encounterClearObserved = false;
            encounterClearScans = 0;
            encounterEvidenceUntil = Mathf.Max(encounterEvidenceUntil, Time.time + 1f);
            encounterClearSince = Single.NegativeInfinity;
            // Damage renews the encounter but never makes a companion abandon a
            // living target mid-strike. Acquire the attacker only when combat has
            // no target; native combat finishes the current opponent first.
            if (authorizedTarget == null) Authorize(attacker, false, damage);
        }

        bool LocalSelfDefense(Character target, float damage, float targetDistance)
        {
            var hostile = target == null ? null : target.GetComponent<MonsterAI>();
            bool targetsSelf = hostile != null && hostile.GetTargetCreature() == body;
            return CompanionPolicy.MayDefendWhileSeparated(damage, targetsSelf, targetDistance);
        }

        bool IsActiveThreat(Character target)
        {
            if (target == null) return false;
            var hostile = target.GetComponent<MonsterAI>();
            if (hostile == null) return false;
            Character victim = hostile.GetTargetCreature();
            bool targetsProtectedParty = victim == body || (ProtectingOwner() && victim == observedLeader);
            var ally = victim == null ? null : victim.GetComponent<HumanCompanion>();
            long ownParty, otherParty; Player ownLeader, otherLeader;
            bool attackingParty = ally != null && TryGetConfidenceParty(out ownParty, out ownLeader) &&
                ally.TryGetConfidenceParty(out otherParty, out otherLeader) && ownParty == otherParty &&
                Vector3.Distance(victim.transform.position, OrderAnchor()) <= CompanionPolicy.NpcLeash;
            targetsProtectedParty |= attackingParty;

            bool sensed = Vector3.Distance(target.transform.position, transform.position) <= DetectionLeash &&
                Vector3.Distance(target.transform.position, OrderAnchor()) <= DetectionLeash &&
                (ai.CanSeeTarget(target) || ai.CanHearTarget(target));
            bool enemy = BaseAI.IsEnemy(body, target);
            if (!enemy) return false;

            // A crouched leader is asking the party to remain concealed. The
            // companion may observe and visibly track a hostile, but proximity
            // alone must not turn that observation into lethal authorization.
            // An attack on self, leader or party above still overrides concealment.
            if (ProtectingOwner() && observedLeader.IsCrouching() && !targetsProtectedParty) return false;

            // Native victim selection is the commit line. IsAlerted and sensing
            // remain awareness inputs, so curiosity can turn heads and display
            // its marker without stealing torch, formation or combat ownership.
            return CompanionPolicy.HostileCommitted(targetsProtectedParty, sensed, hostile.IsAlerted());
        }

        bool ProtectingOwner()
        {
            return observedLeader != null && CompanionPolicy.MayProtectOwner(!observedLeader.IsDead(), activeMotion,
                Vector3.Distance(observedLeader.transform.position, OrderAnchor()));
        }

        void RefreshNearbyThreat()
        {
            if (authorizedTarget == null) return;
            float distance = Vector3.Distance(authorizedTarget.transform.position, transform.position);
            bool sensed = LocalSelfDefense(authorizedTarget, 0f, distance) ||
                ai.CanSeeTarget(authorizedTarget) || ai.CanHearTarget(authorizedTarget);
            if (sensed) lastSensed = Time.time;
            if (CompanionPolicy.RefreshEngagement(IsActiveThreat(authorizedTarget), sensed,
                Time.time - lastAttributedDamage, distance))
            { lastNearbyThreat = Time.time; MarkEncounterEvidence(1f); }
        }

        void MarkEncounterEvidence(float seconds)
        {
            encounterOwned = true;
            encounterClearObserved = false;
            encounterClearScans = 0;
            encounterClearSince = Single.NegativeInfinity;
            encounterEvidenceUntil = Mathf.Max(encounterEvidenceUntil, Time.time + Mathf.Max(0f, seconds));
        }

        bool PartyCombatEvidence()
        {
            long ownParty; Player ownLeader;
            if (!TryGetConfidenceParty(out ownParty, out ownLeader))
                return authorizedTarget != null;
            foreach (Character candidate in Character.GetAllCharacters())
            {
                if (candidate == null || candidate.IsDead()) continue;
                var companion = candidate.GetComponent<HumanCompanion>();
                if (companion == null) continue;
                long candidateParty; Player candidateLeader;
                if (!companion.TryGetConfidenceParty(out candidateParty, out candidateLeader) || candidateParty != ownParty) continue;
                // Only real evidence may be shared. Reading another member's
                // short evidence pulse never renews it, so party arbitration
                // cannot form a self-sustaining feedback loop.
                if (companion.authorizedTarget != null ||
                    (companion != this && Time.time < companion.encounterEvidenceUntil)) return true;
            }
            return false;
        }

        float ThreatPriority(Character candidate)
        {
            if (candidate == null) return Single.NegativeInfinity;
            var hostile = candidate.GetComponent<MonsterAI>();
            if (hostile == null) return Single.NegativeInfinity;
            Character victim = hostile.GetTargetCreature();
            bool leader = ProtectingOwner() && victim == observedLeader;
            bool self = victim == body;
            bool party = false;
            var ally = victim == null ? null : victim.GetComponent<HumanCompanion>();
            long ownParty, otherParty; Player ownLeader, otherLeader;
            if (ally != null && TryGetConfidenceParty(out ownParty, out ownLeader) &&
                ally.TryGetConfidenceParty(out otherParty, out otherLeader)) party = ownParty == otherParty;
            float priority = CompanionPolicy.ThreatPriority(leader, self, party,
                Vector3.Distance(candidate.transform.position, OrderAnchor()),
                Vector3.Distance(candidate.transform.position, transform.position));
            // Alerted, sensed enemies have committed even while native AI is
            // still choosing a victim. Party-directed targets remain higher
            // priority, but companions no longer wait to be hit first.
            if (!CompanionPolicy.Finite(priority) && IsActiveThreat(candidate))
                priority = 500f - Vector3.Distance(candidate.transform.position, transform.position);
            return priority;
        }

        void FindActiveThreat()
        {
            if (Time.time < nextThreatScan) return;
            nextThreatScan = Time.time + 0.25f;
            Character best = null;
            float bestPriority = Single.NegativeInfinity;
            Character seen = null;
            float seenDistance = Single.PositiveInfinity;
            bool encounterHostileObserved = false;
            foreach (Character candidate in Character.GetAllCharacters())
            {
                if (candidate == null || candidate == body || candidate.IsDead() || candidate.IsPlayer() ||
                    candidate.IsTamed() || candidate.GetComponent<HumanCompanion>() != null) continue;
                float distance = Vector3.Distance(transform.position, candidate.transform.position);
                float fromAnchor = Vector3.Distance(candidate.transform.position, OrderAnchor());
                bool localDefense = LocalSelfDefense(candidate, 0f, distance);
                bool sensed = distance <= CompanionPolicy.AwarenessRange &&
                    (fromAnchor <= CompanionPolicy.AwarenessRange || localDefense) &&
                    (localDefense || ai.CanSeeTarget(candidate) || ai.CanHearTarget(candidate));
                bool enemy = BaseAI.IsEnemy(body, candidate);
                if (sensed && enemy && distance < seenDistance)
                { seen = candidate; seenDistance = distance; }
                var hostile = candidate.GetComponent<MonsterAI>();
                bool encounterThreat = CompanionPolicy.EncounterThreat(encounterOwned, enemy,
                    hostile != null && hostile.IsAlerted(), sensed,
                    candidate.GetFaction() == Character.Faction.AnimalsVeg);
                encounterHostileObserved |= encounterThreat;
                if ((!IsActiveThreat(candidate) && !encounterThreat) || distance > DetectionLeash || !sensed) continue;
                if (!CompanionPolicy.MayReacquireRetired(retiredPursuitTarget == candidate, false, true)) continue;
                if (!enemy || (!localDefense && fromAnchor > DetectionLeash)) continue;
                float priority = ThreatPriority(candidate);
                if (!CompanionPolicy.Finite(priority) && encounterThreat)
                    priority = 500f - fromAnchor * 8f - distance;
                if (best == null || CompanionPolicy.ShouldSwitchThreat(bestPriority, priority))
                { best = candidate; bestPriority = priority; }
            }
            awarenessTarget = seen;
            var seenAi = seen == null ? null : seen.GetComponent<MonsterAI>();
            awarenessAlarmed = seenAi != null && seenAi.IsAlerted();
            // A quarter-second scan is evidence for acquiring or switching a
            // target, not authority to revoke the target already in combat.
            // AuthorizationValid owns disengagement and deliberately tolerates
            // short native alert, sight and hearing gaps. The old immediate
            // drop here bypassed that retention contract, producing the exact
            // acquire/idle/acquire loop recorded in the 2026-10-17 lab run.
            if (authorizedTarget != null)
            {
                // A committed fight is stable. Repeated quarter-second scans may
                // observe the rest of the encounter, but they never replace a
                // living legal target and reset native movement/attack cadence.
                encounterClearScans = 0;
                encounterClearObserved = false;
                encounterClearSince = Single.NegativeInfinity;
                encounterEvidenceUntil = Mathf.Max(encounterEvidenceUntil, Time.time + 1f);
                return;
            }
            if (best != null)
            {
                if (Authorize(best, false, 0f)) status = "Companion tactics selected the highest-duty active threat";
            }
            else if (best == null && encounterOwned)
            {
                bool partyCombatEvidence = PartyCombatEvidence();
                bool recentLocalEvidence = Time.time < encounterEvidenceUntil;
                if (encounterHostileObserved || partyCombatEvidence || recentLocalEvidence)
                {
                    encounterClearScans = 0;
                    encounterClearObserved = false;
                    encounterClearSince = Single.NegativeInfinity;
                    if (encounterHostileObserved) encounterEvidenceUntil = Time.time + 1f;
                    return;
                }
                // Clear scans and the quiet interval are sequential, not
                // parallel. Old clear observations from a previous target may
                // never satisfy the next target handoff.
                int previous = encounterClearScans;
                encounterClearScans = CompanionPolicy.AdvanceEncounterClearScans(encounterClearScans,
                    partyCombatEvidence, encounterHostileObserved);
                if (previous < CompanionPolicy.EncounterClearScanCount &&
                    encounterClearScans >= CompanionPolicy.EncounterClearScanCount)
                {
                    encounterClearObserved = true;
                    encounterClearSince = Time.time;
                }
                float clearSeconds = encounterClearSince < 0f ? 0f : Time.time - encounterClearSince;
                if (CompanionPolicy.EncounterClearVerified(encounterClearScans, clearSeconds,
                    PartyCombatEvidence(), false))
                {
                    encounterOwned = false;
                    recoveryDefense = false;
                    encounterEvidenceUntil = Single.NegativeInfinity;
                    ClearNativeCombat();
                    status = "Party encounter clear verified; peaceful companion behavior released";
                }
            }
        }

        bool OrderActive()
        {
            return order != null && activeMotion != CompanionMotion.Invalid;
        }

        Vector3 OrderAnchor()
        {
            if (activeMotion == CompanionMotion.WaitForLeader && haveWaitAnchor) return waitAnchor;
            return activeMotion == CompanionMotion.Follow && observedLeader != null ? observedLeader.transform.position : new Vector3(order.X, order.Y, order.Z);
        }

        float NearestLivingPartyMember(out bool found)
        {
            found = false;
            long ownParty; Player ownLeader;
            if (!TryGetConfidenceParty(out ownParty, out ownLeader)) return Single.PositiveInfinity;
            float nearest = Single.PositiveInfinity;
            foreach (Character candidate in Character.GetAllCharacters())
            {
                if (candidate == null || candidate == body || candidate.IsDead()) continue;
                var companion = candidate.GetComponent<HumanCompanion>();
                if (companion == null) continue;
                long candidateParty; Player candidateLeader;
                if (!companion.TryGetConfidenceParty(out candidateParty, out candidateLeader) || candidateParty != ownParty) continue;
                found = true;
                nearest = Mathf.Min(nearest, Vector3.Distance(transform.position, candidate.transform.position));
            }
            return nearest;
        }

        bool AuthorizationValid(Character candidate)
        {
            if (candidate != authorizedTarget || !LegalCombatTarget(candidate)) return false;
            Vector3 anchor = OrderAnchor();
            float npcFromAnchor = Vector3.Distance(transform.position, anchor);
            bool livingPartyMember;
            float partyDistance = NearestLivingPartyMember(out livingPartyMember);
            if (!recoveryDefense && CompanionPolicy.MustEnterCombatRecovery(npcFromAnchor,
                livingPartyMember, partyDistance))
            {
                recoveryDefense = true;
                combatOrigin = transform.position;
                status = "Combat recovery boundary reached; finish locally and regroup";
            }
            float targetFromNpc = Vector3.Distance(candidate.transform.position, transform.position);
            bool sensed = LocalSelfDefense(candidate, 0f, targetFromNpc) || ai.CanSeeTarget(candidate) || ai.CanHearTarget(candidate);
            if (sensed) lastSensed = Time.time;
            if (sensed || IsActiveThreat(candidate) || Time.time - lastAttributedDamage < 3f)
                lastNearbyThreat = Time.time;
            if (recoveryDefense)
                return CompanionPolicy.WithinRecoveryDefenseBounds(Time.time - lastNearbyThreat, Time.time - lastSensed,
                    Vector3.Distance(candidate.transform.position, combatOrigin), Vector3.Distance(transform.position, combatOrigin),
                    targetFromNpc);
            return CompanionPolicy.WithinCommittedCombatBounds(Time.time - lastNearbyThreat, Time.time - lastSensed,
                Vector3.Distance(candidate.transform.position, combatOrigin), Vector3.Distance(transform.position, combatOrigin),
                Vector3.Distance(candidate.transform.position, anchor), npcFromAnchor, targetFromNpc);
        }

        static bool SafeNativeAttack(Attack attack)
        {
            return attack != null && CompanionPolicy.SafeNativeAttack(attack.m_hitFriendly, attack.m_attackKillsSelf, attack.m_selfDamage);
        }

        bool SafeEquippedWeapon()
        {
            if (body == null || appearance == null) return false;
            var weapon = body.GetCurrentWeapon();
            var equipment = GetComponent<HumanPartyEquipment>();
            return weapon != null && weapon.m_equipped && body.GetInventory().GetAllItems().Contains(weapon) &&
                (weapon.m_shared.m_name == appearance.WeaponName || (equipment != null && equipment.AllowsWeapon(weapon))) && SafeNativeAttack(weapon.m_shared.m_attack) &&
                SafeNativeAttack(weapon.m_shared.m_secondaryAttack);
        }

        // Party identity persists during combat and Hold. Peaceful PresenceLeader
        // is intentionally not used as a proxy for separation or citizenship.
        internal bool TryGetConfidenceParty(out long partyId, out Player leader)
        {
            partyId = 0; leader = null;
            if (!Ready() || order == null || order.OwnerId == 0) return false;
            partyId = order.OwnerId;
            leader = Player.GetPlayer(partyId);
            return true;
        }

        public void StopCombat()
        { StopCombatCore(true); }

        void StopCombatCore(bool endEncounter)
        {
            if (!Owner()) { authorizedTarget = null; if (endEncounter) { recoveryDefense = false; encounterOwned = false; encounterClearObserved = true; encounterClearScans = 0; encounterEvidenceUntil = Single.NegativeInfinity; encounterClearSince = Single.NegativeInfinity; } return; }
            ReleaseOptionalActivity();
            retiredPursuitTarget = authorizedTarget != null ? authorizedTarget : retiredPursuitTarget;
            authorizedTarget = null;
            if (endEncounter) { recoveryDefense = false; encounterOwned = false; encounterClearObserved = true; encounterClearScans = 0; encounterEvidenceUntil = Single.NegativeInfinity; encounterClearSince = Single.NegativeInfinity; }
            ClearNativeCombat(); status = "Combat stopped; persistent movement order retained";
        }

        void DropCombatTarget()
        {
            bool recovering = recoveryDefense;
            recoveryDefense = false;
            MarkEncounterEvidence(.5f);
            StopCombatCore(false);
            status = recovering ?
                "Local defense target ended; party encounter retained while regrouping" :
                "Target ended; party encounter retained through target handoff";
        }

        void ReleaseOptionalActivity()
        {
            var ambient = GetComponent<HumanCompanionAmbient>();
            if (ambient != null) ambient.Cancel("companion order/combat transition");
            else
            {
                var posture = GetComponent<HumanCompanionPosture>();
                if (posture != null) posture.StopAmbient();
            }
        }

        void ClearNativeCombat()
        {
            if (!Owner() || ai == null || body == null) return;
            TargetField.SetValue(ai, null); StaticTargetField.SetValue(ai, null);
            TargetInfoMethod.Invoke(ai, new object[] { ZDOID.None });
            AlertMethod.Invoke(ai, new object[] { false });
            AttackTimerField.SetValue(ai, 0f); ai.ChargeStop();
            var current = CurrentAttackField.GetValue(body) as Attack;
            if (current != null)
            {
                current.Abort();
                // Stop has a native optional suicide side effect. An unexpected
                // unsafe attack is detached without executing that branch.
                if (!current.m_attackKillsSelf) current.Stop();
                // Native OnAttackTrigger ignores Abort/IsDone. Removing this
                // instance prevents delayed animation events reusing it.
                CurrentAttackField.SetValue(body, null);
            }
        }

        void ObserveLeader(Player next)
        {
            if (observedLeader == next) return;
            if (observedLeader != null) observedLeader.m_onDamaged -= LeaderDamaged;
            observedLeader = next;
            if (observedLeader != null) observedLeader.m_onDamaged += LeaderDamaged;
        }

        void LeaderDamaged(float damage, Character attacker)
        {
            try { if (Ready() && ProtectingOwner()) Authorize(attacker, false, damage); }
            catch (Exception error) { Fault(error); }
        }

        void ReconcileLeader()
        {
            // Hold retains the same protected owner identity without becoming a
            // follow order; its combat anchor remains the saved hold position.
            Player leader = temporaryFollow ? temporaryLeader : (order != null ? Player.GetPlayer(order.OwnerId) : null);
            if (leader != null && leader.IsDead()) leader = null;
            ObserveLeader(leader);
            CompanionMotion motion = CompanionPolicy.Motion(order, leader != null, leader != null && !leader.IsDead(),
                leader == null ? Single.PositiveInfinity : Vector3.Distance(transform.position, leader.transform.position));
            if (temporaryFollow) motion = leader != null && Vector3.Distance(transform.position, leader.transform.position) <= CompanionPolicy.FollowResumeRange
                ? CompanionMotion.Follow : CompanionMotion.WaitForLeader;
            if (motion == CompanionMotion.Follow)
            {
                if (authorizedTarget != null) ai.SetFollowTarget(null);
                else
                {
                    if (CompanionPolicy.MustClearPatrol(activeMotion, motion, ai.GetFollowTarget() == leader.gameObject))
                    { ai.ResetPatrolPoint(); ai.ResetRandomMovement(); }
                    ai.SetFollowTarget(leader.gameObject);
                }
                patrolApplied = false;
                haveWaitAnchor = false;
            }
            else
            {
                ai.SetFollowTarget(null);
                if (motion == CompanionMotion.Hold && !patrolApplied)
                {
                    PatrolMethod.Invoke(ai, new object[] { new Vector3(order.X, order.Y, order.Z) });
                    ai.ResetRandomMovement(); patrolApplied = true;
                }
                else if (CompanionPolicy.MustCaptureWaitAnchor(activeMotion, motion, haveWaitAnchor))
                {
                    waitAnchor = transform.position; haveWaitAnchor = true;
                    // Clear the previous saved hold point before installing a
                    // runtime wait anchor at the current position.
                    ai.ResetPatrolPoint(); PatrolMethod.Invoke(ai, new object[] { waitAnchor });
                    ai.ResetRandomMovement(); ai.StopMoving(); patrolApplied = false;
                }
            }
            CompanionMotion previousMotion = activeMotion;
            activeMotion = motion;
            if (motion == CompanionMotion.WaitForLeader && previousMotion != motion && authorizedTarget != null && !IsActiveThreat(authorizedTarget))
                StopCombat();
        }

        bool Tick()
        {
            Cache();
            if (!Owner())
            {
                // Never claim authority. Remote state remains native replication.
                hadAuthority = false; authorizedTarget = null; recoveryDefense = false; encounterOwned = false; encounterClearObserved = true; encounterEvidenceUntil = Single.NegativeInfinity; encounterClearSince = Single.NegativeInfinity; temporaryFollow = false; temporaryLeader = null;
                activeMotion = CompanionMotion.Invalid; haveWaitAnchor = false; ObserveLeader(null);
                status = "Remote instance: no companion writes"; return false;
            }
            if (!hadAuthority)
            {
                hadAuthority = true; authorizedTarget = null; recoveryDefense = false; encounterOwned = false; encounterClearObserved = true; encounterEvidenceUntil = Single.NegativeInfinity; encounterClearSince = Single.NegativeInfinity; patrolApplied = false;
                ClearNativeCombat(); // Combat authorizations never survive reload/ownership transfer.
            }
            if (!Ready())
            {
                ClearNativeCombat(); if (ai != null) { ai.SetFollowTarget(null); ai.StopMoving(); }
                ObserveLeader(null);
                status = "Passive: composition/appearance/orders or saved native hunt/event/sleep state not ready"; return false;
            }
            ReconcileLeader();
            // A target may have been selected by the preceding native update or
            // by native OnDamaged between frames. Adopt it; never make native AI
            // wait for a separate companion authorization vote.
            ObserveNativeTarget(false, false);
            RefreshNearbyThreat();
            if (authorizedTarget != null && !AuthorizationValid(authorizedTarget))
            { DropCombatTarget(); status = "Target ended; encounter ownership retained while nearby threats are checked"; }
            if (!encounterOwned && PartyCombatEvidence())
            {
                MarkEncounterEvidence(1f);
                status = "Party encounter shared from an engaged companion";
            }
            FindActiveThreat();
            // Do not fall back into leader-follow during the short gap between
            // a dead/lost target and the encounter's next threat selection.
            // Combat movement remains authoritative until repeated clear scans
            // end encounter ownership.
            if (authorizedTarget != null) ai.SetFollowTarget(null);
            else if (encounterOwned && activeMotion == CompanionMotion.Follow && observedLeader != null)
                ai.SetFollowTarget(observedLeader.gameObject);
            if (activeMotion == CompanionMotion.WaitForLeader && authorizedTarget == null)
            {
                ClearNativeCombat(); ai.StopMoving();
                status = "Follow retained: waiting here for living bound player within 64m; local self-defense available"; return false;
            }
            return true;
        }

        void ObserveNativeTarget(bool canHear, bool canSee)
        {
            var native = TargetField.GetValue(ai) as Character;
            if (native != null)
            {
                if (AdoptNativeTarget(native, canHear || canSee, "Native MonsterAI selected combat target")) return;
                // Post-selection validation is a safety fence, not another combat
                // controller. It rejects only forbidden allies, stealth holds or
                // targets outside the companion/order envelope.
                ClearNativeCombat();
            }
            if (authorizedTarget == null) return;
            // Native combat may end pursuit after its own lost-target/death/path
            // rules. Treat that as target retirement, retain the encounter long
            // enough to select another active threat, and do not resurrect it.
            retiredPursuitTarget = authorizedTarget;
            DropCombatTarget();
        }

        void Fault(Exception error)
        {
            localFault = true; status = "Companion fail-closed: " + error.GetType().Name + ": " + error.Message;
            try { StopCombat(); if (Owner() && ai != null) { ai.SetFollowTarget(null); ai.StopMoving(); } }
            catch { /* Preserve the original fail-closed flag; native update remains vetoed. */ }
            status = "Companion fail-closed: " + error.GetType().Name + ": " + error.Message;
        }

        void OnDisable() { ObserveLeader(null); }
        void OnDestroy() { ObserveLeader(null); }

        static bool BeforeUpdate(MonsterAI __instance, ref bool __result)
        {
            var companion = __instance.GetComponent<HumanCompanion>();
            if (companion == null) return true;
            try
            {
                if (companion.Tick())
                {
                    var cleric = companion.GetComponent<HumanCleric>();
                    if (cleric == null || !cleric.Casting) return true;
                    __instance.StopMoving();
                }
            }
            catch (Exception error) { companion.Fault(error); }
            __result = false; return false;
        }

        static void AfterTarget(MonsterAI __instance, ref bool __2, ref bool __3)
        {
            var companion = __instance.GetComponent<HumanCompanion>();
            if (companion == null) return;
            try { companion.ObserveNativeTarget(__2, __3); }
            catch (Exception error) { companion.Fault(error); }
        }

        static void BeforeDamaged(MonsterAI __instance, float __0, Character __1)
        {
            var companion = __instance.GetComponent<HumanCompanion>();
            if (companion == null) return;
            try { companion.ObserveAttributedDamage(__0, __1); }
            catch (Exception error) { companion.Fault(error); }
        }

        static bool BeforeSetTarget(MonsterAI __instance, Character __0)
        {
            var companion = __instance.GetComponent<HumanCompanion>();
            if (companion == null) return true;
            try { return companion.NativeTargetAllowed(__0); }
            catch (Exception error) { companion.Fault(error); return false; }
        }

        static bool BeforeAttack(MonsterAI __instance, Character __0, ref bool __result)
        {
            var companion = __instance.GetComponent<HumanCompanion>();
            if (companion == null) return true;
            try
            {
                if (companion.AuthorizationValid(__0) && companion.SafeEquippedWeapon())
                {
                    var cleric = companion.GetComponent<HumanCleric>();
                    if (cleric != null && cleric.Casting) { __result = false; return false; }
                    var equipment = companion.GetComponent<HumanPartyEquipment>();
                    if (equipment == null || equipment.MayAttack(__0, false)) return true;
                }
                // A transient equipment, draw, lane or casting gate refuses only
                // this attack request.  It is not evidence that the enemy ceased
                // to exist and must never erase combat authorization.
            }
            catch (Exception error) { companion.Fault(error); }
            __result = false; return false;
        }

        static bool BeforeAttackTrigger(Attack __instance)
        {
            var attacker = AttackCharacterField.GetValue(__instance) as Humanoid;
            var companion = attacker == null ? null : attacker.GetComponent<HumanCompanion>();
            if (companion == null) return true;
            try
            {
                bool valid = companion.AuthorizationValid(companion.authorizedTarget);
                if (ReferenceEquals(CurrentAttackField.GetValue(attacker), __instance) && valid &&
                    companion.SafeEquippedWeapon() && SafeNativeAttack(__instance))
                {
                    var cleric = companion.GetComponent<HumanCleric>();
                    if (cleric != null && cleric.Casting) return false;
                    var equipment = companion.GetComponent<HumanPartyEquipment>();
                    if (equipment == null || equipment.MayAttack(companion.authorizedTarget, true)) return true;
                }
            }
            catch (Exception error) { companion.Fault(error); }
            return false;
        }
    }
}

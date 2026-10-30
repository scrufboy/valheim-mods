using System;
using BepInEx.Configuration;
using LivingWorld.Companions.FormidableAI.Classes;
using LivingWorld.FormidableAI.Integration;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Profile;
using LivingWorld.FormidableAI.Shadow;
using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Read-only bridge from the real 1.0.x NpcKernel frame to FormidableAI.
    /// It claims no domain and performs no native, ZDO, target, movement, facing,
    /// equipment, attack, ownership, or class-state writes.
    /// </summary>
    public sealed class FormidableShadowBehavior : NpcBehavior, IClassCombatFactSource
    {
        const int TraceCapacity = 64;
        const float SampleInterval = .5f;
        readonly BoundedShadowTrace trace = new BoundedShadowTrace(TraceCapacity);
        readonly FormidableIntegration integration = new FormidableIntegration();
        static ConfigEntry<bool> enabled;
        ClassCombatFacts facts;
        ClassProposalModule classModule;
        CharacterCombatProfile profile;
        string profileKey;
        FormidableClass role;
        long sequence;
        float nextSample;

        public override string Name { get { return "formidable-shadow"; } }
        public override NpcDomain Claims { get { return NpcDomain.None; } }
        public int TraceCount { get { return trace.Count; } }
        public string ProfileJob { get { return profile == null ? "" : profile.Job.ToString(); } }
        public string ProfileClass { get { return profile == null ? "" : profile.FormidableClass.ToString(); } }
        public string ProfileWeapons { get { return profile == null ? "" : profile.Weapons.ToString(); } }
        public string ProfileSurvival { get { return profile == null ? "" : profile.Survival.ToString(); } }

        public System.Collections.Generic.IList<ShadowTraceEntry> Snapshot()
        {
            return trace.Snapshot();
        }

        internal static void Bind(ConfigFile config)
        {
            enabled = config.Bind("FormidableAI", "ShadowObserver", false,
                "Development only: evaluate FormidableAI decisions against the real 1.0.x companion frame. Read-only, bounded, and claims no behavior domain.");
        }

        public override bool Wants(NpcFrame frame)
        {
            return enabled != null && enabled.Value;
        }

        public ClassCombatFacts Observe(CombatFrame frame)
        {
            return facts;
        }

        public override void Tick(NpcFrame nativeFrame)
        {
            if (nativeFrame.Now < nextSample) return;
            nextSample = nativeFrame.Now + SampleInterval;
            Character target = nativeFrame.Senses.Target;
            string actorId = Identity(Body.Human);
            string targetId = Identity(target);
            profile = Profile(actorId);
            FormidableClass nextRole = profile.FormidableClass;
            if (classModule == null || nextRole != role)
            {
                role = nextRole;
                classModule = new ClassProposalModule("formidable.class." + role.ToString().ToLowerInvariant(),
                    ClassProfile.Create(role), this);
            }

            float distance = target == null ? float.PositiveInfinity :
                Vector3.Distance(Body.Human.transform.position, target.transform.position);
            bool committed = nativeFrame.Phase == NpcPhase.Combat || nativeFrame.Phase == NpcPhase.CombatClear;
            facts = new ClassCombatFacts {
                OwnerAuthorized = Body.IsOwner,
                ActorReady = Body.Human != null && !Body.Human.IsDead(),
                CommittedCombat = committed,
                TargetAuthorized = committed && target != null,
                IncomingStrike = target != null && (target.InAttack() || target.IsDrawingBow()),
                ShieldReady = Body.ShieldInHand,
                PersonalThreat = target != null && TargetsActor(target),
                LineOfFireClear = true,
                BelowPreferredRange = role == FormidableClass.Huntress && distance < 8f,
                AbovePreferredRange = role == FormidableClass.Huntress && distance > 25f,
                TargetInReach = distance <= 4.5f,
                Lifecycle = ActionLifecyclePhase.Idle
            };
            LivingWorld.FormidableAI.Kernel.ThreatObservation[] threats = target == null ?
                new LivingWorld.FormidableAI.Kernel.ThreatObservation[0] : new[] {
                new LivingWorld.FormidableAI.Kernel.ThreatObservation(targetId, distance, true, facts.IncomingStrike)
            };
            float stamina = Body.MaxStamina <= 0f ? 0f : Body.Stamina / Body.MaxStamina;
            var frame = new CombatFrame(sequence, nativeFrame.Now, actorId, role.ToString(), targetId,
                Body.IsOwner, committed, Body.Human.GetHealthPercentage(), stamina, threats);
            FormidableIntegrationResult result = integration.Evaluate(new FormidableIntegrationInput {
                Frame = frame, ClassModule = classModule, DefenseDecision = null
            });
            NativeObservation observed = Native(targetId, nativeFrame.Now);
            FormidablePrediction predicted = Prediction(result, targetId, nativeFrame.Now, observed.Action);
            ShadowComparison comparison = ShadowComparer.Compare(observed, predicted);
            trace.Add(actorId, comparison);
            Status = "samples=" + trace.Count + "; role=" + role + "; mismatch=" + comparison.Mismatch +
                "; native=" + observed.Action + "; predicted=" + predicted.Action + "; reason=" + predicted.Reason;
            sequence++;
        }

        public override void Exit(NpcFrame frame, string reason)
        {
            Status = "disabled; retained samples=" + trace.Count;
        }

        NativeObservation Native(string targetId, float now)
        {
            ShadowAction action = Body.Human.InAttack() || Body.Human.IsDrawingBow() ? ShadowAction.Attack :
                Body.Human.IsBlocking() ? ShadowAction.Block : ShadowAction.None;
            return new NativeObservation(CombatDomain.Attack, DomainOwner.Native, action, targetId, null, now);
        }

        static FormidablePrediction Prediction(FormidableIntegrationResult result, string targetId,
            float now, ShadowAction nativeFallback)
        {
            DomainLease lease = result.Arbitration.LeaseFor(DecisionDomain.Attack);
            bool fallback = lease == null || lease.IsNativeFallback;
            ShadowAction action = fallback ? nativeFallback : Action(lease.ActionId);
            string reason = result.ClassDecision == null ? "no-class-decision" : result.ClassDecision.Reason;
            return new FormidablePrediction(CombatDomain.Attack,
                fallback ? DomainOwner.Native : DomainOwner.Formidable, action, reason, targetId, null, now);
        }

        CharacterCombatProfile Profile(string actorId)
        {
            NpcAppearance appearance = Body.Human.GetComponent<NpcAppearance>();
            string savedRole = appearance == null ? "guard" : appearance.Role;
            bool oneHanded=false, shield=false, bow=false, spear=false, heavy=false, magic=false;
            Inventory inventory=Body.Human.GetInventory();
            if(inventory!=null)
            {
                foreach(ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    if(item==null||item.m_shared==null) continue;
                    ItemDrop.ItemData.ItemType type=item.m_shared.m_itemType;
                    if(type==ItemDrop.ItemData.ItemType.Shield) shield=true;
                    if(type==ItemDrop.ItemData.ItemType.Bow) bow=true;
                    if(type==ItemDrop.ItemData.ItemType.TwoHandedWeapon||type==ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft) heavy=true;
                    if(item.m_shared.m_skillType==Skills.SkillType.Spears) spear=true;
                    if(item.m_shared.m_skillType==Skills.SkillType.ElementalMagic||item.m_shared.m_skillType==Skills.SkillType.BloodMagic) magic=true;
                    if(item.IsWeapon()&&type!=ItemDrop.ItemData.ItemType.Bow&&type!=ItemDrop.ItemData.ItemType.TwoHandedWeapon&&type!=ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft) oneHanded=true;
                }
            }
            string key=savedRole+"|"+oneHanded+shield+bow+spear+heavy+magic;
            if(profile!=null&&profileKey==key) return profile;
            profileKey=key;
            profile=CharacterCombatProfileFactory.Create(new CharacterRollupFacts{
                PersonIdentity=actorId, SavedRole=savedRole, Origin=OriginBiome.Unknown,
                Culture="", GenderExpression=appearance==null?"":(appearance.Female?"female":"male"),
                Training=TrainingBand.Trained, HasOneHanded=oneHanded, HasShield=shield,
                HasBow=bow, HasSpear=spear, HasHeavy=heavy, HasMagic=magic
            });
            return profile;
        }

        bool TargetsActor(Character threat)
        {
            MonsterAI ai = threat == null ? null : threat.GetBaseAI() as MonsterAI;
            return ai != null && ReferenceEquals(ai.GetTargetCreature(), Body.Human);
        }

        static ShadowAction Action(string actionId)
        {
            if (String.IsNullOrEmpty(actionId)) return ShadowAction.None;
            if (actionId.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                actionId.IndexOf("pressure", StringComparison.OrdinalIgnoreCase) >= 0) return ShadowAction.Attack;
            if (actionId.IndexOf("reposition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                actionId.IndexOf("protect", StringComparison.OrdinalIgnoreCase) >= 0) return ShadowAction.Reposition;
            if (actionId.IndexOf("support", StringComparison.OrdinalIgnoreCase) >= 0) return ShadowAction.Support;
            return ShadowAction.Yield;
        }

        static string Identity(Character character)
        {
            return character == null ? null : character.GetZDOID().ToString();
        }
    }
}

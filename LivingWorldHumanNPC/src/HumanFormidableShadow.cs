using System;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Integration;
using LivingWorld.FormidableAI.Shadow;
using LivingWorld.Companions.FormidableAI.Classes;
using UnityEngine;

namespace LivingWorld.HumanNpc
{
    // Read-only migration observer. It never writes movement, facing, targets,
    // attacks, ZDO state, ownership or class state.
    public sealed class HumanFormidableShadow : MonoBehaviour, IClassCombatFactSource
    {
        internal static bool Enabled;
        const float SampleInterval = .1f;
        const int TraceCapacity = 64;

        readonly BoundedShadowTrace trace = new BoundedShadowTrace(TraceCapacity);
        readonly FormidableIntegration integration = new FormidableIntegration();
        ZNetView view;
        Humanoid body;
        MonsterAI ai;
        HumanIdentity identity;
        HumanCompanion companion;
        HumanCleric cleric;
        ClassCombatFacts currentFacts;
        ClassProposalModule classModule;
        FormidableClass role;
        bool haveRole;
        float nextSample;
        long sequence;
        string latest = "shadow disabled";

        public int TraceCount { get { return trace.Count; } }

        void Cache()
        {
            if (view == null) view = GetComponent<ZNetView>();
            if (body == null) body = GetComponent<Humanoid>();
            if (ai == null) ai = GetComponent<MonsterAI>();
            if (identity == null) identity = GetComponent<HumanIdentity>();
            if (companion == null) companion = GetComponent<HumanCompanion>();
            if (cleric == null) cleric = GetComponent<HumanCleric>();
        }

        public ClassCombatFacts Observe(CombatFrame frame) { return currentFacts; }

        public string Describe()
        {
            return "formidableShadow=" + (Enabled ? "on" : "off") + "; samples=" + trace.Count + "; " + latest;
        }

        void Update()
        {
            if (!Enabled) { latest = "shadow disabled"; return; }
            Cache();
            if (Time.realtimeSinceStartup < nextSample) return;
            nextSample = Time.realtimeSinceStartup + SampleInterval;
            if (view == null || !view.IsValid() || !view.IsOwner() || body == null || body.IsDead() ||
                ai == null || identity == null || companion == null)
            { latest = "passive: actor not locally authoritative/ready"; return; }

            HumanProfile profile;
            if (identity.Read(out profile) != HumanProfileState.Ready)
            { latest = "passive: stable human identity unavailable"; return; }

            Character target = ai.GetTargetCreature();
            string targetId = Identity(target);
            FormidableClass nextRole = HumanCleric.IsCleric(gameObject) ? FormidableClass.Mystic : FormidableClass.Warden;
            if (!haveRole || role != nextRole)
            {
                role = nextRole; haveRole = true;
                classModule = new ClassProposalModule("formidable.class." + role.ToString().ToLowerInvariant(),
                    ClassProfile.Create(role), this);
            }

            currentFacts = new ClassCombatFacts {
                OwnerAuthorized = true,
                ActorReady = true,
                CommittedCombat = companion.CombatOwnsActor,
                TargetAuthorized = target != null && companion.HasAuthorizedCombat,
                IncomingStrike = false,
                ShieldReady = body.IsBlocking(),
                PersonalThreat = ThreatTargetsActor(),
                LineOfFireClear = true,
                SupportSafe = false,
                Lifecycle = ActionLifecyclePhase.Idle
            };
            ThreatObservation[] threats = target == null ? new ThreatObservation[0] : new[] {
                new ThreatObservation(targetId, Vector3.Distance(transform.position, target.transform.position),
                    ai.CanSeeTarget(target), target.InAttack() || target.IsDrawingBow())
            };
            var frame = new CombatFrame(sequence, Time.time, profile.Id, role.ToString(), targetId,
                true, companion.CombatOwnsActor, body.GetHealthPercentage(), StaminaFraction(), threats);
            var result = integration.Evaluate(new FormidableIntegrationInput {
                Frame = frame, ClassModule = classModule, DefenseDecision = null
            });
            NativeObservation native = NativeAttackObservation(targetId);
            FormidablePrediction prediction = Prediction(result, targetId);
            ShadowComparison comparison = ShadowComparer.Compare(native, prediction);
            trace.Add(profile.Id, comparison);
            latest = "role=" + role + "; mismatch=" + comparison.Mismatch +
                "; native=" + native.Action + "; predicted=" + prediction.Action +
                "; reason=" + prediction.Reason;
            sequence++;
        }

        NativeObservation NativeAttackObservation(string targetId)
        {
            ShadowAction action = body.InAttack() || body.IsDrawingBow() ? ShadowAction.Attack :
                body.IsBlocking() ? ShadowAction.Block : ShadowAction.None;
            return new NativeObservation(CombatDomain.Attack, DomainOwner.Native, action,
                targetId, null, Time.time);
        }

        FormidablePrediction Prediction(FormidableIntegrationResult result, string targetId)
        {
            DomainLease lease = result.Arbitration.LeaseFor(DecisionDomain.Attack);
            bool fallback = lease == null || lease.IsNativeFallback;
            ShadowAction action = fallback ? NativeAttackObservation(targetId).Action : Action(lease.ActionId);
            string reason = result.ClassDecision == null ? "no-class-decision" : result.ClassDecision.Reason;
            return new FormidablePrediction(CombatDomain.Attack,
                fallback ? DomainOwner.Native : DomainOwner.Formidable, action, reason,
                targetId, null, Time.time);
        }

        static ShadowAction Action(string actionId)
        {
            if (String.IsNullOrEmpty(actionId)) return ShadowAction.None;
            if (actionId.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                actionId.IndexOf("pressure", StringComparison.OrdinalIgnoreCase) >= 0)
                return ShadowAction.Attack;
            if (actionId.IndexOf("reposition", StringComparison.OrdinalIgnoreCase) >= 0 ||
                actionId.IndexOf("protect", StringComparison.OrdinalIgnoreCase) >= 0)
                return ShadowAction.Reposition;
            if (actionId.IndexOf("support", StringComparison.OrdinalIgnoreCase) >= 0)
                return ShadowAction.Support;
            return ShadowAction.Yield;
        }

        bool ThreatTargetsActor()
        {
            foreach (Character candidate in Character.GetAllCharacters())
            {
                if (candidate == null || candidate == body || candidate.IsDead()) continue;
                MonsterAI hostile = candidate.GetComponent<MonsterAI>();
                if (hostile != null && hostile.GetTargetCreature() == body) return true;
            }
            return false;
        }

        float StaminaFraction()
        {
            // Non-player Humanoid exposes the budget decision, not a public
            // current-stamina value. Preserve that evidence boundary rather
            // than reflecting private state or fabricating precision.
            return body.HaveStamina(15f) ? 1f : 0f;
        }

        static string Identity(Character character)
        {
            return character == null ? null : character.GetZDOID().ToString();
        }
    }
}

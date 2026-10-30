using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;
using LivingWorld.FormidableAI.Defense;
using LivingWorld.Companions.FormidableAI.Classes;

namespace LivingWorld.FormidableAI.Integration
{
    public sealed class FormidableIntegrationInput
    {
        public CombatFrame Frame;
        public ClassActionRequest ClassRequest;
        public DefenseDecision DefenseDecision;
    }

    public sealed class FormidableIntegrationResult
    {
        public ArbitrationResult Arbitration;
        public string DefenseAction;
        public string ClassAction;
    }

    internal sealed class FixedProposalModule : IFormidableCombatModule
    {
        private readonly string _id;
        private readonly CombatProposal[] _proposals;
        public string Id { get { return _id; } }

        public FixedProposalModule(string id, CombatProposal proposal)
        {
            _id = id;
            _proposals = proposal == null ? new CombatProposal[0] : new[] { proposal };
        }

        public IEnumerable<CombatProposal> Evaluate(CombatFrame frame) { return _proposals; }
    }

    public sealed class FormidableIntegration
    {
        private readonly CombatArbitrator _arbitrator = new CombatArbitrator();

        public FormidableIntegrationResult Evaluate(FormidableIntegrationInput input)
        {
            if (input == null || input.Frame == null) throw new ArgumentNullException("input");
            List<IFormidableCombatModule> modules = new List<IFormidableCombatModule>();

            CombatProposal defense = DefenseProposal(input.DefenseDecision, input.Frame.Time);
            if (defense != null) modules.Add(new FixedProposalModule("formidable.defense", defense));

            CombatProposal role = ClassProposal(input.ClassRequest, input.Frame.Time,
                input.Frame.AuthorizedTargetIdentity);
            if (role != null) modules.Add(new FixedProposalModule("formidable.class", role));

            return new FormidableIntegrationResult {
                Arbitration = _arbitrator.Evaluate(input.Frame, modules),
                DefenseAction = input.DefenseDecision == null ? null : input.DefenseDecision.Action.ToString(),
                ClassAction = input.ClassRequest == null ? null : input.ClassRequest.Kind.ToString()
            };
        }

        private static CombatProposal DefenseProposal(DefenseDecision decision, float now)
        {
            if (decision == null || decision.Action == DefenseAction.None ||
                decision.Action == DefenseAction.YieldToNative) return null;
            return new CombatProposal("defense:" + decision.Action.ToString().ToLowerInvariant(),
                DecisionDomain.Defense, 1000, decision.ThreatIdentity, now + .1f);
        }

        private static CombatProposal ClassProposal(ClassActionRequest request, float now, string target)
        {
            if (request == null || request.Kind == ClassActionKind.Yield) return null;
            // A class profile grants capability and priority; it does not duplicate the exact
            // action-moment defense policy. Only DefensePolicy may claim the Defense domain.
            if (request.Domain == ClassActionDomain.Defense) return null;
            DecisionDomain domain = Map(request.Domain);
            if (domain == DecisionDomain.None) return null;
            return new CombatProposal("class:" + request.Kind.ToString().ToLowerInvariant(), domain,
                request.Priority, target, now + .1f);
        }

        private static DecisionDomain Map(ClassActionDomain domain)
        {
            switch (domain)
            {
                case ClassActionDomain.Awareness: return DecisionDomain.Perception | DecisionDomain.Targeting;
                case ClassActionDomain.MovementIntent: return DecisionDomain.Movement | DecisionDomain.Facing;
                case ClassActionDomain.Support: return DecisionDomain.Support | DecisionDomain.Facing;
                case ClassActionDomain.WeaponAction: return DecisionDomain.Attack | DecisionDomain.Facing;
                default: return DecisionDomain.None;
            }
        }
    }
}

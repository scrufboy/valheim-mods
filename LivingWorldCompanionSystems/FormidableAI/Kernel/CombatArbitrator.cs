using System;
using System.Collections.Generic;

namespace LivingWorld.FormidableAI.Kernel
{
    public sealed class CombatArbitrator
    {
        public const string NativeOwnerId = "NativeMonsterAI";
        private static readonly DecisionDomain[] AtomicDomains = {
            DecisionDomain.Perception, DecisionDomain.Targeting, DecisionDomain.Movement,
            DecisionDomain.Facing, DecisionDomain.Defense, DecisionDomain.Equipment,
            DecisionDomain.Attack, DecisionDomain.Support
        };

        private struct Candidate
        {
            public string ModuleId;
            public CombatProposal Proposal;
        }

        private readonly Dictionary<DecisionDomain, DomainLease> _active =
            new Dictionary<DecisionDomain, DomainLease>();
        private readonly HashSet<string> _disabledModules = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<Candidate> _candidates = new List<Candidate>(16);
        private readonly List<CombatProposal> _moduleProposals = new List<CombatProposal>(8);
        private readonly Candidate[] _winners = new Candidate[8];
        private readonly bool[] _hasWinner = new bool[8];
        private long _revision;

        public ArbitrationResult Evaluate(CombatFrame frame, IList<IFormidableCombatModule> modules)
        {
            ArbitrationBuffer buffer = new ArbitrationBuffer();
            EvaluateInto(frame, modules, buffer);
            DomainLease[] leases = new DomainLease[buffer.LeaseCount];
            for (int i = 0; i < leases.Length; i++)
            {
                DomainLease x = buffer.GetLease(i);
                leases[i] = new DomainLease { Domain = x.Domain, OwnerId = x.OwnerId, ActionId = x.ActionId,
                    TargetIdentity = x.TargetIdentity, Revision = x.Revision };
            }
            return new ArbitrationResult(frame.Sequence, leases, buffer.CopyFaults());
        }

        public void EvaluateInto(CombatFrame frame, IList<IFormidableCombatModule> modules, ArbitrationBuffer output)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            if (output == null) throw new ArgumentNullException("output");
            output.Reset(frame.Sequence);
            _candidates.Clear();
            Array.Clear(_hasWinner, 0, _hasWinner.Length);

            if (frame.OwnerReady && modules != null)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    IFormidableCombatModule module = modules[i];
                    if (module == null || string.IsNullOrEmpty(module.Id) || _disabledModules.Contains(module.Id)) continue;
                    try
                    {
                        IBufferedFormidableCombatModule buffered = module as IBufferedFormidableCombatModule;
                        if (buffered != null)
                        {
                            _moduleProposals.Clear();
                            buffered.Collect(frame, _moduleProposals);
                            for (int p = 0; p < _moduleProposals.Count; p++)
                            {
                                CombatProposal proposal = _moduleProposals[p];
                                if (Valid(proposal, frame.Time)) _candidates.Add(new Candidate { ModuleId = module.Id, Proposal = proposal });
                            }
                        }
                        else
                        {
                            IEnumerable<CombatProposal> proposals = module.Evaluate(frame);
                            if (proposals == null) continue;
                            foreach (CombatProposal proposal in proposals)
                            {
                                if (Valid(proposal, frame.Time)) _candidates.Add(new Candidate { ModuleId = module.Id, Proposal = proposal });
                            }
                        }
                    }
                    catch (Exception error)
                    {
                        _disabledModules.Add(module.Id);
                        output.AddFault(module.Id, error);
                    }
                }
            }

            _candidates.Sort(CompareCandidates);
            DecisionDomain claimed = DecisionDomain.None;
            for (int i = 0; i < _candidates.Count; i++)
            {
                Candidate candidate = _candidates[i];
                if ((claimed & candidate.Proposal.Domains) != DecisionDomain.None) continue;
                for (int d = 0; d < AtomicDomains.Length; d++)
                {
                    DecisionDomain domain = AtomicDomains[d];
                    if ((candidate.Proposal.Domains & domain) != 0)
                    {
                        _winners[d] = candidate;
                        _hasWinner[d] = true;
                    }
                }
                claimed |= candidate.Proposal.Domains;
            }

            for (int i = 0; i < AtomicDomains.Length; i++)
            {
                DecisionDomain domain = AtomicDomains[i];
                Candidate winner = _winners[i];
                DomainLease lease = _hasWinner[i]
                    ? SetLease(domain, winner.ModuleId, winner.Proposal.ActionId, winner.Proposal.TargetIdentity)
                    : SetLease(domain, NativeOwnerId, "native-fallback", frame.AuthorizedTargetIdentity);
                output.SetLease(i, lease);
            }
        }

        public bool CancelOwner(string moduleId, string reason)
        {
            if (string.IsNullOrEmpty(moduleId)) return false;
            bool changed = false;
            for (int i = 0; i < AtomicDomains.Length; i++)
            {
                DomainLease current;
                if (_active.TryGetValue(AtomicDomains[i], out current) && current.OwnerId == moduleId)
                {
                    SetLease(AtomicDomains[i], NativeOwnerId,
                        string.IsNullOrEmpty(reason) ? "cancelled-to-native" : "cancelled-to-native:" + reason, null);
                    changed = true;
                }
            }
            return changed;
        }

        public bool IsModuleDisabled(string moduleId) { return _disabledModules.Contains(moduleId); }
        public void ResetModule(string moduleId) { if (!string.IsNullOrEmpty(moduleId)) _disabledModules.Remove(moduleId); }

        private DomainLease SetLease(DecisionDomain domain, string owner, string action, string target)
        {
            DomainLease current;
            if (_active.TryGetValue(domain, out current) && current.OwnerId == owner &&
                current.ActionId == action && current.TargetIdentity == target) return current;
            DomainLease next = new DomainLease { Domain = domain, OwnerId = owner, ActionId = action,
                TargetIdentity = target, Revision = ++_revision };
            _active[domain] = next;
            return next;
        }

        private static bool Valid(CombatProposal proposal, float now)
        {
            return proposal != null && !string.IsNullOrEmpty(proposal.ActionId) &&
                proposal.Domains != DecisionDomain.None &&
                (proposal.Domains & ~DecisionDomain.All) == 0 && proposal.ValidUntil >= now;
        }

        private static int CompareCandidates(Candidate left, Candidate right)
        {
            int priority = right.Proposal.Priority.CompareTo(left.Proposal.Priority);
            if (priority != 0) return priority;
            int module = string.CompareOrdinal(left.ModuleId, right.ModuleId);
            if (module != 0) return module;
            return string.CompareOrdinal(left.Proposal.ActionId, right.Proposal.ActionId);
        }

    }
}

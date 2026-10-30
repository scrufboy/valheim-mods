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

        private sealed class Candidate
        {
            public string ModuleId;
            public CombatProposal Proposal;
        }

        private readonly Dictionary<DecisionDomain, DomainLease> _active =
            new Dictionary<DecisionDomain, DomainLease>();
        private readonly HashSet<string> _disabledModules = new HashSet<string>(StringComparer.Ordinal);
        private long _revision;

        public ArbitrationResult Evaluate(CombatFrame frame, IList<IFormidableCombatModule> modules)
        {
            if (frame == null) throw new ArgumentNullException("frame");
            List<Candidate> candidates = new List<Candidate>();
            List<ModuleFault> faults = new List<ModuleFault>();

            if (frame.OwnerReady && modules != null)
            {
                for (int i = 0; i < modules.Count; i++)
                {
                    IFormidableCombatModule module = modules[i];
                    if (module == null || string.IsNullOrEmpty(module.Id) || _disabledModules.Contains(module.Id)) continue;
                    try
                    {
                        IEnumerable<CombatProposal> proposals = module.Evaluate(frame);
                        if (proposals == null) continue;
                        foreach (CombatProposal proposal in proposals)
                        {
                            if (Valid(proposal, frame.Time)) candidates.Add(new Candidate { ModuleId = module.Id, Proposal = proposal });
                        }
                    }
                    catch (Exception error)
                    {
                        _disabledModules.Add(module.Id);
                        faults.Add(new ModuleFault { ModuleId = module.Id, ErrorType = error.GetType().Name, Message = error.Message });
                    }
                }
            }

            candidates.Sort(CompareCandidates);
            Dictionary<DecisionDomain, Candidate> winners = new Dictionary<DecisionDomain, Candidate>();
            DecisionDomain claimed = DecisionDomain.None;
            for (int i = 0; i < candidates.Count; i++)
            {
                Candidate candidate = candidates[i];
                if ((claimed & candidate.Proposal.Domains) != DecisionDomain.None) continue;
                for (int d = 0; d < AtomicDomains.Length; d++)
                {
                    DecisionDomain domain = AtomicDomains[d];
                    if ((candidate.Proposal.Domains & domain) != 0) winners[domain] = candidate;
                }
                claimed |= candidate.Proposal.Domains;
            }

            DomainLease[] leases = new DomainLease[AtomicDomains.Length];
            for (int i = 0; i < AtomicDomains.Length; i++)
            {
                DecisionDomain domain = AtomicDomains[i];
                Candidate winner;
                leases[i] = winners.TryGetValue(domain, out winner)
                    ? SetLease(domain, winner.ModuleId, winner.Proposal.ActionId, winner.Proposal.TargetIdentity)
                    : SetLease(domain, NativeOwnerId, "native-fallback", frame.AuthorizedTargetIdentity);
            }
            return new ArbitrationResult(frame.Sequence, Clone(leases), faults.ToArray());
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

        private static DomainLease[] Clone(DomainLease[] leases)
        {
            DomainLease[] copy = new DomainLease[leases.Length];
            for (int i = 0; i < leases.Length; i++)
            {
                DomainLease x = leases[i];
                copy[i] = new DomainLease { Domain = x.Domain, OwnerId = x.OwnerId, ActionId = x.ActionId,
                    TargetIdentity = x.TargetIdentity, Revision = x.Revision };
            }
            return copy;
        }
    }
}

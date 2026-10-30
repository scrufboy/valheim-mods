using System;
using System.Collections.Generic;

namespace LivingWorld.FormidableAI.Kernel
{
    [Flags]
    public enum DecisionDomain
    {
        None = 0,
        Perception = 1,
        Targeting = 2,
        Movement = 4,
        Facing = 8,
        Defense = 16,
        Equipment = 32,
        Attack = 64,
        Support = 128,
        All = Perception | Targeting | Movement | Facing | Defense | Equipment | Attack | Support
    }

    public sealed class CombatProposal
    {
        public string ActionId { get; private set; }
        public DecisionDomain Domains { get; private set; }
        public int Priority { get; private set; }
        public string TargetIdentity { get; private set; }
        public float ValidUntil { get; private set; }

        public CombatProposal(string actionId, DecisionDomain domains, int priority,
            string targetIdentity, float validUntil)
        {
            ActionId = actionId;
            Domains = domains;
            Priority = priority;
            TargetIdentity = targetIdentity;
            ValidUntil = validUntil;
        }
    }

    public interface IFormidableCombatModule
    {
        string Id { get; }
        IEnumerable<CombatProposal> Evaluate(CombatFrame frame);
    }

    public interface IBufferedFormidableCombatModule : IFormidableCombatModule
    {
        void Collect(CombatFrame frame, IList<CombatProposal> output);
    }

    public sealed class DomainLease
    {
        public DecisionDomain Domain { get; internal set; }
        public string OwnerId { get; internal set; }
        public string ActionId { get; internal set; }
        public string TargetIdentity { get; internal set; }
        public long Revision { get; internal set; }
        public bool IsNativeFallback { get { return OwnerId == CombatArbitrator.NativeOwnerId; } }
    }

    public sealed class ModuleFault
    {
        public string ModuleId { get; internal set; }
        public string ErrorType { get; internal set; }
        public string Message { get; internal set; }
    }

    public sealed class ArbitrationResult
    {
        private readonly DomainLease[] _leases;
        private readonly ModuleFault[] _faults;

        internal ArbitrationResult(long sequence, DomainLease[] leases, ModuleFault[] faults)
        {
            Sequence = sequence;
            _leases = leases;
            _faults = faults;
        }

        public long Sequence { get; private set; }
        public int LeaseCount { get { return _leases.Length; } }
        public int FaultCount { get { return _faults.Length; } }
        public DomainLease GetLease(int index) { return _leases[index]; }
        public ModuleFault GetFault(int index) { return _faults[index]; }

        public DomainLease LeaseFor(DecisionDomain domain)
        {
            for (int i = 0; i < _leases.Length; i++) if (_leases[i].Domain == domain) return _leases[i];
            return null;
        }
    }

    public sealed class ArbitrationBuffer
    {
        private readonly DomainLease[] _leases = new DomainLease[8];
        private ModuleFault[] _faults = new ModuleFault[2];
        private int _faultCount;

        public ArbitrationBuffer()
        {
            for (int i = 0; i < _leases.Length; i++) _leases[i] = new DomainLease();
            for (int i = 0; i < _faults.Length; i++) _faults[i] = new ModuleFault();
        }

        public long Sequence { get; internal set; }
        public int LeaseCount { get { return _leases.Length; } }
        public int FaultCount { get { return _faultCount; } }
        public DomainLease GetLease(int index) { return _leases[index]; }
        public ModuleFault GetFault(int index)
        {
            if (index < 0 || index >= _faultCount) throw new ArgumentOutOfRangeException("index");
            return _faults[index];
        }

        public DomainLease LeaseFor(DecisionDomain domain)
        {
            for (int i = 0; i < _leases.Length; i++) if (_leases[i].Domain == domain) return _leases[i];
            return null;
        }

        internal void Reset(long sequence) { Sequence = sequence; _faultCount = 0; }

        internal void SetLease(int index, DomainLease source)
        {
            DomainLease target = _leases[index];
            target.Domain = source.Domain;
            target.OwnerId = source.OwnerId;
            target.ActionId = source.ActionId;
            target.TargetIdentity = source.TargetIdentity;
            target.Revision = source.Revision;
        }

        internal void AddFault(string moduleId, Exception error)
        {
            EnsureFaultCapacity(_faultCount + 1);
            ModuleFault fault = _faults[_faultCount++];
            fault.ModuleId = moduleId;
            fault.ErrorType = error.GetType().Name;
            fault.Message = error.Message;
        }

        internal ModuleFault[] CopyFaults()
        {
            ModuleFault[] copy = new ModuleFault[_faultCount];
            for (int i = 0; i < _faultCount; i++)
            {
                ModuleFault x = _faults[i];
                copy[i] = new ModuleFault { ModuleId = x.ModuleId, ErrorType = x.ErrorType, Message = x.Message };
            }
            return copy;
        }

        private void EnsureFaultCapacity(int required)
        {
            if (_faults.Length >= required) return;
            int oldLength = _faults.Length;
            Array.Resize(ref _faults, Math.Max(required, oldLength * 2));
            for (int i = oldLength; i < _faults.Length; i++) _faults[i] = new ModuleFault();
        }
    }
}

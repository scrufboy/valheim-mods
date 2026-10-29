using UnityEngine;

namespace LivingWorld.Core
{
    /// <summary>
    /// Shared per-tick view handed to every behavior. Senses are read-only facts;
    /// the blackboard is the only channel between behaviors.
    /// </summary>
    public sealed class NpcFrame
    {
        public NpcPhase Phase;
        public NpcContext Context = NpcContext.FieldCompanion;
        public NpcSenses Senses = new NpcSenses();
        public NpcBlackboard Board = new NpcBlackboard();
        public float Now;
        public float Dt;
        public float SecondsSinceEncounter = float.PositiveInfinity;
        public bool FallingBack;
        public Vector3 FallBackFrom;      // position of the nearest engaged enemy when last sensed
    }

    public sealed class NpcSenses
    {
        public Player Leader;
        public bool HasLeader;
        public float LeaderDistance = -1f;
        public float LeaderSpeed;
        public bool LeaderMoving;
        public bool LeaderRunning;
        public bool LeaderCrouching;
        public bool LeaderWalkToggle;   // 1.0.32.10: the leader's own walk toggle
        public bool LeaderSitting;
        public bool LeaderArmed;
        public bool LeaderBusy;
        public Character Target;
        public bool HasStaticTarget;
        public bool NativeAlert;
        public float SecondsSinceDamaged = float.PositiveInfinity;
        public Character LastAttacker;
        public Character NearestAlertedHostile;
        public float NearestAlertedHostileDistance = float.PositiveInfinity;
        public float NearestPartyDistance = -1f;
        public bool Dark;
        public bool NearBuiltLight;
        public float NearestBuiltLight = float.PositiveInfinity;
        public bool Swimming;
        public float OwnSpeed;
    }

    /// <summary>Requests between behaviors. Writers own a domain; requesters only fill these.</summary>
    public sealed class NpcBlackboard
    {
        // Settled guard -> traversal: where to stand while the leader is stopped.
        public object SettledRequester;
        public Vector3 SettledDestination;
        public Vector3 SettledFollowAnchor;
        public bool SettledHold;

        // Traversal -> everyone: distance to the current formation destination.
        public float FormationDistance = -1f;
        // Traversal -> everyone (1.0.32.19): distance to the leased formation slot itself, even while a settled request holds.
        public float SlotDistance = -1f;
        public int FormationSlot = -1;

        // Hands -> diagnostics.
        public bool TorchLit;
        public bool TorchWanted;

        public void ClearSettled(object requester)
        {
            if (requester != null && SettledRequester != requester) return;
            SettledRequester = null;
            SettledHold = false;
        }
    }

    /// <summary>
    /// One behavior module. It declares which domains it writes. The kernel calls
    /// Enter once when it is granted, Tick while it holds its domains, and Exit
    /// exactly once when it loses them — before the next owner's Enter runs.
    /// A behavior can only write native state through NpcBody, which checks ownership.
    /// </summary>
    public abstract class NpcBehavior
    {
        protected NpcBody Body { get; private set; }
        public abstract string Name { get; }
        public abstract NpcDomain Claims { get; }
        public string Status { get; protected internal set; }
        public bool Active { get; internal set; }

        internal void Attach(NpcBody body) { Body = body; Status = "idle"; }

        public abstract bool Wants(NpcFrame frame);
        public virtual void Enter(NpcFrame frame) { }
        public abstract void Tick(NpcFrame frame);
        public virtual void Exit(NpcFrame frame, string reason) { Status = "released: " + reason; }

        /// <summary>Per-frame visual work (e.g. look direction) for active behaviors.</summary>
        public virtual void LateFrame(NpcFrame frame) { }
    }
}

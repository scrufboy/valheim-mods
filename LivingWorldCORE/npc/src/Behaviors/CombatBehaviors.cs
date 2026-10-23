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

        public override string Name { get { return "combat"; } }
        public override NpcDomain Claims { get { return NpcDomain.All; } }

        public override bool Wants(NpcFrame f)
        {
            return f.Phase == NpcPhase.Combat || f.Phase == NpcPhase.CombatClear;
        }

        public override void Enter(NpcFrame f)
        {
            prepared = null;
            Body.RestorePace(this);
        }

        public override void Tick(NpcFrame f)
        {
            NpcSenses s = f.Senses;
            // Native leader tether: does not drive locomotion while MonsterAI has a
            // target, but gives it a deterministic return instead of idle wandering.
            Body.Follow(this, s.Leader == null ? null : s.Leader.gameObject);
            if (s.Target != null && s.Target != prepared)
            {
                // Valheim's own distance/attack-data selector, once per new target.
                Body.EquipBestWeaponFor(this, s.Target);
                prepared = s.Target;
            }
            Body.PublishObserved(this);
            Status = f.Phase == NpcPhase.CombatClear
                ? "verifying clear " + f.Senses.SecondsSinceDamaged.ToString("0.0") + "s since hit"
                : s.Target != null ? "native target " + s.Target.GetHoverName() : "native combat (" + f.Phase + ")";
        }

        public override void Exit(NpcFrame f, string reason)
        {
            prepared = null;
            Status = "released: " + reason;
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

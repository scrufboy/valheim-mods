// Independently authored LWC proposal module. No Unity movement/damage writer.
using System;
using System.Collections.Generic;
using LivingWorld.FormidableAI.Kernel;

namespace LivingWorld.FormidableAI.Swarm
{
    public sealed class DartFacts
    {
        // Executor supplies observed facts for THIS frame/actor/authorized target.
        public long Sequence;
        public string ActorIdentity, TargetIdentity;
        public bool WeaponReady, ClearShot, FirePermit, RecoveryComplete;
        public bool EscapeReachable, RegroupReachable, PackCommitted;
    }

    public interface IDartFactSource
    {
        DartFacts Observe(CombatFrame frame);
    }

    public sealed class DartSkirmisherModule : IFormidableCombatModule
    {
        readonly IDartFactSource source;
        readonly float minimumRange, maximumRange;
        public string Id { get { return "lwc.swarm.dart-skirmisher"; } }

        public DartSkirmisherModule(IDartFactSource source, float minimumRange, float maximumRange)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (!Finite(minimumRange) || !Finite(maximumRange) || minimumRange <= 0 || maximumRange <= minimumRange)
                throw new ArgumentException("Explicit positive ordered weapon ranges required.");
            this.source = source; this.minimumRange = minimumRange; this.maximumRange = maximumRange;
        }

        public IEnumerable<CombatProposal> Evaluate(CombatFrame frame)
        {
            if (frame == null || !frame.OwnerReady || !frame.CombatCommitted ||
                !Finite(frame.Time) || !Finite(frame.HealthFraction) || frame.HealthFraction <= 0 ||
                String.IsNullOrEmpty(frame.AuthorizedTargetIdentity) ||
                frame.ActorIdentity == frame.AuthorizedTargetIdentity) yield break;
            ThreatObservation target = null;
            for (int i = 0; i < frame.ThreatCount; i++)
                if (frame.GetThreat(i) != null && frame.GetThreat(i).Identity == frame.AuthorizedTargetIdentity)
                    target = frame.GetThreat(i);
            // No hidden-target tracking, target substitution or inferred shooting lane.
            if (target == null || !target.Visible || !Finite(target.Distance) || target.Distance < 0) yield break;
            DartFacts facts = source.Observe(frame);
            if (facts == null || facts.Sequence != frame.Sequence || facts.ActorIdentity != frame.ActorIdentity ||
                facts.TargetIdentity != target.Identity) yield break;
            string action = null;
            DecisionDomain domains = DecisionDomain.None;
            if (target.Distance < minimumRange && facts.EscapeReachable)
            {
                action = "lwc.dart.evade";
                domains = DecisionDomain.Movement;
            }
            else if (!facts.PackCommitted && facts.RegroupReachable)
            {
                action = "lwc.dart.regroup";
                domains = DecisionDomain.Movement;
            }
            else if (facts.PackCommitted && facts.WeaponReady && facts.ClearShot &&
                     facts.FirePermit && facts.RecoveryComplete &&
                     target.Distance >= minimumRange && target.Distance <= maximumRange)
            {
                action = "lwc.dart.fire";
                domains = DecisionDomain.Attack | DecisionDomain.Facing;
            }
            // Unknown routes, blocked lanes, no pack fire permit and cooldown yield
            // to the existing owner. Evaluating a proposal does NOT consume a shot.
            if (action != null) yield return new CombatProposal(action, domains, 200, target.Identity, frame.Time + .1f);
        }

        static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }
    }
}

using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionActivities
{
    public enum Membership { Companion, Settlement, FreeAgent, Dead }
    public enum LeaderActivity { Travelling, Standing, Busy, Sitting, Sleeping, Crouching }
    public enum ActivityKind { None, Observe, GuardPosition, Socialize, AmbientCraft, Snack, Drink, GroundSit, ChairSit, BedSleep }
    public enum ActivityPhase { None, AwaitingLease, Approaching, Performing }
    public enum Control { Passive, Remote, Follow, Defend, WaitForLeader, Settlement, FreeRoam, NativePassage, Begin, Continue, Perform, Complete }
    public enum Reason { Inactive, AuthorityLost, InvalidFacts, ClockReset, Threat, LeaderUnavailable, Travel,
        Passage, ExternalAction, OrderChanged, ContextChanged, QuietDelay, ContinuingIdle, Cooldown, NoCandidate,
        Selected, WaitingForLease, LeaseTimeout, LeaseLost, TargetUnavailable, PathFailed,
        ApproachTimeout, Approaching, Arrived, Performing, Extended, Completed }
    [Flags]
    public enum Capabilities { None = 0, Observe = 1, Guard = 2, Social = 4, AmbientCraft = 8,
        Snack = 16, Drink = 32, Sit = 64, Sleep = 128, All = 255 }

    public struct ActivityToken : IEquatable<ActivityToken>
    {
        public long Session, Sequence;
        public bool IsEmpty { get { return Session == 0 || Sequence == 0; } }
        public bool Equals(ActivityToken other) { return Session == other.Session && Sequence == other.Sequence; }
        public override bool Equals(object other) { return other is ActivityToken && Equals((ActivityToken)other); }
        public override int GetHashCode() { return Session.GetHashCode() ^ Sequence.GetHashCode(); }
    }

    // Read-only facts supplied by an authority-aware Unity adapter. No scene scans,
    // steering, inventory changes, spawning, player controller or time skipping here.
    public struct Context
    {
        public double Now, CandidateSnapshotAt;
        public bool IsOwner, Ready, Alive, Threat, LeaderPresent, LeaderAlive, LeaderMoving;
        public bool PassageActive, ExternalActionBusy;
        public Membership Membership;
        public LeaderActivity LeaderActivity;
        public string LeaderId, InterestGroup;
        public long OrderRevision;
        public Capabilities Capabilities;
    }

    public struct Opportunity
    {
        public string Id, InterestGroup;
        public ActivityKind Kind;
        public bool Safe, Reachable, Permitted, Available;
        public double LeaderDistance, ActorDistance, Duration, CompletionCooldown;
        public float X, Y, Z, Yaw;
    }

    public struct Feedback
    {
        public ActivityToken Token;
        public bool LeaseHeld, TargetValid, Arrived, Finished, PathFailed;
    }

    public struct Decision
    {
        public Control Control;
        public Reason Reason;
        public ActivityPhase Phase;
        public ActivityToken Token, ReleasedToken;
        public Opportunity Opportunity;
        public int CandidatesExamined;
        // Ambient crafting is deliberately pantomime. This engine never authorizes
        // manufacture, consumption, repair, pickup, or dropping an inventory item.
        public bool AllowsInventoryMutation { get { return false; } }
    }

    public sealed class ActivitySettings
    {
        public double QuietDelay = 5, BusyQuietDelay = 1.5, ScanInterval = 3, Cooldown = 15, Radius = 12;
        public double SnapshotLifetime = 3, LeaseTimeout = 2, ApproachTimeout = 14;
        public double MinimumDwell = 15, MaximumDwell = 120, RecentWindow = 180;
        public double PassiveInterval = 30, FailureRetry = 3;
        public int StandingPauseCycles, BusyPauseCycles, MaximumContinuityExtensions;
        public int CandidateBudget = 24;

        internal ActivitySettings Copy()
        {
            var copy = (ActivitySettings)MemberwiseClone();
            foreach (double value in new[] { QuietDelay, BusyQuietDelay, ScanInterval, Cooldown, Radius,
                SnapshotLifetime, LeaseTimeout, ApproachTimeout, MinimumDwell, MaximumDwell, RecentWindow, PassiveInterval, FailureRetry })
                if (!Scheduler.Finite(value) || value <= 0 || value > 600) throw new ArgumentException("Invalid activity limit.");
            if (MinimumDwell > MaximumDwell || CandidateBudget < 1 || CandidateBudget > 64 ||
                StandingPauseCycles < 0 || StandingPauseCycles > 8 || BusyPauseCycles < 0 || BusyPauseCycles > 8 ||
                MaximumContinuityExtensions < 0 || MaximumContinuityExtensions > 4)
                throw new ArgumentException("Invalid dwell or candidate budget.");
            return copy;
        }
    }

    public sealed class Scheduler
    {
        readonly ActivitySettings settings;
        readonly long session;
        readonly string[] recentIds = new string[16];
        readonly double[] recentAt = new double[16];
        int recentCursor, candidateCursor, passiveCycles, continuityExtensions;
        long sequence;
        ActivityToken token;
        Opportunity current;
        ActivityPhase phase;
        double lastNow = double.NaN, quietSince, nextDecision, committedAt, startedAt;
        bool observedContext;
        string leaderId, interestGroup;
        long orderRevision;
        LeaderActivity leaderActivity;

        public Scheduler(long sessionNonce, ActivitySettings settings)
        {
            if (sessionNonce <= 0) throw new ArgumentException("A fresh positive session nonce is required.");
            if (settings == null) throw new ArgumentNullException("settings");
            session = sessionNonce; this.settings = settings.Copy();
        }

        public ActivityToken CurrentToken { get { return token; } }
        public ActivityPhase Phase { get { return phase; } }
        public double NextDecisionAt { get { return nextDecision; } }
        internal static bool Finite(double value) { return !double.IsNaN(value) && !double.IsInfinity(value); }

        public Decision Tick(Context facts, IList<Opportunity> nearby, Feedback feedback)
        {
            if (!facts.IsOwner) return Reset(Control.Remote, Reason.AuthorityLost);
            if (!Finite(facts.Now) || facts.Now < 0 || facts.Membership < Membership.Companion || facts.Membership > Membership.Dead ||
                facts.LeaderActivity < LeaderActivity.Travelling || facts.LeaderActivity > LeaderActivity.Crouching)
                return Reset(Control.Passive, Reason.InvalidFacts);
            if (Finite(lastNow) && facts.Now < lastNow)
            {
                lastNow = facts.Now; nextDecision = facts.Now;
                Array.Clear(recentIds, 0, recentIds.Length);
                return Reset(Control.Passive, Reason.ClockReset);
            }
            lastNow = facts.Now;
            if (!facts.Ready || !facts.Alive || facts.Membership == Membership.Dead)
                return Reset(Control.Passive, Reason.Inactive);
            if (facts.Threat) return Reset(Control.Defend, Reason.Threat);
            if (facts.Membership != Membership.Companion)
                return Reset(facts.Membership == Membership.Settlement ? Control.Settlement : Control.FreeRoam, Reason.OrderChanged);
            if (!facts.LeaderPresent || !facts.LeaderAlive || string.IsNullOrEmpty(facts.LeaderId))
                return Reset(Control.WaitForLeader, Reason.LeaderUnavailable);
            if (facts.PassageActive) return Reset(Control.NativePassage, Reason.Passage);
            if (facts.ExternalActionBusy) return Reset(Control.Passive, Reason.ExternalAction);
            if (facts.LeaderMoving || facts.LeaderActivity == LeaderActivity.Travelling || facts.LeaderActivity == LeaderActivity.Crouching)
                return Reset(Control.Follow, Reason.Travel);

            bool orderChanged = observedContext && (leaderId != facts.LeaderId || orderRevision != facts.OrderRevision);
            bool contextChanged = observedContext && (leaderActivity != facts.LeaderActivity || interestGroup != facts.InterestGroup);
            if (!observedContext || orderChanged || contextChanged)
            {
                var released = ClearActivity();
                observedContext = true; leaderId = facts.LeaderId; orderRevision = facts.OrderRevision;
                leaderActivity = facts.LeaderActivity; interestGroup = facts.InterestGroup;
                quietSince = facts.Now; nextDecision = facts.Now + RequiredQuietDelay(facts.LeaderActivity);
                passiveCycles = 0;
                return new Decision { Control = Control.Follow, ReleasedToken = released,
                    Reason = orderChanged ? Reason.OrderChanged : contextChanged ? Reason.ContextChanged : Reason.QuietDelay };
            }

            if (!token.IsEmpty)
            {
                // No rescoring while committed. Incoming snapshots cannot move the
                // destination or turn a late acknowledgement into a different job.
                if (!Allowed(facts.LeaderActivity, current.Kind, facts.Capabilities))
                    return Cancel(facts.Now, Reason.ContextChanged, Control.Follow);
                // A late matching acknowledgement must not revive an expired
                // reservation. Deadlines win over arrival/ack on the same tick.
                if (phase == ActivityPhase.AwaitingLease && facts.Now - committedAt >= settings.LeaseTimeout)
                    return Cancel(facts.Now, Reason.LeaseTimeout, Control.Follow, true);
                if (!feedback.Token.Equals(token))
                {
                    if (facts.Now - committedAt >= settings.LeaseTimeout)
                        return Cancel(facts.Now, Reason.LeaseTimeout, Control.Follow, true);
                    return Output(Control.Continue, Reason.WaitingForLease, 0);
                }
                if (!feedback.LeaseHeld) return Cancel(facts.Now, Reason.LeaseLost, Control.Follow, true);
                if (!feedback.TargetValid) return Cancel(facts.Now, Reason.TargetUnavailable, Control.Follow, true);
                if (feedback.PathFailed) return Cancel(facts.Now, Reason.PathFailed, Control.Follow, true);
                if (phase == ActivityPhase.AwaitingLease) phase = ActivityPhase.Approaching;
                if (phase == ActivityPhase.Approaching)
                {
                    if (facts.Now - committedAt >= settings.ApproachTimeout)
                        return Cancel(facts.Now, Reason.ApproachTimeout, Control.Follow, true);
                    if (feedback.Arrived)
                    { phase = ActivityPhase.Performing; startedAt = facts.Now; return Output(Control.Perform, Reason.Arrived, 0); }
                    return Output(Control.Continue, Reason.Approaching, 0);
                }
                bool resting = facts.LeaderActivity == LeaderActivity.Sitting || facts.LeaderActivity == LeaderActivity.Sleeping;
                if (!feedback.Finished && !resting && facts.Now - startedAt >= current.Duration &&
                    continuityExtensions < settings.MaximumContinuityExtensions)
                {
                    continuityExtensions++; startedAt = facts.Now;
                    return Output(Control.Continue, Reason.Extended, 0);
                }
                if (feedback.Finished || (!resting && facts.Now - startedAt >= current.Duration))
                    return Cancel(facts.Now, Reason.Completed, Control.Complete);
                return Output(Control.Continue, Reason.Performing, 0);
            }

            if (facts.Now - quietSince < RequiredQuietDelay(facts.LeaderActivity))
                return new Decision { Control = Control.Follow, Reason = Reason.QuietDelay };
            if (facts.Now < nextDecision) return new Decision { Control = Control.Follow, Reason = Reason.Cooldown };
            int pauseTarget = facts.LeaderActivity == LeaderActivity.Standing ? settings.StandingPauseCycles :
                facts.LeaderActivity == LeaderActivity.Busy ? settings.BusyPauseCycles : 0;
            if (passiveCycles < pauseTarget)
            {
                passiveCycles++;
                // Remaining nearby and attentive is an intentional decision,
                // not a fabricated zero-distance movement activity.
                nextDecision = facts.Now + settings.PassiveInterval;
                return new Decision { Control = Control.Follow, Reason = Reason.ContinuingIdle };
            }
            nextDecision = facts.Now + settings.ScanInterval;
            if (!Finite(facts.CandidateSnapshotAt) || facts.CandidateSnapshotAt > facts.Now ||
                facts.Now - facts.CandidateSnapshotAt > settings.SnapshotLifetime || nearby == null)
                return new Decision { Control = Control.Follow, Reason = Reason.NoCandidate };

            Opportunity best = new Opportunity(); double bestScore = double.NegativeInfinity;
            int examined = Math.Min(settings.CandidateBudget, nearby.Count);
            // Rotate through larger cached snapshots. Keep the per-scan budget,
            // but do not permanently starve candidates beyond the first page.
            if (candidateCursor >= nearby.Count) candidateCursor = 0;
            for (int i = 0; i < examined; i++)
            {
                Opportunity candidate = nearby[candidateCursor];
                if (++candidateCursor == nearby.Count) candidateCursor = 0;
                if (!Valid(candidate) || !Allowed(facts.LeaderActivity, candidate.Kind, facts.Capabilities)) continue;
                double score = BaseScore(candidate.Kind) - candidate.LeaderDistance - candidate.ActorDistance * .2;
                // Context changes preference, not permission. Standing favors
                // local observation; a leader actually using this station
                // makes shared work more relevant. Generic menu use does not.
                if (facts.LeaderActivity == LeaderActivity.Standing)
                {
                    // Security is the first settled response, observation is the
                    // second, and useful village pantomime gets a regular turn.
                    // This is a continuity rotation, not a random job scramble.
                    int turn = (int)(sequence % 3);
                    if (turn == 0) score += candidate.Kind == ActivityKind.GuardPosition ? 70 :
                        candidate.Kind == ActivityKind.Observe ? 35 : candidate.Kind == ActivityKind.AmbientCraft ? -40 : 0;
                    else if (turn == 1) score += candidate.Kind == ActivityKind.Observe ? 50 :
                        candidate.Kind == ActivityKind.GuardPosition ? 15 : candidate.Kind == ActivityKind.AmbientCraft ? -20 : 0;
                    else score += candidate.Kind == ActivityKind.AmbientCraft ? 40 :
                        candidate.Kind == ActivityKind.GuardPosition ? 10 : 0;
                }
                else if (facts.LeaderActivity == LeaderActivity.Busy && string.IsNullOrEmpty(facts.InterestGroup))
                    score += candidate.Kind == ActivityKind.Observe ? 10 : candidate.Kind == ActivityKind.AmbientCraft ? -10 : 0;
                if (!string.IsNullOrEmpty(facts.InterestGroup) && candidate.InterestGroup == facts.InterestGroup) score += 80;
                for (int recent = 0; recent < recentIds.Length; recent++)
                    if (recentIds[recent] == candidate.Id && facts.Now - recentAt[recent] < settings.RecentWindow)
                    { score -= 100; break; }
                if (score > bestScore || (score == bestScore && string.CompareOrdinal(candidate.Id, best.Id) < 0))
                { best = candidate; bestScore = score; }
            }
            if (best.Kind == ActivityKind.None)
                return new Decision { Control = Control.Follow, Reason = Reason.NoCandidate, CandidatesExamined = examined };
            if (sequence == long.MaxValue) return Reset(Control.Passive, Reason.InvalidFacts);
            token = new ActivityToken { Session = session, Sequence = ++sequence };
            current = best; current.Duration = Math.Max(settings.MinimumDwell, Math.Min(settings.MaximumDwell, best.Duration));
            committedAt = facts.Now; phase = ActivityPhase.AwaitingLease; passiveCycles = 0; continuityExtensions = 0;
            return Output(Control.Begin, Reason.Selected, examined);
        }

        double RequiredQuietDelay(LeaderActivity activity)
        { return activity == LeaderActivity.Busy ? Math.Min(settings.QuietDelay, settings.BusyQuietDelay) : settings.QuietDelay; }

        bool Valid(Opportunity candidate)
        {
            return !string.IsNullOrEmpty(candidate.Id) && candidate.Id.Length <= 128 &&
                candidate.Safe && candidate.Reachable && candidate.Permitted && candidate.Available &&
                Finite(candidate.LeaderDistance) && candidate.LeaderDistance >= 0 && candidate.LeaderDistance <= settings.Radius &&
                Finite(candidate.ActorDistance) && candidate.ActorDistance >= 0 && candidate.ActorDistance <= settings.Radius * 2 &&
                Finite(candidate.X) && Finite(candidate.Y) && Finite(candidate.Z) && Finite(candidate.Yaw) &&
                Finite(candidate.Duration) && candidate.Duration > 0 &&
                Finite(candidate.CompletionCooldown) && candidate.CompletionCooldown >= 0 && candidate.CompletionCooldown <= 600;
        }

        static double BaseScore(ActivityKind kind)
        {
            switch (kind)
            {
                case ActivityKind.BedSleep: return 150;
                case ActivityKind.ChairSit: return 100;
                case ActivityKind.GroundSit: return 70;
                case ActivityKind.AmbientCraft: return 90;
                case ActivityKind.Socialize: return 80;
                case ActivityKind.Snack: case ActivityKind.Drink: return 65;
                case ActivityKind.GuardPosition: return 60;
                case ActivityKind.Observe: return 30;
                default: return double.NegativeInfinity;
            }
        }

        public static bool Allowed(LeaderActivity leader, ActivityKind kind, Capabilities capabilities)
        {
            Capabilities required;
            switch (kind)
            {
                case ActivityKind.Observe: required = Capabilities.Observe; break;
                case ActivityKind.GuardPosition: required = Capabilities.Guard; break;
                case ActivityKind.Socialize: required = Capabilities.Social; break;
                case ActivityKind.AmbientCraft: required = Capabilities.AmbientCraft; break;
                case ActivityKind.Snack: required = Capabilities.Snack; break;
                case ActivityKind.Drink: required = Capabilities.Drink; break;
                case ActivityKind.GroundSit: case ActivityKind.ChairSit: required = Capabilities.Sit; break;
                case ActivityKind.BedSleep: required = Capabilities.Sleep; break;
                default: return false;
            }
            if ((capabilities & required) == 0) return false;
            switch (leader)
            {
                case LeaderActivity.Standing: return kind == ActivityKind.Observe || kind == ActivityKind.GuardPosition || kind == ActivityKind.AmbientCraft;
                case LeaderActivity.Busy: return kind != ActivityKind.GroundSit && kind != ActivityKind.BedSleep;
                case LeaderActivity.Sitting: return kind == ActivityKind.GroundSit || kind == ActivityKind.ChairSit;
                case LeaderActivity.Sleeping: return kind == ActivityKind.BedSleep || kind == ActivityKind.GuardPosition;
                default: return false;
            }
        }

        Decision Output(Control control, Reason reason, int examined)
        { return new Decision { Control = control, Reason = reason, Token = token, Phase = phase, Opportunity = current, CandidatesExamined = examined }; }
        Decision Cancel(double now, Reason reason, Control control, bool failed = false)
        {
            recentIds[recentCursor] = current.Id; recentAt[recentCursor] = now;
            recentCursor = (recentCursor + 1) % recentIds.Length;
            double cooldown = failed ? settings.FailureRetry : current.CompletionCooldown > 0 ?
                current.CompletionCooldown : settings.Cooldown;
            ActivityToken released = ClearActivity(); nextDecision = now + cooldown;
            // A rejected destination is not a life choice. Retry a different
            // candidate promptly instead of forcing the whole idle prelude again.
            passiveCycles = failed ? Math.Max(settings.StandingPauseCycles, settings.BusyPauseCycles) : 0;
            continuityExtensions = 0;
            return new Decision { Control = control, Reason = reason, ReleasedToken = released };
        }
        ActivityToken ClearActivity()
        { var released = token; token = new ActivityToken(); current = new Opportunity(); phase = ActivityPhase.None; return released; }
        Decision Reset(Control control, Reason reason)
        {
            var released = ClearActivity(); observedContext = false;
            return new Decision { Control = control, Reason = reason, ReleasedToken = released };
        }
    }
}

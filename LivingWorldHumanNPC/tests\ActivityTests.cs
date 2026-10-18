using System;
using System.Collections.Generic;
using System.Diagnostics;
using LivingWorld.CompanionActivities;

static class ActivityTests
{
    static int passed;
    static void Check(bool value, string label) { if (!value) throw new Exception(label); passed++; }
    static Context Facts(double time, LeaderActivity activity)
    { return new Context { Now = time, CandidateSnapshotAt = time, IsOwner = true, Ready = true, Alive = true,
        Membership = Membership.Companion, LeaderId = "player-1", LeaderPresent = true, LeaderAlive = true,
        LeaderActivity = activity, Capabilities = Capabilities.All }; }
    static Opportunity Place(string id, ActivityKind kind)
    { return new Opportunity { Id = id, Kind = kind, Safe = true, Permitted = true, Reachable = true,
        Available = true, LeaderDistance = 3, ActorDistance = 4, Duration = 8, X = 3, Z = 4 }; }
    static Scheduler New() { return new Scheduler(123, new ActivitySettings()); }
    static Feedback Ack(Decision decision, bool arrived)
    { return new Feedback { Token = decision.Token, LeaseHeld = true, TargetValid = true, Arrived = arrived }; }
    static Decision Begin(Scheduler engine, LeaderActivity activity, Opportunity[] opportunities)
    { engine.Tick(Facts(0, activity), opportunities, new Feedback()); return engine.Tick(Facts(6, activity), opportunities, new Feedback()); }

    static void Main()
    {
        try
        {
            var all = new List<Opportunity>();
            foreach (ActivityKind kind in Enum.GetValues(typeof(ActivityKind))) all.Add(Place(kind.ToString(), kind));
            foreach (LeaderActivity leader in Enum.GetValues(typeof(LeaderActivity)))
            foreach (ActivityKind kind in Enum.GetValues(typeof(ActivityKind)))
            {
                Check(!Scheduler.Allowed(leader, kind, Capabilities.None), "capability is required");
                if (leader == LeaderActivity.Travelling || leader == LeaderActivity.Crouching)
                    Check(!Scheduler.Allowed(leader, kind, Capabilities.All), "travel/crouch never ambient");
            }
            Check(!Scheduler.Allowed(LeaderActivity.Standing, ActivityKind.ChairSit, Capabilities.All), "standing idle is not sitting");
            Check(Scheduler.Allowed(LeaderActivity.Busy, ActivityKind.ChairSit, Capabilities.All), "busy-player ambient chair allowed by latest request");
            Check(Scheduler.Allowed(LeaderActivity.Busy, ActivityKind.AmbientCraft, Capabilities.All), "pretend crafting allowed");
            Check(Scheduler.Allowed(LeaderActivity.Standing, ActivityKind.AmbientCraft, Capabilities.All), "idle leader permits autonomous station work");
            Check(!Scheduler.Allowed(LeaderActivity.Busy, ActivityKind.BedSleep, Capabilities.All), "menu does not mean sleep");
            var accelerated = New(); var acceleratedForge = Place("accelerated-forge", ActivityKind.AmbientCraft);
            accelerated.Tick(Facts(0, LeaderActivity.Busy), new[] { acceleratedForge }, new Feedback());
            Check(accelerated.Tick(Facts(2, LeaderActivity.Busy), new[] { acceleratedForge }, new Feedback()).Control == Control.Begin,
                "busy leader accelerates companion activity handoff");
            var ordinary = New(); ordinary.Tick(Facts(0, LeaderActivity.Standing), new[] { acceleratedForge }, new Feedback());
            Check(ordinary.Tick(Facts(2, LeaderActivity.Standing), new[] { acceleratedForge }, new Feedback()).Reason == Reason.QuietDelay,
                "ordinary standing retains its distinct quiet delay");
            var engine = New(); var forge = Place("forge", ActivityKind.AmbientCraft);
            var decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
            Check(decision.Control == Control.Begin && decision.Phase == ActivityPhase.AwaitingLease, "begin reserves before acting");
            Check(!decision.AllowsInventoryMutation, "ambient engine cannot produce/consume items");
            Check(decision.Opportunity.Id == "forge", "ordinary station requires no maypole, village tag, or interest group");
            var firstToken = decision.Token;
            var next = engine.Tick(Facts(6.5, LeaderActivity.Busy), all, new Feedback());
            Check(next.Reason == Reason.WaitingForLease, "no duplicate begin before ack");
            next = engine.Tick(Facts(7, LeaderActivity.Busy), all, Ack(decision, false));
            Check(next.Opportunity.Id == "forge" && next.Phase == ActivityPhase.Approaching && next.CandidatesExamined == 0, "committed choice does not rescore");
            next = engine.Tick(Facts(8, LeaderActivity.Busy), all, Ack(decision, true));
            Check(next.Control == Control.Perform, "animation only after arrival");
            next = engine.Tick(Facts(9, LeaderActivity.Busy), all, Ack(decision, true));
            Check(next.Control == Control.Continue && next.Reason == Reason.Performing, "perform trigger is one-shot");
            next = engine.Tick(Facts(24, LeaderActivity.Busy), all, Ack(decision, true));
            Check(next.Control == Control.Complete && next.ReleasedToken.Equals(firstToken), "completion releases lease");
            next = engine.Tick(Facts(25, LeaderActivity.Busy), all, new Feedback());
            Check(next.Reason == Reason.Cooldown, "dwell then cooldown");

            var lifeSettings = new ActivitySettings { StandingPauseCycles = 2, BusyPauseCycles = 1,
                PassiveInterval = 30, MaximumContinuityExtensions = 1 };
            engine = new Scheduler(125, lifeSettings);
            engine.Tick(Facts(0, LeaderActivity.Standing), new[] { forge }, new Feedback());
            next = engine.Tick(Facts(5, LeaderActivity.Standing), new[] { forge }, new Feedback());
            Check(next.Reason == Reason.ContinuingIdle && engine.CurrentToken.IsEmpty, "first standing window intentionally remains local");
            next = engine.Tick(Facts(35, LeaderActivity.Standing), new[] { forge }, new Feedback());
            Check(next.Reason == Reason.ContinuingIdle && engine.CurrentToken.IsEmpty, "second standing window may continue without fake movement");
            next = engine.Tick(Facts(65, LeaderActivity.Standing), new[] { forge }, new Feedback());
            Check(next.Control == Control.Begin, "activity becomes an option only after continuity windows");
            decision = next;
            engine.Tick(Facts(66, LeaderActivity.Standing), all, Ack(decision, true));
            next = engine.Tick(Facts(81, LeaderActivity.Standing), all, Ack(decision, true));
            Check(next.Reason == Reason.Extended && next.Control == Control.Continue, "unchanged safe activity extends instead of forced departure");
            next = engine.Tick(Facts(96, LeaderActivity.Standing), all, Ack(decision, true));
            Check(next.Control == Control.Complete, "bounded extension eventually reevaluates");

            foreach (string interrupt in new[] { "threat", "moving", "missing", "deadleader", "remote", "door", "external", "order", "settle", "free", "deadnpc", "notready" })
            {
                engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
                var f = Facts(7, LeaderActivity.Busy);
                switch (interrupt)
                {
                    case "threat": f.Threat = true; break; case "moving": f.LeaderMoving = true; break;
                    case "missing": f.LeaderPresent = false; break; case "deadleader": f.LeaderAlive = false; break;
                    case "remote": f.IsOwner = false; break; case "door": f.PassageActive = true; break;
                    case "external": f.ExternalActionBusy = true; break; case "order": f.OrderRevision++; break;
                    case "settle": f.Membership = Membership.Settlement; break; case "free": f.Membership = Membership.FreeAgent; break;
                    case "deadnpc": f.Alive = false; break; case "notready": f.Ready = false; break;
                }
                next = engine.Tick(f, all, Ack(decision, true));
                Check(next.ReleasedToken.Equals(decision.Token) && engine.CurrentToken.IsEmpty, "interrupt releases: " + interrupt);
                Check(next.Control != Control.Perform && next.Control != Control.Begin && next.Control != Control.Continue, "no competing activity: " + interrupt);
            }

            foreach (string failure in new[] { "lease", "target", "path", "acktimeout", "approachtimeout", "clock", "nan", "capability" })
            {
                engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
                var f = Facts(7, LeaderActivity.Busy); var feedback = Ack(decision, false);
                switch (failure)
                {
                    case "lease": feedback.LeaseHeld = false; break; case "target": feedback.TargetValid = false; break;
                    case "path": feedback.PathFailed = true; break; case "acktimeout": f.Now = 8; feedback.Token = new ActivityToken(); break;
                    case "approachtimeout": f.Now = 20; break; case "clock": f.Now = 5; break;
                    case "nan": f.Now = double.NaN; break; case "capability": f.Capabilities = Capabilities.Observe; break;
                }
                next = engine.Tick(f, all, feedback);
                Check(next.ReleasedToken.Equals(decision.Token) && engine.CurrentToken.IsEmpty, "failure clears: " + failure);
            }

            foreach (string invalid in new[] { "unsafe", "unreachable", "denied", "occupied", "far", "faractor", "nan", "duration", "cooldown", "cooldownnan", "coordinate", "id" })
            {
                var candidate = forge;
                switch (invalid)
                {
                    case "unsafe": candidate.Safe = false; break; case "unreachable": candidate.Reachable = false; break;
                    case "denied": candidate.Permitted = false; break; case "occupied": candidate.Available = false; break;
                    case "far": candidate.LeaderDistance = 13; break; case "faractor": candidate.ActorDistance = 25; break;
                    case "nan": candidate.LeaderDistance = double.NaN; break; case "duration": candidate.Duration = 0; break;
                    case "cooldown": candidate.CompletionCooldown = -1; break; case "cooldownnan": candidate.CompletionCooldown = double.NaN; break;
                    case "coordinate": candidate.X = float.PositiveInfinity; break; case "id": candidate.Id = null; break;
                }
                engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { candidate });
                Check(decision.Control != Control.Begin, "reject candidate: " + invalid);
            }
            engine = New(); engine.Tick(Facts(0, LeaderActivity.Busy), all, new Feedback());
            var stale = Facts(6, LeaderActivity.Busy); stale.CandidateSnapshotAt = 0;
            Check(engine.Tick(stale, all, new Feedback()).Control != Control.Begin, "stale scene snapshot cannot start activity");
            stale.Now = 8; stale.CandidateSnapshotAt = 9;
            Check(engine.Tick(stale, all, new Feedback()).Control != Control.Begin, "future snapshot rejected");
            var huge = new List<Opportunity>();
            for (int i = 0; i < 1000; i++) huge.Add(Place("chair" + i, ActivityKind.ChairSit));
            engine = New(); engine.Tick(Facts(0, LeaderActivity.Busy), huge, new Feedback());
            decision = engine.Tick(Facts(6, LeaderActivity.Busy), huge, new Feedback());
            Check(decision.CandidatesExamined == 24, "bounded selection on huge candidate list");

            // Regression: matching acknowledgements/arrivals cannot revive an
            // expired activity; absence of an acknowledgement is not required.
            foreach (double late in new[] { 8.0, 9.0 })
            {
                engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
                next = engine.Tick(Facts(late, LeaderActivity.Busy), all, Ack(decision, true));
                Check(next.Reason == Reason.LeaseTimeout && next.ReleasedToken.Equals(decision.Token), "late matching ack expires at " + late);
            }
            foreach (double late in new[] { 20.0, 21.0 })
            {
                engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
                engine.Tick(Facts(7, LeaderActivity.Busy), all, Ack(decision, false));
                next = engine.Tick(Facts(late, LeaderActivity.Busy), all, Ack(decision, true));
                Check(next.Reason == Reason.ApproachTimeout && next.ReleasedToken.Equals(decision.Token), "late arrival expires at " + late);
            }
            var blocked = new List<Opportunity>();
            for (int i = 0; i < 24; i++) { var unavailable = forge; unavailable.Available = false; blocked.Add(unavailable); }
            blocked.Add(forge);
            engine = New(); engine.Tick(Facts(0, LeaderActivity.Busy), blocked, new Feedback());
            next = engine.Tick(Facts(6, LeaderActivity.Busy), blocked, new Feedback());
            Check(next.Control != Control.Begin && next.CandidatesExamined == 24, "first unavailable page remains bounded");
            next = engine.Tick(Facts(9, LeaderActivity.Busy), blocked, new Feedback());
            Check(next.Control == Control.Begin && next.Opportunity.Id == forge.Id && next.CandidatesExamined == 24, "later candidate is not starved");
            engine = New(); engine.Tick(Facts(0, LeaderActivity.Busy), blocked, new Feedback());
            engine.Tick(Facts(6, LeaderActivity.Busy), blocked, new Feedback());
            next = engine.Tick(Facts(9, LeaderActivity.Busy), new Opportunity[0], new Feedback());
            Check(next.CandidatesExamined == 0 && next.Control != Control.Begin, "empty snapshot resets cursor safely");
            next = engine.Tick(Facts(12, LeaderActivity.Busy), new[] { forge }, new Feedback());
            Check(next.Control == Control.Begin && next.CandidatesExamined == 1, "shrinking snapshots remain safe");

            var floor = Place("floor", ActivityKind.GroundSit); var seat = Place("seat", ActivityKind.ChairSit);
            engine = New(); decision = Begin(engine, LeaderActivity.Sitting, new[] { floor, seat });
            Check(decision.Opportunity.Id == "seat", "rest prefers a free chair");
            next = engine.Tick(Facts(7, LeaderActivity.Sitting), all, Ack(decision, true));
            next = engine.Tick(Facts(120, LeaderActivity.Sitting), all, Ack(decision, true));
            Check(next.Reason == Reason.Performing, "rest does not hop chairs on dwell expiry");
            next = engine.Tick(Facts(121, LeaderActivity.Standing), all, Ack(decision, true));
            Check(next.ReleasedToken.Equals(decision.Token), "stand ends rest");
            seat.Available = false; engine = New(); decision = Begin(engine, LeaderActivity.Sitting, new[] { floor, seat });
            Check(decision.Opportunity.Id == "floor", "occupied chair falls back to ground");
            var bed = Place("bed", ActivityKind.BedSleep); var guard = Place("guard", ActivityKind.GuardPosition);
            engine = New(); decision = Begin(engine, LeaderActivity.Sleeping, new[] { guard, bed });
            Check(decision.Opportunity.Id == "bed", "sleep selects permitted bed");
            bed.Permitted = false; engine = New(); decision = Begin(engine, LeaderActivity.Sleeping, new[] { guard, bed });
            Check(decision.Opportunity.Id == "guard", "no permitted bed means watch, not theft");
            engine = New(); decision = Begin(engine, LeaderActivity.Standing, all.ToArray());
            Check(decision.Opportunity.Kind == ActivityKind.GuardPosition, "standing idle first establishes outward security coverage");

            var coverage = Place("guard-sector", ActivityKind.GuardPosition); coverage.CompletionCooldown = 2.5;
            engine = New(); decision = Begin(engine, LeaderActivity.Standing, new[] { coverage });
            next = engine.Tick(Facts(7, LeaderActivity.Standing), all, Ack(decision, true));
            var resolved = Ack(decision, true); resolved.Finished = true;
            next = engine.Tick(Facts(8, LeaderActivity.Standing), all, resolved);
            Check(next.Control == Control.Complete, "resolved coverage completes without waiting for decorative dwell");
            next = engine.Tick(Facts(9, LeaderActivity.Standing), new[] { coverage }, new Feedback());
            Check(next.Reason == Reason.Cooldown, "coverage uses its explicit short completion cooldown");
            next = engine.Tick(Facts(11, LeaderActivity.Standing), new[] { coverage }, new Feedback());
            Check(next.Control == Control.Begin, "resolved guard can reposition after its bounded cooldown");

            engine = New(); decision = Begin(engine, LeaderActivity.Busy, new[] { forge });
            var replay = Ack(decision, true); replay.Token.Session++;
            Check(engine.Tick(Facts(7, LeaderActivity.Busy), all, replay).Reason == Reason.WaitingForLease, "other-session ack cannot execute");
            Check(engine.Tick(Facts(8, LeaderActivity.Busy), all, replay).Reason == Reason.LeaseTimeout, "wrong-session ack expires");
            var modifiedSettings = new ActivitySettings(); engine = new Scheduler(124, modifiedSettings); modifiedSettings.Radius = 999;
            var outside = forge; outside.LeaderDistance = 13;
            Check(Begin(engine, LeaderActivity.Busy, new[] { outside }).Control != Control.Begin, "caller cannot mutate engine limits");

            // Authority and emergency checks must run on every invocation, even
            // though candidate scoring itself is rate-limited.
            for (int flags = 0; flags < 64; flags++)
            {
                engine = New(); var f = Facts(0, LeaderActivity.Busy);
                f.IsOwner = (flags & 1) != 0; f.Alive = (flags & 2) != 0; f.Ready = (flags & 4) != 0;
                f.Threat = (flags & 8) != 0; f.LeaderMoving = (flags & 16) != 0; f.LeaderPresent = (flags & 32) != 0;
                engine.Tick(f, all, new Feedback()); f.Now = 6; f.CandidateSnapshotAt = 6;
                decision = engine.Tick(f, all, new Feedback());
                if (!f.IsOwner) Check(decision.Control == Control.Remote, "remote never authors action");
                else if (!f.Alive || !f.Ready) Check(decision.Control == Control.Passive, "notready/dead passive");
                else if (f.Threat) Check(decision.Control == Control.Defend, "threat preempts all optional actions");
                else if (!f.LeaderPresent) Check(decision.Control == Control.WaitForLeader, "missing leader is not dismissal");
                else if (f.LeaderMoving) Check(decision.Control == Control.Follow, "movement stops ambient behavior");
                else Check(decision.Control == Control.Begin, "eligible settled context can begin");
            }
            var watch = Stopwatch.StartNew(); engine = New();
            for (int i = 0; i < 100000; i++) engine.Tick(Facts(i * .02, LeaderActivity.Standing), null, new Feedback());
            watch.Stop();
            Console.WriteLine("ActivityPolicy PASS: " + passed + " assertions; 100000 policy ticks=" + watch.ElapsedMilliseconds + "ms. Unity/runtime NOT RUN.");
        }
        catch (Exception error) { Console.Error.WriteLine("ActivityPolicy FAIL: " + error.Message); Environment.Exit(1); }
    }
}

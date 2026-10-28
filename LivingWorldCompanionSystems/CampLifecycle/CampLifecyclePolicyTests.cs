using System;
using System.Collections.Generic;
using LivingWorld.CompanionSystems.CampLifecycle;

static class CampLifecyclePolicyTests
{
    static int checks;
    static void Check(bool pass, string name) { if (!pass) throw new Exception("FAIL: " + name); checks++; }
    static void Throws(Action action, string name) { try { action(); } catch (InvalidOperationException) { checks++; return; } throw new Exception("FAIL: " + name); }
    static CampPersonState Person() { return new CampPersonState { PersonId="astrid-01", CampId="camp-meadows-4", Life=PersonLife.Living, Presence=PersonPresence.AtCamp, SchemaVersion=1, LastSafeAnchor="fire-a" }; }
    static CampState Camp() { return new CampState { CampId="camp-meadows-4", Disposition=CampDisposition.Active, SchemaVersion=1 }; }
    static Observation Seen(long tick) { return new Observation { WorldTick=tick, ZoneLoaded=true, PlayerNear=true, AuthoritativeOwner=true, ActorExists=true, SafeAnchorAvailable=true, SafeAnchor="fire-a" }; }

    public static int Main()
    {
        var p=Person(); var c=Camp();
        var a=CampLifecyclePolicy.Observe(p,c,Seen(1200)); var b=CampLifecyclePolicy.Observe(p,c,Seen(1200));
        Check(a.Directive==RuntimeDirective.RunRoutine && a.Owner==ActionOwner.CampRoutine, "loaded camp runs peaceful routine");
        Check(a.State.Routine==b.State.Routine && a.State.RoutineEpoch==2, "routine is deterministic");
        Check(CampLifecyclePolicy.RoutineFor(p.CampId,p.PersonId,2)!=CampLifecyclePolicy.RoutineFor(p.CampId,p.PersonId,3), "epochs rotate work");
        var all=new HashSet<CampRoutine>(); for(int i=0;i<5;i++) all.Add(CampLifecyclePolicy.RoutineFor(p.CampId,p.PersonId,i));
        Check(all.Count==5, "watch cook rest repair and patrol all occur once per rotation");

        var pair=new[]{"astrid-01","bjorn-02"};
        var full=new RoutineAvailability{HasCookingFire=true,HasShelter=true,HasRepairSurface=true};
        for(int epoch=0;epoch<12;epoch++) {
            var jobs=CampLifecyclePolicy.AssignCampRoutines(c.CampId,pair,epoch,full); int watchers=0;
            foreach(var job in jobs.Values) if(job==CampRoutine.Watch) watchers++;
            Check(jobs.Count==2 && watchers==1, "two-person camp keeps exactly one watcher at epoch "+epoch);
        }
        var reversed=CampLifecyclePolicy.AssignCampRoutines(c.CampId,new[]{"bjorn-02","astrid-01"},3,full);
        var ordered=CampLifecyclePolicy.AssignCampRoutines(c.CampId,pair,3,full);
        Check(reversed["astrid-01"]==ordered["astrid-01"] && reversed["bjorn-02"]==ordered["bjorn-02"], "assignment ignores scene enumeration order");
        var bare=CampLifecyclePolicy.AssignCampRoutines(c.CampId,new[]{"astrid-01","bjorn-02","cara-03"},4,new RoutineAvailability());
        int bareWatch=0,barePatrol=0; foreach(var job in bare.Values){if(job==CampRoutine.Watch)bareWatch++;if(job==CampRoutine.Patrol)barePatrol++;}
        Check(bareWatch==1 && barePatrol==2, "missing stations fall back to watch and patrol only");
        var assignedObservation=Seen(1200); assignedObservation.HasCampRoutineAssignment=true; assignedObservation.AssignedCampRoutine=ordered[p.PersonId];
        Check(CampLifecyclePolicy.Observe(p,c,assignedObservation).State.Routine==ordered[p.PersonId], "person policy consumes coordinated camp assignment");
        var combatAssigned=assignedObservation; combatAssigned.CombatActive=true;
        Check(CampLifecyclePolicy.Observe(p,c,combatAssigned).Owner==ActionOwner.Combat, "combat still preempts coordinated camp assignment");

        var needObservation=Seen(1200); needObservation.HasCampRoutineAssignment=true; needObservation.AssignedCampRoutine=CampRoutine.Cook;
        var cooking=CampLifecyclePolicy.Observe(p,c,needObservation);
        var lowFood=new CampNeedObservation{EvidenceKey="food-bin-a",WorldTick=1201,Need=CampNeedKind.CookingSuppliesLow,Unmet=true};
        var surfaced=CampLifecyclePolicy.ObserveRoutineNeed(p,cooking,lowFood);
        Check(surfaced.Kind==CampFactEventKind.NeedObserved && surfaced.FactId.Contains("food-bin-a") && !surfaced.ResumeOrdinaryLife, "ordinary cook task exposes one concrete unmet fact");
        var repeated=CampLifecyclePolicy.ObserveRoutineNeed(p,cooking,lowFood);
        Check(repeated.FactId==surfaced.FactId, "same evidence produces stable fact identity for external deduplication");
        lowFood.Unmet=false; lowFood.AidConfirmed=true; lowFood.WorldTick=1210;
        var helped=CampLifecyclePolicy.ObserveRoutineNeed(p,cooking,lowFood);
        Check(helped.Kind==CampFactEventKind.AidConfirmed && helped.FactId==surfaced.FactId && helped.ResumeOrdinaryLife, "confirmed aid closes same fact and resumes ordinary life");
        var combatNeed=CampLifecyclePolicy.ObserveRoutineNeed(p,CampLifecyclePolicy.Observe(p,c,combatAssigned),lowFood);
        Check(combatNeed.Kind==CampFactEventKind.None, "combat suppresses ordinary need seam");
        var awayPerson=p.Copy(); awayPerson.Presence=PersonPresence.Away;
        Check(CampLifecyclePolicy.ObserveRoutineNeed(awayPerson,cooking,lowFood).Kind==CampFactEventKind.None, "away person freezes ordinary need seam");
        Throws(()=>CampLifecyclePolicy.ObserveRoutineNeed(p,cooking,new CampNeedObservation{EvidenceKey="route",Need=CampNeedKind.PatrolRouteObstructed,Unmet=true}), "routine cannot surface unrelated need");

        foreach(var owner in new[]{ActionOwner.Alert,ActionOwner.CombatRegroup,ActionOwner.Combat,ActionOwner.Incapacitated}) {
            var o=Seen(1200); o.AlertActive=owner==ActionOwner.Alert; o.CombatClearRegroup=owner==ActionOwner.CombatRegroup; o.CombatActive=owner==ActionOwner.Combat; o.Incapacitated=owner==ActionOwner.Incapacitated;
            var d=CampLifecyclePolicy.Observe(p,c,o); Check(d.Directive==RuntimeDirective.Yield && d.Owner==owner, "higher owner preempts routine: "+owner);
        }

        var unloaded=Seen(1800); unloaded.ZoneLoaded=false; unloaded.ActorExists=false;
        var frozen=CampLifecyclePolicy.Observe(p,c,unloaded); Check(frozen.Directive==RuntimeDirective.Despawn && frozen.State.Life==PersonLife.Living && !frozen.Persist, "unload freezes and never kills");
        var absent=Seen(1800); absent.PlayerNear=false; absent.ConfirmedDeath=true;
        Check(CampLifecyclePolicy.Observe(p,c,absent).State.Life==PersonLife.Living, "offscreen death claim refused");
        var remote=Seen(1800); remote.AuthoritativeOwner=false; remote.ConfirmedDeath=true;
        Check(CampLifecyclePolicy.Observe(p,c,remote).State.Life==PersonLife.Living, "remote death claim refused");

        var death=Seen(1900); death.ConfirmedDeath=true; death.DeathCause="draugr"; death.DeathPlace="old hall";
        var dead=CampLifecyclePolicy.Observe(p,c,death); Check(dead.Persist && dead.State.Life==PersonLife.Dead && dead.State.DeathCause=="draugr", "observed authoritative death commits memorial");
        var reload=CampLifecyclePolicy.Observe(dead.State,c,Seen(2400)); Check(reload.State.Life==PersonLife.Dead && reload.Owner==ActionOwner.Dead, "reload cannot revive");
        Throws(()=>CampLifecyclePolicy.ReturnToCamp(dead.State,c.CampId), "dead identity cannot return");
        Throws(()=>CampLifecyclePolicy.BeginMigration(dead.State,"camp-2"), "dead identity cannot migrate");

        var moving=CampLifecyclePolicy.BeginMigration(p,"camp-blackforest-2");
        var enroute=CampLifecyclePolicy.Observe(moving,c,Seen(2000)); Check(enroute.Directive==RuntimeDirective.MoveToDestination && enroute.State.CampId==p.CampId, "migration does not pre-spawn destination copy");
        var arrived=Seen(2100); arrived.ArrivalConfirmed=true; arrived.DestinationLoaded=true; arrived.SafeAnchor="tower-b";
        var arrival=CampLifecyclePolicy.Observe(moving,c,arrived); Check(arrival.Persist && arrival.State.CampId=="camp-blackforest-2" && arrival.State.Presence==PersonPresence.AtCamp && arrival.State.DestinationCampId=="", "arrival atomically transfers identity");
        Check(CampLifecyclePolicy.SpawnClaimKey(p)==CampLifecyclePolicy.SpawnClaimKey(arrival.State), "spawn claim follows person, not camp");
        var migratingCamp=CampLifecyclePolicy.BeginCampMigration(c,"camp-blackforest-2");
        Check(migratingCamp.Disposition==CampDisposition.Migrating && c.Disposition==CampDisposition.Active, "camp migration is explicit and immutable-input");
        Throws(()=>CampLifecyclePolicy.RetireCamp(migratingCamp,1), "camp with a living member cannot retire");
        Check(CampLifecyclePolicy.RetireCamp(migratingCamp,0).Disposition==CampDisposition.Retired, "empty origin camp may retire without deleting people");

        var legacy=new Dictionary<string,string>{{"person_id","astrid-01"},{"schema","0"}};
        var plan=CampLifecyclePolicy.MigrationWritePlan(legacy); Check(plan.Count==4 && plan["life"]=="Living", "known legacy record gets bounded additive plan");
        Throws(()=>CampLifecyclePolicy.MigrationWritePlan(new Dictionary<string,string>{{"schema","0"}}), "identity-less migration refused");
        Throws(()=>CampLifecyclePolicy.MigrationWritePlan(new Dictionary<string,string>{{"person_id","x"},{"schema","99"}}), "unknown future schema refused");

        Console.WriteLine("CampLifecyclePolicyTests PASS: " + checks + " checks. Unity/ZDO integration NOT RUN."); return 0;
    }
}

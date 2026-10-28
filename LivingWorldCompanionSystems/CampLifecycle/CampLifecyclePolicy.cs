using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.CampLifecycle
{
    public enum CampRoutine { Watch, Cook, Rest, Repair, Patrol }
    public enum PersonLife { Living, Dead }
    public enum PersonPresence { AtCamp, Away, Migrating, Resident }
    public enum CampDisposition { Active, Migrating, Retired }
    public enum ActionOwner { None, CampRoutine, OrderedMovement, Alert, CombatRegroup, Combat, Incapacitated, Dead }
    public enum RuntimeDirective { None, RunRoutine, Yield, Despawn, Spawn, MoveToDestination, RecordMemorial }
    public enum CampNeedKind { None, WatchVisibilityBlocked, CookingSuppliesLow, RestShelterDamaged, RepairMaterialsLow, PatrolRouteObstructed }
    public enum CampFactEventKind { None, NeedObserved, AidConfirmed }
    public enum CampSceneActivity { CampLife, Hunt, Patrol }
    public enum ObservedActorKind { Player, Wildlife, OtherModCharacter }
    public enum SceneNoticeKind { None, CautiousNotice, ForwardForThreatEvaluation, RequestReadiness, YieldToNativeCombat }

    public sealed class CampPersonState
    {
        public string PersonId, CampId, DestinationCampId, LastSafeAnchor, DeathPlace, DeathCause;
        public PersonLife Life;
        public PersonPresence Presence;
        public CampRoutine Routine;
        public long RoutineEpoch, DeathWorldTick;
        public int SchemaVersion;

        public CampPersonState Copy()
        {
            return (CampPersonState)MemberwiseClone();
        }
    }

    public sealed class CampState
    {
        public string CampId, MigrationDestinationCampId;
        public CampDisposition Disposition;
        public long RoutineEpoch, LastObservedWorldTick;
        public int SchemaVersion;
        public CampState Copy() { return (CampState)MemberwiseClone(); }
    }

    public sealed class Observation
    {
        public long WorldTick;
        public bool ZoneLoaded, PlayerNear, AuthoritativeOwner, ActorExists, CombatActive, CombatClearRegroup,
            AlertActive, Incapacitated, ConfirmedDeath, SafeAnchorAvailable, DestinationLoaded, ArrivalConfirmed;
        public string SafeAnchor, DeathPlace, DeathCause;
        // Supplied by AssignCampRoutines for a loaded multi-person camp.
        public bool HasCampRoutineAssignment;
        public CampRoutine AssignedCampRoutine;
    }

    public sealed class RoutineAvailability
    {
        public bool HasCookingFire, HasShelter, HasRepairSurface;
    }

    // A transport fact only. Needs/Affinity owns need state, aid attribution, rewards, and dialogue.
    public sealed class CampNeedObservation
    {
        public string EvidenceKey;
        public long WorldTick;
        public CampNeedKind Need;
        public bool Unmet, AidConfirmed;
    }

    public sealed class CampFactEvent
    {
        public CampFactEventKind Kind;
        public string FactId, CampId, PersonId;
        public CampRoutine Routine;
        public CampNeedKind Need;
        public long WorldTick;
        public bool ResumeOrdinaryLife;
    }

    public sealed class SceneObservation
    {
        public string ActorId;
        public CampSceneActivity Activity;
        public ObservedActorKind ActorKind;
        public float DistanceFromObserver, DistanceFromCampAnchor;
        public long WorldTick, LastVisibleWorldTick;
        public bool HasLineOfSight, ExternalThreatAuthorized, NativeCombatCommitted;
    }

    public sealed class SceneNotice
    {
        public SceneNoticeKind Kind;
        public string CampId, ObserverPersonId, ActorId;
        public CampSceneActivity Activity;
        public ObservedActorKind ActorKind;
        public bool GrantsAttackAuthority; // Always false: allegiance/native combat owns authorization.
        public string Reason;
    }

    public sealed class LifecycleDecision
    {
        public CampPersonState State;
        public RuntimeDirective Directive;
        public ActionOwner Owner;
        public string Reason;
        public bool Persist;
    }

    public static class CampLifecyclePolicy
    {
        public const int CurrentSchema = 1;
        public const long RoutinePeriodTicks = 600;
        public const float MaximumSceneObservationDistance = 40f;
        public const float MaximumHuntPatrolRadius = 60f;
        public const long MaximumVisibleEvidenceAgeTicks = 5;

        static readonly CampRoutine[] Rotation = {
            CampRoutine.Watch, CampRoutine.Cook, CampRoutine.Rest, CampRoutine.Repair, CampRoutine.Patrol
        };

        public static CampRoutine RoutineFor(string campId, string personId, long epoch)
        {
            RequireId(campId, "camp"); RequireId(personId, "person");
            unchecked
            {
                uint hash = 2166136261;
                string value = campId + "|" + personId;
                for (int i = 0; i < value.Length; i++) { hash ^= value[i]; hash *= 16777619; }
                return Rotation[(int)((hash + (uint)(epoch % Rotation.Length)) % Rotation.Length)];
            }
        }

        // Coordinates the whole loaded camp. Input order is irrelevant; identity order and
        // epoch deterministically select one watcher while available work is staggered.
        public static IDictionary<string, CampRoutine> AssignCampRoutines(string campId,
            IList<string> loadedLivingPersonIds, long epoch, RoutineAvailability availability)
        {
            RequireId(campId, "camp");
            if (loadedLivingPersonIds == null) throw new ArgumentNullException("loadedLivingPersonIds");
            if (availability == null) throw new ArgumentNullException("availability");
            var ids = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < loadedLivingPersonIds.Count; i++)
            {
                RequireId(loadedLivingPersonIds[i], "person");
                if (!unique.Add(loadedLivingPersonIds[i])) throw new InvalidOperationException("Duplicate person identity in loaded camp.");
                ids.Add(loadedLivingPersonIds[i]);
            }
            ids.Sort(StringComparer.Ordinal);
            var result = new Dictionary<string, CampRoutine>(StringComparer.Ordinal);
            if (ids.Count == 0) return result;

            int watcher = PositiveModulo(StableHash(campId) + epoch, ids.Count);
            result[ids[watcher]] = CampRoutine.Watch;
            var work = new List<CampRoutine>();
            if (availability.HasCookingFire) work.Add(CampRoutine.Cook);
            if (availability.HasShelter) work.Add(CampRoutine.Rest);
            if (availability.HasRepairSurface) work.Add(CampRoutine.Repair);
            work.Add(CampRoutine.Patrol); // station-free safe fallback

            int offset = PositiveModulo(StableHash(campId + "|work") + epoch, work.Count);
            int assigned = 0;
            for (int i = 0; i < ids.Count; i++)
            {
                if (i == watcher) continue;
                result[ids[i]] = work[(offset + assigned) % work.Count];
                assigned++;
            }
            return result;
        }

        public static CampFactEvent ObserveRoutineNeed(CampPersonState person,
            LifecycleDecision lifecycle, CampNeedObservation observation)
        {
            if (person == null) throw new ArgumentNullException("person");
            if (lifecycle == null) throw new ArgumentNullException("lifecycle");
            if (observation == null) throw new ArgumentNullException("observation");
            // This also freezes the seam while away/unloaded and while any higher action owns the actor.
            if (person.Life != PersonLife.Living || person.Presence != PersonPresence.AtCamp ||
                lifecycle.Owner != ActionOwner.CampRoutine || lifecycle.Directive != RuntimeDirective.RunRoutine)
                return EmptyFact();
            if (!observation.Unmet && !observation.AidConfirmed) return EmptyFact();
            if (observation.Need == CampNeedKind.None || !NeedMatchesRoutine(observation.Need, lifecycle.State.Routine))
                throw new InvalidOperationException("Need evidence does not match the active ordinary routine.");
            RequireId(observation.EvidenceKey, "need evidence");
            string id = "campfact:" + person.CampId + ":" + person.PersonId + ":" +
                observation.Need.ToString() + ":" + observation.EvidenceKey;
            if (observation.AidConfirmed)
                return new CampFactEvent { Kind=CampFactEventKind.AidConfirmed, FactId=id, CampId=person.CampId,
                    PersonId=person.PersonId, Routine=lifecycle.State.Routine, Need=observation.Need,
                    WorldTick=observation.WorldTick, ResumeOrdinaryLife=true };
            return new CampFactEvent { Kind=CampFactEventKind.NeedObserved, FactId=id, CampId=person.CampId,
                PersonId=person.PersonId, Routine=lifecycle.State.Routine, Need=observation.Need,
                WorldTick=observation.WorldTick, ResumeOrdinaryLife=false };
        }

        public static SceneNotice ObserveScene(CampPersonState person,
            LifecycleDecision lifecycle, SceneObservation observation)
        {
            if (person == null) throw new ArgumentNullException("person");
            if (lifecycle == null) throw new ArgumentNullException("lifecycle");
            if (observation == null) throw new ArgumentNullException("observation");
            RequireId(observation.ActorId, "observed actor");
            if (!Enum.IsDefined(typeof(CampSceneActivity), observation.Activity) ||
                !Enum.IsDefined(typeof(ObservedActorKind), observation.ActorKind))
                throw new InvalidOperationException("Unknown scene observation value.");
            if (person.Life != PersonLife.Living || person.Presence != PersonPresence.AtCamp)
                return EmptyNotice("away, unloaded, or terminal person is frozen");
            if (observation.NativeCombatCommitted || lifecycle.Owner == ActionOwner.Combat)
                return Notice(person, observation, SceneNoticeKind.YieldToNativeCombat, "native combat preempts scene activity");
            if (lifecycle.Owner != ActionOwner.CampRoutine || lifecycle.Directive != RuntimeDirective.RunRoutine)
                return EmptyNotice("higher action owner suppresses scene observation");
            if (!observation.HasLineOfSight || observation.DistanceFromObserver < 0f ||
                observation.DistanceFromObserver > MaximumSceneObservationDistance ||
                observation.DistanceFromCampAnchor < 0f || observation.DistanceFromCampAnchor > MaximumHuntPatrolRadius ||
                observation.LastVisibleWorldTick > observation.WorldTick ||
                observation.WorldTick - observation.LastVisibleWorldTick > MaximumVisibleEvidenceAgeTicks)
                return EmptyNotice("observation is stale, obstructed, or outside bounded activity radius");

            if (observation.ActorKind == ObservedActorKind.Player)
                return Notice(person, observation, SceneNoticeKind.CautiousNotice,
                    "player noticed without hostility or attack authority");
            if (observation.ExternalThreatAuthorized)
                return Notice(person, observation, SceneNoticeKind.RequestReadiness,
                    "external allegiance/threat layer authorized readiness; native combat still decides commitment");
            return Notice(person, observation, SceneNoticeKind.ForwardForThreatEvaluation,
                "visible wildlife or third-party character forwarded to existing allegiance/native threat evaluation");
        }

        public static LifecycleDecision Observe(CampPersonState source, CampState camp, Observation o)
        {
            Validate(source, camp);
            if (o == null) throw new ArgumentNullException("o");
            var next = source.Copy();

            // A committed death is immutable. Loading, migration, recall, and routine selection cannot revive it.
            if (source.Life == PersonLife.Dead)
                return Decision(next, RuntimeDirective.RecordMemorial, ActionOwner.Dead, "committed death is terminal", false);

            // Death is accepted only as an authoritative, loaded, observed transaction. Absence is never death.
            if (o.ConfirmedDeath && o.ZoneLoaded && o.PlayerNear && o.ActorExists && o.AuthoritativeOwner)
            {
                next.Life = PersonLife.Dead;
                next.Presence = PersonPresence.Away;
                next.DeathWorldTick = o.WorldTick;
                next.DeathPlace = o.DeathPlace ?? "";
                next.DeathCause = o.DeathCause ?? "unknown";
                return Decision(next, RuntimeDirective.RecordMemorial, ActionOwner.Dead, "authoritative death committed once", true);
            }

            if (!o.ZoneLoaded || !o.PlayerNear)
                return Decision(next, RuntimeDirective.Despawn, ActionOwner.None,
                    "unloaded or unobserved: freeze persisted person; do not simulate casualty", false);

            if (!o.AuthoritativeOwner)
                return Decision(next, RuntimeDirective.None, ActionOwner.None, "remote replica is read-only", false);

            if (o.Incapacitated)
                return Decision(next, RuntimeDirective.Yield, ActionOwner.Incapacitated, "incapacitation outranks camp work", false);
            if (o.CombatActive)
                return Decision(next, RuntimeDirective.Yield, ActionOwner.Combat, "combat owns movement, facing, hands and animation", false);
            if (o.CombatClearRegroup)
                return Decision(next, RuntimeDirective.Yield, ActionOwner.CombatRegroup, "camp work waits for verified regroup release", false);
            if (o.AlertActive)
                return Decision(next, RuntimeDirective.Yield, ActionOwner.Alert, "readiness outranks camp work", false);

            if (source.Presence == PersonPresence.Migrating)
            {
                if (o.ArrivalConfirmed && o.DestinationLoaded && !String.IsNullOrEmpty(source.DestinationCampId))
                {
                    next.CampId = source.DestinationCampId;
                    next.DestinationCampId = "";
                    next.Presence = PersonPresence.AtCamp;
                    next.LastSafeAnchor = o.SafeAnchorAvailable ? o.SafeAnchor : next.LastSafeAnchor;
                    next.RoutineEpoch = o.WorldTick / RoutinePeriodTicks;
                    next.Routine = RoutineFor(next.CampId, next.PersonId, next.RoutineEpoch);
                    return Decision(next, RuntimeDirective.Spawn, ActionOwner.None, "authoritative arrival commits destination", true);
                }
                return Decision(next, RuntimeDirective.MoveToDestination, ActionOwner.OrderedMovement,
                    "migration remains one persisted person; destination cannot spawn a copy before arrival", false);
            }

            if (o.SafeAnchorAvailable && !String.IsNullOrEmpty(o.SafeAnchor) && o.SafeAnchor != source.LastSafeAnchor)
            { next.LastSafeAnchor = o.SafeAnchor; }
            long epoch = o.WorldTick / RoutinePeriodTicks;
            CampRoutine routine = o.HasCampRoutineAssignment ? o.AssignedCampRoutine : RoutineFor(source.CampId, source.PersonId, epoch);
            bool changed = next.LastSafeAnchor != source.LastSafeAnchor || next.RoutineEpoch != epoch || next.Routine != routine;
            next.RoutineEpoch = epoch; next.Routine = routine;
            return Decision(next, RuntimeDirective.RunRoutine, ActionOwner.CampRoutine, "deterministic peaceful routine lease", changed);
        }

        public static CampPersonState BeginMigration(CampPersonState source, string destinationCampId)
        {
            if (source == null) throw new ArgumentNullException("source");
            RequireId(destinationCampId, "destination camp");
            if (source.Life == PersonLife.Dead) throw new InvalidOperationException("Dead people cannot migrate or return.");
            if (source.Presence == PersonPresence.Migrating) throw new InvalidOperationException("Migration already in progress.");
            if (source.CampId == destinationCampId) throw new InvalidOperationException("Destination must differ from current camp.");
            var next = source.Copy(); next.Presence = PersonPresence.Migrating; next.DestinationCampId = destinationCampId; return next;
        }

        public static CampState BeginCampMigration(CampState source, string destinationCampId)
        {
            if (source == null) throw new ArgumentNullException("source"); RequireId(source.CampId, "camp"); RequireId(destinationCampId, "destination camp");
            if (source.SchemaVersion != CurrentSchema) throw new InvalidOperationException("Camp must be migrated explicitly before use.");
            if (source.Disposition != CampDisposition.Active) throw new InvalidOperationException("Only an active camp may begin migration.");
            if (source.CampId == destinationCampId) throw new InvalidOperationException("Destination must differ from current camp.");
            var next = source.Copy(); next.Disposition = CampDisposition.Migrating; next.MigrationDestinationCampId = destinationCampId; return next;
        }

        public static CampState RetireCamp(CampState source, int livingMembersStillAssigned)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (livingMembersStillAssigned != 0) throw new InvalidOperationException("A camp cannot retire while it still owns living people.");
            var next = source.Copy(); next.Disposition = CampDisposition.Retired; next.MigrationDestinationCampId = ""; return next;
        }

        public static CampPersonState ReturnToCamp(CampPersonState source, string campId)
        {
            if (source == null) throw new ArgumentNullException("source"); RequireId(campId, "camp");
            if (source.Life == PersonLife.Dead) throw new InvalidOperationException("There is no Third Breath.");
            var next = source.Copy(); next.CampId = campId; next.DestinationCampId = ""; next.Presence = PersonPresence.AtCamp; return next;
        }

        // The adapter must enforce this key globally before instantiating an actor.
        public static string SpawnClaimKey(CampPersonState state)
        { if (state == null) throw new ArgumentNullException("state"); RequireId(state.PersonId, "person"); return "livingworld.person." + state.PersonId; }

        public static IDictionary<string, string> MigrationWritePlan(IDictionary<string, string> record)
        {
            if (record == null) throw new ArgumentNullException("record");
            string person; if (!record.TryGetValue("person_id", out person) || String.IsNullOrWhiteSpace(person))
                throw new InvalidOperationException("Legacy record has no stable person identity; migration refused.");
            string schema; record.TryGetValue("schema", out schema);
            if (schema == CurrentSchema.ToString()) return new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(schema) && schema != "0") throw new InvalidOperationException("Unknown schema; migration refused.");
            return new Dictionary<string, string> { { "schema", CurrentSchema.ToString() }, { "life", "Living" },
                { "presence", "AtCamp" }, { "routine_epoch", "0" } };
        }

        static LifecycleDecision Decision(CampPersonState state, RuntimeDirective directive, ActionOwner owner, string reason, bool persist)
        { return new LifecycleDecision { State = state, Directive = directive, Owner = owner, Reason = reason, Persist = persist }; }

        static void Validate(CampPersonState person, CampState camp)
        {
            if (person == null) throw new ArgumentNullException("person"); if (camp == null) throw new ArgumentNullException("camp");
            RequireId(person.PersonId, "person"); RequireId(person.CampId, "camp"); RequireId(camp.CampId, "camp record");
            if (person.SchemaVersion != CurrentSchema || camp.SchemaVersion != CurrentSchema) throw new InvalidOperationException("State must be migrated explicitly before use.");
            if (person.Presence != PersonPresence.Migrating && person.CampId != camp.CampId) throw new InvalidOperationException("Person/camp mismatch.");
        }

        static void RequireId(string value, string label)
        { if (String.IsNullOrWhiteSpace(value)) throw new ArgumentException(label + " identity is required."); }

        static int StableHash(string value)
        {
            unchecked { uint hash=2166136261; for(int i=0;i<value.Length;i++){hash^=value[i];hash*=16777619;} return (int)(hash & 0x7fffffff); }
        }
        static int PositiveModulo(long value, int divisor)
        { long result=value%divisor; return (int)(result<0 ? result+divisor : result); }
        static CampFactEvent EmptyFact() { return new CampFactEvent { Kind=CampFactEventKind.None }; }
        static bool NeedMatchesRoutine(CampNeedKind need, CampRoutine routine)
        {
            return (routine==CampRoutine.Watch && need==CampNeedKind.WatchVisibilityBlocked) ||
                (routine==CampRoutine.Cook && need==CampNeedKind.CookingSuppliesLow) ||
                (routine==CampRoutine.Rest && need==CampNeedKind.RestShelterDamaged) ||
                (routine==CampRoutine.Repair && need==CampNeedKind.RepairMaterialsLow) ||
                (routine==CampRoutine.Patrol && need==CampNeedKind.PatrolRouteObstructed);
        }
        static SceneNotice EmptyNotice(string reason)
        { return new SceneNotice { Kind=SceneNoticeKind.None, GrantsAttackAuthority=false, Reason=reason }; }
        static SceneNotice Notice(CampPersonState person, SceneObservation observation, SceneNoticeKind kind, string reason)
        {
            return new SceneNotice { Kind=kind, CampId=person.CampId, ObserverPersonId=person.PersonId,
                ActorId=observation.ActorId, Activity=observation.Activity, ActorKind=observation.ActorKind,
                GrantsAttackAuthority=false, Reason=reason };
        }
    }
}

using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.SpecialistCombat.PredictionAndThrows
{
    public struct Vec3
    {
        public double X, Y, Z;
        public Vec3(double x, double y, double z) { X=x; Y=y; Z=z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) { return new Vec3(a.X+b.X,a.Y+b.Y,a.Z+b.Z); }
        public static Vec3 operator -(Vec3 a, Vec3 b) { return new Vec3(a.X-b.X,a.Y-b.Y,a.Z-b.Z); }
        public static Vec3 operator *(Vec3 a, double b) { return new Vec3(a.X*b,a.Y*b,a.Z*b); }
        public double Length { get { return Math.Sqrt(X*X+Y*Y+Z*Z); } }
        public double HorizontalLength { get { return Math.Sqrt(X*X+Z*Z); } }
    }

    public enum ThrowKind { Spear, Area }
    public enum RequestStatus { Reserved, Released, Cancelled }

    public sealed class ThrowRequest
    {
        public RequestStatus Status;
        public Vec3 AimPoint, PredictedImpact;
        public double FlightSeconds, DetonationSeconds, Confidence;
        public ThrowRequest Copy() { return (ThrowRequest)MemberwiseClone(); }
    }

    public sealed class MotionSample
    {
        public Vec3 Position, Velocity, PreviousVelocity;
        public double AgeSeconds;
        public bool Visible;
    }

    public sealed class MovingBody
    {
        public Vec3 Position, Velocity;
        public double Radius;
    }

    public sealed class ThrowConfig
    {
        public ThrowKind Kind;
        public double ProjectileSpeed, Gravity, FuseSeconds, BlastRadius, MaximumFlightSeconds;
        public double MinimumRange, MaximumRange, MaximumSenseAgeSeconds, MinimumConfidence;
        public double DirectionChangePenalty, SafetyMargin, EscapeSpeed;
    }

    // World queries stay outside this engine-free policy. The live adapter must calculate these
    // facts from the authoritative scene immediately before asking for a reservation.
    public sealed class ThrowFacts
    {
        public string ActorId, PersonId, EncounterId, TargetId, RequestId;
        public long GearRevision;
        public long OwnerPeerId, LocalPeerId;
        public bool ServerAuthoritative, ActorNetworkOwner, CombatOwnsAction, NativeAttackReady;
        public bool TerrainArcClear, BuildingArcClear, TargetAuthorized, ImpactGroundReachable;
        public bool EscapeRouteReachable;
        public Vec3 Origin, EncounterCenter;
        public double EncounterRadius;
        public MotionSample Target;
        public IList<MovingBody> Allies = new List<MovingBody>();
        public IList<MovingBody> ProtectedNeutrals = new List<MovingBody>();
    }

    public sealed class ThrowState
    {
        public int AvailableAmmo;
        public long Revision;
        public readonly Dictionary<string, ThrowRequest> Requests = new Dictionary<string, ThrowRequest>(StringComparer.Ordinal);
        public ThrowState Copy() { var x=new ThrowState { AvailableAmmo=AvailableAmmo, Revision=Revision }; foreach(var p in Requests)x.Requests.Add(p.Key,p.Value.Copy()); return x; }
    }

    public sealed class ThrowDecision
    {
        public bool Authorized, Replayed;
        public string Reason;
        public Vec3 AimPoint, PredictedImpact;
        public double FlightSeconds, DetonationSeconds, Confidence;
        public ThrowState State;
    }

    public static class PredictionAndThrowsPolicy
    {
        public static ThrowDecision Reserve(ThrowState source, ThrowConfig config, ThrowFacts facts)
        {
            string invalid=Validate(source,config,facts); if(invalid!=null)return No(source,invalid);
            ThrowRequest prior;
            if(source.Requests.TryGetValue(facts.RequestId,out prior))
                return new ThrowDecision { Authorized=prior.Status==RequestStatus.Reserved, Replayed=true,
                    Reason=prior.Status==RequestStatus.Reserved?"request_already_reserved":"request_already_terminal", State=source,
                    AimPoint=prior.AimPoint,PredictedImpact=prior.PredictedImpact,FlightSeconds=prior.FlightSeconds,
                    DetonationSeconds=prior.DetonationSeconds,Confidence=prior.Confidence };
            if(source.AvailableAmmo<=ReservedCount(source))return No(source,"no_unreserved_ammunition");

            double confidence=Confidence(facts.Target,config);
            if(confidence<config.MinimumConfidence)return No(source,"prediction_confidence_too_low");
            double t=Math.Max(.01,(facts.Target.Position-facts.Origin).Length/config.ProjectileSpeed);
            Vec3 future=facts.Target.Position;
            for(int i=0;i<8;i++) {
                future=facts.Target.Position+facts.Target.Velocity*t;
                t=(future-facts.Origin).Length/config.ProjectileSpeed;
            }
            if(t>config.MaximumFlightSeconds)return No(source,"flight_time_exceeded");
            double horizontal=(future-facts.Origin).HorizontalLength;
            if(horizontal<config.MinimumRange||horizontal>config.MaximumRange)return No(source,"range_invalid");
            if(!Inside(future,facts.EncounterCenter,facts.EncounterRadius))return No(source,"predicted_target_outside_encounter");
            if(!facts.TerrainArcClear||!facts.BuildingArcClear)return No(source,"ballistic_arc_obstructed");

            double detonation=config.Kind==ThrowKind.Area?Math.Max(t,config.FuseSeconds):t;
            Vec3 impact=config.Kind==ThrowKind.Area?new Vec3(future.X,facts.Target.Position.Y,future.Z):future;
            if(!Inside(impact,facts.EncounterCenter,facts.EncounterRadius))return No(source,"impact_outside_encounter");
            if(config.Kind==ThrowKind.Area&&!facts.ImpactGroundReachable)return No(source,"impact_ground_invalid");
            double unsafeRadius=config.Kind==ThrowKind.Area?config.BlastRadius+config.SafetyMargin:config.SafetyMargin;
            if(Threatens(facts.Allies,impact,detonation,unsafeRadius))return No(source,"ally_in_release_or_impact_space");
            if(Threatens(facts.ProtectedNeutrals,impact,detonation,unsafeRadius))return No(source,"protected_neutral_in_release_or_impact_space");
            if(config.Kind==ThrowKind.Area) {
                double actorDistance=(facts.Origin-impact).HorizontalLength;
                if(actorDistance<unsafeRadius) {
                    double escapeNeeded=unsafeRadius-actorDistance;
                    double escapeTime=Math.Max(t,config.FuseSeconds);
                    if(!facts.EscapeRouteReachable||config.EscapeSpeed<=0||escapeNeeded/config.EscapeSpeed>escapeTime)
                        return No(source,"blast_escape_not_proven");
                }
            }
            // Gravity compensation changes aim only. Native projectile physics remain authoritative.
            Vec3 aim=new Vec3(impact.X,impact.Y+.5*config.Gravity*t*t,impact.Z);
            var next=source.Copy(); next.Requests.Add(facts.RequestId,new ThrowRequest { Status=RequestStatus.Reserved,
                AimPoint=aim,PredictedImpact=impact,FlightSeconds=t,DetonationSeconds=detonation,Confidence=confidence }); next.Revision++;
            return new ThrowDecision { Authorized=true,Reason="bounded_native_special_reserved",AimPoint=aim,
                PredictedImpact=impact,FlightSeconds=t,DetonationSeconds=detonation,Confidence=confidence,State=next };
        }

        public static ThrowDecision ConfirmNativeRelease(ThrowState source, string requestId)
        { return Finish(source,requestId,true); }
        public static ThrowDecision CancelBeforeNativeRelease(ThrowState source, string requestId)
        { return Finish(source,requestId,false); }

        static ThrowDecision Finish(ThrowState source,string id,bool released)
        {
            if(source==null||String.IsNullOrWhiteSpace(id))return No(source,"invalid_confirmation");
            ThrowRequest request;
            if(!source.Requests.TryGetValue(id,out request))return No(source,"unknown_request");
            if(request.Status!=RequestStatus.Reserved)return new ThrowDecision { Authorized=request.Status==RequestStatus.Released,
                Replayed=true,Reason=request.Status==RequestStatus.Released?"native_release_already_confirmed":"cancellation_already_confirmed",State=source };
            var next=source.Copy(); next.Requests[id].Status=released?RequestStatus.Released:RequestStatus.Cancelled;
            if(released) { if(next.AvailableAmmo<=0)return No(source,"ammunition_reconciliation_failed"); next.AvailableAmmo--; }
            next.Revision++;
            return new ThrowDecision { Authorized=released,Reason=released?"native_release_confirmed":"reservation_cancelled",State=next };
        }

        static string Validate(ThrowState s,ThrowConfig c,ThrowFacts f)
        {
            if(s==null||c==null||f==null||f.Target==null)return "missing_input";
            if(String.IsNullOrWhiteSpace(f.ActorId)||String.IsNullOrWhiteSpace(f.PersonId)||String.IsNullOrWhiteSpace(f.EncounterId)||
                String.IsNullOrWhiteSpace(f.TargetId)||String.IsNullOrWhiteSpace(f.RequestId)||f.GearRevision<0)return "stable_ids_required";
            if(!f.ServerAuthoritative||!f.ActorNetworkOwner||f.OwnerPeerId!=f.LocalPeerId)return "not_authoritative_owner";
            if(!f.CombatOwnsAction||!f.TargetAuthorized)return "combat_or_target_not_authorized";
            if(!f.NativeAttackReady)return "native_attack_not_ready";
            if(!f.Target.Visible||f.Target.AgeSeconds<0||f.Target.AgeSeconds>c.MaximumSenseAgeSeconds)return "target_motion_not_recently_visible";
            if(c.ProjectileSpeed<=0||c.MaximumFlightSeconds<=0||c.MaximumRange<=c.MinimumRange||f.EncounterRadius<=0)return "invalid_configuration";
            return null;
        }
        static double Confidence(MotionSample m,ThrowConfig c)
        {
            double age=1.0-m.AgeSeconds/c.MaximumSenseAgeSeconds;
            double a=m.Velocity.Length,b=m.PreviousVelocity.Length,alignment=1;
            if(a>.001&&b>.001)alignment=(m.Velocity.X*m.PreviousVelocity.X+m.Velocity.Y*m.PreviousVelocity.Y+m.Velocity.Z*m.PreviousVelocity.Z)/(a*b);
            double changed=(1-Math.Max(-1,Math.Min(1,alignment)))*.5;
            return Math.Max(0,Math.Min(1,age-changed*c.DirectionChangePenalty));
        }
        static bool Threatens(IList<MovingBody> bodies,Vec3 impact,double seconds,double margin)
        { if(bodies==null)return false; for(int i=0;i<bodies.Count;i++){MovingBody b=bodies[i]; if(b!=null&&(b.Position+b.Velocity*seconds-impact).Length<=margin+b.Radius)return true;} return false; }
        static bool Inside(Vec3 p,Vec3 center,double radius) { return (p-center).HorizontalLength<=radius; }
        static int ReservedCount(ThrowState s) { int n=0; foreach(var x in s.Requests.Values)if(x.Status==RequestStatus.Reserved)n++; return n; }
        static ThrowDecision No(ThrowState s,string reason) { return new ThrowDecision { Reason=reason,State=s }; }
    }
}

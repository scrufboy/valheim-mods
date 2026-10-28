using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.SpecialistCombat.MeleeRoles
{
    public struct PlanarPoint
    {
        public readonly double X, Z;
        public PlanarPoint(double x, double z) { X = x; Z = z; }
        public static PlanarPoint operator +(PlanarPoint a, PlanarPoint b) { return new PlanarPoint(a.X + b.X, a.Z + b.Z); }
        public static PlanarPoint operator -(PlanarPoint a, PlanarPoint b) { return new PlanarPoint(a.X - b.X, a.Z - b.Z); }
        public static PlanarPoint operator *(PlanarPoint a, double b) { return new PlanarPoint(a.X * b, a.Z * b); }
        public double Length { get { return Math.Sqrt(X * X + Z * Z); } }
    }

    public enum AttackShape { Thrust, Sweep, Spin }
    public enum MeleeRequest { None, NativeKnifeLunge, NativeThrust, NativeSweep, NativeSpin, HoldLine, CloseRange, ExploitStagger }
    public enum Refusal
    {
        None, NotAuthoritative, CombatDoesNotOwnActor, TargetNotAuthorized, MissingRecentMotion,
        NativeAttackUnverified, WeaponOrAnimationMismatch, AlreadyCommitted, TooClose, BeyondContact,
        EndpointUnreachable, PathObstructed, OutsideTacticalBounds, AllyOrNeutralUnsafe,
        RecoveryActive, NoSafeNativeAttack
    }

    // Runtime proof, not a guessed animation name. An adapter may expose only attacks
    // whose item, animation and contact behavior were verified in the current build.
    public sealed class NativeAttackCandidate
    {
        public string AttackId, RequiredWeaponId, AnimationId;
        public AttackShape Shape;
        public bool LiveVerified, IsEquippedWeapon, IsCurrentAnimationSet;
        public double WindupSeconds, StepDistance, MinimumContact, MaximumContact, ContactRadius;
        public double HalfArcDegrees, RecoverySeconds;
    }

    public sealed class MotionSample
    {
        public PlanarPoint Position;
        public double AgeSeconds;
    }

    public sealed class SpaceEvidence
    {
        public bool EndpointReachable, StepPathClear, EndpointInsideTacticalBounds;
        // These facts must come from the same precommit cast/overlap snapshot used by
        // the authoritative owner. Friendly and protected-neutral safety is fail closed.
        public bool AllyInStepOrContactVolume, ProtectedNeutralInStepOrContactVolume;
        public ISet<AttackShape> UnsafeArcs = new HashSet<AttackShape>();
    }

    public sealed class KnifeLungeContext
    {
        public bool IsAuthoritativeOwner, CombatOwnsActor, TargetAuthorized, AttackAlreadyCommitted;
        public PlanarPoint ActorPosition;
        public MotionSample TargetNow, TargetPrevious;
        public double TargetRadius, MaximumMotionSampleAgeSeconds;
        public NativeAttackCandidate NativeAttack;
        public SpaceEvidence Space;
    }

    public sealed class HeavyPressureContext
    {
        public bool IsAuthoritativeOwner, CombatOwnsActor, TargetAuthorized, RecoveryActive;
        public double TargetDistance;
        public bool TargetStaggered, LineNeedsHolding;
        public IList<NativeAttackCandidate> NativeAttacks;
        public SpaceEvidence Space;
    }

    public sealed class MeleeDecision
    {
        public MeleeRequest Request;
        public Refusal Refusal;
        public string NativeAttackId;
        public PlanarPoint PredictedContact, ReachableEndpoint;
        public bool CommitsTargetLock, AdapterMayMoveAfterCommit, AdapterMayRetargetAfterCommit;
        public string Reason;
    }

    public static class MeleeRolePolicy
    {
        public static MeleeDecision EvaluateKnifeLunge(KnifeLungeContext c)
        {
            if (c == null) throw new ArgumentNullException("c");
            Refusal common = Common(c.IsAuthoritativeOwner, c.CombatOwnsActor, c.TargetAuthorized);
            if (common != Refusal.None) return Deny(common);
            if (c.AttackAlreadyCommitted) return Deny(Refusal.AlreadyCommitted);
            if (!Usable(c.NativeAttack)) return Deny(c.NativeAttack != null && c.NativeAttack.LiveVerified ? Refusal.WeaponOrAnimationMismatch : Refusal.NativeAttackUnverified);
            if (c.Space == null || c.TargetNow == null || c.TargetPrevious == null ||
                c.TargetNow.AgeSeconds < 0 || c.TargetPrevious.AgeSeconds <= c.TargetNow.AgeSeconds ||
                c.TargetPrevious.AgeSeconds > c.MaximumMotionSampleAgeSeconds)
                return Deny(Refusal.MissingRecentMotion);

            double dt = c.TargetPrevious.AgeSeconds - c.TargetNow.AgeSeconds;
            PlanarPoint velocity = (c.TargetNow.Position - c.TargetPrevious.Position) * (1.0 / dt);
            double predictionTime = c.NativeAttack.WindupSeconds + c.TargetNow.AgeSeconds;
            PlanarPoint contact = c.TargetNow.Position + velocity * predictionTime;
            PlanarPoint delta = contact - c.ActorPosition;
            double distance = delta.Length;
            double minimum = Math.Max(0, c.NativeAttack.MinimumContact - c.TargetRadius);
            double maximum = c.NativeAttack.MaximumContact + c.TargetRadius + c.NativeAttack.ContactRadius;
            if (distance < minimum) return Deny(Refusal.TooClose);
            if (distance > maximum || distance == 0) return Deny(Refusal.BeyondContact);
            if (!c.Space.EndpointInsideTacticalBounds) return Deny(Refusal.OutsideTacticalBounds);
            if (!c.Space.EndpointReachable) return Deny(Refusal.EndpointUnreachable);
            if (!c.Space.StepPathClear) return Deny(Refusal.PathObstructed);
            if (c.Space.AllyInStepOrContactVolume || c.Space.ProtectedNeutralInStepOrContactVolume) return Deny(Refusal.AllyOrNeutralUnsafe);

            double step = Math.Min(c.NativeAttack.StepDistance, Math.Max(0, distance - c.NativeAttack.ContactRadius));
            PlanarPoint endpoint = c.ActorPosition + delta * (step / distance);
            return new MeleeDecision {
                Request=MeleeRequest.NativeKnifeLunge, NativeAttackId=c.NativeAttack.AttackId,
                PredictedContact=contact, ReachableEndpoint=endpoint, CommitsTargetLock=true,
                AdapterMayMoveAfterCommit=false, AdapterMayRetargetAfterCommit=false,
                Reason="Request one verified native lunge; native execution owns windup, step, contact, hit and recovery."
            };
        }

        public static MeleeDecision EvaluateHeavyPressure(HeavyPressureContext c)
        {
            if (c == null) throw new ArgumentNullException("c");
            Refusal common = Common(c.IsAuthoritativeOwner, c.CombatOwnsActor, c.TargetAuthorized);
            if (common != Refusal.None) return Deny(common);
            if (c.RecoveryActive) return Deny(Refusal.RecoveryActive);
            if (c.Space == null || c.NativeAttacks == null) return Deny(Refusal.NoSafeNativeAttack);
            if (c.Space.AllyInStepOrContactVolume || c.Space.ProtectedNeutralInStepOrContactVolume)
                return Deny(Refusal.AllyOrNeutralUnsafe);

            NativeAttackCandidate selected = Select(c, AttackShape.Thrust);
            if (c.TargetStaggered) selected = Select(c, AttackShape.Spin) ?? Select(c, AttackShape.Sweep) ?? selected;
            else if (!c.LineNeedsHolding) selected = Select(c, AttackShape.Sweep) ?? selected;
            if (selected != null) return new MeleeDecision {
                Request=ToRequest(selected.Shape), NativeAttackId=selected.AttackId, CommitsTargetLock=true,
                AdapterMayMoveAfterCommit=false, AdapterMayRetargetAfterCommit=false,
                Reason="Request one safe verified attack item; MonsterAI remains the exclusive combat executor."
            };

            // These are capability requests to MonsterAI, not locomotion or attack commands.
            bool anyVerifiedCurrent = false;
            for (int i=0; i<c.NativeAttacks.Count; i++) if (Usable(c.NativeAttacks[i])) anyVerifiedCurrent = true;
            if (!anyVerifiedCurrent) return Deny(Refusal.WeaponOrAnimationMismatch);
            return new MeleeDecision {
                Request=c.LineNeedsHolding ? MeleeRequest.HoldLine : MeleeRequest.CloseRange,
                Refusal=Refusal.NoSafeNativeAttack,
                Reason="No safe native arc; request tactical range/line preference only and let MonsterAI move."
            };
        }

        static NativeAttackCandidate Select(HeavyPressureContext c, AttackShape shape)
        {
            if (c.Space.UnsafeArcs == null || c.Space.UnsafeArcs.Contains(shape)) return null;
            for (int i=0; i<c.NativeAttacks.Count; i++) {
                NativeAttackCandidate a=c.NativeAttacks[i];
                if (Usable(a) && a.Shape==shape && c.TargetDistance>=a.MinimumContact && c.TargetDistance<=a.MaximumContact) return a;
            }
            return null;
        }
        static bool Usable(NativeAttackCandidate a) { return a != null && a.LiveVerified && a.IsEquippedWeapon && a.IsCurrentAnimationSet; }
        static Refusal Common(bool owner, bool combat, bool target) { if(!owner)return Refusal.NotAuthoritative; if(!combat)return Refusal.CombatDoesNotOwnActor; if(!target)return Refusal.TargetNotAuthorized; return Refusal.None; }
        static MeleeRequest ToRequest(AttackShape s) { return s==AttackShape.Thrust?MeleeRequest.NativeThrust:s==AttackShape.Sweep?MeleeRequest.NativeSweep:MeleeRequest.NativeSpin; }
        static MeleeDecision Deny(Refusal r) { return new MeleeDecision { Request=MeleeRequest.None, Refusal=r, AdapterMayMoveAfterCommit=false, AdapterMayRetargetAfterCommit=false, Reason=r.ToString() }; }
    }
}

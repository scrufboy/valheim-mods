using System;
using System.Globalization;

namespace LivingWorld.HumanNpc
{
    public enum CompanionOrder { Hold, Follow }
    public enum CompanionMotion { Hold, Follow, WaitForLeader, Invalid }

    // One invariant-culture ZDO string is the commit unit. Runtime Unity objects,
    // combat targets and combat authorizations are deliberately never persisted.
    public sealed class CompanionOrderRecord
    {
        public long OwnerId;
        public long LeaderId;
        public CompanionOrder Order;
        public float X, Y, Z;
    }

    public static class CompanionPolicy
    {
        public const string RecordKey = "livingworld_companion_order_record";
        public const string Schema = "human-orders-v1";
        public const float FollowResumeRange = 64f;
        // Souls of the Land enemy definitions use a 30m view range. Companion
        // awareness and committed-threat acquisition use the same envelope;
        // movement remains constrained independently by NpcLeash.
        public const float AwarenessRange = 30f;
        public const float TargetLeash = 12f;
        public const float NpcLeash = 14f;
        public const float PursuitSeconds = 20f;
        public const float UnsensedSeconds = 3f;
        public const float NearbyThreatRange = 6f;
        public const float RecentDamageSeconds = 5f;
        public const float TacticalNpcLeash = 10f;
        // A committed fire team may break formation, but it may not become two
        // independent 30m hunts on opposite sides of the leader. Crossing either
        // boundary finishes only the immediate fight, then returns to the party.
        public const float CombatAnchorLeash = 18f;
        public const float CombatPairLeash = 18f;
        public const float RecoveryDefenseRange = 8f;
        public const float RecoveryDefenseSeconds = 8f;

        public static bool Finite(float value) { return !Single.IsNaN(value) && !Single.IsInfinity(value); }

        public static bool Valid(CompanionOrderRecord record)
        {
            return record != null && record.OwnerId != 0 &&
                ((record.Order == CompanionOrder.Hold && record.LeaderId == 0) ||
                 (record.Order == CompanionOrder.Follow && record.LeaderId == record.OwnerId)) &&
                Finite(record.X) && Finite(record.Y) && Finite(record.Z) &&
                Math.Abs(record.X) <= 1000000f && Math.Abs(record.Y) <= 1000000f && Math.Abs(record.Z) <= 1000000f;
        }

        public static string Write(CompanionOrderRecord record)
        {
            if (!Valid(record)) throw new ArgumentException("Invalid companion order record.");
            return String.Join("|", new[] { Schema,
                record.OwnerId.ToString(CultureInfo.InvariantCulture), record.LeaderId.ToString(CultureInfo.InvariantCulture),
                record.Order == CompanionOrder.Follow ? "follow" : "hold",
                record.X.ToString("R", CultureInfo.InvariantCulture), record.Y.ToString("R", CultureInfo.InvariantCulture),
                record.Z.ToString("R", CultureInfo.InvariantCulture) });
        }

        public static bool TryRead(string text, out CompanionOrderRecord record)
        {
            record = null;
            if (String.IsNullOrEmpty(text) || text.Length > 256) return false;
            string[] fields = text.Split('|');
            if (fields.Length != 7 || fields[0] != Schema || (fields[3] != "hold" && fields[3] != "follow")) return false;
            var candidate = new CompanionOrderRecord();
            if (!Int64.TryParse(fields[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out candidate.OwnerId) ||
                !Int64.TryParse(fields[2], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out candidate.LeaderId) ||
                !Single.TryParse(fields[4], NumberStyles.Float, CultureInfo.InvariantCulture, out candidate.X) ||
                !Single.TryParse(fields[5], NumberStyles.Float, CultureInfo.InvariantCulture, out candidate.Y) ||
                !Single.TryParse(fields[6], NumberStyles.Float, CultureInfo.InvariantCulture, out candidate.Z)) return false;
            candidate.Order = fields[3] == "follow" ? CompanionOrder.Follow : CompanionOrder.Hold;
            if (!Valid(candidate)) return false;
            record = candidate; return true;
        }

        public static bool MayWrite(bool validView, bool owner, bool compositionReady)
        { return validView && owner && compositionReady; }

        public static bool MayIssue(CompanionOrderRecord record, long playerId, bool validView, bool owner, bool ready)
        { return MayWrite(validView, owner, ready) && Valid(record) && record.OwnerId == playerId; }

        public static CompanionMotion Motion(CompanionOrderRecord record, bool leaderPresent, bool leaderAlive, float distance)
        {
            if (!Valid(record)) return CompanionMotion.Invalid;
            if (record.Order == CompanionOrder.Hold) return CompanionMotion.Hold;
            // Separation is a recovery condition, not permission to replace a
            // persistent Follow order with "wait here". Combat has its own
            // tight leashes; a living connected leader must always be pursued.
            if (!leaderPresent || !leaderAlive || !Finite(distance) || distance < 0)
                return CompanionMotion.WaitForLeader;
            return CompanionMotion.Follow;
        }

        public static bool MustClearPatrol(CompanionMotion previous, CompanionMotion next, bool sameFollowTarget)
        { return next == CompanionMotion.Follow && (previous != CompanionMotion.Follow || !sameFollowTarget); }

        public static bool MustCaptureWaitAnchor(CompanionMotion previous, CompanionMotion next, bool haveAnchor)
        { return next == CompanionMotion.WaitForLeader && (previous != next || !haveAnchor); }

        public static bool MayProtectOwner(bool living, CompanionMotion motion, float ownerFromAnchor)
        {
            return living && (motion == CompanionMotion.Hold || motion == CompanionMotion.Follow) &&
                Finite(ownerFromAnchor) && ownerFromAnchor >= 0 && ownerFromAnchor <= TargetLeash;
        }

        // Explicit orders and party-defense signals may hand a target directly
        // to native combat. Ordinary acquisition is owned by MonsterAI and is
        // validated after selection by MayAdoptNativeSelection below.
        public static bool MayAuthorize(bool ownerReady, bool alive, bool self, bool player, bool tame,
            bool companion, bool nativeEnemy, bool explicitOrder, float attributedDamage, bool activeThreat)
        {
            return ownerReady && alive && !self && !player && !tame && !companion && nativeEnemy &&
                (explicitOrder || activeThreat || (Finite(attributedDamage) && attributedDamage > 0f));
        }

        public static bool HostileCommitted(bool targetsProtectedParty, bool sensedEnemy, bool alerted)
        {
            // Sensing and the native alert flag feed visible awareness. They do
            // not by themselves authorize lethal combat. A hostile commits when
            // native MonsterAI has selected this protected party as its victim;
            // that selection occurs before the first hit. Attributed damage is
            // an independent authorization path in MayAuthorize.
            return targetsProtectedParty;
        }

        public static bool MayAdoptNativeSelection(bool stealthHold, bool activeThreat, bool passiveWildlife, bool localDefense,
            float targetFromNpc, float targetFromAnchor, float npcFromAnchor)
        {
            if (!Finite(targetFromNpc) || targetFromNpc < 0f || targetFromNpc > AwarenessRange ||
                !Finite(targetFromAnchor) || targetFromAnchor < 0f ||
                !Finite(npcFromAnchor) || npcFromAnchor < 0f) return false;
            if (!localDefense && (targetFromAnchor > AwarenessRange || npcFromAnchor > NpcLeash)) return false;
            if (passiveWildlife && !activeThreat) return false;
            return !stealthHold || activeThreat;
        }

        public static bool RefreshPursuit(bool activeThreat, float secondsSinceDamage, float distance)
        {
            return Finite(distance) && distance >= 0 && distance <= NearbyThreatRange &&
                (activeThreat || (Finite(secondsSinceDamage) && secondsSinceDamage >= 0 && secondsSinceDamage <= RecentDamageSeconds));
        }

        // An authorized, still-sensed threat keeps the engagement alive across
        // ordinary weapon changes and native-AI attack intervals.  The smaller
        // NearbyThreatRange remains the fallback for recent attributed damage;
        // it must not be the only way a ranged fight can remain authorized.
        public static bool RefreshEngagement(bool activeThreat, bool sensed, float secondsSinceDamage, float distance)
        {
            if (!Finite(distance) || distance < 0 || distance > AwarenessRange) return false;
            if (activeThreat && sensed) return true;
            return distance <= NearbyThreatRange && Finite(secondsSinceDamage) &&
                secondsSinceDamage >= 0 && secondsSinceDamage <= RecentDamageSeconds;
        }

        public static bool MayReacquireRetired(bool sameTarget, bool explicitOrder, bool nearbyThreat)
        { return !sameTarget || explicitOrder || nearbyThreat; }

        // Separation must never turn an attacked companion into a defenseless
        // follower.  This is intentionally local: it permits fighting the actor
        // already attacking this NPC, but never authorizes a new distant chase.
        public static bool MayDefendWhileSeparated(float attributedDamage, bool targetsSelf, float targetDistance)
        {
            return Finite(targetDistance) && targetDistance >= 0f && targetDistance <= RecoveryDefenseRange &&
                (targetsSelf || (Finite(attributedDamage) && attributedDamage > 0f));
        }

        public static bool MustEnterCombatRecovery(float npcFromAnchor, bool livingPartyMember,
            float nearestPartyMember)
        {
            if (!Finite(npcFromAnchor) || npcFromAnchor < 0f) return true;
            if (npcFromAnchor > CombatAnchorLeash) return true;
            if (!livingPartyMember) return false;
            return !Finite(nearestPartyMember) || nearestPartyMember < 0f ||
                nearestPartyMember > CombatPairLeash;
        }

        public static bool EncounterThreat(bool encounterOwned, bool enemy, bool alerted,
            bool sensed, bool passiveWildlife)
        {
            // Alert alone never begins a peaceful fight. Once this party already
            // owns a real encounter, however, an alerted sensed hostile is part
            // of that encounter even during native target handoff gaps.
            return encounterOwned && enemy && alerted && sensed && !passiveWildlife;
        }

        public static bool WithinRecoveryDefenseBounds(float elapsed, float unsensed,
            float targetFromOrigin, float npcFromOrigin, float targetFromNpc)
        {
            return InRange(elapsed, 0, RecoveryDefenseSeconds, false) &&
                InRange(unsensed, 0, UnsensedSeconds, false) &&
                InRange(targetFromOrigin, 0, RecoveryDefenseRange, true) &&
                InRange(npcFromOrigin, 0, RecoveryDefenseRange, true) &&
                InRange(targetFromNpc, 0, RecoveryDefenseRange, true);
        }

        public static float ThreatPriority(bool targetsLeader, bool targetsSelf, bool targetsParty,
            float targetFromAnchor, float targetFromNpc)
        {
            if ((!targetsLeader && !targetsSelf && !targetsParty) || !Finite(targetFromAnchor) || targetFromAnchor < 0 ||
                !Finite(targetFromNpc) || targetFromNpc < 0) return Single.NegativeInfinity;
            // Protecting the leader is the defining companion duty. Distance
            // breaks ties but never lets a nearby distraction outrank an enemy
            // actively attacking the leader or another party member.
            float duty = targetsLeader ? 3000f : targetsSelf ? 2000f : 1000f;
            return duty - targetFromAnchor * 8f - targetFromNpc;
        }

        public static bool ShouldSwitchThreat(float currentPriority, float candidatePriority)
        {
            return Finite(candidatePriority) && (!Finite(currentPriority) || candidatePriority > currentPriority + 100f);
        }

        public static bool SafeNativeAttack(bool hitFriendly, bool killsSelf, int selfDamage)
        { return !hitFriendly && !killsSelf && selfDamage == 0; }

        public static bool WithinCombatBounds(float elapsed, float unsensed, float targetFromOrigin,
            float npcFromOrigin, float targetFromOrder, float npcFromOrder)
        {
            return InRange(elapsed, 0, PursuitSeconds, false) && InRange(unsensed, 0, UnsensedSeconds, false) &&
                InRange(targetFromOrigin, 0, TargetLeash, true) && InRange(npcFromOrigin, 0, NpcLeash, true) &&
                InRange(targetFromOrder, 0, TargetLeash, true) && InRange(npcFromOrder, 0, NpcLeash, true);
        }

        public static bool WithinRangedCombatBounds(float elapsed, float unsensed, float targetFromOrigin,
            float npcFromOrigin, float targetFromOrder, float npcFromOrder)
        {
            // Seeing/shooting farther does not authorize the actor to roam farther.
            return InRange(elapsed, 0, PursuitSeconds, false) && InRange(unsensed, 0, UnsensedSeconds, false) &&
                InRange(targetFromOrigin, 0, 24f, true) && InRange(npcFromOrigin, 0, NpcLeash, true) &&
                InRange(targetFromOrder, 0, 24f, true) && InRange(npcFromOrder, 0, NpcLeash, true);
        }

        public static bool WithinTacticalCombatBounds(bool ranged, float elapsed, float unsensed,
            float targetFromOrigin, float npcFromOrigin, float targetFromOrder, float npcFromOrder)
        {
            float targetLeash = AwarenessRange;
            return InRange(elapsed, 0, 8f, false) && InRange(unsensed, 0, UnsensedSeconds, false) &&
                InRange(targetFromOrigin, 0, targetLeash, true) && InRange(npcFromOrigin, 0, TacticalNpcLeash, true) &&
                InRange(targetFromOrder, 0, targetLeash, true) && InRange(npcFromOrder, 0, TacticalNpcLeash, true);
        }

        // Once combat is committed, its target must not be revoked by the much
        // tighter formation station. These bounds are an encounter escape fence,
        // not a per-frame tactics vote: sensing/contact renews the clocks while
        // native MonsterAI owns the actual pursuit and attack.
        public static bool WithinCommittedCombatBounds(float elapsed, float unsensed,
            float targetFromOrigin, float npcFromOrigin, float targetFromOrder,
            float npcFromOrder, float targetFromNpc)
        {
            return InRange(elapsed, 0, 12f, false) && InRange(unsensed, 0, 8f, false) &&
                InRange(targetFromOrigin, 0, 45f, true) && InRange(npcFromOrigin, 0, 45f, true) &&
                InRange(targetFromOrder, 0, 45f, true) && InRange(npcFromOrder, 0, AwarenessRange, true) &&
                InRange(targetFromNpc, 0, AwarenessRange, true);
        }

        static bool InRange(float value, float minimum, float maximum, bool inclusive)
        { return Finite(value) && value >= minimum && (inclusive ? value <= maximum : value < maximum); }
    }
}

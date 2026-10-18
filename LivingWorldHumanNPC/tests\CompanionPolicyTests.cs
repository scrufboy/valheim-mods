using System;
using System.Globalization;
using LivingWorld.HumanNpc;

class CompanionPolicyTests
{
    static int checks;
    static void Check(bool condition, string label)
    { checks++; if (!condition) throw new Exception("FAIL " + label); }
    static CompanionOrderRecord Hold()
    { return new CompanionOrderRecord { OwnerId = 123456789012345, Order = CompanionOrder.Hold, X = 1.25f, Y = 42.125f, Z = -121.4f }; }
    static CompanionOrderRecord Follow()
    { var order = Hold(); order.Order = CompanionOrder.Follow; order.LeaderId = order.OwnerId; return order; }
    static bool Parse(string text) { CompanionOrderRecord result; return CompanionPolicy.TryRead(text, out result); }
    static bool Allow(bool ready = true, bool alive = true, bool self = false, bool player = false,
        bool tame = false, bool companion = false, bool enemy = true, bool selected = true, float damage = 0f, bool activeThreat = false)
    { return CompanionPolicy.MayAuthorize(ready, alive, self, player, tame, companion, enemy, selected, damage, activeThreat); }
    static bool Bounds(float elapsed = 1, float unsensed = 0, float targetOrigin = 2, float npcOrigin = 1, float targetOrder = 3, float npcOrder = 1)
    { return CompanionPolicy.WithinCombatBounds(elapsed, unsensed, targetOrigin, npcOrigin, targetOrder, npcOrder); }

        static void Main()
        {
            float leaderThreat = CompanionPolicy.ThreatPriority(true, false, false, 10f, 9f);
            float selfThreat = CompanionPolicy.ThreatPriority(false, true, false, 1f, 1f);
            float partyThreat = CompanionPolicy.ThreatPriority(false, false, true, 1f, 1f);
            Check(leaderThreat > selfThreat && selfThreat > partyThreat, "duty priority outranks proximity");
            Check(CompanionPolicy.ShouldSwitchThreat(selfThreat, leaderThreat), "switches to leader threat");
            Check(!CompanionPolicy.ShouldSwitchThreat(leaderThreat, selfThreat), "does not abandon leader for nearby distraction");
            Check(Single.IsNegativeInfinity(CompanionPolicy.ThreatPriority(false, false, false, 1f, 1f)), "non-threat has no priority");
        Check(CompanionPolicy.Valid(Hold()), "valid hold");
        Check(CompanionPolicy.Valid(Follow()), "valid follow");
        Check(!CompanionPolicy.Valid(null), "null invalid");
        var invalid = Hold(); invalid.OwnerId = 0; Check(!CompanionPolicy.Valid(invalid), "zero owner invalid");
        invalid = Hold(); invalid.OwnerId = -1; Check(CompanionPolicy.Valid(invalid), "negative native owner valid");
        Check(Parse(CompanionPolicy.Write(invalid)), "negative owner roundtrip");
        invalid = Follow(); invalid.OwnerId = invalid.LeaderId = Int64.MinValue;
        Check(Parse(CompanionPolicy.Write(invalid)), "minimum signed owner and leader roundtrip");
        invalid = Hold(); invalid.LeaderId = 99; Check(!CompanionPolicy.Valid(invalid), "hold may not have leader");
        invalid = Follow(); invalid.LeaderId = 99; Check(!CompanionPolicy.Valid(invalid), "follow bound leader mismatch");
        invalid = Follow(); invalid.Order = (CompanionOrder)999; Check(!CompanionPolicy.Valid(invalid), "unknown order invalid");
        invalid = Hold(); invalid.X = Single.NaN; Check(!CompanionPolicy.Valid(invalid), "nan hold point");
        invalid = Hold(); invalid.Y = Single.PositiveInfinity; Check(!CompanionPolicy.Valid(invalid), "infinite hold point");
        invalid = Hold(); invalid.Z = 1000001; Check(!CompanionPolicy.Valid(invalid), "extreme hold point");
        bool threw = false; try { CompanionPolicy.Write(invalid); } catch (ArgumentException) { threw = true; }
        Check(threw, "cannot serialize invalid record");
        string encoded = CompanionPolicy.Write(Follow()); CompanionOrderRecord restored;
        Check(CompanionPolicy.TryRead(encoded, out restored), "roundtrip follow");
        Check(restored.OwnerId == Follow().OwnerId && restored.LeaderId == Follow().LeaderId && restored.Order == CompanionOrder.Follow, "stable identity roundtrip");
        Check(restored.X == Follow().X && restored.Y == Follow().Y && restored.Z == Follow().Z, "coordinates roundtrip");
        Check(Parse(CompanionPolicy.Write(Hold())), "roundtrip hold");
        string valid = CompanionPolicy.Write(Hold());
        Check(!Parse(null), "null saved record"); Check(!Parse(""), "missing record fails closed");
        Check(!Parse(valid.Replace("human-orders-v1", "human-orders-v2")), "future schema fails closed");
        Check(!Parse(valid.Replace("human-orders-v1", "")), "missing schema fails closed");
        Check(!Parse(valid.Replace("hold", "guard")), "unknown saved order");
        Check(!Parse(valid + "|extra"), "extra field rejected");
        Check(!Parse("human-orders-v1|1|0|hold|1|2"), "missing field rejected");
        Check(!Parse("human-orders-v1|0|0|hold|1|2|3"), "saved zero owner rejected");
        Check(!Parse("human-orders-v1|1|2|follow|1|2|3"), "saved foreign leader rejected");
        Check(!Parse("human-orders-v1|1|0|follow|1|2|3"), "saved absent follow leader rejected");
        Check(!Parse("human-orders-v1|1|0|hold|NaN|2|3"), "saved nan rejected");
        Check(!Parse("human-orders-v1|1|0|hold|Infinity|2|3"), "saved infinity rejected");
        Check(!Parse("human-orders-v1|1|0|hold|1,5|2|3"), "localized number rejected");
        Check(!Parse(new String('x', 257)), "oversized record rejected");
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Check(CompanionPolicy.Write(Follow()) == encoded, "serialization culture independent");
            Check(CompanionPolicy.TryRead(encoded, out restored), "parse culture independent");
        }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture = previousCulture; }

        Check(CompanionPolicy.MayWrite(true, true, true), "owner composition write");
        Check(!CompanionPolicy.MayWrite(false, true, true), "invalid view never writes");
        Check(!CompanionPolicy.MayWrite(true, false, true), "remote never writes");
        Check(!CompanionPolicy.MayWrite(true, true, false), "unmigrated composition never writes");
        Check(CompanionPolicy.MayIssue(Follow(), Follow().OwnerId, true, true, true), "bound owner may issue");
        Check(!CompanionPolicy.MayIssue(Follow(), 4, true, true, true), "other player may not issue");
        Check(!CompanionPolicy.MayIssue(Hold(), 4444, true, true, true), "recreated same-name profile with changed stable ID cannot command existing hold");
        var signedOwner = Follow(); signedOwner.OwnerId = signedOwner.LeaderId = -500;
        Check(CompanionPolicy.MayIssue(signedOwner, -500, true, true, true), "bound negative native owner may command");
        Check(!CompanionPolicy.MayIssue(Follow(), Follow().OwnerId, true, false, true), "nonowner may not issue");
        Check(!CompanionPolicy.MayIssue(Follow(), Follow().OwnerId, true, true, false), "uninitialized may not issue");
        Check(!CompanionPolicy.MayIssue(null, 1, true, true, true), "invalid order may not issue");
        Check(CompanionPolicy.Motion(Hold(), false, false, Single.PositiveInfinity) == CompanionMotion.Hold, "hold does not require runtime player");
        Check(CompanionPolicy.Motion(Follow(), true, true, 4) == CompanionMotion.Follow, "living nearby leader resumes");
        Check(CompanionPolicy.Motion(restored, true, true, 4) == CompanionMotion.Follow, "serialized follow resumes after simulated reload");
        Check(CompanionPolicy.Motion(Follow(), false, false, 4) == CompanionMotion.WaitForLeader, "disconnected leader waits");
        Check(CompanionPolicy.Motion(Follow(), true, false, 4) == CompanionMotion.WaitForLeader, "dead leader waits");
        Check(CompanionPolicy.Motion(Follow(), true, true, 64) == CompanionMotion.Follow, "resume range boundary");
        Check(CompanionPolicy.Motion(Follow(), true, true, 64.01f) == CompanionMotion.Follow, "separated companion keeps recovering toward living leader");
        Check(CompanionPolicy.Motion(Follow(), true, true, 185f) == CompanionMotion.Follow, "telemetry-scale separation cannot strand a follower");
        Check(CompanionPolicy.Motion(Follow(), true, true, Single.NaN) == CompanionMotion.WaitForLeader, "invalid range waits");
        Check(CompanionPolicy.Motion(null, true, true, 4) == CompanionMotion.Invalid, "invalid motion fails closed");
        Check(Parse(encoded), "waiting does not mutate saved follow");
        Check(CompanionPolicy.MustClearPatrol(CompanionMotion.Hold, CompanionMotion.Follow, true), "hold to follow clears native patrol");
        Check(CompanionPolicy.MustClearPatrol(CompanionMotion.WaitForLeader, CompanionMotion.Follow, true), "respawn reconnect clears wait patrol");
        Check(CompanionPolicy.MustClearPatrol(CompanionMotion.Follow, CompanionMotion.Follow, false), "new runtime leader clears patrol");
        Check(!CompanionPolicy.MustClearPatrol(CompanionMotion.Follow, CompanionMotion.Follow, true), "steady follow avoids repeated patrol writes");
        Check(CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.Follow, CompanionMotion.WaitForLeader, false), "leader death captures current wait position");
        Check(CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.Invalid, CompanionMotion.WaitForLeader, false), "offline reload captures current wait position");
        Check(!CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.WaitForLeader, CompanionMotion.WaitForLeader, true), "waiting anchor does not drift every tick");
        Check(!CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.Hold, CompanionMotion.Hold, false), "hold keeps saved hold anchor");
        Check(CompanionPolicy.MayProtectOwner(true, CompanionMotion.Hold, 4), "hold defends nearby owner without following");
        Check(CompanionPolicy.MayProtectOwner(true, CompanionMotion.Hold, 12), "hold owner protection boundary");
        Check(!CompanionPolicy.MayProtectOwner(true, CompanionMotion.Hold, 12.01f), "hold does not chase threats to distant owner");
        Check(CompanionPolicy.MayProtectOwner(true, CompanionMotion.Follow, 0), "follow protects its living owner anchor");
        Check(!CompanionPolicy.MayProtectOwner(false, CompanionMotion.Hold, 0), "dead owner not protected target");
        Check(!CompanionPolicy.MayProtectOwner(true, CompanionMotion.WaitForLeader, 0), "waiting protects self only");
        Check(!CompanionPolicy.MayProtectOwner(true, CompanionMotion.Hold, Single.NaN), "invalid owner location fails closed");

        Check(Allow(), "explicit hostile target accepted");
        Check(!Allow(ready: false), "unready combat denied");
        Check(!Allow(alive: false), "dead target denied");
        Check(!Allow(self: true), "self attack denied");
        Check(!Allow(player: true), "player attack denied even explicit");
        Check(!Allow(tame: true), "tamed attack denied");
        Check(!Allow(companion: true), "other companion attack denied");
        Check(!Allow(enemy: false), "friendly explicit target denied");
        Check(!Allow(selected: false), "unprovoked wildlife denied despite enemy faction");
        Check(!Allow(selected: false, damage: -1), "negative damage not provocation");
        Check(!Allow(selected: false, damage: Single.NaN), "nan damage not provocation");
        Check(!Allow(selected: false, damage: Single.PositiveInfinity), "infinite damage not provocation");
        Check(Allow(selected: false, damage: 1), "positive attributed hostile damage permits defense");
        Check(!Allow(selected: false, damage: 1, enemy: false), "friendly damage does not authorize retaliation");
        Check(!Allow(selected: false, damage: 1, player: true), "player damage does not authorize retaliation");
        Check(Allow(selected: false, activeThreat: true), "active hostile targeting protected character permits defense before first hit");
        Check(!Allow(selected: false, enemy: false, activeThreat: true), "friendly target selection is not hostile threat");
        Check(!Allow(selected: false, tame: true, activeThreat: true), "tame native target never acquired");
        Check(Allow(selected: false, damage: 1), "no universal cooldown suppresses current attack defense");
        Check(CompanionPolicy.RefreshPursuit(true, Single.PositiveInfinity, 6), "nearby active threat refreshes pursuit without damage");
        Check(CompanionPolicy.RefreshPursuit(false, 5, 6), "nearby recent positive damage refreshes pursuit");
        Check(!CompanionPolicy.RefreshPursuit(false, 5.01f, 6), "stale damage cannot extend pursuit");
        Check(!CompanionPolicy.RefreshPursuit(true, 0, 6.01f), "distant threat does not renew pursuit timer");
        Check(!CompanionPolicy.RefreshPursuit(false, Single.PositiveInfinity, 1), "passive nearby wildlife does not renew pursuit");
        Check(!CompanionPolicy.RefreshPursuit(true, 0, Single.NaN), "invalid nearby distance denied");
        Check(CompanionPolicy.RefreshEngagement(true, true, 99, 24), "sensed active ranged threat renews engagement");
        Check(!CompanionPolicy.RefreshEngagement(true, false, 99, 24), "unsensed distant threat does not renew engagement");
        Check(CompanionPolicy.RefreshEngagement(false, false, 2, 5), "recent close damage renews engagement");
        Check(CompanionPolicy.MayReacquireRetired(false, false, false), "new real threat immediately eligible");
        Check(!CompanionPolicy.MayReacquireRetired(true, false, false), "expired distant chase cannot instantly restart");
        Check(CompanionPolicy.MayReacquireRetired(true, false, true), "retired target returning to nearby active melee is defendable");
        Check(CompanionPolicy.MayReacquireRetired(true, true, false), "explicit order may renew bounded chase");
        Check(CompanionPolicy.SafeNativeAttack(false, false, 0), "safe ordinary native melee permitted");
        Check(!CompanionPolicy.SafeNativeAttack(true, false, 0), "friendly-hit override cannot bypass target discrimination");
        Check(!CompanionPolicy.SafeNativeAttack(false, true, 0), "native self-killing attack denied");
        Check(!CompanionPolicy.SafeNativeAttack(false, false, 1), "native self-damage attack denied");
        Check(!CompanionPolicy.SafeNativeAttack(false, false, -1), "unexpected negative self-damage denied");
        Check(Bounds(), "bounded combat allowed");
        Check(Bounds(elapsed: 19.99f), "before pursuit deadline");
        Check(!Bounds(elapsed: 20), "at pursuit deadline retires distant chase");
        Check(Bounds(elapsed: 0), "ongoing nearby fight refreshing pursuit has no absolute combat deadline");
        Check(!Bounds(elapsed: -1), "clock rollback fails closed");
        Check(!Bounds(elapsed: Single.NaN), "invalid clock fails closed");
        Check(Bounds(unsensed: 2.99f), "before sight loss deadline");
        Check(!Bounds(unsensed: 3), "sight hearing loss bound");
        Check(Bounds(targetOrigin: 12, npcOrigin: 14, targetOrder: 12, npcOrder: 14), "inclusive spatial leash boundaries");
        Check(!Bounds(targetOrigin: 12.01f), "target cannot extend engagement origin leash");
        Check(!Bounds(npcOrigin: 14.01f), "npc cannot overrun origin leash");
        Check(!Bounds(targetOrder: 12.01f), "target cannot drag npc away from moving leader");
        Check(!Bounds(npcOrder: 14.01f), "npc cannot leave active hold or leader anchor");
        Check(!Bounds(targetOrder: Single.NaN), "nan distance fails closed");
        Check(!Bounds(npcOrigin: Single.PositiveInfinity), "infinite distance fails closed");
        Check(!Bounds(targetOrigin: -1), "negative distance fails closed");
        Check(CompanionPolicy.WithinRangedCombatBounds(1, 0, 24, 14, 24, 14), "ranged target extension retains NPC tether");
        Check(!CompanionPolicy.WithinRangedCombatBounds(1, 0, 24, 14.01f, 24, 14), "ranged actor cannot chase farther");
        Check(CompanionPolicy.WithinTacticalCombatBounds(false, 7.99f, 0, 12, 10, 12, 10), "tactical melee boundary accepts its edge");
        Check(!CompanionPolicy.WithinTacticalCombatBounds(false, 8f, 0, 12, 10, 12, 10), "tactical pursuit expires at eight seconds");
        Check(!CompanionPolicy.WithinTacticalCombatBounds(true, 1, 0, 24, 10.01f, 24, 10), "tactical ranged role cannot abandon station");
        Check(!CompanionPolicy.WithinRangedCombatBounds(1, 0, 24.01f, 0, 24, 0), "ranged target remains bounded");
        Check(!CompanionPolicy.WithinRangedCombatBounds(1, 3, 20, 0, 20, 0), "ranged sight loss unchanged");
        Check(!CompanionPolicy.WithinRangedCombatBounds(1, 0, Single.NaN, 0, 20, 0), "ranged invalid observation fails closed");
        Console.WriteLine("PASS " + checks + " companion policy checks (pure offline; Unity execution NOT RUN)");
    }
}


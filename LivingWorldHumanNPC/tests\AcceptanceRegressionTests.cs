using System;
using LivingWorld.HumanNpc;

// Independent product acceptance checks for the revised 0.3.0 draft.
// Does not execute Unity and does not certify the NPC root or animation.
static class AcceptanceRegressionTests
{
    static int passed, failed;
    static void Check(string name, bool ok)
    {
        if (ok) { passed++; Console.WriteLine("PASS " + name); }
        else { failed++; Console.WriteLine("FAIL " + name); }
    }
    static int Main()
    {
        var order = new CompanionOrderRecord {
            OwnerId = 54321, LeaderId = 54321, Order = CompanionOrder.Follow,
            X = 12, Y = 46, Z = -30
        };
        string saved = CompanionPolicy.Write(order);
        CompanionOrderRecord restored;
        Check("Saved follow has stable owner and leader identity",
            CompanionPolicy.TryRead(saved, out restored) && restored.OwnerId == 54321 &&
            restored.LeaderId == 54321 && restored.Order == CompanionOrder.Follow);
        Check("Restart with nearby living owner resolves to follow",
            CompanionPolicy.Motion(restored, true, true, 185) == CompanionMotion.Follow);
        Check("Dead leader causes waiting",
            CompanionPolicy.Motion(restored, true, false, 5) == CompanionMotion.WaitForLeader);
        Check("Leader death does not overwrite the saved order",
            CompanionPolicy.Write(restored) == saved);
        Check("Absent leader causes waiting",
            CompanionPolicy.Motion(restored, false, false, float.PositiveInfinity) == CompanionMotion.WaitForLeader);
        Check("Remote authority cannot issue an order",
            !CompanionPolicy.MayIssue(restored, 54321, true, false, true));
        Check("Another player cannot take over this companion",
            !CompanionPolicy.MayIssue(restored, 123, true, true, true));
        Check("Corrupt order is rejected",
            !CompanionPolicy.TryRead(saved + "|extra", out restored));
        Check("Players are never authorized as combat victims",
            !CompanionPolicy.MayAuthorize(true, true, false, true, false, false, true, true, 10, false));
        Check("Tamed animals are never authorized as combat victims",
            !CompanionPolicy.MayAuthorize(true, true, false, false, true, false, true, true, 10, false));
        float refreshedAt = 0;
        bool ongoingDefense = true;
        for (int second = 0; second < 120; second++) {
            if (CompanionPolicy.RefreshPursuit(true, float.PositiveInfinity, 2)) refreshedAt = second;
            ongoingDefense &= CompanionPolicy.WithinCombatBounds(second - refreshedAt, 0, 2, 2, 2, 2);
        }
        Check("Nearby ongoing threat refresh permits sustained defense", ongoingDefense);
        Check("Fresh damage permits immediate defense without a universal cooldown",
            CompanionPolicy.MayAuthorize(true, true, false, false, false, false, true, false, 10, false));
        Check("Active hostile threat can be engaged before its first damaging hit",
            CompanionPolicy.MayAuthorize(true, true, false, false, false, false, true, false, 0, true));
        Check("Unprovoked wildlife is not automatically authorized",
            !CompanionPolicy.MayAuthorize(true, true, false, false, false, false, true, false, 0, false));
        Check("Retired chase is not automatically reacquired",
            !CompanionPolicy.MayReacquireRetired(true, false, false));
        Check("Retired enemy returning to threaten nearby can be defended against",
            CompanionPolicy.MayReacquireRetired(true, false, true));
        Check("Hold to follow clears the old patrol point",
            CompanionPolicy.MustClearPatrol(CompanionMotion.Hold, CompanionMotion.Follow, false));
        Check("Unchanged following does not continuously clear its route",
            !CompanionPolicy.MustClearPatrol(CompanionMotion.Follow, CompanionMotion.Follow, true));
        Check("Death wait captures a current location",
            CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.Follow, CompanionMotion.WaitForLeader, false));
        Check("Waiting does not continuously relocate its anchor",
            !CompanionPolicy.MustCaptureWaitAnchor(CompanionMotion.WaitForLeader, CompanionMotion.WaitForLeader, true));
        Check("Distant pursuit is rejected",
            !CompanionPolicy.WithinCombatBounds(1, 0, 100, 2, 100, 2));

        var legacy = new HumanMigrationSnapshot(HumanMigrationPolicy.VisualRevision, "", "",
            HumanMigrationPolicy.Identity, HumanMigrationPolicy.NpcTag);
        var plan = HumanMigrationPolicy.PlanMigration(legacy, true, true, true);
        Check("Migration changes only two composition metadata keys", plan.Length == 2 &&
            plan[0].Key == HumanMigrationPolicy.OriginKey && plan[1].Key == HumanMigrationPolicy.CompositionKey);
        Check("An interrupted metadata upgrade remains retryable", HumanMigrationPolicy.MayMigrate(
            new HumanMigrationSnapshot(legacy.Visual, "", HumanMigrationPolicy.LegacyOrigin,
                legacy.Identity, legacy.LabTag), true, true, true));
        Check("Unknown saved revision is not silently reinterpreted", !HumanMigrationPolicy.MayMigrate(
            new HumanMigrationSnapshot("unknown", "", "", legacy.Identity, legacy.LabTag), true, true, true));
        Check("Remote owner cannot migrate saved actor", !HumanMigrationPolicy.MayMigrate(legacy, true, false, true));
        Console.WriteLine("Independent acceptance: " + passed + " passed; " + failed + " failed.");
        return failed == 0 ? 0 : 1;
    }
}

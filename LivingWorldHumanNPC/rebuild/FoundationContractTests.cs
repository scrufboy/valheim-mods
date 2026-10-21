using System;
using LivingWorld.HumanNpcV2;

internal static class FoundationContractTests
{
    static int failures;

    static void Check(bool condition, string message)
    {
        if (!condition) { failures++; Console.Error.WriteLine("FAIL: " + message); }
    }

    static void Identity()
    {
        var random = new Random(44721);
        bool male = false;
        bool female = false;
        for (int index = 0; index < 10000; index++)
        {
            HumanIdentityRecord identity = HumanIdentityRecord.Create(random, 20, 18);
            Check(!String.IsNullOrEmpty(identity.Name) && identity.Name.Contains(" "), "identity requires a full name");
            Check(identity.Hair.StartsWith("Hair", StringComparison.Ordinal), "every identity requires native hair");
            if (identity.Female)
            {
                female = true;
                Check(identity.Beard.Length == 0, "female identity cannot receive a beard");
            }
            else
            {
                male = true;
                Check(identity.Beard.StartsWith("Beard", StringComparison.Ordinal), "male identity requires a native beard");
            }
        }
        Check(male && female, "randomized population must exercise both model types");
    }

    static void DurableCombatState()
    {
        var latch = new CombatStateLatch(25);
        Check(!latch.Active && !latch.Observe(false), "calm begins calm");
        Check(latch.Observe(true) && latch.Active, "a committed threat enters combat immediately");
        for (int scan = 1; scan < 25; scan++)
        {
            Check(latch.Observe(false), "combat must survive incomplete clear verification " + scan);
            Check(latch.Active && latch.ClearScans == scan, "clear scan count must advance without leaving combat");
        }
        latch.Retain();
        Check(latch.Active && latch.ClearScans == 24, "a transient failed tick must retain the exact combat state");
        Check(!latch.Observe(false) && !latch.Active, "only the final verified clear scan may release combat");
        Check(latch.Observe(true), "combat can reacquire after release");
        for (int scan = 0; scan < 12; scan++) latch.Observe(false);
        Check(latch.Observe(true) && latch.ClearScans == 0, "a reacquired threat cancels accumulated clear evidence");
    }

    public static int Main()
    {
        Identity();
        DurableCombatState();
        if (failures != 0) return 1;
        Console.WriteLine("Foundation contracts PASS: randomized visible identity and durable combat state.");
        return 0;
    }
}

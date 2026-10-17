using System;
using System.Globalization;
using LivingWorld.HumanNpc;

static class HumanResourcePoolTests
{
    static int checks;
    static void Check(bool value, string message)
    { if (!value) throw new Exception(message); checks++; }
    static bool Near(double actual, double expected) { return Math.Abs(actual - expected) < .00001; }
    static int Main()
    {
        var p = new HumanResourcePool(50);
        Check(p.Current == 50 && p.Capacity == 50, "new capacity");
        Check(p.Have(49) && !p.Have(50), "native strictly greater boundary");
        Check(!p.Have(-1) && !p.Have(double.NaN), "invalid checks");
        Check(p.Use(20, 1, 1), "native debit");
        Check(p.Current == 30 && p.RecoveryDelay == 1, "one debit, saved delay");
        p.Advance(.25, 5, 1, 1, true);
        Check(p.Current == 30 && p.RecoveryDelay == .75, "delay before recovery");
        p.Advance(.25, 5, 1, 1, true); p.Advance(.25, 5, 1, 1, true);
        p.Advance(.25, 5, 1, 1, true);
        Check(p.Current == 30 && p.RecoveryDelay == 0, "delay expiry not full tick of free recovery");
        p.Advance(.25, 5, 1, 1, true);
        Check(Near(p.Current, 31.75), "native missing-fraction recovery curve");
        double saved = p.Current;
        p.Advance(.25, 5, 1, 1, false);
        Check(p.Current == saved, "attack prevents recovery");
        Check(!p.Advance(5, 5, 1, 1, true) && p.Current == saved, "hitch cannot become offline recovery");
        foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Check(!p.Use(bad, 1, 1) && p.Current == saved, "bad debit");
            Check(!p.Add(bad) && p.Current == saved, "bad add");
            Check(!p.Advance(.1, 5, 1, bad, true) && p.Current == saved, "bad recovery");
        }
        Check(!p.Use(1, -1, 1) && !p.Use(1, 1, 61), "invalid rate or delay");
        Check(!p.Use(0, 1, 1) && !p.Use(5, 0, 1), "zero spends do not reset recovery");
        p.Use(100, 1, 1);
        Check(p.Current == 0 && !p.Have(0), "no negative stamina or free exhausted block");
        p.Add(999);
        Check(p.Current == 50, "refund saturates");
        p.Use(5, 2, 1);
        Check(p.Current == 40, "world cost modifier applied once");
        p.Use(-5, 1, 1);
        Check(p.Current == 45, "native signed return path");
        p.Add(-10);
        Check(p.Current == 35 && p.RecoveryDelay == 1, "add path preserves delay");
        string record = p.Save();
        HumanResourcePool restored;
        Check(HumanResourcePool.TryRestore(record, 50, out restored) && restored.Save() == record, "exact reload; no refill");
        Check(!HumanResourcePool.TryRestore(record, 100, out restored), "changed capacity needs explicit migration");
        foreach (string invalid in new[] { "", "2|50|20|0", "1|50|51|0", "1|50|-1|0", "1|50|NaN|0", "1|50|20|-1", "1|50|20|61", "1|0|0|0", "1|50|20|0|ignored" })
            Check(!HumanResourcePool.TryRestore(invalid, 50, out restored), "malformed record rejected");
        var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
        try
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Check(p.Save() == record && HumanResourcePool.TryRestore(record, 50, out restored), "culture-independent record");
        }
        finally { System.Threading.Thread.CurrentThread.CurrentCulture = culture; }
        p.Use(1, 1, .1);
        double before = p.Current;
        p.Advance(.25, 5, 0, 1, true);
        Check(Near(p.Current, before + .75), "only post-delay portion recovers");
        p.Use(100, 1, 0);
        for (int i = 0; i < 100; i++) p.Advance(.25, 5, 1, 1, true);
        Check(p.Current == 50, "full pool reachable without overflow");
        Console.WriteLine("HumanResourcePool PASS: " + checks + " ledger checks.");
        return 0;
    }
}

using System;
using LWC.Formidable.Encounters;
public static class FactionScopeTests
{
    public static int Main()
    {
        string exact = SwampRaidFactionScope.EventName;
        if (SwampRaidFactionScope.RunOriginalModifier(exact)) throw new Exception("Exact owned event must bypass forced faction");
        string[] ordinary = {null, "", "army_theelder", "lwc_formidable_souls_swamp", "lwc_souls_pillagers",
            exact.ToUpperInvariant(), exact + "_other", " " + exact, exact + " ", "lwc_drowned_colossus_trial"};
        foreach (string other in ordinary)
            if (!SwampRaidFactionScope.RunOriginalModifier(other)) throw new Exception("Unrelated event modifier changed: " + other);
        Console.WriteLine("PASS: 11 exact event-scope checks; ordinary raids retain original modifier");
        return 0;
    }
}

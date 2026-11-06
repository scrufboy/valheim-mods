using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public static class AllegianceMatrix
    {
        static readonly Dictionary<string, Disposition> Rules = Build();

        public static Disposition Get(Allegiance observer, FactionIdentity target)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (target.Allegiance == Allegiance.OtherMod)
                return !String.IsNullOrEmpty(target.AdapterFactionId) && target.HasAuthoredDisposition
                    ? target.AuthoredDisposition : Disposition.Neutral;
            Disposition value;
            return Rules.TryGetValue(Key(observer, target.Allegiance), out value) ? value : Disposition.Neutral;
        }

        static Dictionary<string, Disposition> Build()
        {
            Dictionary<string, Disposition> r = new Dictionary<string, Disposition>(StringComparer.Ordinal);
            Friendly(r, Allegiance.Player, Allegiance.Hearthbound);
            Friendly(r, Allegiance.Player, Allegiance.Hearthless);
            Friendly(r, Allegiance.Hearthbound, Allegiance.Hearthless);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Player);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Hearthbound);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Hearthless);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Eitrherjar);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Fuling);
            HostileBoth(r, Allegiance.Pillager, Allegiance.Draugr);
            HostileBoth(r, Allegiance.Eitrherjar, Allegiance.Player);
            HostileBoth(r, Allegiance.Eitrherjar, Allegiance.Hearthbound);
            HostileBoth(r, Allegiance.Eitrherjar, Allegiance.Hearthless);
            HostileBoth(r, Allegiance.Eitrherjar, Allegiance.Fuling);
            HostileBoth(r, Allegiance.Eitrherjar, Allegiance.Draugr);
            HostileBoth(r, Allegiance.Fuling, Allegiance.Player);
            HostileBoth(r, Allegiance.Fuling, Allegiance.Hearthbound);
            HostileBoth(r, Allegiance.Fuling, Allegiance.Hearthless);
            HostileBoth(r, Allegiance.Draugr, Allegiance.Player);
            HostileBoth(r, Allegiance.Draugr, Allegiance.Hearthbound);
            HostileBoth(r, Allegiance.Draugr, Allegiance.Hearthless);
            return r;
        }

        static void Friendly(Dictionary<string, Disposition> r, Allegiance a, Allegiance b)
        { r[Key(a, b)] = Disposition.Friendly; r[Key(b, a)] = Disposition.Friendly; }
        static void HostileBoth(Dictionary<string, Disposition> r, Allegiance a, Allegiance b)
        { r[Key(a, b)] = Disposition.Hostile; r[Key(b, a)] = Disposition.Hostile; }
        static string Key(Allegiance a, Allegiance b) { return a.ToString() + ">" + b.ToString(); }
    }
}

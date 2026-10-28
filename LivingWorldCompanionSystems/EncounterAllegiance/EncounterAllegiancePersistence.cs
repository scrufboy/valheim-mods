using System;
using System.Collections.Generic;
using System.Globalization;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    public static class EncounterAllegiancePersistence
    {
        public const string RecordKeyPrefix = "lwc_encounter_allegiance_v1_";

        public static string Write(PersonContactRecord value)
        {
            if (!Valid(value)) throw new ArgumentException("Invalid contact record", "value");
            List<string> facts = new List<string>(value.AppliedFactIds); facts.Sort(StringComparer.Ordinal);
            for (int i = 0; i < facts.Count; i++) facts[i] = Uri.EscapeDataString(facts[i]);
            return String.Join("|", new[] { EncounterAllegiancePolicy.Schema, Esc(value.PersonId), Esc(value.CampId),
                value.Allegiance.ToString(), value.Readiness.ToString(), value.Relationship.ToString(),
                value.PersonNoticedPlayer ? "1" : "0", value.PlayerNoticedPerson ? "1" : "0",
                value.IdentityRevealed ? "1" : "0", Esc(value.RevealedName ?? ""),
                value.Revision.ToString(CultureInfo.InvariantCulture), String.Join(",", facts.ToArray()) });
        }

        public static bool TryRead(string text, out PersonContactRecord value)
        {
            value = null;
            if (String.IsNullOrEmpty(text) || text.Length > 8192) return false;
            string[] p = text.Split('|');
            Allegiance allegiance; Readiness readiness; Relationship relationship; long revision;
            if (p.Length != 12 || p[0] != EncounterAllegiancePolicy.Schema ||
                !Enum.TryParse(p[3], false, out allegiance) || !Enum.TryParse(p[4], false, out readiness) ||
                !Enum.TryParse(p[5], false, out relationship) || !Int64.TryParse(p[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out revision)) return false;
            string person, camp, name;
            if (!Unesc(p[1], out person) || !Unesc(p[2], out camp) || !Unesc(p[9], out name)) return false;
            PersonContactRecord parsed = new PersonContactRecord { PersonId = person, CampId = camp, Allegiance = allegiance,
                Readiness = readiness, Relationship = relationship, PersonNoticedPlayer = p[6] == "1", PlayerNoticedPerson = p[7] == "1",
                IdentityRevealed = p[8] == "1", RevealedName = name, Revision = revision };
            if ((p[6] != "0" && p[6] != "1") || (p[7] != "0" && p[7] != "1") || (p[8] != "0" && p[8] != "1")) return false;
            if (p[11].Length > 0) foreach (string encoded in p[11].Split(',')) { string id; if (!Unesc(encoded, out id) || String.IsNullOrEmpty(id) || !parsed.AppliedFactIds.Add(id)) return false; }
            if (!Valid(parsed)) return false;
            value = parsed; return true;
        }

        static bool Valid(PersonContactRecord r)
        {
            if (r == null || String.IsNullOrEmpty(r.PersonId) || String.IsNullOrEmpty(r.CampId) || r.Revision < 0 ||
                r.PersonId.Length > 128 || r.CampId.Length > 128 || (r.RevealedName != null && r.RevealedName.Length > 80)) return false;
            return r.IdentityRevealed ? r.PersonNoticedPlayer && r.PlayerNoticedPerson && !String.IsNullOrEmpty(r.RevealedName) : String.IsNullOrEmpty(r.RevealedName);
        }
        static string Esc(string value) { return Uri.EscapeDataString(value); }
        static bool Unesc(string value, out string decoded)
        { decoded = null; try { decoded = Uri.UnescapeDataString(value); return Esc(decoded) == value; } catch (UriFormatException) { return false; } }
    }
}

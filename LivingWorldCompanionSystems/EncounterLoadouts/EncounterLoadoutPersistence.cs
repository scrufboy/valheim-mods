using System;
using System.Collections.Generic;
using System.Text;

namespace LivingWorld.CompanionSystems.EncounterLoadouts
{
    public static class EncounterLoadoutPersistence
    {
        const string Header = "lwc-encounter-loadout-v1";

        public static string Write(PersonLoadoutState state)
        {
            var fields = new List<string> { Header, E(state.PersonId), E(state.OriginEncounterId), E(state.OriginCampId), E(state.FactionId),
                E(state.RoleId), ((int)state.OriginBiome).ToString(), E(state.ActiveVariantId), state.Revision.ToString(),
                ((int)state.Life).ToString(), E(state.Relationship), E(state.Wounds), E(state.DeathRecord), state.Knowledge.Count.ToString() };
            foreach (KnowledgeRecord fact in state.Knowledge)
            { fields.Add(E(fact.FactId)); fields.Add(E(fact.SiteId)); fields.Add(E(fact.Source)); fields.Add(fact.Verified ? "1" : "0"); }
            fields.Add(state.PersonalGear.Count.ToString());
            foreach (GearRecord item in state.PersonalGear)
            { fields.Add(E(item.ItemId)); fields.Add(((int)item.Kind).ToString()); fields.Add(((int)item.Tier).ToString()); fields.Add(item.Quantity.ToString()); fields.Add(item.Equipped ? "1" : "0"); fields.Add(item.PersonalBound ? "1" : "0"); }
            return String.Join("|", fields.ToArray());
        }

        public static bool TryRead(string text, out PersonLoadoutState state)
        {
            state = null;
            try
            {
                string[] p = text.Split('|'); int i = 0;
                if (p[i++] != Header) return false;
                var value = new PersonLoadoutState { PersonId = D(p[i++]), OriginEncounterId = D(p[i++]), OriginCampId = D(p[i++]),
                    FactionId = D(p[i++]), RoleId = D(p[i++]), OriginBiome = (BiomeTier)Int(p[i++]), ActiveVariantId = D(p[i++]),
                    Revision = Int(p[i++]), Life = (LifeState)Int(p[i++]), Relationship = D(p[i++]), Wounds = D(p[i++]), DeathRecord = D(p[i++]) };
                int knowledge = Int(p[i++]); if (knowledge < 0 || knowledge > 64) return false;
                for (int n = 0; n < knowledge; n++) value.Knowledge.Add(new KnowledgeRecord { FactId = D(p[i++]), SiteId = D(p[i++]), Source = D(p[i++]), Verified = p[i++] == "1" });
                int gear = Int(p[i++]); if (gear < 0 || gear > 64) return false;
                for (int n = 0; n < gear; n++) value.PersonalGear.Add(new GearRecord { ItemId = D(p[i++]), Kind = (GearKind)Int(p[i++]), Tier = (BiomeTier)Int(p[i++]), Quantity = Int(p[i++]), Equipped = p[i++] == "1", PersonalBound = p[i++] == "1" });
                if (i != p.Length || value.Revision < 1 || EncounterLoadoutPolicy.ValidateRoster(new[] { value }) != null) return false;
                state = value; return true;
            }
            catch { return false; }
        }

        static string E(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")); }
        static string D(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        static int Int(string value) { int result; if (!Int32.TryParse(value, out result)) throw new FormatException(); return result; }
    }
}

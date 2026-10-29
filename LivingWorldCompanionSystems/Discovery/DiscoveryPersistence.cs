using System;
using System.Globalization;

namespace LivingWorld.CompanionSystems.Discovery
{
    public static class DiscoveryPersistence
    {
        public const string LeadKey = "lwc_discovery_lead_v1";

        public static string Write(LeadRecord lead)
        {
            if (!Valid(lead)) throw new ArgumentException("Invalid discovery lead", "lead");
            return String.Join("|", new[] {
                DiscoveryPolicy.Schema, lead.PartyId.ToString(CultureInfo.InvariantCulture), Escape(lead.TemplateId),
                Escape(lead.EncounterId), Escape(lead.SiteId), lead.State.ToString(),
                lead.CreatedDay.ToString(CultureInfo.InvariantCulture), lead.ClosedDay.ToString(CultureInfo.InvariantCulture),
                lead.X.ToString("R", CultureInfo.InvariantCulture), lead.Y.ToString("R", CultureInfo.InvariantCulture),
                lead.Z.ToString("R", CultureInfo.InvariantCulture)
            });
        }

        public static bool TryRead(string text, out LeadRecord lead)
        {
            lead = null;
            if (String.IsNullOrEmpty(text) || text.Length > 1024) return false;
            string[] p = text.Split('|');
            if (p.Length != 11 || p[0] != DiscoveryPolicy.Schema) return false;
            long party; int created, closed; float x, y, z; LeadState state;
            if (!Int64.TryParse(p[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out party) ||
                !Enum.TryParse(p[5], false, out state) ||
                !Int32.TryParse(p[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out created) ||
                !Int32.TryParse(p[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out closed) ||
                !Single.TryParse(p[8], NumberStyles.Float, CultureInfo.InvariantCulture, out x) ||
                !Single.TryParse(p[9], NumberStyles.Float, CultureInfo.InvariantCulture, out y) ||
                !Single.TryParse(p[10], NumberStyles.Float, CultureInfo.InvariantCulture, out z)) return false;
            string template, encounter, site;
            if (!Unescape(p[2], out template) || !Unescape(p[3], out encounter) || !Unescape(p[4], out site)) return false;
            LeadRecord parsed = new LeadRecord { PartyId = party, TemplateId = template, EncounterId = encounter, SiteId = site,
                State = state, CreatedDay = created, ClosedDay = closed, X = x, Y = y, Z = z };
            if (!Valid(parsed)) return false;
            lead = parsed; return true;
        }

        static bool Valid(LeadRecord lead)
        {
            if (lead == null || lead.PartyId == 0 || String.IsNullOrEmpty(lead.TemplateId) ||
                String.IsNullOrEmpty(lead.EncounterId) || String.IsNullOrEmpty(lead.SiteId)) return false;
            if (lead.TemplateId.Length > 128 || lead.EncounterId.Length > 128 || lead.SiteId.Length > 128 || lead.CreatedDay < 0) return false;
            if (Single.IsNaN(lead.X) || Single.IsInfinity(lead.X) || Single.IsNaN(lead.Y) || Single.IsInfinity(lead.Y) || Single.IsNaN(lead.Z) || Single.IsInfinity(lead.Z)) return false;
            return lead.State == LeadState.Active ? lead.ClosedDay == -1 :
                lead.State != LeadState.None && lead.ClosedDay >= lead.CreatedDay;
        }
        static string Escape(string value) { return Uri.EscapeDataString(value); }
        static bool Unescape(string value, out string decoded)
        {
            decoded = null;
            try { decoded = Uri.UnescapeDataString(value); return Escape(decoded) == value; }
            catch (UriFormatException) { return false; }
        }
    }
}

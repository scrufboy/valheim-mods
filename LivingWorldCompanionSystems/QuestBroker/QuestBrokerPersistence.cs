using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LivingWorld.CompanionSystems.QuestBroker
{
    public static class QuestBrokerPersistence
    {
        public const string Schema = "lwc-quest-broker-v1";

        public static string Write(BrokerState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            var b = new StringBuilder(); b.AppendLine(Schema); b.AppendLine(state.Revision.ToString(CultureInfo.InvariantCulture));
            foreach (var pair in state.Requests)
            {
                var r = pair.Value;
                b.Append("R|").Append(E(r.RequestId)).Append('|').Append(E(r.AuthoredRequestId)).Append('|').Append(E(r.PartyId)).Append('|')
                    .Append(E(r.IssuerPersonId)).Append('|').Append(E(r.EncounterId)).Append('|').Append(E(r.WorldId)).Append('|')
                    .Append(E(r.SiteId)).Append('|').Append(E(r.SiteIdentity)).Append('|').Append(E(r.Biome)).Append('|').Append(E(r.Description)).Append('|')
                    .Append(E(r.RequiredWorldKey)).Append('|').Append(E(r.AuthoredEncounterId)).Append('|').Append(E(r.EncounterAdapterId)).Append('|')
                    .Append((int)r.Kind).Append('|').Append((int)r.Visibility).Append('|').Append((int)r.Status).Append('|').Append(r.CreatedWorldTick).Append('|')
                    .Append(r.ExpiresWorldTick).Append('|').Append(r.ResolvedWorldTick).Append('|').Append(E(r.ArrivalEvidenceId)).Append('|')
                    .Append(r.ArrivedWorldTick).Append('|').Append(E(r.ActivationId)).Append('|').Append(E(r.CompletionEvidenceId)).Append('|').Append(E(r.ClaimId)).AppendLine();
            }
            foreach (var pair in state.Presentations)
            {
                var p = pair.Value;
                b.Append("P|").Append(E(p.PresentationId)).Append('|').Append(E(p.RequestId)).Append('|').Append(E(p.SourceId)).Append('|')
                    .Append(E(p.SourcePersonId)).Append('|').Append(E(p.SettlementId)).Append('|').Append((int)p.SourceKind).Append('|')
                    .Append(p.PresentedWorldTick).AppendLine();
            }
            foreach (var id in state.AppliedCommands) b.Append("C|").Append(E(id)).AppendLine();
            return b.ToString();
        }

        public static bool TryRead(string text, out BrokerState state)
        {
            state = null;
            try
            {
                using (var reader = new StringReader(text ?? ""))
                {
                    if (reader.ReadLine() != Schema) return false;
                    long revision; if (!Int64.TryParse(reader.ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture, out revision) || revision < 0) return false;
                    var loaded = new BrokerState { Revision = revision }; string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue; var p = line.Split('|');
                        if (p[0] == "R" && p.Length == 25)
                        {
                            int kind, visibility, status; long created, expires, resolved, arrived;
                            if (!Int32.TryParse(p[14], out kind) || !Int32.TryParse(p[15], out visibility) || !Int32.TryParse(p[16], out status) ||
                                !Int64.TryParse(p[17], out created) || !Int64.TryParse(p[18], out expires) || !Int64.TryParse(p[19], out resolved) || !Int64.TryParse(p[21], out arrived) ||
                                !Enum.IsDefined(typeof(RequestKind), kind) || !Enum.IsDefined(typeof(RequestVisibility), visibility) || !Enum.IsDefined(typeof(RequestStatus), status)) return false;
                            var r = new WorldRequest { RequestId=D(p[1]), AuthoredRequestId=D(p[2]), PartyId=D(p[3]), IssuerPersonId=D(p[4]),
                                EncounterId=D(p[5]), WorldId=D(p[6]), SiteId=D(p[7]), SiteIdentity=D(p[8]), Biome=D(p[9]), Description=D(p[10]),
                                RequiredWorldKey=D(p[11]), AuthoredEncounterId=D(p[12]), EncounterAdapterId=D(p[13]), Kind=(RequestKind)kind,
                                Visibility=(RequestVisibility)visibility, Status=(RequestStatus)status, CreatedWorldTick=created, ExpiresWorldTick=expires, ResolvedWorldTick=resolved,
                                ArrivalEvidenceId=D(p[20]), ArrivedWorldTick=arrived, ActivationId=D(p[22]), CompletionEvidenceId=D(p[23]), ClaimId=D(p[24]) };
                            if (!Valid(r) || loaded.Requests.ContainsKey(r.RequestId)) return false; loaded.Requests.Add(r.RequestId, r);
                        }
                        else if (p[0] == "P" && p.Length == 8)
                        {
                            int kind; long tick; if (!Int32.TryParse(p[6], out kind) || !Int64.TryParse(p[7], out tick) || !Enum.IsDefined(typeof(OfferSourceKind), kind)) return false;
                            var v = new OfferPresentation { PresentationId=D(p[1]), RequestId=D(p[2]), SourceId=D(p[3]), SourcePersonId=D(p[4]), SettlementId=D(p[5]), SourceKind=(OfferSourceKind)kind, PresentedWorldTick=tick };
                            if (String.IsNullOrEmpty(v.PresentationId) || loaded.Presentations.ContainsKey(v.PresentationId)) return false; loaded.Presentations.Add(v.PresentationId, v);
                        }
                        else if (p[0] == "C" && p.Length == 2) { if (!loaded.AppliedCommands.Add(D(p[1]))) return false; }
                        else return false;
                    }
                    foreach (var v in loaded.Presentations.Values) if (!loaded.Requests.ContainsKey(v.RequestId)) return false;
                    state = loaded; return true;
                }
            }
            catch { state = null; return false; }
        }

        static bool Valid(WorldRequest r)
        {
            if (String.IsNullOrEmpty(r.RequestId) || String.IsNullOrEmpty(r.AuthoredRequestId) || String.IsNullOrEmpty(r.PartyId) ||
                String.IsNullOrEmpty(r.IssuerPersonId) || String.IsNullOrEmpty(r.WorldId) || String.IsNullOrEmpty(r.SiteId) ||
                String.IsNullOrEmpty(r.SiteIdentity) || r.CreatedWorldTick < 0 || r.ExpiresWorldTick <= r.CreatedWorldTick) return false;
            bool closed = r.Status == RequestStatus.Refused || r.Status == RequestStatus.Completed || r.Status == RequestStatus.Expired;
            if (closed != (r.ResolvedWorldTick >= r.CreatedWorldTick)) return false;
            if (String.IsNullOrEmpty(r.ArrivalEvidenceId) != (r.ArrivedWorldTick < r.CreatedWorldTick)) return false;
            bool authoredEncounter = r.Kind == RequestKind.AuthoredMiniboss || r.Kind == RequestKind.AuthoredRaid;
            if (String.IsNullOrEmpty(r.ArrivalEvidenceId) && !String.IsNullOrEmpty(r.ActivationId)) return false;
            if (!String.IsNullOrEmpty(r.ArrivalEvidenceId) && authoredEncounter && r.ActivationId != "activation:" + r.RequestId) return false;
            if (!authoredEncounter && !String.IsNullOrEmpty(r.ActivationId)) return false;
            return r.Status != RequestStatus.Completed || (!String.IsNullOrEmpty(r.CompletionEvidenceId) && r.ClaimId == "claim:" + r.RequestId);
        }
        static string E(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")); }
        static string D(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
    }
}

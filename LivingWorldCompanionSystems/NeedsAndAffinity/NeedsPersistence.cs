using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace LivingWorld.CompanionSystems.NeedsAndAffinity
{
    public static class NeedsPersistence
    {
        public const string Schema = "lwc-needs-affinity-v1";

        public static string Write(NeedsState state)
        {
            var b = new StringBuilder();
            b.AppendLine(Schema); b.AppendLine(state.Revision.ToString(CultureInfo.InvariantCulture));
            foreach (var pair in state.Requests)
            {
                var r = pair.Value;
                b.Append("R|").Append(E(r.RequestId)).Append('|').Append(E(r.PersonId)).Append('|').Append(E(r.EncounterId)).Append('|')
                    .Append(E(r.AuthoredNeedId)).Append('|').Append(E(r.TargetId)).Append('|').Append(E(r.Description)).Append('|')
                    .Append((int)r.Kind).Append('|').Append((int)r.Status).Append('|').Append(r.RequiredAmount).Append('|')
                    .Append(r.OfferedWorldTick).Append('|').Append(r.ResolvedWorldTick).AppendLine();
            }
            foreach (var pair in state.Memories)
            {
                var m = pair.Value;
                b.Append("M|").Append(E(m.MemoryId)).Append('|').Append(E(m.PersonId)).Append('|').Append(E(m.RequestId)).Append('|')
                    .Append(E(m.EvidenceId)).Append('|').Append((int)m.Choice).Append('|').Append(m.WorldTick).AppendLine();
            }
            foreach (var id in state.AppliedCommands) b.Append("C|").Append(E(id)).AppendLine();
            return b.ToString();
        }

        public static bool TryRead(string text, out NeedsState state)
        {
            state = null;
            try
            {
                using (var reader = new StringReader(text ?? ""))
                {
                    if (reader.ReadLine() != Schema) return false;
                    long revision; if (!Int64.TryParse(reader.ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture, out revision) || revision < 0) return false;
                    var loaded = new NeedsState { Revision = revision }; string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Length == 0) continue; var p = line.Split('|');
                        if (p[0] == "R" && p.Length == 12)
                        {
                            int kind, status, amount; long offered, resolved;
                            if (!Int32.TryParse(p[7], out kind) || !Int32.TryParse(p[8], out status) || !Int32.TryParse(p[9], out amount) ||
                                !Int64.TryParse(p[10], out offered) || !Int64.TryParse(p[11], out resolved) ||
                                !Enum.IsDefined(typeof(NeedKind), kind) || !Enum.IsDefined(typeof(RequestStatus), status)) return false;
                            var r = new RequestRecord { RequestId = D(p[1]), PersonId = D(p[2]), EncounterId = D(p[3]), AuthoredNeedId = D(p[4]),
                                TargetId = D(p[5]), Description = D(p[6]), Kind = (NeedKind)kind, Status = (RequestStatus)status,
                                RequiredAmount = amount, OfferedWorldTick = offered, ResolvedWorldTick = resolved };
                            if (String.IsNullOrEmpty(r.RequestId) || loaded.Requests.ContainsKey(r.RequestId)) return false; loaded.Requests.Add(r.RequestId, r);
                        }
                        else if (p[0] == "M" && p.Length == 7)
                        {
                            int choice; long tick; if (!Int32.TryParse(p[5], out choice) || !Int64.TryParse(p[6], out tick) || !Enum.IsDefined(typeof(RememberedChoice), choice)) return false;
                            var m = new ChoiceMemory { MemoryId = D(p[1]), PersonId = D(p[2]), RequestId = D(p[3]), EvidenceId = D(p[4]), Choice = (RememberedChoice)choice, WorldTick = tick };
                            if (String.IsNullOrEmpty(m.MemoryId) || loaded.Memories.ContainsKey(m.MemoryId)) return false; loaded.Memories.Add(m.MemoryId, m);
                        }
                        else if (p[0] == "C" && p.Length == 2) { if (!loaded.AppliedCommands.Add(D(p[1]))) return false; }
                        else return false;
                    }
                    state = loaded; return true;
                }
            }
            catch { state = null; return false; }
        }
        static string E(string value) { return Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? "")); }
        static string D(string value) { return Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
    }
}

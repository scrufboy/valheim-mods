using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LivingVillageCore
{
    // EntityId is an opaque composite key supplied by the adapter: world,
    // village, and entity identity. No Unity objects are accessed here.
    public sealed class TextResidentNameStore : IResidentNameStore
    {
        static readonly object RegistryGate = new object();
        static readonly Dictionary<string, object> FileGates =
            new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        readonly string path;
        readonly object gate;

        public TextResidentNameStore(string path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("Ledger path required.");
            this.path = Path.GetFullPath(path);
            lock (RegistryGate)
            {
                if (!FileGates.TryGetValue(this.path, out gate))
                {
                    gate = new object();
                    FileGates.Add(this.path, gate);
                }
            }
        }

        static void Field(string value, string label)
        {
            if (value == null) throw new ArgumentException(label + " must not be null.");
            foreach (char c in value)
                if (Char.IsControl(c)) throw new ArgumentException(label + " contains a control/delimiter character.");
        }

        public Task SaveAsync(StoredResidentName name)
        {
            if (name == null || name.Identity == null) throw new ArgumentNullException("name");
            ResidentIdentity id = name.Identity;
            Field(id.EntityId, "EntityId");
            if (String.IsNullOrWhiteSpace(id.EntityId)) throw new ArgumentException("EntityId required.");
            string display = id.DisplayName ?? "";
            Field(display, "DisplayName");
            if (!Enum.IsDefined(typeof(NameOrigin), name.Origin)) throw new ArgumentException("Invalid naming origin.");
            string line = String.Join("\t", new string[] { "v1", id.EntityId,
                id.Female ? "1" : "0", display, id.NamingCompleted ? "1" : "0",
                name.OnlineAttempted ? "1" : "0", name.Origin.ToString() });
            return Task.Run(() =>
            {
                lock (gate)
                {
                    string directory = Path.GetDirectoryName(path);
                    if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                    byte[] bytes = new UTF8Encoding(false, true).GetBytes(line + "\n");
                    using (FileStream file = new FileStream(path, FileMode.OpenOrCreate,
                        FileAccess.ReadWrite, FileShare.Read))
                    {
                        // Separate an interrupted trailing record from the next append.
                        if (file.Length > 0)
                        {
                            file.Position = file.Length - 1;
                            if (file.ReadByte() != 10)
                            {
                                file.Position = file.Length;
                                file.WriteByte(10);
                            }
                        }
                        file.Position = file.Length;
                        file.Write(bytes, 0, bytes.Length);
                        file.Flush(true);
                    }
                }
            });
        }

        public Task<StoredResidentName> LoadAsync(string entityId)
        {
            Field(entityId, "EntityId");
            return Task.Run(() =>
            {
                lock (gate)
                {
                    if (!File.Exists(path)) return null;
                    StoredResidentName latest = null;
                    using (StreamReader reader = new StreamReader(path, new UTF8Encoding(false, true)))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] f = line.Split('\t');
                            NameOrigin origin;
                            if (f.Length != 7 || f[0] != "v1" || f[1] != entityId ||
                                (f[2] != "0" && f[2] != "1") ||
                                (f[4] != "0" && f[4] != "1") ||
                                (f[5] != "0" && f[5] != "1") ||
                                !Enum.TryParse<NameOrigin>(f[6], out origin) ||
                                !Enum.IsDefined(typeof(NameOrigin), origin)) continue;
                            latest = new StoredResidentName { Origin = origin,
                                OnlineAttempted = f[5] == "1", Identity = new ResidentIdentity {
                                    EntityId = f[1], Female = f[2] == "1", DisplayName = f[3],
                                    NamingCompleted = f[4] == "1" } };
                        }
                    }
                    return latest;
                }
            });
        }
    }
}

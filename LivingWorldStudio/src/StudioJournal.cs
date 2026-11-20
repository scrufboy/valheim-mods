using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LivingWorld.Studio
{
    // Utility-owned recovery data only. No companion orders, inventory or combat state.
    public sealed class StudioRecord
    {
        public long World, Player;
        public string Session, Phase;
        public float X, Y, Z, Qx, Qy, Qz, Qw, RoomX, RoomY, RoomZ;

        public void Validate()
        {
            Guid session;
            if (World == 0 || Player == 0 || !Guid.TryParseExact(Session, "N", out session) ||
                session == Guid.Empty || session.ToString("N") != Session)
                throw new InvalidDataException("Studio recovery identity is invalid.");
            if (Phase != "Prepared" && Phase != "Entering" && Phase != "Active" && Phase != "Returning" && Phase != "Restored")
                throw new InvalidDataException("Studio recovery phase is unknown.");
            foreach (float value in new[] { X, Y, Z, Qx, Qy, Qz, Qw, RoomX, RoomY, RoomZ })
                if (float.IsNaN(value) || float.IsInfinity(value))
                    throw new InvalidDataException("Studio recovery coordinates must be finite.");
            double norm = (double)Qx * Qx + (double)Qy * Qy + (double)Qz * Qz + (double)Qw * Qw;
            if (Math.Abs(norm - 1d) > 0.01d)
                throw new InvalidDataException("Studio return rotation is not a unit quaternion.");
        }
    }

    // Single-writer journal. A checksum detects damage; it is not authentication.
    // The caller must still validate actual world/player and movement authority.
    public sealed class StudioJournal
    {
        const string Schema = "LWSJ1";
        const int MaxBytes = 4096;
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        readonly string directory;
        readonly object gate = new object();

        public StudioJournal(string directory)
        {
            if (string.IsNullOrEmpty(directory)) throw new ArgumentException("Journal directory is required.", "directory");
            this.directory = Path.GetFullPath(directory);
        }

        string PathFor(long world, long player)
        {
            if (world == 0 || player == 0) throw new ArgumentException("Nonzero world/player IDs are required.");
            return Path.Combine(directory, world.ToString(Invariant) + "_" + player.ToString(Invariant) + ".studio");
        }

        public static string Encode(StudioRecord record)
        {
            if (record == null) throw new ArgumentNullException("record");
            record.Validate();
            string payload = string.Join("\n", new[] {
                Schema, record.World.ToString(Invariant), record.Player.ToString(Invariant), record.Session, record.Phase,
                F(record.X), F(record.Y), F(record.Z), F(record.Qx), F(record.Qy), F(record.Qz), F(record.Qw),
                F(record.RoomX), F(record.RoomY), F(record.RoomZ)
            }) + "\n";
            return payload + Digest(payload) + "\n";
        }

        public static StudioRecord Decode(string encoded)
        {
            if (encoded == null || Utf8.GetByteCount(encoded) > MaxBytes || encoded.IndexOf('\r') >= 0)
                throw new InvalidDataException("Studio recovery record is missing, oversized or malformed.");
            string[] lines = encoded.Split('\n');
            if (lines.Length != 17 || lines[0] != Schema || lines[16] != "")
                throw new InvalidDataException("Studio recovery schema or field count is invalid.");
            string payload = string.Join("\n", lines, 0, 15) + "\n";
            if (lines[15] != Digest(payload)) throw new InvalidDataException("Studio recovery checksum failed.");
            long world, player;
            if (!long.TryParse(lines[1], NumberStyles.AllowLeadingSign, Invariant, out world) ||
                !long.TryParse(lines[2], NumberStyles.AllowLeadingSign, Invariant, out player))
                throw new InvalidDataException("Studio recovery identity cannot be parsed.");
            var record = new StudioRecord {
                World = world, Player = player, Session = lines[3], Phase = lines[4],
                X = P(lines[5]), Y = P(lines[6]), Z = P(lines[7]),
                Qx = P(lines[8]), Qy = P(lines[9]), Qz = P(lines[10]), Qw = P(lines[11]),
                RoomX = P(lines[12]), RoomY = P(lines[13]), RoomZ = P(lines[14])
            };
            record.Validate();
            // Strict canonical representation rejects alternate spellings and whitespace.
            if (Encode(record) != encoded) throw new InvalidDataException("Studio recovery encoding is not canonical.");
            return record;
        }

        public void Save(StudioRecord record)
        {
            string encoded = Encode(record);
            lock (gate)
            {
                Directory.CreateDirectory(directory);
                string path = PathFor(record.World, record.Player);
                string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                bool preserveBackup = false;
                if (File.Exists(path))
                {
                    try { Read(path, record.World, record.Player); }
                    catch (InvalidDataException)
                    {
                        // Do not overwrite the last valid backup with corrupt primary bytes.
                        Read(path + ".bak", record.World, record.Player);
                        preserveBackup = true;
                    }
                }
                try
                {
                    byte[] bytes = Utf8.GetBytes(encoded);
                    using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    {
                        stream.Write(bytes, 0, bytes.Length);
                        stream.Flush(true);
                    }
                    if (File.Exists(path)) File.Replace(temp, path, preserveBackup ? null : path + ".bak");
                    else File.Move(temp, path);
                    // Verify durable-file contents before the caller may move a participant.
                    Read(path, record.World, record.Player);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
        }

        public StudioRecord Load(long world, long player)
        {
            lock (gate)
            {
                string path = PathFor(world, player);
                bool primary = File.Exists(path), backup = File.Exists(path + ".bak");
                if (!primary && !backup) return null;
                if (primary)
                {
                    try { return Read(path, world, player); }
                    catch (InvalidDataException) { if (!backup) throw; }
                }
                return Read(path + ".bak", world, player);
            }
        }

        // Invoke only after verified return and normal authority restoration.
        // No automatic expiry, disposal, entrance deletion or shutdown deletes recovery data.
        public void Delete(long world, long player)
        {
            lock (gate)
            {
                string path = PathFor(world, player);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                if (File.Exists(path)) File.Delete(path);
            }
        }

        static StudioRecord Read(string path, long world, long player)
        {
            if (!File.Exists(path)) throw new InvalidDataException("Studio recovery backup is unavailable.");
            var info = new FileInfo(path);
            if (info.Length > MaxBytes) throw new InvalidDataException("Studio recovery record is oversized.");
            StudioRecord record;
            try { record = Decode(File.ReadAllText(path, Utf8)); }
            catch (DecoderFallbackException error) { throw new InvalidDataException("Studio recovery UTF-8 is invalid.", error); }
            if (record.World != world || record.Player != player)
                throw new InvalidDataException("Studio recovery belongs to another world/player.");
            return record;
        }

        static string F(float value) { return value.ToString("R", Invariant); }
        static float P(string value)
        {
            float parsed;
            if (!float.TryParse(value, NumberStyles.Float, Invariant, out parsed))
                throw new InvalidDataException("Studio recovery coordinate cannot be parsed.");
            return parsed;
        }
        static string Digest(string value)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Utf8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
        }
    }
}

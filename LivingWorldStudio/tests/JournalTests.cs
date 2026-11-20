using System;
using System.Globalization;
using System.IO;
using LivingWorld.Studio;

internal static class JournalTests
{
    static int checks;
    static void Check(bool value, string label)
    { if (!value) throw new Exception(label); checks++; }
    static void Reject(Action action, string label)
    {
        try { action(); }
        catch (InvalidDataException) { checks++; return; }
        throw new Exception("Expected fail-closed rejection: " + label);
    }
    static StudioRecord Record()
    {
        return new StudioRecord { World = 123, Player = 456, Session = "00112233445566778899aabbccddeeff",
            Phase = "Prepared", X = -12.125f, Y = 24.375f, Z = 0.00005f,
            Qx = 0, Qy = 0.70710677f, Qz = 0, Qw = 0.70710677f,
            RoomX = 100, RoomY = 5000, RoomZ = -100 };
    }
    public static int Main()
    {
        string directory = Path.Combine(Path.GetTempPath(), "lwc-studio-journal-" + Guid.NewGuid().ToString("N"));
        try
        {
            var journal = new StudioJournal(directory);
            Check(journal.Load(123, 456) == null, "Absent recovery is absent.");
            var record = Record();
            string encoded = StudioJournal.Encode(record);
            var decoded = StudioJournal.Decode(encoded);
            Check(decoded.X == record.X && decoded.Z == record.Z && decoded.Qy == record.Qy &&
                decoded.Qw == record.Qw && decoded.RoomY == record.RoomY, "Exact coordinate/rotation roundtrip.");
            var culture = CultureInfo.CurrentCulture;
            try {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                Check(StudioJournal.Encode(record) == encoded, "Locale-independent serialization.");
            } finally { CultureInfo.CurrentCulture = culture; }
            journal.Save(record);
            Check(journal.Load(123, 456).Phase == "Prepared", "Initial flushed write.");
            record.Phase = "Entering"; journal.Save(record);
            Check(journal.Load(123, 456).Phase == "Entering", "Atomic replacement.");
            string path = Path.Combine(directory, "123_456.studio");
            Check(StudioJournal.Decode(File.ReadAllText(path + ".bak")).Phase == "Prepared", "Prior valid snapshot retained.");
            File.WriteAllText(path, "broken");
            Check(journal.Load(123, 456).Phase == "Prepared", "Damaged primary recovers valid backup.");
            record.Phase = "Returning"; journal.Save(record);
            Check(journal.Load(123, 456).Phase == "Returning", "Recovered session can advance.");
            Check(StudioJournal.Decode(File.ReadAllText(path + ".bak")).Phase == "Prepared", "Repair does not replace good backup with corruption.");
            File.WriteAllText(path, "broken"); File.WriteAllText(path + ".bak", "broken");
            Reject(() => journal.Load(123, 456), "Both snapshots invalid.");
            Reject(() => journal.Save(record), "Invalid snapshots are not silently erased.");
            File.WriteAllText(path, encoded); File.Delete(path + ".bak");
            var foreign = Record(); foreign.World = 987;
            File.WriteAllText(path, StudioJournal.Encode(foreign));
            Reject(() => journal.Load(123, 456), "Wrong-world content at expected filename.");
            foreign = Record(); foreign.Player = 654;
            File.WriteAllText(path, StudioJournal.Encode(foreign));
            Reject(() => journal.Load(123, 456), "Wrong-player content at expected filename.");
            Reject(() => StudioJournal.Decode(encoded.Replace("Prepared", "Active")), "Checksum detects mutation.");
            Reject(() => StudioJournal.Decode(encoded + "extra\n"), "Extra fields rejected.");
            Reject(() => StudioJournal.Decode(encoded.Replace("LWSJ1", "LWSJ2")), "Unknown schema rejected.");
            Reject(() => StudioJournal.Decode(encoded.Replace("\n", "\r\n")), "Noncanonical newlines rejected.");
            record.X = float.NaN; Reject(() => StudioJournal.Encode(record), "NaN rejected.");
            record.X = float.PositiveInfinity; Reject(() => StudioJournal.Encode(record), "Infinity rejected.");
            record = Record(); record.Qw = 0; record.Qy = 0;
            Reject(() => StudioJournal.Encode(record), "Invalid return rotation rejected.");
            record = Record(); record.Phase = "Closed";
            Reject(() => StudioJournal.Encode(record), "Unsupported durable phase rejected.");
            record = Record(); record.Session = "../../invalid";
            Reject(() => StudioJournal.Encode(record), "Malformed session identity rejected.");
            journal.Delete(123, 456);
            Check(journal.Load(123, 456) == null, "Explicit restore cleanup removes both snapshots.");
            record = Record(); record.Phase = "Restored"; journal.Save(record);
            Check(journal.Load(123, 456).Phase == "Restored", "Completed session tombstone survives reload.");
            Check(StudioJournal.Decode(StudioJournal.Encode(record)).Session == record.Session, "Completion fence retains exact session identity.");
            record.Phase = "Prepared"; journal.Save(record);
            Check(journal.Load(123, 456).Phase == "Prepared", "A new prepared transaction can replace completion fence.");
            record.Phase = "Entering"; journal.Save(record);
            Check(StudioJournal.Decode(File.ReadAllText(path + ".bak")).Phase == "Prepared", "Moved-session backup points to recoverable preparation, never an old completion fence.");
            journal.Delete(123, 456);
            Check(Directory.GetFiles(directory, "*.tmp").Length == 0, "No temporary files remain.");
            Console.WriteLine("PASS StudioJournal: " + checks + " assertions.");
            return 0;
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}

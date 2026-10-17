using System;
using System.IO;
using System.Threading.Tasks;
using LivingVillageCore;

public static class TextNameStoreTests
{
    static int checks;
    static void Assert(bool value, string message) { checks++; if (!value) throw new Exception(message); }
    static StoredResidentName Name(string id, string display, bool complete)
    {
        return new StoredResidentName { OnlineAttempted = true,
            Origin = complete ? NameOrigin.Online : NameOrigin.Pending,
            Identity = new ResidentIdentity { EntityId = id, Female = true,
                DisplayName = display, NamingCompleted = complete } };
    }
    public static void Main() { Run().GetAwaiter().GetResult(); Console.WriteLine("TextResidentNameStore: " + checks + " checks passed."); }
    static async Task Run()
    {
        string path = Path.Combine(Path.GetTempPath(), "LivingVillageNameStore-" + Guid.NewGuid().ToString("N") + ".tsv");
        try
        {
            TextResidentNameStore store = new TextResidentNameStore(path);
            Assert(await store.LoadAsync("world/village/entity") == null, "missing ledger");
            await store.SaveAsync(Name("world/village/entity", "", false));
            StoredResidentName n = await store.LoadAsync("world/village/entity");
            Assert(n.OnlineAttempted && !n.Identity.NamingCompleted && n.Origin == NameOrigin.Pending,
                "durable attempt record roundtrip");
            await store.SaveAsync(Name("world/village/entity", "Þóra", true));
            n = await new TextResidentNameStore(path).LoadAsync("world/village/entity");
            Assert(n.Identity.DisplayName == "Þóra" && n.Identity.Female && n.Identity.NamingCompleted &&
                n.Origin == NameOrigin.Online, "restart latest UTF8 record");
            Assert(File.ReadAllLines(path).Length == 2, "append only history retained");
            Assert(await store.LoadAsync("otherworld/village/entity") == null, "world composite key independent");
            bool bad = false;
            try { await store.SaveAsync(Name("bad\tid", "Name", true)); } catch (ArgumentException) { bad = true; }
            Assert(bad, "key delimiter rejected");
            bad = false;
            try { await store.SaveAsync(Name("good", "Bad\nName", true)); } catch (ArgumentException) { bad = true; }
            Assert(bad, "display delimiter rejected");
            await Task.WhenAll(store.SaveAsync(Name("one", "Astrid", true)),
                new TextResidentNameStore(path).SaveAsync(Name("two", "Sigrid", true)));
            Assert((await store.LoadAsync("one")).Identity.DisplayName == "Astrid" &&
                (await store.LoadAsync("two")).Identity.DisplayName == "Sigrid", "same file writes serialize");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}

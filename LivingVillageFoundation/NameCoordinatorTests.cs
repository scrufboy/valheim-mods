using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LivingVillageCore;

public static class NameCoordinatorTests
{
    sealed class Store : IResidentNameStore
    {
        readonly Dictionary<string, StoredResidentName> data = new Dictionary<string, StoredResidentName>();
        public Task<StoredResidentName> LoadAsync(string id)
        { lock (data) { StoredResidentName n; data.TryGetValue(id, out n); return Task.FromResult(Copy(n)); } }
        public Task SaveAsync(StoredResidentName n)
        { lock (data) data[n.Identity.EntityId] = Copy(n); return Task.FromResult(0); }
        static StoredResidentName Copy(StoredResidentName n)
        { return n == null ? null : new StoredResidentName { OnlineAttempted = n.OnlineAttempted,
            Origin = n.Origin, Identity = new ResidentIdentity { EntityId = n.Identity.EntityId,
            DisplayName = n.Identity.DisplayName, Female = n.Identity.Female,
            NamingCompleted = n.Identity.NamingCompleted } }; }
    }
    sealed class Provider : IResidentNameProvider
    {
        public int Calls;
        public bool Female;
        public string Result = "Astrid";
        public bool Fail;
        public TaskCompletionSource<string> Hold;
        public Task<string> RequestNameAsync(string id, bool female)
        {
            Calls++; Female = female;
            if (Fail) throw new Exception("fake provider failure");
            return Hold != null ? Hold.Task : Task.FromResult(Result);
        }
    }
    static int checks;
    static void Assert(bool test, string message) { checks++; if (!test) throw new Exception(message); }
    static NameCoordinator Create(Store s, Provider p)
    { return new NameCoordinator(s, p, (id, female) => female ? "Sigrid" : "Bjorn"); }
    public static void Main() { Run().GetAwaiter().GetResult(); Console.WriteLine("NameCoordinator: " + checks + " checks passed."); }
    static async Task Run()
    {
        Store s = new Store(); Provider p = new Provider { Hold = new TaskCompletionSource<string>() };
        NameCoordinator c = Create(s, p);
        Task<StoredResidentName> first = c.GetAsync("a", true), second = c.GetAsync("a", true);
        Assert(Object.ReferenceEquals(first, second), "concurrent calls deduplicated");
        p.Hold.SetResult("Astrid");
        StoredResidentName result = await first;
        Assert(p.Calls == 1 && p.Female, "single request with female metadata");
        Assert(result.Origin == NameOrigin.Online && result.Identity.NamingCompleted, "online completed");
        await c.GetAsync("a", true);
        await Create(s, p).GetAsync("a", true);
        Assert(p.Calls == 1, "repeat and restart cached");
        p.Hold = null; p.Result = "Erik";
        result = await c.GetAsync("b", false);
        Assert(p.Calls == 2 && !result.Identity.Female, "new entity independently named");
        p.Result = "Bad\nName";
        result = await c.GetAsync("bad", false);
        Assert(result.Origin == NameOrigin.InvalidProviderName && result.Identity.DisplayName == "Bjorn", "invalid fallback explicit");
        p.Fail = true;
        result = await c.GetAsync("fail", true);
        Assert(result.Origin == NameOrigin.ProviderFailure && result.Identity.DisplayName == "Sigrid", "failure fallback explicit");
        int calls = p.Calls;
        await Create(s, p).GetAsync("fail", true);
        Assert(p.Calls == calls, "failed online request not repeated");
        result = await Create(s, null).GetAsync("offline", false);
        Assert(result.Origin == NameOrigin.Offline && !result.OnlineAttempted, "provider optional");
        await s.SaveAsync(new StoredResidentName { OnlineAttempted = true, Origin = NameOrigin.Pending,
            Identity = new ResidentIdentity { EntityId = "interrupted", Female = false } });
        result = await c.GetAsync("interrupted", false);
        Assert(result.Origin == NameOrigin.InterruptedRequest && p.Calls == calls, "interrupted durable attempt never resent");
        Assert(!NameCoordinator.ValidName(new String('a', 41)) && !NameCoordinator.ValidName("<color>"), "length and markup rejected");
    }
}

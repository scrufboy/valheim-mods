using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LivingVillageCore
{
    public enum NameOrigin { Pending, Online, Offline, ProviderFailure, InvalidProviderName, InterruptedRequest }
    public sealed class StoredResidentName
    {
        public ResidentIdentity Identity;
        public bool OnlineAttempted;
        public NameOrigin Origin;
    }

    // Adapter must durably write before completing SaveAsync. Unity/ZDO adapter
    // is pending and must marshal access to the game thread without blocking it.
    // One coordinator owns a store; cross-process arbitration is not provided.
    public interface IResidentNameStore
    {
        Task<StoredResidentName> LoadAsync(string entityId);
        Task SaveAsync(StoredResidentName name);
    }

    public sealed class NameCoordinator
    {
        readonly IResidentNameStore store;
        readonly IResidentNameProvider provider;
        readonly Func<string, bool, string> fallback;
        readonly object gate = new object();
        readonly Dictionary<string, Task<StoredResidentName>> requests =
            new Dictionary<string, Task<StoredResidentName>>(StringComparer.Ordinal);

        public NameCoordinator(IResidentNameStore store, IResidentNameProvider provider,
            Func<string, bool, string> fallback)
        {
            if (store == null) throw new ArgumentNullException("store");
            if (fallback == null) throw new ArgumentNullException("fallback");
            this.store = store; this.provider = provider; this.fallback = fallback;
        }

        public Task<StoredResidentName> GetAsync(string entityId, bool female)
        {
            if (String.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Entity ID required.");
            lock (gate)
            {
                Task<StoredResidentName> task;
                if (!requests.TryGetValue(entityId, out task))
                {
                    task = Task.Run(() => ResolveAsync(entityId, female));
                    requests.Add(entityId, task);
                }
                return task;
            }
        }

        public static bool ValidName(string name)
        {
            if (String.IsNullOrWhiteSpace(name) || name.Length > 40) return false;
            foreach (char c in name) if (Char.IsControl(c) || c == '<' || c == '>') return false;
            return true;
        }

        async Task<StoredResidentName> ResolveAsync(string entityId, bool female)
        {
            StoredResidentName record = await store.LoadAsync(entityId).ConfigureAwait(false);
            if (record != null && record.Identity != null && record.Identity.NamingCompleted &&
                ValidName(record.Identity.DisplayName)) return record;
            if (record == null || record.Identity == null)
                record = new StoredResidentName { Identity = new ResidentIdentity {
                    EntityId = entityId, Female = female }, Origin = NameOrigin.Pending };
            // An interrupted persisted attempt must not issue another online request.
            if (record.OnlineAttempted)
                return await CompleteFallback(record, NameOrigin.InterruptedRequest).ConfigureAwait(false);
            if (provider == null)
                return await CompleteFallback(record, NameOrigin.Offline).ConfigureAwait(false);

            record.OnlineAttempted = true;
            record.Origin = NameOrigin.Pending;
            await store.SaveAsync(record).ConfigureAwait(false);
            string name = null;
            bool failed = false;
            try { name = await provider.RequestNameAsync(entityId, record.Identity.Female).ConfigureAwait(false); }
            catch (Exception)
            {
                failed = true;
            }
            if (failed)
                return await CompleteFallback(record, NameOrigin.ProviderFailure).ConfigureAwait(false);
            if (!ValidName(name))
                return await CompleteFallback(record, NameOrigin.InvalidProviderName).ConfigureAwait(false);
            record.Identity.DisplayName = name.Trim();
            record.Identity.NamingCompleted = true;
            record.Origin = NameOrigin.Online;
            await store.SaveAsync(record).ConfigureAwait(false);
            return record;
        }

        async Task<StoredResidentName> CompleteFallback(StoredResidentName record, NameOrigin origin)
        {
            string name = fallback(record.Identity.EntityId, record.Identity.Female);
            if (!ValidName(name)) throw new InvalidOperationException("Offline fallback returned an invalid name.");
            record.Identity.DisplayName = name.Trim();
            record.Identity.NamingCompleted = true;
            record.Origin = origin;
            await store.SaveAsync(record).ConfigureAwait(false);
            return record;
        }
    }
}

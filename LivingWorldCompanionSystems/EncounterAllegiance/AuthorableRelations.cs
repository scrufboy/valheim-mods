using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterAllegiance
{
    // Engine-free shape for Formidable's eventual public faction API. String IDs
    // allow independent mods to author identities without extending this enum.
    public sealed class AuthoredFaction
    {
        public string Id;
        public string DisplayName;
    }

    public sealed class PrefabFactionBinding
    {
        public string PrefabId;
        public string FactionId;
    }

    public sealed class AuthoredNativeRelation
    {
        public string AuthoredFactionId;
        public string NativeFactionName;
        public Disposition Disposition;
        public bool BothDirections;
    }

    public sealed class AuthoredRelation
    {
        public string ObserverFactionId;
        public string TargetFactionId;
        public Disposition Disposition;
    }

    public sealed class AuthorableRelationConfig
    {
        public readonly List<AuthoredFaction> Factions = new List<AuthoredFaction>();
        public readonly List<PrefabFactionBinding> PrefabBindings = new List<PrefabFactionBinding>();
        public readonly List<AuthoredNativeRelation> AuthoredNativeRelations = new List<AuthoredNativeRelation>();
        public readonly List<AuthoredRelation> Relations = new List<AuthoredRelation>();
    }

    public sealed class AuthoredFactionIdentity
    {
        public string FactionId;
        public string Source;
    }

    public sealed class AuthorableRelationResolver
    {
        readonly HashSet<string> factions;
        readonly Dictionary<string, string> prefabBindings;
        readonly List<AuthoredNativeRelation> authoredNativeRelations;
        readonly Dictionary<string, Disposition> relations;

        public AuthorableRelationResolver(AuthorableRelationConfig config)
        {
            if (config == null) throw new ArgumentNullException("config");
            factions = new HashSet<string>(StringComparer.Ordinal);
            prefabBindings = new Dictionary<string, string>(StringComparer.Ordinal);
            authoredNativeRelations = new List<AuthoredNativeRelation>();
            relations = new Dictionary<string, Disposition>(StringComparer.Ordinal);

            foreach (AuthoredFaction faction in config.Factions)
            {
                if (faction == null || !ValidId(faction.Id) || !factions.Add(faction.Id))
                    throw new ArgumentException("Faction IDs must be non-empty and unique", "config");
            }
            foreach (PrefabFactionBinding binding in config.PrefabBindings)
            {
                if (binding == null || !ValidId(binding.PrefabId) || !factions.Contains(binding.FactionId) ||
                    prefabBindings.ContainsKey(binding.PrefabId))
                    throw new ArgumentException("Prefab bindings must be exact, unique, and reference a known faction", "config");
                prefabBindings.Add(binding.PrefabId, binding.FactionId);
            }
            foreach (AuthoredNativeRelation relation in config.AuthoredNativeRelations)
            {
                if (relation == null || !ValidId(relation.NativeFactionName) || !factions.Contains(relation.AuthoredFactionId))
                    throw new ArgumentException("Authored/native relations must reference a known authored faction", "config");
                authoredNativeRelations.Add(relation);
            }
            foreach (AuthoredRelation relation in config.Relations)
            {
                if (relation == null || !factions.Contains(relation.ObserverFactionId) || !factions.Contains(relation.TargetFactionId))
                    throw new ArgumentException("Relations must reference known factions", "config");
                string key = Key(relation.ObserverFactionId, relation.TargetFactionId);
                if (relations.ContainsKey(key)) throw new ArgumentException("Each directed relation may be authored once", "config");
                relations.Add(key, relation.Disposition);
            }
        }

        public bool TryResolve(string persistentFactionId, string prefabId, string nativeFactionName,
            out AuthoredFactionIdentity identity)
        {
            identity = null;
            if (!String.IsNullOrEmpty(persistentFactionId) && factions.Contains(persistentFactionId))
            {
                identity = new AuthoredFactionIdentity { FactionId = persistentFactionId, Source = "persistent-profile" };
                return true;
            }
            string factionId;
            if (!String.IsNullOrEmpty(prefabId) && prefabBindings.TryGetValue(prefabId, out factionId))
            {
                identity = new AuthoredFactionIdentity { FactionId = factionId, Source = "exact-prefab-binding" };
                return true;
            }
            // Native buckets never become authored identity. They are available
            // only to explicit pairwise relation selectors below.
            return false;
        }

        public Disposition Get(string observerFactionId, string targetFactionId)
        {
            Disposition value;
            return TryGet(observerFactionId, targetFactionId, out value) ? value : Disposition.Neutral;
        }

        public bool TryGet(string observerFactionId, string targetFactionId, out Disposition disposition)
        {
            disposition = Disposition.Neutral;
            return factions.Contains(observerFactionId) && factions.Contains(targetFactionId) &&
                relations.TryGetValue(Key(observerFactionId, targetFactionId), out disposition);
        }

        public bool TryGetOverride(AuthoredFactionIdentity observer, string observerNativeFaction,
            AuthoredFactionIdentity target, string targetNativeFaction, out Disposition disposition)
        {
            disposition = Disposition.Neutral;
            if (observer != null && target != null && TryGet(observer.FactionId, target.FactionId, out disposition)) return true;
            foreach (AuthoredNativeRelation relation in authoredNativeRelations)
            {
                if (observer != null && observer.FactionId == relation.AuthoredFactionId && target == null &&
                    targetNativeFaction == relation.NativeFactionName) { disposition = relation.Disposition; return true; }
                if (relation.BothDirections && target != null && target.FactionId == relation.AuthoredFactionId && observer == null &&
                    observerNativeFaction == relation.NativeFactionName) { disposition = relation.Disposition; return true; }
            }
            return false;
        }

        static bool ValidId(string value) { return !String.IsNullOrEmpty(value) && value.Length <= 128; }
        static string Key(string observer, string target) { return observer + ">" + target; }
    }

    public static class LivingWorldRelationDefaults
    {
        public const string Pillagers = "LWC.Pillagers";
        public const string Eitrherjar = "LWC.Eitrherjar";
        public const string Draugr = "LWC.Draugr";

        public static AuthorableRelationConfig Create()
        {
            AuthorableRelationConfig config = new AuthorableRelationConfig();
            config.Factions.Add(new AuthoredFaction { Id = Pillagers, DisplayName = "Pillagers" });
            config.Factions.Add(new AuthoredFaction { Id = Eitrherjar, DisplayName = "Eitrherjar" });
            config.Factions.Add(new AuthoredFaction { Id = Draugr, DisplayName = "Draugr" });

            // Compatibility bindings preserve Living World identity despite the
            // installed Souls definitions using Undead and PlainsMonsters.
            config.PrefabBindings.Add(new PrefabFactionBinding { PrefabId = "RRRN_Pillager", FactionId = Pillagers });
            config.PrefabBindings.Add(new PrefabFactionBinding { PrefabId = "RRRN_OrcBrute", FactionId = Eitrherjar });
            config.PrefabBindings.Add(new PrefabFactionBinding { PrefabId = "Krauguk", FactionId = Eitrherjar });

            HostileBoth(config, Pillagers, Draugr);
            HostileBoth(config, Pillagers, Eitrherjar);
            HostileBoth(config, Eitrherjar, Draugr);
            config.AuthoredNativeRelations.Add(new AuthoredNativeRelation { AuthoredFactionId = Pillagers,
                NativeFactionName = "Undead", Disposition = Disposition.Hostile, BothDirections = true });
            config.AuthoredNativeRelations.Add(new AuthoredNativeRelation { AuthoredFactionId = Eitrherjar,
                NativeFactionName = "PlainsMonsters", Disposition = Disposition.Hostile, BothDirections = true });
            return config;
        }

        static void HostileBoth(AuthorableRelationConfig config, string a, string b)
        {
            config.Relations.Add(new AuthoredRelation { ObserverFactionId = a, TargetFactionId = b, Disposition = Disposition.Hostile });
            config.Relations.Add(new AuthoredRelation { ObserverFactionId = b, TargetFactionId = a, Disposition = Disposition.Hostile });
        }
    }
}

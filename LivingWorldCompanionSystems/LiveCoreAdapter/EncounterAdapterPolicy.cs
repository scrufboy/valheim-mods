using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.LiveCoreAdapter
{
    public sealed class EncounterActorIds
    {
        public string PersonId, EncounterId, SiteId, CampId, IncidentId;

        public bool IsValid(out string reason)
        {
            if (!Stable(PersonId)) { reason = "invalid_person_id"; return false; }
            if (!Stable(EncounterId)) { reason = "invalid_encounter_id"; return false; }
            if (!Stable(SiteId)) { reason = "invalid_site_id"; return false; }
            if (!Stable(CampId)) { reason = "invalid_camp_id"; return false; }
            if (!Stable(IncidentId)) { reason = "invalid_incident_id"; return false; }
            reason = "accepted"; return true;
        }

        static bool Stable(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Length > 96) return false;
            for (int i = 0; i < value.Length; i++)
                if (Char.IsControl(value[i]) || value[i] == '|' || Char.IsWhiteSpace(value[i])) return false;
            return true;
        }
    }

    // Process claims prevent a second loaded actor. Persistent ZDO enumeration remains
    // the authoritative pre-spawn duplicate gate and must happen before TryClaim.
    public sealed class PersonSpawnClaims
    {
        readonly HashSet<string> claimed = new HashSet<string>(StringComparer.Ordinal);
        public bool TryClaim(string personId) { return !String.IsNullOrEmpty(personId) && claimed.Add(personId); }
        public bool IsClaimed(string personId) { return !String.IsNullOrEmpty(personId) && claimed.Contains(personId); }
        public bool Release(string personId) { return !String.IsNullOrEmpty(personId) && claimed.Remove(personId); }
    }

    public enum ContactDirective { None, RecordWitnessedDanger, YieldToNativeCombat }

    public sealed class FirstContactFacts
    {
        public bool IsServer, IsOwner, ActorLoaded, PlayerWitnessed;
        public bool AttributedDamage, HostileTargetingActor, NativeCombatCommitted;
        public EncounterActorIds Ids;
    }

    public static class EncounterAdapterPolicy
    {
        // This is deliberately the complete creation and persistent-scan set.
        // Adding a prefab requires adding it here so creation cannot outrun deduplication.
        public static readonly string[] SupportedPrefabIds = {
            "LWC_HumanGuard_v2", "LWC_HumanArcher_v2", "LWC_HumanScout_v1"
        };

        public static bool IsSupportedPrefab(string prefabId)
        {
            if (String.IsNullOrEmpty(prefabId)) return false;
            for (int i = 0; i < SupportedPrefabIds.Length; i++)
                if (String.Equals(prefabId, SupportedPrefabIds[i], StringComparison.Ordinal)) return true;
            return false;
        }

        public static ContactDirective ObserveFirstContact(FirstContactFacts facts, out string reason)
        {
            reason = "invalid_facts";
            if (facts == null || facts.Ids == null || !facts.Ids.IsValid(out reason)) return ContactDirective.None;
            if (!facts.IsServer) { reason = "server_authority_required"; return ContactDirective.None; }
            if (!facts.ActorLoaded || !facts.IsOwner) { reason = "loaded_owner_required"; return ContactDirective.None; }
            if (!facts.PlayerWitnessed) { reason = "local_witness_required"; return ContactDirective.None; }
            if (!facts.AttributedDamage && !facts.HostileTargetingActor)
            { reason = "material_danger_required"; return ContactDirective.None; }
            if (facts.NativeCombatCommitted)
            { reason = "native_combat_committed"; return ContactDirective.YieldToNativeCombat; }
            reason = "witnessed_material_danger"; return ContactDirective.RecordWitnessedDanger;
        }
    }

    public static class AdapterCompletion
    {
        // Deliberately no catch/retry: a consumer exception propagates after one delivery.
        public static void Deliver<T>(Action<T, string> callback, T value, string reason)
        {
            if (callback != null) callback(value, reason);
        }
    }
}

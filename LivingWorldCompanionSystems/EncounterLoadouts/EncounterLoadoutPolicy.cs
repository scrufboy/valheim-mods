using System;
using System.Collections.Generic;

namespace LivingWorld.CompanionSystems.EncounterLoadouts
{
    public static class EncounterLoadoutPolicy
    {
        public const int SchemaVersion = 1;

        static readonly string[] TierKeys = {
            "", "defeated_elder", "defeated_bonemass", "defeated_moder",
            "defeated_yagluth", "defeated_queen", "approved_ashlands_veteran_gate", "approved_deepnorth_veteran_gate"
        };

        public static string Validate(EncounterProfile profile)
        {
            if (profile == null || !Id(profile.PersonId) || !Id(profile.OriginEncounterId) ||
                !Id(profile.OriginCampId) || !Id(profile.FactionId) || !Id(profile.RoleId)) return "stable_identity_required";
            if (String.IsNullOrWhiteSpace(profile.DisplayName) || String.IsNullOrWhiteSpace(profile.LocalHistory)) return "authored_person_required";
            if (String.IsNullOrWhiteSpace(profile.DistantSilhouette)) return "distant_silhouette_required";
            if (profile.Variants.Count == 0) return "variant_required";
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (KnowledgeRecord fact in profile.Knowledge)
                if (fact == null || !Id(fact.FactId) || !Id(fact.SiteId) || String.IsNullOrWhiteSpace(fact.Source) || !fact.Verified)
                    return "verified_local_knowledge_required";
            foreach (LoadoutVariant variant in profile.Variants)
            {
                if (variant == null || !Id(variant.VariantId) || !ids.Add(variant.VariantId)) return "unique_variant_required";
                if (!TierUnlockedBy(variant.Tier, variant.RequiredWorldKey)) return "variant_world_key_mismatch";
                if (String.IsNullOrWhiteSpace(variant.ArmorSilhouette) || String.IsNullOrWhiteSpace(variant.WeaponSilhouette))
                    return "silhouette_required";
                string gearError = ValidateGear(variant);
                if (gearError != null) return gearError;
            }
            return null;
        }

        public static PolicyResult Create(EncounterProfile profile, string variantId, ISet<string> worldKeys)
        {
            string error = Validate(profile);
            if (error != null) return Denied(error, null);
            LoadoutVariant variant = Find(profile, variantId);
            if (variant == null) return Denied("unknown_variant", null);
            if (!Has(worldKeys, profile.DiscoveryWorldKey) || !Has(worldKeys, variant.RequiredWorldKey))
                return Denied("world_key_locked", null);
            var state = new PersonLoadoutState {
                PersonId = profile.PersonId, OriginEncounterId = profile.OriginEncounterId,
                OriginCampId = profile.OriginCampId, FactionId = profile.FactionId,
                RoleId = profile.RoleId, OriginBiome = profile.OriginBiome,
                ActiveVariantId = variant.VariantId, Revision = 1, Life = LifeState.Living,
                Relationship = "wary", Wounds = "none", DeathRecord = ""
            };
            foreach (KnowledgeRecord fact in profile.Knowledge) state.Knowledge.Add(fact.Copy());
            foreach (GearRecord item in variant.Gear) state.PersonalGear.Add(item.Copy());
            return Accepted(state);
        }

        public static PolicyResult Upgrade(EncounterProfile profile, PersonLoadoutState current, UpgradeRequest request)
        {
            string error = Validate(profile);
            if (error != null) return Denied(error, current);
            if (current == null || request == null || !String.Equals(profile.PersonId, current.PersonId, StringComparison.Ordinal) ||
                !String.Equals(current.PersonId, request.PersonId, StringComparison.Ordinal)) return Denied("person_identity_mismatch", current);
            if (current.Life != LifeState.Living || !String.IsNullOrEmpty(current.DeathRecord)) return Denied("dead_person_cannot_advance", current);
            LoadoutVariant target = Find(profile, request.TargetVariantId);
            if (target == null) return Denied("unknown_variant", current);
            LoadoutVariant active = Find(profile, current.ActiveVariantId);
            if (active == null) return Denied("saved_variant_unknown", current);
            if (target.Tier <= active.Tier) return Denied("advancement_must_increase_tier", current);
            if (!Has(request.WorldKeys, target.RequiredWorldKey)) return Denied("world_key_locked", current);
            if (!Has(request.VerifiedFacts, target.RequiredStationFact) || !Has(request.VerifiedFacts, target.RequiredMaterialFact))
                return Denied("legal_upgrade_evidence_required", current);

            PersonLoadoutState next = current.Copy();
            next.ActiveVariantId = target.VariantId;
            next.PersonalGear.Clear();
            foreach (GearRecord item in target.Gear) next.PersonalGear.Add(item.Copy());
            next.Revision++;
            return Accepted(next);
        }

        public static string ValidateRoster(IEnumerable<PersonLoadoutState> people)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (people == null) return null;
            foreach (PersonLoadoutState person in people)
                if (person == null || !Id(person.PersonId) || !ids.Add(person.PersonId)) return "duplicate_or_invalid_person";
            return null;
        }

        public static bool IsRecruitable(EncounterProfile profile, ISet<string> worldKeys)
        { return profile != null && Has(worldKeys, profile.RecruitmentWorldKey); }

        static string ValidateGear(LoadoutVariant variant)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (GearRecord item in variant.Gear)
            {
                if (item == null || !Id(item.ItemId) || !ids.Add(item.ItemId) || item.Quantity < 1) return "valid_unique_gear_required";
                if (item.Tier > variant.Tier) return "gear_exceeds_variant_tier";
                if (!item.PersonalBound) return "personal_gear_must_be_bound";
                if (item.Kind == GearKind.Supply && item.Quantity > 12) return "starting_supply_exceeds_cap";
                if (item.Kind == GearKind.Ammunition && item.Quantity > 40) return "reserve_exceeds_cap";
            }
            return null;
        }

        static bool TierUnlockedBy(BiomeTier tier, string key)
        { return String.Equals(TierKeys[(int)tier], key ?? "", StringComparison.Ordinal); }
        static LoadoutVariant Find(EncounterProfile profile, string id)
        { foreach (LoadoutVariant value in profile.Variants) if (String.Equals(value.VariantId, id, StringComparison.Ordinal)) return value; return null; }
        static bool Has(ISet<string> values, string value)
        { return String.IsNullOrEmpty(value) || (values != null && values.Contains(value)); }
        static bool Id(string value)
        {
            if (String.IsNullOrEmpty(value) || value.Length > 96) return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (!(Char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' || c == ':')) return false;
            }
            return true;
        }
        static PolicyResult Accepted(PersonLoadoutState state) { return new PolicyResult { Accepted = true, Reason = "accepted", State = state }; }
        static PolicyResult Denied(string reason, PersonLoadoutState state) { return new PolicyResult { Accepted = false, Reason = reason, State = state == null ? null : state.Copy() }; }
    }
}

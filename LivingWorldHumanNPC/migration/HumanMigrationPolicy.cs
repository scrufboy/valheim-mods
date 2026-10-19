using System;
using System.Collections.Generic;

namespace LivingWorld.HumanNpc
{
    internal enum HumanMigrationState
    {
        Unstamped,
        LegacyMigrationRequired,
        CurrentAppearancePending,
        Ready,
        Refused
    }

    internal sealed class HumanMigrationSnapshot
    {
        internal readonly string Visual, Composition, Origin, Identity, LabTag;
        internal HumanMigrationSnapshot(string visual, string composition, string origin, string identity, string labTag)
        {
            Visual = visual; Composition = composition; Origin = origin; Identity = identity; LabTag = labTag;
        }
    }

    // Visual revision describes the unchanged native human visual contract only.
    // Root composition has a separate revision and requires an explicit transition.
    // All methods are pure. No native field, health, position, inventory, lab or
    // world-global key is part of this metadata-only migration write plan.
    internal static class HumanMigrationPolicy
    {
        internal const string VisualKey = "livingworld_human_rig_revision";
        internal const string VisualRevision = "native-human-v1";
        internal const string CompositionKey = "livingworld_human_composition_revision";
        internal const string CompositionRevision = "human-components-v1";
        internal const string OriginKey = "livingworld_human_composition_origin";
        internal const string FreshOrigin = "fresh-human-components-v1";
        internal const string LegacyOrigin = "legacy-0.2.x-native-human-v1";
        internal const string IdentityKey = "livingworld_human_npc_v1";
        internal const string Identity = "LWC_HumanScout_v1";
        internal const string LabTagKey = "livingworld_human_lab_v1";
        internal const string NpcTag = "npc";

        internal static HumanMigrationState Inspect(HumanMigrationSnapshot value)
        {
            if (value == null || value.Visual == null || value.Composition == null || value.Origin == null || value.Identity == null || value.LabTag == null)
                return HumanMigrationState.Refused;
            if (value.Visual == "" && value.Composition == "" && value.Origin == "" && value.Identity == "" && value.LabTag == "")
                return HumanMigrationState.Unstamped;
            if (value.Identity != Identity || value.LabTag != NpcTag) return HumanMigrationState.Refused;
            if (value.Visual == VisualRevision && value.Composition == "" && (value.Origin == "" || value.Origin == LegacyOrigin))
                return HumanMigrationState.LegacyMigrationRequired;
            if (value.Composition != CompositionRevision || (value.Origin != FreshOrigin && value.Origin != LegacyOrigin))
                return HumanMigrationState.Refused;
            if (value.Visual == VisualRevision) return HumanMigrationState.Ready;
            if (value.Visual == "" && value.Origin == FreshOrigin) return HumanMigrationState.CurrentAppearancePending;
            return HumanMigrationState.Refused;
        }

        internal static bool MayInitializeFresh(HumanMigrationSnapshot value, bool trustedNewSpawn, bool worldAllowed, bool owner)
        {
            return trustedNewSpawn && worldAllowed && owner && value != null && value.Visual == "" && value.Composition == "" && value.Origin == "" &&
                (value.Identity == "" || value.Identity == Identity) && (value.LabTag == "" || value.LabTag == NpcTag);
        }

        internal static bool MayMigrate(HumanMigrationSnapshot value, bool worldAllowed, bool owner, bool explicitConsent)
        {
            return worldAllowed && owner && explicitConsent && Inspect(value) == HumanMigrationState.LegacyMigrationRequired;
        }

        internal static KeyValuePair<string, string>[] PlanMigration(HumanMigrationSnapshot value, bool worldAllowed, bool owner, bool explicitConsent)
        {
            Require(MayMigrate(value, worldAllowed, owner, explicitConsent), "Saved scout is not eligible for the explicitly requested metadata migration.");
            // Composition is the commit marker, written last. An interrupted first
            // write remains LegacyMigrationRequired and is safe to retry explicitly.
            return new[] { Pair(OriginKey, LegacyOrigin), Pair(CompositionKey, CompositionRevision) };
        }

        internal static KeyValuePair<string, string>[] PlanFresh(HumanMigrationSnapshot value, bool trustedNewSpawn, bool worldAllowed, bool owner)
        {
            Require(MayInitializeFresh(value, trustedNewSpawn, worldAllowed, owner), "Fresh initialization requires a verified newly instantiated, unrevisioned scout.");
            return new[] { Pair(IdentityKey, Identity), Pair(LabTagKey, NpcTag), Pair(OriginKey, FreshOrigin), Pair(CompositionKey, CompositionRevision) };
        }

        internal static string Describe(HumanMigrationState state)
        {
            switch (state)
            {
                case HumanMigrationState.Ready: return "Human-specific component composition accepted; live migration/gameplay NOT certified";
                case HumanMigrationState.CurrentAppearancePending: return "Fresh human composition accepted; awaiting native visual initialization";
                case HumanMigrationState.LegacyMigrationRequired: return "Saved 0.2.x scout is frozen; explicit lwtest migrate is required before companion behavior";
                case HumanMigrationState.Unstamped: return "Unstamped actor frozen; only the trusted fresh-spawn path may initialize it";
                default: return "Unknown, partial or conflicting saved human metadata; frozen without rewriting it";
            }
        }

        static KeyValuePair<string, string> Pair(string key, string value) { return new KeyValuePair<string, string>(key, value); }
        static void Require(bool allowed, string message) { if (!allowed) throw new InvalidOperationException(message); }
    }
}

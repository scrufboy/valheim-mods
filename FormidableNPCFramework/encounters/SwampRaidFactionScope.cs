// SPDX-License-Identifier: GPL-3.0-only
using System;
#if !LWC_FACTION_POLICY_TEST
using HarmonyLib;
using Valheim.CustomRaids.Spawns;
using Valheim.CustomRaids.Spawns.Modifiers.General;
#endif

namespace LWC.Formidable.Encounters
{
    public static class SwampRaidFactionScope
    {
        public const string EventName = "lwc_formidable_swamp_faction_trial";

        // Every other raid keeps its existing faction modifier. This decision
        // is event-scoped, not a global toggle or a guessed prefab family.
        public static bool RunOriginalModifier(string eventName)
        {
            return !String.Equals(eventName, EventName, StringComparison.Ordinal);
        }

#if !LWC_FACTION_POLICY_TEST
        // Optional integration source: called explicitly by the existing owning
        // plugin after Custom Raids is available. No standalone plugin, install,
        // config endpoint, actor mutation or faction writer is added here.
        public static void Install(Harmony owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            var original = AccessTools.Method(typeof(SpawnModifierSetFaction), "Modify", new[] { typeof(SpawnContext) });
            if (original == null) throw new MissingMethodException("Custom Raids SpawnModifierSetFaction.Modify(SpawnContext)");
            owner.Patch(original, prefix: new HarmonyMethod(typeof(SwampRaidFactionScope), "Prefix"));
        }

        public static bool Prefix(SpawnContext __0)
        {
            var raid = __0 == null ? null : __0.RaidConfig;
            string name = raid == null || raid.Name == null ? null : raid.Name.Value;
            return RunOriginalModifier(name);
        }
#endif
    }
}

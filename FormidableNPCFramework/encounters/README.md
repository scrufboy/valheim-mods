# Swamp faction manual trial candidate

The full behavior trial is **blocked** until the hostile Neckman Shaman and tiny blowgun creature have real registered prefabs **and the native shaman summon/control executor is wired and verified for this event, with verified preservation of actor-scoped faction relations**. `swamp_inventory.json` records existing identities and missing capabilities. It is a dated audit receipt, not a live registration check or runtime configuration. No config has been installed and no encounter has been run by this work.

The builder uses existing **Custom Raids 1.8.2** fields from installed `custom_raids.supplemental.lwc_formidable_souls.cfg`. Tooler `src/LivingWorldTrainerPanel.cs` already uses `event <name>` and `stopevent` for manual RandomEvents. No new spawning framework or UI is added. Bonemass remains unchanged.

After exact prefab registration and native executor integration have been verified, update the inventory with that evidence and its actual configured encounter summon probability, then compile:

```powershell
python build_swamp_trial.py --inventory swamp_inventory.json --output candidate/custom_raids.supplemental.lwc_formidable_swamp_trial.cfg
python -m unittest discover -s . -p test_swamp_trial.py -v
```

The compiler requires all five actor identities, native executor readiness evidence, the exact event and beast identity, a verified single-decision encounter latch, and a finite configured native chance within 0–100 percent. This receipt does not enable that executor or transmit probability. Those must already be wired through the actual runtime contract; there is no invented Custom Raids probability field or sidecar configuration here.

| Role | Raid concurrent cap | Raid group size | Current identity |
| --- | ---: | ---: | --- |
| Neckman | 3 | 1–2 | `RRRN_Neckman` (legacy RRR actor) |
| Marauder | 3 | 1–2 | `LWC_MarshMarauder_Prototype` |
| Tiny blowgun skirmisher | 7 | 2–3 | Missing |
| Neckman Shaman | 1 | 1 | Missing |
| Behemoth | Native shaman owns summon | No raid spawn slot | `LWC_UndeadColossus_Test` |

**Custom Raids never directly spawns or replenishes Behemoths in this full trial.** Its optional arrival belongs to one native shaman/encounter summon decision using the stable encounter latch (`CommitBeastSummon` policy). An encounter roll that produces no beast remains consumed; replacing or respawning a shaman must not reroll that same encounter. The full trial remains blocked until the actual native executor handles this. No separate direct-beast fixture is shipped here.

The raid stages/replenishes the four ground roles over 120 seconds with a 70–95 metre approach ring. The native executor remains responsible for beast summoning, shield eligibility, orange-eye/tether effects, damage interruption, berserk allegiance, retreat and rebind. No Faction field is emitted. Native control must own scoped family relations and later beast targeting. The ordinary Beast prototype uses Undead outside this event. No source prefab faction is edited here. Registration alone is not combat acceptance or a completed RRR-free Formidable conversion.

The builder refuses missing/duplicate/unsafe actor IDs, incomplete executor evidence and existing output; it compiles entirely before touching the candidate path. It does not install or launch anything. Synthetic test fixture identities/readiness never enter the shipped inventory.

Once reviewed and installed through normal packaging, run `event lwc_formidable_swamp_faction_trial`; `stopevent` ends the timer. Existing enemies may remain. Use existing arena cleanup and verify an empty area between repeats. Native encounter teardown must retire its latch so a deliberately new trial can make its own single decision.

Live acceptance remains pending: capture event identity, actor IDs, counts and failures through existing telemetry; observe ranged/close behavior and eligible-ally shields; confirm one summon decision per encounter, damage breaks control, Beast attacks former allies, and Shaman retreats/rebinds. Repeat with native probability configured to zero and then 100 to cover both branches, verifying replaced shamans never add a second summon. Current pure policy is not proof of native execution.

## Verified Custom Raids compatibility blocker

Read-only Mono.Cecil inspection of the installed `Valheim.CustomRaids.dll` found that omission of `Faction` **still forces Boss faction**. `RaidEventConfiguration` constructor IL_0163 sets its default string to `Boss`. `SpawnModifierSetFaction.Modify` initializes faction enum value 8 at IL_006d; when both spawn and raid faction strings are blank, IL_0075 jumps to IL_009c, which unconditionally writes `Character.m_faction`, followed by `ZDO.Set("faction", ...)` at IL_00b4. There is no identity-preserving blank branch.

The candidate contains no global or per-spawn Faction field, and its compiler separately blocks until actual actor-scoped faction preservation has been implemented and verified. Merely deleting the old PlainsMonsters line does not resolve this installed modifier. No installed mod patch, blanket replacement faction or alternate spawn executor is introduced here. The optional source adapter below addresses this exact modifier but still requires integration and live verification. This compatibility finding is an additional blocker, alongside missing actors and the native control executor.

## Narrow optional source adapter

`SwampRaidFactionScope.cs` targets the inspected public method `void Valheim.CustomRaids.Spawns.Modifiers.General.SpawnModifierSetFaction.Modify(SpawnContext)`. Its public context exposes `RaidConfig.Name.Value`. An optional Harmony prefix skips only that faction modifier for exact ordinal event name `lwc_formidable_swamp_faction_trial`. Null contexts, other raids, case variants and near-matching names retain the original modifier. It does not assign factions, suppress other spawn modifiers, or affect ordinary raids. Call `Install` explicitly from the existing owning plugin after Custom Raids is present; no standalone plugin is introduced. The owning plugin manages its normal Harmony teardown.

This source compiled against installed Custom Raids/Harmony and the game SDK. Eleven event-routing checks passed. It is **not installed or wired**, and live spawn/reload behavior is untested, so `raid_faction_passthrough.verified` remains false. Pure routing tests and a successful source compile do not clear this runtime gate.

Inspected installed DLL SHA256: `12228867767e49c2e13ad642bc40b7a5036f034239eda44fbc5de740de5b784d`. Both installed `CharacterSetFactionPatch.AssignFaction` methods read the stored faction key using default `-1` and skip assignment when negative. Skipping the initial modifier prevents its forced-faction key on newly spawned trial actors. The adapter deliberately does not erase keys on previously spawned actors or interfere with native control persistence. Use fresh trial actors and verify native family/control relations survive ownership and reload before marking the gate ready.

Reproduce source compilation and routing checks with `Verify-FactionScope.ps1 -ManagedDirectory <Valheim/valheim_Data/Managed> -BepInExCoreDirectory <profile/BepInEx/core> -CustomRaidsDll <installed/Valheim.CustomRaids.dll>`. Outputs stay in temporary storage. No game/app launch or installation occurs.

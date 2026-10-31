# Changelog

## 2.6.1

- Added first-class Raids-page buttons for the current `lwc_formidable_arena` and `lwc_formidable_souls_swamp` Elder-pattern FormidableAI stress raids.
- Added the standard noon setup beside the current training-raid controls and kept the existing stop-event control explicit.
- Preserved the older bounded Pillager and Krauguk matchups, every vanilla raid control, and all non-raid Tooler pages.

## 2.6.0

- Living World pages reorganized from 14 to 7: Companions, Raids, Player, Spawn, World, Build, Admin.
- Every control appears exactly once (lwparty had been on six pages); destructive actions stay on their own page behind the two-click confirmation; the separate Danger page is gone.
- Removed about twenty buttons that called the retired 0.x `lwtest` commands; added the 1.0 companion commands: lwparty new/clean, lwstatus, lwheal, lwsummon, lwgoto.
- Site Survey moved onto the Build page with terrain tools; Console moved onto Admin.
- The four original Tooler pages and the Tooler core file are unchanged.

## 2.5.0

- Added an isolated, controller-usable Site Survey module without removing or replacing any existing Tooler page.
- Added exact four-corner capture from live Valheim ground, quadrilateral validation, best-fit campus geometry, bounded grid sampling, water/biome/blocker evidence, cut/fill estimates, removable owned preview markers, and text/JSON exports.
- Kept terrain application visibly locked and absent until a later build can enforce backup receipts, second confirmation, bounded journaling, cancellation, no combat, and no peers.

## 2.4.1

- Established the existing Tooler shell, toolbar, page order, and capabilities as a build-checked interface contract.
- Registered each Living World responsibility as an independently identified module instead of maintaining a fragile page-index switch.
- Preserved the complete original Tooler surface and every existing Living World page and command.

## 2.4.0

- Added real, manually triggered Souls of the Land RandomEvent matchups to the Raid page.
- Added a bounded two-to-three Common Folk Pillager trial and a two-combatant Krauguk Legion trial.
- Kept both events out of the random raid pool and preserved all native and specialized raid controls.
- Packaged the supplemental Custom Raids configuration and declared Custom Raids 1.8.2 as a dependency.

## 2.3.0

- Restored the complete Living World trainer surface inside the fifth Tooler page.
- Restored lifecycle, community, combat, party, native raid, player, item/spawn, time, environment, terrain, map/admin, danger-confirmation, and console pages.
- Added the previously missing Midnight control and direct clear, rain, thunderstorm, and fog controls.
- Kept all four proven original Tooler pages and their feature sets intact.

## 2.2.0

- Replaced the inherited ValheimTooler plugin identity with the Living World C.O.R.E. Tooler identity.
- Rebranded the visible title, assembly metadata, configuration names, and runtime log prefixes for Brian / scrufboy.
- Removed the legacy ValheimTooler loader plugin from the package.
- Kept MIT attribution in the documentation and license only.

## 2.1.0

- Restored the complete original ValheimTooler UI, colors, tabs, catalogs, and controls.
- Added Living World as a fifth section inside the original toolbar.
- Added direct party, follow, hold, status, recovery, and wave controls.
- Preserved the full original item and prefab catalogs.
- Silenced only the repeated log lines for invalid unfinished item sprites; valid catalog entries remain.

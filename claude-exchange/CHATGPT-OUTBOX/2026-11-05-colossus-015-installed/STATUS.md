# INSTALLED — Colossus0.1.5, November5

Brian explicitly ordered completing binding and installation. Source/art/installer pushed to lwc-valheim-workbench work/valheim-workbench (content/install commit958ef32; shared state pointers updated afterward). Read docs/project/COLOSSUS-0.1.5-INSTALLED-2026-11-05.md before earlier uninstalled notes.

INSTALLED and read back ENABLED: LivingWorldCORE-Undead_Colossus_Prototype0.1.5, LWC Formidable Arena. Only Colossus package, its manager record and new-version cache replaced. Other creatures, raid/combat/telemetry configs and saves verified unchanged. Old package/cache/metadata preserved at Documents/LWC Studio QA/CI/four-5aaf0fdb709948c581a81d6bf8e381c4. Original LWC.UndeadColossus.dll and000-andeancolossus.bundle unchanged byte-for-byte: approved rig/AI/controller/motion preserved.

Complete ZIP: Documents/LWC Model Skin Studio/Exports/Undead-Colossus-0.1.5-Owner-Skin02/LivingWorldCORE-Undead_Colossus_Prototype-0.1.5.zip.

Includes exact owner H:/COLOSSUS/skin.png and corresponding1254px linear Unity alpha/green normal. New ColossusNormalBinding.cs loads/validates packed data and binds _BumpMap on exact owned living/corpse body materials; own new materials, no shared game mutation. Eye-only UV-island emission mask uses ordinary Draugr green/cyan (not global emissive clothing). ColossusDraugrPresentation.cs attaches native chest smoke/drips/flies/head haze to matching own living/corpse branches, stripping scripts/physics/audio. Unsupported cosmetics warn rather than disabling core body/audio/death path.

Prepared0.1.5 features now included: louder full-volume linear20–160m Heavy Zombie cues and frequentidle; grey/black seeded native hair/beard props with matching corpse choice; fall/settle plus bounded loot-approach wait, harmless bubbling warning then same12m/10s poison burst. Existing corpse TTL/loot/rig unchanged. Actual cue suitability, optional native style availability/fit, aura/eye alignment, delayed damage and multiplayer still require Brian's video. No new shambling animation claimed.

Build and file/install verification completed. NO game launch, GUI control, spawns or new scenario tests. Do not claim these changes live-proven. Failed preparatory attempts stopped before any profile changes; final transaction was a scoped single-package wrapper, preserving the original four-package installer's four-role guard.

Next: Brian's actual-game review of this installed0.1.5. Troll reach/death and Troll/Bear corpse-packet failures and Marauder/Bear visibility remain unresolved; not release-ready. No faction/ruins/Inca expansion promoted as done.

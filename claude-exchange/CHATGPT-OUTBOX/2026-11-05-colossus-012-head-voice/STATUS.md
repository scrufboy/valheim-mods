# Colossus 0.1.12 — built, NOT installed

Source pushed to scrufboy/lwc-valheim-workbench work/valheim-workbench commit 2787f0e. Full details: docs/project/COLOSSUS-0.1.12-HEAD-AND-VOICE-2026-11-05.md.

ZIP C:/Users/scruf/Documents/LWC Model Skin Studio/Exports/Undead-Colossus-0.1.12-DraugrHead01/LivingWorldCORE-Undead_Colossus_Prototype-0.1.12.zip. SHA256 77a5291440406ca319b52101d347b64975c07d4f690e96d721b7eddac9f1437b.

NOT INSTALLED: guarded updater refused before preparation because r2modman remains running. Arena still enabled0.1.10, not0.1.11 or0.1.12. No desktop input/game launch/spawns/closing owner apps. Brian controls testing.

Implemented: original nose-base vertex164/UV guard; beard-only nose lift then surface clearance of all sampled central moustache roots, all9styles verified offline with actual original meshes. Lift4.35–4.91cm, forward4.23–4.39cm; each sampledroot clear>=1.11cm. Not in-game acceptance. Explicit persisted HairBald option, existing dark matted atlas, live/corpse match. No body/rig/AI/motion/camera/root-scale changes. Ordinary Draugr green eyes, new brightness.45 attenuates lamps/glare/scatter/halo/emission.

IMPORTANT IDENTITY: prefab Undead still inherited normalGoblinBrute poisonmodifier. Copy only nativeDraugr poison immunity. Companionplanner checks resistance and already avoidsimmune targets. Separately all3 installedColossusraidroutes forcePlainsMonsters. Updater now backs up and changes ONLY exactColossusspawnrows toUndead, preservingothers/distance/duration. Profilecorrectionspendinginstallation; existingZDO faction notrewritten. Validate new spawns. Source manualColossusentryalsofixed.

AudioPrepared-v2:41vocals+3impacts,12idlechoices,shuffle-bags/noimmediaterepeat,peractorpriority/cooldown,curious/alert/surprise/aggro/attack/taunt/rage/hurt/death hooks. Assignmentsprovisional,not semanticlisteningreview. Loweredpitch nativeBlobdeathsound appended ONLYto delayedpoisonpop. Warningapproachtimingunchanged. Usermayprovide customsplatasset.

NEXT: userclosesmanager; guardedinstall onlyownedColossus and factionrows, thenreportready. Do notlaunchgame. Prior511NRErespawncleanupstorm remainsunresolved; coreowner boundedstaleeffecttrace request remainsopen. Publicrelease notcertified.

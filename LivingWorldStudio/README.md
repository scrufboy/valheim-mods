# Living World Studio Lab 0.1.0

By Brian / scrufboy · Living World C.O.R.E.

An independent photographic staging utility: place a timber Portrait Hall Door
with the hammer, interact to enter a 32 × 32 × 12 m studio, dress the set with
decorative vanilla scenery, and return through the exit door.

**This is the first player/scenery test build, not the complete NPC photography
release.** Companions, live enemies, pose control and multiplayer are disabled.
Existing companion combat, orders, identities and equipment are not modified.

## Install and first test

1. Exit Valheim completely. Create a separate disposable mod-manager profile with
   BepInExPack Valheim and Jötunn. Do not replace any NPC or camera DLL.
2. Import the lab ZIP, or copy its BepInEx/plugins/LivingWorldStudio directory into
   that profile. The only mod binary supplied is LivingWorld.Studio.dll.
3. Create **both** a disposable character and world named **LWC_StudioLab_v1**.
   Leave Start Server off. The entrance refuses production worlds and remote peers.
4. On flat, clear ground, acquire a hammer and ten Wood. For a new disposable
   character, native console devcommands / spawn Hammer / spawn Wood 50 can supply
   these. The mod itself does not grant items or resources.
5. Select **Portrait Hall Door (Studio Lab)** in the hammer's Misc build category.
   Place it, then use the normal mapped Interact button, as with a dungeon entrance.
6. After native teleport completes, verify the room floor/walls and green backdrop.
   Keep the first visit short. Return through the wooden door behind the arrival
   position, then verify your exact original position and facing before further tests.

The reference build targets genuine cached **Valheim 1.0.17**, Jötunn **2.30.2**
and BepInEx **5.4.22** APIs. This has not been installed or run in Valheim here.
Current installed-game compatibility, visual quality and controller interaction
need the live checklist below; compile success does not establish them.

## Controller operation and scenery

Use ordinary gameplay movement, look and Interact; no camera mod or keyboard
shortcut is required for the room controls. Eleven timber consoles line the entry
wall. Aim at a console to read its action in the native hover text:

| Console | Action |
| --- | --- |
| Exit | Return to the recorded origin |
| Green / Blue / Neutral | Change private backdrop colors |
| Choose scenery | Cycle bushes, trees, table, chair, bench, chest, workbench, wall and pole |
| Placement pad | Cycle the 16 numbered positions, four columns by four rows |
| Rotate | Cycle scenery yaw in 45° steps |
| Scale | Cycle 100%, 75%, 50%, 25%; use smaller trees where needed |
| Place | Add the selected decorative visual at the selected pad |
| Clear | Remove all placed scenery |
| Pads | Hide/show subject pad markers for clean screenshots |

The separate wooden exit door performs the same return as the Exit console.
Scenery is bounded to 32 props and must fit inside the room; unsupported or oversized
assets are refused visibly. It is render-only: a tree cannot be cut, a chest cannot
store items, and a workstation cannot craft. Props reset on exit/reload. No harvested
materials, loot, creatures, AI, fire or ambient spawns are created by the studio.

Green and blue source colors are centered on #00FF00 and #0000FF. Neutral uses
#808080. Real screenshot pixels will differ with shading and graphics settings.
Private native material copies, neutral lights, a non-forced native environment
volume and a gameplay-camera render scope aim to remove weather/fog/world lighting.
Allow the native environment transition to settle before capture. External forced
weather, postprocessing or camera mods remain compatibility tests, not silently
overridden settings. The first shell is flat, wood-derived geometry; a seamless
cove is not implemented or promised by this build.

Use normal first/third-person screenshots or an OBS game-capture source. Save an
unkeyed master before chroma keying; use PHOTOGRAPHY-PLAN.md for release shots.
The prior CameraMode/free-camera mod is neither required nor changed.

## Saved return and recovery

Before movement, the mod flushes a checksummed return journal outside the world
save and mirrors the record into character data. Each record contains the world,
character, session, exact position and full rotation. Native non-distant teleport
is used because its missing-floor path reverts instead of inventing a fallback Y.

Rejoining a pending session initiates return automatically. Failed movement,
removed entrance, duplicate entrances and an unfinished visit do not delete the
return record. A blocked or unloaded origin remains pending; the room is retained
instead of hiding an unsafe offset. The current lab slot is high above the same
world coordinates; general dungeon-slot allocation is a later multiplayer gate.

Diagnostic fallback: **lwstudio status**, **lwstudio recover**, **lwstudio exit**.
Palette/clear equivalents: lwstudio green / blue / neutral / clear.
If recovery reports an obstruction or corrupt journal, retain the profile and
records for inspection. Do not remove the mod while the character is inside.
Recovery retry/error messages are rate-limited. A small completed-session journal
is retained to prevent an old character-save mirror from replaying a finished trip.

To remove: return outside, confirm status reports outside, save/quit, then remove
only LivingWorldStudio from the stopped disposable profile. No other mod needs it.

## Verification required before promotion

- Door registration, controller hover/interaction, arrival and exact return.
- Green/blue/neutral captures: no texture grain, wood gaps, light seams, weather,
  shadows or unacceptable spill; normal first/third-person and OBS capture.
- A scaled tree, each supported prop, 16 pads, marker toggle and 32-prop limit.
- Save/quit while inside, restart, verify automatic exact return and cleanup.
- Delete the exterior door while outside; duplicate doors must not share an active
  trip. Deliberately fail a teleport in a disposable copy; retain recovery data.
- Verify native environment and lighting resume outside the studio.
- Verify loaded Living World companions cause admission refusal, not an AI change.

Offline result: real-reference compilation passed; journal tests passed 28
assertions. Live result: **NOT RUN**. No DLL was installed into a game/profile.

MIT source; no extracted vanilla assets, game libraries or third-party mod DLLs
are distributed. Vanilla geometry/materials/shaders are accessed at runtime.

Source and design: https://github.com/scrufboy/valheim-mods/tree/design/photographic-studio/LivingWorldStudio

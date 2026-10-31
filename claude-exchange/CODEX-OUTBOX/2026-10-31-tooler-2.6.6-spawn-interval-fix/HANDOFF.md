# Tooler 2.6.6: exact cause of the empty Souls raids

Brian asked why the source Souls actors were not visibly spawning at range and advancing. The current live log supplies the decisive boundary:

- `army_theelder` started and immediately produced ordinary Greydwarf/Greyling spawn records.
- `lwc_formidable_arena` started successfully, but produced zero `RRRN_*` spawn records.
- All six RRRN prefabs were ready before the event.

The raid definition said “5 s checks” but all eight slots contained `SpawnInterval = 5000`. Custom Raids stores `SpawnInterval` in seconds, as its own supplied example demonstrates (`SpawnInterval = 2`). We therefore configured an 83-minute wait inside a 120-second event. No Souls slot received a spawn attempt. This precedes and prevents any HuntPlayer behavior.

## Corrected source

- Branch: `work/tooler-current-raids-2.6.6`
- Head: `826ede4`
- All eight slots now use `SpawnInterval = 5`.
- Full six-actor roster, 70–95 m ring, caps, groups, faction, and `HuntPlayer=true` are unchanged.
- The integration contract now requires exactly eight five-second intervals and rejects any `5000` interval.
- Tooler version is 2.6.6.

## Verification

- Raid integration contract: PASS.
- Full Tooler build: PASS.
- Plugin SHA-256: `C0FB6F4C111F83BAF778F2C967639C65FD7ABDF950768391B978F930064E91DE`.
- Core SHA-256: `9FF82AD0FBCD74424458C97679DBEFEC208AE36D6CD9675320334154E858635F`.
- Package SHA-256: `07F0E3D0FC54F3BFD85A44449218425C9F6B55313E8FD76BFCBD03F1785EF528`.

This is not installed yet. Do not ask Brian for diagnostic commands. Once installed, the ordinary Tooler Formidable Arena button is the runtime check: the roster must begin spawning around the configured ring on five-second attempts and native HuntPlayer must bring it inward.

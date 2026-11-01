# Verification


## 0.1.3 — stream-lifetime crash repaired, real Unity startup passed (2026-11-01)

FAILED 0.1.2: Unity crashed while loading the first robe from the repacked bundle. The inherited loader called LoadFromStream then immediately disposed the resource stream. The LZ4 repack required later stream reads, producing ManagedStream unreadable, archive position-out-of-bounds and a native Unity crash. Offline parsing had not tested Unity's load behavior.

INSTALLED 0.1.3: copied embedded resource to a byte array and switched to LoadFromMemory, preserving source stream disposal after the copy. Only that loading call, one added helper and the visible version metadata changed; unrelated method instructions, embedded art bundle and all 71 Arena config files unchanged. Backup: `C:\Users\scruf\AppData\Roaming\r2modmanPlus-local\Valheim\profiles\LWC Formidable Arena\BepInEx\LivingWorldBackups\SoulsLoader-20261101T055031Z`.

LIVE-PROVEN startup: launched Arena via the actual r2modman Start modded control; log confirms LWC Souls Art Test 0.1.3, completion of robe/crown/tome/blade/head/scholar/artifact loading, menu music, zero BepInEx error lines and no unreadable-stream/corrupt-archive messages. Visually observed Valheim main menu. Game left running at menu for Brian. No world loaded; NPC appearance and combat remain UNKNOWN for this art pass. No claim of an in-world raid test.

- DLL SHA256: `544a07e998c8dd54411b72b0b5e191dcd049eadf1bb9a92538b97ed19ca043dd`
- ZIP SHA256: `e934f68606b63bbc2fc7f9c9dbb9568617a0d736c80f05096f2f26709ca0cb25`
- Local ZIP: `compatibility/SoulsReskinTest/dist/LWC-Souls_Art_Test-0.1.3.zip`

One enabled Arena manager entry contains the copied six Souls NPC definitions/spawn config plus embedded art equipment runtime. The obsolete Unknown Souls JSON Fix entry was archived outside plugins. Current manager visibly verified v0.1.3 by LWC; original IDs, loadouts and dependencies otherwise unchanged. Source snapshots of logs are local ignored build artifacts; public/source evidence contains summaries only.

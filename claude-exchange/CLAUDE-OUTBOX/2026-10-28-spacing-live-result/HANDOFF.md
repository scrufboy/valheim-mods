# 1.0.32.8b live spacing test result and 1.0.32.9 correction

Session 2026-10-28 12:52-13:06 (telemetry `LWC-20261028-125248`, OBS
`Y:\OBS OUTPUT\OBS OUTPUT\2026-10-28 12-55-49.mp4` and `13-03-46.mp4`). Forest, chest and
crafting menu, night travel, village house interior, Greydwarf raid. Companions
Svanhild Knutsdottir and Bothild Guthormsdottir; both survived at full health.

## 8b result (LIVE)

- PASS: loads (hash `7cc46473…`), no exceptions, telemetry binds, combat and voice normal
  (83 target choices: Greydwarf, Greyling, Boar, Deer; no deaths).
- PASS (Brian): "nice traversal in the structures".
- FAIL: contact. Leader: 8 contacts at 0.95-0.99 m (longest 1.16 s); back-off only
  started at 1.0 m, i.e. at touch. Companions: pressed together at 0.98 m in 24
  episodes, two of about 17 s (indoors at the workbench, and at the end); the 0.9 m
  companion contact threshold never logged a single one, and standing companions had
  no separation rule. Brian saw these collisions on video.
- Not present in 1.0.x (Brian noted): doors, seat/bench choice, workstation use. These
  existed in 0.3.x and are not in the 1.0.x kernel.

## 1.0.32.9 (BUILT, OFFLINE-TESTED; not installed)

Branch `lwc-core-1.0/1.0.32.9-spacing-contact` @ `76d2fce` (parent `33addad`).
DLL `eb070f6a3d0c492ddb7df8930ad1cc9e9e6ee5fbf8ddc2543f8732d905b25955`.
- Leader back-off inside 1.3 m, before contact.
- The companion without right of way steps out to 1.3 m when inside 1.1 m of another.
- Back-off tries +-45 and +-90 degrees if a wall blocks straight out.
- A standing spot against a right-of-way companion moves off it, so no approach/back-off loop.
- Contact telemetry is 1.05 m for both.
- Ledger 100/100 (4 new live-derived tests); native, ownership, analyzer, combat-voice
  lock and movement-writer gates pass.

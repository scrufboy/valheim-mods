# Verification



## Arena embedded Souls reskin 0.1.1 — 2026-11-01

INSTALLED in LWC Formidable Arena on Brian's explicit instruction: “so now in our arena... just install this Souls testpack, enable it, check all of the data, sim test..then I will load and go look”. Equipment DLL SHA256 `6f931d1e1f37c31913462ae0d2bd4244d771d7cadea7a7dbab5533a24cafb021`; ZIP SHA256 `4e62c83dbfc5be95e7850aa9290f0e66450dd4991b3bf81c1c56f10f4b807671`. The existing managed equipment slot remains enabled and is displayed as LWC Souls Embedded Reskin Test 0.1.1. Exactly one active plugin GUID copy. Original equipment DLL remains backed up under BepInEx/LivingWorldBackups/SoulsReskin-20261101T051945Z; the temporary 0.1.0 test is backed up at SoulsReskin-20261101T052307Z.

OFFLINE-TESTED: 31 new texture pixel/mipmap/color-space/filter checks; 108 material assignments; 66 prefab reference walks; six unchanged NPC definitions; no new dangling local references. Registration instructions unchanged. Core stays approved 1.0.32.29 (`d376d63b…`); 417 other profile files unchanged, including raid and NPC config. Color maps use sRGB, data maps linear, all new maps bilinear. This fixes an inherited template sampling error found before gameplay.

In-game loading, rendering and combat are UNKNOWN for this new bundle: no game launched. Brian will inspect the installed art. Existing Pillager and Servant loadouts reference repainted Sage gear; other actor loadouts unchanged. See CODEX-OUTBOX/2026-11-01-souls-reskin-arena-installed/.

Offline simulation means serialized prefab/material reference traversal, not Unity execution, physics, AI or shader rendering.

# Live: Tooler 2.6.1 raid launch session (1.0.32.29, Arena), 11:32–11:52

Telemetry `LWC-20261031-113258-LWC_HumanLab_v1-part00.jsonl` (1,177 s); `LogOutput.log`; OBS `2026-10-31 11-32-21.mp4`,
`11-40-22.mp4`, `11-48-21.mp4`.

- **LIVE-PROVEN, Tooler launcher works:** Tooler 2.6.1 loaded; `Random event set:` `army_theelder` (11:34:00),
  `lwc_formidable_arena` (11:36:38, 11:41:44, 11:44:46), `lwc_formidable_souls_swamp` (11:39:09, 11:41:42). Spawned:
  Pillager, WoodsSoul, NinoBosque, Neckman, Servant (all from the raid configs).
- **FAILED as raids (Brian):** raid mobs did not come in; Brian needed a map aid to find them. The installed config has
  `HuntPlayer = false` on every entry plus a 70–95 m ring. **Decision recorded:** raids hunt the player. Change every
  entry to `HuntPlayer = true` and update the Tooler package check that currently requires `HuntPlayer=false`.
- **Combat:** 2 companions, 0 deaths, 76 kills (Pillager 21, WoodsSoul 7, Neckman 1, Servant 1, plus vanilla).
  - Damage taken from Souls was low: Pillager median 9 (n=16), WoodsSoul 3 (n=14).
  - Heavies 16 started, 16 landed (median 147).
  - Brian: "we owned them". The Souls NPCs need FormidableAI help (decision recorded).
- **A1:** sight notices n=113, median 53 m, max 73 m; 122 commitments (86 targeting the leader); CPU median 0.66 ms/s;
  native target before commitment 7 (5 late, median 0.75 s); 0 loss, 0 faults.
- **Known FAILs:** churn 11.4/min, shield in calm 23%, dead time 8%, bow-for-distant 21% melee, torch flicker 2.
- **Foreign NRE burst, second occurrence:** 299 × `ZNetScene.RemoveObjects` NRE, 11:45:38–11:45:55, about 20 s after
  `Spawned RRRN_Pillager` (11:45:19) in an active fight. Session 1.0.32.29-1 had the same signature about 2 min after
  Pillagers spawned, with no bombs thrown. Both times Souls actors were present. Inference: a Souls/RRRCore actor's
  removal path destroys a `ZNetView` without `ZNetScene.Destroy`. Living World logged no fault in either window.
  **Needs source tracing in the RRRCore compatibility layer** before longer raid sessions.

## Update: the NRE bursts very likely come from `RRRN_OrcBrute` (Brian: "we saw no legion; perhaps it's that and its error")

- **Never spawned:** across the 1.0.32.29 Arena sessions the log shows `Spawned RRRN_*` for Pillager, WoodsSoul,
  NinoBosque, Neckman and Servant, but **never** `RRRN_OrcBrute`. It's in both training raids (MaxSpawned 1, otherwise
  identical to the NinoBosque slot that spawned) and in the Souls pack's world spawner `[WorldSpawner.9004]`
  (Swamp, Plains, BlackForest).
- **Both bursts are where OrcBrute spawns would run:** 11:09:22 on entering BlackForest (natural spawner) and 11:45:38
  during the third `lwc_formidable_arena` (OrcBrute slot). Human Lab sessions (no Souls pack) had no such errors.
- **Mechanism fits:** `ZNetScene.RemoveObjects` → `Component.get_gameObject` NRE means a registered `ZNetView` whose
  GameObject was destroyed outside `ZNetScene.Destroy`. A spawn that is torn down after registration would also never
  reach the vanilla "Spawned" line.
- `rrrnpcs.custom.Orcs.json`: `sNewPrefabName = OrcBrute`, faction `PlainsMonsters`, its own appearance/hair settings.
  The working Pillager (`CommonFolk.json`) is faction `Undead`. INFERRED: OrcBrute's RRRCore design/visual step fails.
- **Confirming test (one command):** `spawn RRRN_OrcBrute` in an empty spot. Expect the NRE burst and no living actor.
- **Interim:** disable the OrcBrute slot in both raids and `[WorldSpawner.9004]` until the compat layer repairs it, so
  the raid data stays clean.

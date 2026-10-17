**Blocker before anything else: your handoff and your repo disagree about which build is current.**

| Source | Version | DLL SHA-256 |
|---|---|---|
| Handoff (this file) | 0.3.31 "combat-arbitration" | `7443EA84…775A0` |
| GitHub repo source and `archive-verification.json` | **0.3.34** "Automatic Wave Site Search Lab" | `8575846E…C3E6` |

The 0.3.31 hash appears nowhere in the repo, and the 0.3.31 source folder (`work/LivingVillage/human-npc-0.3.31-combat-arbitration`) wasn't supplied. So I can't tell which DLL is actually in your profile. Three successors may also exist past the build the handoff names. Its rule "do not create 0.3.32 as a broad rewrite" may already have been broken by 0.3.32–0.3.34.

## 1. The non-regression requirement, in engineering terms

- **Preserve before you change.** Every accepted behavior gets an explicit test plus a source/hash check that stays in the gate permanently. A gate can never pass because the behavior it protects was removed.
- **One owner per resource.** Movement, facing, hands, target, utility, doors, and posture each have exactly one controller at a time. A repair changes only the owning layer.
- **Native outcomes are the proof.** Selecting a bow proves nothing. Proof is `StartAttack` firing, the arrow count going down, and a projectile appearing.
- **Each claim is labeled** source-inspected, offline-tested, installed, or live-proven, and only live-proven counts toward promotion.
- **No new candidate without a named failing boundary,** backed by live evidence.

## 2. Evidence inventory

| Evidence | Have it? | Proves |
|---|---|---|
| Handoff text | Yes | Claims about 0.3.31 (offline-passed, installed) and the 0.3.17–0.3.19 live failure audit |
| Repo source at 0.3.34 | Yes | Source-inspectable only; its own build script records `UnityRuntime='NOT RUN'` |
| 0.3.31 source/build | **No** | — |
| Installed profile DLL hash | **No** | — |
| Startup log, telemetry, video for 0.3.31 or 0.3.34 | **No** (the repo deliberately excludes them) | — |
| 0.2.2 live observations (visual, path arrival, attributed hit, quit/reload) | Referenced in `COMPATIBILITY-DECISION.txt` | Live-proven for **0.2.2 only**; that doc says they don't certify the 0.3.x composition |

**Live-proven passes for any current build: none.** The only live evidence for the 0.3 line is the failure list from 0.3.17–0.3.19.

## 3. Protected behavior matrix

All 13 contracts in the handoff's matrix are **Unknown** for the current build. The last live evidence I have is:

| Contract | Last live evidence |
|---|---|
| Identity/persistence | Pass at 0.2.2 only (pre-composition) |
| Spawn/party transaction | None |
| Follow/formation | **Fail** at 0.3.17–19 (separation of ~185 m and ~83 m) |
| Peaceful life | None |
| Leader readiness | **Fail** at 0.3.17–19 (`LeaderReady` removed) |
| Threat lifecycle | **Fail** at 0.3.17–19 (state oscillation) |
| Hands/equipment | **Fail** at 0.3.17–19 (impossible hand sets) |
| Visible stow | **Fail** at 0.3.17–19 (torch broke the stow transaction) |
| Bow | **Fail** at 0.3.17–19 (no proven release) |
| Melee/block | **Fail** at 0.3.17–19 (native gates refused attacks) |
| Torch | **Fail** at 0.3.17–19 (light-component logic) |
| Wet/weather | **Fail** at 0.3.17–19 (humans stayed dry), still open |
| Human timing | **Fail** at 0.3.17–19 (synchronized behavior) |
| Doors | **Fail** at 0.3.17–19 (unseen-entrance knowledge) |
| Commands/telemetry | None |

## 4. First live validation sequence

**Step 0 (new, because of the conflict):** identify the installed build. In PowerShell on the game machine:

```powershell
Get-FileHash "<profile>\BepInEx\plugins\LivingWorldHumanNpc\LivingWorld.HumanNpc.dll" -Algorithm SHA256
```

Then compare it with the startup line `STATUS human plugin=<version>`.

- `7443…` and 0.3.31 → run the handoff's 14 steps exactly as written.
- `8575…` and 0.3.34 → 0.3.34 becomes the candidate under test. The same 14 steps apply, but first you decide whether 0.3.32–0.3.34 were legitimate scoped repairs or need to be rolled back to 0.3.31.
- Anything else → stop. The profile is untrusted.

After that, steps 1–14 from the handoff, unchanged: version and hash, then party spawn, peaceful stow, leader readiness, sensing and commitment at 30 m, bounded pursuit, ownership through target switches, bow proof, melee proof, regroup and restow, torch with hysteresis, torch-to-combat, doors, and Wet parity. Stop at the first failure and capture the startup log, telemetry, and video.

## 5. What I need from you

1. The output of that hash check and the startup version line. Alternatively, connect the profile's `BepInEx` folder to this chat ("Add folder" in the desktop app) and I'll read it myself.
2. Whether a 0.3.31 source folder still exists on your machine. Without it, the comparison against "the last version that live-proved each behavior" is impossible for 0.3.31.

I'm making no code changes until a failing boundary is evidenced. The earlier offer to fix the backslash file names is a repo-housekeeping commit only, and it can wait.

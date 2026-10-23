# Living World C.O.R.E. — NPC core 1.0.0

Ground-up replacement for the Human NPC lab (0.3.x → 0.6.5). It keeps every
live-accepted piece and changes the structure that made regressions keep coming back.

## Why the old builds kept regressing

Two causes are verified, not guessed:

1. **Several modules wrote the same native state.** Torch, readiness, stow, follow,
   guard and combat each wrote to hands and movement under their own rules. Fixing one
   silently un-fixed another.
2. **`Humanoid.EquipItem` wipes both back-stow slots on every call.** This was verified
   in the IL of the installed Valheim 1.0.20. Every native torch equip erased the
   companion's memory of the sword and shield on its back. That is the torch ↔ stow loop.

## The structure

```
NpcKernel (one per NPC, 10 Hz)
 ├─ Sense()          read the world once: leader, target, damage, hostiles, light, swim…
 ├─ PhaseArbiter     the ONLY code that decides the phase (pure, unit-tested)
 │                   Initializing · AwaitingLeader · Calm · Readiness ·
 │                   Combat · CombatClear · CombatRegroup · Regroup
 ├─ DomainScheduler  grants Movement / Hands / Look / Posture in priority order,
 │                   all-or-nothing, never two owners
 └─ behaviors        Enter once → Tick while owning → Exit once (before the next owner)
      1 combat            All      native MonsterAI fights; we only hand off the weapon once
      2 combat-regroup    All      too far from party → return; local defense still allowed
      3 stealth           Posture  crouch with leader
      4 sit               Posture  sit with leader (native IsSitting)
      5 settled-guard     Look     sector sweeps when the leader stops
      6 awareness         Look     readiness: watch the nearest alerted hostile
      7 traversal         Movement formation leases + native Follow (only path writer)
      8 hands             Hands    stow / draw / torch
NpcBody              the ONLY writer of native state; refuses a caller that
                     doesn't hold the domain (runtime check + build-time IL gate)
NpcWetStatus         embodiment (native Wet + drip), holds no domain
```

**Adding a behavior** (doors, workstation, town resident, hire/dismiss…) means writing one
class: declare its domains, write `Wants` / `Enter` / `Tick` / `Exit`, and add it to the
priority list in `NpcKernel.Awake`. You don't edit other modules. If it wants a domain
someone else holds, the scheduler decides by priority, and the loser's `Exit` runs first.

## What's carried over unchanged (live-accepted)

- Native Player-clone factory and random identity/hair/beard (0.4.1).
- Formation leases, follow anchor lead and pace math (0.5.1/0.5.2).
- Visible back stow and four-slot visual sync (0.6.1/0.6.3).
- Settled guard sector coverage (0.6.0).
- Complete native encounter release (0.6.2).
- Native combat ownership plus a one-time `EquipBestWeapon` per new target (0.6.4).
- Prefab names and saved ZDO keys are the same, so existing companions load straight into 1.0.0.

## What's fixed or restored

| Item | Status |
|---|---|
| Torch at night, weapons stay on the back | Restored. The back slots are captured and restored around `EquipItem`. The torch leaves hand *and* inventory on every exit, so native combat can't pick it. |
| Torch flicker at light boundaries | Fixed by 3 s hysteresis, plus a 6 m in / 10 m out built-light band. |
| Crouch with leader | Restored via animator `crouching` and a sneak pace. |
| Sit with leader | Restored via `emote_sit`, driven by native `IsSitting`. |
| Rain wetness + drip | Restored (port of the 0.3.45 adapter). |
| Returning companion couldn't defend itself (0.6.5) | Fixed. Anything hurting it within 6 m is fought; then it resumes returning. |
| Clear was "50 targetless ticks" | Fixed. It now needs 5 s with no target, no damage and no alerted hostile within 30 m. |
| Friendly fire started combat | Fixed. Only enemies count. |
| `lwparty` wiped your companions every time | Fixed. `lwparty` does nothing if you already have companions; `lwparty new` replaces them. |
| Party rollback missed an actor created mid-failure | Fixed. The actor is tracked before validation. |

**Not in 1.0.0:** doors, workstation/crafting pose, beds, cleric/healer role, town/resident
context, hire/dismiss. Each is now one new behavior class rather than surgery on shared code.

## Verification

`./build.sh` (Linux/Mono) or `Build.ps1` (Windows) refuses to output DLLs unless all of these pass:

1. **Regression ledger.** 23 offline tests, each named after a real past failure.
2. **Native gate.** Every private Valheim member we reflect must exist as a live instance
   member in *your* `assembly_valheim.dll`. It rejects the 0.6.4 static-constant mistake.
3. **Compile** at C# 5, matching the Windows .NET Framework compiler.
4. **Ownership gate.** Reads the compiled IL; any native write outside `NpcBody` fails the build.
5. **Telemetry compile.**
6. **Analyzer self-test.**

After a play session, grade it:

```
python tools/analyze_session.py "<profile>\BepInEx\LivingWorldTelemetry\LWC-<stamp>-<world>-part00.jsonl"
```

The analyzer checks torch/combat, stow loss, flicker, crouch/sit sync, combat churn,
discrete releases, party separation, survival, ownership violations and faults.
Checks a session didn't exercise are reported as NOT EXERCISED, never as passed.

## Commands

- `lwparty`: creates your two companions if you have none.
- `lwparty new`: replaces the lab pair.
- `lwstatus`: shows each companion's phase, why it's in that phase, and who owns each domain.

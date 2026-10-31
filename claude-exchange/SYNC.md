# Keeping Claude and ChatGPT/Codex in sync

The repository is the only memory both agents share. A chat's own memory, and
uncommitted files on Brian's PC, are invisible to the other agent.

## Start of every session (either agent)

1. `git fetch --all --tags` and fast-forward `main`.
2. Read, in order: the latest `docs/CURRENT-PROJECT-STATE-*.md`, the tail of
   `claude-exchange/DECISIONS.md`, the work order
   (`design/lwc-design-archive:docs/design-archive/10-CLAUDE-WORK-ORDER.md`), and
   every outbox folder newer than your own last submission.
   Before any work involving characters, classes, roles, biome tiers, profiles,
   spawning or loadouts, also read `docs/development/CHARACTER-IDENTITY-CONTRACT.md`
   completely. A shorter proposed roster never supersedes or deletes an existing one.
3. Tell Brian in five lines or fewer what changed since the last session and
   what you are about to do.

## End of every session that changed anything

1. Commit and push. Source changes go on a branch or pull request; exchange
   submissions, decisions and state documents go on `main`. Never leave finished
   work uncommitted in a working tree.
2. If the state changed (a build installed, a test passed or failed, an owner
   decision), update the current state record and append owner decisions to
   `DECISIONS.md`.
3. If the other agent must act, leave a dated outbox folder with `REQUEST.md`.

## Before any install

Compare the installed DLL's SHA-256 with the state record's installed row. If
the candidate comes from a different actor line (0.3.x `LivingWorldHumanNPC/`
versus 1.0.x `LivingWorldCORE/npc/`; both build `LivingWorld.HumanNpc.dll` with
the same GUID), stop and ask Brian. The installed runtime is the 1.0.x line until
Brian decides otherwise.

Also stop before installation when the candidate:

- changes, aliases or migrates an existing character identity, class, role, biome
  placement, default pair or persisted profile without Brian's explicit authorization;
- contains a “decision needed” or unresolved semantic conflict affecting runtime defaults;
- enables a new behavior by default before its ownership and compatibility review.

Compilation and offline tests do not authorize choosing among unresolved character
semantics. Preserve the installed baseline until the decision is explicit.

## Brian's part

Say "sync" in either chat. That agent runs the start steps above and reports.

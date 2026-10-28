# Append-only decisions

## 2026-10-17 — Non-regression authority

The user requires Claude and Codex to build Living World C.O.R.E. collaboratively without repeatedly removing working behavior. Working systems must not be erased or inverted while another subsystem is repaired.

## 2026-10-17 — Current sequencing

Finish and live-prove the Human NPC companion before beginning UnJohnny's Outsiders, Smoothbrain's Timed Item Destruction, or other postponed expansion work.

## 2026-10-17 — Shared GitHub exchange

The user authorized a GitHub handoff folder that Claude and Codex can both use. Submissions remain dated and append-only. Neither agent may silently replace the other's evidence or treat offline success as live proof.

## 2026-10-27 — Traversal before bombs and new classes (owner statement)

Brian: traversal must be finished first. That means live-testing party spacing
(walking around without bumping into the player or each other), then portals
and caves/dungeons: who may go through a portal, and active companions must
follow the player into troll caves and other dungeons ("I walked into a troll
cave and my guys aren't with me"). Then bombs. Then new classes such as the
Plains spear huntress. This settles the archive's open portal-versus-bombs
ordering question: portals and interiors come first.

## 2026-10-27 — One shared sync routine

Brian reported that keeping the ChatGPT and Claude chats current is getting
harder. Both agents follow `claude-exchange/SYNC.md`: pull and read at session
start, commit and push at session end, and stop before installing a build from
a different actor line.

## 2026-10-28 — Nomadic camps, release to camp, memorial token (owner statement)

Brian, as the safe starting version:

- **Nomadic camps.** A recruitable camp may despawn and later reappear somewhere
  in the same general area where it was first spawned. Returning after time, the
  player finds it near the old spot, possibly moved. The people in it are the same
  persisted people and remember the player. This is how the existing rule
  ("rejected or unresolved camps expire or migrate") is implemented first.
- **Release goes back the way they were found.** For now, Release from Party
  returns the companion to their nomadic origin camp. Homes, reclaimed villages
  and allied camps remain the later destinations in
  `COMPANION-PROGRESSION-ROSTER-AND-RELEASE.md`.
- **Pairs.** Brian: "If it's a single from a pair..." (sentence unfinished).
  Claude's reading, unconfirmed: when one of a camp's pair is recruited, the
  partner stays and the camp persists around them, and a released companion
  rejoins that partner. Needs Brian's confirmation.
- **The end is a token.** On a companion's permanent death the player receives a
  memorial token and chooses whether to place a tombstone. This is separate from
  the equipment-recovery transaction and is not a replacement or revival token.

## 2026-10-28 — Correction: "camp of one" and the found companion (owner idea, unresolved)

Supersedes Claude's unconfirmed "Pairs" reading in the previous entry; that
reading was wrong.

Brian: a companion released back to the wild forms a **camp of one**. When the
player later re-enters the general area, the camp (possibly moved) may hold a
**second person**: someone who found them. Lore question in the world: is there
another out there, and have they found each other? Brian marks this as an
unresolved idea with a quick code implementation.

Claude's proposed shape (DESIGN-AUTHORITY only after Brian accepts):

- On each camp respawn, a camp of one may gain one newcomer, decided
  deterministically from the camp id and respawn count (no reload rerolls).
- The newcomer is a new persisted person with a new identity and history,
  never a reroll of the existing one and never a dead companion returning.
- The newcomer's role complements the existing one (the biome class pair).
- At most one newcomer; a camp never grows past two through this rule.
- Open: does the newcomer count as a recruitment lead under the one-active-lead
  and five-day rules, and how long must the player be away first?

## 2026-10-28 — Body states and the player-parity layer (Brian)

- Brian approved building ChatGPT's body-reactions handoff ("ok Now you can build...."): 1.0.32.14 installed, live
  test and per-clip audition pending.
- Weather Cold/Freezing for companions is not to be patched on its own. Brian: the status piece belongs in "a special
  interior mod that we build to allow things like this and skills to be accounted for... part of how our brain will work."
- Brian: write that layer now, as one design, instead of later recursive and possibly regressive patching ("we divide
  here"). Scope in his words: "a real Player set of things", a predictive model "that only allows what naturally would
  befall a player", "a chance to hit, and not a character always missing", dual-purpose attacks.
- Design written by Claude (DESIGN, not built): `claude-exchange/CLAUDE-OUTBOX/2026-10-28-player-parity-layer-design`.
  Brian shares it with ChatGPT before code.

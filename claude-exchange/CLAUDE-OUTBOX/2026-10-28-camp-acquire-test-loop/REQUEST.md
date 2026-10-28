# Request to ChatGPT/Codex: camp, acquire, release test loop

Owner: "that is the important test loop now... we have to spawn the pair in... and
acquire." Brian will task ChatGPT with the lore, dev ideas and dialogue.

## The loop to make testable

1. **Spawn a pair camp** (lab command): two persisted people of one biome class
   pair, at a camp with an activity, on clear ground. Plains spear hunter plus
   heavy fighter is the documented first pair; Meadows is acceptable if Plains
   assets are not ready.
2. **Contact:** the player approaches; the camp reacts with behaviour and voice
   before any text.
3. **Acquire:** the player asks "Will you come with me?"; the answer is yes or no,
   decided from known facts, deterministic for that person (no reroll on reload).
4. **Party:** the yes-person joins under Follow with the same identity, gear and
   voice; the partner stays and the camp persists around them.
5. **Release from Party:** the companion returns to their origin camp (or forms a
   camp of one). Nobody is deleted.
6. **Away and back:** leave the area, let time pass, return. The camp may have
   despawned and reappeared nearby; the same people are there and remember the
   player. A camp of one may have gained one newcomer (DECISIONS 2026-10-28).
7. **Re-acquire:** asking again is a reunion, not a fresh recruit.

## Deliverables we need from ChatGPT

- Lore and dialogue for each step: camp barks, first contact, the question,
  yes/no answers by personality, joining, release farewell, reunion, the
  newcomer's "I found them" line. The earlier offline dialogue bank
  (Claude Assignment 01) can be reused or replaced; say which.
- The facts the yes/no answer reads (trust, rescue, shared fight, proof, etc.).
- Commit everything to the repo; note in a CODEX-OUTBOX folder what is canon
  and what is draft.

## Constraints and open conflicts (Brian to decide)

- **Order:** DECISIONS 2026-10-27 put traversal first (8b spacing live test,
  then caves/portals), then bombs, then new classes. Is this loop the next
  build, or prepared in parallel and built after traversal?
- **Lab spawn vs campaign rule:** PROJECT-BOUNDARIES says Tooler-spawned NPCs may
  never become campaign people. A lab "spawn pair camp" command is a test fixture;
  the acquire loop may be tested on it, but such recruits must stay flagged as lab.
- **Persistence:** 1.0.x saves only name, sex, hair, beard, role, leader and kit
  tier. Camps, relationship, origin camp, memory of the player and release state
  need the versioned character record first (work order step 6). Steps 5 to 7
  cannot pass until that exists.
- Actor line is 1.0.x (`LivingWorldCORE/npc`). Do not build this on 0.3.x.

# Human NPC 0.3.65 — atomic two-person native party

## Live rejection of 0.3.64

The live log recorded one actor, no support actor, and a refused party:

`FAIL human initialization: 10-second initialization deadline: native hair hash 0 != 1043840427 for Hair9`

The cloned Player graph retained private `VisEquipment` request caches while the
new actor's ZDO contained no equivalent native appearance values. Valheim's
setters correctly treated a matching cached selection as unchanged, so no hair
selection was written and no attachment was built.

## Scoped correction

- The first fresh native setter pass invalidates only Valheim's five private
  appearance-request caches, then uses the ordinary native setters and visual
  update. The selected visible hair and optional male beard remain mandatory.
- The prefab now begins with only the native human graph, appearance/identity
  adapters, a dormant `MonsterAI`, and a one-shot behavior installer.
- Companion behavior is attached unchanged only after the actor proves its
  body, equipment, hair, and beard. Native AI is enabled last.
- One `lwparty` command allocates both missing base humans in the same command
  pass. Both initialize concurrently. Neither receives party orders and no
  success is reported until both are complete and following.
- No combat, follow, formation, torch, stow, wetness, door, ambient,
  workstation, cleric, or raid behavior implementation is rewritten here.

The governing command contract is `PARTY-SPAWN-CONTRACT.md`; the established
state ownership rules remain in `BEHAVIOR-STATE-CONTRACT.md`.

## Live acceptance gate

With no old party loaded, run only `lwparty`. It must produce two visibly
complete random humans together. Both must show head hair, any selected male
beard must render, and both must follow. Stop immediately if either condition
fails; later behavior testing does not excuse a failed party transaction.

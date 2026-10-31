# Living World C.O.R.E. — agent start

Before any work, follow `claude-exchange/SYNC.md` (start-of-session steps), then
`claude-exchange/PROTOCOL.md` for submissions. The latest
`docs/CURRENT-PROJECT-STATE-*.md` is the status authority; `claude-exchange/CURRENT.md`
only points to it.

## Non-negotiable character identity rule

Before touching NPC classes, roles, profiles, spawning, biome tiers, default pairs or
loadouts, read `docs/development/CHARACTER-IDENTITY-CONTRACT.md` completely.

- Existing characters and types are additive, permanent roster members unless Brian
  explicitly authorizes their removal. Omission from a proposal is never deletion.
- Never infer, rename, migrate, upgrade or alias one character/type into another from
  role, weapon, equipment, roster slot, biome, sex/gender or shared doctrine.
- Guard remains Guard. Archer remains Archer. Scout remains Scout. Huntress is her own
  authored character and an Archer never becomes Huntress. Warden is not a Guard alias.
- Progression changes a persisted person's skills, knowledge and equipment; it never
  changes who that person is or which authored character/type they were created as.
- Shared behavior modules are allowed; shared identity is not.
- If a candidate changes an existing roster identity or reports an unresolved semantic
  decision, stop. Do not build a migration, choose a default or install it. Obtain
  Brian's explicit decision first.

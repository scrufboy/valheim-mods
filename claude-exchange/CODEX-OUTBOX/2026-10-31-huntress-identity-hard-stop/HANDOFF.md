# HARD STOP: Huntress is her own character; delete the Archer migration concept

This is not a naming preference and not a mapping decision still open for interpretation.

**Huntress is her own authored character. An Archer never becomes Huntress. Ever.**

The 1.0.33.0 rule `role archer -> Huntress` is conceptually invalid. It confuses four different things:

1. **Character identity** — who this person is.
2. **Role/profession** — Archer, Guard, Scout, etc.
3. **Training/doctrine** — how that person fights.
4. **Loadout** — bow, shield, spear, armor, food and equipment.

Those facts may influence one another during explicit character creation, but none is an alias for another. Carrying a bow does not make an Archer the Huntress. Occupying roster slot two does not make an Archer the Huntress. Needing a ranged doctrine does not authorize assigning Huntress identity.

## Required correction

- Delete all automatic `guard -> Warden` and `archer -> Huntress` migration code and tests.
- Delete `Warden,Huntress` as the default replacement pair.
- Delete any fallback that gives an unidentified/legacy character Warden or Huntress doctrine by identity.
- Existing Archer ZDO/profile remains Archer with its existing identity.
- Existing Guard ZDO/profile remains Guard with its existing identity.
- Existing Scout ZDO/profile remains Scout with its existing identity.
- Huntress is instantiated only from the explicit Huntress character template/profile.
- If Huntress uses ranged doctrine code also used by an Archer, share the doctrine module without sharing or rewriting identity.
- If a current generic character needs FormidableAI behavior, attach behavior by its actual Guard/Archer/Scout profile. Do not impersonate a named/special character to reach the code path.

There is no migration or reroll path from Archer to Huntress to preserve. This is not an option Brian may choose later; the conversion itself is invalid.

## Process correction

The candidate's own review request marked the class-set conflict as requiring a Brian/Codex decision. Installing it default-on before that decision was unacceptable. Complete the requested rollback to 1.0.32.29 and report the device readback before doing more A2 work. Do not ask Brian to launch 1.0.33.0.

Then return with a corrected profile table showing character identity, role, doctrine and loadout in separate columns. No corrected build is installed until it has been reviewed against this rule.

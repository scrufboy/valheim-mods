# Human NPC 0.3.59 — required head-hair contract

## Live failure

Two consecutive freshly generated parties presented without head hair even
though the runtime ObjectDB reported 87 valid native hair assets. The catalog
adapter had inserted an empty string into both the hair and beard candidate
lists, making baldness an authored random outcome rather than an asset failure.

## Narrow correction

- Fresh companion head-hair candidates now contain only runtime-verified,
  non-empty native hair prefab names.
- Beard selection continues to permit the empty choice because clean-shaven is
  a valid presentation.
- `HumanProfile.Generate` enforces the non-empty fresh-hair invariant even if a
  future caller supplies a malformed candidate list.
- Existing persisted identities are not rewritten or rerolled. Replacing the
  current disposable test party creates new 0.3.59 identities under the fixed
  contract.

No combat, awareness, formation, torch, wetness, doors, ambient activity,
equipment, health, persistence, or pathfinding behavior was changed.

## Name correction

The former 12-by-12 generator mixed modern fantasy given names with decorative
nature surnames such as `Hearth`, `Vale`, and `Willow`. That was too small and
did not fit the setting. Fresh 0.3.59 identities instead draw from much larger
normalized Viking-age/Old Norse given-name lists and sex-appropriate
patronymics. The design is based on the Nordic runic-name corpus published by
the Swedish Institute for Language and Folklore and keeps ASCII spellings for
the existing Valheim identity-storage contract. Existing saved names remain
stable; only newly created people use the expanded pool.

Reference: [Nordiskt runnamnslexikon](https://www.isof.se/utforska/publikationer/publikationer/2007-01-01-nordiskt-runnamnslexikon)
and Isof's overview of common names in Nordic runic inscriptions.

## Acceptance

After installation, remove the disposable test party, run `lwparty`, and verify
both freshly created companions have visible native head hair. If either is
bald, preserve the run log and telemetry; that would identify a visual
application failure rather than an allowed identity result.

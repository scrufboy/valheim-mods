# Human NPC 0.3.61 — spawn and appearance repair

## Live failure

0.3.60 reached `lwparty` but refused before instantiating either actor with
`Native hair catalog metadata is invalid`. The Player prefab's
`m_hairPrefabPrefix` and `m_hairPrefabCount` are deliberately empty/zero in the
extracted native game data. They belong to non-player random appearance setup;
they are not the Player customization catalog.

The prior broad ObjectDB search also admitted `HairNone` and internal `_2`/`_3`
helmet-remap assets as fresh base styles. That is why it could persist a
non-selectable base appearance such as `Hair21_2` and then fail the live visual
attachment check.

## Correction

- Fresh appearance discovery follows Valheim's actual contiguous selectable
  style contract: `Hair1..N` and `Beard1..N`, resolved through ObjectDB until
  the first missing index.
- `HairNone`, `BeardNone`, and `_2`/`_3` helmet-remap prefabs cannot enter the
  fresh identity lottery.
- Every accepted prefab must remain a customization item with an `attach` or
  `attach_skin` visual before identity persistence.
- The existing post-spawn live check still requires the exact selected hair
  and optional beard hash to produce an attached GameObject. Failure blocks the
  actor; it cannot silently become a bald ready companion.
- No combat, encounter, torch, stowing, formation, door, wetness, ambient,
  workstation, resource, name, equipment, or stat policy changed.

## Live acceptance

With no companions loaded, one `lwparty` command must create both companions.
The log must report `hair=38; beard=26` for the native base catalog, followed by
one successful appearance initialization line per actor containing concrete
`hair=` values. Both actors must visibly have head hair; male beard selection
may be clean-shaven by design. `lwtest party status` must report both scout and
cleric loaded.

## Installed candidate

- Installed in `LWC Human Lab v1` only after confirming Valheim was closed.
- DLL SHA-256:
  `5EFDE086011A24EFE42CEABCB9C287221138CC687BD521C66205D77E00DC180F`.
- The failed 0.3.60 DLL and its start document are preserved under
  `BepInEx\LivingWorldBackups\HumanNpc\20261020-broken-0.3.60`.
- Package SHA-256:
  `617B597D1AF99CE59F47D517DF23973E5EA6C9A14627D43D639F7C4D702AFC42`.
- Offline verification passed. Live Unity creation and visible hair/beard remain
  the required acceptance test.

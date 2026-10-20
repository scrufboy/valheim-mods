# NPC presentation handoff — actor owner
Date: 2026-10-20. Owner: Brian / scrufboy.
Requested destination: the LivingWorldHumanNPC technical chat / actor implementation owner.
The current toolset cannot post directly to another existing conversation. Root will
publish this source branch and handoff to GitHub; no direct chat delivery is claimed.

## Scope and user direction
Brian reported missing male hair/facial hair and unpolished naming presentation.
He explicitly chose BOTH accented lore-appropriate names AND polished nameplates
with role/title formatting. He then redirected this studio chat to stop actor
implementation, hand off this work and focus on the studio.
This submission preserves that work in a separate source candidate:
branch fix/npc-appearance-identity, parent main 91f80f2.
Studio implementation does not depend on accepting or installing these changes.

## Source-inspected root causes and limits
- HumanAppearance legacy no-profile path explicitly sets hair/beard IDs to zero.
- NativeAppearanceChoices includes empty entries; fresh male selection allowed them.
- Known HairNone/BeardNone candidates can have nonempty names but no visible geometry.
- Hair empty-string handling hashed empty text while beard explicitly used ID zero.
- HumanIdentity hydrates plain profile.Name into body.m_name; no decorative nameplate
  formatting exists in that path, and a competing m_name writer would be overwritten.
- Existing new actors already receive GUID-seeded, persisted names/customization;
  loading does not reroll. Legacy no-profile actors differ.
- Separate resident name provider/store scaffolding is not connected to actor identity.
No installed actor DLL, affected actor save record or live rendered male was examined.
These source facts do not prove the reported live actor belongs to the legacy/empty cohort.

## Candidate changes
Fresh males skip empty/HairNone/BeardNone when eligible nonempty catalog candidates
exist; existing nonempty selection and PRNG draw count are preserved. Female policy
is unchanged. Empty-only catalogs remain bounded and representable.
Empty saved hair maps to native ID zero, symmetrically with beard.
Accented Norse-inspired first names were appended; saved spellings and IDs stay intact.
Parser accepts canonical NFC Unicode letters/attached combining marks and rejects
non-NFC bytes, tags, controls and delimiter injection rather than rewriting saved names.
WithAppearance clones exact canonical identity and unrelated fields, changing only
validated hair/beard strings. It does not persist or touch native state.
HumanNamePresentation is a pure escaped rich-text formatter; optional title comes
from an authoritative role owner. The profile overload emits no invented title.

## Unimplemented
No runtime command/API repairs existing saved bald actors. No identity migration.
No HUD integration; no Character.m_name formatting change.
No real AI name provider selected/connected. No free hosted service claim.
No actor version bump, full plugin package, installed binary or combat edit.

# Appearance repair review — source candidate only

Parent: main 91f80f2. Branch: fix/npc-appearance-identity.
No version bump, actor package, installation or combat change.

The source change preserves the existing random draw sequence and all nonempty
choices. When a freshly generated male selects an empty or known HairNone/BeardNone entry, it
uses the next nonempty runtime-verified catalog entry instead. An empty-only
catalog remains representable. Female selection policy is unchanged. Other
runtime assets still need adapter-level visual eligibility inspection; a nonempty
prefab name does not prove visible geometry. ObjectDB item presence alone does not
prove that an attach mesh renders correctly on the native rig. Do not discard
unknown customization prefabs based only on guessed child names; inspect actual
attach geometry and bind paths on the target version before narrowing that list.

HumanAppearance now maps an empty saved hair string to item ID zero, symmetrically
with beard. All saved profile data remains unchanged. The normal revision guard
still prevents automatic rewriting of committed actor visuals.

The first-name pools append accented Norse-inspired choices such as Björn, Søren,
Håkon, Ásgeir, Eiríkr, Þóra and Guðrún. Existing entries retain their spelling.
The name parser accepts Unicode letters and attached combining marks in strict
NFC form, rejecting noncanonical encodings rather than rewriting stored bytes.
Canonical identity contains no rich text. Current saved ASCII names are preserved.

HumanNamePresentation is a pure trusted formatter: bold escaped name, plus an
optional smaller muted escaped title supplied by an authoritative role owner.
The profile overload emits no role for current unassigned profiles. It does not
write Character.m_name or any persisted data. HumanIdentity integration was not
changed here; root is reviewing a HUD-only integration that keeps entity names raw.
The new formatter must enter the explicit build allowlist before compiling the
actor plugin; no recursive source inclusion is introduced by this candidate.

Validation: pure HumanProfileTests compiled/run against .NET 8.0.21 via the local
Roslyn toolchain: PASS. Includes 512 seeds with empty/None-first runtime catalogs,
exact Unicode/ASCII save/read roundtrips, strict NFC/control/tag injection rejection,
safe nameplate escaping, WithAppearance immutable field preservation, preservation
of a saved bald male profile, empty-only catalogs and the unchanged golden sample. git diff --check
passed. This is OFFLINE-TESTED generation/serialization, not a live hair-render test.
The full actor plugin has not been rebuilt or live-tested by this candidate.

## Existing saved bald male profiles: review proposal only

Do not reroll a profile or remove its rig revision to force initialization.
Proposed explicit actor-owned operation, subject to review:
`TrySetCustomization(expectedIdentitySeed, expectedProfileRecord, hairId, beardId)`.

The operation must require local network authority, exact compatible composition,
current matching persisted record, a male model, no conflicting actor activity,
and verified native hair/beard assets. It must preserve seed, name, sex, colors,
traits and role byte-for-byte. If an existing nonempty profile fails to render,
diagnose/repair native visual hydration or equipment hiding first; do not rewrite
correct identity data to compensate for a renderer failure.

For an actually empty customization, Brian's explicit command can choose new
hair/beard, persist the edited record through HumanIdentity as the one identity
writer, apply the native visual setters and verify the resulting authoritative
visual state. Network/disk failure must not be reported as success. Save/reload and
authority handoff must prove agreement. The API, transaction/recovery behavior and
whether a schema revision is needed require owner review before implementation.

The pure HumanProfile.WithAppearance value edit is implemented. It clones all
existing fields, changes only validated hair/beard identifiers, and preserves the
exact canonical name and stable ID. It does not verify runtime assets, save data,
change visual state or claim actor authority. No live repair method or schema
change is implemented here. Legacy actors with
no profile require a separately explicit identity adoption/migration path; they
must not receive a new GUID/name silently on load.

Brian selected accented lore-appropriate names and polished nameplates with
role/title formatting. These value/presentation helpers support that direction;
native HUD rendering, font glyph coverage and authoritative role selection still
need integration and live verification. No title is inferred from clothing or
weapon loadout.

# NPC presentation regression audit

Date: 2026-10-20. Scope: read-only actor/name-source audit for Brian / scrufboy.
Baseline inspected: studio worktree at ce9085d, actor source inherited from main
91f80f2 (plugin build script identifies 0.3.58). No actor source or version changed.
Evidence label: SOURCE-INSPECTED. Current installed binary, affected save records,
rendered output and multiplayer reload behavior are UNKNOWN, not LIVE-PROVEN.

## Findings

| Reported concern | Source evidence | What it establishes |
| --- | --- | --- |
| Male actors lack hair/beards | HumanAppearance.cs 56–66 sets male model and hair/beard item IDs to zero when profile is absent | Legacy no-profile actors are intentionally initialized bald/clean-shaven; this is a direct source explanation for that cohort |
| Fresh male actors may be bald/clean-shaven | ManualTestPlugin.cs 345–364 adds an empty string to both native customization catalogs; HumanProfile.cs 60–61 samples those choices | Fresh generation permits no-hair and no-beard results; it does not ensure visible hair/beards on every male |
| Empty hair selected | HumanAppearance.cs 80 hashes profile.Hair unconditionally; beard line 82 handles empty explicitly as zero | Hair and beard use inconsistent empty-item handling. Native empty-hash behavior needs verification; do not assume a nonzero empty-string hash is a valid “none” ID |
| Hair missing on a valid nonempty profile | HumanAppearance.cs 53–85 only applies profile visuals before rig revision commit | A committed actor is not repaired merely by changing generation or restarting. Inspect saved profile and visual ZDO hashes before choosing a repair |
| Generic NPC random customization is disabled | NativeHumanFactory.cs 99–103 sets m_isPlayer=true, null player component, random model false, NPC hair/beard chance zero | Random NPC hair is intentionally bypassed in favor of the saved profile; turning these chances up would introduce a second appearance writer |
| Names have no decorative markup | HumanIdentity.cs 139–141 hydrates body.m_name directly from profile.Name every half second | Plain stored names are the entire current identity display; no color/title/role formatter is used here |
| Names need to be remembered | HumanIdentity.InitializeFresh persists profile.Save into livingworld_human_identity; HumanProfile.Save stores the resolved values | Fresh generated identities already have stable saved names and customization, rather than rerolling on load |
| “Free AI names” | ResidentIdentity.cs defines a provider interface; NameCoordinator.cs explicitly says Unity/ZDO adapter pending | Provider scaffolding exists separately, but no active AI naming integration is established by inspected source |

## Hair, beard and equipment path

The active fresh scout path resolves ObjectDB customization items, creates an
explicit compatible composition and calls HumanIdentity.InitializeFresh with
Guid.NewGuid().ToString("N") before the first Update (ManualTestPlugin.cs
266–302). The cleric fresh path does the same (396–407). HumanIdentity validates
selected native asset availability before persisting. HumanAppearance then uses
native VisEquipment.SetModel, SetSkinColor, SetHairColor, SetHairItem and
SetBeardItem setters. Ownership is checked at each write boundary.

The default scout outfit at ManualTestPlugin.cs 132 contains leather chest/legs,
bronze sword and wooden shield, with no helmet in that initial list. This makes
“default helmet hides all hair” unsupported as the explanation for a fresh scout.
Later equipment, a cleric outfit, native helmet hair/beard hiding modes, attachment
state and third-party cosmetics still require inspecting the affected instance.
The inspected actor source does not contain a global m_hideHair/m_hideBeard
override that demonstrates an all-male masking defect. Native visibility behavior
and equipped item metadata have not been live-audited here.

HumanRigPolicy.MayInitializeAppearance accepts only an empty revision marker.
Once native-human-v1 is saved, initial appearance is not reapplied. Therefore a
profile/generation change is insufficient evidence of a repair to an existing
actor. A repair must compare intended saved profile, actual replicated visual IDs,
equipped hide modes and actual renderers without rerolling identity.

The source supports a definite legacy bald path and possible fresh empty choices.
It does not prove that every reported bald male belongs to either case. For an
actor whose persisted profile has nonempty valid Hair and Beard values, a separate
visual hydration/equipment regression remains possible and unproved.

## Names: existing generation, persistence and markup

HumanProfile.cs contains twelve male first names, twelve female first names and
twelve family names. A deterministic sequence seeded from a persisted 128-bit
hexadecimal seed selects the full name, sex, customization and descriptive traits.
The caller uses a fresh GUID only at explicit creation. Save stores the selected
name and appearance values, so subsequent library changes do not rename actors.
InitializeFresh is idempotent for an already valid record and refuses malformed
existing bytes. Read parses existing records; Update only hydrates local display.

This is a working source-level persistence contract, not proof that the current
installed save has those records. Check the affected actor's actual ZDO record.
Legacy actors without a profile retain the generic prefab name and are not given
new identities automatically. Per-sex first/family combinations are limited to
144 names, so duplicate display names remain possible. Stable identity is the seed,
not uniqueness of the display name. No uniqueness registry is connected here.

HumanProfile.ValidName allows ASCII letters, spaces, hyphens and apostrophes, up
to forty characters; it rejects markup. NameCoordinator.ValidName also rejects
angle brackets. This is appropriate for keeping canonical identity plain, but
current display does not add trusted decorative markup afterward. A separate
script that writes a decorated body.m_name would be overwritten by HumanIdentity
on its next hydration interval. The smallest presentation change belongs in the
identity display owner, using a trusted formatter while preserving profile.Name.
The exact requested title/color/role treatment must be established before adding
one. Do not insert arbitrary provider HTML/rich text into canonical name storage.

## Separate resident name foundation

LivingVillageFoundation/NameCoordinator.cs supports a provider abstraction,
deduplicated per-entity asynchronous requests, a persisted attempted marker,
validated result storage and a deterministic fallback supplied by the caller.
TextResidentNameStore.cs supplies an append-only UTF-8 ledger with Flush(true),
per-path in-process locking and last valid matching record hydration. Its entity
key is expected to include world/village/entity identity. Source comments explicitly
state that Unity/ZDO adaptation is pending; repository references found only tests,
not an active actor runtime connection.

This foundation should not become a competing identity authority beside the saved
HumanProfile. An optional naming provider would propose a name once at creation;
the existing canonical actor identity owner must validate and persist the result.
No API service, credentials, current free tier, latency or terms were verified in
this audit. Do not promise a perpetually free hosted AI endpoint. The existing
offline generator already costs no service fees; expanded curated name data or a
separately configured local model can be evaluated without making network access
a requirement for spawning or loading an NPC. All existing names remain unchanged.

## Available reference-mod evidence

Only the following local configuration files were used as comparison evidence;
their source/runtime internals were not inspected or copied:

- PoP Villages HumanM01 configuration: fixed display label “Human Commoner”,
  fixed male model, Beard3/Beard4/empty beard choices and nonempty Hair4/Hair5/
  Hair6/Hair7/Hair8/Hair11 choices. This shows a different data policy for visible
  hair; it does not demonstrate persistent individual naming or AI-generated names.
- Other local PoP labels include Human Guard, Elven Ranger and Dwarven Warrior:
  category/role labels rather than unique remembered individual names.
- Souls of the Land CommonFolk configuration: “Common Folk Pillager” fixed label,
  HairNone plus explicit hair IDs, BeardNone plus explicit beard IDs. Again,
  configuration-based customization is not evidence of a persistent name service.

Friendlies Reloaded and LivingFactions archives were not found as locally available
bytes in this audit's filename scan. Their naming behavior is UNKNOWN here; no
claim is made from remembered feature summaries or an uninspected package.

## Minimal controlled repair proposal

1. On the active actor branch, capture the actual candidate version/hash and one
   affected actor's canonical profile, rig revision, sex, intended hair/beard,
   replicated visual item IDs and currently equipped hair/beard hiding modes.
   Compare a new profile-bearing male, a legacy male and a female. Preserve saves.
2. Normalize the empty hair selection to native item ID zero, matching beard, if
   confirmed by the native contract. Add a narrow check for this branch. Do not
   reroll names or change combat code.
3. If the intended policy is visible hair and facial hair on new male portraits,
   remove “none” from the explicit eligible choices for that creation policy only.
   Preserve existing saved choices unless Brian explicitly authorizes a targeted
   customization migration. Bald/clean-shaven selections are identity data, not
   automatically corrupt data.
4. If valid nonempty saved IDs are not rendering, repair only the evidenced native
   hydration/attachment or equipment mask boundary. Do not turn on NPC randomness,
   rewrite every actor per frame or bypass the saved revision guard globally.
5. Put approved name formatting in HumanIdentity's display path, with plain
   canonical names and one presentation writer. Prove HUD/rich-text support on
   the actual target UI; tests should establish the intended formatted output,
   persistence of plain identity and no repeated markup nesting.
6. Keep optional AI naming a separate integration after the existing persistence
   owner is confirmed. Provider timeout/failure must retain a saved offline result;
   loading never sends another naming request or silently replaces a known name.

Acceptance: fresh male visible choices, legacy behavior/migration explicitly
defined, female compatibility, helmet drawn/stowed changes, save/reload, two-peer
replication and authority migration; unchanged name/profile identity across all
cases. No NPC source, production version or installed DLL was changed by this audit.

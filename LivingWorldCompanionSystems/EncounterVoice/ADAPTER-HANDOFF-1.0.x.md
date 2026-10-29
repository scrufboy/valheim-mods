# Encounter cue presentation adapter handoff — Living World 1.0.x

Status: engine-free policy and audit catalog only. No audio is copied, no active
profile is edited, and no conversational candidate is approved by this module.

## Adapter input

1. Resolve the current authored lore line by stable ID from the version-matched
   EncounterLore contract. Supply its exact localized readable text, stable
   speaker identity, stable persona identity, and identity gender. The adapter
   must not generate text from a clip name.
2. Build `EncounterCueDefinition` entries from the validated candidate JSON.
   `lineBindings` chooses the intent; `intentMap` supplies the safe emote and
   gender candidate. Reject unresolved IDs and unsafe emotes at load time by
   running `validate_line_bindings.py` as a build/content gate.
   The current map distinguishes cautious first contact, warning, mutual notice,
   accepted help, temporary travel offer, and later full-party offer. Preserve
   that distinction: `contact.travel.offer` is not recruitment, while
   `splitash.invitation.npc` is the gated full-party choice.
3. Supply authoritative `NativeCombatOwnsPresentation` and
   `AlertOwnsPresentation` flags from the shared behavior owner. Do not infer
   ownership from animation, distance, or a visible enemy.
4. Supply a monotonic world/session clock. Persist or reconstruct cooldown and
   incident-deduplication state through the encounter owner; do not use wall-clock
   time or client-local state as multiplayer authority.

## Output handling

- Always render `ReadableText` when the interaction itself remains valid. A null
  emote or voice means suppression, not permission to substitute another cue.
- Play at most `SafeEmote`. The adapter still owns native animation availability,
  cancellation, hand lease, root-motion, body, and network checks. If any fails,
  show text without an emote.
- In `Normal`, every conversational candidate returns no voice. Only
  `lwc.voice.surprised.existing` may resolve, and only for an approved surprise
  definition with a fresh stable incident ID.
- In `Audition`, `VoiceCue` is a candidate asset key and
  `VoiceIsAuditionCandidate` is true. Resolve it only in a private audition tool;
  never silently enable audition mode in a shipped profile.
- Resolve `lwc.voice.surprised.existing` through the installed uniform Female A
  or Male B A/B pool. Do not expose filenames to this policy, change weights,
  change event timing, or add a conversational candidate to that pool.

## Ownership and anti-spam

The reducer gives one speaker a two-second presentation lease, enforces an
eight-second speaker cooldown and three-second camp cooldown, suppresses
conversation while Alert owns presentation, and suppresses all emote/voice work
while native Combat owns presentation. Readable text is retained unchanged.

The stable danger incident ID is recorded after the first approved surprise cue.
Reload, zone re-entry, two observers, or a repeated callback must restore the
dedupe set and cannot replay the voice. FriendInNeed remains the incident and
timing authority; this policy only guards presentation.

## Approval transition

After human isolation and in-game audition, licensing review, actor-continuity
review, and recorded approval, add a distinct approved-conversation resolver
mode and immutable pool identifier in a later schema. Do not convert the current
`candidate_only_unapproved` bindings in place or infer approval from a file being
present. Update the JSON validator, pure tests, hashes, and this handoff together.

Run `Test-EncounterCuePresentationPolicy.ps1` for pure offline coverage. The
adapter must also live-test localization, animation cancellation, audio spatial
routing, multiplayer authority, reload dedupe, and combat/alert handoff.

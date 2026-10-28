# Encounter voice candidate review

Status: **audit and audition plan only**. Nothing here changes an active profile,
copies audio, or authorizes distribution. The JSON sibling is a candidate map,
not a runtime pool.

## Result

The approved surprise assignment is preserved exactly: Female A
`expression_emote_03` / `expression_emote_11`, Male B
`breath_shock_alert_01` / `breath_shock_alert_02`, resolved through
`lwc.voice.surprised.existing`. Keep the installed uniform A/B selection and the
existing witness → shock-signal timing. Do not replay it on scanning, reload, or
zone entry. A later call requires a new material fact and the incident cooldown.

The rest of the meeting vocabulary is **not approved**. The available assets can
support short nonverbal color, but they cannot safely carry literal greeting,
questions, gratitude, refusal, or “Will you have me?” by themselves. The subtitle
must carry those meanings. Each cue should be one speaker, one text line, and one
safe gesture; never a camp chorus.

## What was actually inspected

The source WAVs in the candidate map were opened and measured from their sample
data, not accepted from filenames alone. The JSON records duration, RMS, active
sample fraction, and full-file SHA-256. Inspection confirmed that the proposed
cues are brief single vocal events (roughly 0.26–1.08 seconds), except that their
emotional semantics still require human listening/in-game audition. No playback
or speech claim is made where the recording is nonverbal.

The Gamemaster source is 16-bit/48 kHz and preserves the installed Male B/Female A
actor identities. The Ultimate Female Voice Pack is 16-bit/44.1 kHz and offers
clearer category labels, but it is a different female performer and has no male
counterpart. Mixing it into a single named NPC would break actor continuity.

## Recommended decision

For a coherent shipping system, keep Gamemaster Female A and Male B as the NPC
actors and commission or record a small paired encounter-emote set for both:
neutral hail, guarded hmm, strained need, rising question, warm acknowledgment,
firm no, and quiet relief. Until then:

- surprise/alarm may use the approved installed pool;
- all other JSON mappings remain audition candidates only;
- the Ultimate Female candidates are useful as semantic references or a separate
  clearly defined persona, not as drop-in lines for the installed Female A actor;
- Male B `action_release`, `sigh`, and `snort` candidates must be heard by a human
  before assignment because their source labels describe performance mechanics,
  not dialogue intent.

The NPC-initiated “Will you have me?” beat should be especially restrained: an
open-hand/bow gesture, the readable exact question, and at most one soft hopeful
cue. It must use the same factual home, roster, world-gate, and consent checks as
the player-initiated invitation. The player may answer yes or no without repeat
vocal prompting.

The current contract ID for that beat is `splitash.invitation.npc`. Request and
earned-respect candidates are also bound to the current `splitash.request.*`
offer, acceptance, decline, and completion lines. These additions remain
candidate-only: a contract voice category does not approve a particular WAV.

## Suggested presentation bindings

| Intent | Gesture | Text owns | Voice role |
| --- | --- | --- | --- |
| greeting | wave/open hand | identity and hospitality | brief attention cue |
| suspicion | halt | boundary and witnessed reason | guarded color |
| need | point to person/object | concrete need and target | strain/tiredness |
| asking | come-here/point | exact request or question | uncertainty/urgency |
| gratitude | bow/thumbs-up | what the player actually did | warmth/release |
| refusal | no | reason and remaining boundary | firmness, not hostility |
| surprise | point/turn | named danger or person | approved shock signal |
| relief | relax | verified clear/casualty fact | quiet release |
| Will you have me? | open hand/bow | the exact mutual choice | hope without coercion |

Use a minimum eight-second same-speaker cooldown, three-second camp-wide vocal
spacing, and at most one cue per dialogue exchange. Combat, pain, death, and war
cry priority must suppress encounter color; encounter work must not change the
locked combat pools or their timing.

## Licensing and installation

- **Gamemaster Human Vocalizations:** purchased source. The project's approved
  combat baseline records private use only and no redistribution. The four
  surprise clips are installed and approved only for their existing pool. The
  other Gamemaster files exist in the outer source library but are neither
  installed in encounter pools nor approved for these meanings.
- **Ultimate Female Voice Pack by topmademarti:** its bundled readme permits use
  and modification in personal/commercial projects and forbids standalone audio
  redistribution. Credit is optional. The selected source files are present in
  the outer workspace; this audit found no evidence that these exact encounter
  candidates are installed or approved.

Before any install: human-listen every candidate in isolation and in-game, reject
semantic mismatches, confirm actor continuity per named NPC, decide packaging
against each license, and record exact effective arrays and hashes. Do not alter
the source masters.

Run `python validate_line_bindings.py` from this folder (or pass explicit map and
contract paths) whenever EncounterLore IDs change. It fails on unresolved or
duplicate references, non-voice signal bindings, missing intent/asset references,
or any non-surprise binding accidentally marked approved.

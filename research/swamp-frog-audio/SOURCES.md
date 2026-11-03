# Swamp Frog creature — audio source compendium

Date: 2026-11-03 · Author: Claude (research only, no game files touched)
Scope: free / obtainable frog and toad recordings that can be mined for an aggressive
swamp-creature voice set. Evidence labels follow `claude-exchange/PROTOCOL.md`.

## 1. Licence tiers (how every candidate gets tagged)

| Tier | Licences | Meaning for us |
|---|---|---|
| SHIP | CC0, Sonniss GDC royalty-free | Can go in a public Thunderstore package. Keep a credits line anyway. |
| SHIP-ATTRIB | CC BY | Can ship **with** a credits file naming recordist + source + licence. |
| NC | CC BY-NC, BY-NC-SA, BBC RemArc | Non-commercial only. Whether a free Thunderstore mod counts is **Brian's call** — not decided here. |
| REF | All-rights-reserved, Macaulay, californiaherps.com | Listen/reference only. Shipping needs a licence or written permission. |

## 2. Sources

### A. xeno-canto — frogs group (best targeted source)
- SOURCE-INSPECTED: xeno-canto now covers frogs and toads as a recording group; API v3 supports group, sound-type, licence, quality and length filters. Since 2025-10-10 an API key is required (free account → account page).
- Per-recording "sound type" field includes values like *territorial call* (e.g. XC883286, Marsh Frog, advertisement + territorial call). That lets a bot pull **by behaviour**, not just species.
- Licences vary per recording; the example above is **CC BY-NC-SA 4.0** → NC tier. Expect many NC; filter `lic:` for CC0/BY when shipping.
- Bot-listenable: yes — API returns direct audio file URLs + metadata.

### B. Freesound (largest SHIP/SHIP-ATTRIB pool)
- SOURCE-INSPECTED: APIv2 with free token; text search + filter syntax (duration, tag, licence, rating); preview MP3s downloadable with token; per-sound audio-analysis descriptors available through the API.
- Site has an "Approved for Free Cultural Works" licence filter = CC0 or CC BY only (excludes NC/Sampling+).
- Known example: diegolar "hydro single frog vocalizations in a pond" (#630061, CC BY 4.0) — hydrophone recording, used by others as a source for tuned croaks. Worth auditioning for underwater/bubbling texture.
- Bot-listenable: yes.

### C. iNaturalist sounds
- SOURCE-INSPECTED: observation API filters on `sounds=true` and `sound_license=` (cc0, cc-by, …); each sound carries `license_code`, `attribution`, `file_url`.
- iNatSounds dataset (2025 paper): ~230k files, 5,500+ species, amphibians included — large, but labels are **species**, not call type. Needs ear/AI sorting.
- Bot-listenable: yes. Quality is phone-grade more often than xeno-canto.

### D. Sonniss GDC Game Audio Bundles (2015–2026)
- SOURCE-INSPECTED: free, royalty-free, commercial use, no attribution required; 2024+ licence forbids using the files to train AI models (analysing/selecting with tools is a different act — UNKNOWN whether Sonniss would see feature extraction as an issue; low risk, flag only).
- UNKNOWN: whether any bundle contains frog/swamp/creature-vocal files. Each bundle page lists its files — needs a text search of those lists for frog, toad, swamp, creature, monster, croak, growl.
- High value even without frogs: creature/monster vocal libraries for layering under frog material.

### E. BigSoundBank / OpenGameArt
- SOURCE-INSPECTED: BigSoundBank "Frogs 1" (#0997) is CC0; OpenGameArt "Ribbit Frog Sounds" is CC0 but tiny and cartoonish. Ambience/chorus filler at best.

### F. BBC Sound Effects archive
- SOURCE-INSPECTED: 33,000+ effects incl. frog calls, WAV download, **RemArc licence = personal / educational / research only**, commercial use requires purchase. NC tier at best; treat as prototype/reference unless Brian decides otherwise.

### G. Macaulay Library (Cornell)
- SOURCE-INSPECTED: free to listen and embed (non-commercial); any download for commercial use, and some educational uses, needs a licence request by ML catalog number. REF tier. Large amphibian holdings incl. the classic frog-call albums.

### H. californiaherps.com (best **reference map** of call types)
- SOURCE-INSPECTED: per-species sound pages already split by behaviour — e.g. American Bullfrog: advertisement, **alarm call** (squeak on jumping into water), **encounter / territorial** calls, **release call** when handled, **distress scream**. Boreal Toad and Arizona Treefrog pages have encounter calls (the Arizona one is a low grating rasp).
- Licence: UNKNOWN (not stated in what was read; assume all rights reserved). REF tier — use as the listening checklist for what to find elsewhere, or ask the site owner.

### I. Science literature (what "heinous" frog sounds actually exist)
- SOURCE-INSPECTED: recognised anuran call categories — advertisement, aggressive/encounter, release, defensive (**distress**), feeding. Distress calls are loud, open-mouthed, scream-like; documented for Leptodactylus (incl. the *pentadactylus* group), Pristimantis, Smilisca. Bullfrog aggressive interactions include encounter calls and a **low-frequency growl**. Fonoteca Neotropical (FNJV, Brazil) holds distress-call recordings; licence UNKNOWN.
- Classic album *Sounds of North American Frogs* (Bogert, 1958) has a "Scream of the Southern Leopard Frog" track and Pig Frog choruses — copyrighted, REF only.

## 3. Call type → creature state map (PROPOSAL, not decided)

| Creature state | Real frog source to hunt for | Notes |
|---|---|---|
| Battle cry / aggro | bullfrog encounter call + low growl; pig frog grunt | Pitch-down 3–7 st for size (design inference) |
| Hunt / stalking | low growl, toad encounter trill, treefrog rasp | Short, low, repeated |
| Investigate / inquisitive | single croak, soft chirp, short encounter note | Quiet, rising inflection if available |
| Alarmed | bullfrog alarm squeak (+ splash) | Bright, abrupt |
| Respond / call-back | answering advertisement call | Used for pack chatter |
| Exclaim | short sharp territorial call | Sub-second |
| Hurt | release call, short distress yelp | 0.2–0.8 s |
| Death | long distress scream (Leptodactylus, leopard frog) | Strongest "heinous" material |
| Night chatter / ambience | choruses (bullfrog, pig frog, marsh frog) | Loopable beds, separate from creature voice |

Realistic expectation (inference): raw frog audio alone will read as "frog", not "monster".
Expect pitch/time processing and layering under creature growls (Sonniss) or ElevenLabs
gap-fill for the battle-cry and death slots.

## 4. Open items for Brian
1. Ship policy for NC material (xeno-canto BY-NC, BBC RemArc): prototype-only, or allowed in the free release?
2. UNKNOWN: the Unity asset's licence terms for redistribution inside a Valheim AssetBundle. Check before any public package.
3. Where the creature lives: repo layout says enemy species are Living World content (LivingWorldEnemyFamilies already ships Spawn That configs) and Souls repairs stay in `compatibility/` — placement still yours.

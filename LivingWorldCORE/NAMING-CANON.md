# Living World personal names and kinship

Established 2026-10-27. Living World content canon, consumed today by
`LivingWorldHumanNPC/src/HumanProfile.cs` and `NorseNameCatalog.cs`. This is not
Iron Gate canon or a claim that every Scandinavian community named people alike.

## What runs today

The fresh human identity path is `HumanIdentity.InitializeFresh` →
`HumanProfile.Generate` → persisted identity record → character display name.
The catalog contains 128 masculine and 138 feminine given names, replacing the
previous 12 of each and 12 English-style family names. Each masculine entry has
an explicit genitive (possessive) form for patronymics. There are 34,048 possible
given-name/patronymic combinations before considering other identity fields.
Names are not unique IDs: different people can legitimately share a full name.

The first seeded name draw chooses the person's given name; the second chooses
a father's name. The child's name style selects `son` or `dóttir`. The unchanged
draw count preserves seeded appearance and temperament. The full resulting name
is saved literally; adding names to a later catalog never renames a saved person.
No online naming service is needed. Newly generated names may change between
catalog revisions when the same seed has not yet been saved.

The separate Village Foundation name coordinator handles provider/fallback
requests; it is not this runtime's naming source. The enemy lore-name overlay
changes creature-type labels, not personal names. No patronymic implementation
was found in the workspace before this revision. Names already seen in another
installed mod cannot be attributed to this generator without runtime evidence.

## Orthography is language, not decoration

Use a consistent normalized West Norse register with the scholarly `j`
convention: Bjǫrn, Bjarni, Þorbjǫrn. The historical sources and editions also use
`i` for this consonant. These are editorial normalizations, not exact copies of
runestones or one universal Viking spelling. The catalog retains a few documented
variants (for example Þrándr and Þróndr); totals count catalog spellings, not
necessarily distinct etymological roots.

Preserve acute accents and the letters Þ/þ, Ð/ð, Æ/æ, and Ǫ/ǫ. An acute accent
is not a random flourish. Þ and ð represent different dental sounds; æ and ǫ are
letters/vowels, not alternative skins for a/e/o. Do not insert combining marks,
rich-text markup, or runes into persisted personal names. A future rune inscription
should be a separately researched presentation, not letter-for-letter replacement.
The profile reader accepts explicit precomposed Nordic letters and keeps its
40-character name and 256-character record bounds. In-game font coverage remains
a visual acceptance check; serialization passing does not establish font support.

## Parent-name grammar

The relationship suffix attaches to the parent's possessive form, not blindly to
their everyday first name. Examples from the explicit catalog:

| Parent | Possessive | Son | Daughter |
| --- | --- | --- | --- |
| Bjǫrn | Bjarnar | Bjarnarson | Bjarnardóttir |
| Þórðr | Þórðar | Þórðarson | Þórðardóttir |
| Helgi | Helga | Helgason | Helgadóttir |
| Snorri | Snorra | Snorrason | Snorradóttir |
| Ketill | Ketils | Ketilsson | Ketilsdóttir |
| Sturla | Sturlu | Sturluson | Sturludóttir |
| Ǫrn | Arnar | Arnarson | Arnardóttir |

Thus **Þóra Bjarnardóttir** means Þóra, daughter of Bjǫrn. Her brother might be
**Egill Bjarnarson**. If Egill has a daughter named Guðrún, her patronymic would
be **Guðrún Egilsdóttir**, not an inherited Bjarnarson family surname. Marriage
does not automatically replace a woman's patronymic with her spouse's name.
The catalog uses explicit forms so irregular declensions are reviewable data.

## Honest lineage: naming is not yet a family tree

Current generated patronymics supply an implied background father. They do not
create a second NPC, establish a shared parent ID, or prove two matching names
are relatives. The serialized profile still contains a display name and identity,
not ancestry links. Do not describe this revision as functioning genealogy.

The canon contract for future real families is:

1. Store stable parent identities and the relation's provenance separately from
   a display name. Distinguish known, reported, unknown, and authored background.
2. For a known parent, derive from that actual parent's given name and recorded
   grammatical form. Never draw a new unrelated parent to fill a surname.
3. Share the same parent reference between siblings; do not infer it from text.
4. Keep birth relationship, foster/adoptive kin, household, marriage, faction,
   and hereditary family names as different concepts.
5. Record parent-name snapshots so death, missing actors, and later spelling
   edits cannot erase ancestry. Preserve existing names unless an explicit,
   recorded story event changes them.
6. Add validated feminine genitives before automatic matronymics. Their absence
   in this runtime is an implementation boundary, not a claim mothers did not
   appear in historical naming.

## Masculine, feminine, and shared names

Generate complete established names from curated pools. Do not turn arbitrary
names masculine or feminine by replacing a final letter. Documented pairs such
as Helgi/Helga, Bjarni/Birna, and Þórhallr/Þórhildr need different treatment:
the first pairs share roots; the last shares the Þór- element but is not a
mechanical male/female translation. Bjǫrn and Bjarni are both masculine despite
different endings. Grímr and Gríma have separate catalog entries.

Auðr is included in both pools because masculine and feminine bearers occur in
the Landnámabók extraction. This is a small evidence-backed shared pool; it does
not imply a large historical inventory of modern gender-neutral names. Current
`HumanSex` still has two presentation models. Name style, pronouns, gender
identity, and model choice should become separate fields before supporting
independent selections. Do not silently label a new suffix an ancient custom.
An authored neutral relationship expression can be added later with an explicit
Living World label and a persistence design.

## Meanings and regional flavor

Name elements can carry associations without making every compound a literal
sentence or the bearer an embodiment of its translation. Some etymologies are
uncertain or have multiple proposed explanations. Examples of useful roots:

| Element/name | Association | Living World storytelling use |
| --- | --- | --- |
| Bjǫrn, -bjǫrn | bear | Family naming echoes; not proof of a bear cult |
| Úlfr, -úlfr | wolf | Shared family element; not an automatic enemy flag |
| Hrafn | raven | Memory, scouting, family stories |
| Ari / Ǫrn | eagle | Coastal or highland imagery |
| Steinn, Stein- | stone | Continuity, craftsmanship, a remembered ancestor |
| Geirr, Geir- | spear | Martial associations without assigning a class |
| Gunn-, -gunnr | battle | Ancestral naming echoes across name styles |
| -hildr | battle | An established feminine compound element |
| Þór-, Þor- | Þórr | Theophoric naming, not a claim to divinity |
| -dís | female supernatural/guardian association | Mythic resonance; avoid one rigid gloss |
| -leifr / -leif | inheritance, heir association | Remembered kin and continuity |
| Sæ- | sea | Coastal family traditions |

Regional policy is **authored Living World lore**, not historical Valheim biome
ethnography. Meadows communities may favor ancestral repetition; Black Forest
families may tell raven/bear/wolf stories; mountain communities may favor stone
imagery; coastal communities may favor sea imagery. These remain story guidance,
not hard name restrictions. Ancestral origin must be separate from current biome:
a woman named Sólveig can live in the Swamp, migrate, or have an unexpected trade.

This revision applies the whole pool everywhere. Biome weighting is deliberately
not connected to the current generator, which receives no birthplace context.
When origins exist, retain a substantial global pool and persist the first
selection. Travel must never reroll a name. Occupational and regional bynames
should be separate earned/authored fields; translating English fantasy titles
word-by-word and claiming Old Norse accuracy is prohibited.

Family storytelling can later reuse a documented name element, initial sound,
or ancestor's name. Such echoes support a relationship already recorded; they
must not fabricate one. This provides variety with cultural structure instead
of assembling unlimited unattested combinations.

## Research trail and limits

- [Sara L. Uckelman: names extracted from Landnámabók](https://www.ellipsis.cx/~liana/names/norse/landnamabok.html): catalog selection and masculine/feminine usage. The surviving text is later than the settlement events; it includes later descendants. We use normalized spellings and omit several foreign/Christian names from our thematic selection; the historical population was not culturally uniform.
- [Uckelman: Old Norse naming guide](https://www.ellipsis.cx/~liana/sca/boke/pageoldnorse.pdf): West Norse scope, common patterns, relationship names and inflection. This concise guide contains apparent typographical inconsistencies; do not transcribe its table mechanically (for example Sturla requires Sturlu- for either suffix).
- [Viking Answer Lady: men's names](https://www.vikinganswerlady.com/ONMensNames.shtml) and [women's names](https://www.vikinganswerlady.com/ONWomensNames.shtml): individual element discussions with references to Cleasby–Vigfusson, Geirr Bassi and Peterson. Reconstructed/doubtful headwords are explicitly flagged there; their existence on that site alone is not proof of historical use.
- [Tristan Matthias Mueller-Vollmer, Personal Names on Swedish Viking Age Runestones (2021)](https://asset.library.wisc.edu/1711.dl/6YOMT7DYKX7CU8Q/R/file-555a3.pdf): original research on inscription evidence, alliteration, variation, repetition and regional/social factors. East Scandinavian inscription findings should not be silently presented as identical to Icelandic saga practice.

The implementer-curated genitives are explicit grammatical editorial data, not
a claim that every possible full-name combination is attested. New additions
need a source, spelling register, appropriate name pool and, if used as a parent,
a reviewed possessive form. Avoid definitive whole-name translations where only
an element or one of several proposed etymologies is established.

## Acceptance

Offline profile tests cover the full catalog, 4,096 generated identities,
determinism, name-style/suffix membership, irregular parent forms, unsafe text,
Unicode and old-record round trips, and seeded appearance preservation. The
identity adapter harness covers fresh/persisted identities with game API doubles.
The offline package build includes the catalog explicitly. Unity rendering,
network play and actual campaign kinship are separate acceptance work.

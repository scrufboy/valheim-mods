# Download targets for ChatGPT (it has a browser and your PC; Claude's sandbox only reaches GitHub)

Goal: real aggressive frog audio for the swamp creature slots. Pull audio, tag licence, drop
into `work/raw/`, then run `research/swamp-frog-audio/tools/analyze.py` (or AUDITION.html).
Never commit third-party audio to the repo. Labels: SOURCE-INSPECTED unless noted.

## 1. Full-length originals behind the ESC-50 clips already delivered (Freesound)
The delivered clips are 5-second cuts. The originals are longer and contain more calls.
Download needs a free Freesound login (or the API token).

| Original | Recordist | Licence | URL |
|---|---|---|---|
| tree frog.wav | reinsamba | CC-BY | https://freesound.org/people/reinsamba/sounds/15689/ |
| Water frogs.wav | reinsamba | CC-BY | https://freesound.org/people/reinsamba/sounds/17970/ |
| frogs01.wav | WIM | CC-BY | https://freesound.org/people/WIM/sounds/18755/ |
| frogs03.wav | WIM | CC-BY | https://freesound.org/people/WIM/sounds/18757/ |
| 20070303.frogs.flac | dobroide | CC-BY | https://freesound.org/people/dobroide/sounds/31836/ |
| 200703101950PacificChorusFrogsChorusAndTrainNR.flac | daveincamas | CC-BY | https://freesound.org/people/daveincamas/sounds/32515/ |
| 200703101950PacificChorusFrogsConfrontAirplane.wav | daveincamas | CC-BY | https://freesound.org/people/daveincamas/sounds/32834/ |
| sherman_frogs_spring_2007.mp3 | aguasonic | CC-BY | https://freesound.org/people/aguasonic/sounds/52085/ |
| frogs.wav | alexbird | CC-BY | https://freesound.org/people/alexbird/sounds/52789/ |
| frog in the tropical garden.wav | E330 | CC-BY | https://freesound.org/people/E330/sounds/102908/ |
| FROGS_4824_st.wav | Sedi | CC-BY-NC | https://freesound.org/people/Sedi/sounds/70962/ |
| FrogsFLP.wav | daveincamas | CC-BY | https://freesound.org/people/daveincamas/sounds/71964/ |
| tree frog.wav | Tito Lahaye | CC-BY-NC | https://freesound.org/people/Tito%20Lahaye/sounds/83527/ |
| Frogs Under Pier.wav | casemundy | CC-BY | https://freesound.org/people/casemundy/sounds/130584/ |
| Heavy Frogs | unfa | CC-BY | https://freesound.org/people/unfa/sounds/154793/ |
| swamp frogs.wav | Tomlija | CC-BY | https://freesound.org/people/Tomlija/sounds/99193/ |
| Frogs.wav | juskiddink | CC-BY | https://freesound.org/people/juskiddink/sounds/99644/ |
| Frog Concerto | Anton | CC-BY-NC | https://freesound.org/people/Anton/sounds/156026/ |
| file0306_pond_frog_bird_3.wav | maphill | CC0 | https://freesound.org/people/maphill/sounds/187201/ |
| Frogs Forever | magixmusic | CC-BY-NC | https://freesound.org/people/magixmusic/sounds/189795/ |
| Edible Frog - Pelophylax kl esculentus140430_0191.wav | klankbeeld | CC-BY | https://freesound.org/people/klankbeeld/sounds/237499/ |
## 2. Freesound searches (log in, licence filter "Approved for Free Cultural Works" = CC0 + CC BY)
- https://freesound.org/search/?q=bullfrog
- https://freesound.org/search/?q=frog+growl
- https://freesound.org/search/?q=frog+scream
- https://freesound.org/search/?q=frog+distress
- https://freesound.org/search/?q=toad
- https://freesound.org/search/?q=pig+frog
- https://freesound.org/search/?q=frog+chorus+night
- https://freesound.org/people/diegolar/sounds/630061/ - hydrophone frog vocalizations in a pond (CC BY 4.0), underwater texture

## 3. xeno-canto (frogs group; free account + API key for download)
Search box queries:
- `grp:frogs type:"distress call"` - death / hurt (the screams)
- `grp:frogs type:"territorial call"` - battle cry / exclaim
- `grp:frogs type:"aggressive call"` - battle cry / hunt
- `grp:frogs type:"alarm call"` - alarmed
- `grp:frogs type:"release call"` - hurt
- `grp:frogs Lithobates catesbeianus` (bullfrog), `Leptodactylus` (screams), `Pelophylax ridibundus` (marsh frog)
Check each recording's licence line; BY-NC-SA is common (NC tier).

## 4. iNaturalist (CC0 / CC BY sounds)
- https://www.inaturalist.org/observations?taxon_id=20979&sounds=true&sound_license=cc0,cc-by&quality_grade=research
- Narrow by species: bullfrog taxon 65489 (from bioamla docs; verify on the site).

## 5. Reference listening (licence unknown / all rights reserved - listen, do not ship)
- https://californiaherps.com/frogs/pages/l.catesbeianus.sounds.html - bullfrog alarm, encounter, release, DISTRESS SCREAM (the best map of what to look for)
- https://californiaherps.com/frogs/pages/a.b.boreas.sounds.html - boreal toad encounter/release calls
- https://www.californiaherps.com/noncal/southwest/swamphibians/pages/h.wrightorum.sounds.html - low grating encounter rasp
- Macaulay Library (search Anura): listen free; licence request needed for any use.
- BBC Sound Effects (https://sound-effects.bbcrewind.co.uk/search?q=frog): RemArc = personal/education/research only.

## 6. Sonniss GDC bundles (royalty-free, no attribution, commercial OK; 2024+ forbids AI training use)
Search each year's file list (https://gdc.sonniss.com) for: frog, toad, swamp, croak, creature, monster, growl.
Creature growls are layering material for battle cry / death even if no frogs are found.

## Return
A picks CSV (AUDITION.html "Export picks CSV" format) + licence for each file, into a new
CODEX-OUTBOX submission. No processing, installs, or creature/Spawn That/Drop That changes.

# Swamp frog creature — audio research kit

Hostile swamp creature (frog type), Unity asset + Spawn That / Drop That.
This folder is research and tooling only. Nothing here is in the game.

- `SOURCES.md` — where frog audio lives, licence tiers, call-type → creature-state map
- `slots.v1.json` — PROPOSED voice slots, variant counts, jitter and cooldown ranges
- `tools/harvest.py` — pulls candidates from Freesound, xeno-canto, iNaturalist into `candidates.csv` (+ audio with `--download`)
- `tools/analyze.py` — "tone check": splits files into call events, measures each one, proposes a slot

## Pipeline

1. Keys: `FREESOUND_TOKEN` (freesound.org/apiv2/apply), `XC_API_KEY` (xeno-canto account page). iNaturalist needs none.
2. `pip install numpy scipy soundfile`
3. `python tools/harvest.py --out work --limit 5 --download` (smoke run), then raise `--limit`.
4. `python tools/analyze.py work/raw --out work/features.csv --candidates work/candidates.csv`
5. Ear / AI pass on `features.csv`: fill `ear_verdict` and `keep`, correct `slot_guess`.
6. Cut kept events, process (pitch/time/layer), export per slot as `swamp_frog_<slot>_NN.wav`.
7. Keep a `CREDITS.md` row for every SHIP-ATTRIB file that ends up in a build.

`work/` is scratch and should not be committed (audio + third-party files).

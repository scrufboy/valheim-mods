# VERIFICATION
- OFFLINE-TESTED: `analyze.py` run on 3 synthetic files (tonal croak x3, rough growl, noisy scream). Event splitting found 5 events correctly; durations/centroid/f0 sane (croak f0 302 Hz vs 300 Hz generated). Slot guess: scream → death (correct), croaks → hunt/exclaim/investigate (plausible), **growl → respond (wrong; synthetic noise inflated centroid)**. Treat guesses as a pre-sort; calibrate on real frog audio.
- OFFLINE-TESTED: `harvest.py` compiles; licence tiering checked on 7 strings (CC0 URL→SHIP, BY→SHIP-ATTRIB, BY-NC-SA→NC, blank→REF).
- UNKNOWN: harvest.py against live APIs — authoring sandbox could not reach freesound.org / api.inaturalist.org (proxy refused). xeno-canto v3 field names and Freesound OR-licence filter syntax are from docs, unverified. First run: `--limit 5`, inspect CSV.
- INSTALLED: nothing. LIVE-PROVEN: nothing.

# REQUEST — ChatGPT / Codex

0. Start from `research/swamp-frog-audio/DOWNLOAD-TARGETS.md`; Brian has the first candidate pack and AUDITION.html.
1. Commit this package to a branch (`research/swamp-frog-audio`), not main, unless Brian says otherwise.
2. Get keys from Brian or have him create them: Freesound API token, xeno-canto API key.
3. Smoke run `harvest.py --limit 5 --download`; fix field-name/filter errors and record them in your outbox VERIFICATION.
4. Full harvest, then `analyze.py`. Do an ear/audio pass on `features.csv`: fill `ear_verdict`, `keep`, correct slot. Prioritize: distress screams (death/hurt), bullfrog encounter/growl (battle cry/hunt), alarm calls, choruses (night/ambience).
5. Search the Sonniss GDC bundle file lists (2015–2026) for frog / toad / swamp / creature / croak / growl and report hits with bundle + filename.
6. Return a shortlist per slot with licence tier — do not cut, process or install audio, and do not touch the creature, Spawn That / Drop That configs or Souls until Brian decides the open items in HANDOFF.md.
7. Do not commit downloaded third-party audio to the repo.

## Where the audio is (18:25)
Brian's PC: `C:\Users\scruf\Downloads\LWC-swamp-frog-audio-candidates-2026-11-03.zip`
(548 clips + 14 pitched demos + AUDITION.html + MANIFEST.csv + CREDITS.md). Not in the repo:
the repo is public and 463 clips have no known licence.
Next for ChatGPT: fetch what `research/swamp-frog-audio/DOWNLOAD-TARGETS.md` lists, prioritising
low/aggressive bullfrog calls and distress screams (battle_cry and hunt are nearly empty).

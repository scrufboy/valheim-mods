# Body-reaction voice clips (1.0.32.13)

Licensed Gamemaster Audio Human Vocalizations (Human Male B / Human Female A). The WAVs are NOT in GitHub;
they are copied from Brian's licensed source into `BepInEx/plugins/LivingWorldHumanNpc/voices/body/<gender>/<pool>/`.
Candidate set for Brian's in-game audition (issue #5). None of these is a combat-voice clip.

| Gender | Pool | File | Seconds | SHA-256 (first 16) |
| --- | --- | --- | --- | --- |
| male | onset | `01__voice_male_b_cough_interrupt_02.wav` | 0.52 | `7e80d0cfb3e2aebf` |
| male | onset | `02__voice_male_b_cough_interrupt_03.wav` | 0.73 | `131cdbb08ba43bc9` |
| male | onset | `03__voice_male_b_cough_interrupt_04.wav` | 0.59 | `0865711a0136a7bc` |
| male | onset | `04__voice_male_b_cough_short_croak_01.wav` | 0.5 | `85fc7c7c4173168e` |
| male | onset | `05__voice_male_b_cough_short_croak_02.wav` | 0.53 | `8812b482463fc981` |
| male | repeat | `01__voice_male_b_cough_short_01.wav` | 0.68 | `fbaa428eea92ad6b` |
| male | repeat | `02__voice_male_b_cough_short_02.wav` | 0.45 | `33a6c11ecea58aa8` |
| male | repeat | `03__voice_male_b_cough_short_03.wav` | 0.45 | `539eb5c56f3deb29` |
| male | repeat | `04__voice_male_b_cough_short_04.wav` | 0.54 | `a4ef368b338551ac` |
| male | repeat | `05__voice_male_b_cough_short_05.wav` | 0.52 | `cb5d7894d80bb626` |
| male | repeat | `06__voice_male_b_cough_short_06.wav` | 0.43 | `4772f52423b15a90` |
| male | repeat | `07__voice_male_b_cough_short_07.wav` | 0.59 | `42c9d5c122522386` |
| male | repeat | `08__voice_male_b_cough_short_08.wav` | 0.4 | `2e272c25afaed28f` |
| male | repeat | `09__voice_male_b_cough_short_09.wav` | 0.45 | `ad13a071dab2b147` |
| male | repeat | `10__voice_male_b_cough_short_10.wav` | 0.67 | `9b41671a34a8c6ca` |
| male | repeat | `11__voice_male_b_breath_wheeze_01.wav` | 1.16 | `010351334f3f222b` |
| male | repeat | `12__voice_male_b_breath_wheeze_02.wav` | 1.34 | `01e34c994f6101a0` |
| male | recover | `01__voice_male_b_breath_pant_01.wav` | 0.27 | `09aac39ac94b3eeb` |
| male | recover | `02__voice_male_b_breath_pant_02.wav` | 0.71 | `bd288744002bb1aa` |
| male | recover | `03__voice_male_b_breath_pant_03.wav` | 0.39 | `431522e4848fb9e9` |
| male | recover | `04__voice_male_b_breath_sigh_06.wav` | 1.08 | `b1f1a705f85a3d94` |
| male | recover | `05__voice_male_b_breath_sigh_09.wav` | 0.9 | `cf4997d1f18763d8` |
| female | onset | `01__voice_female_a_cough_interrupt_01.wav` | 0.54 | `bad0902d5d937d4f` |
| female | onset | `02__voice_female_a_cough_interrupt_02.wav` | 0.45 | `197e89bdee451fa2` |
| female | onset | `03__voice_female_a_cough_interrupt_03.wav` | 0.71 | `3ec09d6e2545771c` |
| female | onset | `04__voice_female_a_cough_interrupt_04.wav` | 0.72 | `c052d90bc15bae0a` |
| female | onset | `05__voice_female_a_cough_short_03.wav` | 0.56 | `31f0d275adf7f8b2` |
| female | repeat | `01__voice_female_a_cough_short_01.wav` | 0.45 | `76c4b4dc6a81961b` |
| female | repeat | `02__voice_female_a_cough_short_02.wav` | 0.32 | `c2586efd6d953cdf` |
| female | repeat | `03__voice_female_a_cough_short_04.wav` | 0.33 | `867b413b789c991f` |
| female | repeat | `04__voice_female_a_cough_short_05.wav` | 0.34 | `c1d11d537252dfd2` |
| female | repeat | `05__voice_female_a_cough_06.wav` | 1.06 | `bff7d8401d814117` |
| female | repeat | `06__voice_female_a_cough_08.wav` | 0.53 | `24acb0bfcd18142b` |
| female | repeat | `07__voice_female_a_cough_09.wav` | 0.69 | `f8519291694e45cf` |
| female | repeat | `08__voice_female_a_cough_03.wav` | 1.38 | `69c537879cabb838` |
| female | recover | `01__voice_female_a_breath_out_of_air_01_short.wav` | 1.37 | `1e63b324fd9cd61d` |
| female | recover | `02__voice_female_a_breath_exhausted_01.wav` | 0.99 | `af917ee770d30d0f` |
| female | recover | `03__voice_female_a_breath_exhausted_02.wav` | 1.15 | `d81b0bce272d0c69` |
| female | recover | `04__voice_female_a_breath_tired_02.wav` | 0.94 | `5ff899d98ee4ddc1` |
| female | recover | `05__voice_female_a_breath_tired_03.wav` | 0.85 | `1091e6aef403a6c2` |

## 1.0.32.14 body states (candidates, installed for audition; not live-verified)

Source: ChatGPT's private handoff `Living_World_CORE_Body_Reactions_COMPLETE_PRIVATE_2026-10-28` (18 audition
candidates, package SHA-256 list verified). Licensed Gamemaster Human Male B / Female A; WAVs are not in this repo.
Installed to `voices/body/<gender>/<pool>/` under their original file names. The 40 poison clips above are unchanged.

Triggers (source-inspected in Valheim 1.0.20 decompile; status owner only, never damage ticks):

| Pool | Trigger | Rule |
| --- | --- | --- |
| `burning_onset` | native `Burning` status present (fire-damage hits, lava; `Character.AddFireDamage` / `UpdateLava`) | once, 0.3 s after it holds |
| `burning_repeat` | same, still burning | every 6–9 s |
| `frost` | native `Frost` status (frost hits; `Character.AddFrostDamage`) | onset, then every 8–12 s |
| `swim` | `Character.IsSwimming` continuous ≥ SwimSeconds (20), then out of the water ≥ 1.5 s | one breath, 30 s cooldown |
| `exhaustion` | companion heavy-attack stamina reaches 0 (`NpcBody.Exhaustions`) | one breath, 45 s cooldown |
| `env_recover` | burning or frost ends after ≥ 3 s | one exhale, at most one per 30 s |

One voice per actor: combat lines and poison lines take precedence and cut a state line; a state line starts only when
the actor is silent and 2.5 s after its last body line; a line whose state ends while it sounds is cut. No other-gender
fallback; a missing pool is silent.

Not wired (no native signal reaches a companion; would need a new status writer = Brian's decision):
- **Cold / Freezing** (weather): applied only in `Player.UpdateEnvStatusEffects`. Companions are non-Player Humanoids.
- **Wet / rain**: by design never a voice trigger.
- **Smoke** (`Smoked`): companions can receive it (`Character.UpdateSmoke`, Player-copied `m_tolerateSmoke`), but the
  handoff requires source validation and no duplicate cough pool; left silent.
- **Air / drowning**: Valheim has no air meter; only the long-swim rule above.

| Gender | Pool | File | s | SHA-256 (16) |
| --- | --- | --- | --- | --- |
| male | burning_onset | `voice_male_b_hurt_pain_set_4_04.wav` | 0.61 | `a06dcb2c9a5f9f3a` |
| male | burning_repeat | `voice_male_b_effort_struggle_01.wav` | 1.42 | `027b058dfd398691` |
| male | env_recover | `voice_male_b_breath_sigh_03.wav` | 1.66 | `ec7611ad54bdc2f9` |
| male | exhaustion | `voice_male_b_breath_sigh_04.wav` | 1.38 | `6dfee4197a0c15fa` |
| male | exhaustion | `voice_male_b_breathing_08_sequence_short_03.wav` | 0.65 | `e3bb80739e8ba5a0` |
| male | frost | `voice_male_b_breath_quick_06.wav` | 0.36 | `1164580b36f279a2` |
| male | frost | `voice_male_b_breath_struggle_01.wav` | 0.38 | `92f0adc1a4c63eef` |
| male | swim | `voice_male_b_breath_pant_04.wav` | 1.48 | `16ddc82951e117fe` |
| male | swim | `voice_male_b_breath_struggle_03.wav` | 0.68 | `3d09bfef2c54a848` |
| female | burning_onset | `voice_female_a_hurt_pain_07.wav` | 0.46 | `dc52c47c04e05f8f` |
| female | burning_repeat | `voice_female_a_growl_frustrated_05.wav` | 0.6 | `81cead3cfd06507c` |
| female | env_recover | `voice_female_a_breath_deep_out_01.wav` | 1.2 | `7d6b342a5e52d216` |
| female | exhaustion | `voice_female_a_breath_tired_01.wav` | 1.17 | `1467eb2e889060ff` |
| female | exhaustion | `voice_female_a_effort_tired_03.wav` | 0.41 | `616818919fa9a350` |
| female | frost | `voice_female_a_breath_blow_01.wav` | 0.9 | `30667e5371c64f60` |
| female | frost | `voice_female_a_breath_quick_01.wav` | 0.71 | `f318040cc8cf2b3a` |
| female | swim | `voice_female_a_breath_deep_01.wav` | 1.09 | `3d90c64c5f067402` |
| female | swim | `voice_female_a_breath_out_of_air_01.wav` | 1.96 | `29479e4d2751dbf7` |

## 1.0.32.15 party clouds (no new clips)
Own and party bomb clouds now reach the party at `[Hazards] PartyCloudFraction` (0.35) through native `SE_Poison`, so
the installed poison pools cough on self-exposure too. Enemy clouds are unchanged (full, native).

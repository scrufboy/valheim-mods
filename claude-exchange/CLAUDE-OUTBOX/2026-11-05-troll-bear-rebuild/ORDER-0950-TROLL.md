# ORDER 09:50 — Þurs is not right. Fix all of this in one run, after Colossus 0.1.5 (ORDER-0940 step 1).

Brian (watched his own test video — you have it): "it's not right. it makes me mad that Trolls Revamped just handles it.. our club keeps showing up when it isn't equipped... it should not even be sneaking around the NON club variant troll... the guys are weird.. the trophy still not done! the handling of the dropped assets being reskinned to the duller more subdued blue. still not done..."

1. **Club shows when not equipped.** The purchased mace is baked into the body visual. Remove it from the body. Weapon visuals must come only from the equipped item through VisEquipment, attached to the model's own hand bone — exactly how Trolls Revamped' `Troll_ru` does it (vanilla components only, VisEquipment on the prefab). Unarmed variant = empty hands. Log variant = log/club in hand only while equipped.
2. **Unarmed troll "sneaking".** Locomotion looks wrong. Match clip playback to actual movement speed (walk clip at walk speed, run clip at run speed; blend thresholds against the real Troll walk/run speeds). No crouch/sneak look when it is just walking or chasing.
3. **"The guys are weird."** Watch Brian's video. List each thing that looks wrong in one line each, fix each one.
4. **Trophy.** Head of THIS Þurs on `TrophyFrostTroll` (world drop, inventory icon, item stand). Simple. Ship it.
5. **Dropped assets reskinned to Þurs' duller, subdued blue:** troll hide, trophy, any troll drop — and the hit/death shreds/VFX (Directive 4 §B). All match the body colorway.
6. Rebuild Þurs 0.3.1, install into LWC Formidable Arena, confirm load, one screenshot each: unarmed troll, log troll, trophy, hide drop. Report once.

Reference available: Trolls Revamped bundle (Brian supplied, `External Assets/Trolls-Revamped-2.1.1-ReadOnly-Study`). Copy the method, not the files.
No new test frameworks. Do not stop between steps.

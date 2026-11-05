# ORDER 08:40 — one Tooler button per creature, coming in from afar. Do it now, small.

Brian: "you can't raid those all at once... but buttons for me to have them come in from afar — we need to see them in action."

Do, in LWC Formidable Arena (Brian asked for it — this is the go):
1. Add four **manual** Custom Raids events (`Random = false`), one creature type each, in a new file `custom_raids.supplemental.lwc_creature_tests.cfg`:
   - `lwc_test_thurs` — Þurs (`Troll`), 1
   - `lwc_test_bjorn` — Bjorn (`Bjorn`), 1
   - `lwc_test_marauder` — Sunken Marauder (`LWC_MarshMarauder_Prototype`), 2–3, pickaxes
   - `lwc_test_colossus` — Undead Colossus (`LWC_UndeadColossus_Test`), 1
   Spawn them out of sight at raid distance so they travel toward Brian. Short duration. Start message names the creature.
2. Put four labelled buttons in the Tooler raid/test section: **Call Þurs · Call Bjorn · Call Marauders · Call Colossus**. One click = that event starts. Plus **End event**.
3. Do not edit the existing souls raids or the four-creature event. Do not touch combat/telemetry configs.
4. Install, verify the buttons appear and each event starts, report one line. Then Brian films.

No new tests. Git state when done.

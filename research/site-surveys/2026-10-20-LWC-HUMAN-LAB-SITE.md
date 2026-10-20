# LWC Human Lab site survey — 2026-10-20

Status: reconstructed from accepted live evidence. No world or terrain changes are
performed by this report.

## Evidence

- World: `LWC_HumanLab_v1`
- Character: `LWC_Tester`
- Human NPC build: `0.3.58`
- Tooler active during the run: `2.4.1`
- Telemetry: `LWC-20261020-115817-LWC_HumanLab_v1-part00.jsonl`
- Telemetry session: `4414b8966a0649a78d2913db3abfd22d`
- Video: `2026-10-20 11-57-03.mp4`
- Telemetry duration: 865.99 seconds
- Records: 3,540 including summary; dropped records: 0
- Video commentary and stationary pauses identify the center, four site corners,
  current workshop footprint, and final center confirmation.

The telemetry schema did not record the player position directly. It recorded two
companions' synchronized world positions plus their side/forward offsets from the
leader. Player X/Z was reconstructed by solving the two-dimensional rotation from
the companions' local leader-relative vectors to their world-space separation,
then subtracting each transformed offset from its actor position. The independent
player positions derived from the two companions agreed within an average of
`0.00000450 m` and maximum of `0.00002868 m`.

This reconstruction proves horizontal positions. It does not replace a live
ground-height/water survey for Y, slopes, blockers, or cut/fill estimates.

## Recorded main site

| Marker | Reconstructed X | Reconstructed Z | Notes |
| --- | ---: | ---: | --- |
| Spoken initial center | 305.46 | -264.78 | 5.34 m from polygon centroid |
| Corner 1 | 254.44 | -317.07 | Southwest/lower recorded edge |
| Corner 2 | 353.39 | -314.58 | Southeast/lower recorded edge |
| Corner 3 | 341.44 | -207.15 | Northeast/upper recorded edge |
| Corner 4 | 254.92 | -238.91 | Northwest/upper recorded edge |
| Spoken final center confirmation | 297.90 | -282.10 | 13.58 m from polygon centroid |

Derived geometry:

- polygon area: `8,703.56 m²`;
- polygon centroid: **X 303.70, Z -269.82**;
- edge lengths, Corner 1 through Corner 4: `98.98 m`, `108.09 m`,
  `92.17 m`, and `78.16 m`;
- axis-aligned walked bounds: approximately `98.95 × 109.92 m`;
- centroid-to-edge clearances: `46.00 m`, `44.44 m`, `45.83 m`, and
  `48.97 m`.

The polygon centroid is the best available maypole/settlement-lab center. The
spoken initial center was a useful approximation; the final confirmation was
farther south than the geometric center.

## Current workshop footprint

The four workshop pauses outline an approximately `18.31 × 24.75 m` footprint.
Its reconstructed center is approximately **X 274.45, Z -273.73**, 29.51 m from
the main-site centroid. It therefore fits naturally inside the resident/work area
and should be retained as the first surveyed facility rather than relocated.

## Fit decision

- A conservative 40 m resident/work circle fits inside all four surveyed edges.
- A 48 m working radius requires only local outward extension on the three tighter
  sides: approximately 2.00 m, 3.56 m, and 2.17 m respectively. Use a larger
  practical safety margin when grading; do not form another razor edge.
- The planned 64 m authority/transition radius does not require every metre to be
  flat. It may extend onto graded or natural surrounding terrain while beds,
  facilities, primary roads, and work routes remain in the inner measured area.
- The current pad cannot also contain the separated 64 × 64 m arena, safety area,
  and isolation corridor. The Combat Ward/arena needs its own live-surveyed
  expansion or nearby field. Do not compress the settlement and arena onto this
  pad.

## Runtime observations

- The world saved and exited normally.
- Automatic telemetry closed cleanly with zero dropped records.
- Natural brief combat occurred during the wider walk, but the long stationary
  marker windows and paired-coordinate reconstruction remained valid.
- A startup `NullReferenceException` came from
  `RRRCore.npc.Npc.DesignNpcBase` while RRRCore was attempting to create missing
  third-party NPC prefabs. It was not caused by terrain work.
- Terrain/road work produced 19 instances of
  `Can't unregister renderer for mud_road_v2(Clone)` between 12:01:58 PM and
  12:03:53 PM. This is a blocker for any future automated terrain-application
  feature until its ownership and cleanup path are reproduced and diagnosed.

## Next use

Use the installed Site Survey Tooler only for non-destructive live measurement,
grid sampling, campus preview, and export. Keep terrain application locked. Place
no permanent maypole or Combat Ward from this reconstruction alone; first verify
the centroid's ground height, the surrounding 48 m working circle, and candidate
arena direction through the live survey.

# The 10 deaths of 1.0.32.22: what a trained companion should have done differently

Source: `LWC-20261030-013547-LWC_HumanLab_v1-part00.jsonl` + `LogOutput.log` (LIVE data). Last 30 s of each
death reconstructed in `DEATH-WINDOWS.txt`. Native combat was in control; the observer owned nothing.

## By gear/tier

| Pair | Gear (armor) | Killed by | Big hits | Verdict |
|---|---|---|---|---|
| Tove (archer), Stig (guard) | iron q1 (29) | Seekers | 83-88 | Tier mismatch: Mistlands enemy vs Swamp-tier gear. |
| Astrid (guard), Aslak (archer) | iron q1 (63 shown) | Fulings, Berserkers | 28-66 | Under-geared for Plains, but winnable with defense. |
| Geirlaug, Frode, Jorunn, Helge, Sigurd, Gerlaug | plains q3 (63) | Fulings, Berserkers | 52-67 | Fair matchup. These six are on the AI. |

Each companion has 250 max HP. Each died to 183-343 damage within 30 s, from 5-19 hits.

## Patterns across all 10 (LIVE-PROVEN from the data)

1. **No hit was blocked. 0 of the 102 hits in the death windows.** `cfgShieldBlocking=false`: the experimental shield
   blocking is off, so companions carry the shield (shield in hand 28-93% of the final 30 s) but never raise it.
   Stamina was never the limit (minimum 122-150 in every window). **This is the largest single lever** and is exactly the
   Defense domain FormidableAI is meant to take over first.
2. **The fall-back starts too late and gets caught.** It fired in 9 of 10 windows. It started at 26-77 HP (10-30%)
   and ended "caught: stand and fight" in most cases. Fulings and Berserkers keep pace. A trained survivor
   disengages earlier, toward the partner or leader, and not into open ground.
3. **Archers die in melee.** All four archers held the sword for most of their final 30 s and were the enemy's target
   ("targetsWho=me"). There was no kiting and no keeping range. The Huntress doctrine (`MaintainFiringLane`/`KitePressure`)
   addresses exactly this.
4. **Partners die 2-11 s apart.** Each pair collapsed together. The second death followed the first with no rescue or
   break-off ("partnerDefense" never shows a save). The leader's HP stayed near full in most windows, so the party
   was not fighting as one.
5. **Their own poison was minor.** Attacker-less ticks (party cloud x0.35 at about 0.8 per tick) total 196 of 4,606 damage
   across the session (4%). This is not a cause of death.

## What a trained companion should have perceived and chosen

| Fact (fair source) | Should have chosen | Available in the live frame today? |
|---|---|---|
| Berserker windup in reach (sight) | Block with the shield in hand; dodge if no shield | Partly: `IncomingStrike` = target in attack. No windup/impact timing yet. |
| HP below about 50% with 2+ attackers (self, sight) | Disengage toward the partner/leader early | HP yes; attacker count yes (Strike/targetsWho); retreat path no. |
| Partner under 2+ attackers (party sight) | Protect/peel or regroup | Partner state partly (`partnerDefense`); no partner-HP fact in the shadow frame. |
| Archer with melee enemy within about 4 m (sight) | Kite to bow range before drawing the sword | Distance yes; kite path/footing no. |
| Seeker-class threat vs iron q1 gear (training/biome) | Avoid the engagement / stay at the leader | Enemy family yes; gear tier yes; no tier-vs-threat rule. |

## Recommendation (for Brian's decision, not a build)

Make Defense (block/dodge) the first FormidableAI live handoff as planned, judged by **survival outcomes**:
blocked-hit ratio, HP at disengage, and deaths in fair matchups (the plains q3 pairs). Then an early, directed
disengage. Do not tune damage or HP.

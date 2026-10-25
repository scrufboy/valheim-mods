#!/usr/bin/env python3
"""Regression ledger, part 2: grade a recorded play session.

usage: analyze_session.py <LWC-...jsonl> [more files...]

Reads the schema-10 telemetry written by LivingWorld.Telemetry and checks the
behaviors that have regressed before. Prints PASS / FAIL / INFO per check, exits
non-zero on any FAIL. Checks that had no opportunity in the session (e.g. no
night, no combat) are reported as NOT EXERCISED rather than passed.
"""
import json
import math
import sys
from collections import defaultdict

COMBAT = {"Combat", "CombatClear", "CombatRegroup"}
NO_TORCH = COMBAT | {"Readiness"}


def load(paths):
    rows = []
    for p in paths:
        with open(p, encoding="utf-8") as fh:
            for line in fh:
                line = line.strip()
                if line:
                    try:
                        rows.append(json.loads(line))
                    except json.JSONDecodeError:
                        pass
    return rows


def main(paths):
    rows = load(paths)
    start = next((r for r in rows if r.get("type") == "RunStart"), {})
    npc = [r for r in rows if r.get("type") == "Npc"]
    leader = [r for r in rows if r.get("type") == "Leader"]
    results = []

    def add(status, name, detail):
        results.append((status, name, detail))

    if start.get("schema") not in (10, 11):
        print("Not a Living World C.O.R.E. schema-10/11 session (core %s)." % start.get("core"))
        return 2
    names = sorted({r["npc"] for r in npc})
    duration = max((r["t"] for r in rows), default=0)
    print("Session: core %s, world %s, %.0f s, companions: %s" % (start.get("core"), start.get("world"), duration,
                                                                  ", ".join(names) or "none"))
    if not npc:
        add("FAIL", "Companions present", "no companion samples at all (registration/party failure?)")
        report(results)
        return 1
    add("PASS", "Companions present", "%d companion(s), %d samples" % (len(names), len(npc)))

    # 1. Ownership violations / faults
    violations = max((r.get("violations", 0) for r in npc), default=0)
    faults = max((r.get("faults", 0) for r in npc), default=0)
    add("PASS" if violations == 0 else "FAIL", "No domain ownership violations", "violations=%d" % violations)
    add("PASS" if faults == 0 else "FAIL", "No kernel/behavior faults", "faults=%d" % faults)

    # 2. Torch never during combat or readiness (0.3.46/0.3.59/0.3.60)
    bad = [r for r in npc if r.get("torch") and r.get("phase") in NO_TORCH]
    lit = [r for r in npc if r.get("torch")]
    dark_calm = [r for r in npc if r.get("dark") and r.get("phase") == "Calm"]
    add("PASS" if not bad else "FAIL", "Torch never in hand during combat/readiness",
        "%d bad samples" % len(bad) + (" (first at t=%.1f %s)" % (bad[0]["t"], bad[0]["npc"]) if bad else ""))
    if not dark_calm:
        add("NOT EXERCISED", "Torch lit at night in calm", "no dark calm samples")
    else:
        share = sum(1 for r in dark_calm if r.get("torch")) / len(dark_calm)
        add("PASS" if share >= .6 else "FAIL", "Torch lit at night in calm",
            "%.0f%% of dark calm samples lit (built light or settling delays account for the rest)" % (share * 100))

    # 3. Torch flicker (0.3.47): lit/unlit toggles less than 3 s apart
    flicker = 0
    by_npc = defaultdict(list)
    for r in rows:
        if r.get("type") == "Torch":
            by_npc[r["npc"]].append(r)
    for ev in by_npc.values():
        # A torch going out because a fight started is correct, not a flicker; only calm toggles count.
        flicker += sum(1 for a, b in zip(ev, ev[1:])
                       if b["t"] - a["t"] < 3.0 and b.get("phase") in ("Calm", "Regroup"))
    add("PASS" if flicker == 0 else "FAIL", "Torch does not flicker", "%d toggles under 3 s" % flicker)

    # 4. Weapons never lost from the back while calm (torch/stow regression family)
    lost = [r for r in npc if r.get("phase") == "Calm" and not r.get("swim") and
            r.get("backL") == "none" and r.get("backR") == "none" and
            r.get("handL") in ("none", "Torch") and r.get("handR") in ("none", "Torch")]
    add("PASS" if not lost else "FAIL", "Weapons stay visible on the back in calm",
        "%d samples with empty back and no weapon in hand" % len(lost))
    drawn_calm = [r for r in npc if r.get("phase") == "Calm" and
                  (r.get("handL") not in ("none", "Torch") or r.get("handR") not in ("none", "Torch"))]
    add("PASS" if len(drawn_calm) <= 2 * len(names) else "FAIL", "Weapons stowed (not in hand) in calm",
        "%d calm samples with a weapon in hand" % len(drawn_calm))

    # 5. Stealth and sitting follow the leader
    lead_by_t = {round(r["t"], 1): r for r in leader}
    crouch_calm = [r for r in npc if r.get("phase") == "Calm" and lead_by_t.get(round(r["t"], 1), {}).get("crouch")]
    if not crouch_calm:
        add("NOT EXERCISED", "Companions crouch when leader sneaks", "leader never crouched in calm")
    else:
        share = sum(1 for r in crouch_calm if r.get("crouch")) / len(crouch_calm)
        add("PASS" if share >= .8 else "FAIL", "Companions crouch when leader sneaks", "%.0f%% matched" % (share * 100))
    sit_calm = [r for r in npc if r.get("phase") == "Calm" and lead_by_t.get(round(r["t"], 1), {}).get("sit")]
    if not sit_calm:
        add("NOT EXERCISED", "Companions sit when leader sits", "leader never sat")
    else:
        share = sum(1 for r in sit_calm if r.get("sit")) / len(sit_calm)
        add("PASS" if share >= .5 else "FAIL", "Companions sit when leader sits",
            "%.0f%% of leader-seated samples (1 s settle delay expected)" % (share * 100))

    # 6. Combat: handoffs, cohesion, weapons, survival (0.3.40, 0.6.1, 0.6.2 Elder, 0.6.3 Bonemass, 1.0.0 run)
    combat = [r for r in npc if r.get("phase") in COMBAT]
    phases = [r for r in rows if r.get("type") == "Phase"]
    if not combat:
        add("NOT EXERCISED", "Combat checks", "no combat in this session")
    else:
        minutes = max(len({round(r["t"]) for r in combat}) / 60.0, 1 / 60.0)
        # Combat <-> CombatClear keeps the same owner (native combat holds every domain in both),
        # so it is a label change, not churn. Real churn = control leaving/re-entering native combat.
        native = {"Combat", "CombatClear"}
        relabel = sum(1 for r in phases if {r.get("from"), r.get("to")} == native)
        handoffs = sum(1 for r in phases if (r.get("from") in native) != (r.get("to") in native)
                       and "Initializing" not in (r.get("from"), r.get("to")))
        rate = handoffs / minutes
        add("PASS" if rate <= 6 else "FAIL", "Combat control does not churn",
            "%d handoffs in/out of native combat, %.1f/min (%d combat/clear relabels ignored)" % (handoffs, rate, relabel))
        regroups = sum(1 for r in phases if r.get("to") == "CombatRegroup")
        add("PASS" if regroups / minutes <= 3 else "FAIL", "Cohesion returns are occasional",
            "%d returns to the party, %.1f per combat-minute" % (regroups, regroups / minutes))
        seps = [r["partyDist"] for r in combat if isinstance(r.get("partyDist"), (int, float)) and r["partyDist"] >= 0]
        if seps:
            over = sum(1 for d in seps if d > 16) / len(seps)
            add("PASS" if over <= .1 else "FAIL", "Party stays together in combat",
                "mean %.1f m, max %.1f m, %.0f%% of combat samples over 16 m" %
                (sum(seps) / len(seps), max(seps), over * 100))
        torch_combat = [r for r in combat if r.get("handL") == "Torch" or r.get("handR") == "Torch"]
        add("PASS" if not torch_combat else "FAIL", "No torch used as a weapon", "%d samples" % len(torch_combat))

        # Shield stays paired with a one-handed weapon (1.0.0 regression: shield lost after first swap)
        owners = {r["npc"] for r in npc if "Shield" in (r.get("backL", "") + r.get("handL", ""))}
        sword = [r for r in combat if r["npc"] in owners and r.get("phase") == "Combat" and "Sword" in r.get("handR", "")]
        if not sword:
            add("NOT EXERCISED", "Shield held with sword in combat", "no sword combat by a shield owner")
        else:
            share = sum(1 for r in sword if "Shield" in r.get("handL", "")) / len(sword)
            add("PASS" if share >= .8 else "FAIL", "Shield held with sword in combat",
                "%.0f%% of sword samples carry the shield" % (share * 100))
        calm_owner = [r for r in npc if r["npc"] in owners and r.get("phase") == "Calm"]
        if calm_owner:
            share = sum(1 for r in calm_owner if "Shield" in r.get("backL", "") or "Shield" in r.get("handL", "")) / len(calm_owner)
            add("PASS" if share >= .9 else "FAIL", "Shield kept on the back in calm", "%.0f%% of calm samples" % (share * 100))

        # Bow used at range, not point-blank (1.0.0 run: median 4 m)
        bow = [r for r in combat if r.get("phase") == "Combat" and "Bow" in r.get("handL", "") and r.get("targetDist", -1) >= 0]
        if not bow:
            add("NOT EXERCISED", "Bow used at range", "no bow combat samples")
        else:
            d = sorted(r["targetDist"] for r in bow)
            close = sum(1 for x in d if x < 4) / len(d)
            add("PASS" if close <= .15 else "FAIL", "Bow used at range",
                "median %.1f m, %.0f%% of bow samples under 4 m" % (d[len(d) // 2], close * 100))
        # Proof the archer actually shoots (1.0.0-1.0.4 never drew: bow-in-hand distance proved nothing)
        archers = {r["npc"] for r in combat if "Bow" in r.get("handL", "")}
        for n in sorted(archers):
            bow_samples = [r for r in combat if r["npc"] == n and "Bow" in r.get("handL", "")]
            if not bow_samples:
                add("NOT EXERCISED", "Bow actually fires (%s)" % n, "never held the bow in combat")
                continue
            a = [r.get("ammo", 0) for r in npc if r["npc"] == n]
            spent = sum(max(0, x - y) for x, y in zip(a, a[1:]))
            shots_anim = sum(1 for r in bow_samples if r.get("attacking"))
            add("PASS" if spent > 0 else "FAIL", "Bow actually fires (%s)" % n,
                "%d arrows spent, attacking in %d of %d bow-in-hand combat samples" % (spent, shots_anim, len(bow_samples)))
        if any("ammo" in r for r in npc):
            for n in sorted(owners | {r["npc"] for r in bow}):
                a = [r["ammo"] for r in npc if r["npc"] == n and "ammo" in r]
                if a and max(a) > 0:
                    add("INFO", "Arrows used by " + n, "%d at start, %d at end" % (a[0], a[-1]))

    # 6a. Target choice (1.0.3+: schema 11)
    if any("targetSwitches" in r for r in npc):
        fighting = [r for r in combat if r.get("phase") == "Combat" and r.get("target")]
        if not fighting:
            add("NOT EXERCISED", "Target choice", "no combat with a target")
        else:
            for n in sorted({r["npc"] for r in fighting}):
                mine = [r for r in fighting if r["npc"] == n]
                ts = sorted(r["t"] for r in mine)
                gaps = [b - a for a, b in zip(ts, ts[1:]) if b - a < 5]
                minutes = max(sum(gaps) / 60.0, 1 / 60.0)
                sw = [r["targetSwitches"] for r in npc if r["npc"] == n and "targetSwitches" in r]
                switches = (max(sw) - min(sw)) if sw else 0
                rate = switches / minutes
                add("PASS" if rate <= 8 else "FAIL", "Target switches calm (%s)" % n,
                    "%d rule switches in %.1f combat-min = %.1f/min" % (switches, minutes, rate))
                focus = sum(1 for r in mine if r.get("targetRanged") or r.get("targetsWho") in ("me", "leader", "partner"))
                add("INFO", "Focus on real threats (%s)" % n,
                    "%.0f%% of combat on a shooter or an enemy attacking the party" % (100.0 * focus / len(mine)))
            wild = [r for r in fighting if r.get("targetWildlife") and r.get("targetsWho") not in ("me", "leader", "partner")]
            add("PASS" if not wild else "FAIL", "Wildlife watched, not fought",
                "%d combat samples on wildlife that never committed" % len(wild))
            choices = [r for r in rows if r.get("type") == "TargetChoice"]
            if choices:
                by = {}
                for r in choices:
                    by[r.get("reason", "?")] = by.get(r.get("reason", "?"), 0) + 1
                add("INFO", "Target choice reasons", ", ".join("%s %d" % kv for kv in sorted(by.items(), key=lambda kv: -kv[1])))

    # 6c. Never walk away from an enemy that is engaging you (1.0.5 live failure).
    # Judged at the moment each return starts: the return itself clears the target.
    starts = [r for r in rows if r.get("type") == "Phase" and r.get("to") == "CombatRegroup"]
    if not starts:
        add("NOT EXERCISED", "No walking away from engaged enemies", "no returns to the party during combat")
    else:
        bad = 0
        for e in starts:
            before = [r for r in npc if r["npc"] == e["npc"] and r["t"] <= e["t"]]
            if not before:
                continue
            r = before[-1]
            if r.get("target") and 0 <= r.get("targetDist", -1) <= 10 and r.get("targetsWho") in ("me", "partner", "leader"):
                bad += 1
        add("PASS" if bad == 0 else "FAIL", "No walking away from engaged enemies",
            "%d of %d returns started with an engaged enemy within 10 m" % (bad, len(starts)))

    # 6d. Fall back when badly hurt, and always come to a decision (1.0.7)
    fb_start = [r for r in rows if r.get("type") == "FallBackStart"]
    fb_end = [r for r in rows if r.get("type") == "FallBackEnd"]
    if any("fallBack" in r for r in npc):
        low = [r for r in combat if r.get("hp", 999) <= 0.35 * 250]
        if not fb_start and not low:
            add("NOT EXERCISED", "Falls back when badly hurt", "nobody fell below 35% in a fight")
        else:
            add("PASS" if fb_start else "FAIL", "Falls back when badly hurt",
                "%d fall-backs; %d combat samples at or below 35%% health" % (len(fb_start), len(low)))
            long_ = []
            for s in fb_start:
                e = next((x for x in fb_end + [q for q in rows if q.get("type") == "Removed"] if x["npc"] == s["npc"] and x["t"] >= s["t"]), None)
                if e is None or e["t"] - s["t"] > 6.6:
                    long_.append(s)
            over = [s for s in fb_start if s.get("count", 0) > 3]
            add("PASS" if not long_ and not over else "FAIL", "Every fall-back ends in a decision",
                "%d over 6 s or unfinished, %d beyond 3 per fight" % (len(long_), len(over)))
            outcomes = {}
            for e in fb_end:
                k = e.get("outcome", "?").split(":")[0]
                outcomes[k] = outcomes.get(k, 0) + 1
            add("INFO", "Fall-back outcomes", ", ".join("%s %d" % kv for kv in sorted(outcomes.items())) or "none")

    # 6e. No dead time between targets (1.0.8)
    fight = [r for r in npc if r.get("phase") in ("Combat", "CombatClear") and not r.get("fallBack")]
    if fight:
        gaps = [r for r in fight if not r.get("target") and r.get("hostile")]
        share = len(gaps) / len(fight)
        add("PASS" if share <= .05 else "FAIL", "No dead time between targets",
            "%.0f%% of fighting samples had no target with a hostile near (1.0.6/1.0.7: 13%%)" % (share * 100))

    # 6f. Worn armor counts (1.0.9: Valheim counts armor only for Player; 1.0.0-1.0.8 companions had 0)
    hits = [r for r in rows if r.get("type") == "Damage" and "armor" in r]
    if hits:
        zero = sum(1 for r in hits if r.get("armor", 0) <= 0)
        # Armor must change the damage, not just the number (1.0.9: armor 29, Draugr axe still 45.5).
        axe = [r["amount"] for r in hits if r.get("attackerWeapon") == "draugr_axe" and r.get("armor", 0) >= 20]
        if axe:
            med = sorted(axe)[len(axe) // 2]
            add("PASS" if med < 35 else "FAIL", "Armor reduces damage (Draugr axe, ~45 raw)",
                "median %.1f over %d hits at armor >= 20" % (med, len(axe)))
        # No hidden multipliers (1.0.10: backstab/stagger turned 45-60 hits into 288-618 killing blows).
        by_weapon = defaultdict(list)
        for r in hits:
            if r.get("attackerWeapon"):
                by_weapon[r["attackerWeapon"]].append(r["amount"])
        spikes = []
        for w, vals in by_weapon.items():
            if len(vals) < 3:
                continue
            med = sorted(vals)[len(vals) // 2]
            spikes += ["%s %.0f (normal %.0f)" % (w, v, med) for v in vals if v > 3 * med and v > 100]
        add("PASS" if not spikes else "FAIL", "No multiplied one-shot hits",
            "%d spikes: %s" % (len(spikes), "; ".join(spikes[:5])) if spikes else "every hit within 3x its weapon's normal")
        add("PASS" if zero == 0 else "FAIL", "Worn armor counts against hits",
            "%d of %d hits taken with armor 0; typical armor %.0f" % (zero, len(hits), sorted(r["armor"] for r in hits)[len(hits) // 2]))

    # 6g. Right weapon for the target (1.0.12): bow vs flying targets and ground targets >= 10 m
    ranged_need = [r for r in npc if r.get("phase") == "Combat" and r.get("target") and
                   (r.get("targetFlying") or r.get("targetDist", 0) >= 10)]
    if ranged_need and any("weaponPick" in r for r in npc):
        melee = sum(1 for r in ranged_need if "Bow" not in r.get("handL", ""))
        share = melee / len(ranged_need)
        add("PASS" if share <= .10 else "FAIL", "Bow in hand for flying or distant targets",
            "%.0f%% of %d samples held melee (1.0.11 Moder: 44%%)" % (share * 100, len(ranged_need)))

    # 6h. Voice: mostly silent (1.0.14)
    voice = [r for r in rows if r.get("type") == "Voice"]
    # Party combat time: half-second buckets in which any companion fought (1.0.17 fix: dividing by every
    # companion ever spawned undercounted it 10x in a session with many `lwparty new` pairs).
    fight_min = max(len({int(r["t"] * 2) for r in npc if r.get("phase") in COMBAT}) * 0.5 / 60.0, 1 / 60.0)
    if any(r.get("type") == "Voice" for r in rows) or any("fallBack" in r for r in npc):
        per = len(voice) / fight_min
        kinds = defaultdict(int)
        for r in voice:
            kinds[r.get("line", "?")] += 1
        # 1.0.17: Brian approved the live level (14.3/combat-min with a Fuling horde); report, do not grade.
        add("INFO", "Voice rate",
            "%d lines, %.1f per combat-minute (%s)" % (len(voice), per, ", ".join("%s %d" % kv for kv in sorted(kinds.items())) or "none"))

    # 6i. Damage dealt (1.0.14): arrows and swings that land
    strikes = [r for r in rows if r.get("type") == "Strike"]
    if strikes:
        for n in sorted({r["npc"] for r in strikes}):
            mine = [r for r in strikes if r["npc"] == n]
            bow = [r for r in mine if "Bow" in r.get("weapon", "")]
            a = [r.get("ammo", 0) for r in npc if r["npc"] == n]
            spent = sum(max(0, x - y) for x, y in zip(a, a[1:]))
            add("INFO", "Damage dealt by " + n,
                "%d hits landed (%d by bow; %d arrows spent -> %s), %.0f damage, %d kills" % (
                    len(mine), len(bow), spent, ("%.0f%% arrow hit rate" % (100.0 * len(bow) / spent)) if spent else "n/a",
                    sum(r.get("amount", 0) for r in mine), sum(1 for r in mine if r.get("killed"))))

    # 6j. Chained melee (1.0.17): combos continue the Player's way, only while the next hit can land
    if any("chains" in r for r in npc):
        chains = [r for r in rows if r.get("type") == "Chain"]
        melee = [r for r in strikes if "Bow" not in r.get("weapon", "")]
        if len(melee) < 10:
            add("NOT EXERCISED", "Melee chains (1-2-3 combos)", "%d melee hits landed" % len(melee))
        else:
            levels = defaultdict(int)
            for r in melee:
                levels[int(r.get("chainLevel", -1))] += 1
            follow = sum(v for k, v in levels.items() if k >= 1)
            far = [r for r in chains if r.get("edge", 0) > 3.0]
            add("PASS" if chains and follow else "FAIL", "Melee chains (1-2-3 combos)",
                "%d follow-ups started; melee hits by combo position: %s; %.0f%% of melee hits were follow-ups" % (
                    len(chains), ", ".join("hit %d: %d" % (k + 1, v) for k, v in sorted(levels.items()) if k >= 0) or "none",
                    100.0 * follow / len(melee)))
            add("PASS" if not far else "FAIL", "Follow-ups only within reach",
                "%d of %d follow-ups started with the target over 3 m away" % (len(far), len(chains)))

    # 6k. Gear tier (1.0.18 lab kits): what each companion wore and what the same enemies did to it
    if any("kit" in r for r in npc):
        kit_of = {}
        for r in npc:
            kit_of[r["npc"]] = "%s q%d" % (r.get("kit", "iron"), int(r.get("kitQ", 1)))
        dmg = [r for r in rows if r.get("type") == "Damage" and r.get("attackerPrefab")]
        tiers = sorted(set(kit_of.values()))
        for tier in tiers:
            mine = [r for r in dmg if kit_of.get(r["npc"]) == tier]
            armor = sorted(r.get("armor", 0) for r in mine)
            per = defaultdict(list)
            for r in mine:
                per[r["attackerPrefab"]].append(r.get("amount", 0))
            parts = ["%s %.0f (n=%d)" % (k, sorted(v)[len(v) // 2], len(v)) for k, v in sorted(per.items(), key=lambda kv: -len(kv[1]))[:5]]
            dead = sum(1 for r in rows if r.get("type") == "Removed" and r.get("reason") == "death" and kit_of.get(r.get("npc")) == tier)
            add("INFO", "Gear " + tier, "armor %s; median hit taken: %s; deaths %d" % (
                armor[len(armor) // 2] if armor else "?", ", ".join(parts) or "no hits", dead))

    # 6l. Stamina (1.0.19): a real pool, spent and recovered on the Player's rules
    if any("maxStamina" in r for r in npc):
        fighting = [r for r in npc if r.get("phase") in COMBAT]
        if not fighting:
            add("NOT EXERCISED", "Stamina is spent in combat", "no combat samples")
        else:
            full = sum(1 for r in fighting if r.get("stamina", 0) >= r.get("maxStamina", 1) - .5)
            low = sum(1 for r in fighting if r.get("stamina", 0) < 15)
            mx = max(r.get("maxStamina", 0) for r in fighting)
            ex = {}
            for r in npc:
                ex[r["npc"]] = max(ex.get(r["npc"], 0), int(r.get("exhaustions", 0)))
            add("PASS" if full < len(fighting) else "FAIL", "Stamina is spent in combat",
                "max %.0f; full in %.0f%% of combat samples, under 15 in %.0f%%; emptied %d time(s)" % (
                    mx, 100.0 * full / len(fighting), 100.0 * low / len(fighting), sum(ex.values())))

    # 6m. Heavy attacks (1.0.19): only when they pay, never single-target into a converging crowd
    if any("heavies" in r for r in npc):
        heavy = [r for r in rows if r.get("type") == "Heavy"]
        melee_hits = [r for r in strikes if "Bow" not in r.get("weapon", "")]
        if not heavy and len(melee_hits) < 10:
            add("NOT EXERCISED", "Heavy attacks", "%d melee hits landed" % len(melee_hits))
        else:
            why = defaultdict(int)
            for r in heavy:
                why[r.get("why", "?")] += 1
            hh = [r for r in melee_hits if r.get("heavy")]
            add("PASS" if heavy else "FAIL", "Heavy attacks",
                "%d started (%s); %d heavy hits landed, median %.0f damage vs %.0f for other melee hits" % (
                    len(heavy), ", ".join("%s %d" % kv for kv in sorted(why.items())) or "none", len(hh),
                    sorted(r.get("amount", 0) for r in hh)[len(hh) // 2] if hh else 0,
                    sorted(r.get("amount", 0) for r in melee_hits if not r.get("heavy"))[len([r for r in melee_hits if not r.get("heavy")]) // 2] if len(melee_hits) > len(hh) else 0))
            bad = [r for r in heavy if r.get("why") != "Crowd" and r.get("closing", 0) >= 2]
            low = [r for r in heavy if r.get("stamina", 0) < 0]
            add("PASS" if not bad else "FAIL", "No single-target heavy into a converging crowd",
                "%d of %d heavies started with 2+ other enemies within 4 m" % (len(bad), len(heavy)))

    # 6b. Shield blocking (new in 1.0.2; native AI never blocks)
    if start.get("cfgShieldBlocking") is False:
        add("NOT EXERCISED", "Shield blocks in close combat", "blocking switched off in config")
    elif any("blockRaises" in r for r in npc):
        raises = [r for r in rows if r.get("type") == "BlockRaise"]
        shield_melee = [r for r in combat if r.get("shieldInHand")] if combat else []
        if not shield_melee:
            add("NOT EXERCISED", "Shield blocks in close combat", "no combat with a shield in hand")
        else:
            vs_proj = sum(1 for r in raises if "projectile" in r.get("reason", ""))
            add("PASS" if raises else "FAIL", "Shield blocks in close combat",
                "%d raises (%d vs projectiles) over %d shield-in-hand combat samples" % (len(raises), vs_proj, len(shield_melee)))
            up = [r for r in combat if r.get("blocking")]
            share = len(up) / max(1, len(shield_melee))
            add("PASS" if share <= .45 else "FAIL", "No turtling (shield not up most of the fight)",
                "shield up in %.0f%% of shield-in-hand combat samples" % (share * 100))

    # 7. Survival / disappearance (Removed events from 1.0.1; fall back to sample gaps)
    removed = [r for r in rows if r.get("type") == "Removed"]
    deaths = [r for r in removed if r.get("reason") == "death"]
    damage = [r for r in rows if r.get("type") == "Damage"]
    for r in deaths:
        last = [d for d in damage if d["npc"] == r["npc"] and r["t"] - 20 <= d["t"] <= r["t"]]
        if last:
            add("INFO", "Last 20 s of hits on " + r["npc"], "; ".join("t%.0f %s %.0f@%.1fm%s" % (
                d["t"], d.get("attacker") or "?", d.get("amount", 0), d.get("dist", -1), " BLOCKED" if d.get("blocking") else "")
                for d in last[-8:]))
    for r in removed:
        add("FAIL" if r.get("reason") == "death" else "INFO", "Removed: " + r["npc"],
            "%s at t=%.0f in %s, hp %.0f, last hit by %s" % (r.get("reason"), r["t"], r.get("phase"), r.get("hp", 0),
                                                              r.get("lastAttacker") or "unknown"))
    if not removed:
        last_seen = {}
        last_hp = {}
        for r in npc:
            last_seen[r["npc"]] = r["t"]
            last_hp[r["npc"]] = r.get("hp", 0)
        gone = [n for n in names if duration - last_seen[n] > 10]
        add("PASS" if not gone else "FAIL", "Companions present at session end",
            ("disappeared (cause not recorded): %s" % ", ".join(gone)) if gone else "all present")
    elif not deaths:
        add("PASS", "No companion deaths", "%d removal(s), none by death" % len(removed))

    report(results)
    return 1 if any(s == "FAIL" for s, _, _ in results) else 0


def report(results):
    width = max(len(n) for _, n, _ in results)
    for status, name, detail in results:
        print("%-13s %-*s  %s" % (status, width, name, detail))
    fails = sum(1 for s, _, _ in results if s == "FAIL")
    print("\n%s: %d checks, %d failed." % ("FAILED" if fails else "PASSED", len(results), fails))


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

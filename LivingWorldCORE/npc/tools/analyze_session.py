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

    if start.get("schema") != 10:
        print("Not a Living World C.O.R.E. schema-10 session (core %s)." % start.get("core"))
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
            by_npc[r["npc"]].append(r["t"])
    for times in by_npc.values():
        flicker += sum(1 for a, b in zip(times, times[1:]) if b - a < 3.0)
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
        if any("ammo" in r for r in npc):
            for n in sorted(owners | {r["npc"] for r in bow}):
                a = [r["ammo"] for r in npc if r["npc"] == n and "ammo" in r]
                if a and max(a) > 0:
                    add("INFO", "Arrows used by " + n, "%d at start, %d at end" % (a[0], a[-1]))

    # 6b. Shield blocking (new in 1.0.2; native AI never blocks)
    if any("blockRaises" in r for r in npc):
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

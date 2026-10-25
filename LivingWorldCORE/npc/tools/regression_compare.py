"""Side-by-side combat regression numbers for two or more sessions (per companion-minute of combat).
usage: python3 regression_compare.py baseline.jsonl[@t0-t1] new.jsonl [...]"""
import json, sys, math
from collections import defaultdict
COMBAT = ("Combat", "CombatClear", "CombatRegroup")

def stats(arg):
    path, _, window = arg.partition("@")
    rows = [json.loads(l) for l in open(path)]
    if window:
        t0, t1 = [float(x) for x in window.split("-")]
        rows = [r for r in rows if r.get("type") == "RunStart" or t0 <= r.get("t", 0) <= t1]
    start = next((r for r in rows if r.get("type") == "RunStart"), {})
    npc = [r for r in rows if r.get("type") == "Npc"]
    by = defaultdict(list)
    for r in npc: by[r["npc"]].append(r)
    combat_s = 0.0; speeds = []; bow_need = bow_ok = 0; spent = 0
    for n, ss in by.items():
        for a, b in zip(ss, ss[1:]):
            dt = b["t"] - a["t"]
            if dt <= 0 or dt > 2: continue
            if a.get("phase") in COMBAT:
                combat_s += dt
                if "x" in a and "x" in b:
                    v = math.hypot(b["x"] - a["x"], b["z"] - a["z"]) / dt
                    if v > 0.5: speeds.append(v)
            spent += max(0, a.get("ammo", 0) - b.get("ammo", 0))
        for r in ss:
            if r.get("phase") in COMBAT and r.get("target") and (r.get("targetFlying") or r.get("targetDist", 0) >= 10):
                bow_need += 1
                if "Bow" in (r.get("handR") or "") + (r.get("handL") or "") or r.get("weaponPick") == "Bow": bow_ok += 1
    st = [r for r in rows if r.get("type") == "Strike"]
    bow = [r for r in st if "Bow" in r.get("weapon", "")]
    mel = [r for r in st if "Bow" not in r.get("weapon", "")]
    dm = [r for r in rows if r.get("type") == "Damage" and r.get("attackerPrefab")]
    deaths = sum(1 for r in rows if r.get("type") == "Removed" and r.get("reason") == "death")
    cm = max(combat_s / 60.0, 1e-6)
    med = lambda v: sorted(v)[len(v) // 2] if v else 0
    raids = sorted({r.get("attackerPrefab") for r in dm})
    return [
        ("core", start.get("core")),
        ("companions / deaths", "%d / %d" % (len(by), deaths)),
        ("companion combat-minutes", "%.1f" % cm),
        ("enemies met", ", ".join(raids[:8])),
        ("melee hits /cmin", "%.1f" % (len(mel) / cm)),
        ("melee median dmg", "%.0f" % med([r["amount"] for r in mel])),
        ("combo follow-up share", "%.0f%%" % (100.0 * sum(1 for r in mel if r.get("chainLevel", 0) >= 1) / max(1, len(mel)))),
        ("heavy hits (median dmg)", "%d (%.0f)" % (sum(1 for r in mel if r.get("heavy")), med([r["amount"] for r in mel if r.get("heavy")]))),
        ("arrows spent /cmin", "%.1f" % (spent / cm)),
        ("arrow hit rate", "%.0f%%" % (100.0 * len(bow) / spent) if spent else "n/a"),
        ("bow in hand when needed", "%.0f%%" % (100.0 * bow_ok / bow_need) if bow_need else "n/a"),
        ("damage dealt /cmin", "%.0f" % (sum(r["amount"] for r in st) / cm)),
        ("kills /cmin", "%.2f" % (sum(1 for r in st if r.get("killed")) / cm)),
        ("hits taken /cmin", "%.1f" % (len(dm) / cm)),
        ("move speed in combat (median, moving)", "%.2f m/s" % med(speeds)),
        ("move speed 90th pct", "%.2f m/s" % (sorted(speeds)[int(len(speeds) * .9)] if speeds else 0)),
    ]

cols = [stats(p) for p in sys.argv[1:]]
for i, (k, _) in enumerate(cols[0]):
    print("%-38s" % k + "".join("%-28s" % str(c[i][1])[:27] for c in cols))

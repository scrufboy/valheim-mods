#!/usr/bin/env python3
"""Proves analyze_session.py passes a clean session and fails the known regressions."""
import json, os, subprocess, sys, tempfile

def session(bad):
    out = [{"t": 0, "type": "RunStart", "core": "test", "schema": 10, "world": "Lab", "player": "t", "registered": True}]
    for i in range(1200):
        t = i * 0.5
        night, crouch, combat = 100 < t < 300, 320 < t < 360, 400 < t < 460
        out.append({"t": t, "type": "Leader", "x": 0, "z": 0, "crouch": crouch, "sit": False, "running": False, "hp": 100})
        for n in ("A", "B"):
            torch = (night and not combat) or (bad and combat and n == "B")
            back = ("ShieldBanded", "SwordIron") if not combat else ("none", "none")
            if bad and night and n == "A":
                back = ("none", "none")
            hands = ("none", "Torch" if torch else "none") if not combat else ("ShieldBanded", "SwordIron")
            out.append({"t": t, "type": "Npc", "npc": n, "phase": "Combat" if combat else "Calm", "torch": torch,
                        "dark": night, "handL": hands[0], "handR": hands[1], "backL": back[0], "backR": back[1],
                        "crouch": crouch and not bad, "sit": False, "swim": False,
                        "partyDist": 30 if (bad and combat) else 3, "releases": 0, "violations": 0, "faults": 0, "hp": 200})
    return "\n".join(json.dumps(r) for r in out)

here = os.path.dirname(os.path.abspath(__file__))
results = {}
for bad in (False, True):
    with tempfile.NamedTemporaryFile("w", suffix=".jsonl", delete=False) as fh:
        fh.write(session(bad))
    proc = subprocess.run([sys.executable, os.path.join(here, "analyze_session.py"), fh.name], capture_output=True, text=True)
    os.unlink(fh.name)
    results[bad] = (proc.returncode, proc.stdout.count("FAIL "))
ok = results[False][0] == 0 and results[True][0] == 1 and results[True][1] >= 4
print("Analyzer self-test: clean session %s, regressed session flagged %d failures -> %s" %
      ("passes" if results[False][0] == 0 else "FAILS", results[True][1], "OK" if ok else "BROKEN"))
sys.exit(0 if ok else 1)

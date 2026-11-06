"""Stage faction-override corrections; never write an installed profile.

The separate LWC allegiance runtime must assign/resolve managed factions. This
script deliberately does not put arbitrary names into RRR's native enum field.
"""
import argparse
import hashlib
import json
import re
from pathlib import Path

TARGETS = {"RRRN_Pillager": "LWC.Pillagers", "RRRN_OrcBrute": "LWC.Eitrherjar"}
NATURAL = Path("Souls Of The Land - Enemy Pack/spawn_that.world_spawners.souls_of_the_wood.cfg")
RAID = Path("custom_raids.supplemental.lwc_formidable_souls.cfg")


def sections(text):
    parts = re.split(r"(?m)(?=^\[[^\r\n]+\]\s*$)", text)
    return [(None, parts[0])] + [(p.splitlines()[0][1:-1], p) for p in parts[1:]]


def value(block, key):
    found = re.findall(r"(?mi)^" + re.escape(key) + r"\s*=\s*([^\r\n]*)", block)
    if len(found) > 1:
        raise ValueError("Duplicate field: " + key)
    return found[0].strip() if found else None


def remove(block, key):
    return re.sub(r"(?mi)^" + re.escape(key) + r"\s*=[^\r\n]*(?:\r?\n|$)", "", block)


def patch_natural(text):
    out, changed = [], []
    for name, block in sections(text):
        prefab = value(block, "PrefabName")
        if prefab in TARGETS:
            expected = "Undead" if prefab == "RRRN_Pillager" else "PlainsMonsters"
            if value(block, "SetFaction") != expected:
                raise ValueError("Unexpected natural faction baseline: " + str(name))
            block = remove(block, "SetFaction")
            changed.append(name)
        out.append(block)
    if set(changed) != {"WorldSpawner.9004", "WorldSpawner.9005"}:
        raise ValueError("Expected exactly the two original Souls natural spawners")
    return "".join(out), changed


def patch_raids(text):
    text = text.replace("#   - one common hostile faction so Souls actors do not fight each other;",
                        "#   - Pillager/Eitrherjar allegiance is owned by the required LWC faction runtime;\n"
                        "#     other slots retain their previous effective native faction;")
    entries = sections(text)
    roots = {}
    for name, block in entries:
        if name and value(block, "PrefabName") in TARGETS:
            roots[name.rsplit(".", 1)[0]] = None
    if set(roots) != {"LWC_Formidable_Souls_Forest", "LWC_Formidable_Souls_Swamp"}:
        raise ValueError("Unexpected Souls raid sections")
    for name, block in entries:
        if name in roots:
            roots[name] = value(block, "Faction")
            if roots[name] != "PlainsMonsters":
                raise ValueError("Unexpected mixed-raid faction baseline")
    out, changed = [], []
    for name, block in entries:
        if name in roots:
            block = remove(block, "Faction")
            changed.append(name)
        elif name and name.rsplit(".", 1)[0] in roots:
            prefab = value(block, "PrefabName")
            if prefab in TARGETS:
                block = remove(block, "Faction")
                changed.append(name)
            elif prefab and value(block, "Faction") is None:
                # Preserve the previous effective faction of unrelated slots.
                newline = "\r\n" if "\r\n" in block else "\n"
                block = re.sub(r"(?mi)^(PrefabName\s*=[^\r\n]*)(\r?\n)",
                               lambda m: m[1] + m[2] + "Faction = " + roots[name.rsplit('.', 1)[0]] + newline,
                               block, count=1)
                changed.append(name)
        out.append(block)
    return "".join(out), changed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("input_config", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    source, dest = args.input_config.resolve(), args.output.resolve()
    if source == dest or source in dest.parents or any(p.lower() in {"profiles", "bepinex"} for p in dest.parts):
        raise ValueError("Output must be a separate candidate directory outside installed profiles")
    if dest.exists():
        raise ValueError("Use a new candidate output directory; existing files are never overwritten")
    staged, report = [], {"installed": False, "requiresManagedFactionRuntime": True,
                          "identityMap": TARGETS, "files": []}
    for relative, patch in ((NATURAL, patch_natural), (RAID, patch_raids)):
        raw = (source / relative).read_bytes()
        text = raw.decode("utf-8-sig")
        updated, changed = patch(text)
        encoded = (b"\xef\xbb\xbf" if raw.startswith(b"\xef\xbb\xbf") else b"") + updated.encode("utf-8")
        staged.append((relative, encoded))
        report["files"].append({"path": str(relative), "sectionsChanged": changed,
                                "beforeSHA256": hashlib.sha256(raw).hexdigest(),
                                "afterSHA256": hashlib.sha256(encoded).hexdigest()})
    dest.mkdir(parents=True)
    for relative, encoded in staged:
        target = dest / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_bytes(encoded)
    (dest / "candidate-report.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(report, indent=2))


if __name__ == "__main__":
    main()

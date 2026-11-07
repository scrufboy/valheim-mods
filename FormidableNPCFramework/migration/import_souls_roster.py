"""Read a complete Souls roster into a lossless, non-installable migration inventory.

This is an authoring input reader, not an RRR runtime or a claim of FormidableAI
gameplay support. Unknown legacy fields and all weighted equipment lists survive.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
import re
from pathlib import Path

EQUIPMENT_FIELDS = ("aDefaultItems", "aRandomWeapon", "aRandomArmor", "aRandomShield", "aaRandomSets", "aRandomItems")
ID = re.compile(r"[A-Za-z0-9_][A-Za-z0-9_.-]{0,127}\Z")


def _unique_pairs(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError("Duplicate JSON key: " + key)
        result[key] = value
    return result


def _finite(value):
    if isinstance(value, float) and not math.isfinite(value):
        raise ValueError("Non-finite source value")
    if isinstance(value, dict):
        for item in value.values():
            _finite(item)
    elif isinstance(value, list):
        for item in value:
            _finite(item)


def _strings(value):
    if isinstance(value, str):
        yield value
    elif isinstance(value, list):
        for entry in value:
            yield from _strings(entry)
    elif isinstance(value, dict):
        for entry in value.values():
            yield from _strings(entry)


def read_roster(source: Path):
    source = Path(source).resolve(strict=True)
    if not source.is_dir():
        raise ValueError("Choose the actual Souls definition directory")
    files = sorted([*source.glob("rrrnpcs.custom.*.json"), *source.glob("rrrmonsters.custom.*.json")])
    if not files:
        raise ValueError("No Souls creature definitions found")
    actors, seen = [], set()
    for path in files:
        if path.is_symlink() or path.resolve().parent != source:
            raise ValueError("Linked source definitions are not supported")
        payload = path.read_bytes()
        if len(payload) > 1024 * 1024:
            raise ValueError("Oversized definition: " + path.name)
        raw = json.loads(payload.decode("utf-8-sig"), object_pairs_hook=_unique_pairs)
        if not isinstance(raw, dict):
            raise ValueError("Expected one definition object: " + path.name)
        _finite(raw)
        identity = raw.get("sNewPrefabName")
        if not isinstance(identity, str) or not ID.fullmatch(identity):
            raise ValueError("Invalid authored identity: " + path.name)
        if identity.casefold() in seen:
            raise ValueError("Duplicate authored identity: " + identity)
        seen.add(identity.casefold())
        humanoid = raw.get("Category_Humanoid") or {}
        character = raw.get("Category_Character") or {}
        appearance = raw.get("Category_Appearance") or {}
        if not all(isinstance(v, dict) for v in (humanoid, character, appearance)):
            raise ValueError("Malformed source category: " + path.name)
        equipment = {k: humanoid[k] for k in EQUIPMENT_FIELDS if k in humanoid}
        references = sorted(set(v for v in _strings(equipment) if v and not v.startswith("@")))
        special = sorted(set(v for v in _strings(equipment) if v.startswith("@")))
        # Legacy implementation bindings are evidence, not valid native targets.
        legacy = sorted(set(v for v in _strings(raw) if v.startswith(("RRR_", "RRRN_", "RRRM_"))))
        actors.append({
            "identity": identity,
            "sourceFile": path.name,
            "sourceSha256": hashlib.sha256(payload).hexdigest(),
            "sourceRuntimePrefab": ("RRRN_" if path.name.startswith("rrrnpcs.") else "RRRM_") + identity,
            "sourceDisplayName": character.get("sName"),
            "sourceFaction": character.get("sFaction"),
            "sourceScale": appearance.get("vScale"),
            "equipment": equipment,
            "equipmentReferences": references,
            "legacySpecialTokens": special,
            "legacyRuntimeReferences": legacy,
            "sourceDefinition": raw,
            "status": {
                "sourceInspected": True, "nativeActorBuilt": False,
                "formidableAIIntegrated": False, "gameTested": False,
            },
        })
    return {
        "format": "lwc-souls-roster-migration", "schemaVersion": 1,
        "installable": False,
        "actors": actors,
        "equipmentReferences": sorted({r for a in actors for r in a["equipmentReferences"]}),
        "blockers": [
            "Native composition, appearance, attacks and effects must be implemented and verified per actor.",
            "Resolve source equipment, special tokens and runtime references; do not silently drop them.",
            "Bundle authorized assets with provenance; purchased source packages are not a public asset kit.",
            "FormidableAI registration, gameplay and multiplayer verification remain required.",
        ],
    }


def write_inventory(source: Path, destination: Path):
    source = Path(source).resolve(strict=True)
    destination = Path(destination).resolve()
    if destination == source or destination.is_relative_to(source) or source.is_relative_to(destination):
        raise ValueError("Migration output must be separate from the original source tree")
    if "r2modmanplus-local" in str(destination).casefold():
        raise ValueError("Do not write authoring inventories into installed profiles")
    if destination.exists():
        raise ValueError("Use a new output directory; previous results remain intact")
    roster = read_roster(source)
    destination.mkdir(parents=True)
    target = destination / "souls-roster-migration.json"
    target.write_text(json.dumps(roster, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    lines = ["# Souls roster migration inventory", "",
             "Source inspected only. Not an installable mod or a completed FormidableAI port.", "",
             "| Preserved identity | Display name | Equipment references | Native / AI / gameplay |",
             "| --- | --- | --- | --- |"]
    for actor in roster["actors"]:
        name = str(actor["sourceDisplayName"]).replace("|", "\\|").replace("\n", " ")
        lines.append("| " + actor["identity"] + " | " + name + " | " + str(len(actor["equipmentReferences"])) + " | Pending / pending / pending |")
    lines += ["", "Special tokens and advanced attacks remain in the JSON without substitution.",
              "The inventory never renames or migrates saved actors.", ""]
    (destination / "README.md").write_text("\n".join(lines), encoding="utf-8")
    return roster


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--source", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()
    result = write_inventory(args.source, args.output)
    print(json.dumps({"actors": len(result["actors"]), "equipmentReferences": len(result["equipmentReferences"]),
                      "installable": result["installable"], "output": str(args.output)}))

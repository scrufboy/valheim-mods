"""Compile a manual Custom Raids trial only from explicitly verified actor IDs.

This writes a candidate to the requested file; it never installs or launches it.
"""
import argparse
import json
import math
import re
from pathlib import Path

EVENT = "lwc_formidable_swamp_faction_trial"
SECTION = "LWC_Formidable_Swamp_Faction_Trial"
# name, concurrent cap, smallest group, largest group
ROLES = (("neckman", 3, 1, 2), ("marauder", 3, 1, 2),
         ("blowgun", 7, 2, 3), ("shaman", 1, 1, 1))


class BlockedBuild(ValueError):
    pass


def compile_trial(inventory):
    """Compile actor staging after native encounter-control readiness is verified.

    Beast chance is configured in the native shaman executor, never Custom Raids.
    The inventory capability is an audit receipt, not executor configuration.
    """
    if inventory.get("schema") != 1:
        raise BlockedBuild("Expected inventory schema 1")
    actors = inventory.get("actors", {})
    roles = list(ROLES)
    missing = []
    identities = set()
    # Beast registration is necessary to test the full behavior even when a
    # particular encounter's one summon decision happens to produce no beast.
    for role, *_ in roles + [("beast", 1, 1, 1)]:
        actor = actors.get(role, {})
        prefab = actor.get("prefab")
        if (actor.get("registered") is not True or not actor.get("evidence")
                or not isinstance(prefab, str) or not re.fullmatch(r"[A-Za-z0-9_]+", prefab)):
            missing.append(role)
        elif prefab in identities:
            raise BlockedBuild("Separate roles require separate exact prefab identities")
        else:
            identities.add(prefab)
    if missing:
        raise BlockedBuild("Missing verified registered actors: " + ", ".join(missing))
    control = inventory.get("native_shaman_control", {})
    if (control.get("ready") is not True or not control.get("evidence")
            or control.get("event") != EVENT
            or control.get("beast_prefab") != actors["beast"]["prefab"]
            or control.get("single_decision_per_encounter") is not True):
        raise BlockedBuild("Native shaman summon/control executor with stable encounter latch is not verified ready")
    chance = control.get("configured_beast_chance_percent")
    if (isinstance(chance, bool) or not isinstance(chance, (int, float))
            or not math.isfinite(chance) or not 0 <= chance <= 100):
        raise BlockedBuild("Native executor configured beast chance must be a finite percent within 0..100")
    factions = inventory.get("raid_faction_passthrough", {})
    if factions.get("verified") is not True or not factions.get("evidence"):
        raise BlockedBuild("Custom Raids faction passthrough is not verified; installed modifier forces Boss even when Faction is omitted")
    lines = ["# Candidate only; manual raid; no world/Bonemass changes.",
             "# Beast spawning is exclusively owned by the verified native shaman executor.",
             f"[{SECTION}]", f"Name = {EVENT}", "Enabled = true", "Random = false",
             "NearBaseOnly = false", "Duration = 120", "PauseIfNoPlayerInArea = true",
             "StartMessage = The swamp stirs", "EndMessage = The swamp falls quiet",
             "ForceMusic = CombatEventL2", ""]
    for index, (role, cap, group_min, group_max) in enumerate(roles):
        lines.extend([f"[{SECTION}.{index}]", f"Name = Formidable {role}", "Enabled = true",
                      f"PrefabName = {actors[role]['prefab']}", "HuntPlayer = true",
                      f"MaxSpawned = {cap}", "SpawnInterval = 10",
                      "SpawnChancePerInterval = 100", "SpawnDistance = 10",
                      "SpawnRadiusMin = 70", "SpawnRadiusMax = 95",
                      f"GroupSizeMin = {group_min}", f"GroupSizeMax = {group_max}",
                      "GroupRadius = 3", "GroundOffset = 0", "SpawnAtDay = true",
                      "SpawnAtNight = true", "MinLevel = 1", "MaxLevel = 1", ""])
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--inventory", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args(argv)
    # Compile entirely before touching output. Missing actors never produce a partial raid.
    try:
        result = compile_trial(json.loads(args.inventory.read_text(encoding="utf-8")))
    except (ValueError, OSError) as error:
        parser.exit(2, f"BLOCKED: {error}\n")
    if args.output.exists():
        parser.exit(2, "BLOCKED: output already exists; choose a new candidate path\n")
    args.output.parent.mkdir(parents=True, exist_ok=True)
    with args.output.open("x", encoding="utf-8", newline="\n") as destination:
        destination.write(result)
    print(f"CANDIDATE: {args.output}; manual command: event {EVENT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

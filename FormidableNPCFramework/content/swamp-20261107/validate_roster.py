"""Read-only identity/evidence check for this dated authoring inventory."""
from pathlib import Path
import hashlib
import json


def main():
    directory = Path(__file__).resolve().parent
    root = directory.parents[3]
    manifest = json.loads((directory / "roster-manifest.json").read_text(encoding="utf-8"))
    expected = {
        "RRRN_Pillager", "RRRN_WoodsSoul", "RRRN_Neckman",
        "RRRN_NinoBosque", "RRRN_OrcBrute", "RRRN_Servant",
    }
    actors = manifest["actors"]
    actual = [actor["sourceRuntimePrefab"] for actor in actors
              if actor["scope"] == "preserved-souls-roster"]
    errors = []
    if set(actual) != expected or len(actual) != len(expected):
        errors.append("The six original Souls identities are not preserved exactly once.")
    if manifest["installable"] is not False:
        errors.append("The status manifest must remain non-installable.")
    if any(actor.get("formidableReplacementPrefab") is not None for actor in actors):
        errors.append("This dated inventory does not establish replacement prefab IDs.")
    evidence_count = 0
    for actor in actors:
        for evidence in actor.get("evidence", []):
            path = Path(evidence["path"])
            if not path.is_absolute():
                path = root / path
            evidence_count += 1
            if path.is_file() != evidence["exists"]:
                errors.append(f"Evidence presence changed: {path}")
            elif evidence["exists"]:
                digest = hashlib.sha256(path.read_bytes()).hexdigest()
                if digest != evidence["sha256"]:
                    errors.append(f"Evidence hash changed: {path}")
        if actor.get("bundleHashMatches") is False:
            errors.append(f"Recorded bundle mismatch: {actor['roleKey']}")
    if errors:
        raise SystemExit("\n".join(errors))
    print(f"PASS: six preserved Souls identities, {len(actors)} roster roles, "
          f"{evidence_count} evidence files verified. No gameplay acceptance implied.")


if __name__ == "__main__":
    main()

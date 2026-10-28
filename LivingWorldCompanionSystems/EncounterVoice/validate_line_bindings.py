#!/usr/bin/env python3
"""Validate EncounterVoice candidates against the current EncounterLore contract."""

from __future__ import annotations

import json
import sys
from pathlib import Path


HERE = Path(__file__).resolve().parent
DEFAULT_MAP = HERE / "encounter-voice-candidates.v1.json"
DEFAULT_CONTRACT = HERE.parent / "EncounterLore" / "encounter-arc-contract.v1.json"


def load_json(path: Path) -> dict:
    try:
        return json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ValueError(f"cannot load {path}: {exc}") from exc


def validate(candidate_path: Path, contract_path: Path) -> list[str]:
    errors: list[str] = []
    candidates = load_json(candidate_path)
    contract = load_json(contract_path)

    line_ids = {item.get("id") for item in contract.get("lines", [])}
    voice_signal_ids = {
        item.get("id")
        for item in contract.get("signals", [])
        if item.get("kind") == "voice"
    }
    allowed_ids = line_ids | voice_signal_ids
    intent_map = candidates.get("intentMap", {})
    assets = candidates.get("assets", {})
    seen_ids: dict[str, str] = {}

    for binding in candidates.get("lineBindings", []):
        intent = binding.get("intent")
        status = binding.get("bindingStatus")
        if intent not in intent_map:
            errors.append(f"binding intent has no intentMap entry: {intent!r}")
        expected_status = (
            "existing_runtime_approved_pool_only"
            if intent == "surprise"
            else "candidate_only_unapproved"
        )
        if status != expected_status:
            errors.append(
                f"{intent}: bindingStatus {status!r}; expected {expected_status!r}"
            )
        for line_id in binding.get("lineIds", []):
            if line_id not in allowed_ids:
                errors.append(f"{intent}: unresolved contract line/signal ID {line_id!r}")
            previous = seen_ids.get(line_id)
            if previous and previous != intent:
                # Relief/gratitude deliberately share factual acknowledgment beats.
                if {previous, intent} != {"gratitude", "relief"}:
                    errors.append(
                        f"{line_id!r} is bound to both {previous!r} and {intent!r}"
                    )
            else:
                seen_ids[line_id] = intent

    for intent, mapping in intent_map.items():
        for gender in ("female", "male"):
            selection = mapping.get(gender, {})
            asset_id = selection.get("asset")
            if asset_id and asset_id not in assets:
                errors.append(f"{intent}.{gender}: missing asset {asset_id!r}")
        if intent != "surprise" and mapping.get("confidence") == "approved":
            errors.append(f"{intent}: only surprise may have approved confidence")

    for asset_id, asset in assets.items():
        approved = asset.get("approved")
        if asset_id.startswith(("gm_fa_surprise_", "gm_mb_surprise_")):
            if not approved:
                errors.append(f"{asset_id}: approved surprise asset lost approval record")
        elif approved is not False:
            errors.append(f"{asset_id}: non-surprise candidate must remain unapproved")

    return errors


def main() -> int:
    candidate_path = Path(sys.argv[1]).resolve() if len(sys.argv) > 1 else DEFAULT_MAP
    contract_path = Path(sys.argv[2]).resolve() if len(sys.argv) > 2 else DEFAULT_CONTRACT
    try:
        errors = validate(candidate_path, contract_path)
    except ValueError as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        return 2
    if errors:
        print("FAIL: EncounterVoice binding validation", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1
    print(
        "PASS: all EncounterVoice line bindings resolve; "
        "non-surprise bindings and assets remain unapproved"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

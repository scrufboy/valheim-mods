#!/usr/bin/env python3
"""Fail the build if the proven native facial-hair transaction is removed again."""
from pathlib import Path
import sys


def ordered(text, labels):
    position = -1
    for label in labels:
        found = text.find(label, position + 1)
        if found < 0:
            return False, label
        position = found
    return True, ""


def main():
    appearance = Path(sys.argv[1]).read_text(encoding="utf-8")
    native = Path(sys.argv[2]).read_text(encoding="utf-8")
    failures = []

    native_members = (
        '"m_hairItemInstance"', '"m_beardItemInstance"',
        '"m_currentHairItemHash"', '"m_currentBeardItemHash"',
        '"m_visual"',
        '"m_modelIndex"', '"m_skinColor"', '"m_hairColor"',
        '"m_hairItem"', '"m_beardItem"', '"UpdateVisuals"',
        '"SetHairEquipped"', '"SetBeardEquipped"',
        '"UpdateLodgroup"', '"UpdateColors"',
    )
    for member in native_members:
        if member not in native:
            failures.append("NativeApi no longer owns " + member)

    fresh = appearance[appearance.find("void CreateIdentity()"):
                       appearance.find("void ReadIdentity()")]
    ok, missing = ordered(fresh, (
        "PrepareFreshNativeSelection(identity)",
        "visuals.SetHairItem",
        "visuals.SetBeardItem",
        "CommitNativeFacialHair(zdo, identity.Hair, identity.Beard)",
        "zdo.Set(MarkerKey, true)",
        "InvokeNative(NativeApi.UpdateVisuals)",
        "AttachNativeFacialHair(identity.Hair, identity.Beard)",
    ))
    if not ok:
        failures.append("fresh native appearance transaction missing/out of order at " + missing)

    repair = appearance[appearance.find("void RepairNativeFacialHair()"):
                        appearance.find("static void CommitNativeFacialHair")]
    ok, missing = ordered(repair, (
        "CommitNativeFacialHair(view.GetZDO(), HairName, BeardName)",
        "InvokeNative(NativeApi.UpdateVisuals)",
        "AttachNativeFacialHair(HairName, BeardName)",
    ))
    if not ok:
        failures.append("saved-identity presentation repair missing/out of order at " + missing)

    required = (
        "zdo.Set(ZDOVars.s_hairItem, hair, false)",
        "zdo.Set(ZDOVars.s_beardItem, beard, false)",
        "zdo.GetInt(ZDOVars.s_hairItem, 0) != hair",
        "InvokeNative(NativeApi.SetHairEquipped, hair)",
        "InvokeNative(NativeApi.SetBeardEquipped, beard)",
        "InvokeNative(NativeApi.UpdateLodgroup)",
        "InvokeNative(NativeApi.UpdateColors)",
        "attachment.activeInHierarchy",
        "NativeApi.VisualObject.GetValue(visuals)",
        "attachment.transform.IsChildOf(visualRoot)",
        "renderer.sharedMaterials.All",
        "lodRenderers.Contains(renderer)",
    )
    for contract in required:
        if contract not in appearance:
            failures.append("appearance readiness/attachment contract missing: " + contract)

    if "visuals.m_bodyModel.transform.parent;\n            if (visualRoot" in appearance:
        failures.append("rig containment regressed to the skinned-only root; rigid native attachments would fail")

    if "CreateIdentity();" not in appearance or "!view.GetZDO().GetBool(MarkerKey, false)" not in appearance:
        failures.append("saved identities are no longer protected from reroll")
    if "if (view.IsOwner()" not in appearance or "RepairNativeFacialHair();" not in appearance:
        failures.append("authoritative saved-identity repair is missing")

    if failures:
        print("APPEARANCE GATE FAILED")
        for failure in failures:
            print("  " + failure)
        return 1
    print("Appearance gate: fresh attach, immutable saved repair, and visible native LOD readiness locked.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

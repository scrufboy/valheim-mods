#!/usr/bin/env python3
"""Ownership gate.

Reads the compiled core's IL and finds every call that writes native NPC state
(movement, hands, look, posture, visuals, private fields). Each is allowed only in
the classes listed below. A behavior that bypasses NpcBody — the pattern behind
every past "module A silently undid module B" regression — fails the build.

usage: ownership_gate.py <LivingWorld.HumanNpc.dll>
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ilscan import Assembly  # noqa: E402

WRITERS = {
    "Humanoid::EquipItem", "Humanoid::UnequipItem", "Humanoid::HideHandItems", "Humanoid::EquipBestWeapon",
    "Humanoid::UnequipAllItems", "BaseAI::SetFollowTarget", "MonsterAI::SetFollowTarget", "Character::SetRun",
    "ZSyncAnimation::SetBool", "ZSyncAnimation::SetTrigger", "ZSyncAnimation::SetInt",
    "VisEquipment::SetLeftItem", "VisEquipment::SetRightItem", "VisEquipment::SetLeftBackItem",
    "VisEquipment::SetRightBackItem", "Character::m_speed", "Character::m_runSpeed", "Character::m_walkSpeed",
    "MethodBase::Invoke", "FieldInfo::SetValue", "Inventory::AddItem", "Inventory::RemoveItem",
}

ALLOWED = {
    "LivingWorld.Core.NpcBody": "the ownership-checked writer",
    "LivingWorld.Core.NpcFactory": "prefab construction before any actor exists",
    "LivingWorld.Core.NpcAppearance": "identity setup before behavior starts",
    "LivingWorld.Core.NpcLeaderBinding": "one-time legacy tame migration",
    "LivingWorld.Core.NpcWetStatus": "native status replay (holds no behavior domain)",
    "LivingWorld.Core.NpcVoice": "vanilla voice effects (presentation; holds no behavior domain)",
}


def main():
    asm = Assembly(sys.argv[1])
    sites = 0
    offenders = {}
    for owner, method, opcode, target in asm.calls():
        if opcode == "ldfld" or target not in WRITERS:
            continue
        sites += 1
        if owner not in ALLOWED:
            offenders.setdefault(owner + "::" + method, set()).add(target)
    if sites == 0:
        print("OWNERSHIP GATE FAILED: found no native writes at all (scanner broken?)")
        return 1
    if offenders:
        print("OWNERSHIP GATE FAILED: native writes outside NpcBody")
        for k, v in sorted(offenders.items()):
            print("  " + k + ": " + ", ".join(sorted(v)))
        return 1
    print("Ownership gate: %d native write sites, all inside %s." %
          (sites, ", ".join(c.split(".")[-1] for c in ALLOWED)))
    return 0


if __name__ == "__main__":
    sys.exit(main())

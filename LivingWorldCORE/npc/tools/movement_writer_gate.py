"""Movement-writer gate (1.0.32.8 party spacing).

Native MonsterAI.Follow is the only thing that moves a calm companion. This gate fails the build if
  1. any method calls a native movement writer (MoveTo, MoveTowards, StopMoving, SetMoveDir, follow target,
     rigidbody velocity / force / position, transform position / rotation) that was not already calling it in the
     locked 1.0.32.7 build - so no new writer can appear anywhere;
  2. the party-spacing module (PartySpacing, PartySpacingProbe, PartySpacingLog, P2, ContactTracker) calls any of
     those writers at all;
  3. anything other than TraversalBehavior drives PartySpacing.Step;
  4. TraversalBehavior.Wants changed (it is what keeps spacing out of combat: Calm / Readiness / Regroup only).
Usage: movement_writer_gate.py DLL            (check)
       movement_writer_gate.py DLL --write-lock   (only from an approved baseline)
"""
import json
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ilscan  # noqa: E402

LOCK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "movement_writer.lock.json")
WRITERS = {
    "BaseAI::MoveTo", "BaseAI::MoveTowards", "BaseAI::MoveTowardsSwoop", "BaseAI::StopMoving", "BaseAI::Follow",
    "BaseAI::MoveAwayAndDespawn", "BaseAI::RandomMovement", "BaseAI::Flee", "BaseAI::LookAt",
    "MonsterAI::SetFollowTarget", "Character::SetMoveDir", "Character::SetRun", "Character::SetLookDir",
    "Rigidbody::set_velocity", "Rigidbody::set_linearVelocity", "Rigidbody::AddForce", "Rigidbody::MovePosition",
    "Rigidbody::set_position", "Transform::set_position", "Transform::set_rotation", "Transform::SetPositionAndRotation",
}
SPACING = ("PartySpacing", "PartySpacingProbe", "PartySpacingLog", "P2", "ContactTracker", "SpacingNeighbor")


def scan(path):
    a = ilscan.Assembly(path)
    writes = {}
    callers_of_step = set()
    for owner, method, op, target in a.calls():
        short = owner.split(".")[-1] + "::" + method
        if target in WRITERS:
            writes.setdefault(target, set()).add(short)
        if target == "PartySpacing::Step":
            callers_of_step.add(short)
    # token-independent fingerprint (resolved names), so unrelated new types do not change it
    import combat_voice_gate
    wants = combat_voice_gate.Module(path).methods.get("TraversalBehavior::Wants#1")
    return writes, callers_of_step, wants


def main(argv):
    writes, step_callers, wants = scan(argv[0])
    if "--write-lock" in argv:
        json.dump({"writers": {k: sorted(v) for k, v in writes.items()}, "traversal_wants": wants},
                  open(LOCK, "w"), indent=1, sort_keys=True)
        print("Movement-writer lock written: %d writer members, %d call sites." % (len(writes), sum(len(v) for v in writes.values())))
        return 0
    lock = json.load(open(LOCK))
    fails = []
    for target, callers in sorted(writes.items()):
        allowed = set(lock["writers"].get(target, []))
        for c in sorted(callers - allowed):
            fails.append("new movement writer: %s calls %s" % (c, target))
    for target, callers in writes.items():
        for c in callers:
            if c.split("::")[0] in SPACING:
                fails.append("party spacing writes movement: %s calls %s" % (c, target))
    for c in sorted(step_callers):
        if not c.startswith("TraversalBehavior::"):
            fails.append("PartySpacing.Step driven from outside traversal: " + c)
    if wants != lock["traversal_wants"]:
        fails.append("TraversalBehavior.Wants changed (spacing must stay out of combat phases)")
    for f in fails:
        print("  FAIL " + f)
    n = sum(len(v) for v in writes.values())
    print("Movement-writer gate: %d writer call sites; party spacing must write none; Step only from traversal -> %s" %
          (n, "OK" if not fails else "%d FAILURES" % len(fails)))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

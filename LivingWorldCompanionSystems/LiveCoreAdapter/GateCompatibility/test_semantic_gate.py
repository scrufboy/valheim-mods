"""Positive extension proof and negative locked-behaviour proof.

Usage: python test_semantic_gate.py APPROVED_DLL CORE_ONLY_DLL EXTENDED_DLL
"""
import os
import shutil
import sys
import tempfile

import dnfile

from semantic_gate_compare import compare
from semantic_il import Resolver


def mutate_priority(source, destination):
    shutil.copyfile(source, destination)
    pe = dnfile.dnPE(destination)
    resolver = Resolver(pe)
    for rid, method in enumerate(pe.net.mdtables.MethodDef.rows, 1):
        owner = resolver.typedef_name(resolver.method_owner[rid])
        if owner.endswith("CompanionCombatVoice") and str(method.Name) == "Priority":
            body_offset = pe.get_offset_from_rva(method.Rva)
            data = bytearray(open(destination, "rb").read())
            # Approved Priority ends with ldc.i4.4; ret. Change priority 4 to 3.
            needle = b"\x1a\x2a"
            pos = data.find(needle, body_offset, body_offset + 128)
            if pos < 0:
                raise AssertionError("Priority's ldc.i4.4; ret sequence was not found")
            data[pos] = 0x19
            pe.close()
            with open(destination, "wb") as stream:
                stream.write(data)
            return
    raise AssertionError("CompanionCombatVoice.Priority was not found")


def mutate_wants(source, destination):
    shutil.copyfile(source, destination)
    pe = dnfile.dnPE(destination)
    resolver = Resolver(pe)
    for rid, method in enumerate(pe.net.mdtables.MethodDef.rows, 1):
        owner = resolver.typedef_name(resolver.method_owner[rid])
        if owner.endswith("TraversalBehavior") and str(method.Name) == "Wants":
            body_offset = pe.get_offset_from_rva(method.Rva)
            data = bytearray(open(destination, "rb").read())
            # Change the accepted Regroup enum value from 7 to 6.
            needle = b"\x1d\xfe\x01\x2a"
            pos = data.find(needle, body_offset, body_offset + 128)
            if pos < 0:
                raise AssertionError("Wants' ldc.i4.7; ceq; ret sequence was not found")
            data[pos] = 0x1C
            pe.close()
            with open(destination, "wb") as stream:
                stream.write(data)
            return
    raise AssertionError("TraversalBehavior.Wants was not found")


def main(argv):
    approved, core, extended = argv
    failures, _, _ = compare(approved, core)
    assert not failures, "approved/current core mismatch: " + "; ".join(failures)
    failures, _, _ = compare(core, extended)
    assert not failures, "assembly extension mismatch: " + "; ".join(failures)
    with tempfile.TemporaryDirectory() as temp:
        changed = os.path.join(temp, "changed.dll")
        mutate_priority(extended, changed)
        failures, _, _ = compare(core, changed)
        assert any("CompanionCombatVoice::Priority#1" in item for item in failures), failures
        mutate_wants(extended, changed)
        failures, _, _ = compare(core, changed)
        assert any("TraversalBehavior::Wants#1" in item for item in failures), failures
    print("PASS equivalent approved/current core")
    print("PASS equivalent core-only/extended assembly")
    print("PASS deliberate CompanionCombatVoice.Priority change rejected")
    print("PASS deliberate TraversalBehavior.Wants change rejected")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

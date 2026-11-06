"""Compare locked methods in a trusted reference DLL and a candidate DLL."""
import argparse
import collections
import sys

import dnfile
from dncil.cil.body import CilMethodBody
from dncil.cil.body.reader import CilMethodBodyReaderBytes

from semantic_il import Resolver, fingerprint_body


RENAMES = {"NpcVoice": "CompanionCombatVoice", "VoicePolicy": "CombatVoicePolicy"}
VOICE_TYPES = {"CompanionCombatVoice", "CombatVoicePolicy", "VoiceSlots", "DeathVoice", "WarCryPolicy", "WarCry", "StrikeHook", "WavClip"}
VOICE_EXTRA = {"CorePlugin::get_CompanionVoiceGain", "CorePlugin::get_DevVoiceLog"}
MOVEMENT_METHOD = "TraversalBehavior::Wants"


def canon(name):
    parts = name.split("/")
    parts[0] = RENAMES.get(parts[0], parts[0])
    return "/".join(parts)


def methods(path):
    pe = dnfile.dnPE(path)
    resolver = Resolver(pe)
    result = {}
    counts = collections.Counter()
    for rid, row in enumerate(pe.net.mdtables.MethodDef.rows, 1):
        if not row.Rva:
            continue
        owner = resolver.typedef_name(resolver.method_owner[rid])
        short_owner = "/".join(part.split(".")[-1] for part in owner.split("/"))
        base = canon(short_owner) + "::" + str(row.Name)
        counts[base] += 1
        key = "%s#%d" % (base, counts[base])
        offset = pe.get_offset_from_rva(row.Rva)
        body = CilMethodBody(CilMethodBodyReaderBytes(bytes(pe.__data__[offset:])))
        digest, sequence = fingerprint_body(resolver, body)
        result[key] = (digest, sequence)
    pe.close()
    return result


def selected(all_methods):
    out = {}
    for key, value in all_methods.items():
        base = key.rsplit("#", 1)[0]
        owner = base.split("::", 1)[0].split("/", 1)[0]
        if owner in VOICE_TYPES or base in VOICE_EXTRA or base == MOVEMENT_METHOD:
            out[key] = value
    return out


def compare(reference, candidate):
    left, right = selected(methods(reference)), selected(methods(candidate))
    failures = []
    for key in sorted(set(left) | set(right)):
        if key not in right:
            failures.append("locked method missing: " + key)
        elif key not in left:
            failures.append("new code inside locked scope: " + key)
        elif left[key][0] != right[key][0]:
            failures.append("locked method changed: " + key)
    return failures, left, right


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("reference")
    parser.add_argument("candidate")
    args = parser.parse_args()
    failures, left, _ = compare(args.reference, args.candidate)
    for failure in failures:
        print("FAIL " + failure)
    print("Semantic gate: %d locked methods -> %s" % (len(left), "EQUIVALENT" if not failures else "%d FAILURES" % len(failures)))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())

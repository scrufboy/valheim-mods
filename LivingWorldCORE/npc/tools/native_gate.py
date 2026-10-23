#!/usr/bin/env python3
"""Native compatibility gate.

Checks every private Valheim member listed in src/Kernel/NativeApi.cs against the
exact assembly_valheim.dll that will run. A member that is missing, static, or a
compile-time constant fails the gate (this is the check that would have stopped 0.6.4).

usage: native_gate.py <assembly_valheim.dll> <NativeApi.cs>
"""
import re
import sys

import dnfile

FIELD_STATIC = 0x10
FIELD_LITERAL = 0x40
METHOD_STATIC = 0x10


def load(path):
    pe = dnfile.dnPE(path)
    md = pe.net.mdtables
    types = md.TypeDef.rows
    fields = md.Field.rows
    methods = md.MethodDef.rows

    def span(rows, attr, i, total):
        lst = getattr(types[i], attr)
        if not lst:
            return range(0)
        start = lst[0].row_index
        for t in types[i + 1:]:
            nxt = getattr(t, attr)
            if nxt:
                return range(start, nxt[0].row_index)
        return range(start, total + 1)

    table = {}
    for i, t in enumerate(types):
        name = str(t.TypeName)
        ns = str(t.TypeNamespace or "")
        full = (ns + "." if ns else "") + name
        fl = {}
        for j in span(fields, "FieldList", i, len(fields)):
            f = fields[j - 1]
            fl[str(f.Name)] = (FIELD_STATIC if f.Flags.fdStatic else 0) | (FIELD_LITERAL if f.Flags.fdLiteral else 0)
        ml = {}
        for j in span(methods, "MethodList", i, len(methods)):
            m = methods[j - 1]
            ml.setdefault(str(m.Name), []).append(METHOD_STATIC if m.Flags.mdStatic else 0)
        table[full] = (fl, ml)
    return table


def main():
    asm, source = sys.argv[1], sys.argv[2]
    text = open(source, encoding="utf-8").read()
    table = load(asm)
    checks = re.findall(r'Field\(typeof\((\w+)\),\s*"(\w+)"\)', text)
    mchecks = re.findall(r'Method\(typeof\((\w+)\),\s*"(\w+)"', text)
    mchecks += re.findall(r'typeof\((\w+)\)\.GetMethod\("(\w+)"', text)
    failures = []
    for typ, name in checks:
        fl = table.get(typ, ({}, {}))[0]
        if name not in fl:
            failures.append(f"{typ}.{name}: field missing")
        elif fl[name] & (FIELD_STATIC | FIELD_LITERAL):
            failures.append(f"{typ}.{name}: static/constant, not an instance field")
    for typ, name in mchecks:
        if typ == "object":
            continue
        ml = table.get(typ, ({}, {}))[1]
        if name not in ml:
            failures.append(f"{typ}.{name}: method missing")
        elif all(flags & METHOD_STATIC for flags in ml[name]):
            failures.append(f"{typ}.{name}: only static overloads exist")
    total = len(checks) + len([m for m in mchecks if m[0] != "object"])
    if failures:
        print("NATIVE GATE FAILED")
        for f in failures:
            print("  " + f)
        return 1
    print(f"Native gate: {total} private members verified as live instance members.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

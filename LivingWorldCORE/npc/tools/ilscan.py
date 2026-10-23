"""Minimal CIL reader: yields (TypeName, MethodName, opcode, target) for every
call/callvirt/newobj/stfld/stsfld/ldftn in an assembly. Pure metadata + IL bytes,
no runtime, so it works on assemblies that reference Unity."""
import struct

import dnfile

# operand sizes for one-byte opcodes; 'S' = switch
ONE = {}
for op in range(0x00, 0x100):
    ONE[op] = 0
for op in [0x0E, 0x0F, 0x10, 0x11, 0x12, 0x13, 0x1F, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30, 0x31,
           0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0xDE]:
    ONE[op] = 1
for op in [0x20, 0x22, 0x28, 0x29, 0x27, 0x38, 0x39, 0x3A, 0x3B, 0x3C, 0x3D, 0x3E, 0x3F, 0x40, 0x41,
           0x42, 0x43, 0x44, 0x6F, 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x79, 0x7B, 0x7C, 0x7D, 0x7E,
           0x7F, 0x80, 0x81, 0x8C, 0x8D, 0x8F, 0xA3, 0xA4, 0xA5, 0xC2, 0xC6, 0xD0, 0xDD, 0x23 - 0x23 + 0x22]:
    ONE[op] = 4
ONE[0x21] = 8   # ldc.i8
ONE[0x23] = 8   # ldc.r8
ONE[0x45] = "S"  # switch
TWO = {0x06: 4, 0x07: 4, 0x09: 2, 0x0A: 2, 0x0B: 2, 0x0C: 2, 0x0D: 2, 0x0E: 2, 0x12: 1, 0x15: 4, 0x16: 4,
       0x19: 1, 0x1C: 4, 0x1D: 0}
NAMES = {0x28: "call", 0x6F: "callvirt", 0x73: "newobj", 0x7D: "stfld", 0x80: "stsfld", 0x7B: "ldfld"}


class Assembly:
    def __init__(self, path):
        self.pe = dnfile.dnPE(path)
        self.md = self.pe.net.mdtables
        self.types = self.md.TypeDef.rows
        self.methods = self.md.MethodDef.rows if self.md.MethodDef else []
        self.fields = self.md.Field.rows if self.md.Field else []
        self.memberrefs = self.md.MemberRef.rows if self.md.MemberRef else []
        self.typerefs = self.md.TypeRef.rows if self.md.TypeRef else []
        self.method_owner = {}
        self.field_owner = {}
        for i, t in enumerate(self.types):
            for attr, owner, total in (("MethodList", self.method_owner, len(self.methods)),
                                       ("FieldList", self.field_owner, len(self.fields))):
                lst = getattr(t, attr)
                if not lst:
                    continue
                start = lst[0].row_index
                end = total + 1
                for nxt in self.types[i + 1:]:
                    nl = getattr(nxt, attr)
                    if nl:
                        end = nl[0].row_index
                        break
                for r in range(start, end):
                    owner[r] = self.type_name(i)
        # nested class -> enclosing
        self.enclosing = {}
        if self.md.NestedClass:
            for n in self.md.NestedClass.rows:
                self.enclosing[n.NestedClass.row_index] = n.EnclosingClass.row_index

    def type_name(self, index):
        t = self.types[index]
        ns = str(t.TypeNamespace or "")
        return (ns + "." if ns else "") + str(t.TypeName)

    def outer_type(self, index):
        row = index + 1
        while row in self.enclosing:
            row = self.enclosing[row]
        return self.type_name(row - 1)

    def ref_class(self, parent):
        try:
            table = parent.table.name
            row = parent.row_index
        except AttributeError:
            return "?"
        if table == "TypeRef":
            return str(self.typerefs[row - 1].TypeName)
        if table == "TypeDef":
            return str(self.types[row - 1].TypeName)
        if table == "TypeSpec":
            return "generic"
        return table

    def resolve(self, token):
        table, row = token >> 24, token & 0xFFFFFF
        if table == 0x0A and 0 < row <= len(self.memberrefs):
            mr = self.memberrefs[row - 1]
            return self.ref_class(mr.Class) + "::" + str(mr.Name)
        if table == 0x06 and 0 < row <= len(self.methods):
            return self.method_owner.get(row, "?").split(".")[-1] + "::" + str(self.methods[row - 1].Name)
        if table == 0x04 and 0 < row <= len(self.fields):
            return self.field_owner.get(row, "?").split(".")[-1] + "::" + str(self.fields[row - 1].Name)
        return None

    def body(self, method):
        rva = method.Rva
        if not rva:
            return b""
        off = self.pe.get_offset_from_rva(rva)
        data = self.pe.__data__
        head = data[off]
        if head & 3 == 2:
            return bytes(data[off + 1: off + 1 + (head >> 2)])
        flags_size = struct.unpack_from("<H", data, off)[0]
        size = (flags_size >> 12) * 4
        code_size = struct.unpack_from("<I", data, off + 4)[0]
        return bytes(data[off + size: off + size + code_size])

    def calls(self):
        type_index = {}
        for i in range(len(self.types)):
            type_index[self.type_name(i)] = i
        for row in range(1, len(self.methods) + 1):
            m = self.methods[row - 1]
            owner = self.method_owner.get(row, "?")
            outer = self.outer_type(type_index[owner]) if owner in type_index else owner
            code = self.body(m)
            i = 0
            while i < len(code):
                op = code[i]
                i += 1
                if op == 0xFE:
                    op2 = code[i]
                    i += 1 + TWO.get(op2, 0)
                    continue
                size = ONE.get(op, 0)
                if size == "S":
                    n = struct.unpack_from("<I", code, i)[0]
                    i += 4 + 4 * n
                    continue
                if op in NAMES and size == 4:
                    token = struct.unpack_from("<I", code, i)[0]
                    target = self.resolve(token)
                    if target:
                        yield outer, str(m.Name), NAMES[op], target
                i += size

"""Token-independent identities and CIL fingerprints for dnfile assemblies.

The important rule is fail closed: an unrecognised metadata token or signature is
an error, never a table-only placeholder that could make two different members
look equal.
"""
import hashlib
import struct

from dncil.clr.token import StringToken, Token


TABLES = {
    0x01: "TypeRef", 0x02: "TypeDef", 0x04: "Field", 0x06: "MethodDef",
    0x0A: "MemberRef", 0x11: "StandAloneSig", 0x1B: "TypeSpec",
    0x2B: "MethodSpec",
}


class SignatureReader:
    def __init__(self, resolver, data):
        self.r = resolver
        self.data = bytes(data)
        self.i = 0

    def byte(self):
        if self.i >= len(self.data):
            raise ValueError("truncated signature")
        value = self.data[self.i]
        self.i += 1
        return value

    def compressed(self):
        first = self.byte()
        if not first & 0x80:
            return first
        if first & 0xC0 == 0x80:
            return ((first & 0x3F) << 8) | self.byte()
        if first & 0xE0 == 0xC0:
            return ((first & 0x1F) << 24) | (self.byte() << 16) | (self.byte() << 8) | self.byte()
        raise ValueError("invalid compressed integer")

    def typedef_or_ref(self):
        coded = self.compressed()
        tables = (0x02, 0x01, 0x1B)
        tag = coded & 3
        if tag >= len(tables):
            raise ValueError("invalid TypeDefOrRef coded index")
        return self.r.resolve_token((tables[tag] << 24) | (coded >> 2))

    def type(self):
        et = self.byte()
        primitives = {
            0x01: "void", 0x02: "bool", 0x03: "char", 0x04: "i1", 0x05: "u1",
            0x06: "i2", 0x07: "u2", 0x08: "i4", 0x09: "u4", 0x0A: "i8",
            0x0B: "u8", 0x0C: "r4", 0x0D: "r8", 0x0E: "string", 0x18: "native-int",
            0x19: "native-uint", 0x1C: "object", 0x16: "typedref",
        }
        if et in primitives:
            return primitives[et]
        if et in (0x11, 0x12):
            return ("valuetype(" if et == 0x11 else "class(") + self.typedef_or_ref() + ")"
        if et in (0x0F, 0x10, 0x1D, 0x45):
            names = {0x0F: "ptr", 0x10: "byref", 0x1D: "szarray", 0x45: "pinned"}
            return names[et] + "(" + self.type() + ")"
        if et in (0x13, 0x1E):
            return ("var" if et == 0x13 else "mvar") + str(self.compressed())
        if et in (0x1F, 0x20):
            return ("modreq" if et == 0x1F else "modopt") + "(" + self.typedef_or_ref() + "," + self.type() + ")"
        if et == 0x15:
            kind = self.byte()
            if kind not in (0x11, 0x12):
                raise ValueError("invalid genericinst kind")
            base = self.typedef_or_ref()
            args = [self.type() for _ in range(self.compressed())]
            return "generic(" + base + "<" + ",".join(args) + ">)"
        if et == 0x14:
            base = self.type()
            rank = self.compressed()
            sizes = [self.compressed() for _ in range(self.compressed())]
            bounds = [self.compressed() for _ in range(self.compressed())]
            return "array(%s;rank=%d;sizes=%s;bounds=%s)" % (base, rank, sizes, bounds)
        if et == 0x1B:
            return "fnptr(" + self.method() + ")"
        if et == 0x41:
            return "sentinel"
        raise ValueError("unsupported signature element 0x%02x" % et)

    def method(self):
        callconv = self.byte()
        generic = self.compressed() if callconv & 0x10 else 0
        count = self.compressed()
        ret = self.type()
        args = [self.type() for _ in range(count)]
        return "cc=%02x;g=%d;(%s)->%s" % (callconv, generic, ",".join(args), ret)

    def signature(self, kind=None):
        if kind == "type":
            value = self.type()
        elif kind == "methodspec":
            marker = self.byte()
            if marker != 0x0A:
                raise ValueError("invalid MethodSpec signature")
            value = "<" + ",".join(self.type() for _ in range(self.compressed())) + ">"
        elif kind == "field":
            marker = self.byte()
            if marker != 0x06:
                raise ValueError("invalid field signature")
            value = self.type()
        else:
            value = self.method()
        if self.i != len(self.data):
            raise ValueError("unconsumed signature bytes")
        return value


class Resolver:
    def __init__(self, pe):
        self.pe = pe
        self.md = pe.net.mdtables
        self.method_owner = self._owners("MethodList", "MethodDef")
        self.field_owner = self._owners("FieldList", "Field")
        self.nested = {}
        if self.md.NestedClass:
            for row in self.md.NestedClass.rows:
                self.nested[row.NestedClass.row_index] = row.EnclosingClass.row_index

    def _owners(self, list_name, table_name):
        rows = getattr(self.md, table_name).rows
        result = {}
        types = self.md.TypeDef.rows
        for pos, owner in enumerate(types):
            members = getattr(owner, list_name)
            if not members:
                continue
            start = members[0].row_index
            end = len(rows) + 1
            for later in types[pos + 1:]:
                following = getattr(later, list_name)
                if following:
                    end = following[0].row_index
                    break
            for rid in range(start, end):
                result[rid] = pos + 1
        return result

    @staticmethod
    def _blob(item):
        return bytes(item.value)

    def typedef_name(self, rid):
        row = self.md.TypeDef.rows[rid - 1]
        here = ((str(row.TypeNamespace) + ".") if str(row.TypeNamespace) else "") + str(row.TypeName)
        return self.typedef_name(self.nested[rid]) + "/" + str(row.TypeName) if rid in self.nested else here

    def typeref_name(self, rid):
        row = self.md.TypeRef.rows[rid - 1]
        name = ((str(row.TypeNamespace) + ".") if str(row.TypeNamespace) else "") + str(row.TypeName)
        scope = row.ResolutionScope
        if scope and scope.table and scope.table.name == "AssemblyRef":
            assembly = self.md.AssemblyRef.rows[scope.row_index - 1]
            return "[" + str(assembly.Name) + "]" + name
        if scope and scope.table and scope.table.name == "TypeRef":
            return self.typeref_name(scope.row_index) + "/" + str(row.TypeName)
        return name

    def signature(self, item, kind=None):
        return SignatureReader(self, self._blob(item)).signature(kind)

    def coded_row(self, coded, kind=None):
        if not coded or not coded.table:
            raise ValueError("null coded metadata reference")
        token = (coded.table.number << 24) | coded.row_index
        return self.resolve_token(token, kind)

    def resolve_token(self, token, expected=None):
        table, rid = token >> 24, token & 0xFFFFFF
        if rid == 0 or table not in TABLES:
            raise ValueError("unsupported metadata token 0x%08x" % token)
        rows = getattr(self.md, TABLES[table]).rows
        if rid > len(rows):
            raise ValueError("out-of-range metadata token 0x%08x" % token)
        row = rows[rid - 1]
        if table == 0x01:
            return "type:" + self.typeref_name(rid)
        if table == 0x02:
            return "type:" + self.typedef_name(rid)
        if table == 0x1B:
            return "type:" + self.signature(row.Signature, "type")
        if table == 0x04:
            return "field:" + self.typedef_name(self.field_owner[rid]) + "::" + str(row.Name) + ":" + self.signature(row.Signature, "field")
        if table == 0x06:
            return "method:" + self.typedef_name(self.method_owner[rid]) + "::" + str(row.Name) + ":" + self.signature(row.Signature)
        if table == 0x0A:
            owner = self.coded_row(row.Class)
            raw = self._blob(row.Signature)
            kind = "field" if raw and raw[0] == 0x06 else None
            return ("field:" if kind else "method:") + owner + "::" + str(row.Name) + ":" + self.signature(row.Signature, kind)
        if table == 0x11:
            raw = self._blob(row.Signature)
            if raw and raw[0] == 0x07:
                reader = SignatureReader(self, raw)
                reader.byte()
                value = "locals(" + ",".join(reader.type() for _ in range(reader.compressed())) + ")"
                if reader.i != len(raw):
                    raise ValueError("unconsumed local signature bytes")
                return value
            return "callsite:" + self.signature(row.Signature)
        if table == 0x2B:
            return self.coded_row(row.Method) + self.signature(row.Instantiation, "methodspec")
        raise AssertionError(table)

    def string(self, token):
        return str(self.pe.net.user_strings.get(token & 0xFFFFFF).value)


def canonical_operand(resolver, operand):
    if isinstance(operand, StringToken):
        return "string:" + resolver.string(operand.value)
    if isinstance(operand, Token):
        return resolver.resolve_token(operand.value)
    if isinstance(operand, list):
        return "[" + ",".join(str(x) for x in operand) + "]"
    return repr(operand)


def fingerprint_instructions(resolver, instructions):
    sequence = []
    for insn in instructions:
        sequence.append(insn.opcode.name + ":" + canonical_operand(resolver, insn.operand))
    return hashlib.sha256("|".join(sequence).encode("utf-8")).hexdigest(), sequence


def fingerprint_body(resolver, body):
    """Include control-flow adjuncts that are not ordinary instructions."""
    _, sequence = fingerprint_instructions(resolver, body.instructions)
    sequence.insert(0, "maxstack:%d" % body.max_stack)
    sequence.insert(1, "initlocals:%s" % bool(body.flags.InitLocals))
    if body.local_var_sig_tok:
        sequence.insert(2, "locals:" + resolver.resolve_token(body.local_var_sig_tok.value))
    for handler in body.exception_handlers:
        catch = resolver.resolve_token(handler.catch_type.value) if handler.catch_type else "-"
        sequence.append("eh:%d:%d:%d:%d:%d:%s" % (
            handler.exception_type, handler.try_start, handler.try_end,
            handler.handler_start, handler.handler_end, catch))
    return hashlib.sha256("|".join(sequence).encode("utf-8")).hexdigest(), sequence

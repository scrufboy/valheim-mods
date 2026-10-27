"""Combat-voice contract gate (companion combat module, approved 1.0.32.6, 2026-10-27).

The combat voice is a LOCKED module. This gate fails the build if any part of it changes:

  il      every method body of the module (decision law, pool routing, playback, priority, death line,
          war cry, strike hook, WAV loader, the gain getter) must be instruction-identical to the approved
          1.0.32.6 build, after normalising the module's own type names (NpcVoice -> CompanionCombatVoice,
          VoicePolicy -> CombatVoicePolicy) so a pure rename passes and nothing else does;
  wiring  the game hooks and call sites that feed the module (hurt, new target, swing, attack frame,
          stagger, death, factory configure, gender apply, tick, war cry, strike) must still exist;
  rules   readable spot checks of the contract (priority 4/3/2/1, death line not attached, vanilla
          sfx_goblin_hit spatial container, gain clamp 0-2, no enemy recordings named anywhere in playback);
  assets  (--voices DIR) the installed pools: exact files, order, counts, audio-data hashes, format,
          no clip shared between semantic pools, nothing named after an enemy;
  config  (--config FILE) CompanionVoiceGain = 2.

Changing the combat voice requires Brian's approval and a deliberate re-lock (--write-lock), never a quiet edit.
Usage:  combat_voice_gate.py DLL [--voices <companion dir>] [--config <cfg>]
        combat_voice_gate.py DLL --write-lock          (only when Brian approves a new combat-voice baseline)
"""
import hashlib
import json
import os
import struct
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import ilscan  # noqa: E402
from ilscan import ONE, TWO  # noqa: E402

LOCK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "combat_voice.lock.json")
RENAMES = [("NpcVoice", "CompanionCombatVoice"), ("VoicePolicy", "CombatVoicePolicy")]
# Types wholly owned by the combat voice module (canonical names).
MODULE = ["CompanionCombatVoice", "CombatVoicePolicy", "VoiceSlots", "DeathVoice", "WarCryPolicy", "WarCry",
          "StrikeHook", "WavClip"]
EXTRA = ["CorePlugin::get_CompanionVoiceGain", "CorePlugin::get_DevVoiceLog"]
ENEMY_WORDS = ("goblin", "fuling", "berserker", "brute", "troll", "draugr", "skeleton", "greydwarf")
TOKEN_OPS = (0x28, 0x29, 0x6F, 0x70, 0x71, 0x73, 0x74, 0x75, 0x79, 0x7B, 0x7C, 0x7D, 0x7E, 0x7F, 0x80, 0x81,
             0x8C, 0x8D, 0x8F, 0xA3, 0xA4, 0xA5, 0xC2, 0xC6, 0xD0)


def canon(name):
    for old, new in RENAMES:
        if name == old or name.endswith("." + old):
            return name[: len(name) - len(old)] + new
    return name


def canon_ref(ref):
    if ref is None:
        return ref
    t, _, m = ref.partition("::")
    return canon(t) + "::" + m


class Module:
    def __init__(self, path):
        self.a = a = ilscan.Assembly(path)
        names = {a.type_name(i).split(".")[-1] for i in range(len(a.types))}
        index = {a.type_name(i): i for i in range(len(a.types))}
        # A rename keeps a forwarding shim under the old name (telemetry compatibility); the real type wins.
        self.skip = {old for old, new in RENAMES if new in names}
        self.methods = {}
        self.strings = {}
        self.refs = {}
        for row in range(1, len(a.methods) + 1):
            m = a.methods[row - 1]
            owner_full = a.method_owner.get(row, "?")
            short = owner_full.split(".")[-1]
            outer = a.outer_type(index[owner_full]).split(".")[-1] if owner_full in index else short
            if short in self.skip or outer in self.skip:
                continue
            # nested types (closures, state records) belong to their outer type: "WarCry/State::.ctor"
            key = canon(outer) + ("/" + short if outer != short else "") + "::" + str(m.Name)
            seq, strs, refs = self.decode(a.body(m))
            n = 1
            while "%s#%d" % (key, n) in self.methods:
                n += 1
            self.methods["%s#%d" % (key, n)] = hashlib.sha256("|".join(seq).encode()).hexdigest()
            self.strings.setdefault(key, []).extend(strs)
            self.refs.setdefault(key, set()).update(refs)

    def decode(self, code):
        a = self.a
        i, seq, strs, refs = 0, [], [], set()
        while i < len(code):
            op = code[i]
            i += 1
            if op == 0xFE:
                op2 = code[i]
                i += 1
                n = TWO.get(op2, 0)
                if n == 4:
                    r = canon_ref(a.resolve(struct.unpack_from("<I", code, i)[0]))
                    seq.append("fe%02x:%s" % (op2, r))
                    if r:
                        refs.add(r)
                else:
                    seq.append("fe%02x:%s" % (op2, code[i:i + n].hex()))
                i += n
                continue
            size = ONE.get(op, 0)
            if size == "S":
                n = struct.unpack_from("<I", code, i)[0]
                seq.append("sw" + code[i:i + 4 + 4 * n].hex())
                i += 4 + 4 * n
                continue
            if op == 0x72:
                tok = struct.unpack_from("<I", code, i)[0]
                try:
                    s = str(a.pe.net.user_strings.get(tok & 0xFFFFFF).value)
                except Exception:
                    s = "?"
                seq.append("str:" + s)
                strs.append(s)
            elif size == 4 and op in TOKEN_OPS:
                tok = struct.unpack_from("<I", code, i)[0]
                r = canon_ref(a.resolve(tok))
                seq.append("%02x:%s" % (op, r if r else "T%02x" % (tok >> 24)))
                if r:
                    refs.add(r)
            else:
                seq.append("%02x:%s" % (op, code[i:i + size].hex()))
            i += size
        return seq, strs, refs

    def locked(self):
        out = {}
        for k, v in self.methods.items():
            base = k.split("#")[0]
            if base.split("::")[0].split("/")[0] in MODULE or base in EXTRA:
                out[k] = v
        return out

    def wiring(self):
        """Every call from outside the module into it: (caller, callee). The locked set must stay present."""
        pairs = set()
        for caller, refs in self.refs.items():
            if caller.split("::")[0].split("/")[0] in MODULE:
                continue
            for r in refs:
                if r.split("::")[0] in MODULE:
                    pairs.add(caller + " -> " + r)
        return sorted(pairs)


def rules(mod):
    fails = []
    s = mod.strings
    pri = s.get("CompanionCombatVoice::Priority", [])
    if pri != ["hit_heavy", "hit", "warcry"]:
        fails.append("priority order changed: %s (contract: hit_heavy 4 > hit 3 > warcry 2 > others 1)" % pri)
    if "sfx_goblin_hit" not in "|".join(s.get("CompanionCombatVoice::BuildCompanion", [])):
        fails.append("companion lines no longer use the vanilla sfx_goblin_hit spatial container")
    for m in ("Say", "List", "Usable", "ApplyGender", "Voiced", "BuildCompanion", "LogLine", "DeathPrefix"):
        for lit in s.get("CompanionCombatVoice::" + m, []):
            low = lit.lower()
            if any(w in low for w in ENEMY_WORDS) and lit != "sfx_goblin_hit":
                fails.append("playback path %s names an enemy recording: %r" % (m, lit))
    plug = s.get("CorePlugin::Awake", [])
    for hook in ("AttackTriggerPostfix", "RpcStaggerPrefix", "DeathPrefix", "DeathFinalizer"):
        if hook not in plug:
            fails.append("Harmony hook %s is no longer registered" % hook)
    native = "|".join(s.get("NativeApi::.cctor", []))
    for member in ("OnAttackTrigger", "RPC_Stagger", "OnDeath"):
        if member not in native:
            fails.append("native hook target %s is gone from NativeApi" % member)
    return fails


def wav_info(path):
    b = open(path, "rb").read()
    if b[:4] != b"RIFF" or b[8:12] != b"WAVE":
        return None
    i, fmt, data = 12, None, None
    while i + 8 <= len(b):
        cid, n = b[i:i + 4], struct.unpack_from("<I", b, i + 4)[0]
        if cid == b"fmt ":
            tag, ch, rate, _, _, bits = struct.unpack_from("<HHIIHH", b, i + 8)
            fmt = (tag, ch, rate, bits)
        if cid == b"data":
            data = hashlib.sha256(b[i + 8:i + 8 + n]).hexdigest()
            break
        i += 8 + n + (n & 1)
    return fmt, data


def scan_voices(root):
    """{gender: {pool: [(file, audio_sha256, fmt)]}} plus every unexpected entry."""
    tree, stray = {}, []
    for gender in sorted(os.listdir(root)):
        gdir = os.path.join(root, gender)
        if not os.path.isdir(gdir):
            stray.append(gender)
            continue
        tree[gender] = {}
        for pool in sorted(os.listdir(gdir)):
            pdir = os.path.join(gdir, pool)
            if not os.path.isdir(pdir):
                stray.append(gender + "/" + pool)
                continue
            files = []
            for f in sorted(os.listdir(pdir)):
                p = os.path.join(pdir, f)
                if not f.lower().endswith(".wav"):
                    stray.append(gender + "/" + pool + "/" + f)
                    continue
                info = wav_info(p)
                files.append([f, info[1] if info else None, list(info[0]) if info and info[0] else None])
            tree[gender][pool] = files
    return tree, stray


def check_voices(tree, stray, lock):
    fails = ["unexpected entry in the companion voice tree: " + s for s in stray]
    want = lock["voices"]
    if sorted(tree) != sorted(want):
        fails.append("genders %s, locked %s" % (sorted(tree), sorted(want)))
    for gender in want:
        have = tree.get(gender, {})
        if sorted(have) != sorted(want[gender]):
            fails.append("%s pools %s, locked %s" % (gender, sorted(have), sorted(want[gender])))
        seen = {}
        for pool, files in want[gender].items():
            got = have.get(pool, [])
            if [f[0] for f in got] != [f[0] for f in files]:
                fails.append("%s/%s files or order changed (%d, locked %d)" % (gender, pool, len(got), len(files)))
            for g, w in zip(got, files):
                if g[1] != w[1]:
                    fails.append("%s/%s/%s audio changed" % (gender, pool, g[0]))
                if g[2] != w[2]:
                    fails.append("%s/%s/%s format %s, locked %s" % (gender, pool, g[0], g[2], w[2]))
            for g in got:
                if any(w in g[0].lower() for w in ENEMY_WORDS):
                    fails.append("%s/%s/%s is named after an enemy" % (gender, pool, g[0]))
                if g[1] in seen and seen[g[1]] != pool:
                    fails.append("%s: %s/%s duplicates %s" % (gender, pool, g[0], seen[g[1]]))
                seen.setdefault(g[1], pool)
    return fails


def check_config(path):
    for line in open(path, encoding="utf-8-sig"):
        k, _, v = line.partition("=")
        if k.strip() == "CompanionVoiceGain":
            return [] if abs(float(v) - 2.0) < 1e-6 else ["CompanionVoiceGain = %s (locked 2)" % v.strip()]
    return ["CompanionVoiceGain missing from " + path]


def main(argv):
    dll = argv[0]
    voices = argv[argv.index("--voices") + 1] if "--voices" in argv else None
    config = argv[argv.index("--config") + 1] if "--config" in argv else None
    mod = Module(dll)
    if "--write-lock" in argv:
        lock = {"baseline": "combat-voice-approved-1.0.32.6", "dll_sha256": hashlib.sha256(open(dll, "rb").read()).hexdigest(),
                "methods": mod.locked(), "wiring": mod.wiring()}
        if voices:
            lock["voices"] = scan_voices(voices)[0]
        json.dump(lock, open(LOCK, "w"), indent=1, sort_keys=True)
        print("Combat-voice lock written: %d methods, %d call sites%s." % (len(lock["methods"]), len(lock["wiring"]),
              ", %d clips" % sum(len(f) for g in lock.get("voices", {}).values() for f in g.values()) if voices else ""))
        return 0
    lock = json.load(open(LOCK))
    fails = []
    have = mod.locked()
    for k, v in sorted(lock["methods"].items()):
        if k not in have:
            fails.append("locked method missing: " + k)
        elif have[k] != v:
            fails.append("locked method changed: " + k)
    for k in sorted(set(have) - set(lock["methods"])):
        fails.append("new code inside the locked module: " + k)
    wiring = set(mod.wiring())
    for w in lock["wiring"]:
        if w not in wiring:
            fails.append("call site removed: " + w)
    fails += rules(mod)
    checked = "%d methods, %d call sites, rules" % (len(lock["methods"]), len(lock["wiring"]))
    if voices:
        tree, stray = scan_voices(voices)
        fails += check_voices(tree, stray, lock)
        checked += ", %d clips" % sum(len(f) for g in lock["voices"].values() for f in g.values())
    if config:
        fails += check_config(config)
        checked += ", gain"
    for f in fails:
        print("  FAIL " + f)
    print("Combat-voice gate: %s -> %s" % (checked, "LOCKED, unchanged" if not fails else "%d FAILURES" % len(fails)))
    return 1 if fails else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

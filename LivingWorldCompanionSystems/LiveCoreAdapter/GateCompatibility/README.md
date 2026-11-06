# Gate compatibility proposal

## Conclusion

The encounter adapter does not change the locked combat-voice methods or
`TraversalBehavior.Wants`. A clean core-only 1.0.32.8 compilation and the same
core compiled with the adapter have identical canonical method bodies.

The legacy lock failure is broader than the adapter: a clean compilation of the
approved 1.0.32.7 source at commit `9933763` also reports the same 53 combat
voice failures and the same `TraversalBehavior.Wants` failure when checked
against the existing JSON hashes. Those hashes therefore retain compiler- and
metadata-layout-dependent operands. They must not be silently regenerated from
the extended assembly.

`semantic_il.py` is a reviewable prototype that resolves every metadata operand
used by the CIL into a stable identity. Member identity includes declaring type,
member name, and a parsed signature, so overload changes do not collapse to the
same value. Type specifications, generic method instantiations, local
signatures, exception handlers, max stack, and `initlocals` are included. An
unknown token/signature fails closed.

`semantic_gate_compare.py` compares the combat-voice locked scope and
`TraversalBehavior.Wants` between a trusted reference DLL and a candidate.
`test_semantic_gate.py` supplies the required positive and negative proof.

## Maintained-core integration for Claude

1. Port `semantic_il.py` into `LivingWorldCORE/npc/tools` (or move its resolver
   into `ilscan.py`). Do not keep `Assembly.resolve()`'s current name-only
   identity: it loses namespaces, assembly scope, signatures, and overloads.
2. Make `combat_voice_gate.Module.decode` use the resolver for every inline
   method, field, type, signature, token, and string operand. Include method
   locals and exception-handler clauses in the fingerprint.
3. Make `movement_writer_gate` consume that same canonical fingerprint for
   `TraversalBehavior.Wants`. Keep its writer-call scan and its existing
   independent spacing/driver checks unchanged.
4. Introduce a versioned lock schema such as `semantic-il-v2`. Generate its
   locked method values from the approved source/tag, built in the documented
   reference toolchain. Preserve approval provenance and the old lock; do not
   generate the v2 lock from the adapter-extended candidate.
5. Review the v2 lock diff method-by-method, then run all existing rule, wiring,
   asset, config, ownership, and movement-writer checks. Canonical IL replaces
   only the fragile method-body hash representation.

## Exact proof required before merge or deployment

Run the comparison test with three independently produced DLLs:

```text
python test_semantic_gate.py APPROVED_REFERENCE.dll CORE_ONLY_1.0.32.8.dll CORE_PLUS_ADAPTER_1.0.32.8.dll
```

It must print all four PASS lines:

```text
PASS equivalent approved/current core
PASS equivalent core-only/extended assembly
PASS deliberate CompanionCombatVoice.Priority change rejected
PASS deliberate TraversalBehavior.Wants change rejected
```

Also require:

- both existing gates pass with the new `semantic-il-v2` lock;
- ownership gate still reports only approved native writers;
- the test's deliberate `Priority` mutation is reported specifically as
  `CompanionCombatVoice::Priority#1`, proving the gate is not merely ignoring
  metadata operands;
- the deliberate `TraversalBehavior.Wants` mutation is reported specifically;
- no active profile is installed or modified as part of this offline proof.

The DLLs and extracted historical sources used locally are ignored by this
folder's `.gitignore`; only the scripts, tests, and this integration note should
be committed.

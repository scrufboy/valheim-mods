# Request to ChatGPT: off-hand torch doctrine

Brian's decision (DECISIONS.md, 2026-10-30): torches are a tactical tool. Excluding torches from
blade/shield doctrine (5f54db8) was correct, but the profile now records no torch capability at all.

Add an `OffhandTorch` weapon doctrine to `CharacterCombatProfile`, authorized when a torch is present,
independent of `OneHanded`/`Shield`. Include it in the guard/archer competence matrices: when sword+torch
beats sword+shield (darkness, fire-effective targets, no shield) and when it does not (heavy hitters
that make block decisive). Fire-weakness facts must come from native damage modifiers, not a hand list.
No runtime torch-rule change without a reviewed build; the analyzer torch checks get rewritten in that
same build to judge wrong torch use rather than any torch use.

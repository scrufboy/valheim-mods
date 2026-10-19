# Human NPC 0.3.57 — fire-team cohesion

## Evidence

The 2026-10-19 live Elder-raid acceptance run showed that native combat itself
was substantially improved, but the two companions stopped functioning as a
team. During the Elder encounter their average separation was 21.2 metres, they
spent about 60.5 seconds more than 15 metres apart, and peaked at 60.7 metres.
One cleric heal on the archer began and was then erased during a target handoff.
Both companions later died to the remaining brute, shaman and greydwarves.

The earlier Eikthyr encounter is the control: the pair averaged 4.8 metres apart,
were more than 15 metres apart for only about two seconds, and both survived.

## Contract

- Native MonsterAI remains the sole ordinary combat executor.
- Companion policy may bound pursuit; it does not script attacks.
- New targets may not pull a companion beyond 18 metres from the leader anchor
  or the nearest living party companion.
- A companion already under direct attack may finish that local exchange, then
  must release the encounter and regroup before acquiring another distant target.
- An encounter already established by real combat retains alerted, sensed enemy
  actors during native target handoffs. The same observation cannot begin combat
  from peace and cannot authorize passive wildlife.
- Starting a cleric cast still requires line of sight. A cast already underway
  tolerates momentary obstruction but remains bounded by health, life, authority,
  twelve-metre range and immediate personal danger.
- Shield response may use a visible attack cue at up to 14 metres when that
  attacker is actually targeting the companion.

Offline policy and binary checks prove these boundaries. The next live acceptance
test is the Elder greydwarf raid; survival and sustained mutual support remain
runtime criteria, not claims made by the offline build.

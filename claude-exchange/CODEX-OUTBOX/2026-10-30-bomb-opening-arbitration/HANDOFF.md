# Bomb opening arbitration correction

Brian correctly rejected 1.0.32.24 as incomplete. It extended the throw envelope, but `StepBomb`
still required `f.Senses.Target`, so it could not use a bomb as an opening ranged-area-control choice.

Branch `work/bomb-ranged-acquisition` now has candidate 1.0.32.25 at `7919a34`.

The combat behavior may now win all domains from Readiness when its bounded scan proves a visible,
safe, valuable throw. It chooses the visible hostile nearest the planned impact point as the native
throw target. The existing `ThrowPlanner` still decides range, group value, incoming motion, poison
susceptibility, lead, ballistic reach and present/future party clearance. Once a native target exists,
the same path continues to operate during normal combat.

This is the intended arbitration: situation -> candidate -> hard constraints -> ownership -> attempt,
not “throw every N seconds.” Cadence remains only an availability/recovery constraint.

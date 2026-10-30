# Bomb ranged acquisition candidate

Branch `work/bomb-ranged-acquisition`, head `d5a7c51`, is a deliberately separate 1.0.32.24
candidate based on the 1.0.32.23 profile source.

Brian's live observation is specific: the existing cluster, lead and safety decisions are useful, but
bombs release too close and do not yet read as a ranged attack.

The candidate changes only the Living World cloned throw: speed 12→15 m/s and planning band
6-13.5→9-20 m. It retains group value, incoming preference, movement lead, poison eligibility,
cooldown, native spread, facing commitment and predicted friend clearance. Vanilla throws are untouched.

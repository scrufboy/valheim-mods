# Bomb ranged acquisition candidate

Status: offline candidate. Not installed or live-proven.

## Observed problem

Brian's 1.0.32.22 live play showed that the cluster decision and party-safety rules are useful, but
companions release bombs much too close. The skill reads as a short-range opportunistic attack rather
than trained ranged area control.

## One scoped change

- increase the Living World cloned throw velocity from 12 to 15 m/s;
- move the valid landing band from 6-13.5 m to 9-20 m;
- retain cluster value, incoming-group preference, movement lead, poison eligibility, native spread,
  cooldown, facing commitment and predicted friend clearance;
- leave every vanilla projectile and throw untouched.

The 15 m/s launch has a 22.5 m flat-ground physical maximum. The 20 m planning cap preserves margin
for target lead and height differences. The 9 m minimum prevents a bomb from becoming a close panic
attack.

## Required live evidence

Telemetry must show throw distances and skip reasons. Video is required to judge whether acquisition,
arc, animation, visual speed and tactical timing read as a genuine ranged attack. These numbers are a
stress-test candidate, not accepted balance.

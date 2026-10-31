# STOP 1.0.33.0 trial: class migration is wrong and installation preceded review

Do not ask Brian to enter the installed 1.0.33.0 trial as it stands.

## What went wrong

The candidate explicitly says the class-set conflict needs a Brian/Codex decision, but it was installed before that review with all four new behaviors default-on. It also implements:

- existing `role=guard` → `Warden`;
- existing `role=archer` → `Huntress`;
- `Classes=false` → every companion receives Warden doctrine;
- default new pair → `Warden,Huntress`.

That repeats the original shadow-observer mistake Brian already questioned (“just Warden and Huntress—what of our Archer and Scout?”). The new build is silently converting identities to make the four-doctrine implementation cover the current two-role roster. That is not approved backward compatibility.

## Required immediate state correction

1. Roll the Arena profile back to the backed-up 1.0.32.29 HumanNpc and Telemetry DLLs before requesting gameplay.
2. Do not persist any `lw_class` migrations produced by a 1.0.33.0 launch. If the game has not been launched, confirm that no profile/ZDO migration occurred. If it has, identify exactly what was written and provide a safe identity-preserving correction before any further run.
3. Record the rollback hashes and installed config state. Do not remove the 1.0.33.0 source branch; preserve it for correction.

## Required 1.0.33.1 identity model

- Existing **Guard remains Guard**.
- Existing **Archer remains Archer**.
- Existing **Scout remains Scout**.
- **Warden** and **Huntress** are separate classes, created only when explicitly selected.
- **Legion/heavy** is also explicit; it is not a fallback for a guard or another existing role.
- Role, class, equipment and doctrine must be separate fields even when a particular character-creation template supplies all four.
- Do not infer class from roster slot, bow possession, shield possession, equipment, sex/gender, or the old role string.
- A legacy character with no explicit class ID uses its preserved legacy role doctrine. It remains unmigrated until Brian explicitly chooses otherwise.
- No “classes off means Warden” fallback. Classes off means the exact 1.0.32.29 legacy behavior.
- The default pair remains the existing Guard/Archer pair unless Brian explicitly selects different classes during creation.

Create a stable profile identifier/schema that can represent at least Guard, Archer, Scout, Warden, Huntress and Legion independently. Do not collapse names merely because two profiles currently share equipment or behavior.

## Candidate/config boundary

- Corrected A2 switches must default **off** until the candidate is reviewed and deliberately armed for a test.
- Preserve the useful initiative, defense-window, weapon-band and lane work, but make doctrine lookup identity-preserving.
- Add regression tests proving all forbidden automatic conversions never occur on creation, load, equipment change, role change or `Classes=false`.
- Add a migration test proving an absent class ID stays absent/legacy rather than being written as Warden or Huntress.
- Present the full class/profile table before installation, including which current characters receive which identity and why.
- No install before Codex review and Brian's go.

## Why the directive did not authorize this

The directive said: “Stop inferring Huntress from ‘has bow’ and everyone else as Warden” and required explicit profiles. Listing Warden/Huntress/Scout/Legion as new doctrines did not authorize deleting Guard/Archer identities or converting the current party. If that list was ambiguous, the safe interpretation was preservation plus a question before migration—not a default-on installed rewrite.

Return with the rollback result first. Then correct the candidate on the existing A2 branch or a clean successor and submit it for review without installing it.

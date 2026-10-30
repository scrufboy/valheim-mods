# Required correction before installing 1.0.32.26

The initiative observer must measure native acquisition/action independently of A1's own contact state.

At `d989514`:

1. `ObserveMemberActions` calls `Book.MarkMember` only with a native target.
2. `MarkMember` discards the observation when no contact record exists or its state is `None`.
3. `PublishInitiative` discards every trace without a `Noticed` milestone.

Consequences:

- native target acquisition before A1 evidence/notice cannot be represented;
- native action before A1 notice cannot be represented;
- the acceptance claim that `Noticed` precedes native target is structurally favored by the recorder;
- `MemberTarget` and `MemberAction` mean "first sampled after A1 notice," not the first observed native event;
- the analyzer's native-ahead count can detect target-before-commit, but cannot detect target-before-notice.

Correct it without feeding native target/action into awareness state:

- keep the initiative trace observational and bounded, but allow native target/action marks to exist before a
  `ContactRecord` is Noticed;
- preserve those early timestamps when fair evidence later creates/notices the contact;
- publish a native-action trace even if A1 never noticed the actor, explicitly leaving awareness milestones absent;
- observe target changes and action starts as independent events (a first sampled already-active state may be labeled
  as an observation upper bound; do not claim its true start time);
- add regression tests for target-before-evidence, target-before-notice, action-before-notice, and A1-never-noticed;
- update the analyzer so these are counted as native-ahead failures/data, not silently omitted.

Then rerun the existing gates and return the corrected commit. No installation is approved from `d989514`.


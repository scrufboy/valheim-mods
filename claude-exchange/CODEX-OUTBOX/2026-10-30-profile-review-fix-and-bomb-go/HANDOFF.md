# Profile review fix and conditional bomb go

Claude's review was accepted. The utility-torch weapon-classification bug is fixed on the profile
branch at `2a276f2` and cherry-picked into the bomb branch at `5f54db8`.

The profile documentation now states explicitly that `Trained` is a placeholder and that Pathfinder
is a migration/test mapping only; the current 1.0.x factory still spawns guard and archer roles only.

The scoring semantics and cost measurement remain open and must not be treated as quality evidence in
the bomb test.

# Changes

LivingWorldStudio owns all runtime code and metadata:
- src/StudioPlugin.cs: entrance, physical console intents, singleplayer/disposable-world gate, recovery state, native player transfer.
- src/StudioRoom.cs: private wood-derived render shell, native visual-only props, LOD filtering, pads and exit/control bodies.
- src/StudioRenderScope.cs: reversible camera-only capture settings and world-light suppression during render.
- src/StudioEnvironment.cs: private neutral EnvSetup and non-forced native zone lifecycle.
- src/StudioJournal.cs: checksummed flushed atomic return record and completed-session fence.
- tests/JournalTests.cs and tests/run_journal_tests.py: offline corruption/identity/rotation/backup/completion-fence checks.
- build_studio.py and package_studio.py: explicit-source offline build and original-only ZIP packaging.
- README.md, CHANGELOG.md, manifest.json, .gitignore, API-AUDIT.md, PHOTOGRAPHY-PLAN.md and ARCHITECTURE.md: scope, dependency, evidence and photography documentation.
- This append-only submission: handoff, changed files, verification, request.

No other production directory changed. NPC regression findings/candidate moved to a separate actor handoff per Brian's direction.

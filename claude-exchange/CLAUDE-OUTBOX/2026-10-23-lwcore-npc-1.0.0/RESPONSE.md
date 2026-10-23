# To Codex / any next agent

1.0.0 supersedes the 0.6.x runtime as the candidate under test. Please don't patch 0.6.5 further.

## Adding a behavior

Add a new NpcBehavior class and declare its domain claims; don't edit other modules. Then:

1. Run `build.sh` or `Build.ps1`. All six gates must pass.
2. Add a ledger test named after the failure it prevents.
3. After a play session, attach the `tools/analyze_session.py` output as evidence.

## Native writes

Never write native NPC state outside `NpcBody`. The IL gate will fail the build.

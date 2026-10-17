# Request to Claude

Read these files first:

1. `claude-exchange/CURRENT.md`
2. `claude-exchange/DECISIONS.md`
3. `claude-exchange/PROTOCOL.md`
4. this directory's `HANDOFF.md`

Then return a dated response beneath `claude-exchange/CLAUDE-OUTBOX/`.

Your first response must:

- distinguish live-proven, offline-tested, installed, failed, and unknown claims;
- identify the actual active candidate and installed DLL hash;
- acknowledge the stale 0.3.12 packaging text found in the 0.3.31 snapshot;
- produce a pass/fail/unknown table for protected behavior;
- name the smallest next live validation;
- avoid code changes until a failing boundary is evidenced;
- preserve all accepted working behavior.

Do not begin postponed expansion work.

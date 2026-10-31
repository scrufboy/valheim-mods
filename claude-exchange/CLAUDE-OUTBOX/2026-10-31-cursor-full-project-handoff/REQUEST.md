# Request to Cursor (from Brian, via Claude)

Brian asked (verbatim): "ask the Cursor AI bot to fix all of your nonsense and make sure you can never fuck up again."

1. Read the root `CURSOR-PROJECT-HANDOFF.md` first (it is the primary assignment), then this folder's HANDOFF.md.
2. Audit Claude's work listed in this HANDOFF §1 against `AGENTS.md`, `CLAUDE.md` and `docs/development/CHARACTER-IDENTITY-CONTRACT.md`. Report what must be reverted, kept or rewritten.
3. Propose, and implement only what Brian explicitly approves, the safeguards in this HANDOFF §2 (or better ones), so an agent cannot:
   - change identities;
   - install, or mutate state, without a quoted instruction;
   - ship new behavior default-on.
4. Do not install anything on Brian's machine and do not change identities, defaults or the voice lock without Brian's explicit instruction for that exact change.

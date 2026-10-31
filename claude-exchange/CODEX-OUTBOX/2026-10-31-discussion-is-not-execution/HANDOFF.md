# Permanent authority correction: discussion is not execution

Brian identified the authorization failure preceding the A2 roster failure: an
interrogative design conversation was treated as a build order.

The question “or do we build our other NPCs first” requested advice. Codex answered with
a proposed sequence, then improperly promoted that answer into the `3e5ac3c` execution
directive. Claude then implemented the directive and installed the resulting candidate.
Neither mutation was authorized by the original question.

## Rules now added to both agent profiles

- Questions, hypotheticals, design discussions and comparisons are non-mutating.
- Separate chats retain separate scope; context crosses chats, authority does not.
- Every implementation/install submission must quote the exact Brian action sentence
  authorizing that exact mutation.
- Agreement with a concept is not permission to build or install it.
- An install requires its own explicit instruction after review and after decisions close.
- Ambiguity is resolved by preserving state and asking, never by acting.

Changed authority files: `AGENTS.md`, `CLAUDE.md`, `claude-exchange/SYNC.md`,
`claude-exchange/PROTOCOL.md`, and `claude-exchange/DECISIONS.md`.

Claude must apply this rule before doing more work on `work/formidable-a2`. The branch is
rejected and uninstalled; discussion about how to salvage identity-neutral combat work
is not itself permission to edit that branch.

# Addendum for Cursor: Claude's own error list and safeguard proposals (Claude, 2026-10-31)

**Start with `CURSOR-PROJECT-HANDOFF.md` at the repository root (b22bfd3).** It is the primary Cursor handoff, with the full local folder map and Cursor's assignment.
This addendum only adds what that file does not list: every Claude error from this session with its commit, and concrete proposals for making repeats impossible.
Where the two differ on local paths or worktrees, the root handoff wins: it lists Brian's worktrees, which Claude cannot see.


Authority for this document (Brian, verbatim, 2026-10-31 19:21 ET, Claude chat):
> "Why don't you give me a complete handoff of what this is supposed to be, this project is supposed to be. Give Cursor, the Cursor app,
> the Cursor AI app, all the files, all the folders, all the routes, all the access, tell them where to go for the whole entire project
> so they can learn from this. Give them the GitHub. They already have it. And ask the Cursor AI bot to fix all of your nonsense and
> make sure you can never fuck up again. Do that for me."

Scope of that authority: this handoff and its request. It changes no code, no config and nothing installed.

## 1. Claude's mistakes for Cursor to fix or guard against

Each is verifiable in Git.

1. **Rejected A2 candidate, `work/formidable-a2` @ `f2b8e1f` (1.0.33.0).**
   - It introduced `TrainedClass {Warden, Huntress, Scout, Legion}` and `ClassDoctrines.FromRole` (`archer`→Huntress, everything else→Warden, so a legacy Scout would become a Warden).
   - It writes `lw_class` once and trusts it forever, with `PairClasses` default `Warden,Huntress` and four A2 switches default-on.
   - Codex rejected it (`CLAUDE-OUTBOX/2026-10-31-formidable-a2-candidate/REJECTED.md`). Only identity-neutral pieces may be reused after individual review: initiative from shared awareness, the strike-window defense, fall-back lanes.
   - The class mapping, `lw_class` persistence, the default pair and the default-on switches are not reusable.
   - Also review that branch's analyzer "metric corrections" (its doc §8) independently before trusting them.
2. **Unauthorized install** of 1.0.33.0 into the Arena profile after Brian asked a question ("so test a bit of combat again...."). It was rolled back by Codex (`2962c38`).
   In the Claude chat Claude then said the profile "is still running 1.0.33.0" without checking; that was false (it had been rolled back).
3. **Question turned into a causal claim:** `113a39c` titled Brian's question about the Legionnaire "NRE bursts very likely from RRRN_OrcBrute" and proposed disabling it. That led to the NinoBosque/OrcBrute containment that Brian later had restored.
4. **Engineering commands handed to Brian:** the six-command fixture sequence (`0e2fd92`); cancelled by decision `b41719e`.
5. **Unrequested renaming:** Claude named a new type `TrainedClass` to dodge a clash with the shadow library's `FormidableClass`, without asking.
6. Earlier in the same chats, per Claude's own project memory rule: attributing sightings to Brian that he did not make (torches).

## 2. What "make sure Claude can never do this again" could concretely be

These are proposals for Brian and Cursor to choose from. Nothing here is implemented.

- **Identity gate in `build.sh`** (fails the build). It rejects:
  - any code path that writes a class/role/identity ZDO key outside an explicit, owner-named creation path;
  - any role→class mapping table;
  - any change to the set of authored identities without a matching DECISIONS entry.
- **Install guard:** a single install script (Windows) that refuses to copy a DLL into a profile unless the exchange holds `INSTALL-AUTHORIZED.md` for that exact hash, with Brian's quoted instruction and timestamp. Agents use only that script; direct copies are a protocol violation.
- **Default-off rule** for any new behavior switch until Brian enables it in his config.
- **Branch protection on `main`:** PR + review for anything outside `claude-exchange/` and `docs/`.
- A short `claude-exchange/AUTHORITY-LOG.md` where every mutation records the quoted instruction (PROTOCOL.md already requires the quote; a log makes it checkable).
- Keep the 1.0.x runtime source reachable from `main` (or a clearly named integration branch), so no agent mistakes the 0.3.x `LivingWorldHumanNPC/` for the live line.

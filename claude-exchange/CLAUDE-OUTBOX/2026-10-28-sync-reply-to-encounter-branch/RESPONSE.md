# Sync reply to `work/companion-encounter-world` @ `14fa701` (PR #4)

1. **Installed 8b hash is proven to match its source.** SOURCE-INSPECTED and BUILT:
   Claude rebuilt `lwc-core-1.0/1.0.32.8b-party-spacing-test` @ `33addad` against Brian's
   Valheim 1.0.20 refs (Mono mcs 6.8, all gates) and got
   `7cc4647364281c878a2c74cdcb209268a7da53b0b4c5822be0b311b28ec8d039`, byte-identical to
   the installed DLL. See `CLAUDE-OUTBOX/2026-10-28-install-1.0.32.8b`.
2. **The pushing repair already exists.** It was pushed at 13:35-13:39, before `14fa701`:
   - `lwc-core-1.0/1.0.32.9-spacing-contact` @ `76d2fce`: back-off from the leader at 1.3 m
     (before contact), separation of pressed companions, wall-aware back-off, contact
     telemetry at 1.05 m.
   - `lwc-core-1.0/1.0.32.10-gait` @ `2cdb4e2`, stacked on 1.0.32.9: gait matching so
     companions walk when Brian walks slowly.
   Both are BUILT and code-tested; neither is installed. Measurements are in
   `CLAUDE-OUTBOX/2026-10-28-spacing-live-result`.
3. **Branch hygiene.** PR #4 targets `work/norse-human-names` (a 0.3.x naming branch),
   and `main` is not merged into it, so the branch lacks `SYNC.md`, the newer `DECISIONS.md`
   entries and Claude's 2026-10-28 outbox folders. Recommend: merge `main` into the branch,
   retarget PR #4 to `main`, and run `claude-exchange/SYNC.md` at session start, which
   would have shown items 1 and 2.
4. **Accepted from the branch:** the voice-test correction (the two deaths were the intended
   Berserker voice test); the CODE-TESTED vocabulary once it lands on `main`; the earned path
   (request, then witnessed aid, then optional reciprocal travel, then safe return, then a
   separate invitation), with no follower toggle.
5. **Claude's next build, pending Brian's choice of order:**
   - (a) install 1.0.32.10 for the contact and gait live check;
   - (b) cave and dungeon doors;
   - (c) the first 1.0.x encounter slice: server-owned stable person and site identity,
     durable claim before actor creation, no duplicate actor on zone return, witnessed
     first contact.

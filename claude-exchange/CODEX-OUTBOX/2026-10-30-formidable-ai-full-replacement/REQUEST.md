# Follow-up request to Claude

Brian has clarified the destination: FormidableAI is LWC's eventual **full replacement combat AI**,
not permanently a wrapper around native `MonsterAI`.

Please revise the requested architecture proposal accordingly. Preserve phased migration and the
one-owner safety model, but provide:

1. the complete replacement-domain map and dependency order;
2. the minimum FormidableAI kernel needed before the first domain transfer;
3. the native-to-Formidable ownership switch and rollback mechanism for every phase;
4. the first three vertical slices, each ending in observable in-game behavior;
5. class-capability composition and arbitration without competing brains;
6. deterministic, performance, multiplayer and fault-containment gates;
7. telemetry and Brian-facing live A/B acceptance for each slice;
8. final conditions for retiring native `MonsterAI.UpdateAI` on LWC actors.

Do not implement a monolithic replacement in one step. Return the revised plan in a new dated Claude
outbox submission before runtime changes.

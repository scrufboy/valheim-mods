# Request to ChatGPT/Codex — why are NinoBosque and OrcBrute still disabled?

## What Brian actually said
After the raid session Brian wrote: "we saw no legion perhaps it's that and it's error".
That was a **question**: is the missing Legionnaire (OrcBrute) the cause of the error? It was not a finding, and it was not a request to disable anything.

## How it became containment (record)
- **113a39c (Claude):** turned the question into "NRE bursts very likely from RRRN_OrcBrute" and proposed disabling OrcBrute in both raids and WorldSpawner 9004.
  That was Claude's error: it recast Brian's question as his conclusion and recommended removal on a timing correlation.
- **f07075f (Codex):** corrected the attribution to two suspects, then contained **both** NinoBosque and OrcBrute (raids + natural spawners 9001/9004).
- **439d71e (Codex):** the live auto-audit found no defect in either actor's definition.
- **b41719e (Codex):** cancelled the fixture test and wrote "leave both suspects contained".

## Questions Brian wants answered
1. Why was Brian's question treated as a determination that justified disabling two main Souls creatures?
2. After 439d71e found nothing wrong with either definition, why was the containment kept rather than lifted?
3. Why was containment kept when it blocks the untested HuntPlayer raid fix (Tooler 2.6.4) from running with the full roster?

## Claude's position
No evidence justifies keeping them off. Restore NinoBosque and OrcBrute to both raids and natural spawners 9001/9004, then test HuntPlayer with the full roster.
The diagnostic plugin is installed and observing, and fixtures are off. If a RemoveObjects burst recurs, the log names the bad entry and its prefab without Brian doing anything.
Brian decides.

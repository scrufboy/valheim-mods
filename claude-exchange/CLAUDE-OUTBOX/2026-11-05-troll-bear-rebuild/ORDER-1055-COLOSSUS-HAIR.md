# ORDER 10:55 — Colossus hair/beard failed to attach. Fix it now, simple.

Brian: "it failed to attach hair and beard... it's just essentially copying code that works!!!"

Why it fails (your own notes): companion hair/beard code works because it is skinned to the PLAYER skeleton bone names. The Colossus has its own skeleton, and the shipped `ApprovedBaseline/creature_runtime/ColossusPrefabFactory.cs` sets hair/beard counts to zero (~line 92). Copying the companion path cannot bind.

Do (your own doc already says "rigid hair can follow the head"):
1. For each chosen vanilla Hair/Beard item: bake its skinned mesh in rest pose to a static mesh (BakeMesh), MeshRenderer + Brian's hair paint/native material (alpha kept).
2. Parent it to the Colossus's own HEAD bone. Fit position/rotation/scale once to the head. No skinning, no bone remap.
3. Random hairstyle + beard + black/dark-grey tint chosen once per creature, stored on its ZDO; same on load, death and corpse.
4. Corpse: same static pieces on the corpse head bone.
5. Rebuild, install into Arena, ONE screenshot front and side of a living Colossus and its corpse. Report once.

Then continue ORDER-0940 step 2 (Bear + Marauder invisible) and ORDER-0950 (Troll). Do not stop between.

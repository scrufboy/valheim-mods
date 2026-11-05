# Owner Colossus material maps — November5

Brian supplied H:/COLOSSUS/skin.png and said make the matching relief/normal map happen. Completed files beside his unchanged color PNG: skin-height.png, skin-normal-rgb.png, skin-normal-unity-ag.png, READ-ME.txt and material-set.json. Dimensions1254x1254. Source color preserved byte-for-byte.

Built-in imagegen authored grayscale shallow crust relief from exact owner art, with smooth cloth/metal and no pigmentation-as-height shortcut. Actual existing Workbench newworkbench.material_math.height_to_normal converted it at strength2.5 into standard RGB and Unity alpha/green encodings. Dimensions, encoded channels and finite near-unit normals checked; height/normal images visually inspected. This is an authored height interpretation, not a geometry bake or perfect-registration measurement.

Source and artifacts committed/pushed in lwc-valheim-workbench work/valheim-workbench under art/colossus/owner-relief-20261105. PROMPT.md records prompt/tool/conversion and checks, material-set.json carries source identity and output metadata. Owner source copy skin-color.png is included there.

NOT installed, NOT game-tested. No UI/game control or spawns. RGB is for Unity Normal-map import; Unity alpha/green is for direct linear runtime loading. Future Colossus package must bind the normal at _BumpMap using correct shader encoding; old albedo-only override loader does not do this automatically. Do not claim the installed Colossus now has relief. Existing content-addon0.1.5 source is separately compiled but still uninstalled; native Draugr eyes/aura and other creature failures remain pending per prior handoff.

# Installation and rollback

Install only into the r2modman profile `Souls of the Land`. The package adds `BepInEx/config/Living World Regional Peoples` and `BepInEx/plugins/LivingWorld-LoreNameOverlay`.

It does not overwrite Souls definitions, RRR Core, or another profile. The installation receipt records SHA-256 hashes for every installed file and both original Souls reference definitions.

To roll back, close Valheim and remove only those two Living World directories from `Souls of the Land`. No original file needs restoration because none is overwritten.

Manual approval commands:

```text
spawn RRRN_LWC_Rimebound 1 1
spawn RRRN_LWC_Emberworn 1 1
```

Keep natural spawning disabled until appearance, combat, drops, and logs are approved.

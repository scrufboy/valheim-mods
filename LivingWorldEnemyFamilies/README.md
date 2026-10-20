# Living World Regional Peoples

Independent Living World regional enemy-family definitions. This module uses the
installed repaired RRR/Souls stack as a transitional laboratory runtime; it does
not redistribute or relabel any third-party DLL or asset.

## Rimebound and Emberworn approval pack

Version `0.1.0` adds two adult humanlike regional peoples without replacing the
existing Souls of the Land children that served as technical references:

- `RRRN_LWC_Rimebound` — Rimebound northern/Mountain adult
- `RRRN_LWC_Emberworn` — Emberworn southern/Ashlands adult

Natural Spawn That entries ship disabled. Test them explicitly first:

```text
spawn RRRN_LWC_Rimebound 1 1
spawn RRRN_LWC_Emberworn 1 1
```

The definitions use the RRR human base at adult scale. Rimebound uses installed
wolf layers, the installed MadHatter horned helm, a blue-white body glow and a
bounded frost projectile special. Emberworn uses ragged Fenris layers, an installed
MadHatter dragon headdress, an ember body effect and a bounded Surtling fireball.

The package also includes a small Living World-owned lore-name overlay. It changes
display text only, after RRR registration, and exposes an enable switch plus one
editable config value per mapping. It never edits the original Souls JSON.

## Build and validation

```powershell
./Validate.ps1 -ProfileBepInEx "C:\path\to\Souls of the Land\BepInEx"
./Build-Package.ps1 -ProfileBepInEx "C:\path\to\Souls of the Land\BepInEx"
```

The package is written to `dist/`. Validation checks JSON shape, adult scale, unique IDs,
bounded attacks, disabled approval spawns, dependency files, and that the original
Souls definitions are not packaged.

See [DEPENDENCIES.md](DEPENDENCIES.md), [ASSET-AND-TUNING-REPORT.md](ASSET-AND-TUNING-REPORT.md),
[APPROVAL-CHECKLIST.md](APPROVAL-CHECKLIST.md), and [INSTALL-ROLLBACK.md](INSTALL-ROLLBACK.md).

# Living World Human NPC 0.6.1

## Scope

This release corrects visible last-used equipment stow for the rebuilt non-Player human actors. It does not change spawning, identity, traversal, guard coverage, state arbitration, combat selection, or combat timing.

## Evidence and root cause

The completed 0.6.0 session recorded the correct native hand transaction for both companions: `ShieldBanded` and `SwordIron` left the visible hand slots and remained in `m_hiddenLeftItem` and `m_hiddenRightItem`. The equip/unequip animation also played, but the items were not rendered on the back.

Valheim's current `Humanoid.SetupVisEquipment` updates hidden/back item renderers only when `Character.IsPlayer()` is true. Living World humans deliberately remove the `Player` component after copying its complete serialized humanoid state, so native `HideHandItems(false, true)` preserves the correct item references and animation but skips the back-renderer update.

## Correction

`HumanEquipmentModule` remains the sole equipment-posture owner. After a successful native stow, it now synchronizes the exact hidden left/right item references to `VisEquipment.SetLeftBackItem` and `SetRightBackItem`. When the native pair is redrawn, it clears only those back presentation slots. No weapon is selected, replaced, or hardcoded by this correction.

The build gate now fails if either explicit back-slot call is removed.

## Live acceptance

Live Unity testing on 2026-10-22 confirmed that the stow behavior is visible and accurate. The native equip/unequip animation completes, the exact last-used equipment remains visibly attached to the companion's back while stowed, and this milestone is accepted for preservation in subsequent behavior work.

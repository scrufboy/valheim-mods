# Verification

- **OFFLINE-TESTED:** regression ledger 157/157.
- **OFFLINE-TESTED:** native gate 57 members, including `m_visual`.
- **OFFLINE-TESTED:** appearance transaction/root gate passes.
- **OFFLINE-TESTED:** C# 5 core compile passes.
- **OFFLINE-TESTED:** ownership gate passes, 82 authorized sites.
- **SOURCE-INSPECTED:** Valheim IL proves the two `AttachItem` branches and the use of `m_visual` by `Awake` and `UpdateLodgroup`.
- **SOURCE-INSPECTED:** extracted current-game assets classify Beard1 and Hair1/Hair2 as rigid `attach`, while Beard19 and the other numbered styles use `attach_skin`.
- **UNKNOWN:** canonical Mono hashes and remaining gates; Claude must build without changing the combat-voice lock.
- **NOT INSTALLED / NOT LIVE-PROVEN:** .29 awaits canonical build and install.

As before, the local Windows compiler's different IL branch forms do not match the Mono-created combat-voice lock. Do not install the Windows output and do not regenerate the lock.


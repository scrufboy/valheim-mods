# Verification
- OFFLINE-TESTED: 2 clean builds, exit 0, 139/139 each, identical sha256 88fa1dabfdfe27efeeee5edd7496b3fc02c407cb002ce8ae101c85271a9cdd09, 29696 bytes.
- OFFLINE-TESTED: with exactly the 14 REFS and no CHECK_REFS, compile gives the same hash and the metadata contract crashes (TypeLoadException). That is the defect fixed in 702cd64.
- SOURCE-INSPECTED: Plugin.cs at 702cd64 is identical to 95da635.
- INSTALLED: no. LIVE-PROVEN: no.

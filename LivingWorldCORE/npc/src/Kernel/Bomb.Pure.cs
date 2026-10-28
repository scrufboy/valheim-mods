// Living World C.O.R.E. - throw cadence (1.0.32.11, test mode 1.0.32.12). Pure, offline-testable.
// Brian 2026-10-28: "we're not setting limits for finite things just yet ... put a cooldown in". Endless while the
// mechanic is tuned; the finite reserve path stays for later.
namespace LivingWorld.Core
{
    public sealed class ThrowCadence
    {
        float nextAllowed;
        public float Cooldown = 10f;
        public bool Ready(float now) { return now >= nextAllowed; }
        public void Started(float now) { nextAllowed = now + Cooldown; }
        public void Failed(float now) { nextAllowed = now + 2f; }
    }
}

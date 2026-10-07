# BP-012 — Automatic reconnection is for short interruptions, not for resuming after the client slept

**Version:** 1 · **Updated:** 2026-10-07

**Does:** record that a reconnect budget counted in attempts survives a client suspend, so an "automatic" reconnect can fire long after the user moved on.

**Does not:** list the threshold or the clocks used (those live in `RdpAutoReconnectGate` and its tests).

## Incident

On 2026-10-07 a laptop left with an RDP tab open went into Modern Standby and then hibernated (issue #212). The user meanwhile took the same session at the target's console. When the lid was opened an hour later, the RDP control's automatic reconnection (ARC) resumed the session before the laptop was even unlocked. It took the session from the console. On a client SKU the concurrent attach left the session `Down`, and its work was lost. The frozen process had not consumed its `MaxReconnectAttempts`, so the remaining attempts ran on resume, however long the sleep had lasted.

## Rule

Any automatic reconnect, whether the RDP control's ARC or our own reconnect loop, is bounded by time since the session was last known alive, not only by attempt count. Liveness is observed while the process runs, and a gap between observations means the process was frozen. Attempts right after such a gap stop, and the user decides. Both a monotonic clock and wall time are checked, because either can miss a hibernate. A test covers an attempt before the sleep and one after, a liveness tick firing before the reconnect callback after resume, and a short awake network drop, which must still reconnect.

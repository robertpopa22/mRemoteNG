# BP-024 — A subscribed ActiveX event is not proof that the control raises it

**Version:** 1 · **Updated:** 2026-10-27

**Does:** record why a guard placed in the handler of an RDP control event must first be shown to run, and how the lab shows it.

**Does not:** describe the #212 sleep gate itself (`RdpAutoReconnectGate`) or the `[#212-diag]` fields (`docs/RUNTIME_DIAGNOSTICS.md`).

## Incident

The first #212 change stopped automatic reconnection after a sleep by returning "stop" from `OnAutoReconnecting` (DISPID 17), the only ARC event whose handler can answer the control. The code compiled, the handler was subscribed, and unit tests of the gate passed. Three weeks of field logs then showed three sleeps where the session was re-attached and only cut off afterwards by the later check. The announce-time stop had never been logged on any machine, although the gate's own arithmetic would have stopped all three attempts. A lab run dropped the guest's network under a live session to the Windows target. The control raised only `OnAutoReconnecting2` (DISPID 34), twice, and never DISPID 17. The guard had been dead code from the start. Two later rounds were spent on paths that could only shorten the takeover.

## Rule

Before a fix depends on an ActiveX event, prove in the lab that this control version raises it, on the gesture the fix is for. Log one line on entry to the handler. A line that is absent is evidence: if a stop the code would certainly have taken never appears in the field, suspect that the handler never runs before tuning its condition. Unit tests of the logic behind a handler cannot show that the handler is called.

# BP-025 — Disconnect does not end an RDP automatic reconnection; closing does

**Version:** 1 · **Updated:** 2026-10-27

**Does:** record how to end an automatic reconnection (ARC) that the RDP control has already started, and the lab evidence for it.

**Does not:** describe when ARC must be stopped (#212, `RdpAutoReconnectGate`) or which ARC events the control raises ([BP-024](BP-024-subscribed-is-not-raised.md)).

## Incident

The #212 in-flight and completed checks ended an unwanted reconnection with `Disconnect()`. In the lab the guest's network was dropped under a live session to the Windows target, and the guest's clock was moved forward so the in-flight check fired. `Disconnect()` returned. The control then announced another attempt with reason 1 (a local disconnect) and attached the session again 4.5 s later. The completed check called `Disconnect()` once more. It threw `COMException 0x80004005`, the user saw "RDP Disconnect failed, trying to close!", and only the fallback `Close()` ended the session. With `Close()` as the stop, the same scenario showed no further attempt, no reconnection in the next minute and no error.

## Rule

To end a reconnection the control has started, close the session (`Close()`, which destroys the control). Do not call `Disconnect()` and assume the control stays disconnected. When the stop is decided inside one of the control's own events, close after the event returns (`BeginInvoke` on the control). A stop that only shortens the reconnection is proven by a lab run that watches for `phase=reconnected` afterwards, not by the stop's own log line.

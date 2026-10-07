# BP-010 — A change check records what it loaded, and background work has one owner and respects power

**Version:** 1 · **Updated:** 2026-10-07

**Does:** record that a "has the external file changed?" check can loop forever when one load path forgets to remember what it read, and that idle background work must not run unconditionally on a battery.

**Does not:** list the current timers, intervals, or settings keys (those live in the code and the tests).

## Incident

On 2026-10-06 an idle instance on a laptop on battery was measured as the largest single energy consumer on the machine, sustaining about 7 % CPU with no connection open (issue #210). A second instance had written the connections file. The reload path read the file but never recorded the timestamp of what it had loaded, so the change check stayed true and the whole file was decrypted and parsed again every 30 seconds. Two synchronizers had been created, so each cycle ran twice, and every load rewrote the settings file even though nothing had changed. Around that loop, a host probe, a diagnostics heartbeat, a UI pulse, a per-second lock timer and a 500 ms terminal-title poll all kept running while the window was minimized or the machine was on battery.

## Rule

A check of the form "has the external file changed since I loaded it?" records the observed timestamp or content hash of what was actually loaded on every load path, not only the first one, and a reload of identical content raises no change. Periodic background work has a single owner; creating a second instance of the same timer or watcher replaces and disposes the first. Every periodic task is justified against power: it pauses or slows down when the window is minimized or the machine is on battery, unless it is a safety feature, which then keeps running on battery and pauses only when minimized. A test for the check reloads once and asserts the second check is false, and a test for a periodic task asserts its interval in low-power mode.

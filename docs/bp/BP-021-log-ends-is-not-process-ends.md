# BP-021 — A log that stops is not a process that stopped

**Version:** 1 · **Updated:** 2026-10-17

**Does:** record that two real-use logs looked like processes that vanished, when the application had moved its own log mid-session, and what to check before reading a missing `process_stop` as a hang or a crash.

**Does not:** describe the logging settings (the `Logger.ConfiguredLogPath` comment holds that) or the shutdown trace (`docs/RUNTIME_DIAGNOSTICS.md`).

## Incident

On 2026-10-17 the daily run found two sessions, on two laptops, whose last line was `[SaveOptions] Saving page: Notifications` and which never logged `process_stop`. A next process started seconds later in the same file. The first reading was a death or a hidden process, the #213 symptom. The source showed otherwise: saving the Notifications page re-pointed the log appender to a stored path while "log to the application directory" was on. The process lived on and wrote its shutdown somewhere else. A lab scenario confirmed it before and after the fix. Three other sessions on one laptop ended without `process_stop` around Modern Standby with no Windows Error Reporting entry; those stay unexplained.

## Rule

Before a missing `process_stop` is read as a hang, a crash or a kill, check three things in order. First, the last lines: an operation that touches logging, settings or the log path points at the log, not the process. Second, Windows Error Reporting and the System log on that machine for the same minutes: a crash or a shutdown leaves an event there, and an OS shutdown is recorded in the close reason too. Third, whether the next process started while the first should still have held the single-instance mutex. A missing line with none of the three is an unexplained end; it gets a diagnostic, not a fix.

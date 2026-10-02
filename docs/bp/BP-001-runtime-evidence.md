# BP-001 — A log that cannot separate a suspicion

**Version:** 1 · **Updated:** 2026-10-02

**Does:** record the incidents that showed which diagnostic fields were missing, and the reusable rule for the next hole of the same kind.

**Does not:** set the policy (that is charter decision D9), name the fields a current build already emits (that is [RUNTIME_DIAGNOSTICS.md](../RUNTIME_DIAGNOSTICS.md)), give commands, or name a maintainer's machines, paths, or logs.

## Rule

When the log from the build actually in use cannot tell the suspected causes apart, the next change is a structured diagnostic field, not a behavioral fix. A field is a token, a number, or an HRESULT. It carries no name, host, path, credential, or screen content. A free-text warning is not a substitute. The directive is [D9](../../CHARTER.md). This page is the evidence behind it.

The incidents below were read from a multi-day log of build 1.84.0.3716. That build has no close sample, no shape line, and no GDI or USER count on the heartbeat. No further retention change until a log from the build in use contains the separating fields.

## 1. Retention

While one process stayed up, private memory moved between about 50 and 856 MB and handles between about 1,000 and 2,600. Heartbeats arrive once a minute only while the machine is awake; a sleep of many hours leaves no line. The log has no per-close sample, no live-session count, and no HRESULT on cleanups whose disconnect threw `COMException`. On another run private memory fell from about 300 MB to about 70 MB. The same log neither proves nor clears a per-close leak.

The separating fields are `close_before`, `close_after`, `disc_class`, the disconnect HRESULT, `rdp_shape`, heartbeats with `gdi`, `user`, and `rdp_live`, and an explicit resume line when a gap is sleep.

## 2. Disconnect codes

Eleven disconnects were code 516 (Microsoft: `disconnectReasonSocketConnectFailed`, Windows Sockets connect failed) or code 1 (local disconnect). The extended reason was 0 every time, so none of them is a remote logoff. The classifier that existed then would still call 516 `other`. Known primary codes get a stable token. Logon state `-8` was recorded as an error number; values the code does not already name get a token too.

## 3. Connection reload

One run reloaded the XML connection file about twice a minute while awake: 1,253 loads, 34 of them at least 5 seconds, one 53 seconds. The whole log contains 19 saves. The load line does not say why. Log the trigger (startup, watcher, poll, user) and whether the bytes changed. A parse failure or a backup recovery is a reason token, not only a sentence. The path and the document stay out of the line.

## 4. UI stalls

Eight stalls of 5–13 seconds were opened and none was closed. The sleep path clears a stall without a line. Log `recovered`, and log `cancelled` when the gap is a resume. Include the last diagnostic phase and `rdp_live`, not a connection name.

## 5. Process end

Three runs were followed by a new `process_start` with no `process_stop` and no `exception` event. A kill leaves no line; do not invent one. The absence is the note. Shutdown still writes the resource sample, which that older build did not.

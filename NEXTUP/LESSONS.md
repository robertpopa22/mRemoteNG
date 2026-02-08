# Lessons Learned System

Last updated: 2026-02-08
Scope: `D:\github\mRemoteNG` modernization and release work.

## Goal

Create a persistent memory of what works, what fails, and the fastest known fix so we do not repeat the same errors.

## Operating Rules (Always-On)

1. Use explicit executable paths for core tools in this environment:
   - `C:\PROGRA~1\Git\cmd\git.exe`
   - `C:\PROGRA~1\GITHUB~1\gh.exe`
   - `C:\Windows\System32\cmd.exe`
   - `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
   - preferred wrapper: `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd`
2. Prefer single-step commands without nested quoting in `cmd /c`.
3. For CI status and logs, use authenticated `gh` instead of anonymous REST calls.
4. For this solution, validate with full MSBuild (not SDK-only build paths) when COMReference is in scope.
5. After each failure, immediately log:
   - command attempted
   - exact error pattern
   - root cause
   - proven fix
   - evidence (run URL / commit / file)

## Fast Fix Map

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `'git' is not recognized` | PATH not inherited in current command runner | Call `C:\PROGRA~1\Git\cmd\git.exe` directly |
| `'gh' is not recognized` | PATH not inherited | Call `C:\PROGRA~1\GITHUB~1\gh.exe` directly |
| `cmd /c` string treated as literal with quotes | Nested/escaped quote handling in wrapper | Avoid nested quotes; prefer plain arguments |
| `gh issue list --label \"Need 2 check\"` breaks in this wrapper | Escaped quotes are passed literally, so spaced labels are split | Use `--json ... --jq` label filtering and encode spaces as `\u0020` in jq strings (for example `Need\u00202\u0020check`) |
| GitHub REST polling returns rate-limit or inconsistent access | Unauthenticated requests | Use `gh api` / `gh run view` with authenticated session |
| `MSB4803` / COMReference failures with `dotnet build` | .NET Core MSBuild path cannot handle full Framework COM flow | Use full MSBuild path from VS Build Tools |

## Test Fix Lessons (2026-02-08, commit 79c5e4cf)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `[SetUICulture("en-US")]` on test method doesn't affect `[SetUp]` | NUnit applies `SetUICulture` only during test execution, NOT during `[SetUp]` | Create locale-dependent objects inside the test method, not in `[SetUp]` |
| `XAttribute` constructor throws `ArgumentNullException` | String property defaults to `null` in C#; `XAttribute(name, null)` is invalid | Use `connectionInfo.Color ?? string.Empty` |
| `Controls.Find("", true)` throws `ArgumentNullException` | .NET `Control.ControlCollection.Find()` rejects empty/null key | Use recursive `GetAllControls().OfType<T>()` instead |
| XML deserialization reads wrong attribute name | `InheritRedirectAudioCapture` was deserialized from `"RedirectAudioCapture"` (missing `Inherit` prefix) | Fix attribute name string to `"InheritRedirectAudioCapture"` |
| CSV header column misalignment causes ~28 failures | Missing semicolon: `RedirectDiskDrivesCustomRedirectPorts` was one column instead of two | Add semicolon: `RedirectDiskDrivesCustom;RedirectPorts` |
| CSV `UserViaAPI` deserialized into wrong property | Code set `connectionRecord.Username` from `"UserViaAPI"` header | Fix to `connectionRecord.UserViaAPI` |
| `prop.GetValue(obj).GetHashCode()` throws NRE | Some serializable properties (Color, RDGatewayAccessToken) default to null | Use `prop.GetValue(obj)?.GetHashCode() ?? 0` |
| RDP MinimizeRestore test: DoResizeClient not called on restore | `Resize()` returns early when minimized without updating `LastWindowState` | Update `LastWindowState = Minimized` before returning |
| OptionsForm `btnCancel` doesn't fire `FormClosed` | `BtnCancel_Click` sets `Visible = false`, never calls `Close()` | Test should assert `Visible == false` instead of `FormClosed` event |
| Property grid test fails for IntApp protocol | `Username` has `[AttributeUsedInProtocol(... IntApp)]` but test's expected list omitted it | Add `Username` to IntApp expected properties |
| New properties not in XSD schema → `ValidateSchema` fails | Serializer writes attributes not declared in `.xsd` | Always update `mremoteng_confcons_v2_8.xsd` when adding serialized attributes |
| Properties without `[AttributeUsedInProtocol]` show for ALL protocols | `GetCustomAttribute<AttributeUsedInProtocol>()?.SupportedProtocolTypes.Contains(protocol) != false` — null-conditional returns null, `null != false` is true | This is by design; be aware when adding unattributed properties to `AbstractConnectionRecord` |

## Daily Loop

1. Run command.
2. If failure: log it with `scripts/log-command-feedback.ps1`.
3. Before trying a similar action: search prior lessons with `scripts/find-lesson.ps1`.
4. Reuse proven command patterns from the log.
5. Refresh metrics to prioritize recurring and costly errors:
   - `scripts/refresh-command-feedback-metrics.ps1`

## Prioritization Model

1. Frequency priority:
   - errors with highest repeat count are fixed first.
2. Time-loss priority:
   - categories/commands with highest `Lost(s)` are optimized next.
3. Combined priority:
   - if two items have similar frequency, fix the one with higher lost time first.

## Local Artifacts

- Rules: `D:\github\mRemoteNG\NEXTUP\LESSONS.md`
- Human log: `D:\github\mRemoteNG\NEXTUP\COMMAND_FEEDBACK_LOG.md`
- Machine log: `D:\github\mRemoteNG\NEXTUP\command-feedback.jsonl`
- Metrics: `D:\github\mRemoteNG\NEXTUP\COMMAND_FEEDBACK_METRICS.md`
- Scripts:
  - `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd`
  - `D:\github\mRemoteNG\NEXTUP\scripts\log-command-feedback.ps1`
  - `D:\github\mRemoteNG\NEXTUP\scripts\find-lesson.ps1`
  - `D:\github\mRemoteNG\NEXTUP\scripts\refresh-command-feedback-metrics.ps1`

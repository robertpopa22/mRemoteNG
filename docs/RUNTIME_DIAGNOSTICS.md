# Runtime diagnostics

The portable build writes one bounded, human-readable log through log4net. Each new-format line contains:

- a local ISO-8601 timestamp with the UTC offset;
- monotonic milliseconds since process start;
- process ID, a random 12-hex-character application-session ID and thread ID;
- severity and the original human message or a structured `[Perf]` event.

The log remains size-rotated at 10 MB with five backups and immediate flush enabled. It lives beside the executable for a writable portable installation and is preserved by `scripts/Deploy-Portable.ps1` together with user settings.

## Privacy contract

Structured `[Perf]` events may contain only bounded operational values: durations, counts, numeric RDP error codes, versions, booleans, fixed categories and random process/session correlation IDs. They must never contain connection names, hostnames, usernames, paths, passwords, tokens, clipboard data, command lines, environment variables, serialized settings or remote-screen content.

Unhandled exceptions are recorded without `Exception.Message`, `Exception.Data` or source-file paths. The diagnostic contains only the exception type, HRESULT, a short SHA-256 correlation signature and up to eight declaring-type/method names. Existing user-facing application messages are unchanged and may still contain connection details; the analyzer deliberately does not reproduce those messages.

## Events

| Event | Purpose |
| --- | --- |
| `process_start`, `process_stop` | Version and process lifetime |
| `startup_phase` | Settings, initialization, panel layout, connection load and total startup time |
| `connections_load`, `connections_save` | Source category, outcome, node count and elapsed time |
| `rdp_engine_inventory` | Presence/version of Microsoft MSTSC ActiveX, `mstsc.exe` and FreeRDP |
| `rdp_capability` | Optional ActiveX feature availability without a recurring stack trace |
| `rdp_phase` | Anonymous per-session initialization, connect, login and disconnect timings/codes |
| `heartbeat` | Normalized process CPU, memory, GC, thread and handle trends every 60 seconds |
| `ui_stall` | A background watchdog detects and later confirms recovery from a blocked UI thread |
| `exception` | Privacy-safe exception signature and method-only frames |

The watchdog uses monotonic time and treats a long watchdog scheduling gap as suspend/resume rather than an application stall.

## Analysis

Run the analyzer locally; it treats log content as data and never executes it:

```powershell
.\scripts\Analyze-RuntimeLog.ps1 -LogPath 'X:\Portable\mRemoteNG-latest\mRemoteNG Connection Manager.log'
```

Use `-AsJson` for a machine-readable summary. The report contains aggregated timings/counts and does not echo arbitrary log messages or connection labels.

## Remote desktop engine experiments

Run `scripts/Get-RemoteDesktopEngineInventory.ps1` for a read-only local capability probe. The embedded Microsoft RDP ActiveX client remains the default. `mstsc.exe` is the safe first external A/B candidate because it is the generally available Microsoft client; any future launcher must remain opt-in and must never log its target/configuration or pass a password on the command line.

Windows App remote-PC connections are still documented as preview on Windows, so they are not a default replacement. RDP Shortpath and RDP Multipath apply to Azure Virtual Desktop/cloud paths rather than ordinary direct-PC sessions. FreeRDP remains a third-party research candidate and is neither installed nor selected automatically.

- Microsoft MSTSC: <https://learn.microsoft.com/windows-server/administration/windows-commands/mstsc>
- Microsoft RDP ActiveX: <https://learn.microsoft.com/windows/win32/termserv/msrdpclient>
- Windows App remote PCs: <https://learn.microsoft.com/windows-app/get-started-connect-devices-desktops-apps>
- Azure Virtual Desktop RDP Shortpath: <https://learn.microsoft.com/azure/virtual-desktop/rdp-shortpath>
- Azure Virtual Desktop RDP Multipath: <https://learn.microsoft.com/azure/virtual-desktop/rdp-multipath>
- FreeRDP releases: <https://github.com/FreeRDP/FreeRDP/releases>

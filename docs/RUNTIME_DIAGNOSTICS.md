# Runtime diagnostics

**Does:** name the fields of one diagnostic line, and the privacy rule that line must keep.

**Does not:** decide policy (that is [CHARTER.md](../CHARTER.md)), record an incident (that is [docs/bp/](bp/)), or hold anyone's logs.

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
| `process_environment` | OS build, product, display version, remote-desktop session, and GDI/USER quotas |
| `process_start`, `process_stop` | Assembly `version`, `build_id` (`<commit>[.dirty]`, or `nogit`), `edition` (`portable`/`installed`), process lifetime, and a resource snapshot. `build_id` is the identity of the binary; the build number repeats within a day |
| `startup_phase` | Settings, initialization, panel layout, connection load and total startup time |
| `connections_load`, `connections_save` | Source category, outcome, node count and elapsed time |
| `rdp_engine_inventory` | Presence/version of Microsoft MSTSC ActiveX, `mstsc.exe` and FreeRDP |
| `rdp_capability` | Optional ActiveX feature availability without a recurring stack trace |
| `rdp_shape` | Applied RDP settings for one connect: resolution, color, redirection, scale, gateway mode, server-auth number. Also the bandwidth settings read back from the control: `perf_flags` next to `perf_flags_requested`, `net_type` (1 modem … 6 LAN, 7 auto-detect), `bw_detect` (always `unsupported` on RDC8+ controls: the get before Connect fails with `0x8345000A` and the set after `OnConnected`/`OnLoginComplete` with `E_FAIL 0x80004005`, lab 2026-10-15), `compress`, `bitmap_persist`, `keepalive_ms`, `audio_quality`, `audio_capture`. No names, paths, or secrets. Logged just before Connect, after every property write, so `perf_flags`, `net_type` and `bw_detect` are the applied values; `net_autodetect` is the option itself |
| `rdp_network` | The client's own reading of the link from `OnNetworkStatusChanged`: `quality`, `bandwidth_kbps`, `rtt_ms`. Written on the first sample, on a change of quality level, and otherwise at most every 30 seconds, carrying the minimum and maximum seen since the previous line |
| `rdp_network_summary` | Once per connection, at disconnect or close: sample count, bandwidth and round-trip minimum, mean and maximum, `quality_seconds` as level:seconds pairs, reconnect attempts, reconnects, and total `outage_ms`; from the link probe, `probe_samples`, `probe_rtt_avg_ms`, `probe_loss_pct` and `probe_jitter_ms` over the whole connection |
| `rdp_link_probe` | The application's own measurement of the session's link, independent of the control's events. After login the probe takes the endpoint the control is connected to from the process's own TCP table (the session port, or 443 when a gateway may be in use), never resolves a name, and never logs the address. It pings that endpoint (`method=icmp`); when the host has never answered and three pings are lost while the session is up, it times a TCP connect to the same endpoint instead (`method=tcp`). Every 15 s, every 5 s for two minutes after a drop, every 60 s on battery; paused while minimized or while automatic reconnection is stopped after a resume; stops two minutes after a drop the session does not recover from. Written on the first probe, when the loss band of the window changes (`reason=loss_change`: none, under half, half or more), and otherwise at most every 30 seconds, over the last 20 probes or five minutes: `samples`, `lost`, `loss_pct`, `rtt_min_ms`, `rtt_med_ms`, `rtt_max_ms`, `jitter_ms` (mean absolute difference between consecutive answered probes), `interval_s`, `low_power`. `reason=no_target` with `candidates` means no single established connection matched within 10 s of login, so the session was not probed |
| `rdp_reconnect` | Each automatic-reconnection attempt with the numeric disconnect reason and attempt number, and each successful reconnection with how long the session was gone |
| `rdp_adapt` | Shadow-mode decision of the link-stability monitor: `would_prompt` when the link turns unstable (median round trip of at least 250 ms over two minutes, or two drops within ten minutes), `unstable_suppressed` inside the 30-minute prompt cooldown, `would_offer_restore` after 15 calm minutes. Three lost of the last five link probes count as an unstable reading (`reason=loss`) without counting as a drop. Carries the thresholds, whether auto-detect was on, and the `method` of the reading that decided it (`control`, `icmp` or `tcp`). Nothing is shown to the user yet |
| `rdp_phase` | Anonymous per-session initialization, connect, login and disconnect timings/codes, plus a resource snapshot. `disc_class` names the disconnect: `user_logoff`, `api_logoff`, `logoff`, `no_info`, `local_disconnect`, `remote_user`, `remote_server`, `normal`, and the network classes `dns` (reason 260, name lookup failed), `timeout` (264, connection timed out), `tcp_connect` (516, socket connect failed; the host saw no connection) and `socket_closed` (2308, an established session's socket closed). Anything else is `other` |
| `rdp_open` | One line per RDP tab opened, written when the open ends: `outcome` (`connect_called`, `initialize_failed`, `connect_failed`, `exception`, `aborted`), `total_ms`, and the wall-clock milliseconds of each step in order as `<step>_ms`: `entry`, `prepare` (EC2 lookup, wait for the host), `pre_app` (external app started before connecting), `switch`, `panel`, `form`, `tunnel` (SSH tunnel only), `protocol`, `tab`, `interface_control`, `control_new` (ActiveX host constructed), `host_attach`, `control_create` (`CreateControl`), `control_wait` (wait for the control to be created, then `GetOcx`; this one yields to other messages), `ui_parent`, `credentials`, `settings`, `redirection`, `gateway`, `perf_flags`, `props_rest`, `post_init` (RDC7+ `PostInitialize` chain), `shape_log`, `connect_call` (the control's `Connect`), `select`. A step that awaits includes time the UI thread spent on other messages. `first_control=true` marks the open that created the first RDP ActiveX control of the process (COM registration and type library load are a one-time cost). `slow_step` names the longest step when it took 1000 ms or more. Step names and numbers only |
| `rdp_resources` | Resource snapshot at `close_before` and `close_after`, with the close trigger, disconnect class, and whether Disconnect or Dispose threw. A throw records the numeric HRESULT only |
| `heartbeat` | CPU plus the same resource snapshot every 60 seconds, including GDI, USER, handle types, and thread modules |
| `[#198-diag]` | Window DPI, screen DPI, and for the form, menu, dock, tree and config grid: font unit, raw size, and line spacing at the window DPI. A `dpi message` line records the suggested DPI, the suggested rectangle, and whether bounds were applied. No names |
| `[#216-diag]` | Opening a connection: at entry the protocol, whether it uses an SSH tunnel or waits for the host, the force flags, and the calling method names; after the tunnel starts, after its port answers, and before the target control is built: managed thread, whether that is the UI thread, the synchronization context type, and whether the tab needs Invoke. No names, hosts or ports |
| `ui_stall` | A background watchdog detects and later confirms recovery from a blocked UI thread; `detected` carries `phase=` (the last startup phase recorded before the stall, or `running` once startup logged `total`) |
| `exception` | Privacy-safe exception signature and method-only frames |

A retention paste has to answer the next question by itself. If a field was missing from a report, the next build adds it to `rdp_shape`, `rdp_resources`, `process_stop`, and `heartbeat` rather than asking the reporter to run a second tool. `process_stop` carries the same resource sample as a heartbeat, so the floor at exit is on one line. `disconnect=threw` with `hresult` is a live tab close whose Disconnect call failed; `disconnect=not_connected` together with `disc_class=user_logoff` is a session that had already logged off. Handle types and thread modules name kinds of objects only. Object names, file paths, hostnames, usernames, and secrets stay out. `disc_class=user_logoff` (extended reason 12) or `api_logoff` (extended reason 2) is the reading of a line whose display text is still "An internal error has occurred."

The watchdog uses monotonic time and treats a long watchdog scheduling gap as suspend/resume rather than an application stall.

## Analysis

Run the analyzer locally; it treats log content as data and never executes it:

```powershell
.\scripts\Analyze-RuntimeLog.ps1 -LogPath 'X:\Portable\mRemoteNG-latest\mRemoteNG Connection Manager.log'
```

Use `-AsJson` for a machine-readable summary. The report contains aggregated timings/counts and does not echo arbitrary log messages or connection labels.

## Tuning for slow or mobile links

The question "which settings are fastest on this link for the least data" is answered from three sources together:

- `rdp_shape` says what the control actually applied.
- `rdp_network` and `rdp_network_summary` say what the control measured on the link while those settings were in force.
- `rdp_reconnect` and the `disconnected` phase codes say how often the link dropped and how long each outage lasted.

`rdp_network` is event-driven: the control raises `OnNetworkStatusChanged` only when it decides to, and with the Modem connection type it can stay silent for a whole day, so on a travel link it usually has no usable samples. The application's own link probe, `rdp_link_probe`, is the source for travel: it measures round trip, jitter and loss to the endpoint the session already uses, on a fixed schedule, and its totals land in `rdp_network_summary` as the `probe_*` fields.

File → Options... → Advanced → "Let RDP detect the network speed and adapt image quality" does one thing: it changes `NetworkConnectionType` from 1 (Modem, the default) to 7 (auto-detect). With 7 the control measures bandwidth and adapts (lab: `rdp_network_summary samples=2 bw_avg_kbps=3257`), but the server then chooses the visual effects itself. With 1 the control does not measure at all, so travel calibration uses `rdp_link_probe`. The `BandwidthDetection` property is not involved: the control rejects it. `scripts/Analyze-RuntimeLog.ps1` reports `ResultsByNetType`, which attributes each connection summary to the `net_type` its session applied, so journeys with each setting can be compared, and `AdaptDecisions`, which counts what the monitor would have asked. The bytes actually sent are not visible to the embedded control; measure them outside the application, for example from the VPN's per-peer counters.

## Remote desktop engine experiments

Run `scripts/Get-RemoteDesktopEngineInventory.ps1` for a read-only local capability probe. The embedded Microsoft RDP ActiveX client remains the default. `mstsc.exe` is the safe first external A/B candidate because it is the generally available Microsoft client; any future launcher must remain opt-in and must never log its target/configuration or pass a password on the command line.

Windows App remote-PC connections are still documented as preview on Windows, so they are not a default replacement. RDP Shortpath and RDP Multipath apply to Azure Virtual Desktop/cloud paths rather than ordinary direct-PC sessions. FreeRDP remains a third-party research candidate and is neither installed nor selected automatically.

- Microsoft MSTSC: <https://learn.microsoft.com/windows-server/administration/windows-commands/mstsc>
- Microsoft RDP ActiveX: <https://learn.microsoft.com/windows/win32/termserv/msrdpclient>
- Windows App remote PCs: <https://learn.microsoft.com/windows-app/get-started-connect-devices-desktops-apps>
- Azure Virtual Desktop RDP Shortpath: <https://learn.microsoft.com/azure/virtual-desktop/rdp-shortpath>
- Azure Virtual Desktop RDP Multipath: <https://learn.microsoft.com/azure/virtual-desktop/rdp-multipath>
- FreeRDP releases: <https://github.com/FreeRDP/FreeRDP/releases>

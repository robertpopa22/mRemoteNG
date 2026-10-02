# Lab guest — running UI scenarios on the isolated VM, step by step

The lab guest is a throwaway Windows VM on the maintainer's Hyper-V host. It exists for the
checks a developer desktop cannot give an honest answer to: anything that needs a real mouse,
real keyboard focus, a clean first run, or a dialog that must be seen on a screen. This page is
the procedure, written so that a person or an agent who has never done it can do it in one pass.

## 0. What is where

| Thing | Where |
|-------|-------|
| Runner | `lab-run.ps1` in the repository root (build → stage → deploy → run → results → artifacts) |
| Installer check | `lab-msi.ps1` (same guest; installs, upgrades, launches, uninstalls) |
| Scenarios | NUnit fixtures in `mRemoteNGSpecs/Fixtures/*AcceptanceTests.cs`, all on `UiAcceptanceTestBase` |
| Helpers | `mRemoteNGSpecs/Support` (`UiWait`, `ConnectionsSeeder`, `Win32Mouse`, `ModalDialogs`), `mRemoteNGSpecs/Drivers` (`AppDriver`, `IsolatedDeployment`, `CrashWatcher`) |
| Guest VM | `mRNG-Lab-WinSrv2025` (the battery runs here); `mRNG-Lab-WinTarget` and `mRNG-Lab-Ubuntu` are connection targets |
| Guest paths | `C:\mRNG-Lab\mRemoteNG`, `C:\mRNG-Lab\mRemoteNGSpecs`, results in `C:\mRNG-Lab\_results` |
| Failure artifacts (pulled back) | `lab-artifacts/<scenario>-<id>/{desktop.png, uia-tree.txt, mRemoteNG.log}` |
| A 96/192 DPI bounce | Section 6. Add an indirect display for that run, then remove it |

The guest password is **not** in the repository. `lab-run.ps1` reads it from the environment
variable `MRNG_LAB_GUEST_PASSWORD` and refuses to start without it.

## 1. Bring the guest up

The VMs are normally off. From an elevated or Hyper-V-Administrators PowerShell on the host:

```powershell
Get-VM -Name 'mRNG-Lab*' | Select-Object Name, State
Start-VM -Name 'mRNG-Lab-WinSrv2025'
# wait until Heartbeat reads OK (about a minute)
Get-VM -Name 'mRNG-Lab-WinSrv2025' | Select-Object State, Heartbeat, Uptime
```

The guest must have its console user logged on: the battery runs as a scheduled task with an
interactive principal, because PowerShell Direct lands in session 0 where there is no desktop
and UI Automation sees nothing. The guest is configured for that; if the run finishes with no
results file, that logon is the first thing to check.

## 2. Put the password in the environment — without printing it

Take the guest password from your password manager and set it **in the process that runs the
script**, never on the command line and never in a file in the repository. Two patterns that work:

```powershell
# interactive shell
$env:MRNG_LAB_GUEST_PASSWORD = Read-Host -AsSecureString | ConvertFrom-SecureString -AsPlainText
pwsh -NoProfile -ExecutionPolicy Bypass -File lab-run.ps1 -NoBuild -Artifacts -Filter "FullyQualifiedName~Startup"
```

```python
# from an agent or a script: fetch from the password manager in-process, hand it to the child only
import os, subprocess
env = dict(os.environ)
env["MRNG_LAB_GUEST_PASSWORD"] = fetch_from_password_manager("<lab guest item>")
subprocess.run(["pwsh", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", "lab-run.ps1",
                "-NoBuild", "-Artifacts", "-Filter", "FullyQualifiedName~Startup"], env=env)
```

An agent must never echo the value, log it, or paste it into a chat.

## 3. Build, then run

`lab-run.ps1` builds by default. When the build is already current (it usually is after the
unit suite), pass `-NoBuild`; the staging step copies `mRemoteNG\bin\x64\Release` and
`mRemoteNGSpecs\bin\x64\Release` as they are.

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File lab-run.ps1                  # everything, builds first
pwsh -NoProfile -ExecutionPolicy Bypass -File lab-run.ps1 -NoBuild -Artifacts -Filter "FullyQualifiedName~ExternalConnectorsMissingAcceptanceTests|FullyQualifiedName~SqlServerOptionsPageAcceptanceTests"
pwsh -NoProfile -ExecutionPolicy Bypass -File lab-run.ps1 -NoDeploy -Filter "FullyQualifiedName~Startup"   # re-run what is already on the guest
```

- `-Filter` is an NUnit filter; `|` means or.
- `-Artifacts` pulls `desktop.png`, `uia-tree.txt` and the application log for every failed
  scenario into `lab-artifacts/`. Always pass it; a failure without artifacts is a guess.
- Do not rebuild on the host while the `Stage` step is running — it reads `bin` at that moment.
  Once `Deploy` has started, the payload is a zip in the temp directory and `bin` is free.
- If the script is launched from a wrapper that pipes its output (an agent through `grep`, for
  example), nothing appears until the end; the run takes a few minutes. That is buffering, not
  a hang.

The exit code is 0 only when every selected scenario passed.

## 4. Read the result

The summary prints `total / passed / failed` and, per failure, the assertion message plus the
lines the scenario logged about dialogs, focus and top-level windows. Then open the artifacts:

1. `desktop.png` — the whole guest screen at the moment of failure. Look for a dialog first.
2. `uia-tree.txt` — every window and control UI Automation could see, with names and ids.
3. `mRemoteNG.log` — what the application did (`log4net` writes it beside the executable).

A pass in the guest is evidence of the mechanism on a clean machine. It is still not the
reporter's environment; say so in the reply.

## 5. Writing a scenario

Derive from `UiAcceptanceTestBase`; every test gets a fresh, hard-linked copy of the build with
an empty portable `Settings` folder and its own process. The things that took a session each to
learn, so they are written down here once:

- **Seed before start.** Override `SeedSettings()`; `Deployment.WriteConnectionsFile(new
  ConnectionsSeeder().Add(...).Build())` puts connections in place before the app starts.
- **Simulate an incomplete install** by deleting a file in `Deployment.Directory` — it is a hard
  link, so the canonical build is untouched. `ExternalConnectorsMissingAcceptanceTests` removes
  `ExternalConnectors.dll` this way, the folder #175/#191/#192 reported.
- **Menus:** a plain `Click()` on a menu-bar item does not open it; use
  `element.Patterns.ExpandCollapse.Pattern.Expand()`. The drop-down is a separate popup, so find
  its entries from `Driver.Automation.GetDesktop()`, not under `MainWindow`.
- **Options pages** live docked inside the main window; select a page with
  `lstOptionPages` → `Patterns.SelectionItem.Pattern.Select()`, controls by their WinForms
  `Name` (`chkUseSQLServer`, `btnTestConnection`, `pnlMain`).
- **Double-click a tree row with `Win32Mouse.DoubleClick(row)`**, not `row.DoubleClick()`.
  FlaUI's version is two waited clicks; on the GPU-less guest they land as two single clicks and
  the tree starts a rename instead of opening the connection. `Win32Mouse.DoubleClick` sends all
  four button events in one `SendInput` batch.
- **Right-click a tree row with `Win32Mouse.RightClick(row)`** — the context menu is wired to a
  real mouse event, not to Invoke.
- **Crash detection:** `CrashWatcher.Check(Driver, grace)` finds the application's own
  unhandled-exception window (`FrmUnhandledException`). Use `AssertNoCrash("context")` at the
  end of a scenario that must not crash, and `CrashWatcher` directly when the crash is the point.
- **Unexpected dialogs fail the test** with their caption in the message (`AnswerExpectedPrompts`);
  do not wait them out.
- **Evidence:** `Capture.Element(MainWindow).ToFile(path)` plus
  `TestContext.AddTestAttachment(path, "what it is")` gives a real screenshot from the guest —
  the only kind of screenshot a scrolled WinForms container renders correctly.
- Every scenario carries `[Issues("#n")]` for what it covers, `[Touches("#n")]` for what it
  merely exercises, `[StressCoverage("#n")]` for races it can only make more likely.
- **Evidence that must survive a pass** goes in `_uiscenarios/_evidence/<test>/`: the scenario
  directory itself is deleted when the test passes, so anything you want to look at afterwards
  has to live beside it. `lab-run.ps1 -Artifacts` copies that folder out of the guest either way.
- **The verbose log** is the application's own decision trace (`DevLog`). It is written only when
  a file named `verbose.log.enable` sits next to the executable, so a scenario that wants it
  writes one into `Deployment.Directory` in `SeedSettings()` and then reads
  `mRemoteNG-verbose.log` from the same folder.

### Which RDP target answers which question

- **`ConnectionsSeeder` seeds `RdpVersion = RdpVersion.Highest`**, because a bare `ConnectionInfo`
  leaves the enum at its zero value, `Rdc6` — the base `RdpProtocol`, an RDC 6 ActiveX class with
  none of the dynamic-resize code added in `RdpProtocol8` and later. The application never does
  that; it copies `DefaultConnectionInfo`, whose value is `Highest`. Change this and the battery
  silently tests a class no user has run since Windows 7.
- **The Linux target (xrdp) never raises `OnLoginComplete`.** `RdpProtocol8.DoResizeClient` and
  everything else gated on `loginComplete` therefore skips with "login not complete" in the
  verbose log. xrdp is fine for connect/disconnect lifecycle and memory measurements; it cannot
  exercise the resize/reconnect path at all. That needs the Windows target.
- **Windows target session replacement was resolved on September 29.** Both `AutoAdminLogon`
  and `ForceAutoLogon` were `1` under Winlogon on the connection target, causing a local console
  reconnection to replace the RDP session. Both were changed to `0` on **WinTarget only**, with
  previous values retained for rollback. The UI runner guest still needs its interactive logon.
  The Windows scenario then completed all four splitter drags with `OnLoginComplete` observed
  and session widths 600, 760, 840 and 760 pixels. Screenshots showed no persistent stale frame
  after the drags. This does not establish the absence of a transient artifact during dragging
  or reproduce every sizing mode. If a target disconnects again, report the scenario as
  unexecuted rather than treating absence of a live session as a pass.
- **An unattended upgrade on the Linux target can break every session until it is rebooted.**
  Ubuntu's `unattended-upgrade` restarts `xrdp`/`xrdp-sesman` after installing packages, and the
  desktop session that was already running is orphaned from the new `xrdp-sesman`. Every new
  logon then starts a second XFCE session for the same user on the next display, which exits at
  once, and mRemoteNG logs "Protocol Event Disconnected ... An internal error has occurred" about
  half a second after "established by user". Seen on 2026-09-24: a kernel upgrade at 06:20 UTC
  restarted xrdp while a 13-day-old session was still alive on `:10`. `Restart-VM mRNG-Lab-Ubuntu`
  clears it. `RdpSessionMemoryAcceptanceTests` now reports such a run as inconclusive instead of
  passing it: a scenario about closing a live session must not go green when there was no session.

## 6. A second monitor at another DPI

The guest desktop is one monitor at one scale. Moving a window on it does not send
`WM_DPICHANGED`, so the battery cannot show the 96 ↔ 192 bounce behind `[#198-diag]`.
Windows lists a monitor only when a display adapter exposes a target. No call in
`user32` or Display Configuration invents one.

When a check needs a second device DPI, load an indirect display driver (IddCx) for
that run, on the throwaway guest. The driver adds a target that
`EnumDisplayMonitors`, `GetDpiForMonitor`, and `WM_DPICHANGED` treat as a panel.
Hyper-V's synthetic adapter stays a single guest monitor, and an enhanced session
copies the client's monitors into the guest instead of giving the guest its own
second scale. Remove the indirect display device before the run is finished, and
confirm it is gone from the monitor list.

### Give the two monitors different scales

A second monitor at the same scale still does not send `WM_DPICHANGED`. After Windows
lists the new panel, set one side to 100% (effective DPI 96) and the other to 200%
(effective DPI 192) when Display settings offers 200%. A high resolution on the virtual
panel, such as 3840×2160, is the mode on which Windows usually offers 200%. On this
guest that mode's recommended scale is 200%, and the measured effective DPI was 192
while the Hyper-V panel stayed at 96. A 1920×1080 target on the same guest tops out at
175% (168 DPI); a request for 200% is clamped. A panel whose maximum is below 200%
still produces a bounce; record that effective DPI and do not call it 192.

Confirm the scale from a process that is per-monitor v2 (`GetDpiForMonitor` with
`MDT_EFFECTIVE_DPI`). A DPI-unaware process reports 96 for every monitor, including
after the scale has changed. Restore every scale you change, and confirm both sides
are back, before the session ends.

Two placements, both required for the report:

- Primary at 96, and the main window moved entirely onto the 192 monitor. The log line
  `dpi change` goes from 96 to 192. The menu's line spacing at the window DPI is within
  15% of the dock panel. When a font or icon size was rebuilt, the following line is
  `dpi change after fit`.
- Primary at 192, and the main window moved entirely onto the 96 monitor. The same line
  reports screen DPI 192 while the window DPI is 96. That is the placement that logs a
  16px line as 4.5pt, because a font that is not in points asks the screen DC.

Move the window from a per-monitor v2 process, with `SetWindowPos`, and keep the whole
window inside the target work area. A DPI-unaware mover uses virtualized coordinates
and can miss the monitor. Read `[#198-diag]` in the portable log beside the executable
(`mRemoteNG Connection Manager.log`). The fields are in
[RUNTIME_DIAGNOSTICS.md](RUNTIME_DIAGNOSTICS.md). A menu that keeps its old point size
while the dock scales is the open symptom. A run whose window procedure does not
return, so `dpi change after fit` is never written, is an incomplete run.

The evidence is the DPI numbers, the font unit, the raw size, and the line spacing.
Monitor names of the form `\\.\DISPLAYn` may be in the line. Account ids, passwords,
and machine names stay out of the log and out of this repository.

### What the guest enumerated (2026-10-02)

The desktop starts as one Hyper-V panel, 1024×768 at 96 DPI. That panel's own scale
list stops at 125%. An IddCx 1.2 sample was loaded for the measurement and removed
before the guest was returned to one monitor. The sample reads the target count when
the device is added, so a new count means removing the device and adding it again.
Session 0 does not see this topology. The counts are from the interactive console, in
a per-monitor v2 process. "Monitors" includes the Hyper-V panel.

| Virtual targets | Monitors enumerated | What was on screen |
| --- | --- | --- |
| 1 | 2 | 1920×1080, its own `\\.\DISPLAYn` |
| 2 | 3 | 1920×1080, side by side, each at 96 DPI |
| 4, 8, 16 | 5, 9, 17 | 1280×720, each attached and placed to the right of the previous one |
| 17, 18, 32, 44 | 1 | The device started and exposed no target |

Sixteen virtual targets is the highest count that enumerated, including a repeat after
a reboot. Seventeen and above leave the device started and the desktop on the Hyper-V
panel alone, so 45 monitors was not reached. A shared EDID did not merge the targets:
each one kept its own device name and its own origin. Active display paths matched the
monitor count. At 16 virtual targets the all-paths query returned 257 paths and 514
modes. The guest was not out of memory at the failed counts.

The 96/192 pair measured here is the Hyper-V panel at 100% beside a 3840×2160 virtual
target at its recommended 200%. That target's scale list runs from 100% to 350%. Both
were restored afterwards (1920×1080 at 96, Hyper-V still 96). This pass did not move a
window, so it does not close the font-fit check.

Loading that sample required test signing. Turn test signing off and confirm the
indirect display is gone before the ordinary battery.

## 7. When done

Leave the guest running if another run is coming; otherwise `Stop-VM -Name 'mRNG-Lab-WinSrv2025'`.
Nothing on the host changes: the scenario copies live under
`mRemoteNGSpecs\bin\x64\Release\_uiscenarios` on the guest and are swept after four hours.

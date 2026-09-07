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
pwsh -NoProfile -ExecutionPolicy Bypass -File lab-run.ps1 -NoBuild -Artifacts -Filter "FullyQualifiedName~CrashReportAcceptanceTests|FullyQualifiedName~SqlServerOptionsPageAcceptanceTests"
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
  link, so the canonical build is untouched. `CrashReportAcceptanceTests` removes
  `ExternalConnectors.dll` this way to reach the #175 crash.
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

## 6. When done

Leave the guest running if another run is coming; otherwise `Stop-VM -Name 'mRNG-Lab-WinSrv2025'`.
Nothing on the host changes: the scenario copies live under
`mRemoteNGSpecs\bin\x64\Release\_uiscenarios` on the guest and are swept after four hours.

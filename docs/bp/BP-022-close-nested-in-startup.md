# BP-022 — A close can run inside startup

**Version:** 1 · **Updated:** 2026-10-20

**Does:** record how a close that arrived while startup was still opening a restored RDP tab ran the whole shutdown inside that call, and why the code that unwound afterwards turned a clean exit into a crash.

**Does not:** describe the close path itself (`docs/RUNTIME_DIAGNOSTICS.md`) or the reconnect-at-startup rules (BP-020).

## Incident

On 2026-10-20 a scripted restart on a maintainer's laptop sent WM_CLOSE about 16 seconds into startup. "Reconnect at startup" was on. The restored RDP tab was inside `IMsRdpClient.Connect`, which pumps messages. That call runs inside the `ConnectionsLoaded` handlers, so `FrmMain_Load` was still on the stack. The close ran to the end inside it: the panels closed, the connections and the settings were saved, and the main form was disposed. Then `Connect` returned and startup went on. The connection tree, already disposed, threw `ObjectDisposedException`. `Runtime.LoadConnections` reported that as "Connections file could not be loaded!". Its recovery branch then tried to open a message box on the disposed form. Finally WinForms itself focused the active control of the disposed, maximized main form after `Load`, and that exception escaped `Application.Run` as a fatal crash. The connections file was intact. The crash was reproduced locally by posting WM_CLOSE during startup, and the same sequence was confirmed gone after the fix.

## Rule

Any call that pumps messages (an ActiveX connect, a modal dialog, `DoEvents`) can run the whole shutdown before it returns. Code that resumes after such a call checks `FrmMain.IsShuttingDown` before it acts, and stops when it is set. It does not read the exceptions that follow as a broken file or a failed load: recovery dialogs, a new empty file, a retry or `Application.Exit` are all wrong there. A save is never skipped and never repeated: the nested shutdown already made it.

# RDP retention regression checks

These checks detect bounded regressions in a finite Windows lab run. They cannot certify that
all allocations are released, explain a native heap's ownership, or cover every RDP server.
Issue #182 stays open while the reporter's contrary result is unanswered.

## Automatic checks

- Real WinForms ownership tests create and dispose fourteen docked panels, tabs and their
  menus, then require all 56 weak references to be dead while the shared theme stays alive.
  They exercise PanelAdder's outer menu registration and repeated disposal.
- Real RDP ActiveX ownership tests cover protocol selections 6 through 11, both raw hosts
  and initialized protocols with event handlers. Each case disposes three instances and
  requires all three to be collectable. Only these ownership tests force GC. They protect
  the COM event subscription and the display-event cleanup even without a network target.
- The unit suite replays the September 29 fourteen-close report (274 to 1,274 MB). It must flag
  growth regardless of the first session's peak. Slow cumulative growth and a misleading final
  dip are covered too. A plateau is called `within-budget`, never leak-free.
- The same suite runs the actual PowerShell reporting function against growing, flat,
  decreasing and insufficient series. Exit zero means measurement completed.
- `RdpSessionMemoryAcceptanceTests` and its retained-tab variant require a Windows target,
  fourteen completed logins, a 30-second wait after each close, and a clean application exit.
  Both the inner session tab and the outer General panel are closed in separate scenarios.
  Missing credentials, failed logins and dropped sessions fail; they are not skipped.
- Every long scenario records six idle-control samples, each open/closed sample, process
  handles, GDI objects, screenshots after login, the application log (including managed-memory
  heartbeats), and a SHA-256 of the tested application DLL. No forced garbage collection or
  working-set trimming is performed.

## Budgets and interpretation

The initial regression budgets are **32 MiB total private-byte growth, 32 process handles and
16 GDI objects** over the run. These are conservative engineering limits, not an experimentally
established leak/no-leak boundary. They do not scale with the first session or with noisy data.
Growth is the greater of last-minus-first and the difference between the medians of the last
three and first three closes. An idle range exceeding half the relevant budget is inconclusive
and blocks acceptance. An incomplete run also blocks acceptance.

A failed budget requires investigation; a valid cache may fail it. Do not loosen a budget to
make a run pass. A budget change needs recorded repeated control runs, the complete series,
and an explicit explanation of what additional retained resources are acceptable.

The Windows acceptance run also measures Microsoft's generated RDP control in the same test
process, against the same target. The baseline host does not assign a server-authentication
policy; it answers this process's certificate warning when one appears. The application is
charged only for growth beyond that control. The control does not
widen the budget: its growth is an allowance, a shrinking control gives none, and an
inconclusive or incomplete control blocks the run. `RetentionAssessmentTests` pins those rules.
The receipt schema is 2 and names the control series. This differential does not declare #182
fixed, and it does not replace the reporter's measurement.

## Lab differential, 2026-10-01

Isolated Hyper-V lab, Windows target, 1100x750, steady per closed session after warm-up.
These figures explain where the lab's residue sits. They do not reproduce the reporter's
approximately 70 MB per session (lab private growth is about 1 MB per session, about half of
that from the control).

| Configuration | handles | GDI | USER | threads |
|---|---|---|---|---|
| Microsoft generated host, fresh instance, no certificate dialog | ~+73 | 0 | +8 | +2..3 |
| Same host, per-monitor-v2 thread context | ~+72 | 0 | +8 | +2 |
| Same host, one instance reused | ~+9 | 0 | 0 | 0 |
| Any host that shows the server-certificate warning | ~+1 | +9 | +3 | — |
| `RdpProtocol11` plus `InterfaceControl`, no certificate dialog | ~+73 | 0 | +8 | +2 |
| Same protocol inside a connection tab, icon and tab menu kept | ~+77 | +5 | +9 | +2 |
| Same tab, document icon off and tab menu detached | ~+77 | +2 | +8 | +2 |
| `ConnectionInitiator` opening that same tab | ~+77 | +5 | +9 | +2 |
| Full application, default warn (dialog each session) | ~+82 | +23 | — | — |
| Full application, no certificate dialog | ~+81 | +8 | — | — |

Waiting for native `OnDisconnected` before dispose changed nothing. A settings bisect (colors,
clipboard, performance flags, session options, scale factors, drives) did not move GDI, handles,
USER objects or threads. UI Automation polling during the session added nothing. An induced
collection left full-application GDI unchanged, so that residue is not finalizer-pending.
The native RDP refcount reaches 0 when an initialized protocol is disposed, connected and
unconnected (`DisposingAnInitializedProtocolReleasesTheNativeRdpObject`, selections 6–11).

A live session inside a real connection tab, measured for six closes on 2026-10-01, keeps about
5 GDI objects, 4 handles and 1 USER object per session above the Microsoft host. Opening that
tab through `ConnectionInitiator` does not change the per-session slope. Turning the document
icon off and detaching the tab context menu removes about 3 of those GDI objects and the extra
USER object, and leaves about 2 GDI objects and 4 handles per session in the tab host.
The full application, with no certificate dialog, is still about 3 GDI objects and 4 handles
per session above that tab. That remainder is outside the protocol, the tab and the initiator.
Over fourteen sessions the full-application excess stays above the 16 GDI and 32 handle budgets,
so acceptance stays red until it is explained or removed. Reusing one ActiveX instance cuts the
control's residue from about 73 handles to about 9, and drops the extra threads. It also reuses
credential and session state across logons, so it stays a design proposal and is not a #182 fix.
`rdclientax.dll` is not a substitute: it is not redistributable. The Remote Desktop client for Windows (MSI) that ships
it left public-cloud support on 2026-03-27
([Microsoft Learn](https://learn.microsoft.com/en-us/previous-versions/remote-desktop-client/overview),
retrieved 2026-10-01). Direct RDP in this application stays on `mstscax`.

## Maintainer acceptance

Run `lab-run.ps1 -NoBuild -Artifacts -Filter 'FullyQualifiedName~RdpSessionMemory'` after building.
An empty, skipped, failed or incomplete selected battery returns failure. Inspect all saved
desktop screenshots: a login event alone does not prove that the expected desktop rendered.
Keep the receipt and logs with the tested DLL hash. Re-run against the final application build;
an older successful run cannot validate a changed binary.

The hosted CI unit suite can reject the historical false verdict without credentials. It does
not have the isolated Windows RDP target. A green hosted CI check is therefore not Windows
memory acceptance and must not be described as such in a release or issue reply.

The transient-menu adapter relies on DockPanelSuite 3.1.1's restoration-map contract. The RDP
event adapter avoids an unbalanced temporary sink reference observed with Windows Forms
10.0.11. Dependency upgrades must continue to pass the actual ownership tests; neither adapter
is a reason to relax those tests. Explicit tab/panel close and exit are covered by the Windows
matrix. Disconnect-only retained placeholders and the reporter's exact machine remain separate
acceptance paths.

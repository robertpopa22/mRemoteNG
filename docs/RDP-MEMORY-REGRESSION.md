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

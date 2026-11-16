# BP-028 — Suspend the container being populated

**Version:** 1 · **Updated:** 2026-11-16

**Does:** record how to isolate repeated WinForms layout work without making a wall-clock assertion in a unit test.

**Does not:** promise a startup time on another computer or replace the runtime evidence required by [D9](../../CHARTER.md).

## Incident

The trace supplied for #232 separated a 9,325 ms first Options opening from a 90 ms reopening. Constructing the form took 8,202 ms, with individual page-layout stages taking up to 2,438 ms. In `AdaptiveLayout.ReflowRows`, the parent was suspended while a newly created, auto-sized `TableLayoutPanel` was populated. Suspending the parent did not suspend that new table. Adding controls could measure them while the table was still incomplete.

A regression test added twenty labels and recorded the table's control count whenever a label was measured. Before the change, measurement started with just one control. Suspending the new table during construction and resuming it in `finally` moved those measurements until all twenty controls were present. This proves removal of partial-table measurement; it does not prove that all of the reporter's cold-start delay had that cause.

## Rule

When batching dynamic layout construction, suspend the container whose children and styles are changing, including a newly created container. Resume it in `finally`. Test the unwanted intermediate work directly when possible, then drive opening, resizing and reopening in the running application. Report those timings as observations from that run; cold JIT, caches and display settings can change them.

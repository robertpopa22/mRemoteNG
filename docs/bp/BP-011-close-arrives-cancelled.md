# BP-011 — A close handler that commits to shutting down owns the final value of Cancel

**Version:** 1 · **Updated:** 2026-10-07

**Does:** record that WinForms can raise `FormClosing` with `Cancel` already true, and that a handler which tears the application down must not leave it that way.

**Does not:** name the control whose validation failed (not yet identified; issue #213 tracks it).

## Incident

From 2026-09-25 to 2026-10-07 every application close on one laptop after hours or days of use hid the window but never ended the process (issue #213). The close path logged `the message loop should now end`, but no `process_stop` followed. Every such close showed the connection panel at `cancel True, tabs left 0` without taking its only cancelling branch. Every clean close showed `cancel False`. `Form.WmClose` initialises `Cancel` to `!Validate(true)`. `FrmMain_FormClosing` never read it: it saved, hid the window and disposed the tray icon, then returned with `Cancel` still true. The form stayed open, invisible, holding the single-instance mutex, so the next start did nothing. `CloseCancelledByValidationTests` reproduces the WinForms half.

## Rule

A `FormClosing` handler logs the `Cancel` value it received. Once it has made a decision that cannot be undone, such as hiding the window, saving for exit or tearing down services, it sets `Cancel` to the value that matches that decision before returning. It never leaves an inherited value in place. A handler that may legitimately cancel does so before any irreversible step. A child panel closed as part of application exit does not keep a validation veto when the user was not asked anything.

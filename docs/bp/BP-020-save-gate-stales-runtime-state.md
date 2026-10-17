# BP-020 — A save gate also stops runtime state written by the same save

**Version:** 1 · **Updated:** 2026-10-17

**Does:** record that skipping saves when the model is unchanged left a runtime flag in the connections file stale, and that a startup path kept reading that flag as current.

**Does not:** describe the restore paths themselves (the #172 comments in `PreviousSessionOpener` and `ConnectionWindow` hold that).

## Incident

Commit 78fa64c2c (2026-08-31, first in 1.84.0) made the autosave timer write `confCons.xml` only when the model had changed. Its dirty tracking ignores `OpenConnections`, because a tab opening or closing is not an edit. The same save also writes the `Connected` attribute from `OpenConnections`. With the default save setting nothing else writes the file at exit, so `Connected` kept describing the session at the last edit. The tree restore path reopened every node with that flag, and a tab closed during the session came back on the next start (#172, reported 2026-10-16). The unit suite stayed green: no test read a file written before a tab was closed. The lab scenario that seeds stale flags, closes one tab and restarts showed two tabs instead of one.

## Rule

Before a change stops a save from running, list everything that save writes, not only the fields that arm it. A value written from runtime state, but excluded from dirty tracking, is no longer current once the gate is in place. Every reader of that value must either get its data from a record written on every exit, or treat the value only as a fallback. The test for the gate includes a restart after a runtime-only change.

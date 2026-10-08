# BP-014 — Anything that runs before the first control can take the UI thread's synchronization context

**Version:** 1 · **Updated:** 2026-10-08

**Does:** record that a subscription made on the UI thread before WinForms has installed its context can leave a plain `SynchronizationContext` there for the whole session, and how that was told apart from the other explanations in one run.

**Does not:** describe the helper that prevents it (`PowerAwareness.WithoutInstallingContext` and its tests hold that).

## Incident

Issue #216, 2026-10-08: connections through an SSH tunnel failed with "Controls created on one thread cannot be parented to a control on a different thread", and HTTP failed to start WebView2. The reporter named the last good nightly. Two independent reviews found no change in that diff that touched the tunnel path, and no await on it that dropped the context. A diagnostic line that logged the context type at the start of an open answered it at once: the good build had `WindowsFormsSynchronizationContext`, the bad one a plain `SynchronizationContext`. The cause was a new power-state subscription to `SystemEvents`, made during start-up before any control existed. `SystemEvents` reads `AsyncOperationManager.SynchronizationContext`, and that getter installs a plain context on a thread that has none. Every UI `await` then resumed on the thread pool. Only the tunnel path awaited real I/O between building two controls, so it was the only visible symptom.

## Rule

Code that runs on the UI thread before `Application.Run` and before the first control must not leave a synchronization context behind. Subscribe to `SystemEvents`, `BackgroundWorker` or anything that goes through `AsyncOperationManager` after the main form exists, or restore the empty context afterwards. When a threading symptom has no visible cause in the diff, log the thread and the context type at the boundary, run the same scenario on the last good build and on the bad one, and compare. That costs one build in a worktree. Reading the diff again does not separate the explanations.

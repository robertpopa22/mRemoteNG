# BP-026 — A crash inside a handler that only stale objects can run is a leak report

**Version:** 1 · **Updated:** 2026-11-09

**Does:** record how to read an `OutOfMemoryException` (or any crash) whose stack runs through an event handler of an object that is rebuilt on every load.

**Does not:** describe the smart group nodes themselves (#227, `ConnectedGroupNode`, `RecentGroupNode`) or how runtime evidence is collected ([BP-001](BP-001-runtime-evidence.md)).

## Incident

#227 was an `OutOfMemoryException` in a 32-bit process, thrown in `ConnectionTreeModel.GetRecursiveChildList` under `ConnectedGroupNode.OnConnectionsLoaded`. The allocation site was a small array copy and said nothing. The handler said everything: the tree window builds a new `ConnectedGroupNode` inside each `ConnectionsLoaded` raise, and a delegate added during a raise is not called by that raise. So `OnConnectionsLoaded` could only ever run on a node of an earlier tree. Its presence on the stack proved that earlier trees were still alive. Source then showed two process-lifetime events (`ConnectionsService.ConnectionsLoaded`, `RecentConnectionsService.RecentConnectionsChanged`) holding every replaced tree through the node's `Parent`.

## Rule

When a crash stack runs through a handler, first ask which instance can be running it. If only an object that should already be gone can reach that handler, treat the report as a leak, whatever the exception type, and find the long-lived event that keeps the object. An object created per load or per window that subscribes to a process-lifetime event needs a matching unsubscribe on the path that replaces it, and a test that counts the subscribers.

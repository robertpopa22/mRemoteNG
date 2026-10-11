# BP-015 — A provider authorization wait must leave the message loop running

**Version:** 1 · **Updated:** 2026-10-11

**Does:** record how a delayed external credential CLI exposed blocking work inside an async connection path.

**Does not:** certify a vendor's authorization dialog or prescribe a session credential cache.

## Incident

Issue #217 reported that both the application and the 1Password authorization window froze. The connection entry point already awaited initialization, but RDP property setup and PuTTY connection setup still read the CLI's redirected streams synchronously. A fake CLI waiting on a release marker reproduced the application freeze in the isolated lab: its main window stopped answering Windows messages. Moving the provider read off the UI thread left the window responsive in the connection, gateway and default-provider paths. The vendor's own dialog was outside that reproduction.

## Rule

Trace the entire path below an async entry point. Read provider output off the UI thread, drain stdout and stderr concurrently, and bound the child process wait. Resume control and ActiveX work on the originating UI context. Exercise pending authorization, failure and closing while pending in the real application. A delayed fake provider proves message-loop responsiveness; only testing with the vendor's application proves its authorization UI.

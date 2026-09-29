# September catch-up execution status

This is the current result of the maintainer's instruction to resolve the remaining actions.
Earlier checkpoints in the catch-up plan are historical, not the current status.

| Work | Completed evidence | Remaining condition |
|---|---|---|
| SSH.NET | 2026.0.0, required BouncyCastle 2.7.0, explicit POSIX ShellQuote, SFTP default. Six quoting regressions and 16 real SCP/SFTP literal-path uploads; exact bytes, no shell marker, no retained source-file handle. Async SFTP errors are observed before reporting completion. | Publish verified code. SCP requires a POSIX shell; other servers use SFTP. |
| #200 | Disposed tree subscribers detached; normal File/Open and saved/reloaded-layout File/Open both replace the visible tree without the exception. | Publish build and obtain reporter result. |
| #196 | Real app records requested settings and control acceptance/rejection. Lab: EnableRdsAadAuth accepted; RedirectWebAuthn rejected with 0x8000FFFF. No WebAuthn property exists in the bundled interop interfaces inspected. No authentication or certificate policy changed. | A matching Entra target and reporter diagnostic result; setting acceptance does not prove sign-in. |
| #198 | Dialog close/cancel/disconnect UI check passed at measured 96 DPI; checkbox metrics now recorded. | Reporter build/scaling and mixed-font evidence. The attempted 150% guest configuration still measured 96 DPI, so it is not a 150% validation. |
| #177 | Target forced autologon corrected; four splitter drags completed after OnLoginComplete. No persistent stale frame in retained screenshots. Result posted and read back. | Reporter sizing mode and transient-during-drag capture; issue remains open. |
| PR #199 | Exact contributor head integrated locally; missing dependencies label created and applied. | Remote integration. |
| #179 | Concrete OK/Apply/Cancel proposal posted for human discussion and read back. | Affected-user feedback before changing the editor's interaction model. |
| #192 | Pristine release sample downloaded, published checksum matched, extracted DLL hashed; submission explanation prepared. Signing prerequisites verified. | Microsoft portal sign-in, submission receipt, and maintainer selection of a public signing provider/legal publisher. No signed build or vendor clearance exists. |
| #182/#197/#165 | September 25 evidence requests reconciled; no fresh reporter answer. | One follow-up is due no earlier than October 2; preserve the attempt budgets and human-review boundary. |

The full build of the combined code succeeded. Four focused UI scenarios passed, followed by
two current-code authentication/dialog diagnostic scenarios. The final full-suite rerun passed
**7,335/7,335 tests in 229 seconds**, with no crashed groups. All 20 IIS regression tests passed.
The preceding interrupted run is not counted. Publication is tracked in
[PR #201](https://github.com/robertpopa22/mRemoteNG/pull/201).

Existing local main commits included in this integration also fix duplicate confirmation when the
last session tab closes and make portable deployment rollback use atomic renames and verify restored
hashes. They were already integrated locally before this task and are preserved in the public diff.

Public communication receipts are in
[completion receipts](issues-db/reply-drafts/completion-receipts-2026-09-29.json).
The Microsoft submission package is [prepared separately](MICROSOFT-SUBMISSION-192-2026-09-29.md).
No issue is closed by this status document.

# Maintainer decisions prepared on September 29

These record the initial decision baseline. For the subsequent authorized execution, see
[current completion status](COMPLETION-STATUS-2026-09-29.md). See also the
[catch-up plan](CATCH-UP-PLAN-2026-09-29.md) and [action register](issues-db/maintainer-actions.json).

## PR #199

[PR #199](https://github.com/robertpopa22/mRemoteNG/pull/199), head
`20c28bbd1d9ddc2ff24f8ac4d49896fc466c664c`, changes only Meziantou.Analyzer
2.0.194 → 3.0.290 in `Directory.Packages.props`. GitHub reports MERGEABLE/CLEAN.
All six checks passed: solution builds on x86/x64/ARM64, tests/specs builds on x86/x64,
and leaked-credential detection. These check names establish builds, not test execution.

The change is suitable for the normal dependency merge path once authorized, with the usual
post-merge build/test review. It does not address the SSH.NET advisories below. The configured
`dependencies` label is absent; create that label with description "Dependency updates" and
colour `0366d6`, then apply it to the PR. No label or merge was performed in this review.

## #192: vendor submission and signing

The original report names 1.82.0 Release 3593; the September 22 quarantine comment does not name
the build then in use. The detection name is already supplied: `Trojan:Win32/Bearfoos.A!ml`.
No Microsoft submission receipt was found. Do not claim the cloud/ML suffix proves a false
positive, repeat the detection-name question, or promise signing without a working signer.

Prepare the vendor submission using a pristine published release archive, its published checksum,
the extracted DLL's SHA-256, exact package/build, and the detection name. Record uncertainty about
the September 22 build. Submit the binary only after authorization, retain the submission ID/date,
and post a status supported by that receipt. No reporter connection file is required.

The ordinary-connection fallback in `40f78094c` is in stable v1.84.0. That mitigates a missing DLL;
it does not clear Defender's detection or sign the binary. Upstream #3514 reports a present,
unsigned file blocked by Smart App Control; it needs its own disposition.

## SSH.NET advisories

The build resolves SSH.NET 2025.1.0. `SecureTransfer.Connect` constructs `ScpClient` with default
remote-path transformation; `Upload` passes the requested destination. An attacker-influenced
destination interpreted by a POSIX shell can therefore reach the path-handling condition in
[the publisher's command-injection advisory](https://github.com/sshnet/SSH.NET/security/advisories/GHSA-mggc-4xg6-vcxf).
This is a source-level exposure assessment, not an exploit demonstrated against a target.

2026.0.0 provides an explicit transformation constructor, but retains the legacy default for
compatibility. A package-only upgrade is insufficient. The next patch must choose explicit quoting
appropriate to supported servers, test literal paths with spaces, quotes, dollar substitutions and
backticks on a disposable target, and retain SFTP compatibility. Do not silently assume every SSH
server runs a POSIX shell. No dependency or transport change was made during this intake update.

The separate [recursive-download advisory](https://github.com/sshnet/SSH.NET/security/advisories/GHSA-q939-rpr3-3284)
also affects the installed version; no SCP recursive download call was found in product source.
It remains a dependency warning, with reachability distinct from the upload issue.

## #196: diagnostic boundary

The [Microsoft extended-settings contract](https://learn.microsoft.com/en-us/windows/win32/termserv/imsrdpextendedsettings-property)
lists `EnableRdsAadAuth` as a **write-only boolean**, set before connection starts. A failed getter
would not prove setter failure. Current source requests this property but suppresses E_UNEXPECTED.
The next diagnostic should record requested-property acceptance/rejection and HRESULT without
credentials, tokens or server names. It must not change CredSSP, certificate checks or the selected
authentication method. The actual sign-in still requires a matching Entra target; none was verified
in this session. This source finding does not establish why the reporter sees the Windows dialog.

## #177: current lab check

The Windows-target scenario was retried on September 29. An initial attempt timed out during UI
startup; the retry connected and reached logon, but the target replaced the session before the
splitter could be dragged. NUnit reported **NotExecuted**, not passed. The saved connected-screen
image and result are in local ignored lab artifacts. The stale-pixel symptom remains untested on
a stable Windows session. Repairing that lab condition is the next maintainer action; sending
the reporter the same request again would not resolve this coverage gap.

## #179: options UX proposal for human review

Proposal only: **OK** validates and saves, then closes; **Apply** validates and saves while keeping
the current page open; **Cancel** discards changes since the last Apply and closes. If validation
fails, keep the dialog open at the affected setting and explain the error. Live theme previews
would need to revert on Cancel without reverting previously applied values.

Before implementation, maintainers and affected users should try three short flows: edit and
cancel, apply and continue editing, and fail validation then correct it. Ask whether they can predict
what persists; record disagreements. Preserve the current workflow until that review settles the
proposal. No UI change or public invitation was sent here.

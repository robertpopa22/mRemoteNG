---
name: mremoteng-verify
description: Verify an mRemoteNG candidate or a published revision. Use for build, tests, lab evidence, applicable code checks, or CI and nightly readback.
---

# Verify an mRemoteNG candidate or published revision

Read [CLAUDE.md](../../../CLAUDE.md) and [CHARTER.md](../../../CHARTER.md).
Use `/mremoteng-verify` in Claude Code or `$mremoteng-verify` in Codex.
Run commands from the root of the checkout being verified. By default, report local
health and the available CI and release evidence. When called from a fix or import,
verify the candidate first and do the publication readback only if it is shipped.
Keep source, the Git index and published state untouched.

## Local candidate

Record the base SHA, branch or detached HEAD and the candidate diff or clean
checkout. Check for existing WIP with `git --no-optional-locks status --short --branch`.
Use the candidate's isolated checkout for D11/D15 verification. For
verification-only work, check for the ignored local post-build hook before
building. If it is present, use an isolated copy of the requested source and
diff without the hook so the build cannot refresh the maintainer's copy.
Keep reports in a unique directory outside the checkout.

1. For product changes, inspect the actual diff against the security properties
   in the charter before building. Run `scripts/security-tripwire.sh` with Git
   Bash on the exact committed range or the exact candidate staged by the
   coordinator. The no-argument form reads the index. Confirm that the scanned
   paths and contents match the candidate. If verification-only work has
   neither a matching index nor a committed range, report the tripwire scope
   still unverified. A block requires human security review under the charter.
2. Run the full build, then the suite against its output in the same checkout.
   Capture each exit code immediately and keep the raw output.

   ```powershell
   pwsh -NoProfile -ExecutionPolicy Bypass -File ./build.ps1
   pwsh -NoProfile -ExecutionPolicy Bypass -File ./run-tests.ps1 -Headless -NoBuild
   ```

   After a failed build, report errors and skip the suite against stale binaries.
   Report compiler and analyzer warnings by rule, plus passed, failed and skipped
   tests where reported. A zero-test, phantom, crashed or incomplete run cannot
   satisfy the gate. The wrapper uses Git Bash. The manual describes the supported
   direct runner and the COM and WSL constraints.
3. Use [the lab procedure](../../../docs/LAB-GUEST.md) for the actual user path
   required by D11. Record the scenario, candidate, observations and artifacts.
   For a fix, compare the same path before and after. Record comparable timings
   for performance work under D13. For installer, upgrade or settings changes,
   check install, upgrade and preservation of existing settings and connections.
   Record live backend and reporter confirmation separately when available.

## Additional checks by scope

Use [FREE-VERIFICATION.md](../../../docs/FREE-VERIFICATION.md) for tool commands,
current baseline dispositions, supported paths, reports and credit limits.

| Check | When to use and what to record |
| --- | --- |
| Semgrep CE | For authored C# changes in the scanner's product scope. Inspect JSON/SARIF, scanned and excluded paths, and parser diagnostics. Exit 0 is a complete clean scan, 10 is complete with findings, and scanner errors or missing results mean incomplete. Recheck known SQL dispositions when callers change. |
| Stryker.NET | For changes to the linked `MainWindowPlacement` source or pilot tests. Report killed, surviving, uncovered, ignored and compile-error counts from JSON. Score applies only to the pilot. |
| Pester | For the covered PowerShell orchestration or Sonar coverage work. Use the existing runner and record measured and missed lines. Read Sonar's server conditions separately. |
| CodeRabbit OSS / Greptile OSS | When a hosted review is in scope and an active, repository-scoped free route is verified. Read usage before and after. Follow the credit reserve and reconcile debits before retrying. Never enable paid usage, expand access or create a review-only PR. Record the actual review on the selected diff, or why it could not run. |
| Qodo | Paused per the verification guide. Omit it from active gates and leave its historical helper unused. |

Keep scan scope, thresholds and rule coverage intact when investigating a red
result. Existing dispositions apply to the reviewed code and evidence. Reassess
them if the affected source changed.

## CI and nightly readback

Set `$revision` to the requested full commit SHA. For default health,
distinguish local HEAD from the published fork tip if they differ. Identify
the run for each workflow and the actual nightly tag:

```powershell
gh run list --repo robertpopa22/mRemoteNG --commit $revision --limit 30 --json databaseId,workflowName,status,conclusion,headSha,url
gh release view nightly --repo robertpopa22/mRemoteNG --json name,tagName,targetCommitish,assets,body,url
git ls-remote origin 'refs/tags/nightly' 'refs/tags/nightly^{}'
```

- Read PR Validation, Nightly Build, CodeQL, SonarCloud and Secret Scanning
  (Gitleaks) individually. Record missing, skipped, running and failed runs as
  such. For a failed run, inspect `gh run view <id> --repo robertpopa22/mRemoteNG --log-failed`.
  PR Validation build and startup results are distinct from the Nightly suite.
- For Sonar, match its analysis revision to the SHA, then read the Quality Gate
  and conditions for that analysis. If only the latest project gate is available
  and its revision cannot be matched, record the gate for the requested SHA as
  unverified. Preserve thresholds and exclusions. Document a false positive
  separately with source evidence.
- For CodeQL and Gitleaks, read the analysis or job outcome for the requested SHA.
  Keep the reviewed historical Gitleaks fingerprints in
  [the existing list](../../../.gitleaksignore). Investigate any new finding and
  redact sensitive values in the report.
- For nightly, resolve the actual tag to a commit and match the release text and
  available manifest to the build. Check expected package names, architecture, size
  and published hashes or digests. When verifying package integrity, download the
  assets outside the checkout, hash them and compare to those digests. Record a
  missing asset, mismatched SHA or unverified digest explicitly.

## Report

Name the checkout and SHA or candidate diff. Record each applicable check with its
outcome, scope, count and link or report path. Explicitly label incomplete,
unavailable and inapplicable checks. In a default health report, include dirty
paths and the open fork issue numbers, including maintainer reports.

**#ponytail.** Reuse the canonical runners and their existing reports. Select
additional checks by affected scope and state any gap in the evidence.

# Free verification tools

These checks complement the full build, test suite, UI checks and existing CI. They
do not replace them. Use local report directories outside the checkout. Do not
enable paid plans, usage credits, automatic top-ups or paid scanning add-ons.

## Credit budget and stop conditions

Before and after each hosted review, read the provider's current usage page and
record the time, balance, expiry/reset date, selected review mode and measured
delta in the local operations report. A configured app is not permission to spend.
Do not assume a failed request or retry costs nothing. Do not retry until its
usage has been reconciled. Stop if entitlement, price or remaining quota is unclear.

| Provider | Free budget | Operating limit |
|---|---|---|
| Greptile | Confirmed OSS grant: 100 credits/month. At setup the displayed balance was 100 **trial** credits expiring 2026-11-30; do not add an unseen OSS grant to that balance | Use at most 80 verified available credits per cycle; reserve 20. Start with Base mode and one bounded review; inspect its actual debit before another. No Plus, Apex, TREX or automatic retriggers without checking their cost and the reserve |
| CodeRabbit | Public-repository review entitlement; provider-specific rate limits, not a verified numerical credit grant | Read Usage before/after a review. No `--use-credits`, paid usage-based reviews, Deep Scan, Coding Agent or trial conversion. If the allowance is unavailable, stop the hosted review and continue local checks |
| Semgrep CE | No hosted credits | Use the pinned local CE command only; no cloud login, Pro engine or AI triage |
| Stryker.NET | No hosted credits | Local pilot and HTML/JSON reports only; no dashboard subscription |
| Qodo | Paused; no usable free entitlement verified | Do not invoke or reactivate paid reviews |

The budget applies to the **shared organization balance**, so usage by another
repository must be deducted too. Never infer the balance from this document or
from the absence of invoices. Do not add payment methods, buy credits, enable
automatic top-ups or replace a free quota with a paid subscription. Included-trial
credits expiring do not authorize conversion. These checks are performed when
the procedure runs; no unattended credit-monitoring service is installed.

## Semgrep Community Edition

Requires PowerShell 7 and [uv](https://docs.astral.sh/uv/). Run from any directory:

```powershell
pwsh -NoProfile -File ./scripts/Invoke-Semgrep.ps1 -OutputDirectory "$env:TEMP/mRemoteNG-semgrep"
```

The script pins Semgrep 1.185.0 and 15 upstream C# rule files at commit
`a5ad1714f00c9f7fde980cab0ea1d6dc6d414fd2`. It downloads the rules and their license
for local use; they are not redistributed under this repository's GPL license.
See the [Semgrep Rules License](https://semgrep.dev/legal/rules-license/).
The scanner uses the CE engine, no service login, an isolated settings file,
metrics disabled and no version check. Package/rule downloads still use the network;
source and scan reports are not uploaded by this command.

Scope: authored C# in `mRemoteNG`, `ExternalConnectors` and `mRemoteNG.Plugins`.
The generated 7.9 MB NSwag `ExternalConnectors/DSS/SecretServerRestClient.cs` is
excluded explicitly: forcing it into the scan caused rule timeouts. Its authored
interface remains in scope. CE's C# analysis has language and single-function
limits; this is not whole-program data-flow analysis.

Each run creates a unique directory with `semgrep.json` and `semgrep.sarif`.
Exit **0** means a complete scan without findings; **10** means a complete scan
with findings. Nonzero scanner exits are preserved before interpreting findings,
so a partial scan cannot become a clean result.

Baseline on 16 November 2026: **727 files, 15 rules, 3 findings**, approximately
99.4% parsed lines, **23 PartialParsing diagnostics, exit 3**, no timeouts.
The findings concern SQL passthrough in `MSSqlDatabaseConnector.cs:28` and
`OdbcDatabaseConnector.cs:34`, and path construction in
`PuttySessionsFileProvider.cs:78`. They require source-level triage; no suppression
or product fix is implied. Keep this check advisory until parser gaps and findings
have a recorded disposition. It is not an all-green CI gate.

## Stryker.NET mutation pilot

Requires the .NET 10 SDK and PowerShell. No hosted account or dashboard is used:

```powershell
pwsh -NoProfile -File ./scripts/run-mutation-tests.ps1 -OutputDirectory "$env:TEMP/mRemoteNG-mutation"
```

The local tool manifest pins Stryker.NET 5.1.0. The pilot under `tests/mutation`
links the real `MainWindowPlacement.cs` and its seven existing NUnit tests rather
than copying product behavior. The separate projects avoid COM build constraints
and the application suite's WinForms startup fixture. This does **not** measure
the entire application, its UI or RDP behavior. No production source or existing
test is changed by the integration.

Read `reports/mutation-report.html` and `reports/mutation-report.json` under the
output directory. The initial result was **62.07%**: 65 generated mutants,
36 killed, 21 survived, 1 uncovered, 1 compile error and 6 ignored; no timeouts.
Surviving mutations identify test gaps for follow-up. A successful tool exit means
the pilot ran, not that all mutants were killed. No project-wide quality threshold
has been lowered or replaced by this pilot.

## CodeRabbit OSS

The [public-repository plan](https://www.coderabbit.ai/oss) has no minimum-star
requirement. `.coderabbit.yaml` configures review behavior, disables automatic
approvals and code-writing finishing touches, and keeps early-access features off.
Account billing and usage-credit controls are separate from this YAML file.

Verified 16 November 2026: the GitHub App is installed **only** on this repository
and the repository is listed in CodeRabbit review settings. The provider started
an automatic Advanced trial through 30 November; the billing page explicitly
says it moves to Free and nothing is charged. Paid usage products are disabled
(zero active products, $0 usage), no payment was submitted and no invoice exists.
The same page's estimated-plan-price panel displays $90; that display is not a
paid subscription or an OSS acceptance receipt. Do not activate an upgrade.

CLI 0.9.5 is installed locally, but its OAuth login requests `public_repo` access
across the account in both regions. That broader grant was declined: the
maintainer authorized only this repository. CLI authentication/review is therefore
**not active**. Use the commands below only after a suitably scoped authentication
method has been separately verified. No PR or successful AI review is claimed.

The fork uses `main` directly: do not create an artificial branch or PR just to
invoke a reviewer. After installing the official CLI and authenticating the
correct account, review a concrete commit range without creating a PR:

```powershell
coderabbit auth status
coderabbit review --agent --committed --base-commit <reviewed-base-sha>
```

For tracked work in progress use `--uncommitted` instead. Do not add
`--include-untracked` without checking every file; local operations material must
never be sent. Never add `--use-credits`. Confirm the included/free allowance and
record authentication, rate-limit or service errors as such, not as a passed
review. Repository configuration alone does not prove service activation or a
completed review.

## Greptile OSS

The [OSS application](https://www.greptile.com/pricing/open-source) is for the
public GPL-2.0 repository `robertpopa22/mRemoteNG`. Submission, vendor acceptance,
app installation and a completed review are separate evidence. Do not substitute
a paid trial or Pro subscription for OSS acceptance. Keep access scoped to this
repository and do not create PRs merely to exercise the service.

**Accepted on 16 November 2026:** Greptile confirmed GPL-2.0/public-repository
eligibility at 80 stars, enrolled the repository, and grants **100 credits each
month** to the organization's shared balance. The repository is enabled and
marked Open source, with **0 completed reviews**. This entitlement is separate
from the provider's initial 100-credit trial through 30 November. No payment
method or Pro upgrade was added. Automatic approvals remain disabled and optional
learning from usage was turned off. Both GitHub App grants are restricted to this
one repository; the maintainer explicitly approved their displayed code/workflow
write permissions.

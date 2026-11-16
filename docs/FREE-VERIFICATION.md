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
Follow-up on the same day: **727 files, 15 rules, 2 findings**. The KiTTY path
finding is fixed; the two SQL findings have the disposition below. The same
23 PartialParsing diagnostics and strict exit 3 remain. No rules, findings or
parser errors were suppressed. This is still a partial, advisory scan, not an
all-green CI gate.

### Finding dispositions

- **KiTTY path construction — fixed.** `GetSession` now accepts only a raw leaf
  filename before accessing the Sessions directory. Rooted, traversal and nested
  paths are rejected rather than silently redirected to another filename. A
  source-linked Windows probe with real temporary files accepted all five invalid
  paths before the change and rejected all five afterwards. NUnit regression
  coverage also preserves literal percent-encoded filenames: display decoding
  happens only after reading the file. The usual UI enumeration already supplies
  leaf names; the regression exercises direct calls to the public provider.
  This addresses path syntax, not filesystem links or directory permissions.
  A focused repeat of the same 15 rules on this file passed with no findings or
  parser diagnostics; the full scan's parser limitations above remain unchanged.
- **`MSSqlDatabaseConnector.cs:28` and `OdbcDatabaseConnector.cs:34` — false
  positives for the current callers.** All 43 `DbCommand` call sites in 17 files
  were traced. These factories construct commands before callers bind their
  parameters; the pinned CE rule taints a nonliteral string parameter inside the
  factory without following the callers. Commands use literal SQL, code-defined
  schema identifiers or bound application values. The only user-supplied SQL
  identifier is the database name in `DatabaseConnectionTester.TryCreateDatabaseAsync`:
  its validation precedes connection and command creation and prevents bracket or
  statement-delimiter injection. `DatabaseCreationValidationTests` covers those
  payloads. The regex's end anchor may permit a final newline; this does not allow
  an identifier breakout and is not described as strict whole-string matching.
  `SqlDatabaseMetaDataRetriever` uses validated fixed table names and schema
  definitions from `DataTableSerializer`; migration batches are source constants.
  `SqlSafeUpdateHelper` receives three literal DELETE statements, and
  `SqlMigrationHelper` rewrites source-defined batches with bound row values.
  Keep both raw findings visible and repeat this caller review when SQL
  construction changes. This is source review, not live database acceptance.

## Stryker.NET mutation pilot

Requires the .NET 10 SDK and PowerShell. No hosted account or dashboard is used:

```powershell
pwsh -NoProfile -File ./scripts/run-mutation-tests.ps1 -OutputDirectory "$env:TEMP/mRemoteNG-mutation"
```

The local tool manifest pins Stryker.NET 5.1.0. The pilot under `tests/mutation`
links the real `MainWindowPlacement.cs` and its NUnit tests rather
than copying product behavior. The separate projects avoid COM build constraints
and the application suite's WinForms startup fixture. This does **not** measure
the entire application, its UI or RDP behavior. No production source or existing
test was changed by the initial integration.

Read `reports/mutation-report.html` and `reports/mutation-report.json` under the
output directory. The initial result was **62.07%**: 65 generated mutants,
36 killed, 21 survived, 1 uncovered, 1 compile error and 6 ignored; no timeouts.
The follow-up adds nine behavioral cases (16 total): stacked monitors, largest
overlap, overlap/distance ties, oversized windows and nearest-screen selection on
both axes, plus a null-screen-list assertion. That raised the score to **94.83%** on the
same 65 mutants: **55 killed, 3 survived, 0 uncovered, 1 compile error, 6 ignored**;
no timeouts. Product logic, mutation scope, thresholds and exclusions are unchanged.
The three survivors changed comparisons in `Clamp` at equality; each branch returned
the same bound at that point. The next follow-up replaced that handwritten clamp
with `Math.Clamp(value, min, Math.Max(min, max))`, preserving the oversized-window
behavior, and added an exact-fit boundary case (17 cases total).
The pilot now scores **100.00%: 48 of 48 scored mutants killed**, with 54 generated,
5 ignored by the existing default block filter and 1 compile error. No survivors,
uncovered mutants or timeouts remain. Fewer mutants are generated because the
handwritten branches no longer exist; this is not a like-for-like 65-mutant run.
Scope, thresholds and exclusions are unchanged. The report's source was compared
with the product file. This result still covers only the placement pilot.

## Sonar coverage verification

Keep the existing quality thresholds and product exclusions unchanged. Resolve
each finding from its source; an executable name such as `op.exe` is not a secret.
Record a confirmed false positive on the individual server issue, with evidence,
instead of hiding it by renaming code or disabling a rule.

The Sonar workflow collects Visual Studio XML from the same grouped test runner
as nightly, including the isolated UI tests. It also imports measured Pester
coverage for `lab-msi-edition.ps1`:

```powershell
Install-Module -Name Pester -RequiredVersion 6.2.0 -Repository PSGallery -Scope CurrentUser -Force
./scripts/Invoke-PowerShellTests.ps1 -OutputDirectory "$env:TEMP/mRemoteNG-powershell-tests"
```

The fixture lives in the test project. Its seven tests mock the VM boundary and
check host orchestration, failure reporting and cleanup. They never execute the
guest installer blocks. Pester measured 85/170 lines; the generic Sonar XML keeps
every measured missed line as uncovered, and its values were checked against the
raw Cobertura report. Reports must be written outside the checkout.

The workflow waits for the server Quality Gate and fails when that gate fails.
An upload, local coverage estimate or successful build alone is not a green gate;
read back the analysis revision, conditions and remaining findings after push.

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

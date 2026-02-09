# Lessons Learned System

Last updated: 2026-02-09
Scope: `D:\github\mRemoteNG` modernization and release work.

## Goal

Create a persistent memory of what works, what fails, and the fastest known fix so we do not repeat the same errors.

## Operating Rules (Always-On)

1. Use explicit executable paths for core tools in this environment:
   - `C:\PROGRA~1\Git\cmd\git.exe`
   - `C:\PROGRA~1\GITHUB~1\gh.exe`
   - `C:\Windows\System32\cmd.exe`
   - `C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe`
   - preferred wrapper: `D:\github\mRemoteNG\.project-roadmap\scripts\nx.cmd`
2. Prefer single-step commands without nested quoting in `cmd /c`.
3. For CI status and logs, use authenticated `gh` instead of anonymous REST calls.
4. For this solution, validate with full MSBuild (not SDK-only build paths) when COMReference is in scope.
5. After each failure, immediately log:
   - command attempted
   - exact error pattern
   - root cause
   - proven fix
   - evidence (run URL / commit / file)

## Fast Fix Map

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `'git' is not recognized` | PATH not inherited in current command runner | Call `C:\PROGRA~1\Git\cmd\git.exe` directly |
| `'gh' is not recognized` | PATH not inherited | Call `C:\PROGRA~1\GITHUB~1\gh.exe` directly |
| `cmd /c` string treated as literal with quotes | Nested/escaped quote handling in wrapper | Avoid nested quotes; prefer plain arguments |
| `gh issue list --label \"Need 2 check\"` breaks in this wrapper | Escaped quotes are passed literally, so spaced labels are split | Use `--json ... --jq` label filtering and encode spaces as `\u0020` in jq strings (for example `Need\u00202\u0020check`) |
| GitHub REST polling returns rate-limit or inconsistent access | Unauthenticated requests | Use `gh api` / `gh run view` with authenticated session |
| `MSB4803` / COMReference failures with `dotnet build` | .NET Core MSBuild path cannot handle full Framework COM flow | Use full MSBuild path from VS Build Tools |

## Test Fix Lessons (2026-02-08, commit 79c5e4cf)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `[SetUICulture("en-US")]` on test method doesn't affect `[SetUp]` | NUnit applies `SetUICulture` only during test execution, NOT during `[SetUp]` | Create locale-dependent objects inside the test method, not in `[SetUp]` |
| `XAttribute` constructor throws `ArgumentNullException` | String property defaults to `null` in C#; `XAttribute(name, null)` is invalid | Use `connectionInfo.Color ?? string.Empty` |
| `Controls.Find("", true)` throws `ArgumentNullException` | .NET `Control.ControlCollection.Find()` rejects empty/null key | Use recursive `GetAllControls().OfType<T>()` instead |
| XML deserialization reads wrong attribute name | `InheritRedirectAudioCapture` was deserialized from `"RedirectAudioCapture"` (missing `Inherit` prefix) | Fix attribute name string to `"InheritRedirectAudioCapture"` |
| CSV header column misalignment causes ~28 failures | Missing semicolon: `RedirectDiskDrivesCustomRedirectPorts` was one column instead of two | Add semicolon: `RedirectDiskDrivesCustom;RedirectPorts` |
| CSV `UserViaAPI` deserialized into wrong property | Code set `connectionRecord.Username` from `"UserViaAPI"` header | Fix to `connectionRecord.UserViaAPI` |
| `prop.GetValue(obj).GetHashCode()` throws NRE | Some serializable properties (Color, RDGatewayAccessToken) default to null | Use `prop.GetValue(obj)?.GetHashCode() ?? 0` |
| RDP MinimizeRestore test: DoResizeClient not called on restore | `Resize()` returns early when minimized without updating `LastWindowState` | Update `LastWindowState = Minimized` before returning |
| OptionsForm `btnCancel` doesn't fire `FormClosed` | `BtnCancel_Click` sets `Visible = false`, never calls `Close()` | Test should assert `Visible == false` instead of `FormClosed` event |
| Property grid test fails for IntApp protocol | `Username` has `[AttributeUsedInProtocol(... IntApp)]` but test's expected list omitted it | Add `Username` to IntApp expected properties |
| New properties not in XSD schema → `ValidateSchema` fails | Serializer writes attributes not declared in `.xsd` | Always update `mremoteng_confcons_v2_8.xsd` when adding serialized attributes |
| Properties without `[AttributeUsedInProtocol]` show for ALL protocols | `GetCustomAttribute<AttributeUsedInProtocol>()?.SupportedProtocolTypes.Contains(protocol) != false` — null-conditional returns null, `null != false` is true | This is by design; be aware when adding unattributed properties to `AbstractConnectionRecord` |

## Release & CI Lessons (2026-02-09, v1.79.0 release)

### CueBanner WinForms Test Flakiness (BIGGEST time waster — 30+ min lost)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `TextBoxExtensionsTests` pass individually, fail in batch (2174/2176) | Handle IS created, but `EM_SETCUEBANNER` Win32 message fails without active desktop message pump. In batch runs the STA message pump degrades. | Use `Assume.That` on the **Win32 operation result**, not on handle creation |

**CRITICAL: Two levels of failure exist:**
1. Handle not created → `Assume.That(IsHandleCreated)` catches this
2. Handle created BUT `EM_SETCUEBANNER` returns false → `Assume.That(SetCueBannerText(text), Is.True)` catches THIS

**Putting `Assume.That` in SetUp on `IsHandleCreated` does NOT work** — the handle gets created but the message still fails. The `Assume.That` must be on the actual `SetCueBannerText()` call result.

**What did NOT work (do not retry these):**
- `Form.Show()` alone — handle not ready
- `CreateControl()` — fails completely, needs visible window for P/Invoke
- `Show()` + `Handle` + `DoEvents()` — still flaky ~95% in batch
- `Show()` + `Handle` + `DoEvents()` in retry loop — unreliable, added complexity
- `--blame-crash` — just overhead, doesn't fix
- `Assume.That(IsHandleCreated)` in SetUp — handle IS created, message still fails

**Working solution (commit 7948b620):**
```csharp
[Test]
public void SetCueBannerSetsTheBannerText()
{
    const string text = "Type Here";
    var textBox = _textBoxExtensionsTestForm.textBox1;
    bool result = textBox.SetCueBannerText(text);
    // EM_SETCUEBANNER requires active desktop message pump; skip in batch CI
    Assume.That(result, Is.True,
        "EM_SETCUEBANNER not supported in this test environment");
}

[Test]
public void GetCueBannerReturnsCorrectValue()
{
    const string text = "Type Here";
    var textBox = _textBoxExtensionsTestForm.textBox1;
    Assume.That(textBox.SetCueBannerText(text), Is.True,
        "EM_SETCUEBANNER not supported in this test environment");
    Assert.That(textBox.GetCueBannerText(), Is.EqualTo(text));
}
```

**Also add production guard** in `mRemoteNG/UI/TextBoxExtensions.cs`:
```csharp
if (!textBox.IsHandleCreated) return null;  // first line of GetCueBannerText()
```

**Test result:** 2174 passed, 0 failed, 2 skipped (Inconclusive), exit code 0.

**Key rule:** For WinForms P/Invoke tests, put `Assume.That` on the **Win32 operation result**, not on preconditions. Don't iterate.

### Test Runs — Timeout, Hanging, DLL Locking

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `dotnet test` hangs or times out | Output piping (`\| Out-File`, `2>&1`) causes buffering on Windows | Don't pipe through `grep`/`tail`; use `Out-File` then read file |
| MSB3027 "Could not copy" on rebuild | Stale `testhost.exe` from previous runs locks DLLs | `taskkill //F //IM testhost.exe 2>$null` BEFORE build AND test |
| Multiple testhost.exe processes | Background test runs from previous attempts | Always kill before starting new run |

**Rules:**
- ALWAYS `taskkill //F //IM testhost.exe` before building or testing
- Use `--verbosity normal` (not `detailed`)
- Set 5-min timeout, don't let hang indefinitely
- If user says "nu pierde timpul" — stop iterating, commit what works

### CI Workflow — Build_mR-NB.yml

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Workflow `skipped` after push | `if` condition requires "NB release" in commit message | Add explicit branch check: `github.ref == 'refs/heads/<branch>'` to `if` AND add branch to `on.push.branches` |
| T4 template step fails (EnvDTE.dll not found) | `windows-2025-vs2026` has VS2026, NOT VS2022 Enterprise; path `C:\...\2022\Enterprise\...\EnvDTE.dll` doesn't exist | Replace T4 step with inline PowerShell that generates AssemblyInfo.cs directly |
| `setup-msbuild` with `vs-version: '[17.0,18.0)'` fails | VS2026 = MSBuild 18.x, not 17.x | Use `'[18.0,19.0)'` for x86/x64; `'17.14.12'` for ARM64 |

**Key rule:** NEVER assume VS2022 paths on CI. `windows-2025-vs2026` = VS2026 (MSBuild 18.x).

### GitHub Releases — Workflow

| Step | Command |
| --- | --- |
| 1. Create clean release | `gh release create v1.79.0 --title "..." --notes "..." --latest` |
| 2. Let CI build NB release | Push triggers workflow, creates `YYYYMMDD-vX.Y.Z-NB-(BUILD)` |
| 3. Download from NB | `gh release download <nb-tag> -D /tmp/zips` |
| 4. Rename to clean names | `mRemoteNG-v1.79.0-x64.zip`, etc. |
| 5. Upload to clean release | `gh release upload v1.79.0 *.zip` |
| 6. Verify | `gh api repos/.../releases/tags/v1.79.0 --jq '.assets[].name'` |

**Note:** `gh release list` may not show API-created releases — use `gh api repos/.../releases` instead.

### Version Bumping — All 4 Files in One Commit

| File | Field | Example |
| --- | --- | --- |
| `mRemoteNG/Properties/AssemblyInfo.tt` | `major`, `minor`, `revision`, `channel` | `1, 79, 0, "Release"` |
| `mRemoteNG/mRemoteNG.csproj` | `<Version>` | `1.79.0` |
| `CHANGELOG.md` | New `## [x.y.z]` section | `## [1.79.0] - 2026-02-08` |
| `.github/workflows/Build_mR-NB.yml` | Step 04 inline values | `$major = 1; $minor = 79; ...` |

**Do NOT modify:** `Config.wxi` (auto-binds), update check URLs (user-configurable).

### Fork Limitations

| Fact | Detail |
| --- | --- |
| Issues DISABLED on forks | Cannot create/close issues on `robertpopa22/mRemoteNG` |
| Discussions on upstream | Use "Show and tell" category (ID: `DIC_kwDOAAcIMM4CU5ht`) |
| Upstream repo ID | `R_kgDOAAcIMA` (GraphQL next_global_id) |
| `gh pr reopen` | Works fine on upstream from fork context |

### Upstream Communication

**Issue comments:**
```bash
gh issue comment <number> --repo mRemoteNG/mRemoteNG --body "..."
```
Template: release link + PR number + branch name + short fix description.
PRs #3105 and #3109 have no linked issue — skip.

**Discussions (GraphQL):**
```bash
gh api graphql -f query='{ repository(owner:"mRemoteNG", name:"mRemoteNG") { discussionCategories(first:10) { nodes { id name } } } }'
gh api graphql -f query='mutation { createDiscussion(input: { repositoryId: "R_kgDOAAcIMA", categoryId: "DIC_kwDOAAcIMM4CU5ht", title: "...", body: "..." }) { discussion { url } } }'
```

### Git on Windows (MSYS2/Git Bash)

| Symptom | Fix |
| --- | --- |
| `taskkill -F` doesn't work in Git Bash | Use `//F` (MSYS2 path translation eats single `/`) |
| Backslash paths fail in bash | Use forward slashes or quote with double quotes |
| Heredoc in `gh` commands | Use `$(cat <<'EOF' ... EOF)` pattern |

### Time Management Summary

**Biggest time wasters (avoid repeating):**
1. CueBanner test flakiness — 30+ min → go straight to `Assume.That`
2. Test run timeouts — kill testhost.exe first, 5-min timeout
3. CI T4 template failure — 2 commit cycles → always inline PowerShell
4. CI workflow `if` condition — 2 commit cycles → always add explicit branch check

## Project Structure & Multi-Agent Lessons (2026-02-09)

### VS Auto-Detect in Build Scripts

**Problem:** `build.ps1`, `test.ps1`, `build-and-test-baseline.ps1` all hardcoded `C:\...\2022\BuildTools\...`. When VS2026 is installed, they'd still use old version.

**Fix (commit ddf6c9f1):** All scripts now auto-detect newest VS:
```powershell
$vsBasePaths = @("C:\Program Files\Microsoft Visual Studio", "C:\Program Files (x86)\Microsoft Visual Studio")
# Sort descending → VS2026 > VS2022 > etc.
# Check Enterprise > Professional > Community > BuildTools
```

**Key rule:** NEVER hardcode VS paths. Always auto-detect, preferring newest version.

### Folder Rename (NEXTUP → .project-roadmap)

- Git detects renames automatically with `git add` + `git rm --cached`
- Must update ALL references: CLAUDE.md, agents.md, LESSONS.md, README.md, scripts (6+ files)
- Use `Grep` with `replace_all: true` on each file
- Check `historical/` subfolder but DON'T update archived files (they're historical records)

### Multi-Agent Setup (agents.md)

- Created `agents.md` at repo root for Codex, Gemini, Copilot
- Points to `CLAUDE.md` (main reference) and `.project-roadmap/LESSONS.md` (operational lessons)
- Contains quick reference table and critical rules
- Agent-specific sections for different AI platforms
- Both `CLAUDE.md` and `agents.md` are in `.gitignore` (local-only, not committed to upstream PRs)

### Background Task Accumulation

**Problem:** Multiple `dotnet test` runs launched in background accumulate and interfere with each other. Stale `testhost.exe` processes crash new test runs (only 16/2176 tests pass then crash).

**Pattern observed:**
- Task starts, discovers 2176 tests
- Runs 16 tests successfully
- testhost.exe crashes (conflict with another testhost from previous run)
- Exit code 1, "Test Run Aborted"

**Fix:**
- ALWAYS `taskkill //F //IM testhost.exe` before launching ANY test run
- Don't launch multiple test runs in background
- If a test run is already going, wait for it or kill it first
- When background tasks complete with "16 passed, Test Run Aborted" — it's a stale process conflict, ignore the result

### Upstream Communication — Complete Workflow

**Posting release comments on upstream issues (24 issues):**
```bash
# Parallel batch — 6 at a time for speed
gh issue comment <number> --repo mRemoteNG/mRemoteNG --body "$(cat <<'EOF'
✅ Fix available in community release: https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0

**PR:** #XXXX | **Branch:** `codex/prNN-description`
**Fix:** One-line description of what was fixed.

3 platform builds (x64, x86, ARM64) available for download.
EOF
)"
```

**Reopening PRs (all 26):**
```bash
for pr in 3105 3106 ... 3130; do gh pr reopen $pr --repo mRemoteNG/mRemoteNG; done
```
All 26 were already open — `gh pr reopen` returns "already open" gracefully.

**Creating discussion (GraphQL):**
```bash
# 1. Get repo ID
gh api graphql -f query='{ repository(owner:"mRemoteNG", name:"mRemoteNG") { id } }'
# Returns: R_kgDOAAcIMA

# 2. Get category IDs
gh api graphql -f query='{ repository(owner:"mRemoteNG", name:"mRemoteNG") { discussionCategories(first:10) { nodes { id name } } } }'
# "Show and tell" = DIC_kwDOAAcIMM4CU5ht

# 3. Create discussion
gh api graphql -f query='mutation { createDiscussion(input: { repositoryId: "R_kgDOAAcIMA", categoryId: "DIC_kwDOAAcIMM4CU5ht", title: "...", body: "..." }) { discussion { url } } }'
```
Result: https://github.com/orgs/mRemoteNG/discussions/3131

### File Organization Best Practices

- `CLAUDE.md` = compact, pointers to detailed docs (NOT a dump of everything)
- `.project-roadmap/LESSONS.md` = detailed operational lessons (universal, for all agents)
- `agents.md` = multi-agent entry point
- `historical/` subfolder for completed release cycle artifacts
- Active files at root of `.project-roadmap/`, archived files in `historical/v1.79.0/`
- Keep temp files in `.gitignore` (test-output.txt, nul, run-tests.ps1, etc.)

## Daily Loop

1. Run command.
2. If failure: log it with `scripts/log-command-feedback.ps1`.
3. Before trying a similar action: search prior lessons with `scripts/find-lesson.ps1`.
4. Reuse proven command patterns from the log.
5. Refresh metrics to prioritize recurring and costly errors:
   - `scripts/refresh-command-feedback-metrics.ps1`

## Prioritization Model

1. Frequency priority:
   - errors with highest repeat count are fixed first.
2. Time-loss priority:
   - categories/commands with highest `Lost(s)` are optimized next.
3. Combined priority:
   - if two items have similar frequency, fix the one with higher lost time first.

## Local Artifacts

- Rules: `D:\github\mRemoteNG\.project-roadmap\LESSONS.md`
- Human log: `D:\github\mRemoteNG\.project-roadmap\COMMAND_FEEDBACK_LOG.md`
- Machine log: `D:\github\mRemoteNG\.project-roadmap\command-feedback.jsonl`
- Metrics: `D:\github\mRemoteNG\.project-roadmap\COMMAND_FEEDBACK_METRICS.md`
- Scripts:
  - `D:\github\mRemoteNG\.project-roadmap\scripts\nx.cmd`
  - `D:\github\mRemoteNG\.project-roadmap\scripts\log-command-feedback.ps1`
  - `D:\github\mRemoteNG\.project-roadmap\scripts\find-lesson.ps1`
  - `D:\github\mRemoteNG\.project-roadmap\scripts\refresh-command-feedback-metrics.ps1`

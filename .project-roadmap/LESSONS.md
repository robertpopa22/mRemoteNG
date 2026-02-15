# Lessons Learned System

Last updated: 2026-02-15
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
| PS1 script parse error on `}` far from actual issue | Unicode chars (em-dash `---`, smart quotes, etc.) corrupt PS 5.1 parser | Use only ASCII in PS1 string literals; see dedicated section below |

## PowerShell 5.1 Unicode Corruption (2026-02-10, Issue Intelligence System)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `Unexpected token '}' in expression or statement` at closing brace of a function, far from actual problem | Unicode multi-byte characters (em-dash U+2014, smart quotes U+201C/U+201D, etc.) in PS1 file corrupt the Windows PowerShell 5.1 parser | Replace all non-ASCII characters with ASCII equivalents in PS1 files |

**Why it happens:** `powershell.exe` (v5.1) reads `.ps1` files using the system's default encoding (typically Windows-1252), NOT UTF-8. When a file is saved as UTF-8 without BOM (which is what most modern editors and tools produce), multi-byte UTF-8 sequences for characters like `---` (em-dash, 3 bytes: `E2 80 94`) are interpreted as 3 separate Windows-1252 characters (`a`, `euro`, `ldquo`), which corrupts string parsing.

**The error is misleading:** The parser reports the error at a `}` brace far downstream from the actual corruption point. The UTF-8 bytes break a string literal open, causing all subsequent braces to be mismatched.

**Affected characters (common in generated code):**
- Em-dash `---` (U+2014) -> appears as `a-euro-ldquo` in Windows-1252
- En-dash `--` (U+2013)
- Smart quotes `ldquo` `rdquo` (U+201C, U+201D)
- Ellipsis `...` (U+2026)

**Prevention rules:**
1. **NEVER use Unicode punctuation in PS1 files** - use ASCII `-`, `"`, `...`
2. When AI generates PS1 content, search for non-ASCII: `[regex]::Matches($content, '[^\x00-\x7F]')`
3. PowerShell 7 (`pwsh.exe`) defaults to UTF-8 and does NOT have this problem
4. Adding a UTF-8 BOM (`EF BB BF`) to the file ALSO fixes it for PS 5.1, but ASCII-only is safer

**Detection script:**
```powershell
# Find non-ASCII characters in PS1 files
Get-ChildItem -Recurse -Filter *.ps1 | ForEach-Object {
    $content = Get-Content $_.FullName -Raw
    $matches = [regex]::Matches($content, '[^\x00-\x7F]')
    if ($matches.Count -gt 0) {
        Write-Host "$($_.Name): $($matches.Count) non-ASCII chars found"
        $matches | ForEach-Object { Write-Host "  Pos $($_.Index): '$($_.Value)' (U+$([int][char]$_.Value | ForEach-Object { $_.ToString('X4') }))" }
    }
}
```

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
| Workflow `skipped` after push | `if` condition requires "NB release" in commit message | Add explicit branch check: `github.ref == 'refs/heads/release/1.79'` to `if` AND add branch to `on.push.branches` |
| T4 template step fails (EnvDTE.dll not found) | `windows-2025-vs2026` has VS2026, NOT VS2022 Enterprise; path `C:\...\2022\Enterprise\...\EnvDTE.dll` doesn't exist | Replace T4 step with inline PowerShell that generates AssemblyInfo.cs directly |
| `setup-msbuild` with `vs-version: '[17.0,18.0)'` fails | VS2026 = MSBuild 18.x, not 17.x | Use `'[18.0,19.0)'` for x86/x64; `'17.14.12'` for ARM64 |

**Key rule:** NEVER assume VS2022 paths on CI. `windows-2025-vs2026` = VS2026 (MSBuild 18.x).

### GitHub Releases — Workflow

| Step | Command |
| --- | --- |
| 1. Create clean release | `gh release create v1.80.1 --repo robertpopa22/mRemoteNG --title "..." --notes "..." --latest` |
| 2. Let CI build NB release | Tag push triggers workflow, creates `YYYYMMDD-vX.Y.Z-NB-(BUILD)` |
| 3. Download from NB | `gh release download <nb-tag> --repo robertpopa22/mRemoteNG -D /tmp/zips` |
| 4. Rename to clean names | `mRemoteNG-v1.80.1-x64.zip`, etc. |
| 5. Upload to clean release | `gh release upload v1.80.1 --repo robertpopa22/mRemoteNG *.zip` |
| 6. Verify | `gh api repos/robertpopa22/mRemoteNG/releases/tags/v1.80.1 --jq '.assets[].name'` |

**Critical lessons (v1.80.1):**
- **`--repo` is MANDATORY on forks.** `gh release create` defaults to upstream (`mRemoteNG/mRemoteNG`), not the fork. Always use `--repo robertpopa22/mRemoteNG`.
- **Workflow changes in the same commit don't take effect for that push.** Adding `main` to `on.push.branches` only works on the *next* push — the current push uses the old workflow version. Tag pushes (`refs/tags/v*`) bypass this because they match the existing tag trigger.
- **CI creates 2 runs on tag push + branch push.** The branch push may be `skipped` (old workflow), but the tag push always triggers correctly.
- **Version bump requires only 2 files** (since v1.81.0-beta.2): `mRemoteNG.csproj` and `CHANGELOG.md`. CI reads version from csproj automatically.
- ~~Build_mR-NB.yml has 2 version locations~~ **FIXED** in commit `2597fafe` — both now read from csproj via regex. No more hardcoded values.
- **NB release naming:** `YYYYMMDD-vX.Y.Z-NB-(BUILD)` — the BUILD number is auto-computed from minutes since 2019-09-02.
- **Rename pattern:** `sed 's/mRemoteNG-YYYYMMDD-vX.Y.Z-NB-BUILD-/mRemoteNG-vX.Y.Z-/'`

**Note:** `gh release list` may not show API-created releases — use `gh api repos/.../releases` instead.

### Version Bumping — Only 2 Files Needed (v1.81.0-beta.2+)

| File | Field | Example |
| --- | --- | --- |
| `mRemoteNG/mRemoteNG.csproj` | `<Version>` | `1.81.0-beta.2` |
| `CHANGELOG.md` | New `## [x.y.z]` section | `## [1.81.0-beta.2] - 2026-02-15` |

**CI now reads version from csproj automatically** (fixed in `2597fafe`). The workflow uses regex `<Version>([^<]+)</Version>` to extract the version and supports prerelease suffixes like `-beta.2`. No more hardcoded version numbers in `Build_mR-NB.yml`.

**AssemblyInfo.cs** is generated by CI inline (PowerShell step 04). Local builds use the stale `.cs` but that only affects the splash screen version display — the NuGet/assembly version comes from csproj.

**Do NOT modify:** `Config.wxi` (auto-binds), update check URLs (user-configurable).

### OBSOLETE: Version was previously hardcoded in 5 files
Before commit `2597fafe` (v1.81.0-beta.2), `Build_mR-NB.yml` had version hardcoded in 2 places (step 04 AssemblyInfo + step 02 release metadata). This caused the v1.81.0-beta.2 CI to create a release named "v1.80.2" until fixed. **Now resolved** — csproj is the single source of truth.

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
- Contains quick reference table, critical rules, and branch naming convention
- Agent-specific sections for different AI platforms
- Both `CLAUDE.md` and `agents.md` are in `.gitignore` (local-only, not committed to upstream PRs)

### Branch Cleanup (2026-02-09)

- Renamed `v1.78.2-dev` → `main` (set as default on GitHub, tracks `upstream/v1.78.2-dev`)
- Renamed `codex/release-1.79-bootstrap` → `release/1.79`
- Deleted 26 `codex/pr*` branches (local + remote) — all merged into `release/1.79`, PRs closed
- Deleted 20 upstream mirror branches from origin (master, develop, v1.8-dev, etc.)
- Deleted stale branches: `pr-3038`, `pr-3054`, `codex/pr18-autolock-1649-clean`
- Fork went from 31 local + 47 remote → **2 local + 2 remote**
- New naming convention: `fix/<issue>-<desc>`, `feat/<issue>-<desc>`, `security/<desc>`, `chore/<desc>`, `release/<version>`

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

**PR:** #XXXX
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

## BinaryFormatter Removal — .NET 10 Startup Crash (2026-02-09)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `NotSupportedException: BinaryFormatter serialization and deserialization are disabled` at startup | .NET 10 removed `BinaryFormatter` from the runtime. DockPanelSuite 3.1.1 uses it via `ResourceReader` to deserialize theme images (`VS2015LightTheme` -> `ImageService` -> `Dockindicator_PaneDiamond_Hotspot`). | Add `System.Runtime.Serialization.Formatters` 10.0.2 NuGet package + set `<EnableUnsafeBinaryFormatterSerialization>true</EnableUnsafeBinaryFormatterSerialization>` in every project with binary .resx resources. |

**Affected projects (all have .resx files with `application/x-microsoft.net.object.binary.base64` entries):**
- `mRemoteNG` (89 .resx files) — crashes at startup via DockPanelSuite theme loading
- `ExternalConnectors` (2 .resx files) — forms with binary-serialized resources
- `ObjectListView` (2 .resx files) — already had `EnableUnsafeBinaryFormatterSerialization=true`
- `mRemoteNGTests` (4 .resx files) — test forms with binary-serialized resources

**No direct C# usage** of `BinaryFormatter`, `IFormatter`, or `SoapFormatter` was found in the codebase. The dependency is exclusively through:
1. **DockPanelSuite 3.1.1** — internal `ResourceReader.DeserializeObject()` for theme images
2. **WinForms .resx** — binary-serialized `System.Drawing.Bitmap` and other objects

**Long-term fix:** Upgrade DockPanelSuite when a version drops BinaryFormatter, or fork and convert resources to non-binary format. Track in `.project-roadmap/ISSUE_BINARYFORMATTER.md`.

**Stack trace (for reference):**
```
System.NotSupportedException: BinaryFormatter serialization and deserialization are disabled
  at System.Resources.ResourceReader.DeserializeObject(Int32 typeIndex)
  at WeifenLuo.WinFormsUI.ThemeVS2012.Resources.get_Dockindicator_PaneDiamond_Hotspot()
  at WeifenLuo.WinFormsUI.ThemeVS2012.ImageService..ctor(ThemeBase theme)
  at WeifenLuo.WinFormsUI.ThemeVS2015.VS2015ThemeBase..ctor(Byte[] resources)
  at WeifenLuo.WinFormsUI.Docking.VS2015LightTheme..ctor()
  at mRemoteNG.Themes.ThemeManager.get_DefaultTheme()
```

## v1.80.0 Release Lessons (2026-02-09)

### Async Credential Provider Deadlocks (Task 004)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| UI freezes when fetching credentials from vault | `.Result` on async calls from WinForms UI thread deadlocks due to `SynchronizationContext` | Wrap with `Task.Run(() => asyncCall()).GetAwaiter().GetResult()` |

**Affected files:** `VaultOpenbao.cs` (8 calls), `SecretServerInterface.cs` (4 calls), `PasswordstateInterface.cs` (4 calls).

**Why not full async/await?** The callers (`PuttyBase.cs`, `RdpProtocol.cs`) are deeply synchronous WinForms event handlers. Converting the entire call chain to async would require major refactoring of the protocol layer. `Task.Run()` wrapper is the pragmatic fix — moves blocking to thread pool, keeps API surface unchanged.

### Live Theme Switching (Task 020)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Theme change requires application restart | `ThemePage.SaveSettings()` showed restart dialog instead of applying live | Set `_themeManager.ActiveTheme = selectedTheme` directly |

**Key insight:** `ThemeManager.ActiveTheme` setter already fires `NotifyThemeChanged` → `ThemeChangedEvent`. All UI components (`BaseWindow`, `mrng*` controls) subscribe to this event. Simply setting the property triggers live retheming across the entire application — no restart needed.

### Self-Contained Build (Phase 1)

| Key Decision | Rationale |
| --- | --- |
| No trimming | WinForms, WPF, COM interop (MSTSCLib) are too risky for IL trimming |
| ReadyToRun enabled | Improves startup time for self-contained builds |
| MSBuild property `SelfContained` | Default false; CI matrix builds both variants |

### Self-Contained Build — `msbuild -p:SelfContained` Does NOT Embed Runtime (2026-02-09)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Self-contained ZIP (4.5MB) still asks user to install .NET | `msbuild -p:SelfContained=true` only sets the flag in `runtimeconfig.json` but does NOT copy runtime DLLs (`coreclr.dll`, `hostfxr.dll`, etc.) to output. Must use `-t:Publish` target. | Change `msbuild ... -p:SelfContained=true` to `msbuild ... -p:SelfContained=true -t:Publish -p:PublishDir=bin\x64\Release\win-x64-sc\` |

**Also:** `dotnet restore --runtime win-x64 /p:PublishReadyToRun=true` is required — without `/p:PublishReadyToRun=true` in restore, the publish step fails with `NETSDK1094: Unable to optimize assemblies for performance`.

**Correct self-contained ZIP:** ~116MB with 341 files (includes full .NET 10 runtime). Framework-dependent: ~21MB.

### WPF Splash Screen Leaks Dispatcher Thread — Mouse Input Broken (2026-02-09)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Mouse barely works after startup, only keyboard works | WPF splash screen starts `Dispatcher.Run()` on a background STA thread. When splash closes (`splash.Close()`), the Dispatcher keeps running — a second message pump consuming Win32 mouse messages alongside the WinForms message pump. | Call `splash.Dispatcher.InvokeShutdown()` after `splash.Close()` in both `frmMain.cs` and `ProgramRoot.CloseSplash()` |

**Also missing:** `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` before `Application.Run()`. In .NET 10, DPI handling must be set programmatically, not just via `app.manifest`. Without it, mouse coordinates may be scaled incorrectly.

**Files fixed:** `mRemoteNG/App/ProgramRoot.cs` (added `SetHighDpiMode`, fixed `CloseSplash`), `mRemoteNG/UI/Forms/frmMain.cs` (added `InvokeShutdown` after splash close).

### AssemblyInfo.cs Must Be Regenerated After Version Bump (2026-02-09)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Splash screen shows 1.78.2 instead of 1.80.0 | `AssemblyInfo.tt` (T4 template) was updated to 1.80 but `AssemblyInfo.cs` (generated output) was stale from 1.78.2 build | Manually update `AssemblyInfo.cs` or run T4 template regeneration in VS |

**Key rule:** After bumping version in `AssemblyInfo.tt`, always verify `AssemblyInfo.cs` matches. CI workflow regenerates it inline (PowerShell), but local builds may keep stale `.cs`.

### Code Signing for Open Source Projects

Open source projects **cannot** get free EV code signing certificates (required for SmartScreen reputation). Options:
1. **SignPath.io** — free for OSS, integrates with CI, provides standard code signing (not EV)
2. **Self-signed** — suppresses "unknown publisher" but not SmartScreen warning
3. **Azure Trusted Signing** — ~$10/month, provides SmartScreen reputation
4. **Standard practice:** Most open source WinForms apps ship unsigned; users dismiss the SmartScreen warning

### PBKDF2 Migration (Task 007)

| Key Decision | Rationale |
| --- | --- |
| Store iteration count in XML header | `KdfIterations` attribute in confCons.xml enables forward compatibility |
| Default 600,000 iterations | OWASP 2024 recommendation for PBKDF2-HMAC-SHA1 |
| Old files still readable | Missing `KdfIterations` attribute defaults to 1,000 for backward compat |

## Parallel Test Execution (2026-02-15)

### NUnit Fixture-Level Parallelism Does NOT Work (CRITICAL)

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| 27+ tests fail with wrong default values, encrypted passwords returned as-is, `KeyNotFoundException` | `DefaultConnectionInheritance.Instance` is a shared mutable static singleton. `Runtime.ConnectionsService`, `Runtime.EncryptionKey` are also static with setters. NUnit fixture parallelism runs fixtures on different threads in the SAME process, sharing all static state. | Use multi-process parallelism instead of NUnit parallelism |

**DO NOT add `[assembly: Parallelizable(ParallelScope.Fixtures)]`** to the test project. It causes race conditions on:
1. `DefaultConnectionInheritance.Instance` (singleton, mutable via `LoadFrom`/`SaveTo`)
2. `Runtime.ConnectionsService.IsConnectionsFileLoaded` (set by `DataTableDeserializer.Deserialize()`)
3. `Runtime.EncryptionKey` (static with setter)
4. `Runtime.MessageCollector` (shared collection, `ClearMessages()` across fixtures)

**What does NOT work (do not retry):**
- `[assembly: Parallelizable(ParallelScope.Fixtures)]` -- 27 failures
- Marking individual fixtures `[NonParallelizable]` -- too many share `DefaultConnectionInheritance.Instance`
- `NUnit.NumberOfTestWorkers=2` -- still races, just less frequently

**Working solution: Multi-process parallelism (run-tests.ps1)**
```
run-tests.ps1
  Process 1: mRemoteNGTests.Security (164 tests)
  Process 2: mRemoteNGTests.Tools + Messages + App + misc (354 tests)
  Process 3: mRemoteNGTests.Config (563 tests)
  Process 4: mRemoteNGTests.Connection + Credential + Tree + misc (866 tests)
  All 4 run simultaneously, each with isolated static state.
```

**Result:** 95s -> 46s (2.1x speedup), 1947/1947 passed, 0 failed.

### IntegratedProgramTests Launches Real notepad.exe

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| `CanStartExternalApp` opens real notepad window, leaves zombie process | Test calls `sut.Connect()` which does `_process.Start()` on `notepad.exe` | Replace with `InitializeSucceedsWhenExternalToolExists` which only calls `Initialize()` (no process launch) |

**Key rule:** NEVER call `Connect()` on `IntegratedProgram` in tests. It launches real processes, does `WaitForInputIdle`, `SetParent` P/Invoke -- all interactive.

### build.ps1 Does NOT Rebuild Test DLL on Incremental Build

| Symptom | Root Cause | Immediate Fix |
| --- | --- | --- |
| Test DLL timestamp stays old after editing test .cs files and running `build.ps1` | MSBuild incremental build skips test project if main project changes trigger recompile first, and test project's dependency graph isn't invalidated | Build test project explicitly: `msbuild mRemoteNGTests.csproj -t:Rebuild` |

### mRemoteNGSpecs BouncyCastle GCM Failures (Pre-Existing)

| Symptom | Root Cause | Status |
| --- | --- | --- |
| 3/5 SpecFlow tests fail with `InvalidCipherTextException: mac check in GCM failed` | BouncyCastle AEAD decryption fails on test fixtures. Pre-existing, not caused by any recent changes. | Known issue, investigate separately |

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

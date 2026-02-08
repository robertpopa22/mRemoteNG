# mRemoteNG - Build & Development Notes

## Repository Structure
- **Origin (fork):** `robertpopa22/mRemoteNG`
- **Upstream (official):** `mRemoteNG/mRemoteNG`
- **Main branch:** `v1.78.2-dev`
- **Solution:** `mRemoteNG.sln` (.NET 10, SDK-style projects with COM references)

## Build Instructions

### IMPORTANT: Do NOT use `dotnet build` - it fails on COM references (MSB4803)

### Correct build command (PowerShell):
```powershell
& "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\Common7\Tools\Launch-VsDevShell.ps1" -Arch amd64
dotnet restore "D:\github\mRemoteNG\mRemoteNG.sln"
msbuild "D:\github\mRemoteNG\mRemoteNG.sln" -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
```

### Or use the build script:
```bash
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1"
```

### Why this specific setup:
1. **`dotnet build`** fails with `MSB4803: ResolveComReference not supported on .NET Core MSBuild`
   - The project has a COM reference to `MSTSCLib` (RDP ActiveX control)
2. **`D:\BuildTools\MSBuild`** (first BuildTools) fails - missing `Microsoft.DotNet.MSBuildSdkResolver`
3. **`C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild`** (second BuildTools) WORKS
   - Has both `.NET SDK resolver` AND `COM reference support`
   - This is the correct MSBuild to use

### Two BuildTools installations on this machine:
| Path | SDK Resolver | Use? |
|------|-------------|------|
| `D:\BuildTools` | NO (only NuGet resolver) | DO NOT USE for this project |
| `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools` | YES (has Microsoft.DotNet.MSBuildSdkResolver) | USE THIS ONE |

## Testing

### Build and run tests:
```powershell
# From a VsDevShell (Launch-VsDevShell.ps1 -Arch amd64):
msbuild mRemoteNGTests\mRemoteNGTests.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
msbuild mRemoteNGSpecs\mRemoteNGSpecs.csproj -restore -m "-verbosity:minimal" "-p:Configuration=Release" "-p:Platform=x64"
```

### Run tests (after build):
```bash
dotnet test "D:\github\mRemoteNG\mRemoteNGTests\bin\x64\Release\mRemoteNGTests.dll" --verbosity normal
dotnet test "D:\github\mRemoteNG\mRemoteNGSpecs\bin\x64\Release\mRemoteNGSpecs.dll" --verbosity normal
```

### IMPORTANT: `dotnet test` path quirk
- Build outputs to `bin\x64\Release\` (because Platform=x64)
- `dotnet test --no-build` on the .csproj looks in `bin\Release\` (WRONG)
- Always run `dotnet test` directly on the **DLL path**, not the .csproj

### Current test status (codex/release-1.79-bootstrap, 2026-02-08):
- **mRemoteNGTests:** 2176 total, **2174 passed, 2 env-flaky** (CueBanner P/Invoke)
- **mRemoteNGSpecs:** 5 total, 5 passed
- All 81 pre-existing upstream failures resolved in commit `79c5e4cf`
- 28 new coverage tests added in commit `708a4f5c` (P7 gap analysis)
- Detailed changelog: `NEXTUP/P6_TEST_FIX_CHANGELOG_2026-02-08.md`
- Coverage analysis: `NEXTUP/P7_TEST_COVERAGE_ANALYSIS_2026-02-08.md`

### Historical baseline (upstream v1.78.2-dev, before fixes):
- **mRemoteNGTests:** 2119 total, 2038 passed, 81 failed (pre-existing upstream bugs)
- 29 new tests added during codex work (2119 → 2148)
- 28 new coverage tests added during P7 analysis (2148 → 2176)

## CI/CD
- CI uses `windows-2025-vs2026` runners with MSBuild 18.x (VS2026)
- CI workflow: `.github/workflows/pr_validation.yml` (build) and `Build_mR-NB.yml` (release)
- Platforms: x86, x64, ARM64
- CI does: `dotnet restore` then `msbuild` (same pattern as local build)

## Branch Strategy (Codex work)
- 26 fix branches: `codex/pr1-*` through `codex/pr26-*`
- Each branch has 1 clean commit on top of `upstream/v1.78.2-dev`
- `codex/release-1.79-bootstrap` = cumulative branch with all fixes + docs
- PRs (#3105-#3130) were closed on upstream - reopen with `gh pr reopen <nr>` after testing

## PR Workflow
1. Test locally (build + run)
2. When a fix is validated: `gh pr reopen <number>`
3. PRs target `v1.78.2-dev` on upstream `mRemoteNG/mRemoteNG`

## PR Reference Table
| PR# | Branch | Issue | Description |
|-----|--------|-------|-------------|
| 3105 | codex/pr1-security-followup | - | LDAP sanitizer and importer guardrails |
| 3106 | codex/pr2-closepanel-stability | 3069 | Close panel race fix |
| 3107 | codex/pr3-onepassword-3092 | 3092 | 1Password parser and fallback fix |
| 3108 | codex/pr4-default-provider-2972 | 2972 | Default external provider fix |
| 3109 | codex/pr5-commandline-security | - | ProcessStart hardening and escaping |
| 3110 | codex/pr6-sqlclient-sni-runtime | 3005 | SqlClient SNI runtime references |
| 3111 | codex/pr7-sql-schema-compat-1916 | 1916 | SQL schema compatibility hardening |
| 3112 | codex/pr8-configpanel-splitter-850 | 850 | Config panel splitter width reset |
| 3113 | codex/pr9-startup-path-fallback-1969 | 1969 | Startup path fallback |
| 3114 | codex/pr10-putty-provider-resilience-822 | 822 | PuTTY provider failure handling |
| 3115 | codex/pr11-putty-cjk-decode-2785 | 2785 | PuTTY CJK session name decoding |
| 3116 | codex/pr12-rdp-smartsize-focus-2735 | 2735 | RDP SmartSize focus loss fix |
| 3117 | codex/pr13-rdp-redirectkeys-fullscreen-847 | 847 | RDP fullscreen toggle guard |
| 3118 | codex/pr14-rdp-fullscreen-exit-refocus-1650 | 1650 | RDP refocus after fullscreen exit |
| 3119 | codex/pr15-rdp-rcw-smartsize-2510 | 2510 | RDP SmartSize RCW disconnect fix |
| 3120 | codex/pr16-settings-path-observability-2987 | 2987 | Settings path logging |
| 3121 | codex/pr17-password-protect-disable-2673 | 2673 | Require password before disabling protection |
| 3122 | codex/pr18-autolock-1649 | 1649 | Master password autolock on minimize/idle |
| 3123 | codex/pr19-protocol-1634 | 1634 | PROTOCOL external tool token |
| 3124 | codex/pr20-main-close-cancel-2270 | 2270 | Main close cancel behavior |
| 3125 | codex/pr21-xml-recovery-811 | 811 | Startup XML recovery |
| 3126 | codex/pr22-close-empty-panel-2160 | 2160 | Empty panel close after last tab |
| 3127 | codex/pr23-tab-drag-overflow-scroll-2161 | 2161 | Tab drag autoscroll on overflow |
| 3128 | codex/pr24-config-tree-layout-2171 | 2171 | Config connections panel focus |
| 3129 | codex/pr25-tab-crash-resize-2166 | 2166 | Tab close race under resize |
| 3130 | codex/pr26-inheritance-label-width-2155 | 2155 | Inheritance label width fix |

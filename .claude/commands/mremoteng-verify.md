# /mremoteng-verify — Health check: build, warnings, tests, CI, Sonar, git state

**Use when:** before a release tag, after a large change, or when you want to know whether the tree and CI are green. Read-only apart from the build output.

**Not for:** fixing anything. It lists failures; the fix happens in `/mremoteng-fix-repo` or by hand.

**Does:** build, test, and read CI for `robertpopa22/mRemoteNG`, then list what failed.

**Does not:** triage issues, start the orchestrator, or inspect an upstream pull request. Those are other skills.

Shell is PowerShell. Logs go under `$env:TEMP`. Do not use `tee`, `grep`, `tail`, `awk`, or `/tmp`. Do not call `bash` from `C:\Windows\System32`. That Bash is WSL and reports 0 tests.

## What to do

### 1. Build

```powershell
Set-Location D:\github\mRemoteNG
$buildLog = Join-Path $env:TEMP "mremoteng-verify-build.txt"
pwsh -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -NoRestore *> $buildLog
Write-Output "build-exit $LASTEXITCODE"
Select-String -Path $buildLog -Pattern ": error " | Measure-Object | Select-Object -ExpandProperty Count
Get-Content $buildLog -Tail 8
```

If the exit code is not 0, report the errors and skip the suite. Continue with CI and git.

### 2. Warnings

```powershell
Select-String -Path (Join-Path $env:TEMP "mremoteng-verify-build.txt") -Pattern ": warning " |
  ForEach-Object { if ($_.Line -match "warning ([A-Z]+[0-9]+)") { $Matches[1] } } |
  Group-Object | Sort-Object Count -Descending |
  Select-Object Count, Name
```

A count of 0 is the expected result. Do not classify rules that did not appear.

### 3. Tests

```powershell
Set-Location D:\github\mRemoteNG
$testLog = Join-Path $env:TEMP "mremoteng-verify-tests.txt"
& "C:\Program Files\Git\bin\bash.exe" .\run-tests-core.sh *> $testLog
Write-Output "suite-exit $LASTEXITCODE"
Select-String -Path $testLog -Pattern "Total tests|Passed!|Failed!|suite-exit|^Passed |^Failed "
```

The passing total belongs in `test-config.json` only after a green run of this command. Do not use `dotnet build` or `run-tests.ps1` for this gate.

### 4. CI and the nightly release

```powershell
gh run list --repo robertpopa22/mRemoteNG --limit 8 --json databaseId,name,status,conclusion,headSha,url
gh release view nightly --repo robertpopa22/mRemoteNG --json name,targetCommitish
```

For a failed run, read its log with `gh run view <id> --repo robertpopa22/mRemoteNG --log-failed`.

### 5. SonarCloud

```powershell
curl.exe -s "https://sonarcloud.io/api/qualitygates/project_status?projectKey=robertpopa22_mRemoteNG"
```

If the body is empty, say the gate was not read. Do not invent a status.

### 6. Git and open issues

```powershell
Set-Location D:\github\mRemoteNG
git status -sb
git log --oneline origin/main..HEAD
gh issue list --repo robertpopa22/mRemoteNG --state open --json number,title,author
```

An open issue whose author is us is listed. It is work for `/mremoteng-fix-repo`, not for this skill.

## Report

One block: build exit, warning count, suite exit and passed total, CI conclusion for the current tip, nightly commit, Sonar gate, dirty files, open issue numbers. Name the action only where a check failed.

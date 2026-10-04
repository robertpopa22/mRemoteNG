# /mremoteng-pipeline — Unattended issue pipeline

Run the unattended pipeline: sync from GitHub, analyze, triage, report. Issues we opened are in that session, including an auto-submitted crash report with no comments. Do not drop one because the author is us.

This is not the daily path. Interactive work is `/mremoteng-fix-repo`, which includes the status report. `/mremoteng-pipeline-supervise` stops or restarts this pipeline. `/mremoteng-pipeline-edit` edits its script. Do not start this pipeline to answer one open issue.

## Usage

The user may specify arguments after the command:
- `/mremoteng-pipeline` — **full session**: sync → analyze → orchestrate issues → report
- `/mremoteng-pipeline quick` — sync + analyze + report only (no AI triage, ~15 min)
- `/mremoteng-pipeline issues` — full session focused on issues only
- `/mremoteng-pipeline warnings` — full session focused on warnings only
- `/mremoteng-pipeline issues --max-issues 10` — limit AI triage to 10 issues
- `/mremoteng-pipeline warnings --max-files 5` — limit files processed
- `/mremoteng-pipeline --dry-run` — simulate without changes

## What to do

### Step 1: Sync (MANDATORY — always first)
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py sync
```
Run it and read its own progress. Do not budget 14 minutes; that figure was for a backlog of about 800 issues.

### Step 2: Analyze
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py analyze
```
Quick — shows categorized issues. Capture the output summary for the user. An open issue we filed is in that summary, including an auto-submitted crash report with no comments. Do not drop it because the author is us. A `wontfix` announcement stays out.

### Step 3: Report (pre-orchestrator snapshot)
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py report --include-all
```
Generates markdown report. Note the stats for comparison later.

### Step 4: Orchestrate (AI triage + fix)
Skip this step if user specified `quick` mode.
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py <issues|warnings|all> [args]
```
Run it in the background and read the status file. Duration follows the number of issues actually selected, not a fixed hour count.

```powershell
Get-Content D:\github\mRemoteNG\.project-roadmap\scripts\orchestrator-status.json
```

### Step 5: Final report
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py report --include-all
```
Compare with Step 3 stats to show what changed.

### Step 6: Present summary
Show the user:
- **Sync**: issues synced, new comments, waiting for us count
- **Orchestrator**: triaged / implemented / wontfix / duplicate / needs_info / failed
- **Reports**: link to generated report file
- **Commits**: any commits made by the orchestrator
- **Errors**: list of failures
- **Duration**: total session time (sync + orchestrate)
- **Delta**: what changed vs pre-orchestrator state

## Important notes

- The script uses `claude -p` (headless mode) as sub-agent — it strips CLAUDECODE env vars automatically
- Each file fix is verified independently (build + test) and reverted on failure
- Commits are pushed to origin at the end
- The orchestrator kills stale processes (notepad.exe, testhost.exe) after every step
- Log file: `.project-roadmap/scripts/orchestrator.log`
- Status file: `.project-roadmap/scripts/orchestrator-status.json`
- **JSON DB files are updated during orchestrator run** — each triage decision writes priority, notes, and status to the issue JSON

# /iis-orchestrator — Run the IIS Orchestrator

Run the automated Issue Intelligence System Orchestrator to fix GitHub issues and/or CS8xxx nullable warnings.

## Usage

The user may specify arguments after the command:
- `/iis-orchestrator` — run all (issues + warnings)
- `/iis-orchestrator warnings` — only warning cleanup
- `/iis-orchestrator issues` — only open issues
- `/iis-orchestrator warnings --max-files 5` — limit files processed
- `/iis-orchestrator issues --max-issues 3` — limit issues processed
- `/iis-orchestrator --dry-run` — simulate without changes

## What to do

1. Parse the user's arguments (default: `warnings --max-files 5`)
2. Run the orchestrator script:
   ```bash
   python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py <args>
   ```
3. Run it in the background (it can take 10-60 minutes)
4. Monitor progress by periodically reading the status file:
   ```bash
   cat D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator-status.json
   ```
5. When complete, show the summary to the user with:
   - Number of files processed
   - Warnings fixed vs reverted
   - Commits created
   - Any errors encountered

## Important notes

- The script uses `claude -p` (headless mode) as sub-agent — it will strip CLAUDECODE env vars automatically
- Each file fix is verified independently (build + test) and reverted on failure
- Commits are pushed to origin at the end
- The orchestrator kills stale processes (notepad.exe, testhost.exe) after every step
- Log file: `.project-roadmap/scripts/orchestrator.log`
- Status file: `.project-roadmap/scripts/orchestrator-status.json`

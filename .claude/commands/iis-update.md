# /iis-update — Edit the unattended pipeline script

**Does:** change `iis_orchestrator.py` when a run of that pipeline showed a concrete script defect.

**Does not:** start the pipeline, and it is not the daily issue path. One open issue goes to `/mremoteng-fix-repo`.

## What to do

1. Read `.project-roadmap/scripts/iis_orchestrator.py` at the symbol the run named. Do not trust a line number from an old note.
2. Read the tail of `.project-roadmap/scripts/orchestrator.log` and `orchestrator-status.json` when they exist.
3. Change only the setting the user named.
4. Show the diff. Do not launch the orchestrator.

Search by name: `TEST_FILTER`, `STALE_PROCESSES`, and the prompt inside the warning flow. A string with quotes goes in a file first. PowerShell rewrites `$()` and f-strings before a remote or nested command sees them.

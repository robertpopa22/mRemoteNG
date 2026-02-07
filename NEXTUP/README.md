# NEXTUP Modernization Workspace

This folder is the persistent execution workspace for modernizing this fork to a stable, releasable version.

Goals:
- Keep a single source of truth for plan, status, and issue triage.
- Make restart/resume trivial (after PC reboot, continue from `WORK_STATE.md`).
- Track objective acceptance criteria before closing any phase.

Files:
- `MASTER_PLAN.md`: full phased plan with entry/exit criteria.
- `WORK_STATE.md`: current progress, blockers, and next action queue.
- `ISSUE_PACKAGES.md`: issue triage strategy and intervention packages.
- `scripts/refresh-issues.ps1`: refreshes issue snapshots and package views.

Local analysis output location:
- `D:\github\LOCAL\analysis\mRemoteNG`

Resume workflow:
1. Open `NEXTUP/WORK_STATE.md`.
2. Execute the first item under `Immediate Next Actions`.
3. Update status and evidence links after each completed action.

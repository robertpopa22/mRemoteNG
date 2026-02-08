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
- `LESSONS.md`: durable rules from real failures and fixes.
- `COMMAND_FEEDBACK_LOG.md`: chronological feedback log of commands.
- `COMMAND_FEEDBACK_METRICS.md`: top recurring errors and time-loss hotspots.

Local analysis output location:
- `D:\github\LOCAL\analysis\mRemoteNG`

Resume workflow:
1. Open `NEXTUP/WORK_STATE.md`.
2. Execute the first item under `Immediate Next Actions`.
3. Update status and evidence links after each completed action.

Command learning loop:
1. Before repeating a risky command, search lessons:
   - `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd lesson -Pattern "<keyword>"`
2. After each failed/partial command, log it:
   - `powershell -File D:\github\mRemoteNG\NEXTUP\scripts\log-command-feedback.ps1 -Command "<cmd>" -Result FAIL -Resolution "<fix>" -Category "<cat>" -ErrorPattern "<error>" -DurationSeconds <sec>`
3. Refresh counters and time-loss report:
   - `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd metrics -Top 15`

Alias runner:
1. `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd g -C D:\github\mRemoteNG status -sb`
2. `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd h auth status`
3. `D:\github\mRemoteNG\NEXTUP\scripts\nx.cmd paths`
4. Optional for interactive shells: `call D:\github\LOCAL\env.cmd` then use `nx`, `nxlesson`, `nxmetrics`.

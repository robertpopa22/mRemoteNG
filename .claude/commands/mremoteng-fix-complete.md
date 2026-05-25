# /mremoteng-fix-complete — Maintenance status + delegate to fix-repo

Run-at-startup situational report for the maintenance phase: where the fork stands (local vs origin vs upstream), what upstream work is pending integration, and which open **fork** issues have new tester comments waiting on us. After the report, automatically hand off the actionable fork comments to the `/mremoteng-fix-repo` workflow.

This command is **read-only** for assessment; the only changes come from the delegated fix-repo phase (which itself stops for confirmation before anything outward-facing). It never integrates upstream automatically — it only lists the gap.

## Usage

The user may specify arguments after the command:
- `/mremoteng-fix-complete` — full status report, then run `/mremoteng-fix-repo` if fork comments need action
- `/mremoteng-fix-complete --report-only` — status report only, do NOT delegate
- `/mremoteng-fix-complete --no-sync` — skip the GitHub sync (use cached issue DB)

## What to do

### Step 1: Sync issue DB (skip if `--no-sync`)
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py sync
```
Refreshes both repos' issue JSON from GitHub. Expected ~10-14 min for 800+ issues. For a faster startup the user can pass `--no-sync`.

### Step 2: Git state — local vs origin
```bash
cd /d/github/mRemoteNG && git fetch origin --quiet && git branch --show-current && git log --oneline origin/main..HEAD && git status -sb
```
Report: current branch, unpushed commits (`origin/main..HEAD`), uncommitted changes.

### Step 3: Upstream integration gap
```bash
bash D:/github/mRemoteNG/.project-roadmap/scripts/check-upstream.sh
```
Then read pending integration items from the tracking file:
```bash
python -c "import json; d=json.load(open(r'D:/github/mRemoteNG/.project-roadmap/upstream-tracking.json')); print('branch:', d['_meta']['upstream_branch'], '| last_checked:', d['_meta']['last_checked']); [print('COMMIT', c['sha'][:9], c.get('message','')[:70]) for c in d.get('commits',[]) if c.get('status')=='pending']; [print('PR', p['number'], p.get('title','')[:70]) for p in d.get('prs',[]) if p.get('status')=='pending']"
```
List untracked upstream commits + `status==pending` commits/PRs that need a decision. Note PR #3189 (release/1.81 → upstream) state. **Do not integrate — just report the gap.**

### Step 4: Fork issues waiting on us (the fix-repo work queue)
```bash
python -c "import json,glob,os; rows=[]; [rows.append((j['number'], j.get('title','')[:60], j.get('unread_comments',0), next((c['snippet'][:80] for c in reversed(j.get('comments',[])) if not c.get('is_ours')), ''))) for j in (json.load(open(f,encoding='utf-8')) for f in glob.glob(r'D:/github/mRemoteNG/.project-roadmap/issues-db/fork/*.json')) if j.get('state')=='open' and j.get('unread_comments',0)>0 and j.get('waiting_for_us')]; [print(f'#{n}\t({u} new)\t{t}\t-> {s}') for n,t,u,s in sorted(rows)]"
```
This is the actionable queue: open fork issues with new external comments. Capture it.

### Step 5: Release / CI snapshot
```bash
cd /d/github/mRemoteNG && grep -m1 "<Version>" mRemoteNG/mRemoteNG.csproj && gh run list --repo robertpopa22/mRemoteNG --branch main --limit 6 --json headSha,status,conclusion,workflowName --jq '.[] | "\(.headSha[0:9])\t\(.status)\t\(.conclusion // "-")\t\(.workflowName)"'
```
Report version + last CI runs. For deep QA (SonarCloud, analyzer warnings, full CI log triage) tell the user to run `/iis-verify` — do not duplicate it here.

### Step 6: Consolidated report + delegate
Print ONE consolidated markdown table covering: git state (local/origin), upstream integration gap, fork work queue, release/CI. Then:
- If `--report-only`, stop.
- Otherwise, if Step 4 found actionable fork comments, execute the `/mremoteng-fix-repo` workflow — read and follow `D:/github/mRemoteNG/.claude/commands/mremoteng-fix-repo.md` (pass `--no-sync` to it, since Step 1 already synced).

## Important notes

- Read-only except the delegated fix-repo phase; that phase stops for confirmation before push + GitHub replies.
- Never integrates upstream automatically — only lists pending commits/PRs for a human decision.
- `sync` hits the network; use `--no-sync` for an offline/fast startup view.
- Issue DB: `.project-roadmap/issues-db/fork/*.json` (fork) and `upstream/*.json` (upstream); upstream gap: `.project-roadmap/upstream-tracking.json`.
- Deep QA lives in `/iis-verify`; this command intentionally references it rather than re-implementing the checks.

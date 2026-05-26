# /mremoteng-fix-repo — Process local fork issue comments (classify → dual-review fix → commit)

Handle ONLY the local fork's open issues that have new tester comments waiting on us. For each: classify the comment, and for actionable bugs investigate the root cause, get an independent counter-opinion from Codex AND Gemini, apply a minimal fix, build, run the full test suite, and make an atomic local commit. Then **stop and ask for confirmation** before pushing and posting any GitHub reply.

Scope is the fork (`robertpopa22/mRemoteNG`) only — this command never touches upstream tracking or merges upstream changes.

## Usage

The user may specify arguments after the command:
- `/mremoteng-fix-repo` — process every open fork issue with new external comments
- `/mremoteng-fix-repo 110` — target a single issue number
- `/mremoteng-fix-repo --no-sync` — skip the fork sync (use cached issue DB; used when called by `/mremoteng-fix-complete`)

## What to do

### Step 1: Sync fork only (skip if `--no-sync`)
```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py sync --repos fork
```
Explicitly fork-scoped — never sync or modify upstream here.

### Step 2: Build the work queue
```bash
python -c "import json,glob; rows=[(j['number'], j.get('title','')[:70]) for j in (json.load(open(f,encoding='utf-8')) for f in glob.glob(r'D:/github/mRemoteNG/.project-roadmap/issues-db/fork/*.json')) if j.get('state')=='open' and j.get('unread_comments',0)>0 and j.get('waiting_for_us') and (j.get('comments') and not j['comments'][-1].get('is_ours'))]; [print(f'#{n}\t{t}') for n,t in sorted(rows)]"
```
If a single issue number was given, restrict to it. For each queued issue, fetch the full new comment(s):
```bash
gh issue view <n> --repo robertpopa22/mRemoteNG --json title,comments --jq '.title, (.comments | sort_by(.createdAt) | .[-2:] | .[] | "[\(.author.login) @ \(.createdAt)]\n\(.body)")'
```
Download any attached screenshots (`curl -sL <asset-url> -o D:/github/mRemoteNG/<tmp>.png` then Read the image) when the comment references one.

### Step 3: Classify each issue
Assign one class, then record it in the issue JSON:
- `fix` — actionable bug/regression → proceed to Step 4
- `needs-info` — ambiguous; reply asking for repro/log (no code change)
- `wontfix` — out of scope / by design
- `confirm-fixed` — reporter confirms the fix works → mark released / close
- `upstream-only` — belongs upstream; record and defer, do NOT fix here

```bash
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py update --issue <n> --repo fork --status <new|triaged|testing|released|wontfix|needs_info> --notes "<one-line reason>"
```
(status map: fix→triaged, needs-info→needs_info, wontfix→wontfix, confirm-fixed→released, upstream-only→triaged + note.)

### Step 4: Investigate + dual counter-opinion (only for `fix`)
1. Root-cause from source first; cite `file:line`. Verify the premise against the actual code (sources are authoritative, not the comment's framing).
2. Get TWO independent opinions — spawn both `codex:codex-rescue` and `gemini:gemini-rescue` as **READ-ONLY diagnosis** (do not feed them your conclusion). Each prompt MUST open with this framing verbatim:
   > Read-only review — do NOT modify any files, do NOT build. Re-derive the premise from source independently and return: root cause (file:line) + a minimal proposed diff in text only.

   - This phrasing makes `codex:codex-rescue` omit `--write` so it runs in a `read-only` sandbox (edits are physically blocked). It is **write-by-default otherwise**, and a write-mode run silently edits the working tree (auto-applied, uncommitted) — which has happened and nearly shipped an unreviewed change.
   - Also pass **`--wait`** to `codex:codex-rescue` so the bounded review runs foreground and returns the actual result, not a background launch stub.
   - The reviewers must NOT build — the main thread builds/tests in Step 5 (and a read-only sandbox can't write build outputs anyway).
   - **If codex-rescue still returns a background stub** (`"…started in the background as <jobId>. Check /codex:status…"`), fetch the result with `/codex:status <jobId>` then `/codex:result <jobId>`. **Do NOT re-invoke a fresh `codex:codex-rescue`** — a fresh run reads a possibly-mutated tree and mis-reports state ("already in source / no fix needed").
3. **Guard:** after both reviews return, run `git status --short`. The reviewers must not have touched the tree; if anything was modified, surface it and reconcile (revert, or deliberately adopt with eyes open) BEFORE Step 5 — never silently inherit a reviewer's edit.
4. Converge. If Codex and Gemini diverge, resolve the disagreement before editing (a divergence has caught a wrong fix before). The **main thread** applies the **minimal** fix only — do not change unrelated behavior.

### Step 5: Verify (full build + full test suite)
```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File "D:/github/mRemoteNG/build.ps1"
pwsh -NoProfile -ExecutionPolicy Bypass -File "D:/github/mRemoteNG/run-tests.ps1" -Headless
```
Must be green (full suite; current baseline ~6251). Golden Rule: every test failure is resolved — fix the code, fix the test, or remove an invalid test; **never** `[Ignore]`.

### Step 6: Atomic local commit per fix
One commit per issue: subject `fix(#<n>): <summary>`. Body explains root cause + fix. **No `Co-Authored-By`, no "Generated with" lines.**
```bash
cd /d/github/mRemoteNG && git add <changed files> && git commit -F <message-file>
```

### Step 7: CONFIRMATION GATE — push + GitHub replies
Do NOT push or comment yet. Present to the user: a table of commits made, and the drafted GitHub reply for each issue. Ask for approval of **push + replies** (AskUserQuestion). Only after approval:
```bash
cd /d/github/mRemoteNG && git push origin main
gh issue comment <n> --repo robertpopa22/mRemoteNG -F <reply-file>
python D:/github/mRemoteNG/.project-roadmap/scripts/iis_orchestrator.py update --issue <n> --repo fork --status testing --notes "fix shipped <commit>; awaiting reporter confirm on nightly"
```
For `needs-info` / `wontfix` / `confirm-fixed` issues (no commit), draft the reply and include it in the same approval gate.

### Step 8: Record memory
Write a session memory file under the project memory dir + add a one-line pointer to `MEMORY.md`: issues handled, root causes (file:line), commit hashes, and any Codex/Gemini divergence resolved.

## Important notes

- **Fork-scoped only** — never edits `upstream-tracking.json` or merges upstream. Upstream decisions belong to `/mremoteng-fix-complete`'s report.
- **Stops before every outward-facing action** — local commits are autonomous; push + GitHub comments require explicit confirmation.
- Replies are custom-written via `gh issue comment` (not the orchestrator's templated `update --post-comment`), so the daily comment rate limit does not gate this path.
- **Reviewers are read-only.** `codex:codex-rescue` defaults to `--write` (it edits the working tree, auto-applied, uncommitted) unless the prompt explicitly says read-only/diagnosis. Always invoke it read-only + `--wait` for the dual review, and `git status --short` after — the main thread is the sole author of edits/builds/commits.
- Build: `build.ps1` (NOT `dotnet build` — COM refs fail MSB4803). Tests: `run-tests.ps1 -Headless`, `--verbosity normal` only.
- Issue DB: `.project-roadmap/issues-db/fork/*.json`; flags used — `unread_comments`, `waiting_for_us`, `comments[].is_ours`.
- This is the codified version of the manual #113/#110 maintenance loop.

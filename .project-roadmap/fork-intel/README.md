# Fork Intelligence

Upstream `mRemoteNG/mRemoteNG` has ~1600 forks. A handful of them contain real work — bug fixes, features, hardening — that was never offered back upstream and that nobody ever told us about. This subsystem finds that work, throws away the noise and the hostile code, and hands back a **ranked queue of things worth importing**.

Nothing is ever imported automatically. The pipeline proposes; a human decides.

## Quick start

```bash
python .project-roadmap/fork-intel/fork_intel.py discover --since-months 6
python .project-roadmap/fork-intel/fork_intel.py diverge
python .project-roadmap/fork-intel/fork_intel.py screen
python .project-roadmap/fork-intel/fork_intel.py triage
python .project-roadmap/fork-intel/fork_intel.py report
```

Then read `reports/<date>_fork-radar.md` and `IMPORT_QUEUE.md`. After acting on an entry:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported --note "landed as abc1234"
```

`status` prints what the local database currently holds. Every stage is resumable and cached by commit SHA, so a second run costs almost nothing.

## What the first live run found

| Stage | Result |
|---|---|
| Forks of upstream | 1698 seen |
| Pushed within 6 months | 127 |
| Actually ahead of upstream | 24 |
| Own commits in those forks | 306 |
| Dropped as noise | 137 |
| Quarantined for security review | 25 |
| Clean, worth judging | 20 |

The whole pass costs about 190 GitHub API calls against a 5000/hour limit.

## Directory structure

```
fork-intel/
├── fork_intel.py              CLI - stdlib only, GitHub access through the gh CLI
├── test_fork_intel.py         unittest suite (no network)
├── rules/security_rules.json  screening rules as data, so they can be audited and tuned
├── db/
│   ├── _meta.json             last run per stage, counters, API budget spent
│   ├── forks/<owner>__<repo>.json   one file per fork: metadata, divergence, its commits
│   └── candidates/<sha>.json        one file per commit: files, flags, triage verdict
├── reports/<date>_fork-radar.md
├── IMPORT_QUEUE.md            actionable queue with ready-to-run commands
└── EXCLUDE.json               permanent denylist + memory of decisions already made
```

## Pipeline

### 1. `discover`

Pages through the upstream fork list and keeps forks pushed within the window (default 6 months). Owners listed in `EXCLUDE.json` are skipped. A fork whose `pushed_at` moved since the last run is reset to `discovered` so it gets re-examined.

### 2. `diverge`

For each candidate, compares `upstream:default...owner:default` and records `ahead_by`, `behind_by`, the merge base and the commit list. `ahead_by == 0` means a fork that only ever synced — marked `no-divergence` and not revisited.

### 3. `screen`

Two deterministic layers. No AI, no judgement calls.

**Layer A — noise.** Drops merge commits, bot authors, commits authored by upstream maintainers (these are merge-base artefacts from forks that branched off an older upstream branch, not fork work), activity-farming patterns like `chore: activity sync`, subjects too short to describe anything, and anything whose normalized subject already exists in our own history.

**Layer B — security.** Fetches each surviving commit's file list and patch, then applies `rules/security_rules.json`:

- CI workflow and pipeline edits — the primary supply-chain vector
- committed binaries and files with no reviewable text diff — the OpenSSF Scorecard heuristic
- dependency manifest changes, which can pull arbitrary code at restore time
- added lines that download, spawn processes, evaluate strings, or read secrets
- long base64 blobs, exfiltration-friendly hosts
- anything under security/crypto paths, build scripts, the installer, or the licence

A hit does **not** mean the change is malicious — most of the flagged commits in the first run were honest CI or crypto work. It means the change can never reach the ready-to-import tier without a human reading the diff first.

**Fork code is never executed.** Stages 1–3 use only API metadata and text patches: no clone, no fetch, no build, no tests.

### 4. `triage`

Batches the survivors to an AI agent (`claude` by default, falling back to `codex` and `gemini`). Each commit is presented with its diff, touched files, our open issue titles, and commit subjects from our own history that share keywords — so the model can tell "we already did this" from "this is new". The model returns strict JSON per commit: category, mapped issue, whether we already have it, value/effort/risk on 1–5, whether the patch is likely to apply, and a recommended action.

### 5. `report`

The AI supplies inputs; **the scoring is deterministic** and lives in code, so a verdict can be audited and re-derived:

```
score = 3 x value + 2 x novelty - 2 x risk - effort
```

Hard gates run first and cannot be overridden by an enthusiastic model:

| Gate | Result |
|---|---|
| already in our fork, or triaged REJECT | Tier D |
| any security flag | Quarantine |
| patch needs a rewrite over our tree | Tier B |
| diff too large for a blind cherry-pick | never Tier A |

Tiers: **A** ready to cherry-pick · **B** worth porting by hand · **C** watch list · **Q** security review first · **D** rejected.

`IMPORT_QUEUE.md` carries ready-to-run commands for tier A and porting notes for tier B.

### 6. `mark`

Records a decision (`imported` / `rejected` / `deferred`) in `EXCLUDE.json` so the same commit never resurfaces. This is what makes repeated runs quiet.

## Licence and attribution

mRemoteNG is GPL-2.0, and so is every fork of it, so importing is licence-compatible. Preserve authorship:

- `git cherry-pick` keeps the original author, `-x` records the source commit
- add a `Ported-from: <commit URL>` trailer so the origin stays visible in our history
- never squash away the original author's name

(The project rule against `Co-Authored-By` applies to AI attribution. Human attribution required by the licence is a different thing and must be kept.)

## Rules

- **Never import without reading the diff.** Tier A means "safe to try", not "safe to trust".
- **Never run anything from a fork** — not their build scripts, not their tests, not their workflows.
- **Quarantine is not a verdict.** It routes the change to a human, nothing more.
- **Always build and test after a cherry-pick**: `build.ps1`, then `run-tests.ps1 -Headless`.
- **Never contact fork authors automatically.** Any outreach is written by a human.
- Run `test_fork_intel.py` after touching the filters — its fixtures are calibrated against real observed forks.

## Prior art

- [useful-forks](https://github.com/useful-forks/useful-forks.github.io) and [active-forks](https://github.com/techgaun/active-forks) — surface which forks are ahead. Discovery only: no content analysis, no security screening, no import triage.
- [INFOX (ICSE 2018)](https://www.cs.cmu.edu/~shuruiz/paper/INFOX_ICSE2018.pdf) — clusters diverged fork code into labelled features with ~90% median accuracy. The idea of reasoning about coherent features rather than isolated commits comes from there.
- [Meta-maintenance (arXiv 2102.06355)](https://arxiv.org/abs/2102.06355) — establishes that forks hold maintenance value worth propagating, but its analysis is manual and offers no automated method.
- [OpenSSF Scorecard](https://github.com/ossf/scorecard) — the binary-artifact heuristic used in layer B.

What is new here is the end-to-end chain: discovery → deduplication against our own 1600-commit divergence → security screening → AI triage → a ranked, human-gated import queue.

## Maintained by

<a href="https://geseidl.ro/servicii-it"><img src="https://geseidl.ro/assets/icons/logo-green.png" alt="Geseidl Consulting Group" height="40"></a>

Built for the mRemoteNG community fork so that good work done in isolated forks stops being lost. Maintained by [Geseidl IT Solutions](https://geseidl.ro/servicii-it), part of [Geseidl Consulting Group](https://geseidl.ro).

# Fork Intelligence

Upstream `mRemoteNG/mRemoteNG` has ~1600 forks. A handful of them contain real work — bug fixes, features, hardening — that was never offered back upstream and that nobody ever told us about. This subsystem finds that work, throws away the noise and the hostile code, and hands back a **ranked queue of things worth importing**.

Nothing is ever imported automatically. The pipeline proposes; a human decides.

## Quick start

```bash
python .project-roadmap/fork-intel/fork_intel.py discover --since-months 6
python .project-roadmap/fork-intel/fork_intel.py diverge
python .project-roadmap/fork-intel/fork_intel.py screen
python .project-roadmap/fork-intel/fork_intel.py triage
python .project-roadmap/fork-intel/fork_intel.py preapprove
python .project-roadmap/fork-intel/fork_intel.py report
```

Then read `reports/<date>_fork-radar.md` and `IMPORT_QUEUE.md`. After acting on an entry:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported --note "landed as abc1234"
```

`status` prints what the local database currently holds. Every stage is resumable and cached by commit SHA, so a second run costs almost nothing.

## What the first full run found (2026-07-25/26)

| Stage | Result |
|---|---|
| Forks of upstream seen | 1698 |
| Pushed within 6 months | 127 |
| Actually ahead of upstream | **58** |
| Their own commits | 1794 |
| Dropped as noise | 204 |
| Quarantined for security review | 89 |
| Clean, worth judging | 166 |
| **Tier A — ready to cherry-pick** | **2** |
| Tier B — worth porting by hand | 14 |

Cost: about 830 GitHub API calls against a 5000/hour limit, plus roughly 250 AI calls.
Everything is cached by commit SHA, so a repeat run is nearly free.

**Signal ratio: ~0.1%.** Two commits out of 1794 were worth taking as they stand. That
number is the point of the system, not a disappointment: it is the cost of *knowing*
rather than guessing what the ecosystem holds.

**Value concentrates in people, not in the network.** Of the 16 tier A/B candidates,
`k-meeks` accounted for 7 and `Hovn` for 4 — the other 56 diverged forks produced 5
between them. Ongoing monitoring should watch a handful of individuals rather than
dredge 1698 repositories.

**The most useful result was the negative one.** Nobody in the ecosystem is meaningfully
ahead of this fork. That closes a question which otherwise stays open indefinitely.

### What was actually imported

| Commit | From | How |
|---|---|---|
| Connection tree jumping to a random "…Research" node when the search box holds its placeholder | Kyle Meeks (`k-meeks`) | cherry-picked, author preserved |
| `%GUID%` variable for external tools | `Hovn` | reimplemented — the original targeted the pre-2015 `mRemoteV1/` tree |

### Scan the branches, not just the branch

The first pass compared only each fork's **default branch** and concluded that nobody had
added anything worth having. That conclusion was an artefact of the method: serious
contributors keep their work on feature branches and never touch the branch the fork was
created with. Re-running with `--all-branches`:

| | Default branch only | All branches |
|---|---|---|
| Diverged forks | 24 | **58** |
| Commits examined | 306 | **1794** |
| Candidates | 45 | **300** |
| Tier A | **0** | **2** |

1488 commits existed *only* on side branches. When a scan of this kind reports "there is
nothing here", suspect the scan before believing the terrain.

## Directory structure

```
fork-intel/
├── fork_intel.py              CLI - stdlib only, GitHub through gh, AI through the agent CLIs
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

### 5. `preapprove`

Reviewing every candidate by hand is the bottleneck the pipeline exists to remove, so candidates are put to a vote before they reach a human.

Each tier A/B candidate — and every quarantined one — goes to two independent model families (`codex` and `gemini` by default) as a **read-only, opinion-only** review, with our project direction in the prompt: .NET 10, `build.ps1`, no new dependencies, no telemetry, no interactive tests, 6341 tests must stay green. Each reviewer returns a single JSON verdict.

**When the reviewers disagree, a third family arbitrates.** A split verdict is exactly the case an outside opinion can settle, so `grok-4.5` (xAI REST API, no CLI) is asked — but only then. Unanimous approval or unanimous refusal needs no arbiter and does not pay for one.

The decision rule is deliberately one-sided:

| Situation | Result |
|---|---|
| every reviewer approves, all aligned, no security flag | `pre-approved` |
| reviewers split, arbiter fetched, clear majority approves | `pre-approved` |
| any dissent without an arbiter | `manual-review` |
| a reviewer did not answer | `manual-review` — silence is never consent |
| any security flag | `manual-review` — cannot be voted away |
| any reviewer says it does not fit our direction | `manual-review` |

Quarantined changes are reviewed but can never be pre-approved. The votes only tell the maintainer whether the diff is worth their reading time.

**Pre-approved still means a human lands it.** It means "decide quickly", not "no decision needed".

### 6. `report`

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

### 7. `mark`

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

## What building this taught us

Three defects surfaced by *using* the system, not by testing it. All three failed the same
way — as **silence rather than as an error**, which is the dangerous class for anything that
aggregates opinions:

- prompts passed as command-line arguments blew past the Windows limit (`WinError 206`) and
  quietly lost 8 candidates. Prompts now go through stdin.
- `codex` and `gemini` are npm `.cmd` shims that a bare-name `subprocess` call cannot
  resolve. Executables are now resolved with `shutil.which`.
- grok answered with reasoning prose instead of the verdict, so a real opinion read as no
  answer. A system turn plus `response_format: {"type":"json_object"}` makes the shape
  non-optional.

This is why **a missing answer counts as dissent**: while one provider was down, that rule
was the only thing preventing a broken CLI from being read as tacit approval.

A fourth defect was worse, because it produced confident wrong output rather than nothing.
Running with `--reviewers codex,grok` while the arbiter defaulted to `grok` let the same
model vote twice, turning "2 of 3 approved" into one family outvoting another. Re-running
the three affected candidates with an independent arbiter flipped **all three** back to
manual review. An arbiter that is already a reviewer is now refused up front.

The lesson generalises past this tool: **independence between reviewers has to be enforced
mechanically, not assumed** — and the mechanism built to avoid correlated error is exactly
where correlated error hid.

## Prior art

- [useful-forks](https://github.com/useful-forks/useful-forks.github.io) and [active-forks](https://github.com/techgaun/active-forks) — surface which forks are ahead. Discovery only: no content analysis, no security screening, no import triage.
- [INFOX (ICSE 2018)](https://www.cs.cmu.edu/~shuruiz/paper/INFOX_ICSE2018.pdf) — clusters diverged fork code into labelled features with ~90% median accuracy. The idea of reasoning about coherent features rather than isolated commits comes from there.
- [Meta-maintenance (arXiv 2102.06355)](https://arxiv.org/abs/2102.06355) — establishes that forks hold maintenance value worth propagating, but its analysis is manual and offers no automated method.
- [OpenSSF Scorecard](https://github.com/ossf/scorecard) — the binary-artifact heuristic used in layer B.

What is new here is the end-to-end chain: discovery → deduplication against our own 1600-commit divergence → security screening → AI triage → a ranked, human-gated import queue.

## Maintained by

<a href="https://geseidl.ro/servicii-it"><img src="https://geseidl.ro/assets/icons/logo-green.png" alt="Geseidl Consulting Group" height="40"></a>

Built for the mRemoteNG community fork so that good work done in isolated forks stops being lost. Maintained by [Geseidl IT Solutions](https://geseidl.ro/servicii-it), part of [Geseidl Consulting Group](https://geseidl.ro).

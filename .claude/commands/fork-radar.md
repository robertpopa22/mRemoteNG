# /fork-radar — Mine the upstream fork network for work worth importing

Run the Fork Intelligence pipeline over the ~1600 forks of `mRemoteNG/mRemoteNG`, pre-approve the safe candidates through independent counter-opinions, and present a ranked queue. **Nothing is imported without explicit approval.**

Full system documentation: `.project-roadmap/fork-intel/README.md`.

## Usage

- `/fork-radar` — full pass: discover → diverge → screen → triage → preapprove → report
- `/fork-radar refresh` — same, but re-screen and re-triage cached candidates
- `/fork-radar report` — regenerate the report and queue from what is already in the database
- `/fork-radar import <sha>` — carry out one import from the queue (see step 6)

## What to do

### Step 1: Run the pipeline

```bash
cd /d/github/mRemoteNG
python .project-roadmap/fork-intel/fork_intel.py discover --since-months 6
python .project-roadmap/fork-intel/fork_intel.py diverge
python .project-roadmap/fork-intel/fork_intel.py screen
python .project-roadmap/fork-intel/fork_intel.py triage
```

Each stage is cached by commit SHA, so a repeat run is cheap. `triage` calls an AI CLI and is the slow stage — run it in the background and wait for the notification rather than polling.

If a stage reports API failures, check the budget with `gh api rate_limit` before re-running.

### Step 2: Pre-approve through counter-opinions

```bash
python .project-roadmap/fork-intel/fork_intel.py preapprove --reviewers codex,gemini
```

Every tier A/B candidate goes to two independent model families as a **read-only, opinion-only** review, with our project direction in the prompt. The consensus rule is strict and lives in code:

- all reviewers APPROVE + all confirm alignment + no security flag → `pre-approved`
- any dissent, any missing answer, any flag → `manual-review`, with the dissenting reason preserved

A missing vote is never consent, and a security flag can never be voted away.

### Step 3: Report

```bash
python .project-roadmap/fork-intel/fork_intel.py report
```

Writes `.project-roadmap/fork-intel/reports/<date>_fork-radar.md` and `IMPORT_QUEUE.md`.

### Step 4: Present to the user

Summarise, do not dump the report:

- counts per tier, and how many are pre-approved vs. needing manual review
- for each **pre-approved** candidate: one line — what it does, which fork, size, and why both reviewers approved
- for each **quarantined** candidate that looks valuable: one line with the flag that gated it
- anything the reviewers disagreed about, with the dissenting reason

Then ask (AskUserQuestion) which candidates to import. Never import on your own initiative, not even a pre-approved one — pre-approval means "a human can decide quickly", not "no human needed".

### Step 5: Verify before proposing an import

For each candidate the user picks, read the actual diff first (`gh api repos/<fork>/commits/<sha>`), and confirm:

- it is not already implemented differently in our tree
- it touches nothing outside `mRemoteNG/`, `mRemoteNGTests/`, `mRemoteNGSpecs/` unless the user agreed to that
- it does not add a dependency, telemetry, or an interactive test

If any of these fail, say so and stop — do not adapt the change silently.

### Step 6: Import (only after approval)

```bash
cd /d/github/mRemoteNG
git remote add fi-<owner> https://github.com/<fork>.git
git fetch fi-<owner> --depth=50 <sha>
git cherry-pick -x <sha>
```

Then, mandatory:

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File "D:/github/mRemoteNG/build.ps1"
bash run-tests-core.sh headless
```

Both must be green before the commit stands. Keep the original author (cherry-pick does this), keep the `-x` line, and add a `Ported-from: <commit URL>` trailer. If the patch conflicts, do **not** force it — reimplement by hand and credit the original author in the commit body.

Finally, record the decision so it never resurfaces:

```bash
python .project-roadmap/fork-intel/fork_intel.py mark --sha <sha> --decision imported --note "landed as <our sha>"
```

Use `--decision rejected` or `deferred` for the ones you did not take, with a one-line reason.

### Step 7: Clean up

Remove the temporary remote (`git remote remove fi-<owner>`) and confirm `git status --short` is clean apart from intended changes.

## Important notes

- **Never execute fork code.** No running their build scripts, tests or workflows — not even to "check if it works".
- **Quarantine is routing, not rejection.** A flagged change may be perfectly good; it just needs a human to read the diff.
- **Never contact fork authors automatically.** Any outreach is written by a human, in their own words.
- **Push and GitHub comments always require explicit confirmation**, same as `/mremoteng-fix-repo`.
- Tests: `python .project-roadmap/fork-intel/test_fork_intel.py` after touching filters or scoring — the fixtures are calibrated against real observed forks.

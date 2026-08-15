<p align="center">
  <img width="450" src="https://github.com/mRemoteNG/mRemoteNG/blob/mRemoteNGProjectFiles/Header_dark.png">
</p>

# mRemoteNG — Community Edition

<blockquote>

**This fork is alive.** We love mRemoteNG and we're committed to keeping it moving forward. This Community Edition ships regular releases with security patches, bug fixes, and long-requested features — backed by proper CI, automated tests, and builds for x64, x86, and ARM64.

Full transparency: this project is built by humans and AI working together, and it only works **together with you**. Fixes are developed and verified by an automated pipeline — 6,600+ automated tests, adversarial cross-review between independent AI models — and we test too: **we run mRemoteNG every day on the latest build, as our daily driver**. What we cannot do is reproduce *your* setup — your network, your servers, your locale, the specific state that triggers your bug. That gap is the honest limit of our testing, and it is exactly where you come in. When a fix for your issue lands in a nightly, *your* test is what actually verifies it. Sometimes a fix is right on the first try; sometimes it takes rounds, and your logs, traces, and even small suggestions are what get it there. We provide the engineering, the infrastructure, and continuously updated models; you provide the ground truth we cannot generate ourselves. How it works in detail: [#167](https://github.com/robertpopa22/mRemoteNG/issues/167).

*— Robert & contributors (human + AI)*

</blockquote>

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/latest">
    <img alt="Latest Stable" src="https://img.shields.io/badge/latest%20stable-v1.82.0-brightgreen?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly">
    <img alt="Nightly" src="https://img.shields.io/badge/nightly-rolling-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/actions">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/pr_validation.yml?style=for-the-badge&label=CI">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly">
    <img alt="Nightly" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/nightly.yml?style=for-the-badge&label=Nightly&color=blueviolet">
  </a>
  <a href="https://sonarcloud.io/project/overview?id=robertpopa22_mRemoteNG">
    <img alt="SonarCloud" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/sonarcloud.yml?style=for-the-badge&label=Sonar">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/security/code-scanning">
    <img alt="CodeQL" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/codeql.yml?style=for-the-badge&label=CodeQL">
  </a>
  <a href="https://www.qodo.ai/">
    <img alt="Qodo Review" src="https://img.shields.io/badge/Qodo-AI%20Review-8A2BE2?style=for-the-badge">
  </a>
  <a href="https://www.virustotal.com/gui/file/026b8a161db68b88e5fff3b734d7d5c7c34168384327e0bf3c53b11d26df5881">
    <img alt="VirusTotal" src="https://img.shields.io/badge/VirusTotal-0%2F75%20clean-brightgreen?style=for-the-badge">
  </a>
  <a href="COPYING.TXT">
    <img alt="License" src="https://img.shields.io/badge/license-GPL--2.0-green?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases">
    <img alt="Total Downloads" src="https://img.shields.io/badge/total%20downloads-831-green?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/stargazers">
    <img alt="Stars" src="https://img.shields.io/github/stars/robertpopa22/mRemoteNG?style=for-the-badge&color=yellow">
  </a>
</p>

---

## Downloads

Two live releases, always:

| Release | Version | Description |
|---------|---------|-------------|
| **[Stable](https://github.com/robertpopa22/mRemoteNG/releases/latest)** | v1.82.0 | Latest tagged release — all platforms. **Recommended.** |
| **[Nightly](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly)** | rolling | Rebuilt and overwritten on every push to `main`. Latest changes, fully tested. |
| **[Legacy](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.76.20)** | v1.76.20 | Last .NET Framework 4.x release. |

### Download matrix — Stable v1.82.0

| Variant | x64 | x86 | ARM64 |
|---------|-----|-----|-------|
| Framework-dependent (~21 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-x64.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-x86.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-arm64.zip) |
| Self-contained (~108-150 MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-x64-selfcontained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-x86-selfcontained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.82.0/mRemoteNG-v1.82.0-arm64-selfcontained.zip) |

Nightly builds (ZIP + MSI, x64) live on the [nightly release](https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly), refreshed every push.

**Framework-dependent** requires [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0). **Self-contained** includes the .NET runtime — no prerequisites. **MSI** installs to Program Files with desktop & Start Menu shortcuts.

### How releases work

mRemoteNG ships entirely from GitHub Releases with a deliberately small, predictable model: **one rolling `nightly`** (everything on `main`, overwritten on each push) and **immutable `vX.Y.Z` stable tags** — the newest of which is exactly what the app's built-in *Check for updates* reports (`releases/latest`). We chose this over the old multi-channel, self-hosted update-feed setup because it is simpler and fully GitHub-native: no update server to run, no per-channel text files to keep in sync, and a single obvious source of truth for both users and the app. Fewer moving parts, less to break, easier to trust.

---

## Features

16 protocols supported: **RDP**, **VNC**, **SSH**, **Telnet**, **HTTP/HTTPS**, **rlogin**, **Raw Socket**, **PowerShell Remoting**, **AnyDesk**, **VMRC** (VMware), **MSRA** (Remote Assistance), **OpenSSH** (native Windows), **Winbox** (MikroTik), **WSL**, **Terminal**, **Serial** (COM port).

**Security:** PBKDF2 600K iterations, HTTPS-only vaults, SSH key wipe, AnyDesk command injection prevention, LDAP sanitization, owner-only ACL on the PuTTY credential pipe, master-password re-authentication on password copy/reveal, 4 CodeQL alerts fixed.

**Enterprise:** Self-contained builds (zero prerequisites), ADMX/ADML Group Policy templates, connection audit logging, JSON export, protocol/tag filtering.

**Performance:** Startup optimized to **under 1 second** with 200 connections (down from 10-30s). WMI queries, plugin loading, and IE emulation deferred to background threads. XML deserialization uses O(1) dictionary lookups instead of O(n) attribute scans.

**Recent additions** (nightly, ported from upstream and adapted): *Clear Cached RDP Credentials* action (drop the stale `TERMSRV/<host>` entry that overrides your configured credentials), *Use Redirection Server Name* RDP property for load-balance redirects (GNOME Remote Desktop `--system`), Explorer-style slow-click rename in the connection tree (opt-in), RD Gateway access-token inheritance from parent folders.

**Quality:** 6,632 automated tests (0 failures), 0 analyzer warnings, 5-level code quality pipeline (Roslynator + Meziantou + SonarCloud + CodeQL + Qodo AI Review), x64/x86/ARM64. 853 upstream issues triaged (712 released, March 2026 snapshot); 89 reports from this fork's own users, 83 closed. SonarCloud Quality Gate green: reliability, security and maintainability all A, 81.6% coverage on new code, 0.6% duplication, 100% of security hotspots reviewed.

For detailed usage, refer to the [Documentation](https://mremoteng.readthedocs.io/en/latest/).

### Antivirus — 0/75 on VirusTotal

mRemoteNG uses Windows APIs (SendInput, DPAPI, COM Interop for RDP) that can trigger antivirus heuristic false positives. We worked directly with AV vendors to resolve all detections:

| Date | VirusTotal Score | Action |
|------|-----------------|--------|
| 2026-03-03 | 8/66 flagged | Submitted false positive reports to all flagging vendors |
| 2026-03-05 | Xcitium confirmed fix | Comodo/Xcitium whitelisted mRemoteNG |
| 2026-03-06 | **0/75 — clean** | BitDefender fixed `IL:Trojan.MSILZilla` → cascaded to 7 OEM vendors |

Current scan: [VirusTotal](https://www.virustotal.com/gui/file/026b8a161db68b88e5fff3b734d7d5c7c34168384327e0bf3c53b11d26df5881) — **0 detections across 75 engines**.

If your antivirus flags mRemoteNG, please see [Antivirus False Positive Guide](docs/ANTIVIRUS_FALSE_POSITIVE.md).

---

## How We Build This — AI-Assisted Development

This project uses an AI orchestrator (Python, ~6,900 LOC) coordinating multiple AI agents against a
backlog of 800+ upstream issues. It did not start that way, and the road here was not smooth —
the history is worth keeping, because most of what the harness does today exists because an earlier
version failed at it.

<details>
<summary><b>How the harness evolved — five generations, and what broke in each</b></summary>

**Gen 1 — one model, hand-driven (Feb 2026).** A single model, prompted manually, one issue at a
time. It worked, and it did not scale: every fix needed a human to open the issue, paste context,
read the diff, run the build. Throughput was bounded by the human, not the model. This is the
baseline the project's measurements still compare against.

**Gen 2 — three models, poorly coordinated.** Adding models did not simply add throughput. Agents
overwrote each other's work, re-fixed what was already fixed, and disagreed with no mechanism to
resolve the disagreement. Failures were silent: an agent would report success while its edit never
reached disk, or reach disk and never build. The lesson that shaped everything after: *parallel
agents without a protocol are slower than one agent, not faster.*

**Gen 3 — the orchestrator.** A Python controller took ownership of the loop: issue sync, triage,
agent dispatch, and — critically — **build/test gating between every step**, so no agent could
declare victory unverified. Roles were separated by strength rather than availability (fast triage
vs. deep multi-file work vs. bulk transforms). Silent failures became loud.

**Gen 4 — supervision and self-healing.** Long runs died in ways a single loop could not survive:
hung agents, buffered logs that made a live process look dead, orphaned build processes, sessions
that mass-committed dirty trees. Gen 4 added a supervisor, an internal auto-flushed log (stdout
redirection on Windows buffers and lies), timeouts, and hard scope rules about what an agent may
touch. Most of `.project-roadmap/LESSONS.md` is the scar tissue from this phase.

**Gen 5 — adversarial review and human-facing discipline (current).** Verification moved from
"tests pass" to "an independent model family tried to break this and failed". Added on top: a hard
attempt budget (two failed premise-based fixes → the next build must ship instrumentation, not a
third guess), a blocking security tripwire, and honest reporter communication. This generation came
directly out of a public failure — four consecutive fixes on one issue missed, and a reporter said
so bluntly. The rules exist so the cost of a miss lands on the pipeline, not on the person who
reported the bug.

</details>

**The agent stack is maintained continuously** — every agent runs on its family's current flagship
model at maximum reasoning effort, and pinned versions are treated as drift to be fixed, not as
stability. Model releases are picked up as they ship. Five agents are in the loop today, and the
roster grows as capable models appear:

| Agent | Role in the loop |
|-------|------------------|
| **Claude** (Anthropic) | Main thread: root-cause analysis, multi-file fixes, WinForms / COM interop, and final review of every other agent's output |
| **Codex** (OpenAI) | Fast triage, single-file patches, and independent adversarial review via the `codex-rescue` contract |
| **Gemini** (Google) | Long-context analysis, bulk transforms, and independent adversarial review via the `gemini-rescue` contract |
| **Grok** (xAI) | Adversarial counter-opinion from a fourth model family — tasked with *refuting* a proposed fix, not confirming it |
| **Qodo** | AI review on pull requests, complementing SonarCloud + CodeQL |

Why several families rather than one strong model: correlated blind spots. A single model —
however capable — repeats its own mistakes under review. Independent families disagree, and the
disagreements are where wrong fixes get caught. Two recent examples from this repo: a proposed
focus fix was killed in review because it would have broken Quick Connect, and a follow-up
implementation was caught with a stale-state hole before it shipped. Both were found by an agent
whose explicit job was to break the fix, not to approve it.

Human direction sits on top: the maintainer directs the work, reviews what ships, and takes over directly when automated rounds fail (hard rule: after two missed fixes the pipeline ships instrumentation instead of a third guess, and after three failed rounds a human takes the issue). What the pipeline cannot do is reproduce *your* environment — which is why reporter testing is treated as the most valuable contribution this project receives, and why every fix announcement says plainly that it is an automated fix awaiting your verification.

### Guardrails: how an automated pipeline is kept safe

A pipeline that turns issue text — written by anyone on the internet — into code has one serious
risk, and it is worth naming publicly. It is **not** crude "ignore your instructions" injection.
It is a *plausible bug report whose obvious fix happens to be a vulnerability*: "connections only
work if I disable certificate validation", "encrypted files won't open on my other PC, use a fixed
key", "SSH fails unless host-key checking is off". Each reads like a genuine bug. Each fix would
pass all 6,400 tests, because weakening a security property breaks nothing functional.

So the rules are mechanical, not aspirational:

- **Reporter input is data, never instructions.** A report describes a *symptom*; it does not get
  to name the fix. A stated cause, file, or patch is a hypothesis to verify against the source.
- **A security tripwire blocks delivery, not just review.** `scripts/security-tripwire.sh` runs as
  a pre-commit hook and refuses any change touching cryptography, key derivation, credential
  handling, authentication, database connectors or transport ACLs — or introducing security-relevant
  tokens anywhere in the tree. The automated pipeline cannot override it; only a human can, and the
  commit must record which security property was examined and why it still holds.
- **Every diff passes a security lens** before it is built: does this weaken certificate or
  host-key validation, key derivation, credential storage, authorization, ACLs, input validation,
  or the release path?
- **When a fix would weaken a security property, the answer is a different fix — or an honest
  explanation to the reporter.** Never the weakening. If your issue is closed with "we won't do
  that, here's why", this is why.
- Issue-sourced requests to change CI, signing, tokens or release infrastructure are never acted on.

**We will keep hardening this.** These guardrails are a floor, not a finished design — the roadmap
includes signed commits, branch protection with required checks, tighter least-privilege scoping for
the automation's credentials, and provenance for release artifacts. As the pipeline does more, the
constraints around it get tighter, not looser. If you spot a gap in this model, open an issue —
that report is as valuable as any bug report, and it will be treated the same way.

**Key results:**

- **712/853 issues addressed (83.5%)**, 1,400+ commits, 7 regressions (1.2%)
- **Cost:** ~$320 total, stabilized at $1.49/commit (down from $4.02 on day 1)
- **Best session:** Codex Spark resolved 89/104 issues (86%) autonomously in a single run
- **Quality:** 5,247 analyzer warnings → 0 (drifted back to 181 and re-cleared 2026-08-13 — zero is a state to maintain, not a milestone)
- **Code review:** every fix now goes through **independent Codex *and* Gemini counter-opinions** before commit — each reviewer re-derives the root cause from the source first, so agreement means two models reached the same diagnosis independently, and disagreement surfaces bad fixes (in a recent batch the dual review caught and discarded an incorrect proposed fix before it shipped)
- **4 upstream PRs backported:** URL injection fix, AD Protected Users, VNC Caps Lock, RDP resize

The complete research documentation is in [`scientific-paper/`](scientific-paper/):

| Document | What's inside |
|----------|---------------|
| [**PAPER.md**](scientific-paper/PAPER.md) | Full research paper — hypothesis, architecture, results, discussion |
| [METHODOLOGY.md](scientific-paper/METHODOLOGY.md) | Formal methodology, instruments, metrics, baseline |
| [RELATED_WORK.md](scientific-paper/RELATED_WORK.md) | Comparison with SWE-bench, Devin, Aider, AutoCodeRover, MetaGPT |
| [COST_ANALYSIS.md](scientific-paper/COST_ANALYSIS.md) | Detailed cost breakdown and learning curve |
| [FAILURE_CATALOG.md](scientific-paper/FAILURE_CATALOG.md) | Post-mortems: 31-hour disaster, 7 regressions, Codex repo wipe |
| [EVIDENCE.md](scientific-paper/EVIDENCE.md) | Verifiable data trail (git history, CI artifacts, metrics) |

---

## What's Next

*Figures below were re-verified against live sources on 2026-08-15. Where a number is historical
(a snapshot from an earlier phase) it is dated as such.*

### 6.1. Issue Triage — Upstream backlog cleared, fork inbox live

**Upstream backlog (snapshot, 2026-03-02).** All 853 upstream issues tracked at the time were
triaged and classified:

| Status | Count | % |
|--------|-------|---|
| released | 712 | 83.5% |
| wontfix | 116 | 13.6% |
| duplicate | 25 | 2.9% |

**Post-triage verification (2026-03-02):** All 195 `testing`-status issues were resolved: 179 bulk-verified via commit hash validation, 3 manually verified, 13 reclassified during wontfix correction. Wontfix repass found 38% imprecision in AI triage (47/123 were implementable); corrected to 116 with individual justifications.

**Status definitions:**
- **released** — fix committed, build/test verified, included in a release
- **wontfix** — classified as out-of-scope (upstream limitation, requires hardware, or not reproducible)
- **duplicate** — merged with another issue tracking the same root cause

**The fork's own inbox (live, 2026-08-15).** Since the fork started accepting reports directly,
**89 issues have been opened by external reporters and 83 are closed**; 6 issues are open, all of
them either fixed and awaiting reporter confirmation or deliberately left open with the reason
stated in the thread. The upstream tracker now stands at 873 issues followed, against 841 currently
open upstream.

This inbox is the part that matters day to day: it is where the pipeline meets real users, and
where its failures are visible. See [#167](https://github.com/robertpopa22/mRemoteNG/issues/167)
for how reports are handled and what we ask of reporters.

### 6.2. Code Quality — Five Levels Operational, Zero Warnings

**5,247 analyzer warnings → 0** across 100+ files in a single session using parallel AI agents (Claude Opus + Sonnet; later passes added Codex and Gemini in parallel).

**Zero is a state to maintain, not a milestone to announce.** The backlog came back: by August 2026
it had drifted to 181 warnings — 139 of them from a single missing `<Nullable>` setting in the test
project — and was cleared again on 2026-08-13. Two lessons worth recording: an incremental build
re-emits nothing for unchanged projects, so a "clean" build can be an artefact of not rebuilding
(verify only after forcing a full rebuild); and a warning sweep is not cosmetic — that pass
surfaced four genuine defects, including a primary key built from a nullable column lookup and an
unguarded dereference in a log message.

| Phase | What was done | Count fixed |
|-------|---------------|-------------|
| **Autofix** | CA1507 `nameof`, CA1822 `static`, CA1805 defaults, CA1510 `ThrowIfNull`, CA2263 generics, CA1825 `Array.Empty` | ~1,300 |
| **String comparisons** | MA0006 `string.Equals`, CA1309/CA1310 `StringComparison`, CA1304 `IFormatProvider`, MA0074 overloads | ~400 |
| **Collection safety** | MA0002 `StringComparer` on Dictionary/HashSet, MA0015/MA0016 enum comparison | ~350 |
| **Misc fixes** | CA1806, CA2201, CA1069, CA1305, CA1872, CA1850, CA1869, CA2249, RCS1075 | ~200 |
| **Suppressed** | 46 architectural/legacy rules demoted in `.editorconfig` (CA1711, CA5351, MA0062, etc.) | ~3,000 |

**SonarCloud bugs fixed (beta.6):** S2259 (null reference ×6), S2583 (dead branch), S4275 (getter/setter mismatch ×2), S1751 (no-op loop ×2), S3903 (missing namespace ×2), S3456 (redundant ToCharArray ×3), S2674 (unchecked Read), MA0037 (stray semicolon ×4). All ObjectListView issues (25) dismissed as won't fix — vendored dependency outside our control.

**SonarCloud on fork (live, 2026-08-15): Quality Gate green.** Reliability, security and
maintainability all rate A, coverage on new code is 81.6% against an 80% threshold, duplication
0.6%, and 100% of security hotspots are reviewed.

Getting there was not a matter of adjusting thresholds. The gate went red on the security rating
with six open findings, and each was examined on its merits: the private key a credential vault
hands to PuTTY was being written to a temp file with a predictable name and an inherited DACL (a
real weakness — now a random name and an owner-only DACL applied at creation); two regexes running
over remote-influenced input had no timeout; the RSA "weak key" findings were false positives on a
method that imports an existing key rather than generating one, and were made unambiguous rather
than suppressed; and one hard-coded-credential finding is a genuine false positive on 1Password API
field metadata, left annotated rather than obfuscated to please a scanner.

Then the gate stayed red on coverage, for a reason worth admitting: 63 of the uncovered lines were
a maintenance script written the day before, being measured as product code. Tooling that never
reaches a user is now scoped out of analysis, and the rest was covered with real tests rather than
exclusions — including the SQL diagnostics that only execute while a save is already failing.

The earlier "PASSED — A/A/A, 80.7% coverage" figure was from the March 2026 upstream-PR push and
was left in this README long after it stopped being true. That is exactly the failure mode this
revision is meant to end.

`TreatWarningsAsErrors` enforced for compiler rules (CS0168, CS0219, CS0162, CS0164). Next: extend to analyzer rules once stable.

### 6.3. Human Testing — What We Can and Cannot Cover

**We use mRemoteNG ourselves, every day, on the latest build.** That is the first line of human
testing and it catches a real class of problems: startup regressions, broken tab handling, settings
that do not persist, anything that makes the app unpleasant to live with.

What it cannot cover is *someone else's environment* — a specific SQL Server version, a MariaDB in
strict mode, an RDP gateway, a locale where the regional format differs from the display language,
a machine where a session drops at exactly the wrong moment. Several of the hardest bugs fixed here
were invisible to both the test suite and our daily use, and only became findable when the reporter
sent a log. That gap is the honest limit of testing on our side, and it is why reporter
confirmation is treated as the real verification.

**Why automated tests alone are not enough:** beta.5 shipped 7 AI-introduced regressions out of 585
changes that passed all 6,201 automated tests at the time. A ~1.2% miss rate sounds tolerable until
you look at what got through — one regression (PuTTY root save) would have silently destroyed a
user's connections.

**Protocol:** a manual session at every release, focused on UX flows that cannot be unit tested:

- Tree navigation: click, double-click, drag-drop, context menu — no phantom tabs
- Tab management: switch, close, reorder — no focus stealing, no hangs
- Save/load round-trip: `confCons.xml` survives save → close → reopen → save
- COM lifecycle: connect → disconnect → reconnect → close — no RCW crashes
- Settings persistence: change settings → restart → verify persistence (especially Portable mode)

### 6.4. Remaining Unsolved Problems — What Still Doesn't Work

*Rewritten 2026-08-15. The previous version of this table described the March 2026 upstream-PR
push and had gone stale — items were listed as unsolved that were fixed months ago, and the
genuinely open problems below were missing entirely. The six open security findings that headed
this list the same day are now resolved and the SonarCloud gate is green; that entry is gone,
which is the most useful thing that can happen to a row in this table.*

| # | Problem | Status | Why it's hard |
|---|---------|--------|---------------|
| 1 | **Virtual list row-count desync** ([#149](https://github.com/robertpopa22/mRemoteNG/issues/149)) | Guarded, not root-caused | `SetVirtualListSize` swallows an `ArgumentOutOfRangeException` when assigning `VirtualListSize`; the control then reports a stale row count while the model has grown, and an expand computed a redraw range of 427 against a 41-row list. The crash is contained by a guard and the condition is now instrumented, but the failing assignment has never been reproduced. It lives in vendored ObjectListView code shared by every list in the app, so a speculative fix is worse than the guard |
| 2 | **VNC disconnect race** ([#166](https://github.com/robertpopa22/mRemoteNG/issues/166)) | Narrowed, not closed | VncSharpCore polls the framebuffer on its own thread and marshals connection loss back with `Control.Invoke`; if the handle is gone, that throws on a thread we do not own and kills the process. Our teardown now stops the session while the handle is alive, which closes the common path — but the remaining window belongs to the package (v1.2.1), not to us |
| 3 | **Legacy SQL upgraders swallow every provider error** | Logged, narrowing deferred | The two oldest schema upgraders catch `DbException` and skip. Correct for the expected duplicate-object error, wrong to do silently for a permission or connectivity failure. They now classify and log what they skipped; tightening the catch waits on real field logs, because guessing the provider error set wrong turns a working legacy import into a hard failure |
| 4 | **NUnit parallelization impossible** | Architectural | Shared mutable singletons (`DefaultConnectionInheritance.Instance`, `Runtime.EncryptionKey`, `Runtime.ConnectionsService`) make fixture-level parallelism race. Multi-process isolation (9 groups, sliding-window concurrency) works but is slower. Fixing it properly means dependency injection throughout the application — a multi-month refactor |
| 5 | **MSBuild output path vs. `dotnet test`** | Workaround | MSBuild outputs to `bin/x64/Release/`; `dotnet test --no-build` against the csproj looks in `bin/Release/`. Coverage cannot be collected through the standard `dotnet test --collect` path; the `dotnet-coverage` workaround functions but adds a tool dependency |
| 6 | **Upstream PR [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) still open** | Waiting, understandably | 765 files, ~64K insertions. A diff that size is genuinely hard to review responsibly, and the upstream maintainers built this project — their caution is reasonable. The lesson is on our side: future contributions go upstream as smaller, focused PRs |
| 7 | **Reporter confirmation is the bottleneck** | Structural | Of the fork's closed external issues, a large share were closed on code and test evidence without the reporter ever replying. Those fixes are probably right, but "probably" is the honest word. There is no way around this other than making it easy and worthwhile to reply — which is what the transparency work is for |

### 6.5. Gen 5 — Adversarial Verification and Bounded Autonomy (current)

Gen 5 is running, and it is not the "fully autonomous maintenance" that earlier drafts of this
section aimed at. The target changed because the failure mode changed: throughput stopped being the
constraint, and *confidence* became it. An agent that ships four wrong fixes quickly is worse than
one that ships a right fix slowly.

What Gen 5 actually does:

- **Verification is adversarial, not confirmatory.** Before a non-trivial fix ships, an independent
  model family is tasked with *breaking* it — not reviewing it politely. Findings from that pass
  have killed fixes that would have shipped, including one that would have broken Quick Connect.
- **Autonomy is bounded by an attempt budget.** Two failed fixes on an unproven premise, and the
  next build must ship instrumentation instead of a third guess. Three failed rounds, and a human
  takes the issue. This is enforced in the pipeline's own instructions, not left to judgement.
- **Security has a mechanical stop.** A tripwire blocks delivery of any change touching
  cryptography, credentials, authentication or transport ACLs, regardless of test results, because
  weakening a security property breaks no test.
- **Transparency is part of the output.** Every fix announcement states that it is an automated fix
  awaiting the reporter's verification, and every guard that is not a root-cause fix says so.

**Target state:** not "no humans" — humans on the decisions that need judgement (security impact,
scope, whether a repeated failure needs a different approach) and the pipeline on everything that
can be verified mechanically.

### 6.6. Upstream Convergence

PR [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) (beta.6) passed the SonarCloud Quality
Gate on 2026-03-01 after resolving 6 security vulnerabilities (S2068 ×3, S8264 ×2, S8233 ×1) and 50
security hotspots reviewed as SAFE. **As of 2026-08-15 it is still open** — 765 files, ~64K
insertions. PR [#3188](https://github.com/mRemoteNG/mRemoteNG/pull/3188) (beta.5) has since been
closed as its predecessor.

Upstream currently has **841 open issues** (873 tracked by our orchestrator). This fork addressed
712 of the 853 triaged in the March snapshot. We understand a diff that size takes time to review
responsibly — the upstream team built this project and their careful stewardship is what makes it
worth contributing to. The correction is ours to make: **future upstream contributions go as
smaller, focused PRs**, because a 765-file PR asks more of a reviewer than is reasonable.

Additionally, 4 upstream copilot draft PRs (#3177, #3176, #3154, #3171) have been reviewed and their fixes backported to our fork's `main` branch, ahead of upstream merge.

The convergence runs in both directions. In July 2026 every upstream commit since the March sync point (~230, mostly dependency bumps) was triaged against this fork; the substantive pieces we lacked were ported and adapted: the *Use Redirection Server Name* RDP property ([#3314](https://github.com/mRemoteNG/mRemoteNG/pull/3314), extended here with SQL-schema persistence upstream does not have), the *Clear Cached RDP Credentials* action ([#3315](https://github.com/mRemoteNG/mRemoteNG/pull/3315)), Explorer-style slow-click rename ([#3251](https://github.com/mRemoteNG/mRemoteNG/pull/3251)), RD Gateway access-token inheritance ([#3243](https://github.com/mRemoteNG/mRemoteNG/pull/3243)), and the PuTTY credential-pipe ACL hardening class. The same review surfaced and fixed a pre-existing CSV export bug of our own (#141) — porting with independent model review cuts both ways. Dependencies were synced to upstream levels at the same time (SqlClient 7, log4net, WebView2, AWSSDK).

### 6.7. The Bigger Picture

This project demonstrates a reproducible model: **orchestrator + supervisor + multi-model AI + human oversight** applied to a legacy codebase with a large backlog.

The model is not specific to mRemoteNG. Any project with hundreds of open issues, a test suite, and a build system could benefit from the same approach. The orchestrator code is ~6,900 lines of Python — not trivial, but not a research project either.

**The economics make it viable:** ~$1.50/commit, 24/7 operation, no burnout, no context switching. This is complementary to human developers, not a replacement — humans set direction, review output, and handle the 30% that AI cannot.

*Note: This section has not yet undergone peer review. Projections and priorities are subject to revision.*

---

## Release History

| Version | Date | Highlights |
|---------|------|------------|
| **v1.82.0** | 2026-07-02 | First stable of the 1.82 line (.NET 10). GitHub-Releases-only update check, WebAuthn/FIDO2 + Entra ID auth, MSI installer (WiX 6), MS Remote Desktop + MobaXTerm importers, host-status LED icons, startup ~10s→1.2s, plus the 2-release model and a repo-wide simplification cleanup |
| **v1.81.0** | 2026-03-02 | First stable of the 1.81 line — SonarCloud Quality Gate A/A/A, 0 analyzer warnings, upstream PR [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) |
| **v1.81.0-beta.6** | 2026-03-01 | SonarCloud Quality Gate pass on upstream PR #3189 — 6 security vulnerabilities fixed, 50 hotspots reviewed, 5,247→0 analyzer warnings, 4-level code quality (Roslynator + Meziantou + SonarCloud + CodeQL), coverage collection via `dotnet-coverage`, workflow permissions hardened (S8264/S8233), upstream sync with v1.78.2-dev |
| **v1.81.0-beta.5** | 2026-02-27 | 7 manual-testing regressions fixed, AV false positive hardening (`SendInput`, `DefaultDllImportSearchPaths`, VirusTotal in CI), `PortableSettingsInitializer` for .NET 10, 5,963 tests |
| **v1.81.0-beta.4** | 2026-02-25 | AV hardening, test suite expansion 2,916 → 5,963 via `TestCaseSource` parametrization |
| **v1.81.0-beta.3** | 2026-02-24 | 585 issues addressed (70% of 838), 744 commits, 7 new protocols, 81s→ms deserialization fix, orchestrator v2 (Claude-only, self-healing supervisor) |
| **v1.81.0-beta.2** | 2026-02-15 | 2,554 nullable warnings fixed (100% clean, 242 files), testable architecture via DI |
| **v1.80.2** | 2026-02-14 | AlwaysShowPanelTabs initialization fix |
| **v1.80.1** | 2026-02-13 | Security patch — AnyDesk command injection, Process.Start hardening, .NET 10.0.3 |
| **v1.80.0** | 2026-02-10 | Self-contained builds, 6 security hardening items, external tool tokens, JSON export, live theme switching, 830-issue triage complete |
| **v1.79.0** | 2026-02-08 | 26 bug fixes, 81 pre-existing test failures fixed, LDAP sanitizer, .NET 10 with x64/x86/ARM64 |

Full details: [CHANGELOG.md](CHANGELOG.md) | [All releases](https://github.com/robertpopa22/mRemoteNG/releases)

---

## Build from Source

```powershell
# Requires Visual Studio BuildTools (VS2026 or VS2022) with .NET SDK
# Full build (~15s on 48-thread Threadripper):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1

# Fast incremental (~9s, skips restore):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1 -NoRestore

# Self-contained (embeds .NET runtime, ~108-116MB output):
pwsh -NoProfile -ExecutionPolicy Bypass -File build.ps1 -SelfContained
```

> **Note:** `dotnet build` does **not** work — the project has COM references (MSTSCLib for RDP). `build.ps1` uses full MSBuild via VS BuildTools and auto-detects the newest VS installation.

### Code Quality

| Level | Tool | Scope | Status |
|-------|------|-------|--------|
| 1 | **Roslynator + Meziantou Analyzers** | Every local build | Active |
| 2 | **SonarCloud** | Push to `main` — quality gate | [![SonarCloud](https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/sonarcloud.yml?label=SonarCloud&style=flat-square)](https://sonarcloud.io/project/overview?id=robertpopa22_mRemoteNG) |
| 3 | **CodeQL** | Push to `main` + weekly — security scanning | [![CodeQL](https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/codeql.yml?label=CodeQL&style=flat-square)](https://github.com/robertpopa22/mRemoteNG/security/code-scanning) |
| 4 | **.NET Analyzers** | `AnalysisLevel=latest-recommended` | Active |
| 5 | **[Qodo Code Review](https://www.qodo.ai/)** | On-demand AI review | Complements static analysis |

**0 analyzer warnings** in main project. `TreatWarningsAsErrors` enforced for compiler rules (CS0168, CS0219, CS0162, CS0164). 46 noisy/architectural rules suppressed in `.editorconfig` for legacy WinForms code. Next: extend to analyzer rules once stable.

**Qodo Code Review** catches logic bugs that static analyzers miss — bounds checks, SQL schema mismatches, plaintext credential patterns, and validation gaps. Run on-demand via `scripts/qodo-review.sh`. Found and fixed: SQL INSERT missing 6 columns, URL scheme injection vulnerability (upstream [#3177](https://github.com/mRemoteNG/mRemoteNG/issues/3177)).

---

## Testing

```powershell
# Recommended (bash runner, 9 groups, max 2 concurrent, ~80s):
bash run-tests-core.sh

# PowerShell wrapper (builds first):
pwsh -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -Headless

# Skip build (use existing binaries):
pwsh -NoProfile -ExecutionPolicy Bypass -File run-tests.ps1 -Headless -NoBuild
```

**6,632 tests**, 9 groups with sliding-window concurrency (max 2) + 2 isolated, 0 failures.

Multi-process parallelism is required because the production code uses shared mutable singletons — NUnit fixture-level parallelism causes race conditions. Each `dotnet test` process gets isolated static state.

| Group | Namespace | Tests |
|-------|-----------|-------|
| 1 | Connection | 1,113 |
| 2 | Config.Xml | 124 |
| 3 | Config.Other | 784 |
| 4 | UI | 374 |
| 5 | Tools | 394 |
| 6 | Security | 166 |
| 7 | Tree + Container + Credential | 178 |
| 8 | Remaining | 3,110 |
| 9 | Integration | 21 |
| Isolated | FrmOptions (GDI handle leak) | 2 |

---

## Upstream Relationship

This fork is based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`. We submit fixes back upstream:

- v1.79.0: PRs [#3105](https://github.com/mRemoteNG/mRemoteNG/pull/3105)–[#3130](https://github.com/mRemoteNG/mRemoteNG/pull/3130) (26 individual PRs)
- v1.80.0: [#3133](https://github.com/mRemoteNG/mRemoteNG/issues/3133) (consolidated status)
- v1.81.0-beta.5: [#3188](https://github.com/mRemoteNG/mRemoteNG/pull/3188)
- v1.81.0-beta.6: [#3189](https://github.com/mRemoteNG/mRemoteNG/pull/3189) — SonarCloud Quality Gate passed

---

## License

[GPL-2.0](COPYING.TXT)

## Maintained by

<a href="https://geseidl.ro/servicii-it"><img src="https://geseidl.ro/assets/icons/logo-green.png" alt="Geseidl Consulting Group" height="45"></a>

This Community Edition is maintained by **[Geseidl IT Solutions](https://geseidl.ro/servicii-it)**, part of [Geseidl Consulting Group](https://geseidl.ro) — an IT infrastructure, cloud, and cybersecurity services provider in Ploiești, Romania.

We use mRemoteNG daily across our managed infrastructure (50+ servers, 300+ client connections) and built this modernized edition because we needed it to be better. The research paper documenting the AI-assisted development process is available at [`scientific-paper/PAPER.md`](scientific-paper/PAPER.md).

[geseidl.ro/servicii-it](https://geseidl.ro/servicii-it) | [About us](https://geseidl.ro/despre-noi)

## Support the Project

If you find this fork useful, please consider giving it a star — it helps others discover the project and motivates continued development.

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/stargazers">
    <img alt="Star this repo" src="https://img.shields.io/github/stars/robertpopa22/mRemoteNG?style=for-the-badge&label=Star%20on%20GitHub&color=yellow">
  </a>
</p>

## Contributing

Submit code via pull request. See the [Wiki](https://github.com/mRemoteNG/mRemoteNG/wiki) for development environment setup.

---

<p align="center">
  <a href="https://make-it-count.ro">
    <img src="https://geseidl.ro/assets/icons/makeitcount-amprenta-gold.png" alt="makeitcount" height="60">
  </a>
  <br>
  <sub><em>Building better tools, one commit at a time. Make it count.</em></sub>
  <br>
  <sub><a href="https://make-it-count.ro">make-it-count.ro</a></sub>
</p>

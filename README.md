<p align="center">
  <img width="450" src="https://github.com/mRemoteNG/mRemoteNG/blob/mRemoteNGProjectFiles/Header_dark.png">
</p>

# mRemoteNG — Community Edition

<blockquote>

<strong>This fork is alive.</strong>

We love mRemoteNG and we're committed to keeping it moving forward. This Community Edition ships regular releases with security patches, bug fixes, and long-requested features — backed by proper CI, 2100+ automated tests, and builds for x64, x86, and ARM64.

<strong>The plan:</strong> work through the full <strong>830+ issue backlog</strong>, 100 at a time. Security first, then stability, then features. Every fix gets a release. Every release gets tested. Every issue gets a response.

<strong>How we work:</strong> Development runs in parallel across <strong>Claude Code</strong> (Anthropic), <strong>Gemini</strong> (Google), and <strong>Codex</strong> (OpenAI) — each AI agent tackling different issue batches simultaneously, with every change reviewed and merged by a human maintainer. A custom <strong>Issue Intelligence System</strong> — a git-tracked JSON database — follows every issue through its full lifecycle: triage → fix → test → release. Automated priority classification and templated GitHub comments ensure nothing falls through the cracks.

<strong>What's next:</strong> Once the backlog is current, ongoing maintenance — bug fixes, dependency updates, security patches — will run autonomously via <a href="https://docs.anthropic.com/en/docs/agents-and-tools/claude-code/overview">Claude Code</a>, continuously monitoring new issues and shipping fixes with minimal human intervention.

Full transparency: this project is built by humans and AI working together. We believe that's the future of open source.

<em>— Robert & contributors (human + AI)</em>

</blockquote>

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0-beta.2">
    <img alt="Beta" src="https://img.shields.io/badge/beta-v1.81.0--beta.2-orange?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/tree/main">
    <img alt="Next" src="https://img.shields.io/badge/next-v1.81.0--beta.3-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/actions">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/pr_validation.yml?style=for-the-badge&label=CI">
  </a>
  <a href="COPYING.TXT">
    <img alt="License" src="https://img.shields.io/badge/license-GPL--2.0-green?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/releases">
    <img alt="Total Downloads" src="https://img.shields.io/badge/total%20downloads-831-green?style=for-the-badge">
  </a>
</p>

## Latest Stable Release

<a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.76.20">
  <img alt="Stable v1.76.20" src="https://img.shields.io/badge/stable-v1.76.20-blue?style=for-the-badge">
</a>

The latest production-ready version of mRemoteNG. For most users, this is the recommended version.

---

## Beta Builds (Main Branch)

<a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0-beta.2">
  <img alt="Beta v1.81.0-beta.2" src="https://img.shields.io/badge/beta-v1.81.0--beta.2-orange?style=for-the-badge">
</a>

> [!IMPORTANT]
> These are high-velocity builds from the `main` branch. They include the latest .NET 10 updates, security hardening, and experimental features. Use these if you want to help test new functionality. A version is promoted to **Stable** only after at least 5 beta iterations.

| Variant | x64 | x86 | ARM64 |
|---------|-----|-----|-------|
| Framework-dependent (~21MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-x64.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-x86.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-arm64.zip) |
| Self-contained (~108-116MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-win-x64-SelfContained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-win-x86-SelfContained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.81.0-beta.2/mRemoteNG-v1.81.0-beta.2-win-arm64-SelfContained.zip) |

**Framework-dependent** requires [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0).
**Self-contained** includes the .NET runtime — no prerequisites needed.

---

<details>
<summary><strong>What's in v1.81.0-beta.2?</strong></summary>

### Highlight: Zero Nullable Warnings
This release is **100% nullable-clean**, having resolved all **2,554 CS8xxx warnings** across the entire codebase. This massive quality effort was coordinated by the **IIS Orchestrator** using AI agents to ensure strict null safety and type reliability.

### Architecture: Testable Connection Loaders
- **Decoupled Logic**: `SqlConnectionsLoader` and `XmlConnectionsLoader` now use **Dependency Injection**, removing hardcoded dependencies on interactive UI dialogs and SQL infrastructure.
- **New SQL Abstractions**: Introduced `ISqlDatabaseMetaDataRetriever` and `ISqlDatabaseVersionVerifier` interfaces.
- **Autonomous Integration Tests**: Added fully automated test suites for encrypted connection loading that run in CI without a live database or user input.

### Security
- **AnyDesk command injection prevention**: Validates IDs via `IsValidAnydeskId()` before process execution.
- Dependency update: `System.Drawing.Common` to 10.0.3 (GDI+ security fix).

See [CHANGELOG.md](CHANGELOG.md) for the full list.

</details>

<details>
<summary><strong>Previous release: v1.80.2</strong> (security patch, maintenance)</summary>

### Security Fixes (upstream merge)
- **AnyDesk command injection prevention** — `IsValidAnydeskId()` validates IDs before passing to process
- **Process.Start hardening** — `ProcessStartInfo` with `UseShellExecute` across all UI forms
- **URL format validation** in FrmAbout — rejects non-HTTP(S) URLs
- **Path validation** in NotificationsPage — prevents command injection via log file paths

### Dependency Updates
- .NET SDK 10.0.2 → 10.0.3 (runtime security patch)
- Removed 27 redundant System.* NuGet packages (now built-in to .NET 10)
- Updated AWS SDK packages

</details>

<details>
<summary><strong>Previous release: v1.80.0</strong> (self-contained builds, security hardening, new features)</summary>

### Highlights
- **Self-contained build variant** — includes .NET runtime, no install needed
- **6 security hardening items** — encryption keys, auth levels, PBKDF2 600K, SSH wipe, HTTPS vaults
- **External tool tokens** — `%PUTTYSESSION%`, `%ENVIRONMENTTAGS%`, `%SSHOPTIONS%`
- **Options panel stability** — Cancel properly reverts, SQL fields work
- **Batch file password fix** — comma in passwords no longer splits args

### New Features
- Self-contained (.NET embedded) build — no .NET runtime installation required
- JSON export, protocol/tag filtering, quick connect history
- Connection audit log, keyboard shortcuts panel
- Middle-click: open connection from tree, close tab
- Live theme switching (no restart), crash dialog auto-submit

[v1.80.0 release page](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.0)

</details>

<details>
<summary><strong>Previous release: v1.79.0</strong> (26 bug fixes, security hardening, test suite)</summary>

### Bug Fixes (26 total)
- **RDP:** SmartSize focus loss, fullscreen toggle guard, refocus after exit, RCW disconnect safety
- **PuTTY:** Provider failure handling, CJK session name decoding
- **UI:** Close panel race, tab drag autoscroll, tab close race under resize, empty panel close, config panel splitter, inheritance label width, connections panel focus
- **Data:** 1Password parser fix, SQL schema compatibility, SqlClient SNI runtime, default credential provider
- **Core:** Startup path fallback, XML recovery for corrupt configs, main form close cancel, master password autolock, PROTOCOL external tool token, settings path logging, password protection verification

### Quality
- 2179 tests passing (81 pre-existing failures fixed, 31 new tests added)
- .NET 10, 3-architecture support (x64, x86, ARM64)

[v1.79.0 release page](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0)

</details>

[View all releases](https://github.com/robertpopa22/mRemoteNG/releases)

---

## Features

The following protocols are supported:

* RDP (Remote Desktop Protocol)
* VNC (Virtual Network Computing)
* SSH (Secure Shell)
* Telnet (TELecommunication NETwork)
* HTTP/HTTPS (Hypertext Transfer Protocol)
* rlogin (Remote Login)
* Raw Socket Connections
* Powershell remoting
* AnyDesk

For a detailed feature list and general usage support, refer to the [Documentation](https://mremoteng.readthedocs.io/en/latest/).

---

## Installation

### Supported Operating Systems

- Windows 11
- Windows 10
- Windows Server 2022
- Windows Server 2019
- Windows Server 2016

### How to Update (Portable ZIP)

1. Download the ZIP for your architecture
2. Extract over your existing mRemoteNG installation
3. Your `confCons.xml` and settings are preserved

---

## Build from Source

```powershell
# Requires Visual Studio BuildTools (VS2026 or VS2022) with .NET SDK
powershell.exe -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

`build.ps1` auto-detects the newest VS installation. For manual builds, see `CLAUDE.md`.

---

## Upstream Relationship

This fork is based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`.
v1.79.0 fixes have individual PRs on upstream ([#3105](https://github.com/mRemoteNG/mRemoteNG/pull/3105)–[#3130](https://github.com/mRemoteNG/mRemoteNG/pull/3130)).
v1.80.0 consolidated status: [#3133](https://github.com/mRemoteNG/mRemoteNG/issues/3133).

---

## What's New

### v1.81.0-beta.2 (2026-02-15)
- **Zero Nullable Warnings** — 100% clean codebase (2,554 fixed) via IIS Orchestrator
- **Testable Architecture** — Decoupled `SqlConnectionsLoader` & `XmlConnectionsLoader` via DI
- **AnyDesk Security** — command injection prevention fix
- **100% Autonomous Loading Tests** — zero-UI integration tests for encrypted files

### v1.80.2 (2026-02-13)
- **Security patch** — AnyDesk command injection fix, Process.Start hardening, URL/path validation (upstream merge)
- **.NET 10.0.3** runtime patch + removed 27 redundant packages
- **CI improvements** — self-contained build matrix, actions/setup-dotnet v5

### v1.80.0 (2026-02-10)
- **Self-contained builds** — .NET runtime included, zero prerequisites
- **Security hardening** — PBKDF2 600K iterations, HTTPS-only vaults, SSH key wipe, stronger master passwords
- **New tokens** — `%PUTTYSESSION%`, `%ENVIRONMENTTAGS%`, `%SSHOPTIONS%` for external tools
- **Options panel fixes** — Cancel reverts properly, SQL fields work, panel tabs no longer corrupt layout
- **RDP improvements** — auto-resize on monitor hot-plug, async initialization
- **New features** — JSON export, protocol/tag filtering, connection audit log, live theme switching

### v1.79.0 (2026-02-09)
- **26 bug fixes** — RDP, PuTTY, UI, SQL, credential providers
- **81 pre-existing test failures fixed** — 2179 tests, zero failures
- **LDAP sanitizer** and process-start hardening
- **.NET 10** with x64, x86, ARM64 support

Full details: [CHANGELOG.md](CHANGELOG.md) | [All releases](https://github.com/robertpopa22/mRemoteNG/releases)

---

## Issue Intelligence System

This fork includes a custom **Issue Intelligence System** — a git-tracked JSON database that monitors the full upstream issue backlog (830+ issues) and automates triage, lifecycle tracking, and release communication.

**What it does:**
- Syncs issues and comments from both upstream and fork repositories via `gh` CLI
- Tracks each issue through a full lifecycle: `new` → `triaged` → `roadmap` → `in-progress` → `testing` → `released`
- Detects **iteration loops** — when a user reports a fix didn't fully resolve their issue, the system tracks re-fix cycles
- Posts templated comments to GitHub on status transitions (release notifications, acknowledgements)
- Generates markdown reports for triage sessions and releases
- Auto-classifies issues by priority based on labels and comment activity

**Scripts** (in `.project-roadmap/scripts/`):

| Script | Purpose |
|--------|---------|
| `Sync-Issues.ps1` | Fetch latest issues + comments from GitHub into local JSON DB |
| `Analyze-Issues.ps1` | Classify and prioritize — shows what needs immediate attention |
| `Update-Status.ps1` | Transition issues through lifecycle, post GitHub comments |
| `Generate-Report.ps1` | Create markdown reports for triage and releases |

**Current stats** (as of 2026-02-13): 831 issues tracked, 25 released, 8 urgent, 24 new comments detected.

See [.project-roadmap/issues-db/README.md](.project-roadmap/issues-db/README.md) for full documentation.

---

## License

[GPL-2.0](COPYING.TXT)

## Contributing

Submit code via pull request. See the [Wiki](https://github.com/mRemoteNG/mRemoteNG/wiki) for development environment setup.

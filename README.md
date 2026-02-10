<p align="center">
  <img width="450" src="https://github.com/mRemoteNG/mRemoteNG/blob/mRemoteNGProjectFiles/Header_dark.png">
</p>

# mRemoteNG — Community Edition

> Active fork with bug fixes, security hardening, and a complete test suite.
> Based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`.

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.0">
    <img alt="Stable" src="https://img.shields.io/badge/stable-v1.80.0-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/tree/release/1.81">
    <img alt="In Development" src="https://img.shields.io/badge/dev-v1.81.0-orange?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/actions">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/pr_validation.yml?style=for-the-badge&label=CI">
  </a>
  <a href="COPYING.TXT">
    <img alt="License" src="https://img.shields.io/badge/license-GPL--2.0-green?style=for-the-badge">
  </a>
</p>

---

## Latest Stable Release — v1.80.0

<a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.80.0">
  <img alt="Stable v1.80.0" src="https://img.shields.io/badge/stable-v1.80.0-blue">
</a>

| Variant | x64 | x86 | ARM64 |
|---------|-----|-----|-------|
| Framework-dependent (~21MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-x64.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-x86.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-arm64.zip) |
| Self-contained (~108-116MB) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-x64-selfcontained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-x86-selfcontained.zip) | [Download](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.80.0/mRemoteNG-v1.80.0-arm64-selfcontained.zip) |

**Framework-dependent** requires [.NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0) + [VC++ Redistributable](https://aka.ms/vs/18/release/vc_redist.x64.exe).
**Self-contained** includes the .NET runtime — no prerequisites needed.

<details>
<summary><strong>What's in v1.80.0?</strong></summary>

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

### Bug Fixes
- External tool comma-splitting in batch files (#3044)
- Options panel Cancel now reverts theme changes (#2914)
- "Always show panel tabs" no longer corrupts Options (#2910)
- SQL Server options fields properly enable (#2913)
- RDP auto-resize on monitor connect/disconnect (#2142)
- 15+ additional stability fixes

### Performance
- SQL hierarchy O(n^2) → O(n), async RDP init, yield return child lists
- Dual-build matrix: 6 builds per release, mandatory code signing

See [CHANGELOG.md](CHANGELOG.md) for the full list.

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

## License

[GPL-2.0](COPYING.TXT)

## Contributing

Submit code via pull request. See the [Wiki](https://github.com/mRemoteNG/mRemoteNG/wiki) for development environment setup.

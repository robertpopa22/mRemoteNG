<p align="center">
  <img width="450" src="https://github.com/mRemoteNG/mRemoteNG/blob/mRemoteNGProjectFiles/Header_dark.png">
</p>

# mRemoteNG v1.79.0 — Community Edition

> Active fork with 26 bug fixes, improved security, and a complete test suite.
> Based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`.

<p align="center">
  <a href="https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.79.0">
    <img alt="Release" src="https://img.shields.io/badge/release-v1.79.0-blue?style=for-the-badge">
  </a>
  <a href="https://github.com/robertpopa22/mRemoteNG/actions">
    <img alt="CI" src="https://img.shields.io/github/actions/workflow/status/robertpopa22/mRemoteNG/pr_validation.yml?style=for-the-badge&label=CI">
  </a>
</p>

---

## Download

| Platform | Download |
|----------|----------|
| Windows x64 | [mRemoteNG-v1.79.0-x64.zip](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.79.0/mRemoteNG-v1.79.0-x64.zip) |
| Windows x86 | [mRemoteNG-v1.79.0-x86.zip](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.79.0/mRemoteNG-v1.79.0-x86.zip) |
| Windows ARM64 | [mRemoteNG-v1.79.0-arm64.zip](https://github.com/robertpopa22/mRemoteNG/releases/download/v1.79.0/mRemoteNG-v1.79.0-arm64.zip) |

### Requirements
- [Microsoft .NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft Visual C++ Redistributable 2015-2026 ([x64](https://aka.ms/vs/18/release/vc_redist.x64.exe) | [x86](https://aka.ms/vs/18/release/vc_redist.x86.exe) | [ARM64](https://aka.ms/vs/18/release/vc_redist.arm64.exe))

---

## What's New in v1.79.0?

### Security
- LDAP filter sanitizer and XML importer guardrails
- ProcessStart argument hardening and shell escaping

### Bug Fixes (26 total)
- **RDP:** SmartSize focus loss, fullscreen toggle guard, refocus after exit, RCW disconnect safety
- **PuTTY:** Provider failure handling, CJK session name decoding
- **UI:** Close panel race, tab drag autoscroll, tab close race under resize, empty panel close, config panel splitter, inheritance label width, connections panel focus
- **Data:** 1Password parser fix, SQL schema compatibility, SqlClient SNI runtime, default credential provider
- **Core:** Startup path fallback, XML recovery for corrupt configs, main form close cancel, master password autolock, PROTOCOL external tool token, settings path logging, password protection verification

### Quality
- 2176 tests passing (81 pre-existing failures fixed, 28 new tests added)
- Zero flaky tests
- .NET 10, 3-architecture support (x64, x86, ARM64)

---

## All Fixes — Detailed Table

| PR | Issue | Description |
|----|-------|-------------|
| [#3105](https://github.com/mRemoteNG/mRemoteNG/pull/3105) | — | LDAP sanitizer and importer guardrails |
| [#3106](https://github.com/mRemoteNG/mRemoteNG/pull/3106) | [#3069](https://github.com/mRemoteNG/mRemoteNG/issues/3069) | Close panel race fix |
| [#3107](https://github.com/mRemoteNG/mRemoteNG/pull/3107) | [#3092](https://github.com/mRemoteNG/mRemoteNG/issues/3092) | 1Password parser and fallback fix |
| [#3108](https://github.com/mRemoteNG/mRemoteNG/pull/3108) | [#2972](https://github.com/mRemoteNG/mRemoteNG/issues/2972) | Default external provider fix |
| [#3109](https://github.com/mRemoteNG/mRemoteNG/pull/3109) | — | ProcessStart hardening and escaping |
| [#3110](https://github.com/mRemoteNG/mRemoteNG/pull/3110) | [#3005](https://github.com/mRemoteNG/mRemoteNG/issues/3005) | SqlClient SNI runtime references |
| [#3111](https://github.com/mRemoteNG/mRemoteNG/pull/3111) | [#1916](https://github.com/mRemoteNG/mRemoteNG/issues/1916) | SQL schema compatibility hardening |
| [#3112](https://github.com/mRemoteNG/mRemoteNG/pull/3112) | [#850](https://github.com/mRemoteNG/mRemoteNG/issues/850) | Config panel splitter width reset |
| [#3113](https://github.com/mRemoteNG/mRemoteNG/pull/3113) | [#1969](https://github.com/mRemoteNG/mRemoteNG/issues/1969) | Startup path fallback |
| [#3114](https://github.com/mRemoteNG/mRemoteNG/pull/3114) | [#822](https://github.com/mRemoteNG/mRemoteNG/issues/822) | PuTTY provider failure handling |
| [#3115](https://github.com/mRemoteNG/mRemoteNG/pull/3115) | [#2785](https://github.com/mRemoteNG/mRemoteNG/issues/2785) | PuTTY CJK session name decoding |
| [#3116](https://github.com/mRemoteNG/mRemoteNG/pull/3116) | [#2735](https://github.com/mRemoteNG/mRemoteNG/issues/2735) | RDP SmartSize focus loss fix |
| [#3117](https://github.com/mRemoteNG/mRemoteNG/pull/3117) | [#847](https://github.com/mRemoteNG/mRemoteNG/issues/847) | RDP fullscreen toggle guard |
| [#3118](https://github.com/mRemoteNG/mRemoteNG/pull/3118) | [#1650](https://github.com/mRemoteNG/mRemoteNG/issues/1650) | RDP refocus after fullscreen exit |
| [#3119](https://github.com/mRemoteNG/mRemoteNG/pull/3119) | [#2510](https://github.com/mRemoteNG/mRemoteNG/issues/2510) | RDP SmartSize RCW disconnect fix |
| [#3120](https://github.com/mRemoteNG/mRemoteNG/pull/3120) | [#2987](https://github.com/mRemoteNG/mRemoteNG/issues/2987) | Settings path logging |
| [#3121](https://github.com/mRemoteNG/mRemoteNG/pull/3121) | [#2673](https://github.com/mRemoteNG/mRemoteNG/issues/2673) | Require password before disabling protection |
| [#3122](https://github.com/mRemoteNG/mRemoteNG/pull/3122) | [#1649](https://github.com/mRemoteNG/mRemoteNG/issues/1649) | Master password autolock on minimize/idle |
| [#3123](https://github.com/mRemoteNG/mRemoteNG/pull/3123) | [#1634](https://github.com/mRemoteNG/mRemoteNG/issues/1634) | PROTOCOL external tool token |
| [#3124](https://github.com/mRemoteNG/mRemoteNG/pull/3124) | [#2270](https://github.com/mRemoteNG/mRemoteNG/issues/2270) | Main close cancel behavior |
| [#3125](https://github.com/mRemoteNG/mRemoteNG/pull/3125) | [#811](https://github.com/mRemoteNG/mRemoteNG/issues/811) | Startup XML recovery |
| [#3126](https://github.com/mRemoteNG/mRemoteNG/pull/3126) | [#2160](https://github.com/mRemoteNG/mRemoteNG/issues/2160) | Empty panel close after last tab |
| [#3127](https://github.com/mRemoteNG/mRemoteNG/pull/3127) | [#2161](https://github.com/mRemoteNG/mRemoteNG/issues/2161) | Tab drag autoscroll on overflow |
| [#3128](https://github.com/mRemoteNG/mRemoteNG/pull/3128) | [#2171](https://github.com/mRemoteNG/mRemoteNG/issues/2171) | Config connections panel focus |
| [#3129](https://github.com/mRemoteNG/mRemoteNG/pull/3129) | [#2166](https://github.com/mRemoteNG/mRemoteNG/issues/2166) | Tab close race under resize |
| [#3130](https://github.com/mRemoteNG/mRemoteNG/pull/3130) | [#2155](https://github.com/mRemoteNG/mRemoteNG/issues/2155) | Inheritance label width fix |

---

## How to Update

### Portable (ZIP)
1. Download the ZIP for your architecture
2. Extract over your existing mRemoteNG installation
3. Your `confCons.xml` and settings are preserved

### Auto-Update (Optional)
You can configure mRemoteNG to check this fork for updates:
1. Go to **Tools > Options > Updates**
2. Change the **Update Address** to: `https://raw.githubusercontent.com/robertpopa22/mRemoteNG/codex/release-1.79-bootstrap/docs/nightly-update-portable.txt`
3. mRemoteNG will check this fork for new versions

---

## Upstream Relationship

This fork is based on [mRemoteNG/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG) `v1.78.2-dev`.
All 26 fixes have individual PRs open on upstream ([#3105](https://github.com/mRemoteNG/mRemoteNG/pull/3105)–[#3130](https://github.com/mRemoteNG/mRemoteNG/pull/3130)), ready for upstream merge.

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

## Installation

### Supported Operating Systems

- Windows 11
- Windows 10
- Windows Server 2022
- Windows Server 2019
- Windows Server 2016

### Minimum Requirements

- [Microsoft .NET Desktop Runtime 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)
- Microsoft Visual C++ Redistributable 2015-2026
- Microsoft Terminal Service Client 6.0 or later (for RDP)

## Build from Source

```powershell
# Requires Visual Studio BuildTools (VS2026 or VS2022) with .NET SDK
powershell.exe -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

`build.ps1` auto-detects the newest VS installation. For manual builds, see `CLAUDE.md`.

## License

[GPL-2.0](COPYING.TXT)

## Contributing

Submit code via pull request. See the [Wiki](https://github.com/mRemoteNG/mRemoteNG/wiki) for development environment setup.

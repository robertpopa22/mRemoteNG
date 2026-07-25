# Fork Radar - 2026-07-25

Upstream `mRemoteNG/mRemoteNG` - forks scanned for changes worth importing into `robertpopa22/mRemoteNG`.

| Tier | Count |
|---|---|
| Tier A - ready to cherry-pick | 0 |
| Tier B - worth porting by hand | 2 |
| Tier C - watch list | 1 |
| Quarantine - security review required before anything else | 10 |
| Tier D - rejected | 32 |

## Tier B - worth porting by hand

### `3f94a2c239` Dark mode: follow the OS, honor the theming setting, dark title bars

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3f94a2c23980a384cbf15386ae7ffc506a92e6e5) by vindict6
- **size:** 10 files (+298/-11)
- **score 7** - port the idea, the patch will not apply
- **triage:** feature | value 4 | effort 3 | risk 3 | applies conflict | REIMPLEMENT
- **our issue:** #47
- **why:** Follow-OS dark mode + DWM dark title bars addresses open #47. Clean idea, but flips ThemingActive default and our ThemeManager/settings diverged; re-derive carefully.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - OS matching is valuable, but this untested patch conflicts with live-switch and high-contrast theming, assumes restart-only behavior, and requires a scoped reimplementation.
  - dissent - gemini: NEEDS_HUMAN - Valuable dark mode UX improvements matching modern Windows settings, but requires careful refactoring of settings and ThemeManager initialization to prevent regressions.

### `932e6f6116` Enhance connection handling and UI features

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/932e6f611674e6227db18d977f67a1b577af25a2) by Loïc THOBOIS
- **size:** 15 files (+732/-138)
- **score -2** - port the idea, the patch will not apply
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Mixed bag: new inheritance props (ExternalAddressProvider, RDP StartProgram, gateway token), notification detail, plus personal junk (.vscode, WorldOfFanXP.xml). Partly overlaps our upstream ports; cherry-pick only if users ask.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - A 732-line mixed, untested commit also bypasses notification filters, leaks writer subscriptions, duplicates shipped UI/retry features, adds untranslated labels, and uses noncanonical tooling.
  - dissent - gemini: REJECT - This is a mixed bag of personal settings, French locale scripts, and features already integrated or overlapping with our upstream ports. Not suitable for import.

## Quarantine - security review required before anything else

### `7241b81225` Add a workflow to build PuTTYNG from a pinned PuTTY tag

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7241b81225eefc24e07cc6830fdb818b638a785d) by vindict6
- **size:** 1 files (+217/-0)
- **score 7** - security review required (critical)
- **triage:** security | value 3 | effort 2 | risk 2 | applies likely | IMPORT
- **why:** Pinned, anchor-verified PuTTYNG build workflow (dispatch-only) improves supply-chain reproducibility of shipped PuTTYNG.exe; standalone file, no conflict with our CI.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:APPROVE / grok:NEEDS_HUMAN)
  - dissent - codex: REJECT - Updating PuTTY is valuable, but this workflow breaks the fork’s detection contract and also interpolates untrusted input into PowerShell.
  - dissent - grok: NEEDS_HUMAN - Useful pinned PuTTYNG build CI for SSH stability, but patch parity and binary drop need maintainer judgment.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `4e5bc44024` Update bundled PuTTYNG to 0.84

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4e5bc4402415db3b351bdb4845d3b70613796856) by vindict6
- **size:** 2 files (+12/-3)
- **score 5** - security review required (critical)
- **triage:** chore | value 3 | effort 2 | risk 3 | applies conflict | REIMPLEMENT
- **why:** PuTTYNG 0.84 bump worthwhile, but never import third-party binary. Rebuild via our Build_PuTTYNG.yml from tag; tag-check workflow tweak worth porting.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NO_ANSWER)
  - dissent - codex: REJECT - PuTTY 0.84 security fixes are valuable, but this commit is partly inapplicable and regresses binary trust; rebuild and sign 0.84 through this fork's pipeline.
  - dissent - gemini: NO_ANSWER - Do not import compiled binaries from third-party forks. Instead, we must build PuTTYNG 0.84 ourselves via our secure Build_PuTTYNG workflow.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `binary-artifact` (critical) in `mRemoteNG/PuTTYNG.exe` - committed binary cannot be reviewed (OpenSSF Scorecard)

### `58ebf6e3fa` Read and write machine-bound connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/58ebf6e3faf513390fbcdbfb1dd033651dd00475) by vindict6
- **size:** 7 files (+479/-5)
- **score 4** - security review required (high)
- **triage:** security | value 4 | effort 4 | risk 4 | applies conflict | WATCH
- **our issue:** #128
- **why:** DPAPI-wrapped master key fixes mR3m weak-default (our #128/incident #92 concern). Real value, but new file-format attribute, compat + serializer conflicts with our 1600-commit divergence. Reimplement deliberately if pursued.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The fork already solves KDF cost through 600,000-iteration caching, while this stale patch conflicts with certificate, TOTP, and serializer changes.
  - dissent - gemini: NEEDS_HUMAN - Addresses critical default password weakness, but breaking file portability and causing serializer conflicts requires deliberate human decision and custom reimplementation.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Factories/CryptoProviderFactoryFromXml.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/Factories/MasterKeyProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/MasterKeyCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/MasterKeyCryptographyProviderTests.cs` - credential and crypto paths need human review regardless of intent

### `1534a261c8` Heavy refactor & move to fork

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/1534a261c8d547df36f73588f9e37c924e314abd) by Chris Zarlengo
- **size:** 14 files (+1152/-0)
- **score 1** - security review required (critical)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Bitwarden external credential connector — absent in our fork, plausible user value, but 1150+ lines with process-exec/security surface and fork-specific refactor; watch upstream maturity.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The untested 1,152-line import is functionally broken, collides with PasswordSafe's enum value 5, and requires redesign against current credential-provider plumbing.
  - dissent - gemini: REJECT - We avoid large speculative rewrites and external dependency integrations (like Bitwarden) that increase attack surface and maintenance overhead.
- **security flags:**
  - `process-exec` (critical) in `ExternalConnectors/BW/BitwardenCommandRunner.cs` - added code spawns a process or evaluates a string as code
  - `security-code` (high) in `mRemoteNG/Connection/ExternalCredentialProviderSelector.cs` - credential and crypto paths need human review regardless of intent

### `bcc65a861a` Add passphrase key derivation for exported connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bcc65a861a88d82f44168135e83d974422c2c058) by vindict6
- **size:** 4 files (+376/-1)
- **score 1** - security review required (high)
- **triage:** security | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** Argon2id passphrase export keys is sound, but builds on vindict6-only ConnectionFileProtection scheme absent from our fork (verified via grep); would need full redesign atop our PBKDF2/MasterPasswordGate stack.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The security goal fits, but this commit is an unwired stacked fragment whose required master-key infrastructure is absent from the target fork.
  - dissent - gemini: REJECT - Relies on vindict6's custom KeyProtection / ConnectionFileProtection architecture absent from our fork, requiring a major redesign to integrate.
- **security flags:**
  - `security-code` (high) in `mRemoteNG/Security/Factories/MasterKeyProviderFactory.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyDerivation/Argon2idKeyGenerator.cs` - credential and crypto paths need human review regardless of intent

### `eeb7944d3d` Add SFTP connection to Linux remote server for file transfer functionality

- **fork:** [raohj1987/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/eeb7944d3d6d973e20390cfb1067c10c7adeefff) by raohj1987
- **size:** 8 files (+1963/-1)
- **score 1** - security review required (critical)
- **triage:** feature | value 3 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** SFTP browser could add value, but 2K-line drop with password-only auth, plaintext handling, duplicate of SSHTransferWindow scope. Reimplement properly only if users request it.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It duplicates existing SFTP support with 1,963 untested lines; fake session reuse, cross-thread UI access, and nullable warnings make quick verification impossible.
  - dissent - gemini: REJECT - Violates guidelines against introducing external dependencies, massive unverified UI rewrites, and features overlapping with existing SSH file transfer functionality.
- **security flags:**
  - `network-download` (critical) in `mRemoteNG/UI/Window/SftpFileManagerWindow.cs` - added code fetches remote content at build or run time

### `4cded1cc37` Add master password feature with startup unlock, hint, and settings migration

- **fork:** [yosale2011/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4cded1cc376680b686a5fd219c982e16920e7fd4) by Yosale2011
- **size:** 19 files (+1226/-18)
- **score -1** - security review required (high)
- **triage:** feature | value 3 | effort 4 | risk 5 | applies rewrite | WATCH
- **our issue:** #128
- **why:** App-level master password with settings re-encryption. Overlaps our MasterPasswordGate + WebAuthn/Entra ID hardening. Homegrown key-hierarchy migration needs deep security review; touches Runtime.EncryptionKey.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It overlaps the existing master-password gate, adds a forbidden dependency/build-path change, breaks a current EncryptionKey assignment, and leaves critical migration/startup behavior untested.
  - dissent - gemini: REJECT - The commit includes an unwanted external dependency (AxMSTSCLib) that risks breaking the custom MSBuild setup for COM references, violating explicit fork guidelines.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `security-code` (high) in `MASTER_PASSWORD_FEATURE.md` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/App/MasterPasswordService.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Config/Serializers/XmlConnectionsDecryptor.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/XmlKeyValidator.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/FrmPassword.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/UI/Forms/MasterPasswordManager.cs` - credential and crypto paths need human review regardless of intent
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `7c4c9d891f` Refactor RDP protocol initialization and enhance error handling; update launch configurations

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7c4c9d891f81a8d2fbae959ed4c5cc010cf94363) by Loïc THOBOIS
- **size:** 5 files (+163/-89)
- **score -1** - security review required (high)
- **triage:** refactor | value 2 | effort 3 | risk 4 | applies conflict | WATCH
- **why:** Mixed bag: useful init/connect failure logging, but interop assembly-preload hack dubious on .NET 10; our RdpProtocol heavily diverged. Cherry-pick logging only if needed.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - The commit conflicts with the fork’s async path, existing diagnostics, assembly layout, exception semantics, and canonical build tooling.
  - dissent - gemini: REJECT - The change conflicts with our .NET 10 SDK-style project, lacks InitializeAsync integration, and contains localized/VS Code-specific launch configurations.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `2c92360d63` Add master key primitives for machine-bound connection files

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/2c92360d63d7190e125cb6194ba1611856fe48a5) by vindict6
- **size:** 8 files (+426/-3)
- **score -2** - security review required (critical)
- **triage:** security | value 2 | effort 4 | risk 4 | applies conflict | WATCH
- **why:** DPAPI-bound master key is sound but unwired primitives; breaks file portability, CI change gates tests to Security only. Our KDF-cost issue already fixed (#120). Revisit if integrated.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:NEEDS_HUMAN)
  - dissent - codex: REJECT - The primitives are unused, their KDF-performance rationale is already solved by cached 600k PBKDF2, and the divergent base makes import nontrivial.
  - dissent - gemini: NEEDS_HUMAN - The DPAPI master key security primitives are highly valuable for credential protection, but the workflow changes must be discarded so all tests remain active.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/DpapiMasterKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/IMasterKeyProtector.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/KeyProtection/MasterKey.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNG/Security/SymmetricEncryption/AeadCryptographyProvider.cs` - credential and crypto paths need human review regardless of intent
  - `security-code` (high) in `mRemoteNGTests/Security/KeyedCryptographyTests.cs` - credential and crypto paths need human review regardless of intent

### `8ae75ec5d2` postregsql database support

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/8ae75ec5d2012a91457c3d210a2a7e97724feda6) by Sylvain LAFFONT
- **size:** 7 files (+121/-5)
- **score -2** - security review required (high)
- **triage:** feature | value 2 | effort 4 | risk 4 | applies rewrite | WATCH
- **why:** PostgreSQL backend is genuinely new, but code targets pre-rework SQL layer (old SqlClient, no v3.5 schema/upgrade path, SELECT * CommandBuilder our #145/#148 fixes replaced). Reimplement only if users ask.
- **pre-approval:** **MANUAL-REVIEW** (codex:REJECT / gemini:REJECT)
  - dissent - codex: REJECT - It also adds an obsolete preview dependency, no PostgreSQL schema lifecycle, and no tests; adapting it requires clean reimplementation, not a quick import.
  - dissent - gemini: REJECT - PostgreSQL support uses obsolete CommandBuilder SELECT * patterns we removed, lacks schema upgrades, and adds an unneeded Npgsql preview package.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

## Tier C - watch list

### `374eb8a34a` fixed WinSCP extended arguments

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/374eb8a34ab3e07a26554751cbfc0f67280ec28a) by radiosti
- **size:** 1 files (+1/-1)
- **score 4** - keep an eye on it
- **triage:** docs | value 1 | effort 1 | risk 1 | applies likely | IMPORT
- **why:** One-char docs typo: WinSCP flag is -rawsettings not -rawsetting. Trivial, correct, zero risk if cheat sheet file still exists.

## Tier D - rejected

### `66ad958c47` Fix Visual Studio targets path in passive RDP build

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/66ad958c477e0217554c90af1963098f67c88a65) by guvity
- **size:** 1 files (+2/-2)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes a workflow file (passive-rdp-monitor-build.yml) that only exists in that fork; our build.ps1/CI already auto-detect VS. Not applicable.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `92eae2e42e` Fix errors

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/92eae2e42e1f2805bced75e4d80829ec0f2f47e7) by Chris Zarlengo
- **size:** 3 files (+6/-6)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fixes compile breakage in that fork's own Bitwarden connector code plus version bump; our fork lacks their broken code, nothing to import.

### `9c48be3008` bump minor version and build number up

- **fork:** [Ahmed-ElHamidy/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/9c48be30086691de64e7a4d2a29c3cb241e45f87) by AHMED OMAR ELHAMIDY
- **size:** 3 files (+7/-7)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Fork-local version bump plus cosmetic colon in one log message; our fork has own versioning (1.82.0), zero benefit.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `a3241cac85` Fix ambiguous WinForms Message reference

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/a3241cac85258c371bdc3ce97a7a0d94d76af1f1) by guvity
- **size:** 1 files (+1/-1)
- **score 4** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fixes ambiguity inside RdpInputBlocker, a class only in that fork's passive-RDP feature; no such code or compile error here.

### `bcaa39f4db` windows-agent: add file logging so it runs silently in background

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bcaa39f4db1b25d19f4335019d0b709fc40d0865) by Kees
- **size:** 1 files (+9/-3)
- **score 4** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Touches windows-agent/poller.py, fork-private infrastructure tooling unrelated to mRemoteNG; directory absent in our fork.

### `f066add845` Enhance notification message handling and add new settings for RDP gateway access token and start program

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/f066add8452cb4221010dffa8a7aa0174277290d) by Loïc THOBOIS
- **size:** 4 files (+277/-223)
- **score 4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Cosmetic notification prefix with 12-hour 'hh' bug, mixed with whitespace churn and fork-specific RDP gateway token settings. Low value against our diverged ConnectionInitiator.

### `5b7e2ae0fb` Add translations in other languages

- **fork:** [raohj1987/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/5b7e2ae0fb3f5527b5ff2df538ca082be09c7054) by raohj1987
- **size:** 17 files (+116/-39)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Mostly resx whitespace reflow noise; only substantive string is SftpFileManager for fork-specific feature we lack. Guaranteed merge conflicts, no user benefit.

### `a0809b0d01` Only require PuTTYNG end anchors to follow their start anchor

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/a0809b0d01dbbb247e74c00c12da8e69b2c777f3) by vindict6
- **size:** 1 files (+7/-4)
- **score 1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies rewrite | REJECT
- **why:** Patches Build_PuTTYNG.yml which does not exist in our fork; our PuTTYNG lives in separate repo with own build.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_PuTTYNG.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `e188e12904` Config now read tools from the command line config position

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/e188e129048b62a027908f8a4bdda754def50522) by takemaker63
- **size:** 5 files (+38/-26)
- **score 1** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Depends on that fork's private --config/--cfg CLI feature; our ProgramRoot has no such arg. Also carries personal launchSettings/AssemblyInfo noise. Loose thematic overlap with #145 portable-path work only.

### `006f651ddc` Fix misleading version label

- **fork:** [wolverine2k/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/006f651ddc4f02d36163b53f55197cf696c8632f) by Manuel Thalmann
- **size:** 1 files (+2/-2)
- **score 0** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Our UpdateWindow.cs line 76 already shows Language.Version for installed-version label; identical fix present after #136 update-flow rework.

### `6b5c4cfe6e` Fix DockState namespace reference and update build number

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/6b5c4cfe6e45f37d9c1fa46a46b83170fe4d8fc2) by Nick Beentjes
- **size:** 2 files (+5/-5)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies conflict | REJECT
- **why:** Cosmetic namespace qualification plus nightly build-number bump on 1.78.2 AssemblyInfo; our .NET 10 fork versions via csproj, no benefit.

### `83b66b5d8e` Build passive RDP with VS 2026 runner

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/83b66b5d8e0daa1d1324eb53d4da65b5df871f81) by guvity
- **size:** 1 files (+22/-6)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 1 | applies rewrite | REJECT
- **why:** Fork-private workflow file we don't have; our CI already runs windows-2025-vs2026 with MSBuild 18 and our build.ps1 handles VS detection. Nothing to import.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `d78f3317ec` Allow item name & handle revoked tokens + logging

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d78f3317ec089aae42c2bf6c320d7ceba7ae5c0a) by Chris Zarlengo
- **size:** 7 files (+178/-39)
- **score 0** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Patches Zarlengo-only Bitwarden connector (ExternalConnectors/BW absent in our fork); also removes UUID validation and adds noisy notification spam.

### `ff3ed60e88` Disable auto-update + fix Claude API key setup

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/ff3ed60e8807fb80b95c7227ee1cf59862127484) by Kees
- **size:** 2 files (+34/-3)
- **score 0** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 3 | applies rewrite | REJECT
- **why:** Fork-private hacks: hard-disables update check (ours is GitHub-only by design) and stores Claude API key plaintext in AppData for a ClaudeChatWindow we don't have.

### `3e43d6855d` Fix passive RDP portable build workflow

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/3e43d6855db0d441befaa429ff56c37a687e0a74) by guvity
- **size:** 1 files (+9/-11)
- **score -1** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 3 | applies rewrite | REJECT
- **why:** Fixes that fork's own passive-rdp workflow; file doesn't exist here. Also downgrades action versions and our CI model differs entirely.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `3dc00ad1ae` Add options

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/3dc00ad1aedd73bd1002e70b02dd5c947a6d29ca) by Chris Zarlengo
- **size:** 14 files (+877/-157)
- **score -2** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 4 | applies conflict | REJECT
- **why:** Fork-personal Bitwarden connector rework; removes SSO/password-file paths (regression for those users). Large Designer churn, no tracked issue, credential-flow risk.

### `7dd9011eec` Enhance Prot_Event_Closed with exception handling

- **fork:** [Ahmed-ElHamidy/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/7dd9011eec0bb331b14d2f4d5ba4b1535247d665) by Ahmed Omar ElHamidy
- **size:** 1 files (+13/-6)
- **score -2** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **our issue:** #142
- **why:** Blanket catch that swallows close errors. Our fork already guards this path (disposed checks, deferred QueueCloseTab NRE-guarded, 6c788ae45). Masking exceptions is a regression.

### `9f03e57d3b` Use MSBuild for passive RDP portable publish

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/9f03e57d3b91f45a2976d3f43a1030a828d3ee5e) by guvity
- **size:** 1 files (+5/-2)
- **score -2** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 1 | risk 2 | applies conflict | REJECT
- **why:** Switches their custom passive-rdp workflow to MSBuild to dodge MSB4803 COM-ref failure; our build.ps1 and CI already use full MSBuild. Workflow file not in our repo.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `567f108484` Run the unit tests in CI

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/567f108484aaf48c62cc291f874521f4a79a007e) by vindict6
- **size:** 1 files (+31/-0)
- **score -3** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 2 | risk 2 | applies conflict | REJECT
- **why:** Our CI already runs full test suite (run-tests.ps1, nightly trx collection); vstest.console approach inferior to our headless runner. Nothing to gain.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `c5606cccf5` Hide username/password

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/c5606cccf5d8b618df63b2b4e7e32bdf84584462) by Chris Zarlengo
- **size:** 4 files (+27/-31)
- **score -3** - already covered or rejected at triage
- **triage:** bugfix | value 1 | effort 4 | risk 3 | applies rewrite | REJECT
- **why:** Incremental tweak to Zarlengo's own Bitwarden connector (ExternalConnectors/BW, VaultOpenbao fields, NotificationBridge) — feature absent from our fork; nothing to patch.

### `eb4787f9e4` Add additional unlock methods

- **fork:** [Zarlengo/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/eb4787f9e4bf591713f4341c6fa21dece2b024d3) by Chris Zarlengo
- **size:** 7 files (+319/-190)
- **score -3** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 4 | applies rewrite | REJECT
- **why:** Extends Bitwarden connector (ExternalConnectors/BW) that exists only in Zarlengo fork; our fork has no BW connector, so nothing to patch. Full-feature import out of scope.

### `0707b71af5` Add passive RDP monitor mode

- **fork:** [guvity/mRemoteNG-passive-rdp](https://github.com/mRemoteNG/mRemoteNG/commit/0707b71af5eddbb73d7abf6fbafd26ea6daafaa1) by guvity
- **size:** 5 files (+896/-8)
- **score -4** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Niche passive-monitor fork: input blocker, focus-suppression timers, custom CI workflow. Conflicts with our #118/#143 focus fixes and ViewOnly semantics; unrequested feature.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/passive-rdp-monitor-build.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `b032927087` Modernize remote connection UX while preserving session state

- **fork:** [YuLiangLin/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/b03292708796b64ab08f2b230ff0521aeefe6208) by Nathan Lin
- **size:** 60 files (+777/-152)
- **score -5** - already covered or rejected at triage
- **triage:** feature | value 2 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** 60-file opinionated UX rework: drops VncSharpCore, writes PuTTY registry theme, ships binary icons; conflicts with our RDP/focus fixes, unreviewable risk.
- **security flags:**
  - `dependency-manifest` (high) in `Directory.Packages.props` - a new or repointed package can pull arbitrary code at restore time
  - `license` (medium) in `mRemoteNG/Icons/FLUENT-LICENSE.txt` - licence edits change redistribution terms
  - `binary-artifact` (critical) in `mRemoteNG/References/VncSharpCore.dll` - committed binary cannot be reviewed (OpenSSF Scorecard)
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `73267c1fcf` Tag releases with the version that was built; honor release_flag

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/73267c1fcfc95daccb3abd954b2a122c6007bd09) by vindict6
- **size:** 1 files (+18/-24)
- **score -6** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 3 | applies rewrite | REJECT
- **why:** Fixes dated-NB tagging our fork removed; 2-release model (ef6420d9a) tags from csproj Version, no T4 build numbers. Obsolete.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)

### `15635dff6d` added copy password option

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/15635dff6d0d6ad9d2a435430c50a41ba319e6c3) by takemaker63
- **size:** 3 files (+24/-5)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 2 | risk 4 | applies conflict | REJECT
- **our issue:** #128
- **why:** Plaintext SetText copy with no re-auth gate; our 0e7b9c75e already ships gated copy/reveal with SetSecret clipboard hygiene. Importing would regress security.
- **security flags:**
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `bfcf3c26d4` Add NickHQ session controller — register terminal tabs, poll + execute remote commands

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bfcf3c26d484e82baee96cfef319acc1d1c1b572) by Kees
- **size:** 3 files (+572/-0)
- **score -7** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **why:** Personal remote-control backdoor: polls private server, executes arbitrary commands, screenshots sessions. Hardcoded owner URL. Security liability, zero user value.
- **security flags:**
  - `process-exec` (critical) in `mRemoteNG/Connection/NickHq/NickHqClient.cs` - added code spawns a process or evaluates a string as code

### `457fcad4a0` Fix NB build workflow

- **fork:** [vindict6/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/457fcad4a08d67d7e3975d33577ec158af4515f9) by vindict6
- **size:** 2 files (+75/-38)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** Fork-of-our-fork repairing its own NB workflow. Our CI already restructured (windows-2025-vs2026, 2-release model, all GREEN). Fix targets divergent workflow state.
- **security flags:**
  - `ci-workflow` (critical) in `.github/workflows/Build_mR-NB.yml` - CI workflow changes are the primary supply-chain vector (pull_request_target abuse, workflow injection)
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

### `4eb1833a62` Enhance build instructions and settings; refactor connection handling

- **fork:** [lthobois/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/4eb1833a62da203783124f967105d0516b4d416c) by Loïc THOBOIS
- **size:** 12 files (+329/-89)
- **score -8** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 3 | risk 4 | applies conflict | REJECT
- **why:** VS Code tasks/launch config for their dev setup; we build via build.ps1. Bundles unseen connection-handling refactor (truncated diff) — blind import risky, no mapped issue.
- **security flags:**
  - `build-script` (high) in `Tools/invoke_msbuild.ps1` - scripts execute on a maintainer machine

### `888cc44fda` Add AI layer: Claude chat, session logging, SCP transfers, host-call protocol, Windows agent

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/888cc44fdae9f83a989724966d1816efd17138ed) by Kees
- **size:** 15 files (+1914/-1)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Personal AI-layer experiment (Claude chat panel, SendKeys command injection, API keys in settings). Out of scope, large attack surface, no user demand in our tracker.

### `bef31a3ca2` NickHQ multi-server config: settings UI, auto-connect on startup

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/bef31a3ca23b4784bfc97be21f022febf983a4f3) by Kees
- **size:** 4 files (+710/-88)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Personal fork's private NickHQ backend: registers sessions, polls remote server, executes exec/paste/screenshot commands. Effectively a remote-control agent with hardcoded Tailscale URL. Unacceptable.

### `d49f440d52` Add orchestrator engine + rich notifications panel; drop Telegram

- **fork:** [nickbeentjes/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/d49f440d5279c765b6c0412628a22170f98da961) by Kees
- **size:** 16 files (+1865/-3)
- **score -8** - already covered or rejected at triage
- **triage:** feature | value 1 | effort 5 | risk 5 | applies rewrite | REJECT
- **why:** Fork-personal NickHQ orchestrator/notifications infrastructure; depends on NickHqClient we don't have; no user value for our fork.

### `18c26d1c33` now uses mariadb and shared user-config

- **fork:** [hthvdmeer/mRemoteNG](https://github.com/mRemoteNG/mRemoteNG/commit/18c26d1c33690db02d80f1d7919d5b6acdfae902) by takemaker63
- **size:** 24 files (+883/-77)
- **score -11** - already covered or rejected at triage
- **triage:** chore | value 1 | effort 4 | risk 5 | applies rewrite | REJECT
- **our issue:** #145
- **why:** Personal-environment dump: committed root password, temp logs, private CLAUDE.md. MariaDB/SQL fixes already covered by our #145-#148 series.
- **security flags:**
  - `opaque-file` (high) in `CTempmremote_stderr.txt` - added file has no reviewable text diff
  - `build-script` (high) in `Tools/ConvertXmlToSql.ps1` - scripts execute on a maintainer machine
  - `dependency-manifest` (high) in `mRemoteNG/mRemoteNG.csproj` - a new or repointed package can pull arbitrary code at restore time

---

Generated by `.project-roadmap/fork-intel/fork_intel.py report`. Nothing here has been imported: every entry needs a human decision.

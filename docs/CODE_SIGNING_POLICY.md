# Code Signing Policy

## Overview

mRemoteNG release binaries are to be signed with [SignPath Foundation](https://signpath.org/) code signing certificates, so users can verify the authenticity and integrity of a release. The workflows are wired for it; whether a given build is actually signed depends on the status table below.

## Current Status

| Channel | Signing Status |
|---------|---------------|
| **Stable releases** (`vX.Y.Z` tags, currently 1.83.x) | :x: **Unsigned today.** Wired for the SignPath `release-signing` policy; activates when the secrets exist |
| **Nightly builds** (main) | :x: **Unsigned today.** Wired for the SignPath `test-signing` policy, which uses a test certificate Windows does not trust: no SmartScreen or Defender benefit, it only proves the pipeline works before a tag depends on it |
| **Self-built** | :x: Unsigned (expected — user builds from source) |

> **Honest status (2026-09-22):** no binary this fork has ever published carries an Authenticode
> signature — not the ZIPs, not the MSI, not v1.82.0, not v1.83.0, not the nightlies. That is
> the missing piece behind #192, where Windows Defender quarantined `ExternalConnectors.dll`:
> an unsigned library whose job is to read credential vaults and launch other programs' CLIs is
> exactly what a heuristic engine distrusts.
>
> Both workflows are ready. They key off `SIGNPATH_CONFIGURED`, a job-level flag that is `true`
> only when **both** `SIGNPATH_API_TOKEN` and `SIGNPATH_ORGANIZATION_ID` exist as repository
> secrets. Until then every signing step is skipped and builds ship unsigned, as before. Once
> they exist, signing is mandatory: a signing request that fails fails the build, and the
> release job refuses to publish an unsigned ZIP or MSI.

### What has to happen outside this repository

1. Apply for [SignPath Foundation](https://signpath.org/foundation) open-source signing for
   `robertpopa22/mRemoteNG` (human review, typically days).
2. In the SignPath portal, create project `mRemoteNG` with:
   - artifact configuration **`zip`** — what SignPath receives is the GitHub Actions artifact,
     i.e. a ZIP wrapper around our release ZIP. Describe it as a `zip-file` containing one
     `zip-file`, and inside that sign `mRemoteNG.exe` and every `*.dll`, `Assemblies/` included;
   - artifact configuration **`msi`** — the same wrapper around one `msi-file`. The MSI is built
     from unsigned binaries before the ZIP is signed, so this configuration **must deep-sign**:
     the `msi-file` element needs the embedded `pe-file`s listed so the installed `mRemoteNG.exe`
     and DLLs are signed too, not just the installer container;
   - signing policies **`test-signing`** (auto-approved, test certificate; used by nightlies)
     and **`release-signing`** (manual approval by the Approver; used by `vX.Y.Z` tags).
3. Add `SIGNPATH_API_TOKEN` and `SIGNPATH_ORGANIZATION_ID` under Settings → Secrets → Actions.
4. Push to `main` and check that the nightly's "Sign nightly ZIP" step ran and that
   `Get-AuthenticodeSignature` on the downloaded `mRemoteNG.exe` reports `Valid`.

Nothing in this list can be done from the repository; it needs the maintainer's SignPath and
GitHub accounts.

## Publisher

- **Certificate Holder:** SignPath Foundation
- **Purpose:** Authenticode signing of Windows executables and DLLs
- **SmartScreen:** Yes, for binaries signed under the `release-signing` policy. Nightlies use SignPath's
  test certificate, which Windows does not trust, so they gain no SmartScreen or Defender reputation

## Signing Process

1. **Automated:** All signing happens in CI (GitHub Actions) — no manual signing
2. **Mandatory once configured:** with the secrets in place a failed signing request fails the
   build and the release job refuses to publish an unsigned asset. Without them, builds ship
   unsigned; there is no half-way state where some assets are signed and some are not
3. **Verified:** SignPath verifies that binaries were built from this GitHub repository
4. **Secure:** Private signing keys are stored on SignPath's HSM (Hardware Security Module)
5. **Least privilege:** the SignPath action fetches the artifact with the job's `GITHUB_TOKEN`; the
   jobs that call it hold `contents: read` only, and publishing runs in a separate job with the
   write token

## Team Roles

| Role | Responsibility | Members |
|------|---------------|---------|
| **Author** | Write code, create PRs | All contributors |
| **Reviewer** | Review and approve PRs before merge | @robertpopa22 |
| **Approver** | Approve release signing requests | @robertpopa22 |

## What Gets Signed

- `mRemoteNG.exe` — main application executable
- every `*.dll` in the archive, `Assemblies/` included — `mRemoteNG.dll`, `ExternalConnectors.dll`
  (credential provider plugins, the file Defender quarantined in #192), `ObjectListView.dll`
  and the third-party libraries the app loads from `Assemblies/`
- the MSI installer

## Requirements for Contributors

- All code contributions must go through pull request review
- Multi-factor authentication (MFA) is required for team members with merge access
- No binaries or pre-compiled code may be committed to the repository

## Verification

Users can verify signed binaries by:
1. Right-click the `.exe` > Properties > Digital Signatures tab
2. The signer should show **"SignPath Foundation"**
3. The certificate chain should be valid and trusted

## CI Integration

Stable releases, `.github/workflows/Build_mR-NB.yml` (the signing and replacement steps are gated on
`SIGNPATH_CONFIGURED`; the unsigned-artifact uploads `(10a)` and `(09b)` always run, because the release
job consumes them either way):
- `(10a)` uploads the unsigned ZIP as a build artifact; `(09b)` does the same for the MSI
- `(10b)` / `(10d)` submit the ZIP and the MSI to SignPath under `release-signing` and wait
  (up to an hour, because release signing needs a human approval in the portal)
- `(10c)` / `(10e)` upload the signed copies as `signed-*` artifacts
- release job `(04c)` / `(04d)` replace each unsigned asset with its signed copy and **fail the
  release** if any ZIP or the MSI has no signed counterpart

Nightlies, `.github/workflows/nightly.yml`: the same two submissions under `test-signing`, then
the signed files replace the unsigned ones before the `nightly` release is recreated. A nightly
whose signing fails is not published.

Before 2026-09-22 the release job downloaded the `build-*` (unsigned) artifacts and never read
`signed-*`, so even with secrets in place it would have shipped unsigned ZIPs; the MSI was never
submitted at all. Both are fixed.

## References

- [SignPath Foundation](https://signpath.org/)
- [SignPath Foundation Terms](https://signpath.org/terms.html)
- [SignPath GitHub Actions](https://github.com/SignPath/github-action-submit-signing-request)

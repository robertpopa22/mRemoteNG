# Code Signing Policy

## Overview

mRemoteNG release binaries should carry an Authenticode signature, so users can verify the authenticity and integrity of a release. **Today they do not.** The workflows are wired for [SignPath](https://signpath.org/), but SignPath Foundation declined this fork on 2026-03-05: after a second internal review they found that `robertpopa22/mRemoteNG` does not yet provide sufficient external reputation signals, so no signing route is active yet. The status table below is the source of truth.

## Current Status

| Channel | Signing Status |
|---------|---------------|
| **Stable releases** (`vX.Y.Z` tags, currently 1.83.x) | :x: **Unsigned.** The workflow is wired for a SignPath `release-signing` policy and stays inert without the secrets |
| **Nightly builds** (main) | :x: **Unsigned.** Wired for a SignPath `test-signing` policy (a test certificate Windows does not trust, so it would only prove the pipeline, not earn reputation); inert without the secrets |
| **Self-built** | :x: Unsigned (expected — user builds from source) |

> **Honest status (2026-09-22):** no binary this fork has ever published carries an Authenticode
> signature — not the ZIPs, not the MSI, not v1.82.0, not v1.83.0, not the nightlies. That is
> the missing piece behind #192, where Windows Defender quarantined `ExternalConnectors.dll`:
> an unsigned library whose job is to read credential vaults and launch other programs' CLIs is
> exactly what a heuristic engine distrusts.
>
> The workflows key off `SIGNPATH_CONFIGURED`, a job-level flag that is `true` only when
> **both** `SIGNPATH_API_TOKEN` and `SIGNPATH_ORGANIZATION_ID` exist as repository secrets. They
> do not exist, because the SignPath Foundation application was declined, so every signing step
> is skipped and builds ship unsigned. If a SignPath route opens later, signing becomes mandatory
> the moment the secrets are added: a failed signing request fails the build, and the release
> job refuses to publish an unsigned ZIP or MSI. A different provider would need its own steps.

### Why it is still unsigned, and what would change that

The fork applied to the SignPath Foundation OSS program on 2026-03-03. SignPath asked for
references, the maintainer answered with the fork-to-upstream relationship and project history
([`SIGNPATH_REPLY_2026-03-05.md`](SIGNPATH_REPLY_2026-03-05.md)), and on 2026-03-05 SignPath
replied that its assessment was unchanged: not enough external reputation signals for the fork
itself. The signing route is being re-evaluated; the candidates are a later re-application to
SignPath once the fork has more independent references, a paid cloud-HSM signing service that
runs unattended in CI, or an open-source certificate that has to be driven through the vendor's
cloud signing tool. None of these is in place.

If the SignPath route does open, the steps are:

1. Get the SignPath Foundation approval for `robertpopa22/mRemoteNG`.
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

- **Certificate Holder:** none yet. Under the SignPath route it would be SignPath Foundation; under
  another provider it would be the maintainer or the maintainer's organisation
- **Purpose:** Authenticode signing of Windows executables and DLLs
- **SmartScreen:** only for binaries signed with a publicly trusted certificate. A test-signing
  certificate (as the nightly steps are wired for) is not trusted by Windows and earns no
  SmartScreen or Defender reputation

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

Once a release is signed, users can verify it by:
1. Right-click the `.exe` > Properties > Digital Signatures tab
2. The signer named in the release notes should appear there (today there is no tab at all,
   because nothing is signed)
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

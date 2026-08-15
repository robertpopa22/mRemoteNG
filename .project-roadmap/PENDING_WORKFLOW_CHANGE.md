# Pending: wire the shipped-assembly check into CI (plan stage 4.2)

**Status:** script written, tested, committed. Workflow wiring **not applied** — it needs a human.

## Why this file exists instead of a commit

`scripts/security-tripwire.sh` treats `.github/workflows/` as a security-relevant path, so any
workflow edit stops the automated pipeline and requires `MRNG_SECURITY_REVIEWED=1`. That override is
documented as human-only, and the reason is written into the tripwire itself: a guard that its only
user bypasses by habit protects nothing. An agent approving its own workflow change is exactly the
bypass the rule exists to prevent — so the change stops here rather than being self-approved.

Workflows carry the signing and analysis secrets and decide what ships. That is why they are on the
list.

## What the change does

`scripts/verify-shipped-assemblies.ps1` asserts that every runtime assembly declared in
`mRemoteNG.deps.json` is present in the package, either beside the executable or in `Assemblies\`,
which is where the custom `AssemblyResolve` handler looks. Verified locally:

- against the real x64 build output: `Declared: 71`, `Present: 71`, passed;
- with one dependency removed: exit 1, `absent from the shipped package: AWSSDK.EC2.dll`.

`ShippedAssemblyLayoutTests` already runs the same comparison in the test suite, but it can only see
the build output on the machine that ran it. The packaging step in between is where files actually
go missing (#150), so the authoritative check belongs on the artifact that ships.

## The change to apply

Two files, same step in each: immediately **after** the existing post-zip sanity check that prints
`Sanity check passed: no Settings/confCons/backup entries in ZIP`.

### `.github/workflows/Build_mR-NB.yml` — step `(09) Create unsigned ZIP`, after line ~248

```yaml
          # Every assembly the build declares it needs must be in the package. The app-local
          # Assemblies\ layout means a missing dependency resolves fine in development and throws
          # FileNotFoundException on a user's machine (#150).
          pwsh -NoProfile -ExecutionPolicy Bypass `
            -File "$Env:GITHUB_WORKSPACE\scripts\verify-shipped-assemblies.ps1" `
            -Path $zipPath
          if ($LASTEXITCODE -ne 0) { exit 1 }
```

### `.github/workflows/nightly.yml` — same step, after line ~217

Identical block, with `$zipName` in place of `$zipPath` (that workflow's variable name).

## Applying it

```bash
MRNG_SECURITY_REVIEWED=1 git commit -m "ci: verify shipped assembly completeness on the release ZIP"
```

Record in the commit body what was examined: the change adds a read-only verification step and
introduces no new secret access, no new network egress, and no change to what is built or published
— it can only fail a build that was already going to ship a broken package.

## After applying

Workflow YAML cannot be validated locally. Watch the first nightly run after the merge, per the
project rule for workflow changes.

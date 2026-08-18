# Portable deployment

Portable deployments must treat `Settings` as user-owned state. Build output must
never contain a developer's connection file, and a deployment must never replace,
decrypt, or reserialize an existing target profile.

Use the repository-generic deployer after a successful portable build:

```powershell
pwsh ./build.ps1 -Portable
pwsh ./scripts/Deploy-Portable.ps1 `
    -SourceDirectory ./mRemoteNG/bin/x64/Portable `
    -TargetDirectory <install-dir>/mRemoteNG-latest `
    -LegacyProfileDirectory <install-dir>/mRemoteNG-old
```

`LegacyProfileDirectory` is only used when the target has no
`Settings/confCons.xml`. The deployer reads the old `CustomConsPath`, copies that
exact file without opening its encrypted payload, migrates the portable settings
and backups, and records initialization state. Once a target profile exists, it is
authoritative and is only hash-checked before and after program deployment.

For automatic workstation deployment, create the gitignored
`post-build-local.ps1`. `build.ps1` calls this hook only outside CI and passes
`Arch`, `Configuration`, `BuildOutput`, and the `Portable` switch. Keep all machine-
specific paths in that ignored hook, not in tracked files.

Local builds also derive their assembly metadata from the `<Version>` in
`mRemoteNG.csproj`. CI continues to generate `AssemblyInfo.cs`; the local path does
not rewrite that tracked file, so a current local build no longer displays stale
release metadata.

The deployer stages and validates program files, requires the target application
to be stopped, retains program rollback copies, rejects overlapping/root/reparse-
point paths, and excludes `Settings`, logs, deploy state, and rollback data from
program replacement. It never logs serialized profile contents or credential
values.

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
to be stopped, retains program rollback copies, rejects overlapping/root paths,
and excludes `Settings`, logs, deploy state, and rollback data from program
replacement. It never logs serialized profile contents or credential values.

Junctions, mount points, symbolic links and unknown reparse points are refused
anywhere in the source, target, profile and rollback trees. Cloud-file
placeholders (`IO_REPARSE_TAG_CLOUD_*`, as OneDrive creates) are ordinary content
and are accepted, so a target inside a synced folder deploys and prunes normally.

Rollback copies go to `<target>/_rollback` unless `-RollbackDirectory` names
another folder. For a target inside a synced folder, point it outside the synced
tree so backups are not uploaded; it must be on the same volume as the target.
Every move into and out of it is a rename, never a copy, so a locked file makes a
move fail whole instead of leaving half a folder on each side.

If the deployment fails after anything in the target has moved, the previous
program is put back and verified by hash against the state taken before the
deployment. If that verification fails the deployer stops with an error naming
the target, the backup folder that still holds the previous files, and the
discard folder holding anything taken out of the target; nothing is deleted.
Pruning old rollback copies happens only after a verified deployment and never
undoes it: a copy that cannot be removed is reported as a warning and retried on
the next run.

## Star the project

If this project helps you, please [star the repository](https://github.com/robertpopa22/mRemoteNG).

Some of the community tools we use set a public-star minimum before they will work with a project. [Qodo's free plan for open source](https://docs.qodo.ai/open-source-program) requires at least 200 stars on this repository, or on another public repository in the same GitHub organization. Other tools we use ask for more than that. A star is what keeps those tools available here.

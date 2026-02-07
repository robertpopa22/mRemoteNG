# Upstream PR Packages

Date: 2026-02-07  
Execution branch: `codex/release-1.79-bootstrap`

Purpose:
- split fork changes into small, reviewable PRs for upstream maintainers.

## PR-1 Security Follow-up (P0)

Scope:
- LDAP sanitizer centralization and call-site hardening.
- importer missing-file guardrails.
- related tests.

Primary commits:
- `3c419ded` (`p0-ldap-import-hardening`)
- `8680c53f` (test compile follow-up)

Core files:
- `mRemoteNG/Security/LdapPathSanitizer.cs`
- `mRemoteNG/Config/Serializers/MiscSerializers/ActiveDirectoryDeserializer.cs`
- `mRemoteNG/Tools/ADhelper.cs`
- `mRemoteNG/Config/Import/MRemoteNGCsvImporter.cs`
- `mRemoteNG/Config/Import/MRemoteNGXmlImporter.cs`
- tests under `mRemoteNGTests/Security` and `mRemoteNGTests/Config`

Execution status:
- branch: `codex/pr1-security-followup`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3105
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3105#issuecomment-3865040700

## PR-2 Stability Fix: Close-Panel Race (`#3069`) (P5)

Primary commit:
- `c12abbe1` (includes this fix plus docs; split commit for PR if needed)

Core file:
- `mRemoteNG/UI/Window/ConnectionWindow.cs`

Notes:
- if upstream wants minimal diff, cherry-pick only relevant hunk from `c12abbe1`.

Execution status:
- branch: `codex/pr2-closepanel-stability`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3106
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3106#issuecomment-3865040687

## PR-3 1Password Parsing and Field Fallback (`#3092`) (P5)

Primary commit:
- `821eaad6`

Core files:
- `ExternalConnectors/OP/OnePasswordCli.cs`
- `mRemoteNGTests/ExternalConnectors/OnePasswordCliTests.cs`

Execution status:
- branch: `codex/pr3-onepassword-3092`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3107
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3107#issuecomment-3865040688

## PR-4 Default External Credential Provider Handling (`#2972`) (P5)

Primary commit:
- `35831bb5` (contains additional triage/docs; split commit for PR if needed)

Core files:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
- `mRemoteNG/Connection/Protocol/PuttyBase.cs`

Notes:
- for clean upstream PR, isolate only protocol changes from this mixed commit.

Execution status:
- branch: `codex/pr4-default-provider-2972`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3108
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3108#issuecomment-3865040693

## PR-5 Command-Line Hardening + External Tool Escaping (`#2989`, `#3044`)

Scope:
- Process.Start invocation hardening.
- AnyDesk validation tests.
- external-tools argument escaping fixes for comma/semicolon in credentials.

Execution status:
- branch: `codex/pr5-commandline-security`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3109
- CI evidence comment: https://github.com/mRemoteNG/mRemoteNG/pull/3109#issuecomment-3865049494

## PR-6 SQL Server Connector Runtime Packaging Hardening (`#3005`)

Scope:
- explicit SqlClient native SNI runtime package references for platform-targeted builds.

Primary commit:
- `8623d978`

Core file:
- `mRemoteNG/mRemoteNG.csproj`

Execution status:
- branch: `codex/pr6-sqlclient-sni-runtime`
- upstream PR (ready for review): https://github.com/mRemoteNG/mRemoteNG/pull/3110
- issue link comment: https://github.com/mRemoteNG/mRemoteNG/issues/3005#issuecomment-3865102490
- CI evidence (fork): https://github.com/robertpopa22/mRemoteNG/actions/runs/21785156992

## Excluded From Upstream PRs

- local triage automation/docs:
  - `NEXTUP/*`
  - local execution logs and report artifacts
- comment-level issue triage evidence is already posted directly on upstream issues.

## Recommended Merge Order

1. PR-1 (security)
2. PR-5 (command-line/process hardening + external-tools escaping)
3. PR-2 (close-panel stability)
4. PR-3 (1Password regression)
5. PR-4 (default external provider handling)
6. PR-6 (SQL Server SNI runtime packaging)

## Operational Guidance

- Create one branch per PR package (`codex/pr1-security-followup`, etc.).
- Use `git cherry-pick -n <commit>` then stage only package files.
- Keep each PR tied to one issue cluster to reduce review cycle time.


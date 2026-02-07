# Upstream PR Packages (Proposed)

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

## PR-2 Stability Fix: Close-Panel Race (`#3069`) (P5)

Primary commit:
- `c12abbe1` (includes this fix plus docs; split commit for PR if needed)

Core file:
- `mRemoteNG/UI/Window/ConnectionWindow.cs`

Notes:
- if upstream wants minimal diff, cherry-pick only relevant hunk from `c12abbe1`.

## PR-3 1Password Parsing and Field Fallback (`#3092`) (P5)

Primary commit:
- `821eaad6`

Core files:
- `ExternalConnectors/OP/OnePasswordCli.cs`
- `mRemoteNGTests/ExternalConnectors/OnePasswordCliTests.cs`

## PR-4 Default External Credential Provider Handling (`#2972`) (P5)

Primary commit:
- `35831bb5` (contains additional triage/docs; split commit for PR if needed)

Core files:
- `mRemoteNG/Connection/Protocol/RDP/RdpProtocol.cs`
- `mRemoteNG/Connection/Protocol/PuttyBase.cs`

Notes:
- for clean upstream PR, isolate only protocol changes from this mixed commit.

## Excluded From Upstream PRs

- local triage automation/docs:
  - `NEXTUP/*`
  - local execution logs and report artifacts
- comment-level issue triage evidence is already posted directly on upstream issues.

## Recommended Merge Order

1. PR-1 (security)
2. PR-2 (close-panel stability)
3. PR-3 (1Password regression)
4. PR-4 (default external provider handling)

## Operational Guidance

- Create one branch per PR package (`codex/pr1-security-followup`, etc.).
- Use `git cherry-pick -n <commit>` then stage only package files.
- Keep each PR tied to one issue cluster to reduce review cycle time.


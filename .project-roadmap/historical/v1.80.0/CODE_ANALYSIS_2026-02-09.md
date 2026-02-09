# Code Analysis & Error Backlog — v1.80.0 (2026-02-09)

Consolidated from: ERROR_BACKLOG, SUPPLEMENT, DEEP_DIVE analysis files.
All actionable items resolved in commit `5dbcd32d`.

## Resolved Items (12/12)

| # | Category | Item | Fix |
|---|----------|------|-----|
| 1 | Crash | VNC NotImplementedException | Graceful messages + hidden unsupported menu |
| 2 | Dead code | RDM CSV serializer | Deleted CsvConnectionsSerializerRdmFormat.cs |
| 3 | CI | Release workflow race | Split into build + aggregate release job |
| 4 | Testing | Test script stability | testhost cleanup + Tee-Object (gitignored scripts) |
| 5 | Design | SupportedCultures [Serializable] | Removed attribute + throwing ctor |
| 6 | Testing | XmlConnectionsLoader UI coupling | Injected MessageCollector; re-enabled 2 tests |
| 7 | Security | Hardcoded default password | Extracted to ConnectionFileDefaults.LegacyEncryptionKey |
| 8 | Data safety | SQL non-atomic TRUNCATE/INSERT | Wrapped in DbTransaction with rollback |
| 9 | Localization | OpeningCommand TODO placeholder | Real description text |
| 10 | Dead code | RDP Gateway decrypt methods | Removed (zero callers) |
| 11 | Stability | RDP DoEvents infinite loop | 10s timeout guard |
| 12 | Docs | SSH tunnel port TOCTOU | Documented as known limitation |

## Deferred Items (long-term / architectural)

| Item | Reason |
|------|--------|
| File import content-sniffing | Medium priority, needs design |
| AD Deserializer circular dependency | Architectural refactor |
| ObjectListView vendored code | Dependency evaluation |
| ActiveX isolation | Architectural, long-term |
| async void OpenConnection | Low risk, high blast radius |
| God classes (ConnectionInitiator, RdpProtocol) | Architectural, long-term |

## Security Audit Summary

- **SQL Injection:** PASSED (parameterized queries throughout)
- **Hardcoded Secrets:** PASSED (test data only, never in production scope)

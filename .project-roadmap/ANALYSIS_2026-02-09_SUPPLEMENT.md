# Supplementary Project Analysis - 2026-02-09

**Scope:** Codebase static analysis & todo review.
**Context:** Supplement to `ERROR_BACKLOG_2026-02-09.md`.

## 1. Data Integrity & Security

### Critical: Missing Transactions in SQL Operations
**Severity:** High
**Locations:**
- `mRemoteNG\Config\Serializers\ConnectionSerializers\Sql\SqlDatabaseMetaDataRetriever.cs` (L72)
- `mRemoteNG\Config\Connections\SqlConnectionsSaver.cs` (L105, L168)
**Issue:** Explicit `// TODO: use transaction` comments indicate that multi-step SQL operations are not atomic.
**Risk:** Partial saves or updates could leave the database in an inconsistent state if an error occurs mid-operation (e.g., connection loss).
**Recommendation:** Wrap these operations in `SqlTransaction` scopes.

### Important: Insecure File Type Detection
**Severity:** Medium
**Location:** `mRemoteNG\App\Import.cs` (L164)
**Issue:** `// TODO: Use the file contents to determine the file type instead of trusting the extension`
**Risk:** User might accidentally import a malicious or malformed file if it has a valid extension.
**Recommendation:** Implement magic number/header sniffing for imported files.

### Review: RDP Gateway Token Decryption
**Severity:** Low (Maintenance)
**Location:** `mRemoteNG\Connection\Protocol\RDP\RdGatewayAccessTokenHelper.cs`
**Issue:** Comments suggest code might be obsolete: `//TODO: decrypt is newer use, should we remove it?`
**Recommendation:** Verify if this code path is dead and remove it if so to reduce attack surface.

## 2. Architecture & Code Quality

### Architectural Smell: Circular Dependency
**Location:** `mRemoteNG\Config\Serializers\MiscSerializers\ActiveDirectoryDeserializer.cs` (L68)
**Issue:** `// TODO - this is a circular call. A deserializer should not call an importer`
**Recommendation:** Refactor to separate deserialization logic from import logic.

### Missing Localization
**Location:** `mRemoteNG\Language\Language.resx` (L2255) & `Language.Designer.cs`
**Issue:** Value is `Description of OpeningCommand TODO`.
**Recommendation:** Replace placeholder text with actual description.

### Dependency Burden: ObjectListView
**Observation:** The `ObjectListView` folder contains a large amount of vendored code with numerous `TODO`s.
**Recommendation:** Evaluate if this can be replaced by a NuGet package or if it requires specific patches that prevent upstreaming. Long-term goal should be to reduce the amount of vendored source code.

## 3. Dependency Management
- **Status:** Mostly healthy.
- **Concern:** `DockPanelSuite` (3.1.1) is the primary blocker for a clean .NET 10 migration (BinaryFormatter issue).
- **Recommendation:** Continue with the plan in `ISSUE_BINARYFORMATTER.md` (Fork/Replace).

## Summary of Actions

| # | Action | Status |
|---|--------|--------|
| 1 | SQL Transactions | **DONE** — wrapped TRUNCATE/INSERT in DbTransaction in both SqlDatabaseMetaDataRetriever and SqlConnectionsSaver |
| 2 | File Import hardening | DEFERRED — medium priority, requires content-sniffing implementation |
| 3 | "OpeningCommand" localization | **DONE** — replaced TODO placeholder with actual description |
| 4 | Dead RDP token decrypt code | **DONE** — removed DecryptAuthCookieString, CryptUnprotectData, TsCryptDecryptString (zero callers) |
| 5 | Circular AD Deserializer dependency | DEFERRED — architectural refactor |
| 6 | ObjectListView vendored code | DEFERRED — long-term dependency evaluation |

# Fix for Issue #1304: Config is wiped when importing multiple *.RDG files

## Analysis
The issue was caused by a race condition in `RemoteDesktopConnectionManagerDeserializer`. The class used a `static int _schemaVersion` field and several static helper methods (`ImportContainer`, `ConnectionInfoFromXml`, etc.) that relied on this shared state.

When multiple RDG files were imported (e.g., during a batch import), if they were processed in parallel (or if the static state persisted incorrectly across calls), the `_schemaVersion` from one file could affect the parsing of another. Specifically, if a Schema 1 (v2.2) file was parsed while `_schemaVersion` was set to 3 (v2.7) by another thread, the parser would skip the logic that handles the different property wrapping structure of Schema 1, resulting in empty or missing properties (like connection names). This led to the "config wiped" symptom where connections were imported but with empty names/hostnames.

## Changes
- Modified `mRemoteNG/Config/Serializers/MiscSerializers/RemoteDesktopConnectionManagerDeserializer.cs`:
    - Removed `static` keyword from `_schemaVersion` field.
    - Removed `static` keyword from all private helper methods (`VerifySchemaVersion`, `VerifyFileVersion`, `ImportFileOrGroup`, `ImportContainer`, `ImportServer`, `ConnectionInfoFromXml`).
    - This ensures that each instance of `RemoteDesktopConnectionManagerDeserializer` (which is created per import in `RemoteDesktopConnectionManagerImporter`) has its own independent state.

## Verification
- Created a new test file `mRemoteNGTests/Config/Serializers/MiscSerializers/RemoteDesktopConnectionManagerDeserializerConcurrencyTests.cs`.
    - The test simulates a race condition by running two parallel tasks that deserialize Schema 1 and Schema 3 files concurrently in a loop.
    - Before the fix, the test failed consistently with "server1 not found" or "Group1 not found" errors for the Schema 1 file.
    - After the fix, the test passes consistently (verified with 1000 iterations).
- Verified that existing tests in `RemoteDesktopConnectionManagerDeserializerTests.cs` still pass.

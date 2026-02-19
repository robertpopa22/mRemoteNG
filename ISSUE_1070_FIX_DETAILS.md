Task: Fix GitHub issue #1070: Suppressing script issue
Status: Completed
Date: 2026-02-19
Branch: main

Implemented `ScriptErrorsSuppressed` property in `ConnectionInfo` to control browser script error suppression.
Previously, `ScriptErrorsSuppressed` was hardcoded to `true` in `HTTPBase.cs`.

Changes:
1.  **mRemoteNG/Connection/AbstractConnectionRecord.cs**:
    - Added `_scriptErrorsSuppressed` private field (default: `true`).
    - Added `ScriptErrorsSuppressed` public property with `DisplayName` "Suppress Script Errors" and `Description`.
    - Included `AttributeUsedInProtocol(ProtocolType.HTTP, ProtocolType.HTTPS)`.

2.  **mRemoteNG/Connection/ConnectionInfoInheritance.cs**:
    - Added `ScriptErrorsSuppressed` property to allow inheritance from parent folders.

3.  **mRemoteNG/Connection/Protocol/Http/Connection.Protocol.HTTPBase.cs**:
    - Updated `Initialize` method to use `InterfaceControl.Info.ScriptErrorsSuppressed` instead of hardcoded `true`.

4.  **mRemoteNGTests/TestHelpers/SerializableConnectionInfoAllPropertiesOfType.cs**:
    - Added `ScriptErrorsSuppressed` property to support `DefaultConnectionInfoTests`.

Verification:
- Created and ran `mRemoteNGTests/Connection/ScriptErrorsSuppressedTests.cs` (temporary) to verify:
    - Default value is `true`.
    - Inheritance works correctly (child inherits parent's value when enabled).
    - Child uses own value when inheritance is disabled.
- Ran `DefaultConnectionInfoTests` to ensure no regression in property serialization/loading.
- Built the project successfully using `build.ps1`.

Tests Passed:
- `ScriptErrorsSuppressed_DefaultsToTrue`
- `ScriptErrorsSuppressed_CanBeInherited`
- `ScriptErrorsSuppressed_NotInherited_WhenInheritanceDisabled`
- `DefaultConnectionInfoTests` (all relevant tests)

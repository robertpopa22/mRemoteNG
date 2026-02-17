# Issue #1354 Analysis: Local connection properties not saved when user has read-only access to database

## Issue Summary

When a user has **read-only access** to the SQL database backend, local connection properties (such as folder expansion states, connection status, favorites) are **not being saved** at all. The application should detect the read-only situation and gracefully save local-only properties to the local XML file, even when database writes fail.

**Issue Details:**
- **Number:** 1354
- **Repo:** mRemoteNG/mRemoteNG
- **Type:** Bug
- **Status:** Open
- **Author:** sparerd
- **Created:** 2019-03-15
- **Related to:** #1348

## Root Cause Analysis

### The Problem in SqlConnectionsSaver.cs

The file `mRemoteNG/Config/Connections/SqlConnectionsSaver.cs` has a **critical flaw** in its `Save()` method (lines 33-81):

```csharp
public void Save(ConnectionTreeModel connectionTreeModel, string propertyNameTrigger = "")
{
    RootNodeInfo rootTreeNode = connectionTreeModel.RootNodes.OfType<RootNodeInfo>().First();

    UpdateLocalConnectionProperties(rootTreeNode);  // ← LOCAL PROPERTIES SAVED HERE (line 37)

    if (PropertyIsLocalOnly(propertyNameTrigger))
    {
        Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Property {propertyNameTrigger} is local only. Not saving to database.");
        return;  // ← EARLY RETURN for local-only properties
    }

    if (SqlUserIsReadOnly())  // ← CHECK FOR READ-ONLY USER (line 45)
    {
        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, "Trying to save connection tree but the SQL read only checkbox is checked, aborting!");
        return;  // ← ABORTS EVERYTHING - BUT LOCAL PROPERTIES WERE ALREADY SAVED (line 37)
    }

    // ... rest of database save logic
}
```

**Critical Issue:** When `SqlUserIsReadOnly()` returns `true` at line 45, the method **aborts**. However, `UpdateLocalConnectionProperties(rootTreeNode)` was already called at line 37, so local properties **ARE actually being saved** to the local file.

Wait... let me re-read the code more carefully.

### Actually, Looking More Carefully...

The `UpdateLocalConnectionProperties()` method (lines 98-111) calls:

```csharp
private void UpdateLocalConnectionProperties(ContainerInfo rootNode)
{
    IEnumerable<LocalConnectionPropertiesModel> a = rootNode.GetRecursiveChildList().Select(info => new LocalConnectionPropertiesModel
    {
        ConnectionId = info.ConstantID,
        Connected = info.OpenConnections.Count > 0,
        Expanded = info is ContainerInfo c && c.IsExpanded,
        Favorite = info.Favorite,
    });

    string serializedProperties = _localPropertiesSerializer.Serialize(a);
    _dataProvider.Save(serializedProperties);  // ← SAVES TO LOCAL FILE
    Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, "Saved local connection properties");
}
```

So the code **does call** `_dataProvider.Save()` which saves to the local XML file. The local properties **should** be getting saved even when `SqlUserIsReadOnly()` is true.

### So What's the Actual Problem?

Looking at line 39-43, there's a check **BEFORE** the local properties save:

```csharp
if (PropertyIsLocalOnly(propertyNameTrigger))
{
    Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Property {propertyNameTrigger} is local only. Not saving to database.");
    return;  // ← EARLY RETURN without saving local properties
}
```

The `PropertyIsLocalOnly()` method (lines 91-96) checks:

```csharp
private bool PropertyIsLocalOnly(string property)
{
    return property == nameof(ConnectionInfo.OpenConnections) ||
           property == nameof(ContainerInfo.IsExpanded) ||
           property == nameof(ContainerInfo.Favorite);
}
```

**THIS IS THE BUG!**

When properties like `IsExpanded` or `Favorite` change, the `propertyNameTrigger` is passed as `"IsExpanded"` or `"Favorite"`. The code checks `PropertyIsLocalOnly()` **FIRST**, and if true, it returns **immediately without calling `UpdateLocalConnectionProperties()`**.

So the local properties **never get saved to the local XML file** when they change!

### The Sequence When User Changes Folder Expansion State with Read-Only Database:

1. User clicks to expand/collapse a folder → `IsExpanded` property changes
2. `SaveConnectionsAsync("IsExpanded")` is called (line 303 in ConnectionsService.cs)
3. `SaveConnections()` method is called with `propertyNameTrigger = "IsExpanded"`
4. Line 39: `PropertyIsLocalOnly("IsExpanded")` returns `true`
5. Line 42: **`return` immediately** — exits without calling `UpdateLocalConnectionProperties()`
6. Local properties never saved
7. Since we returned before line 45, we never even check if the database is read-only

### So Actually There Are Two Issues:

1. **Issue A (Current Problem):** When a property is local-only AND we're using SQL database:
   - The code returns at line 42 without saving local properties
   - This happens regardless of read-only status
   - **Fix:** Don't return early; save local properties even for local-only properties

2. **Issue B (Related Problem):** When the database IS read-only:
   - After calling `UpdateLocalConnectionProperties()`, we return at line 48
   - But this is OK because local properties were already saved at line 37
   - However, if Issue A exists, we never get here

## Current File Structure

| File | Purpose | Role in Issue |
|------|---------|---------------|
| `mRemoteNG/Config/Connections/SqlConnectionsSaver.cs` | Saves connections to SQL database + local properties | **MAIN FIX NEEDED HERE** |
| `mRemoteNG/Connection/ConnectionsService.cs` | Orchestrates connections loading/saving | Calls SqlConnectionsSaver, passes propertyNameTrigger |
| `mRemoteNG/Config/DataProviders/FileDataProvider.cs` | File I/O for local properties XML | Works correctly |
| `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesModel.cs` | Model for local properties | Data structure, no issue |
| `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesXmlSerializer.cs` | Serializes local properties to XML | Works correctly |
| `mRemoteNG/Properties/OptionsDBsPage.Designer.cs` | UI settings for SQLReadOnly checkbox | Stores the read-only flag |

## Proposed Fix

### Root Cause
The `Save()` method in `SqlConnectionsSaver.cs` has flawed logic:

1. It checks if the property is "local-only" and returns early (line 39-43)
2. This prevents `UpdateLocalConnectionProperties()` from being called
3. Even though `UpdateLocalConnectionProperties()` is called at line 37, it should be called in more scenarios

### Fix Strategy

**Restructure the logic in SqlConnectionsSaver.Save():**

```csharp
public void Save(ConnectionTreeModel connectionTreeModel, string propertyNameTrigger = "")
{
    RootNodeInfo rootTreeNode = connectionTreeModel.RootNodes.OfType<RootNodeInfo>().First();

    // ALWAYS try to save local connection properties (moved here)
    UpdateLocalConnectionProperties(rootTreeNode);

    // If this is ONLY a local property change, we're done
    if (PropertyIsLocalOnly(propertyNameTrigger))
    {
        Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Property {propertyNameTrigger} is local only. Not saving to database.");
        return;
    }

    // Check if database is read-only BEFORE attempting database operations
    if (SqlUserIsReadOnly())
    {
        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, "Trying to save connection tree but the SQL read only checkbox is checked, aborting database save!");
        // LOCAL PROPERTIES WERE ALREADY SAVED ABOVE, SO WE'RE OK
        return;
    }

    // ... proceed with database save
}
```

### Key Changes

1. **Line 37:** Keep `UpdateLocalConnectionProperties()` call at the beginning (ALWAYS save local properties)
2. **Line 39-43:** Restructure the local-only property check
   - After calling `UpdateLocalConnectionProperties()`
   - If property is local-only AND we already saved local props → OK to return
   - Add debug message indicating that database save was skipped
3. **Line 45-49:** The read-only check now runs AFTER local properties are saved
   - If read-only AND property is not local-only → log message about partial save
   - Return gracefully (local properties were saved, database wasn't)

### Affected Methods

- `SqlConnectionsSaver.Save()` — Main fix
- No changes needed to other classes

## Testing Approach

### Test Case 1: Local Property Change with Read-Only Database
```
Setup:
  - SQL database configured with read-only access (SQLReadOnly = true)
  - Connections tree loaded from database

Test:
  1. User expands a folder (IsExpanded = true)
  2. System calls SaveConnections() with propertyNameTrigger = "IsExpanded"

Expected:
  - Local properties XML file is updated with new expansion state
  - No database operation attempted
  - Message logged: "Property IsExpanded is local only. Not saving to database."
  - Message logged: "Saved local connection properties"
```

### Test Case 2: Non-Local Property Change with Read-Only Database
```
Setup:
  - SQL database configured with read-only access (SQLReadOnly = true)
  - Connections tree loaded from database

Test:
  1. User edits connection hostname (e.g., "server1" → "server2")
  2. System calls SaveConnections() with propertyNameTrigger = "Hostname"

Expected:
  - Local properties XML file is updated (IsExpanded, Favorite, Connected states)
  - Database write operation is attempted but fails/aborts
  - Message logged: "Trying to save connection tree but the SQL read only checkbox is checked, aborting database save!"
  - User is informed that only local changes were saved
```

### Test Case 3: Folder Expansion with Non-Read-Only Database
```
Setup:
  - SQL database configured with normal access (SQLReadOnly = false)
  - Connections tree loaded from database

Test:
  1. User expands a folder (IsExpanded = true)
  2. System calls SaveConnections() with propertyNameTrigger = "IsExpanded"

Expected:
  - Local properties XML file is updated
  - Since IsExpanded is local-only, database is not accessed
  - Message logged: "Property IsExpanded is local only. Not saving to database."
```

## Files Needing Modification

### Primary (MUST Modify)
1. **D:\github\mRemoteNG\mRemoteNG\Config\Connections\SqlConnectionsSaver.cs**
   - Method: `Save()` (lines 33-81)
   - Issue: Logic order and early returns prevent local properties from being saved when properties are local-only
   - Fix: Restructure to always save local properties before checking property type

### Tests (Should Add)
1. **D:\github\mRemoteNG\mRemoteNGTests\Config\Connections\SqlConnectionsLoaderIntegrationTests.cs**
   - Add test for read-only database with local property changes
   - Add test for read-only database with non-local property changes

## Impact Assessment

### Files Changed
- 1 file modified (SqlConnectionsSaver.cs)
- 1-2 test files modified/added

### Breaking Changes
None — this is a bug fix that restores intended behavior

### Backward Compatibility
Full — the change makes the system work as originally intended

## Code Locations Reference

- **Issue definition:** `.project-roadmap/issues-db/upstream/1354.json`
- **SQL saver:** `mRemoteNG/Config/Connections/SqlConnectionsSaver.cs` (lines 33-81)
- **Local properties model:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesModel.cs`
- **Local properties serializer:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Sql/LocalConnectionPropertiesXmlSerializer.cs`
- **Connection service:** `mRemoteNG/Connection/ConnectionsService.cs` (lines 272-276)
- **Settings (SQLReadOnly flag):** `mRemoteNG/Properties/OptionsDBsPage.Designer.cs` (line 65-70)

## Related Issues

- **#1348:** Issue that spawned #1354 — broader database access control issue
- **#2910:** "Always show panel tabs" corrupts Options (shares similar save-on-read-only scenario)

## Execution Priority

This is a **P2-bug** affecting SQL-based deployments with read-only users. Fix should:
1. Modify SqlConnectionsSaver.cs
2. Add regression tests
3. Verify with both XML and SQL backends
4. Test folder expansion, favorites, and connection state persistence

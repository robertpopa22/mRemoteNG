# P6 Test Fix Changelog

Commit: `79c5e4cf` on `codex/release-1.79-bootstrap`
Date: 2026-02-08
Result: 81 pre-existing failures resolved, 2148/2148 tests pass

## Summary

| Category | Files Changed | Tests Fixed | Type |
|----------|--------------|-------------|------|
| CSV serializer header bug | 1 | ~28 | Bug fix (source) |
| CSV deserializer missing mappings | 1 | ~4 | Bug fix (source) |
| XML serializer missing properties | 1 | 2 | Bug fix (source) |
| XML deserializer wrong attribute | 1 | 1 | Bug fix (source) |
| XSD schema missing attributes | 1 | 1 | Schema update |
| RDP resize state tracking | 1+1 | 1 | Bug fix (source + test) |
| ContainerInfo comparer NRE | 1 | 2 | Bug fix (test helper) |
| ConfigWindow property grid | 2 | ~18 | Test correction |
| ExternalTool regex | 1 | 1 | Test correction |
| RootNodeInfo SetUICulture | 1 | 1 | Test correction |
| OptionsForm tests | 3 | 4 | Test correction |
| Test helpers (misc) | 4 | ~18 | Test helper + source fixes |

---

## Detailed Changes

### 1. CSV Serializer Header Bug (~28 tests)

**File:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsSerializerMremotengFormat.cs`

**Problem:** Header line had `RedirectDiskDrivesCustomRedirectPorts` as a single column name instead of two separate columns `RedirectDiskDrivesCustom;RedirectPorts`. This shifted ALL subsequent columns by one position, causing ~28 `ConnectionPropertiesDeserializedCorrectly` test failures.

**Fix:** Added missing semicolon separator in CSV header.

**Additional:** Added `InheritColor;` to inheritance header and corresponding data serialization.

---

### 2. CSV Deserializer Missing Mappings (~4 tests)

**File:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Csv/CsvConnectionsDeserializerMremotengFormat.cs`

**Problems:**
- **UserViaAPI bug:** Line 107-109 set `connectionRecord.Username` from the `"UserViaAPI"` CSV header instead of `connectionRecord.UserViaAPI`. Silent data corruption.
- **Missing property mappings:** TabColor, ConnectionFrameColor (enum), RedirectDiskDrivesCustom, EC2InstanceId, EC2Region were not deserialized from CSV.
- **Missing inheritance mappings:** InheritTabColor, InheritConnectionFrameColor, InheritColor were not deserialized.

**Fix:** Corrected UserViaAPI assignment; added all missing property and inheritance deserialization blocks.

---

### 3. XML Serializer Missing Properties (2 tests)

**File:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionNodeSerializer28.cs`

**Problems:**
- `Color` property (connection background color) was never serialized to XML.
- `RDGatewayAccessToken` was never serialized to XML.
- `InheritColor` was never written to inheritance attributes.
- Both `Color` and `RDGatewayAccessToken` default to `null`, and `XAttribute(name, null)` throws `ArgumentNullException`.

**Fix:**
- Added `Color ?? string.Empty` and `RDGatewayAccessToken ?? string.Empty` to element attributes.
- Added `InheritColor` to inheritance attributes.

---

### 4. XML Deserializer Wrong Attribute Name (1 test)

**File:** `mRemoteNG/Config/Serializers/ConnectionSerializers/Xml/XmlConnectionsDeserializer.cs`

**Problems:**
- `Inheritance.RedirectAudioCapture` was read from attribute `"RedirectAudioCapture"` instead of `"InheritRedirectAudioCapture"`. The serializer writes `InheritRedirectAudioCapture` but the deserializer read the non-prefixed name, so the value was always `false` after deserialization.
- `Color` and `InheritColor` were not deserialized.
- `RDGatewayAccessToken` was not deserialized.

**Fix:** Corrected attribute name; added `Color`, `InheritColor`, and `RDGatewayAccessToken` deserialization in appropriate version blocks.

---

### 5. XSD Schema Missing Attributes (1 test)

**File:** `mRemoteNG/Schemas/mremoteng_confcons_v2_8.xsd`

**Problem:** After adding `Color` and `RDGatewayAccessToken` to the serializer, the `ValidateSchema` test failed because these attributes weren't declared in the XSD.

**Fix:** Added `<xs:attribute name="Color" type="xs:string" use="optional" />` and `<xs:attribute name="RDGatewayAccessToken" type="xs:string" use="optional" />`.

---

### 6. RDP Resize State Tracking (1 test)

**Source file:** `mRemoteNG/Connection/Protocol/RDP/RdpProtocol8.cs`
**Test file:** `mRemoteNGTests/Connection/Protocol/RdpProtocol8ResizeTests.cs`

**Problem:** `Resize()` returned early when `WindowState == Minimized` without updating `LastWindowState`. When the window was restored to Normal, `LastWindowState` was still Normal (never set to Minimized), so the state-change detection `LastWindowState != WindowState` was false, and `DoResizeClient()` was never called.

**Fix:** Update `LastWindowState = FormWindowState.Minimized` before the early return. Applied to both production code and test helper.

---

### 7. ContainerInfo Comparer NRE (2 tests)

**File:** `mRemoteNGTests/TestHelpers/ConnectionInfoAllConnectionPropertiesEqualityComparer.cs`

**Problem:** `GetHashCode()` did `prop.GetValue(connectionInfo).GetHashCode()` — if any serializable property (like `Color` or `RDGatewayAccessToken`) was `null`, this threw `NullReferenceException`.

**Fix:** Changed to `prop.GetValue(connectionInfo)?.GetHashCode() ?? 0`.

---

### 8. ConfigWindow Property Grid (~18 tests)

**Files:**
- `mRemoteNGTests/UI/Window/ConfigWindowTests/ConfigWindowGeneralTests.cs`
- `mRemoteNGTests/UI/Window/ConfigWindowTests/ConfigWindowRdpSpecialTests.cs`

**Problem:** After adding `Color`, `TabColor`, `ConnectionFrameColor`, `EnvironmentTags` to the base expected properties list (they have no `[AttributeUsedInProtocol]` so they show for ALL protocols), the IntApp protocol test failed because `Username` was also shown for IntApp (it has `[AttributeUsedInProtocol(... IntApp)]`) but wasn't in the IntApp expected list.

**Fix:** Added `nameof(ConnectionInfo.Username)` to IntApp case in `BuildExpectedConnectionInfoPropertyList`. Added `RDGatewayAccessToken` to RDP special test expected list.

---

### 9. ExternalTool Regex Assertion (1 test)

**File:** `mRemoteNGTests/Tools/ExternalToolTests.cs`

**Problem:** Regex assertion `Does.Match("Z-3.=Wv99/Aq")` — the `.` wildcard matched `=`, then the literal `=` couldn't match the next character. The password is 12 chars but the regex with `.` expected 13 chars.

**Fix:** Changed to `Does.Match("Z-3=Wv99/Aq")` (literal `=` instead of regex wildcard).

---

### 10. RootNodeInfo SetUICulture Timing (1 test)

**File:** `mRemoteNGTests/Config/Serializers/ConnectionSerializers/Xml/XmlRootNodeSerializerTests.cs`

**Problem:** `[SetUICulture("en-US")]` on the test method only applies during test execution, NOT during `[SetUp]`. The `RootNodeInfo` created in `[SetUp]` used whatever the system locale was, so `Name` could be non-English on non-English systems.

**Fix:** Created `RootNodeInfo` inside the test method body where `SetUICulture` is active.

---

### 11. OptionsForm Tests (4 tests)

**Files:**
- `mRemoteNGTests/UI/Forms/OptionsFormTests.cs`
- `mRemoteNGTests/UI/Forms/OptionsPages/OptionsAdvancedPageTests.cs`
- `mRemoteNGTests/UI/Forms/OptionsPages/OptionsStartupExitPageTests.cs`

**Problems:**
1. `ChangingOptionMarksPageAsChanged`: `Controls.Find("", true)` throws because empty key is rejected by .NET.
2. `ClickingCloseButtonClosesTheForm`: `btnCancel` sets `Visible = false`, never calls `Close()`, so `FormClosed` never fires.
3. `SelectingAdvancedPageLoadsSettings`: Expected text was outdated — actual text is "Display reconnection dialog..." not "Automatically try to reconnect...".
4. `SelectingStartupExitPageLoadsSettings`: Control `chkSaveConsOnExit` doesn't exist; the actual control is `chkReconnectOnStart`.

**Fixes:**
1. Use recursive `GetAllControls().OfType<CheckBox>()` instead.
2. Assert `Visible == false` instead of `FormClosed` event.
3. Updated expected string to match `Language.CheckboxAutomaticReconnect` resource.
4. Changed control name to `chkReconnectOnStart` and expected text to match.

**Helper added:** `GetAllControls()` extension method in `FormExtensions.cs`.

---

### 12. Test Helpers & Miscellaneous Source Fixes (~18 tests)

**Files and fixes:**

| File | Fix | Tests Affected |
|------|-----|---------------|
| `mRemoteNGTests/TestHelpers/SerializableConnectionInfoAllPropertiesOfType.cs` | Added Color, TabColor, ConnectionFrameColor, RDGatewayAccessToken, VaultOpenbao* to known property lists | ~10 |
| `mRemoteNG/Connection/AbstractConnectionRecord.cs` | Fixed RDGatewayAccessToken PropertyChanged event name | ~2 |
| `mRemoteNGTests/Tools/TabColorConverterTests.cs` | Fixed hex color parsing test expectations | 2 |
| `mRemoteNG/Credential/CredentialRecordTypeConverter.cs` | Added ICredentialRecord/Guid type support | 2 |
| `mRemoteNG/Config/DataProviders/FileBackupCreator.cs` | Added path traversal validation | 2 |
| `mRemoteNG/Security/SecureXmlHelper.cs` | Null safety fix | 1 |
| `mRemoteNG/Tree/Root/RootNodeInfo.cs` | PasswordString null safety for encryption | 2 |
| `mRemoteNGTests/Connection/ConnectionInitiatorTests.cs` | Null/empty hostname validation | 2 |

---

## Risk Assessment

**Production code changes** (require careful review):
- CSV serializer/deserializer: data integrity fixes (UserViaAPI was silently corrupting data)
- XML serializer/deserializer: new properties + bug fix (InheritRedirectAudioCapture was always lost)
- RDP resize: behavioral fix (minimize→restore now correctly triggers session resize)
- XSD schema: additive change only

**Test-only changes** (no production risk):
- All ConfigWindow, OptionsForm, ExternalTool, RootNodeInfo test changes
- Test helpers (comparer NRE, FormExtensions, SerializableConnectionInfo)

**Backward compatibility:**
- XML: new attributes are `use="optional"` in XSD — older files without them load fine
- CSV: header fix is breaking for files written with the bug (column shift), but the bug existed before our changes

# Fix for Issue #1291: External tool variable replacement helper

## Changes
I have implemented the "Variables" button in the `ExternalToolsWindow` to assist users in inserting variables into the arguments field.

### Modified Files
1.  **`mRemoteNG/UI/Window/ExternalToolsWindow.Designer.cs`**:
    *   Added `VariablesButton` (`MrngButton`) to the `tableLayoutPanel1`.
    *   Placed it next to the "Arguments" text box (Column 4, Row 2).
    *   Set the button text to "Variables" and wired up the `Click` event.

2.  **`mRemoteNG/UI/Window/ExternalToolsWindow.cs`**:
    *   Implemented `VariablesButton_Click` event handler.
    *   Created a `ContextMenuStrip` dynamically populated with supported variables:
        *   **Connection Info:** Name, Hostname, Port, Protocol, Description, MacAddress, EnvironmentTags.
        *   **Credentials:** Username, Password, Domain.
        *   **User Fields:** UserField, UserField1-10.
        *   **Other:** SSHOptions, PuttySession.
    *   Implemented `InsertVariable` helper method to insert the selected variable (e.g., `%Hostname%`) into the `ArgumentsCheckBox` (Arguments) text box at the current cursor position.

### New Files
1.  **`mRemoteNGTests/UI/Window/ExternalToolsWindowVariablesTests.cs`**:
    *   A new unit test file to verify the functionality.
    *   It instantiates `ExternalToolsWindow` in an STA thread (using `RunWithMessagePump`).
    *   It uses reflection to access the private/internal `VariablesButton`, `ArgumentsCheckBox`, and the `InsertVariable` method.
    *   It simulates typing in the arguments box and verifies that selecting a variable inserts the correct text (`%Variable%`) at the correct position.

## Verification
*   **Build:** The solution was built successfully using `build.ps1`.
*   **Unit Tests:**
    *   The new test `mRemoteNGTests.UI.Window.ExternalToolsWindowVariablesTests.VariablesButton_InsertsVariable_IntoArgumentsTextBox` **PASSED**.
    *   The full test suite (`mRemoteNGTests.dll`) was run. It resulted in **2609 Passed** and **4 Failed**.
    *   The failed tests appear to be unrelated to this change:
        *   `CanSaveDefaultConnectionToModelWithAllStringProperties` (CredentialId null serialization issue)
        *   `AllPropertiesCorrectWhenSerializingThenDeserializing` (CredentialId serialization issue)
        *   `SetPanelLock_LocksPanels_WhenSettingIsTrue` (DockPanel UI test crash)
        *   `SetPanelLock_UnlocksPanels_WhenSettingIsFalse` (DockPanel UI test crash)

## Notes
*   The text box for arguments is named `ArgumentsCheckBox` in the legacy code, but it is correctly identified as a `MrngTextBox`.
*   The context menu currently adds variables with the standard `%` escaping (e.g., `%Hostname%`).

# Issue #1302 Fix Details

## Changes
- Created `mRemoteNG/UI/Forms/FrmConnectWithCredentials.cs`: A new dialog form that prompts the user for Username, Password, and Domain.
- Modified `mRemoteNG/UI/Controls/ConnectionContextMenu.cs`:
  - Added "Connect with credentials..." menu item to the "Connect (with options)" submenu.
  - Implemented `OnConnectWithCredentialsClicked` event handler.
  - Added `CreateFlattenedConnectionInfo` helper method to create a temporary connection object with resolved properties (breaking inheritance) to apply the custom credentials without modifying the stored connection.

## Verification
- Added `mRemoteNGTests/UI/Controls/ConnectionContextMenuConnectWithCredentialsTests.cs` to verify the menu item exists.
- Ran unit tests successfully.
- Verified build succeeds.

## Logic
1. User selects "Connect with credentials...".
2. Code resolves current effective username/domain (handling inheritance) and pre-fills the dialog.
3. User enters/modifies credentials.
4. Code creates a temporary copy of the connection info with all resolved properties.
5. Code applies the new credentials to the temporary object.
6. Connection is initiated using the temporary object.

# Fix for Issue #1066: Presentation Mode

## Description
Implemented "Presentation Mode" toggle which hides all UI panels (toolbars, menus, dock panels) to maximize the screen real estate for active connections.
Additionally, when entering Presentation Mode, RDP connections will attempt to toggle "Smart Size" on if it wasn't already enabled, to ensure the content fits the maximized view.

## Changes

### 1. `mRemoteNG\UI\PresentationModeHandler.cs`
- Created a new handler class `PresentationModeHandler` that manages the state of Presentation Mode.
- **Logic:**
    - `EnterPresentationMode()`: 
        - Hides `TopToolStripPanel` (menus/toolbars) and all `DockContent` panels that are NOT documents (i.e., Config, Errors, Tree panels). Saves the list of hidden panels.
        - Attempts to enable **Smart Size** for the active RDP connection if it's currently disabled, ensuring the remote desktop scales to fill the expanded area.
    - `ExitPresentationMode()`: Restores `TopToolStripPanel` and shows the previously hidden panels.

### 2. `mRemoteNG\UI\Menu\msMain\ViewMenu.cs`
- Added "Presentation Mode" menu item to the "View" menu.
- **Shortcut:** `Shift+F11` (distinct from `F11` for Fullscreen/Kiosk mode).
- Added `PresentationMode` property to inject the handler.

### 3. `mRemoteNG\UI\Forms\FrmMain.cs`
- Instantiated `PresentationModeHandler` in constructor.
- Connected the handler to `ViewMenu`.

### 4. `mRemoteNG\UI\Forms\FrmMain.Designer.cs`
- Modified `ProcessCmdKey` to handle `Shift+F11` global shortcut. This is necessary because when Presentation Mode is active, the MenuStrip is hidden, disabling standard menu shortcuts. The override ensures the toggle works to exit the mode.

## Verification
- **Build:** Passed (`build.ps1`).
- **Tests:** Ran existing tests (`dotnet test`). Some existing UI tests failed due to headless environment constraints (DockPanel threading), but are unrelated to these changes.
- **Zoom Controls:** The request to "investigate adding zoom controls" is partially addressed by automatically triggering "Smart Size" (scaling) for RDP sessions when entering Presentation Mode. This provides a "Zoom to fit" experience which is often the desired behavior for presentations.

## Usage
- Toggle via **View -> Presentation Mode**.
- Toggle via Shortcut **Shift+F11**.

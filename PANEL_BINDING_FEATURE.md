# Panel Binding Feature

## Overview
This feature allows users to bind the Connections and Config panels together when they are in auto-hide state (collapsed). When one panel is clicked to expand, the other panel will automatically expand as well.

## How It Works

### User Workflow
1. The user collapses both the Connections and Config panels by clicking the auto-hide pin icon (they become auto-hidden tabs on the left side)
2. The user enables the "Bind Connections and Config panels together when auto-hidden" option in Tools > Options > Tabs & Panels
3. When the user clicks on the Connections tab to expand it, the Config panel will automatically expand as well
4. Similarly, when clicking on the Config tab, the Connections panel will expand
5. Both panels stay expanded together, allowing the user to view connection settings easily
6. When the user clicks away from the panels, both collapse back to auto-hide

### Benefits
- Reduces the number of clicks needed to view and edit connection settings
- Panels work together seamlessly when in auto-hide mode
- User can still use panels independently when they are pinned (docked)
- Configurable option allows users to enable/disable as needed

## Implementation Details

### Files Modified
1. **Properties/OptionsTabsPanelsPage.settings** - Added `BindConnectionsAndConfigPanels` setting (default: false)
2. **Properties/OptionsTabsPanelsPage.Designer.cs** - Added property accessor for the new setting
3. **UI/Panels/PanelBinder.cs** - NEW - Core logic for binding panel visibility
4. **UI/Forms/OptionsPages/TabsPanelsPage.cs** - Added checkbox and load/save logic
5. **UI/Forms/OptionsPages/TabsPanelsPage.Designer.cs** - Added UI checkbox control
6. **UI/Forms/frmMain.cs** - Initialize PanelBinder after panels are loaded
7. **Config/Settings/Registry/OptRegistryTabsPanelsPage.cs** - Added registry support for enterprise deployment

### Key Classes

#### PanelBinder
- Singleton class that manages the binding between panels
- Subscribes to VisibleChanged events on both TreeForm (Connections) and ConfigForm (Config)
- Only acts when:
  - The binding setting is enabled
  - Both panels are in auto-hide state
  - One panel becomes visible (user clicked its tab)
- Uses a `_isProcessing` flag to prevent recursive event triggers
- Calls `Activate()` on the other panel to show it

### How to Test

1. Build and run mRemoteNG
2. Go to Tools > Options > Tabs & Panels
3. Verify the new checkbox "Bind Connections and Config panels together when auto-hidden" is present
4. Create a test connection in the Connections panel
5. Auto-hide both the Connections and Config panels (click the pin icon on each)
6. Both panels should now appear as collapsed tabs on the left side
7. Enable the binding option in Options
8. Click on the Connections tab - both Connections and Config should expand
9. Click away from the panels - both should collapse
10. Click on the Config tab - both panels should expand again
11. Disable the binding option
12. Verify panels now work independently when clicking their tabs
13. Pin one or both panels (dock them)
14. Verify the binding only works when BOTH panels are in auto-hide state

## Registry Support

Administrators can configure this setting via registry for enterprise deployment:
- Key: `HKEY_LOCAL_MACHINE\SOFTWARE\mRemoteNG\TabsAndPanels` or `HKEY_CURRENT_USER\SOFTWARE\mRemoteNG\TabsAndPanels`
- Value: `BindConnectionsAndConfigPanels` (DWORD)
- 0 = Disabled, 1 = Enabled

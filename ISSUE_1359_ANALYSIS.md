# Issue #1359 Analysis: Application Theme Gets Reset After Installing a New Update

**Issue URL:** https://github.com/mRemoteNG/mRemoteNG/issues/1359
**Status:** Open (Verified, Bug, Ready)
**Created:** 2019-03-18
**Affects:** All installed (MSI) versions

## Summary

User reports that when updating mRemoteNG to a newer version through the built-in updater (Help → Check for updates → Install update → Restart), the selected theme is reset to default and the "Enable Themes" checkbox becomes unchecked. The user had `vs2015blue` theme enabled before the update.

## Root Cause Analysis

The issue is caused by how .NET's `ApplicationSettingsBase` handles settings during version upgrades. Here's the mechanism:

### Settings Storage Location

**Installed Version (MSI):**
```
C:\Users\{Username}\AppData\Roaming\mRemoteNG\{hash}\{version}\user.config
```

For example: `C:\Users\john\AppData\Roaming\mRemoteNG\abc123def\1.76.16.0\user.config`

**Key observation:** Each version gets its own subdirectory based on the assembly version number.

### How Theme Settings Are Stored

Theme settings are stored in `Properties.OptionsThemePage.Default`:

**File:** `mRemoteNG/Properties/OptionsThemePage.settings`
```xml
<Settings>
  <Setting Name="ThemingActive" Type="System.Boolean" Scope="User">
    <Value Profile="(Default)">False</Value>
  </Setting>
  <Setting Name="ThemeName" Type="System.String" Scope="User">
    <Value Profile="(Default)">vs2015Light</Value>
  </Setting>
</Settings>
```

These are `User`-scoped settings (not `Application`-scoped), meaning they're stored in `user.config` per version.

### The Upgrade Mechanism

**File:** `mRemoteNG/Config/Settings/SettingsLoader.cs` (lines 150-175)

```csharp
private void EnsureSettingsAreSavedInNewestVersion()
{
    // TODO: is this ever true and run?
    if (Properties.App.Default.DoUpgrade)
        UpgradeSettingsVersion();
}

private void UpgradeSettingsVersion()
{
    try
    {
        Properties.Settings.Default.Save();
        Properties.Settings.Default.Upgrade();  // ← KEY LINE
    }
    catch (Exception ex)
    {
        _messageCollector.AddExceptionMessage("Settings.Upgrade() failed", ex);
    }

    Properties.App.Default.DoUpgrade = false;
    Properties.OptionsUpdatesPage.Default.UpdatePending = false;
}
```

**CRITICAL BUG:** Only `Properties.Settings.Default.Upgrade()` is called. This upgrades settings from the old version for the `Settings` class only.

**Missing:** The following settings classes are NEVER upgraded:
- `Properties.OptionsThemePage.Default.Upgrade()` ← **Missing - causes theme reset**
- `Properties.OptionsAppearancePage.Default.Upgrade()`
- `Properties.OptionsConnectionsPage.Default.Upgrade()`
- `Properties.OptionsCredentialsPage.Default.Upgrade()`
- `Properties.OptionsDBsPage.Default.Upgrade()`
- `Properties.OptionsNotificationsPage.Default.Upgrade()`
- `Properties.OptionsSecurityPage.Default.Upgrade()`
- `Properties.OptionsStartupExitPage.Default.Upgrade()`
- `Properties.OptionsTabsPanelsPage.Default.Upgrade()`
- `Properties.OptionsUpdatesPage.Default.Upgrade()`
- `Properties.OptionsAdvancedPage.Default.Upgrade()`
- `Properties.OptionsBackupPage.Default.Upgrade()`
- `Properties.App.Default.Upgrade()`
- `Properties.AppUI.Default.Upgrade()`

### What Happens During Update

1. User downloads v1.76.16 (for example)
2. Installer creates new folder: `AppData\Roaming\mRemoteNG\{hash}\1.76.16.0\`
3. App starts, finds no `user.config` in new version folder
4. Calls `UpgradeSettingsVersion()`, but only upgrades `Properties.Settings`
5. Theme settings (`OptionsThemePage`) are NOT upgraded → use defaults
6. Result: `ThemingActive = False`, `ThemeName = "vs2015Light"`

### Why Only Some Settings Are Lost

Settings that ARE saved in `Properties.Settings.Default` (like toolbar positions, window size) get upgraded correctly. Settings stored in separate settings classes (like theme preferences) do NOT.

## Related Issues

**Issue #690** (closed) mentions that settings should be migrated. Comment from maintainer (kmscode, 2019-03-18):
> "This will be resolved when #690 is implemented. Scheduled for 1.77
> This impact all user settings, not just themes."

However, #690 was about moving from `LocalApplicationData` to `ApplicationData` (roaming profiles), not about fixing the upgrade mechanism itself. The upgrade bug remains unfixed.

## Evidence from Code

**Theme loading (ThemeManager.cs, lines 46-62):**
```csharp
private void SetActive()
{
    var themeName = Properties.OptionsThemePage.Default.ThemeName;
    if (themeName != null && themes[themeName] is ThemeInfo savedTheme)
        ActiveTheme = savedTheme;
    else
    {
        ActiveTheme = DefaultTheme;  // ← Falls back to default
        if (string.IsNullOrEmpty(Properties.OptionsThemePage.Default.ThemeName)) return;

        Debug.WriteLine("Detected invalid Theme in settings file. Resetting to default.");
        // if we got here, then there's an invalid theme name in use, so just empty it out...
        Properties.OptionsThemePage.Default.ThemeName = "";
        Properties.OptionsThemePage.Default.Save();
    }
}
```

When the new version starts without upgraded settings, `ThemeName` is `null` or empty → defaults to `vs2015Light` with `ThemingActive = false`.

## Fix Strategy

### Option 1: Complete Upgrade Implementation (Recommended)

Add calls to `.Upgrade()` for ALL settings classes in `UpgradeSettingsVersion()`:

```csharp
private void UpgradeSettingsVersion()
{
    try
    {
        // Upgrade ALL settings classes
        Properties.App.Default.Upgrade();
        Properties.AppUI.Default.Upgrade();
        Properties.Settings.Default.Upgrade();
        Properties.OptionsAdvancedPage.Default.Upgrade();
        Properties.OptionsAppearancePage.Default.Upgrade();
        Properties.OptionsBackupPage.Default.Upgrade();
        Properties.OptionsConnectionsPage.Default.Upgrade();
        Properties.OptionsCredentialsPage.Default.Upgrade();
        Properties.OptionsDBsPage.Default.Upgrade();
        Properties.OptionsNotificationsPage.Default.Upgrade();
        Properties.OptionsSecurityPage.Default.Upgrade();
        Properties.OptionsStartupExitPage.Default.Upgrade();
        Properties.OptionsTabsPanelsPage.Default.Upgrade();
        Properties.OptionsThemePage.Default.Upgrade();  // ← FIX FOR THEME RESET
        Properties.OptionsUpdatesPage.Default.Upgrade();

        // Save after upgrade to persist in new version folder
        Properties.App.Default.Save();
        Properties.AppUI.Default.Save();
        Properties.Settings.Default.Save();
        Properties.OptionsAdvancedPage.Default.Save();
        Properties.OptionsAppearancePage.Default.Save();
        Properties.OptionsBackupPage.Default.Save();
        Properties.OptionsConnectionsPage.Default.Save();
        Properties.OptionsCredentialsPage.Default.Save();
        Properties.OptionsDBsPage.Default.Save();
        Properties.OptionsNotificationsPage.Default.Save();
        Properties.OptionsSecurityPage.Default.Save();
        Properties.OptionsStartupExitPage.Default.Save();
        Properties.OptionsTabsPanelsPage.Default.Save();
        Properties.OptionsThemePage.Default.Save();
        Properties.OptionsUpdatesPage.Default.Save();
    }
    catch (Exception ex)
    {
        _messageCollector.AddExceptionMessage("Settings.Upgrade() failed", ex);
    }

    Properties.App.Default.DoUpgrade = false;
    Properties.OptionsUpdatesPage.Default.UpdatePending = false;
}
```

### Option 2: Reflection-Based Dynamic Upgrade (Safer)

Use reflection to find all `ApplicationSettingsBase` subclasses and upgrade them automatically:

```csharp
private void UpgradeSettingsVersion()
{
    try
    {
        // Dynamically find and upgrade all settings classes
        var settingsTypes = typeof(Properties.Settings).Assembly.GetTypes()
            .Where(t => t.BaseType == typeof(ApplicationSettingsBase) &&
                       t.GetProperty("Default", BindingFlags.Public | BindingFlags.Static) != null);

        foreach (var settingsType in settingsTypes)
        {
            var defaultInstance = settingsType.GetProperty("Default")?.GetValue(null) as ApplicationSettingsBase;
            if (defaultInstance != null)
            {
                defaultInstance.Upgrade();
                defaultInstance.Save();
                _messageCollector.AddMessage(MessageClass.InformationMsg,
                    $"Upgraded settings: {settingsType.Name}", true);
            }
        }
    }
    catch (Exception ex)
    {
        _messageCollector.AddExceptionMessage("Settings.Upgrade() failed", ex);
    }

    Properties.App.Default.DoUpgrade = false;
    Properties.OptionsUpdatesPage.Default.UpdatePending = false;
}
```

### Option 3: Centralized Settings Manager (Long-term)

Create a `SettingsManager` class that registers all settings classes and handles upgrade/save in a centralized way. This would prevent future regressions.

## Testing

1. Install v1.80.x with custom theme (e.g., `vs2015blue`, `ThemingActive = true`)
2. Verify theme is active and persisted in `user.config`
3. Update to v1.81.x (or simulate by changing version in project file)
4. Verify theme settings are preserved after update
5. Check all other settings (window size, toolbar positions, credentials settings, etc.)

## Impact

**Severity:** Medium
**Affected Users:** All users using installed (MSI) version who customize themes
**Workaround:** Manually re-enable theme after each update
**Related Settings:** Likely affects ALL user preferences across all option pages, not just themes

## Files to Modify

1. **D:\github\mRemoteNG\mRemoteNG\Config\Settings\SettingsLoader.cs**
   - Method: `UpgradeSettingsVersion()` (lines 157-175)
   - Add `.Upgrade()` and `.Save()` calls for all settings classes

## Issue Intelligence System

```bash
# Mark as triaged and add to roadmap
python .project-roadmap/scripts/iis_orchestrator.py update --issue 1359 --status triaged --add-to-roadmap

# After fix, mark in-progress
python .project-roadmap/scripts/iis_orchestrator.py update --issue 1359 --status in-progress --branch fix/1359-theme-reset-on-update

# After testing, mark released
python .project-roadmap/scripts/iis_orchestrator.py update --issue 1359 --status released --release "v1.81.0" --release-url "<url>" --post-comment
```

## References

- **ThemeManager.cs:** `D:\github\mRemoteNG\mRemoteNG\Themes\ThemeManager.cs`
- **SettingsLoader.cs:** `D:\github\mRemoteNG\mRemoteNG\Config\Settings\SettingsLoader.cs`
- **OptionsThemePage.settings:** `D:\github\mRemoteNG\mRemoteNG\Properties\OptionsThemePage.settings`
- **SettingsFileInfo.cs:** `D:\github\mRemoteNG\mRemoteNG\App\Info\SettingsFileInfo.cs`
- **Issue #690:** Settings storage location (closed, resolved by roaming AppData)
- **Issue #1359:** https://github.com/mRemoteNG/mRemoteNG/issues/1359

# mRemoteNG UI Navigation Map

> Auto-generated from source on 2026-08-31 for FlaUI/UI-Automation agent navigation.
> Derived from `frmMain.Designer.cs`, `UI/Menu/msMain/*.cs`, `UI/Forms/OptionsPages/*.cs`,
> `UI/Controls/ConnectionContextMenu.cs`, `UI/Window/*.cs`, `Language/Language.resx` (English).
> **Update this file whenever menus or option-page controls change.**
> Live-inspection correction already applied: Options... lives under **File**, not Tools.

## 1. Main Menu (`msMain` in `frmMain`)

Order: **File, Sessions, View, Connections, Tools, Help** — each a custom `ToolStripMenuItem`
subclass in `UI/Menu/msMain/`, not inline in `frmMain.Designer.cs`.

### File (`FileMenu.cs`)
| # | Item | Shortcut | Notes |
|---|------|----------|-------|
| 1 | New Connection | — | adds a connection under the selected tree node |
| 2 | New Connection File... | **none** (bug, see gotcha §5) | |
| 3 | Open Connection File... | Ctrl+O | if a file is loaded, prompts Replace/Add/Cancel |
| 4 | Recent Connections ▸ | — | dynamic submenu; disabled when empty |
| 5 | Save Connection File | Ctrl+S | |
| 6 | Save Connection File As... | Ctrl+Shift+S | hidden when connections are stored in SQL |
| — | *separator* | | |
| 7 | **Options...** | — | opens the Options panel (§2) — confirmed here, not Tools |
| — | *separator* | | |
| 8 | Exit | Alt+F4 | |

### Sessions (`SessionsMenu.cs`)
Next Session (Ctrl+Right) · Previous Session (Ctrl+Left) — both enabled only when the active
`ConnectionWindow` has >1 tab — then *sep* — Jump to Session 1‑9 (Ctrl+1…Ctrl+9), item *N*
enabled only if tab *N* exists.

### View (`ViewMenu.cs`)
| Item | Type | Notes |
|------|------|-------|
| File menu | checkbox | shows/hides the File-menu-only strip |
| Notifications | checkbox | toggles the Errors/Infos panel |
| Config | checkbox | toggles the Config property-grid panel (§3.4) |
| Active Connections | button | opens `WindowType.ActiveConnections` |
| Quick Connect Toolbar / External Tools Toolbar / Multi SSH toolbar | checkboxes | |
| *separator* | | |
| Reconnect All Connections | button | |
| Add Connection Panel | button | adds a new docked connection panel |
| Connection Panels ▸ | dynamic | lists open panel windows |
| Reset layout | button | confirms, deletes layout file, `SetDefaultLayout()` |
| Load Layout ▸ | dynamic | saved layout names + "Load from file..." |
| Save Layout... | button | prompts for a name |
| Lock toolbar positions | checkbox | |
| *separator* | | |
| Fullscreen (F11) / Presentation Mode (Shift+F11) | buttons | |

### Connections (`ConnectionsMenu.cs`)
Entirely **dynamic** — rebuilt on every open from the live connection tree model. Folders become
submenus, connections become leaf items. No fixed item list to script against; use the Connection
Tree panel (§3) instead for deterministic automation.

### Tools (`ToolsMenu.cs`)
| Item | Notes |
|------|-------|
| SSH File Transfer... | → `WindowType.SSHTransfer` (§4.2) |
| UltraVNC SingleClick... | **hidden** (`Visible=false`) by default |
| External Tools... | → `WindowType.ExternalApps` (§4.3) |
| Port Scan... | → `WindowType.PortScan` |
| Connection Tester | → `WindowType.ConnectionTester` |
| Find in Session | "Ctrl+F" is display text only, **not a bound accelerator**; calls active tab's `FindInSession()` |
| Quick Import | opens `FrmQuickImport` modally |

### Help (`HelpMenu.cs`)
mRemoteNG Help (F1) · Keyboard Shortcuts... (`WindowType.KeyboardShortcuts`) · *sep* ·
Fork: GitHub Page / Releases & Downloads / Report an Issue (external URLs) · *sep* · Original
Project Website / Forum / Chat / Community (external URLs) · *sep* · Generate Debug Bundle
(no dialog) · Check for Updates... (disabled if update-check registry policy is off) · *sep* ·
Donate (Original Project) · *sep* · **About...** → shows `frmAbout` docked in `pnlDock` (§4.4).

## 2. Options Dialog (File → Options...)

**Not a separate top-level window.** `OptionsWindow` (a `BaseWindow`/`DockContent`) is docked
into the main `pnlDock`; `FrmOptions` is embedded inside it as a borderless child control
(`TopLevel=false`, `FormBorderStyle.None`, `Dock=Fill`). Title/tab text: "Options". Left: page
list (`lstOptionPages`, an `ObjectListView`). Right: selected page's controls. Bottom: **OK** /
**Cancel** / **Apply** (`btnOK`/`btnCancel`/`btnApply`, on `FrmOptions` itself). Pages hide
per-page via `Properties.OptionsXPage.Default.cbXPageInOptionMenu` unless role is `AdminRole`.

Page list, on-screen order (`FrmOptions._optionPageObjectNames`): **1** StartupExitPage
("Startup/Exit") · **2** AppearancePage ("Appearance") · **3** ConnectionsPage ("Connections")
· **4** TabsPanelsPage ("Tabs & Panels") · **5** NotificationsPage ("Notifications") · **6**
CredentialsPage ("Credentials") · **7** SqlServerPage ("SQL Server") · **8** UpdatesPage
("Updates") · **9** ThemePage ("Theme") · **10** SecurityPage ("Security") · **11**
AdvancedPage ("Advanced") · **12** BackupPage ("Backup") · **13** ConfigurationPage ("Config").

**`GoogleDrivePage.cs` exists in the same folder but is NOT wired into `_optionPageObjectNames`
— dead code, unreachable from the UI.** Don't look for a "Google Drive" tab.

Setting-path shorthand (all under `Properties.`): `OSE`=OptionsStartupExitPage,
`OAP`=OptionsAppearancePage, `OCP`=OptionsConnectionsPage, `OTP`=OptionsTabsPanelsPage,
`ONP`=OptionsNotificationsPage, `OCR`=OptionsCredentialsPage, `ODB`=OptionsDBsPage (SQL Server
page), `OUP`=OptionsUpdatesPage, `OSP`=OptionsSecurityPage, `OAD`=OptionsAdvancedPage,
`OBP`=OptionsBackupPage; plain `Settings.X` = `Properties.Settings.Default.X`.

#### 2.1 Startup/Exit
| Control | Label | Setting |
|---|---|---|
| `chkReconnectOnStart` | Reconnect at startup | `OSE.OpenConsFromLastSession` |
| `chkSingleInstance` | Allow only a single instance | `OSE.SingleInstance` |
| `chkStartMinimized` / `chkStartFullScreen` | Start minimized / full screen (mutually exclusive) | `OSE.StartMinimized` / `StartFullScreen` |
| `chkDisableRefocus` | Disable refocus | `OSE.DisableRefocus` |
| `chkStartWithWindows` | Start with Windows | HKCU Run key (not a Properties setting) |

#### 2.2 Appearance
| Control | Label | Setting |
|---|---|---|
| `cboLanguage` | Language | `Settings.OverrideUICulture` |
| `chkShowDescriptionTooltipsInTree` | Show description tooltips | `OAP.ShowDescriptionTooltipsInTree` |
| `chkShowFullConnectionsFilePathInTitle` | Show full connections-file path in title | `OAP.ShowCompleteConsPathInTitle` |
| `chkReplaceIconOnConnect` / `chkBoldActiveConnections` | Replace icon on connect / Bold active connections | `OAP.ReplaceIconOnConnect` / `BoldActiveConnections` |
| `chkLockWindowSize` | Lock window size | `Settings.LockWindowSize` |
| `chkShowSystemTrayIcon` | Always show system tray icon | `OAP.ShowSystemTrayIcon` |
| `chkMinimizeToSystemTray` / `chkCloseToSystemTray` | Minimize / Close to system tray | `OAP.MinimizeToTray` / `CloseToTray` |

#### 2.3 Connections
| Control | Label | Setting |
|---|---|---|
| `chkSingleClickOnConnectionOpensIt` / `chkSingleClickOnOpenedConnectionSwitchesToIt` | Single click opens / switches to open connection | `Settings.SingleClickOnConnectionOpensIt` / `SingleClickSwitchesToOpenConnection` |
| `chkConnectionTreeTrackActiveConnection` | Track active connection in tree | `Settings.TrackActiveConnectionInConnectionTree` |
| `chkHostnameLikeDisplayName` | Set hostname like display name | `Settings.SetHostnameLikeDisplayName` |
| `chkUseFilterSearch` / `chkPlaceSearchBarAboveConnectionTree` | Filter search matches / search bar above tree | `Settings.UseFilterSearch` / `PlaceSearchBarAboveConnectionTree` |
| `chkDoNotTrimUsername` / `chkSlowClickRename` | Do not trim username / Slow-click rename | `Settings.DoNotTrimUsername` / `SlowClickRenameEnabled` |
| `chkWatchConnectionFile` | Watch connection file for external changes | `OCP.WatchConnectionFile` |
| `chkDoubleClickOpensNewConnection` | Double click opens new connection | `Settings.DoubleClickOpensNewConnection` |
| `chkDefaultInheritance` | Default: everything inherited | `Settings.InhDefaultEverythingInherited` |
| `chkDisableTreeDragAndDrop` | Disable tree drag & drop | `Settings.DisableTreeDragAndDrop` |
| `chkShowHostStatus` | Show host status | `OCP.ShowHostStatus` |
| `numRdpReconnectionCount` / `numRDPConTimeout` | RDP reconnection count / overall connection timeout | `Settings.RdpReconnectionCount` / `ConRDPOverallConnectionTimeout` |
| `numAutoSave` | Auto-save every N minutes | `OBP.AutoSaveEveryMinutes` |
| `radCloseWarn{All,Multiple,Exit,Never}` | Closing-connections warning mode | `Settings.ConfirmCloseConnection` (enum) |
| `chkSaveConnectionsAfterEveryEdit` | *(hidden, dead — `Visible=false`)* | unused |
| `btnResetResolver` | "Reset connections-file picker (Remember)" — **added at runtime, not in the .resx** | clears `OCP.ConnectionFilePath`/`ResolvedConnectionFilePath` etc. |

#### 2.4 Tabs & Panels
| Control | Label | Setting |
|---|---|---|
| `chkAlwaysShowPanelTabs` / `chkAlwaysShowConnectionTabs` | Always show panel / connection tabs | `OTP.AlwaysShowPanelTabs` / `AlwaysShowConnectionTabs` |
| `chkOpenNewTabRightOfSelected` | *(hidden, dead — `Visible=false`)* | unused |
| `chkShowLogonInfoOnTabs` / `chkShowProtocolOnTabs` / `chkShowFolderPathOnTabs` | Show logon info / protocol / folder path on tabs | `OTP.ShowLogonInfoOnTabs` / `ShowProtocolOnTabs` / `ShowFolderPathOnTabs` |
| `chkIdentifyQuickConnectTabs` | Identify Quick Connect tabs | `OTP.IdentifyQuickConnectTabs` |
| `chkDoubleClickClosesTab` | Double click closes tab | `OTP.DoubleClickOnTabClosesIt` |
| `chkAlwaysShowPanelSelectionDlg` | Always show panel selection dialog | `OTP.AlwaysShowPanelSelectionDlg` |
| `chkCreateEmptyPanelOnStart` + `txtBoxPanelName` | Create empty panel on startup + Panel name | `OTP.CreateEmptyPanelOnStartUp` / `StartUpPanelName` |
| `chkBindConnectionsAndConfigPanels` / `chkLockPanels` | Bind Connections/Config panels when auto-hidden / Lock panels | `OTP.BindConnectionsAndConfigPanels` / `LockPanels` |
| `chkDoNotRestoreOnRdpMinimize` | Do not restore on RDP minimize | `OTP.DoNotRestoreOnRdpMinimize` |
| `chkAutoClosePanelOnLastTabClose` | Auto close panel after last tab closes | `OTP.AutoClosePanelOnLastTabClose` |
| `chkMinimizePanelsOnConnect` | Auto-hide Connections/Config panels on connect | `OTP.MinimizePanelsOnConnect` |
| `chkKeepTabsOpenAfterDisconnect` | Keep tabs open after disconnecting | `OTP.KeepTabsOpenAfterDisconnect` |
| `chkUseCustomConnectionTabColor` + picker | Use custom connection tab color | `OTP.UseCustomConnectionTabColor` / `ConnectionTabColor` |
| `chkUseCustomConnectionTabFont` + picker | Use custom connection tab font | `OTP.UseCustomConnectionTabFont` / `ConnectionTabFontName`/`Size` |
| `nudSplitterSize` / `nudDockPadding` | Splitter size / Border size | `OTP.SplitterSize` / `DockPadding` |

#### 2.5 Notifications
Three group boxes, each with Debug/Information/Warning/Error checkboxes:
| Group | Controls | Setting prefix |
|---|---|---|
| Notifications (`groupBoxNotifications`) | `chkShow{Debug,Info,Warning,Error}InMC` (shown in panel); `chkSwitchToMC{Information,Warnings,Errors}` (auto-switch) | `ONP.NotificationPanelWriterWrite*Msgs`; `ONP.SwitchToMCOn*` |
| Logging (`groupBoxLogging`) | `chkLog{Debug,Info,Warning,Error}Msgs`; `chkLogToCurrentDir`, `textBoxLogPath`, `buttonSelectLogPath`, `buttonRestoreDefaultLogPath`, `buttonOpenLogFile` | `ONP.TextLogMessageWriterWrite*Msgs`; `ONP.LogToApplicationDirectory`/`LogFilePath` |
| Popups (`groupBoxPopups`) | `chkPopup{Debug,Info,Warning,Error}` | `ONP.PopupMessageWriterWrite*Msgs` |

Each group can be entirely disabled by registry policy (`AllowNotifications`/`AllowLogging`/`AllowPopups`).

#### 2.6 Credentials
| Control | Label | Setting |
|---|---|---|
| `radCredentialsNoInfo`/`Windows`/`Custom` | None / My current Windows creds / The following | `OCR.EmptyCredentials` ("noinfo"/"windows"/"custom") |
| `txtCredentialsUsername` / `txtCredentialsPassword` / `txtCredentialsDomain` | Username / Password / Domain | `OCR.DefaultUsername`/`DefaultPassword` (Rijndael-encrypted)/`DefaultDomain` |
| `txtCredentialsUserViaAPI` | (user via API default) | `OCR.UserViaAPIDefault` |

#### 2.7 SQL Server
| Control | Label | Setting |
|---|---|---|
| `chkUseSQLServer` | Use SQL Server | `ODB.UseSQLServer` |
| `txtSQLType` | SQL type dropdown (SQL Server/MySQL/MariaDB...) | `ODB.SQLServerType` |
| `txtSQLServer` / `txtSQLDatabaseName` | Hostname / Database | `ODB.SQLHost` / `SQLDatabaseName` |
| `txtSQLUsername` / `txtSQLPassword` | Username / Password | `ODB.SQLUser` / `SQLPass` (Rijndael-encrypted) |
| `chkSQLReadOnly` / `chkShowDatabasePickerOnStartup` | Read only / Show picker on startup | `ODB.SQLReadOnly` / `ShowDatabasePickerOnStartup` |
| `txtSQLAuthType` | Auth type dropdown (Windows/SQL Auth) | `ODB.SQLAuthType` |
| `btnTestConnection` + `lblTestConnectionResults` | Test Connection | offers to auto-create the DB if missing |

Advanced sub-features not itemized above: DCM setup radios (`DCMSetupRdBtnV`/`DCMSetupRdBtnC`) +
schema dropdown (`DCMSetupddschema`), saved-profiles list (`lstProfiles`, `btnLoadProfile`,
`btnSaveProfile`, `btnDeleteProfile`) — see `SqlServerPage.cs`.

#### 2.8 Updates
| Control | Label | Setting |
|---|---|---|
| `chkCheckForUpdatesOnStartup` + `cboUpdateCheckFrequency` | Check for updates on startup, Daily/Weekly/Monthly/Never | `OUP.CheckForUpdatesOnStartup` / `CheckForUpdatesFrequencyDays` |
| `btnUpdateCheckNow` | Check Now | opens `WindowType.Update` and runs a check |
| `chkUseProxyForAutomaticUpdates` + `txtProxyAddress`/`numProxyPort` | Use proxy, Address/Port | `OUP.UpdateUseProxy`, `UpdateProxyAddress`/`Port` |
| `chkUseProxyAuthentication` + `txtProxyUsername`/`txtProxyPassword` | Use proxy authentication, Username/Password | `OUP.UpdateProxyUseAuthentication`, `UpdateProxyAuthUser`/`Pass` |
| `btnTestProxy` | Test Proxy | |

Whole page disabled by `AllowCheckForUpdates` registry policy.

#### 2.9 Theme
`cboTheme` picker — selecting applies **live** (no restart). `listPalette` — color grid, shown
only for "extendable" themes; click a row → `ColorDialog`. `btnThemeNew`/`btnThemeDelete` —
create/delete a theme (delete disabled for base themes).

#### 2.10 Security
| Control | Label | Setting |
|---|---|---|
| `chkEncryptCompleteFile` | Encrypt complete connections file | `OSP.EncryptCompleteConnectionsFile` |
| `comboBoxEncryptionEngine` / `comboBoxBlockCipher` | Encryption engine / Block cipher mode | `OSP.EncryptionEngine`/`EncryptionBlockCipherMode` (enums) |
| `numberBoxKdfIterations` | KDF iterations | `OSP.EncryptionKeyDerivationIterations` |
| `btnTestSettings` | Test Settings | serializes the live tree, reports elapsed time |
| `txtPasswdGenerator`+`btnPasswdGenerator` | ad-hoc password encryptor → clipboard, auto-clears 30s | not persisted |

#### 2.11 Advanced
| Control | Label | Setting |
|---|---|---|
| `chkAutomaticReconnect` + `chkNoReconnect` | Automatic reconnect + no-reconnect toggle | `OAD.ReconnectOnDisconnect` / `NoReconnect` (inverted) |
| `chkLoadBalanceInfoUseUtf8` | RDP load-balance info uses UTF-8 | `OAD.RdpLoadBalanceInfoUseUtf8` |
| `numPuttyWaitTime` | Max PuTTY wait time (seconds) | `OAD.MaxPuttyWaitTime` |
| `chkUseCustomPuttyPath`+`txtCustomPuttyPath`+Browse | Custom PuTTY path | `OAD.UseCustomPuttyPath`/`CustomPuttyPath` |
| `btnLaunchPutty` | Launch PuTTY session config UI | enabled only if resolved PuTTY path exists |
| `numUVNCSCPort` | UltraVNC SC listening port | `OAD.UVNCSCPort` |
| `chkConnectionLogging` / `chkShowPortScan` / `chkShowPuttySavedSessions` | Enable connection logging / Show Port Scan / Show PuTTY saved sessions | `OAD.EnableConnectionLogging` / `ShowPortScan` / `ShowPuttySavedSessions` |

#### 2.12 Backup
| Control | Label | Setting |
|---|---|---|
| `numMaxBackups`/`rbBackupEnableDisable` | Max backups to keep (0 = disabled) | `OBP.BackupFileKeepCount` |
| `txtBackupNameFormat` | Backup file name format | `OBP.BackupFileNameFormat` |
| `txtConnectionsBackupPath`+Browse | Backup path | `OBP.BackupLocation` |
| `cbMakeBackupOnExit`/`OnEdit`/`OnSave` | Backup on exit/edit/save | `OBP.BackupConnectionsOn{Exit,Edit,Save}` |
| `cbBacupPageInOptionMenu` | Show this page for non-admin users | `OBP.cbBacupPageInOptionMenu` |

Extensive **RBAC/ACL controls** (`cbBackup*ACL` dropdowns: Hidden/Read-only/Write-allow, one per
field) are only visible when the active role is `AdminRole`; for a regular user role most rows
hide based on the saved ACL values — check `Properties.OptionsRbac.Default.ActiveRole` first.

#### 2.13 Config
| Control | Label | Setting |
|---|---|---|
| `txtConfigurationDirectory`+Browse | Configuration directory | `Settings.CustomConfigurationPath` (restart required; disabled in portable edition) |
| `txtExtAppsFilePath`+Browse | External apps file | `Settings.CustomExtAppsFilePath` |

## 3. Connection Tree Panel (`ConnectionTreeWindow`)

Docked panel, `AccessibleName="Connections"`. Layout top→bottom: toolbar, the tree
(`ConnectionTree`, an `ObjectListView`/`TreeListView` in `View.Details`, headers hidden), then a
search box row (dockable Bottom by default, or Top if "Place search bar above connection tree").

### 3.1 Toolbar (`msMain` in `ConnectionTreeWindow`)
| Button | AccessibleName | Behavior |
|---|---|---|
| `mMenAddConnection` | "Add Connection" | adds a connection under the current node |
| `mMenAddFolder` | "Add Folder" | adds a folder |
| `mMenViewExpandAllFolders` / `mMenViewCollapseAllFolders` | — | expand/collapse all |
| `mMenSort` | "Sort Connections" | **toggle**, not a dropdown — flips ascending/descending recursive sort, swaps its own icon |
| `mMenFavorites` | — | populates its own dropdown live with favorited connections, then opens it |

### 3.2 Search box
`pbSearch` (icon) — `txtSearch` (`MrngSearchBox`, `AccessibleName="Search Connections"`,
placeholder "Search") — `pbClearSearch` (× icon, hidden until there's text). Filters the tree
live via `TextChanged`.

### 3.3 Context menu (right-click a tree node) — `ConnectionContextMenu.cs`
Top-level order (▸ = submenu):
1. New Connection · New Entity · New Folder · New Root Folder
2. *sep* — Connect · **Connect (with options) ▸** [Connect with options..., Connect with
   credentials, Connect to console session, (Don't connect to console session — hidden), Connect
   in fullscreen, Connect without credentials, Choose panel before connecting, Connect using
   alternative hostname/IP, Connect in View-only mode] · Disconnect · Reconnect · Open in Browser
   · Type Username · Type Password · Type Clipboard Text
3. *sep* — External Applications ▸ (dynamic) · Transfer File (SSH) · Wake On LAN
4. *sep* — Duplicate · Copy · Paste · Create Link · Rename · Delete · Copy Hostname · Copy
   Username · Copy Password · Clear Cached RDP Credentials · **Inheritance ▸** [Apply
   inheritance to children, Apply default inheritance] · Properties · Configure Dynamic
   Source... · Refresh Dynamic Folder
5. *sep* — Open Connection File... · **Import ▸** [Import from File..., Text List..., Remote
   Desktop Manager..., Active Directory..., Putty..., mTTY..., SecureCRT..., Port Scan...,
   Guacamole...] · Export to File...
6. *sep* — **Sort ▸** [Ascending (A-Z), Descending (Z-A), By Tag (A-Z), By Tag (Z-A)] · Move up
   · Move down
7. *sep* — Options

**Enablement is node-type-dependent** (root / folder / PuTTY session / normal connection /
multi-selection each show a different enabled subset — see `ShowHideMenuItemsFor*` methods).
Right-click on empty tree space still opens the menu with nothing node-specific enabled. The
whole menu is heavily disabled when the store is read-only (`OptionsDBsPage.Default.SQLReadOnly`).

### 3.4 Config panel (property grid) — `ConfigWindow.cs` / `ConnectionInfoPropertyGrid`
Docked panel, `PropertySort.Categorized`. Toolbar (left→right, injected into the PropertyGrid's
own internal `ToolStrip` — see gotcha §5): **Properties** (default checked) · **Inheritance** ·
**Default Properties** · **Default Inheritance** · **Presets** · **Host Status** · **Icon**.
Right-click a grid row → **Reset**, *sep*, **Show Help Text** (checkable, toggles description pane).

Category order (`LocalizedCategoryAttribute` order values on `AbstractConnectionRecord.cs`
properties, lower = earlier): **Display → Connection → Protocol → RDP-Gateway → Appearance →
Redirect → Miscellaneous → Proxy** (Miscellaneous/Proxy tie at order=7, broken alphabetically).

## 4. Key Dialogs

### 4.1 First-run update prompt
Not a Form — a `CTaskDialog` from `FrmMain.PromptForUpdatesPreference()` on first startup (gated
on `OptionsUpdatesPage.Default.CheckForUpdatesAsked==false` and the `AllowCheckForUpdates*`
registry policies). Main instruction: "Automatic update settings". Three command-link buttons:
1. **"Use the recommended settings"** — enables auto-check, sets frequency to 14 days if unset.
2. **"Customize the settings now"** — opens Options directly on the **Updates** page.
3. **"Ask me again later"** — leaves the asked-flag false; dialog reappears next launch.

### 4.2 SSH File Transfer (`SSHTransferWindow`)
Docked panel, two group boxes. **Connection**: Host/Port (`txtHost`/`txtPort`, default port 22)
/ User/Password (`txtUser`/`txtPassword`) / Protocol (`radProtSFTP`/`radProtSCP`). **Files**:
Local file (`txtLocalFile`) / Remote file (`txtRemoteFile`) / **Browse** (local picker) /
**Transfer** (`btnTransfer`).

### 4.3 External Tools (`ExternalToolsWindow`)
Docked panel: a `ListView` of tools (columns: Display Name, Filename, Arguments, Working
Directory, Wait for exit, Try To Integrate, Run Elevated, Show On Toolbar, Category, Hidden)
with its own toolbar (**New**, **Delete**, **Launch**) and right-click menu (New/Delete/Launch
External Tool). Below: an **"External Tool Properties"** group box editing the selected row —
Display Name, Show on toolbar, Icon Path(+Browse), Filename(+Browse), Run Elevated, Arguments,
Try to integrate, Working Directory(+Browse), Options, Wait for exit, Variables button,
Authentication Type/Username/Password, Private Key File(+Browse), Passphrase.

### 4.4 About (`frmAbout`)
Shown docked in `pnlDock` (not modal). Title "Fructus temporum" (tagline), Version/License/
Copyright labels, links **Credits**/**Changelog**/**License**. "This Fork": **GitHub Page** /
**Releases** / **Changelog**. "Maintained by": "Geseidl IT Solutions" + **geseidl.ro/servicii-it**.

## 5. FlaUI Gotchas

- **Options pages' controls are largely invisible to UI Automation.** Content inside each
  `OptionsPage` `UserControl` (checkboxes, combos, textboxes) doesn't reliably enumerate in the
  UIA tree — don't expect `windows_snapshot`/`find` to locate them by name. Use screenshot +
  coordinate click, or edit the underlying `user.config`/`Properties.OptionsXPage` settings file
  directly and relaunch. Worth its own accessibility-defect issue.
- **Tree rename in-place editor grabs typed text.** After triggering Rename (F2/slow-click/
  context menu) the in-place `TextBox` captures all keystrokes — confirm focus before typing.
- **Property-grid in-place editors swallow typed text unpredictably.** `windows_type` right
  after clicking a grid row can land in the wrong field if the grid hasn't finished committing
  the previous edit — click, re-read the focused control, then type.
- **A modal `MessageBox` freezes the whole UIA provider.** Every `mcp__flaui__*` call then times
  out (`0x80131505`) even though the process is still responding. Fall back to Win32
  (`AppActivate` + `SendKeys`) to dismiss it — see parent `CLAUDE.md` FlaUI section. `{ESC}` does
  nothing on Yes/No boxes (no Cancel button).
- **`windows_click` on a tree row uses the Invoke pattern, which ADDS to the selection** instead
  of replacing it — two selected rows blanks the Config property grid (correct multi-select
  behavior, easy to misreport as a bug). Click the row's text child for a clean single selection.
- **Options is a docked panel, not a dialog window** — don't search for a top-level window
  titled "Options"; it lives inside the main window's dock area (§2), OK/Cancel/Apply included.
- **The Connections top-level menu (§1) is fully dynamic** — rebuilt from the live tree on every
  open, so there's no fixed item list to script against by index.
- **`GoogleDrivePage.cs`** exists on disk but isn't reachable from the Options dialog (§2) —
  no "Google Drive" tab in the running app.
- **Ctrl+F for "Find in Session"** is a `ShortcutKeyDisplayString` only, not a bound
  `ShortcutKeys` — if global Ctrl+F doesn't trigger search, use the Tools menu item directly.
- **Ctrl+N does not open "New Connection File..."** — see the File-menu shortcut bug (§1).

## 6. File Map

| UI area | Source file(s) |
|---|---|
| Main window shell + menu bar host | `mRemoteNG/UI/Forms/frmMain.cs`, `frmMain.Designer.cs` |
| File/Sessions/View/Connections/Tools/Help menus | `mRemoteNG/UI/Menu/msMain/{FileMenu,SessionsMenu,ViewMenu,ConnectionsMenu,ToolsMenu,HelpMenu}.cs` |
| Window registry (`WindowType` → concrete window) | `mRemoteNG/App/AppWindows.cs`, `mRemoteNG/UI/WindowType.cs` |
| Options dialog host (docked panel) | `mRemoteNG/UI/Window/OptionsWindow.cs` |
| Options dialog content (page list, OK/Cancel/Apply) | `mRemoteNG/UI/Forms/frmOptions.cs`, `frmOptions.Designer.cs` |
| Options pages | `mRemoteNG/UI/Forms/OptionsPages/*.cs` (+ matching `.Designer.cs`/`.resx`) |
| Connection tree panel (toolbar, search box) | `mRemoteNG/UI/Window/ConnectionTreeWindow.cs`, `.Designer.cs` |
| Connection tree control | `mRemoteNG/UI/Controls/ConnectionTree/ConnectionTree.cs` |
| Connection tree right-click context menu | `mRemoteNG/UI/Controls/ConnectionContextMenu.cs` |
| Config property-grid panel | `mRemoteNG/UI/Window/ConfigWindow.cs` |
| Property grid control + custom editors | `mRemoteNG/UI/Controls/ConnectionInfoPropertyGrid/*.cs` |
| Connection property definitions/categories | `mRemoteNG/Connection/AbstractConnectionRecord.cs`, `ConnectionInfo.cs` |
| Session tab container (open connections) | `mRemoteNG/UI/Window/ConnectionWindow.cs`, `.Designer.cs` |
| SSH File Transfer dialog | `mRemoteNG/UI/Window/SSHTransferWindow.cs` |
| External Tools window | `mRemoteNG/UI/Window/ExternalToolsWindow.cs`, `.Designer.cs` |
| About dialog | `mRemoteNG/UI/Forms/FrmAbout.cs`, `FrmAbout.Designer.cs` |
| First-run update prompt | `mRemoteNG/UI/Forms/frmMain.cs` (`PromptForUpdatesPreference`) |
| Quick Import dialog | `mRemoteNG/UI/Forms/FrmQuickImport.cs` |
| English UI strings (source of truth for labels) | `mRemoteNG/Language/Language.resx` |

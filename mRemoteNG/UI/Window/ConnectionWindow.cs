using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Themes;
using mRemoteNG.Tools;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Tabs;
using mRemoteNG.UI.TaskDialog;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using mRemoteNG.Security;

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class ConnectionWindow : BaseWindow
    {
        private VisualStudioToolStripExtender? _vsToolStripExtender;
        private readonly ToolStripRenderer _toolStripProfessionalRenderer = new ToolStripProfessionalRenderer();
        private readonly ToolStripMenuItem _cmenTabMoveToPanel = new();
        private readonly ToolStripMenuItem _cmenTabIncludeInMultiSsh = new();
        private readonly ToolStripMenuItem _cmenTabExcludeFromMultiSsh = new();
        private readonly ToolStripSeparator _cmenTabMultiSshSeparator = new();

        #region Public Methods

        public ConnectionWindow(DockContent panel, string formText = "")
        {
            if (formText == "")
            {
                formText = Language.NewPanel;
            }

            WindowType = WindowType.Connection;
            DockPnl = panel;
            InitializeComponent();
            SetEventHandlers();
            // ReSharper disable once VirtualMemberCallInConstructor
            Text = formText;
            TabText = formText;
            connDock.DocumentStyle = DocumentStyle.DockingWindow;
            connDock.ShowDocumentIcon = true;

            connDock.ActiveContentChanged += ConnDockOnActiveContentChanged;
            InitializeConnectionTabDragDropTargets();
        }

        private InterfaceControl? GetInterfaceControl()
        {
            return InterfaceControl.FindInterfaceControl(connDock);
        }

        private ConnectionTab? GetSelectedTab()
        {
            return connDock.ActiveDocument as ConnectionTab ?? GetInterfaceControl()?.Parent as ConnectionTab;
        }

        private static ConnectionInfo? GetConnectionInfoForTab(ConnectionTab? connectionTab)
        {
            if (connectionTab == null) return null;

            if (connectionTab.Tag is InterfaceControl interfaceControl)
                return interfaceControl.Info;

            if (connectionTab.Tag is ConnectionInfo connectionInfo)
                return connectionInfo;

            return connectionTab.TrackedConnectionInfo;
        }

        private static ConnectionInfo? GetMultiSshConnectionInfoForTab(ConnectionTab? connectionTab)
        {
            if (connectionTab?.Tag is InterfaceControl interfaceControl)
                return interfaceControl.OriginalInfo ?? interfaceControl.Info;

            return GetConnectionInfoForTab(connectionTab);
        }

        private ConnectionTab? FindReusableClosedTab(ConnectionInfo connectionInfo)
        {
            foreach (IDockContent dockContent in connDock.DocumentsToArray())
            {
                if (dockContent is not ConnectionTab connectionTab) continue;
                if (InterfaceControl.FindInterfaceControl(connectionTab) != null) continue;

                if (GetConnectionInfoForTab(connectionTab) == connectionInfo)
                    return connectionTab;
            }

            return null;
        }

        private void SetEventHandlers()
        {
            SetFormEventHandlers();
            SetContextMenuEventHandlers();
        }

        private void SetFormEventHandlers()
        {
            Load += Connection_Load;
            DockStateChanged += Connection_DockStateChanged;
            FormClosing += Connection_FormClosing;
        }

        private void SetContextMenuEventHandlers()
        {
            InitializeMoveToPanelContextMenuItems();
            InitializeMultiSshContextMenuItems();

            // event handler to adjust the items within the context menu
            cmenTab.Opening += ShowHideMenuButtons;

            // event handlers for all context menu items...
            cmenTabFullscreen.Click += (sender, args) => ToggleFullscreen();
            cmenTabSmartSize.Click += (sender, args) => ToggleSmartSize();
            cmenTabViewOnly.Click += (sender, args) => ToggleViewOnly();
            cmenTabStartChat.Click += (sender, args) => StartChat();
            cmenTabTransferFile.Click += (sender, args) => TransferFile();
            cmenTabRefreshScreen.Click += (sender, args) => RefreshScreen();
            cmenTabSendSpecialKeysCtrlAltDel.Click += (sender, args) => SendSpecialKeys(ProtocolVNC.SpecialKeys.CtrlAltDel);
            cmenTabSendSpecialKeysCtrlEsc.Click += (sender, args) => SendSpecialKeys(ProtocolVNC.SpecialKeys.CtrlEsc);
            cmenTabRenameTab.Click += (sender, args) => RenameTab();
            cmenTabDuplicateTab.Click += (sender, args) => DuplicateTab();
            cmenTabReconnect.Click += (sender, args) => Reconnect();
            cmenTabDisconnect.Click += (sender, args) => CloseTabMenu();
            cmenTabDisconnectOthers.Click += (sender, args) => CloseOtherTabs();
            cmenTabDisconnectOthersRight.Click += (sender, args) => CloseOtherTabsToTheRight();
            cmenTabPuttySettings.Click += (sender, args) => ShowPuttySettingsDialog();
            _cmenTabIncludeInMultiSsh.Click += (sender, args) => ToggleMultiSshInclude();
            _cmenTabExcludeFromMultiSsh.Click += (sender, args) => ToggleMultiSshExclude();
            GotFocus += ConnectionWindow_GotFocus;
        }

        private void InitializeMoveToPanelContextMenuItems()
        {
            _cmenTabMoveToPanel.Name = "cmenTabMoveToPanel";
            _cmenTabMoveToPanel.Image = Properties.Resources.Panel_16x;
            _cmenTabMoveToPanel.DropDownOpening += MoveToPanelMenu_DropDownOpening;

            int insertIndex = cmenTab.Items.IndexOf(cmenTabSep1);
            if (insertIndex < 0)
                insertIndex = cmenTab.Items.Count;

            cmenTab.Items.Insert(insertIndex, _cmenTabMoveToPanel);
            _cmenTabMoveToPanel.Visible = false;
        }

        private void InitializeMultiSshContextMenuItems()
        {
            _cmenTabIncludeInMultiSsh.Name = "cmenTabIncludeInMultiSsh";
            _cmenTabExcludeFromMultiSsh.Name = "cmenTabExcludeFromMultiSsh";
            _cmenTabMultiSshSeparator.Name = "cmenTabMultiSshSeparator";

            int puttySettingsIndex = cmenTab.Items.IndexOf(cmenTabPuttySettings);
            if (puttySettingsIndex < 0)
                puttySettingsIndex = cmenTab.Items.Count;

            cmenTab.Items.Insert(puttySettingsIndex, _cmenTabMultiSshSeparator);
            cmenTab.Items.Insert(puttySettingsIndex + 1, _cmenTabIncludeInMultiSsh);
            cmenTab.Items.Insert(puttySettingsIndex + 2, _cmenTabExcludeFromMultiSsh);

            _cmenTabMultiSshSeparator.Visible = false;
            _cmenTabIncludeInMultiSsh.Visible = false;
            _cmenTabExcludeFromMultiSsh.Visible = false;
        }

        private void InitializeConnectionTabDragDropTargets()
        {
            connDock.AllowDrop = true;
            connDock.DragEnter += ConnectionTabDragEnter;
            connDock.DragOver += ConnectionTabDragOver;
            connDock.DragDrop += ConnectionTabDragDrop;
            connDock.ControlAdded += ConnDock_ControlAdded;

            AttachConnectionTabDropTarget(connDock);
        }

        private void ConnDock_ControlAdded(object? sender, ControlEventArgs e)
        {
            AttachConnectionTabDropTarget(e.Control);
        }

        private void AttachConnectionTabDropTarget(Control control)
        {
            if (control is DockPaneStripNG dockPaneStrip)
            {
                dockPaneStrip.AllowDrop = true;
                dockPaneStrip.DragEnter -= ConnectionTabDragEnter;
                dockPaneStrip.DragOver -= ConnectionTabDragOver;
                dockPaneStrip.DragDrop -= ConnectionTabDragDrop;
                dockPaneStrip.DragEnter += ConnectionTabDragEnter;
                dockPaneStrip.DragOver += ConnectionTabDragOver;
                dockPaneStrip.DragDrop += ConnectionTabDragDrop;
            }

            foreach (Control child in control.Controls)
            {
                AttachConnectionTabDropTarget(child);
            }
        }

        private void ConnectionTabDragEnter(object? sender, DragEventArgs e)
        {
            if (CanDropConnectionTab(e.Data, out _))
                e.Effect = DragDropEffects.Move;
            else if (CanDropConnectionInfo(e.Data, out _))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ConnectionTabDragOver(object? sender, DragEventArgs e)
        {
            if (CanDropConnectionTab(e.Data, out _))
                e.Effect = DragDropEffects.Move;
            else if (CanDropConnectionInfo(e.Data, out _))
                e.Effect = DragDropEffects.Copy;
            else
                e.Effect = DragDropEffects.None;
        }

        private void ConnectionTabDragDrop(object? sender, DragEventArgs e)
        {
            if (CanDropConnectionTab(e.Data, out ConnectionTab? draggedTab) && draggedTab != null)
            {
                e.Effect = MoveConnectionTabToPanel(draggedTab, this)
                    ? DragDropEffects.Move
                    : DragDropEffects.None;
                return;
            }

            if (CanDropConnectionInfo(e.Data, out List<ConnectionInfo> connectionInfos))
            {
                e.Effect = DragDropEffects.Copy;
                foreach (var info in connectionInfos)
                {
                    Runtime.ConnectionInitiator.OpenConnection(info, ConnectionInfo.Force.None, this);
                }
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private bool CanDropConnectionTab(IDataObject? dataObject, out ConnectionTab? draggedTab)
        {
            draggedTab = null;
            if (!TryGetDraggedConnectionTab(dataObject, out draggedTab) || draggedTab == null)
                return false;

            ConnectionWindow? sourcePanel = GetOwningConnectionWindow(draggedTab);
            return sourcePanel != null && !ReferenceEquals(sourcePanel, this);
        }

        private bool CanDropConnectionInfo(IDataObject? dataObject, out List<ConnectionInfo> connectionInfos)
        {
            return TryGetDraggedConnectionInfos(dataObject, out connectionInfos);
        }

        private static bool TryGetDraggedConnectionInfos(IDataObject? dataObject, out List<ConnectionInfo> connectionInfos)
        {
            connectionInfos = new List<ConnectionInfo>();
            if (dataObject == null) return false;

            if (dataObject.GetDataPresent("System.Collections.ArrayList"))
            {
                if (dataObject.GetData("System.Collections.ArrayList") is System.Collections.ArrayList list)
                {
                    foreach (var item in list)
                    {
                        if (item is ConnectionInfo ci)
                        {
                            connectionInfos.Add(ci);
                        }
                    }
                }
            }

            if (connectionInfos.Count == 0 && dataObject.GetDataPresent(typeof(ConnectionInfo)))
            {
                if (dataObject.GetData(typeof(ConnectionInfo)) is ConnectionInfo ci)
                {
                    connectionInfos.Add(ci);
                }
            }

            return connectionInfos.Any();
        }

        private static bool TryGetDraggedConnectionTab(IDataObject? dataObject, out ConnectionTab? draggedTab)
        {
            draggedTab = null;
            if (dataObject == null || !dataObject.GetDataPresent(typeof(ConnectionTab)))
                return false;

            draggedTab = dataObject.GetData(typeof(ConnectionTab)) as ConnectionTab;
            return draggedTab is { IsDisposed: false };
        }

        private void MoveToPanelMenu_DropDownOpening(object? sender, EventArgs e)
        {
            for (int i = _cmenTabMoveToPanel.DropDownItems.Count - 1; i >= 0; i--)
                _cmenTabMoveToPanel.DropDownItems[i].Dispose();

            _cmenTabMoveToPanel.DropDownItems.Clear();

            ConnectionTab? selectedTab = GetSelectedTab();
            if (selectedTab == null)
            {
                _cmenTabMoveToPanel.Enabled = false;
                return;
            }

            ConnectionWindow[] targetPanels = GetOtherConnectionPanels().ToArray();
            if (targetPanels.Length == 0)
            {
                _cmenTabMoveToPanel.Enabled = false;
                return;
            }

            _cmenTabMoveToPanel.Enabled = true;
            foreach (ConnectionWindow panel in targetPanels)
            {
                ToolStripMenuItem panelItem = new(GetPanelName(panel))
                {
                    Tag = panel
                };

                panelItem.Click += MoveToPanelMenuItem_Click;
                _cmenTabMoveToPanel.DropDownItems.Add(panelItem);
            }
        }

        private void MoveToPanelMenuItem_Click(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: ConnectionWindow targetPanel })
                return;

            MoveSelectedTabToPanel(targetPanel);
        }

        private void MoveSelectedTabToPanel(ConnectionWindow targetPanel)
        {
            ConnectionTab? selectedTab = GetSelectedTab();
            if (selectedTab == null)
                return;

            MoveConnectionTabToPanel(selectedTab, targetPanel);
        }

        private bool MoveConnectionTabToPanel(ConnectionTab connectionTab, ConnectionWindow targetPanel)
        {
            if (targetPanel.IsDisposed)
                return false;

            ConnectionWindow? sourcePanel = GetOwningConnectionWindow(connectionTab);
            if (sourcePanel == null || ReferenceEquals(sourcePanel, targetPanel))
                return false;

            string targetPanelName = GetPanelName(targetPanel);
            UpdateConnectionPanelAssignment(connectionTab, targetPanelName);
            connectionTab.TabPageContextMenuStrip = targetPanel.cmenTab;

            try
            {
                if (targetPanel.DockState == DockState.Unknown || targetPanel.DockState == DockState.Hidden || !targetPanel.Visible)
                    targetPanel.Show(FrmMain.Default.pnlDock, DockState.Document);
                else
                    targetPanel.Show(FrmMain.Default.pnlDock);

                connectionTab.Show(targetPanel.connDock, DockState.Document);
                connectionTab.DockHandler.Activate();
                connectionTab.Focus();
                TabHelper.Instance.CurrentPanel = targetPanel;

                ConnectionInfo? movedConnectionInfo = GetConnectionInfoForTab(connectionTab);
                if (movedConnectionInfo != null)
                    FrmMain.Default.SelectedConnection = movedConnectionInfo;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("MoveConnectionTabToPanel (UI.Window.ConnectionWindow) failed", ex);
                return false;
            }

            sourcePanel.ClosePanelIfEmpty();
            return true;
        }

        private IEnumerable<ConnectionWindow> GetOtherConnectionPanels()
        {
            if (Runtime.WindowList == null)
                return Enumerable.Empty<ConnectionWindow>();

            return Runtime.WindowList
                .OfType<ConnectionWindow>()
                .Where(window => !window.IsDisposed && !ReferenceEquals(window, this))
                .OrderBy(window => window.Text, StringComparer.CurrentCultureIgnoreCase);
        }

        private static string GetPanelName(ConnectionWindow panel)
        {
            return panel.Text.Replace("&&", "&");
        }

        private static ConnectionWindow? GetOwningConnectionWindow(ConnectionTab connectionTab)
        {
            if (connectionTab.DockPanel?.FindForm() is ConnectionWindow dockPanelOwner)
                return dockPanelOwner;

            Control? current = connectionTab.Parent;
            while (current != null && current is not ConnectionWindow)
            {
                current = current.Parent;
            }

            return current as ConnectionWindow;
        }

        private static void UpdateConnectionPanelAssignment(ConnectionTab connectionTab, string panelName)
        {
            if (connectionTab.Tag is InterfaceControl interfaceControl)
            {
                interfaceControl.Info.Panel = panelName;
                if (interfaceControl.OriginalInfo != null)
                    interfaceControl.OriginalInfo.Panel = panelName;
            }

            if (connectionTab.Tag is ConnectionInfo taggedConnectionInfo)
                taggedConnectionInfo.Panel = panelName;

            if (connectionTab.TrackedConnectionInfo != null)
                connectionTab.TrackedConnectionInfo.Panel = panelName;
        }

        private void ConnectionWindow_GotFocus(object sender, EventArgs e)
        {
            TabHelper.Instance.CurrentPanel = this;
        }

        private sealed class FocusSnapshot
        {
            public IDockContent? ActiveMainDocument { get; init; }
            public IDockContent? ActiveConnectionDocument { get; init; }
            public Control? FocusedControl { get; init; }
        }

        private FocusSnapshot CaptureFocusSnapshot()
        {
            return new FocusSnapshot
            {
                ActiveMainDocument = FrmMain.Default.pnlDock.ActiveDocument,
                ActiveConnectionDocument = connDock.ActiveContent,
                FocusedControl = GetFocusedControl(Form.ActiveForm as ContainerControl)
            };
        }

        private static Control? GetFocusedControl(ContainerControl? containerControl)
        {
            Control? activeControl = containerControl?.ActiveControl;
            while (activeControl is ContainerControl nestedContainer && nestedContainer.ActiveControl != null)
            {
                activeControl = nestedContainer.ActiveControl;
            }

            return activeControl;
        }

        private void RestoreFocusSnapshot(FocusSnapshot? snapshot, ConnectionTab openedTab)
        {
            if (snapshot == null) return;

            try
            {
                if (ReferenceEquals(snapshot.ActiveMainDocument, this))
                {
                    if (snapshot.ActiveConnectionDocument != null &&
                        !ReferenceEquals(snapshot.ActiveConnectionDocument, openedTab))
                    {
                        snapshot.ActiveConnectionDocument.DockHandler.Activate();
                    }
                }
                else if (snapshot.ActiveMainDocument != null)
                {
                    snapshot.ActiveMainDocument.DockHandler.Activate();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                if (snapshot.FocusedControl is { IsDisposed: false } && snapshot.FocusedControl.CanFocus)
                {
                    snapshot.FocusedControl.Focus();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        public ConnectionTab? AddConnectionTab(ConnectionInfo connectionInfo, bool switchToConnection = true)
        {
            try
            {
                FocusSnapshot? focusSnapshot = switchToConnection ? null : CaptureFocusSnapshot();

                //Set the connection text based on name and preferences
                string titleText;
                if (Properties.OptionsTabsPanelsPage.Default.ShowProtocolOnTabs)
                    titleText = connectionInfo.Protocol + @": ";
                else
                    titleText = "";

                titleText += ConnectionNameFormatter.FormatName(connectionInfo);

                if (Properties.OptionsTabsPanelsPage.Default.ShowFolderPathOnTabs)
                {
                    var folderPath = GetFolderPath(connectionInfo);
                    if (!string.IsNullOrEmpty(folderPath))
                        titleText += $" \u2014 {folderPath}";
                }

                if (Properties.OptionsTabsPanelsPage.Default.ShowLogonInfoOnTabs)
                {
                    titleText += @" (";
                    if (connectionInfo.Domain != "")
                        titleText += connectionInfo.Domain;

                    if (connectionInfo.Username != "")
                    {
                        if (connectionInfo.Domain != "")
                            titleText += @"\";
                        titleText += connectionInfo.Username;
                    }

                    titleText += @")";
                }

                titleText = titleText.Replace("&", "&&");

                ConnectionTab conTab = new()
                {
                    Tag = connectionInfo,
                    DockAreas = DockAreas.Document | DockAreas.Float,
                    Icon = ConnectionIcon.FromString(connectionInfo.Icon),
                    TabText = titleText,
                    TabPageContextMenuStrip = cmenTab
                };

                conTab.TrackConnection(connectionInfo);
                conTab.HideClosedState();

                //if (Settings.Default.AlwaysShowConnectionTabs == false)
                // TODO: See if we can make this work with DPS...
                //TabController.HideTabsMode = TabControl.HideTabsModes.HideAlways;

                // Ensure the ConnectionWindow is visible before adding the tab
                // This prevents visibility issues when the window was created but not yet shown
                // Check DockState instead of Visible to properly detect if window is shown in DockPanel
                if (DockState == DockState.Unknown || DockState == DockState.Hidden || !Visible)
                {
                    Show(FrmMain.Default.pnlDock, DockState.Document);
                }

                //Show the tab
                conTab.Show(connDock, DockState.Document);
                if (switchToConnection)
                {
                    conTab.Focus();
                }
                else
                {
                    RestoreFocusSnapshot(focusSnapshot, conTab);
                }

                return conTab;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("AddConnectionTab (UI.Window.ConnectionWindow) failed", ex);
            }

            return null;
        }

        public ConnectionTab? GetOrAddConnectionTab(ConnectionInfo connectionInfo, bool switchToConnection = true)
        {
            ConnectionTab? reusableTab = FindReusableClosedTab(connectionInfo);
            if (reusableTab != null)
            {
                reusableTab.TrackConnection(connectionInfo);
                reusableTab.HideClosedState();

                if (switchToConnection)
                {
                    reusableTab.DockHandler.Activate();
                    reusableTab.Focus();
                }

                return reusableTab;
            }

            return AddConnectionTab(connectionInfo, switchToConnection);
        }

        private static string GetFolderPath(ConnectionInfo connectionInfo)
        {
            var parts = new List<string>();
            var current = connectionInfo.Parent;
            while (current?.Parent != null)
            {
                parts.Insert(0, current.Name);
                current = current.Parent;
            }

            return string.Join(" / ", parts);
        }

        #endregion

        public void ReconnectAll(IConnectionInitiator initiator)
        {
            List<InterfaceControl> controlList = new();
            try
            {
                foreach (IDockContent dockContent in connDock.DocumentsToArray())
                {
                    if (dockContent is not ConnectionTab tab) continue;
                    if (tab.Tag is InterfaceControl ic)
                        controlList.Add(ic);
                }

                foreach (InterfaceControl iControl in controlList)
                {
                    iControl.Protocol.Close();
                    initiator.OpenConnection(iControl.Info, ConnectionInfo.Force.DoNotJump, this);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("reconnectAll (UI.Window.ConnectionWindow) failed", ex);
            }

            controlList.Clear();
        }

        #region Form

        private void Connection_Load(object sender, EventArgs e)
        {
            ApplyTheme();
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyLanguage();
        }

        private new void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ThemingActive)
            {
                connDock.Theme = ThemeManager.getInstance().DefaultTheme.Theme;
                return;
            }

            base.ApplyTheme();
            try
            {
                connDock.Theme = ThemeManager.getInstance().ActiveTheme.Theme;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("UI.Window.ConnectionWindow.ApplyTheme() failed", ex);
            }

            _vsToolStripExtender = new VisualStudioToolStripExtender(components)
            {
                DefaultRenderer = _toolStripProfessionalRenderer
            };
            _vsToolStripExtender.SetStyle(cmenTab, ThemeManager.getInstance().ActiveTheme.Version, ThemeManager.getInstance().ActiveTheme.Theme);

            if (!ThemeManager.getInstance().ActiveAndExtended) return;
            connDock.DockBackColor = ThemeManager.getInstance().ActiveTheme.ExtendedPalette?.getColor("Tab_Item_Background") ?? connDock.DockBackColor;
        }

        private bool _documentHandlersAdded;
        private bool _floatHandlersAdded;
        private bool _emptyPanelCloseQueued;

        private void Connection_DockStateChanged(object sender, EventArgs e)
        {
            switch (DockState)
            {
                case DockState.Float:
                    {
                        if (_documentHandlersAdded)
                        {
                            FrmMain.Default.ResizeBegin -= Connection_ResizeBegin;
                            FrmMain.Default.ResizeEnd -= Connection_ResizeEnd;
                            _documentHandlersAdded = false;
                        }

                        DockHandler.FloatPane.FloatWindow.ResizeBegin += Connection_ResizeBegin;
                        DockHandler.FloatPane.FloatWindow.ResizeEnd += Connection_ResizeEnd;
                        _floatHandlersAdded = true;
                        break;
                    }
                case DockState.Document:
                    {
                        if (_floatHandlersAdded)
                        {
                            DockHandler.FloatPane.FloatWindow.ResizeBegin -= Connection_ResizeBegin;
                            DockHandler.FloatPane.FloatWindow.ResizeEnd -= Connection_ResizeEnd;
                            _floatHandlersAdded = false;
                        }

                        FrmMain.Default.ResizeBegin += Connection_ResizeBegin;
                        FrmMain.Default.ResizeEnd += Connection_ResizeEnd;
                        _documentHandlersAdded = true;
                        break;
                    }
            }
        }

        private void ApplyLanguage()
        {
            _cmenTabMoveToPanel.Text = Language.SendTo;
            cmenTabFullscreen.Text = Language.Fullscreen;
            cmenTabSmartSize.Text = Language.SmartSize;
            cmenTabViewOnly.Text = Language.ViewOnly;
            cmenTabStartChat.Text = Language.StartChat;
            cmenTabTransferFile.Text = Language.TransferFile;
            cmenTabRefreshScreen.Text = Language.RefreshScreen;
            cmenTabSendSpecialKeys.Text = Language.SendSpecialKeys;
            cmenTabSendSpecialKeysCtrlAltDel.Text = Language.CtrlAltDel;
            cmenTabSendSpecialKeysCtrlEsc.Text = Language.CtrlEsc;
            cmenTabExternalApps.Text = Language._Tools;
            cmenTabRenameTab.Text = Language.RenameTab;
            cmenTabDuplicateTab.Text = Language.DuplicateTab;
            cmenTabReconnect.Text = Language.Reconnect;
            cmenTabDisconnect.Text = Language.Disconnect;
            cmenTabDisconnectOthers.Text = Language.DisconnectOthers;
            cmenTabDisconnectOthersRight.Text = Language.DisconnectOthersRight;
            cmenTabPuttySettings.Text = Language.PuttySettings;
            _cmenTabIncludeInMultiSsh.Text = "Include in Multi SSH";
            _cmenTabExcludeFromMultiSsh.Text = "Exclude from Multi SSH";
        }

        private void Connection_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!FrmMain.Default.IsClosing &&
                (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.All & connDock.Documents.Any() ||
                 Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.Multiple &
                 connDock.Documents.Count() > 1))
            {
                DialogResult result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName, string.Format(Language.ConfirmCloseConnectionPanelMainInstruction, Text), "", "", "", Language.CheckboxDoNotShowThisMessageAgain, ETaskDialogButtons.YesNo, ESysIcons.Question, ESysIcons.Question);
                if (CTaskDialog.VerificationChecked)
                {
                    Settings.Default.ConfirmCloseConnection = (int)ConfirmCloseEnum.Never;
                    Settings.Default.Save();
                }

                if (result == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }

            try
            {
                foreach (IDockContent dockContent in connDock.Documents.ToArray())
                {
                    ConnectionTab tabP = (ConnectionTab)dockContent;
                    if (tabP.Tag == null) continue;
                    tabP.silentClose = true;
                    tabP.Close();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("UI.Window.Connection.Connection_FormClosing() failed", ex);
            }
        }

        public new event EventHandler? ResizeBegin;

        private void Connection_ResizeBegin(object sender, EventArgs e)
        {
            ResizeBegin?.Invoke(this, e);
        }

        public new event EventHandler? ResizeEnd;

        private void Connection_ResizeEnd(object sender, EventArgs e)
        {
            ResizeEnd?.Invoke(sender, e);
        }

        internal void NavigateToNextTab()
        {
            try
            {
                var documents = connDock.DocumentsToArray();
                if (documents.Length <= 1) return;

                var currentIndex = Array.IndexOf(documents, connDock.ActiveContent);
                if (currentIndex == -1)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, "NavigateToNextTab: ActiveContent not found in documents array");
                    return;
                }

                var nextIndex = (currentIndex + 1) % documents.Length;
                documents[nextIndex].DockHandler.Activate();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("NavigateToNextTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        internal void NavigateToPreviousTab()
        {
            try
            {
                var documents = connDock.DocumentsToArray();
                if (documents.Length <= 1) return;

                var currentIndex = Array.IndexOf(documents, connDock.ActiveContent);
                if (currentIndex == -1)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, "NavigateToPreviousTab: ActiveContent not found in documents array");
                    return;
                }

                var previousIndex = currentIndex - 1;
                if (previousIndex < 0)
                    previousIndex = documents.Length - 1;
                documents[previousIndex].DockHandler.Activate();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("NavigateToPreviousTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        #endregion

        #region Events

        private void ConnDockOnActiveContentChanged(object sender, EventArgs e)
        {
            ConnectionTab? selectedTab = GetSelectedTab();
            ConnectionInfo? selectedConnectionInfo = GetConnectionInfoForTab(selectedTab);
            if (selectedConnectionInfo == null) return;
            FrmMain.Default.SelectedConnection = selectedConnectionInfo;
        }

        private void ClosePanelIfEmpty()
        {
            if (_emptyPanelCloseQueued || IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            if (FrmMain.Default?.IsClosing == true)
            {
                return;
            }

            if (connDock.Documents.Any())
            {
                return;
            }

            _emptyPanelCloseQueued = true;
            try
            {
                BeginInvoke((MethodInvoker)ClosePanelIfEmptyOnUiTick);
            }
            catch (ObjectDisposedException)
            {
                _emptyPanelCloseQueued = false;
            }
            catch (InvalidOperationException)
            {
                _emptyPanelCloseQueued = false;
            }
        }

        private void ClosePanelIfEmptyOnUiTick()
        {
            _emptyPanelCloseQueued = false;

            if (IsDisposed || Disposing || !IsHandleCreated)
            {
                return;
            }

            if (FrmMain.Default?.IsClosing == true)
            {
                return;
            }

            if (connDock.Documents.Any())
            {
                return;
            }

            try
            {
                Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ClosePanelIfEmptyOnUiTick (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        #endregion

        #region Tab Menu

        private void ShowHideMenuButtons(object sender, CancelEventArgs e)
        {
            try
            {
                ConnectionTab? selectedTab = GetSelectedTab();
                bool canMoveToAnotherPanel = selectedTab != null && GetOtherConnectionPanels().Any();
                _cmenTabMoveToPanel.Visible = canMoveToAnotherPanel;
                _cmenTabMoveToPanel.Enabled = canMoveToAnotherPanel;

                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null)
                {
                    cmenTabViewOnly.Visible = false;
                    cmenTabFullscreen.Visible = false;
                    cmenTabSmartSize.Visible = false;
                    cmenTabSendSpecialKeys.Visible = false;
                    cmenTabStartChat.Visible = false;
                    cmenTabRefreshScreen.Visible = false;
                    cmenTabTransferFile.Visible = false;
                    cmenTabPuttySettings.Visible = false;
                    cmenTabExternalApps.Visible = false;
                    _cmenTabMultiSshSeparator.Visible = false;
                    _cmenTabIncludeInMultiSsh.Visible = false;
                    _cmenTabExcludeFromMultiSsh.Visible = false;
                    return;
                }

                cmenTabExternalApps.Visible = true;

                if (interfaceControl.Protocol is ISupportsViewOnly viewOnly)
                {
                    cmenTabViewOnly.Visible = true;
                    cmenTabViewOnly.Checked = viewOnly.ViewOnly;
                }
                else
                {
                    cmenTabViewOnly.Visible = false;
                }

                if (interfaceControl.Info.Protocol == ProtocolType.RDP)
                {
                    RdpProtocol rdp = (RdpProtocol)interfaceControl.Protocol;
                    cmenTabFullscreen.Visible = true;
                    cmenTabFullscreen.Enabled = !rdp.RedirectKeysEnabled || !rdp.Fullscreen;
                    cmenTabFullscreen.Checked = rdp.Fullscreen;
                    cmenTabSmartSize.Visible = true;
                    cmenTabSmartSize.Checked = rdp.SmartSize;
                }
                else
                {
                    cmenTabFullscreen.Visible = false;
                    cmenTabFullscreen.Enabled = true;
                    cmenTabSmartSize.Visible = false;
                }

                if (interfaceControl.Info.Protocol == ProtocolType.VNC)
                {
                    cmenTabSendSpecialKeys.Visible = true;
                    cmenTabSmartSize.Visible = true;
                    cmenTabStartChat.Visible = false;
                    cmenTabRefreshScreen.Visible = true;
                    cmenTabTransferFile.Visible = false;
                }
                else
                {
                    cmenTabSendSpecialKeys.Visible = false;
                    cmenTabStartChat.Visible = false;
                    cmenTabRefreshScreen.Visible = false;
                    cmenTabTransferFile.Visible = false;
                }

                if (interfaceControl.Info.Protocol == ProtocolType.SSH1 |
                    interfaceControl.Info.Protocol == ProtocolType.SSH2)
                {
                    cmenTabTransferFile.Visible = true;
                }

                ConnectionInfo? selectedConnectionInfo = GetMultiSshConnectionInfoForTab(GetSelectedTab());
                bool showMultiSshFilters = interfaceControl.Protocol is PuttyBase && selectedConnectionInfo != null;

                _cmenTabMultiSshSeparator.Visible = showMultiSshFilters;
                _cmenTabIncludeInMultiSsh.Visible = showMultiSshFilters;
                _cmenTabExcludeFromMultiSsh.Visible = showMultiSshFilters;

                if (showMultiSshFilters)
                {
                    _cmenTabIncludeInMultiSsh.Checked = selectedConnectionInfo!.IncludeInMultiSsh;
                    _cmenTabExcludeFromMultiSsh.Checked = selectedConnectionInfo.ExcludeFromMultiSsh;
                    _cmenTabIncludeInMultiSsh.Enabled = !selectedConnectionInfo.ExcludeFromMultiSsh;
                    _cmenTabExcludeFromMultiSsh.Enabled = !selectedConnectionInfo.IncludeInMultiSsh;
                }

                cmenTabPuttySettings.Visible = interfaceControl.Protocol is PuttyBase;

                AddExternalApps();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ShowHideMenuButtons (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        #endregion

        #region Tab Actions

        private void ToggleSmartSize()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                switch (interfaceControl.Protocol)
                {
                    case RdpProtocol rdp:
                        rdp.ToggleSmartSize();
                        break;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleSmartSize (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void TransferFile()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                if (interfaceControl.Info.Protocol == ProtocolType.SSH1 |
                    interfaceControl.Info.Protocol == ProtocolType.SSH2)
                    SshTransferFile();
                else if (interfaceControl.Info.Protocol == ProtocolType.VNC)
                    VncTransferFile();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("TransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleMultiSshInclude()
        {
            try
            {
                ConnectionInfo? connectionInfo = GetMultiSshConnectionInfoForTab(GetSelectedTab());
                if (connectionInfo == null)
                    return;

                connectionInfo.IncludeInMultiSsh = !connectionInfo.IncludeInMultiSsh;
                if (connectionInfo.IncludeInMultiSsh)
                    connectionInfo.ExcludeFromMultiSsh = false;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleMultiSshInclude (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleMultiSshExclude()
        {
            try
            {
                ConnectionInfo? connectionInfo = GetMultiSshConnectionInfoForTab(GetSelectedTab());
                if (connectionInfo == null)
                    return;

                connectionInfo.ExcludeFromMultiSsh = !connectionInfo.ExcludeFromMultiSsh;
                if (connectionInfo.ExcludeFromMultiSsh)
                    connectionInfo.IncludeInMultiSsh = false;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleMultiSshExclude (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void SshTransferFile()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;

                AppWindows.Show(WindowType.SSHTransfer);
                ConnectionInfo connectionInfo = interfaceControl.Info;

                AppWindows.SshtransferForm.Hostname = connectionInfo.Hostname;
                AppWindows.SshtransferForm.Username = connectionInfo.Username;
                //App.Windows.SshtransferForm.Password = connectionInfo.Password.ConvertToUnsecureString();
                AppWindows.SshtransferForm.Password = connectionInfo.Password;
                AppWindows.SshtransferForm.Port = Convert.ToString(connectionInfo.Port);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("SSHTransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void VncTransferFile()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                ProtocolVNC? vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.StartFileTransfer();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("VNCTransferFile (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleViewOnly()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (!(interfaceControl?.Protocol is ISupportsViewOnly viewOnly))
                    return;

                cmenTabViewOnly.Checked = !cmenTabViewOnly.Checked;
                viewOnly.ToggleViewOnly();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleViewOnly (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void StartChat()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                ProtocolVNC? vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.StartChat();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("StartChat (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void RefreshScreen()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                ProtocolVNC? vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.RefreshScreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RefreshScreen (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void SendSpecialKeys(ProtocolVNC.SpecialKeys keys)
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                ProtocolVNC? vnc = interfaceControl?.Protocol as ProtocolVNC;
                vnc?.SendSpecialKeys(keys);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("SendSpecialKeys (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                RdpProtocol? rdp = interfaceControl?.Protocol as RdpProtocol;
                if (rdp?.RedirectKeysEnabled == true && rdp.Fullscreen)
                    return;
                rdp?.ToggleFullscreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("ToggleFullscreen (UI.Window.ConnectionWindow) failed",
                                                             ex);
            }
        }

        private void ShowPuttySettingsDialog()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                PuttyBase? puttyBase = interfaceControl?.Protocol as PuttyBase;
                puttyBase?.ShowSettingsDialog();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(
                                                             "ShowPuttySettingsDialog (UI.Window.ConnectionWindow) failed",
                                                             ex);
            }
        }

        private void AddExternalApps()
        {
            try
            {
                //clean up. since new items are added below, we have to dispose of any previous items first
                if (cmenTabExternalApps.DropDownItems.Count > 0)
                {
                    for (int i = cmenTabExternalApps.DropDownItems.Count - 1; i >= 0; i--)
                        cmenTabExternalApps.DropDownItems[i].Dispose();
                    cmenTabExternalApps.DropDownItems.Clear();
                }

                //add ext apps
                foreach (ExternalTool externalTool in Runtime.ExternalToolsService.ExternalTools)
                {
                    ToolStripMenuItem nItem = new()
                    {
                        Text = externalTool.DisplayName,
                        Tag = externalTool,
                        /* rare failure here. While ExternalTool.Image already tries to default this
                         * try again so it's not null/doesn't crash.
                         */
                        Image = externalTool.Image ?? Properties.Resources.mRemoteNG_Icon.ToBitmap()
                    };

                    nItem.Click += (sender, args) =>
                    {
                        if (sender is ToolStripMenuItem menuItem && menuItem.Tag is ExternalTool tool)
                            StartExternalApp(tool);
                    };
                    cmenTabExternalApps.DropDownItems.Add(nItem);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("cMenTreeTools_DropDownOpening failed (UI.Window.ConnectionWindow)", ex);
            }
        }

        private void StartExternalApp(ExternalTool externalTool)
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl?.Info != null)
                    externalTool.Start(interfaceControl.Info);
                else
                    externalTool.Start();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("cmenTabExternalAppsEntry_Click failed (UI.Window.ConnectionWindow)", ex);
            }
        }


        private void CloseTabMenu()
        {
            ConnectionTab? selectedTab = GetSelectedTab();
            if (selectedTab == null) return;

            try
            {
                selectedTab.Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("CloseTabMenu (UI.Window.ConnectionWindow) failed", ex);
            }
            finally
            {
                ClosePanelIfEmpty();
            }
        }

        private void CloseOtherTabs()
        {
            ConnectionTab? selectedTab = GetSelectedTab();
            if (selectedTab == null) return;
            if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.Multiple)
            {
                DialogResult result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName,
                                                    string.Format(Language.ConfirmCloseConnectionOthersInstruction,
                                                                  selectedTab.TabText), "", "", "",
                                                    Language.CheckboxDoNotShowThisMessageAgain,
                                                    ETaskDialogButtons.YesNo, ESysIcons.Question,
                                                    ESysIcons.Question);
                if (CTaskDialog.VerificationChecked)
                {
                    Settings.Default.ConfirmCloseConnection = (int)ConfirmCloseEnum.Never;
                    Settings.Default.Save();
                }

                if (result == DialogResult.No)
                {
                    return;
                }
            }

            foreach (IDockContent dockContent in connDock.Documents.ToArray())
            {
                ConnectionTab tab = (ConnectionTab)dockContent;
                if (selectedTab != tab)
                {
                    tab.Close();
                }
            }
        }

        private void CloseOtherTabsToTheRight()
        {
            try
            {
                ConnectionTab? selectedTab = GetSelectedTab();
                if (selectedTab == null) return;
                DockPane dockPane = selectedTab.Pane;

                bool pastTabToKeepAlive = false;
                List<ConnectionTab> connectionsToClose = new();
                foreach (IDockContent dockContent in dockPane.Contents)
                {
                    ConnectionTab tab = (ConnectionTab)dockContent;
                    if (pastTabToKeepAlive)
                        connectionsToClose.Add(tab);

                    if (selectedTab == tab)
                        pastTabToKeepAlive = true;
                }

                foreach (ConnectionTab tab in connectionsToClose)
                {
                    tab.Close();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("CloseTabMenu (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void DuplicateTab()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;
                Runtime.ConnectionInitiator.OpenConnection(interfaceControl.Info, ConnectionInfo.Force.DoNotJump);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("DuplicateTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void Reconnect()
        {
            try
            {
                ConnectionTab? selectedTab = GetSelectedTab();
                ConnectionInfo? connectionInfo = GetConnectionInfoForTab(selectedTab);
                if (connectionInfo == null)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, "Reconnect (UI.Window.ConnectionWindow) failed. Could not find ConnectionInfo.");
                    return;
                }

                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl != null)
                    HandleProtocolClosed(interfaceControl.Protocol, keepTabOpen: true);

                Runtime.ConnectionInitiator.OpenConnection(connectionInfo, ConnectionInfo.Force.DoNotJump, this);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Reconnect (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        private void RenameTab()
        {
            try
            {
                InterfaceControl? interfaceControl = GetInterfaceControl();
                if (interfaceControl == null) return;
                if (interfaceControl.Parent is not ConnectionTab connectionTab) return;
                using (FrmInputBox frmInputBox = new(Language.NewTitle, Language.NewTitle,
                                                         connectionTab.TabText))
                {
                    DialogResult dr = frmInputBox.ShowDialog();
                    if (dr != DialogResult.OK) return;
                    if (!string.IsNullOrEmpty(frmInputBox.returnValue))
                        connectionTab.TabText = frmInputBox.returnValue.Replace("&", "&&");
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RenameTab (UI.Window.ConnectionWindow) failed", ex);
            }
        }

        #endregion

        protected override void WndProc(ref System.Windows.Forms.Message m)
        {
            if (m.Msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                // Dismiss the tab context menu when the user clicks inside the RDP frame.
                // The RDP ActiveX control swallows mouse events, so the context menu never
                // receives a "click elsewhere" notification and stays open (#330).
                if (cmenTab.Visible)
                    cmenTab.Close();
            }

            base.WndProc(ref m);
        }

        #region Protocols

        public void Prot_Event_Closed(object sender)
        {
            HandleProtocolClosed(sender, keepTabOpen: true);
        }

        private void HandleProtocolClosed(object sender, bool keepTabOpen)
        {
            if (IsDisposed || Disposing || !IsHandleCreated)
                return;

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(new Action<object, bool>(HandleProtocolClosed), sender, keepTabOpen);
                }
                catch (ObjectDisposedException)
                {
                    // Window already disposed while protocol close callback was queued.
                }
                catch (InvalidOperationException)
                {
                    // Window handle is no longer valid.
                }

                return;
            }

            ProtocolBase? protocolBase = sender as ProtocolBase;
            if (!(protocolBase?.InterfaceControl?.Parent is ConnectionTab tabPage)) return;
            if (tabPage.Disposing || tabPage.IsDisposed) return;

            ConnectionInfo? closedConnectionInfo =
                tabPage.TrackedConnectionInfo ??
                protocolBase.InterfaceControl.OriginalInfo ??
                GetConnectionInfoForTab(tabPage) ??
                protocolBase.InterfaceControl.Info;

            if (closedConnectionInfo != null)
                tabPage.TrackConnection(closedConnectionInfo);

                        if (keepTabOpen)
                        {
                            if (protocolBase.InterfaceControl != null)
                            {
                                tabPage.Controls.Remove(protocolBase.InterfaceControl);
                                protocolBase.InterfaceControl.Dispose();
                            }
            
                            tabPage.ShowClosedState();
                            if (closedConnectionInfo != null)
                                FrmMain.Default.SelectedConnection = closedConnectionInfo; 
                            return;
                        }
            
                        tabPage.protocolClose = true;            try
            {
                tabPage.Close();
            }
            catch (ObjectDisposedException)
            {
                // Tab was already disposed by another close path.
            }
            catch (InvalidOperationException)
            {
                // Handle invalidated during close operation.
            }
            finally
            {
                ClosePanelIfEmpty();
            }
        }

        #endregion
    }
}

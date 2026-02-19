using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using mRemoteNG.Tools.Clipboard;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using mRemoteNG.Security;
using mRemoteNG.UI.TaskDialog;

// ReSharper disable UnusedParameter.Local


namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    public sealed class ConnectionContextMenu : ContextMenuStrip
    {
        private ToolStripMenuItem _cMenTreeAddConnection = null!;
        private ToolStripMenuItem _cMenTreeAddFolder = null!;
        private ToolStripMenuItem _cMenTreeAddRootFolder = null!;
        private ToolStripSeparator _cMenTreeSep1 = null!;
        private ToolStripMenuItem _cMenTreeConnect = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptions = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsDialog = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsConnectToConsoleSession = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsNoCredentials = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsConnectInFullscreen = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsAlternativeAddress = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsViewOnly = null!;
        private ToolStripMenuItem _cMenTreeDisconnect = null!;
        private ToolStripMenuItem _cMenTreeReconnect = null!;
        private ToolStripMenuItem _cMenTreeTypePassword = null!;
        private ToolStripMenuItem _cMenTreeTypeClipboard = null!;
        private ToolStripSeparator _cMenTreeSep2 = null!;
        private ToolStripMenuItem _cMenTreeToolsTransferFile = null!;
        private ToolStripMenuItem _cMenTreeToolsWakeOnLan = null!;
        private ToolStripMenuItem _cMenTreeToolsSort = null!;
        private ToolStripMenuItem _cMenTreeToolsSortAscending = null!;
        private ToolStripMenuItem _cMenTreeToolsSortDescending = null!;
        private ToolStripSeparator _cMenTreeSep3 = null!;
        private ToolStripMenuItem _cMenTreeRename = null!;
        private ToolStripMenuItem _cMenTreeDelete = null!;
        private ToolStripMenuItem _cMenTreeCopyHostname = null!;
        private ToolStripSeparator _cMenTreeSep4 = null!;
        private ToolStripMenuItem _cMenTreeMoveUp = null!;
        private ToolStripMenuItem _cMenTreeMoveDown = null!;
        private ToolStripMenuItem _cMenTreeToolsExternalApps = null!;
        private ToolStripMenuItem _cMenTreeDuplicate = null!;
        private ToolStripMenuItem _cMenTreeCreateLink = null!;
        private ToolStripMenuItem _cMenTreeProperties = null!;
        private ToolStripMenuItem _cMenInheritanceSubMenu = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting = null!;
        private ToolStripMenuItem _cMenTreeConnectWithOptionsDontConnectToConsoleSession = null!;
        private ToolStripMenuItem _cMenTreeImport = null!;
        private ToolStripMenuItem _cMenTreeLoadAdditionalFile = null!;
        private ToolStripMenuItem _cMenTreeExportFile = null!;
        private ToolStripSeparator _toolStripSeparator1 = null!;
        private ToolStripMenuItem _cMenTreeImportFile = null!;
        private ToolStripMenuItem _cMenTreeImportFromRemoteDesktopManager = null!;
        private ToolStripMenuItem _cMenTreeImportActiveDirectory = null!;
        private ToolStripMenuItem _cMenTreeImportPortScan = null!;
        private ToolStripMenuItem _cMenTreeImportGuacamole = null!;
        private ToolStripMenuItem _cMenTreeImportPutty = null!;
        private ToolStripMenuItem _cMenTreeApplyInheritanceToChildren = null!;
        private ToolStripMenuItem _cMenTreeApplyDefaultInheritance = null!;
        private ToolStripMenuItem _cMenTreeConfigureDynamicSource = null!;
        private ToolStripMenuItem _cMenTreeRefreshDynamicSource = null!;
        private ToolStripSeparator _cMenTreeSep5 = null!;
        private ToolStripMenuItem _cMenTreeOptions = null!;
        private readonly ConnectionTree.ConnectionTree _connectionTree;


        public ConnectionContextMenu(ConnectionTree.ConnectionTree connectionTree)
        {
            _connectionTree = connectionTree;
            InitializeComponent();
            ApplyLanguage();
            EnableShortcutKeys();
            Opening += (sender, args) =>
            {
                AddExternalApps();
                if (_connectionTree.SelectedNode == null)
                {
                    args.Cancel = true;
                    return;
                }

                ShowHideMenuItems();
            };
        }

        private void InitializeComponent()
        {
            _cMenTreeConnect = new ToolStripMenuItem();
            _cMenTreeConnectWithOptions = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsDialog = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsConnectToConsoleSession = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsConnectInFullscreen = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsNoCredentials = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsAlternativeAddress = new ToolStripMenuItem();
            _cMenTreeConnectWithOptionsViewOnly = new ToolStripMenuItem();
            _cMenTreeDisconnect = new ToolStripMenuItem();
            _cMenTreeReconnect = new ToolStripMenuItem();
            _cMenTreeTypePassword = new ToolStripMenuItem();
            _cMenTreeTypeClipboard = new ToolStripMenuItem();
            _cMenTreeSep1 = new ToolStripSeparator();
            _cMenTreeToolsExternalApps = new ToolStripMenuItem();
            _cMenTreeToolsTransferFile = new ToolStripMenuItem();
            _cMenTreeToolsWakeOnLan = new ToolStripMenuItem();
            _cMenTreeSep2 = new ToolStripSeparator();
            _cMenTreeDuplicate = new ToolStripMenuItem();
            _cMenTreeCreateLink = new ToolStripMenuItem();
            _cMenTreeProperties = new ToolStripMenuItem();
            _cMenTreeRename = new ToolStripMenuItem();
            _cMenTreeDelete = new ToolStripMenuItem();
            _cMenTreeCopyHostname = new ToolStripMenuItem();
            _cMenTreeSep3 = new ToolStripSeparator();
            _cMenTreeLoadAdditionalFile = new ToolStripMenuItem();
            _cMenTreeImport = new ToolStripMenuItem();
            _cMenTreeImportFile = new ToolStripMenuItem();
            _cMenTreeImportFromRemoteDesktopManager = new ToolStripMenuItem();
            _cMenTreeImportActiveDirectory = new ToolStripMenuItem();
            _cMenTreeImportPortScan = new ToolStripMenuItem();
            _cMenTreeImportGuacamole = new ToolStripMenuItem();
            _cMenTreeImportPutty = new ToolStripMenuItem();
            _cMenInheritanceSubMenu = new ToolStripMenuItem();
            _cMenTreeApplyInheritanceToChildren = new ToolStripMenuItem();
            _cMenTreeApplyDefaultInheritance = new ToolStripMenuItem();
            _cMenTreeConfigureDynamicSource = new ToolStripMenuItem();
            _cMenTreeRefreshDynamicSource = new ToolStripMenuItem();
            _cMenTreeExportFile = new ToolStripMenuItem();
            _cMenTreeSep4 = new ToolStripSeparator();
            _cMenTreeAddConnection = new ToolStripMenuItem();
            _cMenTreeAddFolder = new ToolStripMenuItem();
            _cMenTreeAddRootFolder = new ToolStripMenuItem();
            _toolStripSeparator1 = new ToolStripSeparator();
            _cMenTreeToolsSort = new ToolStripMenuItem();
            _cMenTreeToolsSortAscending = new ToolStripMenuItem();
            _cMenTreeToolsSortDescending = new ToolStripMenuItem();
            _cMenTreeMoveUp = new ToolStripMenuItem();
            _cMenTreeMoveDown = new ToolStripMenuItem();
            _cMenTreeSep5 = new ToolStripSeparator();
            _cMenTreeOptions = new ToolStripMenuItem();


            //
            // cMenTree
            //
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular,
                                           System.Drawing.GraphicsUnit.Point, 0);
            Items.AddRange(new ToolStripItem[]
            {
                _cMenTreeAddConnection,
                _cMenTreeAddFolder,
                _cMenTreeAddRootFolder,
                _cMenTreeSep4,
                _cMenTreeConnect,
                _cMenTreeConnectWithOptions,
                _cMenTreeDisconnect,
                _cMenTreeReconnect,
                _cMenTreeTypePassword,
                _cMenTreeTypeClipboard,
                _cMenTreeSep1,
                _cMenTreeToolsExternalApps,
                _cMenTreeToolsTransferFile,
                _cMenTreeToolsWakeOnLan,
                _cMenTreeSep2,
                _cMenTreeDuplicate,
                _cMenTreeCreateLink,
                _cMenTreeRename,
                _cMenTreeDelete,
                _cMenTreeCopyHostname,
                _cMenTreeProperties,
                _cMenInheritanceSubMenu,
                _cMenTreeConfigureDynamicSource,
                _cMenTreeRefreshDynamicSource,
                _cMenTreeSep3,
                _cMenTreeLoadAdditionalFile,
                _cMenTreeImport,
                _cMenTreeExportFile,
                _toolStripSeparator1,
                _cMenTreeToolsSort,
                _cMenTreeMoveUp,
                _cMenTreeMoveDown,
                _cMenTreeSep5,
                _cMenTreeOptions
            });
            Name = "cMenTree";
            RenderMode = ToolStripRenderMode.Professional;
            Size = new System.Drawing.Size(200, 386);
            //
            // cMenTreeConnect
            //
            _cMenTreeConnect.Image = Properties.Resources.Run_16x;
            _cMenTreeConnect.Name = "_cMenTreeConnect";
            _cMenTreeConnect.Size = new System.Drawing.Size(199, 22);
            _cMenTreeConnect.Text = "Connect";
            _cMenTreeConnect.Click += OnConnectClicked;
            //
            // cMenTreeConnectWithOptions
            //
            _cMenTreeConnectWithOptions.DropDownItems.AddRange(new ToolStripItem[]
            {
                _cMenTreeConnectWithOptionsDialog,
                _cMenTreeConnectWithOptionsConnectToConsoleSession,
                _cMenTreeConnectWithOptionsDontConnectToConsoleSession,
                _cMenTreeConnectWithOptionsConnectInFullscreen,
                _cMenTreeConnectWithOptionsNoCredentials,
                _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting,
                _cMenTreeConnectWithOptionsAlternativeAddress,
                _cMenTreeConnectWithOptionsViewOnly
            });
            _cMenTreeConnectWithOptions.Name = "_cMenTreeConnectWithOptions";
            _cMenTreeConnectWithOptions.Size = new System.Drawing.Size(199, 22);
            _cMenTreeConnectWithOptions.Text = "Connect (with options)";
            //
            // cMenTreeConnectWithOptionsDialog
            //
            _cMenTreeConnectWithOptionsDialog.Name = "_cMenTreeConnectWithOptionsDialog";
            _cMenTreeConnectWithOptionsDialog.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsDialog.Text = "Connect with options...";
            _cMenTreeConnectWithOptionsDialog.Click += OnConnectWithOptionsDialogClicked;
            //
            // cMenTreeConnectWithOptionsConnectToConsoleSession
            //
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Name =
                "_cMenTreeConnectWithOptionsConnectToConsoleSession";
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Text = "Connect to console session";
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Click += OnConnectToConsoleSessionClicked;
            //
            // cMenTreeConnectWithOptionsDontConnectToConsoleSession
            //
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Name =
                "_cMenTreeConnectWithOptionsDontConnectToConsoleSession";
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Text = "Don\'t connect to console session";
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Visible = false;
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Click += OnDontConnectToConsoleSessionClicked;
            //
            // cMenTreeConnectWithOptionsConnectInFullscreen
            //
            _cMenTreeConnectWithOptionsConnectInFullscreen.Image = Properties.Resources.FullScreen_16x;
            _cMenTreeConnectWithOptionsConnectInFullscreen.Name = "_cMenTreeConnectWithOptionsConnectInFullscreen";
            _cMenTreeConnectWithOptionsConnectInFullscreen.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsConnectInFullscreen.Text = "Connect in fullscreen";
            _cMenTreeConnectWithOptionsConnectInFullscreen.Click += OnConnectInFullscreenClicked;
            //
            // cMenTreeConnectWithOptionsNoCredentials
            //
            _cMenTreeConnectWithOptionsNoCredentials.Image = Properties.Resources.UniqueKeyError_16x;
            _cMenTreeConnectWithOptionsNoCredentials.Name = "_cMenTreeConnectWithOptionsNoCredentials";
            _cMenTreeConnectWithOptionsNoCredentials.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsNoCredentials.Text = "Connect without credentials";
            _cMenTreeConnectWithOptionsNoCredentials.Click += OnConnectWithNoCredentialsClick;
            //
            // cMenTreeConnectWithOptionsChoosePanelBeforeConnecting
            //
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Image = Properties.Resources.Panel_16x;
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Name =
                "_cMenTreeConnectWithOptionsChoosePanelBeforeConnecting";
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Text = "Choose panel before connecting";
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Click += OnChoosePanelBeforeConnectingClicked;
            //
            // cMenTreeConnectWithOptionsAlternativeAddress
            //
            _cMenTreeConnectWithOptionsAlternativeAddress.Name =
                "_cMenTreeConnectWithOptionsAlternativeAddress";
            _cMenTreeConnectWithOptionsAlternativeAddress.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsAlternativeAddress.Text = "Connect using alternative hostname/IP";
            _cMenTreeConnectWithOptionsAlternativeAddress.Click += OnConnectUsingAlternativeAddressClick;
            //
            // cMenTreeConnectWithOptionsViewOnly
            //
            _cMenTreeConnectWithOptionsViewOnly.Image = Properties.Resources.Monitor_16x;
            _cMenTreeConnectWithOptionsViewOnly.Name =
                "_cMenTreeConnectWithOptionsViewOnly";
            _cMenTreeConnectWithOptionsViewOnly.Size = new System.Drawing.Size(245, 22);
            _cMenTreeConnectWithOptionsViewOnly.Text = Language.ConnectInViewOnlyMode;
            _cMenTreeConnectWithOptionsViewOnly.Click += ConnectWithOptionsViewOnlyOnClick;
            //
            // cMenTreeDisconnect
            //
            _cMenTreeDisconnect.Image = Properties.Resources.Stop_16x;
            _cMenTreeDisconnect.Name = "_cMenTreeDisconnect";
            _cMenTreeDisconnect.Size = new System.Drawing.Size(199, 22);
            _cMenTreeDisconnect.Text = "Disconnect";
            _cMenTreeDisconnect.Click += OnDisconnectClicked;
            //
            // cMenTreeReconnect
            //
            _cMenTreeReconnect.Image = Properties.Resources.Refresh_16x;
            _cMenTreeReconnect.Name = "_cMenTreeReconnect";
            _cMenTreeReconnect.Size = new System.Drawing.Size(199, 22);
            _cMenTreeReconnect.Text = "Reconnect";
            _cMenTreeReconnect.Click += OnReconnectClicked;
            //
            // cMenTreeTypePassword
            //
            _cMenTreeTypePassword.Name = "_cMenTreeTypePassword";
            _cMenTreeTypePassword.Size = new System.Drawing.Size(199, 22);
            _cMenTreeTypePassword.Text = "Type Password";
            _cMenTreeTypePassword.Click += OnTypePasswordClicked;
            //
            // cMenTreeTypeClipboard
            //
            _cMenTreeTypeClipboard.Name = "_cMenTreeTypeClipboard";
            _cMenTreeTypeClipboard.Size = new System.Drawing.Size(199, 22);
            _cMenTreeTypeClipboard.Text = "Type Clipboard Text";
            _cMenTreeTypeClipboard.Click += OnTypeClipboardClicked;
            //
            // cMenTreeSep1
            //
            _cMenTreeSep1.Name = "_cMenTreeSep1";
            _cMenTreeSep1.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeToolsExternalApps
            //
            _cMenTreeToolsExternalApps.Image = Properties.Resources.Console_16x;
            _cMenTreeToolsExternalApps.Name = "_cMenTreeToolsExternalApps";
            _cMenTreeToolsExternalApps.Size = new System.Drawing.Size(199, 22);
            _cMenTreeToolsExternalApps.Text = "External Applications";
            //
            // cMenTreeToolsTransferFile
            //
            _cMenTreeToolsTransferFile.Image = Properties.Resources.SyncArrow_16x;
            _cMenTreeToolsTransferFile.Name = "_cMenTreeToolsTransferFile";
            _cMenTreeToolsTransferFile.Size = new System.Drawing.Size(199, 22);
            _cMenTreeToolsTransferFile.Text = "Transfer File (SSH)";
            _cMenTreeToolsTransferFile.Click += OnTransferFileClicked;
            //
            // cMenTreeToolsWakeOnLan
            //
            _cMenTreeToolsWakeOnLan.Name = "_cMenTreeToolsWakeOnLan";
            _cMenTreeToolsWakeOnLan.Size = new System.Drawing.Size(199, 22);
            _cMenTreeToolsWakeOnLan.Text = "Wake On LAN";
            _cMenTreeToolsWakeOnLan.Click += OnWakeOnLanClicked;
            //
            // cMenTreeSep2
            //
            _cMenTreeSep2.Name = "_cMenTreeSep2";
            _cMenTreeSep2.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeDuplicate
            //
            _cMenTreeDuplicate.Image = Properties.Resources.Copy_16x;
            _cMenTreeDuplicate.Name = "_cMenTreeDuplicate";
            _cMenTreeDuplicate.Size = new System.Drawing.Size(199, 22);
            _cMenTreeDuplicate.Text = "Duplicate";
            _cMenTreeDuplicate.Click += OnDuplicateClicked;
            //
            // cMenTreeCreateLink
            //
            _cMenTreeCreateLink.Image = Properties.Resources.Copy_16x;
            _cMenTreeCreateLink.Name = "_cMenTreeCreateLink";
            _cMenTreeCreateLink.Size = new System.Drawing.Size(199, 22);
            _cMenTreeCreateLink.Text = "Create Link";
            _cMenTreeCreateLink.Click += OnCreateLinkClicked;
            //
            // cMenTreeRename
            //
            _cMenTreeRename.Image = Properties.Resources.Rename_16x;
            _cMenTreeRename.Name = "_cMenTreeRename";
            _cMenTreeRename.Size = new System.Drawing.Size(199, 22);
            _cMenTreeRename.Text = "Rename";
            _cMenTreeRename.Click += OnRenameClicked;
            //
            // cMenTreeDelete
            //
            _cMenTreeDelete.Image = Properties.Resources.Close_16x;
            _cMenTreeDelete.Name = "_cMenTreeDelete";
            _cMenTreeDelete.Size = new System.Drawing.Size(199, 22);
            _cMenTreeDelete.Text = "Delete";
            _cMenTreeDelete.Click += OnDeleteClicked;
            //
            // cMenTreeCopyHostname
            //
            _cMenTreeCopyHostname.Name = "_cMenTreeCopyHostname";
            _cMenTreeCopyHostname.Size = new System.Drawing.Size(199, 22);
            _cMenTreeCopyHostname.Text = "Copy Hostname";
            _cMenTreeCopyHostname.Click += OnCopyHostnameClicked;
            //
            // cMenTreeProperties
            //
            _cMenTreeProperties.Image = Properties.Resources.Property_16x;
            _cMenTreeProperties.Name = "_cMenTreeProperties";
            _cMenTreeProperties.Size = new System.Drawing.Size(199, 22);
            _cMenTreeProperties.Text = "Properties";
            _cMenTreeProperties.Click += OnPropertiesClicked;
            //
            // cMenTreeSep3
            //
            _cMenTreeSep3.Name = "_cMenTreeSep3";
            _cMenTreeSep3.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeLoadAdditionalFile
            //
            _cMenTreeLoadAdditionalFile.Name = "_cMenTreeLoadAdditionalFile";
            _cMenTreeLoadAdditionalFile.Size = new System.Drawing.Size(199, 22);
            _cMenTreeLoadAdditionalFile.Text = "Open Connection File...";
            _cMenTreeLoadAdditionalFile.Click += OnLoadAdditionalFileClicked;
            //
            // cMenTreeImport
            //
            _cMenTreeImport.DropDownItems.AddRange(new ToolStripItem[]
            {
                _cMenTreeImportFile,
                _cMenTreeImportFromRemoteDesktopManager,
                _cMenTreeImportActiveDirectory,
                _cMenTreeImportPutty,
                _cMenTreeImportPortScan,
                _cMenTreeImportGuacamole
            });
            _cMenTreeImport.Name = "_cMenTreeImport";
            _cMenTreeImport.Size = new System.Drawing.Size(199, 22);
            _cMenTreeImport.Text = "&Import";
            //
            // cMenTreeImportFile
            //
            _cMenTreeImportFile.Name = "_cMenTreeImportFile";
            _cMenTreeImportFile.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportFile.Text = "Import from &File...";
            _cMenTreeImportFile.Click += OnImportFileClicked;

            // cMenTreeImportFromRemoteDesktopManager
            _cMenTreeImportFromRemoteDesktopManager.Name = "_cMenTreeImportFromRemoteDesktopManager";
            _cMenTreeImportFromRemoteDesktopManager.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportFromRemoteDesktopManager.Text = "Import from &Remote Desktop Manager...";
            _cMenTreeImportFromRemoteDesktopManager.Click += OnImportRemoteDesktopManagerClicked;
            //
            // cMenTreeImportActiveDirectory
            //
            _cMenTreeImportActiveDirectory.Name = "_cMenTreeImportActiveDirectory";
            _cMenTreeImportActiveDirectory.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportActiveDirectory.Text = "Import from &Active Directory...";
            _cMenTreeImportActiveDirectory.Click += OnImportActiveDirectoryClicked;
            //
            // cMenTreeImportPortScan
            //
            _cMenTreeImportPortScan.Name = "_cMenTreeImportPortScan";
            _cMenTreeImportPortScan.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportPortScan.Text = "Import from &Port Scan...";
            _cMenTreeImportPortScan.Click += OnImportPortScanClicked;
            //
            // cMenTreeImportGuacamole
            //
            _cMenTreeImportGuacamole.Name = "_cMenTreeImportGuacamole";
            _cMenTreeImportGuacamole.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportGuacamole.Text = "Import from &Guacamole...";
            _cMenTreeImportGuacamole.Click += OnImportGuacamoleClicked;
            //
            // cMenTreeImportPutty
            //
            _cMenTreeImportPutty.Name = "_cMenTreeImportPutty";
            _cMenTreeImportPutty.Size = new System.Drawing.Size(226, 22);
            _cMenTreeImportPutty.Text = "Import from &Putty...";
            _cMenTreeImportPutty.Click += OnImportPuttyClicked;
            //
            // cMenTreeExportFile
            //
            _cMenTreeExportFile.Name = "_cMenTreeExportFile";
            _cMenTreeExportFile.Size = new System.Drawing.Size(199, 22);
            _cMenTreeExportFile.Text = "&Export to File...";
            _cMenTreeExportFile.Click += OnExportFileClicked;
            //
            // cMenTreeSep4
            //
            _cMenTreeSep4.Name = "_cMenTreeSep4";
            _cMenTreeSep4.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeAddConnection
            //
            _cMenTreeAddConnection.Image = Properties.Resources.AddItem_16x;
            _cMenTreeAddConnection.Name = "_cMenTreeAddConnection";
            _cMenTreeAddConnection.Size = new System.Drawing.Size(199, 22);
            _cMenTreeAddConnection.Text = "New Connection";
            _cMenTreeAddConnection.Click += OnAddConnectionClicked;
            //
            // cMenTreeAddFolder
            //
            _cMenTreeAddFolder.Image = Properties.Resources.AddFolder_16x;
            _cMenTreeAddFolder.Name = "_cMenTreeAddFolder";
            _cMenTreeAddFolder.Size = new System.Drawing.Size(199, 22);
            _cMenTreeAddFolder.Text = "New Folder";
            _cMenTreeAddFolder.Click += OnAddFolderClicked;
            //
            // cMenTreeAddRootFolder
            //
            _cMenTreeAddRootFolder.Image = Properties.Resources.AddFolder_16x;
            _cMenTreeAddRootFolder.Name = "_cMenTreeAddRootFolder";
            _cMenTreeAddRootFolder.Size = new System.Drawing.Size(199, 22);
            _cMenTreeAddRootFolder.Text = "New Root Folder";
            _cMenTreeAddRootFolder.Click += OnAddRootFolderClicked;
            //
            // ToolStripSeparator1
            //
            _toolStripSeparator1.Name = "_toolStripSeparator1";
            _toolStripSeparator1.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeToolsSort
            //
            _cMenTreeToolsSort.DropDownItems.AddRange(new ToolStripItem[]
            {
                _cMenTreeToolsSortAscending,
                _cMenTreeToolsSortDescending
            });
            _cMenTreeToolsSort.Name = "_cMenTreeToolsSort";
            _cMenTreeToolsSort.Size = new System.Drawing.Size(199, 22);
            _cMenTreeToolsSort.Text = "Sort";
            //
            // cMenTreeToolsSortAscending
            //
            _cMenTreeToolsSortAscending.Image = Properties.Resources.SortAscending_16x;
            _cMenTreeToolsSortAscending.Name = "_cMenTreeToolsSortAscending";
            _cMenTreeToolsSortAscending.Size = new System.Drawing.Size(161, 22);
            _cMenTreeToolsSortAscending.Text = "Ascending (A-Z)";
            _cMenTreeToolsSortAscending.Click += OnSortAscendingClicked;
            //
            // cMenTreeToolsSortDescending
            //
            _cMenTreeToolsSortDescending.Image = Properties.Resources.SortDescending_16x;
            _cMenTreeToolsSortDescending.Name = "_cMenTreeToolsSortDescending";
            _cMenTreeToolsSortDescending.Size = new System.Drawing.Size(161, 22);
            _cMenTreeToolsSortDescending.Text = "Descending (Z-A)";
            _cMenTreeToolsSortDescending.Click += OnSortDescendingClicked;
            //
            // cMenTreeMoveUp
            //
            _cMenTreeMoveUp.Image = Properties.Resources.GlyphUp_16x;
            _cMenTreeMoveUp.Name = "_cMenTreeMoveUp";
            _cMenTreeMoveUp.Size = new System.Drawing.Size(199, 22);
            _cMenTreeMoveUp.Text = "Move up";
            _cMenTreeMoveUp.Click += OnMoveUpClicked;
            //
            // cMenTreeMoveDown
            //
            _cMenTreeMoveDown.Image = Properties.Resources.GlyphDown_16x;
            _cMenTreeMoveDown.Name = "_cMenTreeMoveDown";
            _cMenTreeMoveDown.Size = new System.Drawing.Size(199, 22);
            _cMenTreeMoveDown.Text = "Move down";
            _cMenTreeMoveDown.Click += OnMoveDownClicked;
            //
            // cMenTreeSep5
            //
            _cMenTreeSep5.Name = "_cMenTreeSep5";
            _cMenTreeSep5.Size = new System.Drawing.Size(196, 6);
            //
            // cMenTreeOptions
            //
            _cMenTreeOptions.Image = Properties.Resources.Settings_16x;
            _cMenTreeOptions.Name = "_cMenTreeOptions";
            _cMenTreeOptions.Size = new System.Drawing.Size(199, 22);
            _cMenTreeOptions.Text = "Options";
            _cMenTreeOptions.Click += OnOptionsClicked;
            //
            // cMenEditSubMenu
            //
            _cMenInheritanceSubMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                _cMenTreeApplyInheritanceToChildren,
                _cMenTreeApplyDefaultInheritance
            });
            _cMenInheritanceSubMenu.Name = "_cMenInheritanceSubMenu";
            _cMenInheritanceSubMenu.Size = new System.Drawing.Size(199, 22);
            _cMenInheritanceSubMenu.Text = "Inheritance";
            //
            // _cMenTreeApplyInheritanceToChildren
            //
            _cMenTreeApplyInheritanceToChildren.Name = "_cMenTreeApplyInheritanceToChildren";
            _cMenTreeApplyInheritanceToChildren.Size = new System.Drawing.Size(199, 22);
            _cMenTreeApplyInheritanceToChildren.Text = "Apply inheritance to children";
            _cMenTreeApplyInheritanceToChildren.Click += OnApplyInheritanceToChildrenClicked;
            //
            // _cMenTreeApplyDefaultInheritance
            //
            _cMenTreeApplyDefaultInheritance.Name = "_cMenTreeApplyDefaultInheritance";
            _cMenTreeApplyDefaultInheritance.Size = new System.Drawing.Size(199, 22);
            _cMenTreeApplyDefaultInheritance.Text = "Apply default inheritance";
            _cMenTreeApplyDefaultInheritance.Click += OnApplyDefaultInheritanceClicked;

            //
            // _cMenTreeConfigureDynamicSource
            //
            _cMenTreeConfigureDynamicSource.Name = "_cMenTreeConfigureDynamicSource";
            _cMenTreeConfigureDynamicSource.Size = new System.Drawing.Size(199, 22);
            _cMenTreeConfigureDynamicSource.Text = "Configure Dynamic Source...";
            _cMenTreeConfigureDynamicSource.Click += OnConfigureDynamicSourceClicked;

            //
            // _cMenTreeRefreshDynamicSource
            //
            _cMenTreeRefreshDynamicSource.Name = "_cMenTreeRefreshDynamicSource";
            _cMenTreeRefreshDynamicSource.Size = new System.Drawing.Size(199, 22);
            _cMenTreeRefreshDynamicSource.Text = "Refresh Dynamic Folder";
            _cMenTreeRefreshDynamicSource.Click += OnRefreshDynamicSourceClicked;
        }


        private void ApplyLanguage()
        {
            _cMenTreeConnect.Text = Language.Connect;
            _cMenTreeConnectWithOptions.Text = Language.ConnectWithOptions;
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Text = Language.ConnectToConsoleSession;
            _cMenTreeConnectWithOptionsDontConnectToConsoleSession.Text = Language.DontConnectToConsoleSession;
            _cMenTreeConnectWithOptionsConnectInFullscreen.Text = Language.ConnectInFullscreen;
            _cMenTreeConnectWithOptionsNoCredentials.Text = Language.ConnectNoCredentials;
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Text = Language.ChoosePanelBeforeConnecting;
            _cMenTreeConnectWithOptionsAlternativeAddress.Text = "Connect using alternative hostname/IP";
            _cMenTreeConnectWithOptionsViewOnly.Text = Language.ConnectInViewOnlyMode;
            _cMenTreeDisconnect.Text = Language.Disconnect;
            _cMenTreeReconnect.Text = Language.Reconnect;
            _cMenTreeTypePassword.Text = Language.TypePassword;
            _cMenTreeTypeClipboard.Text = Language.TypeClipboard;

            _cMenTreeToolsExternalApps.Text = Language._Tools;
            _cMenTreeToolsTransferFile.Text = Language.TransferFile;
            _cMenTreeToolsWakeOnLan.Text = Language.ResourceManager.GetString("WakeOnLan", Language.Culture) ?? "Wake On LAN";

            _cMenTreeDuplicate.Text = Language.Duplicate;
            _cMenTreeCreateLink.Text = "Create Link";
            _cMenTreeRename.Text = Language.Rename;
            _cMenTreeDelete.Text = Language.Delete;
            _cMenTreeCopyHostname.Text = Language.CopyHostname;
            _cMenTreeProperties.Text = Language.Properties;

            _cMenTreeImport.Text = Language._Import;
            _cMenTreeImportFile.Text = Language.ImportFromFile;
            _cMenTreeImportActiveDirectory.Text = Language.ImportAD;
            _cMenTreeImportPortScan.Text = Language.ImportPortScan;
            _cMenTreeExportFile.Text = Language._ExportToFile;

            _cMenTreeAddConnection.Text = Language.NewConnection;
            _cMenTreeAddFolder.Text = Language.NewFolder;
            _cMenTreeAddRootFolder.Text = "New Root Folder";

            _cMenTreeToolsSort.Text = Language.Sort;
            _cMenTreeToolsSortAscending.Text = Language.SortAsc;
            _cMenTreeToolsSortDescending.Text = Language.SortDesc;
            _cMenTreeMoveUp.Text = Language.MoveUp;
            _cMenTreeMoveDown.Text = Language.MoveDown;
            _cMenTreeOptions.Text = Language.OptionsMenuItem;

            _cMenInheritanceSubMenu.Text = Language.Inheritance;
            _cMenTreeApplyInheritanceToChildren.Text = Language.ApplyInheritanceToChildren;
            _cMenTreeApplyDefaultInheritance.Text = Language.ApplyDefaultInheritance;
        }

        internal void ShowHideMenuItems()
        {
            try
            {
                Enabled = true;
                EnableMenuItemsRecursive(Items);
                if (_connectionTree.SelectedNode is RootPuttySessionsNodeInfo)
                {
                    ShowHideMenuItemsForRootPuttyNode();
                }
                else if (_connectionTree.SelectedNode is RootNodeInfo)
                {
                    ShowHideMenuItemsForRootConnectionNode();
                }
                else if (_connectionTree.SelectedNode is ContainerInfo containerInfo)
                {
                    ShowHideMenuItemsForContainer(containerInfo);
                }
                else if (_connectionTree.SelectedNode is PuttySessionInfo puttyNode)
                {
                    ShowHideMenuItemsForPuttyNode(puttyNode);
                }
                else
                {
                    ShowHideMenuItemsForConnectionNode(_connectionTree.SelectedNode);
                }

                _cMenInheritanceSubMenu.Enabled = _cMenInheritanceSubMenu.DropDownItems
                    .OfType<ToolStripMenuItem>().Any(i => i.Enabled);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "ShowHideMenuItems (UI.Controls.ConnectionContextMenu) failed",
                                                                ex);
            }
        }

        internal void ShowHideMenuItemsForRootPuttyNode()
        {
            _cMenTreeAddConnection.Enabled = false;
            _cMenTreeAddFolder.Enabled = false;
            _cMenTreeAddRootFolder.Enabled = false;
            _cMenTreeConnect.Enabled = false;
            _cMenTreeConnectWithOptions.Enabled = false;
            _cMenTreeDisconnect.Enabled = false;
            _cMenTreeReconnect.Enabled = false;
            _cMenTreeTypePassword.Enabled = false;
            _cMenTreeTypeClipboard.Enabled = false;
            _cMenTreeToolsTransferFile.Enabled = false;
            _cMenTreeToolsWakeOnLan.Enabled = false;
            _cMenTreeConnectWithOptions.Enabled = false;
            _cMenTreeToolsSort.Enabled = false;
            _cMenTreeToolsExternalApps.Enabled = false;
            _cMenTreeDuplicate.Enabled = false;
            _cMenTreeCreateLink.Enabled = false;
            _cMenTreeImport.Enabled = false;
            _cMenTreeExportFile.Enabled = false;
            _cMenTreeRename.Enabled = false;
            _cMenTreeDelete.Enabled = false;
            _cMenTreeMoveUp.Enabled = false;
            _cMenTreeMoveDown.Enabled = false;
            _cMenTreeConnectWithOptionsAlternativeAddress.Enabled = false;
            _cMenTreeConnectWithOptionsViewOnly.Enabled = false;
            _cMenTreeApplyInheritanceToChildren.Enabled = false;
            _cMenTreeApplyDefaultInheritance.Enabled = false;
            _cMenTreeCopyHostname.Enabled = false;
            _cMenTreeProperties.Enabled = false;
            _cMenTreeConfigureDynamicSource.Visible = false;
            _cMenTreeRefreshDynamicSource.Visible = false;
        }

        internal void ShowHideMenuItemsForRootConnectionNode()
        {
            _cMenTreeConnect.Enabled = false;
            _cMenTreeConnectWithOptions.Enabled = false;
            _cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
            _cMenTreeConnectWithOptionsChoosePanelBeforeConnecting.Enabled = false;
            _cMenTreeConnectWithOptionsAlternativeAddress.Enabled = false;
            _cMenTreeDisconnect.Enabled = false;
            _cMenTreeReconnect.Enabled = false;
            _cMenTreeTypePassword.Enabled = false;
            _cMenTreeTypeClipboard.Enabled = false;
            _cMenTreeToolsTransferFile.Enabled = false;
            _cMenTreeToolsWakeOnLan.Enabled = false;
            _cMenTreeToolsExternalApps.Enabled = false;
            _cMenTreeDuplicate.Enabled = false;
            _cMenTreeCreateLink.Enabled = false;
            _cMenTreeDelete.Enabled = _connectionTree.ConnectionTreeModel.RootNodes.Count > 1;
            _cMenTreeMoveUp.Enabled = false;
            _cMenTreeMoveDown.Enabled = false;
            _cMenTreeConnectWithOptionsViewOnly.Enabled = false;
            _cMenTreeApplyInheritanceToChildren.Enabled = false;
            _cMenTreeApplyDefaultInheritance.Enabled = false;
            _cMenTreeConfigureDynamicSource.Visible = false;
            _cMenTreeRefreshDynamicSource.Visible = false;
        }

        internal void ShowHideMenuItemsForContainer(ContainerInfo containerInfo)
        {
            _cMenTreeConnectWithOptionsDialog.Enabled = false;
            _cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;

            bool hasOpenConnections = containerInfo.Children.Any(child => child.OpenConnections.Count > 0);
            _cMenTreeDisconnect.Enabled = hasOpenConnections;
            _cMenTreeReconnect.Enabled = hasOpenConnections;
            _cMenTreeTypePassword.Enabled = false;
            _cMenTreeTypeClipboard.Enabled = false;

            _cMenTreeToolsTransferFile.Enabled = false;
            _cMenTreeToolsWakeOnLan.Enabled = WakeOnLan.IsValidMacAddress(containerInfo.MacAddress);
            _cMenTreeCreateLink.Enabled = false;
            _cMenTreeConnectWithOptionsAlternativeAddress.Enabled = false;
            _cMenTreeConnectWithOptionsViewOnly.Enabled = false;

            _cMenTreeConfigureDynamicSource.Visible = true;
            _cMenTreeRefreshDynamicSource.Visible = containerInfo.DynamicSource != DynamicSourceType.None;
        }

        internal void ShowHideMenuItemsForPuttyNode(PuttySessionInfo connectionInfo)
        {
            _cMenTreeAddConnection.Enabled = false;
            _cMenTreeAddFolder.Enabled = false;
            _cMenTreeAddRootFolder.Enabled = false;

            if (connectionInfo.OpenConnections.Count == 0)
            {
                _cMenTreeDisconnect.Enabled = false;
                _cMenTreeReconnect.Enabled = false;
                _cMenTreeTypePassword.Enabled = false;
                _cMenTreeTypeClipboard.Enabled = false;
            }

            if (!(connectionInfo.Protocol == ProtocolType.SSH1 | connectionInfo.Protocol == ProtocolType.SSH2))
                _cMenTreeToolsTransferFile.Enabled = false;

            _cMenTreeToolsWakeOnLan.Enabled = WakeOnLan.IsValidMacAddress(connectionInfo.MacAddress);

            _cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
            _cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
            _cMenTreeToolsSort.Enabled = false;
            _cMenTreeDuplicate.Enabled = false;
            _cMenTreeCreateLink.Enabled = false;
            _cMenTreeRename.Enabled = false;
            _cMenTreeDelete.Enabled = false;
            _cMenTreeMoveUp.Enabled = false;
            _cMenTreeMoveDown.Enabled = false;
            _cMenTreeImport.Enabled = false;
            _cMenTreeExportFile.Enabled = false;
            _cMenTreeConnectWithOptionsAlternativeAddress.Enabled = false;
            _cMenTreeConnectWithOptionsViewOnly.Enabled = false;
            _cMenTreeApplyInheritanceToChildren.Enabled = false;
            _cMenTreeApplyDefaultInheritance.Enabled = false;
            _cMenTreeProperties.Enabled = false;
            _cMenTreeConfigureDynamicSource.Visible = false;
            _cMenTreeRefreshDynamicSource.Visible = false;
        }

        internal void ShowHideMenuItemsForConnectionNode(ConnectionInfo connectionInfo)
        {
            if (connectionInfo.OpenConnections.Count == 0)
            {
                _cMenTreeDisconnect.Enabled = false;
                _cMenTreeReconnect.Enabled = false;
                _cMenTreeTypePassword.Enabled = false;
                _cMenTreeTypeClipboard.Enabled = false;
            }

            if (!(connectionInfo.Protocol == ProtocolType.SSH1 | connectionInfo.Protocol == ProtocolType.SSH2))
                _cMenTreeToolsTransferFile.Enabled = false;

            _cMenTreeToolsWakeOnLan.Enabled = WakeOnLan.IsValidMacAddress(connectionInfo.MacAddress);

            if (!(connectionInfo.Protocol == ProtocolType.RDP))
            {
                _cMenTreeConnectWithOptionsConnectInFullscreen.Enabled = false;
                _cMenTreeConnectWithOptionsConnectToConsoleSession.Enabled = false;
            }

            if (connectionInfo.Protocol == ProtocolType.IntApp)
                _cMenTreeConnectWithOptionsNoCredentials.Enabled = false;

            if (connectionInfo.Protocol != ProtocolType.RDP && connectionInfo.Protocol != ProtocolType.VNC)
                _cMenTreeConnectWithOptionsViewOnly.Enabled = false;

            _cMenTreeConnectWithOptionsAlternativeAddress.Enabled = !string.IsNullOrWhiteSpace(connectionInfo.AlternativeAddress);
            _cMenTreeApplyInheritanceToChildren.Enabled = false;
            _cMenTreeConfigureDynamicSource.Visible = false;
            _cMenTreeRefreshDynamicSource.Visible = false;
        }

        internal void DisableShortcutKeys()
        {
            _cMenTreeConnect.ShortcutKeys = Keys.None;
            _cMenTreeDuplicate.ShortcutKeys = Keys.None;
            _cMenTreeRename.ShortcutKeys = Keys.None;
            _cMenTreeDelete.ShortcutKeys = Keys.None;
            _cMenTreeMoveUp.ShortcutKeys = Keys.None;
            _cMenTreeMoveDown.ShortcutKeys = Keys.None;
        }

        internal void EnableShortcutKeys()
        {
            _cMenTreeConnect.ShortcutKeys = ((Keys.Control | Keys.Shift) | Keys.C);
            _cMenTreeDuplicate.ShortcutKeys = Keys.Control | Keys.D;
            _cMenTreeRename.ShortcutKeys = Keys.F2;
            _cMenTreeDelete.ShortcutKeys = Keys.Delete;
            _cMenTreeMoveUp.ShortcutKeys = Keys.Control | Keys.Up;
            _cMenTreeMoveDown.ShortcutKeys = Keys.Control | Keys.Down;
        }

        private static void EnableMenuItemsRecursive(ToolStripItemCollection items, bool enable = true)
        {
            foreach (ToolStripItem item in items)
            {
                if (item is not ToolStripMenuItem menuItem)
                    continue;

                menuItem.Enabled = enable;
                if (menuItem.HasDropDownItems)
                {
                    EnableMenuItemsRecursive(menuItem.DropDownItems, enable);
                }
            }
        }

        private void AddExternalApps()
        {
            try
            {
                ResetExternalAppMenu();

                foreach (ExternalTool extA in Runtime.ExternalToolsService.ExternalTools)
                {
                    ToolStripMenuItem menuItem = new()
                    {
                        Text = extA.DisplayName,
                        Tag = extA,
                        Image = extA.Image
                    };

                    menuItem.Click += OnExternalToolClicked;
                    _cMenTreeToolsExternalApps.DropDownItems.Add(menuItem);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "cMenTreeTools_DropDownOpening failed (UI.Window.ConnectionTreeWindow)",
                                                                ex);
            }
        }

        private void ResetExternalAppMenu()
        {
            if (_cMenTreeToolsExternalApps.DropDownItems.Count <= 0) return;
            for (int i = _cMenTreeToolsExternalApps.DropDownItems.Count - 1; i >= 0; i--)
                _cMenTreeToolsExternalApps.DropDownItems[i].Dispose();

            _cMenTreeToolsExternalApps.DropDownItems.Clear();
        }

        #region Click handlers

        private void OpenSelectedConnections(ConnectionInfo.Force force)
        {
            foreach (ConnectionInfo node in _connectionTree.GetSelectedNodes())
            {
                if (node is ContainerInfo container)
                    Runtime.ConnectionInitiator.OpenConnection(container, force);
                else
                    Runtime.ConnectionInitiator.OpenConnection(node, force);
            }
        }

        private void OnConnectClicked(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.DoNotJump);
        }

        private void OnConnectWithOptionsDialogClicked(object sender, EventArgs e)
        {
            var selectedNode = _connectionTree.SelectedNode;
            if (selectedNode == null || selectedNode is ContainerInfo) return;

            using (var frm = new UI.Forms.FrmConnectWithOptions(selectedNode))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    Runtime.ConnectionInitiator.OpenConnection(frm.ConnectionInfo, ConnectionInfo.Force.DoNotJump);
                }
            }
        }

        private void OnConnectToConsoleSessionClicked(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.UseConsoleSession | ConnectionInfo.Force.DoNotJump);
        }

        private void OnDontConnectToConsoleSessionClicked(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.DontUseConsoleSession | ConnectionInfo.Force.DoNotJump);
        }

        private void OnConnectInFullscreenClicked(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.Fullscreen | ConnectionInfo.Force.DoNotJump);
        }

        private void OnConnectWithNoCredentialsClick(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.NoCredentials);
        }

        private void OnChoosePanelBeforeConnectingClicked(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.OverridePanel | ConnectionInfo.Force.DoNotJump);
        }

        private void OnConnectUsingAlternativeAddressClick(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.UseAlternativeAddress | ConnectionInfo.Force.DoNotJump);
        }

        private void ConnectWithOptionsViewOnlyOnClick(object sender, EventArgs e)
        {
            OpenSelectedConnections(ConnectionInfo.Force.ViewOnly);
        }

        private void OnDisconnectClicked(object sender, EventArgs e)
        {
            foreach (ConnectionInfo node in _connectionTree.GetSelectedNodes())
            {
                DisconnectConnectionInternal(node);
            }
        }

        private void OnReconnectClicked(object sender, EventArgs e)
        {
            foreach (ConnectionInfo node in _connectionTree.GetSelectedNodes())
            {
                if (DisconnectConnectionInternal(node))
                {
                    if (node is ContainerInfo container)
                        Runtime.ConnectionInitiator.OpenConnection(container, ConnectionInfo.Force.DoNotJump);
                    else
                        Runtime.ConnectionInitiator.OpenConnection(node, ConnectionInfo.Force.DoNotJump);
                }
            }
        }

        private void OnTypePasswordClicked(object sender, EventArgs e)
        {
            if (_connectionTree.SelectedNode is ConnectionInfo connectionInfo && connectionInfo.OpenConnections.Count > 0)
            {
                var protocol = connectionInfo.OpenConnections[connectionInfo.OpenConnections.Count - 1];
                if (protocol != null)
                {
                    protocol.Focus();
                    string password = connectionInfo.Password;
                    if (!string.IsNullOrEmpty(password))
                    {
                        System.Windows.Forms.SendKeys.SendWait(EscapeSendKeys(password));
                    }
                }
            }
        }

        private void OnTypeClipboardClicked(object sender, EventArgs e)
        {
            if (_connectionTree.SelectedNode is ConnectionInfo connectionInfo && connectionInfo.OpenConnections.Count > 0)
            {
                var protocol = connectionInfo.OpenConnections[connectionInfo.OpenConnections.Count - 1];
                if (protocol != null)
                {
                    protocol.Focus();
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(text))
                        {
                            System.Windows.Forms.SendKeys.SendWait(EscapeSendKeys(text));
                        }
                    }
                }
            }
        }

        private string EscapeSendKeys(string str)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                if (c == '+' || c == '^' || c == '%' || c == '~' || c == '(' || c == ')' || c == '{' || c == '}' || c == '[' || c == ']')
                {
                    sb.Append("{");
                    sb.Append(c);
                    sb.Append("}");
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void DisconnectConnection(ConnectionInfo connectionInfo)
        {
            DisconnectConnectionInternal(connectionInfo);
        }

        private bool DisconnectConnectionInternal(ConnectionInfo connectionInfo)
        {
            try
            {
                if (connectionInfo == null) return false;
                
                // Check if confirmation is needed based on settings
                if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.All)
                {
                    string confirmMessage = string.Format(Language.ConfirmDisconnectConnection, connectionInfo.Name);
                    DialogResult result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName,
                                                        confirmMessage, "", "", "",
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
                        return false; // User cancelled the disconnect
                    }
                }
                
                ContainerInfo? nodeAsContainer = connectionInfo as ContainerInfo;
                if (nodeAsContainer != null)
                {
                    foreach (ConnectionInfo child in nodeAsContainer.Children)
                    {
                        for (int i = 0; i <= child.OpenConnections.Count - 1; i++)
                        {
                            child.OpenConnections[i]?.Disconnect();
                        }
                    }
                }
                else
                {
                    for (int i = 0; i <= connectionInfo.OpenConnections.Count - 1; i++)
                    {
                        connectionInfo.OpenConnections[i]?.Disconnect();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "DisconnectConnection (UI.Window.ConnectionTreeWindow) failed",
                                                                ex);
                return true;
            }
        }

        private void OnTransferFileClicked(object sender, EventArgs e)
        {
            SshTransferFile();
        }

        private void OnWakeOnLanClicked(object sender, EventArgs e)
        {
            if (_connectionTree.SelectedNode == null)
                return;

            WakeOnLan.TrySendMagicPacket(_connectionTree.SelectedNode.MacAddress);
        }

        public void SshTransferFile()
        {
            try
            {
                AppWindows.Show(WindowType.SSHTransfer);
                AppWindows.SshtransferForm.Hostname = _connectionTree.SelectedNode.Hostname;
                AppWindows.SshtransferForm.Username = _connectionTree.SelectedNode.Username;
                //App.Windows.SshtransferForm.Password = _connectionTree.SelectedNode.Password.ConvertToUnsecureString();
                AppWindows.SshtransferForm.Password = _connectionTree.SelectedNode.Password;
                AppWindows.SshtransferForm.Port = Convert.ToString(_connectionTree.SelectedNode.Port);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "SSHTransferFile (UI.Window.ConnectionTreeWindow) failed",
                                                                ex);
            }
        }

        private void OnDuplicateClicked(object sender, EventArgs e)
        {
            _connectionTree.DuplicateSelectedNode();
        }

        private void OnCreateLinkClicked(object sender, EventArgs e)
        {
            _connectionTree.CreateLinkToSelectedNode();
        }

        private void OnRenameClicked(object sender, EventArgs e)
        {
            _connectionTree.RenameSelectedNode();
        }

        private void OnDeleteClicked(object sender, EventArgs e)
        {
            _connectionTree.DeleteSelectedNode();
        }

        private void OnCopyHostnameClicked(object sender, EventArgs e)
        {
            _connectionTree.CopyHostnameSelectedNode(new WindowsClipboard());
        }

        private void OnPropertiesClicked(object sender, EventArgs e)
        {
            if (_connectionTree.SelectedNode == null) return;
            AppWindows.ConfigForm.SelectedTreeNode = _connectionTree.SelectedNode;
            AppWindows.ConfigForm.ShowConnectionProperties();
            AppWindows.ConfigForm.Show();
            AppWindows.ConfigForm.Activate();
        }

        private void OnLoadAdditionalFileClicked(object sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new();
            openFileDialog.Filter = "mRemoteNG Connections Files (*.xml)|*.xml|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                Runtime.ConnectionsService.LoadAdditionalConnectionFile(openFileDialog.FileName);
            }
        }

        private void OnImportFileClicked(object sender, EventArgs e)
        {
            ContainerInfo? selectedNodeAsContainer;
            if (_connectionTree.SelectedNode == null)
                selectedNodeAsContainer = Runtime.ConnectionsService.ConnectionTreeModel?.RootNodes.First();
            else
                selectedNodeAsContainer =
                    _connectionTree.SelectedNode as ContainerInfo ?? _connectionTree.SelectedNode.Parent;
            if (selectedNodeAsContainer == null) return;
            Import.ImportFromFile(selectedNodeAsContainer);
        }

        private void OnImportPuttyClicked(object sender, EventArgs e)
        {
            ContainerInfo? selectedNodeAsContainer;
            if (_connectionTree.SelectedNode == null)
                selectedNodeAsContainer = Runtime.ConnectionsService.ConnectionTreeModel?.RootNodes.First();
            else
                selectedNodeAsContainer =
                    _connectionTree.SelectedNode as ContainerInfo ?? _connectionTree.SelectedNode.Parent;
            if (selectedNodeAsContainer == null) return;
            Import.ImportFromPutty(selectedNodeAsContainer);
        }

        private void OnImportRemoteDesktopManagerClicked(object sender, EventArgs e)
        {
            ContainerInfo? selectedNodeAsContainer;
            if (_connectionTree.SelectedNode == null)
                selectedNodeAsContainer = Runtime.ConnectionsService.ConnectionTreeModel?.RootNodes.First();
            else
                selectedNodeAsContainer =
                    _connectionTree.SelectedNode as ContainerInfo ?? _connectionTree.SelectedNode.Parent;
            if (selectedNodeAsContainer == null) return;
            Import.ImportFromRemoteDesktopManagerCsv(selectedNodeAsContainer);
        }

        private void OnImportActiveDirectoryClicked(object sender, EventArgs e)
        {
            AppWindows.Show(WindowType.ActiveDirectoryImport);
        }

        private void OnImportPortScanClicked(object sender, EventArgs e)
        {
            AppWindows.Show(WindowType.PortScan);
        }

        private void OnImportGuacamoleClicked(object sender, EventArgs e)
        {
            ContainerInfo? selectedNodeAsContainer;
            if (_connectionTree.SelectedNode == null)
                selectedNodeAsContainer = Runtime.ConnectionsService.ConnectionTreeModel?.RootNodes.First();
            else
                selectedNodeAsContainer =
                    _connectionTree.SelectedNode as ContainerInfo ?? _connectionTree.SelectedNode.Parent;
            if (selectedNodeAsContainer == null) return;

            using (var frm = new UI.Forms.FrmGuacamoleImport(selectedNodeAsContainer))
            {
                frm.ShowDialog();
            }
        }

        private void OnExportFileClicked(object sender, EventArgs e)
        {
            var model = Runtime.ConnectionsService.ConnectionTreeModel;
            if (model == null) return;
            Export.ExportToFile(_connectionTree.SelectedNode, model);
        }

        private void OnAddConnectionClicked(object sender, EventArgs e)
        {
            _connectionTree.AddConnection();
        }

        private void OnAddFolderClicked(object sender, EventArgs e)
        {
            _connectionTree.AddFolder();
        }

        private void OnSortAscendingClicked(object sender, EventArgs e)
        {
            _connectionTree.SortSelectedNodesRecursive(ListSortDirection.Ascending);
        }

        private void OnSortDescendingClicked(object sender, EventArgs e)
        {
            _connectionTree.SortSelectedNodesRecursive(ListSortDirection.Descending);
        }

        private void OnMoveUpClicked(object sender, EventArgs e)
        {
            _connectionTree.MoveSelectedNodesUp();
        }

        private void OnMoveDownClicked(object sender, EventArgs e)
        {
            _connectionTree.MoveSelectedNodesDown();
        }

        private void OnExternalToolClicked(object sender, EventArgs e)
        {
            if (((ToolStripMenuItem)sender).Tag is ExternalTool externalTool)
                StartExternalApp(externalTool);
        }

        private void StartExternalApp(ExternalTool externalTool)
        {
            try
            {
                foreach (ConnectionInfo node in _connectionTree.GetSelectedNodes())
                {
                    TreeNodeType nodeType = node.GetTreeNodeType();
                    if (nodeType == TreeNodeType.Connection ||
                        nodeType == TreeNodeType.PuttySession ||
                        nodeType == TreeNodeType.Container)
                        externalTool.Start(node);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "cMenTreeToolsExternalAppsEntry_Click failed (UI.Window.ConnectionTreeWindow)",
                                                                ex);
            }
        }

        private void OnConfigureDynamicSourceClicked(object sender, EventArgs e)
        {
            if (!(_connectionTree.SelectedNode is ContainerInfo container))
                return;

            using (var frm = new UI.Forms.FrmDynamicFolderConfig(container))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                     if (container.DynamicSource != DynamicSourceType.None)
                     {
                         Runtime.DynamicFolderManager.RefreshFolder(container);
                     }
                }
            }
        }

        private void OnRefreshDynamicSourceClicked(object sender, EventArgs e)
        {
            if (!(_connectionTree.SelectedNode is ContainerInfo container))
                return;
            
            Runtime.DynamicFolderManager.RefreshFolder(container);
        }

        private void OnAddRootFolderClicked(object sender, EventArgs e)
        {
            _connectionTree.AddRootFolder();
        }

        private void OnApplyInheritanceToChildrenClicked(object sender, EventArgs e)
        {
            if (!(_connectionTree.SelectedNode is ContainerInfo container))
                return;

            container.ApplyInheritancePropertiesToChildren();
        }

        private void OnApplyDefaultInheritanceClicked(object sender, EventArgs e)
        {
            if (_connectionTree.SelectedNode == null)
                return;

            DefaultConnectionInheritance.Instance.SaveTo(_connectionTree.SelectedNode.Inheritance);
        }

        private void OnOptionsClicked(object sender, EventArgs e)
        {
            AppWindows.Show(WindowType.Options);
        }

        #endregion
    }
}
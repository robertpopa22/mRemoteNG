using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using mRemoteNG.App;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Properties;
using System.Security;
using mRemoteNG.Themes;
using mRemoteNG.Tools;
using mRemoteNG.Tools.Clipboard;
using mRemoteNG.Tree;
using mRemoteNG.Tree.ClickHandlers;
using mRemoteNG.Tree.Root;
using mRemoteNG.Resources.Language;
using mRemoteNG.Security;
using mRemoteNG.UI.Forms;
using System.Runtime.Versioning;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    [SupportedOSPlatform("windows")]
    public partial class ConnectionTree : TreeListView, IConnectionTree
    {
        private readonly ConnectionTreeDragAndDropHandler _dragAndDropHandler = new();
        private readonly PuttySessionsManager _puttySessionsManager = PuttySessionsManager.Instance;
        private readonly StatusImageList _statusImageList = new();
        private ThemeManager _themeManager;

        private readonly ConnectionTreeSearchTextFilter _connectionTreeSearchTextFilter = new();
        private System.Collections.IEnumerable? _preFilterExpandedObjects;

        private bool _nodeInEditMode;
        private bool _allowEdit;
        private ConnectionContextMenu _contextMenu = null!;
        private ConnectionTreeModel? _connectionTreeModel;
        private List<ConnectionInfo> _clipboardNodes = new();

        public ConnectionInfo SelectedNode => (ConnectionInfo)SelectedObject;

        public NodeSearcher? NodeSearcher { get; private set; }

        public IConfirm<ConnectionInfo> NodeDeletionConfirmer { get; set; } = new AlwaysConfirmYes();

        public IEnumerable<IConnectionTreeDelegate> PostSetupActions { get; set; } = Array.Empty<IConnectionTreeDelegate>();

        public ITreeNodeClickHandler<ConnectionInfo> DoubleClickHandler { get; set; } = new TreeNodeCompositeClickHandler();

        public ITreeNodeClickHandler<ConnectionInfo> SingleClickHandler { get; set; } = new TreeNodeCompositeClickHandler();

        public ITreeNodeClickHandler<ConnectionInfo> MiddleClickHandler { get; set; } = new TreeNodeCompositeClickHandler();

        public ConnectionTreeModel ConnectionTreeModel
        {
            get { return _connectionTreeModel!; }
            set
            {
                if (_connectionTreeModel == value)
                {
                    return;
                }

                if (_connectionTreeModel != null)
                    UnregisterModelUpdateHandlers(_connectionTreeModel);
                _connectionTreeModel = value;
                PopulateTreeView(value);
            }
        }

        public ConnectionTree()
        {
            InitializeComponent();
            SetupConnectionTreeView();
            UseOverlays = false;
            UseWaitCursorWhenExpanding = false;
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ThemeManagerOnThemeChanged;
            ApplyTheme();
        }

        private void ThemeManagerOnThemeChanged()
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (!_themeManager.ActiveAndExtended)
                return;

            ExtendedColorPalette? themePalette = _themeManager.ActiveTheme.ExtendedPalette;
            if (themePalette == null) return;

            BackColor = themePalette.getColor("TreeView_Background");
            ForeColor = themePalette.getColor("TreeView_Foreground");
            SelectedBackColor = themePalette.getColor("Treeview_SelectedItem_Active_Background");
            SelectedForeColor = themePalette.getColor("Treeview_SelectedItem_Active_Foreground");
            UnfocusedSelectedBackColor = themePalette.getColor("Treeview_SelectedItem_Inactive_Background");
            UnfocusedSelectedForeColor = themePalette.getColor("Treeview_SelectedItem_Inactive_Foreground");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                components?.Dispose();
                _statusImageList?.Dispose();

                _themeManager.ThemeChanged -= ThemeManagerOnThemeChanged;
            }

            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_MOUSEACTIVATE = 0x0021;
            const int WM_LBUTTONDOWN = 0x0201;

            if (DevLog.IsEnabled && (m.Msg == WM_MOUSEACTIVATE || m.Msg == WM_LBUTTONDOWN || m.Msg == 0x0007))
                DevLog.Write($"Msg=0x{m.Msg:X4} Focused={Focused} SelectedObject={SelectedObject}");

            if (m.Msg == WM_MOUSEACTIVATE)
            {
                // Identify the click target BEFORE Focus() triggers WM_SETFOCUS,
                // so the scroll-restore handler knows not to snap back (#68).
                System.Drawing.Point clientPt = PointToClient(MousePosition);
                OlvListViewHitTestInfo hit = OlvHitTest(clientPt.X, clientPt.Y);
                _pendingClickTarget = hit.Item?.RowObject as ConnectionInfo;
                DevLog.Write($"WM_MOUSEACTIVATE: pendingClickTarget={_pendingClickTarget?.Name}");

                // Freeze painting during the focus battle to avoid visible scroll/selection flicker
                const int WM_SETREDRAW = 0x000B;
                NativeMethods.SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                Focus();
                BeginInvoke(() =>
                {
                    NativeMethods.SendMessage(Handle, WM_SETREDRAW, (IntPtr)1, IntPtr.Zero);
                    Invalidate(true);
                });
            }

            // WM_SETFOCUS: previously had scroll-restore logic (#1925) but it
            // breaks tree navigation when RDP ActiveX controls cause focus
            // oscillation — the scroll snaps back and makes the tree unusable.
            // Removed: let ListView handle focus scrolling naturally (#68).
            base.WndProc(ref m);
        }

        #region ConnectionTree Setup

        private void SetupConnectionTreeView()
        {
            SetSmallImageList(_statusImageList.ImageList);
            AddColumns(_statusImageList.ImageGetter);
            LinkModelToView();
            _contextMenu = new ConnectionContextMenu(this);
            ContextMenuStrip = _contextMenu;
            SetupDropSink();
            SetEventHandlers();
        }

        private void AddColumns(ImageGetterDelegate imageGetterDelegate)
        {
            Columns.Add(new NameColumn(imageGetterDelegate));
            Columns.Add(new DescriptionColumn());
        }

        private void LinkModelToView()
        {
            CanExpandGetter = item =>
            {
                ContainerInfo? itemAsContainer = item as ContainerInfo;
                return itemAsContainer?.Children.Count > 0;
            };
            ChildrenGetter = item => ((ContainerInfo)item).Children;
        }

        private void SetupDropSink()
        {
            DropSink = new SimpleDropSink
            {
                CanDropBetween = true
            };
        }

        private void SetEventHandlers()
        {
            Collapsed += (sender, args) =>
            {
                if (args.Model is not ContainerInfo container) return;
                container.IsExpanded = false;
                AutoResizeColumn(Columns[0]);
            };
            Expanded += (sender, args) =>
            {
                if (args.Model is not ContainerInfo container) return;
                container.IsExpanded = true;
                AutoResizeColumn(Columns[0]);
            };
            Expanding += OnExpanding;
            SelectionChanged += TvConnections_AfterSelect;
            MouseDown += OnMouse_Down;
            MouseDoubleClick += OnMouse_DoubleClick;
            MouseClick += OnMouse_SingleClick;
            MouseClick += OnMouse_MiddleClick;
            CellToolTipShowing += TvConnections_CellToolTipShowing;
            ModelCanDrop += _dragAndDropHandler.OnModelCanDrop;
            ModelDropped += _dragAndDropHandler.OnModelDropped;
            BeforeLabelEdit += OnBeforeLabelEdit;
            AfterLabelEdit += OnAfterLabelEdit;
            FormatCell += ConnectionTree_FormatCell;
        }

        private void OnExpanding(object? sender, TreeBranchExpandingEventArgs e)
        {
            if (e.Model is not ContainerInfo container) return;
            if (string.IsNullOrEmpty(container.ContainerPassword)) return;
            if (container.IsUnlocked) return;

            using FrmPassword passwordForm = new(container.Name, false);
            if (passwordForm.ShowDialog() == DialogResult.OK)
            {
                Optional<SecureString> key = passwordForm.GetKey();
                if (key.Any() && key.First().ConvertToUnsecureString() == container.ContainerPassword)
                {
                    container.IsUnlocked = true;
                }
                else
                {
                    e.Canceled = true;
                    MessageBox.Show("Incorrect password.", "Security", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                e.Canceled = true;
            }
        }

        /// <summary>
        /// Resizes the given column to ensure that all content is shown
        /// </summary>
        private void AutoResizeColumn(ColumnHeader column)
        {
            if (InvokeRequired)
            {
                Invoke((MethodInvoker)(() => AutoResizeColumn(column)));
                return;
            }

            int longestIndentationAndTextWidth = int.MinValue;
            int horizontalScrollOffset = LowLevelScrollPosition.X;
            const int padding = 10;

            for (int i = 0; i < Items.Count; i++)
            {
                int rowIndentation = Items[i].Position.X;
                int rowTextWidth = TextRenderer.MeasureText(Items[i].Text, Font).Width;

                longestIndentationAndTextWidth = Math.Max(rowIndentation + rowTextWidth, longestIndentationAndTextWidth);
            }

            column.Width = longestIndentationAndTextWidth + SmallImageSize.Width + horizontalScrollOffset + padding;
        }

        private void PopulateTreeView(ConnectionTreeModel newModel)
        {
            BeginUpdate();
            try
            {
                SetObjects(newModel.RootNodes);
                RegisterModelUpdateHandlers(newModel);
                NodeSearcher = new NodeSearcher(newModel);
                ExecutePostSetupActions();
            }
            finally
            {
                EndUpdate();
            }
            AutoResizeColumn(Columns[0]);
        }

        private void RegisterModelUpdateHandlers(ConnectionTreeModel newModel)
        {
            _puttySessionsManager.PuttySessionsCollectionChanged += OnPuttySessionsCollectionChanged;
            newModel.CollectionChanged += HandleCollectionChanged;
            newModel.PropertyChanged += HandleCollectionPropertyChanged;
        }

        private void UnregisterModelUpdateHandlers(ConnectionTreeModel oldConnectionTreeModel)
        {
            _puttySessionsManager.PuttySessionsCollectionChanged -= OnPuttySessionsCollectionChanged;

            if (oldConnectionTreeModel == null)
                return;

            oldConnectionTreeModel.CollectionChanged -= HandleCollectionChanged;
            oldConnectionTreeModel.PropertyChanged -= HandleCollectionPropertyChanged;
        }

        private void OnPuttySessionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RefreshObjects(GetRootPuttyNodes().ToList());
        }

        private void HandleCollectionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // for some reason property changed events are getting triggered twice for each changed property. should be just once. cant find source of duplication
            // Removed "TO DO" from above comment. Per #142 it apperas that this no longer occurs with ObjectListView 2.9.1
            string? property = propertyChangedEventArgs.PropertyName;
            if (property != nameof(ConnectionInfo.Name)
             && property != nameof(ConnectionInfo.OpenConnections)
             && property != nameof(ConnectionInfo.Icon)
             && property != nameof(ConnectionInfo.Description)
             && property != nameof(ConnectionInfo.HostReachabilityStatus))
            {
                return;
            }

            if (sender is not ConnectionInfo senderAsConnectionInfo)
                return;

            // HostStatusMonitor fires from background thread — marshal to UI
            if (InvokeRequired)
            {
                BeginInvoke(() =>
                {
                    RefreshObject(senderAsConnectionInfo);
                    AutoResizeColumn(Columns[0]);
                });
                return;
            }

            RefreshObject(senderAsConnectionInfo);
            AutoResizeColumn(Columns[0]);
        }

        private void ExecutePostSetupActions()
        {
            foreach (IConnectionTreeDelegate action in PostSetupActions)
            {
                action.Execute(this);
            }
        }

        #endregion

        #region ConnectionTree Behavior

        public RootNodeInfo GetRootConnectionNode()
        {
            return (RootNodeInfo)ConnectionTreeModel.RootNodes.First(item => item is RootNodeInfo);
        }

        public new void Invoke(Action action)
        {
            Invoke((Delegate)action);
        }

        public void InvokeExpand(object model)
        {
            Invoke(() => Expand(model));
        }

        public void InvokeRebuildAll(bool preserveState)
        {
            Invoke(() => RebuildAll(preserveState));
        }

        public IEnumerable<RootPuttySessionsNodeInfo> GetRootPuttyNodes()
        {
            return Objects.OfType<RootPuttySessionsNodeInfo>();
        }

        private static bool IsReadOnly => Properties.OptionsDBsPage.Default.SQLReadOnly;

        public void AddConnection()
        {
            if (IsReadOnly) return;
            try
            {
                AddNode(new ConnectionInfo());
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("UI.Window.Tree.AddConnection() failed.", ex);
            }
        }

        public void AddFolder()
        {
            if (IsReadOnly) return;
            try
            {
                AddNode(new ContainerInfo());
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ErrorAddFolderFailed, ex);
            }
        }

        public void AddEntity()
        {
            if (IsReadOnly) return;
            try
            {
                ContainerInfo entity = new() { IsEntity = true, Name = "New Entity" };
                AddNode(entity);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Failed to add entity", ex);
            }
        }

        public void AddRootFolder()
        {
            if (IsReadOnly) return;
            try
            {
                ContainerInfo newFolder = new();
                newFolder.IsRoot = true;
                DefaultConnectionInfo.Instance.SaveTo(newFolder);
                DefaultConnectionInheritance.SaveTo(newFolder.Inheritance);
                if (Settings.Default.InhDefaultEverythingInherited)
                    newFolder.Inheritance.TurnOnInheritanceCompletely();

                ConnectionTreeModel.AddRootNode(newFolder);

                SelectObject(newFolder, true);
                EnsureModelVisible(newFolder);
                _allowEdit = true;
                SelectedItem.BeginEdit();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ErrorAddFolderFailed, ex);
            }
        }

        private void AddNode(ConnectionInfo newNode)
        {
            if (SelectedNode?.GetTreeNodeType() == TreeNodeType.PuttyRoot ||
                SelectedNode?.GetTreeNodeType() == TreeNodeType.PuttySession)
                return;

            // the new node will survive filtering if filtering is active
            _connectionTreeSearchTextFilter.SpecialInclusionList.Add(newNode);

            // use root node if no node is selected
            ConnectionInfo parentNode = SelectedNode ?? GetRootConnectionNode();
            DefaultConnectionInfo.Instance.SaveTo(newNode);
            DefaultConnectionInheritance.SaveTo(newNode.Inheritance);
            if (Settings.Default.InhDefaultEverythingInherited)
                newNode.Inheritance.TurnOnInheritanceCompletely();
            ContainerInfo? selectedContainer = parentNode as ContainerInfo;
            ContainerInfo? parent = selectedContainer ?? parentNode.Parent;
            if (parent == null) return;
            newNode.SetParent(parent);
            // Default the new node's Panel to the parent folder's Panel (#1982)
            if (!newNode.Inheritance.Panel)
            {
                string parentPanel = parent.Panel;
                if (!string.IsNullOrEmpty(parentPanel))
                    newNode.Panel = parentPanel;
            }
            Expand(parent);
            SelectObject(newNode, true);
            EnsureModelVisible(newNode);
            _allowEdit = true;
            SelectedItem.BeginEdit();
        }

        internal List<ConnectionInfo> GetSelectedNodes()
        {
            List<ConnectionInfo> selectedNodes = SelectedObjects?.OfType<ConnectionInfo>().Distinct().ToList() ?? [];

            if (selectedNodes.Count == 0 && SelectedNode != null)
                selectedNodes.Add(SelectedNode);

            return selectedNodes;
        }

        private static void ExecuteInBatchedSaveContext(Action action)
        {
            Runtime.ConnectionsService.BeginBatchingSaves();

            try
            {
                action();
            }
            finally
            {
                Runtime.ConnectionsService.EndBatchingSaves();
            }
        }

        public void DuplicateSelectedNode()
        {
            if (IsReadOnly) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ConnectionInfo selectedNode in GetSelectedNodes())
                {
                    TreeNodeType selectedNodeType = selectedNode.GetTreeNodeType();
                    if (selectedNodeType != TreeNodeType.Connection && selectedNodeType != TreeNodeType.Container)
                        continue;

                    ConnectionInfo newNode = selectedNode.Clone();
                    if (selectedNode.Parent == null) continue;
                    selectedNode.Parent.AddChildBelow(newNode, selectedNode);
                    newNode.Parent?.SetChildBelow(newNode, selectedNode);
                }
            });
        }

        public bool HasClipboardNodes => _clipboardNodes.Count > 0;

        public void CopySelectedNodes()
        {
            _clipboardNodes = GetSelectedNodes()
                .Where(n =>
                {
                    TreeNodeType type = n.GetTreeNodeType();
                    return type == TreeNodeType.Connection || type == TreeNodeType.Container;
                })
                .ToList();
        }

        public void PasteNodes()
        {
            if (IsReadOnly) return;
            if (_clipboardNodes.Count == 0) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ConnectionInfo copiedNode in _clipboardNodes)
                {
                    ConnectionInfo newNode = copiedNode.Clone();
                    if (SelectedNode is ContainerInfo container)
                    {
                        container.AddChild(newNode);
                    }
                    else if (SelectedNode?.Parent != null)
                    {
                        SelectedNode.Parent.AddChildBelow(newNode, SelectedNode);
                    }
                }
            });
        }

        public void CreateLinkToSelectedNode()
        {
            if (IsReadOnly) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ConnectionInfo selectedNode in GetSelectedNodes())
                {
                    if (selectedNode.GetTreeNodeType() != TreeNodeType.Connection)
                        continue;

                    if (selectedNode.Parent == null)
                        continue;

                    ConnectionInfo newNode = selectedNode.Clone();
                    ConnectionInfo sourceNode = ConnectionTreeModel.ResolveLinkedConnection(selectedNode) ?? selectedNode;
                    newNode.LinkedConnectionId = sourceNode.ConstantID;

                    selectedNode.Parent.AddChildBelow(newNode, selectedNode);
                    newNode.Parent?.SetChildBelow(newNode, selectedNode);
                }
            });
        }

        public void RenameSelectedNode()
        {
            if (IsReadOnly) return;
            if (SelectedItem == null) return;
            _allowEdit = true;
            SelectedItem.BeginEdit();
        }

        public void DeleteSelectedNode()
        {
            if (IsReadOnly) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ConnectionInfo selectedNode in GetSelectedNodes())
                {
                    if (selectedNode is RootNodeInfo rootNode)
                    {
                        if (ConnectionTreeModel.RootNodes.Count > 1)
                        {
                            if (!NodeDeletionConfirmer.Confirm(selectedNode)) return;
                            ConnectionTreeModel.RemoveRootNode(rootNode);
                        }
                        continue;
                    }

                    if (selectedNode is PuttySessionInfo) continue;
                    if (selectedNode.Parent == null) continue;
                    if (!NodeDeletionConfirmer.Confirm(selectedNode)) return;
                    mRemoteNG.Tree.ConnectionTreeModel.DeleteNode(selectedNode);
                }
            });
        }

        /// <summary>
        /// Copies the Hostname of the selected connection (or the Name of
        /// the selected container) to the given <see cref="IClipboard"/>.
        /// </summary>
        /// <param name="clipboard"></param>
        public void CopyHostnameSelectedNode(IClipboard clipboard)
        {
            if (SelectedNode == null)
                return;

            string textToCopy = SelectedNode.IsContainer ? SelectedNode.Name : SelectedNode.Hostname;

            if (string.IsNullOrEmpty(textToCopy))
                return;

            clipboard.SetText(textToCopy);
        }

        public void SortRecursive(ConnectionInfo sortTarget, ListSortDirection sortDirection)
        {
            if (IsReadOnly) return;
            sortTarget ??= GetRootConnectionNode();

            Runtime.ConnectionsService.BeginBatchingSaves();

            if (sortTarget is ContainerInfo sortTargetAsContainer)
                sortTargetAsContainer.SortRecursive(sortDirection);
            else
                SelectedNode?.Parent?.SortRecursive(sortDirection);

            Runtime.ConnectionsService.EndBatchingSaves();
        }

        public void SortSelectedNodesRecursive(ListSortDirection sortDirection)
        {
            if (IsReadOnly) return;
            List<ContainerInfo> sortTargets = GetSelectedNodes()
                .Select(selectedNode => selectedNode as ContainerInfo ?? selectedNode.Parent)
                .Where(sortTarget => sortTarget != null)
                .Distinct()
                .Cast<ContainerInfo>()
                .ToList();

            if (sortTargets.Count == 0)
            {
                SortRecursive(SelectedNode, sortDirection);
                return;
            }

            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ContainerInfo sortTarget in sortTargets)
                {
                    sortTarget.SortRecursive(sortDirection);
                }
            });
        }

        public void SortSelectedNodesByTagRecursive(ListSortDirection sortDirection)
        {
            if (IsReadOnly) return;
            List<ContainerInfo> sortTargets = GetSelectedNodes()
                .Select(selectedNode => selectedNode as ContainerInfo ?? selectedNode.Parent)
                .Where(sortTarget => sortTarget != null)
                .Distinct()
                .Cast<ContainerInfo>()
                .ToList();

            if (sortTargets.Count == 0)
            {
                if (GetRootConnectionNode() is ContainerInfo root)
                    ExecuteInBatchedSaveContext(() => root.SortOnRecursive(ci => ci.EnvironmentTags ?? "", sortDirection));
                return;
            }

            ExecuteInBatchedSaveContext(() =>
            {
                foreach (ContainerInfo sortTarget in sortTargets)
                {
                    sortTarget.SortOnRecursive(ci => ci.EnvironmentTags ?? "", sortDirection);
                }
            });
        }

        public void MoveSelectedNodesUp()
        {
            if (IsReadOnly) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (IGrouping<ContainerInfo, ConnectionInfo> parentGroup in
                         GetSelectedNodes()
                             .Where(selectedNode => selectedNode.Parent != null)
                             .GroupBy(selectedNode => selectedNode.Parent!))
                {
                    foreach (ConnectionInfo selectedNode in parentGroup.OrderBy(selectedNode => parentGroup.Key.Children.IndexOf(selectedNode)))
                    {
                        parentGroup.Key.PromoteChild(selectedNode);
                    }
                }
            });
        }

        public void MoveSelectedNodesDown()
        {
            if (IsReadOnly) return;
            ExecuteInBatchedSaveContext(() =>
            {
                foreach (IGrouping<ContainerInfo, ConnectionInfo> parentGroup in
                         GetSelectedNodes()
                             .Where(selectedNode => selectedNode.Parent != null)
                             .GroupBy(selectedNode => selectedNode.Parent!))
                {
                    foreach (ConnectionInfo selectedNode in parentGroup.OrderByDescending(selectedNode => parentGroup.Key.Children.IndexOf(selectedNode)))
                    {
                        parentGroup.Key.DemoteChild(selectedNode);
                    }
                }
            });
        }

        /// <summary>
        /// Expands all tree objects and recalculates the
        /// column widths.
        /// </summary>
        public override void ExpandAll()
        {
            base.ExpandAll();
            AutoResizeColumn(Columns[0]);
        }

        /// <summary>
        /// Expands all tree objects. If filtering is active, it ensures that
        /// when the filter is removed, all objects remain expanded.
        /// </summary>
        public void UserExpandAll()
        {
            ExpandAll();

            if (IsFiltering)
            {
                // Update the pre-filter expanded state to include all containers
                // so that when the filter is cleared, everything stays expanded.
                var allContainers = new List<ContainerInfo>();
                if (ConnectionTreeModel != null)
                {
                    foreach (ContainerInfo root in ConnectionTreeModel.RootNodes)
                    {
                        allContainers.Add(root);
                        allContainers.AddRange(root.GetRecursiveChildList().OfType<ContainerInfo>());
                    }
                }
                _preFilterExpandedObjects = allContainers;
            }
        }

        /// <summary>
        /// Filters tree items based on the given <see cref="filterText"/>
        /// </summary>
        /// <param name="filterText">The text to filter by</param>
        public void ApplyFilter(string filterText)
        {
            if (!UseFiltering)
            {
                _preFilterExpandedObjects = ExpandedObjects;
            }

            UseFiltering = true;
            _connectionTreeSearchTextFilter.FilterText = filterText;
            ModelFilter = _connectionTreeSearchTextFilter;
            ExpandAll();
        }

        /// <summary>
        /// Removes all item filtering from the connection tree
        /// </summary>
        public void RemoveFilter()
        {
            UseFiltering = false;
            ResetColumnFiltering();

            if (_preFilterExpandedObjects != null)
            {
                ExpandedObjects = _preFilterExpandedObjects;
                _preFilterExpandedObjects = null;
            }
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            // disable filtering if necessary. prevents RefreshObjects from
            // throwing an exception
            bool filteringEnabled = IsFiltering;
            IModelFilter filter = ModelFilter;
            if (filteringEnabled)
            {
                ResetColumnFiltering();
            }

            if (sender is ConnectionTreeModel)
            {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        if (args.NewItems != null)
                        {
                            foreach (ConnectionInfo item in args.NewItems.OfType<ConnectionInfo>())
                            {
                                if (item.Parent != null)
                                    RefreshObject(item.Parent);
                                else
                                    AddObject(item);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        if (args.NewItems != null)
                        {
                            foreach (ConnectionInfo item in args.NewItems.OfType<ConnectionInfo>())
                            {
                                if (item.Parent != null)
                                    RefreshObject(item.Parent);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        RemoveObjects(args.OldItems);
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        if (_connectionTreeModel != null)
                            SetObjects(_connectionTreeModel.RootNodes);
                        break;
                }
            }
            else
            {
                RefreshObject(sender);

                if (sender is ConnectionInfo connectionInfo)
                {
                    ContainerInfo? parent = connectionInfo.Parent;
                    while (parent != null)
                    {
                        RefreshObject(parent);
                        parent = parent.Parent;
                    }
                }
            }

            AutoResizeColumn(Columns[0]);

            // turn filtering back on
            if (!filteringEnabled) return;
            ModelFilter = filter;
            UpdateFiltering();
        }

        protected override void UpdateFiltering()
        {
            base.UpdateFiltering();
            if (Columns.Count > 0)
                AutoResizeColumn(Columns[0]);
        }

        private void TvConnections_AfterSelect(object sender, EventArgs e)
        {
            try
            {
                var nodes = GetSelectedNodes();

                // When RDP ActiveX steals focus, ObjectListView may clear selection
                // or select the wrong node (due to auto-scroll). Use the click target
                // captured in WM_MOUSEACTIVATE before the scroll happened (#68).
                if (_pendingClickTarget != null)
                {
                    var target = _pendingClickTarget;
                    _pendingClickTarget = null;

                    if (nodes.Count == 0 || (nodes.Count == 1 && nodes[0] != target))
                    {
                        DevLog.Write($"Correcting selection → {target.Name} (was {nodes.FirstOrDefault()?.Name ?? "empty"})");
                        SelectObject(target);
                        EnsureModelVisible(target);
                        nodes = [target];
                    }
                }
                else
                {
                    _pendingClickTarget = null;
                }

                DevLog.Write($"SelectedNodes={nodes.Count}, First={nodes.FirstOrDefault()?.Name}");
                AppWindows.ConfigForm.SelectedTreeNodes = nodes;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_AfterSelect (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        private ConnectionInfo? _pendingClickTarget;

        private void OnMouse_Down(object sender, MouseEventArgs e)
        {
            DevLog.Write($"at ({e.X},{e.Y}) Button={e.Button} Focused={Focused} SelectedObject={SelectedObject}");
            if (!Focused)
                Focus();

            // _pendingClickTarget may already be set by WM_MOUSEACTIVATE (before
            // the tree scrolled). Only override if not already set (#68).
            if (_pendingClickTarget == null)
            {
                OlvListViewHitTestInfo hit = OlvHitTest(e.X, e.Y);
                _pendingClickTarget = hit.Item?.RowObject as ConnectionInfo;
            }
            DevLog.Write($"pendingClickTarget={_pendingClickTarget?.Name}");
        }

        private void OnMouse_DoubleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Clicks < 2) return;
            // ReSharper disable once NotAccessedVariable
            OLVListItem listItem = GetItemAt(mouseEventArgs.X, mouseEventArgs.Y, out _);
            if (listItem?.RowObject is not ConnectionInfo clickedNode) return;
            DoubleClickHandler.Execute(clickedNode);
        }

        private void OnMouse_SingleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Clicks > 1) return;
            if (mouseEventArgs.Button != MouseButtons.Left) return;
            // ReSharper disable once NotAccessedVariable
            OLVListItem listItem = GetItemAt(mouseEventArgs.X, mouseEventArgs.Y, out _);
            if (listItem?.RowObject is not ConnectionInfo clickedNode) return;
            SingleClickHandler.Execute(clickedNode);
        }

        private void OnMouse_MiddleClick(object sender, MouseEventArgs mouseEventArgs)
        {
            if (mouseEventArgs.Button != MouseButtons.Middle) return;
            OLVListItem listItem = GetItemAt(mouseEventArgs.X, mouseEventArgs.Y, out _);
            if (listItem?.RowObject is not ConnectionInfo clickedNode) return;
            MiddleClickHandler.Execute(clickedNode);
        }

        private void TvConnections_CellToolTipShowing(object sender, ToolTipShowingEventArgs e)
        {
            try
            {
                if (!Properties.OptionsAppearancePage.Default.ShowDescriptionTooltipsInTree)
                {
                    // setting text to null prevents the tooltip from being shown
                    e.Text = null;
                    return;
                }

                ConnectionInfo nodeProducingTooltip = (ConnectionInfo)e.Model;
                string description = nodeProducingTooltip.Description;
                string tags = nodeProducingTooltip.EnvironmentTags ?? "";
                if (!string.IsNullOrWhiteSpace(tags))
                {
                    e.Text = string.IsNullOrWhiteSpace(description)
                        ? $"Tags: {tags}"
                        : $"{description}\nTags: {tags}";
                }
                else
                {
                    e.Text = description;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(
                                                                "tvConnections_MouseMove (UI.Window.ConnectionTreeWindow) failed",
                                                                ex);
            }
        }

        private void OnBeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (_nodeInEditMode || sender is not ConnectionTree)
                return;

            if (IsReadOnly || !_allowEdit || SelectedNode is PuttySessionInfo || SelectedNode is RootPuttySessionsNodeInfo)
            {
                e.CancelEdit = true;
                return;
            }

            _nodeInEditMode = true;
            _contextMenu.DisableShortcutKeys();
        }

        private void ConnectionTree_FormatCell(object sender, FormatCellEventArgs e)
        {
            if (e.Model is not ConnectionInfo connectionInfo)
                return;

            string colorString = connectionInfo.Color;
            if (string.IsNullOrEmpty(colorString))
                return;

            try
            {
                System.Drawing.ColorConverter converter = new();
                object? converted = converter.ConvertFromString(colorString);
                if (converted is System.Drawing.Color color)
                    e.SubItem.ForeColor = color;
            }
            catch
            {
                // If color parsing fails, just ignore and use default color
            }
        }

        private void OnAfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (!_nodeInEditMode)
                return;

            try
            {
                _contextMenu.EnableShortcutKeys();
                mRemoteNG.Tree.ConnectionTreeModel.RenameNode(SelectedNode, e.Label ?? string.Empty);
                _nodeInEditMode = false;
                _allowEdit = false;
                // ensures that if we are filtering and a new item is added that doesn't match the filter, it will be filtered out
                _connectionTreeSearchTextFilter.SpecialInclusionList.Clear();
                UpdateFiltering();
                AppWindows.ConfigForm.SelectedTreeNode = SelectedNode;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("tvConnections_AfterLabelEdit (UI.Window.ConnectionTreeWindow) failed", ex);
            }
        }

        #endregion
    }
}

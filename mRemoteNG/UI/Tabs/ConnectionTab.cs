using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Properties;
using mRemoteNG.Tree;
using mRemoteNG.UI.TaskDialog;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Tabs
{
    [SupportedOSPlatform("windows")]
    public partial class ConnectionTab : DockContent
    {
        /// <summary>
        ///Silent close ignores the popup asking for confirmation
        /// </summary>
        public bool silentClose { get; set; }

        /// <summary>
        /// Protocol close ignores the interface controller cleanup and the user confirmation dialog
        /// </summary>
        public bool protocolClose { get; set; }

        /// <summary>
        /// Disconnect-only close (tab context menu "Disconnect"). The tab is kept open
        /// showing the reconnect panel when KeepTabsOpenAfterDisconnect is enabled.
        /// Closing the tab itself always removes the tab, regardless of that option.
        /// </summary>
        public bool disconnectOnly { get; set; }

        public ConnectionInfo? TrackedConnectionInfo { get; private set; }

        private Label? _closedStateLabel;
        private Panel? _closedStatePanel;

        public ConnectionTab()
        {
            InitializeComponent();
            Font = ConnectionTabAppearanceSettings.GetTabFont(Font);
            GotFocus += ConnectionTab_GotFocus;
        }

        private void ConnectionTab_GotFocus(object sender, EventArgs e)
        {
            TabHelper.Instance.CurrentTab = this;
        }

        public void TrackConnection(ConnectionInfo connectionInfo)
        {
            TrackedConnectionInfo = connectionInfo;
        }

        private bool _hasUnreadActivity;
        public bool HasUnreadActivity
        {
            get => _hasUnreadActivity;
            set
            {
                if (_hasUnreadActivity == value) return;
                _hasUnreadActivity = value;
                DockHandler?.Pane?.Refresh();
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            HasUnreadActivity = false;
        }

        public void ShowClosedState()
        {
            HideClosedState();

            ConnectionInfo? info = TrackedConnectionInfo;
            if (info == null)
            {
                // Fallback: simple label when no connection info is available
                _closedStateLabel ??= new Label
                {
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                _closedStateLabel.Text = Language.ConnenctionCloseEvent;
                Controls.Add(_closedStateLabel);
                _closedStateLabel.BringToFront();
                return;
            }

            _closedStatePanel = BuildClosedStatePanel(info);
            Controls.Add(_closedStatePanel);
            _closedStatePanel.BringToFront();
        }

        public void HideClosedState()
        {
            if (_closedStateLabel != null && Controls.Contains(_closedStateLabel))
                Controls.Remove(_closedStateLabel);

            if (_closedStatePanel != null)
            {
                if (Controls.Contains(_closedStatePanel))
                    Controls.Remove(_closedStatePanel);
                _closedStatePanel.Dispose();
                _closedStatePanel = null;
            }
        }

        private Panel BuildClosedStatePanel(ConnectionInfo info)
        {
            Panel outer = new() { Dock = DockStyle.Fill };

            // Detect dark background to pick readable text color
            Color bg = BackColor;
            bool isDark = bg.GetBrightness() < 0.4f;
            Color fg = isDark ? Color.White : SystemColors.ControlText;
            Color fgDim = isDark ? Color.FromArgb(160, 160, 160) : SystemColors.GrayText;

            Label lblName = new()
            {
                Text = info.Name,
                Font = new Font(Font.FontFamily, 14f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.None,
                ForeColor = fg,
                BackColor = Color.Transparent,
            };

            string details = $"{info.Protocol}   {info.Hostname}:{info.Port}";
            if (!string.IsNullOrWhiteSpace(info.Description))
                details += $"\n{info.Description}";

            Label lblDetails = new()
            {
                Text = details,
                Font = new Font(Font.FontFamily, 9.5f),
                AutoSize = true,
                ForeColor = fgDim,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.None,
            };

            Button btnConnect = new()
            {
                Text = Language.Connect,
                AutoSize = true,
                Padding = new Padding(24, 4, 24, 4),
                Anchor = AnchorStyles.None,
                FlatStyle = FlatStyle.Flat,
                ForeColor = fg,
                BackColor = isDark ? Color.FromArgb(60, 60, 60) : SystemColors.Control,
            };
            btnConnect.Click += (_, _) => Runtime.ConnectionInitiator.OpenConnection(info);

            // TableLayoutPanel with Anchor=None centers each control horizontally
            TableLayoutPanel table = new()
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 0),
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            table.Controls.Add(lblName, 0, 0);
            table.Controls.Add(lblDetails, 0, 1);
            table.Controls.Add(btnConnect, 0, 2);

            outer.Controls.Add(table);

            // Center the table block both horizontally and vertically in the outer panel
            void CenterTable(object? s, EventArgs a)
            {
                if (outer.Width == 0 || outer.Height == 0) return;
                Size preferred = table.PreferredSize;
                table.Location = new Point(
                    Math.Max(0, (outer.Width - preferred.Width) / 2),
                    Math.Max(10, (outer.Height - preferred.Height) / 2));
            }

            outer.SizeChanged += CenterTable;
            return outer;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!protocolClose)
            {
                // If the tab is showing the closed/disconnected state (no active protocol),
                // skip protocol close and confirmation — there's nothing to disconnect.
                bool hasActiveProtocol = Tag is InterfaceControl ic && ic.Protocol != null && !ic.IsDisposed;

                if (!hasActiveProtocol)
                {
                    // Tab is in closed/disconnected state — no protocol to shut down.
                    // Mark protocolClose so downstream logic skips protocol cleanup.
                    protocolClose = true;
                    HideClosedState();
                }
                else if (!silentClose)
                {
                    if (Settings.Default.ConfirmCloseConnection == (int)ConfirmCloseEnum.All)
                    {
                        DialogResult result = CTaskDialog.MessageBox(this, GeneralAppInfo.ProductName,
                                                            string
                                                                .Format(CultureInfo.CurrentCulture, Language.ConfirmCloseConnectionPanelMainInstruction,
                                                                        TabText), "", "", "",
                                                            Language.CheckboxDoNotShowThisMessageAgain,
                                                            ETaskDialogButtons.DisconnectCancel, ESysIcons.Question,
                                                            ESysIcons.Question);
                        if (CTaskDialog.VerificationChecked)
                        {
                            Settings.Default.ConfirmCloseConnection = (int)ConfirmCloseEnum.Multiple;
                            Settings.Default.Save();
                        }

                        if (result == DialogResult.No)
                        {
                            e.Cancel = true;
                        }
                        else
                        {
                            CloseProtocolSafe();
                            if (KeepTabOpenAfterDisconnect)
                                e.Cancel = true;
                        }
                    }
                    else
                    {
                        CloseProtocolSafe();
                        if (KeepTabOpenAfterDisconnect)
                            e.Cancel = true;
                    }
                }
                else if (hasActiveProtocol)
                {
                    // silentClose = true → app shutdown or panel close. Just disconnect, don't keep tab.
                    CloseProtocolSafe();
                }
            }

            base.OnFormClosing(e);
        }

        /// <summary>
        /// The protocol close handler (HandleProtocolClosed) shows the closed state with a
        /// Connect button (#61), so the form close is cancelled to keep the tab alive. This
        /// only applies to a disconnect request - closing the tab must close the tab.
        /// </summary>
        private bool KeepTabOpenAfterDisconnect =>
            disconnectOnly && Properties.OptionsTabsPanelsPage.Default.KeepTabsOpenAfterDisconnect;

        private void CloseProtocolSafe()
        {
            try
            {
                (Tag as InterfaceControl)?.Protocol?.Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector?.AddExceptionMessage("Error closing protocol", ex);
            }
        }


        #region HelperFunctions  

        public void RefreshInterfaceController()
        {
            try
            {
                InterfaceControl? interfaceControl = Tag as InterfaceControl;
                if (interfaceControl?.Info.Protocol == ProtocolType.VNC)
                    ((ProtocolVNC)interfaceControl.Protocol).RefreshScreen();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("RefreshIC (UI.Window.Connection) failed", ex);
            }
        }

        public void FireResizeEnd()
        {
            OnResizeEnd(EventArgs.Empty);
        }

        #endregion
    }
}
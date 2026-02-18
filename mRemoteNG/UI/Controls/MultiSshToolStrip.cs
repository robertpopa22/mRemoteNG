using System.ComponentModel;
using System.Windows.Forms;
using mRemoteNG.Themes;
using System;
using System.Collections;
using System.Linq;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Tabs;
using mRemoteNG.UI.Window;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Controls
{
    [SupportedOSPlatform("windows")]
    public partial class MultiSshToolStrip : ToolStrip
    {
        private IContainer components = null!;
        private ToolStripLabel lblMultiSsh = null!;
        private ToolStripTextBox txtMultiSsh = null!;
        private ToolStripButton btnCurrentPanelOnly = null!;
        private int previousCommandIndex = 0;
        private readonly ArrayList processHandlers = [];
        private readonly ArrayList quickConnectConnections = [];
        private readonly ArrayList previousCommands = [];
        private readonly ThemeManager _themeManager;

        private int CommandHistoryLength { get; set; } = 100;

        public MultiSshToolStrip()
        {
            InitializeComponent();
            _themeManager = ThemeManager.getInstance();
            _themeManager.ThemeChanged += ApplyTheme;
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (!_themeManager.ActiveAndExtended) return;
            txtMultiSsh.BackColor = _themeManager.ActiveTheme.ExtendedPalette!.getColor("TextBox_Background");
            txtMultiSsh.ForeColor = _themeManager.ActiveTheme.ExtendedPalette!.getColor("TextBox_Foreground");
        }

        private ConnectionWindow? GetCurrentConnectionPanel()
        {
            if (FrmMain.Default.pnlDock.ActiveDocument is ConnectionWindow activePanel)
                return activePanel;

            return TabHelper.Instance.CurrentPanel;
        }

        private static ConnectionWindow? GetConnectionPanel(PuttyBase puttyBase)
        {
            Control? current = puttyBase.InterfaceControl.Parent;
            while (current != null && current is not ConnectionWindow)
            {
                current = current.Parent;
            }

            return current as ConnectionWindow;
        }

        private bool ShouldIncludeConnection(ConnectionInfo connection, PuttyBase puttyBase, ConnectionWindow? currentPanel)
        {
            if (connection.ExcludeFromMultiSsh)
                return false;

            if (!btnCurrentPanelOnly.Checked)
                return true;

            if (connection.IncludeInMultiSsh)
                return true;

            if (currentPanel == null)
                return true;

            ConnectionWindow? connectionPanel = GetConnectionPanel(puttyBase);
            return connectionPanel != null && ReferenceEquals(connectionPanel, currentPanel);
        }

        private ArrayList ProcessOpenConnections(ConnectionInfo connection, ConnectionWindow? currentPanel)
        {
            ArrayList handlers = new();

            foreach (ProtocolBase protocolBase in connection.OpenConnections)
            {
                if (protocolBase is not PuttyBase puttyBase)
                    continue;

                if (ShouldIncludeConnection(connection, puttyBase, currentPanel))
                    handlers.Add(puttyBase);
            }

            return handlers;
        }

        private void SendAllKeystrokes(int keyType, int keyData)
        {
            if (processHandlers.Count == 0) return;

            foreach (PuttyBase proc in processHandlers)
            {
                NativeMethods.PostMessage(proc.PuttyHandle, keyType, new IntPtr(keyData), new IntPtr(0));
            }
        }

        #region Key Event Handler

        private void RefreshActiveConnections()
        {
            processHandlers.Clear();
            ConnectionWindow? currentPanel = GetCurrentConnectionPanel();

            foreach (ConnectionInfo connection in quickConnectConnections)
            {
                processHandlers.AddRange(ProcessOpenConnections(connection, currentPanel));
            }

            System.Collections.Generic.IEnumerable<ConnectionInfo>? connectionTreeConnections = Runtime.ConnectionsService.ConnectionTreeModel?.GetRecursiveChildList().Where(item => item.OpenConnections.Count > 0);
            if (connectionTreeConnections is null) return;

            foreach (ConnectionInfo connection in connectionTreeConnections)
            {
                processHandlers.AddRange(ProcessOpenConnections(connection, currentPanel));
            }
        }

        private void RefreshActiveConnections(object sender, EventArgs e)
        {
            RefreshActiveConnections();
        }

        private void ProcessKeyPress(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {
                e.SuppressKeyPress = true;
                try
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Up when previousCommandIndex - 1 >= 0:
                            previousCommandIndex -= 1;
                            break;
                        case Keys.Down when previousCommandIndex + 1 < previousCommands.Count:
                            previousCommandIndex += 1;
                            break;
                        default:
                            return;
                    }
                }
                catch { }

                txtMultiSsh.Text = previousCommands[previousCommandIndex]?.ToString() ?? string.Empty;
                txtMultiSsh.SelectAll();
            }

            if (e.Control && e.KeyCode == Keys.V && !e.Alt)
            {
                if (Clipboard.ContainsText())
                {
                    string text = Clipboard.GetText();
                    string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                    if (lines.Length > 1)
                    {
                        e.SuppressKeyPress = true;
                        RefreshActiveConnections();

                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            foreach (char c in lines[i])
                            {
                                SendAllKeystrokes(NativeMethods.WM_CHAR, (int)c);
                            }
                            SendAllKeystrokes(NativeMethods.WM_KEYDOWN, 13); // Enter
                        }

                        if (!string.IsNullOrEmpty(lines[lines.Length - 1]))
                        {
                            txtMultiSsh.TextBox.SelectedText = lines[lines.Length - 1];
                        }
                    }
                }
                return;
            }

            if (e.Control && e.KeyCode != Keys.V && e.Alt == false)
            {
                RefreshActiveConnections();
                SendAllKeystrokes(NativeMethods.WM_KEYDOWN, e.KeyValue);
            }

            if (e.KeyCode == Keys.Enter)
            {
                RefreshActiveConnections();
                foreach (char chr1 in txtMultiSsh.Text)
                {
                    SendAllKeystrokes(NativeMethods.WM_CHAR, Convert.ToByte(chr1));
                }

                SendAllKeystrokes(NativeMethods.WM_KEYDOWN, 13); // Enter = char13
            }
        }

        private void ProcessKeyRelease(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            if (string.IsNullOrWhiteSpace(txtMultiSsh.Text)) return;

            previousCommands.Add(txtMultiSsh.Text.Trim());

            if (previousCommands.Count >= CommandHistoryLength) previousCommands.RemoveAt(0);

            previousCommandIndex = previousCommands.Count - 1;
            txtMultiSsh.Clear();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if(components != null)
                    components.Dispose();
            }

            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblMultiSsh = new ToolStripLabel();
            this.txtMultiSsh = new ToolStripTextBox();
            this.btnCurrentPanelOnly = new ToolStripButton();
            this.SuspendLayout();
            // 
            // lblMultiSSH
            // 
            this.lblMultiSsh.Name = "_lblMultiSsh";
            this.lblMultiSsh.Size = new System.Drawing.Size(77, 22);
            this.lblMultiSsh.Text = Language.MultiSsh;
            // 
            // txtMultiSsh
            // 
            this.txtMultiSsh.Name = "_txtMultiSsh";
            this.txtMultiSsh.Size = new System.Drawing.Size(new DisplayProperties().ScaleWidth(300), 25);
            this.txtMultiSsh.ToolTipText = Language.MultiSshToolTip;
            this.txtMultiSsh.Enter += RefreshActiveConnections;
            this.txtMultiSsh.KeyDown += ProcessKeyPress;
            this.txtMultiSsh.KeyUp += ProcessKeyRelease;
            // 
            // btnCurrentPanelOnly
            // 
            this.btnCurrentPanelOnly.CheckOnClick = true;
            this.btnCurrentPanelOnly.DisplayStyle = ToolStripItemDisplayStyle.Text;
            this.btnCurrentPanelOnly.Name = "_btnCurrentPanelOnly";
            this.btnCurrentPanelOnly.Size = new System.Drawing.Size(81, 22);
            this.btnCurrentPanelOnly.Text = "Current panel";
            this.btnCurrentPanelOnly.ToolTipText = "Send commands only to tabs in the current panel. Use tab context menu to include or exclude specific tabs.";
            this.btnCurrentPanelOnly.CheckedChanged += RefreshActiveConnections;

            this.Items.AddRange(new ToolStripItem[]
            {
                lblMultiSsh,
                txtMultiSsh,
                btnCurrentPanelOnly
            });
            this.ResumeLayout(false);
        }

        #endregion

    }
}
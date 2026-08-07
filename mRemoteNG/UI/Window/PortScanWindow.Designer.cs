
using mRemoteNG.Themes;
using mRemoteNG.UI.Controls;

namespace mRemoteNG.UI.Window
{
	public partial class PortScanWindow : BaseWindow
	{
        #region  Windows Form Designer generated code
				
		internal Controls.MrngLabel lblEndIP;
		internal Controls.MrngLabel lblStartIP;
		internal MrngIpTextBox ipEnd;
		internal Controls.MrngListView olvHosts;
		internal BrightIdeasSoftware.OLVColumn clmHostName;
		internal BrightIdeasSoftware.OLVColumn clmHostIP;
		internal BrightIdeasSoftware.OLVColumn clmSSH;
		internal BrightIdeasSoftware.OLVColumn clmTelnet;
		internal BrightIdeasSoftware.OLVColumn clmHTTP;
		internal BrightIdeasSoftware.OLVColumn clmHTTPS;
		internal BrightIdeasSoftware.OLVColumn clmRlogin;
		internal BrightIdeasSoftware.OLVColumn clmRDP;
		internal BrightIdeasSoftware.OLVColumn clmVNC;
		internal BrightIdeasSoftware.OLVColumn clmOpenPorts;
		internal BrightIdeasSoftware.OLVColumn clmClosedPorts;
		internal Controls.MrngProgressBar prgBar;
		internal Controls.MrngLabel lblOnlyImport;
		internal MrngComboBox cbProtocol;
		internal Controls.MrngNumericUpDown portEnd;
		internal Controls.MrngNumericUpDown portStart;
		internal MrngButton btnImport;
		internal MrngIpTextBox ipStart;
				
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PortScanWindow));
            this.ipStart = new mRemoteNG.UI.Controls.MrngIpTextBox();
            this.ipEnd = new mRemoteNG.UI.Controls.MrngIpTextBox();
            this.lblStartIP = new mRemoteNG.UI.Controls.MrngLabel();
            this.lblEndIP = new mRemoteNG.UI.Controls.MrngLabel();
            this.olvHosts = new mRemoteNG.UI.Controls.MrngListView();
            this.resultsMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.importHTTPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importHTTPSToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importRDPToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importRloginToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importSSH2ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importTelnetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.importVNCToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnImport = new MrngButton();
            this.cbProtocol = new MrngComboBox();
            this.lblOnlyImport = new mRemoteNG.UI.Controls.MrngLabel();
            this.clmHostName = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmHostIP = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmSSH = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmTelnet = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmHTTP = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmHTTPS = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmRlogin = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmRDP = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmVNC = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmOpenPorts = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.clmClosedPorts = ((BrightIdeasSoftware.OLVColumn)(new BrightIdeasSoftware.OLVColumn()));
            this.prgBar = new mRemoteNG.UI.Controls.MrngProgressBar();
            this.numericSelectorTimeout = new mRemoteNG.UI.Controls.MrngNumericUpDown();
            this.lblTimeout = new System.Windows.Forms.Label();
            this.portEnd = new mRemoteNG.UI.Controls.MrngNumericUpDown();
            this.portStart = new mRemoteNG.UI.Controls.MrngNumericUpDown();
            this.pnlIp = new System.Windows.Forms.TableLayoutPanel();
            this.btnScan = new MrngButton();
            this.chkPortRange = new MrngCheckBox();
            this.pnlIpRange = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlTimeout = new System.Windows.Forms.FlowLayoutPanel();
            this.pnlCustomPorts = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCommonPorts = new MrngButton();
            this.lblParallelScans = new mRemoteNG.UI.Controls.MrngLabel();
            this.numericParallelScans = new mRemoteNG.UI.Controls.MrngNumericUpDown();
            this.pnlPortRange = new System.Windows.Forms.FlowLayoutPanel();
            this.lblStartPort = new mRemoteNG.UI.Controls.MrngLabel();
            this.lblToEndPort = new mRemoteNG.UI.Controls.MrngLabel();
            this.pnlImport = new System.Windows.Forms.TableLayoutPanel();
            this.pnlMain = new System.Windows.Forms.TableLayoutPanel();
            this.lblCustomPorts = new mRemoteNG.UI.Controls.MrngLabel();
            this.txtCustomPorts = new mRemoteNG.UI.Controls.MrngTextBox();
            this.portScanToolTip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.olvHosts)).BeginInit();
            this.resultsMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericSelectorTimeout)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericParallelScans)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.portEnd)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.portStart)).BeginInit();
            this.pnlIp.SuspendLayout();
            this.pnlIpRange.SuspendLayout();
            this.pnlTimeout.SuspendLayout();
            this.pnlCustomPorts.SuspendLayout();
            this.pnlPortRange.SuspendLayout();
            this.pnlImport.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // ipStart
            // 
            this.ipStart.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ipStart.Location = new System.Drawing.Point(0, 3);
            this.ipStart.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.ipStart.Name = "ipStart";
            // Wide enough for a full-length IPv6 address (39 chars, e.g.
            // ffff:ffff:ffff:ffff:ffff:ffff:ffff:ffff).
            this.ipStart.Size = new System.Drawing.Size(265, 22);
            this.ipStart.TabIndex = 1;
            this.ipStart.ToolTipText = "";
            // 
            // ipEnd
            // 
            this.ipEnd.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ipEnd.Location = new System.Drawing.Point(0, 3);
            this.ipEnd.Margin = new System.Windows.Forms.Padding(0, 3, 0, 0);
            this.ipEnd.Name = "ipEnd";
            this.ipEnd.Size = new System.Drawing.Size(265, 22);
            this.ipEnd.TabIndex = 2;
            this.ipEnd.ToolTipText = "";
            // 
            // lblStartIP
            // 
            this.lblStartIP.AutoSize = true;
            this.lblStartIP.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStartIP.Location = new System.Drawing.Point(3, 0);
            this.lblStartIP.Name = "lblStartIP";
            this.lblStartIP.Size = new System.Drawing.Size(124, 26);
            this.lblStartIP.TabIndex = 0;
            this.lblStartIP.Text = "IP Range";
            this.lblStartIP.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblEndIP
            // 
            this.lblEndIP.AutoSize = true;
            this.lblEndIP.Margin = new System.Windows.Forms.Padding(6, 5, 6, 0);
            this.lblEndIP.Name = "lblEndIP";
            this.lblEndIP.TabIndex = 5;
            this.lblEndIP.Text = "-";
            this.lblEndIP.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // olvHosts
            // 
            this.olvHosts.CellEditUseWholeCell = false;
            this.olvHosts.ContextMenuStrip = this.resultsMenuStrip;
            this.olvHosts.Cursor = System.Windows.Forms.Cursors.Default;
            this.olvHosts.DecorateLines = true;
            this.olvHosts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.olvHosts.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.olvHosts.FullRowSelect = true;
            this.olvHosts.GridLines = true;
            this.olvHosts.HideSelection = false;
            this.olvHosts.Location = new System.Drawing.Point(3, 168);
            this.olvHosts.Name = "olvHosts";
            this.olvHosts.ShowGroups = false;
            this.olvHosts.ShowSortIndicators = true;
            this.olvHosts.Size = new System.Drawing.Size(878, 230);
            this.olvHosts.TabIndex = 26;
            this.olvHosts.UseCompatibleStateImageBehavior = false;
            this.olvHosts.View = System.Windows.Forms.View.Details;
            // 
            // resultsMenuStrip
            // 
            this.resultsMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.importHTTPToolStripMenuItem,
            this.importHTTPSToolStripMenuItem,
            this.importRDPToolStripMenuItem,
            this.importRloginToolStripMenuItem,
            this.importSSH2ToolStripMenuItem,
            this.importTelnetToolStripMenuItem,
            this.importVNCToolStripMenuItem});
            this.resultsMenuStrip.Name = "resultsMenuStrip";
            this.resultsMenuStrip.Size = new System.Drawing.Size(148, 158);
            // 
            // importHTTPToolStripMenuItem
            // 
            this.importHTTPToolStripMenuItem.Name = "importHTTPToolStripMenuItem";
            this.importHTTPToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importHTTPToolStripMenuItem.Text = "Import HTTP";
            this.importHTTPToolStripMenuItem.Click += new System.EventHandler(this.importHTTPToolStripMenuItem_Click);
            // 
            // importHTTPSToolStripMenuItem
            // 
            this.importHTTPSToolStripMenuItem.Name = "importHTTPSToolStripMenuItem";
            this.importHTTPSToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importHTTPSToolStripMenuItem.Text = "Import HTTPS";
            this.importHTTPSToolStripMenuItem.Click += new System.EventHandler(this.importHTTPSToolStripMenuItem_Click);
            // 
            // importRDPToolStripMenuItem
            // 
            this.importRDPToolStripMenuItem.Name = "importRDPToolStripMenuItem";
            this.importRDPToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importRDPToolStripMenuItem.Text = "Import RDP";
            this.importRDPToolStripMenuItem.Click += new System.EventHandler(this.importRDPToolStripMenuItem_Click);
            // 
            // importRloginToolStripMenuItem
            // 
            this.importRloginToolStripMenuItem.Name = "importRloginToolStripMenuItem";
            this.importRloginToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importRloginToolStripMenuItem.Text = "Import Rlogin";
            this.importRloginToolStripMenuItem.Click += new System.EventHandler(this.importRloginToolStripMenuItem_Click);
            // 
            // importSSH2ToolStripMenuItem
            // 
            this.importSSH2ToolStripMenuItem.Name = "importSSH2ToolStripMenuItem";
            this.importSSH2ToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importSSH2ToolStripMenuItem.Text = "Import SSH2";
            this.importSSH2ToolStripMenuItem.Click += new System.EventHandler(this.importSSH2ToolStripMenuItem_Click);
            // 
            // importTelnetToolStripMenuItem
            // 
            this.importTelnetToolStripMenuItem.Name = "importTelnetToolStripMenuItem";
            this.importTelnetToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importTelnetToolStripMenuItem.Text = "Import Telnet";
            this.importTelnetToolStripMenuItem.Click += new System.EventHandler(this.importTelnetToolStripMenuItem_Click);
            // 
            // importVNCToolStripMenuItem
            // 
            this.importVNCToolStripMenuItem.Name = "importVNCToolStripMenuItem";
            this.importVNCToolStripMenuItem.Size = new System.Drawing.Size(147, 22);
            this.importVNCToolStripMenuItem.Text = "Import VNC";
            this.importVNCToolStripMenuItem.Click += new System.EventHandler(this.importVNCToolStripMenuItem_Click);
            // 
            // btnImport
            // 
            this.btnImport._mice = MrngButton.MouseState.OUT;
            this.btnImport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnImport.Location = new System.Drawing.Point(765, 27);
            this.btnImport.Name = "btnImport";
            this.btnImport.Size = new System.Drawing.Size(110, 24);
            this.btnImport.TabIndex = 8;
            this.btnImport.Text = "&Import";
            this.btnImport.UseVisualStyleBackColor = true;
            this.btnImport.Click += new System.EventHandler(this.btnImport_Click);
            // 
            // cbProtocol
            // 
            this.cbProtocol._mice = MrngComboBox.MouseState.HOVER;
            this.cbProtocol.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbProtocol.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbProtocol.FormattingEnabled = true;
            this.cbProtocol.Items.AddRange(new object[] {
            "SSH2",
            "Telnet",
            "HTTP",
            "HTTPS",
            "Rlogin",
            "RDP",
            "VNC",
            "All detected"});
            this.cbProtocol.Location = new System.Drawing.Point(3, 27);
            this.cbProtocol.Name = "cbProtocol";
            this.cbProtocol.Size = new System.Drawing.Size(144, 21);
            this.cbProtocol.TabIndex = 7;
            // 
            // lblOnlyImport
            // 
            this.lblOnlyImport.AutoSize = true;
            this.lblOnlyImport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOnlyImport.Location = new System.Drawing.Point(3, 0);
            this.lblOnlyImport.Name = "lblOnlyImport";
            this.lblOnlyImport.Size = new System.Drawing.Size(144, 24);
            this.lblOnlyImport.TabIndex = 1;
            this.lblOnlyImport.Text = "Protocol to import";
            this.lblOnlyImport.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // clmHostName
            //
            this.clmHostName.AspectName = "HostName";
            this.clmHostName.Text = "Hostname";
            this.clmHostName.Width = 130;
            //
            // clmHostIP
            //
            this.clmHostIP.AspectName = "HostIp";
            this.clmHostIP.Text = "IP Address";
            this.clmHostIP.Width = 130;
            // 
            // clmSSH
            // 
            this.clmSSH.AspectName = "SshName";
            this.clmSSH.Text = "SSH";
            this.clmSSH.Width = 50;
            // 
            // clmTelnet
            // 
            this.clmTelnet.AspectName = "TelnetName";
            this.clmTelnet.Text = "Telnet";
            this.clmTelnet.Width = 50;
            // 
            // clmHTTP
            // 
            this.clmHTTP.AspectName = "HttpName";
            this.clmHTTP.Text = "HTTP";
            this.clmHTTP.Width = 50;
            // 
            // clmHTTPS
            // 
            this.clmHTTPS.AspectName = "HttpsName";
            this.clmHTTPS.Text = "HTTPS";
            this.clmHTTPS.Width = 50;
            // 
            // clmRlogin
            // 
            this.clmRlogin.AspectName = "RloginName";
            this.clmRlogin.Text = "Rlogin";
            this.clmRlogin.Width = 50;
            // 
            // clmRDP
            // 
            this.clmRDP.AspectName = "RdpName";
            this.clmRDP.Text = "RDP";
            this.clmRDP.Width = 50;
            // 
            // clmVNC
            // 
            this.clmVNC.AspectName = "VncName";
            this.clmVNC.Text = "VNC";
            this.clmVNC.Width = 50;
            // 
            // clmOpenPorts
            // 
            this.clmOpenPorts.AspectName = "OpenPortsName";
            this.clmOpenPorts.FillsFreeSpace = true;
            this.clmOpenPorts.Text = "Open Ports";
            this.clmOpenPorts.Width = 150;
            // 
            // clmClosedPorts
            // 
            this.clmClosedPorts.AspectName = "ClosedPortsName";
            this.clmClosedPorts.FillsFreeSpace = true;
            this.clmClosedPorts.Text = "Closed Ports";
            this.clmClosedPorts.Width = 150;
            // 
            // prgBar
            // 
            this.prgBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.prgBar.Location = new System.Drawing.Point(3, 138);
            this.prgBar.Name = "prgBar";
            this.prgBar.Size = new System.Drawing.Size(878, 24);
            this.prgBar.Step = 1;
            this.prgBar.TabIndex = 28;
            // 
            // numericSelectorTimeout
            // 
            this.numericSelectorTimeout.Location = new System.Drawing.Point(0, 0);
            this.numericSelectorTimeout.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.numericSelectorTimeout.Maximum = new decimal(new int[] {
            2147482,
            0,
            0,
            0});
            this.numericSelectorTimeout.Name = "numericSelectorTimeout";
            this.numericSelectorTimeout.Size = new System.Drawing.Size(67, 22);
            this.numericSelectorTimeout.TabIndex = 5;
            this.numericSelectorTimeout.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // lblTimeout
            // 
            this.lblTimeout.AutoSize = true;
            this.lblTimeout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeout.Location = new System.Drawing.Point(3, 120);
            this.lblTimeout.Name = "lblTimeout";
            this.lblTimeout.Size = new System.Drawing.Size(124, 33);
            this.lblTimeout.TabIndex = 16;
            this.lblTimeout.Text = "Timeout [seconds]";
            this.lblTimeout.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // portEnd
            // 
            this.portEnd.Enabled = false;
            this.portEnd.Location = new System.Drawing.Point(133, 75);
            this.portEnd.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portEnd.Name = "portEnd";
            this.portEnd.Size = new System.Drawing.Size(67, 22);
            this.portEnd.TabIndex = 4;
            this.portEnd.Value = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portEnd.Enter += new System.EventHandler(this.portEnd_Enter);
            // 
            // portStart
            // 
            this.portStart.Enabled = false;
            this.portStart.Location = new System.Drawing.Point(133, 51);
            this.portStart.Maximum = new decimal(new int[] {
            65535,
            0,
            0,
            0});
            this.portStart.Name = "portStart";
            this.portStart.Size = new System.Drawing.Size(67, 22);
            this.portStart.TabIndex = 3;
            this.portStart.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.portStart.Enter += new System.EventHandler(this.portStart_Enter);
            // 
            // pnlIp
            // 
            this.pnlIp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlIp.ColumnCount = 3;
            this.pnlIp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.pnlIp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 130F));
            this.pnlIp.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlIp.Controls.Add(this.lblStartIP, 0, 0);
            this.pnlIp.Controls.Add(this.pnlIpRange, 1, 0);
            this.pnlIp.SetColumnSpan(this.pnlIpRange, 2);
            this.pnlIp.Controls.Add(this.chkPortRange, 0, 1);
            this.pnlIp.Controls.Add(this.pnlPortRange, 1, 1);
            this.pnlIp.SetColumnSpan(this.pnlPortRange, 2);
            this.pnlIp.Controls.Add(this.lblCustomPorts, 0, 2);
            this.pnlIp.Controls.Add(this.pnlCustomPorts, 1, 2);
            this.pnlIp.SetColumnSpan(this.pnlCustomPorts, 2);
            this.pnlIp.Controls.Add(this.lblTimeout, 0, 3);
            this.pnlIp.Controls.Add(this.pnlTimeout, 1, 3);
            this.pnlIp.SetColumnSpan(this.pnlTimeout, 2);
            this.pnlIp.Location = new System.Drawing.Point(3, 3);
            this.pnlIp.Name = "pnlIp";
            this.pnlIp.RowCount = 4;
            this.pnlIp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.pnlIp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.pnlIp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 26F));
            this.pnlIp.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.pnlIp.Size = new System.Drawing.Size(878, 113);
            this.pnlIp.TabIndex = 103;
            // 
            // btnScan
            // 
            this.btnScan._mice = MrngButton.MouseState.OUT;
            this.btnScan.Image = global::mRemoteNG.Properties.Resources.Search_16x;
            this.btnScan.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnScan.Location = new System.Drawing.Point(75, 0);
            this.btnScan.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
            this.btnScan.Name = "btnScan";
            this.btnScan.Size = new System.Drawing.Size(110, 24);
            this.btnScan.TabIndex = 6;
            this.btnScan.Text = "&Scan";
            this.btnScan.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnScan.UseVisualStyleBackColor = true;
            this.btnScan.Click += new System.EventHandler(this.btnScan_Click);
            // 
            // chkPortRange
            //
            this.chkPortRange._mice = MrngCheckBox.MouseState.OUT;
            this.chkPortRange.AutoSize = true;
            this.chkPortRange.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.chkPortRange.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.chkPortRange.Location = new System.Drawing.Point(3, 51);
            this.chkPortRange.Name = "chkPortRange";
            this.chkPortRange.Size = new System.Drawing.Size(84, 17);
            this.chkPortRange.TabIndex = 17;
            this.chkPortRange.Text = "Port Range";
            this.chkPortRange.UseVisualStyleBackColor = true;
            this.chkPortRange.CheckedChanged += new System.EventHandler(this.ChkPortRange_CheckedChanged);
            //
            // pnlIpRange
            //
            this.pnlIpRange.AutoSize = true;
            this.pnlIpRange.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlIpRange.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pnlIpRange.Controls.Add(this.ipStart);
            this.pnlIpRange.Controls.Add(this.lblEndIP);
            this.pnlIpRange.Controls.Add(this.ipEnd);
            this.pnlIpRange.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlIpRange.Location = new System.Drawing.Point(133, 0);
            this.pnlIpRange.Margin = new System.Windows.Forms.Padding(0);
            this.pnlIpRange.Name = "pnlIpRange";
            this.pnlIpRange.Size = new System.Drawing.Size(536, 26);
            this.pnlIpRange.TabIndex = 1;
            this.pnlIpRange.WrapContents = false;
            //
            // pnlTimeout
            //
            this.pnlTimeout.AutoSize = true;
            this.pnlTimeout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlTimeout.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pnlTimeout.Controls.Add(this.numericSelectorTimeout);
            this.pnlTimeout.Controls.Add(this.lblParallelScans);
            this.pnlTimeout.Controls.Add(this.numericParallelScans);
            this.pnlTimeout.Controls.Add(this.btnScan);
            this.pnlTimeout.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlTimeout.Location = new System.Drawing.Point(133, 0);
            this.pnlTimeout.Margin = new System.Windows.Forms.Padding(0);
            this.pnlTimeout.Name = "pnlTimeout";
            this.pnlTimeout.Size = new System.Drawing.Size(220, 26);
            this.pnlTimeout.TabIndex = 5;
            this.pnlTimeout.WrapContents = false;
            //
            // pnlPortRange
            //
            this.pnlPortRange.AutoSize = true;
            this.pnlPortRange.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlPortRange.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pnlPortRange.Controls.Add(this.lblStartPort);
            this.pnlPortRange.Controls.Add(this.portStart);
            this.pnlPortRange.Controls.Add(this.lblToEndPort);
            this.pnlPortRange.Controls.Add(this.portEnd);
            this.pnlPortRange.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlPortRange.Location = new System.Drawing.Point(133, 48);
            this.pnlPortRange.Margin = new System.Windows.Forms.Padding(0);
            this.pnlPortRange.Name = "pnlPortRange";
            this.pnlPortRange.Size = new System.Drawing.Size(340, 24);
            this.pnlPortRange.TabIndex = 18;
            this.pnlPortRange.WrapContents = false;
            //
            // lblStartPort
            //
            this.lblStartPort.AutoSize = true;
            this.lblStartPort.Enabled = false;
            this.lblStartPort.Margin = new System.Windows.Forms.Padding(3, 6, 3, 0);
            this.lblStartPort.Name = "lblStartPort";
            this.lblStartPort.Text = "Start Port";
            this.lblStartPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblToEndPort
            //
            this.lblToEndPort.AutoSize = true;
            this.lblToEndPort.Enabled = false;
            this.lblToEndPort.Margin = new System.Windows.Forms.Padding(10, 6, 3, 0);
            this.lblToEndPort.Name = "lblToEndPort";
            this.lblToEndPort.Text = "to End Port";
            this.lblToEndPort.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // lblCustomPorts
            //
            this.lblCustomPorts.AutoSize = true;
            this.lblCustomPorts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCustomPorts.Location = new System.Drawing.Point(3, 96);
            this.lblCustomPorts.Name = "lblCustomPorts";
            this.lblCustomPorts.Size = new System.Drawing.Size(124, 24);
            this.lblCustomPorts.TabIndex = 19;
            this.lblCustomPorts.Text = "Custom ports:";
            this.lblCustomPorts.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // txtCustomPorts
            //
            this.txtCustomPorts.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCustomPorts.Location = new System.Drawing.Point(0, 1);
            this.txtCustomPorts.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.txtCustomPorts.Name = "txtCustomPorts";
            this.txtCustomPorts.Size = new System.Drawing.Size(265, 22);
            this.txtCustomPorts.TabIndex = 20;
            //
            // pnlCustomPorts
            //
            this.pnlCustomPorts.AutoSize = true;
            this.pnlCustomPorts.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.pnlCustomPorts.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pnlCustomPorts.Controls.Add(this.txtCustomPorts);
            this.pnlCustomPorts.Controls.Add(this.btnCommonPorts);
            this.pnlCustomPorts.FlowDirection = System.Windows.Forms.FlowDirection.LeftToRight;
            this.pnlCustomPorts.Location = new System.Drawing.Point(133, 0);
            this.pnlCustomPorts.Margin = new System.Windows.Forms.Padding(0);
            this.pnlCustomPorts.Name = "pnlCustomPorts";
            this.pnlCustomPorts.Size = new System.Drawing.Size(420, 26);
            this.pnlCustomPorts.TabIndex = 21;
            this.pnlCustomPorts.WrapContents = false;
            //
            // lblParallelScans
            //
            this.lblParallelScans.AutoSize = true;
            this.lblParallelScans.Margin = new System.Windows.Forms.Padding(16, 5, 4, 0);
            this.lblParallelScans.Name = "lblParallelScans";
            this.lblParallelScans.Text = "Parallel scans";
            this.lblParallelScans.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            //
            // numericParallelScans
            //
            this.numericParallelScans.Location = new System.Drawing.Point(0, 0);
            this.numericParallelScans.Margin = new System.Windows.Forms.Padding(0, 1, 0, 0);
            this.numericParallelScans.Maximum = new decimal(new int[] {
            128,
            0,
            0,
            0});
            this.numericParallelScans.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericParallelScans.Name = "numericParallelScans";
            this.numericParallelScans.Size = new System.Drawing.Size(60, 22);
            this.numericParallelScans.TabIndex = 6;
            this.numericParallelScans.Value = new decimal(new int[] {
            64,
            0,
            0,
            0});
            //
            // btnCommonPorts
            //
            this.btnCommonPorts._mice = MrngButton.MouseState.OUT;
            this.btnCommonPorts.Margin = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.btnCommonPorts.Name = "btnCommonPorts";
            this.btnCommonPorts.Size = new System.Drawing.Size(120, 24);
            this.btnCommonPorts.TabIndex = 22;
            this.btnCommonPorts.Text = "Set common ports";
            this.btnCommonPorts.UseVisualStyleBackColor = true;
            this.btnCommonPorts.Click += new System.EventHandler(this.BtnCommonPorts_Click);
            //
            // pnlImport
            // 
            this.pnlImport.ColumnCount = 2;
            this.pnlImport.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.pnlImport.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlImport.Controls.Add(this.lblOnlyImport, 0, 0);
            this.pnlImport.Controls.Add(this.cbProtocol, 0, 1);
            this.pnlImport.Controls.Add(this.btnImport, 1, 1);
            this.pnlImport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlImport.Location = new System.Drawing.Point(3, 404);
            this.pnlImport.Name = "pnlImport";
            this.pnlImport.RowCount = 2;
            this.pnlImport.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.pnlImport.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.pnlImport.Size = new System.Drawing.Size(878, 54);
            this.pnlImport.TabIndex = 104;
            // 
            // pnlMain
            // 
            this.pnlMain.ColumnCount = 1;
            this.pnlMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlMain.Controls.Add(this.pnlIp, 0, 0);
            this.pnlMain.Controls.Add(this.prgBar, 0, 1);
            this.pnlMain.Controls.Add(this.pnlImport, 0, 3);
            this.pnlMain.Controls.Add(this.olvHosts, 0, 2);
            this.pnlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMain.Location = new System.Drawing.Point(0, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.RowCount = 4;
            this.pnlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 159F));
            this.pnlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.pnlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.pnlMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.pnlMain.Size = new System.Drawing.Size(884, 461);
            this.pnlMain.TabIndex = 105;
            // 
            // PortScanWindow
            // 
            this.AcceptButton = this.btnImport;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.AutoScroll = true;
            this.ClientSize = new System.Drawing.Size(884, 461);
            this.Controls.Add(this.pnlMain);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "PortScanWindow";
            this.TabText = "Port Scan";
            this.Text = "Port Scan";
            this.Load += new System.EventHandler(this.PortScan_Load);
            ((System.ComponentModel.ISupportInitialize)(this.olvHosts)).EndInit();
            this.resultsMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numericSelectorTimeout)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericParallelScans)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.portEnd)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.portStart)).EndInit();
            this.pnlPortRange.ResumeLayout(false);
            this.pnlPortRange.PerformLayout();
            this.pnlIpRange.ResumeLayout(false);
            this.pnlIpRange.PerformLayout();
            this.pnlTimeout.ResumeLayout(false);
            this.pnlTimeout.PerformLayout();
            this.pnlCustomPorts.ResumeLayout(false);
            this.pnlCustomPorts.PerformLayout();
            this.pnlIp.ResumeLayout(false);
            this.pnlIp.PerformLayout();
            this.pnlImport.ResumeLayout(false);
            this.pnlImport.PerformLayout();
            this.pnlMain.ResumeLayout(false);
            this.ResumeLayout(false);

		}
        #endregion

        private System.Windows.Forms.ContextMenuStrip resultsMenuStrip;
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.ToolStripMenuItem importHTTPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importHTTPSToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importRDPToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importRloginToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importSSH2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importTelnetToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem importVNCToolStripMenuItem;
		private System.Windows.Forms.Label lblTimeout;
		private Controls.MrngNumericUpDown numericSelectorTimeout;
        private System.Windows.Forms.TableLayoutPanel pnlIp;
        private System.Windows.Forms.TableLayoutPanel pnlImport;
        internal MrngButton btnScan;
        private System.Windows.Forms.TableLayoutPanel pnlMain;
        private MrngCheckBox chkPortRange;
        private System.Windows.Forms.FlowLayoutPanel pnlPortRange;
        private System.Windows.Forms.FlowLayoutPanel pnlIpRange;
        private System.Windows.Forms.FlowLayoutPanel pnlTimeout;
        private System.Windows.Forms.FlowLayoutPanel pnlCustomPorts;
        internal MrngButton btnCommonPorts;
        private Controls.MrngLabel lblParallelScans;
        internal Controls.MrngNumericUpDown numericParallelScans;
        private Controls.MrngLabel lblStartPort;
        private Controls.MrngLabel lblToEndPort;
        private System.Windows.Forms.ToolTip portScanToolTip;
        private Controls.MrngLabel lblCustomPorts;
        private Controls.MrngTextBox txtCustomPorts;
    }
}

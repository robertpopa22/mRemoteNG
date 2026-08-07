using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;
using WeifenLuo.WinFormsUI.Docking;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Window
{
    [SupportedOSPlatform("windows")]
    public partial class PortScanWindow
    {
        #region Constructors

        public PortScanWindow()
        {
            InitializeComponent();
            _scanResultsTimer.Tick += ScanResultsTimer_Tick;
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.SearchAndApps_16x);
            WindowType = WindowType.PortScan;
            DockPnl = new DockContent();
            ApplyTheme();
            DisplayProperties display = new();
            if (btnScan.Image is not null)
                btnScan.Image = display.ScaleImage(btnScan.Image);
        }

        #endregion

        private new void ApplyTheme()
        {
            base.ApplyTheme();
        }

        #region Private Properties

        private bool IpsValid
        {
            get
            {
                if (!IPAddress.TryParse(ipStart.Text?.Trim(), out IPAddress? start))
                    return false;

                if (!IPAddress.TryParse(ipEnd.Text?.Trim(), out IPAddress? end))
                    return false;

                // A range must be within a single address family (both IPv4 or both IPv6).
                return start.AddressFamily == end.AddressFamily;
            }
        }

        #endregion

        #region Private Fields

        private PortScanner? _portScanner;
        private bool _scanning;

        // Scan results are produced by many worker threads; they are queued here and drained onto the
        // results list in batches by _scanResultsTimer (see PortScanner_HostScanned).
        private readonly ConcurrentQueue<ScanHost> _pendingHosts = new();
        private readonly System.Windows.Forms.Timer _scanResultsTimer = new() { Interval = 200 };
        private int _scannedHostCount;
        private int _totalHostCount;

        #endregion

        #region Private Methods

        #region Event Handlers

        private void PortScan_Load(object sender, EventArgs e)
        {
            ApplyLanguage();

            try
            {
                olvHosts.Columns.AddRange(new ColumnHeader[]
                {
                    clmHostIP, clmHostName, clmSSH, clmTelnet, clmHTTP, clmHTTPS, clmRlogin, clmRDP, clmVNC, clmOpenPorts,
                    clmClosedPorts
                });
                ShowImportControls(true);
                cbProtocol.SelectedIndex = 0;
                numericSelectorTimeout.Value = 5;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(Language.PortScanCouldNotLoadPanel, ex);
            }
        }

        private void portStart_Enter(object sender, EventArgs e)
        {
            portStart.Select(0, portStart.Text.Length);
        }

        private void portEnd_Enter(object sender, EventArgs e)
        {
            portEnd.Select(0, portEnd.Text.Length);
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            if (_scanning)
            {
                StopScan();
            }
            else
            {
                if (IpsValid)
                {
                    StartScan();
                }
                else
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, Language.CannotStartPortScan);
                }
            }
        }

        private void btnImport_Click(object sender, EventArgs e)
        {
            string selectedItem = Convert.ToString(cbProtocol.SelectedItem, CultureInfo.InvariantCulture) ?? string.Empty;
            if (string.Equals(selectedItem, "All detected", StringComparison.Ordinal))
            {
                importAllDetectedProtocols();
                return;
            }

            ProtocolType protocol = Enum.Parse<ProtocolType>(selectedItem, true);
            importSelectedHosts(protocol);
        }

        #endregion

        private void ApplyLanguage()
        {
            // Single "IP Range  <first> - <last>" row; lblEndIP is the separator between the two fields.
            lblStartIP.Text = "IP Range";
            lblEndIP.Text = "-";
            const string ipHint = "IPv4 or IPv6 address (e.g. 192.168.1.1 or 2001:db8::1)";
            ipStart.ToolTipText = ipHint;
            ipEnd.ToolTipText = ipHint;
            btnScan.Text = Language._Scan;
            btnImport.Text = Language._Import;
            lblOnlyImport.Text = Language.ProtocolToImport;
            clmHostIP.Text = "IP Address";
            clmHostName.Text = "Hostname";
            clmOpenPorts.Text = Language.OpenPorts;
            clmClosedPorts.Text = Language.ClosedPorts;
            chkPortRange.Text = "Port Range";
            lblStartPort.Text = "Start Port";
            lblToEndPort.Text = "to End Port";
            lblCustomPorts.Text = "Custom ports (e.g. 22,80,443):";
            btnCommonPorts.Text = "Set common ports";
            portScanToolTip.SetToolTip(btnCommonPorts, "Fill the custom ports box with commonly scanned service ports");
            lblParallelScans.Text = "Parallel scans";
            portScanToolTip.SetToolTip(numericParallelScans,
                $"How many hosts are probed at once ({PortScanner.MinConcurrentHosts}-{PortScanner.MaxConcurrentHosts}). " +
                "Lower this if the scan saturates your network or machine.");
            lblTimeout.Text = Language.TimeoutInSeconds;
            TabText = Language.PortScan;
            Text = Language.PortScan;
        }

        private void ShowImportControls(bool controlsVisible)
        {
            pnlImport.Visible = controlsVisible;
            if (controlsVisible)
                olvHosts.Height = pnlImport.Top - olvHosts.Top;
            else
                olvHosts.Height = pnlImport.Bottom - olvHosts.Top;
        }

        private void StartScan()
        {
            // Build the scanner FIRST. Constructing it validates and enumerates the address range and
            // can throw (e.g. the range exceeds the scan limit, or the endpoints are different IP
            // families). Do this before flipping into the "scanning" state so a failure can't leave the
            // Scan/Stop button stuck on "Stop" with nothing running.
            PortScanner scanner;
            try
            {
                IPAddress ipAddressStart = IPAddress.Parse(ipStart.Text.Trim());
                IPAddress ipAddressEnd = IPAddress.Parse(ipEnd.Text.Trim());
                int timeoutMs = (int)numericSelectorTimeout.Value * 1000;
                int parallelScans = (int)numericParallelScans.Value;

                string customPortsText = txtCustomPorts.Text.Trim();
                if (!string.IsNullOrEmpty(customPortsText))
                {
                    List<int> customPorts = ParsePortList(customPortsText);
                    if (customPorts.Count == 0)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, Language.CannotStartPortScan);
                        return;
                    }
                    scanner = new PortScanner(ipAddressStart, ipAddressEnd, customPorts, timeoutMs, parallelScans);
                }
                else if (!chkPortRange.Checked)
                    // No custom ports and no explicit range: probe the well-known protocol ports.
                    scanner = new PortScanner(ipAddressStart, ipAddressEnd, (int)portStart.Value,
                                              (int)portEnd.Value, timeoutMs, true, parallelScans);
                else
                    scanner = new PortScanner(ipAddressStart, ipAddressEnd, (int)portStart.Value,
                                              (int)portEnd.Value, timeoutMs, false, parallelScans);
            }
            catch (ArgumentException ex)
            {
                // Range too large, mixed IPv4/IPv6 endpoints, or an unparsable address — surface the
                // reason instead of silently doing nothing.
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, ex.Message);
                MessageBox.Show(this, ex.Message, Language.PortScan, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("StartScan failed (UI.Window.PortScan)", ex);
                return;
            }

            _portScanner = scanner;
            _portScanner.BeginHostScan += PortScanner_BeginHostScan;
            _portScanner.HostScanned += PortScanner_HostScanned;
            _portScanner.ScanComplete += PortScanner_ScanComplete;

            _scanning = true;
            SwitchButtonText();
            olvHosts.Items.Clear();

            _pendingHosts.Clear();
            Volatile.Write(ref _scannedHostCount, 0);
            Volatile.Write(ref _totalHostCount, 0);
            _scanResultsTimer.Start();

            _portScanner.StartScan();
        }

        private void StopScan()
        {
            if (_portScanner is not null)
            {
                _portScanner.BeginHostScan -= PortScanner_BeginHostScan;
                _portScanner.HostScanned -= PortScanner_HostScanned;
                _portScanner.ScanComplete -= PortScanner_ScanComplete;
                _portScanner.StopScan();
            }

            // Show whatever has already come back, then stop polling.
            _scanResultsTimer.Stop();
            FlushScanResults();

            _scanning = false;
            SwitchButtonText();
        }

        /// <summary>
        /// Commonly scanned service ports: FTP/SSH/Telnet/SMTP/DNS/HTTP(S), Windows RPC/NetBIOS/SMB,
        /// LDAP(S), IMAP/POP3 (incl. TLS), the usual databases, RDP, VNC, WinRM and common app ports.
        /// </summary>
        private const string CommonPorts =
            "21,22,23,25,53,80,110,111,135,139,143,389,443,445,465,587,636,993,995," +
            "1433,1521,2049,3306,3389,5432,5900,5985,5986,6379,8080,8443,9200,27017";

        private void BtnCommonPorts_Click(object sender, EventArgs e)
        {
            txtCustomPorts.Text = CommonPorts;
        }

        private static List<int> ParsePortList(string portListText)
        {
            List<int> ports = new();
            foreach (string part in portListText.Split(',', ';', ' '))
            {
                string trimmed = part.Trim();
                if (int.TryParse(trimmed, out int port) && port >= 1 && port <= 65535)
                    ports.Add(port);
            }
            return ports;
        }

        private void SwitchButtonText()
        {
            btnScan.Text = _scanning ? Language._Stop : Language._Scan;

            prgBar.Maximum = 100;
            prgBar.Value = 0;
        }

        private static void PortScanner_BeginHostScan(string host)
        {
            // Deliberately does not log: this fires once per address, on a worker thread, and
            // MessageCollector runs its writer chain (including synchronous log file I/O) inline on
            // the caller. At scan scale that serialised every worker on the log appender's lock.
        }

        /// <summary>
        /// Called from scan worker threads. Only queues the result — no marshalling, no logging and no
        /// list manipulation here. <see cref="FlushScanResults"/> drains the queue on the UI thread a
        /// few times a second, so a flood of results costs a handful of batched UI updates instead of
        /// one cross-thread post plus one list rebuild per host.
        /// </summary>
        private void PortScanner_HostScanned(ScanHost host, int scannedCount, int totalCount)
        {
            _pendingHosts.Enqueue(host);
            Volatile.Write(ref _scannedHostCount, scannedCount);
            Volatile.Write(ref _totalHostCount, totalCount);
        }

        private void ScanResultsTimer_Tick(object? sender, EventArgs e)
        {
            FlushScanResults();
        }

        /// <summary>
        /// Moves any queued scan results into the results list in one batch (UI thread only).
        /// </summary>
        private void FlushScanResults()
        {
            int total = Volatile.Read(ref _totalHostCount);
            int scanned = Volatile.Read(ref _scannedHostCount);

            List<ScanHost> batch = new();
            while (_pendingHosts.TryDequeue(out ScanHost? host))
                batch.Add(host);

            if (batch.Count > 0)
            {
                // AddObjects once per batch: ObjectListView rebuilds on every add, so adding one at a
                // time made the cost grow with the number of results already shown.
                olvHosts.BeginUpdate();
                try
                {
                    olvHosts.AddObjects(batch);
                }
                finally
                {
                    olvHosts.EndUpdate();
                }
            }

            if (total > 0)
            {
                prgBar.Maximum = total;
                prgBar.Value = Math.Min(scanned, total);
            }
        }

        private delegate void PortScannerScanComplete(IList<ScanHost> hosts);

        private void PortScanner_ScanComplete(IList<ScanHost> hosts)
        {
            if (InvokeRequired)
            {
                if (IsHandleCreated)
                    BeginInvoke(new PortScannerScanComplete(PortScanner_ScanComplete), hosts);
                return;
            }

            // Drain anything still queued, then stop polling.
            _scanResultsTimer.Stop();
            FlushScanResults();

            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.PortScanComplete);

            _scanning = false;
            SwitchButtonText();
        }

        #endregion

        private void importSelectedHosts(ProtocolType protocol)
        {
            List<ScanHost> hosts = new();
            foreach (ScanHost host in olvHosts.SelectedObjects)
            {
                hosts.Add(host);
            }

            if (hosts.Count < 1)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                                                    "Could not import host(s) from port scan context menu");
                return;
            }

            ContainerInfo? destinationContainer = GetDestinationContainerForImportedHosts();
            if (destinationContainer is null)
                return;
            Import.ImportFromPortScan(hosts, protocol, destinationContainer);
        }

        private void importAllDetectedProtocols()
        {
            List<ScanHost> hosts = new();
            foreach (ScanHost host in olvHosts.SelectedObjects)
            {
                hosts.Add(host);
            }

            if (hosts.Count < 1)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                                                    "Could not import host(s) from port scan context menu");
                return;
            }

            ContainerInfo? destinationContainer = GetDestinationContainerForImportedHosts();
            if (destinationContainer is null)
                return;

            (ProtocolType protocol, Func<ScanHost, bool> detected)[] protocolFilters =
            [
                (ProtocolType.SSH2, h => h.Ssh),
                (ProtocolType.Telnet, h => h.Telnet),
                (ProtocolType.HTTP, h => h.Http),
                (ProtocolType.HTTPS, h => h.Https),
                (ProtocolType.Rlogin, h => h.Rlogin),
                (ProtocolType.RDP, h => h.Rdp),
                (ProtocolType.VNC, h => h.Vnc),
            ];

            foreach (var (protocol, detected) in protocolFilters)
            {
                List<ScanHost> filtered = hosts.Where(detected).ToList();
                if (filtered.Count > 0)
                    Import.ImportFromPortScan(filtered, protocol, destinationContainer);
            }
        }

        /// <summary>
        /// Determines where the imported hosts will be placed
        /// in the connection tree.
        /// </summary>
        private static ContainerInfo? GetDestinationContainerForImportedHosts()
        {
            ConnectionInfo? selectedNode = AppWindows.TreeForm?.SelectedNode ?? AppWindows.TreeForm?.ConnectionTree.ConnectionTreeModel.RootNodes.OfType<RootNodeInfo>().First();

            if (selectedNode is null)
                return null;

            // if a putty node is selected, place imported connections in the root connection node
            if (selectedNode is RootPuttySessionsNodeInfo || selectedNode is PuttySessionInfo)
                selectedNode = AppWindows.TreeForm!.ConnectionTree.ConnectionTreeModel.RootNodes.OfType<RootNodeInfo>()
                                      .First();

            // if the selected node is a connection, use its parent container
            ContainerInfo? selectedTreeNodeAsContainer = selectedNode as ContainerInfo ?? selectedNode.Parent;

            return selectedTreeNodeAsContainer;
        }

        private void importVNCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.VNC);
        }

        private void importTelnetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.Telnet);
        }

        private void importSSH2ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.SSH2);
        }

        private void importRloginToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.Rlogin);
        }

        private void importRDPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.RDP);
        }

        private void importHTTPSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.HTTPS);
        }

        private void importHTTPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            importSelectedHosts(ProtocolType.HTTP);
        }

        /// <summary>
        /// A port range is scanned only when the box is ticked; otherwise the well-known protocol
        /// ports are probed. The start/end fields follow the checkbox, so they can never be left in a
        /// half-configured state (the old UI had a separate checkbox per field).
        /// </summary>
        private void ChkPortRange_CheckedChanged(object sender, EventArgs e)
        {
            bool useRange = chkPortRange.Checked;
            portStart.Enabled = useRange;
            portEnd.Enabled = useRange;
            lblStartPort.Enabled = useRange;
            lblToEndPort.Enabled = useRange;
        }
    }
}
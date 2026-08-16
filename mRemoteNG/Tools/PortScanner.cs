using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using mRemoteNG.App;
using mRemoteNG.Messages;


namespace mRemoteNG.Tools
{
    [SupportedOSPlatform("windows")]
    public class PortScanner
    {
        private readonly List<IPAddress> _ipAddresses = [];
        private readonly List<int> _ports = [];
        private Thread? _scanThread;
        private readonly List<ScanHost> _scannedHosts = [];
        private readonly int _timeoutInMilliseconds;

        #region Public Methods

        public PortScanner(IPAddress ipAddress1,
                           IPAddress ipAddress2,
                           IEnumerable<int> ports,
                           int timeoutInMilliseconds = 5000)
        {
            IPAddress ipAddressStart = IpAddressMin(ipAddress1, ipAddress2);
            IPAddress ipAddressEnd = IpAddressMax(ipAddress1, ipAddress2);

            ArgumentOutOfRangeException.ThrowIfNegative(timeoutInMilliseconds);

            _timeoutInMilliseconds = timeoutInMilliseconds;
            _ports.Clear();
            _ports.AddRange(ports);

            _ipAddresses.Clear();
            _ipAddresses.AddRange(IpAddressArrayFromRange(ipAddressStart, ipAddressEnd));

            _scannedHosts.Clear();
        }

        public PortScanner(IPAddress ipAddress1,
                           IPAddress ipAddress2,
                           int port1,
                           int port2,
                           int timeoutInMilliseconds = 5000,
                           bool checkDefaultPortsOnly = false)
        {
            IPAddress ipAddressStart = IpAddressMin(ipAddress1, ipAddress2);
            IPAddress ipAddressEnd = IpAddressMax(ipAddress1, ipAddress2);

            int portStart = Math.Min(port1, port2);
            int portEnd = Math.Max(port1, port2);

            // if only one port was specified, just scan the one port...
            if (portStart == 0)
                portStart = portEnd;

            ArgumentOutOfRangeException.ThrowIfNegative(timeoutInMilliseconds);

            _timeoutInMilliseconds = timeoutInMilliseconds;
            _ports.Clear();

            if (checkDefaultPortsOnly)
                _ports.AddRange(new[]
                {
                    ScanHost.SshPort, ScanHost.TelnetPort, ScanHost.HttpPort, ScanHost.HttpsPort, ScanHost.RloginPort,
                    ScanHost.RdpPort, ScanHost.VncPort
                });
            else
            {
                for (int port = portStart; port <= portEnd; port++)
                {
                    _ports.Add(port);
                }
            }

            _ipAddresses.Clear();
            _ipAddresses.AddRange(IpAddressArrayFromRange(ipAddressStart, ipAddressEnd));

            _scannedHosts.Clear();
        }

        public void StartScan()
        {
            _scanThread = new Thread(ScanAsync);

            if(OperatingSystem.IsWindows())
                _scanThread.SetApartmentState(ApartmentState.STA);

            _scanThread.IsBackground = true;
            _scanThread.Start();
        }

        public void StopScan()
        {
            foreach (Ping p in _pings)
            {
                p.SendAsyncCancel();
            }

            // Obsolete: https://learn.microsoft.com/en-us/dotnet/core/compatibility/core-libraries/5.0/thread-abort-obsolete
            //_scanThread.Abort();
        }

        /// <summary>Default bound for a single-host probe (Connection Tester, reconnect timers).</summary>
        private const int DefaultProbeTimeoutMilliseconds = 3000;

        public static bool IsPortOpen(string hostname, string port) =>
            IsPortOpen(hostname, port, DefaultProbeTimeoutMilliseconds);

        public static bool IsPortOpen(string hostname, string port, int timeoutMilliseconds) =>
            TryConnect(hostname, Convert.ToInt32(port, CultureInfo.InvariantCulture), timeoutMilliseconds);

        /// <summary>
        /// Connects with a hard upper bound on how long a single host can block the caller.
        ///
        /// <c>new TcpClient(host, port)</c> has no timeout of its own: it waits on the OS-level TCP
        /// connect, which for a host that silently drops the SYN (a filtered port, an unreachable
        /// VPN peer, a machine that is simply off) is 20+ seconds on Windows. That is fine for a
        /// background sweep across many hosts in parallel, but three of this method's callers are
        /// sequential, and one — RdpProtocol's reconnect timer — calls it directly from a WinForms
        /// Timer.Tick on the UI thread with no threading guard at all. Every tick against an
        /// unreachable host froze the whole application for the OS timeout, repeatedly, for as long
        /// as the host stayed down. Bounding the wait here fixes it at the one place all three
        /// callers share, rather than requiring each caller to remember to guard itself.
        ///
        /// The connect attempt itself is not cancelled when the timeout elapses — Socket has no
        /// clean way to abort an in-flight connect — so the background attempt still resolves on its
        /// own thread eventually. What changes is that the caller stops waiting for it.
        /// </summary>
        private static bool TryConnect(string hostname, int port, int timeoutMilliseconds)
        {
            using Socket socket = new(SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Task connectTask = socket.ConnectAsync(hostname, port);
                bool completedInTime = connectTask.Wait(timeoutMilliseconds);
                return completedInTime && socket.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        private int _hostCount;
        private readonly List<Ping> _pings = [];

        private void ScanAsync()
        {
            try
            {
                _hostCount = 0;
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"Tools.PortScan: Starting scan of {_ipAddresses.Count} hosts...", true);
                foreach (IPAddress ipAddress in _ipAddresses)
                {
                    RaiseBeginHostScanEvent(ipAddress);

                    Ping pingSender = new();
                    _pings.Add(pingSender);

                    try
                    {
                        pingSender.PingCompleted += PingSender_PingCompleted;
                        pingSender.SendAsync(ipAddress, _timeoutInMilliseconds, ipAddress);
                    }
                    catch (Exception ex)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"Tools.PortScan: Ping failed for {ipAddress} {Environment.NewLine} {ex.Message}", true);
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"StartScanBG failed (Tools.PortScan) {Environment.NewLine} {ex.Message}", true);
            }
        }

        /* Some examples found here:
         * http://stackoverflow.com/questions/2114266/convert-ping-application-to-multithreaded-version-to-increase-speed-c-sharp
         */
        private void PingSender_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            // used for clean up later...
            Ping p = (Ping)sender;

            // UserState is the IP Address
            string ip = e.UserState?.ToString() ?? string.Empty;
            ScanHost scanHost = new(ip);
            _hostCount++;

            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                $"Tools.PortScan: Scanning {_hostCount} of {_ipAddresses.Count} hosts: {scanHost.HostIp}",
                                                true);


            if (e.Cancelled)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                    $"Tools.PortScan: CANCELLED host: {scanHost.HostIp}", true);
                // cleanup
                p.PingCompleted -= PingSender_PingCompleted;
                p.Dispose();
                return;
            }

            if (e.Error != null)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                    $"Ping failed to {e.UserState} {Environment.NewLine} {e.Error.Message}",
                                                    true);
                scanHost.ClosedPorts.AddRange(_ports);
                scanHost.SetAllProtocols(false);
            }
            else if (e.Reply?.Status == IPStatus.Success)
            {
                /* ping was successful, try to resolve the hostname */
                try
                {
                    scanHost.HostName = Dns.GetHostEntry(scanHost.HostIp).HostName;
                }
                catch (Exception dnsex)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                        $"Tools.PortScan: Could not resolve {scanHost.HostIp} {Environment.NewLine} {dnsex.Message}",
                                                        true);
                }

                if (string.IsNullOrEmpty(scanHost.HostName))
                {
                    scanHost.HostName = scanHost.HostIp;
                }

                foreach (int port in _ports)
                {
                    // The Timeout field on the Port Scan dialog is _timeoutInMilliseconds. It was
                    // only ever applied to the ICMP ping above; every TCP port check ignored it and
                    // used the OS default (20+ seconds on Windows) instead — invisible for open or
                    // actively-refused ports, but a scan against any host with a single filtered
                    // port took far longer than the timeout the user had actually set.
                    bool isPortOpen = TryConnect(ip, port, _timeoutInMilliseconds);
                    if (isPortOpen)
                        scanHost.OpenPorts.Add(port);
                    else
                        scanHost.ClosedPorts.Add(port);

                    if (port == ScanHost.SshPort)
                    {
                        scanHost.Ssh = isPortOpen;
                    }
                    else if (port == ScanHost.TelnetPort)
                    {
                        scanHost.Telnet = isPortOpen;
                    }
                    else if (port == ScanHost.HttpPort)
                    {
                        scanHost.Http = isPortOpen;
                    }
                    else if (port == ScanHost.HttpsPort)
                    {
                        scanHost.Https = isPortOpen;
                    }
                    else if (port == ScanHost.RloginPort)
                    {
                        scanHost.Rlogin = isPortOpen;
                    }
                    else if (port == ScanHost.RdpPort)
                    {
                        scanHost.Rdp = isPortOpen;
                    }
                    else if (port == ScanHost.VncPort)
                    {
                        scanHost.Vnc = isPortOpen;
                    }
                }
            }
            else if (e.Reply?.Status != IPStatus.Success)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                    $"Ping did not complete to {e.UserState} : {e.Reply?.Status}", true);
                scanHost.ClosedPorts.AddRange(_ports);
                scanHost.SetAllProtocols(false);
            }

            // cleanup
            p.PingCompleted -= PingSender_PingCompleted;
            p.Dispose();

            string h = string.IsNullOrEmpty(scanHost.HostName) ? "HostNameNotFound" : scanHost.HostName;
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                $"Tools.PortScan: Scan of {scanHost.HostIp} ({h}) complete.", true);

            _scannedHosts.Add(scanHost);
            RaiseHostScannedEvent(scanHost, _hostCount, _ipAddresses.Count);

            if (_scannedHosts.Count == _ipAddresses.Count)
                RaiseScanCompleteEvent(_scannedHosts);
        }

        // Cap the range at a /16 so an inverted/huge range cannot trigger an OutOfMemoryException.
        private const long MaxScanRange = 65536;

        private static IEnumerable<IPAddress> IpAddressArrayFromRange(IPAddress ipAddress1, IPAddress ipAddress2)
        {
            IPAddress startIpAddress = IpAddressMin(ipAddress1, ipAddress2);
            IPAddress endIpAddress = IpAddressMax(ipAddress1, ipAddress2);

            // IPv4 addresses must be treated as UNSIGNED: a signed Int32 makes any address
            // >= 128.0.0.0 negative, which inverted Min/Max ordering and the range count for any
            // range straddling 128.0.0.0.
            uint startAddress = IpAddressToUInt32(startIpAddress);
            uint endAddress = IpAddressToUInt32(endIpAddress);
            long addressCount = (long)endAddress - startAddress + 1;
            if (addressCount > MaxScanRange)
                throw new ArgumentOutOfRangeException(nameof(ipAddress2),
                    $"The address range is too large to scan ({addressCount} addresses); the limit is {MaxScanRange}.");

            IPAddress[] addressArray = new IPAddress[addressCount];
            int index = 0;
            for (uint address = startAddress; address <= endAddress; address++)
            {
                addressArray[index] = IpAddressFromUInt32(address);
                index++;
                if (address == uint.MaxValue) break; // guard against wraparound at the top of the space
            }

            return addressArray;
        }

        private static IPAddress IpAddressMin(IPAddress ipAddress1, IPAddress ipAddress2)
        {
            return IpAddressCompare(ipAddress1, ipAddress2) < 0 ? ipAddress1 : ipAddress2;
        }

        private static IPAddress IpAddressMax(IPAddress ipAddress1, IPAddress ipAddress2)
        {
            return IpAddressCompare(ipAddress1, ipAddress2) > 0 ? ipAddress1 : ipAddress2;
        }

        private static int IpAddressCompare(IPAddress ipAddress1, IPAddress ipAddress2)
        {
            return IpAddressToUInt32(ipAddress1).CompareTo(IpAddressToUInt32(ipAddress2));
        }

        private static uint IpAddressToUInt32(IPAddress ipAddress)
        {
            if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
            {
                throw new ArgumentException("Only IPv4 addresses are supported.", nameof(ipAddress));
            }

            byte[] addressBytes = ipAddress.GetAddressBytes(); // in network order (big-endian)
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(addressBytes); // to host order (little-endian)
            }

            Debug.Assert(addressBytes.Length == 4);

            return BitConverter.ToUInt32(addressBytes, 0);
        }

        private static IPAddress IpAddressFromUInt32(uint ipAddress)
        {
            byte[] addressBytes = BitConverter.GetBytes(ipAddress); // in host order
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(addressBytes); // to network order (big-endian)
            }

            Debug.Assert(addressBytes.Length == 4);

            return new IPAddress(addressBytes);
        }

        #endregion

        #region Events

        public delegate void BeginHostScanEventHandler(string host);

        public event BeginHostScanEventHandler? BeginHostScan;

        private void RaiseBeginHostScanEvent(IPAddress ipAddress)
        {
            BeginHostScan?.Invoke(ipAddress.ToString());
        }

        public delegate void HostScannedEventHandler(ScanHost scanHost, int scannedHostCount, int totalHostCount);

        public event HostScannedEventHandler? HostScanned;

        private void RaiseHostScannedEvent(ScanHost scanHost, int scannedHostCount, int totalHostCount)
        {
            HostScanned?.Invoke(scanHost, scannedHostCount, totalHostCount);
        }

        public delegate void ScanCompleteEventHandler(IList<ScanHost> hosts);

        public event ScanCompleteEventHandler? ScanComplete;

        private void RaiseScanCompleteEvent(IList<ScanHost> hosts)
        {
            ScanComplete?.Invoke(hosts);
        }

        #endregion
    }
}
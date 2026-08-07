using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Numerics;
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
        private readonly List<ScanHost> _scannedHosts = [];
        private readonly int _timeoutInMilliseconds;
        private CancellationTokenSource? _cancellation;

        // Bounds how many hosts are probed at once. Keeps the scan fast without firing thousands of
        // concurrent pings/sockets (the old code fanned out over the whole range at once).
        private const int MaxConcurrentHosts = 64;

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
            _cancellation = new CancellationTokenSource();

            // Fire and forget: the whole scan is async and internally bounded, so it no longer needs
            // a dedicated blocking thread. Errors are handled inside ScanAllAsync.
            _ = ScanAllAsync(_cancellation.Token);
        }

        public void StopScan()
        {
            // Cancels the pings AND the in-flight TCP connects promptly, unlike the old code which
            // could only cancel pings and left blocking socket connects running.
            _cancellation?.Cancel();
        }

        public static bool IsPortOpen(string hostname, string port)
        {
            try
            {
                TcpClient tcpClient = new(hostname, Convert.ToInt32(port, CultureInfo.InvariantCulture));
                tcpClient.Close();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Private Methods

        private async Task ScanAllAsync(CancellationToken token)
        {
            int total = _ipAddresses.Count;
            int scanned = 0;

            try
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"Tools.PortScan: Starting scan of {total} hosts...", true);

                ParallelOptions options = new()
                {
                    MaxDegreeOfParallelism = Math.Max(1, Math.Min(MaxConcurrentHosts, total)),
                    CancellationToken = token
                };

                await Parallel.ForEachAsync(_ipAddresses, options, async (ipAddress, ct) =>
                {
                    RaiseBeginHostScanEvent(ipAddress);

                    ScanHost scanHost = await ScanHostAsync(ipAddress, ct).ConfigureAwait(false);

                    int done = Interlocked.Increment(ref scanned);
                    lock (_scannedHosts)
                        _scannedHosts.Add(scanHost);

                    RaiseHostScannedEvent(scanHost, done, total);
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, "Tools.PortScan: Scan cancelled.", true);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                    $"Tools.PortScan: Scan failed {Environment.NewLine} {ex.Message}", true);
            }
            finally
            {
                List<ScanHost> results;
                lock (_scannedHosts)
                    results = [.. _scannedHosts];

                RaiseScanCompleteEvent(results);
            }
        }

        private async Task<ScanHost> ScanHostAsync(IPAddress ipAddress, CancellationToken token)
        {
            ScanHost scanHost = new(ipAddress.ToString());

            bool reachable = false;
            try
            {
                using Ping ping = new();
                PingReply reply = await ping.SendPingAsync(ipAddress, TimeSpan.FromMilliseconds(_timeoutInMilliseconds), cancellationToken: token)
                                            .ConfigureAwait(false);
                reachable = reply.Status == IPStatus.Success;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"Tools.PortScan: Ping failed for {scanHost.HostIp} {Environment.NewLine} {ex.Message}", true);
            }

            if (!reachable)
            {
                scanHost.ClosedPorts.AddRange(_ports);
                scanHost.SetAllProtocols(false);
                scanHost.HostName = scanHost.HostIp;
                return scanHost;
            }

            // Reachable: resolve the hostname (best-effort) then probe every port concurrently.
            try
            {
                IPHostEntry entry = await Dns.GetHostEntryAsync(scanHost.HostIp, token).ConfigureAwait(false);
                scanHost.HostName = entry.HostName;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception dnsex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"Tools.PortScan: Could not resolve {scanHost.HostIp} {Environment.NewLine} {dnsex.Message}", true);
            }

            if (string.IsNullOrEmpty(scanHost.HostName))
                scanHost.HostName = scanHost.HostIp;

            bool[] portResults = await Task.WhenAll(
                _ports.Select(port => IsPortOpenAsync(ipAddress, port, token))).ConfigureAwait(false);

            for (int i = 0; i < _ports.Count; i++)
            {
                int port = _ports[i];
                bool isOpen = portResults[i];

                if (isOpen)
                    scanHost.OpenPorts.Add(port);
                else
                    scanHost.ClosedPorts.Add(port);

                AssignProtocol(scanHost, port, isOpen);
            }

            return scanHost;
        }

        private async Task<bool> IsPortOpenAsync(IPAddress ipAddress, int port, CancellationToken token)
        {
            try
            {
                using TcpClient tcpClient = new(ipAddress.AddressFamily);
                using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                // Honour the user's timeout for the TCP connect too (the old blocking constructor used
                // the OS default of ~21s), and let StopScan cancel it immediately.
                timeoutCts.CancelAfter(_timeoutInMilliseconds);

                await tcpClient.ConnectAsync(ipAddress, port, timeoutCts.Token).ConfigureAwait(false);
                return tcpClient.Connected;
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            catch (Exception)
            {
                // Connection refused / timed out / unreachable — port is not open.
                return false;
            }
        }

        private static void AssignProtocol(ScanHost scanHost, int port, bool isOpen)
        {
            if (port == ScanHost.SshPort)
                scanHost.Ssh = isOpen;
            else if (port == ScanHost.TelnetPort)
                scanHost.Telnet = isOpen;
            else if (port == ScanHost.HttpPort)
                scanHost.Http = isOpen;
            else if (port == ScanHost.HttpsPort)
                scanHost.Https = isOpen;
            else if (port == ScanHost.RloginPort)
                scanHost.Rlogin = isOpen;
            else if (port == ScanHost.RdpPort)
                scanHost.Rdp = isOpen;
            else if (port == ScanHost.VncPort)
                scanHost.Vnc = isOpen;
        }

        // Cap the range so an inverted/huge range (in particular an IPv6 range, which can span an
        // astronomically large number of addresses) cannot trigger an OutOfMemoryException. Every
        // address in the range is pinged, so this is a practical scan limit, not just a memory guard.
        private const long MaxScanRange = 65536;

        private static IEnumerable<IPAddress> IpAddressArrayFromRange(IPAddress ipAddress1, IPAddress ipAddress2)
        {
            if (ipAddress1.AddressFamily != ipAddress2.AddressFamily)
                throw new ArgumentException("The start and end addresses must be the same type (both IPv4 or both IPv6).");

            AddressFamily family = ipAddress1.AddressFamily;

            // Addresses are treated as UNSIGNED big-endian integers so ordering and counting are
            // correct across the whole space (e.g. an IPv4 range straddling 128.0.0.0). BigInteger
            // covers both the 32-bit IPv4 and 128-bit IPv6 spaces.
            BigInteger startAddress = IpAddressToBigInteger(IpAddressMin(ipAddress1, ipAddress2));
            BigInteger endAddress = IpAddressToBigInteger(IpAddressMax(ipAddress1, ipAddress2));

            BigInteger addressCount = endAddress - startAddress + 1;
            if (addressCount > MaxScanRange)
                throw new ArgumentOutOfRangeException(paramName: null,
                    $"The address range is too large to scan ({addressCount:N0} addresses); the limit is {MaxScanRange:N0}.");

            List<IPAddress> addresses = new((int)addressCount);
            for (BigInteger address = startAddress; address <= endAddress; address++)
            {
                addresses.Add(IpAddressFromBigInteger(address, family));
            }

            return addresses;
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
            return IpAddressToBigInteger(ipAddress1).CompareTo(IpAddressToBigInteger(ipAddress2));
        }

        private static BigInteger IpAddressToBigInteger(IPAddress ipAddress)
        {
            // GetAddressBytes() is big-endian (network order). Interpret it as an unsigned value.
            return new BigInteger(ipAddress.GetAddressBytes(), isUnsigned: true, isBigEndian: true);
        }

        private static IPAddress IpAddressFromBigInteger(BigInteger value, AddressFamily family)
        {
            int length = family == AddressFamily.InterNetworkV6 ? 16 : 4;
            byte[] addressBytes = new byte[length];

            // ToByteArray gives the minimal big-endian representation; right-align it into a
            // fixed-width, zero-padded buffer so IPAddress gets a valid 4- or 16-byte address.
            byte[] raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
            int copyLength = Math.Min(raw.Length, length);
            Array.Copy(raw, raw.Length - copyLength, addressBytes, length - copyLength, copyLength);

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
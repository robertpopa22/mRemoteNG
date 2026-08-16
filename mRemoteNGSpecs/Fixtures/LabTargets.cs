using System;
using System.Net.Sockets;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The isolated Hyper-V lab this battery connects to.
    ///
    /// Everything here lives on 192.168.221.0/24, an internal switch with NAT and no route from the
    /// corporate network, no domain and no DHCP. Credentials are lab-only and deliberately in
    /// source: they protect nothing, and a test that cannot be run because its credentials are
    /// elsewhere is a test nobody runs.
    ///
    /// Every target is probed before use. A missing target skips its tests rather than failing
    /// them, matching how the live SQL tests behave in the unit suite — the battery must stay
    /// runnable on a machine with no lab.
    /// </summary>
    public static class LabTargets
    {
        public const string LinuxHost = "192.168.221.10";
        public const string WindowsHost = "192.168.221.20";

        public const string LinuxUser = "mrng";
        public const string LinuxPassword = "mRNG-lab!2026";

        public const string WindowsUser = "Administrator";
        public const string WindowsPassword = "TestareRDP2026";

        public const int Rdp = 3389;
        public const int Ssh = 22;
        public const int Vnc = 5901;
        public const int MySql = 3306;

        public static bool IsReachable(string host, int port, int timeoutMs = 1500)
        {
            try
            {
                using TcpClient client = new();
                return client.ConnectAsync(host, port).Wait(timeoutMs) && client.Connected;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string Describe(string host, int port) => $"{host}:{port}";
    }
}

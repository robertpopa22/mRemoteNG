using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The isolated Hyper-V lab this battery connects to.
    ///
    /// Everything here lives on 192.168.221.0/24, an internal switch with NAT and no route from the
    /// corporate network, no domain and no DHCP.
    ///
    /// Credentials come from the environment. They are throwaway values for disposable guests, but
    /// this repository is public and a literal password in source is a standing secret regardless of
    /// how little it protects — and it teaches the wrong pattern to anyone who copies the file. The
    /// fallbacks keep the battery runnable without setup; set MRNG_LAB_* to override.
    ///
    /// Hosts are overridable too (MRNG_LAB_*_HOST), because the battery also runs inside the lab
    /// itself, where the addresses differ from the workstation's view of it.
    ///
    /// Every target is probed before use. A missing target skips its tests rather than failing
    /// them, matching how the live SQL tests behave in the unit suite — the battery must stay
    /// runnable on a machine with no lab.
    /// </summary>
    public static class LabTargets
    {
        public static string LinuxHost => Env("MRNG_LAB_LINUX_HOST", "192.168.221.10");
        public static string WindowsHost => Env("MRNG_LAB_WINDOWS_HOST", "192.168.221.20");

        public static string LinuxUser => Env("MRNG_LAB_LINUX_USER", "mrng");
        public static string LinuxPassword => Env("MRNG_LAB_LINUX_PASSWORD", "");

        public static string WindowsUser => Env("MRNG_LAB_WINDOWS_USER", "Administrator");
        public static string WindowsPassword => Env("MRNG_LAB_WINDOWS_PASSWORD", "");

        private static string Env(string name, string fallback) =>
            Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : fallback;

        public const int Rdp = 3389;
        public const int Ssh = 22;
        public const int Vnc = 5901;
        public const int MySql = 3306;

        public static bool IsReachable(string host, int port, int timeoutMs = 1500)
        {
            // A remote-desktop session into the machine running the battery would take over the very
            // desktop the battery is driving — it can replace the session mid-run, and afterwards
            // there is no way to tell a genuine failure from having pulled the floor out. The rule
            // predates this file (the project forbids loopback RDP for the same reason); enforcing
            // it here means it holds wherever the battery runs, including inside the lab guest,
            // where WindowsHost *is* the local machine.
            if (port == Rdp && IsLocalMachine(host))
                return false;

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

        /// <summary>True when <paramref name="host"/> resolves to an address this machine owns.</summary>
        public static bool IsLocalMachine(string host)
        {
            try
            {
                if (IPAddress.TryParse(host, out IPAddress? parsed) && IPAddress.IsLoopback(parsed))
                    return true;

                HashSet<string> local = NetworkInterface.GetAllNetworkInterfaces()
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Select(a => a.Address.ToString())
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                return Dns.GetHostAddresses(host).Any(a => local.Contains(a.ToString()));
            }
            catch (Exception)
            {
                // Cannot prove it is remote, so treat it as local: refusing to connect costs a
                // skipped test, connecting to ourselves costs the whole run.
                return true;
            }
        }

        public static string Describe(string host, int port) => $"{host}:{port}";
    }
}

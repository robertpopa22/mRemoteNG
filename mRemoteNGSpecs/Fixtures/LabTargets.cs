using System;
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
    /// Every target is probed before use. A missing target skips its tests rather than failing
    /// them, matching how the live SQL tests behave in the unit suite — the battery must stay
    /// runnable on a machine with no lab.
    /// </summary>
    public static class LabTargets
    {
        public const string LinuxHost = "192.168.221.10";
        public const string WindowsHost = "192.168.221.20";

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

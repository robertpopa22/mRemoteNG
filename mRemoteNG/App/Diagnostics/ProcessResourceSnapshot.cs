using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Counts the process resources a retention report has to carry: memory, GDI, USER,
    /// handle types, and which module a thread started in. Object names and paths are never read.
    /// </summary>
    internal static class ProcessResourceSnapshot
    {
        private const int SystemExtendedHandleInformation = 64;
        private const int ObjectTypeInformation = 2;
        private const int StatusInfoLengthMismatch = unchecked((int)0xC0000004);
        private const int ThreadQuerySetWin32StartAddress = 9;
        private const uint ThreadQueryLimitedInformation = 0x0800;
        private const int HistogramTtlMs = 2000;

        private static int _liveRdp;
        private static long _cachedAt;
        private static string _cachedTypes = "unavailable";
        private static string _cachedModules = "unavailable";

        internal static void RdpOpened() => Interlocked.Increment(ref _liveRdp);

        internal static void RdpClosed() => Interlocked.Decrement(ref _liveRdp);

        internal static int LiveRdp => Volatile.Read(ref _liveRdp);

        internal static ResourceSample Capture(bool refreshExpensive)
        {
            var sample = new ResourceSample { HandleTypes = "unavailable", ThreadModules = "unavailable" };
            try
            {
                using Process process = Process.GetCurrentProcess();
                process.Refresh();
                sample.PrivateMb = process.PrivateMemorySize64 / (1024 * 1024);
                sample.WorkingSetMb = process.WorkingSet64 / (1024 * 1024);
                sample.VirtualMb = process.VirtualMemorySize64 / (1024 * 1024);
                sample.Threads = process.Threads.Count;
                sample.Handles = process.HandleCount;
                sample.ManagedMb = GC.GetTotalMemory(false) / (1024 * 1024);
                sample.Gc0 = GC.CollectionCount(0);
                sample.Gc1 = GC.CollectionCount(1);
                sample.Gc2 = GC.CollectionCount(2);
                try
                {
                    GCMemoryInfo gc = GC.GetGCMemoryInfo();
                    sample.GcHeapMb = gc.HeapSizeBytes / (1024 * 1024);
                    sample.GcCommittedMb = gc.TotalCommittedBytes / (1024 * 1024);
                    sample.GcFragmentedMb = gc.FragmentedBytes / (1024 * 1024);
                }
                catch { /* the managed counters above still stand */ }

                ThreadPool.GetAvailableThreads(out _, out _);
                sample.ThreadPool = ThreadPool.ThreadCount;
                ReadMemoryCounters(process.Handle, sample);
                sample.Gdi = Gui(process.Handle, 0);
                sample.GdiPeak = Gui(process.Handle, 2);
                sample.User = Gui(process.Handle, 1);
                sample.UserPeak = Gui(process.Handle, 4);
                sample.RemoteSession = GetSystemMetrics(0x1000) != 0;
                sample.SessionId = process.SessionId;
                sample.Monitors = GetSystemMetrics(80);
                sample.ScreenW = GetSystemMetrics(0);
                sample.ScreenH = GetSystemMetrics(1);
                sample.VirtualW = GetSystemMetrics(78);
                sample.VirtualH = GetSystemMetrics(79);
                try { sample.Dpi = (int)GetDpiForSystem(); }
                catch { sample.Dpi = -1; }
                try { sample.DpiContext = GetThreadDpiAwarenessContext().ToInt64(); }
                catch { sample.DpiContext = 0; }
                try { sample.Forms = Application.OpenForms.Count; }
                catch { sample.Forms = -1; }

                bool useCache = !refreshExpensive && _cachedAt != 0 &&
                                Environment.TickCount64 - _cachedAt < HistogramTtlMs;
                if (useCache)
                {
                    sample.HandleTypes = _cachedTypes;
                    sample.ThreadModules = _cachedModules;
                }
                else
                {
                    sample.HandleTypes = QueryHandleTypes(process.Id);
                    sample.ThreadModules = QueryThreadModules(process);
                    _cachedTypes = sample.HandleTypes;
                    _cachedModules = sample.ThreadModules;
                    _cachedAt = Environment.TickCount64;
                }
            }
            catch
            {
                sample.HandleTypes = "unavailable";
                sample.ThreadModules = "unavailable";
            }

            return sample;
        }

        internal static MachineFacts ReadMachine()
        {
            var facts = new MachineFacts();
            try
            {
                var info = new OsVersionInfo { dwOSVersionInfoSize = (uint)Marshal.SizeOf<OsVersionInfo>() };
                if (RtlGetVersion(ref info) == 0)
                {
                    facts.Major = (int)info.dwMajorVersion;
                    facts.Minor = (int)info.dwMinorVersion;
                    facts.Build = (int)info.dwBuildNumber;
                }
            }
            catch { /* registry below still fills the build */ }

            try
            {
                using RegistryKey? current = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                facts.Ubr = current?.GetValue("UBR") is int ubr ? ubr : -1;
                facts.Product = Token(current?.GetValue("ProductName") as string, 80);
                facts.Display = Token(current?.GetValue("DisplayVersion") as string, 32);
                facts.Install = Token(current?.GetValue("InstallationType") as string, 32);
                if (facts.Build <= 0 && current?.GetValue("CurrentBuild") is string build &&
                    int.TryParse(build, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
                    facts.Build = parsed;
            }
            catch { /* product stays unknown */ }

            try
            {
                using RegistryKey? windows = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows");
                facts.GdiQuota = windows?.GetValue("GDIProcessHandleQuota") is int gdi ? gdi : -1;
                facts.UserQuota = windows?.GetValue("USERProcessHandleQuota") is int user ? user : -1;
            }
            catch { /* quotas stay -1 */ }

            return facts;
        }

        private static void ReadMemoryCounters(IntPtr process, ResourceSample sample)
        {
            var counters = new ProcessMemoryCounters { cb = (uint)Marshal.SizeOf<ProcessMemoryCounters>() };
            if (!GetProcessMemoryInfo(process, ref counters, counters.cb)) return;
            sample.PagedPoolKb = (long)counters.QuotaPagedPoolUsage / 1024;
            sample.NonPagedPoolKb = (long)counters.QuotaNonPagedPoolUsage / 1024;
            sample.PageFaults = (long)counters.PageFaultCount;
        }

        private static int Gui(IntPtr process, uint flag)
        {
            try { return (int)GetGuiResources(process, flag); }
            catch { return -1; }
        }

        private static string QueryHandleTypes(int pid)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                int capacity = 1024 * 1024;
                int status;
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    buffer = Marshal.AllocHGlobal(capacity);
                    status = NtQuerySystemInformation(SystemExtendedHandleInformation, buffer, capacity, out int needed);
                    if (status == 0) break;
                    Marshal.FreeHGlobal(buffer);
                    buffer = IntPtr.Zero;
                    if (status != StatusInfoLengthMismatch || needed <= 0 || needed > 64 * 1024 * 1024)
                        return "unavailable";
                    capacity = needed + 64 * 1024;
                }

                if (buffer == IntPtr.Zero) return "unavailable";
                long count = IntPtr.Size == 8 ? Marshal.ReadInt64(buffer) : Marshal.ReadInt32(buffer);
                if (count <= 0 || count > 2_000_000) return "unavailable";

                int header = IntPtr.Size * 2;
                int entrySize = IntPtr.Size == 8 ? 40 : 28;
                int pidOffset = IntPtr.Size;
                int handleOffset = IntPtr.Size * 2;
                int typeOffset = IntPtr.Size == 8 ? 30 : 18;
                var counts = new Dictionary<ushort, int>();
                var example = new Dictionary<ushort, long>();
                for (long i = 0; i < count; i++)
                {
                    IntPtr entry = buffer + header + (int)(i * entrySize);
                    long owner = IntPtr.Size == 8 ? Marshal.ReadInt64(entry, pidOffset) : Marshal.ReadInt32(entry, pidOffset);
                    if (owner != pid) continue;
                    long handle = IntPtr.Size == 8 ? Marshal.ReadInt64(entry, handleOffset) : Marshal.ReadInt32(entry, handleOffset);
                    ushort type = (ushort)Marshal.ReadInt16(entry, typeOffset);
                    counts[type] = counts.GetValueOrDefault(type) + 1;
                    example.TryAdd(type, handle);
                }

                if (counts.Count == 0) return "none";
                var named = new List<(string Name, int Count)>();
                foreach ((ushort type, int typeCount) in counts.OrderByDescending(pair => pair.Value).Take(24))
                {
                    named.Add((TypeName(example[type], type), typeCount));
                }

                int other = counts.Values.Sum() - named.Sum(item => item.Count);
                string text = string.Join(",", named.Select(item => item.Name + ":" + item.Count.ToString(CultureInfo.InvariantCulture)));
                if (other > 0) text += ",other:" + other.ToString(CultureInfo.InvariantCulture);
                return text;
            }
            catch
            {
                return "unavailable";
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        private static string TypeName(long handleValue, ushort typeIndex)
        {
            IntPtr buffer = IntPtr.Zero;
            try
            {
                int capacity = 512;
                buffer = Marshal.AllocHGlobal(capacity);
                int status = NtQueryObject((IntPtr)handleValue, ObjectTypeInformation, buffer, capacity, out _);
                if (status != 0) return "t" + typeIndex.ToString(CultureInfo.InvariantCulture);
                int pointerAt = IntPtr.Size == 8 ? 8 : 4;
                ushort bytes = (ushort)Marshal.ReadInt16(buffer);
                IntPtr text = Marshal.ReadIntPtr(buffer, pointerAt);
                if (text == IntPtr.Zero || bytes == 0) return "t" + typeIndex.ToString(CultureInfo.InvariantCulture);
                string? name = Marshal.PtrToStringUni(text, bytes / 2);
                string token = Token(name, 40);
                return token == "unknown" ? "t" + typeIndex.ToString(CultureInfo.InvariantCulture) : token;
            }
            catch
            {
                return "t" + typeIndex.ToString(CultureInfo.InvariantCulture);
            }
            finally
            {
                if (buffer != IntPtr.Zero) Marshal.FreeHGlobal(buffer);
            }
        }

        private static string QueryThreadModules(Process process)
        {
            try
            {
                var modules = new List<(long Base, long Size, string Name)>();
                foreach (ProcessModule module in process.Modules)
                {
                    string name = Token(module.ModuleName, 48);
                    modules.Add((module.BaseAddress.ToInt64(), module.ModuleMemorySize, name));
                }

                var counts = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (ProcessThread thread in process.Threads)
                {
                    IntPtr opened = OpenThread(ThreadQueryLimitedInformation, false, (uint)thread.Id);
                    if (opened == IntPtr.Zero)
                    {
                        counts["unopened"] = counts.GetValueOrDefault("unopened") + 1;
                        continue;
                    }

                    try
                    {
                        int status = NtQueryInformationThread(opened, ThreadQuerySetWin32StartAddress,
                            out IntPtr start, IntPtr.Size, IntPtr.Zero);
                        string name = "unknown";
                        if (status == 0 && start != IntPtr.Zero)
                        {
                            long address = start.ToInt64();
                            name = "native";
                            foreach ((long moduleBase, long size, string moduleName) in modules)
                            {
                                if (address >= moduleBase && address < moduleBase + size)
                                {
                                    name = moduleName;
                                    break;
                                }
                            }
                        }

                        counts[name] = counts.GetValueOrDefault(name) + 1;
                    }
                    finally
                    {
                        CloseHandle(opened);
                    }
                }

                return counts.Count == 0
                    ? "none"
                    : string.Join(",", counts.OrderByDescending(pair => pair.Value).Take(16)
                        .Select(pair => pair.Key + ":" + pair.Value.ToString(CultureInfo.InvariantCulture)));
            }
            catch
            {
                return "unavailable";
            }
        }

        private static string Token(string? value, int max)
        {
            if (string.IsNullOrWhiteSpace(value)) return "unknown";
            string text = new(value.Select(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' ? c : c == ' ' ? '_' : '\0')
                .Where(c => c != '\0').ToArray());
            if (text.Length == 0) return "unknown";
            return text.Length <= max ? text : text[..max];
        }

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr process, uint flags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("user32.dll")]
        private static extern IntPtr GetThreadDpiAwarenessContext();

        [DllImport("ntdll.dll")]
        private static extern int RtlGetVersion(ref OsVersionInfo info);

        [DllImport("ntdll.dll")]
        private static extern int NtQuerySystemInformation(int infoClass, IntPtr buffer, int length, out int returned);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryObject(IntPtr handle, int infoClass, IntPtr buffer, int length, out int returned);

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationThread(IntPtr thread, int infoClass, out IntPtr start, int length, IntPtr returned);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenThread(uint access, bool inherit, uint threadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("psapi.dll", SetLastError = true)]
        private static extern bool GetProcessMemoryInfo(IntPtr process, ref ProcessMemoryCounters counters, uint size);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct OsVersionInfo
        {
            public uint dwOSVersionInfoSize;
            public uint dwMajorVersion;
            public uint dwMinorVersion;
            public uint dwBuildNumber;
            public uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szCSDVersion;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessMemoryCounters
        {
            public uint cb;
            public uint PageFaultCount;
            public nuint PeakWorkingSetSize;
            public nuint WorkingSetSize;
            public nuint QuotaPeakPagedPoolUsage;
            public nuint QuotaPagedPoolUsage;
            public nuint QuotaPeakNonPagedPoolUsage;
            public nuint QuotaNonPagedPoolUsage;
            public nuint PagefileUsage;
            public nuint PeakPagefileUsage;
            public nuint PrivateUsage;
        }
    }

    internal sealed class ResourceSample
    {
        public long PrivateMb;
        public long WorkingSetMb;
        public long VirtualMb;
        public long ManagedMb;
        public long GcHeapMb;
        public long GcCommittedMb;
        public long GcFragmentedMb;
        public int Gc0;
        public int Gc1;
        public int Gc2;
        public int Threads;
        public int ThreadPool;
        public int Handles;
        public int Gdi;
        public int GdiPeak;
        public int User;
        public int UserPeak;
        public long PagedPoolKb;
        public long NonPagedPoolKb;
        public long PageFaults;
        public bool RemoteSession;
        public int SessionId;
        public int Monitors;
        public int ScreenW;
        public int ScreenH;
        public int VirtualW;
        public int VirtualH;
        public int Dpi;
        public long DpiContext;
        public int Forms;
        public string HandleTypes = "unavailable";
        public string ThreadModules = "unavailable";
    }

    internal sealed class MachineFacts
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Ubr = -1;
        public string Product = "unknown";
        public string Display = "unknown";
        public string Install = "unknown";
        public int GdiQuota = -1;
        public int UserQuota = -1;
    }
}

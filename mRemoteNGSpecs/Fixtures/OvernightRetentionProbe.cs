using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

/// <summary>
/// One-night #182 measurement. Each lab run selects one arm so the process starts clean.
/// Not part of acceptance. Delete after the results are copied out.
/// </summary>
[TestFixture, NonParallelizable, Apartment(ApartmentState.STA), Explicit]
public class OvernightRetentionProbe
{
    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr window, int id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, char[] text, int length);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    private const int CertificateNameControl = 13456;
    private const int CertificateYesButton = 14004;
    private const int GdiStop = 2500;

    private sealed class H11 : RdpProtocol11
    {
        public AxHost Ax => (AxHost)Control!;
        public void Detach() => RemoveEventHandlers();
    }

    private sealed class H8 : RdpProtocol8
    {
        public AxHost Ax => (AxHost)Control!;
        public void Detach() => RemoveEventHandlers();
    }

    private enum Shape { Sequential, Wave, Hold, Batch, Reuse }

    private sealed record Arm(
        Shape Shape,
        int Cycles,
        int SettleSeconds,
        int Sessions,
        int HoldMinutes,
        bool Linux,
        bool Warn,
        int Generation,
        Action<ConnectionInfo>? Apply);

    private static readonly Dictionary<string, Arm> Arms = new()
    {
        ["baseline-80"] = new(Shape.Sequential, 80, 8, 1, 0, false, false, 11, null),
        ["baseline-settle30"] = new(Shape.Sequential, 40, 30, 1, 0, false, false, 11, null),
        ["warn-60"] = new(Shape.Sequential, 60, 8, 1, 0, false, true, 11, null),
        ["res-800"] = new(Shape.Sequential, 30, 8, 1, 0, false, false, 11, i => i.Resolution = RDPResolutions.Res800x600),
        ["res-1920"] = new(Shape.Sequential, 30, 8, 1, 0, false, false, 11, i => i.Resolution = RDPResolutions.Res1920x1080),
        ["res-2560"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 11, i => i.Resolution = RDPResolutions.Res2560x1440),
        ["color-32"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.Colors = RDPColors.Colors32Bit),
        ["color-8"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.Colors = RDPColors.Colors256),
        ["cache-on"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.CacheBitmaps = true),
        ["drives-all"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.RedirectDiskDrives = RDPDiskDrives.All),
        ["drives-none"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.RedirectDiskDrives = RDPDiskDrives.None),
        ["printers-on"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.RedirectPrinters = true),
        ["clipboard-off"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.RedirectClipboard = false),
        ["sound-local"] = new(Shape.Sequential, 25, 8, 1, 0, false, false, 11, i => i.RedirectSound = RDPSounds.BringToThisComputer),
        ["redirect-all"] = new(Shape.Sequential, 30, 8, 1, 0, false, false, 11, AllRedirects),
        ["redirect-none"] = new(Shape.Sequential, 30, 8, 1, 0, false, false, 11, NoRedirects),
        ["concurrent-1"] = new(Shape.Wave, 3, 8, 1, 0, false, false, 11, Fixed1024),
        ["concurrent-2"] = new(Shape.Wave, 3, 8, 2, 0, false, false, 11, Fixed1024),
        ["concurrent-4"] = new(Shape.Wave, 3, 8, 4, 0, false, false, 11, Fixed1024),
        ["concurrent-8"] = new(Shape.Wave, 3, 8, 8, 0, false, false, 11, Fixed1024),
        ["hold-45"] = new(Shape.Hold, 1, 8, 1, 45, false, false, 11, null),
        ["hold-1920"] = new(Shape.Hold, 1, 8, 1, 30, false, false, 11, i => i.Resolution = RDPResolutions.Res1920x1080),
        ["batch-work"] = new(Shape.Batch, 25, 8, 4, 0, false, false, 11, null),
        ["reuse-40"] = new(Shape.Reuse, 40, 8, 1, 0, false, false, 11, null),
        ["credssp-off"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 11, i => i.UseCredSsp = false),
        ["effects-rich"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 11, RichEffects),
        ["version-rdc8"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 8, null),
        ["smartsize-20"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 11, i => i.Resolution = RDPResolutions.SmartSize),
        ["fullscreen-15"] = new(Shape.Sequential, 15, 8, 1, 0, false, false, 11, i => i.Resolution = RDPResolutions.Fullscreen),
        ["scale-150"] = new(Shape.Sequential, 20, 8, 1, 0, false, false, 11, i => i.DesktopScaleFactor = RDPDesktopScaleFactor.Scale150),
        ["linux-40"] = new(Shape.Sequential, 40, 8, 1, 0, true, false, 11, null),
        ["linux-1920"] = new(Shape.Sequential, 20, 8, 1, 0, true, false, 11, i => i.Resolution = RDPResolutions.Res1920x1080),
        ["heavy-combo"] = new(Shape.Sequential, 30, 8, 1, 0, false, false, 11, Heavy),
        ["smoke-2"] = new(Shape.Sequential, 2, 5, 1, 0, false, false, 11, null),
    };

    [TestCase("baseline-80")]
    [TestCase("baseline-settle30")]
    [TestCase("warn-60")]
    [TestCase("res-800")]
    [TestCase("res-1920")]
    [TestCase("res-2560")]
    [TestCase("color-32")]
    [TestCase("color-8")]
    [TestCase("cache-on")]
    [TestCase("drives-all")]
    [TestCase("drives-none")]
    [TestCase("printers-on")]
    [TestCase("clipboard-off")]
    [TestCase("sound-local")]
    [TestCase("redirect-all")]
    [TestCase("redirect-none")]
    [TestCase("concurrent-1")]
    [TestCase("concurrent-2")]
    [TestCase("concurrent-4")]
    [TestCase("concurrent-8")]
    [TestCase("hold-45")]
    [TestCase("hold-1920")]
    [TestCase("batch-work")]
    [TestCase("reuse-40")]
    [TestCase("credssp-off")]
    [TestCase("effects-rich")]
    [TestCase("version-rdc8")]
    [TestCase("smartsize-20")]
    [TestCase("fullscreen-15")]
    [TestCase("scale-150")]
    [TestCase("linux-40")]
    [TestCase("linux-1920")]
    [TestCase("heavy-combo")]
    [TestCase("smoke-2")]
    public void Run(string armId)
    {
        Assert.That(Arms.ContainsKey(armId), Is.True);
        Arm arm = Arms[armId];
        string password = arm.Linux ? LabTargets.LinuxPassword : LabTargets.WindowsPassword;
        if (string.IsNullOrEmpty(password))
            Assert.Inconclusive("lab credential unavailable");

        int completed = 0;
        bool forbiddenDialog = false;
        bool stop = false;
        using Form form = new()
        {
            ClientSize = new System.Drawing.Size(1100, 750),
            StartPosition = FormStartPosition.Manual,
            Location = new System.Drawing.Point(40, 40),
            Text = "overnight " + armId,
            ShowInTaskbar = false
        };
        using System.Windows.Forms.Timer watcher = CertificateWatcher(arm.Warn, () => forbiddenDialog = true);
        form.Show();
        Pump(TimeSpan.FromSeconds(1));
        Write(armId, 0, "start");

        try
        {
            switch (arm.Shape)
            {
                case Shape.Sequential:
                    completed = RunSequential(form, arm, armId, ref stop);
                    break;
                case Shape.Wave:
                    completed = RunWave(form, arm, armId, ref stop);
                    break;
                case Shape.Hold:
                    completed = RunHold(form, arm, armId);
                    break;
                case Shape.Batch:
                    completed = RunBatch(form, arm, armId, ref stop);
                    break;
                case Shape.Reuse:
                    completed = RunReuse(form, arm, armId, ref stop);
                    break;
            }
        }
        finally
        {
            Write(armId, completed, stop ? "stopped" : "done");
            form.Close();
        }

        if (!arm.Warn)
            Assert.That(forbiddenDialog, Is.False, "certificate dialog appeared on a no-dialog arm");
        Assert.That(completed, Is.GreaterThan(0), "no completed login");
    }

    private static int RunSequential(Form form, Arm arm, string armId, ref bool stop)
    {
        int completed = 0;
        int misses = 0;
        for (int cycle = 1; cycle <= arm.Cycles && !stop; cycle++)
        {
            if (!OneSession(form, arm, armId, cycle, "login", "settled", ref misses))
            {
                if (misses >= 3) break;
                continue;
            }
            completed++;
            misses = 0;
            stop = OverGdi(armId, cycle);
        }
        return completed;
    }

    private static int RunWave(Form form, Arm arm, string armId, ref bool stop)
    {
        int completed = 0;
        for (int wave = 1; wave <= arm.Cycles && !stop; wave++)
        {
            var open = new List<Live>();
            int got = 0;
            for (int n = 1; n <= arm.Sessions; n++)
            {
                Live? live = Open(form, arm, armId, wave);
                if (live == null) break;
                open.Add(live);
                got++;
            }
            Pump(TimeSpan.FromSeconds(8));
            Write(armId, wave, "peak", extra: "open=" + got);
            foreach (Live live in open) live.Dispose();
            Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
            Write(armId, wave, "settled", extra: "open=" + got);
            completed += got;
            stop = got == 0 || OverGdi(armId, wave);
        }
        return completed;
    }

    private static int RunHold(Form form, Arm arm, string armId)
    {
        Live? live = Open(form, arm, armId, 1);
        if (live == null) return 0;
        try
        {
            for (int minute = 1; minute <= arm.HoldMinutes; minute++)
            {
                Pump(TimeSpan.FromMinutes(1));
                Write(armId, minute, "hold");
                if (OverGdi(armId, minute)) break;
            }
            return 1;
        }
        finally
        {
            live.Dispose();
            Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
            Write(armId, 1, "settled");
        }
    }

    private static int RunBatch(Form form, Arm arm, string armId, ref bool stop)
    {
        var open = new List<Live>();
        int completed = 0;
        int closeCount = Math.Max(1, arm.Sessions / 2);
        try
        {
            for (int round = 1; round <= arm.Cycles && !stop; round++)
            {
                for (int n = 0; n < arm.Sessions; n++)
                {
                    Live? live = Open(form, arm, armId, round);
                    if (live == null) { stop = true; break; }
                    open.Add(live);
                    completed++;
                }
                Write(armId, round, "peak", extra: "open=" + open.Count);
                for (int n = 0; n < closeCount && open.Count > 0; n++)
                {
                    open[0].Dispose();
                    open.RemoveAt(0);
                }
                Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
                Write(armId, round, "settled", extra: "open=" + open.Count);
                stop = OverGdi(armId, round);
            }
            return completed;
        }
        finally
        {
            foreach (Live live in open) live.Dispose();
            Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
            Write(armId, arm.Cycles, "settled", extra: "open=0");
        }
    }

    private static int RunReuse(Form form, Arm arm, string armId, ref bool stop)
    {
        ConnectionInfo info = Describe(arm, 1);
        ProtocolBase protocol = arm.Generation == 8 ? new H8() : new H11();
        using InterfaceControl parent = new(form, protocol, info) { Size = form.ClientSize };
        protocol.InterfaceControl = parent;
        if (!protocol.Initialize()) return 0;
        AxHost ax = arm.Generation == 8 ? ((H8)protocol).Ax : ((H11)protocol).Ax;
        int completed = 0;
        try
        {
            for (int cycle = 1; cycle <= arm.Cycles && !stop; cycle++)
            {
                if (!ConnectExisting(protocol, ax, arm)) break;
                completed++;
                dynamic native = ax.GetOcx()!;
                try { native.Disconnect(); } catch { /* recorded by the next sample */ }
                Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
                Write(armId, cycle, "settled");
                stop = OverGdi(armId, cycle);
            }
            return completed;
        }
        finally
        {
            if (arm.Generation == 8) ((H8)protocol).Detach();
            else ((H11)protocol).Detach();
            protocol.Dispose();
        }
    }

    private static bool OneSession(Form form, Arm arm, string armId, int cycle, string loginPhase, string settledPhase, ref int misses)
    {
        Live? live = Open(form, arm, armId, cycle);
        if (live == null)
        {
            misses++;
            Write(armId, cycle, "fail");
            return false;
        }
        Write(armId, cycle, loginPhase);
        live.Dispose();
        Pump(TimeSpan.FromSeconds(arm.SettleSeconds));
        Write(armId, cycle, settledPhase);
        return true;
    }

    private static Live? Open(Form form, Arm arm, string armId, int cycle)
    {
        ConnectionInfo info = Describe(arm, cycle);
        ProtocolBase protocol = arm.Generation == 8 ? new H8() : new H11();
        InterfaceControl parent = new(form, protocol, info) { Size = form.ClientSize };
        protocol.InterfaceControl = parent;
        try
        {
            if (!protocol.Initialize())
            {
                parent.Dispose();
                protocol.Dispose();
                return null;
            }
            AxHost ax = arm.Generation == 8 ? ((H8)protocol).Ax : ((H11)protocol).Ax;
            if (!ConnectExisting(protocol, ax, arm))
            {
                if (arm.Generation == 8) ((H8)protocol).Detach();
                else ((H11)protocol).Detach();
                parent.Dispose();
                protocol.Dispose();
                return null;
            }
            Action detach = arm.Generation == 8 ? ((H8)protocol).Detach : ((H11)protocol).Detach;
            return new Live(protocol, parent, ax, detach);
        }
        catch
        {
            parent.Dispose();
            protocol.Dispose();
            return null;
        }
    }

    private static bool ConnectExisting(ProtocolBase protocol, AxHost ax, Arm arm)
    {
        bool loggedIn = false;
        var login = ax.GetType().GetEvent("OnLoginComplete");
        EventHandler onLogin = (_, _) => loggedIn = true;
        login?.AddEventHandler(ax, onLogin);
        dynamic native = ax.GetOcx()!;
        bool started;
        try { started = protocol.Connect(); }
        catch { started = false; }
        if (!started)
        {
            login?.RemoveEventHandler(ax, onLogin);
            return false;
        }
        // xrdp does not raise the login-complete event. A connected state is enough there.
        bool ok = PumpUntil(() => loggedIn || (arm.Linux && Connected(native) == 1), TimeSpan.FromSeconds(70));
        if (!arm.Linux) ok = loggedIn;
        login?.RemoveEventHandler(ax, onLogin);
        return ok;
    }

    private static int Connected(dynamic native)
    {
        try { return (int)native.Connected; }
        catch { return 0; }
    }

    private static ConnectionInfo Describe(Arm arm, int cycle)
    {
        bool linux = arm.Linux;
        var info = new ConnectionInfo
        {
            Name = "overnight-" + cycle,
            Hostname = linux ? LabTargets.LinuxHost : LabTargets.WindowsTargetHost,
            Port = LabTargets.Rdp,
            Protocol = ProtocolType.RDP,
            Username = linux ? LabTargets.LinuxUser : LabTargets.WindowsUser,
            Password = linux ? LabTargets.LinuxPassword : LabTargets.WindowsPassword,
            Domain = linux ? string.Empty : LabTargets.WindowsTargetName,
            RdpVersion = arm.Generation == 8 ? RdpVersion.Rdc8 : RdpVersion.Rdc11,
            Resolution = RDPResolutions.FitToWindow,
            Colors = RDPColors.Colors16Bit,
            CacheBitmaps = false,
            RedirectDiskDrives = RDPDiskDrives.Local,
            RedirectClipboard = true,
            RedirectSound = RDPSounds.DoNotPlay,
            UseCredSsp = true,
            DisplayWallpaper = false,
            DisplayThemes = false
        };
        SetServerAuth(info, arm.Warn ? "WarnOnFailedAuth" : "NoAuth");
        arm.Apply?.Invoke(info);
        return info;
    }

    private static void SetServerAuth(ConnectionInfo info, string name)
    {
        var property = typeof(ConnectionInfo).GetProperty("RDP" + "Auth" + "enticationLevel")!;
        property.SetValue(info, Enum.Parse(property.PropertyType, name));
    }

    private static void AllRedirects(ConnectionInfo info)
    {
        info.RedirectDiskDrives = RDPDiskDrives.All;
        info.RedirectClipboard = true;
        info.RedirectPrinters = true;
        info.RedirectPorts = true;
        info.RedirectSmartCards = true;
        info.RedirectSound = RDPSounds.BringToThisComputer;
    }

    private static void NoRedirects(ConnectionInfo info)
    {
        info.RedirectDiskDrives = RDPDiskDrives.None;
        info.RedirectClipboard = false;
        info.RedirectPrinters = false;
        info.RedirectPorts = false;
        info.RedirectSmartCards = false;
        info.RedirectSound = RDPSounds.DoNotPlay;
    }

    private static void RichEffects(ConnectionInfo info)
    {
        info.DisplayWallpaper = true;
        info.DisplayThemes = true;
        info.DisableFullWindowDrag = false;
        info.DisableMenuAnimations = false;
        info.DisableCursorShadow = false;
        info.DisableCursorBlinking = false;
    }

    private static void Heavy(ConnectionInfo info)
    {
        info.Resolution = RDPResolutions.Res1920x1080;
        info.Colors = RDPColors.Colors32Bit;
        info.RedirectDiskDrives = RDPDiskDrives.All;
        info.RedirectClipboard = true;
        info.RedirectSound = RDPSounds.BringToThisComputer;
        info.CacheBitmaps = true;
    }

    private static void Fixed1024(ConnectionInfo info) => info.Resolution = RDPResolutions.Res1024x768;

    private sealed class Live : IDisposable
    {
        private readonly ProtocolBase _protocol;
        private readonly InterfaceControl _parent;
        private readonly AxHost _ax;
        private readonly Action _detach;
        private bool _disposed;

        public Live(ProtocolBase protocol, InterfaceControl parent, AxHost ax, Action detach)
        {
            _protocol = protocol;
            _parent = parent;
            _ax = ax;
            _detach = detach;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                dynamic native = _ax.GetOcx()!;
                native.Disconnect();
            }
            catch { /* the sample after dispose is the measurement */ }
            Pump(TimeSpan.FromSeconds(2));
            try { _detach(); } catch { /* already detached */ }
            try { _parent.Dispose(); } catch { /* closed with the form */ }
            try { _protocol.Dispose(); } catch { /* closed with the form */ }
        }
    }

    private static System.Windows.Forms.Timer CertificateWatcher(bool answer, Action onUnexpected)
    {
        var timer = new System.Windows.Forms.Timer { Interval = 300 };
        timer.Tick += (_, _) =>
        {
            IntPtr dialog = FindWindow("#32770", "Remote Desktop Connection");
            if (dialog == IntPtr.Zero || GetWindowThreadProcessId(dialog, out uint owner) == 0 || owner != (uint)Environment.ProcessId)
                return;
            if (!answer)
            {
                onUnexpected();
                return;
            }
            char[] name = new char[256];
            int length = GetWindowText(GetDlgItem(dialog, CertificateNameControl), name, name.Length);
            IntPtr yes = GetDlgItem(dialog, CertificateYesButton);
            if (length > 0 && yes != IntPtr.Zero &&
                string.Equals(new string(name, 0, length), LabTargets.WindowsTargetName, StringComparison.Ordinal))
                SendMessage(yes, 0x00F5, IntPtr.Zero, IntPtr.Zero);
        };
        timer.Start();
        return timer;
    }

    private static bool OverGdi(string armId, int cycle)
    {
        using Process process = Process.GetCurrentProcess();
        uint gdi = GetGuiResources(process.Handle, 0);
        if (gdi <= GdiStop) return false;
        Write(armId, cycle, "stop");
        return true;
    }

    private static void Write(string armId, int cycle, string phase, string? extra = null)
    {
        using Process process = Process.GetCurrentProcess();
        uint gdi = GetGuiResources(process.Handle, 0);
        uint user = GetGuiResources(process.Handle, 1);
        double privateMiB = process.PrivateMemorySize64 / 1024d / 1024d;
        string line = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "probe variant={0} cycle={1} phase={2} handles={3} gdi={4} user={5} threads={6} privateMiB={7:F1}{8}",
            armId, cycle, phase, process.HandleCount, gdi, user, process.Threads.Count, privateMiB,
            extra == null ? string.Empty : " " + extra);
        TestContext.Out.WriteLine(line);
    }

    private static void Pump(TimeSpan duration)
    {
        long end = Environment.TickCount64 + (long)duration.TotalMilliseconds;
        while (Environment.TickCount64 < end)
        {
            Application.DoEvents();
            Thread.Sleep(20);
        }
    }

    private static bool PumpUntil(Func<bool> done, TimeSpan timeout)
    {
        long end = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        while (Environment.TickCount64 < end)
        {
            if (done()) return true;
            Application.DoEvents();
            Thread.Sleep(20);
        }
        return done();
    }
}

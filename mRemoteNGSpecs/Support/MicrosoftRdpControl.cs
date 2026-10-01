using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGSpecs.Fixtures;

namespace mRemoteNGSpecs.Support;

/// <summary>
/// Microsoft's own RDP control, hosted by its generated Windows Forms wrapper in this test process
/// with no mRemoteNG code in the session path. It measures what the control itself leaves behind
/// per session under the same target, certificate prompt and settle time as the application run.
///
/// Lab evidence for #182 showed the bare control keeping about 74 handles, 8 USER objects and two
/// or three worker threads per closed session, plus 9 GDI objects whenever its certificate warning
/// is shown. That residue is the control's, not the application's, so the application is charged
/// only for what it retains beyond this series.
/// </summary>
[SupportedOSPlatform("windows")]
public static class MicrosoftRdpControl
{
    public sealed record Sample(long PrivateBytes, long Handles, long GdiObjects);
    public sealed record Series(IReadOnlyList<Sample> Idle, IReadOnlyList<Sample> Closed, int CompletedLogins, string? Failure);

    private static readonly Lazy<Series> Measured = new(() => RunOnStaThread(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>One measurement per test process; every acceptance scenario compares against it.</summary>
    public static Series Measure() => Measured.Value;

    [DllImport("user32.dll")]
    private static extern uint GetGuiResources(IntPtr process, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string className, string title);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")]
    private static extern IntPtr GetDlgItem(IntPtr window, int id);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, [Out] char[] text, int length);
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

    private const int CertificateNameControl = 13456;
    private const int CertificateYesButton = 14004;

    private static Series RunOnStaThread()
    {
        Series? result = null;
        Thread thread = new(() => result = Run()) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return result!;
    }

    private static Series Run()
    {
        List<Sample> idle = [], closed = [];
        int logins = 0;
        try
        {
            if (string.IsNullOrEmpty(LabTargets.WindowsPassword))
                return new(idle, closed, 0, "lab Windows credential unavailable");

            using Form form = new() { Width = 1100, Height = 750, StartPosition = FormStartPosition.Manual,
                Location = new Point(40, 40), Text = "Microsoft RDP control baseline", ShowInTaskbar = false };
            using System.Windows.Forms.Timer certificate = CertificatePrompt();
            form.Show();
            for (int i = 0; i < 6; i++)
            {
                Pump(TimeSpan.FromSeconds(10));
                idle.Add(Read());
            }

            // The generated wrapper from the interop assembly: Microsoft's hosting code, not ours.
            Type generated = typeof(RdpProtocol).Assembly
                .GetType("mRemoteNG.Connection.Protocol.RDP.RdpActiveXHosts+Client11", throwOnError: true)!.BaseType!;
            for (int cycle = 1; cycle <= RetentionAssessment.RequiredCycles; cycle++)
            {
                AxHost host = (AxHost)Activator.CreateInstance(generated)!;
                bool loggedIn = false;
                var login = host.GetType().GetEvent("OnLoginComplete")!;
                EventHandler onLogin = (_, _) => loggedIn = true;
                login.AddEventHandler(host, onLogin);
                host.Dock = DockStyle.Fill;
                form.Controls.Add(host);
                host.CreateControl();
                dynamic client = host;
                client.Server = LabTargets.WindowsTargetHost;
                client.UserName = LabTargets.WindowsUser;
                client.Domain = LabTargets.WindowsTargetName;
                client.AdvancedSettings2.ClearTextPassword = LabTargets.WindowsPassword;
                client.AdvancedSettings2.RDPPort = LabTargets.Rdp;
                client.AdvancedSettings8.EnableCredSspSupport = true;
                // The host is left on the control's own server-authentication policy. The prompt
                // below answers this process's certificate warning when one appears.
                client.DesktopWidth = form.ClientSize.Width;
                client.DesktopHeight = form.ClientSize.Height;
                client.Connect();
                if (!PumpUntil(() => loggedIn, TimeSpan.FromSeconds(90)))
                    return new(idle, closed, logins, $"control login {cycle} did not complete");
                Pump(TimeSpan.FromSeconds(5));
                logins++;
                client.Disconnect();
                Pump(TimeSpan.FromSeconds(2));
                login.RemoveEventHandler(host, onLogin);
                form.Controls.Remove(host);
                host.Dispose();
                Pump(TimeSpan.FromSeconds(RetentionAssessment.RequiredSettleSeconds));
                closed.Add(Read());
            }
            form.Close();
            return new(idle, closed, logins, null);
        }
        catch (Exception ex)
        {
            return new(idle, closed, logins, $"{ex.GetType().Name}: {ex.Message}");
        }
    }

    private static Sample Read()
    {
        using Process process = Process.GetCurrentProcess();
        return new(process.PrivateMemorySize64, process.HandleCount, GetGuiResources(process.Handle, 0));
    }

    /// <summary>Confirms only this process's warning for the lab target's own certificate name.</summary>
    private static System.Windows.Forms.Timer CertificatePrompt()
    {
        System.Windows.Forms.Timer timer = new() { Interval = 300 };
        timer.Tick += (_, _) =>
        {
            IntPtr dialog = FindWindow("#32770", "Remote Desktop Connection");
            if (dialog == IntPtr.Zero || GetWindowThreadProcessId(dialog, out uint owner) == 0 || owner != Environment.ProcessId)
                return;
            char[] name = new char[256];
            int length = GetWindowText(GetDlgItem(dialog, CertificateNameControl), name, name.Length);
            IntPtr yes = GetDlgItem(dialog, CertificateYesButton);
            if (length > 0 && yes != IntPtr.Zero &&
                string.Equals(new string(name, 0, length), LabTargets.WindowsTargetName, StringComparison.Ordinal))
                SendMessage(yes, 0x00F5 /* BM_CLICK */, IntPtr.Zero, IntPtr.Zero);
        };
        timer.Start();
        return timer;
    }

    private static bool PumpUntil(Func<bool> done, TimeSpan timeout)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (!done())
        {
            if (elapsed.Elapsed > timeout) return false;
            Application.DoEvents();
            Thread.Sleep(20);
        }
        return true;
    }

    private static void Pump(TimeSpan duration) => PumpUntil(() => false, duration);
}

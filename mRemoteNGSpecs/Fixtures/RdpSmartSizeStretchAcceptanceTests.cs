using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using FlaUI.Core.AutomationElements;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// A session rendered smaller than its panel must be stretched to the whole panel when
    /// SmartSizing is on. On a laptop (2026-10-20) a Custom 1288x815 SmartSize connection in a
    /// 1610x1019 panel showed the desktop unscaled, 80 % of the panel with a grey border, although
    /// the connection log recorded smart_size=true before connect. Adaptive resolution relies on
    /// the same stretch, so this measures it: the size of the control's output window against the
    /// size of the RDP surface, plus a screenshot.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSmartSizeStretchAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windows-smartsize";
        private const string AdaptiveName = "lab-windows-adaptive";
        private const string IcmpBlockRule = "mRNG-lab-adaptive-icmp-block";
        private const int SessionWidth = 480;
        private const int SessionHeight = 288;

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName,
                       c =>
                       {
                           c.Resolution = RDPResolutions.Custom;
                           c.ResolutionWidth = SessionWidth;
                           c.ResolutionHeight = SessionHeight;
                           c.RDPSizingMode = RDPSizingMode.SmartSize;
                       });
            // Fit to the panel at desktop scale 125, the laptop the adaptive mode was built for:
            // its one lower level is remote scale 100 at four fifths of the panel.
            seeder.Add(AdaptiveName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName,
                       c =>
                       {
                           c.Resolution = RDPResolutions.FitToWindow;
                           c.DesktopScaleFactor = RDPDesktopScaleFactor.Scale125;
                       });
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["ConfirmCloseConnection"] = "1",
                ["RdpAdaptiveResolution"] = "True",
            });
            File.WriteAllText(Path.Combine(Deployment.Directory, "verbose.log.enable"), string.Empty);
            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "smartsize");
            Directory.CreateDirectory(_evidenceDir);
        }

        private static void SkipUnlessReachable(string host, int port)
        {
            try
            {
                using System.Net.Sockets.TcpClient probe = new();
                if (!probe.ConnectAsync(host, port).Wait(TimeSpan.FromSeconds(3)))
                    Assert.Ignore($"lab RDP target {host}:{port} did not answer.");
            }
            catch (Exception ex)
            {
                Assert.Ignore($"lab RDP target {host}:{port} is not reachable: {ex.GetType().Name}");
            }
        }

        private AutomationElement Row(string name)
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains(name, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private bool LogContains(string phrase) =>
            (Deployment.ReadAppLog() ?? string.Empty).Contains(phrase, StringComparison.Ordinal);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr parent, EnumProc callback, IntPtr lParam);
        private delegate bool EnumProc(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hwnd, StringBuilder name, int max);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hwnd);
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        private static List<(string Class, Rectangle Rect, bool Visible)> Children(IntPtr parent)
        {
            List<(string, Rectangle, bool)> found = [];
            EnumChildWindows(parent, (h, _) =>
            {
                StringBuilder sb = new(256);
                _ = GetClassName(h, sb, sb.Capacity);
                GetWindowRect(h, out RECT r);
                found.Add((sb.ToString(), Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom), IsWindowVisible(h)));
                return true;
            }, IntPtr.Zero);
            return found;
        }

        private void AssertSessionFillsSurface(string step, bool sessionIsSmallerThanPanel = true)
        {
            AutomationElement surface = MainWindow.FindFirstDescendant(cf => cf.ByClassName("UIMainClass"))
                ?? throw new AssertionException("no RDP surface (UIMainClass) in the main window");
            IntPtr surfaceHandle = surface.Properties.NativeWindowHandle.Value;
            Rectangle surfaceRect = surface.BoundingRectangle;
            List<(string Class, Rectangle Rect, bool Visible)> children = Children(surfaceHandle);
            string listing = string.Join(Environment.NewLine,
                children.Select(c => $"{c.Class} {c.Rect.Width}x{c.Rect.Height} at {c.Rect.X},{c.Rect.Y} visible={c.Visible}"));
            File.WriteAllText(Path.Combine(_evidenceDir, $"windows-{step}.txt"),
                $"surface {surfaceRect.Width}x{surfaceRect.Height} at {surfaceRect.X},{surfaceRect.Y}{Environment.NewLine}{listing}");
            string smartSizeLine = (Deployment.ReadAppLog() ?? string.Empty).Split('\n')
                .LastOrDefault(l => l.Contains("[rdp-zoom]", StringComparison.Ordinal)) ?? "no [rdp-zoom] line";
            TestContext.Out.WriteLine(smartSizeLine);
            File.AppendAllText(Path.Combine(_evidenceDir, $"windows-{step}.txt"), Environment.NewLine + smartSizeLine);
            TestContext.Out.WriteLine($"surface {surfaceRect.Width}x{surfaceRect.Height}");
            TestContext.Out.WriteLine(listing);

            (string Class, Rectangle Rect, bool Visible) output = children
                .Where(c => c.Visible && c.Class.Contains("IHWindowClass", StringComparison.Ordinal))
                .OrderByDescending(c => c.Rect.Width * c.Rect.Height).FirstOrDefault();
            Assert.That(output.Class, Is.Not.Null, "no visible IHWindowClass under the RDP surface:" + Environment.NewLine + listing);
            if (sessionIsSmallerThanPanel)
                Assert.That(surfaceRect.Width, Is.GreaterThan(SessionWidth + 40), "the panel is not wider than the session; nothing to stretch");
            // Scaled with its own aspect ratio, the session meets the surface on one side.
            bool fillsWidth = output.Rect.Width >= surfaceRect.Width - 4;
            bool fillsHeight = output.Rect.Height >= surfaceRect.Height - 4;
            Assert.That(fillsWidth || fillsHeight, Is.True,
                $"the session is shown {output.Rect.Width}x{output.Rect.Height} in a {surfaceRect.Width}x{surfaceRect.Height} surface: not stretched");
        }

        [Test]
        public void ACustomSessionSmallerThanThePanelIsStretchedByItsSmartSizing()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => LogContains("phase=login_complete"), "the RDP logon to complete", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            Thread.Sleep(TimeSpan.FromSeconds(6));

            string shot = Path.Combine(_evidenceDir, "connected.png");
            FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(shot);
            TestContext.AddTestAttachment(shot, "Custom session with SmartSize after logon");

            AssertSessionFillsSurface("connected");

            // A larger panel must get a larger zoom, not keep the first one.
            MainWindow.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Maximized);
            Thread.Sleep(TimeSpan.FromSeconds(4));
            FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(Path.Combine(_evidenceDir, "maximized.png"));
            AssertSessionFillsSurface("maximized");
        }
        private static int Netsh(string arguments)
        {
            using System.Diagnostics.Process p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("netsh", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            })!;
            p.WaitForExit(15000);
            return p.ExitCode;
        }

        /// <summary>
        /// A link that loses its probes: the guest blocks its own probes to the target, so
        /// the session's link probe counts losses while RDP itself keeps working. The adaptive mode
        /// must step down to remote scale 100 and zoom the smaller session back over the panel.
        /// </summary>
        [Test]
        public void AnAdaptiveStepDownIsZoomedBackOverThePanel()
        {
            SkipUnlessReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp);
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "MRNG_LAB_WINDOWS_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(AdaptiveName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => LogContains("phase=login_complete"), "the RDP logon to complete", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            // The probe must have answered once before the block, or it never counts a loss.
            UiWait.Until(() => LogContains("event=rdp_link_probe"), "an answered link probe", TimeSpan.FromSeconds(90));
            bool tcp = LogContains("event=rdp_link_probe") && (Deployment.ReadAppLog() ?? "").Contains("method=tcp", StringComparison.Ordinal);
            FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(Path.Combine(_evidenceDir, "before.png"));

            _ = Netsh($"advfirewall firewall delete rule name={IcmpBlockRule}");
            // The lab target does not answer ICMP, so the probe times TCP connects to the RDP port.
            // Blocking new outbound connects fails those probes; the session's own connection is
            // already established and keeps working.
            string blocked = tcp ? $"protocol=TCP remoteport={LabTargets.Rdp}" : "protocol=icmpv4:8,any";
            int added = Netsh($"advfirewall firewall add rule name={IcmpBlockRule} dir=out action=block " +
                              $"{blocked} remoteip={LabTargets.WindowsTargetHost}");
            TestContext.Out.WriteLine($"probe method {(tcp ? "tcp" : "icmp")}, blocked {blocked}");
            if (added != 0)
                Assert.Inconclusive($"could not add the outbound ICMP block (netsh exit {added}); the battery may not run elevated");
            try
            {
                // Like a tunnel: the link degrades, the controller decides, then the link returns.
                UiWait.Until(() => LogContains("event=rdp_resolution") && LogContains("phase=decision"),
                             "the adaptive controller to decide a step down", TimeSpan.FromSeconds(240));
                _ = Netsh($"advfirewall firewall delete rule name={IcmpBlockRule}");
                UiWait.Until(() => LogContains("[rdp-zoom] adaptive"),
                             "the step down to be applied and zoomed back over the panel", TimeSpan.FromSeconds(180));
                Thread.Sleep(TimeSpan.FromSeconds(4));
                string shot = Path.Combine(_evidenceDir, "stepped-down.png");
                FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(shot);
                TestContext.AddTestAttachment(shot, "adaptive level 1, zoomed");
                string log = Deployment.ReadAppLog() ?? string.Empty;
                string resolutionLines = string.Join(Environment.NewLine,
                    log.Split('\n').Where(l => l.Contains("event=rdp_resolution", StringComparison.Ordinal)));
                File.WriteAllText(Path.Combine(_evidenceDir, "rdp_resolution.txt"), resolutionLines);
                TestContext.Out.WriteLine(resolutionLines);
                Assert.That(resolutionLines, Does.Contain("phase=applied").And.Contain("to_scale=100"),
                            "no applied step to remote scale 100");
                AssertSessionFillsSurface("stepped-down", sessionIsSmallerThanPanel: false);
            }
            finally
            {
                _ = Netsh($"advfirewall firewall delete rule name={IcmpBlockRule}");
            }
        }
    }
}

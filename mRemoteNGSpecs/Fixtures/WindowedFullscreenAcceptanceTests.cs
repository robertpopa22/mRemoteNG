using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class WindowedFullscreenAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windowed-fullscreen";
        private const string ReturnButtonName = "Return to tab (Ctrl+Alt+Enter)";
        private const int SwShowMaximized = 3;

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            bool savedFullscreen = TestContext.CurrentContext.Test.MethodName?.StartsWith("AltClick", StringComparison.Ordinal) == true;
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName,
                       connection => connection.Resolution = savedFullscreen
                           ? RDPResolutions.Fullscreen
                           : RDPResolutions.FitToWindow);
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ConfirmCloseConnection"] = "1"
            });
            File.WriteAllText(Path.Combine(Deployment.Directory, "verbose.log.enable"), string.Empty);

            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "windowed-fullscreen");
            Directory.CreateDirectory(_evidenceDir);
        }

        [Test]
        [Issues("#211")]
        public void AltClickStartsSavedFullscreenConnectionInWindowedFullscreenAndHostCloseReturnsTab()
        {
            RequireWindowsTarget();

            AltClick(Row());
            Window floating = WaitForWindowedFullscreen();
            WaitForConnectedSession(featureAlreadyInvoked: true);
            AssertWindowedSessionFillsHost(floating);

            AssertWindowedFullscreenChrome(floating);
            Assert.That(ConnectedCount(), Is.EqualTo(1), "Alt+click opened more than one RDP session");
            Snapshot(floating, "1-alt-click-windowed-fullscreen.png");
            SnapshotDesktop("1b-alt-click-entire-desktop.png");

            IntPtr firstHandle = NativeHandle(floating);
            MainWindow.Focus();
            UiWait.Settle(MainWindow);
            AutomationElement activeTreeRow = Row();
            activeTreeRow.Focus();
            AltClick(activeTreeRow);
            Window repeated = WaitForWindowedFullscreen();
            Assert.That(NativeHandle(repeated), Is.EqualTo(firstHandle),
                        "repeating Alt+click while windowed created a different float host");
            AssertSessionWasReused("repeating Alt+click while already windowed");
            AssertWindowedFullscreenChrome(repeated);
            Snapshot(repeated, "2-repeated-alt-click-same-host.png");

            repeated.Patterns.Window.Pattern.Close();
            WaitForReturnedTab();
            AssertSessionWasReused("closing the borderless host");
            Snapshot(MainWindow, "3-host-close-returned-tab.png");
        }

        [Test]
        [Issues("#211")]
        public void ShiftEnterReusesConnectedSessionAcrossWindowedFullscreenCyclesAndBothExitRoutes()
        {
            RequireWindowsTarget();

            Win32Mouse.DoubleClick(Row());
            WaitForConnectedSession(featureAlreadyInvoked: false);
            Assert.That(TabCount(), Is.EqualTo(1), "the normal connection did not create exactly one tab");
            Thread.Sleep(TimeSpan.FromSeconds(5));
            UiWait.Settle(MainWindow);
            Snapshot(MainWindow, "1-connected-in-tab.png");

            SelectRowAndShiftEnter();
            Window first = WaitForWindowedFullscreen();
            AssertWindowedSessionFillsHost(first);
            AssertWindowedFullscreenChrome(first);
            Snapshot(first, "2-first-windowed-fullscreen.png");

            Win32Mouse.LeftClick(first);
            Wait.UntilInputIsProcessed();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.ALT, VirtualKeyShort.RETURN);
            WaitForReturnedTab();
            AssertSessionWasReused("Ctrl+Alt+Enter");

            SelectRowAndShiftEnter();
            Window second = WaitForWindowedFullscreen();
            AssertWindowedSessionFillsHost(second);
            AssertWindowedFullscreenChrome(second);
            AssertSessionWasReused("the second Shift+Enter");

            Thread.Sleep(TimeSpan.FromMilliseconds(2200));
            Assert.That(VisibleReturnButtons(second), Is.Zero, "the return button did not hide after its reveal interval");
            RevealReturnButton(second);
            AutomationElement? returnButton = null;
            UiWait.Until(() =>
            {
                returnButton = second.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                                     .FirstOrDefault(button => string.Equals(SafeName(button), ReturnButtonName, StringComparison.Ordinal)
                                                               && !button.IsOffscreen);
                return returnButton != null;
            }, "hovering the top edge to reveal the return button", TimeSpan.FromSeconds(5));
            Snapshot(second, "3-return-button-revealed.png");
            Win32Mouse.LeftClick(returnButton!);

            WaitForReturnedTab();
            AssertSessionWasReused("the return button");
            Snapshot(MainWindow, "4-returned-to-original-tab.png");
        }

        private static void RequireWindowsTarget()
        {
            if (string.IsNullOrWhiteSpace(LabTargets.WindowsPassword))
                Assert.Ignore("MRNG_LAB_WINDOWS_PASSWORD is not visible to the isolated lab battery process.");

            if (!LabTargets.IsReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp, 3000))
                Assert.Ignore($"lab RDP target {LabTargets.Describe(LabTargets.WindowsTargetHost, LabTargets.Rdp)} " +
                              "is unreachable or resolves to the machine running the battery.");
        }

        private AutomationElement Row()
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(element => SafeName(element).Contains(ConnectionName, StringComparison.OrdinalIgnoreCase));
        }

        private static void AltClick(AutomationElement row)
        {
            Keyboard.Press(VirtualKeyShort.ALT);
            try { Win32Mouse.LeftClick(row); }
            finally { Keyboard.Release(VirtualKeyShort.ALT); }
            Wait.UntilInputIsProcessed();
        }

        private void SelectRowAndShiftEnter()
        {
            MainWindow.Focus();
            Win32Mouse.LeftClick(Row());
            Wait.UntilInputIsProcessed();
            Keyboard.TypeSimultaneously(VirtualKeyShort.SHIFT, VirtualKeyShort.RETURN);
        }

        private Window WaitForWindowedFullscreen()
        {
            Window? result = null;
            UiWait.Until(() =>
            {
                result = ProcessTopLevelWindows().FirstOrDefault(IsWindowedFullscreenHost);
                return result != null;
            }, "the borderless windowed-fullscreen host", TimeSpan.FromSeconds(30));
            return result!;
        }

        private Window[] ProcessTopLevelWindows() =>
            Driver.Automation.GetDesktop().FindAllChildren(cf => cf.ByControlType(ControlType.Window))
                  .Where(element =>
                  {
                      try { return element.Properties.ProcessId.ValueOrDefault == Driver.Application.ProcessId; }
                      catch (Exception) { return false; }
                  })
                  .Select(element => element.AsWindow())
                  .ToArray();

        private void WaitForConnectedSession(bool featureAlreadyInvoked)
        {
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => ConnectedCount() >= 1, "the RDP session to connect", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            UiWait.Until(() => CountInAppLog("phase=login_complete") >= 1 || DisconnectedCount() > 0,
                         "the Windows RDP logon to complete or report a disconnect", TimeSpan.FromSeconds(90));
            Thread.Sleep(TimeSpan.FromSeconds(3));

            if (DisconnectedCount() == 0)
                return;

            string message = "the RDP session disconnected before it was stable:" + Environment.NewLine + AppLogTail();
            if (!featureAlreadyInvoked)
                Assert.Ignore(message + Environment.NewLine + "This happened before the windowed-fullscreen action.");
            Assert.Fail(message);
        }

        private void AssertWindowedFullscreenChrome(Window window)
        {
            IntPtr handle = new(window.Properties.NativeWindowHandle.ValueOrDefault);
            Rectangle bounds = window.BoundingRectangle;
            Rectangle workArea = Screen.FromHandle(handle).WorkingArea;
            WINDOWPLACEMENT placement = new() { length = Marshal.SizeOf<WINDOWPLACEMENT>() };

            Assert.Multiple(() =>
            {
                Assert.That(GetWindowPlacement(handle, ref placement), Is.True, "could not read float-window placement");
                Assert.That(placement.showCmd, Is.EqualTo(SwShowMaximized), "the float host is not maximized");
                Assert.That(bounds.X, Is.EqualTo(workArea.X).Within(2), "float host starts outside the working area");
                Assert.That(bounds.Y, Is.EqualTo(workArea.Y).Within(2), "float host starts outside the working area");
                Assert.That(bounds.Width, Is.EqualTo(workArea.Width).Within(2), "float host does not fill the working-area width");
                Assert.That(bounds.Height, Is.EqualTo(workArea.Height).Within(2), "float host covers the taskbar or misses the working-area height");
                Assert.That(VisibleElements(window, ControlType.Tree), Is.Zero,
                            "the connection tree is visible in windowed fullscreen");
                Assert.That(VisibleElements(window, ControlType.MenuBar), Is.Zero,
                            "a menu bar is visible in windowed fullscreen");
                Assert.That(VisibleElements(window, ControlType.Tab), Is.Zero,
                            "a tab strip is visible in windowed fullscreen");
                Assert.That(ProcessTopLevelWindows().Count(IsWindowedFullscreenHost), Is.EqualTo(1),
                            "the gesture created duplicate windowed-fullscreen hosts");
            });
        }

        private void AssertWindowedSessionFillsHost(Window window)
        {
            Match? sizing = null;
            UiWait.Until(() =>
            {
                string log = Deployment.ReadAppLog() ?? string.Empty;
                sizing = Regex.Matches(log,
                        @"phase=windowed_fullscreen_settled panel=(\d+)x(\d+) target=(\d+)x(\d+) desktop=(\d+)x(\d+) smartSize=True control=(\d+)x(\d+)")
                    .Cast<Match>()
                    .LastOrDefault();
                return sizing != null || log.Contains("phase=windowed_fullscreen_settle_timeout", StringComparison.Ordinal);
            }, "the connected RDP session to settle at its windowed-fullscreen size", TimeSpan.FromSeconds(20));

            if (sizing == null)
                Assert.Fail("the ActiveX session did not reach the requested windowed-fullscreen size:"
                            + Environment.NewLine + AppLogTail());

            Match settled = sizing!;
            int panelWidth = int.Parse(settled.Groups[1].Value);
            int panelHeight = int.Parse(settled.Groups[2].Value);
            int targetWidth = int.Parse(settled.Groups[3].Value);
            int targetHeight = int.Parse(settled.Groups[4].Value);
            int desktopWidth = int.Parse(settled.Groups[5].Value);
            int desktopHeight = int.Parse(settled.Groups[6].Value);
            int controlWidth = int.Parse(settled.Groups[7].Value);
            int controlHeight = int.Parse(settled.Groups[8].Value);
            Rectangle host = window.BoundingRectangle;

            Assert.Multiple(() =>
            {
                Assert.That(targetWidth, Is.EqualTo(panelWidth).Within(2), "RDP target width does not fill the float content");
                Assert.That(targetHeight, Is.EqualTo(panelHeight).Within(2), "RDP target height does not fill the float content");
                Assert.That(desktopWidth, Is.EqualTo(targetWidth), "the observed ActiveX desktop width did not reach its target");
                Assert.That(desktopHeight, Is.EqualTo(targetHeight), "the observed ActiveX desktop height did not reach its target");
                Assert.That(controlWidth, Is.EqualTo(panelWidth).Within(2), "RDP control width remains at its embedded-tab size");
                Assert.That(controlHeight, Is.EqualTo(panelHeight).Within(2), "RDP control height remains at its embedded-tab size");
                Assert.That(panelWidth, Is.EqualTo(host.Width).Within(8), "RDP panel does not span the borderless host width");
                Assert.That(panelHeight, Is.EqualTo(host.Height).Within(8), "RDP panel does not span the borderless host height");
            });

            string diagnostic = settled.Value + Environment.NewLine;
            string path = Path.Combine(_evidenceDir, $"windowed-sizing-{targetWidth}x{targetHeight}.txt");
            File.WriteAllText(path, diagnostic);
            TestContext.AddTestAttachment(path, "windowed fullscreen RDP sizing diagnostic");
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }

        private bool IsWindowedFullscreenHost(Window window) =>
            NativeHandle(window) != NativeHandle(MainWindow) &&
            (SafeName(window).Contains(ConnectionName, StringComparison.OrdinalIgnoreCase) ||
             window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                   .Any(button => string.Equals(SafeName(button), ReturnButtonName, StringComparison.Ordinal)));

        private static int VisibleElements(Window window, ControlType type) =>
            window.FindAllDescendants(cf => cf.ByControlType(type))
                  .Count(element =>
                  {
                      try
                      {
                          Rectangle bounds = element.BoundingRectangle;
                          return !element.IsOffscreen && bounds.Width > 0 && bounds.Height > 0;
                      }
                      catch (Exception) { return false; }
                  });

        private static int VisibleReturnButtons(Window window) =>
            window.FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                  .Count(button => string.Equals(SafeName(button), ReturnButtonName, StringComparison.Ordinal)
                                   && !button.IsOffscreen);

        private static void RevealReturnButton(Window window)
        {
            Rectangle bounds = window.BoundingRectangle;
            Mouse.MoveTo(new Point(bounds.X + bounds.Width / 2, bounds.Y + 1));
            Wait.UntilInputIsProcessed();
            Thread.Sleep(300);
        }

        private void WaitForReturnedTab()
        {
            UiWait.Until(() => ProcessTopLevelWindows().All(window => !IsWindowedFullscreenHost(window)),
                         "windowed fullscreen to close", TimeSpan.FromSeconds(15));
            UiWait.Until(() => TabCount() == 1, "the original connection tab to return", TimeSpan.FromSeconds(15));
        }

        private void AssertSessionWasReused(string action)
        {
            Thread.Sleep(TimeSpan.FromSeconds(2));
            Assert.Multiple(() =>
            {
                Assert.That(ConnectedCount(), Is.EqualTo(1), $"{action} reconnected the RDP session");
                Assert.That(DisconnectedCount(), Is.Zero,
                            $"{action} disconnected the RDP session:{Environment.NewLine}{AppLogTail()}");
                Assert.That(TabCount(), Is.LessThanOrEqualTo(1), $"{action} duplicated the connection tab");
            });
        }

        private int TabCount() =>
            Driver.Automation.GetDesktop().FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                  .Count(element =>
                  {
                      try
                      {
                          return element.Properties.ProcessId.ValueOrDefault == Driver.Application.ProcessId &&
                                 SafeName(element).Contains(ConnectionName, StringComparison.OrdinalIgnoreCase);
                      }
                      catch (Exception) { return false; }
                  });

        private int ConnectedCount() => CountInAppLog("established by user");

        private int DisconnectedCount() =>
            CountInAppLog("Protocol Event Disconnected") + CountInAppLog("closed by user");

        private string AppLogTail()
        {
            string log = Deployment.ReadAppLog() ?? string.Empty;
            return string.Join(Environment.NewLine, log.Split('\n').TakeLast(40));
        }

        private int CountInAppLog(string phrase)
        {
            string log = Deployment.ReadAppLog() ?? string.Empty;
            int count = 0;
            for (int index = log.IndexOf(phrase, StringComparison.Ordinal);
                 index >= 0;
                 index = log.IndexOf(phrase, index + phrase.Length, StringComparison.Ordinal))
                count++;
            return count;
        }

        private void Snapshot(AutomationElement element, string name)
        {
            string path = Path.Combine(_evidenceDir, name);
            FlaUI.Core.Capturing.Capture.Element(element).ToFile(path);
            TestContext.AddTestAttachment(path, name);
        }

        private void SnapshotDesktop(string name)
        {
            string path = Path.Combine(_evidenceDir, name);
            FlaUI.Core.Capturing.Capture.Screen().ToFile(path);
            TestContext.AddTestAttachment(path, name);
        }

        private static string SafeName(AutomationElement element)
        {
            try { return element.Name ?? string.Empty; }
            catch (Exception) { return string.Empty; }
        }

        private static IntPtr NativeHandle(AutomationElement element) =>
            new(element.Properties.NativeWindowHandle.ValueOrDefault);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowPlacement(IntPtr window, ref WINDOWPLACEMENT placement);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public Point minPosition;
            public Point maxPosition;
            public Rectangle normalPosition;
        }
    }
}

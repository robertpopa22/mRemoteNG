using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #222: an external tool that opens Microsoft Edge with "Try integration" was docked only
    /// sometimes.
    ///
    /// When Edge is already running with the same profile, the msedge.exe that mRemoteNG starts
    /// can hand its address to that browser. The reporter's log showed the started process exit
    /// with no window. In the lab, with Edge already open, the same tool gave a different result
    /// on each run: once a window was docked and its process exited 19 s later, closing the panel;
    /// once the docked window was the one that held both the page opened outside and the tool's
    /// page. That case is not kept as a test because its outcome is not stable.
    ///
    /// With its own <c>--user-data-dir</c> Edge has no running browser to hand off to. This
    /// scenario pins that the tool is then docked and stays docked while another Edge runs.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class ExternalToolEdgeIntegrationAcceptanceTests : UiAcceptanceTestBase
    {
        private const string OwnProfileTool = "EdgeOwnProfile";
        private const string NotDockable = "no window that can be docked";
        private const string Docked = "Int App Handle: ";

        private static string? EdgePath
        {
            get
            {
                foreach (string root in new[] { Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) })
                {
                    string candidate = Path.Combine(root, "Microsoft", "Edge", "Application", "msedge.exe");
                    if (File.Exists(candidate))
                        return candidate;
                }
                return null;
            }
        }

        private string OwnProfileDirectory => Path.Combine(Deployment.Directory, "edge-profile");

        protected override void SeedSettings()
        {
            string edge = EdgePath ?? @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";
            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ViewMenuExternalTools"] = "True",
            });
            File.WriteAllText(Path.Combine(Deployment.SettingsPath, "extApps.xml"),
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<Apps>\r\n" +
                Tool(OwnProfileTool, edge, $"--user-data-dir=\"{OwnProfileDirectory}\" --no-first-run about:blank --edge-kiosk-type=fullscreen") +
                "</Apps>\r\n");
        }

        private static string Tool(string name, string fileName, string arguments) =>
            $"    <App DisplayName=\"{name}\" FileName=\"{SecurityElement.Escape(fileName)}\" IconPath=\"\" " +
            $"Arguments=\"{SecurityElement.Escape(arguments)}\" WorkingDir=\"\" WaitForExit=\"False\" " +
            "TryToIntegrate=\"True\" RunElevated=\"False\" ShowOnToolbar=\"True\" Category=\"\" Hidden=\"False\" />\r\n";

        [TearDown]
        public void StopEdge()
        {
            foreach (Process edge in Process.GetProcessesByName("msedge"))
            {
                try { edge.Kill(entireProcessTree: true); } catch (InvalidOperationException) { } catch (System.ComponentModel.Win32Exception) { }
                edge.Dispose();
            }
        }

        [Test]
        public void EdgeWithItsOwnProfileIsDockedWhileAnotherEdgeRuns()
        {
            RequireEdge();
            StartEdgeOutsideTheApp();

            string log = ClickToolAndReadLog(OwnProfileTool);

            Assert.Multiple(() =>
            {
                Assert.That(log, Does.Contain(Docked),
                            "Edge started with its own --user-data-dir was not docked");
                Assert.That(log, Does.Not.Contain(NotDockable));
                Assert.That(log, Does.Not.Contain("closed by user"), "the docked Edge panel closed by itself");
            });
        }

        private static void RequireEdge()
        {
            if (EdgePath is null)
                Assert.Inconclusive("Microsoft Edge is not installed on this machine.");
        }

        private static void StartEdgeOutsideTheApp()
        {
            using Process? edge = Process.Start(new ProcessStartInfo(EdgePath!, "--no-first-run about:blank") { UseShellExecute = false });
            UiWait.Until(() => Process.GetProcessesByName("msedge").Length > 0 && HasEdgeWindow(), "an Edge window outside mRemoteNG", TimeSpan.FromSeconds(30));
        }

        private static bool HasEdgeWindow()
        {
            foreach (Process edge in Process.GetProcessesByName("msedge"))
            {
                using (edge)
                {
                    if (edge.MainWindowHandle != IntPtr.Zero)
                        return true;
                }
            }
            return false;
        }

        private static void LogEdgeWindows(string when)
        {
            foreach (Process edge in Process.GetProcessesByName("msedge"))
            {
                using (edge)
                {
                    if (edge.MainWindowHandle != IntPtr.Zero)
                        TestContext.Out.WriteLine($"{when}: msedge pid={edge.Id} window={edge.MainWindowHandle} title=\"{edge.MainWindowTitle}\"");
                }
            }
        }

        private string ClickToolAndReadLog(string toolName)
        {
            // The Edge window started before the click lies over the app, so a pointer click lands
            // in Edge. Clear any startup prompt and press the button through UIA instead.
            AnswerExpectedPrompts(TimeSpan.FromSeconds(3));
            AutomationElement toolbar = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("_externalToolsToolStrip"), "External Tools toolbar");
            AutomationElement button = UiWait.FindRequired(toolbar, cf => cf.ByName(toolName), $"toolbar button '{toolName}'");
            int edgeBefore = Process.GetProcessesByName("msedge").Length;
            LogEdgeWindows("before click");
            button.Patterns.Invoke.Pattern.Invoke();

            string log = "";
            UiWait.Happened(() =>
            {
                log = Deployment.ReadAppLog() ?? "";
                return log.Contains(NotDockable, StringComparison.Ordinal) || log.Contains(Docked, StringComparison.Ordinal);
            }, TimeSpan.FromSeconds(60));
            // A docked panel whose process exits closes on its own. In the lab a shared-profile Edge
            // stayed docked for about 19 s before its process exited, so watch for 30 s.
            // UiWait.Happened returning false here is the expected outcome.
            UiWait.Happened(() => (Deployment.ReadAppLog() ?? "").Contains("closed by user", StringComparison.Ordinal), TimeSpan.FromSeconds(30));
            log = Deployment.ReadAppLog() ?? "";
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(2));
            LogEdgeWindows("after click");

            foreach (string line in log.Split('\n'))
            {
                if (line.Contains("Attempting to start", StringComparison.Ordinal) || line.Contains(NotDockable, StringComparison.Ordinal)
                    || line.Contains("Int App", StringComparison.Ordinal) || line.Contains("Tracking process", StringComparison.Ordinal)
                    || line.Contains("closed by user", StringComparison.Ordinal))
                    TestContext.Out.WriteLine(line.TrimEnd());
            }
            TestContext.Out.WriteLine($"msedge processes before click: {edgeBefore}, after: {Process.GetProcessesByName("msedge").Length}");
            return log;
        }
    }
}

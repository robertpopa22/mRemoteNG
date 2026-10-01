using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #182 measurement on the full application. Not an acceptance gate and not part of the
    /// normal battery: each method is one lab run so GDI from one arm cannot leak into the next.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    [Explicit]
    public class RdpRetentionShapeMeasurement : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-windows-rdp";
        private const int SettleSeconds = 30;
        private const uint GdiStop = 2500;

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr process, uint flags);

        protected override void SeedSettings()
        {
            string test = TestContext.CurrentContext.Test.Name;
            bool wide = test.Contains("1920", StringComparison.Ordinal);
            bool logoff = test.Contains("Logoff", StringComparison.Ordinal);
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.WindowsTargetHost, ProtocolType.RDP, LabTargets.Rdp,
                LabTargets.WindowsUser, LabTargets.WindowsPassword, LabTargets.WindowsTargetName,
                info =>
                {
                    if (wide)
                        info.Resolution = RDPResolutions.Res1920x1080;
                    if (logoff)
                    {
                        // Full desktop, then the remote session logs itself off. A live tab close
                        // is the other arm. The program string is a fixed lab command, not a secret.
                        info.RDPStartProgram = "cmd.exe /c ping -n 12 127.0.0.1 >nul & logoff.exe";
                        info.RDPStartProgramWorkDir = @"C:\Windows\System32";
                    }
                });
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["KeepTabsOpenAfterDisconnect"] = "False",
                ["AlwaysShowPanelTabs"] = "True",
                ["ConfirmCloseConnection"] = "1",
            });
            File.WriteAllText(Path.Combine(Deployment.Directory, "verbose.log.enable"), string.Empty);
        }

        [Test]
        [Issues("#182")]
        public void FullApp1920TabClose() => RunClosedCycles("tab1920", 14, remoteLogoff: false);

        [Test]
        [Issues("#182")]
        public void FullApp1920Logoff() => RunClosedCycles("logoff1920", 8, remoteLogoff: true);

        [Test]
        [Issues("#182")]
        public void FullAppDefaultForty() => RunClosedCycles("default40", 40, remoteLogoff: false);

        [Test]
        [Issues("#182")]
        public void FullApp1920Hold()
        {
            RequireTarget();
            string evidence = EvidenceDir("hold1920");
            List<string> lines = [];
            try
            {
                OpenAndWait(1);
                DateTimeOffset until = DateTimeOffset.UtcNow.AddMinutes(30);
                int minute = 0;
                while (DateTimeOffset.UtcNow < until)
                {
                    minute++;
                    Thread.Sleep(TimeSpan.FromSeconds(60));
                    Sample sample = ReadSample();
                    lines.Add(Format("hold1920", minute, "open", sample));
                    TestContext.Out.WriteLine(lines[^1]);
                    if (sample.Gdi >= GdiStop)
                        break;
                }

                CloseSession();
                Thread.Sleep(TimeSpan.FromSeconds(SettleSeconds));
                lines.Add(Format("hold1920", minute, "closed", ReadSample()));
                TestContext.Out.WriteLine(lines[^1]);
                AssertNoCrash("after the held session");
                AssertExitedCleanly(CloseApplicationAndWaitForExit(TimeSpan.FromSeconds(45), true), "after the held session");
            }
            finally
            {
                WriteEvidence(evidence, lines);
            }
        }

        private void RunClosedCycles(string arm, int cycles, bool remoteLogoff)
        {
            RequireTarget();
            string evidence = EvidenceDir(arm);
            List<string> lines = [];
            int logoffs = 0;
            try
            {
                for (int cycle = 1; cycle <= cycles; cycle++)
                {
                    bool loggedOff = OpenAndWait(cycle);
                    if (loggedOff)
                        logoffs++;
                    if (TabCount() > 0)
                        CloseSession();
                    Thread.Sleep(TimeSpan.FromSeconds(SettleSeconds));
                    Sample sample = ReadSample();
                    lines.Add(Format(arm, cycle, loggedOff ? "logoff" : "tab", sample));
                    TestContext.Out.WriteLine(lines[^1]);
                    File.WriteAllLines(Path.Combine(evidence, "series.txt"), lines);
                    if (sample.Gdi >= GdiStop)
                    {
                        TestContext.Out.WriteLine($"shape arm={arm} stop=gdi gdi={sample.Gdi}");
                        break;
                    }
                }

                AssertNoCrash("after the closed cycles");
                AssertExitedCleanly(CloseApplicationAndWaitForExit(TimeSpan.FromSeconds(60), true), "after the closed cycles");
                if (remoteLogoff)
                    Assert.That(logoffs, Is.GreaterThan(0), "the remote session never logged off; this arm did not measure extended reason 12");
            }
            finally
            {
                WriteEvidence(evidence, lines);
            }
        }

        /// <summary>True when this cycle ended by a remote logoff before the tab was clicked.</summary>
        private bool OpenAndWait(int expected)
        {
            Win32Mouse.DoubleClick(Row());
            AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
            UiWait.Until(
                () => CountInLog("phase=login_complete") >= expected || CountInLog("disc_class=user_logoff") >= expected,
                "Windows RDP login or logoff", TimeSpan.FromSeconds(90));
            bool loggedOff = CountInLog("disc_class=user_logoff") >= expected && TabCount() == 0;
            if (!loggedOff)
                Thread.Sleep(TimeSpan.FromSeconds(8));
            loggedOff = CountInLog("disc_class=user_logoff") >= expected && TabCount() == 0;
            return loggedOff;
        }

        private void RequireTarget()
        {
            Assert.That(LabTargets.IsReachable(LabTargets.WindowsTargetHost, LabTargets.Rdp), Is.True,
                "lab Windows target did not answer");
            Assert.That(LabTargets.WindowsPassword, Is.Not.Empty, "lab Windows credential unavailable");
        }

        private int TabCount() => SessionTabs().Length;

        private AutomationElement[] SessionTabs() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .Where(t =>
                {
                    try { return (t.Name ?? "").Contains(ConnectionName, StringComparison.OrdinalIgnoreCase); }
                    catch (Exception) { return false; }
                })
                .ToArray();

        private AutomationElement Row()
        {
            AutomationElement tree = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(e =>
                {
                    try { return (e.Name ?? "").Contains(ConnectionName, StringComparison.OrdinalIgnoreCase); }
                    catch (Exception) { return false; }
                });
        }

        private int CountInLog(string phrase)
        {
            string? log = Deployment.ReadAppLog();
            if (log is null)
                return 0;
            int count = 0;
            int at = log.IndexOf(phrase, StringComparison.Ordinal);
            while (at >= 0)
            {
                count++;
                at = log.IndexOf(phrase, at + phrase.Length, StringComparison.Ordinal);
            }
            return count;
        }

        private void CloseSession()
        {
            Win32Mouse.MiddleClick(SessionTabs().First());
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            UiWait.Until(() => TabCount() == 0, "session close", TimeSpan.FromSeconds(45));
        }

        private readonly record struct Sample(long PrivateBytes, int Handles, uint Gdi, uint User, int Threads);

        private Sample ReadSample()
        {
            using Process process = Process.GetProcessById(Driver.Application.ProcessId);
            uint gdi = GetGuiResources(process.Handle, 0);
            uint user = GetGuiResources(process.Handle, 1);
            Assert.That(gdi, Is.GreaterThan(0u), "GDI measurement unavailable");
            return new(process.PrivateMemorySize64, process.HandleCount, gdi, user, process.Threads.Count);
        }

        private static string Format(string arm, int cycle, string how, Sample sample) =>
            string.Create(CultureInfo.InvariantCulture,
                $"shape arm={arm} cycle={cycle} how={how} private={sample.PrivateBytes} handles={sample.Handles} gdi={sample.Gdi} user={sample.User} threads={sample.Threads}");

        private static string EvidenceDir(string arm)
        {
            string root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (root.EndsWith("win-x64", StringComparison.OrdinalIgnoreCase))
                root = Path.GetDirectoryName(root) ?? root;
            string dir = Path.Combine(root, "_uiscenarios", "_evidence", "rdp-shape-" + arm);
            Directory.CreateDirectory(dir);
            return dir;
        }

        private void WriteEvidence(string evidence, List<string> lines)
        {
            try
            {
                File.WriteAllLines(Path.Combine(evidence, "series.txt"), lines);
                string? log = Deployment.ReadAppLog();
                if (log is null)
                    return;
                File.WriteAllText(Path.Combine(evidence, "application.log"), log);
                string[] perf = log.Split('\n')
                    .Select(l => l.TrimEnd('\r'))
                    .Where(l => l.Contains("[Perf] event=rdp_shape", StringComparison.Ordinal)
                                || l.Contains("[Perf] event=rdp_resources", StringComparison.Ordinal)
                                || l.Contains("[Perf] event=process_stop", StringComparison.Ordinal)
                                || l.Contains("[Perf] event=heartbeat", StringComparison.Ordinal)
                                || l.Contains("[Perf] event=rdp_phase", StringComparison.Ordinal)
                                || l.Contains("[#182] RDP cleanup", StringComparison.Ordinal))
                    .ToArray();
                File.WriteAllLines(Path.Combine(evidence, "perf.txt"), perf);
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"evidence write failed: {ex.GetType().Name}");
            }
        }
    }
}

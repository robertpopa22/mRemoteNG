using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #177: dragging the DockPanel splitter between the side panels and the RDP panel leaves a
    /// stale frame in one direction. The issue's own theory was written from source and says the
    /// splitter path never reaches the protocol; the source says otherwise (ConnectionTab.Resize
    /// is wired to RdpProtocol8.Resize, which debounces into UpdateSessionDisplaySettings). So
    /// this scenario is evidence first: it drags the splitter both ways against a real RDP
    /// server, keeps a screenshot of the panel after each drag, and reads the application's
    /// verbose log to see which resize calls actually ran for each direction and with what size.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class RdpSplitterRedrawAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-linux-rdp";
        private const int DragDistance = 160;

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.LinuxHost, ProtocolType.RDP, LabTargets.Rdp,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            Deployment.WriteConnectionsFile(seeder.Build());
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["ConfirmCloseConnection"] = "1",
            });

            // DevLog writes mRemoteNG-verbose.log next to the executable when this marker exists;
            // that is where RdpProtocol8 records every Resize/Reconnect decision it makes.
            File.WriteAllText(Path.Combine(Deployment.Directory, "verbose.log.enable"), string.Empty);

            // Outside the scenario directory, which is deleted on success: the screenshots are
            // the point of this scenario whichever way the assertion goes.
            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "splitter");
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

        private int CountInLog(string phrase)
        {
            string? log = Deployment.ReadAppLog();
            if (log is null) return 0;
            int count = 0, at = log.IndexOf(phrase, StringComparison.Ordinal);
            while (at >= 0)
            {
                count++;
                at = log.IndexOf(phrase, at + phrase.Length, StringComparison.Ordinal);
            }
            return count;
        }

        private string VerboseLog()
        {
            string path = Path.Combine(Deployment.Directory, "mRemoteNG-verbose.log");
            if (!File.Exists(path)) return string.Empty;
            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        /// <summary>The splitter sits on the right edge of the docked connection tree window.</summary>
        private Point SplitterGrip()
        {
            AutomationElement treeWindow = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTreeWindow"), "connection tree window");
            Rectangle bounds = treeWindow.BoundingRectangle;
            return new Point(bounds.Right + 2, bounds.Y + bounds.Height / 2);
        }

        /// <summary>A real drag: press, move in steps so the splitter tracks, release.</summary>
        private static void Drag(Point from, int dx)
        {
            Mouse.MoveTo(from);
            Wait.UntilInputIsProcessed();
            Mouse.Down(MouseButton.Left);
            Wait.UntilInputIsProcessed();
            int steps = 8;
            for (int i = 1; i <= steps; i++)
            {
                Mouse.MoveTo(new Point(from.X + dx * i / steps, from.Y));
                Thread.Sleep(30);
            }
            Wait.UntilInputIsProcessed();
            Mouse.Up(MouseButton.Left);
            Wait.UntilInputIsProcessed();
        }

        private void Snapshot(string name)
        {
            string path = Path.Combine(_evidenceDir, name);
            FlaUI.Core.Capturing.Capture.Element(MainWindow).ToFile(path);
            TestContext.AddTestAttachment(path, name);
        }

        [Test]
        [Issues("#177")]
        public void DraggingTheSplitterEitherWayResizesTheRdpSession()
        {
            SkipUnlessReachable(LabTargets.LinuxHost, LabTargets.Rdp);
            Assert.That(LabTargets.LinuxPassword, Is.Not.Empty, "MRNG_LAB_LINUX_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            UiWait.Until(() => CountInLog("established by user") >= 1, "the RDP session to connect", TimeSpan.FromSeconds(90));
            AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
            Thread.Sleep(TimeSpan.FromSeconds(5));
            Snapshot("0-connected.png");

            int reconnectsBefore = ReconnectCalls(VerboseLog()).Count;
            TestContext.Out.WriteLine($"reconnect calls before any drag: {reconnectsBefore}");

            // Four drags: tree wider (RDP panel shrinks), back (grows), tree narrower (RDP grows),
            // back (shrinks). Each direction is therefore exercised twice, from different starts.
            (string Name, int Dx)[] drags =
            [
                ("1-rdp-shrinks", +DragDistance),
                ("2-rdp-grows", -DragDistance),
                ("3-rdp-grows-more", -DragDistance / 2),
                ("4-rdp-shrinks-back", +DragDistance / 2),
            ];

            List<string> observed = [];
            foreach ((string name, int dx) in drags)
            {
                Point grip = SplitterGrip();
                int treeWidthBefore = grip.X;
                Drag(grip, dx);
                Thread.Sleep(TimeSpan.FromSeconds(3));
                int treeWidthAfter = SplitterGrip().X;
                Snapshot($"{name}.png");

                List<(int W, int H)> calls = ReconnectCalls(VerboseLog());
                string line = $"{name}: splitter x {treeWidthBefore} -> {treeWidthAfter}; reconnect calls so far: "
                              + string.Join(", ", calls.Select(c => $"{c.W}x{c.H}"));
                TestContext.Out.WriteLine(line);
                observed.Add(line);
            }

            string verbose = VerboseLog();
            string resizeTrace = string.Join(Environment.NewLine,
                verbose.Split('\n').Where(l => l.Contains("Resize", StringComparison.Ordinal)
                                            || l.Contains("Reconnect", StringComparison.Ordinal)).TakeLast(60));
            TestContext.Out.WriteLine("--- verbose resize trace (tail) ---");
            TestContext.Out.WriteLine(resizeTrace);
            File.WriteAllText(Path.Combine(_evidenceDir, "resize-trace.txt"), verbose);

            List<(int W, int H)> all = ReconnectCalls(verbose);
            Assert.That(all.Count - reconnectsBefore, Is.GreaterThanOrEqualTo(drags.Length),
                        "fewer session resizes than splitter drags — at least one drag never reached the RDP session:"
                        + Environment.NewLine + string.Join(Environment.NewLine, observed));

            // The last two drags moved the panel by the same amount in opposite directions, so the
            // last two resizes must differ in width: a stale second-to-last width means the
            // shrinking drag was resized to the growing drag's size, or not at all.
            List<(int W, int H)> after = all.Skip(reconnectsBefore).ToList();
            Assert.That(after[^1].W, Is.LessThan(after[^2].W),
                        "the shrinking drag did not produce a narrower session than the growing drag before it");
        }

        private static List<(int W, int H)> ReconnectCalls(string verbose) =>
            Regex.Matches(verbose, @"Calling Reconnect\((\d+), (\d+)\)")
                 .Select(m => (int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value)))
                 .ToList();
    }
}

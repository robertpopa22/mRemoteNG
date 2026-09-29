using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// #158: closing a PuTTY tab asked the user to confirm twice — once in mRemoteNG and again in
    /// PuTTY's own warn-on-close box — and left the tab behind afterwards. The fix answers PuTTY's
    /// box on the user's behalf.
    ///
    /// That fix posts a click into a window belonging to another process, so the question this
    /// scenario exists to answer is not "does the tab close" but "which window did we just click".
    /// The unit tests pin <c>IsPuttyExitConfirmation</c> against titles written from memory; this
    /// records the titles PuTTY actually uses, on a real session, against a real SSH server. If
    /// PuTTY's security alert ever carried a title this matcher accepts, the fix would be
    /// dismissing host-key warnings, and no unit test written from the same assumption would say so.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class PuttyTabCloseAcceptanceTests : UiAcceptanceTestBase
    {
        private const string ConnectionName = "lab-linux-ssh";

        private string _evidenceDir = null!;

        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            seeder.Add(ConnectionName, LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                       LabTargets.LinuxUser, LabTargets.LinuxPassword);
            Deployment.WriteConnectionsFile(seeder.Build());

            // ConfirmCloseEnum.All (4): mRemoteNG asks before closing. That is the setting under
            // which the double prompt was reported, so it is the setting worth testing. This used to
            // seed 0 (Unspecified), which asks nothing at all, so "asks once" was never measured.
            // KeepTabsOpenAfterDisconnect off: the tab really closes, and with it the panel, which is
            // where a second, panel-level question used to come from.
            Deployment.WriteSettings(new Dictionary<string, string>
            {
                ["ConfirmCloseConnection"] = "4",
                ["KeepTabsOpenAfterDisconnect"] = "False",
            });

            _evidenceDir = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence",
                                        TestContext.CurrentContext.Test.MethodName ?? "putty");
            Directory.CreateDirectory(_evidenceDir);
        }

        private AutomationElement Row(string name)
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains(name, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private int SessionTabs() =>
            MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                      .Count(t =>
                      {
                          try { return LabTargets.IsLinuxSshTab(t.Name ?? "", ConnectionName); }
                          catch (Exception) { return false; }
                      });

        /// <summary>mRemoteNG's own confirmation (a CTaskDialog), if one is on screen.</summary>
        private AutomationElement? FindConfirmation()
        {
            int pid = Driver.Application.ProcessId;
            return Driver.Automation.GetDesktop()
                         .FindAllDescendants(cf => cf.ByAutomationId("frmTaskDialog"))
                         .FirstOrDefault(w =>
                         {
                             try { return w.Properties.ProcessId.Value == pid; }
                             catch (Exception) { return false; }
                         });
        }

        private static string Question(AutomationElement dialog)
        {
            try { return dialog.FindFirstDescendant(cf => cf.ByAutomationId("lbMainInstruction"))?.Name ?? ""; }
            catch (Exception) { return ""; }
        }

        /// <summary>Every top-level window owned by a PuTTY child process, as class + title.</summary>
        private static List<string> PuttyWindows()
        {
            List<string> found = [];
            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses())
            {
                string name;
                try { name = process.ProcessName; }
                catch (Exception) { continue; }
                if (!name.Contains("putty", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach ((string cls, string title) in Win32Windows.TopLevelWindowsOf(process.Id))
                    found.Add($"{process.ProcessName}({process.Id}) class='{cls}' title='{title}'");
            }
            return found;
        }

        /// <summary>
        /// Accepts PuTTY's host-key alert, once, deliberately, from the test.
        ///
        /// The contrast is the point. This battery clicks that dialog because a scenario author
        /// decided a throwaway lab host is trustworthy and wrote it down. The product never clicks
        /// it: <c>IsPuttyExitConfirmation</c> requires the standard dialog class and a title
        /// containing "Exit Confirmation", and the alert observed in this lab is
        /// class 'PuTTYHostKeyDialog', title 'PuTTY Security Alert' — wrong on both counts.
        /// </summary>
        private bool AcceptHostKeyAlertIfPresent()
        {
            foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses())
            {
                string processName;
                try { processName = process.ProcessName; }
                catch (Exception) { continue; }
                if (!processName.Contains("putty", StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach ((IntPtr handle, string cls, string title) in Win32Windows.WindowsOf(process.Id))
                {
                    if (!title.Contains("Security Alert", StringComparison.OrdinalIgnoreCase))
                        continue;

                    TestContext.Out.WriteLine($"host-key alert found: class='{cls}' title='{title}'");
                    AutomationElement alert;
                    try { alert = Driver.Automation.FromHandle(handle); }
                    catch (Exception ex)
                    {
                        TestContext.Out.WriteLine($"  cannot attach to it: {ex.GetType().Name}");
                        return false;
                    }

                    AutomationElement? accept = alert
                        .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                        .FirstOrDefault(b =>
                        {
                            try { return (b.Name ?? "").Contains("Accept", StringComparison.OrdinalIgnoreCase); }
                            catch (Exception) { return false; }
                        });
                    if (accept is null)
                    {
                        TestContext.Out.WriteLine("  no Accept button on it; leaving it alone");
                        return false;
                    }

                    Win32Mouse.LeftClick(accept);
                    Thread.Sleep(TimeSpan.FromSeconds(3));
                    return true;
                }
            }
            return false;
        }

        [Test]
        [Issues("#158")]
        public void ClosingAPuttyTabAsksOnceAndLeavesNothingBehind()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore($"lab SSH target {LabTargets.LinuxHost}:{LabTargets.Ssh} is not reachable.");
            Assert.That(LabTargets.LinuxPassword, Is.Not.Empty,
                        "MRNG_LAB_LINUX_PASSWORD is not visible to the battery process");

            Win32Mouse.DoubleClick(Row(ConnectionName));
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
            // The lab's SSH path is not dependable enough to fail a build over: the first connect
            // raises a host-key alert, a stale cached key or a refused password ends the session
            // before a tab settles, and none of that says anything about the change under test.
            // When no session comes up there is nothing to measure, so say so rather than go red.
            if (!UiWait.Happened(() => SessionTabs() > 0, TimeSpan.FromSeconds(60)))
                Assert.Ignore("no SSH session tab appeared, so the close path was never exercised.");

            // First contact with a lab host: PuTTY asks about the host key and will not
            // authenticate until somebody answers. Answer it here, explicitly, so the rest of the
            // scenario exercises a live session rather than a stalled one.
            for (int i = 0; i < 20 && !AcceptHostKeyAlertIfPresent(); i++)
                Thread.Sleep(500);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(10));
            Thread.Sleep(TimeSpan.FromSeconds(6));

            // What PuTTY has on screen while the session is up. The terminal window belongs here;
            // a dialog does not.
            List<string> whileOpen = PuttyWindows();
            TestContext.Out.WriteLine("--- PuTTY windows while connected ---");
            whileOpen.ForEach(TestContext.Out.WriteLine);
            if (whileOpen.Count == 0)
                Assert.Ignore("no PuTTY process window was found, so nothing here was measured.");

            string[] tabNames = [.. MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .Select(t => { try { return t.Name ?? ""; } catch (Exception) { return ""; } })];
            TestContext.Out.WriteLine($"tabs on screen: {string.Join(" | ", tabNames)}");

            AutomationElement? tab = MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem))
                .FirstOrDefault(t =>
                {
                    try { return LabTargets.IsLinuxSshTab(t.Name ?? "", ConnectionName); }
                    catch (Exception) { return false; }
                });
            Assert.That(tab, Is.Not.Null,
                        "the session tab is gone before the close was attempted; tabs present: "
                        + string.Join(" | ", tabNames));
            string caption = tab!.Name ?? "";
            Win32Mouse.MiddleClick(tab);

            // The one question the user is meant to see: mRemoteNG's own, about this connection.
            UiWait.Until(() => FindConfirmation() is not null, "mRemoteNG's close confirmation",
                         TimeSpan.FromSeconds(15));
            AutomationElement confirmation = FindConfirmation()!;
            string question = Question(confirmation);
            TestContext.Out.WriteLine($"mRemoteNG asked: \"{question}\"");
            // The tab's caption, whichever it shows at this moment: the connection name or the
            // terminal title PuTTY handed it.
            Assert.That(question, Does.Contain(caption).And.Not.Contain("panel").IgnoreCase,
                        "the tab's confirmation should ask about this tab's connection, not a panel");
            UiWait.FindRequired(confirmation, cf => cf.ByAutomationId("bt2"), "Disconnect button").AsButton().Invoke();

            // Whatever PuTTY puts up during the close is the window the fix acts on. Sample it
            // while the close is in flight rather than after, or there is nothing left to see.
            // A second mRemoteNG confirmation during the same window is the other half of "asks
            // once" and is recorded rather than answered.
            List<string> duringClose = [];
            string? secondQuestion = null;
            for (int i = 0; i < 40 && SessionTabs() > 0; i++)
            {
                foreach (string w in PuttyWindows())
                    if (!duringClose.Contains(w))
                        duringClose.Add(w);
                if (secondQuestion is null && FindConfirmation() is { } again)
                    secondQuestion = Question(again);
                Thread.Sleep(50);
            }

            TestContext.Out.WriteLine("--- PuTTY windows during the close ---");
            duringClose.ForEach(TestContext.Out.WriteLine);
            File.WriteAllText(Path.Combine(_evidenceDir, "putty-windows.txt"),
                              string.Join(Environment.NewLine,
                                          ["while connected:", .. whileOpen,
                                           "", "during close:", .. duringClose]));

            // Closing the only tab closes its panel, and the panel used to ask again because it
            // still counted the tab being disposed. Give it the time it took then and more.
            if (secondQuestion is null && UiWait.Happened(() => FindConfirmation() is not null, TimeSpan.FromSeconds(8)))
                secondQuestion = FindConfirmation() is { } late ? Question(late) : "";
            if (secondQuestion is not null && FindConfirmation() is { } leftOver)
                UiWait.FindRequired(leftOver, cf => cf.ByAutomationId("bt3"), "Cancel button").AsButton().Invoke();
            Assert.That(secondQuestion, Is.Null,
                        $"mRemoteNG asked a second time after the user confirmed: \"{secondQuestion}\"");

            // Nothing else may be left on screen. PuTTY's own warn-on-close box is answered by the
            // product; the harness would answer it too ("Exit Confirmation" matches its whitelist),
            // which would hide exactly the #158 regression, so it is looked for and not answered.
            List<string> leftOpen = [.. ModalDialogs.Find(Driver).Select(d => d.ToString())];
            Assert.That(leftOpen, Is.Empty,
                        "a dialog is still waiting for the user after one confirmation: "
                        + string.Join("; ", leftOpen));

            // The security property, checked against what PuTTY really showed rather than against
            // a remembered string: nothing we were willing to click was a security alert.
            foreach (string window in duringClose)
            {
                bool looksLikeAnAlert = window.Contains("Security Alert", StringComparison.OrdinalIgnoreCase)
                                        || window.Contains("Host Key", StringComparison.OrdinalIgnoreCase);
                Assert.That(looksLikeAnAlert, Is.False,
                            $"a PuTTY security dialog appeared during an ordinary tab close: {window}");
            }

            UiWait.Until(() => SessionTabs() == 0,
                         "the PuTTY tab to close without a second prompt", TimeSpan.FromSeconds(30));

            Thread.Sleep(TimeSpan.FromSeconds(2));
            List<string> afterClose = PuttyWindows();
            TestContext.Out.WriteLine("--- PuTTY windows after the close ---");
            afterClose.ForEach(TestContext.Out.WriteLine);
            Assert.That(afterClose, Is.Empty,
                        "a PuTTY window outlived the tab that owned it: "
                        + string.Join(" | ", afterClose));
        }
    }
}

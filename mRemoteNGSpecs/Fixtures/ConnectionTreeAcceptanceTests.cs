using System;
using System.Linq;
using System.IO;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The connection tree: filtering, and the virtual-list behaviour behind several crash reports.
    ///
    /// The tree is an ObjectListView in virtual mode, which is why this area produced so many
    /// index-out-of-range crashes: the control asks for a row the model has just invalidated. That
    /// interaction is between a native control and the message loop, so the unit suite can only
    /// approach it indirectly.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class ConnectionTreeAcceptanceTests : UiAcceptanceTestBase
    {
        private const int NonMatchingCount = 24;

        protected override void SeedSettings()
        {
            // One row matches the filter, the rest do not, so a filtered view showing more than one
            // row is unambiguous evidence the filter dropped.
            ConnectionsSeeder seeder = new();
            seeder.Add("db-primary", "127.0.0.1", ProtocolType.SSH2, 1);
            seeder.AddUnreachable("web", NonMatchingCount);
            Deployment.WriteConnectionsFile(seeder.Build());
        }

        private AutomationElement Tree() =>
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");

        private AutomationElement SearchBox() =>
            UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("txtSearch"), "tree search box");

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }

        /// <summary>Named rows only: the tree also exposes sub-item elements with empty names.</summary>
        private string[] VisibleConnectionRows() =>
            Tree().FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                  .Select(SafeName)
                  .Where(n => n.Length > 0)
                  .ToArray();

        [Test]
        public void TheSeededTreeLoadsEveryConnection()
        {
            UiWait.Until(() => VisibleConnectionRows().Count(n => n.StartsWith("web-", StringComparison.Ordinal)) == NonMatchingCount,
                         $"all {NonMatchingCount} seeded connections to appear",
                         TimeSpan.FromSeconds(20));

            string[] rows = VisibleConnectionRows();
            TestContext.Out.WriteLine($"rows loaded: {rows.Length}");
            Assert.That(rows, Does.Contain("db-primary"),
                        "the matching connection is missing, so the filter tests below would be vacuous");
        }

        [TestCase(false)]
        [TestCase(true)]
        [Issues("#200")]
        public void OpeningAnotherConnectionsFileReplacesTheVisibleTree(bool reloadLayout)
        {
            if (reloadLayout)
                SaveAndReloadLayout();

            ConnectionsSeeder replacement = new();
            replacement.Add("replacement-only", "127.0.0.1", ProtocolType.SSH2, 1);
            string path = Path.Combine(Deployment.Directory, "replacement.xml");
            File.WriteAllText(path, replacement.Build());

            MainWindow.Focus();
            Keyboard.TypeSimultaneously(VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_O);
            // Native modal dialogs can block the owner's UIA provider.
            UiWait.Until(() => Win32Dialogs.Find([Driver.Application.ProcessId]).Any(d => d.Title == "Open"),
                "connection file picker", TimeSpan.FromSeconds(15));
            Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.KEY_N);
            Keyboard.Type(path);
            Keyboard.Press(VirtualKeyShort.RETURN);
            Keyboard.Release(VirtualKeyShort.RETURN);

            Win32Dialogs.Dialog? question = null;
            UiWait.Until(() => (question = Win32Dialogs.Find([Driver.Application.ProcessId])
                .FirstOrDefault(d => d.Text.StartsWith("Replace current connections with this file", StringComparison.Ordinal))) is not null,
                "replace-or-add question", TimeSpan.FromSeconds(15));
            Keyboard.TypeSimultaneously(VirtualKeyShort.ALT, VirtualKeyShort.KEY_Y);
            UiWait.Until(() => VisibleConnectionRows().Contains("replacement-only"),
                "replacement connections to appear", TimeSpan.FromSeconds(20));
            Assert.That(VisibleConnectionRows(), Does.Not.Contain("db-primary"));
            AssertNoCrash("after File > Open > Replace");
            Assert.That(Deployment.ReadAppLog(), Does.Not.Contain("Invoke or BeginInvoke cannot be called"));
            string evidence = Path.Combine(Path.GetDirectoryName(Deployment.Directory)!, "_evidence", "tree-file-open");
            Directory.CreateDirectory(evidence);
            string screenshot = Path.Combine(evidence, reloadLayout ? "after-layout-reload.png" : "normal-open.png");
            Capture.Element(MainWindow).ToFile(screenshot);
            TestContext.AddTestAttachment(screenshot);
            TestContext.Out.WriteLine("File > Open > Replace showed replacement-only and removed db-primary; no handle exception.");
        }

        private AutomationElement ViewMenuItem(string name)
        {
            UiWait.FindRequired(MainWindow,
                cf => cf.ByName("View").And(cf.ByControlType(ControlType.MenuItem)), "View menu")
                .Patterns.ExpandCollapse.Pattern.Expand();
            return UiWait.FindRequired(Driver.Automation.GetDesktop(),
                cf => cf.ByName(name).And(cf.ByControlType(ControlType.MenuItem)), name);
        }

        private void SaveAndReloadLayout()
        {
            ViewMenuItem("Save Layout...").Click();
            AutomationElement save = UiWait.FindRequired(Driver.Automation.GetDesktop(),
                cf => cf.ByAutomationId("FrmInputBox"), "layout name prompt");
            UiWait.FindRequired(save, cf => cf.ByAutomationId("textBox"), "layout name")
                .AsTextBox().Text = "file-open-repro";
            UiWait.FindRequired(save, cf => cf.ByAutomationId("_Ok"), "save layout")
                .AsButton().Invoke();
            UiWait.Settle(MainWindow);
            ViewMenuItem("Load Layout").Click();
            UiWait.FindRequired(Driver.Automation.GetDesktop(),
                cf => cf.ByName("file-open-repro").And(cf.ByControlType(ControlType.MenuItem)), "saved layout")
                .Click();
            UiWait.Settle(MainWindow);
            AssertNoCrash("after reloading the saved layout");
        }

        /// <summary>
        /// Touches #144. It asserts that the filter holds logically across a connect burst — the
        /// event that triggered the regression, because adding a node to a smart group raised a
        /// structural change that dropped and reapplied the filter.
        ///
        /// MEASURED SCOPE: this does NOT detect #144 itself. The fix was removed from
        /// ConnectionTree.HandleCollectionChanged and this test still passed, because the defect is
        /// a *painting* artifact — the tree repaints an intermediate unfiltered state — while UIA
        /// reports the list model, not what is on the glass. Catching the flash would need
        /// screenshot sampling during the burst, which is a different and far less reliable
        /// technique. Labelled Touches so the suite does not claim a guard it does not have.
        /// </summary>
        [Test]
        [Touches("#144")]
        public void AFilteredTreeNeverShowsUnfilteredRowsWhileConnecting()
        {
            UiWait.Until(() => VisibleConnectionRows().Length > NonMatchingCount,
                         "the tree to finish loading", TimeSpan.FromSeconds(20));

            SearchBox().AsTextBox().Text = "db-primary";
            UiWait.Settle(MainWindow);

            UiWait.Until(() => !VisibleConnectionRows().Any(n => n.StartsWith("web-", StringComparison.Ordinal)),
                         "the tree to filter down to the matching connection",
                         TimeSpan.FromSeconds(15));

            AutomationElement row = Tree()
                .FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .First(e => string.Equals(SafeName(e), "db-primary", StringComparison.Ordinal));

            row.DoubleClick();

            // Port 1 on loopback refuses immediately, so the attempt fails fast and needs no
            // external dependency — but the tree events still fire, which is what matters.
            int worstCase = 0;
            DateTime deadline = DateTime.UtcNow.AddSeconds(6);
            while (DateTime.UtcNow < deadline)
            {
                int leaked = VisibleConnectionRows().Count(n => n.StartsWith("web-", StringComparison.Ordinal));
                worstCase = Math.Max(worstCase, leaked);
                if (worstCase > 0) break;
            }

            TestContext.Out.WriteLine($"max non-matching rows seen during connect: {worstCase}");
            Assert.That(worstCase, Is.Zero,
                        "the filtered connection tree reported connections that do not match the "
                        + "filter while connecting — the filter was dropped, not merely repainted");

            AssertNoCrash("after connecting from a filtered tree");
        }

        /// <summary>
        /// Stress coverage for #135, #126 and #127 — index-out-of-range crashes in the virtual-mode
        /// list when the model shrinks while the control is asking for rows.
        ///
        /// This is deliberately NOT labelled as covering those issues. They are races: driving the
        /// path hard makes a crash more likely to surface, but a pass means "did not reproduce this
        /// time", never "the race is gone". Recording it as regression proof would be false comfort.
        /// </summary>
        [Test]
        [StressCoverage("#135", "#126", "#127")]
        public void RapidFilteringAndScrollingDoesNotCrash()
        {
            UiWait.Until(() => VisibleConnectionRows().Length > NonMatchingCount,
                         "the tree to finish loading", TimeSpan.FromSeconds(20));

            AutomationElement search = SearchBox();
            string[] terms = ["web", "web-1", "db", "", "web-0", "zzz", "", "db-primary", "web"];

            for (int pass = 0; pass < 4; pass++)
            {
                foreach (string term in terms)
                {
                    // No settle between keystrokes on purpose: the crash needs the control to be
                    // asking for rows while the model is being replaced underneath it.
                    search.AsTextBox().Text = term;
                }
            }

            UiWait.Settle(MainWindow);
            AssertNoCrash("after rapid filter changes over a virtual-mode tree");

            // The tree must still work afterwards, not merely have avoided crashing.
            search.AsTextBox().Text = "";
            UiWait.Until(() => VisibleConnectionRows().Count(n => n.StartsWith("web-", StringComparison.Ordinal)) == NonMatchingCount,
                         "the tree to return to showing every connection",
                         TimeSpan.FromSeconds(15));
        }
    }
}

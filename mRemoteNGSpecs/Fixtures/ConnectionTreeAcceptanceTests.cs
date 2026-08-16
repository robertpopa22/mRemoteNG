using System;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
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

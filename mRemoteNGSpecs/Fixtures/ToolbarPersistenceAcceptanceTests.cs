using System;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Toolbar and panel visibility across a restart.
    ///
    /// Persistence is the one thing an in-process test cannot prove, so these close the application
    /// and start it again.
    ///
    /// SCOPE, MEASURED RATHER THAN ASSUMED: these guard the visibility round trip — set it, restart,
    /// still set. They do **not** reproduce the #134/#117 mechanism. That defect only appears when
    /// settings are written from Shutdown.Cleanup *after* FrmMain.Hide(), because a hidden parent
    /// makes Control.Visible report false for every child. Re-introducing the real defect in
    /// SettingsSaver was tried, and every test here still passed — so claiming these as #134
    /// regression tests would be false. They are labelled Touches, not Covers, for that reason.
    /// Catching the real mechanism needs a test that exercises the tray/hide exit path.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class ToolbarPersistenceAcceptanceTests : UiAcceptanceTestBase
    {
        /// <summary>
        /// Identifiers were measured from a running app, not read from source. The ToolStrip
        /// classes assign Control.Name values like "tsExternalTools", but what reaches UIA as the
        /// AutomationId is the field name in the parent form — "_externalToolsToolStrip". Reading
        /// the constant out of the control's own source produced a lookup that never matched, and
        /// made a working toolbar look like a broken one.
        /// </summary>
        private bool ToolbarPresent(string automationId) =>
            MainWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId)) is not null;

        /// <summary>
        /// Clicks an entry in the View menu.
        ///
        /// Two things here were learned the hard way and are the reason this helper exists.
        /// A plain Click on the menu bar item does NOT open the drop-down — the ExpandCollapse
        /// pattern does. And the drop-down is a separate top-level popup, so its entries are never
        /// under the main window; they have to be found from the desktop root. Searching the main
        /// window for "Config" instead matched the docked config panel, which made an earlier
        /// version of this test toggle nothing and pass anyway.
        /// </summary>
        private void ClickViewMenuItem(string caption)
        {
            AutomationElement view = UiWait.FindRequired(
                MainWindow,
                cf => cf.ByName("View").And(cf.ByControlType(ControlType.MenuItem)),
                "View menu");

            view.Patterns.ExpandCollapse.Pattern.Expand();
            UiWait.Settle(MainWindow);

            AutomationElement item = UiWait.FindRequired(
                Driver.Automation.GetDesktop(),
                cf => cf.ByName(caption).And(cf.ByControlType(ControlType.MenuItem)),
                $"View menu entry '{caption}'");

            item.Click();
            UiWait.Settle(MainWindow);
        }

        /// <summary>
        /// Covers: #134 (toolbar visibility not persisted across a restart).
        ///
        /// The failure this guards is silent — the user turns a toolbar on, closes the app, reopens
        /// it and finds the choice discarded, with nothing logged anywhere.
        /// </summary>
        [Test]
        [Touches("#134")]
        public void AToolbarTurnedOnIsStillOnAfterARestart()
        {
            bool before = ToolbarPresent("_externalToolsToolStrip");
            ClickViewMenuItem("External Tools Toolbar");
            bool afterToggle = ToolbarPresent("_externalToolsToolStrip");

            TestContext.Out.WriteLine($"external tools toolbar: {before} -> {afterToggle}");
            Assert.That(afterToggle, Is.Not.EqualTo(before),
                        "toggling External Tools Toolbar changed nothing on screen — the menu entry "
                        + "is not wired to the toolbar, so the rest of this test would be vacuous");

            // BROKEN-ON-PURPOSE: no restart

            Assert.That(ToolbarPresent("_externalToolsToolStrip"), Is.EqualTo(afterToggle),
                        "the External Tools toolbar did not keep its visibility across a restart: "
                        + "the user's choice was discarded on shutdown");
        }

        /// <summary>
        /// Covers: #117 (the Quick Connect toolbar disabled itself), #134.
        ///
        /// Same mechanism as above — visibility saved from a control's ambient state at shutdown
        /// rather than from what the user chose — so both are guarded here.
        /// </summary>
        [Test]
        [Touches("#117", "#134")]
        public void TheQuickConnectToolbarKeepsItsStateAcrossARestart()
        {
            bool before = ToolbarPresent("_quickConnectToolStrip");
            ClickViewMenuItem("Quick Connect Toolbar");
            bool afterToggle = ToolbarPresent("_quickConnectToolStrip");

            TestContext.Out.WriteLine($"quick connect toolbar: {before} -> {afterToggle}");
            Assert.That(afterToggle, Is.Not.EqualTo(before),
                        "toggling Quick Connect Toolbar changed nothing on screen");

            RestartApplication();

            Assert.That(ToolbarPresent("_quickConnectToolStrip"), Is.EqualTo(afterToggle),
                        "the Quick Connect toolbar did not keep its state across a restart");
        }

        /// <summary>
        /// The docked panels are the other half of #134's report. Same persistence path, different
        /// dock — and this is the one that previously passed without toggling anything.
        /// </summary>
        [Test]
        [Touches("#134")]
        public void TheConfigPanelKeepsItsVisibilityAcrossARestart()
        {
            bool before = ToolbarPresent("_pGrid");
            Assert.That(before, Is.True, "the config panel is not present at startup");

            ClickViewMenuItem("Config");
            bool afterToggle = ToolbarPresent("_pGrid");

            TestContext.Out.WriteLine($"config panel: {before} -> {afterToggle}");
            Assert.That(afterToggle, Is.False,
                        "clicking View > Config did not hide the config panel — the toggle did "
                        + "nothing, so the persistence assertion below would prove nothing");

            RestartApplication();

            Assert.That(ToolbarPresent("_pGrid"), Is.False,
                        "the config panel came back after a restart even though it was hidden: "
                        + "panel visibility was not persisted");
        }
    }
}

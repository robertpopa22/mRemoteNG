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
    /// Separates "the application will not close" from "the automation cannot ask it to close".
    ///
    /// The session tests fail on MainWindow.Close() with ElementNotEnabledException / COM
    /// 0x80040200. That is either the #110 family resurfacing, or FlaUI's WindowPattern simply not
    /// being available on this window — and those need opposite responses. Reporting the first
    /// without excluding the second is how a harness limitation gets filed as a product bug.
    ///
    /// Diagnostic, not a regression test: it reports rather than asserts a product property.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    [Explicit("Diagnostic — run deliberately, not as part of the battery.")]
    public class WindowCloseDiagnosticTests : UiAcceptanceTestBase
    {
        protected override void SeedSettings()
        {
            ConnectionsSeeder seeder = new();
            if (LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                seeder.Add("lab-linux-ssh", LabTargets.LinuxHost, ProtocolType.SSH2, LabTargets.Ssh,
                           LabTargets.LinuxUser, LabTargets.LinuxPassword);
            Deployment.WriteConnectionsFile(seeder.Build());
        }

        [Test]
        public void ReportHowTheWindowCanBeClosedWithNoSessionOpen()
        {
            ReportCloseCapabilities("no session open");
        }

        [Test]
        public void ReportHowTheWindowCanBeClosedWithASessionOpen()
        {
            if (!LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Ssh))
                Assert.Ignore("lab SSH target unreachable");

            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            AutomationElement? row = tree
                .FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                .FirstOrDefault(e => SafeName(e).Contains("lab-linux-ssh", StringComparison.OrdinalIgnoreCase));

            Assert.That(row, Is.Not.Null, "seeded SSH connection not found");
            row!.DoubleClick();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => MainWindow.FindAllDescendants(cf => cf.ByControlType(ControlType.TabItem)).Length > 0,
                         "a session tab", TimeSpan.FromSeconds(30));

            ReportCloseCapabilities("one SSH session open");
        }

        private void ReportCloseCapabilities(string state)
        {
            TestContext.Out.WriteLine($"--- close capabilities with {state} ---");

            bool hasPattern = MainWindow.Patterns.Window.IsSupported;
            TestContext.Out.WriteLine($"WindowPattern supported : {hasPattern}");
            if (hasPattern)
            {
                try
                {
                    TestContext.Out.WriteLine(
                        $"CanMaximize             : {MainWindow.Patterns.Window.Pattern.CanMaximize}");
                }
                catch (Exception ex)
                {
                    TestContext.Out.WriteLine($"pattern read threw      : {ex.GetType().Name}");
                }
            }

            // 1. the pattern-based close FlaUI uses
            string patternResult;
            try
            {
                MainWindow.Close();
                patternResult = UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(8))
                    ? "EXITED" : "no exit";
            }
            catch (Exception ex)
            {
                patternResult = $"threw {ex.GetType().Name}";
            }
            TestContext.Out.WriteLine($"MainWindow.Close()      : {patternResult}");

            if (Driver.Application.HasExited) { TestContext.Out.WriteLine("(closed by pattern)"); return; }

            // 2. the title-bar button a user would click
            string buttonResult = "not found";
            AutomationElement? closeButton = MainWindow
                .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                .FirstOrDefault(b => SafeName(b).Contains("Close", StringComparison.OrdinalIgnoreCase));
            if (closeButton is not null)
            {
                try
                {
                    closeButton.Click();
                    buttonResult = UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(8))
                        ? "EXITED" : "no exit";
                }
                catch (Exception ex)
                {
                    buttonResult = $"threw {ex.GetType().Name}";
                }
            }
            TestContext.Out.WriteLine($"title-bar Close button  : {buttonResult}");

            if (Driver.Application.HasExited) { TestContext.Out.WriteLine("(closed by button)"); return; }

            // 3. Alt+F4, the keyboard route
            string keyResult;
            try
            {
                MainWindow.Focus();
                FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT, FlaUI.Core.WindowsAPI.VirtualKeyShort.F4);
                keyResult = UiWait.Happened(() => Driver.Application.HasExited, TimeSpan.FromSeconds(8))
                    ? "EXITED" : "no exit";
            }
            catch (Exception ex)
            {
                keyResult = $"threw {ex.GetType().Name}";
            }
            TestContext.Out.WriteLine($"Alt+F4                  : {keyResult}");
            TestContext.Out.WriteLine($"still running           : {!Driver.Application.HasExited}");
        }

        /// <summary>
        /// Prints the real View-menu entries. WinForms ToolStrip items do not reliably expose
        /// Control.Name as an AutomationId, so the battery has to match on caption — and guessing
        /// captions has already cost two runs.
        /// </summary>
        [Test]
        public void ReportViewMenuContents()
        {
            AutomationElement view = UiWait.FindRequired(
                MainWindow,
                cf => cf.ByName("View").And(cf.ByControlType(ControlType.MenuItem)),
                "View menu");
            view.Click();
            UiWait.Settle(MainWindow);

            // Drop-downs are separate top-level popup windows in WinForms, so they are NOT under
            // MainWindow — searching there returns only the menu bar itself.
            foreach (AutomationElement item in Driver.Automation.GetDesktop().FindAllDescendants(
                         cf => cf.ByControlType(ControlType.MenuItem)))
            {
                string id = "";
                try { id = item.AutomationId; } catch (Exception) { }
                TestContext.Out.WriteLine($"MENUITEM name='{SafeName(item)}' id='{id}'");
            }
        }

        /// <summary>
        /// Tries every plausible way to open a WinForms menu and reports which works.
        ///
        /// A plain Click on the menu bar item does not expand it, and drop-downs are separate
        /// top-level popups so they never appear under the main window. Rather than guess a fourth
        /// time, this measures: the Menu helper, the ExpandCollapse pattern, and the keyboard.
        /// </summary>
        [Test]
        public void ReportHowTheViewMenuCanBeOpened()
        {
            // A: FlaUI's Menu helper, which knows how to expand ToolStrip menus.
            try
            {
                AutomationElement? bar = MainWindow.FindFirstDescendant(cf => cf.ByControlType(ControlType.MenuBar));
                TestContext.Out.WriteLine($"A menubar found        : {bar is not null}");
                if (bar is not null)
                {
                    var menu = bar.AsMenu();
                    var top = menu.Items.Select(i => i.Name).ToArray();
                    TestContext.Out.WriteLine("A top-level items      : " + string.Join(" | ", top));

                    var view = menu.Items.FirstOrDefault(i => i.Name == "View");
                    if (view is not null)
                    {
                        var children = view.Items.Select(i => i.Name).ToArray();
                        TestContext.Out.WriteLine($"A View children ({children.Length}) : " + string.Join(" | ", children));
                    }
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"A threw                : {ex.GetType().Name}: {ex.Message}");
            }

            // B: ExpandCollapse on the menu bar item.
            try
            {
                AutomationElement view = UiWait.FindRequired(
                    MainWindow, cf => cf.ByName("View").And(cf.ByControlType(ControlType.MenuItem)), "View");
                bool supported = view.Patterns.ExpandCollapse.IsSupported;
                TestContext.Out.WriteLine($"B ExpandCollapse       : {supported}");
                if (supported)
                {
                    view.Patterns.ExpandCollapse.Pattern.Expand();
                    UiWait.Settle(MainWindow);
                    var items = Driver.Automation.GetDesktop()
                        .FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                        .Select(SafeName).Where(n => n.Length > 0).ToArray();
                    TestContext.Out.WriteLine($"B items after expand   : " + string.Join(" | ", items));
                }
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"B threw                : {ex.GetType().Name}: {ex.Message}");
            }

            // C: keyboard — Alt+V is how a user opens it.
            try
            {
                MainWindow.Focus();
                FlaUI.Core.Input.Keyboard.TypeSimultaneously(
                    FlaUI.Core.WindowsAPI.VirtualKeyShort.ALT, FlaUI.Core.WindowsAPI.VirtualKeyShort.KEY_V);
                UiWait.Settle(MainWindow);
                var items = Driver.Automation.GetDesktop()
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.MenuItem))
                    .Select(SafeName).Where(n => n.Length > 0).ToArray();
                TestContext.Out.WriteLine($"C items after Alt+V ({items.Length}) : " + string.Join(" | ", items));
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"C threw                : {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Dumps every toolbar before and after toggling one from the View menu, so "the toolbar
        /// did not appear" can be told apart from "the toolbar appeared under a different
        /// identifier". ToolStrip did not surface Control.Name as an AutomationId for menu items;
        /// assuming it does for toolbars would repeat that mistake.
        /// </summary>
        [Test]
        public void ReportToolbarsBeforeAndAfterToggling()
        {
            DumpToolbars("before");

            AutomationElement view = UiWait.FindRequired(
                MainWindow, cf => cf.ByName("View").And(cf.ByControlType(ControlType.MenuItem)), "View menu");
            view.Patterns.ExpandCollapse.Pattern.Expand();
            UiWait.Settle(MainWindow);

            AutomationElement item = UiWait.FindRequired(
                Driver.Automation.GetDesktop(),
                cf => cf.ByName("External Tools Toolbar").And(cf.ByControlType(ControlType.MenuItem)),
                "External Tools Toolbar entry");
            TestContext.Out.WriteLine($"menu entry toggle state: {item.Patterns.Toggle.PatternOrDefault?.ToggleState}");
            item.Click();
            UiWait.Settle(MainWindow);

            DumpToolbars("after");
        }

        private void DumpToolbars(string label)
        {
            foreach (AutomationElement tb in MainWindow.FindAllDescendants(
                         cf => cf.ByControlType(ControlType.ToolBar)))
            {
                string id = "";
                try { id = tb.AutomationId; } catch (Exception) { }
                TestContext.Out.WriteLine($"TOOLBAR[{label}] name='{SafeName(tb)}' id='{id}' offscreen={tb.IsOffscreen}");
            }
        }

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name; } catch (Exception) { return ""; }
        }
    }
}

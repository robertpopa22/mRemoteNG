using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// The SQL Server options page is about 740 px tall and FrmOptions shows every page docked
    /// to fill whatever height the options pane has. Without a scrollbar of its own, the page's
    /// lower part — the Test connection button and the message under it — was simply cut off
    /// (#165, "the error label keeps being cut"). The page must scroll when it does not fit.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class SqlServerOptionsPageAcceptanceTests : UiAcceptanceTestBase
    {
        private const int GwlStyle = -16;
        private const int WsVScroll = 0x00200000;
        private const uint WmVScroll = 0x0115;
        private const int SbBottom = 7;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        private void OpenOptions()
        {
            // The drop-down is a separate popup, found from the desktop, not under the main window.
            AutomationElement file = UiWait.FindRequired(
                MainWindow, cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File menu");
            file.Patterns.ExpandCollapse.Pattern.Expand();
            UiWait.Settle(MainWindow);

            UiWait.FindRequired(Driver.Automation.GetDesktop(),
                                cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)),
                                "File menu entry 'Options...'").Click();
            UiWait.Settle(MainWindow);
        }

        [Test]
        [Issues("#165")]
        public void TheSqlServerPageScrollsInsteadOfCuttingOffTheTestConnectionButton()
        {
            OpenOptions();

            AutomationElement pages = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("lstOptionPages"), "options page list");
            UiWait.FindRequired(pages, cf => cf.ByName("SQL Server"), "SQL Server page entry")
                  .Patterns.SelectionItem.Pattern.Select();
            UiWait.Settle(MainWindow);

            AutomationElement useSql = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("chkUseSQLServer"), "Use SQL Server checkbox");
            if (useSql.Patterns.Toggle.Pattern.ToggleState.Value != ToggleState.On)
                useSql.Patterns.Toggle.Pattern.Toggle();
            UiWait.Settle(MainWindow);

            AutomationElement pane = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("pnlMain"), "options host pane");
            AutomationElement page = UiWait.FindRequired(pane, cf => cf.ByControlType(ControlType.Pane), "SQL Server page");
            AutomationElement button = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnTestConnection"), "Test connection button");

            Rectangle paneRect = pane.BoundingRectangle;
            Rectangle before = button.BoundingRectangle;
            TestContext.Out.WriteLine($"options pane: top={paneRect.Top} bottom={paneRect.Bottom} height={paneRect.Height}");
            TestContext.Out.WriteLine($"Test connection button before scrolling: top={before.Top} bottom={before.Bottom}");

            IntPtr pageHandle = new(page.Properties.NativeWindowHandle.ValueOrDefault);
            bool hasScrollbar = (GetWindowLong(pageHandle, GwlStyle) & WsVScroll) != 0;
            bool fitsAlready = before.Bottom <= paneRect.Bottom;
            TestContext.Out.WriteLine($"page has a vertical scrollbar: {hasScrollbar}; button already inside the pane: {fitsAlready}");

            string shotBefore = Path.Combine(Deployment.Directory, "sql-page-before-scroll.png");
            Capture.Element(MainWindow).ToFile(shotBefore);
            TestContext.AddTestAttachment(shotBefore, "SQL Server page before scrolling");

            Assert.That(hasScrollbar || fitsAlready, Is.True,
                        "the Test connection button is below the visible pane and the page offers no scrollbar — "
                        + "that is the #165 cut, only the button instead of the label");

            if (!fitsAlready)
            {
                // What a user does with the scrollbar, sent as the scrollbar's own message so the
                // check does not depend on wheel focus rules.
                SendMessage(pageHandle, WmVScroll, new IntPtr(SbBottom), IntPtr.Zero);
                UiWait.Settle(MainWindow);

                Rectangle after = button.BoundingRectangle;
                TestContext.Out.WriteLine($"Test connection button after scrolling to the bottom: top={after.Top} bottom={after.Bottom}");

                string shotAfter = Path.Combine(Deployment.Directory, "sql-page-after-scroll.png");
                Capture.Element(MainWindow).ToFile(shotAfter);
                TestContext.AddTestAttachment(shotAfter, "SQL Server page after scrolling");

                Assert.Multiple(() =>
                {
                    Assert.That(after.Top, Is.LessThan(before.Top), "scrolling moved nothing");
                    Assert.That(after.Bottom, Is.LessThanOrEqualTo(paneRect.Bottom),
                                "after scrolling to the bottom the Test connection button is still below the pane");
                    Assert.That(after.Top, Is.GreaterThanOrEqualTo(paneRect.Top),
                                "after scrolling to the bottom the Test connection button went above the pane");
                });
            }

            AssertNoCrash("after scrolling the SQL Server options page");
        }
    }
}

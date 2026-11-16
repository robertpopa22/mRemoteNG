using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class ConnectionsOptionsLayoutAcceptanceTests : UiAcceptanceTestBase
{
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    protected override void SeedSettings() => Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CheckForUpdatesAsked"] = "True",
        ["FollowOsTheme"] = "False",
        ["ThemeName"] = "vs2015Light",
    });

    [Test]
    [Issues("#233")]
    public void ResetPickerDoesNotOverlapConnectionOptionsAfterResize()
    {
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "connections-options-layout");
        Directory.CreateDirectory(evidence);
        MainWindow.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);
        var handle = new IntPtr(MainWindow.Properties.NativeWindowHandle.ValueOrDefault);
        SetWindowPos(handle, IntPtr.Zero, 0, 0, 1000, 720, 0x0040);
        UiWait.FindRequired(MainWindow, cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File")
            .Patterns.ExpandCollapse.Pattern.Expand();
        UiWait.FindRequired(Driver.Automation.GetDesktop(), cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)), "Options").Click();
        var pages = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("lstOptionPages"), "Options pages");
        UiWait.FindRequired(pages, cf => cf.ByName("Connections"), "Connections page").Patterns.SelectionItem.Pattern.Select();

        foreach (var size in new[] { new Size(1000, 720), new Size(800, 600), new Size(1000, 720) })
        {
            SetWindowPos(handle, IntPtr.Zero, 0, 0, size.Width, size.Height, 0x0040);
            UiWait.Settle(MainWindow);
            var button = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnResetResolver"), "Reset picker");
            string screenshot = Path.Combine(evidence, $"{size.Width}x{size.Height}.png");
            Capture.Element(MainWindow).ToFile(screenshot);
            TestContext.AddTestAttachment(screenshot);
            TestContext.Out.WriteLine($"size={size}; reset={button.BoundingRectangle}; offscreen={button.IsOffscreen}");
            Assert.That(button.IsOffscreen, Is.False, "Reset picker should be reachable at the tested size");
            var lastCheckbox = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("chkSlowClickRename"), "last checkbox");
            var firstNumber = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("numRdpReconnectionCount"), "RDP reconnect count");
            Assert.Multiple(() =>
            {
                Assert.That(button.BoundingRectangle.Top, Is.GreaterThanOrEqualTo(lastCheckbox.BoundingRectangle.Bottom),
                    $"Reset picker overlaps the checkbox list at {size}");
                Assert.That(button.BoundingRectangle.Bottom, Is.LessThanOrEqualTo(firstNumber.BoundingRectangle.Top),
                    $"Reset picker overlaps the numeric settings at {size}");
            });
        }
        UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnCancel"), "Cancel").Click();
        AssertExitedCleanly(CloseApplicationAndWaitForExit(), "after resizing Connections options");
    }
}

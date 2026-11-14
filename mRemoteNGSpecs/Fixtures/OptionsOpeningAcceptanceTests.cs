using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Versioning;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class OptionsOpeningAcceptanceTests : UiAcceptanceTestBase
{
    protected override void SeedSettings()
    {
        Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CheckForUpdatesAsked"] = "True",
        });
    }

    [Test]
    [Issues("#232")]
    public void OptionsCanBeOpenedCancelledAndReopened()
    {
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "options-opening");
        Directory.CreateDirectory(evidence);
        for (int opening = 1; opening <= 2; opening++)
        {
            UiWait.FindRequired(MainWindow,
                cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File menu")
                .Patterns.ExpandCollapse.Pattern.Expand();
            var item = UiWait.FindRequired(Driver.Automation.GetDesktop(),
                cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)), "Options menu item");
            var timer = Stopwatch.StartNew();
            item.Click();
            var cancel = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnCancel"), "Options Cancel button");
            UiWait.Until(() => !cancel.IsOffscreen && cancel.IsEnabled, "visible, enabled Options Cancel button");
            UiWait.Settle(MainWindow);
            TestContext.Out.WriteLine($"opening={opening} menu_click_to_cancel_button_ms={timer.ElapsedMilliseconds} (includes automation overhead)");
            string screenshot = Path.Combine(evidence, $"opening-{opening}.png");
            Capture.Element(MainWindow).ToFile(screenshot);
            TestContext.AddTestAttachment(screenshot);
            cancel.Click();
            UiWait.Settle(MainWindow);
            UiWait.Until(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancel")) is not { IsOffscreen: false },
                "Options hidden after Cancel");
        }

        AssertExitedCleanly(CloseApplicationAndWaitForExit(), "after opening and cancelling Options twice");
        string log = Deployment.ReadAppLog() ?? "";
        string logEvidence = Path.Combine(evidence, "application.log");
        File.WriteAllText(logEvidence, log);
        TestContext.AddTestAttachment(logEvidence);
        Assert.That(log, Does.Contain("event=process_stop"));
        Assert.That(log, Does.Contain("stage=FormCreate page=none phase=begin"));
        Assert.That(log, Does.Contain("stage=Reload page=none phase=leave"));
        Assert.That(log, Does.Contain("stage=PageSettings page=AdvancedPage phase=leave"));
        Assert.That(log, Does.Contain("stage=WindowFont page=none phase=leave"));
    }
}

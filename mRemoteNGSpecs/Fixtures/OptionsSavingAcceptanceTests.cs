using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

/// <summary>
/// #232: the Options window builds a page only when it is first selected. Saving must still be
/// right for every page: OK writes what was edited, Cancel throws an edit away, and a page that
/// was never opened keeps its stored values — its controls never existed, so nothing of theirs
/// may be written over the settings file.
/// </summary>
[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class OptionsSavingAcceptanceTests : UiAcceptanceTestBase
{
    protected override void SeedSettings()
    {
        Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CheckForUpdatesAsked"] = "True",
            ["AlwaysShowPanelTabs"] = "False",
            ["IdentifyQuickConnectTabs"] = "False",
            // Advanced page, never opened in this scenario; 7 is not the default (2).
            ["MaxPuttyWaitTime"] = "7",
            // Theme page, never opened: its SaveSettings sets this to True unconditionally, so it
            // stays False only if a page that was prebuilt but never opened does not save.
            ["ThemingActive"] = "False",
        });
    }

    private void OpenOptions()
    {
        UiWait.FindRequired(MainWindow,
            cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File menu")
            .Patterns.ExpandCollapse.Pattern.Expand();
        UiWait.FindRequired(Driver.Automation.GetDesktop(),
            cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)), "Options menu item").Click();
        AutomationElement cancel = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnCancel"), "Options Cancel button");
        UiWait.Until(() => !cancel.IsOffscreen && cancel.IsEnabled, "visible, enabled Options Cancel button");
        UiWait.Settle(MainWindow);
    }

    private void SelectPage(string name)
    {
        AutomationElement pages = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("lstOptionPages"), "options page list");
        UiWait.FindRequired(pages, cf => cf.ByName(name), $"'{name}' page entry").Patterns.SelectionItem.Pattern.Select();
        UiWait.Settle(MainWindow);
    }

    private void Toggle(string automationId, ToggleState wanted)
    {
        AutomationElement box = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId(automationId), automationId);
        if (box.Patterns.Toggle.Pattern.ToggleState.Value != wanted)
            box.Patterns.Toggle.Pattern.Toggle();
        UiWait.Settle(MainWindow);
    }

    private void Press(string automationId)
    {
        UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId(automationId), automationId).AsButton().Invoke();
        UiWait.Settle(MainWindow);
        UiWait.Until(() => MainWindow.FindFirstDescendant(cf => cf.ByAutomationId("btnCancel")) is not { IsOffscreen: false },
            "Options hidden");
    }

    private static string? Stored(string settings, string name)
    {
        Match match = Regex.Match(settings, $"<setting name=\"{Regex.Escape(name)}\"[^>]*>(?<v>[^<]*)</setting>");
        return match.Success ? match.Groups["v"].Value : null;
    }

    [Test]
    [Issues("#232")]
    public void OkSavesTheEditCancelDropsItAndAPageNeverOpenedKeepsItsValues()
    {
        // Let the idle prebuild of the Options pages finish first (it starts ~3 s after the main
        // window is shown), so the never-opened pages below were built ahead of time.
        UiWait.Happened(() => (Deployment.ReadAppLog() ?? "").Contains("stage=Prebuild page=ConfigurationPage phase=leave",
            StringComparison.Ordinal), TimeSpan.FromSeconds(30));
        OpenOptions();
        SelectPage("Tabs & Panels");
        Toggle("chkAlwaysShowPanelTabs", ToggleState.On);
        Press("btnOK");

        OpenOptions();
        SelectPage("Tabs & Panels");
        AutomationElement kept = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("chkAlwaysShowPanelTabs"), "kept checkbox");
        Assert.That(kept.Patterns.Toggle.Pattern.ToggleState.Value, Is.EqualTo(ToggleState.On),
            "after OK and reopen, the page should show the value that was saved");
        Toggle("chkIdentifyQuickConnectTabs", ToggleState.On);
        Press("btnCancel");

        OpenOptions();
        SelectPage("Tabs & Panels");
        AutomationElement dropped = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("chkIdentifyQuickConnectTabs"), "cancelled checkbox");
        Assert.That(dropped.Patterns.Toggle.Pattern.ToggleState.Value, Is.EqualTo(ToggleState.Off),
            "after Cancel and reopen, the page should show the stored value, not the discarded edit");
        Press("btnCancel");

        AssertExitedCleanly(CloseApplicationAndWaitForExit(), "after the Options save scenario");
        string settings = File.ReadAllText(Path.Combine(Deployment.SettingsPath, "mRemoteNG.settings"));
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "options-saving");
        Directory.CreateDirectory(evidence);
        File.WriteAllText(Path.Combine(evidence, "mRemoteNG.settings"), settings);
        File.WriteAllText(Path.Combine(evidence, "application.log"), Deployment.ReadAppLog() ?? "");

        Assert.Multiple(() =>
        {
            Assert.That(Stored(settings, "AlwaysShowPanelTabs"), Is.EqualTo("True"), "OK must save the edited value");
            Assert.That(Stored(settings, "IdentifyQuickConnectTabs"), Is.EqualTo("False"), "Cancel must not save the edit");
            Assert.That(Stored(settings, "MaxPuttyWaitTime"), Is.EqualTo("7"), "a page never opened must keep its stored value");
            Assert.That(Stored(settings, "ThemingActive"), Is.EqualTo("False"), "a prebuilt page never opened must not save");
        });
    }
}

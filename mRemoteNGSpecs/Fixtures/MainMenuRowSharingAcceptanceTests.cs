using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

/// <summary>
/// #253: a toolbar saved on the main menu's row must start on that row, beside the menu, and the
/// menu must still show all six of its menus. Settings place the External Tools toolbar on the top
/// panel at the menu's row; the loader joins it there through the same row rules a drag uses.
/// </summary>
[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class MainMenuRowSharingAcceptanceTests : UiAcceptanceTestBase
{
    protected override void SeedSettings() => Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["CheckForUpdatesAsked"] = "True",
        ["MainMenuParentDock"] = "Top",
        ["MainMenuLocation"] = "3, 0",
        ["ExtAppsTBParentDock"] = "Top",
        ["ExtAppsTBLocation"] = "500, 0",
        ["ExtAppsTBVisible"] = "True",
        ["ViewMenuExternalTools"] = "True",
    });

    [Test]
    [Issues("#253")]
    public void ExternalToolsToolbarSavedBesideTheMenuStartsOnTheMenuRow()
    {
        AutomationElement menu = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("msMain"), "main menu");
        AutomationElement tools = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("_externalToolsToolStrip"), "External Tools toolbar");
        var menuBounds = menu.BoundingRectangle;
        var toolsBounds = tools.BoundingRectangle;
        string[] menus = menu.FindAllChildren(cf => cf.ByControlType(ControlType.MenuItem))
            .Where(item => !item.IsOffscreen).Select(item => item.Name).ToArray();
        TestContext.Out.WriteLine($"menu {menuBounds}, tools {toolsBounds}, menus [{string.Join(", ", menus)}]");

        Assert.Multiple(() =>
        {
            Assert.That(Math.Abs(toolsBounds.Top - menuBounds.Top), Is.LessThanOrEqualTo(2),
                "the External Tools toolbar was put on a row of its own instead of beside the menu");
            Assert.That(toolsBounds.Left, Is.GreaterThanOrEqualTo(menuBounds.Right - 2), "the toolbar overlaps the menu");
            Assert.That(menus, Is.SupersetOf(new[] { "File", "Sessions", "View", "Connections", "Tools", "Help" }),
                "a top-level menu is missing from the menu bar");
        });
    }
}

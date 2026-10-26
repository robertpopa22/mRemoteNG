using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Support;
using NUnit.Framework;
using Microsoft.Win32;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class VisualLayoutAuditAcceptanceTests : UiAcceptanceTestBase
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private object? _originalWindowsTheme;
    private bool _changedWindowsTheme;
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    private sealed class DpiScope : IDisposable
    {
        private readonly IntPtr _original = SetThreadDpiAwarenessContext(new IntPtr(-4));
        public void Dispose() => SetThreadDpiAwarenessContext(_original);
    }

    protected override void SeedSettings()
    {
        int windowsLight = (int)(TestContext.CurrentContext.Test.Arguments[0] ?? throw new InvalidOperationException("Missing Windows theme"));
        bool followWindows = (bool)(TestContext.CurrentContext.Test.Arguments[1] ?? throw new InvalidOperationException("Missing follow preference"));
        double fontSize = (double)(TestContext.CurrentContext.Test.Arguments[2] ?? throw new InvalidOperationException("Missing font size"));
        using var key = Registry.CurrentUser.CreateSubKey(PersonalizeKey);
        _originalWindowsTheme = key.GetValue("AppsUseLightTheme");
        key.SetValue("AppsUseLightTheme", windowsLight, RegistryValueKind.DWord);
        _changedWindowsTheme = true;
        Deployment.WriteConnectionsFile(new ConnectionsSeeder().Add("Synthetic connection", "127.0.0.1", mRemoteNG.Connection.Protocol.ProtocolType.RDP, 1).Build());
        var settings = new Dictionary<string, string>(StringComparer.Ordinal) { ["CheckForUpdatesAsked"] = "True", ["UIFontSize"] = fontSize.ToString(System.Globalization.CultureInfo.InvariantCulture) };
        if (!followWindows)
        {
            settings["FollowOsTheme"] = "False";
            settings["ThemeName"] = "vs2015Light";
        }
        Deployment.WriteSettings(settings);
    }

    [TearDown]
    public void RestoreLabWindowsTheme()
    {
        if (!_changedWindowsTheme) return;
        using var key = Registry.CurrentUser.CreateSubKey(PersonalizeKey);
        if (_originalWindowsTheme is null) key.DeleteValue("AppsUseLightTheme", false);
        else key.SetValue("AppsUseLightTheme", _originalWindowsTheme, RegistryValueKind.DWord);
        _changedWindowsTheme = false;
    }

    [TestCase(1, true, 8.25)]
    [TestCase(0, true, 8.25)]
    [TestCase(0, false, 8.25)]
    [TestCase(0, true, 14.0)]
    [TestCase(0, true, 24.0)]
    public void CaptureEveryOptionsPageAtSmallAndLargeWindowSizes(int windowsLight, bool followWindows, double fontSize)
    {
        using var dpiScope = new DpiScope();
        string directory = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "VisualLayoutAudit", $"windows-{windowsLight}-follow-{followWindows}-font-{fontSize}");
        Directory.CreateDirectory(directory);
        var observations = new List<string>();
        var picker = UiWait.FindOptional(MainWindow, cf => cf.ByName("mRemoteNG — choose connections file"), TimeSpan.FromSeconds(2));
        if (picker is not null)
        {
            Capture.Element(picker).ToFile(Path.Combine(directory, "00-startup-picker.png"));
            UiWait.FindRequired(picker, cf => cf.ByName("Portable (exe\\Settings)").And(cf.ByControlType(ControlType.ListItem)), "synthetic profile")
                .Patterns.SelectionItem.Pattern.Select();
            UiWait.FindRequired(picker, cf => cf.ByName("Load").And(cf.ByControlType(ControlType.Button)), "Load synthetic profile").AsButton().Invoke();
            UiWait.Until(() => MainWindow.IsEnabled, "main window after profile selection");
        }
        MainWindow.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Normal);
        var hwnd = new IntPtr(MainWindow.Properties.NativeWindowHandle.ValueOrDefault);
        observations.Add($"Window DPI={GetDpiForWindow(hwnd)}; font={fontSize}; Windows app theme={windowsLight}; follow={followWindows}");
        SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 1000, 720, 0x0040);
        UiWait.Settle(MainWindow);
        Capture.Element(MainWindow).ToFile(Path.Combine(directory, "00-main.png"));
        UiWait.FindRequired(MainWindow, cf => cf.ByName("File").And(cf.ByControlType(ControlType.MenuItem)), "File menu")
            .Patterns.ExpandCollapse.Pattern.Expand();
        UiWait.FindRequired(Driver.Automation.GetDesktop(), cf => cf.ByName("Options...").And(cf.ByControlType(ControlType.MenuItem)), "Options").Click();
        UiWait.Settle(MainWindow);
        string[] pages = ["Startup/Exit", "Appearance", "Connections", "Tabs & Panels", "Notifications", "Credentials",
            "SQL Server", "Updates", "Theme", "Security", "Advanced", "Backup", "Config"];
        int sequence = 1;
        foreach (var size in new[] { new Size(1000, 720), new Size(800, 600) })
        {
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, size.Width, size.Height, 0x0040);
            UiWait.Settle(MainWindow);
            AutomationElement list = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("lstOptionPages"), "Options pages");
            foreach (string page in pages)
            {
                UiWait.FindRequired(list, cf => cf.ByName(page), page).Patterns.SelectionItem.Pattern.Select();
                UiWait.Settle(MainWindow);
                string file = $"{sequence++:00}-{size.Width}x{size.Height}-{page.Replace('/', '-').Replace('&', '-').Replace(' ', '-')}.png";
                Capture.Element(MainWindow).ToFile(Path.Combine(directory, file));
                var apply = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("btnApply"), "Apply button");
                observations.Add($"{file}: window={MainWindow.BoundingRectangle}; Apply={apply.BoundingRectangle}; offscreen={apply.IsOffscreen}");
                Assert.That(MainWindow.BoundingRectangle.Contains(apply.BoundingRectangle), Is.True, $"Apply remains reachable: {file}");
                if (string.Equals(page, "Theme", StringComparison.Ordinal))
                {
                    var checkbox = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("chkFollowOsTheme"), "Follow Windows theme");
                    var header = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("tableLayoutPanel1"), "Theme header");
                    observations.Add($"Theme checkbox={checkbox.BoundingRectangle}; header={header.BoundingRectangle}; visible height={Rectangle.Intersect(checkbox.BoundingRectangle, header.BoundingRectangle).Height}");
                    Assert.That(header.BoundingRectangle.Contains(checkbox.BoundingRectangle), Is.True,
                        $"Windows theme checkbox {checkbox.BoundingRectangle} fits header {header.BoundingRectangle}");
                    Assert.That(checkbox.AsCheckBox().IsChecked, Is.EqualTo(followWindows));
                    string expected = followWindows ? (windowsLight == 0 ? "vs2015darkNG" : "vs2015lightNG") : "vs2015Light";
                    var theme = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("cboTheme"), "selected theme");
                    Assert.That(theme.Patterns.Value.Pattern.Value.Value, Is.EqualTo(expected));
                    apply.AsButton().Invoke();
                    var settings = System.Xml.Linq.XDocument.Load(Path.Combine(Deployment.SettingsPath, "mRemoteNG.settings"));
                    string? savedManualTheme = settings.Descendants("setting").FirstOrDefault(s => string.Equals((string?)s.Attribute("name"), "ThemeName", StringComparison.Ordinal))?.Value;
                    Assert.That(savedManualTheme, Is.EqualTo("vs2015Light"), "following Windows preserves the manual choice when applying Options");
                }
            }
        }
        File.WriteAllLines(Path.Combine(directory, "observations.txt"), observations);
        AssertNoCrash("after capturing all Options pages");
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class RdpInputDiagnosticsAcceptanceTests : UiAcceptanceTestBase
{
    private const string ConnectionName = "lab-input-diagnostics";
    private const string EventPrefix = "event=rdp_input ";
    private string? _rdpSession;

    protected override void SeedSettings()
    {
        Deployment.WriteConnectionsFile(new ConnectionsSeeder().Add(ConnectionName, LabTargets.LinuxHost,
            ProtocolType.RDP, LabTargets.Rdp, LabTargets.LinuxUser, LabTargets.LinuxPassword).Build());
        Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ConfirmCloseConnection"] = "1",
            ["KeepTabsOpenAfterDisconnect"] = "False",
        });
    }

    private string[] InputEvents() => (Deployment.ReadAppLog() ?? string.Empty).Split('\n')
        .Where(line => line.Contains(EventPrefix, StringComparison.Ordinal))
        .Select(line => line[line.IndexOf(EventPrefix, StringComparison.Ordinal)..].TrimEnd('\r'))
        .Where(line => _rdpSession == null || line.Contains(" rdp_session=" + _rdpSession + " ", StringComparison.Ordinal))
        .ToArray();

    private void RequireLiveSession()
    {
        string[] phases = (Deployment.ReadAppLog() ?? string.Empty).Split('\n')
            .Where(line => line.Contains("event=rdp_phase ", StringComparison.Ordinal) &&
                line.Contains(" rdp_session=" + _rdpSession + " ", StringComparison.Ordinal)).ToArray();
        Assert.That(phases.Any(line => line.Contains(" phase=connected ", StringComparison.Ordinal)), Is.True,
            "no connected RDP transport observed for the diagnostic session");
        Assert.That(phases.Any(line => line.Contains(" phase=disconnected ", StringComparison.Ordinal)), Is.False,
            "the target disconnected before the diagnostic interaction");
    }

    private static bool IsSafeInputEvent(string line) => Regex.IsMatch(line,
        @"^event=rdp_input rdp_session=[a-f0-9]{12} reason=(lifecycle|managed_focus|managed_blur|control_state|focus_requested|focus_returned|sample|fullscreen)" +
        @" phase=(initialized|connecting|connected|logged_in|reconnecting|disconnected|closing) elapsed_ms=[0-9]{1,19} suppressed=[0-9]{1,10}" +
        @" thread_focus=(unavailable|none|host|native_child|other) foreground=(none|application|other|unavailable) thread_activation=(unavailable|none|host_root|other)" +
        @" control_ready=(true|false) visible=(true|false) enabled=(true|false) managed_focused=(true|false) managed_contains_focus=(true|false)" +
        @" current_tab=(true|false) view_only=(true|false) redirect_keys=(true|false) refocus_disabled=(true|false)$",
        RegexOptions.NonBacktracking | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture);

    private static bool HasNativeFocus(string line) =>
        line.Contains(" thread_focus=host ", StringComparison.Ordinal) ||
        line.Contains(" thread_focus=native_child ", StringComparison.Ordinal);

    private static bool HasRenderedSurface(AutomationElement surface)
    {
        Rectangle bounds = surface.BoundingRectangle;
        if (surface.IsOffscreen || bounds.Width < 100 || bounds.Height < 100) return false;
        using Bitmap bitmap = new(bounds.Width, bounds.Height);
        using (Graphics graphics = Graphics.FromImage(bitmap))
            graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        HashSet<int> colors = [];
        // Only a count is retained; no pixels or remote-screen content enter the evidence.
        for (int y = bounds.Height / 10; y < bounds.Height * 9 / 10; y += Math.Max(1, bounds.Height / 30))
            for (int x = bounds.Width / 10; x < bounds.Width * 9 / 10; x += Math.Max(1, bounds.Width / 40))
                colors.Add(bitmap.GetPixel(x, y).ToArgb());
        return colors.Count >= 8;
    }

    [Test]
    [Touches("#223", "#143")]
    public void AConnectedRdpSurfaceReportsNativeFocusAndLocalSearchHandoff()
    {
        Assert.That(LabTargets.IsReachable(LabTargets.LinuxHost, LabTargets.Rdp), Is.True,
            "isolated Linux RDP target unavailable; no connected-session check was performed");
        Assert.That(LabTargets.LinuxPassword, Is.Not.Empty, "isolated RDP credential unavailable");
        AutomationElement tree = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
        AutomationElement row = tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
            .First(element => element.Name.Contains(ConnectionName, StringComparison.Ordinal));
        Win32Mouse.DoubleClick(row);
        AnswerExpectedPrompts(TimeSpan.FromSeconds(20));
        UiWait.Until(() => InputEvents().Any(line => line.Contains(" phase=connected ", StringComparison.Ordinal)),
            "the diagnostic connected lifecycle event", TimeSpan.FromSeconds(60));
        _rdpSession = Regex.Match(InputEvents().First(line => line.Contains(" phase=connected ", StringComparison.Ordinal)),
            @"\brdp_session=(?<session>[a-f0-9]{12})\b",
            RegexOptions.NonBacktracking | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture).Groups["session"].Value;
        Assert.That(_rdpSession, Has.Length.EqualTo(12));
        AnswerExpectedPrompts(TimeSpan.FromSeconds(5));
        AutomationElement surface = UiWait.FindRequired(MainWindow, cf => cf.ByClassName("UIMainClass"), "RDP native surface");
        UiWait.Until(() => HasRenderedSurface(surface), "a visible nonuniform rendered RDP surface", TimeSpan.FromSeconds(45));
        RequireLiveSession();

        MainWindow.Focus();
        Win32Mouse.LeftClick(surface);
        // RDP may already own native focus; the later search/return checks require new events.
        UiWait.Until(() => InputEvents().Any(line => HasNativeFocus(line) &&
            line.Contains(" foreground=application ", StringComparison.Ordinal)), "native RDP focus diagnostic",
            TimeSpan.FromSeconds(40));

        int beforeSearch = InputEvents().Length;
        AutomationElement search = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("txtSearch"), "local search box");
        Win32Mouse.LeftClick(search);
        UiWait.Until(() => InputEvents().Skip(beforeSearch).Any(line => line.Contains(" thread_focus=other ", StringComparison.Ordinal)),
            "the diagnostic to observe focus moving off the RDP native surface", TimeSpan.FromSeconds(40));
        Assert.That(Win32Focus.FocusedWindow(), Is.EqualTo(search.Properties.NativeWindowHandle.Value),
            "the local search box did not retain native keyboard focus");

        int beforeReturn = InputEvents().Length;
        Win32Mouse.LeftClick(surface);
        UiWait.Until(() => InputEvents().Skip(beforeReturn).Any(HasNativeFocus),
            "the diagnostic to observe focus returning to RDP", TimeSpan.FromSeconds(40));
        RequireLiveSession();
        AssertNoCrash("after the connected RDP/local search focus handoff");
        bool exited = CloseApplicationAndWaitForExit(clickCloseButton: true);
        AssertExitedCleanly(exited, "after the diagnostic focus check");

        string[] events = InputEvents();
        Assert.That(events.All(IsSafeInputEvent), Is.True, "diagnostic output contains an unexpected field or value");
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "rdp-input-diagnostics");
        Directory.CreateDirectory(evidence);
        string path = Path.Combine(evidence, "input-events.txt");
        File.WriteAllLines(path, events);
        File.WriteAllText(Path.Combine(evidence, "scope.txt"),
            "connected_transport=true\nrendered_surface_nonuniform=true\nnative_focus_handoff=true\nremote_typing_verified=false\noriginal_failure_reproduced=false\n");
        TestContext.AddTestAttachment(path, "Structured RDP input diagnostics only");
        Assert.That(events.Any(line => line.Contains(" phase=closing ", StringComparison.Ordinal)), Is.True,
            "closing did not emit its diagnostic lifecycle snapshot");
    }
}

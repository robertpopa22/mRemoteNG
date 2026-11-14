using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.Linq;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

// A synthetic version marker check, not a substitute for the reporter's upstream file.
[TestFixture]
[NonParallelizable]
[SupportedOSPlatform("windows")]
public class ConnectionVersion29AcceptanceTests : UiAcceptanceTestBase
{
    protected override void SeedSettings()
    {
        var document = XDocument.Parse(new ConnectionsSeeder()
            .Add("synthetic-version-29", "127.0.0.1", ProtocolType.SSH2, 1).Build());
        document.Root!.SetAttributeValue("ConfVersion", "2.9");
        // ToString omits the declaration; without it the legacy loader asks for a password.
        Assert.That(document.Declaration, Is.Not.Null, "the production serializer's XML declaration must be retained");
        Deployment.WriteConnectionsFile(document.Declaration + Environment.NewLine + document);
        Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal) { ["CheckForUpdatesAsked"] = "True" });
    }

    [Test]
    [Issues("#231")]
    public void Version29MarkerLoadsTheSyntheticConnectionIntoTheTree()
    {
        var tree = UiWait.FindRequired(MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
        UiWait.Until(() => tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
            .Any(row => string.Equals(row.Name, "synthetic-version-29", StringComparison.Ordinal)),
            "synthetic version 2.9 connection visible");
        string evidence = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "version-29");
        Directory.CreateDirectory(evidence);
        string screenshot = Path.Combine(evidence, "loaded.png");
        Capture.Element(MainWindow).ToFile(screenshot);
        TestContext.AddTestAttachment(screenshot);
        TestContext.Out.WriteLine("Synthetic connection with ConfVersion=2.9 appeared in the tree; no upstream-specific field round-trip is claimed.");
        AssertExitedCleanly(CloseApplicationAndWaitForExit(), "after loading the synthetic version 2.9 file");
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNG.Connection.Protocol;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// An inheritance change reaches the connections file while the application runs
    /// (upstream #3570).
    ///
    /// With "save after every edit" on, which is the default, every edit of a connection is
    /// written at once. The inheritance settings raised no change notification, so a change made
    /// there stayed in memory until something else caused a save.
    ///
    /// The scenario uses a folder's context menu, Inheritance, "Apply default inheritance" (the
    /// submenu is shown for folders only): it copies the default inheritance, seeded here with
    /// the user name inherited, onto the folder. The file is read while the application is still running, so a save on exit
    /// cannot hide a missing save on edit.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class InheritanceChangeIsSavedAcceptanceTests : UiAcceptanceTestBase
    {
        private const string FolderName = "lab-inherit-folder";

        private static readonly Regex SeededNodeType =
            new("(?<head><Node Name=\"" + FolderName + "\"[^>]*?Type=\")Connection\"", RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(5));

        private static readonly Regex InheritUsernameOn =
            new("InheritUsername=\"true\"", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(5));

        protected override void SeedSettings()
        {
            // The seeder writes connections only; one of them is turned into a folder in the file.
            string xml = new ConnectionsSeeder().AddUnreachable("lab-inherit", 1)
                .Add(FolderName, "", ProtocolType.SSH2, 22).Build();
            string withFolder = SeededNodeType.Replace(xml, "${head}Container\"");
            Assert.That(withFolder, Is.Not.EqualTo(xml), "the seeded node was not turned into a folder");
            Deployment.WriteConnectionsFile(withFolder);

            Deployment.WriteSettings(new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["InhDefaultUsername"] = "True",
                ["CheckForUpdatesAsked"] = "True",
            });
        }

        private string ConnectionsFile() => Path.Combine(Deployment.SettingsPath, "confCons.xml");

        private string ReadConnectionsFile()
        {
            using FileStream stream = new(ConnectionsFile(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using StreamReader reader = new(stream);
            return reader.ReadToEnd();
        }

        private AutomationElement Row()
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            return tree.FindAllDescendants(cf => cf.ByControlType(ControlType.ListItem))
                       .First(e =>
                       {
                           try { return (e.Name ?? "").Contains(FolderName, StringComparison.OrdinalIgnoreCase); }
                           catch (Exception) { return false; }
                       });
        }

        private AutomationElement PopupItem(string name) =>
            UiWait.FindRequired(Driver.Automation.GetDesktop(),
                                cf => cf.ByName(name).And(cf.ByControlType(ControlType.MenuItem)), name);

        [Test]
        [Touches("upstream#3570")]
        public void ApplyDefaultInheritanceIsWrittenToTheFileWithoutAnExit()
        {
            UiWait.Until(() => File.Exists(ConnectionsFile()), "the connections file", TimeSpan.FromSeconds(15));
            Assert.That(InheritUsernameOn.IsMatch(ReadConnectionsFile()), Is.False,
                        "the seed already inherits the user name, so the scenario would prove nothing");
            DateTime writtenBefore = File.GetLastWriteTimeUtc(ConnectionsFile());

            Win32Mouse.LeftClick(Row());
            UiWait.Settle(MainWindow);
            Win32Mouse.RightClick(Row());
            UiWait.Settle(MainWindow);
            PopupItem("Inheritance").Patterns.ExpandCollapse.Pattern.Expand();
            PopupItem("Apply default inheritance").Click();
            UiWait.Settle(MainWindow);
            AnswerExpectedPrompts(TimeSpan.FromSeconds(2));

            bool saved = UiWait.Happened(() => InheritUsernameOn.IsMatch(ReadConnectionsFile()), TimeSpan.FromSeconds(20));
            TestContext.Out.WriteLine(
                $"file written before the click: {writtenBefore:O}; now: {File.GetLastWriteTimeUtc(ConnectionsFile()):O}; "
                + $"InheritUsername=true in the file: {saved}");
            Assert.That(saved, Is.True,
                        "20 s after 'Apply default inheritance' the connections file still does not carry "
                        + "the inherited user name, with the application running and save-on-edit on");
            AssertNoCrash("after applying the default inheritance");
        }
    }
}

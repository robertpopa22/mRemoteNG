using System;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Drivers;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// Startup: the application launches, shows a usable window, and loads its own assemblies.
    ///
    /// This area exists because the unit suite structurally cannot cover it. It exercises classes
    /// in-process against the build's own output directory, so an application that cannot start at
    /// all — a window created before it is ready, an assembly the copy step forgot, a static
    /// initialiser that throws — passes every one of its 6,666 tests.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    [NonParallelizable]
    public class StartupAcceptanceTests : UiAcceptanceTestBase
    {
        /// <summary>
        /// Covers: #19 (no application window on startup), #122 and #150 (a required assembly
        /// missing from the shipped layout, which surfaces as a load failure at startup).
        ///
        /// Reaching a visible main window means the process got past assembly resolution, static
        /// initialisation and form construction.
        /// </summary>
        [Test]
        [Issues("#19", "#122", "#150")]
        public void ApplicationStartsAndShowsAUsableMainWindow()
        {
            Assert.Multiple(() =>
            {
                Assert.That(MainWindow, Is.Not.Null, "no main window was returned");
                Assert.That(MainWindow.Title, Does.Contain("mRemoteNG"),
                            "the main window is not mRemoteNG's");
                Assert.That(MainWindow.IsOffscreen, Is.False,
                            "the main window exists but is offscreen — this is the #19 symptom: "
                            + "the process runs with no window the user can reach");

                System.Drawing.Rectangle bounds = MainWindow.BoundingRectangle;
                Assert.That(bounds.Width, Is.GreaterThan(200), "main window has no usable width");
                Assert.That(bounds.Height, Is.GreaterThan(200), "main window has no usable height");
            });

            AssertNoCrash("immediately after startup");
        }

        /// <summary>
        /// Touches #131.
        ///
        /// MEASURED SCOPE: the #131 crash lives in the AD-import tree and the task dialog, whose
        /// image lists carried the removed BinaryFormatter payload. This test opens neither, so it
        /// would stay green if that fix were reverted. A type initialiser that throws takes down the
        /// first feature that touches it, not startup — covering it needs AD Import opened.
        /// </summary>
        [Test]
        [Touches("#131")]
        public void TheConnectionTreeAndConfigPanelAreBothPresent()
        {
            AutomationElement tree = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("ConnectionTree"), "connection tree");
            AutomationElement grid = UiWait.FindRequired(
                MainWindow, cf => cf.ByAutomationId("_pGrid"), "config property grid");

            Assert.Multiple(() =>
            {
                Assert.That(tree.IsOffscreen, Is.False, "the connection tree is not visible");
                Assert.That(grid.IsOffscreen, Is.False, "the config panel is not visible");
            });

            AssertNoCrash("after locating the tree and config panel");
        }

        /// <summary>
        /// A portable install must keep its state beside the executable. Covers the portable half
        /// of #129 and guards the isolation this whole battery depends on: if the app wrote to the
        /// user's real profile instead, every scenario would contaminate the next one and the
        /// maintainer's own connections.
        /// </summary>
        [Test]
        [Issues("#129")]
        public void PortableModeWritesItsSettingsBesideTheExecutable()
        {
            // Closing the app is what flushes settings to disk.
            Driver.Dispose();

            UiWait.Until(() => System.IO.Directory.EnumerateFiles(Deployment.SettingsPath).Any(),
                         $"the app to write something into {Deployment.SettingsPath}",
                         TimeSpan.FromSeconds(20));

            string[] written = System.IO.Directory.GetFiles(Deployment.SettingsPath)
                                                  .Select(System.IO.Path.GetFileName)
                                                  .ToArray()!;

            Assert.That(written, Is.Not.Empty,
                        "portable mode wrote nothing beside the executable — it is using a profile "
                        + "directory instead, which is the #129 confusion");
            TestContext.Out.WriteLine("settings written: " + string.Join(", ", written));
        }
    }
}

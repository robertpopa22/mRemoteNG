using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Tools;
using mRemoteNG.UI.Controls;
using mRemoteNG.UI.Window;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Window
{
    /// <summary>
    /// #179, third round: "I click Browse..., select the icon, click Open, but the icon path does
    /// not appear in the Icon Path field", and the path is not saved. Reported against the build
    /// that made the editor commit on every keystroke. This drives the real window the way the
    /// Browse button does — it sets the selected tool's IconPath on the model and relies on the
    /// selection collection's change notification to refresh the editor — and checks both what
    /// the field shows and what the model keeps afterwards.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class ExternalToolsWindowIconPathTests
    {
        private static readonly string IconOnDisk =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "shell32.dll");

        private static T Field<T>(ExternalToolsWindow window, string name) where T : class
        {
            FieldInfo? field = typeof(ExternalToolsWindow).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(field, Is.Not.Null, $"{name} not found on ExternalToolsWindow");
            T? value = field!.GetValue(window) as T;
            Assert.That(value, Is.Not.Null, $"{name} is null");
            return value!;
        }

        private static void Pump()
        {
            Application.DoEvents();
            Thread.Sleep(50);
            Application.DoEvents();
        }

        private static (ExternalToolsWindow Window, ExternalTool Tool) OpenWindowWithOneSelectedTool()
        {
            Runtime.ExternalToolsService.ExternalTools.Clear();
            ExternalTool tool = new("Web", @"C:\Program Files\PowerShell\7\pwsh.exe", "-NoProfile");
            Runtime.ExternalToolsService.ExternalTools.Add(tool);

            ExternalToolsWindow window = new();
            window.CreateControl();
            Pump();

            // Form.Load never fires for a form that is not shown, and Load is where the editor
            // wires commit-on-keystroke. Without this the second test passes on any build,
            // because nothing ever commits — which is not the application the reporter uses.
            MethodInfo? wireCommits = typeof(ExternalToolsWindow).GetMethod("CommitTextBoxEditsAsTheyAreTyped", BindingFlags.NonPublic | BindingFlags.Instance);
            wireCommits!.Invoke(window, null);

            MethodInfo? refresh = typeof(ExternalToolsWindow).GetMethod("UpdateToolsListObjView", BindingFlags.NonPublic | BindingFlags.Instance);
            refresh!.Invoke(window, null);
            Field<BrightIdeasSoftware.ObjectListView>(window, "ToolsListObjView").SelectedObject = tool;
            Pump();

            // The list raises SelectedIndexChanged only once it has a window handle, which a form
            // that is never shown may not have handed its children yet. Do what that handler does
            // so the test is about the editor, not about handle creation order.
            MethodInfo? select = typeof(ExternalToolsWindow).GetMethod("UpdateToolstipControls", BindingFlags.NonPublic | BindingFlags.Instance);
            select!.Invoke(window, null);
            Pump();

            var selected = Field<mRemoteNG.Tools.CustomCollections.FullyObservableCollection<ExternalTool>>(window, "_currentlySelectedExternalTools");
            Assert.That(selected.Count, Is.EqualTo(1), "precondition: the tool is the current selection");
            Assert.That(selected[0], Is.SameAs(tool), "precondition: the selection is the seeded tool");

            return (window, tool);
        }

        [Test]
        public void BrowsingForAnIconShowsThePathInTheField()
        {
            (ExternalToolsWindow window, ExternalTool tool) = OpenWindowWithOneSelectedTool();
            using (window)
            {
                MrngTextBox iconPathBox = Field<MrngTextBox>(window, "IconPathTextBox");
                Assert.That(iconPathBox.Text, Is.Empty, "precondition: no icon path yet");

                // Exactly what BrowseIconButton_Click does after the dialog returns.
                tool.IconPath = IconOnDisk;
                Pump();

                Assert.That(iconPathBox.Text, Is.EqualTo(IconOnDisk),
                            "the icon path chosen through Browse never reached the Icon Path field");
            }
        }

        [Test]
        public void BrowsingForAnIconKeepsThePathOnTheToolAfterTheEditorCommits()
        {
            (ExternalToolsWindow window, ExternalTool tool) = OpenWindowWithOneSelectedTool();
            using (window)
            {
                tool.IconPath = IconOnDisk;
                Pump();

                // The next thing a user does — click into a field to look, then leave it — commits
                // the whole editor to the model with nothing else changed. If the field never
                // showed the path, this writes the empty field back over it. (Changing some OTHER
                // field first would not reproduce it: that change refreshes the editor from the
                // model mid-commit and accidentally rescues the path — which is why the reporter,
                // who changes nothing, loses it.)
                MethodInfo? leaveField = typeof(ExternalToolsWindow).GetMethod("PropertyControl_ChangedOrLostFocus", BindingFlags.NonPublic | BindingFlags.Instance);
                leaveField!.Invoke(window, [Field<MrngTextBox>(window, "IconPathTextBox"), EventArgs.Empty]);
                Pump();

                Assert.That(tool.IconPath, Is.EqualTo(IconOnDisk),
                            "the icon path chosen through Browse was overwritten by the next editor commit");
            }
        }
    }
}

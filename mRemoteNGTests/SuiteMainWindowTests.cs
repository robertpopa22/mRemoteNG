using System;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.UI.Forms;
using NUnit.Framework;

namespace mRemoteNGTests;

/// <summary>
/// Pins <see cref="SuiteMainWindow"/>: one live main window on a thread that keeps pumping, whatever
/// the tests' own threads do with it.
/// </summary>
public class SuiteMainWindowTests
{
    private static void OnAThreadThatEnds(Action action)
    {
        Exception? caught = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
                Application.DoEvents();
            }
            catch (Exception ex)
            {
                caught = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.That(thread.Join(TimeSpan.FromSeconds(30)), Is.True, "the thread never finished");
        if (caught != null)
            throw caught;
    }

    [Test]
    public void TheMainWindowBelongsToTheSuitesThreadAndIsAlive()
    {
        Assert.Multiple(() =>
        {
            Assert.That(SuiteMainWindow.Thread?.IsAlive, Is.True);
            Assert.That(SuiteMainWindow.Describe(), Is.Empty);
        });
    }

    [Test]
    public void ATestThreadThatUsesTheMainWindowAndEndsLeavesItAlive()
    {
        // What the first ConnectionWindow test used to do: create the main window on its own pump
        // thread, which then ended and took the main window's windows with it.
        OnAThreadThatEnds(() => _ = FrmMain.Default.pnlDock.ActiveDocument);

        Assert.That(SuiteMainWindow.Describe(), Is.Empty);
    }

    [Test]
    public void SettingTheSelectedConnectionFromAnotherThreadCompletes()
    {
        // The setter invokes onto the main window's thread; with the main window on a thread that
        // was not pumping, this waited until the test timed out.
        ConnectionInfo? before = FrmMain.Default.SelectedConnection;
        try
        {
            OnAThreadThatEnds(() => FrmMain.Default.SelectedConnection = new ConnectionInfo { Name = "suite main window" });
            Assert.That(FrmMain.Default.SelectedConnection?.Name, Is.EqualTo("suite main window"));
        }
        finally
        {
            OnAThreadThatEnds(() => FrmMain.Default.SelectedConnection = before);
        }
    }
}

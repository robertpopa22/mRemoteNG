using System;
using mRemoteNG.App;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Window;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGTests.UI.Window;

[NonParallelizable]
public class OptionsWindowOpeningTests
{
    [Test]
    public void OpeningOptionsAgainReusesTheDockedWindow()
    {
        var mainWindow = FrmMain.Default;
        mainWindow.Invoke((Action)(() =>
        {
            try
            {
                AppWindows.Show(WindowType.Options);
                OptionsWindow? window = AppWindows.OptionsFormWindow;
                Assert.That(window, Is.Not.Null);
                Assert.That(window!.DockPanel, Is.SameAs(mainWindow.pnlDock));
                DockState originalDockState = window.DockState;
                Assert.That(originalDockState, Is.Not.EqualTo(DockState.Hidden));

                window.DockHandler.Hide();
                Assert.That(window.DockState, Is.EqualTo(DockState.Hidden));

                AppWindows.ShowOptions(Language.Appearance);
                Assert.That(AppWindows.OptionsFormWindow, Is.SameAs(window));
                Assert.That(window.DockState, Is.EqualTo(originalDockState));
                Assert.That(window.DockPanel, Is.SameAs(mainWindow.pnlDock));
            }
            finally
            {
                AppWindows.OptionsFormWindow?.Dispose();
            }
        }));
    }
}

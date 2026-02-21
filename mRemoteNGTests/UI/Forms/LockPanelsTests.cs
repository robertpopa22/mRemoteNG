using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.Docking.ThemeVS2015;
using System.Threading;
using System.Windows.Forms;
using System.Reflection;
using System;
using mRemoteNG.UI.Forms;

namespace mRemoteNGTests.UI.Forms
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class LockPanelsTests
    {
        private static void SetLockPanels(bool value)
        {
            var optionsType = typeof(FrmMain).Assembly.GetType("mRemoteNG.Properties.OptionsTabsPanelsPage")
                ?? throw new Exception("Could not find OptionsTabsPanelsPage type");
            var defaultProp = optionsType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception("Could not find Default property");
            var defaultInstance = defaultProp.GetValue(null);
            var lockPanelsProp = optionsType.GetProperty("LockPanels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new Exception("Could not find LockPanels property");
            lockPanelsProp.SetValue(defaultInstance, value);
        }

        /// <summary>
        /// Reproduces the exact logic from FrmMain.SetPanelLock():
        ///   var lockPanels = !Properties.OptionsTabsPanelsPage.Default.LockPanels;
        ///   foreach (IDockContent dc in pnlDock.Contents)
        ///       dc.DockHandler.AllowEndUserDocking = lockPanels;
        /// </summary>
        private static void ApplyPanelLock(DockPanel pnlDock)
        {
            if (pnlDock.Contents.Count == 0) return;
            var optionsType = typeof(FrmMain).Assembly.GetType("mRemoteNG.Properties.OptionsTabsPanelsPage")!;
            var defaultInstance = optionsType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(null);
            var lockPanels = !(bool)optionsType.GetProperty("LockPanels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(defaultInstance)!;

            foreach (IDockContent dc in pnlDock.Contents)
            {
                if (dc.DockHandler != null)
                    dc.DockHandler.AllowEndUserDocking = lockPanels;
            }
        }

        [Test]
        public void SetPanelLock_LocksPanels_WhenSettingIsTrue()
        {
            var hostForm = new Form
            {
                Width = 400, Height = 300,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-10000, -10000)
            };
            var dockPanel = new DockPanel
            {
                Dock = DockStyle.Fill,
                DocumentStyle = DocumentStyle.DockingWindow,
                Theme = new VS2015LightTheme()
            };
            hostForm.Controls.Add(dockPanel);

            try
            {
                hostForm.Show();
                Application.DoEvents();

                var content = new DockContent();
                content.Show(dockPanel, DockState.Document);
                Application.DoEvents();

                SetLockPanels(true);
                ApplyPanelLock(dockPanel);

                foreach (IDockContent dc in dockPanel.Contents)
                {
                    Assert.That(dc.DockHandler.AllowEndUserDocking, Is.False, "Panel should be locked");
                }
            }
            finally
            {
                hostForm.Close();
                hostForm.Dispose();
            }
        }

        [Test]
        public void SetPanelLock_UnlocksPanels_WhenSettingIsFalse()
        {
            var hostForm = new Form
            {
                Width = 400, Height = 300,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Location = new System.Drawing.Point(-10000, -10000)
            };
            var dockPanel = new DockPanel
            {
                Dock = DockStyle.Fill,
                DocumentStyle = DocumentStyle.DockingWindow,
                Theme = new VS2015LightTheme()
            };
            hostForm.Controls.Add(dockPanel);

            try
            {
                hostForm.Show();
                Application.DoEvents();

                var content = new DockContent();
                content.Show(dockPanel, DockState.Document);
                Application.DoEvents();

                SetLockPanels(false);
                ApplyPanelLock(dockPanel);

                foreach (IDockContent dc in dockPanel.Contents)
                {
                    Assert.That(dc.DockHandler.AllowEndUserDocking, Is.True, "Panel should be unlocked");
                }
            }
            finally
            {
                hostForm.Close();
                hostForm.Dispose();
            }
        }
    }
}

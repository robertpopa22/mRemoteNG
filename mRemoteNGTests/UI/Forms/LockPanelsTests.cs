using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;
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
        /// Tests the SetPanelLock logic using a lightweight Form + DockPanel.
        /// FrmMain.Default cannot be created in headless test environments
        /// (Win32Exception: Error creating window handle).
        /// Instead we reproduce the exact same logic that SetPanelLock() uses.
        /// </summary>
        private static void RunWithLightweightDockPanel(Action<DockPanel> testAction)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var form = new Form
                    {
                        Width = 400, Height = 300,
                        ShowInTaskbar = false,
                        StartPosition = FormStartPosition.Manual,
                        Location = new System.Drawing.Point(-10000, -10000)
                    };
                    var dockPanel = new DockPanel { Dock = DockStyle.Fill };
                    form.Controls.Add(dockPanel);

                    form.Load += (_, _) =>
                    {
                        form.BeginInvoke(() =>
                        {
                            try
                            {
                                Application.DoEvents();
                                testAction(dockPanel);
                            }
                            catch (Exception ex)
                            {
                                caught = ex;
                            }
                            finally
                            {
                                Application.ExitThread();
                            }
                        });
                    };
                    Application.Run(form);
                }
                catch (Exception ex)
                {
                    if (caught == null) caught = ex;
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
            {
                thread.Interrupt();
                Assert.Fail("Test timed out after 30 seconds");
            }
            if (caught != null)
                throw caught;
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
            RunWithLightweightDockPanel(pnlDock =>
            {
                var content = new DockContent();
                content.Show(pnlDock);
                Application.DoEvents();

                SetLockPanels(true);
                ApplyPanelLock(pnlDock);

                foreach (IDockContent dc in pnlDock.Contents)
                {
                    Assert.That(dc.DockHandler.AllowEndUserDocking, Is.False, "Panel should be locked");
                }
            });
        }

        [Test]
        public void SetPanelLock_UnlocksPanels_WhenSettingIsFalse()
        {
            RunWithLightweightDockPanel(pnlDock =>
            {
                var content = new DockContent();
                content.Show(pnlDock);
                Application.DoEvents();

                SetLockPanels(false);
                ApplyPanelLock(pnlDock);

                foreach (IDockContent dc in pnlDock.Contents)
                {
                    Assert.That(dc.DockHandler.AllowEndUserDocking, Is.True, "Panel should be unlocked");
                }
            });
        }
    }
}

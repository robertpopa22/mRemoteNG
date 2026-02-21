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
        private static DockPanel GetPnlDock(FrmMain frm)
        {
            var field = typeof(FrmMain).GetField("pnlDock", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField)
                     ?? typeof(FrmMain).GetField("pnlDock", BindingFlags.Instance | BindingFlags.NonPublic);
            return (DockPanel)field!.GetValue(frm)!;
        }

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

        private static void RunWithMessagePump(Action<FrmMain> testAction)
        {
            Exception? caught = null;
            var thread = new Thread(() =>
            {
                try
                {
                    var frmMain = FrmMain.Default;
                    frmMain.Load += (_, _) =>
                    {
                        frmMain.BeginInvoke(() =>
                        {
                            try
                            {
                                Application.DoEvents();
                                testAction(frmMain);
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
                    Application.Run(frmMain);
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

        [Test]
        public void SetPanelLock_LocksPanels_WhenSettingIsTrue()
        {
            RunWithMessagePump(frmMain =>
            {
                var pnlDock = GetPnlDock(frmMain);
                if (pnlDock.Contents.Count == 0)
                {
                    var content = new DockContent();
                    content.Show(pnlDock);
                    Application.DoEvents();
                }

                SetLockPanels(true);
                frmMain.SetPanelLock();

                foreach (IDockContent content in pnlDock.Contents)
                {
                    Assert.That(content.DockHandler.AllowEndUserDocking, Is.False, "Panel should be locked");
                }
            });
        }

        [Test]
        public void SetPanelLock_UnlocksPanels_WhenSettingIsFalse()
        {
            RunWithMessagePump(frmMain =>
            {
                var pnlDock = GetPnlDock(frmMain);
                if (pnlDock.Contents.Count == 0)
                {
                    var content = new DockContent();
                    content.Show(pnlDock);
                    Application.DoEvents();
                }

                SetLockPanels(false);
                frmMain.SetPanelLock();

                foreach (IDockContent content in pnlDock.Contents)
                {
                    Assert.That(content.DockHandler.AllowEndUserDocking, Is.True, "Panel should be unlocked");
                }
            });
        }
    }
}

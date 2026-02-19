using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;
using System.Linq;
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
        private DockPanel GetPnlDock(FrmMain frm)
        {
            var field = typeof(FrmMain).GetField("pnlDock", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.GetField);
            if (field == null)
            {
                 field = typeof(FrmMain).GetField("pnlDock", BindingFlags.Instance | BindingFlags.NonPublic);
            }
            return (DockPanel)field.GetValue(frm);
        }

        private void SetLockPanels(bool value)
        {
            // Get the internal type mRemoteNG.Properties.OptionsTabsPanelsPage
            var optionsType = typeof(FrmMain).Assembly.GetType("mRemoteNG.Properties.OptionsTabsPanelsPage");
            if (optionsType == null) throw new Exception("Could not find OptionsTabsPanelsPage type");

            var defaultProp = optionsType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (defaultProp == null) throw new Exception("Could not find Default property");

            var defaultInstance = defaultProp.GetValue(null);
            
            var lockPanelsProp = optionsType.GetProperty("LockPanels", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (lockPanelsProp == null) throw new Exception("Could not find LockPanels property");

            lockPanelsProp.SetValue(defaultInstance, value);
        }

        [Test]
        public void SetPanelLock_LocksPanels_WhenSettingIsTrue()
        {
            // Arrange
            var frmMain = FrmMain.Default;
            var pnlDock = GetPnlDock(frmMain);

            // Ensure some content exists
            if (pnlDock.Contents.Count == 0)
            {
                 var content = new DockContent();
                 content.Show(pnlDock);
            }

            SetLockPanels(true);

            // Act
            frmMain.SetPanelLock();

            // Assert
            foreach (IDockContent content in pnlDock.Contents)
            {
                Assert.That(content.DockHandler.AllowEndUserDocking, Is.False, "Panel should be locked");
            }
        }

        [Test]
        public void SetPanelLock_UnlocksPanels_WhenSettingIsFalse()
        {
            // Arrange
            var frmMain = FrmMain.Default;
            var pnlDock = GetPnlDock(frmMain);

             if (pnlDock.Contents.Count == 0)
            {
                 var content = new DockContent();
                 content.Show(pnlDock);
            }

            SetLockPanels(false);

            // Act
            frmMain.SetPanelLock();

            // Assert
            foreach (IDockContent content in pnlDock.Contents)
            {
                Assert.That(content.DockHandler.AllowEndUserDocking, Is.True, "Panel should be unlocked");
            }
        }
    }
}

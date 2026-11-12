using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Properties;
using mRemoteNG.Tree;
using mRemoteNG.UI.Controls.ConnectionTree;
using mRemoteNG.UI.Window;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;


namespace mRemoteNGTests.UI.Window
{
    public class ConnectionTreeWindowTests
    {
        /// <summary>
        /// Runs the given action on a dedicated STA thread with a WinForms message pump.
        /// Required because ConnectionTreeWindow contains a ConnectionTree (ObjectListView)
        /// which forces native Win32 handle creation that deadlocks without a message pump.
        /// </summary>
        private static void RunWithMessagePump(Action testAction)
        {
            Exception caught = null;
            var thread = new Thread(() =>
            {
                var form = new Form
                {
                    Width = 400, Height = 300,
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new System.Drawing.Point(-10000, -10000)
                };
                form.Load += (s, e) =>
                {
                    try
                    {
                        testAction();
                    }
                    catch (Exception ex)
                    {
                        caught = ex;
                    }
                    finally
                    {
                        form.Close();
                    }
                };
                Application.Run(form);
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            if (!thread.Join(TimeSpan.FromSeconds(30)))
            {
                thread.Interrupt();
                Assert.Fail("Test timed out after 30 seconds (message pump deadlock)");
            }
            if (caught != null)
                throw caught;
        }

        [Test]
        public void CanCreateWindow() => RunWithMessagePump(() =>
        {
            var connectionTreeWindow = new ConnectionTreeWindow(new DockContent());
            connectionTreeWindow.Show();
            Application.DoEvents();
            Assert.That(connectionTreeWindow.IsHandleCreated, Is.True);
            connectionTreeWindow.Close();
        });

        [Test]
        public void ConnectionTreeEnablesMultiSelection() => RunWithMessagePump(() =>
        {
            var connectionTreeWindow = new ConnectionTreeWindow(new DockContent());
            connectionTreeWindow.Show();
            Application.DoEvents();
            Assert.That(connectionTreeWindow.ConnectionTree.MultiSelect, Is.True);
            connectionTreeWindow.Close();
        });

        [Test]
        public void PreviousSessionsAreReopenedWhenAutomaticReconnectIsUnticked()
        {
            // OptionsAdvancedPage.NoReconnect is the "Automatically try to reconnect when disconnected
            // from server" checkbox, saved inverted; it used to turn off reopening the previous
            // sessions (upstream #3178).
            List<IConnectionTreeDelegate> actions = PostSetupActions(reopenPreviousSessions: true, savedNoReconnect: true, noReconnectSwitch: false);

            Assert.That(actions, Has.Some.InstanceOf<PreviousSessionOpener>());
        }

        [Test]
        public void NoReconnectSwitchSkipsThePreviousSessions()
        {
            List<IConnectionTreeDelegate> actions = PostSetupActions(reopenPreviousSessions: true, savedNoReconnect: false, noReconnectSwitch: true);

            Assert.That(actions, Has.None.InstanceOf<PreviousSessionOpener>());
        }

        [Test]
        public void PreviousSessionsStayClosedWhenReconnectAtStartupIsOff()
        {
            List<IConnectionTreeDelegate> actions = PostSetupActions(reopenPreviousSessions: false, savedNoReconnect: false, noReconnectSwitch: false);

            Assert.That(actions, Has.None.InstanceOf<PreviousSessionOpener>());
        }

        private static List<IConnectionTreeDelegate> PostSetupActions(bool reopenPreviousSessions, bool savedNoReconnect, bool noReconnectSwitch)
        {
            bool originalReopenPreviousSessions = OptionsStartupExitPage.Default.OpenConsFromLastSession;
            bool originalNoReconnect = OptionsAdvancedPage.Default.NoReconnect;
            bool originalNoReconnectSwitch = Runtime.NoReconnectToPreviousSessions;
            List<IConnectionTreeDelegate> actions = [];
            try
            {
                OptionsStartupExitPage.Default.OpenConsFromLastSession = reopenPreviousSessions;
                OptionsAdvancedPage.Default.NoReconnect = savedNoReconnect;
                Runtime.NoReconnectToPreviousSessions = noReconnectSwitch;

                RunWithMessagePump(() =>
                {
                    var connectionTreeWindow = new ConnectionTreeWindow(new DockContent());
                    actions = connectionTreeWindow.ConnectionTree.PostSetupActions.ToList();
                    connectionTreeWindow.Close();
                });
            }
            finally
            {
                OptionsStartupExitPage.Default.OpenConsFromLastSession = originalReopenPreviousSessions;
                OptionsAdvancedPage.Default.NoReconnect = originalNoReconnect;
                Runtime.NoReconnectToPreviousSessions = originalNoReconnectSwitch;
            }

            return actions;
        }

        [Test]
        public void ClearButtonVisibilityTests() => RunWithMessagePump(() =>
        {
            var connectionTreeWindow = new ConnectionTreeWindow(new DockContent());
            connectionTreeWindow.Show();
            Application.DoEvents();

            var pbClear = connectionTreeWindow.Controls.Find("pbClearSearch", true)[0];
            var txtSearch = connectionTreeWindow.Controls.Find("txtSearch", true)[0];

            // Initially hidden
            Assert.That(pbClear.Visible, Is.False, "Clear button should be hidden initially");

            // Set text
            txtSearch.Text = "foo";
            Application.DoEvents();
            
            // Should be visible
            Assert.That(pbClear.Visible, Is.True, "Clear button should be visible when text is present");

            // Clear text
            txtSearch.Text = "";
            Application.DoEvents();
            Assert.That(pbClear.Visible, Is.False, "Clear button should be hidden when text is empty");
            
            connectionTreeWindow.Close();
        });
    }
}

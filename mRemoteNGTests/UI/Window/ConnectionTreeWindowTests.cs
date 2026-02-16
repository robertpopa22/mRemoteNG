using System;
using System.Threading;
using System.Windows.Forms;
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
    }
}

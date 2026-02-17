using System;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsSQLPageTests : OptionsFormSetupAndTeardown
    {
        /// <summary>
        /// Runs the given action on a dedicated STA thread with a WinForms message pump.
        /// Required because selecting pages in FrmOptions triggers ObjectListView handle
        /// creation that deadlocks without an active message pump.
        /// </summary>
        private static void RunWithMessagePump(Action<FrmOptions> testAction)
        {
            Exception caught = null;
            var thread = new Thread(() =>
            {
                FrmOptions optionsForm = null;
                try
                {
                    optionsForm = new FrmOptions();
                    optionsForm.Load += (s, e) =>
                    {
                        // Defer test action via BeginInvoke so Load completes first
                        // and the message pump can process ObjectListView initialization.
                        optionsForm.BeginInvoke(() =>
                        {
                            try
                            {
                                Application.DoEvents();
                                testAction(optionsForm);
                            }
                            catch (Exception ex)
                            {
                                caught = ex;
                            }
                            finally
                            {
                                // Force-exit the message loop. Don't call Close()
                                // because FrmOptions.FormClosing shows MessageBox
                                // when HasChanges is true.
                                Application.ExitThread();
                            }
                        });
                    };
                    Application.Run(optionsForm);
                }
                catch (Exception ex)
                {
                    if (caught == null) caught = ex;
                }
                finally
                {
                    try { optionsForm?.Dispose(); } catch { }
                }
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
        public void SQLPageLinkExistsInListView()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items[6].Text, Does.Match("SQL Server"));
        }

        [Test]
        public void SQLIconShownInListView()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items[6].ImageList, Is.Not.Null);
        }

        [Test]
        public void SelectingSQLPageLoadsSettings() => RunWithMessagePump(optionsForm =>
        {
            Application.DoEvents();
            ListViewTester listViewTester = new("lstOptionPages", optionsForm);
            listViewTester.Select("SQL Server");
            Application.DoEvents();
            CheckBox checkboxTester = optionsForm.FindControl<CheckBox>("chkUseSQLServer");
            Assert.That(checkboxTester, Is.Not.Null, "chkUseSQLServer checkbox should exist on the SQL Server options page");
            Assert.That(checkboxTester.Text, Does.Match("Use SQL"));
        });
    }
}

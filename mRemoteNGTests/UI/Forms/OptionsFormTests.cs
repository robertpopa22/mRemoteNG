using NUnit.Framework;
using System;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms;
using mRemoteNGTests.TestHelpers;
using System.Linq;

namespace mRemoteNGTests.UI.Forms
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsFormTests : OptionsFormSetupAndTeardown
    {
        /// <summary>
        /// Runs the given action on a dedicated STA thread with a WinForms message pump.
        /// Required because FrmOptions uses ObjectListView which forces native Win32
        /// handle creation that deadlocks without an active message pump.
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
        public void ClickingCloseButtonClosesTheForm()
        {
            Button cancelButton = _optionsForm.FindControl<Button>("btnCancel");
            cancelButton.PerformClick();
            Assert.That(_optionsForm.Visible, Is.False);
        }

        [Test]
        public void ClickingOKButtonSetsDialogResult()
        {
            Button cancelButton = _optionsForm.FindControl<Button>("btnOK");
            cancelButton.PerformClick();
            Assert.That(_optionsForm.DialogResult, Is.EqualTo(DialogResult.OK));
        }

        [Test]
        public void ListViewContainsOptionsPages()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items.Count, Is.EqualTo(13));
        }

        [Test]
        public void ChangingOptionMarksPageAsChanged() => RunWithMessagePump(optionsForm =>
        {
            Application.DoEvents();

            var pnlMain = optionsForm.FindControl<Panel>("pnlMain");
            Assert.That(pnlMain, Is.Not.Null, "pnlMain should exist on the options form");
            Assert.That(pnlMain.Controls.Count, Is.GreaterThan(0), "pnlMain should have at least one options page");

            var optionsPage = pnlMain.Controls[0] as mRemoteNG.UI.Forms.OptionsPages.OptionsPage;
            Assert.That(optionsPage, Is.Not.Null, "First control in pnlMain should be an OptionsPage");

            var checkBoxes = optionsPage.GetAllControls().OfType<CheckBox>().ToList();
            Assert.That(checkBoxes.Count, Is.GreaterThan(0), "Options page should have at least one checkbox");

            var checkBox = checkBoxes[0];
            bool originalValue = checkBox.Checked;
            checkBox.Checked = !originalValue;
            Application.DoEvents();

            Assert.That(optionsPage.HasChanges, Is.True, "Toggling a checkbox should mark the page as having changes");
        });

        [Test]
        public void ControlsAreCreatedAfterFormInitialization()
        {
            Application.DoEvents();
            var pnlMain = _optionsForm.FindControl<Panel>("pnlMain");
            Assert.That(pnlMain, Is.Not.Null, "pnlMain should exist");
            Assert.That(pnlMain.Controls.Count, Is.GreaterThan(0), "pnlMain should have child controls");
        }

        [Test]
        public void OptionsFormHasValidSelectedPage()
        {
            Application.DoEvents();
            var lstOptionPages = _optionsForm.GetType()
                .GetField("lstOptionPages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(_optionsForm);

            Assert.That(lstOptionPages, Is.Not.Null, "lstOptionPages should exist");

            var selectedObject = lstOptionPages.GetType()
                .GetProperty("SelectedObject")
                ?.GetValue(lstOptionPages);

            Assert.That(selectedObject, Is.Not.Null, "SelectedObject should not be null");
        }

        [Test]
        public void ControlHandlesAreCreatedProperly()
        {
            Application.DoEvents();
            var pnlMain = _optionsForm.FindControl<Panel>("pnlMain");
            Assert.That(pnlMain.Controls.Count, Is.GreaterThan(0));

            var firstPage = pnlMain.Controls[0];
            Assert.That(firstPage.IsDisposed, Is.False, "Page should not be disposed");
        }

        [Test]
        public void FormConstructionDoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                using var form = new FrmOptions();
                form.Show();
                Application.DoEvents();
                form.Close();
            });
        }
    }
}

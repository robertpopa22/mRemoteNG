using NUnit.Framework;
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
            Assert.That(listViewTester.Items.Count, Is.EqualTo(12));
        }

        [Test]
        [Ignore("Options page initialization triggers ObjectListView handle creation that deadlocks without Application.Run message pump. Needs OptionsForm refactoring to use RunWithMessagePump pattern.")]
        public void ChangingOptionMarksPageAsChanged()
        {
            Application.DoEvents();

            var pnlMain = _optionsForm.FindControl<Panel>("pnlMain");
            Assert.That(pnlMain, Is.Not.Null);

            if (pnlMain.Controls.Count > 0)
            {
                var optionsPage = pnlMain.Controls[0] as mRemoteNG.UI.Forms.OptionsPages.OptionsPage;
                Assert.That(optionsPage, Is.Not.Null);

                var checkBoxes = optionsPage.GetAllControls().OfType<CheckBox>().ToList();

                if (checkBoxes.Count > 0)
                {
                    var checkBox = checkBoxes[0];
                    bool originalValue = checkBox.Checked;
                    checkBox.Checked = !originalValue;
                    Application.DoEvents();

                    Assert.That(optionsPage.HasChanges, Is.True);
                }
            }
        }

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

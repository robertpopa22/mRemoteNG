using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;
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
            bool eventFired = false;
            _optionsForm.FormClosed += (o, e) => eventFired = true;
            Button cancelButton = _optionsForm.FindControl<Button>("btnCancel");
            cancelButton.PerformClick();
            Assert.That(eventFired, Is.True);
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
        public void ChangingOptionMarksPageAsChanged()
        {
            // Wait for all pages to load
            System.Threading.Thread.Sleep(500);
            Application.DoEvents();

            // Get the options panel
            var pnlMain = _optionsForm.FindControl<Panel>("pnlMain");
            Assert.That(pnlMain, Is.Not.Null);

            if (pnlMain.Controls.Count > 0)
            {
                var optionsPage = pnlMain.Controls[0] as mRemoteNG.UI.Forms.OptionsPages.OptionsPage;
                Assert.That(optionsPage, Is.Not.Null);

                // Find a checkbox in the options page
                var checkBoxes = optionsPage.Controls.Find("", true).OfType<CheckBox>().ToList();
                
                if (checkBoxes.Count > 0)
                {
                    var checkBox = checkBoxes[0];
                    bool originalValue = checkBox.Checked;
                    checkBox.Checked = !originalValue;
                    Application.DoEvents();
                    
                    // Verify the page is marked as changed
                    Assert.That(optionsPage.HasChanges, Is.True);
                }
            }
        }
    }
}
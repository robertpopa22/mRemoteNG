using NUnit.Framework;
using System.Threading;
using System.Windows.Forms;
using mRemoteNGTests.TestHelpers;

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
        public void FormCanBeHiddenAndShownMultipleTimes()
        {
            // First show (already done in Setup)
            Assert.That(_optionsForm.Visible, Is.True);
            
            // Hide the form
            _optionsForm.Hide();
            Assert.That(_optionsForm.Visible, Is.False);
            
            // Show it again
            _optionsForm.Show();
            Assert.That(_optionsForm.Visible, Is.True);
            
            // Verify pages are still loaded correctly
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            Assert.That(listViewTester.Items.Count, Is.EqualTo(12));
            
            // Hide and show one more time
            _optionsForm.Hide();
            _optionsForm.Show();
            Assert.That(_optionsForm.Visible, Is.True);
            
            // Verify pages are still there
            Assert.That(listViewTester.Items.Count, Is.EqualTo(12));
        }
    }
}
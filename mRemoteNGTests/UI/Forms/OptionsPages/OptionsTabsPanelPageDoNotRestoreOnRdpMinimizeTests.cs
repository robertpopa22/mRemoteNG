using System.Threading;
using System.Windows.Forms;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms.OptionsPages
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsTabsPanelPageDoNotRestoreOnRdpMinimizeTests : OptionsFormSetupAndTeardown
    {
        [Test]
        public void CheckboxExistsAndTextIsCorrect()
        {
            ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
            listViewTester.Select("Tabs & Panels");
            CheckBox checkboxTester = _optionsForm.FindControl<CheckBox>("chkDoNotRestoreOnRdpMinimize");
            Assert.That(checkboxTester, Is.Not.Null);
            Assert.That(checkboxTester.Text, Does.Match("Do not dock to tab when minimizing from Full screen"));
        }

        [Test]
        public void SettingIsLoaded()
        {
            // Arrange
            bool originalValue = mRemoteNG.Properties.OptionsTabsPanelsPage.Default.DoNotRestoreOnRdpMinimize;
            mRemoteNG.Properties.OptionsTabsPanelsPage.Default.DoNotRestoreOnRdpMinimize = true;

            try
            {
                // Act - reopen the form/page to reload settings
                Teardown();
                Setup();
                
                ListViewTester listViewTester = new("lstOptionPages", _optionsForm);
                listViewTester.Select("Tabs & Panels");
                CheckBox checkboxTester = _optionsForm.FindControl<CheckBox>("chkDoNotRestoreOnRdpMinimize");

                // Assert
                Assert.That(checkboxTester.Checked, Is.True);
            }
            finally
            {
                // Cleanup
                mRemoteNG.Properties.OptionsTabsPanelsPage.Default.DoNotRestoreOnRdpMinimize = originalValue;
            }
        }
    }
}

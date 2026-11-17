using NUnit.Framework;
using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.UI.Forms;
using mRemoteNGTests.TestHelpers;
using System.Linq;

namespace mRemoteNGTests.UI.Forms
{
    /// <summary>
    /// Tests for FrmOptions. CRITICAL: Only ONE test allowed per fixture.
    /// FrmOptions + ObjectListView leaks native Win32 resources (GDI handles,
    /// window class registrations). Even 2 tests that touch FrmOptions in the
    /// same testhost process crash it. All assertions are in a single test.
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class OptionsFormTests
    {
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

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            if (caught != null)
                ExceptionDispatchInfo.Capture(caught).Throw();
        }

        /// <summary>
        /// Single test that validates FrmOptions construction, controls, ListView descriptors,
        /// lazy page building on selection, Cancel revert, and OK saving only built &amp; loaded pages.
        /// </summary>
        [Test]
        public void FormBehavior() => RunWithMessagePump(optionsForm =>
        {
            string origCulture = mRemoteNG.Properties.Settings.Default.OverrideUICulture;
            bool origDisableRefocus = mRemoteNG.Properties.OptionsStartupExitPage.Default.DisableRefocus;
            bool origBoldActive = mRemoteNG.Properties.OptionsAppearancePage.Default.BoldActiveConnections;
            try
            {
                mRemoteNG.Properties.Settings.Default.OverrideUICulture = string.Empty;

                // 1. Controls are created
                var pnlMain = optionsForm.FindControl<Panel>("pnlMain");
                Assert.That(pnlMain, Is.Not.Null, "pnlMain should exist");

                // 2. ListView has all 13 options page descriptors, while only 1 page is built at open
                ListViewTester listViewTester = new("lstOptionPages", optionsForm);
                Assert.That(listViewTester.Items.Count, Is.EqualTo(13));
                Assert.That(optionsForm.PageLifecycle.Descriptors.Count, Is.EqualTo(13));
                Assert.That(optionsForm.PageLifecycle.BuiltCount, Is.EqualTo(1), "Only the initial selected page should be built on open");

                // 3. SelectedObject is an OptionsPageDescriptor backed by the built CurrentPage
                var lstOptionPages = optionsForm.GetType()
                    .GetField("lstOptionPages", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(optionsForm);
                Assert.That(lstOptionPages, Is.Not.Null, "lstOptionPages should exist");
                var selectedObject = lstOptionPages.GetType()
                    .GetProperty("SelectedObject")
                    ?.GetValue(lstOptionPages) as mRemoteNG.UI.Forms.OptionsPages.OptionsPageDescriptor;
                Assert.That(selectedObject, Is.Not.Null, "SelectedObject should be an OptionsPageDescriptor");
                Assert.That(selectedObject.IsBuilt, Is.True);
                Assert.That(selectedObject.IsLoaded, Is.True);

                var startupPage = optionsForm.CurrentPage;
                Assert.That(startupPage, Is.Not.Null, "CurrentPage should be an OptionsPage");
                Assert.That(startupPage.IsDisposed, Is.False, "Page should not be disposed");

                // 4. Open/activate AppearancePage on demand -> builds second page only
                optionsForm.SetActivatedPage(mRemoteNG.Resources.Language.Language.Appearance);
                Application.DoEvents();
                Assert.That(optionsForm.PageLifecycle.BuiltCount, Is.EqualTo(2), "Selecting Appearance should build only the second page");
                var appearancePage = optionsForm.CurrentPage as mRemoteNG.UI.Forms.OptionsPages.AppearancePage;
                Assert.That(appearancePage, Is.Not.Null, "CurrentPage should now be AppearancePage");
                Assert.That(appearancePage.cboLanguage.Items.Count, Is.EqualTo(1), "Language combo should be seeded with active item only before drop-down");

                // 5. Edit AppearancePage, then Cancel -> reverts control state
                var boldCheckBox = appearancePage.chkBoldActiveConnections;
                boldCheckBox.Checked = !origBoldActive;
                Application.DoEvents();
                Assert.That(appearancePage.HasChanges, Is.True, "Toggling a checkbox should mark the page as having changes");

                Button cancelButton = optionsForm.FindControl<Button>("btnCancel");
                cancelButton.PerformClick();
                Application.DoEvents();
                Assert.That(boldCheckBox.Checked, Is.EqualTo(origBoldActive), "Cancel should revert edited control to persisted value");
                Assert.That(optionsForm.HasUnsavedChanges(), Is.False, "Cancel should clear unsaved change flags");

                // 6. Reopen flow: edit StartupExit + Appearance, click OK -> raises CloseRequested and leaves other 11 pages unbuilt
                optionsForm.SetActivatedPage(mRemoteNG.Resources.Language.Language.StartupExit);
                optionsForm.ReloadAllSettings();
                Application.DoEvents();
                var disableRefocusCheckBox = optionsForm.FindControl<CheckBox>("chkDisableRefocus");
                disableRefocusCheckBox.Checked = !origDisableRefocus;

                optionsForm.SetActivatedPage(mRemoteNG.Resources.Language.Language.Appearance);
                Application.DoEvents();
                boldCheckBox.Checked = !origBoldActive;

                bool closeRequested = false;
                optionsForm.CloseRequested += (_, _) => closeRequested = true;
                Button okButton = optionsForm.FindControl<Button>("btnOK");
                okButton.PerformClick();

                Assert.That(closeRequested, Is.True, "OK button should raise CloseRequested event");
                Assert.That(mRemoteNG.Properties.OptionsStartupExitPage.Default.DisableRefocus, Is.EqualTo(!origDisableRefocus));
                Assert.That(mRemoteNG.Properties.OptionsAppearancePage.Default.BoldActiveConnections, Is.EqualTo(!origBoldActive));
                Assert.That(optionsForm.PageLifecycle.BuiltCount, Is.EqualTo(2), "OK should not build unopened pages");
            }
            finally
            {
                mRemoteNG.Properties.Settings.Default.OverrideUICulture = origCulture;
                mRemoteNG.Properties.OptionsStartupExitPage.Default.DisableRefocus = origDisableRefocus;
                mRemoteNG.Properties.OptionsAppearancePage.Default.BoldActiveConnections = origBoldActive;
            }
        });
    }
}

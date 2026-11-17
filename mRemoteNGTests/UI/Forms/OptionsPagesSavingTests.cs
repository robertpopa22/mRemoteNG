using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using log4net.Appender;
using mRemoteNG.App;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using mRemoteNG.UI.Forms.OptionsPages;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Forms
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    [NonParallelizable]
    public class OptionsPagesSavingTests
    {
        private sealed class FakeSettingStore
        {
            public string Value { get; set; } = string.Empty;
        }

        private sealed class RecordingOptionsPage : OptionsPage
        {
            private readonly FakeSettingStore _store;
            private readonly Action<string>? _onSaveSideEffect;
            private readonly Action? _onRevertSideEffect;
            private string? _livePreviewBeforeEdit;
            private bool _previewActive;

            public TextBox Editor { get; } = new();

            public int LoadCount { get; private set; }

            public int SaveCount { get; private set; }

            public int RevertCount { get; private set; }

            public int RegistryLoadCount { get; private set; }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Action<string>? LivePreviewSink { get; init; }

            [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
            public Func<string>? LivePreviewReader { get; init; }

            public RecordingOptionsPage(
                string pageName,
                FakeSettingStore store,
                Action<string>? onSaveSideEffect = null,
                Action? onRevertSideEffect = null)
            {
                PageName = pageName;
                _store = store;
                _onSaveSideEffect = onSaveSideEffect;
                _onRevertSideEffect = onRevertSideEffect;
                Controls.Add(Editor);
                Editor.TextChanged += (_, _) =>
                {
                    if (!IsLoadingSettings)
                    {
                        HasChanges = true;
                    }
                };
            }

            public void ApplyLivePreview(string previewValue)
            {
                if (!_previewActive && LivePreviewReader != null)
                {
                    _livePreviewBeforeEdit = LivePreviewReader();
                }

                _previewActive = true;
                Editor.Text = previewValue;
                LivePreviewSink?.Invoke(previewValue);
            }

            public override void LoadRegistrySettings()
            {
                RegistryLoadCount++;
            }

            public override void LoadSettings()
            {
                LoadCount++;
                Editor.Text = _store.Value;
                if (!_previewActive && LivePreviewReader != null)
                {
                    _livePreviewBeforeEdit = LivePreviewReader();
                }
            }

            public override void SaveSettings()
            {
                SaveCount++;
                _store.Value = Editor.Text;
                _previewActive = false;
                _livePreviewBeforeEdit = _store.Value;
                _onSaveSideEffect?.Invoke(_store.Value);
            }

            public override void RevertSettings()
            {
                RevertCount++;
                if (_previewActive)
                {
                    _previewActive = false;
                    if (_livePreviewBeforeEdit != null)
                    {
                        LivePreviewSink?.Invoke(_livePreviewBeforeEdit);
                    }
                }

                _onRevertSideEffect?.Invoke();
            }
        }

        private static OptionsPageDescriptor CreateRecordingDescriptor(
            string pageKey,
            string pageName,
            FakeSettingStore store,
            Action? onConstruct = null,
            Action<string>? onSaveSideEffect = null,
            Action<string>? livePreviewSink = null,
            Func<string>? livePreviewReader = null)
        {
            return new OptionsPageDescriptor(
                pageKey,
                pageName,
                static () => null,
                () =>
                {
                    onConstruct?.Invoke();
                    return new RecordingOptionsPage(pageName, store, onSaveSideEffect)
                    {
                        LivePreviewSink = livePreviewSink,
                        LivePreviewReader = livePreviewReader
                    };
                });
        }

        [Test]
        public void PageNeverOpened_ThenOk_DoesNotBuildOrSaveUnopenedPages_AndKeepsPolicyValueAndSpans()
        {
            var startupStore = new FakeSettingStore { Value = "initial-startup" };
            var policyStore = new FakeSettingStore { Value = "policy-enforced-at-startup" };
            int unopenedConstructed = 0;

            var logger = (log4net.Repository.Hierarchy.Logger)Logger.Instance.Log.Logger;
            var capture = new MemoryAppender();
            logger.AddAppender(capture);

            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("StartupExitPage", "Startup/Exit", startupStore),
                CreateRecordingDescriptor("SecurityPage", "Security", policyStore, onConstruct: () => unopenedConstructed++)
            ]);

            try
            {
                var startupPage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[0]);
                Assert.That(lifecycle.BuiltCount, Is.EqualTo(1));
                Assert.That(unopenedConstructed, Is.EqualTo(0));
                Assert.That(lifecycle.Descriptors[1].IsBuilt, Is.False);
                Assert.That(lifecycle.Descriptors[1].IsLoaded, Is.False);

                int persistCalls = 0;
                lifecycle.SaveBuiltAndLoaded(() => persistCalls++);
                lifecycle.InvalidateInactiveBuiltPages();

                Assert.That(persistCalls, Is.EqualTo(1));
                Assert.That(startupPage.SaveCount, Is.EqualTo(1));
                Assert.That(unopenedConstructed, Is.EqualTo(0), "Unopened page must never be constructed on OK");
                Assert.That(policyStore.Value, Is.EqualTo("policy-enforced-at-startup"), "Policy value applied by RegistryLoader at startup must survive OK");

                string[] spanLines = capture.GetEvents()
                    .Select(e => e.RenderedMessage)
                    .Where(line => line.Contains("event=options_load", StringComparison.Ordinal))
                    .ToArray();
                Assert.That(spanLines.Any(l => l.Contains("stage=PageSettings", StringComparison.Ordinal) && l.Contains("page=StartupExitPage", StringComparison.Ordinal)), Is.True);
                Assert.That(spanLines.Any(l => l.Contains("page=SecurityPage", StringComparison.Ordinal)), Is.False);
            }
            finally
            {
                logger.RemoveAppender(capture);
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void PageOpenedEdited_ThenCancel_ThenReopened_RevertsControlAndNeverSavesDiscardedEdit()
        {
            var startupStore = new FakeSettingStore { Value = "saved-startup" };
            var appearanceStore = new FakeSettingStore { Value = "saved-appearance" };

            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("StartupExitPage", "Startup/Exit", startupStore),
                CreateRecordingDescriptor("AppearancePage", "Appearance", appearanceStore)
            ]);

            try
            {
                // Open Startup/Exit, switch to Appearance, edit Appearance, switch back to Startup/Exit, then Cancel
                var startupPage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[0]);
                var appearancePage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[1]);
                appearancePage.Editor.Text = "discarded-appearance-edit";
                Assert.That(lifecycle.HasUnsavedChanges(), Is.True);

                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll(); // Cancel

                Assert.That(lifecycle.HasUnsavedChanges(), Is.False);
                Assert.That(appearancePage.RevertCount, Is.EqualTo(1));
                Assert.That(lifecycle.Descriptors[1].IsLoaded, Is.False, "Inactive cancelled page must not be marked loaded until revisited");
                Assert.That(lifecycle.Descriptors[1].NeedsReload, Is.True);

                // Reopen on Startup/Exit and click OK without visiting Appearance -> discarded edit must NOT save
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                startupPage.Editor.Text = "new-startup";
                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();

                Assert.That(startupStore.Value, Is.EqualTo("new-startup"));
                Assert.That(appearancePage.SaveCount, Is.EqualTo(0), "Cancelled inactive page must not save on subsequent OK");
                Assert.That(appearanceStore.Value, Is.EqualTo("saved-appearance"));

                // Now select Appearance -> reloads persisted value into control
                lifecycle.Activate(lifecycle.Descriptors[1]);
                Assert.That(appearancePage.Editor.Text, Is.EqualTo("saved-appearance"));
                Assert.That(lifecycle.Descriptors[1].IsLoaded, Is.True);
                Assert.That(lifecycle.Descriptors[1].NeedsReload, Is.False);
            }
            finally
            {
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void PageOpenedEdited_ThenOk_SavesEditedValue()
        {
            var connectionsStore = new FakeSettingStore { Value = "before" };
            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("ConnectionsPage", "Connections", connectionsStore)
            ]);

            try
            {
                var page = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[0]);
                page.Editor.Text = "after";
                Assert.That(lifecycle.HasUnsavedChanges(), Is.True);

                int persistCalls = 0;
                lifecycle.SaveBuiltAndLoaded(() => persistCalls++);
                lifecycle.InvalidateInactiveBuiltPages();

                Assert.That(persistCalls, Is.EqualTo(1));
                Assert.That(connectionsStore.Value, Is.EqualTo("after"));
                Assert.That(page.SaveCount, Is.EqualTo(1));
                Assert.That(lifecycle.HasUnsavedChanges(), Is.False);
            }
            finally
            {
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void TwoPagesEdited_ThenOk_SavesBothBuiltAndLoadedPages_AndSkipsThirdUnbuiltPage()
        {
            var storeA = new FakeSettingStore { Value = "a0" };
            var storeB = new FakeSettingStore { Value = "b0" };
            var storeC = new FakeSettingStore { Value = "c0" };

            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("StartupExitPage", "Startup/Exit", storeA),
                CreateRecordingDescriptor("TabsPanelsPage", "Tabs & Panels", storeB),
                CreateRecordingDescriptor("NotificationsPage", "Notifications", storeC)
            ]);

            try
            {
                var pageA = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[0]);
                pageA.Editor.Text = "a1";

                var pageB = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[1]);
                pageB.Editor.Text = "b1";

                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();

                Assert.That(storeA.Value, Is.EqualTo("a1"));
                Assert.That(storeB.Value, Is.EqualTo("b1"));
                Assert.That(storeC.Value, Is.EqualTo("c0"));
                Assert.That(pageA.SaveCount, Is.EqualTo(1));
                Assert.That(pageB.SaveCount, Is.EqualTo(1));
                Assert.That(lifecycle.Descriptors[2].IsBuilt, Is.False);
            }
            finally
            {
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void SideEffectPages_RestartPromptAndLiveThemeSwitch_FireOnlyWhenVisitedInActiveSession()
        {
            var startupStore = new FakeSettingStore { Value = "startup" };
            var configStore = new FakeSettingStore { Value = @"C:\OldConfig" };
            var themeStore = new FakeSettingStore { Value = "vs2015Light" };
            string liveTheme = "vs2015Light";
            int restartNotifications = 0;

            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("StartupExitPage", "Startup/Exit", startupStore),
                CreateRecordingDescriptor("ConfigurationPage", "Configuration", configStore, onSaveSideEffect: _ => restartNotifications++),
                CreateRecordingDescriptor(
                    "ThemePage",
                    "Theme",
                    themeStore,
                    onSaveSideEffect: committed => liveTheme = committed,
                    livePreviewSink: preview => liveTheme = preview,
                    livePreviewReader: () => liveTheme)
            ]);

            try
            {
                // 1. Open only Startup/Exit and click OK -> neither Configuration restart prompt nor Theme save runs
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();
                Assert.That(restartNotifications, Is.EqualTo(0));
                Assert.That(liveTheme, Is.EqualTo("vs2015Light"));

                // 2. Reopen, preview a theme on ThemePage, switch away, then Cancel -> live theme reverts immediately
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                var themePage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[2]);
                themePage.ApplyLivePreview("vs2015Dark");
                Assert.That(liveTheme, Is.EqualTo("vs2015Dark"));

                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll(); // Cancel
                Assert.That(liveTheme, Is.EqualTo("vs2015Light"), "Cancel must revert live theme preview even when another page is active");
                Assert.That(themeStore.Value, Is.EqualTo("vs2015Light"));

                // 3. Reopen, edit ConfigurationPage & ThemePage, click OK -> both side effects run once
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                var configPage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[1]);
                configPage.Editor.Text = @"C:\NewConfig";
                lifecycle.Activate(lifecycle.Descriptors[2]);
                themePage.ApplyLivePreview("Darcula");

                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();
                Assert.That(restartNotifications, Is.EqualTo(1));
                Assert.That(liveTheme, Is.EqualTo("Darcula"));
                Assert.That(themeStore.Value, Is.EqualTo("Darcula"));

                // 4. Reopen on Startup/Exit only and click OK -> previously built ConfigurationPage does NOT re-fire restart prompt
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();
                Assert.That(restartNotifications, Is.EqualTo(1), "Previously built but unvisited page must not re-run SaveSettings in a later session");
            }
            finally
            {
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void SettingsChangedElsewhereWhileHidden_AreNotOverwrittenByStaleControls_AndAppearWhenPageSelected()
        {
            var startupStore = new FakeSettingStore { Value = "s0" };
            var themeStore = new FakeSettingStore { Value = "vs2015Light" };

            var lifecycle = new OptionsPageLifecycle(
            [
                CreateRecordingDescriptor("StartupExitPage", "Startup/Exit", startupStore),
                CreateRecordingDescriptor("ThemePage", "Theme", themeStore)
            ]);

            try
            {
                // Session 1: visit both pages, close via OK
                var startupPage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[0]);
                var themePage = (RecordingOptionsPage)lifecycle.Activate(lifecycle.Descriptors[1]);
                Assert.That(themePage.Editor.Text, Is.EqualTo("vs2015Light"));
                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();

                // While Options is hidden, another window / OS preference changes the theme setting
                themeStore.Value = "vs2015Dark";

                // Session 2: reopen on Startup/Exit (SetActivatedPage + RefreshSettings), edit Startup/Exit, click OK without visiting Theme
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                startupPage.Editor.Text = "s1";
                lifecycle.SaveBuiltAndLoaded();
                lifecycle.InvalidateInactiveBuiltPages();

                Assert.That(startupStore.Value, Is.EqualTo("s1"));
                Assert.That(themeStore.Value, Is.EqualTo("vs2015Dark"), "External theme change while hidden must not be overwritten by unvisited ThemePage");

                // Session 3: reopen and select ThemePage -> displays externally changed theme
                lifecycle.Activate(lifecycle.Descriptors[0]);
                lifecycle.ReloadAll();
                lifecycle.Activate(lifecycle.Descriptors[1]);
                Assert.That(themePage.Editor.Text, Is.EqualTo("vs2015Dark"));
            }
            finally
            {
                lifecycle.DisposeAll();
            }
        }

        [Test]
        public void AppearancePage_SeedsActiveLanguageOnlyUntilDropDown_AndRevertsPendingFontOnCancel()
        {
            string origCulture = Settings.Default.OverrideUICulture;
            string origFontFamily = OptionsAppearancePage.Default.UIFontFamily;
            float origFontSize = OptionsAppearancePage.Default.UIFontSize;
            int origFontStyle = OptionsAppearancePage.Default.UIFontStyle;
            bool origTrayIcon = OptionsAppearancePage.Default.ShowSystemTrayIcon;

            try
            {
                Settings.Default.OverrideUICulture = string.Empty;
                OptionsAppearancePage.Default.UIFontFamily = "Segoe UI";
                OptionsAppearancePage.Default.UIFontSize = 8.25f;
                OptionsAppearancePage.Default.UIFontStyle = (int)FontStyle.Regular;
                OptionsAppearancePage.Default.ShowSystemTrayIcon = false;

                using var page = new AppearancePage();
                page.ApplyLanguage();
                page.LoadSettings();

                // 1. Default OS culture seeds only LanguageDefault until DropDown opens
                Assert.That(page.cboLanguage.Items.Count, Is.EqualTo(1));
                Assert.That(page.cboLanguage.SelectedItem, Is.EqualTo(Language.LanguageDefault));
                page.SaveSettings();
                Assert.That(Settings.Default.OverrideUICulture, Is.Empty);

                // 2. Populating on DropDown loads all supported cultures without marking HasChanges
                page.HasChanges = false;
                page.EnsureLanguagesPopulated();
                Assert.That(page.cboLanguage.Items.Count, Is.GreaterThan(10));
                Assert.That(page.cboLanguage.SelectedItem, Is.EqualTo(Language.LanguageDefault));
                Assert.That(page.HasChanges, Is.False);

                // 3. Selecting a supported culture saves its culture code, and seeding with an active override works without full DropDown
                string frenchNative = SupportedCultures.GetCultureNativeName("fr");
                page.cboLanguage.SelectedItem = frenchNative;
                page.SaveSettings();
                Assert.That(Settings.Default.OverrideUICulture, Is.EqualTo("fr"));

                using var overriddenPage = new AppearancePage();
                overriddenPage.ApplyLanguage();
                overriddenPage.LoadSettings();
                Assert.That(overriddenPage.cboLanguage.Items.Count, Is.EqualTo(2));
                Assert.That(overriddenPage.cboLanguage.SelectedItem, Is.EqualTo(frenchNative));

                // 4. Pending interface font is reverted by RevertSettings + LoadSettings on Cancel
                page.SetPendingInterfaceFont("Consolas", 11f, FontStyle.Bold);
                Assert.That(page.HasChanges, Is.True);
                Assert.That(page.lblInterfaceFont.Text, Does.Contain("Consolas"));

                page.RevertSettings();
                page.LoadSettings();
                Assert.That(page.lblInterfaceFont.Text, Does.Contain("Segoe UI"));

                page.SaveSettings();
                Assert.That(OptionsAppearancePage.Default.UIFontFamily, Is.EqualTo("Segoe UI"), "Reverted pending font must not be written on subsequent SaveSettings");
            }
            finally
            {
                Settings.Default.OverrideUICulture = origCulture;
                OptionsAppearancePage.Default.UIFontFamily = origFontFamily;
                OptionsAppearancePage.Default.UIFontSize = origFontSize;
                OptionsAppearancePage.Default.UIFontStyle = origFontStyle;
                OptionsAppearancePage.Default.ShowSystemTrayIcon = origTrayIcon;
            }
        }
    }
}

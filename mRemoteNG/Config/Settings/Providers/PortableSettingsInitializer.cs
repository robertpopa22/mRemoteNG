using System.Configuration;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Settings.Providers
{
    /// <summary>
    /// Forces all Properties settings classes to use ChooseProvider (PortableSettingsProvider
    /// when PORTABLE is defined). This bypasses SettingsProviderAttribute which may fail
    /// silently in .NET Core/5+/10 due to AssemblyQualifiedName resolution issues.
    /// Must be called BEFORE any settings class .Default property is accessed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class PortableSettingsInitializer
    {
        private static bool _initialized;
        private static ChooseProvider? _sharedProvider;

        internal static void EnsureInitialized()
        {
            if (_initialized) return;
            _initialized = true;

            _sharedProvider = new ChooseProvider();
            _sharedProvider.Initialize(_sharedProvider.Name, null!);

            WireProvider(Properties.Settings.Default);
            WireProvider(Properties.App.Default);
            WireProvider(Properties.AppUI.Default);
            WireProvider(Properties.OptionsAdvancedPage.Default);
            WireProvider(Properties.OptionsAppearancePage.Default);
            WireProvider(Properties.OptionsBackupPage.Default);
            WireProvider(Properties.OptionsConnectionsPage.Default);
            WireProvider(Properties.OptionsCredentialsPage.Default);
            WireProvider(Properties.OptionsDBsPage.Default);
            WireProvider(Properties.OptionsGoogleDrivePage.Default);
            WireProvider(Properties.OptionsNotificationsPage.Default);
            WireProvider(Properties.OptionsRbac.Default);
            WireProvider(Properties.OptionsSecurityPage.Default);
            WireProvider(Properties.OptionsStartupExitPage.Default);
            WireProvider(Properties.OptionsTabsPanelsPage.Default);
            WireProvider(Properties.OptionsThemePage.Default);
            WireProvider(Properties.OptionsUpdatesPage.Default);
        }

        private static void WireProvider(ApplicationSettingsBase settings)
        {
            if (_sharedProvider == null) return;

            if (settings.Providers[_sharedProvider.Name] == null)
                settings.Providers.Add(_sharedProvider);

            foreach (SettingsProperty prop in settings.Properties)
            {
                prop.Provider = _sharedProvider;
            }
        }
    }
}

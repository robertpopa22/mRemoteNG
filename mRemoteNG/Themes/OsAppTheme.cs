using System;
using System.Drawing;
using System.IO;
using System.Runtime.Versioning;
using System.Security;
using Microsoft.Win32;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]
    internal static class OsAppTheme
    {
        public const string LightThemeName = "vs2015Light";
        public const string DarkThemeName = "vs2015Dark";

        public static bool PrefersLight(int? appsUseLightTheme) =>
            appsUseLightTheme is null or not 0;

        public static string ThemeName(bool prefersLight) =>
            prefersLight ? LightThemeName : DarkThemeName;

        public static bool CaptionIsDark(Color background)
        {
            if (background.IsEmpty)
                return false;
            double luminance = ((0.2126 * background.R) + (0.7152 * background.G) + (0.0722 * background.B)) / 255d;
            return luminance < 0.5;
        }

        public static bool CaptionIsDark(ThemeInfo? theme)
        {
            ThemeBase? dockTheme = theme?.Theme;
            if (dockTheme == null)
                return false;
            return CaptionIsDark(dockTheme.ColorPalette.ToolWindowTabSelectedInactive.Background);
        }

        public static bool ReadPrefersLight()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                object? value = key?.GetValue("AppsUseLightTheme");
                return value is int appsUseLightTheme ? PrefersLight(appsUseLightTheme) : true;
            }
            catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException or IOException)
            {
                return true;
            }
        }
    }
}

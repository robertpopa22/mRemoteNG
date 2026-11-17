using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using mRemoteNG.Properties;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.App
{
    [SupportedOSPlatform("windows")]
    public sealed class SupportedCultures : Dictionary<string, string>
    {
        // #232: building the list creates ~30 CultureInfo objects and reads their native names,
        // about 300 ms the first time (culture data, not JIT). Startup warms it on a pool thread;
        // Lazy makes a caller that arrives first wait for that one build instead of racing it.
        private static readonly Lazy<SupportedCultures> _instance =
            new(() => new SupportedCultures(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        private static SupportedCultures SingletonInstance => _instance.Value;

        /// <summary>Builds the list off the UI thread so the first Options open does not pay for it.</summary>
        public static void Warm()
        {
            try { _ = _instance.Value; }
            catch (Exception ex) { Debug.Print($"SupportedCultures warm-up failed: {ex.Message}"); }
        }


        // #232: the shipped languages are static. Creating each CultureInfo and reading its
        // NativeName loads that culture's ICU data, about 300 ms for the 23 of them on first use;
        // these strings are exactly what that code produced (TextInfo.ToTitleCase of NativeName).
        // A culture added to SupportedUICultures without a row here still works, at the old cost.
        private static readonly Dictionary<string, string> KnownNativeNames = new(StringComparer.Ordinal)
        {
            ["cs-CZ"] = "Čeština (Česko)",
            ["de"] = "Deutsch",
            ["el"] = "Ελληνικά",
            ["en"] = "English",
            ["en-US"] = "English (United States)",
            ["es-AR"] = "Español (Argentina)",
            ["es"] = "Español",
            ["fr"] = "Français",
            ["hu"] = "Magyar",
            ["it"] = "Italiano",
            ["lt"] = "Lietuvių",
            ["ja-JP"] = "日本語 (日本)",
            ["ko-KR"] = "한국어(대한민국)",
            ["nb-NO"] = "Norsk Bokmål (Norge)",
            ["nl"] = "Nederlands",
            ["pt"] = "Português",
            ["pt-BR"] = "Português (Brasil)",
            ["pl"] = "Polski",
            ["ru"] = "Русский",
            ["uk"] = "Українська",
            ["tr-TR"] = "Türkçe (Türkiye)",
            ["zh-CN"] = "中文（中国）",
            ["zh-TW"] = "中文（台灣）",
        };

        internal static IReadOnlyDictionary<string, string> KnownNativeNamesForTests => KnownNativeNames;

        private SupportedCultures()
        {
            foreach (string CultureName in Properties.AppUI.Default.SupportedUICultures.Split(','))
            {
                string name = CultureName.Trim();
                if (KnownNativeNames.TryGetValue(name, out string? nativeName))
                {
                    if (!ContainsKey(name)) Add(name, nativeName);
                    continue;
                }

                try
                {
                    CultureInfo CultureInfo = new(name);
                    Add(CultureInfo.Name, CultureInfo.TextInfo.ToTitleCase(CultureInfo.NativeName));
                }
                catch (Exception ex)
                {
                    Debug.Print(
                                $"An exception occurred while adding the culture {CultureName} to the list of supported cultures. {ex.StackTrace}");
                }
            }
        }

        public static bool IsNameSupported(string CultureName)
        {
            return SingletonInstance.ContainsKey(CultureName);
        }

        public static bool IsNativeNameSupported(string CultureNativeName)
        {
            return SingletonInstance.ContainsValue(CultureNativeName);
        }

        public static string GetCultureName(string CultureNativeName)
        {
            string[] Names = new string[SingletonInstance.Count + 1];
            string[] NativeNames = new string[SingletonInstance.Count + 1];

            SingletonInstance.Keys.CopyTo(Names, 0);
            SingletonInstance.Values.CopyTo(NativeNames, 0);

            for (int Index = 0; Index <= SingletonInstance.Count; Index++)
            {
                if (NativeNames[Index] == CultureNativeName)
                {
                    return Names[Index];
                }
            }

            throw (new KeyNotFoundException());
        }

        public static string GetCultureNativeName(string CultureName)
        {
            return SingletonInstance[CultureName];
        }

        public static IList<string> CultureNativeNames
        {
            get
            {
                List<string> ValueList = new();
                foreach (string Value in SingletonInstance.Values)
                {
                    ValueList.Add(Value);
                }

                return ValueList;
            }
        }
    }
}
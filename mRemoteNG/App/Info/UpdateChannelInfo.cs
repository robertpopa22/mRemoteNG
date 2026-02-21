using System;
using System.Runtime.Versioning;
using mRemoteNG.Properties;

// ReSharper disable InconsistentNaming

namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class UpdateChannelInfo
    {
        public const string STABLE = "Stable";
        public const string PREVIEW = "Preview";
        public const string NIGHTLY = "Nightly";
        public const string GITHUB = "GitHub";

        public const string STABLE_PORTABLE = "update-portable.txt";
        public const string PREVIEW_PORTABLE = "preview-update-portable.txt";
        public const string NIGHTLY_PORTABLE = "nightly-update-portable.txt";

        public const string STABLE_MSI = "update.txt";
        public const string PREVIEW_MSI = "preview-update.txt";
        public const string NIGHTLY_MSI = "nightly-update.txt";

        private const string GITHUB_API_URI = "https://api.github.com/repos/mRemoteNG/mRemoteNG/releases/latest";

        public static Uri GetUpdateChannelInfo()
        {
            string channel = IsValidChannel(Properties.OptionsUpdatesPage.Default.UpdateChannel)
                ? Properties.OptionsUpdatesPage.Default.UpdateChannel
                : GITHUB;

            if (channel == GITHUB)
                return new Uri(GITHUB_API_URI);

            return GetUpdateTxtUri(channel);
        }

        public static bool IsGitHubUri(Uri uri)
        {
            return uri.AbsoluteUri.Equals(GITHUB_API_URI, StringComparison.OrdinalIgnoreCase);
        }

        private static string GetChannelFileName(string channel)
        {
            return Runtime.IsPortableEdition
                ? GetChannelFileNamePortableEdition(channel)
                : GetChannelFileNameNormalEdition(channel);
        }

        private static string GetChannelFileNameNormalEdition(string channel)
        {
            switch (channel)
            {
                case STABLE:
                    return STABLE_MSI;
                case PREVIEW:
                    return PREVIEW_MSI;
                case NIGHTLY:
                    return NIGHTLY_MSI;
                default:
                    return STABLE_MSI;
            }
        }

        private static string GetChannelFileNamePortableEdition(string channel)
        {
            switch (channel)
            {
                case STABLE:
                    return STABLE_PORTABLE;
                case PREVIEW:
                    return PREVIEW_PORTABLE;
                case NIGHTLY:
                    return NIGHTLY_PORTABLE;
                default:
                    return STABLE_PORTABLE;
            }
        }

        private static Uri GetUpdateTxtUri(string channel)
        {
            return new Uri(new Uri(Properties.OptionsUpdatesPage.Default.UpdateAddress),
                           new Uri(GetChannelFileName(channel), UriKind.Relative));
        }

        private static bool IsValidChannel(string s)
        {
            return s.Equals(STABLE) || s.Equals(PREVIEW) || s.Equals(NIGHTLY) || s.Equals(GITHUB);
        }
    }
}
using System;
using System.Runtime.Versioning;

// ReSharper disable InconsistentNaming

namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class UpdateChannelInfo
    {
        // Channel names are retained because registry policy (OptRegistryUpdatesPage) and the
        // options UI still reference them, but this fork resolves every channel to GitHub.
        public const string STABLE = "Stable";
        public const string PREVIEW = "Preview";
        public const string NIGHTLY = "Nightly";
        public const string GITHUB = "GitHub";

        private const string GITHUB_API_URI = "https://api.github.com/repos/robertpopa22/mRemoteNG/releases/latest";

        // This fork distributes exclusively via GitHub Releases, so every update check resolves
        // to the GitHub releases API. The legacy Stable/Preview/Nightly text-file channels pointed
        // at the upstream website (mremoteng.org) — which serves upstream's own (ancient) update.txt
        // and has no preview/nightly files for this fork — so they reported wrong versions or failed.
        public static Uri GetUpdateChannelInfo()
        {
            return new Uri(GITHUB_API_URI);
        }

        public static bool IsGitHubUri(Uri uri)
        {
            return uri.AbsoluteUri.Equals(GITHUB_API_URI, StringComparison.OrdinalIgnoreCase);
        }
    }
}

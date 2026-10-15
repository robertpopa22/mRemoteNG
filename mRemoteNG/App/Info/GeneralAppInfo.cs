using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using static System.Environment;


namespace mRemoteNG.App.Info
{
    [SupportedOSPlatform("windows")]
    public static class GeneralAppInfo
    {
        public const string UrlHome = "https://mremoteng.org";
        // The upstream donate URL (mremoteng.org/contribute) 404s. Donations for this fork go to
        // the maintainer's non-profit — the Geseidl Association for Sport and Health.
        public const string UrlDonate = "https://geseidl.ro/en/donate";
        public const string UrlForum = "https://github.com/orgs/mRemoteNG/discussions";
        public const string UrlChat = "https://app.element.io/#/room/#mremoteng:matrix.org";
        public const string UrlCommunity = "https://www.reddit.com/r/mRemoteNG";
        public const string UrlBugs = "https://github.com/robertpopa22/mRemoteNG/issues/new";
        public const string CrashReportOwner = "robertpopa22";
        public const string CrashReportRepo = "mRemoteNG";
        public const string UrlDocumentation = "https://mremoteng.readthedocs.io/en/latest/";

        // Fork-specific URLs
        public const string ForkOwner = "robertpopa22";
        public const string ForkRepo = "mRemoteNG";
        public const string UrlForkHome = "https://github.com/robertpopa22/mRemoteNG";
        public const string UrlForkStars = "https://github.com/robertpopa22/mRemoteNG/stargazers";
        public const string UrlForkReleases = "https://github.com/robertpopa22/mRemoteNG/releases";
        public const string UrlForkChangelog = "https://github.com/robertpopa22/mRemoteNG/blob/main/CHANGELOG.md";
        // The F1 manual is the upstream one and documents none of this fork's additions.
        public const string UrlForkUserGuide = "https://github.com/robertpopa22/mRemoteNG/blob/main/docs/USER-GUIDE.md";
        public static readonly string ApplicationVersion = Application.ProductVersion;
        public static readonly string BuildId = ResolveBuildId(Assembly.GetExecutingAssembly(), ApplicationVersion);
        public static readonly string ProductName = Application.ProductName ?? string.Empty;
        public static readonly string Copyright = (Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyCopyrightAttribute), false) as AssemblyCopyrightAttribute)?.Copyright ?? string.Empty;
        public static readonly string HomePath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) ?? string.Empty;

        //public static string ReportingFilePath = "";
        private static readonly string puttyPath = HomePath + "\\PuTTYNG.exe";

        public static string UserAgent
        {
            get
            {
                List<string> details =
                [
                    "compatible",
                    OSVersion.Platform == PlatformID.Win32NT
                        ? $"Windows NT {OSVersion.Version.Major}.{OSVersion.Version.Minor}"
                        : OSVersion.VersionString
                ];
                if (Is64BitProcess)
                {
                    details.Add("WOW64");
                }

                details.Add(Thread.CurrentThread.CurrentUICulture.Name);
                details.Add($".NET CLR {Environment.Version}");
                string detailsString = string.Join("; ", [.. details]);

                return $"Mozilla/5.0 ({detailsString}) {ProductName}/{ApplicationVersion}";
            }
        }

        public static string PuttyPath => puttyPath;

        /// <summary>Version core of a product-version string, without the "+commit" suffix or trailing text.</summary>
        public static string GetBaseVersion(string productVersion)
        {
            string first = productVersion.Trim().Split(' ')[0];
            int plus = first.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? first[..plus] : first;
        }

        /// <summary>"1.84.0+a1b2c3d4e5[.dirty]" for titles and reports.</summary>
        public static string DisplayVersion => $"{GetBaseVersion(ApplicationVersion)}+{BuildId}";

        public static Version GetApplicationVersion() => ParseApplicationVersion(ApplicationVersion);

        public static Version ParseApplicationVersion(string productVersion)
        {
            string cleanedVersion = GetBaseVersion(productVersion);
            Match build = Regex.Match(productVersion, @"\bbuild\s+(\d+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (build.Success)
                cleanedVersion = cleanedVersion + "." + build.Groups[1].Value;

            _ = System.Version.TryParse(cleanedVersion, out Version? parsedVersion);
            return parsedVersion ?? new Version(0, 0);
        }

        private static string? ReadMetadata(Assembly assembly, string key)
        {
            foreach (AssemblyMetadataAttribute attribute in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
            {
                if (string.Equals(attribute.Key, key, StringComparison.Ordinal))
                    return attribute.Value;
            }

            return null;
        }

        private static string ResolveBuildId(Assembly assembly, string productVersion)
        {
            string? sha = ReadMetadata(assembly, "CommitSha");
            if (string.IsNullOrWhiteSpace(sha))
            {
                // Builds without the metadata (CI) may still carry "+sha" in the product version.
                string first = productVersion.Trim().Split(' ')[0];
                int plus = first.IndexOf('+', StringComparison.Ordinal);
                return plus >= 0 && plus < first.Length - 1 ? first[(plus + 1)..] : "nogit";
            }

            return sha + (ReadMetadata(assembly, "Dirty") ?? string.Empty);
        }
    }
}
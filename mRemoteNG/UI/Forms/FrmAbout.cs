using System;
using System.Diagnostics;
using System.Windows.Forms;
using mRemoteNG.App.Info;
using mRemoteNG.Themes;
using mRemoteNG.Resources.Language;
using mRemoteNG.Properties;
using System.Runtime.Versioning;
using mRemoteNG.UI.Window;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Tools;

namespace mRemoteNG.UI.Forms
{
    [SupportedOSPlatform("windows")]
    public partial class frmAbout : BaseWindow
    {
        public static frmAbout Instance { get; set; } = new frmAbout();

        public frmAbout()
        {
            InitializeComponent();
            Icon = Resources.ImageConverter.GetImageAsIcon(Properties.Resources.UIAboutBox_16x);
            ThemeManager.getInstance().ThemeChanged += ApplyTheme;
            ApplyLanguage();
            ApplyTheme();
        }

        // The live figures below are rewritten mechanically by scripts/sync-readme-metrics.py
        // (the same pass that keeps README.md honest) - do not hand-edit the numbers.
        private const string LiveStats = "6,701 automated tests · 0 analyzer warnings · 92 issues fixed for external reporters";

        private void ApplyLanguage()
        {
            lblLicense.Text = Language.ReleasedUnderGPL;
            base.Text = Language.MenuItem_About;
            TabText = Language.MenuItem_About;
            llChangelog.Text = Language.OriginalChangelog;
            llCredits.Text = Language.OriginalCredits;
            llLicense.Text = Language.License;
            lblCopyright.Text = GeneralAppInfo.Copyright;
            lblTitle.Text = "mRemoteNG Community Edition";
            var puttyVersion = PuttyTypeDetector.GetPuttyVersion(GeneralAppInfo.PuttyPath);
            var puttyVersionStr = (puttyVersion.Major > 0 || puttyVersion.Minor > 0)
                ? $" | PuTTY {puttyVersion.Major}.{puttyVersion.Minor}"
                : string.Empty;
            lblVersion.Text = $@"Version {GeneralAppInfo.ApplicationVersion}{puttyVersionStr} — .NET 10, 16 protocols{PortableSuffix()}";
            lblStats.Text = LiveStats;
            lblStory.Text =
                "This fork is maintained by an AI-assisted pipeline at Geseidl IT Solutions: every " +
                "report is investigated, fixed, built and tested automatically, with human review on " +
                "anything security-sensitive. It stands - with respect and gratitude - on almost two " +
                "decades of work by the mRemoteNG project: Felix Deimel, Riley McArdle, and the " +
                "mRemoteNG Dev Team. Their links live on the right.";
            lblForkHeader.Text = $@"{Language.ForkHeader} ({GeneralAppInfo.ForkOwner})";
            llForkGitHub.Text = Language.ForkGitHubPage;
            llForkReleases.Text = Language.ForkReleases;
            llForkChangelog.Text = Language.ForkChangelog;
            llDonate.Text = "Support the Geseidl Association";
            lblOriginalHeader.Text = "The Original Project — thank you";
            lblMaintainedBy.Text = Language.MaintainedBy;
            lblMaintainer.Text = "Geseidl IT Solutions";
            llMaintainerWebsite.Text = "geseidl.ro/servicii-it";
        }

        private static string PortableSuffix()
        {
#if PORTABLE
            return $" — {Language.PortableEdition}";
#else
            return string.Empty;
#endif
        }

        private new void ApplyTheme()
        {
            if (!ThemeManager.getInstance().ThemingActive) return;
            if (!ThemeManager.getInstance().ActiveAndExtended) return;
            var palette = ThemeManager.getInstance().ActiveTheme.ExtendedPalette;
            if (palette == null) return;
            pnlBottom.BackColor = palette.getColor("Dialog_Background");
            pnlBottom.ForeColor = palette.getColor("Dialog_Foreground");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            // Don't cancel close when shown in DockPanel
            // This allows the tab to close properly without showing connection close dialog
        }

        private void llLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl($"https://raw.githubusercontent.com/{GeneralAppInfo.ForkOwner}/mRemoteNG/main/COPYING.txt");
            Close();
        }

        private void llChangelog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // "Original" changelog = the upstream mRemoteNG project (the fork's own changelog is llForkChangelog).
            // HEAD resolves to upstream's default branch, so this stays valid without a version/channel tag.
            OpenUrl("https://raw.githubusercontent.com/mRemoteNG/mRemoteNG/HEAD/CHANGELOG.md");
            Close();
        }

        private void llCredits_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            // "Original" credits = the upstream mRemoteNG project.
            OpenUrl("https://raw.githubusercontent.com/mRemoteNG/mRemoteNG/HEAD/CREDITS.md");
            Close();
        }

        private void llForkGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(GeneralAppInfo.UrlForkHome);
        }

        private void llForkReleases_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(GeneralAppInfo.UrlForkReleases);
        }

        private void llForkChangelog_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(GeneralAppInfo.UrlForkChangelog);
        }

        private void llMaintainerWebsite_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl("https://geseidl.ro/servicii-it");
        }

        private void llDonate_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            OpenUrl(GeneralAppInfo.UrlDonate);
        }

        private static void OpenUrl(string url)
        {
            // Validate URL format to prevent injection
            if (string.IsNullOrWhiteSpace(url))
                return;

            // Basic URL validation - ensure it starts with http:// or https://
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Invalid URL format - don't try to open it
                return;
            }

            try
            {
                // Use the standard .NET approach for opening URLs securely
                // UseShellExecute=true delegates to the OS default handler
                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch
            {
                // Fallback for older .NET Core versions with bug: https://github.com/dotnet/corefx/issues/10361
                // Use platform-specific URL launchers
                try
                {
                    if (OperatingSystem.IsWindows())
                    {
                        // Use rundll32 with url.dll as fallback
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "rundll32.exe",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        startInfo.ArgumentList.Add("url.dll,FileProtocolHandler");
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                    else if (OperatingSystem.IsLinux())
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "xdg-open",
                            UseShellExecute = false
                        };
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                    else if (OperatingSystem.IsMacOS())
                    {
                        var startInfo = new ProcessStartInfo
                        {
                            FileName = "open",
                            UseShellExecute = false
                        };
                        startInfo.ArgumentList.Add(url);
                        Process.Start(startInfo);
                    }
                }
                catch
                {
                    // Unable to open URL - notify the user
                    Runtime.MessageCollector?.AddMessage(MessageClass.WarningMsg,
                        "Unable to open URL in browser. Please open manually: " + url, true);
                }
            }
        }
    }
}

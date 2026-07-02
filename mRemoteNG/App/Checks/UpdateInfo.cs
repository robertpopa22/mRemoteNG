using System;
using System.Text.Json;

namespace mRemoteNG.App.Update
{
    public class UpdateInfo
    {
        public bool IsValid { get; private set; }
        public bool IsGitHubSource { get; private set; }
        public Version? Version { get; private set; }
        public Uri? ReleasePageUrl { get; private set; }
        public Uri? ChangeLogAddress { get; private set; }
        public string? ChangeLogBody { get; private set; }

        public static UpdateInfo FromGitHubJson(string json)
        {
            UpdateInfo newInfo = new() { IsGitHubSource = true };
            if (string.IsNullOrEmpty(json))
                return newInfo;

            try
            {
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("tag_name", out JsonElement tagEl))
                {
                    string? tag = tagEl.GetString()?.TrimStart('v');
                    if (!string.IsNullOrEmpty(tag) && Version.TryParse(tag, out Version? ver))
                        newInfo.Version = ver;
                }

                // Fallback: if tag_name is not semver (e.g. "nightly"), try to extract
                // version from the release name like "Nightly Build — 20260313 (v1.82.0-beta.1)" (#51)
                if (newInfo.Version == null && root.TryGetProperty("name", out JsonElement nameEl))
                {
                    string? name = nameEl.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        var match = System.Text.RegularExpressions.Regex.Match(
                            name, @"v?(\d+\.\d+\.\d+)");
                        if (match.Success && Version.TryParse(match.Groups[1].Value, out Version? nameVer))
                            newInfo.Version = nameVer;
                    }
                }

                if (root.TryGetProperty("html_url", out JsonElement htmlUrlEl))
                {
                    string? htmlUrl = htmlUrlEl.GetString();
                    if (!string.IsNullOrEmpty(htmlUrl) && Uri.TryCreate(htmlUrl, UriKind.Absolute, out Uri? releaseUri))
                    {
                        newInfo.ChangeLogAddress = releaseUri;
                        newInfo.ReleasePageUrl = releaseUri;
                    }
                }

                if (root.TryGetProperty("body", out JsonElement bodyEl))
                    newInfo.ChangeLogBody = bodyEl.GetString();

                newInfo.IsValid = newInfo.Version != null;
            }
            catch
            {
                newInfo.IsValid = false;
            }

            return newInfo;
        }
    }
}

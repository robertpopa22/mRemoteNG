using System;
using mRemoteNG.App.Update;
using NUnit.Framework;

namespace mRemoteNGTests.App;

[TestFixture]
public class UpdaterTests
{
    private readonly Version TestApplicationVersion = new("1.0.0.0");

    [Test]
    public void GitHubReleaseParsesVersionFromTag()
    {
        const string json = """
        { "tag_name": "v1.82.0", "name": "mRemoteNG v1.82.0", "html_url": "https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.82.0", "body": "Release notes" }
        """;
        var info = UpdateInfo.FromGitHubJson(json);
        Assert.Multiple(() =>
        {
            Assert.That(info.IsValid, Is.True);
            Assert.That(info.IsGitHubSource, Is.True);
            Assert.That(info.Version, Is.EqualTo(new Version("1.82.0")));
            Assert.That(info.Version > TestApplicationVersion, Is.True);
            Assert.That(info.ReleasePageUrl?.ToString(), Is.EqualTo("https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.82.0"));
            Assert.That(info.ChangeLogBody, Is.EqualTo("Release notes"));
        });
    }

    [Test]
    public void GitHubReleaseFallsBackToVersionInName()
    {
        // Rolling nightly: tag_name is not semver, so the version is taken from the release name (#51).
        const string json = """
        { "tag_name": "nightly", "name": "Nightly Build 20260701 (v1.82.0-beta.3)", "html_url": "https://github.com/robertpopa22/mRemoteNG/releases/tag/nightly" }
        """;
        var info = UpdateInfo.FromGitHubJson(json);
        Assert.Multiple(() =>
        {
            Assert.That(info.IsValid, Is.True);
            Assert.That(info.Version, Is.EqualTo(new Version("1.82.0")));
        });
    }

    [Test]
    public void EmptyJsonIsInvalid()
    {
        var info = UpdateInfo.FromGitHubJson("");
        Assert.That(info.IsValid, Is.False);
    }

    [Test]
    public void MalformedJsonIsInvalid()
    {
        var info = UpdateInfo.FromGitHubJson("{ not json ");
        Assert.That(info.IsValid, Is.False);
    }

    [Test]
    public void JsonWithoutVersionIsInvalid()
    {
        var info = UpdateInfo.FromGitHubJson("""{ "html_url": "https://example.com" }""");
        Assert.That(info.IsValid, Is.False);
    }
}

using System;
using mRemoteNG.App.Info;
using NUnit.Framework;

namespace mRemoteNGTests.App.Info
{
    /// <summary>
    /// The product version now carries "+commit[.dirty]". It must not leak into the numeric
    /// version, or AppUpdater.IsUpdateAvailable() would always be true.
    /// </summary>
    [TestFixture]
    public class ApplicationVersionParsingTests
    {
        [TestCase("1.84.0 (Local build 3743) x64")]
        [TestCase("1.84.0+a1b2c3d4e5 (Local build 3743) x64")]
        [TestCase("1.84.0+a1b2c3d4e5.dirty (Local build 3743) x64")]
        public void LocalBuildFormatsParseToTheSameVersion(string productVersion) =>
            Assert.That(GeneralAppInfo.ParseApplicationVersion(productVersion), Is.EqualTo(new Version(1, 84, 0, 3743)));

        [TestCase("1.84.0+a1b2c3d4e5.dirty (Local build 3743) x64", "1.84.0")]
        [TestCase("1.84.0 (Local build 3743) x64", "1.84.0")]
        [TestCase("1.84.0+abc", "1.84.0")]
        public void BaseVersionDropsTheCommitSuffix(string productVersion, string expected) =>
            Assert.That(GeneralAppInfo.GetBaseVersion(productVersion), Is.EqualTo(expected));

        [Test]
        public void GarbageParsesToZero() =>
            Assert.That(GeneralAppInfo.ParseApplicationVersion("nonsense"), Is.EqualTo(new Version(0, 0)));
    }
}

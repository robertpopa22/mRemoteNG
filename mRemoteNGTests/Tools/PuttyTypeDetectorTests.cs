using System;
using System.IO;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    [TestFixture]
    public class PuttyTypeDetectorTests
    {
        [Test]
        public void GetPuttyVersion_ReturnsCorrectVersion_ForPuTTYNG()
        {
            // Assuming PuTTYNG.exe exists in the repo root or can be located.
            // The test runner runs in bin/x64/Release/
            // The PuTTYNG.exe is likely copied there or in the project root.
            
            string puttyPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "PuTTYNG.exe"));
            if (!File.Exists(puttyPath))
            {
                 // Try looking up
                 puttyPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../../mRemoteNG/PuTTYNG.exe"));
            }

            if (!File.Exists(puttyPath))
            {
                Assert.Ignore("PuTTYNG.exe not found for testing version detection.");
            }

            var version = PuttyTypeDetector.GetPuttyVersion(puttyPath);

            // The bundled binary tracks upstream PuTTY releases (0.85 since #169); pin the
            // floor rather than the exact minor so a future security rebase does not need
            // this test edited again, while a stale-binary regression still fails it.
            Assert.That(version.Major, Is.EqualTo(0));
            Assert.That(version.Minor, Is.GreaterThanOrEqualTo(85));
        }

        [Test]
        public void GetPuttyVersion_ReturnsZero_ForNonExistentFile()
        {
            var version = PuttyTypeDetector.GetPuttyVersion("non_existent_file.exe");
            Assert.That(version, Is.EqualTo(new Version(0, 0)));
        }
    }
}

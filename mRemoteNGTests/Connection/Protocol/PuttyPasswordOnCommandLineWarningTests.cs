using System;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol
{
    /// <summary>
    /// A PuTTY without -pwfile still gets the password with -pw, so automatic logon keeps working
    /// on KiTTY and older PuTTY builds, but the exposure on the command line is logged once per
    /// executable instead of being silent.
    /// </summary>
    [TestFixture]
    public class PuttyPasswordOnCommandLineWarningTests
    {
        [Test]
        public void WarnsOncePerExecutable()
        {
            string exe = $@"C:\nowhere\kitty-{Guid.NewGuid():N}.exe";

            Assert.That(PuttyBase.WarnPasswordOnCommandLine(exe, new Version(0, 76, 1, 13)), Is.True);
            Assert.That(PuttyBase.WarnPasswordOnCommandLine(exe, new Version(0, 76, 1, 13)), Is.False);
            Assert.That(PuttyBase.WarnPasswordOnCommandLine(exe.ToUpperInvariant(), new Version(0, 0)), Is.False);
        }

        [Test]
        public void EachExecutableIsWarnedAbout()
        {
            Assert.That(PuttyBase.WarnPasswordOnCommandLine($@"C:\a-{Guid.NewGuid():N}.exe", new Version(0, 0)), Is.True);
            Assert.That(PuttyBase.WarnPasswordOnCommandLine($@"C:\b-{Guid.NewGuid():N}.exe", new Version(0, 0)), Is.True);
        }
    }
}

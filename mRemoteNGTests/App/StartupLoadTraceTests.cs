using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using mRemoteNG.App;

namespace mRemoteNGTests.App
{
    [TestFixture]
    public class StartupLoadTraceTests
    {
        [TestCase(FileAttributes.Normal, false)]
        [TestCase(FileAttributes.Archive | FileAttributes.ReadOnly, false)]
        [TestCase(FileAttributes.Archive | FileAttributes.ReparsePoint, false)]
        [TestCase(FileAttributes.Offline, true)]
        [TestCase((FileAttributes)0x00040000, true)] // RECALL_ON_OPEN
        [TestCase((FileAttributes)0x00400000 | FileAttributes.Archive, true)] // RECALL_ON_DATA_ACCESS: OneDrive online-only
        [TestCase((FileAttributes)0x00080000 | FileAttributes.Archive, false)] // PINNED: always kept on this device
        public void IsNotLocal_ReadsTheCloudPlaceholderBits(FileAttributes attributes, bool expected)
        {
            Assert.That(StartupLoadTrace.IsNotLocal(attributes), Is.EqualTo(expected));
        }

        [Test]
        public void ElapsedMs_ConvertsStopwatchTicks()
        {
            Assert.That(StartupLoadTrace.ElapsedMs(0, Stopwatch.Frequency * 3), Is.EqualTo(3000));
        }
    }
}

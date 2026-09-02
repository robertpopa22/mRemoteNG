using System.IO;
using System.Linq;
using mRemoteNG.Connection;
using NUnit.Framework;

namespace mRemoteNGTests.Connection
{
    /// <summary>
    /// Starting a new connections file is offered as recovery when loading failed, and the reason
    /// for the failure is not always the file itself (#175: a plugin assembly that would not load
    /// was reported as a missing connections file). The answer must never be a silently destroyed
    /// file, so the existing one is copied aside first.
    /// </summary>
    public class ConnectionsServiceBackupBeforeReplaceTests
    {
        private string _directory;

        [SetUp]
        public void Setup()
        {
            _directory = Path.Combine(Path.GetTempPath(), "mRemoteNGTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void ExistingConnectionsFileIsCopiedAsideBeforeBeingReplaced()
        {
            string path = Path.Combine(_directory, "confCons.xml");
            const string original = "<Connections>the user's only copy</Connections>";
            File.WriteAllText(path, original);

            ConnectionsService.BackUpBeforeReplacing(path);

            string backup = Directory.GetFiles(_directory, "confCons.xml.*.replaced.backup").Single();
            Assert.That(File.ReadAllText(backup), Is.EqualTo(original));
        }

        [Test]
        public void OriginalFileIsLeftUntouchedByTheBackup()
        {
            string path = Path.Combine(_directory, "confCons.xml");
            const string original = "<Connections>the user's only copy</Connections>";
            File.WriteAllText(path, original);

            ConnectionsService.BackUpBeforeReplacing(path);

            Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        }

        [Test]
        public void NothingIsWrittenWhenThereIsNoFileToLose()
        {
            string path = Path.Combine(_directory, "confCons.xml");

            ConnectionsService.BackUpBeforeReplacing(path);

            Assert.That(Directory.GetFiles(_directory), Is.Empty);
        }
    }
}

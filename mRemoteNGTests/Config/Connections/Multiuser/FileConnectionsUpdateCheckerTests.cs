using System;
using System.IO;
using System.Threading;
using mRemoteNG.App;
using mRemoteNG.Config.Connections.Multiuser;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNGTests.Properties;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Connections.Multiuser
{
    [TestFixture]
    public class FileConnectionsUpdateCheckerTests
    {
        private string _tempFile;

        [SetUp]
        public void Setup()
        {
            _tempFile = Path.GetTempFileName();
        }

        [TearDown]
        public void Teardown()
        {
            if (File.Exists(_tempFile))
                File.Delete(_tempFile);
        }

        [Test]
        public void IsUpdateAvailable_ReturnsTrue_WhenFileIsNewer()
        {
            // Set LastFileUpdate to past
            Runtime.ConnectionsService.LastFileUpdate = DateTime.UtcNow.AddMinutes(-10);
            
            // File created now
            var checker = new FileConnectionsUpdateChecker(_tempFile);
            
            Assert.That(checker.IsUpdateAvailable(), Is.True);
        }

        [Test]
        public void IsUpdateAvailable_ReturnsFalse_WhenFileIsOlder()
        {
            // Set LastFileUpdate to future
            Runtime.ConnectionsService.LastFileUpdate = DateTime.UtcNow.AddMinutes(10);
            
            var checker = new FileConnectionsUpdateChecker(_tempFile);
            
            Assert.That(checker.IsUpdateAvailable(), Is.False);
        }
        
        [Test]
        public void IsUpdateAvailable_ReturnsFalse_WhenFileIsSameAge()
        {
             // This is tricky because of file system precision.
             // Let's set LastFileUpdate to file's write time.
             DateTime lastWrite = File.GetLastWriteTimeUtc(_tempFile);
             
             // The checker compares exact UTC ticks (no truncation).
             Runtime.ConnectionsService.LastFileUpdate = lastWrite;
             
             var checker = new FileConnectionsUpdateChecker(_tempFile);
             
             Assert.That(checker.IsUpdateAvailable(), Is.False);
        }

        [Test]
        public void IsUpdateAvailable_ReturnsTrue_WhenFileIsNewerBySubSecondAmount()
        {
            DateTime lastWrite = File.GetLastWriteTimeUtc(_tempFile);
            Runtime.ConnectionsService.LastFileUpdate = lastWrite;
            File.SetLastWriteTimeUtc(_tempFile, lastWrite.AddMilliseconds(300));

            using var checker = new FileConnectionsUpdateChecker(_tempFile);

            Assert.That(checker.IsUpdateAvailable(), Is.True);
        }

        [Test]
        public void WatcherError_RaisesWatcherDegraded()
        {
            using var checker = new FileConnectionsUpdateChecker(_tempFile);
            int raised = 0;
            checker.WatcherDegraded += (_, _) => raised++;

            checker.SimulateWatcherError();

            Assert.That(raised, Is.EqualTo(1));
        }

        [Test]
        public void IsUpdateAvailable_StaysFalseAfterReload_UntilFileChangesAgain()
        {
            // #210: an external reload must refresh LastFileUpdate, otherwise every poll
            // re-detects the same change and the file is reloaded forever.
            using var _ = FileTestHelpers.DisposableTempFile(out var filePath, ".xml");
            File.WriteAllText(filePath, Resources.confCons_v2_6);
            File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddMinutes(-5)); // "touched" by another process

            DateTime previousLastFileUpdate = Runtime.ConnectionsService.LastFileUpdate;
            try
            {
                var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);
                connectionsService.LoadConnections(useDatabase: false, import: false, connectionFileName: filePath);

                Assert.That(connectionsService.LastFileUpdate, Is.EqualTo(File.GetLastWriteTimeUtc(filePath)));

                // The checker reads the global service; mirror the reloaded value into it.
                Runtime.ConnectionsService.LastFileUpdate = connectionsService.LastFileUpdate;
                using var checker = new FileConnectionsUpdateChecker(filePath);

                Assert.That(checker.IsUpdateAvailable(), Is.False, "no update right after the reload");
                Assert.That(checker.IsUpdateAvailable(), Is.False, "still no update on the next poll");

                File.SetLastWriteTimeUtc(filePath, DateTime.UtcNow.AddMinutes(-1));

                Assert.That(checker.IsUpdateAvailable(), Is.True, "a later write is detected again");
            }
            finally
            {
                Runtime.ConnectionsService.LastFileUpdate = previousLastFileUpdate;
            }
        }
    }
}

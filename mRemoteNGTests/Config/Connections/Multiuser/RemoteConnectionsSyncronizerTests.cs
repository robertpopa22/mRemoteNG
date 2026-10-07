using System;
using System.IO;
using mRemoteNG.App;
using mRemoteNG.Config.Connections.Multiuser;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Connections.Multiuser
{
    [TestFixture]
    [NonParallelizable]
    public class RemoteConnectionsSyncronizerTests
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
        public void FileModeUsesLongSafetyNetIntervalInsteadOfSqlReloadInterval()
        {
            using var syncronizer = new RemoteConnectionsSyncronizer(new FileConnectionsUpdateChecker(_tempFile));

            Assert.That(syncronizer.TimerIntervalInMilliseconds, Is.EqualTo(300000.0));
            Assert.That(RemoteConnectionsSyncronizer.FileModeSafetyNetIntervalInMilliseconds, Is.EqualTo(300000.0));
        }

        [Test]
        public void WatcherDegradedSwitchesToTheSqlReloadInterval()
        {
            int previous = OptionsDBsPage.Default.SQLReloadInterval;
            try
            {
                OptionsDBsPage.Default.SQLReloadInterval = 20;
                using var checker = new FileConnectionsUpdateChecker(_tempFile);
                using var syncronizer = new RemoteConnectionsSyncronizer(checker);
                Assert.That(syncronizer.TimerIntervalInMilliseconds, Is.EqualTo(300000.0));

                checker.SimulateWatcherError();

                Assert.That(syncronizer.TimerIntervalInMilliseconds, Is.EqualTo(20000.0));
            }
            finally
            {
                OptionsDBsPage.Default.SQLReloadInterval = previous;
            }
        }

        [Test]
        public void WatcherDegradedFallbackIntervalHasAFiveSecondFloor()
        {
            int previous = OptionsDBsPage.Default.SQLReloadInterval;
            try
            {
                OptionsDBsPage.Default.SQLReloadInterval = 1;
                using var checker = new FileConnectionsUpdateChecker(_tempFile);
                using var syncronizer = new RemoteConnectionsSyncronizer(checker);

                checker.SimulateWatcherError();

                Assert.That(syncronizer.TimerIntervalInMilliseconds, Is.EqualTo(5000.0));
            }
            finally
            {
                OptionsDBsPage.Default.SQLReloadInterval = previous;
            }
        }

        [Test]
        public void NonFileCheckerKeepsSqlReloadInterval()
        {
            double expected = OptionsDBsPage.Default.SQLReloadInterval * 1000.0;
            if (expected <= 0) expected = 30000.0;

            using var syncronizer = new RemoteConnectionsSyncronizer(new FakeUpdateChecker());

            Assert.That(syncronizer.TimerIntervalInMilliseconds, Is.EqualTo(expected));
        }

        [Test]
        public void DisposeDisposesTheUpdateChecker()
        {
            var checker = new FakeUpdateChecker();
            var syncronizer = new RemoteConnectionsSyncronizer(checker);

            syncronizer.Enable();
            syncronizer.Dispose();

            Assert.That(checker.DisposeCount, Is.EqualTo(1));
            Assert.DoesNotThrow(() => syncronizer.Enable());
            Assert.DoesNotThrow(() => syncronizer.Disable());
        }

        [Test]
        public void CreateConnectionsProviderKeepsTheExistingFileSyncronizer()
        {
            bool previousUseSql = OptionsDBsPage.Default.UseSQLServer;
            bool previousWatch = OptionsConnectionsPage.Default.WatchConnectionFile;
            RemoteConnectionsSyncronizer? previousSyncronizer = Runtime.ConnectionsService.RemoteConnectionsSyncronizer;
            var checker = new FakeUpdateChecker();
            var existing = new RemoteConnectionsSyncronizer(checker);
            try
            {
                OptionsDBsPage.Default.UseSQLServer = false;
                OptionsConnectionsPage.Default.WatchConnectionFile = true;
                Runtime.ConnectionsService.RemoteConnectionsSyncronizer = existing;

                Startup.CreateConnectionsProvider(new MessageCollector());

                Assert.That(Runtime.ConnectionsService.RemoteConnectionsSyncronizer, Is.SameAs(existing));
                Assert.That(checker.DisposeCount, Is.EqualTo(0));
            }
            finally
            {
                existing.Dispose();
                Runtime.ConnectionsService.RemoteConnectionsSyncronizer = previousSyncronizer;
                OptionsDBsPage.Default.UseSQLServer = previousUseSql;
                OptionsConnectionsPage.Default.WatchConnectionFile = previousWatch;
            }
        }

        private sealed class FakeUpdateChecker : IConnectionsUpdateChecker
        {
            public int DisposeCount { get; private set; }

            public bool IsUpdateAvailable() => false;

            public void IsUpdateAvailableAsync()
            {
            }

            public event EventHandler? UpdateCheckStarted { add { } remove { } }
            public event UpdateCheckFinishedEventHandler? UpdateCheckFinished { add { } remove { } }
            public event ConnectionsUpdateAvailableEventHandler? ConnectionsUpdateAvailable { add { } remove { } }

            public void Dispose() => DisposeCount++;
        }
    }
}

using System;
using System.IO;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNGTests.Properties;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

/// <summary>
/// #210: an external-change reload of a file whose bytes did not change must not deserialize
/// it again nor raise ConnectionsLoaded; it only refreshes LastFileUpdate.
/// </summary>
[NonParallelizable]
public class ConnectionsServiceContentHashTests
{
    [Test]
    public void ReloadOfUnchangedContentIsSkippedAndDoesNotRaiseConnectionsLoaded()
    {
        using var _ = FileTestHelpers.DisposableTempFile(out var filePath, ".xml");
        File.WriteAllText(filePath, Resources.confCons_v2_6);

        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);
        int loadedEvents = 0;
        connectionsService.ConnectionsLoaded += (sender, args) => loadedEvents++;

        Assert.That(connectionsService.LoadConnections(false, false, filePath, skipIfContentUnchanged: true), Is.True);
        Assert.That(loadedEvents, Is.EqualTo(1));
        var modelAfterFirstLoad = connectionsService.ConnectionTreeModel;

        // Same bytes, newer timestamp (e.g. a sync tool or another instance touched the file).
        DateTime touched = DateTime.UtcNow.AddMinutes(1);
        File.SetLastWriteTimeUtc(filePath, touched);

        Assert.That(connectionsService.LoadConnections(false, false, filePath, skipIfContentUnchanged: true), Is.False);
        Assert.That(loadedEvents, Is.EqualTo(1), "unchanged content must not raise ConnectionsLoaded");
        Assert.That(connectionsService.ConnectionTreeModel, Is.SameAs(modelAfterFirstLoad));
        Assert.That(connectionsService.LastFileUpdate, Is.EqualTo(File.GetLastWriteTimeUtc(filePath)));
    }

    [Test]
    public void ReloadOfChangedContentRaisesConnectionsLoaded()
    {
        using var _ = FileTestHelpers.DisposableTempFile(out var filePath, ".xml");
        File.WriteAllText(filePath, Resources.confCons_v2_6);

        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);
        int loadedEvents = 0;
        connectionsService.ConnectionsLoaded += (sender, args) => loadedEvents++;

        connectionsService.LoadConnections(false, false, filePath, skipIfContentUnchanged: true);
        File.AppendAllText(filePath, Environment.NewLine);

        Assert.That(connectionsService.LoadConnections(false, false, filePath, skipIfContentUnchanged: true), Is.True);
        Assert.That(loadedEvents, Is.EqualTo(2));
    }

    [Test]
    public void ExplicitLoadOfSameFileStillReloads()
    {
        using var _ = FileTestHelpers.DisposableTempFile(out var filePath, ".xml");
        File.WriteAllText(filePath, Resources.confCons_v2_6);

        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);
        int loadedEvents = 0;
        connectionsService.ConnectionsLoaded += (sender, args) => loadedEvents++;

        connectionsService.LoadConnections(false, false, filePath);
        connectionsService.LoadConnections(false, false, filePath);

        Assert.That(loadedEvents, Is.EqualTo(2), "user-initiated loads keep their previous behavior");
    }
}

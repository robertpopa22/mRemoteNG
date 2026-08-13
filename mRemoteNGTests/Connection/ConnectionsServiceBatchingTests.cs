using System.Reflection;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

/// <summary>
/// EndBatchingSaves used to leave its request flags set. Once any batched operation asked for
/// an async save, every later batch took the debounced async path — and fired a save even when
/// that batch had requested nothing. Tree moves, duplicates and deletes all run inside a batch,
/// so this leaked into ordinary use. (#148)
/// </summary>
[NonParallelizable]
public class ConnectionsServiceBatchingTests
{
    private static bool GetFlag(ConnectionsService service, string fieldName)
    {
        FieldInfo? field = typeof(ConnectionsService)
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null, $"field {fieldName} not found");
        return (bool)field!.GetValue(service)!;
    }

    [Test]
    public void EndBatchingSavesClearsTheAsyncRequestFlag()
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);

        service.BeginBatchingSaves();
        service.SaveConnectionsAsync();
        service.EndBatchingSaves();

        Assert.That(GetFlag(service, "_saveAsyncRequested"), Is.False);
    }

    [Test]
    public void EndBatchingSavesClearsTheSyncRequestFlag()
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);

        service.BeginBatchingSaves();
        service.SaveConnections();
        service.EndBatchingSaves();

        Assert.That(GetFlag(service, "_saveRequested"), Is.False);
    }

    [Test]
    public void AnEmptyBatchLeavesNoPendingSaveRequest()
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);

        service.BeginBatchingSaves();
        service.SaveConnectionsAsync();
        service.EndBatchingSaves();

        // A second batch that asks for nothing must not inherit the first batch's request.
        service.BeginBatchingSaves();
        service.EndBatchingSaves();

        Assert.Multiple(() =>
        {
            Assert.That(GetFlag(service, "_saveAsyncRequested"), Is.False);
            Assert.That(GetFlag(service, "_saveRequested"), Is.False);
        });
    }

    [Test]
    public void CoalescedDebounceDoesNotReportALocalOnlyTrigger()
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);

        // A database-relevant change followed by a local-only one inside the same debounce
        // window must not be reported as local-only, or the whole save is skipped. (#148)
        service.SaveConnectionsAsync("Name");
        service.SaveConnectionsAsync("OpenConnections");

        FieldInfo? field = typeof(ConnectionsService)
            .GetField("_debouncedPropertyNameTrigger", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        Assert.That((string?)field!.GetValue(service), Is.Empty);
    }

    [Test]
    public void ASingleRepeatedTriggerIsPreservedThroughTheDebounce()
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);

        service.SaveConnectionsAsync("OpenConnections");
        service.SaveConnectionsAsync("OpenConnections");

        FieldInfo? field = typeof(ConnectionsService)
            .GetField("_debouncedPropertyNameTrigger", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That((string?)field!.GetValue(service), Is.EqualTo("OpenConnections"));
    }

    [Test]
    public void RenamingTheRootNodeRaisesPropertyChanged()
    {
        var root = new mRemoteNG.Tree.Root.RootNodeInfo(mRemoteNG.Tree.Root.RootNodeType.Connection);
        string? changed = null;
        root.PropertyChanged += (_, e) => changed = e.PropertyName;

        root.Name = "Renamed root";

        // Without the notification nothing queues a save, so the rename never reaches
        // tblRoot unless an unrelated edit flushes it first. (#148)
        Assert.That(changed, Is.EqualTo(nameof(mRemoteNG.Tree.Root.RootNodeInfo.Name)));
    }

    [Test]
    public void SettingTheRootNodeNameToTheSameValueRaisesNothing()
    {
        var root = new mRemoteNG.Tree.Root.RootNodeInfo(mRemoteNG.Tree.Root.RootNodeType.Connection);
        root.Name = "Same";
        int raised = 0;
        root.PropertyChanged += (_, _) => raised++;

        root.Name = "Same";

        Assert.That(raised, Is.Zero);
    }
}

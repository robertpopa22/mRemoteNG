using System.Reflection;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

/// <summary>
/// The autosave timer used to call SaveConnectionsAsync unconditionally, rewriting the
/// connections file and stamping a fresh .backup every interval even when nothing changed
/// (observed live: one identical backup per minute). HasUnsavedChanges arms only on real
/// model changes and the timer skips the save otherwise.
/// </summary>
[NonParallelizable]
public class ConnectionsServiceDirtyTrackingTests
{
    private static ConnectionsService NewServiceWithModel(out ConnectionTreeModel model)
    {
        var service = new ConnectionsService(PuttySessionsManager.Instance);
        model = new ConnectionTreeModel();
        model.AddRootNode(new RootNodeInfo(RootNodeType.Connection));

        MethodInfo? raise = typeof(ConnectionsService).GetMethod(
            "RaiseConnectionsLoadedEvent", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(raise, Is.Not.Null, "RaiseConnectionsLoadedEvent not found");
        raise!.Invoke(service,
            [new Optional<ConnectionTreeModel>(), model, false, false, "test.xml"]);
        return service;
    }

    [Test]
    public void FreshlyLoadedModelIsClean()
    {
        ConnectionsService service = NewServiceWithModel(out _);
        Assert.That(service.HasUnsavedChanges, Is.False);
    }

    [Test]
    public void AddingAConnectionMarksTheModelDirty()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);

        var root = (RootNodeInfo)model.RootNodes[0];
        root.AddChild(new ConnectionInfo { Name = "new one" });

        Assert.That(service.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void PersistedPropertyChangeMarksTheModelDirty()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        var con = new ConnectionInfo { Name = "target" };
        root.AddChild(con);
        SetDirtyFlag(service, false);

        con.Hostname = "changed.example.com";

        Assert.That(service.HasUnsavedChanges, Is.True);
    }

    [Test]
    public void LastChangeReasonNamesWhatArmedTheFlag()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        var con = new ConnectionInfo { Name = "target" };
        root.AddChild(con);

        Assert.That(service.LastChangeReason, Does.StartWith("collection:"));

        con.Hostname = "why.example.com";
        Assert.That(service.LastChangeReason, Is.EqualTo("property:Hostname"));
    }

    [Test]
    public void RuntimeOnlyPropertyChangeDoesNotMarkTheModelDirty()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        var con = new ConnectionInfo { Name = "target" };
        root.AddChild(con);
        SetDirtyFlag(service, false);

        con.HostReachabilityStatus = mRemoteNG.Connection.HostReachabilityStatus.Reachable; // runtime-only (#83 filter)

        Assert.That(service.HasUnsavedChanges, Is.False);
    }

    private static void SetDirtyFlag(ConnectionsService service, bool value)
    {
        PropertyInfo? prop = typeof(ConnectionsService).GetProperty(nameof(ConnectionsService.HasUnsavedChanges));
        Assert.That(prop, Is.Not.Null);
        prop!.SetValue(service, value);
    }
}

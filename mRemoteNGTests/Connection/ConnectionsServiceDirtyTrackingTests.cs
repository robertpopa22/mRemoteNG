using System;
using System.IO;
using System.Reflection;
using System.Threading;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
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

    [Test]
    public void ASuccessfulSaveClearsTheDirtyFlag()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        root.AddChild(new ConnectionInfo { Name = "to-save" });
        Assert.That(service.HasUnsavedChanges, Is.True);

        string file = Path.Combine(Path.GetTempPath(), $"mrng-dirty-{Guid.NewGuid():N}.xml");
        try
        {
            service.SaveConnections(model, false, new SaveFilter(), file, forceSave: true);

            Assert.Multiple(() =>
            {
                Assert.That(File.Exists(file), Is.True, "the save must actually write the file");
                Assert.That(service.HasUnsavedChanges, Is.False,
                    "a successful save leaves the model clean for the autosave timer");
            });
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public void AFailedSaveReArmsTheDirtyFlag()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        root.AddChild(new ConnectionInfo { Name = "unsaveable" });

        // The file data provider creates missing directories, so an unwritable TARGET is the
        // reliable failure: hold the file open with no sharing and the save's write throws.
        // The service reports the failure and must keep the model marked dirty so the next
        // autosave tick retries instead of considering the edit persisted.
        string file = Path.Combine(Path.GetTempPath(), $"mrng-locked-{Guid.NewGuid():N}.xml");
        try
        {
            using (new FileStream(file, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                service.SaveConnections(model, false, new SaveFilter(), file, forceSave: true);
            }

            Assert.That(service.HasUnsavedChanges, Is.True);
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public void ADebouncedAsyncSaveWritesTheFileOnceTheWindowElapses()
    {
        // Covers the debounce timer callback (#83 coalescing, #148 model re-read): the async
        // path re-reads ConnectionTreeModel/ConnectionFileName when the 2s window fires, so both
        // must be populated the way a real load/save leaves them.
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel model);
        var root = (RootNodeInfo)model.RootNodes[0];
        root.AddChild(new ConnectionInfo { Name = "debounced" });

        string file = Path.Combine(Path.GetTempPath(), $"mrng-debounce-{Guid.NewGuid():N}.xml");
        try
        {
            // A real save stamps ConnectionFileName; the model property only has a private
            // setter (LoadConnections owns it), so the fixture assigns it the same way.
            service.SaveConnections(model, false, new SaveFilter(), file, forceSave: true);
            typeof(ConnectionsService)
                .GetProperty(nameof(ConnectionsService.ConnectionTreeModel))!
                .SetValue(service, model);
            service.IsConnectionsFileLoaded = true; // the callback's save has no forceSave
            File.Delete(file);

            service.SaveConnectionsAsync();
            Assert.That(File.Exists(file), Is.False, "the debounced save must not run immediately");

            bool written = SpinWait.SpinUntil(() => File.Exists(file), TimeSpan.FromSeconds(10));
            Assert.That(written, Is.True, "the debounce window elapsed without the save running");
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    [Test]
    public void ReloadingStopsTheOldModelFromArmingTheFlag()
    {
        ConnectionsService service = NewServiceWithModel(out ConnectionTreeModel oldModel);

        var newModel = new ConnectionTreeModel();
        newModel.AddRootNode(new RootNodeInfo(RootNodeType.Connection));
        MethodInfo raise = typeof(ConnectionsService).GetMethod(
            "RaiseConnectionsLoadedEvent", BindingFlags.Instance | BindingFlags.NonPublic)!;
        raise.Invoke(service,
            [new Optional<ConnectionTreeModel>(oldModel), newModel, false, false, "test2.xml"]);

        Assert.That(service.HasUnsavedChanges, Is.False, "a reload leaves the service clean");

        // The replaced tree must be unsubscribed: edits to it are no longer this service's business.
        ((RootNodeInfo)oldModel.RootNodes[0]).AddChild(new ConnectionInfo { Name = "ghost" });
        Assert.That(service.HasUnsavedChanges, Is.False,
            "an edit on the unloaded model must not arm the autosave");
    }

    [Test]
    public void SqlCachePruningKeepsTheBackupCountBounded()
    {
        // TrySaveSqlConnectionsCache stamps a .backup on every write and (since the 2026-08-31
        // fix) prunes to BackupFileKeepCount afterwards — the event-driven pruner never ran for
        // this path and backups accumulated for five months.
        MethodInfo? trySave = typeof(ConnectionsService).GetMethod(
            "TrySaveSqlConnectionsCache", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(trySave, Is.Not.Null, "TrySaveSqlConnectionsCache not found");

        var model = new ConnectionTreeModel();
        model.AddRootNode(new RootNodeInfo(RootNodeType.Connection));

        int keep = mRemoteNG.Properties.OptionsBackupPage.Default.BackupFileKeepCount;
        string settingsDir = mRemoteNG.App.Info.SettingsFileInfo.SettingsPath;
        string cachePath = Path.Combine(settingsDir, mRemoteNG.App.Info.SettingsFileInfo.SqlConnectionsCache);

        for (int i = 0; i < keep + 3; i++)
            trySave!.Invoke(null, [model]);

        string[] backups = Directory.GetFiles(settingsDir,
            Path.GetFileName(cachePath) + ".*.backup");
        Assert.That(backups.Length, Is.LessThanOrEqualTo(keep),
            $"cache backups must be pruned to BackupFileKeepCount ({keep})");
    }
}

using System.IO;
using System.Reflection;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNGTests.TestHelpers;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

[NonParallelizable]
public class ConnectionsServiceFileRefusalTests
{
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void RefusingAFileOnlyClearsTheLoadedFlagWhenReloadingThatSameFile(bool reload, bool stillLoaded)
    {
        using var tempFile = FileTestHelpers.DisposableTempFile(out string file, ".xml");
        const string malformed = "<?xml version=\"1.0\" encoding=\"utf-8\"?><Connections>";
        File.WriteAllText(file, malformed);

        var service = Runtime.ConnectionsService;
        PropertyInfo modelProperty = typeof(ConnectionsService).GetProperty(nameof(ConnectionsService.ConnectionTreeModel))!;
        PropertyInfo fileProperty = typeof(ConnectionsService).GetProperty(nameof(ConnectionsService.ConnectionFileName))!;
        ConnectionTreeModel? originalModel = service.ConnectionTreeModel;
        string? originalPath = service.ConnectionFileName;
        bool originallyLoaded = service.IsConnectionsFileLoaded;

        // Seed the in-memory result of a successful load without saving settings or starting a watcher.
        string loadedPath = reload ? file : Path.Combine(Path.GetDirectoryName(file)!, "loaded.xml");
        var loadedModel = new ConnectionTreeModel();
        loadedModel.AddRootNode(new RootNodeInfo(RootNodeType.Connection) { Filename = loadedPath });
        try
        {
            modelProperty.SetValue(service, loadedModel);
            fileProperty.SetValue(service, loadedPath);
            service.IsConnectionsFileLoaded = true;

            Assert.Throws<XmlException>(() => service.LoadConnections(useDatabase: false, import: false, connectionFileName: file));
            Assert.That(service.IsConnectionsFileLoaded, Is.EqualTo(stillLoaded));
            Assert.That(service.ConnectionTreeModel, Is.SameAs(loadedModel));
            Assert.That(service.ConnectionFileName, Is.EqualTo(loadedPath));
            Assert.That(File.ReadAllText(file), Is.EqualTo(malformed));
        }
        finally
        {
            service.IsConnectionsFileLoaded = originallyLoaded;
            fileProperty.SetValue(service, originalPath);
            modelProperty.SetValue(service, originalModel);
        }
    }
}

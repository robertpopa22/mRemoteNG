using System;
using System.Linq;
using mRemoteNG.Config.Serializers.MiscSerializers;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNGTests.Properties;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.MiscSerializers;

public class MicrosoftRdClientBackupDeserializerTests
{
    private MicrosoftRdClientBackupDeserializer _deserializer;
    private ConnectionTreeModel _connectionTreeModel;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _deserializer = new MicrosoftRdClientBackupDeserializer();
        _connectionTreeModel = _deserializer.Deserialize(Resources.test_msrdclient_backup_rdb);
    }

    [Test]
    public void ConnectionTreeModelHasARootNode()
    {
        Assert.That(_connectionTreeModel.RootNodes.Count, Is.GreaterThan(0));
    }

    [Test]
    public void ResolvesGroupToContainer()
    {
        var containers = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().ToList();
        Assert.That(containers.Count, Is.EqualTo(1));
        Assert.That(containers[0].Name, Is.EqualTo("Production"));
    }

    [Test]
    public void DeserializesConnectionHostname()
    {
        var container = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var conn = container.Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Hostname, Is.EqualTo("server1.example.com"));
    }

    [Test]
    public void DeserializesConnectionFriendlyName()
    {
        var container = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var conn = container.Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Name, Is.EqualTo("Server 1"));
    }

    [Test]
    public void ResolvesCredentials()
    {
        var container = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var conn = container.Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Username, Is.EqualTo("admin"));
        Assert.That(conn.Domain, Is.EqualTo("corp"));
    }

    [Test]
    public void SetsProtocolToRdp()
    {
        var container = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var conn = container.Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Protocol, Is.EqualTo(ProtocolType.RDP));
    }

    [Test]
    public void SetsGateway()
    {
        var container = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var conn = container.Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.RDGatewayHostname, Is.EqualTo("gateway.example.com"));
        Assert.That(conn.RDGatewayUsageMethod, Is.EqualTo(RDGatewayUsageMethod.Always));
    }

    [Test]
    public void ConnectionWithoutGroupGoesToRoot()
    {
        var rootChildren = _connectionTreeModel.RootNodes.First().Children.OfType<ConnectionInfo>().ToList();
        Assert.That(rootChildren.Any(c => string.Equals(c.Name, "Server 2", StringComparison.Ordinal)), Is.True);
    }

    [Test]
    public void ConnectionWithoutCredentialsHasEmptyUsername()
    {
        var rootChildren = _connectionTreeModel.RootNodes.First().Children.OfType<ConnectionInfo>().ToList();
        var server2 = rootChildren.First(c => string.Equals(c.Name, "Server 2", StringComparison.Ordinal));
        Assert.That(server2.Username, Is.EqualTo(""));
    }

    [Test]
    public void HandlesEmptyConnectionsList()
    {
        var result = _deserializer.Deserialize("{\"version\":\"1.0\",\"Groups\":[],\"Credentials\":[],\"Connections\":[]}");
        Assert.That(result.RootNodes.Count, Is.GreaterThan(0));
        Assert.That(result.RootNodes.First().Children, Is.Empty);
    }

    [Test]
    public void HandlesEmptyFile()
    {
        var result = _deserializer.Deserialize("");
        Assert.That(result.RootNodes.Count, Is.GreaterThan(0));
        Assert.That(result.RootNodes.First().Children, Is.Empty);
    }

    [Test]
    public void HandlesNonArrayCollections()
    {
        // A malformed/hostile .rdb where Groups/Credentials/Connections are not arrays must not
        // throw (EnumerateArray throws on a non-array) and abort the whole import.
        const string rdb = "{\"version\":\"1.0\",\"Groups\":\"oops\",\"Credentials\":{},\"Connections\":42}";
        Assert.That(() => _deserializer.Deserialize(rdb), Throws.Nothing);
        var result = _deserializer.Deserialize(rdb);
        Assert.That(result.RootNodes.Count, Is.GreaterThan(0));
        Assert.That(result.RootNodes.First().Children, Is.Empty);
    }
}

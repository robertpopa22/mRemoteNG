using System;
using System.Linq;
using mRemoteNG.Config.Serializers.MiscSerializers;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNGTests.Properties;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.MiscSerializers;

public class MobaXTermSessionDeserializerTests
{
    private MobaXTermSessionDeserializer _deserializer;
    private ConnectionTreeModel _connectionTreeModel;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _deserializer = new MobaXTermSessionDeserializer();
        _connectionTreeModel = _deserializer.Deserialize(Resources.test_mobaxterm_moba);
    }

    [Test]
    public void ConnectionTreeModelHasARootNode()
    {
        Assert.That(_connectionTreeModel.RootNodes.Count, Is.GreaterThan(0));
    }

    [Test]
    public void CreatesContainerForBookmarkSections()
    {
        var containers = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().ToList();
        Assert.That(containers.Count, Is.EqualTo(2));
        Assert.That(containers[0].Name, Is.EqualTo("Servers"));
        Assert.That(containers[1].Name, Is.EqualTo("Network"));
    }

    [Test]
    public void DeserializesRdpSession()
    {
        var servers = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var rdp = servers.Children.OfType<ConnectionInfo>().First(c => string.Equals(c.Name, "RDP Server", StringComparison.Ordinal));
        Assert.That(rdp.Hostname, Is.EqualTo("rdphost.example.com"));
        Assert.That(rdp.Port, Is.EqualTo(3389));
        Assert.That(rdp.Username, Is.EqualTo("admin"));
        Assert.That(rdp.Protocol, Is.EqualTo(ProtocolType.RDP));
    }

    [Test]
    public void DeserializesSshSession()
    {
        var servers = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var ssh = servers.Children.OfType<ConnectionInfo>().First(c => string.Equals(c.Name, "SSH Server", StringComparison.Ordinal));
        Assert.That(ssh.Hostname, Is.EqualTo("sshhost.example.com"));
        Assert.That(ssh.Port, Is.EqualTo(22));
        Assert.That(ssh.Username, Is.EqualTo("root"));
        Assert.That(ssh.Protocol, Is.EqualTo(ProtocolType.SSH2));
    }

    [Test]
    public void DeserializesVncSession()
    {
        var servers = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().First();
        var vnc = servers.Children.OfType<ConnectionInfo>().First(c => string.Equals(c.Name, "VNC Server", StringComparison.Ordinal));
        Assert.That(vnc.Hostname, Is.EqualTo("vnchost.example.com"));
        Assert.That(vnc.Port, Is.EqualTo(5900));
        Assert.That(vnc.Protocol, Is.EqualTo(ProtocolType.VNC));
    }

    [Test]
    public void DeserializesTelnetSession()
    {
        var network = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().Last();
        var telnet = network.Children.OfType<ConnectionInfo>().First(c => string.Equals(c.Name, "Telnet Switch", StringComparison.Ordinal));
        Assert.That(telnet.Hostname, Is.EqualTo("switch.example.com"));
        Assert.That(telnet.Port, Is.EqualTo(23));
        Assert.That(telnet.Username, Is.EqualTo("netadmin"));
        Assert.That(telnet.Protocol, Is.EqualTo(ProtocolType.Telnet));
    }

    [Test]
    public void HandlesUnknownProtocolDefaultsToRdp()
    {
        var network = _connectionTreeModel.RootNodes.First().Children.OfType<ContainerInfo>().Last();
        var unknown = network.Children.OfType<ConnectionInfo>().First(c => string.Equals(c.Name, "Unknown Proto", StringComparison.Ordinal));
        Assert.That(unknown.Protocol, Is.EqualTo(ProtocolType.RDP));
    }

    [Test]
    public void HandlesEmptyFile()
    {
        var result = _deserializer.Deserialize("");
        Assert.That(result.RootNodes.Count, Is.GreaterThan(0));
        Assert.That(result.RootNodes.First().Children, Is.Empty);
    }

    [Test]
    public void SessionAtRootLevel_NoContainer()
    {
        const string content = "[Bookmarks]\nSubRep=\nImgNum=42\nMyServer=#91#host.test%3389%user%%%0%0%0\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var root = result.RootNodes.First();
        var conn = root.Children.OfType<ConnectionInfo>().FirstOrDefault(c => string.Equals(c.Name, "MyServer", StringComparison.Ordinal));
        Assert.That(conn, Is.Not.Null);
        Assert.That(conn.Hostname, Is.EqualTo("host.test"));
    }

    [Test]
    public void MalformedSession_NoHashPrefix_ReturnsNull()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nBadEntry=noHashHere\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var container = result.RootNodes.First().Children.OfType<ContainerInfo>().FirstOrDefault();
        Assert.That(container, Is.Not.Null);
        Assert.That(container.Children, Is.Empty);
    }

    [Test]
    public void MalformedSession_SingleHash_ReturnsNull()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nBadEntry=#onlyOneHash\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var container = result.RootNodes.First().Children.OfType<ContainerInfo>().FirstOrDefault();
        Assert.That(container.Children, Is.Empty);
    }

    [Test]
    public void MalformedSession_BadProtocolCode_ReturnsNull()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nBadEntry=#abc#host%22%user\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var container = result.RootNodes.First().Children.OfType<ContainerInfo>().FirstOrDefault();
        Assert.That(container.Children, Is.Empty);
    }

    [Test]
    public void FtpProtocol_MapsToHttp()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nFtpServer=#130#ftp.test%21%user\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var conn = result.RootNodes.First().Children.OfType<ContainerInfo>().First()
            .Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Protocol, Is.EqualTo(ProtocolType.HTTP));
    }

    [Test]
    public void DefaultPort_WhenNotSpecified()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nSshNoPort=#109#host.test%%user\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var conn = result.RootNodes.First().Children.OfType<ContainerInfo>().First()
            .Children.OfType<ConnectionInfo>().First();
        Assert.That(conn.Port, Is.EqualTo(22));
    }

    [Test]
    public void EmptySessionValue_ReturnsNull()
    {
        const string content = "[Bookmarks_1]\nSubRep=Test\nEmpty=\n";
        var result = new MobaXTermSessionDeserializer().Deserialize(content);
        var container = result.RootNodes.First().Children.OfType<ContainerInfo>().FirstOrDefault();
        Assert.That(container.Children, Is.Empty);
    }
}

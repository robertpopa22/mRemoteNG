using mRemoteNG.Config.Serializers.MiscSerializers;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.MiscSerializers;

public class ActiveDirectoryDeserializerTests
{
    [Test]
    public void ImportedComputerKeepsItsOwnHostnameUnderAFolderWithoutOne()
    {
        var folder = new ContainerInfo { Name = "Dynamic AD folder", Hostname = string.Empty };
        ConnectionInfo computer = ActiveDirectoryDeserializer.CreateConnection("server1", "File server", "server1.domain.local");

        folder.AddChild(computer);

        Assert.That(computer.Hostname, Is.EqualTo("server1.domain.local"));
        Assert.That(computer.Inheritance.Hostname, Is.False);
    }

    [Test]
    public void ImportedComputerKeepsItsOwnHostnameUnderAFolderWithOne()
    {
        var folder = new ContainerInfo { Name = "Folder", Hostname = "gateway.domain.local" };
        ConnectionInfo computer = ActiveDirectoryDeserializer.CreateConnection("server1", string.Empty, "server1.domain.local");

        folder.AddChild(computer);

        Assert.That(computer.Hostname, Is.EqualTo("server1.domain.local"));
    }

    [Test]
    public void ImportedComputerStillInheritsTheRestFromTheFolder()
    {
        var folder = new ContainerInfo { Name = "Folder", Username = "admin" };
        ConnectionInfo computer = ActiveDirectoryDeserializer.CreateConnection("server1", "File server", "server1.domain.local");

        folder.AddChild(computer);

        Assert.That(computer.Inheritance.Username, Is.True);
        Assert.That(computer.Username, Is.EqualTo("admin"));
        Assert.That(computer.Description, Is.EqualTo("File server"));
    }
}

using mRemoteNG.Config.Putty;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection;

[NonParallelizable]
public class ConnectionsServiceQuickConnectTests
{
    private string _originalDefaultUsername = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _originalDefaultUsername = DefaultConnectionInfo.Instance.Username;
    }

    [TearDown]
    public void TearDown()
    {
        DefaultConnectionInfo.Instance.Username = _originalDefaultUsername;
    }

    [Test]
    public void CreateQuickConnectUsesExplicitUsernameWhenProvided()
    {
        DefaultConnectionInfo.Instance.Username = "root";
        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);

        ConnectionInfo? quickConnect = connectionsService.CreateQuickConnect("myUser@example-host", ProtocolType.SSH2);

        Assert.That(quickConnect, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(quickConnect!.Hostname, Is.EqualTo("example-host"));
            Assert.That(quickConnect.Username, Is.EqualTo("myUser"));
        });
    }

    [Test]
    public void CreateQuickConnectKeepsDefaultUsernameWhenNoOverrideProvided()
    {
        DefaultConnectionInfo.Instance.Username = "root";
        var connectionsService = new ConnectionsService(PuttySessionsManager.Instance);

        ConnectionInfo? quickConnect = connectionsService.CreateQuickConnect("example-host", ProtocolType.SSH2);

        Assert.That(quickConnect, Is.Not.Null);
        Assert.That(quickConnect!.Username, Is.EqualTo("root"));
    }
}

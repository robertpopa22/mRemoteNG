using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol;

public class ProtocolFeatureTests
{
    [TestCase(ProtocolType.SSH1, true)]
    [TestCase(ProtocolType.SSH2, true)]
    [TestCase(ProtocolType.OpenSSH, true)]
    [TestCase(ProtocolType.RDP, false)]
    [TestCase(ProtocolType.VNC, false)]
    [TestCase(ProtocolType.Telnet, false)]
    public void OffersSshFileTransfer(ProtocolType protocol, bool expected)
    {
        Assert.That(ProtocolFeature.OffersSshFileTransfer(protocol), Is.EqualTo(expected));
    }
}

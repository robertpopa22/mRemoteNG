using mRemoteNG.Connection.Protocol.RDP;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol.RDP;

public class RdpLogonErrorClassificationTests
{
    // -8 is LOGON_MSG_SESSION_BUSY_OPTIONS: the "Session is Busy" dialog. Closing on it left a user
    // unable to get back into a session the server had just dropped.
    [TestCase(-8)]
    [TestCase(-7)]
    [TestCase(-6)]
    [TestCase(-4)]
    [TestCase(-5)]
    [TestCase(-3)]
    [TestCase(-2)]
    [TestCase(-1)]
    public void WinlogonStatesAndDialogsLeaveTheSessionOpen(int lError) =>
        Assert.That(RdpProtocol.IsLogonStateNotification(lError), Is.True);

    [TestCase(0)]                         // LOGON_FAILED_BAD_PASSWORD
    [TestCase(unchecked((int)0xC000006D))] // STATUS_LOGON_FAILURE
    [TestCase(unchecked((int)0xC000006E))] // STATUS_ACCOUNT_RESTRICTION
    [TestCase(0x2000c)]
    [TestCase(-9)]
    public void LogonFailuresStillClose(int lError) =>
        Assert.That(RdpProtocol.IsLogonStateNotification(lError), Is.False);
}

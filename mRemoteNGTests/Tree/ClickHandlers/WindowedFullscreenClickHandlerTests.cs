using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;
using mRemoteNG.Tree.ClickHandlers;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Tree.ClickHandlers;

[TestFixture]
public class WindowedFullscreenClickHandlerTests
{
    [Test]
    public void OpensRdpWithoutForceNewEvenWhenDoubleClickWouldDuplicate()
    {
        var initiator = Substitute.For<IConnectionInitiator>();
        var handler = new WindowedFullscreenClickHandler(initiator);
        var node = new ConnectionInfo { Protocol = ProtocolType.RDP };
        bool previous = mRemoteNG.Properties.Settings.Default.DoubleClickOpensNewConnection;
        try
        {
            mRemoteNG.Properties.Settings.Default.DoubleClickOpensNewConnection = true;
            handler.Execute(node);
            initiator.Received(1).OpenConnection(node, ConnectionInfo.Force.WindowedFullscreen);
        }
        finally
        {
            mRemoteNG.Properties.Settings.Default.DoubleClickOpensNewConnection = previous;
        }
    }

    [Test]
    public void RefusesFoldersTemplatesAndOtherProtocols()
    {
        var initiator = Substitute.For<IConnectionInitiator>();
        var handler = new WindowedFullscreenClickHandler(initiator);
        handler.Execute(new ContainerInfo { Hostname = "example.invalid", Protocol = ProtocolType.RDP });
        handler.Execute(new ConnectionInfo { IsTemplate = true, Protocol = ProtocolType.RDP });
        handler.Execute(new ConnectionInfo { Protocol = ProtocolType.SSH2 });
        Assert.That(initiator.ReceivedCalls(), Is.Empty);
    }

    [TestCase(MouseButtons.Left, 1, Keys.Alt, true)]
    [TestCase(MouseButtons.Left, 2, Keys.Alt, false)]
    [TestCase(MouseButtons.Right, 1, Keys.Alt, false)]
    [TestCase(MouseButtons.Left, 1, Keys.None, false)]
    [TestCase(MouseButtons.Left, 1, Keys.Alt | Keys.Control, false)]
    [TestCase(MouseButtons.Left, 1, Keys.Shift, false)]
    public void RecognizesOnlyUnambiguousAltLeftClick(MouseButtons button, int clicks, Keys modifiers, bool expected)
    {
        Assert.That(WindowedFullscreenClickHandler.IsGesture(button, clicks, modifiers), Is.EqualTo(expected));
    }
}

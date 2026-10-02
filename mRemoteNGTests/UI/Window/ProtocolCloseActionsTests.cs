using System;
using mRemoteNG.UI.Window;
using NUnit.Framework;

namespace mRemoteNGTests.UI.Window;

public class ProtocolCloseActionsTests
{
    [Test]
    public void ClosingADisposedTabDoesNotSurfaceObjectDisposedException()
    {
        Assert.DoesNotThrow(() => ProtocolCloseActions.CloseTab(
            () => throw new ObjectDisposedException("ConnectionTab")));
    }

    [Test]
    public void ClosingALiveTabRunsTheClose()
    {
        bool closed = false;
        ProtocolCloseActions.CloseTab(() => closed = true);
        Assert.That(closed, Is.True);
    }
}

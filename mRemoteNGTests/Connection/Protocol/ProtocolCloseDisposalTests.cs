using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol;

/// <summary>
/// Closing a connection has to end with the protocol disposed, because that is the only thing
/// that releases what the protocol holds. For RDP that is the MSTSC ActiveX object, hundreds of
/// megabytes of it: <c>RdpProtocol.Dispose</c> is what calls <c>CleanupResources</c>, which
/// releases the COM object, drops the message filter and unhooks the control. Nothing else does.
///
/// #182 reports roughly 300 MB retained per RDP session and about 2 GB still held after eight
/// sessions were opened and closed with every tab and panel shut. These tests drive the close
/// path with a stand-in protocol, so they need no RDP server and no ActiveX control, and they
/// fail if the path stops reaching Dispose.
/// </summary>
[TestFixture]
[SupportedOSPlatform("windows")]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class ProtocolCloseDisposalTests
{
    private sealed class SpyProtocol : ProtocolBase
    {
        public bool Disposed { get; private set; }

        /// <summary>Stands in for the control a real protocol builds in Initialize.</summary>
        public void HostControl(Control control) => Control = control;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                Disposed = true;
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Builds what a live connection looks like: a panel standing in for the tab, an
    /// InterfaceControl inside it, and the protocol's own control inside that.
    /// </summary>
    private static (SpyProtocol Protocol, InterfaceControl Interface, Panel Tab) BuildOpenConnection()
    {
        Panel tab = new() { Size = new System.Drawing.Size(400, 300) };
        SpyProtocol protocol = new();
        InterfaceControl ic = new(tab, protocol, new ConnectionInfo())
        {
            OriginalInfo = new ConnectionInfo()
        };
        tab.Tag = ic;
        protocol.InterfaceControl = ic;
        return (protocol, ic, tab);
    }

    private static bool DisposedWithin(SpyProtocol protocol, TimeSpan timeout)
    {
        // Close() hands the work to its own STA thread, so the assertion has to wait for it.
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed < timeout)
        {
            if (protocol.Disposed)
                return true;
            Application.DoEvents();
            Thread.Sleep(20);
        }

        return protocol.Disposed;
    }

    [Test]
    public void ClosingAConnectionDisposesTheProtocol()
    {
        (SpyProtocol protocol, InterfaceControl ic, Panel tab) = BuildOpenConnection();
        using (tab)
        {
            protocol.Close();

            Assert.That(DisposedWithin(protocol, TimeSpan.FromSeconds(5)), Is.True,
                        "closing a connection left the protocol undisposed, so whatever it holds — for RDP "
                        + "the MSTSC ActiveX object — is never released");
            Assert.That(ic.IsDisposed, Is.True, "the interface control outlived the connection");
        }
    }

    [Test]
    public void ClosingAConnectionWhoseTabIsAlreadyGoneStillDisposesTheProtocol()
    {
        // The #182 path. Shutting a tab takes the InterfaceControl out of its parent first and
        // the protocol is closed afterwards, so by the time the close runs there is no parent
        // left. The close used to return at that point, before disposing anything — the protocol,
        // its ActiveX object and the whole InterfaceControl stayed alive for the life of the
        // process. It is also the busiest path there is: it happens on every tab close.
        (SpyProtocol protocol, InterfaceControl ic, Panel tab) = BuildOpenConnection();
        using (tab)
        {
            tab.Controls.Remove(ic);
            Assert.That(ic.Parent, Is.Null, "precondition: the tab is gone before the protocol closes");

            protocol.Close();

            Assert.That(DisposedWithin(protocol, TimeSpan.FromSeconds(5)), Is.True,
                        "a connection whose tab closed first never disposed its protocol — this is the "
                        + "leak in #182, and it is the ordinary close path, not an edge case");
            Assert.That(ic.IsDisposed, Is.True, "the interface control outlived the connection");
        }
    }

    [Test]
    public void ClosingAConnectionClearsTheTabTagThatPointsBackAtIt()
    {
        // The tab keeps the InterfaceControl in its Tag, and the InterfaceControl holds the
        // protocol. A tab that outlives the connection therefore keeps the whole graph alive
        // through that one reference, however well everything else is disposed.
        (SpyProtocol protocol, InterfaceControl ic, Panel tab) = BuildOpenConnection();
        using (tab)
        {
            Assert.That(tab.Tag, Is.SameAs(ic), "precondition: the tab points at the interface control");

            protocol.Close();
            DisposedWithin(protocol, TimeSpan.FromSeconds(5));

            Assert.That(tab.Tag, Is.Null,
                        "the tab still points at the closed connection, so nothing it holds can be collected");
        }
    }

    [Test]
    public void ClosingAConnectionDisposesTheProtocolsOwnControl()
    {
        // The protocol's control is the ActiveX host for RDP. Leaving it undisposed keeps the
        // native window and the COM object it hosts.
        (SpyProtocol protocol, InterfaceControl ic, Panel tab) = BuildOpenConnection();
        using (tab)
        {
            Control hosted = new();
            ic.Controls.Add(hosted);
            protocol.HostControl(hosted);

            protocol.Close();
            DisposedWithin(protocol, TimeSpan.FromSeconds(5));

            Assert.That(hosted.IsDisposed, Is.True,
                        "the protocol's control survived the close — for RDP that is the ActiveX host");
        }
    }
}

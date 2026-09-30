using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using MSTSCLib;

namespace mRemoteNG.Connection.Protocol.RDP;

/// <summary>Owns one event subscription, never the RDP ActiveX object itself.</summary>
[SupportedOSPlatform("windows")]
internal sealed class RdpEventConnection : IDisposable
{
    private IConnectionPoint? _point;
    private readonly int _cookie;

    public RdpEventConnection(object source, IMsTscAxEvents sink)
    {
        Guid events = typeof(IMsTscAxEvents).GUID;
        ((IConnectionPointContainer)source).FindConnectionPoint(ref events, out var point);
        if (point == null) throw new InvalidOperationException("RDP event connection point unavailable.");
        try
        {
            // The COM marshaler balances its temporary reference to the sink. WinForms
            // 10.0.11 ConnectionPointCookie obtains an IUnknown for it without releasing it,
            // leaving the entire disposed AxHost rooted even after Unadvise succeeds.
            point.Advise(sink, out _cookie);
            _point = point;
        }
        catch
        {
            Marshal.ReleaseComObject(point);
            throw;
        }
    }

    public void Dispose()
    {
        var point = _point;
        if (point == null) return;
        _point = null;
        try { point.Unadvise(_cookie); }
        finally { Marshal.ReleaseComObject(point); }
    }
}

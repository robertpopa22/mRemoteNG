using AxMSTSCLib;
using System.Runtime.Versioning;

namespace mRemoteNG.Connection.Protocol.RDP;

[SupportedOSPlatform("windows")]
internal static class RdpActiveXHosts
{
    internal sealed class Client6 : AxMsRdpClient6NotSafeForScripting
    {
        private RdpEventConnection? _events;
        protected override void CreateSink()
        {
            DetachSink();
            _events = new(GetOcx()!, new AxMsRdpClient6NotSafeForScriptingEventMulticaster(this));
        }
        protected override void DetachSink()
        {
            var events = _events;
            _events = null;
            events?.Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing) DetachSink(); }
            finally { base.Dispose(disposing); }
        }
    }


    internal sealed class Client8 : AxMsRdpClient8NotSafeForScripting
    {
        private RdpEventConnection? _events;
        protected override void CreateSink()
        {
            DetachSink();
            _events = new(GetOcx()!, new AxMsRdpClient8NotSafeForScriptingEventMulticaster(this));
        }
        protected override void DetachSink()
        {
            var events = _events;
            _events = null;
            events?.Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing) DetachSink(); }
            finally { base.Dispose(disposing); }
        }
    }

    internal sealed class Client9 : AxMsRdpClient9NotSafeForScripting
    {
        private RdpEventConnection? _events;
        protected override void CreateSink()
        {
            DetachSink();
            _events = new(GetOcx()!, new AxMsRdpClient9NotSafeForScriptingEventMulticaster(this));
        }
        protected override void DetachSink()
        {
            var events = _events;
            _events = null;
            events?.Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing) DetachSink(); }
            finally { base.Dispose(disposing); }
        }
    }


    internal sealed class Client11 : AxMsRdpClient11NotSafeForScripting
    {
        private RdpEventConnection? _events;
        protected override void CreateSink()
        {
            DetachSink();
            _events = new(GetOcx()!, new AxMsRdpClient11NotSafeForScriptingEventMulticaster(this));
        }
        protected override void DetachSink()
        {
            var events = _events;
            _events = null;
            events?.Dispose();
        }
        protected override void Dispose(bool disposing)
        {
            try { if (disposing) DetachSink(); }
            finally { base.Dispose(disposing); }
        }
    }

}

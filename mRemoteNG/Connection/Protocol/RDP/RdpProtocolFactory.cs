using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.App;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    public class RdpProtocolFactory
    {
        public RdpProtocol Build(RdpVersion rdpVersion)
        {
            switch (rdpVersion)
            {
                case RdpVersion.Highest:
                    return BuildHighestSupportedVersion();
                case RdpVersion.Rdc6:
                    return new RdpProtocol();
                case RdpVersion.Rdc7:
                    return new RdpProtocol7();
                case RdpVersion.Rdc8:
                    return new RdpProtocol8();
                case RdpVersion.Rdc9:
                    return new RdpProtocol9();
                case RdpVersion.Rdc10:
                    return new RdpProtocol10();
                case RdpVersion.Rdc11:
                    return new RdpProtocol11();
                default:
                    throw new ArgumentOutOfRangeException(nameof(rdpVersion), rdpVersion, null);
            }
        }

        private RdpProtocol BuildHighestSupportedVersion()
        {
            IEnumerable<RdpVersion> versions = Enum.GetValues<RdpVersion>()
                .Except(new[] { RdpVersion.Highest })
                .Reverse();

            foreach (RdpVersion version in versions)
            {
                // Each probe creates and destroys a whole ActiveX control. On one laptop the first
                // probe of a process took 13.7 s (rdp_open protocol_ms, first_control=true) while
                // later opens took about 0.5 s. Whether that is the DLL load or the COM activation
                // is what this line separates.
                bool moduleLoaded = NativeMethods.GetModuleHandle("mstscax.dll") != IntPtr.Zero;
                Stopwatch probe = Stopwatch.StartNew();
                RdpProtocol rdp = Build(version);
                bool supported = rdp.RdpVersionSupported();
                RuntimeDiagnostics.RdpProbe(version.ToString(), supported, probe.ElapsedMilliseconds, moduleLoaded);
                if (supported)
                    return rdp;
            }

            throw new InvalidOperationException("No supported RDP version found.");
        }

        public IList<RdpVersion> GetSupportedVersions()
        {
            IEnumerable<RdpVersion> versions = Enum.GetValues<RdpVersion>()
                .Except(new[] { RdpVersion.Highest });

            List<RdpVersion> supportedVersions = new();
            foreach (RdpVersion version in versions)
            {
                if (Build(version).RdpVersionSupported())
                    supportedVersions.Add(version);
            }

            return supportedVersions;
        }
    }
}

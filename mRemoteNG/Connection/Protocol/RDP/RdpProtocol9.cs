using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    public class RdpProtocol9 : RdpProtocol8
    {
        private MsRdpClient9NotSafeForScripting RdpClient9 => (MsRdpClient9NotSafeForScripting)((AxHost)Control).GetOcx();

        protected override RdpVersion RdpProtocolVersion => RDP.RdpVersion.Rdc9;

        // Constructor not needed - ResizeEnd is already registered in RdpProtocol8 base class

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;

            return PostInitialize();
        }

        public override async System.Threading.Tasks.Task<bool> InitializeAsync()
        {
            if (!await base.InitializeAsync())
                return false;

            return PostInitialize();
        }

        private bool PostInitialize()
        {
            if (RdpVersion < Versions.RDC81) return false; // minimum dll version checked, loaded MSTSCLIB dll version is not capable

            return true;
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            return new AxMsRdpClient9NotSafeForScripting();
        }

        protected override void UpdateSessionDisplaySettings(uint width, uint height)
        {
            try
            {
                if (RdpClient9 != null)
                {
                    RdpClient9.UpdateSessionDisplaySettings(width, height, width, height, Orientation, DesktopScaleFactor, DeviceScaleFactor);
                }
                else
                {
                    base.UpdateSessionDisplaySettings(width, height);
                }
            }
            catch (Exception)
            {
                // target OS does not support newer method, fallback to an older method
                base.UpdateSessionDisplaySettings(width, height);
            }
        }

    }
}
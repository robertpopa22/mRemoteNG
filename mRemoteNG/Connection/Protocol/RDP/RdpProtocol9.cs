using System;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using AxMSTSCLib;
using mRemoteNG.App;
using mRemoteNG.Messages;
using MSTSCLib;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    public class RdpProtocol9 : RdpProtocol8
    {
        private MsRdpClient9NotSafeForScripting? RdpClient9 => (Control as AxHost)?.GetOcx() as MsRdpClient9NotSafeForScripting;

        protected override RdpVersion RdpProtocolVersion => RDP.RdpVersion.Rdc9;

        // Constructor not needed - resize handlers are wired by ProtocolBase via ConnectionTab events.

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
            return new RdpActiveXHosts.Client9();
        }

        protected override bool SupportsDynamicResize => true;

        protected override void UpdateSessionDisplaySettings(uint width, uint height)
        {
            try
            {
                if (RdpClient9 != null)
                {
                    if (WindowedFullscreenSizingActive)
                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                            $"phase=windowed_fullscreen_dynamic_call requested={width}x{height}");
                    RdpClient9.UpdateSessionDisplaySettings(width, height, width, height, Orientation, DesktopScaleFactor, DeviceScaleFactor);
                    ClearWindowedFullscreenDynamicRetry();
                    if (WindowedFullscreenSizingActive)
                    {
                        var desktop = WindowedFullscreenDesktopSize;
                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                            $"phase=windowed_fullscreen_dynamic_return requested={width}x{height} desktop={desktop.Width}x{desktop.Height}");
                    }
                }
                else
                {
                    base.UpdateSessionDisplaySettings(width, height);
                }
            }
            catch (Exception ex)
            {
                if (WindowedFullscreenSizingActive && loginComplete &&
                    ex is COMException && ex.HResult == unchecked((int)0x8000FFFF))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                        $"phase=windowed_fullscreen_dynamic_not_ready requested={width}x{height} " +
                        $"type={ex.GetType().Name} hresult=0x{ex.HResult:X8}");
                    ScheduleWindowedFullscreenDynamicRetry(width, height);
                    return;
                }

                // target OS does not support newer method, fallback to an older method
                Runtime.MessageCollector.AddMessage(
                    WindowedFullscreenSizingActive ? MessageClass.InformationMsg : MessageClass.DebugMsg,
                    $"phase=windowed_fullscreen_dynamic_error requested={width}x{height} " +
                    $"type={ex.GetType().Name} hresult=0x{ex.HResult:X8} message={ex.Message}");
                base.UpdateSessionDisplaySettings(width, height);
            }
        }

    }
}

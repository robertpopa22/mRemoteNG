using System;
using System.Threading;
using System.ComponentModel;
using System.Net.Sockets;
using System.Reflection;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Tools;
using mRemoteNG.UI.Forms;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using mRemoteNG.Security;
using System.Runtime.ExceptionServices;

// ReSharper disable ArrangeAccessorOwnerBody


namespace mRemoteNG.Connection.Protocol.VNC
{
    /// <summary>
    /// Intercepts lock-key messages (Caps Lock, Num Lock, Scroll Lock) destined for the
    /// VncSharpCore RemoteDesktop control and sends the correct X11 keysyms instead.
    /// Without this filter, VncSharpCore's ToAscii fallback mistranslates these keys
    /// (e.g., Caps Lock → lowercase 't'). See GitHub issue #227.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class VncLockKeyFilter : IMessageFilter
    {
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        private const int VK_CAPITAL = 0x14;   // Caps Lock
        private const int VK_NUMLOCK = 0x90;   // Num Lock
        private const int VK_SCROLL = 0x91;    // Scroll Lock

        // X11 keysym values for lock keys
        private const uint XK_Caps_Lock = 0xFFE5;
        private const uint XK_Num_Lock = 0xFF7F;
        private const uint XK_Scroll_Lock = 0xFF14;

        private readonly VncSharpCore.RemoteDesktop _remoteDesktop;
        private readonly FieldInfo? _vncField;
        private MethodInfo? _writeKeyboardEvent;

        public VncLockKeyFilter(VncSharpCore.RemoteDesktop remoteDesktop)
        {
            _remoteDesktop = remoteDesktop;
            _vncField = typeof(VncSharpCore.RemoteDesktop)
                .GetField("vnc", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public bool PreFilterMessage(ref Message m)
        {
            // Only intercept key messages
            if (m.Msg != WM_KEYDOWN && m.Msg != WM_KEYUP &&
                m.Msg != WM_SYSKEYDOWN && m.Msg != WM_SYSKEYUP)
                return false;

            int vk = m.WParam.ToInt32();

            // Only intercept lock keys
            uint keysym;
            switch (vk)
            {
                case VK_CAPITAL:    keysym = XK_Caps_Lock;   break;
                case VK_NUMLOCK:    keysym = XK_Num_Lock;    break;
                case VK_SCROLL:     keysym = XK_Scroll_Lock; break;
                default: return false;
            }

            // Only intercept when targeted at the VNC RemoteDesktop control
            if (m.HWnd != _remoteDesktop.Handle)
                return false;

            // Resolve the VncClient on each call (it's created during Connect)
            var vncClient = _vncField?.GetValue(_remoteDesktop);
            if (vncClient == null)
                return false;

            _writeKeyboardEvent ??= vncClient.GetType()
                .GetMethod("WriteKeyboardEvent", BindingFlags.Public | BindingFlags.Instance);
            if (_writeKeyboardEvent == null)
                return false;

            // Send the correct keysym via the VNC protocol
            bool pressed = m.Msg == WM_KEYDOWN || m.Msg == WM_SYSKEYDOWN;
            try
            {
                _writeKeyboardEvent.Invoke(vncClient, new object[] { keysym, pressed });
            }
            catch
            {
                // If reflection call fails, let the message through (degraded behavior)
                return false;
            }

            // Suppress the original message so VncSharpCore doesn't mistranslate it
            return true;
        }
    }

    [SupportedOSPlatform("windows")]
    public class ProtocolVNC : ProtocolBase
    {
        #region Private Declarations

        private VncSharpCore.RemoteDesktop? _vnc;
        private ConnectionInfo? _info;
        private VncLockKeyFilter? _lockKeyFilter;
        private static volatile bool _isConnectionSuccessful;
        private static ExceptionDispatchInfo? _socketexception;
        private static readonly ManualResetEvent TimeoutObject = new(false);
        private static readonly object _testConnectLock = new();

        #endregion

        #region Public Methods

        public ProtocolVNC()
        {
            Control = new VncSharpCore.RemoteDesktop();
        }

        public override bool Initialize()
        {
            base.Initialize();

            try
            {
                _vnc = Control as VncSharpCore.RemoteDesktop;
                _info = InterfaceControl.Info;
                if (_vnc == null || _info == null) return false;
                _vnc.VncPort = _info.Port;

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.VncSetPropsFailed + Environment.NewLine + ex.Message,
                                                    true);
                return false;
            }
        }
 
        public override bool Connect()
        {
            SetEventHandlers();
            try
            {
                if (_vnc == null || _info == null) return false;
                if (TestConnect(_info.Hostname, _info.Port, 500))
                    _vnc.Connect(_info.Hostname, _info.VNCViewOnly, _info.VNCSmartSizeMode != SmartSizeMode.SmartSNo);

                // Install the lock-key filter after Connect() creates the VncClient.
                // Fixes Caps Lock sending 't' instead of toggle (issue #227).
                _lockKeyFilter = new VncLockKeyFilter(_vnc);
                Application.AddMessageFilter(_lockKeyFilter);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.ConnectionOpenFailed + Environment.NewLine +
                                                    ex.Message);
                return false;
            }

            return true;
        }

        public override void Disconnect()
        {
            try
            {
                if (_lockKeyFilter != null)
                {
                    Application.RemoveMessageFilter(_lockKeyFilter);
                    _lockKeyFilter = null;
                }

                _vnc?.Disconnect();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.VncConnectionDisconnectFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        public void SendSpecialKeys(SpecialKeys Keys)
        {
            try
            {
                if (_vnc == null) return;
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (Keys)
                {
                    case SpecialKeys.CtrlAltDel:
                        _vnc.SendSpecialKeys(VncSharpCore.SpecialKeys.CtrlAltDel);
                        break;
                    case SpecialKeys.CtrlEsc:
                        _vnc.SendSpecialKeys(VncSharpCore.SpecialKeys.CtrlEsc);
                        break;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.VncSendSpecialKeysFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        public void StartChat()
        {
            Runtime.MessageCollector.AddMessage(Messages.MessageClass.InformationMsg,
                                                "VNC chat is not supported by the current VNC library.");
        }

        public void StartFileTransfer()
        {
            Runtime.MessageCollector.AddMessage(Messages.MessageClass.InformationMsg,
                                                "VNC file transfer is not supported by the current VNC library.");
        }

        public void RefreshScreen()
        {
            try
            {
                _vnc?.FullScreenUpdate();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.VncRefreshFailed + Environment.NewLine + ex.Message,
                                                    true);
            }
        }

        #endregion

        #region Private Methods

        private void SetEventHandlers()
        {
            try
            {
                if (_vnc == null) return;
                _vnc.ConnectComplete += VNCEvent_Connected;
                _vnc.ConnectionLost += VNCEvent_Disconnected;
                FrmMain.ClipboardChanged += VNCEvent_ClipboardChanged;
                if (!Force.HasFlag(ConnectionInfo.Force.NoCredentials) && _info?.Password?.Length > 0)
                {
                    _vnc.GetPassword = VNCEvent_Authenticate;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.ErrorMsg,
                                                    Language.VncSetEventHandlersFailed + Environment.NewLine +
                                                    ex.Message, true);
            }
        }

        private static bool TestConnect(string hostName, int port, int timeoutMSec)
        {
            lock (_testConnectLock)
            {
                _socketexception = null;
                TcpClient tcpclient = new();

                TimeoutObject.Reset();
                tcpclient.BeginConnect(hostName, port, CallBackMethod, tcpclient);

                if (TimeoutObject.WaitOne(timeoutMSec, false))
                {
                    if (_isConnectionSuccessful) return true;
                    // Connection completed but failed - tcpclient will be closed in CallBackMethod's finally block
                    if (_socketexception != null)
                    {
                        _socketexception.Throw();
                    }
                }
                else
                {
                    tcpclient.Close();
                    throw new TimeoutException($"Connection timed out to host " + hostName + " on port " + port);
                }

                return false;
            }
        }

        private static void CallBackMethod(IAsyncResult asyncresult)
        {
            TcpClient? tcpclient = null;
            try
            {
                _isConnectionSuccessful = false;
                tcpclient = asyncresult.AsyncState as TcpClient;

                if (tcpclient?.Client == null) return;

                tcpclient.EndConnect(asyncresult);
                _isConnectionSuccessful = true;
            }
            catch (Exception ex)
            {
                _isConnectionSuccessful = false;
                _socketexception = ExceptionDispatchInfo.Capture(ex);
            }
            finally
            {
                tcpclient?.Close();
                TimeoutObject.Set();
            }
        }

        #endregion

        #region Private Events & Handlers

        private void VNCEvent_Connected(object sender, EventArgs e)
        {
            Event_Connected(this);
            if (_vnc != null && _info != null)
                _vnc.AutoScroll = _info.VNCSmartSizeMode == SmartSizeMode.SmartSNo;
        }

        private void VNCEvent_Disconnected(object sender, EventArgs e)
        {
            if (_lockKeyFilter != null)
            {
                Application.RemoveMessageFilter(_lockKeyFilter);
                _lockKeyFilter = null;
            }

            FrmMain.ClipboardChanged -= VNCEvent_ClipboardChanged;
            Event_Disconnected(this, @"VncSharp Disconnected.", null);
            Close();
        }

        private void VNCEvent_ClipboardChanged()
        {
            _vnc?.FillServerClipboard();
        }

        private string VNCEvent_Authenticate()
        {
            //return _info.Password.ConvertToUnsecureString();
            return _info?.Password ?? string.Empty;
        }

        #endregion

        #region Enums

        public enum Defaults
        {
            Port = 5900
        }

        public enum SpecialKeys
        {
            CtrlAltDel,
            CtrlEsc
        }

        public enum Compression
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.NoCompression))]
            CompNone = 99,
            [Description("0")] Comp0 = 0,
            [Description("1")] Comp1 = 1,
            [Description("2")] Comp2 = 2,
            [Description("3")] Comp3 = 3,
            [Description("4")] Comp4 = 4,
            [Description("5")] Comp5 = 5,
            [Description("6")] Comp6 = 6,
            [Description("7")] Comp7 = 7,
            [Description("8")] Comp8 = 8,
            [Description("9")] Comp9 = 9
        }

        public enum Encoding
        {
            [Description("Raw")] EncRaw,
            [Description("RRE")] EncRRE,
            [Description("CoRRE")] EncCorre,
            [Description("Hextile")] EncHextile,
            [Description("Zlib")] EncZlib,
            [Description("Tight")] EncTight,
            [Description("ZlibHex")] EncZLibHex,
            [Description("ZRLE")] EncZRLE
        }

        public enum AuthMode
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.Vnc))]
            AuthVNC,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.Windows))]
            AuthWin
        }

        public enum ProxyType
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.None))]
            ProxyNone,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.Http))]
            ProxyHTTP,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.Socks5))]
            ProxySocks5,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.UltraVncRepeater))]
            ProxyUltra
        }

        public enum Colors
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.Normal))]
            ColNormal,
            [Description("8-bit")] Col8Bit
        }

        public enum SmartSizeMode
        {
            [LocalizedAttributes.LocalizedDescription(nameof(Language.NoSmartSize))]
            SmartSNo,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.Free))]
            SmartSFree,

            [LocalizedAttributes.LocalizedDescription(nameof(Language.Aspect))]
            SmartSAspect
        }

        #endregion
    }
}

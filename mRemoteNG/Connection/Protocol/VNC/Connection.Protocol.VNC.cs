using System;
using System.Threading;
using System.Threading.Tasks;
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
        private const int WM_CHAR = 0x0102;
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
            // Only intercept key messages including WM_CHAR
            if (m.Msg != WM_KEYDOWN && m.Msg != WM_KEYUP &&
                m.Msg != WM_SYSKEYDOWN && m.Msg != WM_SYSKEYUP &&
                m.Msg != WM_CHAR)
                return false;

            // Handle lock keys (existing logic)
            if (m.Msg != WM_CHAR)
            {
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
            else // WM_CHAR
            {
                // Handle Cyrillic
                char c = (char)m.WParam.ToInt32();
                uint keysym = GetCyrillicKeysym(c);
                if (keysym == 0) return false;

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

                try
                {
                    // For WM_CHAR, we simulate a full key press (down then up)
                    _writeKeyboardEvent.Invoke(vncClient, new object[] { keysym, true });
                    _writeKeyboardEvent.Invoke(vncClient, new object[] { keysym, false });
                }
                catch
                {
                    return false;
                }

                return true;
            }
        }

        private static uint GetCyrillicKeysym(char c)
        {
            // X11 keysyms for Cyrillic (from keysymdef.h).
            // IMPORTANT: X11 keysyms follow JCUKEN keyboard order, NOT Unicode order,
            // so a linear offset formula does NOT work. Use explicit lookup.
            return c switch
            {
                // Lowercase (U+0430–U+044F)
                'а' => 0x06C1, // XK_Cyrillic_a
                'б' => 0x06C2, // XK_Cyrillic_be
                'в' => 0x06D7, // XK_Cyrillic_ve
                'г' => 0x06C7, // XK_Cyrillic_ghe
                'д' => 0x06C4, // XK_Cyrillic_de
                'е' => 0x06C5, // XK_Cyrillic_ie
                'ж' => 0x06D6, // XK_Cyrillic_zhe
                'з' => 0x06DA, // XK_Cyrillic_ze
                'и' => 0x06C9, // XK_Cyrillic_i
                'й' => 0x06CA, // XK_Cyrillic_shorti
                'к' => 0x06CB, // XK_Cyrillic_ka
                'л' => 0x06CC, // XK_Cyrillic_el
                'м' => 0x06CD, // XK_Cyrillic_em
                'н' => 0x06CE, // XK_Cyrillic_en
                'о' => 0x06CF, // XK_Cyrillic_o
                'п' => 0x06D0, // XK_Cyrillic_pe
                'р' => 0x06D2, // XK_Cyrillic_er
                'с' => 0x06D3, // XK_Cyrillic_es
                'т' => 0x06D4, // XK_Cyrillic_te
                'у' => 0x06D5, // XK_Cyrillic_u
                'ф' => 0x06C6, // XK_Cyrillic_ef
                'х' => 0x06C8, // XK_Cyrillic_ha
                'ц' => 0x06C3, // XK_Cyrillic_tse
                'ч' => 0x06DE, // XK_Cyrillic_che
                'ш' => 0x06DB, // XK_Cyrillic_sha
                'щ' => 0x06DD, // XK_Cyrillic_shcha
                'ъ' => 0x06DF, // XK_Cyrillic_hardsign
                'ы' => 0x06D9, // XK_Cyrillic_yeru
                'ь' => 0x06D8, // XK_Cyrillic_softsign
                'э' => 0x06DC, // XK_Cyrillic_e
                'ю' => 0x06C0, // XK_Cyrillic_yu
                'я' => 0x06D1, // XK_Cyrillic_ya
                // Uppercase (U+0410–U+042F)
                'А' => 0x06E1, // XK_Cyrillic_A
                'Б' => 0x06E2, // XK_Cyrillic_BE
                'В' => 0x06F7, // XK_Cyrillic_VE
                'Г' => 0x06E7, // XK_Cyrillic_GHE
                'Д' => 0x06E4, // XK_Cyrillic_DE
                'Е' => 0x06E5, // XK_Cyrillic_IE
                'Ж' => 0x06F6, // XK_Cyrillic_ZHE
                'З' => 0x06FA, // XK_Cyrillic_ZE
                'И' => 0x06E9, // XK_Cyrillic_I
                'Й' => 0x06EA, // XK_Cyrillic_SHORTI
                'К' => 0x06EB, // XK_Cyrillic_KA
                'Л' => 0x06EC, // XK_Cyrillic_EL
                'М' => 0x06ED, // XK_Cyrillic_EM
                'Н' => 0x06EE, // XK_Cyrillic_EN
                'О' => 0x06EF, // XK_Cyrillic_O
                'П' => 0x06F0, // XK_Cyrillic_PE
                'Р' => 0x06F2, // XK_Cyrillic_ER
                'С' => 0x06F3, // XK_Cyrillic_ES
                'Т' => 0x06F4, // XK_Cyrillic_TE
                'У' => 0x06F5, // XK_Cyrillic_U
                'Ф' => 0x06E6, // XK_Cyrillic_EF
                'Х' => 0x06E8, // XK_Cyrillic_HA
                'Ц' => 0x06E3, // XK_Cyrillic_TSE
                'Ч' => 0x06FE, // XK_Cyrillic_CHE
                'Ш' => 0x06FB, // XK_Cyrillic_SHA
                'Щ' => 0x06FD, // XK_Cyrillic_SHCHA
                'Ъ' => 0x06FF, // XK_Cyrillic_HARDSIGN
                'Ы' => 0x06F9, // XK_Cyrillic_YERU
                'Ь' => 0x06F8, // XK_Cyrillic_SOFTSIGN
                'Э' => 0x06FC, // XK_Cyrillic_E
                'Ю' => 0x06E0, // XK_Cyrillic_YU
                'Я' => 0x06F1, // XK_Cyrillic_YA
                // Special characters
                'ё' => 0x06A3, // XK_Cyrillic_io
                'Ё' => 0x06B3, // XK_Cyrillic_IO
                _ => 0
            };
        }
    }

    [SupportedOSPlatform("windows")]
    public class ProtocolVNC : ProtocolBase
    {
        #region Private Declarations

        private const int VncConnectTimeoutMs = 10_000;
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

                // Apply VNC color depth (issue #640).
                // VncSharpCore uses BitsPerPixel (8/16/32) and Depth (3/6/8/16).
                // When ColNormal (0), leave defaults so the server's native format is used.
                var bpp = (int)_info.VNCColors;
                if (bpp > 0)
                {
                    _vnc.BitsPerPixel = bpp;
                    _vnc.Depth = bpp switch
                    {
                        8 => 8,
                        16 => 16,
                        32 => 24, // 32 bpp with 24-bit true color depth
                        _ => 0
                    };
                }

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
                if (TestConnect(_info.Hostname, _info.Port, 5000))
                    ConnectWithTimeout(_vnc, _info, VncConnectTimeoutMs);

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

        /// <summary>
        /// Runs the blocking VncSharpCore Connect() on a background thread with a timeout
        /// so that unreachable VNC servers fail fast instead of freezing the UI (issue #636).
        /// </summary>
        private static void ConnectWithTimeout(VncSharpCore.RemoteDesktop vnc, ConnectionInfo info, int timeoutMs)
        {
            Exception? connectException = null;
            var connectTask = Task.Run(() =>
            {
                try
                {
                    vnc.Connect(info.Hostname, info.VNCViewOnly,
                                info.VNCSmartSizeMode != SmartSizeMode.SmartSNo);
                }
                catch (Exception ex)
                {
                    connectException = ex;
                }
            });

            if (!connectTask.Wait(timeoutMs))
            {
                // Timed out — force-disconnect to unblock the background thread
                try { vnc.Disconnect(); } catch { /* best-effort cleanup */ }
                throw new TimeoutException(
                    $"VNC connection to {info.Hostname}:{info.Port} timed out after {timeoutMs / 1000} seconds.");
            }

            if (connectException != null)
                ExceptionDispatchInfo.Capture(connectException).Throw();
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
            ColNormal = 0,
            [Description("8-bit")] Col8Bit = 8,
            [Description("16-bit")] Col16Bit = 16,
            [Description("32-bit")] Col32Bit = 32
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

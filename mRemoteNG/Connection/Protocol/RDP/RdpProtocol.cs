using AxMSTSCLib;
using System.Drawing;
using System.Text;
using mRemoteNG.App;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Resources.Language;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Tabs;
using MSTSCLib;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    public class RdpProtocol : ProtocolBase, ISupportsViewOnly
    {
        /* RDP v8 requires Windows 7 with:
         * https://support.microsoft.com/en-us/kb/2592687
         * OR
         * https://support.microsoft.com/en-us/kb/2923545
         *
         * Windows 8+ support RDP v8 out of the box.
         */

        private MsRdpClient6NotSafeForScripting _rdpClient = null!; // lowest version supported, initialized in Initialize()
        protected virtual RdpVersion RdpProtocolVersion => RDP.RdpVersion.Rdc6;
        protected ConnectionInfo connectionInfo = null!; // initialized in Initialize()
        protected Version RdpVersion = null!; // initialized in Initialize()
        protected readonly FrmMain _frmMain = FrmMain.Default;
        protected bool loginComplete;
        private int _extendedReconnectAttemptsRemaining;
        private readonly System.Windows.Forms.Timer _extendedReconnectTimer;
        private bool _redirectKeys;
        private bool _alertOnIdleDisconnect;
        protected uint DesktopScaleFactor
        {
            get
            {
                if (connectionInfo == null)
                {
                    return (uint)(ResolutionScalingFactor.Width * 100);
                }

                return connectionInfo.DesktopScaleFactor switch
                {
                    RDPDesktopScaleFactor.Scale100 => 100,
                    RDPDesktopScaleFactor.Scale125 => 125,
                    RDPDesktopScaleFactor.Scale150 => 150,
                    RDPDesktopScaleFactor.Scale200 => 200,
                    _ => (uint)(ResolutionScalingFactor.Width * 100)
                };
            }
        }
        protected readonly uint DeviceScaleFactor = 100;
        protected readonly uint Orientation = 0;
        private AxHost AxHost => (AxHost)Control!;

        private SizeF ResolutionScalingFactor
        {
            get
            {
                // Use the DPI of the control hosting the RDP session (per-monitor aware)
                // rather than _frmMain which may be on a different monitor with different DPI.
                // Fix for #1438: incorrect smartsize on multi-monitor mixed-DPI setups.
                Control? dpiSource = InterfaceControl ?? (Control?)_frmMain;

                if (dpiSource == null || dpiSource.IsDisposed)
                {
                    return new SizeF(1f, 1f);
                }

                // 96 DPI is the baseline (100% scale)
                float scale = dpiSource.DeviceDpi / 96f;
                return new SizeF(scale, scale);
            }
        }


        #region Properties

        public virtual bool SmartSize
        {
            get
            {
                if (_rdpClient == null)
                {
                    return false;
                }

                try
                {
                    return _rdpClient.AdvancedSettings2.SmartSizing;
                }
                catch (InvalidComObjectException ex)
                {
                    Runtime.MessageCollector.AddExceptionMessage(
                        "Unable to read RDP SmartSize state because the COM client is no longer valid.",
                        ex,
                        MessageClass.WarningMsg,
                        false);
                    return false;
                }
                catch (COMException ex)
                {
                    Runtime.MessageCollector.AddExceptionMessage(
                        "Unable to read RDP SmartSize state due to a COM access error.",
                        ex,
                        MessageClass.WarningMsg,
                        false);
                    return false;
                }
            }
            protected set
            {
                if (_rdpClient == null)
                {
                    return;
                }

                try
                {
                    _rdpClient.AdvancedSettings2.SmartSizing = value;
                }
                catch (InvalidComObjectException ex)
                {
                    Runtime.MessageCollector.AddExceptionMessage(
                        "Unable to update RDP SmartSize because the COM client is no longer valid.",
                        ex,
                        MessageClass.WarningMsg,
                        false);
                }
                catch (COMException ex)
                {
                    Runtime.MessageCollector.AddExceptionMessage(
                        "Unable to update RDP SmartSize due to a COM access error.",
                        ex,
                        MessageClass.WarningMsg,
                        false);
                }
            }
        }

        public virtual bool Fullscreen
        {
            get => _rdpClient.FullScreen;
            protected set => _rdpClient.FullScreen = value;
        }

        public bool RedirectKeysEnabled => _redirectKeys;

        private bool RedirectKeys
        {
            set
            {
                _redirectKeys = value;
                try
                {
                    if (!_redirectKeys)
                    {
                        return;
                    }

                    Debug.Assert(Convert.ToBoolean(_rdpClient.SecuredSettingsEnabled));
                    IMsRdpClientSecuredSettings msRdpClientSecuredSettings = _rdpClient.SecuredSettings2;
                    msRdpClientSecuredSettings.KeyboardHookMode = 1; // Apply key combinations at the remote server.
                }
                catch (Exception ex)
                {
                    Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetRedirectKeysFailed, ex);
                }
            }
        }

        public bool LoadBalanceInfoUseUtf8 { get; set; }

        public bool ViewOnly
        {
            get => !AxHost.Enabled;
            set => AxHost.Enabled = !value;
        }

        #endregion

        #region Constructors

        public RdpProtocol()
        {
            tmrReconnect.Tick += tmrReconnect_Tick;
            _extendedReconnectTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _extendedReconnectTimer.Tick += ExtendedReconnectTimer_Tick;
        }

        #endregion

        #region Public Methods

        protected virtual AxHost CreateActiveXRdpClientControl()
        {
            return new AxMsRdpClient6NotSafeForScripting();
        }

        public override bool Initialize()
        {
            // Keep synchronous Initialize for backward compatibility,
            // but use the same logic (minus the await).
            connectionInfo = InterfaceControl.Info;
            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Requesting RDP version: {connectionInfo.RdpVersion}. Using: {RdpProtocolVersion}");
            Control = CreateActiveXRdpClientControl();
            Control.Disposed += OnControlDisposed;
            base.Initialize();

            try
            {
                if (!InitializeActiveXControl()) return false;

                RdpVersion = new Version(_rdpClient.Version);

                if (RdpVersion < Versions.RDC61) return false;

                SetRdpClientProperties();

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetPropsFailed, ex);
                return false;
            }
        }

        public override async System.Threading.Tasks.Task<bool> InitializeAsync()
        {
            connectionInfo = InterfaceControl.Info;
            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, $"Requesting RDP version: {connectionInfo.RdpVersion}. Using: {RdpProtocolVersion}");
            Control = CreateActiveXRdpClientControl();
            Control.Disposed += OnControlDisposed;
            base.Initialize();

            try
            {
                if (!await InitializeActiveXControlAsync()) return false;

                RdpVersion = new Version(_rdpClient.Version);

                if (RdpVersion < Versions.RDC61) return false;

                SetRdpClientProperties();

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetPropsFailed, ex);
                return false;
            }
        }

        private bool InitializeActiveXControl()
        {
            try
            {
                if (!Properties.OptionsStartupExitPage.Default.DisableRefocus)
                {
                    Control!.GotFocus += RdpClient_GotFocus;
                }

                Control!.CreateControl();

                // ActiveX controls require the message pump to complete creation.
                // DoEvents() is unavoidable here but introduces re-entrancy risk:
                // the user can interact with the UI while the control is half-initialized.
                // The timeout guard prevents an infinite loop if creation fails silently.
                var deadline = Environment.TickCount64 + 10_000; // 10 second timeout
                while (!Control!.Created)
                {
                    if (Environment.TickCount64 > deadline)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                            "RDP ActiveX control creation timed out after 10 seconds.");
                        Control.Dispose();
                        return false;
                    }
                    Thread.Sleep(10);
                    Application.DoEvents();
                }

                _rdpClient = (MsRdpClient6NotSafeForScripting)((AxHost)Control).GetOcx()!;

                return true;
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("CLASS_E_CLASSNOTAVAILABLE"))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(Language.RdpProtocolVersionNotSupported, connectionInfo.RdpVersion));
                }
                else
                {
                    Runtime.MessageCollector.AddExceptionMessage(Language.RdpControlCreationFailed, ex);
                }
                Control?.Dispose();
                return false;
            }
        }

        private async System.Threading.Tasks.Task<bool> InitializeActiveXControlAsync()
        {
            try
            {
                if (!Properties.OptionsStartupExitPage.Default.DisableRefocus)
                {
                    Control!.GotFocus += RdpClient_GotFocus;
                }

                Control!.CreateControl();

                var deadline = Environment.TickCount64 + 10_000;
                while (!Control!.Created)
                {
                    if (Environment.TickCount64 > deadline)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg,
                            "RDP ActiveX control creation timed out after 10 seconds.");
                        Control.Dispose();
                        return false;
                    }

                    await System.Threading.Tasks.Task.Delay(10);
                }

                _rdpClient = (MsRdpClient6NotSafeForScripting)((AxHost)Control).GetOcx()!;

                return true;
            }
            catch (COMException ex)
            {
                if (ex.Message.Contains("CLASS_E_CLASSNOTAVAILABLE"))
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(Language.RdpProtocolVersionNotSupported, connectionInfo.RdpVersion));
                }
                else
                {
                    Runtime.MessageCollector.AddExceptionMessage(Language.RdpControlCreationFailed, ex);
                }
                Control?.Dispose();
                return false;
            }
        }

        public override bool Connect()
        {
            loginComplete = false;
            SetEventHandlers();

            try
            {
                _rdpClient.Connect();
                base.Connect();

                return true;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ConnectionOpenFailed, ex);
            }

            return false;
        }

        public override void Disconnect()
        {
            try
            {
                _rdpClient.Disconnect();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpDisconnectFailed, ex);
                Close();
            }
        }

        public void ToggleFullscreen()
        {
            try
            {
                Fullscreen = !Fullscreen;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpToggleFullscreenFailed, ex);
            }
        }

        public void ToggleSmartSize()
        {
            try
            {
                SmartSize = !SmartSize;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpToggleSmartSizeFailed, ex);
            }
        }

        /// <summary>
        /// Toggles whether the RDP ActiveX control will capture and send input events to the remote host.
        /// The local host will continue to receive data from the remote host regardless of this setting.
        /// </summary>
        public void ToggleViewOnly()
        {
            try
            {
                ViewOnly = !ViewOnly;
            }
            catch
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"Could not toggle view only mode for host {connectionInfo.Hostname}");
            }
        }

        public override void Focus()
        {
            try
            {
                if (Control is { ContainsFocus: false })
                {
                    Control.Focus();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpFocusFailed, ex);
            }
        }

        /// <summary>
        /// Determines if this version of the RDP client
        /// is supported on this machine.
        /// </summary>
        /// <returns></returns>
        public bool RdpVersionSupported()
        {
            try
            {
                using AxHost control = CreateActiveXRdpClientControl();
                control.CreateControl();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Private Methods

        protected static class Versions
        {
            // https://en.wikipedia.org/wiki/Remote_Desktop_Protocol
            public static readonly Version RDC60 = new(6, 0, 6000);
            public static readonly Version RDC61 = new(6, 0, 6001);
            public static readonly Version RDC70 = new(6, 1, 7600);
            public static readonly Version RDC80 = new(6, 2, 9200);
            public static readonly Version RDC81 = new(6, 3, 9600);
            public static readonly Version RDC100 = new(10, 0, 0);
        }

        private void SetRdpClientProperties()
        {
            // Fix for #1005: Gateway Authentication box hidden behind other windows
            try
            {
                // Use dynamic to set UIParentWindowHandle to avoid manual construction of _RemotableHandle
                // which is required by the strongly-typed interface method in the interop assembly.
                if (_rdpClient is IMsRdpClientNonScriptable3)
                {
                    ((dynamic)_rdpClient).UIParentWindowHandle = _frmMain.Handle;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Failed to set UIParentWindowHandle for RDP client.", ex, MessageClass.WarningMsg, false);
            }

            // https://learn.microsoft.com/en-us/windows-server/remote/remote-desktop-services/clients/rdp-files

            _rdpClient.Server = connectionInfo.Hostname;

            SetCredentials();
            SetResolution();
            _rdpClient.FullScreenTitle = connectionInfo.Name;

            _alertOnIdleDisconnect = connectionInfo.RDPAlertIdleTimeout;
            _rdpClient.AdvancedSettings2.MinutesToIdleTimeout = connectionInfo.RDPMinutesToIdleTimeout;

            #region Remote Desktop Services
            _rdpClient.SecuredSettings2.StartProgram = connectionInfo.RDPStartProgram;
            _rdpClient.SecuredSettings2.WorkDir = connectionInfo.RDPStartProgramWorkDir;
            #endregion

            //not user changeable
            _rdpClient.AdvancedSettings2.GrabFocusOnConnect = true;
            _rdpClient.AdvancedSettings3.EnableAutoReconnect = true;
            try
            {
                int reconnectCount = Settings.Default.RdpReconnectionCount;
                if (reconnectCount > 20)
                {
                    _rdpClient.AdvancedSettings3.MaxReconnectAttempts = 20;
                    _extendedReconnectAttemptsRemaining = reconnectCount - 20;
                }
                else
                {
                    _rdpClient.AdvancedSettings3.MaxReconnectAttempts = reconnectCount;
                    _extendedReconnectAttemptsRemaining = 0;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage($"Failed to set RDP MaxReconnectAttempts to {Settings.Default.RdpReconnectionCount}. Reverting to default maximum (20).", ex, MessageClass.WarningMsg, false);
                _rdpClient.AdvancedSettings3.MaxReconnectAttempts = 20;
                _extendedReconnectAttemptsRemaining = 0;
            }
            _rdpClient.AdvancedSettings2.keepAliveInterval = 60000; //in milliseconds (10,000 = 10 seconds)
            _rdpClient.AdvancedSettings5.AuthenticationLevel = 0;
            _rdpClient.AdvancedSettings2.EncryptionEnabled = 1;

            _rdpClient.AdvancedSettings2.overallConnectionTimeout = Settings.Default.ConRDPOverallConnectionTimeout;

            _rdpClient.AdvancedSettings2.BitmapPeristence = Convert.ToInt32(connectionInfo.CacheBitmaps);

            if (RdpVersion >= Versions.RDC61)
            {
                _rdpClient.AdvancedSettings7.EnableCredSspSupport = connectionInfo.UseCredSsp;
            }
            
            SetUseConsoleSession();
            SetPort();
            RedirectKeys = connectionInfo.RedirectKeys;
            SetRedirection();
            SetAuthenticationLevel();
            SetLoadBalanceInfo();
            SetRdGateway();
            ViewOnly = Force.HasFlag(ConnectionInfo.Force.ViewOnly);

            _rdpClient.ColorDepth = (int)connectionInfo.Colors;

            SetPerformanceFlags();
            SetRdpSignature();

            _rdpClient.ConnectingText = Language.Connecting;
        }

        protected object? GetExtendedProperty(string property)
        {
            try
            {
                // ReSharper disable once UseIndexedProperty
                return ((IMsRdpExtendedSettings)_rdpClient).get_Property(property);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage($"Error getting extended RDP property '{property}'", ex, MessageClass.WarningMsg, false);
                return null;
            }
        }

        protected void SetExtendedProperty(string property, object value)
        {
            try
            {
                // ReSharper disable once UseIndexedProperty
                ((IMsRdpExtendedSettings)_rdpClient).set_Property(property, ref value);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage($"Error setting extended RDP property '{property}'", ex, MessageClass.WarningMsg, false);
            }
        }

        private void SetRdGateway()
        {
            try
            {
                if (_rdpClient.TransportSettings.GatewayIsSupported == 0)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.RdpGatewayNotSupported, true);
                    return;
                }

                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.RdpGatewayIsSupported, true);

                string gatewayHostname = connectionInfo.RDGatewayHostname ?? string.Empty;
                if (!ShouldApplyExplicitGatewaySettings(connectionInfo.RDGatewayUsageMethod, gatewayHostname)) return;

                // USE GATEWAY
                _rdpClient.TransportSettings.GatewayUsageMethod = (uint)connectionInfo.RDGatewayUsageMethod;
                _rdpClient.TransportSettings.GatewayHostname = gatewayHostname;
                _rdpClient.TransportSettings.GatewayProfileUsageMethod = 1; // TSC_PROXY_PROFILE_MODE_EXPLICIT
                if (connectionInfo.RDGatewayUseConnectionCredentials == RDGatewayUseConnectionCredentials.SmartCard)
                {
                    _rdpClient.TransportSettings.GatewayCredsSource = 1; // TSC_PROXY_CREDS_MODE_SMARTCARD
                }

                if (RdpVersion < Versions.RDC61 || Force.HasFlag(ConnectionInfo.Force.NoCredentials)) return;

                switch (connectionInfo.RDGatewayUseConnectionCredentials)
                {
                    case RDGatewayUseConnectionCredentials.Yes:
                        _rdpClient.TransportSettings2.GatewayCredSharing = 0;
                        _rdpClient.TransportSettings2.GatewayUsername = connectionInfo.Username;
                        //_rdpClient.TransportSettings2.GatewayPassword = connectionInfo.Password.ConvertToUnsecureString();
                        _rdpClient.TransportSettings2.GatewayPassword = connectionInfo.Password;
                        _rdpClient.TransportSettings2.GatewayDomain = connectionInfo?.Domain;
                        break;
                    case RDGatewayUseConnectionCredentials.SmartCard:
                        _rdpClient.TransportSettings2.GatewayCredSharing = 0;
                        break;
                    default:
                    {
                        _rdpClient.TransportSettings2.GatewayCredSharing = 0;

                            string gwu = connectionInfo.RDGatewayUsername;
                            string gwp = connectionInfo.RDGatewayPassword;
                            string gwd = connectionInfo.RDGatewayDomain;
                            string pkey = "";

                        // access secret server api if necessary
                        if (InterfaceControl.Info.RDGatewayExternalCredentialProvider == ExternalCredentialProvider.DelineaSecretServer)
                        {
                            try
                            {
                                string RDGUserViaAPI = InterfaceControl.Info.RDGatewayUserViaAPI;
                                ExternalConnectors.DSS.SecretServerInterface.FetchSecretFromServer($"{RDGUserViaAPI}", out gwu, out gwp, out gwd, out pkey);
                            }
                            catch (Exception ex)
                            {
                                Event_ErrorOccured(this, "Secret Server Interface Error: " + ex.Message, 0);
                            }
                        }
                        else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.ClickstudiosPasswordState)
                        {
                            try
                            {
                                string RDGUserViaAPI = InterfaceControl.Info.RDGatewayUserViaAPI;
                                ExternalConnectors.CPS.PasswordstateInterface.FetchSecretFromServer($"{RDGUserViaAPI}", out gwu, out gwp, out gwd, out pkey);
                            }
                            catch (Exception ex)
                            {
                                Event_ErrorOccured(this, "Passwordstate Interface Error: " + ex.Message, 0);
                            }
                        }
                        else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.OnePassword)
                        {
                            try
                            {
                                string RDGUserViaAPI = InterfaceControl.Info.RDGatewayUserViaAPI;
                                ExternalConnectors.OP.OnePasswordCli.ReadPassword($"{RDGUserViaAPI}", out gwu, out gwp, out gwd, out pkey);
                            }
                            catch (ExternalConnectors.OP.OnePasswordCliException ex)
                            {
                                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPOnePasswordCommandLine + ": " + ex.Arguments);
                                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPOnePasswordReadFailed + Environment.NewLine + ex.Message);
                            }
                        }
                        else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.PasswordSafe)
                        {
                            try
                            {
                                string RDGUserViaAPI = InterfaceControl.Info.RDGatewayUserViaAPI;
                                ExternalConnectors.PasswordSafe.PasswordSafeCli.ReadPassword($"{RDGUserViaAPI}", out gwu, out gwp, out gwd, out pkey);
                            }
                            catch (ExternalConnectors.PasswordSafe.PasswordSafeCliException ex)
                            {
                                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPPasswordSafeCommandLine + ": " + ex.Arguments);
                                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPPasswordSafeReadFailed + Environment.NewLine + ex.Message);
                            }
                        }
                        else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.VaultOpenbao)
                        {
                            try {
                                if (connectionInfo.VaultOpenbaoSecretEngine == VaultOpenbaoSecretEngine.Kv)
                                    gwu = connectionInfo.RDGatewayUsername;
                                ExternalConnectors.VO.VaultOpenbao.ReadPasswordRDP((int)connectionInfo.VaultOpenbaoSecretEngine, connectionInfo.VaultOpenbaoMount, connectionInfo.VaultOpenbaoRole, ref gwu, out gwp);
                            } catch (ExternalConnectors.VO.VaultOpenbaoException ex) {
                                Event_ErrorOccured(this, "Secret Server Interface Error: " + ex.Message, 0);
                            }
                        }


                            if (connectionInfo.RDGatewayUseConnectionCredentials != RDGatewayUseConnectionCredentials.AccessToken)
                        {
                            _rdpClient.TransportSettings2.GatewayUsername = gwu;
                            _rdpClient.TransportSettings2.GatewayPassword = gwp;
                            _rdpClient.TransportSettings2.GatewayDomain = gwd;
                        }
                        else
                        {
                            //TODO: should we check client version and throw if it is less than 7
                        }
                        
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetGatewayFailed, ex);
            }
        }

        private static bool ShouldApplyExplicitGatewaySettings(RDGatewayUsageMethod usageMethod, string gatewayHostname)
        {
            if (usageMethod == RDGatewayUsageMethod.Never)
            {
                return false;
            }

            if (usageMethod != RDGatewayUsageMethod.Detect)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(gatewayHostname))
            {
                return false;
            }

            return Uri.CheckHostName(gatewayHostname.Trim()) != UriHostNameType.Unknown;
        }

        private void SetUseConsoleSession()
        {
            try
            {
                bool value;

                if (Force.HasFlag(ConnectionInfo.Force.UseConsoleSession))
                {
                    value = true;
                }
                else if (Force.HasFlag(ConnectionInfo.Force.DontUseConsoleSession))
                {
                    value = false;
                }
                else
                {
                    value = connectionInfo.UseConsoleSession;
                }

                if (RdpVersion >= Versions.RDC61)
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, string.Format(Language.RdpSetConsoleSwitch, RdpVersion), true);
                    _rdpClient.AdvancedSettings7.ConnectToAdministerServer = value;
                }
                else
                {
                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, $"{string.Format(Language.RdpSetConsoleSwitch, RdpVersion)}{Environment.NewLine}No longer supported in this RDP version. Reference: https://msdn.microsoft.com/en-us/library/aa380863(v=vs.85).aspx", true);
                    // ConnectToServerConsole is deprecated
                    //https://msdn.microsoft.com/en-us/library/aa380863(v=vs.85).aspx
                    //_rdpClient.AdvancedSettings2.ConnectToServerConsole = value;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetConsoleSessionFailed, ex);
            }
        }

        private void SetCredentials()
        {
            try
            {
                if (Force.HasFlag(ConnectionInfo.Force.NoCredentials))
                {
                    return;
                }

                string userName = connectionInfo.Username ?? "";
                string domain = connectionInfo.Domain ?? "";
                string userViaApi = connectionInfo.UserViaAPI ?? "";
                string pkey = "";
                //string password = (connectionInfo?.Password?.ConvertToUnsecureString() ?? "");
                string password = connectionInfo.Password ?? "";

                // access secret server api if necessary
                if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.DelineaSecretServer)
                {
                    try
                    {
                        ExternalConnectors.DSS.SecretServerInterface.FetchSecretFromServer($"{userViaApi}", out userName, out password, out domain, out pkey);
                    }
                    catch (Exception ex)
                    {
                        Event_ErrorOccured(this, "Secret Server Interface Error: " + ex.Message, 0);
                    }
                }
                else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.ClickstudiosPasswordState)
                {
                    try
                    {
                        ExternalConnectors.CPS.PasswordstateInterface.FetchSecretFromServer($"{userViaApi}", out userName, out password, out domain, out pkey);
                    }
                    catch (Exception ex)
                    {
                        Event_ErrorOccured(this, "Passwordstate Interface Error: " + ex.Message, 0);
                    }
                }
                else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.OnePassword)
                {
                    try
                    {
                        ExternalConnectors.OP.OnePasswordCli.ReadPassword($"{userViaApi}", out userName, out password, out domain, out pkey);
                    }
                    catch (ExternalConnectors.OP.OnePasswordCliException ex)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPOnePasswordCommandLine + ": " + ex.Arguments);
                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPOnePasswordReadFailed + Environment.NewLine + ex.Message);
                    }
                }
                else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.PasswordSafe)
                {
                    try
                    {
                        ExternalConnectors.PasswordSafe.PasswordSafeCli.ReadPassword($"{userViaApi}", out userName, out password, out domain, out pkey);
                    }
                    catch (ExternalConnectors.PasswordSafe.PasswordSafeCliException ex)
                    {
                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPPasswordSafeCommandLine + ": " + ex.Arguments);
                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPPasswordSafeReadFailed + Environment.NewLine + ex.Message);
                    }
                }
                else if (InterfaceControl.Info.ExternalCredentialProvider == ExternalCredentialProvider.VaultOpenbao) {
                    try {
                        if(connectionInfo.VaultOpenbaoSecretEngine == VaultOpenbaoSecretEngine.Kv)
                            userName = connectionInfo.Username ?? "";
                        ExternalConnectors.VO.VaultOpenbao.ReadPasswordRDP((int)connectionInfo.VaultOpenbaoSecretEngine, connectionInfo.VaultOpenbaoMount ?? "", connectionInfo.VaultOpenbaoRole ?? "", ref userName, out password);
                    } catch (ExternalConnectors.VO.VaultOpenbaoException ex) {
                        Event_ErrorOccured(this, "Secret Server Interface Error: " + ex.Message, 0);
                    }
                }

                if (string.IsNullOrEmpty(userName))
                {
                    switch (Properties.OptionsCredentialsPage.Default.EmptyCredentials)
                    {
                        case "windows":
                            _rdpClient.UserName = Environment.UserName;
                            break;
                        case "custom" when !string.IsNullOrEmpty(Properties.OptionsCredentialsPage.Default.DefaultUsername):
                            _rdpClient.UserName = Properties.OptionsCredentialsPage.Default.DefaultUsername;
                            break;
                        case "custom":
                            switch (Properties.OptionsCredentialsPage.Default.ExternalCredentialProviderDefault)
                            {
                                case ExternalCredentialProvider.DelineaSecretServer:
                                    try
                                    {
                                        ExternalConnectors.DSS.SecretServerInterface.FetchSecretFromServer(
                                            Properties.OptionsCredentialsPage.Default.UserViaAPIDefault, out userName, out password, out domain, out pkey);
                                    }
                                    catch (Exception ex)
                                    {
                                        Event_ErrorOccured(this, "Secret Server Interface Error: " + ex.Message, 0);
                                    }

                                    break;
                                case ExternalCredentialProvider.ClickstudiosPasswordState:
                                    try
                                    {
                                        ExternalConnectors.CPS.PasswordstateInterface.FetchSecretFromServer(
                                            Properties.OptionsCredentialsPage.Default.UserViaAPIDefault, out userName, out password, out domain, out pkey);
                                    }
                                    catch (Exception ex)
                                    {
                                        Event_ErrorOccured(this, "Passwordstate Interface Error: " + ex.Message, 0);
                                    }

                                    break;
                                case ExternalCredentialProvider.OnePassword:
                                    try
                                    {
                                        ExternalConnectors.OP.OnePasswordCli.ReadPassword(
                                            Properties.OptionsCredentialsPage.Default.UserViaAPIDefault, out userName, out password, out domain, out pkey);
                                    }
                                    catch (ExternalConnectors.OP.OnePasswordCliException ex)
                                    {
                                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPOnePasswordCommandLine + ": " + ex.Arguments);
                                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPOnePasswordReadFailed + Environment.NewLine + ex.Message);
                                    }

                                    break;
                                case ExternalCredentialProvider.PasswordSafe:
                                    try
                                    {
                                        ExternalConnectors.PasswordSafe.PasswordSafeCli.ReadPassword(
                                            Properties.OptionsCredentialsPage.Default.UserViaAPIDefault, out userName, out password, out domain, out pkey);
                                    }
                                    catch (ExternalConnectors.PasswordSafe.PasswordSafeCliException ex)
                                    {
                                        Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg, Language.ECPPasswordSafeCommandLine + ": " + ex.Arguments);
                                        Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, Language.ECPPasswordSafeReadFailed + Environment.NewLine + ex.Message);
                                    }

                                    break;
                            }

                            if (!string.IsNullOrEmpty(userName))
                            {
                                _rdpClient.UserName = userName;
                            }

                            break;
                    }
                }
                else
                {
                    _rdpClient.UserName = userName;
                }

                if (string.IsNullOrEmpty(password))
                {
                    if (Properties.OptionsCredentialsPage.Default.EmptyCredentials == "custom")
                    {
                        if (Properties.OptionsCredentialsPage.Default.DefaultPassword != "")
                        {
                            LegacyRijndaelCryptographyProvider cryptographyProvider = new();
                            _rdpClient.AdvancedSettings2.ClearTextPassword = cryptographyProvider.Decrypt(Properties.OptionsCredentialsPage.Default.DefaultPassword, Runtime.EncryptionKey);
                        }
                    }
                }
                else
                {
                    _rdpClient.AdvancedSettings2.ClearTextPassword = password;
                }

                if (string.IsNullOrEmpty(domain))
                {
                    _rdpClient.Domain = Properties.OptionsCredentialsPage.Default.EmptyCredentials switch
                    {
                        "windows" => Environment.UserDomainName,
                        "custom" => Properties.OptionsCredentialsPage.Default.DefaultDomain,
                        _ => _rdpClient.Domain
                    };
                }
                else
                {
                    _rdpClient.Domain = domain;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetCredentialsFailed, ex);
            }
        }

        protected override void Resize(object sender, EventArgs e)
        {
            base.Resize(sender, e);
            if (InterfaceControl?.Info.Resolution == RDPResolutions.SmartSizeAspect)
            {
                ApplySmartSizeAspect();
            }
        }

        private void ApplySmartSizeAspect()
        {
            if (Control == null || Control.IsDisposed || InterfaceControl == null || InterfaceControl.IsDisposed || _rdpClient == null)
            {
                return;
            }

            try
            {
                // Get source resolution (set during connection)
                int sourceWidth = _rdpClient.DesktopWidth;
                int sourceHeight = _rdpClient.DesktopHeight;

                if (sourceWidth <= 0 || sourceHeight <= 0) return;

                double sourceRatio = (double)sourceWidth / sourceHeight;

                // Get available area
                int targetWidth = InterfaceControl.ClientSize.Width;
                int targetHeight = InterfaceControl.ClientSize.Height;

                if (targetWidth <= 0 || targetHeight <= 0) return;

                double targetRatio = (double)targetWidth / targetHeight;

                int newWidth, newHeight;

                if (targetRatio > sourceRatio)
                {
                    // Available area is wider than source -> height limited
                    newHeight = targetHeight;
                    newWidth = (int)(newHeight * sourceRatio);
                }
                else
                {
                    // Available area is taller than source -> width limited
                    newWidth = targetWidth;
                    newHeight = (int)(newWidth / sourceRatio);
                }

                // Only modify if needed to prevent infinite loop or jitter
                if (Control.Width != newWidth || Control.Height != newHeight || Control.Dock != DockStyle.None)
                {
                    if (Control.Dock != DockStyle.None)
                    {
                        Control.Dock = DockStyle.None;
                    }

                    int x = (targetWidth - newWidth) / 2;
                    int y = (targetHeight - newHeight) / 2;

                    Control.SetBounds(x, y, newWidth, newHeight);
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Error applying SmartSize aspect ratio.", ex, MessageClass.WarningMsg, true);
            }
        }

        private void SetResolution()
        {
            try
            {
                uint scaleFactor;
                switch (connectionInfo.DesktopScaleFactor)
                {
                    case RDPDesktopScaleFactor.Scale100:
                        scaleFactor = 100;
                        break;
                    case RDPDesktopScaleFactor.Scale125:
                        scaleFactor = 125;
                        break;
                    case RDPDesktopScaleFactor.Scale150:
                        scaleFactor = 150;
                        break;
                    case RDPDesktopScaleFactor.Scale200:
                        scaleFactor = 200;
                        break;
                    case RDPDesktopScaleFactor.Auto:
                    default:
                        scaleFactor = DesktopScaleFactor;
                        break;
                }

                SetExtendedProperty("DesktopScaleFactor", scaleFactor);
                SetExtendedProperty("DeviceScaleFactor", DeviceScaleFactor);

                if (Force.HasFlag(ConnectionInfo.Force.Fullscreen))
                {
                    _rdpClient.FullScreen = true;
                    var screen = Screen.FromControl(InterfaceControl ?? (Control)_frmMain);
                    _rdpClient.DesktopWidth = (int)(screen.Bounds.Width * ResolutionScalingFactor.Width);
                    _rdpClient.DesktopHeight = (int)(screen.Bounds.Height * ResolutionScalingFactor.Height);

                    return;
                }

                switch (InterfaceControl.Info.Resolution)
                {
                    case RDPResolutions.FitToWindow:
                    case RDPResolutions.SmartSize:
                    case RDPResolutions.SmartSizeAspect:
                        {
                            _rdpClient.DesktopWidth = InterfaceControl.Size.Width;
                            _rdpClient.DesktopHeight = InterfaceControl.Size.Height;

                            if (InterfaceControl.Info.Resolution == RDPResolutions.SmartSize ||
                                InterfaceControl.Info.Resolution == RDPResolutions.SmartSizeAspect)
                            {
                                _rdpClient.AdvancedSettings2.SmartSizing = true;
                            }

                            if (InterfaceControl.Info.Resolution == RDPResolutions.SmartSizeAspect)
                            {
                                ApplySmartSizeAspect();
                            }

                            break;
                        }
                    case RDPResolutions.Fullscreen:
                        _rdpClient.FullScreen = true;
                        var screen = Screen.FromControl(InterfaceControl ?? (Control)_frmMain);
                        _rdpClient.DesktopWidth = (int)(screen.Bounds.Width * ResolutionScalingFactor.Width);
                        _rdpClient.DesktopHeight = (int)(screen.Bounds.Height * ResolutionScalingFactor.Height);
                        break;
                    case RDPResolutions.Custom:
                        {
                            int w = connectionInfo.ResolutionWidth;
                            int h = connectionInfo.ResolutionHeight;
                            if (w > 0 && h > 0)
                            {
                                _rdpClient.DesktopWidth = w;
                                _rdpClient.DesktopHeight = h;
                                _rdpClient.AdvancedSettings2.SmartSizing = true;
                            }
                            else
                            {
                                _rdpClient.DesktopWidth = InterfaceControl.Size.Width;
                                _rdpClient.DesktopHeight = InterfaceControl.Size.Height;
                            }
                            break;
                        }
                    default:
                        {
                            System.Drawing.Rectangle resolution = connectionInfo.Resolution.GetResolutionRectangle();
                            _rdpClient.DesktopWidth = resolution.Width;
                            _rdpClient.DesktopHeight = resolution.Height;
                            _rdpClient.AdvancedSettings2.SmartSizing = true;
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetResolutionFailed, ex);
            }
        }

        private void SetPort()
        {
            try
            {
                if (connectionInfo.Port != (int)Defaults.Port)
                {
                    _rdpClient.AdvancedSettings2.RDPPort = connectionInfo.Port;
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetPortFailed, ex);
            }
        }

        private void SetRedirection()
        {
            try
            {
                SetDriveRedirection();
                _rdpClient.AdvancedSettings2.RedirectPorts = connectionInfo.RedirectPorts;
                _rdpClient.AdvancedSettings2.RedirectPrinters = connectionInfo.RedirectPrinters;
                _rdpClient.AdvancedSettings2.RedirectSmartCards = connectionInfo.RedirectSmartCards;
                _rdpClient.SecuredSettings2.AudioRedirectionMode = (int)connectionInfo.RedirectSound;
                _rdpClient.AdvancedSettings6.RedirectClipboard = connectionInfo.RedirectClipboard;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetRedirectionFailed, ex);
            }
        }

        private void SetDriveRedirection()
        {
            if (RDPDiskDrives.None == connectionInfo.RedirectDiskDrives)
                _rdpClient.AdvancedSettings2.RedirectDrives = false;
            else if (RDPDiskDrives.All == connectionInfo.RedirectDiskDrives)
                _rdpClient.AdvancedSettings2.RedirectDrives = true;
            else if (RDPDiskDrives.Custom == connectionInfo.RedirectDiskDrives)
            {
                IMsRdpClientNonScriptable5 rdpNS5 = (IMsRdpClientNonScriptable5)((AxHost)Control!).GetOcx()!;
                for (uint i = 0; i < rdpNS5.DriveCollection.DriveCount; i++)
                {
                    IMsRdpDrive drive = rdpNS5.DriveCollection.DriveByIndex[i];
                    drive.RedirectionState = connectionInfo.RedirectDiskDrivesCustom.Contains(drive.Name.Substring(0, 1));
                }
            }
            else
            {
                // Local Drives
                IMsRdpClientNonScriptable5 rdpNS5 = (IMsRdpClientNonScriptable5)((AxHost)Control!).GetOcx()!;
                for (uint i = 0; i < rdpNS5.DriveCollection.DriveCount; i++)
                {
                    IMsRdpDrive drive = rdpNS5.DriveCollection.DriveByIndex[i];
                    drive.RedirectionState = IsLocal(drive);
                }
            }
        }

        private bool IsLocal(IMsRdpDrive drive)
        {
            DriveInfo[] myDrives = DriveInfo.GetDrives();
            foreach (DriveInfo myDrive in myDrives)
            {
                if (myDrive.Name.Substring(0, 1).Equals(drive.Name.Substring(0,1)))
                {
                    return myDrive.DriveType == DriveType.Fixed;
                }
            }
            return false;
        }

        private void SetPerformanceFlags()
        {
            try
            {
                int pFlags = 0;
                if (connectionInfo.DisplayThemes == false)
                    pFlags += (int)RDPPerformanceFlags.DisableThemes;

                if (connectionInfo.DisplayWallpaper == false)
                    pFlags += (int)RDPPerformanceFlags.DisableWallpaper;

                if (connectionInfo.EnableFontSmoothing)
                    pFlags += (int)RDPPerformanceFlags.EnableFontSmoothing;

                if (connectionInfo.EnableDesktopComposition)
                    pFlags += (int)RDPPerformanceFlags.EnableDesktopComposition;

                if (connectionInfo.DisableFullWindowDrag)
                    pFlags += (int)RDPPerformanceFlags.DisableFullWindowDrag;

                if (connectionInfo.DisableMenuAnimations)
                    pFlags += (int)RDPPerformanceFlags.DisableMenuAnimations;

                if (connectionInfo.DisableCursorShadow)
                    pFlags += (int)RDPPerformanceFlags.DisableCursorShadow;

                if (connectionInfo.DisableCursorBlinking)
                    pFlags += (int)RDPPerformanceFlags.DisableCursorBlinking;

                _rdpClient.AdvancedSettings2.PerformanceFlags = pFlags;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetPerformanceFlagsFailed, ex);
            }
        }

        private void SetAuthenticationLevel()
        {
            try
            {
                _rdpClient.AdvancedSettings5.AuthenticationLevel = (uint)connectionInfo.RDPAuthenticationLevel;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetAuthenticationLevelFailed, ex);
            }
        }

        private void SetRdpSignature()
        {
            if (string.IsNullOrEmpty(connectionInfo.RDPSignScope) && string.IsNullOrEmpty(connectionInfo.RDPSignature))
                return;

            try
            {
                if (!string.IsNullOrEmpty(connectionInfo.RDPSignScope))
                    SetExtendedProperty("SignScope", connectionInfo.RDPSignScope);

                if (!string.IsNullOrEmpty(connectionInfo.RDPSignature))
                    SetExtendedProperty("Signature", connectionInfo.RDPSignature);
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage("Failed to set RDP signature properties.", ex, MessageClass.WarningMsg, false);
            }
        }

        private void SetLoadBalanceInfo()
        {
            if (string.IsNullOrEmpty(connectionInfo.LoadBalanceInfo))
            {
                return;
            }

            try
            {
                _rdpClient.AdvancedSettings2.LoadBalanceInfo = LoadBalanceInfoUseUtf8
                    ? new AzureLoadBalanceInfoEncoder().Encode(connectionInfo.LoadBalanceInfo)
                    : connectionInfo.LoadBalanceInfo;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Unable to set load balance info.", ex);
            }
        }

        protected virtual void SetEventHandlers()
        {
            try
            {
                _rdpClient.OnConnecting += RDPEvent_OnConnecting;
                _rdpClient.OnConnected += RDPEvent_OnConnected;
                _rdpClient.OnLoginComplete += RDPEvent_OnLoginComplete;
                _rdpClient.OnFatalError += RDPEvent_OnFatalError;
                _rdpClient.OnDisconnected += RDPEvent_OnDisconnected;
                _rdpClient.OnIdleTimeoutNotification += RDPEvent_OnIdleTimeoutNotification;
                _rdpClient.OnEnterFullScreenMode += RDPEvent_OnEnterFullScreenMode;
                _rdpClient.OnLeaveFullScreenMode += RDPEvent_OnLeaveFullscreenMode;
                _rdpClient.OnLogonError += RDPEvent_OnLogonError;
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpSetEventHandlersFailed, ex);
            }
        }
        
        #endregion

        #region Private Events & Handlers

        private void RDPEvent_OnIdleTimeoutNotification()
        {
            Close(); //Simply close the RDP Session if the idle timeout has been triggered.

            if (!_alertOnIdleDisconnect) return;
            MessageBox.Show($@"The {connectionInfo.Name} session was disconnected due to inactivity", @"Session Disconnected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void RDPEvent_OnFatalError(int errorCode)
        {
            string errorMsg = RdpErrorCodes.GetError(errorCode);
            Event_ErrorOccured(this, errorMsg, errorCode);
        }

        private void RDPEvent_OnDisconnected(int discReason)
        {
            const int UI_ERR_NORMAL_DISCONNECT = 0xB08;
            const int UI_ERR_NLA_NOT_ENABLED = 0xB09;
            const int UI_ERR_CONNECT_FAILED_DOWN = 0x1807;

            if (discReason != UI_ERR_NORMAL_DISCONNECT && _extendedReconnectAttemptsRemaining > 0)
            {
                uint extendedDisconnectReason = (uint)_rdpClient.ExtendedDisconnectReason;
                // 4 = exDiscReasonLogoff, 12 = exDiscReasonLogoffByUser
                if (extendedDisconnectReason != 4 && extendedDisconnectReason != 12)
                {
                    int attemptsToSet = Math.Min(_extendedReconnectAttemptsRemaining, 20);
                    _extendedReconnectAttemptsRemaining -= attemptsToSet;

                    try
                    {
                        _rdpClient.AdvancedSettings3.MaxReconnectAttempts = attemptsToSet;
                    }
                    catch { }

                    Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                        $"Auto-reconnect exhausted. Retrying... ({_extendedReconnectAttemptsRemaining} extended attempts remaining)");

                    _extendedReconnectTimer.Start();
                    return;
                }
            }

            if (discReason != UI_ERR_NORMAL_DISCONNECT)
            {
                uint extendedDisconnectReason = (uint)_rdpClient.ExtendedDisconnectReason;
                string reason = _rdpClient.GetErrorDescription((uint)discReason, extendedDisconnectReason);
                if (discReason == UI_ERR_NLA_NOT_ENABLED || extendedDisconnectReason == (uint)UI_ERR_NLA_NOT_ENABLED)
                {
                    reason = "The remote computer requires Network Level Authentication (NLA). Enable \"Use CredSSP\" for this RDP connection and try again.";
                }
                else if (discReason == UI_ERR_CONNECT_FAILED_DOWN || extendedDisconnectReason == (uint)UI_ERR_CONNECT_FAILED_DOWN)
                {
                    reason = "The connection failed. This may be due to a domain trust issue or NLA settings. Please check your domain trust relationship and NLA settings.";
                }

                Event_Disconnected(this, reason, discReason);
            }

            if (Properties.OptionsAdvancedPage.Default.ReconnectOnDisconnect)
            {
                ReconnectGroup = new ReconnectGroup();
                ReconnectGroup.CloseClicked += Event_ReconnectGroupCloseClicked;
                ReconnectGroup.Left = (int)((double)Control!.Width / 2 - (double)ReconnectGroup.Width / 2);
                ReconnectGroup.Top = (int)((double)Control.Height / 2 - (double)ReconnectGroup.Height / 2);
                ReconnectGroup.Parent = Control;
                ReconnectGroup.Show();
                tmrReconnect.Enabled = true;
            }
            else
            {
                Close();
            }
        }

        private void RDPEvent_OnConnecting()
        {
            Event_Connecting(this);
        }

        private void RDPEvent_OnConnected()
        {
            try
            {
                int reconnectCount = Settings.Default.RdpReconnectionCount;
                if (reconnectCount > 20)
                {
                    _rdpClient.AdvancedSettings3.MaxReconnectAttempts = 20;
                    _extendedReconnectAttemptsRemaining = reconnectCount - 20;
                }
                else
                {
                    _extendedReconnectAttemptsRemaining = 0;
                }
            }
            catch { }

            Event_Connected(this);
        }

        private void RDPEvent_OnLoginComplete()
        {
            loginComplete = true;
        }

        private void RDPEvent_OnEnterFullScreenMode()
        {
            try
            {
                // Fix for #1294: RDP session on wrong monitor's taskbar
                // When RDP goes fullscreen, we want it to have its own taskbar button on the correct monitor.
                // We find the foreground window (which should be the RDP window) and add WS_EX_APPWINDOW style.
                
                System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        for (int i = 0; i < 10; i++) // Try for ~1 second
                        {
                            await System.Threading.Tasks.Task.Delay(100);

                            if (_frmMain == null || _frmMain.IsDisposed) return;

                            _frmMain.Invoke((MethodInvoker)delegate
                            {
                                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                                if (hwnd == IntPtr.Zero) return;

                                StringBuilder className = new StringBuilder(256);
                                NativeMethods.GetClassName(hwnd, className, className.Capacity);
                                string cls = className.ToString();

                                // Check for standard RDP container classes
                                if (cls.Contains("TscShellContainerClass") || cls.Contains("UIContainerClass"))
                                {
                                    int exStyle = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE);
                                    bool needsAppWindow = (exStyle & NativeMethods.WS_EX_APPWINDOW) == 0;
                                    bool hasToolWindow = (exStyle & NativeMethods.WS_EX_TOOLWINDOW) != 0;
                                    // Fix for #1413: Ensure fullscreen RDP container appears in Alt+Tab
                                    // WS_EX_APPWINDOW forces the window into Alt+Tab and taskbar;
                                    // WS_EX_TOOLWINDOW must be removed as it hides the window from Alt+Tab.
                                    if (needsAppWindow || hasToolWindow)
                                    {
                                        int newStyle = (exStyle | NativeMethods.WS_EX_APPWINDOW) & ~NativeMethods.WS_EX_TOOLWINDOW;
                                        NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_EXSTYLE, newStyle);
                                        // Force style update
                                        NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                                            NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_FRAMECHANGED | NativeMethods.SWP_NOACTIVATE);
                                    }
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Runtime.MessageCollector.AddExceptionStackTrace("Error in RDP fullscreen taskbar fix task", ex, MessageClass.WarningMsg, false);
                    }
                });
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Error initiating RDP fullscreen taskbar fix", ex, MessageClass.WarningMsg, false);
            }
        }

        private void RDPEvent_OnLeaveFullscreenMode()
        {
            Fullscreen = false;
            _leaveFullscreenEvent?.Invoke(this, EventArgs.Empty);

            try
            {
                if (_frmMain.WindowState == FormWindowState.Minimized && !Properties.OptionsTabsPanelsPage.Default.DoNotRestoreOnRdpMinimize)
                {
                    _frmMain.WindowState = FormWindowState.Normal;
                }

                _frmMain.Activate();
                InterfaceControl?.Parent?.Focus();
                Focus();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("RDP leave-fullscreen refocus failed", ex, MessageClass.WarningMsg, false);
            }
        }

        private void RDPEvent_OnLogonError(int lError)
        {
            try
            {
                string errorMsg = $"RDP Logon Error: {lError}";
                bool isAuthFailure = false;
                // 0x2000c = Authentication failure (131084)
                if (lError == 0x2000c || lError == -2147023570) // E_ACCESSDENIED / 0x8007000E
                {
                    errorMsg = "Authentication failed";
                    isAuthFailure = true;
                }

                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, errorMsg);
                Event_ErrorOccured(this, errorMsg, lError);

                if (isAuthFailure)
                {
                    PromptToUpdatePassword();
                }

                Close();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ConnectionOpenFailed, ex);
            }
        }

        private void ExtendedReconnectTimer_Tick(object? sender, EventArgs e)
        {
            _extendedReconnectTimer.Stop();

            try
            {
                _rdpClient.Connect();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.ConnectionOpenFailed, ex);
                Close();
            }
        }

        private void RdpClient_GotFocus(object sender, EventArgs e)
        {
            (Control?.Parent?.Parent as ConnectionTab)?.Focus();
        }

        private void OnControlDisposed(object? sender, EventArgs e)
        {
            try
            {
                if (Control != null)
                {
                    Control.Disposed -= OnControlDisposed;
                }

                _extendedReconnectTimer.Stop();
                _extendedReconnectTimer.Dispose();

                if (_rdpClient != null)
                {
                    // If connected, try to disconnect first
                    try
                    {
                        if (_rdpClient.Connected == 1)
                        {
                            _rdpClient.Disconnect();
                        }
                    }
                    catch { }

                    // Force release the COM object
                    try
                    {
                        int refCount = Marshal.FinalReleaseComObject(_rdpClient);
                        while (refCount > 0)
                        {
                            refCount = Marshal.FinalReleaseComObject(_rdpClient);
                        }
                    }
                    catch { }
                    finally
                    {
                        _rdpClient = null!;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace("Error during RDP control disposal", ex);
            }
        }
        #endregion

        #region Public Events & Handlers

        public delegate void LeaveFullscreenEventHandler(object sender, EventArgs e);

        private LeaveFullscreenEventHandler? _leaveFullscreenEvent;

        public event LeaveFullscreenEventHandler LeaveFullscreen
        {
            add => _leaveFullscreenEvent = (LeaveFullscreenEventHandler?)Delegate.Combine(_leaveFullscreenEvent, value);
            remove => _leaveFullscreenEvent = (LeaveFullscreenEventHandler?)Delegate.Remove(_leaveFullscreenEvent, value);
        }

        #endregion

        #region Enums

        public enum Defaults
        {
            Colors = RDPColors.Colors16Bit,
            Sounds = RDPSounds.DoNotPlay,
            Resolution = RDPResolutions.FitToWindow,
            Port = 3389
        }

        #endregion
        
        #region Reconnect Stuff

        private void tmrReconnect_Tick(object? sender, EventArgs e)
        {
            try
            {
                if (ReconnectGroup == null) return;

                bool srvReady = PortScanner.IsPortOpen(connectionInfo.Hostname, Convert.ToString(connectionInfo.Port));

                ReconnectGroup.ServerReady = srvReady;

                if (!ReconnectGroup.ReconnectWhenReady || !srvReady) return;
                tmrReconnect.Enabled = false;
                ReconnectGroup.DisposeReconnectGroup();
                //SetProps()
                _rdpClient.Connect();
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage(
                    string.Format(Language.AutomaticReconnectError, connectionInfo.Hostname),
                    ex, MessageClass.WarningMsg, false);
            }
        }

        #endregion

    }
}

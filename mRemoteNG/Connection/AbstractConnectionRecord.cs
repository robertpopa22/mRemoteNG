using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.Serial;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using mRemoteNG.Tools.Attributes;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;
using System.Security;

namespace mRemoteNG.Connection
{
    [SupportedOSPlatform("windows")]
    public abstract class AbstractConnectionRecord(string uniqueId) : INotifyPropertyChanged
    {
        #region Fields

        private string _name = string.Empty;
        private string _description = string.Empty;
        private string _icon = string.Empty;
        private string _panel = string.Empty;
        private string _color = string.Empty;
        private string _tabColor = string.Empty;
        private ConnectionFrameColor _connectionFrameColor = default;

        private string _hostname = string.Empty;
        private string _ipAddress = string.Empty;
        private ConnectionAddressPrimary _connectionAddressPrimary = ConnectionAddressPrimary.Hostname;
        private string _alternativeAddress = string.Empty;
        private ExternalAddressProvider _externalAddressProvider = default;
        private string _ec2InstanceId = "";
        private string _ec2Region = "";
        private ExternalCredentialProvider _externalCredentialProvider = default;
        private string _userViaAPI = "";
        private string _username = string.Empty;
        //private SecureString _password = null;
        private string _password = string.Empty;
        private string _vaultRole = string.Empty;
        private string _vaultMount = string.Empty;
        private VaultOpenbaoSecretEngine _vaultSecretEngine = default;
        private string _domain = string.Empty;
        private string _vmId = string.Empty;
        private bool _useEnhancedMode = default;
        
        private string _sshTunnelConnectionName = string.Empty;
        private ProtocolType _protocol = default;
        private RdpVersion _rdpProtocolVersion = default;
        private string _extApp = string.Empty;
        private int _port = default;
        private string _sshOptions = string.Empty;
        private string _privateKeyPath = string.Empty;
        private string _puttySession = string.Empty;
        private bool _useConsoleSession = default;
        private AuthenticationLevel _rdpAuthenticationLevel = default;
        private int _rdpMinutesToIdleTimeout = default;
        private bool _rdpAlertIdleTimeout = default;
        private string _loadBalanceInfo = string.Empty;
        private HTTPBase.RenderingEngine _renderingEngine = default;
        private bool _scriptErrorsSuppressed = true;
        private bool _usePersistentBrowser = default;
        private bool _showBrowserNavigationBar = default;
        private bool _useCredSsp = default;
        private bool _useRestrictedAdmin = default;
        private bool _useRCG = default;
        private bool _useVmId = default;

        private RDGatewayUsageMethod _rdGatewayUsageMethod = default;
        private string _rdGatewayHostname = string.Empty;
        private RDGatewayUseConnectionCredentials _rdGatewayUseConnectionCredentials = default;
        private string _rdGatewayUsername = string.Empty;
        private string _rdGatewayPassword = string.Empty;
        private string _rdGatewayDomain = string.Empty;
        private string _rdGatewayAccessToken = string.Empty;
        private ExternalCredentialProvider _rdGatewayExternalCredentialProvider = default;
        private string _rdGatewayUserViaAPI = "";


        private RDPResolutions _resolution = default;
        private RDPSizingMode _rdpSizingMode = default;
        private int _resolutionWidth;
        private int _resolutionHeight;
        private RDPDesktopScaleFactor _desktopScaleFactor = default;
        private bool _automaticResize = default;
        private bool _rdpUseMultimon = default;
        private RDPColors _colors = default;
        private bool _cacheBitmaps = default;
        private bool _displayWallpaper = default;
        private bool _displayThemes = default;
        private bool _enableFontSmoothing = default;
        private bool _enableDesktopComposition = default;
        private bool _disableFullWindowDrag = default;
        private bool _disableMenuAnimations = default;
        private bool _disableCursorShadow = default;
        private bool _disableCursorBlinking = default;

        private bool _redirectKeys = default;
        private RDPDiskDrives _redirectDiskDrives = default;
        private string _redirectDiskDrivesCustom = string.Empty;
        private bool _redirectPrinters = default;
        private bool _redirectClipboard = default;
        private bool _redirectPorts = default;
        private bool _redirectSmartCards = default;
        private RDPSounds _redirectSound = default;
        private RDPSoundQuality _soundQuality = default;
        private bool _redirectAudioCapture = default;

        private string _preExtApp = string.Empty;
        private string _postExtApp = string.Empty;
        private string _macAddress = string.Empty;
        private string _openingCommand = string.Empty;
        private string _userField = string.Empty;
        private string _userField1 = string.Empty;
        private string _userField2 = string.Empty;
        private string _userField3 = string.Empty;
        private string _userField4 = string.Empty;
        private string _userField5 = string.Empty;
        private string _userField6 = string.Empty;
        private string _userField7 = string.Empty;
        private string _userField8 = string.Empty;
        private string _userField9 = string.Empty;
        private string _userField10 = string.Empty;
        private string _notes = string.Empty;
        private string _environmentTags = "";
        private string _rdpStartProgram = string.Empty;
        private string _rdpStartProgramWorkDir = string.Empty;
        private string _rdpRemoteAppProgram = string.Empty;
        private string _rdpRemoteAppCmdLine = string.Empty;
        private string _rdpSignScope = string.Empty;
        private string _rdpSignature = string.Empty;
        private bool _favorite = default;
        private bool _retryOnFirstConnect = default;
        private bool _alwaysPromptForCredentials = default;
        private bool _isTemplate = default;

        private ProtocolVNC.Compression _vncCompression = default;
        private ProtocolVNC.Encoding _vncEncoding = default;
        private ProtocolVNC.AuthMode _vncAuthMode = default;
        private ProtocolVNC.ProxyType _vncProxyType = default;
        private string _vncProxyIp = string.Empty;
        private int _vncProxyPort = default;
        private string _vncProxyUsername = string.Empty;
        private string _vncProxyPassword = string.Empty;
        private ProtocolVNC.Colors _vncColors = default;
        private ProtocolVNC.SmartSizeMode _vncSmartSizeMode = default;
        private bool _vncViewOnly = default;
        private bool _vncClipboardRedirect = true;

        private string _credentialId = string.Empty;

        private int _serialDataBits = 8;
        private ProtocolSerial.Parity _serialParity = ProtocolSerial.Parity.None;
        private ProtocolSerial.StopBits _serialStopBits = ProtocolSerial.StopBits.One;
        private ProtocolSerial.FlowControl _serialFlowControl = ProtocolSerial.FlowControl.None;

        #endregion

        #region Properties

        #region Display

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Name)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionName))]
        public virtual string Name
        {
            get => _name;
            set => SetField(ref _name, value, "Name");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Description)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDescription))]
        public virtual string Description
        {
            get => GetPropertyValue(nameof(Description), _description);
            set => SetField(ref _description, value, nameof(Description));
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display))]
        [DisplayName("Is Template")]
        [Description("If enabled, this connection serves as a template and cannot be initiated.")]
        public virtual bool IsTemplate
        {
            get => GetPropertyValue(nameof(IsTemplate), _isTemplate);
            set => SetField(ref _isTemplate, value, nameof(IsTemplate));
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         TypeConverter(typeof(ConnectionIcon)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Icon)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionIcon))]
        public virtual string Icon
        {
            get => GetPropertyValue("Icon", _icon);
            set => SetField(ref _icon, value, "Icon");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Panel)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionPanel))]
        public virtual string Panel
        {
            get => GetPropertyValue("Panel", _panel);
            set => SetField(ref _panel, value, "Panel");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Color)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionColor)),
         Editor(typeof(System.Drawing.Design.ColorEditor), typeof(System.Drawing.Design.UITypeEditor)),
         TypeConverter(typeof(MiscTools.TabColorConverter))]
        public virtual string Color
        {
            get => GetPropertyValue("Color", _color);
            set => SetField(ref _color, value, "Color");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.TabColor)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionTabColor)),
         Editor(typeof(System.Drawing.Design.ColorEditor), typeof(System.Drawing.Design.UITypeEditor)),
         TypeConverter(typeof(MiscTools.TabColorConverter))]
        public virtual string TabColor
        {
            get => GetPropertyValue("TabColor", _tabColor);
            set => SetField(ref _tabColor, value, "TabColor");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Display)),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ConnectionFrameColor)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionConnectionFrameColor)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter))]
        public virtual ConnectionFrameColor ConnectionFrameColor
        {
            get => GetPropertyValue("ConnectionFrameColor", _connectionFrameColor);
            set => SetField(ref _connectionFrameColor, value, "ConnectionFrameColor");
        }

        #endregion

        #region Connection

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.HostnameIp)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionHostnameIp)),
         AttributeUsedInAllProtocolsExcept()]
        public virtual string Hostname
        {
            get => GetPropertyValue("Hostname", GetEffectiveHostname());
            set => SetField(ref _hostname, value?.Trim() ?? string.Empty, "Hostname");
        }

        /// <summary>
        /// Returns the hostname backing field or IP address field based on the <see cref="ConnectionAddressPrimary"/> setting.
        /// Used as the fallback value passed to <see cref="GetPropertyValue"/> for the Hostname property.
        /// </summary>
        private string GetEffectiveHostname() =>
            _connectionAddressPrimary == ConnectionAddressPrimary.IPAddress && !string.IsNullOrWhiteSpace(_ipAddress)
                ? _ipAddress.Trim()
                : _hostname?.Trim() ?? string.Empty;

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         DisplayName("IP Address"),
         Description("IP address for this connection. When 'Primary Address' is set to 'IP Address', this is used for connecting instead of the Hostname field."),
         AttributeUsedInAllProtocolsExcept()]
        public virtual string IPAddress
        {
            get => GetPropertyValue("IPAddress", _ipAddress?.Trim() ?? string.Empty);
            set => SetField(ref _ipAddress, value?.Trim() ?? string.Empty, "IPAddress");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         DisplayName("Primary Address"),
         Description("Determines which address field (Hostname or IP Address) is used when initiating a connection. Defaults to Hostname for backward compatibility."),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInAllProtocolsExcept()]
        public virtual ConnectionAddressPrimary ConnectionAddressPrimary
        {
            get => GetPropertyValue("ConnectionAddressPrimary", _connectionAddressPrimary);
            set => SetField(ref _connectionAddressPrimary, value, "ConnectionAddressPrimary");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         DisplayName("Alternative Hostname/IP"),
         Description("Optional alternate hostname or IP address used when connecting with options."),
         AttributeUsedInAllProtocolsExcept()]
        public virtual string AlternativeAddress
        {
            get => GetPropertyValue("AlternativeAddress", _alternativeAddress?.Trim() ?? string.Empty);
            set => SetField(ref _alternativeAddress, value?.Trim() ?? string.Empty, "AlternativeAddress");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Port)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionPort)),
         AttributeUsedInAllProtocolsExcept(ProtocolType.MSRA)]
        public virtual int Port
        {
            get => GetPropertyValue("Port", _port);
            set => SetField(ref _port, value, "Port");
        }

        // external credential provider selector
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalCredentialProvider)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalCredentialProvider)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2)]
        public ExternalCredentialProvider ExternalCredentialProvider
        {
            get => GetPropertyValue("ExternalCredentialProvider", _externalCredentialProvider);
            set => SetField(ref _externalCredentialProvider, value, "ExternalCredentialProvider");
        }

        [Browsable(false)]
        public virtual string CredentialId
        {
            get => GetPropertyValue("CredentialId", _credentialId);
            set => SetField(ref _credentialId, value, "CredentialId");
        }

        // credential record identifier for external credential provider
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UserViaAPI)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUserViaAPI)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2)]
        public virtual string UserViaAPI
        {
            get => GetPropertyValue("UserViaAPI", _userViaAPI);
            set => SetField(ref _userViaAPI, value, "UserViaAPI");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Username)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUsername)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2, ProtocolType.OpenSSH, ProtocolType.HTTP, ProtocolType.HTTPS, ProtocolType.IntApp, ProtocolType.Winbox)]
        public virtual string Username
        {
            get => GetPropertyValue("Username", _username);
            set => SetField(ref _username, Settings.Default.DoNotTrimUsername ? value : (value?.Trim() ?? string.Empty), "Username");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Password)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionPassword)),
         PasswordPropertyText(true),
         Editor(typeof(UI.Controls.ConnectionInfoPropertyGrid.PasswordRevealEditor), typeof(UITypeEditor)),
         AttributeUsedInAllProtocolsExcept(ProtocolType.Telnet, ProtocolType.Rlogin, ProtocolType.RAW, ProtocolType.MSRA)]
        //public virtual SecureString Password
        public virtual string Password
        {
            get => GetPropertyValue("Password", _password);
            set => SetField(ref _password, value, "Password");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.VaultOpenbaoMount)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.VaultOpenbaoMountDescription)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2)]
        public virtual string VaultOpenbaoMount {
            get => GetPropertyValue("VaultOpenbaoMount", _vaultMount);
            set => SetField(ref _vaultMount, value, "VaultOpenbaoMount");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.VaultOpenbaoRole)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.VaultOpenbaoRoleDescription)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2)]
        public virtual string VaultOpenbaoRole {
            get => GetPropertyValue("VaultOpenbaoRole", _vaultRole);
            set => SetField(ref _vaultRole, value, "VaultOpenbaoRole");
        }

        // external credential provider selector
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.VaultOpenbaoSecretEngine)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVaultOpenbaoSecretEngine)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH1, ProtocolType.SSH2)]
        public VaultOpenbaoSecretEngine VaultOpenbaoSecretEngine {
            get => GetPropertyValue("VaultOpenbaoSecretEngine", _vaultSecretEngine);
            set => SetField(ref _vaultSecretEngine, value, "VaultOpenbaoSecretEngine");
        }


        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Domain)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDomain)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.IntApp, ProtocolType.PowerShell, ProtocolType.WSL)]
        public string Domain
        {
            get => GetPropertyValue("Domain", _domain)?.Trim() ?? string.Empty;
            set => SetField(ref _domain, value?.Trim() ?? string.Empty, "Domain");
        }


        // external address provider selector
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalAddressProvider)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalAddressProvider)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH2)]
        public ExternalAddressProvider ExternalAddressProvider
        {
            get => GetPropertyValue("ExternalAddressProvider", _externalAddressProvider);
            set => SetField(ref _externalAddressProvider, value, "ExternalAddressProvider");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
        LocalizedAttributes.LocalizedDisplayName(nameof(Language.EC2InstanceId)),
        LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEC2InstanceId)),
        AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH2)]
        public string EC2InstanceId
        {
            get => GetPropertyValue("EC2InstanceId", _ec2InstanceId)?.Trim() ?? string.Empty;
            set => SetField(ref _ec2InstanceId, value?.Trim() ?? string.Empty, "EC2InstanceId");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
        LocalizedAttributes.LocalizedDisplayName(nameof(Language.EC2Region)),
        LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEC2Region)),
        AttributeUsedInProtocol(ProtocolType.RDP, ProtocolType.SSH2)]
        public string EC2Region
        {
            get => GetPropertyValue("EC2Region", _ec2Region)?.Trim() ?? string.Empty;
            set => SetField(ref _ec2Region, value?.Trim() ?? string.Empty, "EC2Region");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.VmId)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVmId)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string VmId
        {
            get => GetPropertyValue("VmId", _vmId)?.Trim() ?? string.Empty;
            set => SetField(ref _vmId, value?.Trim() ?? string.Empty, "VmId");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.SshTunnel)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionSshTunnel)),
         TypeConverter(typeof(SshTunnelTypeConverter)),
         AttributeUsedInAllProtocolsExcept()]
        public string SSHTunnelConnectionName
        {
            get => GetPropertyValue("SSHTunnelConnectionName", _sshTunnelConnectionName)?.Trim() ?? string.Empty;
            set => SetField(ref _sshTunnelConnectionName, value?.Trim() ?? string.Empty, "SSHTunnelConnectionName");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
        LocalizedAttributes.LocalizedDisplayName(nameof(Language.OpeningCommand)),
        LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionOpeningCommand)),
           AttributeUsedInProtocol(ProtocolType.SSH1, ProtocolType.SSH2)]
        public virtual string OpeningCommand
        {
            get => GetPropertyValue("OpeningCommand", _openingCommand);
            set => SetField(ref _openingCommand, value, "OpeningCommand");
        }
        #endregion

        #region Protocol

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Protocol)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionProtocol)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter))]
        public virtual ProtocolType Protocol
        {
            get => GetPropertyValue("Protocol", _protocol);
            set => SetField(ref _protocol, value, "Protocol");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpVersion)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRdpVersion)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public virtual RdpVersion RdpVersion
        {
            get => GetPropertyValue("RdpVersion", _rdpProtocolVersion);
            set => SetField(ref _rdpProtocolVersion, value, nameof(RdpVersion));
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalTool)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalTool)),
         TypeConverter(typeof(ExternalToolsTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.IntApp)]
        public string ExtApp
        {
            get => GetPropertyValue("ExtApp", _extApp);
            set => SetField(ref _extApp, value, "ExtApp");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.PuttySession)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionPuttySession)),
         TypeConverter(typeof(Config.Putty.PuttySessionsManager.SessionList)),
         AttributeUsedInProtocol(ProtocolType.SSH1, ProtocolType.SSH2, ProtocolType.Telnet,
            ProtocolType.RAW, ProtocolType.Rlogin)]
        public virtual string PuttySession
        {
            get => GetPropertyValue("PuttySession", _puttySession);
            set => SetField(ref _puttySession, value, "PuttySession");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.SshOptions)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionSshOptions)),
         AttributeUsedInProtocol(ProtocolType.SSH1, ProtocolType.SSH2, ProtocolType.OpenSSH)]
        public virtual string SSHOptions
        {
            get => GetPropertyValue("SSHOptions", _sshOptions);
            set => SetField(ref _sshOptions, value, "SSHOptions");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("Private Key File"),
         Description("Path to a PuTTY private key (.ppk) file for SSH authentication. When set, the key is passed to PuTTY via the -i argument."),
         Editor(typeof(UI.Controls.ConnectionInfoPropertyGrid.PrivateKeyFileEditor), typeof(System.Drawing.Design.UITypeEditor)),
         AttributeUsedInProtocol(ProtocolType.SSH1, ProtocolType.SSH2, ProtocolType.OpenSSH)]
        public virtual string PrivateKeyPath
        {
            get => GetPropertyValue("PrivateKeyPath", _privateKeyPath);
            set => SetField(ref _privateKeyPath, value, "PrivateKeyPath");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseConsoleSession)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseConsoleSession)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseConsoleSession
        {
            get => GetPropertyValue("UseConsoleSession", _useConsoleSession);
            set => SetField(ref _useConsoleSession, value, "UseConsoleSession");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.AuthenticationLevel)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionAuthenticationLevel)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public AuthenticationLevel RDPAuthenticationLevel
        {
            get => GetPropertyValue("RDPAuthenticationLevel", _rdpAuthenticationLevel);
            set => SetField(ref _rdpAuthenticationLevel, value, "RDPAuthenticationLevel");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.MinutesToIdleTimeout)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDPMinutesToIdleTimeout)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public virtual int RDPMinutesToIdleTimeout
        {
            get => GetPropertyValue("RDPMinutesToIdleTimeout", _rdpMinutesToIdleTimeout);
            set
            {
                if (value < 0)
                    value = 0;
                else if (value > 240)
                    value = 240;
                SetField(ref _rdpMinutesToIdleTimeout, value, "RDPMinutesToIdleTimeout");
            }
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.MinutesToIdleTimeout)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDPAlertIdleTimeout)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RDPAlertIdleTimeout
        {
            get => GetPropertyValue("RDPAlertIdleTimeout", _rdpAlertIdleTimeout);
            set => SetField(ref _rdpAlertIdleTimeout, value, "RDPAlertIdleTimeout");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.LoadBalanceInfo)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionLoadBalanceInfo)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string LoadBalanceInfo
        {
            get => GetPropertyValue("LoadBalanceInfo", _loadBalanceInfo)?.Trim() ?? string.Empty;
            set => SetField(ref _loadBalanceInfo, value?.Trim() ?? string.Empty, "LoadBalanceInfo");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("RDP Sign Scope"),
         Description("The signscope value from a signed RDP file. Defines which connection properties are covered by the signature."),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDPSignScope
        {
            get => GetPropertyValue("RDPSignScope", _rdpSignScope);
            set => SetField(ref _rdpSignScope, value, "RDPSignScope");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("RDP Signature"),
         Description("The signature value from a signed RDP file. Used by RD Connection Broker to validate that connection settings have not been tampered with."),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDPSignature
        {
            get => GetPropertyValue("RDPSignature", _rdpSignature);
            set => SetField(ref _rdpSignature, value, "RDPSignature");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RenderingEngine)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRenderingEngine)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.HTTP, ProtocolType.HTTPS)]
        public HTTPBase.RenderingEngine RenderingEngine
        {
            get => GetPropertyValue("RenderingEngine", _renderingEngine);
            set => SetField(ref _renderingEngine, value, "RenderingEngine");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("Suppress Script Errors"),
         Description("If enabled, script errors in the browser will be suppressed."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.HTTP, ProtocolType.HTTPS)]
        public bool ScriptErrorsSuppressed
        {
            get => GetPropertyValue("ScriptErrorsSuppressed", _scriptErrorsSuppressed);
            set => SetField(ref _scriptErrorsSuppressed, value, "ScriptErrorsSuppressed");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("Use Persistent Browser"),
         Description("If enabled, browser cookies and data will be saved across sessions."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.HTTP, ProtocolType.HTTPS)]
        public bool UsePersistentBrowser
        {
            get => GetPropertyValue("UsePersistentBrowser", _usePersistentBrowser);
            set => SetField(ref _usePersistentBrowser, value, "UsePersistentBrowser");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         DisplayName("Show Navigation Bar"),
         Description("If enabled, a navigation bar with back/forward/refresh buttons and an address box is shown above the embedded browser."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.HTTP, ProtocolType.HTTPS)]
        public bool ShowBrowserNavigationBar
        {
            get => GetPropertyValue("ShowBrowserNavigationBar", _showBrowserNavigationBar);
            set => SetField(ref _showBrowserNavigationBar, value, "ShowBrowserNavigationBar");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseCredSsp)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseCredSsp)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseCredSsp
        {
            get => GetPropertyValue("UseCredSsp", _useCredSsp);
            set => SetField(ref _useCredSsp, value, "UseCredSsp");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseRestrictedAdmin)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseRestrictedAdmin)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseRestrictedAdmin
        {
            get => GetPropertyValue("UseRestrictedAdmin", _useRestrictedAdmin);
            set => SetField(ref _useRestrictedAdmin, value, "UseRestrictedAdmin");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseRCG)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseRCG)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseRCG
        {
            get => GetPropertyValue("UseRCG", _useRCG);
            set => SetField(ref _useRCG, value, "UseRCG");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseVmId)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseVmId)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseVmId
        {
            get => GetPropertyValue("UseVmId", _useVmId);
            set => SetField(ref _useVmId, value, "UseVmId");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Protocol), 3),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UseEnhancedMode)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUseEnhancedMode)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool UseEnhancedMode
        {
            get => GetPropertyValue("UseEnhancedMode", _useEnhancedMode);
            set => SetField(ref _useEnhancedMode, value, "UseEnhancedMode");
        }
        #endregion

        #region RD Gateway

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayUsageMethod)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRdpGatewayUsageMethod)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDGatewayUsageMethod RDGatewayUsageMethod
        {
            get => GetPropertyValue("RDGatewayUsageMethod", _rdGatewayUsageMethod);
            set => SetField(ref _rdGatewayUsageMethod, value, "RDGatewayUsageMethod");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayHostname)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDGatewayHostname)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDGatewayHostname
        {
            get => GetPropertyValue("RDGatewayHostname", _rdGatewayHostname)?.Trim() ?? string.Empty;
            set => SetField(ref _rdGatewayHostname, value?.Trim() ?? string.Empty, "RDGatewayHostname");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayUseConnectionCredentials)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDGatewayUseConnectionCredentials)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDGatewayUseConnectionCredentials RDGatewayUseConnectionCredentials
        {
            get => GetPropertyValue("RDGatewayUseConnectionCredentials", _rdGatewayUseConnectionCredentials);
            set => SetField(ref _rdGatewayUseConnectionCredentials, value, "RDGatewayUseConnectionCredentials");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayUsername)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDGatewayUsername)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDGatewayUsername
        {
            get => GetPropertyValue("RDGatewayUsername", _rdGatewayUsername)?.Trim() ?? string.Empty;
            set => SetField(ref _rdGatewayUsername, value?.Trim() ?? string.Empty, "RDGatewayUsername");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayPassword)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRdpGatewayPassword)),
         PasswordPropertyText(true),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDGatewayPassword
        {
            get => GetPropertyValue("RDGatewayPassword", _rdGatewayPassword);
            set => SetField(ref _rdGatewayPassword, value, "RDGatewayPassword");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
        LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayAccessToken)),
        LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRdpGatewayAccessToken)),
        PasswordPropertyText(true),
        AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDGatewayAccessToken
        {
            get => GetPropertyValue("RDGatewayAccessToken", _rdGatewayAccessToken);
            set => SetField(ref _rdGatewayAccessToken, value, "RDGatewayAccessToken");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RdpGatewayDomain)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDGatewayDomain)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RDGatewayDomain
        {
            get => GetPropertyValue("RDGatewayDomain", _rdGatewayDomain)?.Trim() ?? string.Empty;
            set => SetField(ref _rdGatewayDomain, value?.Trim() ?? string.Empty, "RDGatewayDomain");
        }
        // external credential provider selector for rd gateway
        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalCredentialProvider)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalCredentialProvider)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public ExternalCredentialProvider RDGatewayExternalCredentialProvider
        {
            get => GetPropertyValue("RDGatewayExternalCredentialProvider", _rdGatewayExternalCredentialProvider);
            set => SetField(ref _rdGatewayExternalCredentialProvider, value, "RDGatewayExternalCredentialProvider");
        }

        // credential record identifier for external credential provider
        [LocalizedAttributes.LocalizedCategory(nameof(Language.RDPGateway), 4),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UserViaAPI)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUserViaAPI)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public virtual string RDGatewayUserViaAPI
        {
            get => GetPropertyValue("RDGatewayUserViaAPI", _rdGatewayUserViaAPI);
            set => SetField(ref _rdGatewayUserViaAPI, value, "RDGatewayUserViaAPI");
        }
        #endregion

        #region Appearance

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Resolution)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionResolution)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPResolutions Resolution
        {
            get => GetPropertyValue("Resolution", _resolution);
            set => SetField(ref _resolution, value, "Resolution");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         DisplayName("Sizing Mode"),
         Description("Controls how the remote desktop is scaled to fit the panel. SmartSize stretches to fill; SmartSize (Aspect Ratio) preserves aspect ratio."),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPSizingMode RDPSizingMode
        {
            get => GetPropertyValue("RDPSizingMode", _rdpSizingMode);
            set => SetField(ref _rdpSizingMode, value, "RDPSizingMode");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         DisplayName("Resolution Width"),
         Description("Custom resolution width in pixels (used when Resolution is set to Custom)."),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public int ResolutionWidth
        {
            get => GetPropertyValue("ResolutionWidth", _resolutionWidth);
            set => SetField(ref _resolutionWidth, value, "ResolutionWidth");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         DisplayName("Resolution Height"),
         Description("Custom resolution height in pixels (used when Resolution is set to Custom)."),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public int ResolutionHeight
        {
            get => GetPropertyValue("ResolutionHeight", _resolutionHeight);
            set => SetField(ref _resolutionHeight, value, "ResolutionHeight");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         DisplayName("Desktop Scale Factor"),
         Description("The scaling factor to use for the remote desktop session. 'Auto' matches the local display scaling."),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPDesktopScaleFactor DesktopScaleFactor
        {
            get => GetPropertyValue("DesktopScaleFactor", _desktopScaleFactor);
            set => SetField(ref _desktopScaleFactor, value, "DesktopScaleFactor");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.AutomaticResize)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionAutomaticResize)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool AutomaticResize
        {
            get => GetPropertyValue("AutomaticResize", _automaticResize);
            set => SetField(ref _automaticResize, value, "AutomaticResize");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         DisplayName("Use Multiple Monitors"),
         Description("When enabled and connecting in fullscreen, the RDP session spans all local monitors. Requires RDP 8.1 or later."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RDPUseMultimon
        {
            get => GetPropertyValue("RDPUseMultimon", _rdpUseMultimon);
            set => SetField(ref _rdpUseMultimon, value, "RDPUseMultimon");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Colors)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionColors)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPColors Colors
        {
            get => GetPropertyValue("Colors", _colors);
            set => SetField(ref _colors, value, "Colors");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.CacheBitmaps)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionCacheBitmaps)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool CacheBitmaps
        {
            get => GetPropertyValue("CacheBitmaps", _cacheBitmaps);
            set => SetField(ref _cacheBitmaps, value, "CacheBitmaps");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisplayWallpaper)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisplayWallpaper)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisplayWallpaper
        {
            get => GetPropertyValue("DisplayWallpaper", _displayWallpaper);
            set => SetField(ref _displayWallpaper, value, "DisplayWallpaper");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisplayThemes)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisplayThemes)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisplayThemes
        {
            get => GetPropertyValue("DisplayThemes", _displayThemes);
            set => SetField(ref _displayThemes, value, "DisplayThemes");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.FontSmoothing)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEnableFontSmoothing)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool EnableFontSmoothing
        {
            get => GetPropertyValue("EnableFontSmoothing", _enableFontSmoothing);
            set => SetField(ref _enableFontSmoothing, value, "EnableFontSmoothing");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.EnableDesktopComposition)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEnableDesktopComposition)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool EnableDesktopComposition
        {
            get => GetPropertyValue("EnableDesktopComposition", _enableDesktopComposition);
            set => SetField(ref _enableDesktopComposition, value, "EnableDesktopComposition");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisableFullWindowDrag)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisableFullWindowDrag)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisableFullWindowDrag
        {
            get => GetPropertyValue("DisableFullWindowDrag", _disableFullWindowDrag);
            set => SetField(ref _disableFullWindowDrag, value, "DisableFullWindowDrag");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisableMenuAnimations)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisableMenuAnimations)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisableMenuAnimations
        {
            get => GetPropertyValue("DisableMenuAnimations", _disableMenuAnimations);
            set => SetField(ref _disableMenuAnimations, value, "DisableMenuAnimations");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisableCursorShadow)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisableCursorShadow)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisableCursorShadow
        {
            get => GetPropertyValue("DisableCursorShadow", _disableCursorShadow);
            set => SetField(ref _disableCursorShadow, value, "DisableCursorShadow");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DisableCursorShadow)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionDisableCursorShadow)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool DisableCursorBlinking
        {
            get => GetPropertyValue("DisableCursorBlinking", _disableCursorBlinking);
            set => SetField(ref _disableCursorBlinking, value, "DisableCursorBlinking");
        }
        #endregion

        #region Redirect

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RedirectKeys)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectKeys)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectKeys
        {
            get => GetPropertyValue("RedirectKeys", _redirectKeys);
            set => SetField(ref _redirectKeys, value, "RedirectKeys");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.DiskDrives)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectDrives)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPDiskDrives RedirectDiskDrives
        {
            get => GetPropertyValue("RedirectDiskDrives", _redirectDiskDrives);
            set => SetField(ref _redirectDiskDrives, value, "RedirectDiskDrives");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RedirectDiskDrivesCustom)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectDiskDrivesCustom)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public string RedirectDiskDrivesCustom
        {
            get => GetPropertyValue("RedirectDiskDrivesCustom", _redirectDiskDrivesCustom);
            set => SetField(ref _redirectDiskDrivesCustom, value, "RedirectDiskDrivesCustom");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Printers)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectPrinters)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectPrinters
        {
            get => GetPropertyValue("RedirectPrinters", _redirectPrinters);
            set => SetField(ref _redirectPrinters, value, "RedirectPrinters");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Clipboard)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectClipboard)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectClipboard
        {
            get => GetPropertyValue("RedirectClipboard", _redirectClipboard);
            set => SetField(ref _redirectClipboard, value, "RedirectClipboard");
        }


        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Ports)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectPorts)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectPorts
        {
            get => GetPropertyValue("RedirectPorts", _redirectPorts);
            set => SetField(ref _redirectPorts, value, "RedirectPorts");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.SmartCard)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectSmartCards)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectSmartCards
        {
            get => GetPropertyValue("RedirectSmartCards", _redirectSmartCards);
            set => SetField(ref _redirectSmartCards, value, "RedirectSmartCards");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Sounds)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectSounds)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPSounds RedirectSound
        {
            get => GetPropertyValue("RedirectSound", _redirectSound);
            set => SetField(ref _redirectSound, value, "RedirectSound");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.SoundQuality)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionSoundQuality)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public RDPSoundQuality SoundQuality
        {
            get => GetPropertyValue("SoundQuality", _soundQuality);
            set => SetField(ref _soundQuality, value, "SoundQuality");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.AudioCapture)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRedirectAudioCapture)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public bool RedirectAudioCapture
        {
            get => GetPropertyValue("RedirectAudioCapture", _redirectAudioCapture);
            set => SetField(ref _redirectAudioCapture, value, nameof(RedirectAudioCapture));
        }

        #endregion

        #region Misc

        [Browsable(false)] public string ConstantID { get; } = uniqueId.ThrowIfNullOrEmpty(nameof(uniqueId));

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalToolBefore)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalToolBefore)),
         TypeConverter(typeof(ExternalToolsTypeConverter))]
        public virtual string PreExtApp
        {
            get => GetPropertyValue("PreExtApp", _preExtApp);
            set => SetField(ref _preExtApp, value, "PreExtApp");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ExternalToolAfter)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionExternalToolAfter)),
         TypeConverter(typeof(ExternalToolsTypeConverter))]
        public virtual string PostExtApp
        {
            get => GetPropertyValue("PostExtApp", _postExtApp);
            set => SetField(ref _postExtApp, value, "PostExtApp");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.MacAddress)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionMACAddress))]
        public virtual string MacAddress
        {
            get => GetPropertyValue("MacAddress", _macAddress);
            set => SetField(ref _macAddress, value, "MacAddress");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.UserField)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionUser1))]
        public virtual string UserField
        {
            get => GetPropertyValue("UserField", _userField);
            set => SetField(ref _userField, value, "UserField");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 1"),
         Description("Additional user-defined field 1 for custom data. Available as %USERFIELD1% token in external tools.")]
        public virtual string UserField1
        {
            get => GetPropertyValue("UserField1", _userField1);
            set => SetField(ref _userField1, value, "UserField1");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 2"),
         Description("Additional user-defined field 2 for custom data. Available as %USERFIELD2% token in external tools.")]
        public virtual string UserField2
        {
            get => GetPropertyValue("UserField2", _userField2);
            set => SetField(ref _userField2, value, "UserField2");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 3"),
         Description("Additional user-defined field 3 for custom data. Available as %USERFIELD3% token in external tools.")]
        public virtual string UserField3
        {
            get => GetPropertyValue("UserField3", _userField3);
            set => SetField(ref _userField3, value, "UserField3");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 4"),
         Description("Additional user-defined field 4 for custom data. Available as %USERFIELD4% token in external tools.")]
        public virtual string UserField4
        {
            get => GetPropertyValue("UserField4", _userField4);
            set => SetField(ref _userField4, value, "UserField4");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 5"),
         Description("Additional user-defined field 5 for custom data. Available as %USERFIELD5% token in external tools.")]
        public virtual string UserField5
        {
            get => GetPropertyValue("UserField5", _userField5);
            set => SetField(ref _userField5, value, "UserField5");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 6"),
         Description("Additional user-defined field 6 for custom data. Available as %USERFIELD6% token in external tools.")]
        public virtual string UserField6
        {
            get => GetPropertyValue("UserField6", _userField6);
            set => SetField(ref _userField6, value, "UserField6");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 7"),
         Description("Additional user-defined field 7 for custom data. Available as %USERFIELD7% token in external tools.")]
        public virtual string UserField7
        {
            get => GetPropertyValue("UserField7", _userField7);
            set => SetField(ref _userField7, value, "UserField7");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 8"),
         Description("Additional user-defined field 8 for custom data. Available as %USERFIELD8% token in external tools.")]
        public virtual string UserField8
        {
            get => GetPropertyValue("UserField8", _userField8);
            set => SetField(ref _userField8, value, "UserField8");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 9"),
         Description("Additional user-defined field 9 for custom data. Available as %USERFIELD9% token in external tools.")]
        public virtual string UserField9
        {
            get => GetPropertyValue("UserField9", _userField9);
            set => SetField(ref _userField9, value, "UserField9");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("User Field 10"),
         Description("Additional user-defined field 10 for custom data. Available as %USERFIELD10% token in external tools.")]
        public virtual string UserField10
        {
            get => GetPropertyValue("UserField10", _userField10);
            set => SetField(ref _userField10, value, "UserField10");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("Notes"),
         Description("Free-form multiline notes for this connection.")]
        public virtual string Notes
        {
            get => GetPropertyValue("Notes", _notes);
            set => SetField(ref _notes, value, "Notes");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.EnvironmentTags)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEnvironmentTags))]
        public virtual string EnvironmentTags
        {
            get => GetPropertyValue("EnvironmentTags", _environmentTags);
            set => SetField(ref _environmentTags, value, "EnvironmentTags");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.Favorite)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionFavorite)),
            TypeConverter(typeof(MiscTools.YesNoTypeConverter))]
        public virtual bool Favorite
        {
            get => GetPropertyValue("Favorite", _favorite);
            set => SetField(ref _favorite, value, "Favorite");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         DisplayName("Retry On First Connect"),
         Description("If enabled, the reconnect dialog will be shown when the initial connection attempt fails, polling the server until it becomes available."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInAllProtocolsExcept()]
        public bool RetryOnFirstConnect
        {
            get => GetPropertyValue("RetryOnFirstConnect", _retryOnFirstConnect);
            set => SetField(ref _retryOnFirstConnect, value, "RetryOnFirstConnect");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         DisplayName("Always Prompt For Credentials"),
         Description("If enabled, a credential dialog will be shown every time this connection is opened, instead of using stored credentials."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInAllProtocolsExcept(ProtocolType.Telnet, ProtocolType.Rlogin, ProtocolType.RAW, ProtocolType.MSRA)]
        public bool AlwaysPromptForCredentials
        {
            get => GetPropertyValue("AlwaysPromptForCredentials", _alwaysPromptForCredentials);
            set => SetField(ref _alwaysPromptForCredentials, value, "AlwaysPromptForCredentials");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RDPStartProgram)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDPStartProgram)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public virtual string RDPStartProgram
        {
            get => GetPropertyValue("RDPStartProgram", _rdpStartProgram);
            set => SetField(ref _rdpStartProgram, value, "RDPStartProgram");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Miscellaneous), 7),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.RDPStartProgramWorkDir)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionRDPStartProgramWorkDir)),
         AttributeUsedInProtocol(ProtocolType.RDP)]
        public virtual string RDPStartProgramWorkDir
        {
            get => GetPropertyValue("RDPStartProgramWorkDir", _rdpStartProgramWorkDir);
            set => SetField(ref _rdpStartProgramWorkDir, value, "RDPStartProgramWorkDir");
        }

        #endregion

        #region VNC
        // TODO: it seems all these VNC properties were added and serialized but
        // never hooked up to the VNC protocol or shown to the user
        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Compression)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionCompression)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
         Browsable(false)]
        public ProtocolVNC.Compression VNCCompression
        {
            get => GetPropertyValue("VNCCompression", _vncCompression);
            set => SetField(ref _vncCompression, value, "VNCCompression");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Encoding)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionEncoding)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
         Browsable(false)]
        public ProtocolVNC.Encoding VNCEncoding
        {
            get => GetPropertyValue("VNCEncoding", _vncEncoding);
            set => SetField(ref _vncEncoding, value, "VNCEncoding");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Connection), 2),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.AuthenticationMode)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionAuthenticationMode)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
         Browsable(false)]
        public ProtocolVNC.AuthMode VNCAuthMode
        {
            get => GetPropertyValue("VNCAuthMode", _vncAuthMode);
            set => SetField(ref _vncAuthMode, value, "VNCAuthMode");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Proxy), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.ProxyType)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVNCProxyType)),
            TypeConverter(typeof(MiscTools.EnumTypeConverter)),
            AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
            Browsable(false)]
        public ProtocolVNC.ProxyType VNCProxyType
        {
            get => GetPropertyValue("VNCProxyType", _vncProxyType);
            set => SetField(ref _vncProxyType, value, "VNCProxyType");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Proxy), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.ProxyAddress)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVNCProxyAddress)),
            AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
            Browsable(false)]
        public string VNCProxyIP
        {
            get => GetPropertyValue("VNCProxyIP", _vncProxyIp);
            set => SetField(ref _vncProxyIp, value, "VNCProxyIP");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Proxy), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.ProxyPort)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVNCProxyPort)),
            AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
            Browsable(false)]
        public int VNCProxyPort
        {
            get => GetPropertyValue("VNCProxyPort", _vncProxyPort);
            set => SetField(ref _vncProxyPort, value, "VNCProxyPort");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Proxy), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.ProxyUsername)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVNCProxyUsername)),
            AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
            Browsable(false)]
        public string VNCProxyUsername
        {
            get => GetPropertyValue("VNCProxyUsername", _vncProxyUsername);
            set => SetField(ref _vncProxyUsername, value, "VNCProxyUsername");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Proxy), 7),
            LocalizedAttributes.LocalizedDisplayName(nameof(Language.ProxyPassword)),
            LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionVNCProxyPassword)),
            PasswordPropertyText(true),
            AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD),
            Browsable(false)]
        public string VNCProxyPassword
        {
            get => GetPropertyValue("VNCProxyPassword", _vncProxyPassword);
            set => SetField(ref _vncProxyPassword, value, "VNCProxyPassword");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.Colors)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionColors)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD)]
        public ProtocolVNC.Colors VNCColors
        {
            get => GetPropertyValue("VNCColors", _vncColors);
            set => SetField(ref _vncColors, value, "VNCColors");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.SmartSizeMode)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionSmartSizeMode)),
         TypeConverter(typeof(MiscTools.EnumTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD)]
        public ProtocolVNC.SmartSizeMode VNCSmartSizeMode
        {
            get => GetPropertyValue("VNCSmartSizeMode", _vncSmartSizeMode);
            set => SetField(ref _vncSmartSizeMode, value, "VNCSmartSizeMode");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Appearance), 5),
         LocalizedAttributes.LocalizedDisplayName(nameof(Language.ViewOnly)),
         LocalizedAttributes.LocalizedDescription(nameof(Language.PropertyDescriptionViewOnly)),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD)]
        public bool VNCViewOnly
        {
            get => GetPropertyValue("VNCViewOnly", _vncViewOnly);
            set => SetField(ref _vncViewOnly, value, "VNCViewOnly");
        }

        [LocalizedAttributes.LocalizedCategory(nameof(Language.Redirect), 6),
         Browsable(true),
         DisplayName("VNC Clipboard Redirect"),
         Description("If enabled, the local clipboard is shared with the remote VNC server."),
         TypeConverter(typeof(MiscTools.YesNoTypeConverter)),
         AttributeUsedInProtocol(ProtocolType.VNC, ProtocolType.ARD)]
        public bool VNCClipboardRedirect
        {
            get => GetPropertyValue("VNCClipboardRedirect", _vncClipboardRedirect);
            set => SetField(ref _vncClipboardRedirect, value, "VNCClipboardRedirect");
        }

        #endregion
        #endregion

        protected virtual TPropertyType GetPropertyValue<TPropertyType>(string propertyName, TPropertyType value)
        {
            var result = GetType().GetProperty(propertyName)?.GetValue(this, null);
            return result is TPropertyType typed ? typed : value;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void RaisePropertyChangedEvent(object sender, PropertyChangedEventArgs args)
        {
            PropertyChanged?.Invoke(sender, new PropertyChangedEventArgs(args.PropertyName));
        }

        protected void SetField<T>(ref T field, T value, string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
            RaisePropertyChangedEvent(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics; // Added
using System.Globalization;
using System.Security;
using System.Windows.Forms;
using System.Xml;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Container;
using mRemoteNG.Messages;
using mRemoteNG.Security;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.TaskDialog;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Xml
{
    [SupportedOSPlatform("windows")]
    public class XmlConnectionsDeserializer(string connectionFileName = "", Func<Optional<SecureString>>? authenticationRequestor = null) : IDeserializer<string, ConnectionTreeModel>
    {
        private XmlDocument _xmlDocument = null!;
        private double _confVersion;
        private XmlConnectionsDecryptor _decryptor = null!;
        private readonly string ConnectionFileName = connectionFileName;
        private const double MaxSupportedConfVersion = 2.8;
        private readonly RootNodeInfo _rootNodeInfo = new(RootNodeType.Connection);
        private ConnectionTreeModel _connectionTreeModel = null!;
        private BlockCipherEngines _cipherEngine;
        private BlockCipherModes _cipherMode;
        private int _kdfIterations;
        private readonly List<(Action<string> Setter, string CipherText)> _pendingDecrypts = [];

        public Func<Optional<SecureString>>? AuthenticationRequestor { get; set; } = authenticationRequestor;

        public ConnectionTreeModel Deserialize(string xml)
        {
            return Deserialize(xml, false)!;
        }

        public ConnectionTreeModel? Deserialize(string xml, bool import)
        {
            if (string.IsNullOrEmpty(xml)) return null;

            var stopwatch = Stopwatch.StartNew();
            var phaseSw = new Stopwatch();

            try
            {
                _rootNodeInfo.Filename = ConnectionFileName;

                phaseSw.Restart();
                LoadXmlConnectionData(xml);
                ValidateConnectionFileVersion();
                long parseMs = phaseSw.ElapsedMilliseconds;

                XmlElement rootXmlElement = _xmlDocument.DocumentElement
                    ?? throw new XmlException("Failed to parse XML connection file.");
                InitializeRootNode(rootXmlElement);
                CreateDecryptor(_rootNodeInfo, rootXmlElement);
                _connectionTreeModel = new ConnectionTreeModel();
                _connectionTreeModel.AddRootNode(_rootNodeInfo);

                phaseSw.Restart();
                if (_confVersion > 1.3)
                {
                    string protectedString = _xmlDocument.DocumentElement?.Attributes["Protected"]?.Value ?? string.Empty;
                    if (!_decryptor.ConnectionsFileIsAuthentic(protectedString, _rootNodeInfo.PasswordString.ConvertToSecureString()))
                    {
                        return null;
                    }
                }
                long authMs = phaseSw.ElapsedMilliseconds;

                phaseSw.Restart();
                bool fullFileEncryptionValue = false;
                int innerTextLength = 0;
                int decryptedLength = 0;
                if (_confVersion >= 2.6)
                {
                    fullFileEncryptionValue = rootXmlElement.GetAttributeAsBool("FullFileEncryption");
                    innerTextLength = rootXmlElement.InnerText?.Length ?? 0;
                    if (fullFileEncryptionValue)
                    {
                        string decryptedContent = _decryptor.Decrypt(rootXmlElement.InnerText ?? string.Empty);
                        decryptedLength = decryptedContent?.Length ?? 0;
                        rootXmlElement.InnerXml = decryptedContent ?? string.Empty;
                    }
                }
                long fullDecryptMs = phaseSw.ElapsedMilliseconds;

                phaseSw.Restart();
                _pendingDecrypts.Clear();
                AddNodesFromXmlRecursive(rootXmlElement, _rootNodeInfo);
                long nodesMs = phaseSw.ElapsedMilliseconds;

                phaseSw.Restart();
                ProcessPendingDecrypts();
                long batchDecryptMs = phaseSw.ElapsedMilliseconds;

                // Safety net: refuse to hand back an empty tree when the source input
                // was non-trivially large. A downstream AutoSave would otherwise persist
                // the empty tree over the user's file and lose all data. Only kicks in
                // for FullFileEncryption files where ciphertext was expected to decrypt
                // to connections but produced nothing (e.g. wrong password path taken,
                // decrypt silently returned empty, attribute misread).
                int nodeCount = _rootNodeInfo.Children.Count;
                if (nodeCount == 0 && fullFileEncryptionValue && innerTextLength > 1024)
                {
                    stopwatch.Stop();
                    throw new InvalidOperationException(
                        $"Connection file decrypted to 0 nodes despite {innerTextLength} bytes of ciphertext "
                        + $"(decrypted: {decryptedLength} bytes). Refusing to load to prevent data loss. "
                        + "Verify password or restore from backup.");
                }

                if (!import)
                    Runtime.ConnectionsService.IsConnectionsFileLoaded = true;

                stopwatch.Stop();
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"[Deser] XML parse: {parseMs}ms, Auth: {authMs}ms, FullDecrypt: {fullDecryptMs}ms, "
                    + $"Nodes: {nodesMs}ms ({nodeCount} root-level), BatchDecrypt({_pendingDecrypts.Count} fields): {batchDecryptMs}ms, "
                    + $"InnerText: {innerTextLength}B, Decrypted: {decryptedLength}B, FullEnc: {fullFileEncryptionValue}, "
                    + $"Total: {stopwatch.ElapsedMilliseconds}ms");

                return _connectionTreeModel;
            }
            catch (Exception ex)
            {
                Runtime.ConnectionsService.IsConnectionsFileLoaded = false;
                Runtime.MessageCollector.AddExceptionStackTrace(Language.LoadFromXmlFailed, ex);

                stopwatch.Stop();
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, $"Connection deserialization failed after {stopwatch.ElapsedMilliseconds} ms.");

                throw;
            }
        }

        private void ProcessPendingDecrypts()
        {
            if (_pendingDecrypts.Count == 0) return;

            string[] cipherTexts = new string[_pendingDecrypts.Count];
            for (int i = 0; i < _pendingDecrypts.Count; i++)
                cipherTexts[i] = _pendingDecrypts[i].CipherText;

            string[] plainTexts = _decryptor.DecryptBatch(cipherTexts);

            for (int i = 0; i < _pendingDecrypts.Count; i++)
                _pendingDecrypts[i].Setter(plainTexts[i]);
        }

        private void LoadXmlConnectionData(string connections)
        {
            CreateDecryptor(new RootNodeInfo(RootNodeType.Connection));
            connections = _decryptor.LegacyFullFileDecrypt(connections);
            if (connections != "")
            {
                _xmlDocument = SecureXmlHelper.LoadXmlFromString(connections);
            }
        }

        private void ValidateConnectionFileVersion()
        {
            if (_xmlDocument?.DocumentElement == null)
                throw new XmlException("Failed to parse XML connection file.");

            if (_xmlDocument.DocumentElement != null && _xmlDocument.DocumentElement.HasAttribute("ConfVersion"))
                _confVersion = Convert.ToDouble(_xmlDocument.DocumentElement.Attributes["ConfVersion"]?.Value.Replace(",", ".", StringComparison.Ordinal), CultureInfo.InvariantCulture);
            else
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, Language.OldConffile);

            if (!(_confVersion > MaxSupportedConfVersion)) return;
            ShowIncompatibleVersionDialogBox();
            throw new NotSupportedException($"Incompatible connection file format (file format version {_confVersion}).");
        }

        private void ShowIncompatibleVersionDialogBox()
        {
            CTaskDialog.ShowTaskDialogBox(FrmMain.Default, Application.ProductName ?? "mRemoteNG", "Incompatible connection file format", $"The format of this connection file is not supported. Please upgrade to a newer version of {Application.ProductName}.",
                                          string.Format(CultureInfo.InvariantCulture, "{1}{0}File Format Version: {2}{0}Highest Supported Version: {3}", Environment.NewLine, ConnectionFileName, _confVersion, MaxSupportedConfVersion),
                                          "", "", "", "", ETaskDialogButtons.Ok, ESysIcons.Error, ESysIcons.Error);
        }

        private void InitializeRootNode(XmlElement connectionsRootElement)
        {
            _rootNodeInfo.Name = connectionsRootElement.Attributes?["Name"]?.Value?.Trim() ?? string.Empty;
            _rootNodeInfo.AutoLockOnMinimize = connectionsRootElement.GetAttributeAsBool("AutoLockOnMinimize");
        }

        private void CreateDecryptor(RootNodeInfo rootNodeInfo, XmlElement? connectionsRootElement = null)
        {
            if (_confVersion >= 2.6 && connectionsRootElement != null)
            {
                _cipherEngine = connectionsRootElement.GetAttributeAsEnum<BlockCipherEngines>("EncryptionEngine");
                _cipherMode = connectionsRootElement.GetAttributeAsEnum<BlockCipherModes>("BlockCipherMode");
                _kdfIterations = connectionsRootElement.GetAttributeAsInt("KdfIterations");

                _decryptor = new XmlConnectionsDecryptor(_cipherEngine, _cipherMode, rootNodeInfo)
                {
                    AuthenticationRequestor = AuthenticationRequestor,
                    KeyDerivationIterations = _kdfIterations
                };
            }
            else
            {
                _decryptor = new XmlConnectionsDecryptor(_rootNodeInfo)
                {
                    AuthenticationRequestor = AuthenticationRequestor
                };
            }
        }

        private void AddNodesFromXmlRecursive(XmlNode parentXmlNode, ContainerInfo parentContainer)
        {
            try
            {
                if (!parentXmlNode.HasChildNodes) return;
                foreach (XmlNode xmlNode in parentXmlNode.ChildNodes)
                {
                    TreeNodeType nodeType = xmlNode.GetAttributeAsEnum("Type", TreeNodeType.Connection);

                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (nodeType)
                    {
                        case TreeNodeType.Connection:
                            ConnectionInfo? connectionInfo = GetConnectionInfoFromXml(xmlNode);
                            if (connectionInfo != null)
                                parentContainer.AddChild(connectionInfo);
                            break;
                        case TreeNodeType.Container:
                        case TreeNodeType.Entity:
                            ContainerInfo containerInfo = new();
                            if (nodeType == TreeNodeType.Entity)
                                containerInfo.IsEntity = true;

                            if (_confVersion >= 0.9)
                            {
                                ConnectionInfo? containerProps = GetConnectionInfoFromXml(xmlNode);
                                if (containerProps != null)
                                    containerInfo.CopyFrom(containerProps);
                            }
                            if (_confVersion >= 0.8)
                            {
                                containerInfo.IsExpanded = xmlNode.GetAttributeAsBool("Expanded");
                            }

                            if (_confVersion >= 2.8)
                            {
                                containerInfo.AutoSort = xmlNode.GetAttributeAsBool("AutoSort");
                                DeferDecrypt(v => containerInfo.ContainerPassword = v, xmlNode, "ContainerPassword");
                                containerInfo.DynamicSource = xmlNode.GetAttributeAsEnum("DynamicSource", DynamicSourceType.None);
                                containerInfo.DynamicSourceValue = xmlNode.GetAttributeAsString("DynamicSourceValue");
                                containerInfo.DynamicRefreshInterval = xmlNode.GetAttributeAsInt("DynamicRefreshInterval");
                            }

                            if (containerInfo.IsRoot)
                                _connectionTreeModel.AddRootNode(containerInfo);
                            else
                                parentContainer.AddChild(containerInfo);

                            AddNodesFromXmlRecursive(xmlNode, containerInfo);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.AddNodeFromXmlFailed, ex);
                throw;
            }
        }

        private ConnectionInfo? GetConnectionInfoFromXml(XmlNode xmlnode)
        {
            if (xmlnode?.Attributes == null)
                return null;

            // Pre-build attribute dictionary for O(1) lookups instead of O(n) per attribute.
            // With 258 attributes per connection × 200 connections, this avoids 51,600 linear scans.
            var a = xmlnode.BuildAttributeDictionary();

            string connectionId = a.GetAttr("Id");
            if (string.IsNullOrWhiteSpace(connectionId))
                connectionId = Guid.NewGuid().ToString();
            ConnectionInfo connectionInfo = new(connectionId)
            {
                LinkedConnectionId = a.GetAttr("LinkedConnectionId")
            };

            try
            {
                if (_confVersion >= 0.2)
                {
                    connectionInfo.Name = a.GetAttr("Name");
                    connectionInfo.Description = a.GetAttr("Descr");
                    connectionInfo.Hostname = a.GetAttr("Hostname");
                    connectionInfo.AlternativeAddress = a.GetAttr("AlternativeAddress");
                    connectionInfo.DisplayWallpaper = a.GetAttrBool("DisplayWallpaper");
                    connectionInfo.DisplayThemes = a.GetAttrBool("DisplayThemes");
                    connectionInfo.CacheBitmaps = a.GetAttrBool("CacheBitmaps");

                    if (_confVersion < 1.1) //1.0 - 0.1
                    {
                        connectionInfo.Resolution = a.GetAttrBool("Fullscreen")
                            ? RDPResolutions.Fullscreen
                            : RDPResolutions.FitToWindow;
                    }

                    if (!Runtime.UseCredentialManager || _confVersion <= 2.6) // 0.2 - 2.6
                    {
                        connectionInfo.Username = a.GetAttr("Username");
                        DeferDecrypt(v => connectionInfo.Password = v, a, "Password");
                        connectionInfo.Domain = a.GetAttr("Domain");
                    }
                }

                if (_confVersion >= 0.3)
                {
                    if (_confVersion < 0.7)
                    {
                        if (a.GetAttrBool("UseVNC"))
                        {
                            connectionInfo.Protocol = ProtocolType.VNC;
                            connectionInfo.Port = a.GetAttrInt("VNCPort");
                        }
                        else
                        {
                            connectionInfo.Protocol = ProtocolType.RDP;
                        }
                    }
                }
                else
                {
                    connectionInfo.Port = (int)RdpProtocol.Defaults.Port;
                    connectionInfo.Protocol = ProtocolType.RDP;
                }

                if (_confVersion >= 0.4)
                {
                    if (_confVersion < 0.7)
                    {
                        connectionInfo.Port = a.GetAttrBool("UseVNC")
                            ? a.GetAttrInt("VNCPort")
                            : a.GetAttrInt("RDPPort");
                    }

                    connectionInfo.UseConsoleSession = a.GetAttrBool("ConnectToConsole");
                }
                else
                {
                    if (_confVersion < 0.7)
                    {
                        if (a.GetAttrBool("UseVNC"))
                            connectionInfo.Port = (int)ProtocolVNC.Defaults.Port;
                        else
                            connectionInfo.Port = (int)RdpProtocol.Defaults.Port;
                    }

                    connectionInfo.UseConsoleSession = false;
                }

                if (_confVersion >= 0.5)
                {
                    connectionInfo.RedirectPrinters = a.GetAttrBool("RedirectPrinters");
                    connectionInfo.RedirectPorts = a.GetAttrBool("RedirectPorts");
                    connectionInfo.RedirectSmartCards = a.GetAttrBool("RedirectSmartCards");
                }
                else
                {
                    connectionInfo.RedirectDiskDrives = RDPDiskDrives.None;
                    connectionInfo.RedirectPrinters = false;
                    connectionInfo.RedirectPorts = false;
                    connectionInfo.RedirectSmartCards = false;
                }

                if (_confVersion >= 0.7)
                {
                    connectionInfo.Protocol = xmlnode.GetAttributeAsEnum<ProtocolType>("Protocol");
                    connectionInfo.Port = a.GetAttrInt("Port");
                }

                if (_confVersion >= 1.0)
                {
                    connectionInfo.RedirectKeys = a.GetAttrBool("RedirectKeys");
                }

                if (_confVersion >= 1.2)
                {
                    connectionInfo.PuttySession = a.GetAttr("PuttySession");
                }

                if (_confVersion >= 1.3)
                {
                    connectionInfo.Colors = xmlnode.GetAttributeAsEnum<RDPColors>("Colors");
                    connectionInfo.Resolution = xmlnode.GetAttributeAsEnum<RDPResolutions>("Resolution");
                    connectionInfo.RedirectSound = xmlnode.GetAttributeAsEnum<RDPSounds>("RedirectSound");
                    connectionInfo.RedirectAudioCapture = a.GetAttrBool("RedirectAudioCapture");
                    connectionInfo.RedirectWebAuthn = a.GetAttrBool("RedirectWebAuthn");
                    connectionInfo.EnableRdsAadAuth = a.GetAttrBool("EnableRdsAadAuth");
                }
                else
                {
                    connectionInfo.Colors = a.GetAttrInt("Colors") switch
                    {
                        0 => RDPColors.Colors256,
                        1 => RDPColors.Colors16Bit,
                        2 => RDPColors.Colors24Bit,
                        3 => RDPColors.Colors32Bit,
                        // ReSharper disable once RedundantCaseLabel
                        _ => RDPColors.Colors15Bit,
                    };
                    connectionInfo.RedirectSound = xmlnode.GetAttributeAsEnum<RDPSounds>("RedirectSound");
                    connectionInfo.RedirectAudioCapture = a.GetAttrBool("RedirectAudioCapture");
                }

                if (_confVersion >= 1.3)
                {
                    connectionInfo.Inheritance.CacheBitmaps = a.GetAttrBool("InheritCacheBitmaps");
                    connectionInfo.Inheritance.Colors = a.GetAttrBool("InheritColors");
                    connectionInfo.Inheritance.Description = a.GetAttrBool("InheritDescription");
                    connectionInfo.Inheritance.DisplayThemes = a.GetAttrBool("InheritDisplayThemes");
                    connectionInfo.Inheritance.DisplayWallpaper = a.GetAttrBool("InheritDisplayWallpaper");
                    connectionInfo.Inheritance.Icon = a.GetAttrBool("InheritIcon");
                    connectionInfo.Inheritance.Panel = a.GetAttrBool("InheritPanel");
                    connectionInfo.Inheritance.TabColor = a.GetAttrBool("InheritTabColor");
                    connectionInfo.Inheritance.ConnectionFrameColor = a.GetAttrBool("InheritConnectionFrameColor");
                    connectionInfo.Inheritance.Port = a.GetAttrBool("InheritPort");
                    connectionInfo.Inheritance.Protocol = a.GetAttrBool("InheritProtocol");
                    connectionInfo.Inheritance.PuttySession = a.GetAttrBool("InheritPuttySession");
                    connectionInfo.Inheritance.RedirectDiskDrives = a.GetAttrBool("InheritRedirectDiskDrives");
                    connectionInfo.Inheritance.RedirectKeys = a.GetAttrBool("InheritRedirectKeys");
                    connectionInfo.Inheritance.RedirectPorts = a.GetAttrBool("InheritRedirectPorts");
                    connectionInfo.Inheritance.RedirectPrinters = a.GetAttrBool("InheritRedirectPrinters");
                    connectionInfo.Inheritance.RedirectSmartCards = a.GetAttrBool("InheritRedirectSmartCards");
                    connectionInfo.Inheritance.RedirectSound = a.GetAttrBool("InheritRedirectSound");
                    connectionInfo.Inheritance.RedirectAudioCapture = a.GetAttrBool("InheritRedirectAudioCapture");
                    connectionInfo.Inheritance.RedirectWebAuthn = a.GetAttrBool("InheritRedirectWebAuthn");
                    connectionInfo.Inheritance.EnableRdsAadAuth = a.GetAttrBool("InheritEnableRdsAadAuth");
                    connectionInfo.Inheritance.Resolution = a.GetAttrBool("InheritResolution");
                    connectionInfo.Inheritance.UseConsoleSession = a.GetAttrBool("InheritUseConsoleSession");

                    if (!Runtime.UseCredentialManager || _confVersion <= 2.6) // 1.3 - 2.6
                    {
                        connectionInfo.Inheritance.Domain = a.GetAttrBool("InheritDomain");
                        connectionInfo.Inheritance.Password = a.GetAttrBool("InheritPassword");
                        connectionInfo.Inheritance.Username = a.GetAttrBool("InheritUsername");
                    }

                    connectionInfo.Inheritance.Color = a.GetAttrBool("InheritColor");
                    connectionInfo.Icon = a.GetAttr("Icon");
                    connectionInfo.Panel = a.GetAttr("Panel");
                    connectionInfo.Color = a.GetAttr("Color");
                    connectionInfo.TabColor = a.GetAttr("TabColor");
                    connectionInfo.ConnectionFrameColor = xmlnode.GetAttributeAsEnum<ConnectionFrameColor>("ConnectionFrameColor");
                }
                else
                {
                    if (a.GetAttrBool("Inherit"))
                        connectionInfo.Inheritance.TurnOnInheritanceCompletely();
                    connectionInfo.Icon = a.GetAttr("Icon").Replace(".ico", "", StringComparison.Ordinal);
                    connectionInfo.Panel = "General";
                }

                if (_confVersion >= 1.5)
                {
                    connectionInfo.PleaseConnect = a.GetAttrBool("Connected");
                }

                if (_confVersion >= 1.6)
                {
                    connectionInfo.PreExtApp = a.GetAttr("PreExtApp");
                    connectionInfo.PostExtApp = a.GetAttr("PostExtApp");
                    connectionInfo.Inheritance.PreExtApp = a.GetAttrBool("InheritPreExtApp");
                    connectionInfo.Inheritance.PostExtApp = a.GetAttrBool("InheritPostExtApp");
                }

                if (_confVersion >= 1.7)
                {
                    connectionInfo.VNCCompression = xmlnode.GetAttributeAsEnum<ProtocolVNC.Compression>("VNCCompression");
                    connectionInfo.VNCEncoding = xmlnode.GetAttributeAsEnum<ProtocolVNC.Encoding>("VNCEncoding");
                    connectionInfo.VNCAuthMode = xmlnode.GetAttributeAsEnum<ProtocolVNC.AuthMode>("VNCAuthMode");
                    connectionInfo.VNCProxyType = xmlnode.GetAttributeAsEnum<ProtocolVNC.ProxyType>("VNCProxyType");
                    connectionInfo.VNCProxyIP = a.GetAttr("VNCProxyIP");
                    connectionInfo.VNCProxyPort = a.GetAttrInt("VNCProxyPort");
                    connectionInfo.VNCProxyUsername = a.GetAttr("VNCProxyUsername");
                    DeferDecrypt(v => connectionInfo.VNCProxyPassword = v, a, "VNCProxyPassword");
                    connectionInfo.VNCColors = xmlnode.GetAttributeAsEnum<ProtocolVNC.Colors>("VNCColors");
                    connectionInfo.VNCSmartSizeMode = xmlnode.GetAttributeAsEnum<ProtocolVNC.SmartSizeMode>("VNCSmartSizeMode");
                    connectionInfo.VNCViewOnly = a.GetAttrBool("VNCViewOnly");
                    connectionInfo.VNCClipboardRedirect = a.GetAttrBool("VNCClipboardRedirect", true);
                    connectionInfo.Inheritance.VNCCompression = a.GetAttrBool("InheritVNCCompression");
                    connectionInfo.Inheritance.VNCEncoding = a.GetAttrBool("InheritVNCEncoding");
                    connectionInfo.Inheritance.VNCAuthMode = a.GetAttrBool("InheritVNCAuthMode");
                    connectionInfo.Inheritance.VNCProxyType = a.GetAttrBool("InheritVNCProxyType");
                    connectionInfo.Inheritance.VNCProxyIP = a.GetAttrBool("InheritVNCProxyIP");
                    connectionInfo.Inheritance.VNCProxyPort = a.GetAttrBool("InheritVNCProxyPort");
                    connectionInfo.Inheritance.VNCProxyUsername = a.GetAttrBool("InheritVNCProxyUsername");
                    connectionInfo.Inheritance.VNCProxyPassword = a.GetAttrBool("InheritVNCProxyPassword");
                    connectionInfo.Inheritance.VNCColors = a.GetAttrBool("InheritVNCColors");
                    connectionInfo.Inheritance.VNCSmartSizeMode = a.GetAttrBool("InheritVNCSmartSizeMode");
                    connectionInfo.Inheritance.VNCViewOnly = a.GetAttrBool("InheritVNCViewOnly");
                    connectionInfo.Inheritance.VNCClipboardRedirect = a.GetAttrBool("InheritVNCClipboardRedirect");
                }

                if (_confVersion >= 1.8)
                {
                    connectionInfo.RDPAuthenticationLevel = xmlnode.GetAttributeAsEnum<AuthenticationLevel>("RDPAuthenticationLevel");
                    connectionInfo.Inheritance.RDPAuthenticationLevel = a.GetAttrBool("InheritRDPAuthenticationLevel");
                }

                if (_confVersion >= 1.9)
                {
                    connectionInfo.RenderingEngine = xmlnode.GetAttributeAsEnum<HTTPBase.RenderingEngine>("RenderingEngine");
                    connectionInfo.MacAddress = a.GetAttr("MacAddress");
                    connectionInfo.Inheritance.RenderingEngine = a.GetAttrBool("InheritRenderingEngine");
                    connectionInfo.Inheritance.MacAddress = a.GetAttrBool("InheritMacAddress");
                }

                if (_confVersion >= 2.0)
                {
                    connectionInfo.UserField = a.GetAttr("UserField");
                    connectionInfo.Inheritance.UserField = a.GetAttrBool("InheritUserField");
                }

                if (_confVersion >= 2.1)
                {
                    connectionInfo.ExtApp = a.GetAttr("ExtApp");
                    connectionInfo.Inheritance.ExtApp = a.GetAttrBool("InheritExtApp");
                }

                if (_confVersion >= 2.2)
                {
                    // Get settings
                    connectionInfo.RDGatewayUsageMethod = GetRdGatewayUsageMethod(xmlnode);
                    connectionInfo.RDGatewayHostname = a.GetAttr("RDGatewayHostname");
                    connectionInfo.RDGatewayUseConnectionCredentials = xmlnode.GetAttributeAsEnum<RDGatewayUseConnectionCredentials>("RDGatewayUseConnectionCredentials");
                    connectionInfo.RDGatewayUsername = a.GetAttr("RDGatewayUsername");
                    DeferDecrypt(v => connectionInfo.RDGatewayPassword = v, a, "RDGatewayPassword");
                    connectionInfo.RDGatewayDomain = a.GetAttr("RDGatewayDomain");

                    // Get inheritance settings
                    connectionInfo.Inheritance.RDGatewayUsageMethod = a.GetAttrBool("InheritRDGatewayUsageMethod");
                    connectionInfo.Inheritance.RDGatewayHostname = a.GetAttrBool("InheritRDGatewayHostname");
                    connectionInfo.Inheritance.RDGatewayUseConnectionCredentials = a.GetAttrBool("InheritRDGatewayUseConnectionCredentials");
                    connectionInfo.Inheritance.RDGatewayUsername = a.GetAttrBool("InheritRDGatewayUsername");
                    connectionInfo.Inheritance.RDGatewayPassword = a.GetAttrBool("InheritRDGatewayPassword");
                    connectionInfo.Inheritance.RDGatewayDomain = a.GetAttrBool("InheritRDGatewayDomain");
                    connectionInfo.Inheritance.RDGatewayAccessToken = a.GetAttrBool("InheritRDGatewayAccessToken");
                }

                if (_confVersion >= 2.3)
                {
                    // Get settings
                    connectionInfo.EnableFontSmoothing = a.GetAttrBool("EnableFontSmoothing");
                    connectionInfo.EnableDesktopComposition = a.GetAttrBool("EnableDesktopComposition");

                    // Get inheritance settings
                    connectionInfo.Inheritance.EnableFontSmoothing = a.GetAttrBool("InheritEnableFontSmoothing");
                    connectionInfo.Inheritance.EnableDesktopComposition = a.GetAttrBool("InheritEnableDesktopComposition");
                }

                if (_confVersion >= 2.4)
                {
                    connectionInfo.UseCredSsp = a.GetAttrBool("UseCredSsp");
                    connectionInfo.Inheritance.UseCredSsp = a.GetAttrBool("InheritUseCredSsp");
                }

                if (_confVersion >= 2.5)
                {
                    connectionInfo.LoadBalanceInfo = a.GetAttr("LoadBalanceInfo");
                    connectionInfo.AutomaticResize = a.GetAttrBool("AutomaticResize");
                    connectionInfo.Inheritance.LoadBalanceInfo = a.GetAttrBool("InheritLoadBalanceInfo");
                    connectionInfo.Inheritance.AutomaticResize = a.GetAttrBool("InheritAutomaticResize");
                }

                if (_confVersion >= 2.6)
                {
                    connectionInfo.SoundQuality = xmlnode.GetAttributeAsEnum<RDPSoundQuality>("SoundQuality");
                    connectionInfo.Inheritance.SoundQuality = a.GetAttrBool("InheritSoundQuality");
                    connectionInfo.RDPMinutesToIdleTimeout = a.GetAttrInt("RDPMinutesToIdleTimeout");
                    connectionInfo.Inheritance.RDPMinutesToIdleTimeout = a.GetAttrBool("InheritRDPMinutesToIdleTimeout");
                    connectionInfo.RDPAlertIdleTimeout = a.GetAttrBool("RDPAlertIdleTimeout");
                    connectionInfo.Inheritance.RDPAlertIdleTimeout = a.GetAttrBool("InheritRDPAlertIdleTimeout");
                }

                if (_confVersion >= 2.7)
                {
                    connectionInfo.RedirectClipboard = a.GetAttrBool("RedirectClipboard");
                    connectionInfo.Favorite = a.GetAttrBool("Favorite");
                    connectionInfo.UseVmId = a.GetAttrBool("UseVmId");
                    connectionInfo.VmId = a.GetAttr("VmId");
                    connectionInfo.UseEnhancedMode = a.GetAttrBool("UseEnhancedMode");
                    connectionInfo.RdpVersion = a.GetAttrEnum("RdpVersion", RdpVersion.Highest);
                    connectionInfo.SSHTunnelConnectionName = a.GetAttr("SSHTunnelConnectionName");
                    connectionInfo.OpeningCommand = a.GetAttr("OpeningCommand");
                    connectionInfo.SSHOptions = a.GetAttr("SSHOptions");
                    connectionInfo.PrivateKeyPath = a.GetAttr("PrivateKeyPath");
                    connectionInfo.RDPStartProgram = a.GetAttr("StartProgram");
                    connectionInfo.RDPStartProgramWorkDir = a.GetAttr("StartProgramWorkDir");
                    connectionInfo.DisableFullWindowDrag = a.GetAttrBool("DisableFullWindowDrag");
                    connectionInfo.DisableMenuAnimations = a.GetAttrBool("DisableMenuAnimations");
                    connectionInfo.DisableCursorShadow = a.GetAttrBool("DisableCursorShadow");
                    connectionInfo.DisableCursorBlinking = a.GetAttrBool("DisableCursorBlinking");
                    connectionInfo.RDPStartProgram = a.GetAttr("StartProgram");
                    connectionInfo.RDPStartProgramWorkDir = a.GetAttr("StartProgramWorkDir");
                    connectionInfo.Inheritance.RedirectClipboard = a.GetAttrBool("InheritRedirectClipboard");
                    connectionInfo.Inheritance.Favorite = a.GetAttrBool("InheritFavorite");
                    connectionInfo.Inheritance.RdpVersion = a.GetAttrBool("InheritRdpVersion");
                    connectionInfo.Inheritance.UseVmId = a.GetAttrBool("InheritUseVmId");
                    connectionInfo.Inheritance.VmId = a.GetAttrBool("InheritVmId");
                    connectionInfo.Inheritance.UseEnhancedMode = a.GetAttrBool("InheritUseEnhancedMode");
                    connectionInfo.Inheritance.SSHTunnelConnectionName = a.GetAttrBool("InheritSSHTunnelConnectionName");
                    connectionInfo.Inheritance.OpeningCommand = a.GetAttrBool("InheritOpeningCommand");
                    connectionInfo.Inheritance.SSHOptions = a.GetAttrBool("InheritSSHOptions");
                    connectionInfo.Inheritance.PrivateKeyPath = a.GetAttrBool("InheritPrivateKeyPath");
                    connectionInfo.Inheritance.DisableFullWindowDrag = a.GetAttrBool("InheritDisableFullWindowDrag");
                    connectionInfo.Inheritance.DisableMenuAnimations = a.GetAttrBool("InheritDisableMenuAnimations");
                    connectionInfo.Inheritance.DisableCursorShadow = a.GetAttrBool("InheritDisableCursorShadow");
                    connectionInfo.Inheritance.DisableCursorBlinking = a.GetAttrBool("InheritDisableCursorBlinking");
                    connectionInfo.ExternalCredentialProvider = a.GetAttrEnum("ExternalCredentialProvider", ExternalCredentialProvider.None);
                    connectionInfo.Inheritance.ExternalCredentialProvider = a.GetAttrBool("InheritExternalCredentialProvider");
                    connectionInfo.UserViaAPI = a.GetAttr("UserViaAPI");
                    connectionInfo.Inheritance.UserViaAPI = a.GetAttrBool("InheritUserViaAPI");
                    connectionInfo.ExternalAddressProvider = a.GetAttrEnum("ExternalAddressProvider", ExternalAddressProvider.None);
                    connectionInfo.VaultOpenbaoMount = a.GetAttr("VaultOpenbaoMount");
                    connectionInfo.VaultOpenbaoRole = a.GetAttr("VaultOpenbaoRole");
                    connectionInfo.VaultOpenbaoSecretEngine = a.GetAttrEnum("VaultOpenbaoSecretEngine", VaultOpenbaoSecretEngine.Kv);
                    connectionInfo.EC2InstanceId = a.GetAttr("EC2InstanceId");
                    connectionInfo.EC2Region = a.GetAttr("EC2Region");
                    connectionInfo.UseRestrictedAdmin = a.GetAttrBool("UseRestrictedAdmin");
                    connectionInfo.Inheritance.UseRestrictedAdmin = a.GetAttrBool("InheritUseRestrictedAdmin");
                    connectionInfo.UseRCG = a.GetAttrBool("UseRCG");
                    connectionInfo.Inheritance.UseRCG = a.GetAttrBool("InheritUseRCG");
                    connectionInfo.UseRedirectionServerName = a.GetAttrBool("UseRedirectionServerName");
                    connectionInfo.Inheritance.UseRedirectionServerName = a.GetAttrBool("InheritUseRedirectionServerName");
                    connectionInfo.RDGatewayExternalCredentialProvider = a.GetAttrEnum("RDGatewayExternalCredentialProvider", ExternalCredentialProvider.None);
                    connectionInfo.RDGatewayUserViaAPI = a.GetAttr("RDGatewayUserViaAPI");
                    connectionInfo.RDGatewayAccessToken = a.GetAttr("RDGatewayAccessToken");
                    connectionInfo.Inheritance.RDGatewayExternalCredentialProvider = a.GetAttrBool("InheritRDGatewayExternalCredentialProvider");
                    connectionInfo.Inheritance.RDGatewayUserViaAPI = a.GetAttrBool("InheritRDGatewayUserViaAPI");
                }

                if (_confVersion >= 2.8)
                {
                    // Get settings
                    connectionInfo.IsRoot = a.GetAttrBool("IsRoot");
                    connectionInfo.IsTemplate = a.GetAttrBool("IsTemplate");
                    connectionInfo.UsePersistentBrowser = a.GetAttrBool("UsePersistentBrowser");
                    connectionInfo.ScriptErrorsSuppressed = a.GetAttrBool("ScriptErrorsSuppressed", true);
                    connectionInfo.Inheritance.ScriptErrorsSuppressed = a.GetAttrBool("InheritScriptErrorsSuppressed");
                    connectionInfo.DesktopScaleFactor = xmlnode.GetAttributeAsEnum<RDPDesktopScaleFactor>("DesktopScaleFactor");
                    connectionInfo.Inheritance.DesktopScaleFactor = a.GetAttrBool("InheritDesktopScaleFactor");
                    connectionInfo.RDPSignScope = a.GetAttr("RDPSignScope");
                    connectionInfo.RDPSignature = a.GetAttr("RDPSignature");
                    connectionInfo.Inheritance.RDPSignScope = a.GetAttrBool("InheritRDPSignScope");
                    connectionInfo.Inheritance.RDPSignature = a.GetAttrBool("InheritRDPSignature");
                    connectionInfo.IPAddress = a.GetAttr("IPAddress");
                    connectionInfo.ConnectionAddressPrimary = xmlnode.GetAttributeAsEnum<ConnectionAddressPrimary>("ConnectionAddressPrimary");
                    connectionInfo.RDPSizingMode = xmlnode.GetAttributeAsEnum<RDPSizingMode>("RDPSizingMode");
                    connectionInfo.ResolutionWidth = a.GetAttrInt("ResolutionWidth");
                    connectionInfo.ResolutionHeight = a.GetAttrInt("ResolutionHeight");
                    connectionInfo.RDPUseMultimon = a.GetAttrBool("RDPUseMultimon");
                    connectionInfo.Notes = a.GetAttr("Notes");
                    connectionInfo.RetryOnFirstConnect = a.GetAttrBool("RetryOnFirstConnect");
                    connectionInfo.WaitForIPAvailability = a.GetAttrBool("WaitForIPAvailability");
                    connectionInfo.WaitForIPTimeout = a.GetAttrInt("WaitForIPTimeout");
                    connectionInfo.ShowBrowserNavigationBar = a.GetAttrBool("ShowBrowserNavigationBar");
                    connectionInfo.HttpPath = a.GetAttr("HttpPath");
                    connectionInfo.AlwaysPromptForCredentials = a.GetAttrBool("AlwaysPromptForCredentials");
                    connectionInfo.Inheritance.IPAddress = a.GetAttrBool("InheritIPAddress");
                    connectionInfo.Inheritance.ConnectionAddressPrimary = a.GetAttrBool("InheritConnectionAddressPrimary");
                    connectionInfo.Inheritance.RDPSizingMode = a.GetAttrBool("InheritRDPSizingMode");
                    connectionInfo.Inheritance.ResolutionWidth = a.GetAttrBool("InheritResolutionWidth");
                    connectionInfo.Inheritance.ResolutionHeight = a.GetAttrBool("InheritResolutionHeight");
                    connectionInfo.Inheritance.RDPUseMultimon = a.GetAttrBool("InheritRDPUseMultimon");
                    connectionInfo.Inheritance.Notes = a.GetAttrBool("InheritNotes");
                    connectionInfo.Inheritance.RetryOnFirstConnect = a.GetAttrBool("InheritRetryOnFirstConnect");
                    connectionInfo.Inheritance.WaitForIPAvailability = a.GetAttrBool("InheritWaitForIPAvailability");
                    connectionInfo.Inheritance.WaitForIPTimeout = a.GetAttrBool("InheritWaitForIPTimeout");
                    connectionInfo.CredentialId = a.GetAttr("CredentialId");
                }

                switch (_confVersion)
                {
                    case >= 2.8:
                        connectionInfo.RedirectDiskDrives = xmlnode.GetAttributeAsEnum<RDPDiskDrives>("RedirectDiskDrives");
                        connectionInfo.RedirectDiskDrivesCustom = a.GetAttr("RedirectDiskDrivesCustom");
                        connectionInfo.Inheritance.RedirectDiskDrivesCustom = a.GetAttrBool("InheritRedirectDiskDrivesCustom");
                        connectionInfo.EnvironmentTags = a.GetAttr("EnvironmentTags");
                        connectionInfo.Inheritance.EnvironmentTags = a.GetAttrBool("InheritEnvironmentTags");
                        connectionInfo.Inheritance.AutoSort = a.GetAttrBool("InheritAutoSort");
                        connectionInfo.UserField1 = a.GetAttr("UserField1");
                        connectionInfo.UserField2 = a.GetAttr("UserField2");
                        connectionInfo.UserField3 = a.GetAttr("UserField3");
                        connectionInfo.UserField4 = a.GetAttr("UserField4");
                        connectionInfo.UserField5 = a.GetAttr("UserField5");
                        connectionInfo.UserField6 = a.GetAttr("UserField6");
                        connectionInfo.UserField7 = a.GetAttr("UserField7");
                        connectionInfo.UserField8 = a.GetAttr("UserField8");
                        connectionInfo.UserField9 = a.GetAttr("UserField9");
                        connectionInfo.UserField10 = a.GetAttr("UserField10");
                        connectionInfo.Inheritance.UserField1 = a.GetAttrBool("InheritUserField1");
                        connectionInfo.Inheritance.UserField2 = a.GetAttrBool("InheritUserField2");
                        connectionInfo.Inheritance.UserField3 = a.GetAttrBool("InheritUserField3");
                        connectionInfo.Inheritance.UserField4 = a.GetAttrBool("InheritUserField4");
                        connectionInfo.Inheritance.UserField5 = a.GetAttrBool("InheritUserField5");
                        connectionInfo.Inheritance.UserField6 = a.GetAttrBool("InheritUserField6");
                        connectionInfo.Inheritance.UserField7 = a.GetAttrBool("InheritUserField7");
                        connectionInfo.Inheritance.UserField8 = a.GetAttrBool("InheritUserField8");
                        connectionInfo.Inheritance.UserField9 = a.GetAttrBool("InheritUserField9");
                        connectionInfo.Inheritance.UserField10 = a.GetAttrBool("InheritUserField10");
                        connectionInfo.Inheritance.Hostname = a.GetAttrBool("InheritHostname");
                        connectionInfo.Inheritance.AlternativeAddress = a.GetAttrBool("InheritAlternativeAddress");
                        break;

                    case >= 0.5:
                    {
                        // used to be boolean
                        bool tmpRedirect = a.GetAttrBool("RedirectDiskDrives");
                        connectionInfo.RedirectDiskDrives = tmpRedirect ? RDPDiskDrives.Local : RDPDiskDrives.None;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(CultureInfo.InvariantCulture, Language.GetConnectionInfoFromXmlFailed, connectionInfo.Name, ConnectionFileName, ex.Message));
            }

            return connectionInfo;
        }

        private void DeferDecrypt(Action<string> setter, Dictionary<string, string> attrs, string attributeName)
        {
            string cipherText = attrs.GetAttr(attributeName);
            if (string.IsNullOrEmpty(cipherText))
                return;
            _pendingDecrypts.Add((setter, cipherText));
        }

        private void DeferDecrypt(Action<string> setter, XmlNode xmlNode, string attributeName)
        {
            string cipherText = xmlNode.GetAttributeAsString(attributeName);
            if (string.IsNullOrEmpty(cipherText))
                return;
            _pendingDecrypts.Add((setter, cipherText));
        }

        private static RDGatewayUsageMethod GetRdGatewayUsageMethod(XmlNode xmlNode)
        {
            string value = xmlNode.GetAttributeAsString("RDGatewayUsageMethod");
            if (string.IsNullOrWhiteSpace(value))
                return RDGatewayUsageMethod.Never;

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int numericValue))
            {
                return numericValue switch
                {
                    0 => RDGatewayUsageMethod.Never,
                    1 => RDGatewayUsageMethod.Always,
                    2 => RDGatewayUsageMethod.Detect,
                    // Legacy .rdp imports can carry value 4 (do not use RD Gateway, bypass local addresses),
                    // which is unsupported by our enum and should behave as "Never".
                    4 => RDGatewayUsageMethod.Never,
                    _ => RDGatewayUsageMethod.Never,
                };
            }

            if (Enum.TryParse(value, true, out RDGatewayUsageMethod parsedValue) &&
                Enum.IsDefined<RDGatewayUsageMethod>(parsedValue))
            {
                return parsedValue;
            }

            return RDGatewayUsageMethod.Never;
        }
    }
}

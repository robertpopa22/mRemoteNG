using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.Http;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Connection.Protocol.VNC;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Csv
{
    [SupportedOSPlatform("windows")]
    public class CsvConnectionsDeserializerMremotengFormat : IDeserializer<string, ConnectionTreeModel>
    {
        public ConnectionTreeModel Deserialize(string serializedData)
        {
            string[] lines = serializedData.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.RemoveEmptyEntries);
            List<string> csvHeaders = new();
            // used to map a connectioninfo to it's parent's GUID
            Dictionary<ConnectionInfo, string> parentMapping = new();

            char delimiter = ';';
            if (lines.Length > 0)
            {
                int countSemicolon = lines[0].Count(c => c == ';');
                int countComma = lines[0].Count(c => c == ',');

                if (countComma > countSemicolon)
                {
                    delimiter = ',';
                }
            }

            for (int lineNumber = 0; lineNumber < lines.Length; lineNumber++)
            {
                string[] line = ParseCsvLine(lines[lineNumber], delimiter);
                if (lineNumber == 0)
                    csvHeaders = line.ToList();
                else
                {
                    ConnectionInfo connectionInfo = ParseConnectionInfo(csvHeaders, line);
                    parentMapping.Add(connectionInfo, line[csvHeaders.IndexOf("Parent")]);
                }
            }

            RootNodeInfo root = CreateTreeStructure(parentMapping);
            ApplyAutoSortRecursive(root);
            ConnectionTreeModel connectionTreeModel = new();
            connectionTreeModel.AddRootNode(root);
            return connectionTreeModel;
        }

        /// <summary>
        /// Parses a single CSV line respecting RFC 4180 double-quoted fields.
        /// </summary>
        private static string[] ParseCsvLine(string line, char delimiter)
        {
            List<string> fields = new();
            int i = 0;
            while (i <= line.Length)
            {
                if (i == line.Length)
                {
                    fields.Add("");
                    break;
                }

                if (line[i] == '"')
                {
                    // Quoted field
                    i++; // skip opening quote
                    int start = i;
                    System.Text.StringBuilder sb = new();
                    while (i < line.Length)
                    {
                        if (line[i] == '"')
                        {
                            if (i + 1 < line.Length && line[i + 1] == '"')
                            {
                                sb.Append(line, start, i - start);
                                sb.Append('"');
                                i += 2;
                                start = i;
                            }
                            else
                            {
                                // End of quoted field
                                sb.Append(line, start, i - start);
                                i++; // skip closing quote
                                break;
                            }
                        }
                        else
                        {
                            i++;
                        }
                    }

                    if (i == line.Length && (start <= line.Length - 1) && (sb.Length == 0 || line[i - 1] != '"'))
                    {
                        sb.Append(line, start, i - start);
                    }

                    fields.Add(sb.ToString());

                    // skip delimiter after quoted field
                    if (i < line.Length && line[i] == delimiter)
                        i++;
                }
                else
                {
                    // Unquoted field
                    int next = line.IndexOf(delimiter, i);
                    if (next == -1)
                    {
                        fields.Add(line[i..]);
                        break;
                    }
                    else
                    {
                        fields.Add(line[i..next]);
                        i = next + 1;
                    }
                }
            }

            return fields.ToArray();
        }

        private RootNodeInfo CreateTreeStructure(Dictionary<ConnectionInfo, string> parentMapping)
        {
            RootNodeInfo root = new(RootNodeType.Connection);

            foreach (KeyValuePair<ConnectionInfo, string> node in parentMapping)
            {
                // no parent mapped, add to root
                if (string.IsNullOrEmpty(node.Value))
                {
                    root.AddChild(node.Key);
                    continue;
                }

                // search for parent in the list by GUID
                ContainerInfo? parent = parentMapping
                             .Keys
                             .OfType<ContainerInfo>()
                             .FirstOrDefault(info => info.ConstantID == node.Value);

                if (parent != null)
                {
                    parent.AddChild(node.Key);
                }
                else
                {
                    root.AddChild(node.Key);
                }
            }

            return root;
        }

        private void ApplyAutoSortRecursive(ContainerInfo parent)
        {
            foreach (ContainerInfo childContainer in parent.Children.OfType<ContainerInfo>())
                ApplyAutoSortRecursive(childContainer);

            if (parent.AutoSort)
                parent.Sort();
        }

        private ConnectionInfo ParseConnectionInfo(IList<string> headers, string[] connectionCsv)
        {
            TreeNodeType nodeType = headers.Contains("NodeType")
                ? (TreeNodeType)Enum.Parse(typeof(TreeNodeType), connectionCsv[headers.IndexOf("NodeType")], true)
                : TreeNodeType.Connection;

            string nodeId = headers.Contains("Id")
                ? connectionCsv[headers.IndexOf("Id")]
                : Guid.NewGuid().ToString();

            ConnectionInfo connectionRecord = nodeType == TreeNodeType.Connection
                ? new ConnectionInfo(nodeId)
                : new ContainerInfo(nodeId);

            connectionRecord.Name = headers.Contains("Name")
                ? connectionCsv[headers.IndexOf("Name")]
                : "";

            connectionRecord.Description = headers.Contains("Description")
                ? connectionCsv[headers.IndexOf("Description")]
                : "";

            connectionRecord.Icon = headers.Contains("Icon")
                ? connectionCsv[headers.IndexOf("Icon")]
                : "";

            connectionRecord.Panel = headers.Contains("Panel")
                ? connectionCsv[headers.IndexOf("Panel")]
                : "";

            connectionRecord.UserViaAPI = headers.Contains("UserViaAPI")
                ? connectionCsv[headers.IndexOf("UserViaAPI")]
                : "";

            connectionRecord.Username = headers.Contains("Username")
                ? connectionCsv[headers.IndexOf("Username")]
                : "";

            connectionRecord.Password = headers.Contains("Password")
                // ? connectionCsv[headers.IndexOf("Password")].ConvertToSecureString()
                // : "".ConvertToSecureString();
                ? connectionCsv[headers.IndexOf("Password")]
                : "";

            connectionRecord.Domain = headers.Contains("Domain")
                ? connectionCsv[headers.IndexOf("Domain")]
                : "";

            connectionRecord.Hostname = headers.Contains("Hostname")
                ? connectionCsv[headers.IndexOf("Hostname")]
                : "";

            connectionRecord.AlternativeAddress = headers.Contains("AlternativeAddress")
                ? connectionCsv[headers.IndexOf("AlternativeAddress")]
                : "";

            connectionRecord.VmId = headers.Contains("VmId")
                ? connectionCsv[headers.IndexOf("VmId")] : "";

            connectionRecord.SSHOptions =headers.Contains("SSHOptions")
                ? connectionCsv[headers.IndexOf("SSHOptions")]
                : "";

            connectionRecord.SSHTunnelConnectionName = headers.Contains("SSHTunnelConnectionName")
                ? connectionCsv[headers.IndexOf("SSHTunnelConnectionName")]
                : "";

            connectionRecord.PuttySession = headers.Contains("PuttySession")
                ? connectionCsv[headers.IndexOf("PuttySession")]
                : "";

            connectionRecord.LoadBalanceInfo = headers.Contains("LoadBalanceInfo")
                ? connectionCsv[headers.IndexOf("LoadBalanceInfo")]
                : "";

            connectionRecord.OpeningCommand = headers.Contains("OpeningCommand")
                ? connectionCsv[headers.IndexOf("OpeningCommand")]
                : "";

            connectionRecord.PreExtApp = headers.Contains("PreExtApp")
                ? connectionCsv[headers.IndexOf("PreExtApp")]
                : "";

            connectionRecord.PostExtApp =
                headers.Contains("PostExtApp")
                ? connectionCsv[headers.IndexOf("PostExtApp")]
                : "";

            connectionRecord.MacAddress =
                headers.Contains("MacAddress")
                ? connectionCsv[headers.IndexOf("MacAddress")]
                : "";

            connectionRecord.UserField =
                headers.Contains("UserField")
                ? connectionCsv[headers.IndexOf("UserField")]
                : "";

            for (int i = 1; i <= 10; i++)
            {
                string key = $"UserField{i}";
                if (headers.Contains(key))
                {
                    typeof(ConnectionInfo).GetProperty(key)?.SetValue(connectionRecord, connectionCsv[headers.IndexOf(key)]);
                }
            }

            connectionRecord.EnvironmentTags =
                headers.Contains("EnvironmentTags")
                ? connectionCsv[headers.IndexOf("EnvironmentTags")]
                : "";

            connectionRecord.ExtApp = headers.Contains("ExtApp")
                ? connectionCsv[headers.IndexOf("ExtApp")] : "";

            connectionRecord.TabColor = headers.Contains("TabColor")
                ? connectionCsv[headers.IndexOf("TabColor")] : "";

            connectionRecord.ConnectionFrameColor = headers.Contains("ConnectionFrameColor")
                ? Enum.TryParse(connectionCsv[headers.IndexOf("ConnectionFrameColor")], out ConnectionFrameColor cfColor)
                    ? cfColor : default
                : default;

            connectionRecord.RedirectDiskDrivesCustom = headers.Contains("RedirectDiskDrivesCustom")
                ? connectionCsv[headers.IndexOf("RedirectDiskDrivesCustom")] : "";

            connectionRecord.EC2InstanceId = headers.Contains("EC2InstanceId")
                ? connectionCsv[headers.IndexOf("EC2InstanceId")] : "";

            connectionRecord.EC2Region = headers.Contains("EC2Region")
                ? connectionCsv[headers.IndexOf("EC2Region")] : "";

            connectionRecord.VNCProxyUsername = headers.Contains("VNCProxyUsername")
                ? connectionCsv[headers.IndexOf("VNCProxyUsername")]
                : "";

            connectionRecord.VNCProxyPassword = headers.Contains("VNCProxyPassword")
                ? connectionCsv[headers.IndexOf("VNCProxyPassword")]
                : "";

            connectionRecord.RDGatewayUsername = headers.Contains("RDGatewayUsername")
                ? connectionCsv[headers.IndexOf("RDGatewayUsername")]
                : "";

            connectionRecord.RDGatewayPassword = headers.Contains("RDGatewayPassword")
                ? connectionCsv[headers.IndexOf("RDGatewayPassword")]
                : "";

            connectionRecord.RDGatewayDomain = headers.Contains("RDGatewayDomain")
                ? connectionCsv[headers.IndexOf("RDGatewayDomain")]
                : "";

            connectionRecord.RDGatewayHostname = headers.Contains("RDGatewayHostname")
                ? connectionCsv[headers.IndexOf("RDGatewayHostname")]
                : "";

            if (headers.Contains("RDGatewayExternalCredentialProvider"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RDGatewayExternalCredentialProvider")], out ExternalCredentialProvider value))
                    connectionRecord.RDGatewayExternalCredentialProvider = value;
            }

            connectionRecord.RDGatewayUserViaAPI = headers.Contains("RDGatewayUserViaAPI")
                ? connectionCsv[headers.IndexOf("RDGatewayUserViaAPI")]
                : "";


            connectionRecord.VNCProxyIP = headers.Contains("VNCProxyIP")
                ? connectionCsv[headers.IndexOf("VNCProxyIP")]
                : "";


            connectionRecord.RDPStartProgram = headers.Contains("RDPStartProgram")
                ? connectionCsv[headers.IndexOf("RDPStartProgram")]
                : "";

            connectionRecord.RDPStartProgramWorkDir = headers.Contains("RDPStartProgramWorkDir")
                ? connectionCsv[headers.IndexOf("RDPStartProgramWorkDir")]
                : "";

            if (headers.Contains("Protocol"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("Protocol")], out ProtocolType protocolType))
                    connectionRecord.Protocol = protocolType;
            }

            if (headers.Contains("Port"))
            {
                if (int.TryParse(connectionCsv[headers.IndexOf("Port")], out int port))
                    connectionRecord.Port = port;
            }

            if (headers.Contains("ConnectToConsole"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("ConnectToConsole")], out bool useConsoleSession))
                    connectionRecord.UseConsoleSession = useConsoleSession;
            }

            if (headers.Contains("UseCredSsp"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UseCredSsp")], out bool value))
                    connectionRecord.UseCredSsp = value;
            }

            if (headers.Contains("UseRestrictedAdmin"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UseRestrictedAdmin")], out bool value))
                    connectionRecord.UseRestrictedAdmin = value;
            }
            if (headers.Contains("UseRCG"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UseRCG")], out bool value))
                    connectionRecord.UseRCG = value;
            }


            if (headers.Contains("UseVmId"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UseVmId")], out bool value))
                    connectionRecord.UseVmId = value;
            }

            if (headers.Contains("UseEnhancedMode"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UseEnhancedMode")], out bool value))
                    connectionRecord.UseEnhancedMode = value;
            }

            if (headers.Contains("RenderingEngine"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RenderingEngine")], out HTTPBase.RenderingEngine value))
                    connectionRecord.RenderingEngine = value;
            }

            if (headers.Contains("RDPAuthenticationLevel"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RDPAuthenticationLevel")], out AuthenticationLevel value))
                    connectionRecord.RDPAuthenticationLevel = value;
            }

            if (headers.Contains("Colors"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("Colors")], out RDPColors value))
                    connectionRecord.Colors = value;
            }

            if (headers.Contains("Resolution"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("Resolution")], out RDPResolutions value))
                    connectionRecord.Resolution = value;
            }

            if (headers.Contains("AutomaticResize"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("AutomaticResize")], out bool value))
                    connectionRecord.AutomaticResize = value;
            }

            if (headers.Contains("DisplayWallpaper"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisplayWallpaper")], out bool value))
                    connectionRecord.DisplayWallpaper = value;
            }

            if (headers.Contains("DisplayThemes"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisplayThemes")], out bool value))
                    connectionRecord.DisplayThemes = value;
            }

            if (headers.Contains("EnableFontSmoothing"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("EnableFontSmoothing")], out bool value))
                    connectionRecord.EnableFontSmoothing = value;
            }

            if (headers.Contains("EnableDesktopComposition"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("EnableDesktopComposition")], out bool value))
                    connectionRecord.EnableDesktopComposition = value;
            }

            if (headers.Contains("DisableFullWindowDrag"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisableFullWindowDrag")], out bool value))
                    connectionRecord.DisableFullWindowDrag = value;
            }

            if (headers.Contains("DisableMenuAnimations"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisableMenuAnimations")], out bool value))
                    connectionRecord.DisableMenuAnimations = value;
            }

            if (headers.Contains("DisableCursorShadow"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisableCursorShadow")], out bool value))
                    connectionRecord.DisableCursorShadow = value;
            }

            if (headers.Contains("DisableCursorBlinking"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("DisableCursorBlinking")], out bool value))
                    connectionRecord.DisableCursorBlinking = value;
            }

            if (headers.Contains("CacheBitmaps"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("CacheBitmaps")], out bool value))
                    connectionRecord.CacheBitmaps = value;
            }

            if (headers.Contains("RedirectDiskDrives"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RedirectDiskDrives")], out RDPDiskDrives value))
                    connectionRecord.RedirectDiskDrives = value;
            }

            if (headers.Contains("RedirectPorts"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectPorts")], out bool value))
                    connectionRecord.RedirectPorts = value;
            }

            if (headers.Contains("RedirectPrinters"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectPrinters")], out bool value))
                    connectionRecord.RedirectPrinters = value;
            }

            if (headers.Contains("RedirectClipboard"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectClipboard")], out bool value))
                    connectionRecord.RedirectClipboard = value;
            }

            if (headers.Contains("RedirectSmartCards"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectSmartCards")], out bool value))
                    connectionRecord.RedirectSmartCards = value;
            }

            if (headers.Contains("RedirectSound"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RedirectSound")], out RDPSounds value))
                    connectionRecord.RedirectSound = value;
            }

            if (headers.Contains("RedirectAudioCapture"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectAudioCapture")], out bool value))
                    connectionRecord.RedirectAudioCapture = value;
            }

            if (headers.Contains("RedirectKeys"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("RedirectKeys")], out bool value))
                    connectionRecord.RedirectKeys = value;
            }

            if (headers.Contains("VNCCompression"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCCompression")], out ProtocolVNC.Compression value))
                    connectionRecord.VNCCompression = value;
            }

            if (headers.Contains("VNCEncoding"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCEncoding")], out ProtocolVNC.Encoding value))
                    connectionRecord.VNCEncoding = value;
            }

            if (headers.Contains("VNCAuthMode"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCAuthMode")], out ProtocolVNC.AuthMode value))
                    connectionRecord.VNCAuthMode = value;
            }

            if (headers.Contains("VNCProxyType"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCProxyType")], out ProtocolVNC.ProxyType value))
                    connectionRecord.VNCProxyType = value;
            }

            if (headers.Contains("VNCProxyPort"))
            {
                if (int.TryParse(connectionCsv[headers.IndexOf("VNCProxyPort")], out int value))
                    connectionRecord.VNCProxyPort = value;
            }

            if (headers.Contains("VNCColors"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCColors")], out ProtocolVNC.Colors value))
                    connectionRecord.VNCColors = value;
            }

            if (headers.Contains("VNCSmartSizeMode"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("VNCSmartSizeMode")], out ProtocolVNC.SmartSizeMode value))
                    connectionRecord.VNCSmartSizeMode = value;
            }

            if (headers.Contains("VNCViewOnly"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("VNCViewOnly")], out bool value))
                    connectionRecord.VNCViewOnly = value;
            }

            if (headers.Contains("VNCClipboardRedirect"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("VNCClipboardRedirect")], out bool value))
                    connectionRecord.VNCClipboardRedirect = value;
            }

            if (headers.Contains("RDGatewayUsageMethod"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RDGatewayUsageMethod")], out RDGatewayUsageMethod value))
                    connectionRecord.RDGatewayUsageMethod = value;
            }

            if (headers.Contains("RDGatewayUseConnectionCredentials"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RDGatewayUseConnectionCredentials")], out RDGatewayUseConnectionCredentials value))
                    connectionRecord.RDGatewayUseConnectionCredentials = value;
            }

            if (headers.Contains("Favorite"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("Favorite")], out bool value))
                    connectionRecord.Favorite = value;
            }

            if (connectionRecord is ContainerInfo containerRecord && headers.Contains("AutoSort"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("AutoSort")], out bool value))
                    containerRecord.AutoSort = value;
            }

            if (headers.Contains("RdpVersion"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("RdpVersion")], true, out RdpVersion version))
                    connectionRecord.RdpVersion = version;
            }
            if (headers.Contains("ExternalCredentialProvider"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("ExternalCredentialProvider")], out ExternalCredentialProvider value))
                    connectionRecord.ExternalCredentialProvider = value;
            }
            if (headers.Contains("ExternalAddressProvider"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("ExternalAddressProvider")], out ExternalAddressProvider value))
                    connectionRecord.ExternalAddressProvider = value;
            }

            if (headers.Contains("PrivateKeyPath"))
            {
                connectionRecord.PrivateKeyPath = connectionCsv[headers.IndexOf("PrivateKeyPath")];
            }

            if (headers.Contains("UsePersistentBrowser"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("UsePersistentBrowser")], out bool value))
                    connectionRecord.UsePersistentBrowser = value;
            }

            if (headers.Contains("ScriptErrorsSuppressed"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("ScriptErrorsSuppressed")], out bool value))
                    connectionRecord.ScriptErrorsSuppressed = value;
            }

            if (headers.Contains("DesktopScaleFactor"))
            {
                if (Enum.TryParse(connectionCsv[headers.IndexOf("DesktopScaleFactor")], true, out RDPDesktopScaleFactor value))
                    connectionRecord.DesktopScaleFactor = value;
            }

            #region Inheritance

            if (headers.Contains("InheritCacheBitmaps"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritCacheBitmaps")], out bool value))
                    connectionRecord.Inheritance.CacheBitmaps = value;
            }

            if (headers.Contains("InheritColors"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritColors")], out bool value))
                    connectionRecord.Inheritance.Colors = value;
            }

            if (headers.Contains("InheritDescription"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDescription")], out bool value))
                    connectionRecord.Inheritance.Description = value;
            }

            if (headers.Contains("InheritDisplayThemes"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisplayThemes")], out bool value))
                    connectionRecord.Inheritance.DisplayThemes = value;
            }

            if (headers.Contains("InheritDisplayWallpaper"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisplayWallpaper")], out bool value))
                    connectionRecord.Inheritance.DisplayWallpaper = value;
            }

            if (headers.Contains("InheritEnableFontSmoothing"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritEnableFontSmoothing")], out bool value))
                    connectionRecord.Inheritance.EnableFontSmoothing = value;
            }

            if (headers.Contains("InheritEnableDesktopComposition"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritEnableDesktopComposition")], out bool value))
                    connectionRecord.Inheritance.EnableDesktopComposition = value;
            }

            if (headers.Contains("InheritDisableFullWindowDrag"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisableFullWindowDrag")], out bool value))
                    connectionRecord.Inheritance.DisableFullWindowDrag = value;
            }

            if (headers.Contains("InheritDisableMenuAnimations"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisableMenuAnimations")], out bool value))
                    connectionRecord.Inheritance.DisableMenuAnimations = value;
            }

            if (headers.Contains("InheritDisableCursorShadow"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisableCursorShadow")], out bool value))
                    connectionRecord.Inheritance.DisableCursorShadow = value;
            }

            if (headers.Contains("InheritDisableCursorBlinking"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDisableCursorBlinking")], out bool value))
                    connectionRecord.Inheritance.DisableCursorBlinking = value;
            }

            if (headers.Contains("InheritDomain"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDomain")], out bool value))
                    connectionRecord.Inheritance.Domain = value;
            }

            if (headers.Contains("InheritIcon"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritIcon")], out bool value))
                    connectionRecord.Inheritance.Icon = value;
            }

            if (headers.Contains("InheritPanel"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPanel")], out bool value))
                    connectionRecord.Inheritance.Panel = value;
            }

            if (headers.Contains("InheritTabColor"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritTabColor")], out bool value))
                    connectionRecord.Inheritance.TabColor = value;
            }

            if (headers.Contains("InheritConnectionFrameColor"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritConnectionFrameColor")], out bool value))
                    connectionRecord.Inheritance.ConnectionFrameColor = value;
            }

            if (headers.Contains("InheritColor"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritColor")], out bool value))
                    connectionRecord.Inheritance.Color = value;
            }

            if (headers.Contains("InheritPassword"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPassword")], out bool value))
                    connectionRecord.Inheritance.Password = value;
            }

            if (headers.Contains("InheritPort"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPort")], out bool value))
                    connectionRecord.Inheritance.Port = value;
            }

            if (headers.Contains("InheritProtocol"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritProtocol")], out bool value))
                    connectionRecord.Inheritance.Protocol = value;
            }

            if (headers.Contains("InheritSSHTunnelConnectionName"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritSSHTunnelConnectionName")], out bool value))
                    connectionRecord.Inheritance.SSHTunnelConnectionName = value;
            }

            if (headers.Contains("InheritOpeningCommand"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritOpeningCommand")], out bool value))
                    connectionRecord.Inheritance.OpeningCommand = value;
            }

            if (headers.Contains("InheritSSHOptions"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritSSHOptions")], out bool value))
                    connectionRecord.Inheritance.SSHOptions = value;
            }

            if (headers.Contains("InheritPuttySession"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPuttySession")], out bool value))
                    connectionRecord.Inheritance.PuttySession = value;
            }

            if (headers.Contains("InheritRedirectDiskDrives"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectDiskDrives")], out bool value))
                    connectionRecord.Inheritance.RedirectDiskDrives = value;
            }
			
            if (headers.Contains("InheritRedirectDiskDrivesCustom"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectDiskDrivesCustom")], out bool value))
                    connectionRecord.Inheritance.RedirectDiskDrivesCustom = value;
            }

            if (headers.Contains("InheritRedirectKeys"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectKeys")], out bool value))
                    connectionRecord.Inheritance.RedirectKeys = value;
            }

            if (headers.Contains("InheritRedirectPorts"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectPorts")], out bool value))
                    connectionRecord.Inheritance.RedirectPorts = value;
            }

            if (headers.Contains("InheritRedirectPrinters"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectPrinters")], out bool value))
                    connectionRecord.Inheritance.RedirectPrinters = value;
            }

            if (headers.Contains("InheritRedirectClipboard"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectClipboard")], out bool value))
                    connectionRecord.Inheritance.RedirectClipboard = value;
            }

            if (headers.Contains("InheritRedirectSmartCards"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectSmartCards")], out bool value))
                    connectionRecord.Inheritance.RedirectSmartCards = value;
            }

            if (headers.Contains("InheritRedirectSound"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectSound")], out bool value))
                    connectionRecord.Inheritance.RedirectSound = value;
            }

            if (headers.Contains("InheritResolution"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritResolution")], out bool value))
                    connectionRecord.Inheritance.Resolution = value;
            }

            if (headers.Contains("InheritAutomaticResize"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritAutomaticResize")], out bool value))
                    connectionRecord.Inheritance.AutomaticResize = value;
            }

            if (headers.Contains("InheritUseConsoleSession"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseConsoleSession")], out bool value))
                    connectionRecord.Inheritance.UseConsoleSession = value;
            }

            if (headers.Contains("InheritUseCredSsp"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseCredSsp")], out bool value))
                    connectionRecord.Inheritance.UseCredSsp = value;
            }

            if (headers.Contains("InheritUseRestrictedAdmin"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseRestrictedAdmin")], out bool value))
                    connectionRecord.Inheritance.UseRestrictedAdmin = value;
            }

            if (headers.Contains("InheritUseRCG"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseRCG")], out bool value))
                    connectionRecord.Inheritance.UseRCG = value;
            }


            if (headers.Contains("InheritUseVmId"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseVmId")], out bool value))
                    connectionRecord.Inheritance.UseVmId = value;
            }

            if (headers.Contains("InheritUseEnhancedMode"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUseEnhancedMode")], out bool value))
                    connectionRecord.Inheritance.UseEnhancedMode = value;
            }

            if (headers.Contains("InheritRenderingEngine"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRenderingEngine")], out bool value))
                    connectionRecord.Inheritance.RenderingEngine = value;
            }

            if (headers.Contains("InheritExternalCredentialProvider"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritExternalCredentialProvider")], out bool value))
                    connectionRecord.Inheritance.ExternalCredentialProvider = value;
            }
            if (headers.Contains("InheritUserViaAPI"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUserViaAPI")], out bool value))
                    connectionRecord.Inheritance.UserViaAPI = value;
            }

            if (headers.Contains("InheritUsername"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUsername")], out bool value))
                    connectionRecord.Inheritance.Username = value;
            }

            if (headers.Contains("InheritVmId"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVmId")], out bool value))
                    connectionRecord.Inheritance.VmId = value;
            }

            if (headers.Contains("InheritRDPAuthenticationLevel"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDPAuthenticationLevel")], out bool value))
                    connectionRecord.Inheritance.RDPAuthenticationLevel = value;
            }

            if (headers.Contains("InheritLoadBalanceInfo"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritLoadBalanceInfo")], out bool value))
                    connectionRecord.Inheritance.LoadBalanceInfo = value;
            }

            if (headers.Contains("InheritOpeningCommand"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritOpeningCommand")], out bool value))
                    connectionRecord.Inheritance.OpeningCommand = value;
            }

            if (headers.Contains("InheritPreExtApp"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPreExtApp")], out bool value))
                    connectionRecord.Inheritance.PreExtApp = value;
            }

            if (headers.Contains("InheritPostExtApp"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPostExtApp")], out bool value))
                    connectionRecord.Inheritance.PostExtApp = value;
            }

            if (headers.Contains("InheritMacAddress"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritMacAddress")], out bool value))
                    connectionRecord.Inheritance.MacAddress = value;
            }

            if (headers.Contains("InheritUserField"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritUserField")], out bool value))
                    connectionRecord.Inheritance.UserField = value;
            }

            for (int i = 1; i <= 10; i++)
            {
                string key = $"InheritUserField{i}";
                if (headers.Contains(key))
                {
                    if (bool.TryParse(connectionCsv[headers.IndexOf(key)], out bool value))
                    {
                        typeof(ConnectionInfoInheritance).GetProperty($"UserField{i}")?.SetValue(connectionRecord.Inheritance, value);
                    }
                }
            }

            if (headers.Contains("InheritHostname"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritHostname")], out bool value))
                    connectionRecord.Inheritance.Hostname = value;
            }

            if (headers.Contains("InheritAlternativeAddress"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritAlternativeAddress")], out bool value))
                    connectionRecord.Inheritance.AlternativeAddress = value;
            }

            if (headers.Contains("InheritEnvironmentTags"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritEnvironmentTags")], out bool value))
                    connectionRecord.Inheritance.EnvironmentTags = value;
            }

            if (headers.Contains("InheritFavorite"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritFavorite")], out bool value))
                    connectionRecord.Inheritance.Favorite = value;
            }

            if (headers.Contains("InheritAutoSort"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritAutoSort")], out bool value))
                    connectionRecord.Inheritance.AutoSort = value;
            }

            if (headers.Contains("InheritExtApp"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritExtApp")], out bool value))
                    connectionRecord.Inheritance.ExtApp = value;
            }

            if (headers.Contains("InheritVNCCompression"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCCompression")], out bool value))
                    connectionRecord.Inheritance.VNCCompression = value;
            }

            if (headers.Contains("InheritVNCEncoding"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCEncoding")], out bool value))
                    connectionRecord.Inheritance.VNCEncoding = value;
            }

            if (headers.Contains("InheritVNCAuthMode"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCAuthMode")], out bool value))
                    connectionRecord.Inheritance.VNCAuthMode = value;
            }

            if (headers.Contains("InheritVNCProxyType"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCProxyType")], out bool value))
                    connectionRecord.Inheritance.VNCProxyType = value;
            }

            if (headers.Contains("InheritVNCProxyIP"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCProxyIP")], out bool value))
                    connectionRecord.Inheritance.VNCProxyIP = value;
            }

            if (headers.Contains("InheritVNCProxyPort"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCProxyPort")], out bool value))
                    connectionRecord.Inheritance.VNCProxyPort = value;
            }

            if (headers.Contains("InheritVNCProxyUsername"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCProxyUsername")], out bool value))
                    connectionRecord.Inheritance.VNCProxyUsername = value;
            }

            if (headers.Contains("InheritVNCProxyPassword"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCProxyPassword")], out bool value))
                    connectionRecord.Inheritance.VNCProxyPassword = value;
            }

            if (headers.Contains("InheritVNCColors"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCColors")], out bool value))
                    connectionRecord.Inheritance.VNCColors = value;
            }

            if (headers.Contains("InheritVNCSmartSizeMode"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCSmartSizeMode")], out bool value))
                    connectionRecord.Inheritance.VNCSmartSizeMode = value;
            }

            if (headers.Contains("InheritVNCViewOnly"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCViewOnly")], out bool value))
                    connectionRecord.Inheritance.VNCViewOnly = value;
            }

            if (headers.Contains("InheritVNCClipboardRedirect"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritVNCClipboardRedirect")], out bool value))
                    connectionRecord.Inheritance.VNCClipboardRedirect = value;
            }

            if (headers.Contains("InheritRDGatewayUsageMethod"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayUsageMethod")], out bool value))
                    connectionRecord.Inheritance.RDGatewayUsageMethod = value;
            }

            if (headers.Contains("InheritRDGatewayHostname"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayHostname")], out bool value))
                    connectionRecord.Inheritance.RDGatewayHostname = value;
            }

            if (headers.Contains("InheritRDGatewayUseConnectionCredentials"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayUseConnectionCredentials")],
                                  out bool value))
                    connectionRecord.Inheritance.RDGatewayUseConnectionCredentials = value;
            }

            if (headers.Contains("InheritRDGatewayUsername"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayUsername")], out bool value))
                    connectionRecord.Inheritance.RDGatewayUsername = value;
            }

            if (headers.Contains("InheritRDGatewayPassword"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayPassword")], out bool value))
                    connectionRecord.Inheritance.RDGatewayPassword = value;
            }

            if (headers.Contains("InheritRDGatewayDomain"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayDomain")], out bool value))
                    connectionRecord.Inheritance.RDGatewayDomain = value;
            }

            if (headers.Contains("InheritRDGatewayExternalCredentialProvider"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayExternalCredentialProvider")], out bool value))
                    connectionRecord.Inheritance.RDGatewayExternalCredentialProvider = value;
            }
            if (headers.Contains("InheritRDGatewayUserViaAPI"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDGatewayUserViaAPI")], out bool value))
                    connectionRecord.Inheritance.RDGatewayUserViaAPI = value;
            }


            if (headers.Contains("InheritRDPAlertIdleTimeout"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDPAlertIdleTimeout")], out bool value))
                    connectionRecord.Inheritance.RDPAlertIdleTimeout = value;
            }

            if (headers.Contains("InheritRDPMinutesToIdleTimeout"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRDPMinutesToIdleTimeout")], out bool value))
                    connectionRecord.Inheritance.RDPMinutesToIdleTimeout = value;
            }

            if (headers.Contains("InheritSoundQuality"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritSoundQuality")], out bool value))
                    connectionRecord.Inheritance.SoundQuality = value;
            }

            if (headers.Contains("InheritRedirectAudioCapture"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRedirectAudioCapture")], out bool value))
                    connectionRecord.Inheritance.RedirectAudioCapture = value;
            }

            if (headers.Contains("InheritRdpVersion"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritRdpVersion")], out bool value))
                    connectionRecord.Inheritance.RdpVersion = value;
            }

            if (headers.Contains("InheritPrivateKeyPath"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritPrivateKeyPath")], out bool value))
                    connectionRecord.Inheritance.PrivateKeyPath = value;
            }

            if (headers.Contains("InheritDesktopScaleFactor"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritDesktopScaleFactor")], out bool value))
                    connectionRecord.Inheritance.DesktopScaleFactor = value;
            }

            if (headers.Contains("InheritScriptErrorsSuppressed"))
            {
                if (bool.TryParse(connectionCsv[headers.IndexOf("InheritScriptErrorsSuppressed")], out bool value))
                    connectionRecord.Inheritance.ScriptErrorsSuppressed = value;
            }

            #endregion

            return connectionRecord;
        }
    }
}
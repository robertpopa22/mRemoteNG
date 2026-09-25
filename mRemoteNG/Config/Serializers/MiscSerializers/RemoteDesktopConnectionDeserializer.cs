using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Versioning;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Messages;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Config.Serializers.MiscSerializers
{
    [SupportedOSPlatform("windows")]
    public class RemoteDesktopConnectionDeserializer : IDeserializer<string, ConnectionTreeModel>
    {
        // .rdp file schema: https://technet.microsoft.com/en-us/library/ff393699(v=ws.10).aspx

        public ConnectionTreeModel Deserialize(string rdcFileContent)
        {
            ConnectionTreeModel connectionTreeModel = new();
            RootNodeInfo root = new(RootNodeType.Connection);
            connectionTreeModel.AddRootNode(root);
            ConnectionInfo connectionInfo = new();
            List<string> ignored = [];
            foreach (string line in rdcFileContent.Split(Environment.NewLine.ToCharArray()))
            {
                string[] parts = line.Split(new[] { ':' }, 3);
                if (parts.Length < 3)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[2].Trim();

                SetConnectionInfoParameter(connectionInfo, key, value, ignored);
            }

            root.AddChild(connectionInfo);

            if (ignored.Count > 0)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg,
                    $"Imported '{connectionInfo.Hostname}' without the settings that would lower its security: " +
                    string.Join("; ", ignored) + ". Change them on the connection if you really want them.");
            }

            return connectionTreeModel;
        }

        /// <summary>Orders server-authentication levels by how much they protect the user.</summary>
        public static int Strength(AuthenticationLevel level) => level switch
        {
            AuthenticationLevel.NoAuth => 0,
            AuthenticationLevel.WarnOnFailedAuth => 1,
            AuthenticationLevel.AuthRequired => 2,
            _ => 0
        };

        private static void SetConnectionInfoParameter(ConnectionInfo connectionInfo, string key, string value, List<string> ignored)
        {
            switch (key.ToLowerInvariant())
            {
                case "full address":
                    Uri uri = new("dummyscheme" + Uri.SchemeDelimiter + value);
                    if (!string.IsNullOrEmpty(uri.Host))
                        connectionInfo.Hostname = uri.Host;
                    if (uri.Port != -1)
                        connectionInfo.Port = uri.Port;
                    break;
                case "server port":
                    // TryParse, not Convert.ToInt32, so a malformed/out-of-range port in a
                    // hand-edited or hostile .rdp file doesn't throw and abort the whole import.
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int serverPort))
                        connectionInfo.Port = serverPort;
                    break;
                case "username":
                    connectionInfo.Username = value;
                    break;
                case "domain":
                    connectionInfo.Domain = value;
                    break;
                case "session bpp":
                    switch (value)
                    {
                        case "8":
                            connectionInfo.Colors = RDPColors.Colors256;
                            break;
                        case "15":
                            connectionInfo.Colors = RDPColors.Colors15Bit;
                            break;
                        case "16":
                            connectionInfo.Colors = RDPColors.Colors16Bit;
                            break;
                        case "24":
                            connectionInfo.Colors = RDPColors.Colors24Bit;
                            break;
                        case "32":
                            connectionInfo.Colors = RDPColors.Colors32Bit;
                            break;
                    }
                    break;
                case "bitmapcachepersistenable":
                    connectionInfo.CacheBitmaps = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "screen mode id":
                    connectionInfo.Resolution = string.Equals(value, "2", StringComparison.Ordinal)
                        ? RDPResolutions.Fullscreen
                        : RDPResolutions.FitToWindow;
                    break;
                case "connect to console":
                    connectionInfo.UseConsoleSession = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                // "disable ..." keys are negative: 1 means the feature is OFF. These two used to be
                // read as if 1 meant "display", so every imported file got wallpaper and themes
                // backwards, and a file this application exported came back inverted.
                case "disable wallpaper":
                    connectionInfo.DisplayWallpaper = !string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "disable themes":
                    connectionInfo.DisplayThemes = !string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "disable full window drag":
                    connectionInfo.DisableFullWindowDrag = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "disable menu anims":
                    connectionInfo.DisableMenuAnimations = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "disable cursor setting":
                    // Mirrors RdpConnectionSerializer, which writes DisableCursorShadow under this key.
                    connectionInfo.DisableCursorShadow = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "allow font smoothing":
                    connectionInfo.EnableFontSmoothing = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "allow desktop composition":
                    connectionInfo.EnableDesktopComposition = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "keyboardhook":
                    connectionInfo.RedirectKeys = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "redirectsmartcards":
                    connectionInfo.RedirectSmartCards = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "redirectdrives":
                    connectionInfo.RedirectDiskDrives = (string.Equals(value, "1", StringComparison.Ordinal) ? RDPDiskDrives.Local : RDPDiskDrives.None);
                    break;
                case "redirectdrivescustom":
                    connectionInfo.RedirectDiskDrivesCustom = value;
                    break;
                case "redirectcomports":
                    connectionInfo.RedirectPorts = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "redirectprinters":
                    connectionInfo.RedirectPrinters = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "redirectclipboard":
                    connectionInfo.RedirectClipboard = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "audiomode":
                    switch (value)
                    {
                        case "0":
                            connectionInfo.RedirectSound = RDPSounds.BringToThisComputer;
                            break;
                        case "1":
                            connectionInfo.RedirectSound = RDPSounds.LeaveAtRemoteComputer;
                            break;
                        case "2":
                            connectionInfo.RedirectSound = RDPSounds.DoNotPlay;
                            break;
                    }
                    break;
                // "audiocapturemode" is the key mstsc and our own exporter write; the older
                // "redirectaudiocapture" spelling is kept so files that relied on it still import.
                case "audiocapturemode":
                case "redirectaudiocapture":
                    connectionInfo.RedirectAudioCapture = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                // Authentication keys. Without these an imported Entra ID (enablerdsaadauth:i:1)
                // connection came in with Entra ID off and CredSSP on, and failed at once from a
                // machine that is not Entra-joined (#196).
                case "enablerdsaadauth":
                    connectionInfo.EnableRdsAadAuth = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                case "redirectwebauthn":
                    connectionInfo.RedirectWebAuthn = string.Equals(value, "1", StringComparison.Ordinal);
                    break;
                // CredSSP and the server-authentication level are imported only when they keep or
                // raise protection. A file is data from whoever wrote it; "enablecredsspsupport:i:0"
                // or "authentication level:i:0" in a shared or downloaded .rdp would otherwise
                // silently drop NLA or connect to a server whose identity failed to verify. Those
                // values are left at the user's defaults and reported instead. mstsc honours them;
                // a manager that imports many files at once and shows no per-file prompt should not.
                case "enablecredsspsupport":
                    if (string.Equals(value, "1", StringComparison.Ordinal))
                        connectionInfo.UseCredSsp = true;
                    else if (connectionInfo.UseCredSsp)
                        ignored.Add("enablecredsspsupport:i:" + value + " (CredSSP / NLA left on)");
                    break;
                case "authentication level":
                    // The .rdp values 0/1/2 are exactly AuthenticationLevel's values, which is how
                    // RdpConnectionSerializer writes them. Anything else keeps the default.
                    if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int authLevel)
                        && Enum.IsDefined(typeof(AuthenticationLevel), authLevel))
                    {
                        AuthenticationLevel fromFile = (AuthenticationLevel)authLevel;
                        if (Strength(fromFile) >= Strength(connectionInfo.RDPAuthenticationLevel))
                            connectionInfo.RDPAuthenticationLevel = fromFile;
                        else
                            ignored.Add("authentication level:i:" + value + " (kept " + connectionInfo.RDPAuthenticationLevel + ")");
                    }
                    break;
                case "loadbalanceinfo":
                    connectionInfo.LoadBalanceInfo = value;
                    break;
                case "gatewayusagemethod":
                    switch (value)
                    {
                        case "0":
                            connectionInfo.RDGatewayUsageMethod = RDGatewayUsageMethod.Never;
                            break;
                        case "1":
                            connectionInfo.RDGatewayUsageMethod = RDGatewayUsageMethod.Always;
                            break;
                        case "2":
                            connectionInfo.RDGatewayUsageMethod = RDGatewayUsageMethod.Detect;
                            break;
                    }
                    break;
                case "gatewayhostname":
                    connectionInfo.RDGatewayHostname = value;
                    break;
                case "gatewaycredentialssource":
                    switch(value)
                    {
                        case "0":
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.ExternalCredentialProvider;
                            break;
                        case "1":
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.SmartCard;
                            break;
                        case "2":
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.Yes;
                            break;
                        case "3":
                            // Both 3 and 4 require that the user enter gateway credentials manually
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.No;
                            break;
                        case "4":
                            // Both 3 and 4 require that the user enter gateway credentials manually
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.No;
                            break;
                        case "5":
                            connectionInfo.RDGatewayUseConnectionCredentials = RDGatewayUseConnectionCredentials.AccessToken;
                            break;
                    }
                    break;
                case "gatewayaccesstoken":
                    connectionInfo.RDGatewayAccessToken = value;
                    break;
                case "alternate shell":
                    connectionInfo.RDPStartProgram = value;
                    break;
                case "shell working directory":
                    connectionInfo.RDPStartProgramWorkDir = value;
                    break;
            }
        }
    }
}
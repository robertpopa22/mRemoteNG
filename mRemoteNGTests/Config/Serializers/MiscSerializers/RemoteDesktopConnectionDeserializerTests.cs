using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Tree;
using mRemoteNGTests.Properties;
using NUnit.Framework;
using System.Linq;
using mRemoteNG.Config.Serializers.MiscSerializers;

namespace mRemoteNGTests.Config.Serializers.MiscSerializers;

public class RemoteDesktopConnectionDeserializerTests
{
    // .rdp file schema: https://technet.microsoft.com/en-us/library/ff393699(v=ws.10).aspx
    private RemoteDesktopConnectionDeserializer _deserializer;
    private ConnectionTreeModel _connectionTreeModel;
    private const string ExpectedHostname = "testhostname.domain.com";
    private const string ExpectedUserName = "myusernamehere";
    private const string ExpectedDomain = "myspecialdomain";
    private const string ExpectedGatewayHostname = "gatewayhostname.domain.com";
    private const string ExpectedLoadBalanceInfo = "tsv://MS Terminal Services Plugin.1.RDS-NAME";
    private const int ExpectedPort = 9933;
    private const RDPColors ExpectedColors = RDPColors.Colors24Bit;
    private const bool ExpectedBitmapCaching = true;
    private const RDPResolutions ExpectedResolutionMode = RDPResolutions.FitToWindow;
    // The fixture says "disable wallpaper:i:1" and "disable themes:i:1": both are OFF. These two
    // expectations used to be true, which pinned the importer's inversion instead of catching it.
    private const bool ExpectedWallpaperDisplay = false;
    private const bool ExpectedThemesDisplay = false;
    private const bool ExpectedFontSmoothing = true;
    private const bool ExpectedDesktopComposition = true;
    private const bool ExpectedSmartcardRedirection = true;
    private const RDPDiskDrives ExpectedDriveRedirection = RDPDiskDrives.Local;
    private const bool ExpectedPortRedirection = true;
    private const bool ExpectedPrinterRedirection = true;
    private const RDPSounds ExpectedSoundRedirection = RDPSounds.BringToThisComputer;
    private const string ExpectedStartProgram = "alternate shell";

    [OneTimeSetUp]
    public void OnetimeSetup()
    {
        var connectionFileContents = Resources.test_remotedesktopconnection_rdp;
        _deserializer = new RemoteDesktopConnectionDeserializer();
        _connectionTreeModel = _deserializer.Deserialize(connectionFileContents);
    }

    [Test]
    public void ConnectionTreeModelHasARootNode()
    {
        var numberOfRootNodes = _connectionTreeModel.RootNodes.Count;
        Assert.That(numberOfRootNodes, Is.GreaterThan(0));
    }

    [Test]
    public void RootNodeHasConnectionInfo()
    {
        var rootNodeContents = _connectionTreeModel.RootNodes.First().Children.OfType<ConnectionInfo>();
        Assert.That(rootNodeContents, Is.Not.Empty);
    }

    [Test]
    public void HostnameImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Hostname, Is.EqualTo(ExpectedHostname));
    }

    [Test]
    public void PortImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Port, Is.EqualTo(ExpectedPort));
    }

    [Test]
    public void UsernameImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Username, Is.EqualTo(ExpectedUserName));
    }

    [Test]
    public void DomainImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Domain, Is.EqualTo(ExpectedDomain));
    }

    [Test]
    public void RdpColorsImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Colors, Is.EqualTo(ExpectedColors));
    }

    [Test]
    public void BitmapCachingImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.CacheBitmaps, Is.EqualTo(ExpectedBitmapCaching));
    }

    [Test]
    public void ResolutionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.Resolution, Is.EqualTo(ExpectedResolutionMode));
    }

    [Test]
    public void DisplayWallpaperImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.DisplayWallpaper, Is.EqualTo(ExpectedWallpaperDisplay));
    }

    [Test]
    public void DisplayThemesImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.DisplayThemes, Is.EqualTo(ExpectedThemesDisplay));
    }

    [Test]
    public void FontSmoothingImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.EnableFontSmoothing, Is.EqualTo(ExpectedFontSmoothing));
    }

    [Test]
    public void DesktopCompositionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.EnableDesktopComposition, Is.EqualTo(ExpectedDesktopComposition));
    }

    [Test]
    public void SmartcardRedirectionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RedirectSmartCards, Is.EqualTo(ExpectedSmartcardRedirection));
    }

    [Test]
    public void DriveRedirectionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RedirectDiskDrives, Is.EqualTo(ExpectedDriveRedirection));
    }

    [Test]
    public void PortRedirectionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RedirectPorts, Is.EqualTo(ExpectedPortRedirection));
    }

    [Test]
    public void PrinterRedirectionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RedirectPrinters, Is.EqualTo(ExpectedPrinterRedirection));
    }

    [Test]
    public void SoundRedirectionImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RedirectSound, Is.EqualTo(ExpectedSoundRedirection));
    }

    [Test]
    public void LoadBalanceInfoImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.LoadBalanceInfo, Is.EqualTo(ExpectedLoadBalanceInfo));
    }

    [Test]
    public void StartProgramImportedCorrectly()
    {
        var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
        Assert.That(connectionInfo.RDPStartProgram, Is.EqualTo(ExpectedStartProgram));
    }

    //[Test]
    //public void GatewayHostnameImportedCorrectly()
    //{
    //    var connectionInfo = _connectionTreeModel.RootNodes.First().Children.First();
    //    Assert.That(connectionInfo.RDGatewayHostname, Is.EqualTo(_expectedGatewayHostname));
    //}

    [Test]
    public void MalformedServerPort_DoesNotAbortImport()
    {
        // A non-numeric "server port" must not throw (Convert.ToInt32 would) and abort the whole
        // .rdp import; the rest of the file should still import.
        const string rdp = "full address:s:myhost\r\nserver port:i:notanumber\r\nusername:s:bob";
        var deserializer = new RemoteDesktopConnectionDeserializer();
        Assert.That(() => deserializer.Deserialize(rdp), Throws.Nothing);
        var connectionInfo = deserializer.Deserialize(rdp).RootNodes.First().Children.First();
        Assert.That(connectionInfo.Hostname, Is.EqualTo("myhost"));
        Assert.That(connectionInfo.Username, Is.EqualTo("bob"));
    }

    /// <summary>
    /// The #196 reporter's file (anonymised): an Entra ID connection. Before the fix none of these
    /// keys were read, so the imported connection had Entra ID off and failed at once from a
    /// machine that is not Entra-joined. The file also turns CredSSP off; that one is deliberately
    /// NOT imported (it would drop NLA), so CredSSP stays at the default.
    /// </summary>
    [Test]
    public void EntraIdFileFromIssue196_ImportsAuthenticationSettings()
    {
        const string rdp = "full address:s:HOSTNAME\r\n" +
                           "authentication level:i:2\r\n" +
                           "enablecredsspsupport:i:0\r\n" +
                           "enablerdsaadauth:i:1\r\n" +
                           "redirectwebauthn:i:1\r\n" +
                           "audiocapturemode:i:1\r\n" +
                           "disable full window drag:i:1\r\n" +
                           "disable menu anims:i:1\r\n" +
                           "shell working directory:s:C:\\work\r\n";
        var connectionInfo = new RemoteDesktopConnectionDeserializer().Deserialize(rdp).RootNodes.First().Children.First();

        Assert.Multiple(() =>
        {
            Assert.That(connectionInfo.EnableRdsAadAuth, Is.True);
            Assert.That(connectionInfo.UseCredSsp, Is.EqualTo(new ConnectionInfo().UseCredSsp));
            Assert.That(connectionInfo.RDPAuthenticationLevel, Is.EqualTo(AuthenticationLevel.WarnOnFailedAuth));
            Assert.That(connectionInfo.RedirectWebAuthn, Is.True);
            Assert.That(connectionInfo.RedirectAudioCapture, Is.True);
            Assert.That(connectionInfo.DisableFullWindowDrag, Is.True);
            Assert.That(connectionInfo.DisableMenuAnimations, Is.True);
            Assert.That(connectionInfo.RDPStartProgramWorkDir, Is.EqualTo(@"C:\work"));
        });
    }

    /// <summary>
    /// A file may raise protection but never lower it on import: a shared or downloaded .rdp with
    /// "authentication level:i:0" would otherwise connect to a server whose identity failed to
    /// verify, without a warning, and "enablecredsspsupport:i:0" would drop NLA.
    /// </summary>
    [Test]
    public void SettingsThatLowerSecurity_AreNotImported()
    {
        var defaults = new ConnectionInfo();
        var imported = new RemoteDesktopConnectionDeserializer()
            .Deserialize("full address:s:h\r\nauthentication level:i:0\r\nenablecredsspsupport:i:0")
            .RootNodes.First().Children.First();

        Assert.Multiple(() =>
        {
            if (RemoteDesktopConnectionDeserializer.Strength(defaults.RDPAuthenticationLevel) > 0)
                Assert.That(imported.RDPAuthenticationLevel, Is.EqualTo(defaults.RDPAuthenticationLevel));
            if (defaults.UseCredSsp)
                Assert.That(imported.UseCredSsp, Is.True);
        });
    }

    [Test]
    public void StricterAuthenticationLevel_IsImported()
    {
        var imported = new RemoteDesktopConnectionDeserializer()
            .Deserialize("full address:s:h\r\nauthentication level:i:1").RootNodes.First().Children.First();
        Assert.That(imported.RDPAuthenticationLevel, Is.EqualTo(AuthenticationLevel.AuthRequired));
    }

    [TestCase("9")]
    [TestCase("x")]
    [TestCase("-1")]
    public void UnknownAuthenticationLevel_KeepsTheDefault(string value)
    {
        var imported = new RemoteDesktopConnectionDeserializer()
            .Deserialize("full address:s:h\r\nauthentication level:i:" + value).RootNodes.First().Children.First();
        Assert.That(imported.RDPAuthenticationLevel, Is.EqualTo(new ConnectionInfo().RDPAuthenticationLevel));
    }

    [Test]
    public void LegacyRedirectAudioCaptureKey_StillImports()
    {
        var imported = new RemoteDesktopConnectionDeserializer()
            .Deserialize("full address:s:h\r\nredirectaudiocapture:i:1").RootNodes.First().Children.First();
        Assert.That(imported.RedirectAudioCapture, Is.True);
    }

    /// <summary>
    /// Export then import must give back what was exported, for every key the exporter writes and
    /// the importer maps. Each value is the opposite of the ConnectionInfo default, so a key that
    /// is ignored or inverted on import shows up as a named mismatch.
    /// </summary>
    [Test]
    public void ExportThenImport_RoundTripsEveryMappedSetting()
    {
        var defaults = new ConnectionInfo();
        var original = new ConnectionInfo
        {
            Hostname = "roundtrip.example",
            Port = 3390,
            DisplayWallpaper = !defaults.DisplayWallpaper,
            DisplayThemes = !defaults.DisplayThemes,
            DisableFullWindowDrag = !defaults.DisableFullWindowDrag,
            DisableMenuAnimations = !defaults.DisableMenuAnimations,
            DisableCursorShadow = !defaults.DisableCursorShadow,
            EnableFontSmoothing = !defaults.EnableFontSmoothing,
            EnableDesktopComposition = !defaults.EnableDesktopComposition,
            CacheBitmaps = !defaults.CacheBitmaps,
            RedirectClipboard = !defaults.RedirectClipboard,
            RedirectPrinters = !defaults.RedirectPrinters,
            RedirectPorts = !defaults.RedirectPorts,
            RedirectSmartCards = !defaults.RedirectSmartCards,
            RedirectAudioCapture = !defaults.RedirectAudioCapture,
            RedirectWebAuthn = !defaults.RedirectWebAuthn,
            EnableRdsAadAuth = !defaults.EnableRdsAadAuth,
            UseConsoleSession = !defaults.UseConsoleSession,
            // These two only round-trip in the direction that keeps or raises protection (see
            // SettingsThatLowerSecurity_AreNotImported), so they are set to the strictest value.
            UseCredSsp = true,
            RDPAuthenticationLevel = AuthenticationLevel.AuthRequired,
            RDPStartProgram = "notepad.exe",
            RDPStartProgramWorkDir = @"C:\temp",
        };

        string exported = new mRemoteNG.Config.Serializers.ConnectionSerializers.Rdp.RdpConnectionSerializer(
            new mRemoteNG.Security.SaveFilter(true)).Serialize(original);
        var imported = new RemoteDesktopConnectionDeserializer().Deserialize(exported).RootNodes.First().Children.First();

        Assert.Multiple(() =>
        {
            Assert.That(imported.Hostname, Is.EqualTo(original.Hostname), "full address");
            Assert.That(imported.Port, Is.EqualTo(original.Port), "server port");
            Assert.That(imported.DisplayWallpaper, Is.EqualTo(original.DisplayWallpaper), "disable wallpaper");
            Assert.That(imported.DisplayThemes, Is.EqualTo(original.DisplayThemes), "disable themes");
            Assert.That(imported.DisableFullWindowDrag, Is.EqualTo(original.DisableFullWindowDrag), "disable full window drag");
            Assert.That(imported.DisableMenuAnimations, Is.EqualTo(original.DisableMenuAnimations), "disable menu anims");
            Assert.That(imported.DisableCursorShadow, Is.EqualTo(original.DisableCursorShadow), "disable cursor setting");
            Assert.That(imported.EnableFontSmoothing, Is.EqualTo(original.EnableFontSmoothing), "allow font smoothing");
            Assert.That(imported.EnableDesktopComposition, Is.EqualTo(original.EnableDesktopComposition), "allow desktop composition");
            Assert.That(imported.CacheBitmaps, Is.EqualTo(original.CacheBitmaps), "bitmapcachepersistenable");
            Assert.That(imported.RedirectClipboard, Is.EqualTo(original.RedirectClipboard), "redirectclipboard");
            Assert.That(imported.RedirectPrinters, Is.EqualTo(original.RedirectPrinters), "redirectprinters");
            Assert.That(imported.RedirectPorts, Is.EqualTo(original.RedirectPorts), "redirectcomports");
            Assert.That(imported.RedirectSmartCards, Is.EqualTo(original.RedirectSmartCards), "redirectsmartcards");
            Assert.That(imported.RedirectAudioCapture, Is.EqualTo(original.RedirectAudioCapture), "audiocapturemode");
            Assert.That(imported.RedirectWebAuthn, Is.EqualTo(original.RedirectWebAuthn), "redirectwebauthn");
            Assert.That(imported.EnableRdsAadAuth, Is.EqualTo(original.EnableRdsAadAuth), "enablerdsaadauth");
            Assert.That(imported.UseConsoleSession, Is.EqualTo(original.UseConsoleSession), "connect to console");
            Assert.That(imported.UseCredSsp, Is.EqualTo(original.UseCredSsp), "enablecredsspsupport");
            Assert.That(imported.RDPAuthenticationLevel, Is.EqualTo(original.RDPAuthenticationLevel), "authentication level");
            Assert.That(imported.RDPStartProgram, Is.EqualTo(original.RDPStartProgram), "alternate shell");
            Assert.That(imported.RDPStartProgramWorkDir, Is.EqualTo(original.RDPStartProgramWorkDir), "shell working directory");
        });
    }
}
using System;
using System.Reflection;
using System.Runtime.Versioning;
using mRemoteNG.Messages;
using mRemoteNG.Tools.Cmdline;
using NUnit.Framework;

namespace mRemoteNGTests.Tools.Cmdline;

[SupportedOSPlatform("windows")]
public class StartupArgumentsInterpreterTests
{
    private static readonly string[] ConnectArgs = ["mRemoteNG.exe", "--connect", "ConnA"];
    private static readonly string[] StartupArgs = ["mRemoteNG.exe", "--startup", "ConnA"];

    [SetUp]
    public void SetUp()
    {
        StartupArgumentsInterpreter.ResetConnectionArgs();
    }

    #region --connect

    [Test]
    public void ParseArguments_SetsConnectTo_WhenConnectArgumentIsProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(ConnectArgs);

        Assert.That(StartupArgumentsInterpreter.ConnectTo, Is.EqualTo("ConnA"));
        Assert.That(StartupArgumentsInterpreter.StartupConnectTo, Is.Null);
    }

    #endregion

    #region --startup

    [Test]
    public void ParseArguments_SetsStartupConnectTo_WhenStartupArgumentIsProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(StartupArgs);

        Assert.That(StartupArgumentsInterpreter.StartupConnectTo, Is.EqualTo("ConnA"));
        Assert.That(StartupArgumentsInterpreter.ConnectTo, Is.Null);
    }

    #endregion

    #region --quickconnect

    [Test]
    public void ParseArguments_SetsQuickConnectTo_WhenQuickConnectProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--quickconnect", "server1.example.com"]);

        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.EqualTo("server1.example.com"));
    }

    [Test]
    public void ParseArguments_SetsQuickConnectTo_WhenQcShorthandProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--qc", "myhost"]);

        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.EqualTo("myhost"));
    }

    [Test]
    public void ParseArguments_SetsProtocol_WhenProtocolProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--quickconnect", "server1", "--protocol", "SSH2"]);

        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.EqualTo("server1"));
        Assert.That(StartupArgumentsInterpreter.QuickConnectProtocol, Is.EqualTo("SSH2"));
    }

    [Test]
    public void ParseArguments_SetsProtocol_WhenPShorthandProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--qc", "server1", "--p", "VNC"]);

        Assert.That(StartupArgumentsInterpreter.QuickConnectProtocol, Is.EqualTo("VNC"));
    }

    [Test]
    public void ParseArguments_QuickConnectWithoutProtocol_LeavesProtocolNull()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--quickconnect", "server1"]);

        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.EqualTo("server1"));
        Assert.That(StartupArgumentsInterpreter.QuickConnectProtocol, Is.Null);
    }

    #endregion

    #region --exitafter

    [Test]
    public void ParseArguments_SetsExitAfterLastConnection_WhenExitAfterProvided()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--exitafter"]);

        Assert.That(StartupArgumentsInterpreter.ExitAfterLastConnection, Is.True);
    }

    [Test]
    public void ParseArguments_ExitAfterDefaultsFalse()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe"]);

        Assert.That(StartupArgumentsInterpreter.ExitAfterLastConnection, Is.False);
    }

    #endregion

    #region ResetConnectionArgs

    [Test]
    public void ResetConnectionArgs_ClearsAllProperties()
    {
        var sut = CreateSut();
        sut.ParseArguments(["mRemoteNG.exe", "--connect", "A", "--quickconnect", "B", "--protocol", "RDP", "--exitafter"]);

        StartupArgumentsInterpreter.ResetConnectionArgs();

        Assert.That(StartupArgumentsInterpreter.ConnectTo, Is.Null);
        Assert.That(StartupArgumentsInterpreter.StartupConnectTo, Is.Null);
        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.Null);
        Assert.That(StartupArgumentsInterpreter.QuickConnectProtocol, Is.Null);
        Assert.That(StartupArgumentsInterpreter.CustomConnectionFile, Is.Null);
        Assert.That(StartupArgumentsInterpreter.ExitAfterLastConnection, Is.False);
    }

    #endregion

    #region Combined arguments

    [Test]
    public void ParseArguments_MultipleArgs_SetsAll()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe", "--connect", "ConnA", "--exitafter"]);

        Assert.That(StartupArgumentsInterpreter.ConnectTo, Is.EqualTo("ConnA"));
        Assert.That(StartupArgumentsInterpreter.ExitAfterLastConnection, Is.True);
    }

    #endregion

    #region Constructor

    [Test]
    public void Constructor_NullMessageCollector_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new StartupArgumentsInterpreter(null!));
    }

    #endregion

    #region No arguments

    [Test]
    public void ParseArguments_NoArgs_LeavesAllNull()
    {
        var sut = CreateSut();

        sut.ParseArguments(["mRemoteNG.exe"]);

        Assert.That(StartupArgumentsInterpreter.ConnectTo, Is.Null);
        Assert.That(StartupArgumentsInterpreter.StartupConnectTo, Is.Null);
        Assert.That(StartupArgumentsInterpreter.QuickConnectTo, Is.Null);
    }

    #endregion

    #region /noreconnect

    [TestCase("/noreconnect")]
    [TestCase("/norc")]
    public void ParseArguments_NoReconnect_SkipsPreviousSessionsWithoutChangingTheSavedOption(string argument)
    {
        bool originalSavedOption = mRemoteNG.Properties.OptionsAdvancedPage.Default.NoReconnect;
        bool originalSwitch = mRemoteNG.App.Runtime.NoReconnectToPreviousSessions;
        try
        {
            mRemoteNG.Properties.OptionsAdvancedPage.Default.NoReconnect = false;
            mRemoteNG.App.Runtime.NoReconnectToPreviousSessions = false;

            CreateSut().ParseArguments(["mRemoteNG.exe", argument]);

            Assert.That(mRemoteNG.App.Runtime.NoReconnectToPreviousSessions, Is.True);
            // The saved option is the "Automatically try to reconnect when disconnected from
            // server" checkbox (upstream #3178).
            Assert.That(mRemoteNG.Properties.OptionsAdvancedPage.Default.NoReconnect, Is.False);
        }
        finally
        {
            mRemoteNG.Properties.OptionsAdvancedPage.Default.NoReconnect = originalSavedOption;
            mRemoteNG.App.Runtime.NoReconnectToPreviousSessions = originalSwitch;
        }
    }

    #endregion

    private static StartupArgumentsInterpreter CreateSut()
    {
        return new StartupArgumentsInterpreter(new MessageCollector());
    }
}

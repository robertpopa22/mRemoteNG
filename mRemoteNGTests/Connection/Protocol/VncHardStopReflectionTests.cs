using System;
using System.Reflection;
using NUnit.Framework;

namespace mRemoteNGTests.Connection.Protocol;

/// <summary>
/// ProtocolVNC.HardStopVncClientWithoutHandle (#170) stops the RFB worker thread by reflection
/// when the RemoteDesktop control has no window handle: RemoteDesktop's private "vnc" client,
/// VncClient's field-like ConnectionLost event, and RemoteDesktop's private "state" enum field.
/// These tests pin that private contract so a VncSharpCore package bump that renames any of them
/// fails the suite instead of silently turning the hard-stop into a no-op (and re-opening the
/// fatal "Error creating window handle" crash on the worker thread).
/// </summary>
public class VncHardStopReflectionTests
{
    [Test]
    public void RemoteDesktopExposesPrivateVncClientField()
    {
        FieldInfo? field = typeof(VncSharpCore.RemoteDesktop)
            .GetField("vnc", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "VncSharpCore.RemoteDesktop no longer has a private 'vnc' field");
        Assert.That(field!.FieldType, Is.EqualTo(typeof(VncSharpCore.VncClient)));
    }

    [Test]
    public void VncClientExposesConnectionLostBackingField()
    {
        FieldInfo? field = typeof(VncSharpCore.VncClient)
            .GetField(nameof(VncSharpCore.VncClient.ConnectionLost), BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null,
            "VncSharpCore.VncClient.ConnectionLost is no longer a field-like event — the hard-stop cannot silence it");
    }

    [Test]
    public void RemoteDesktopExposesPrivateStateFieldWithDisconnectedValue()
    {
        FieldInfo? field = typeof(VncSharpCore.RemoteDesktop)
            .GetField("state", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, "VncSharpCore.RemoteDesktop no longer has a private 'state' field");
        Assert.That(field!.FieldType.IsEnum, Is.True);
        Assert.That(Enum.GetNames(field.FieldType), Does.Contain("Disconnected"));
    }
}

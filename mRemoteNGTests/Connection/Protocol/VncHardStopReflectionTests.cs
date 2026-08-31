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

    [Test]
    public void HardStopIsANoOpWhenTheControlNeverConnected()
    {
        MethodInfo? hardStop = typeof(mRemoteNG.Connection.Protocol.VNC.ProtocolVNC)
            .GetMethod("HardStopVncClientWithoutHandle", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(hardStop, Is.Not.Null, "HardStopVncClientWithoutHandle not found");

        // A RemoteDesktop that never connected has a null private VncClient: the hard-stop must
        // return without touching anything (the #170 path only matters once a worker exists).
        using var vnc = new VncSharpCore.RemoteDesktop();
        Assert.DoesNotThrow(() => hardStop!.Invoke(null, [vnc]));
    }

    [Test]
    public void HardStopSilencesConnectionLostAndMarksDisconnected()
    {
        MethodInfo? hardStop = typeof(mRemoteNG.Connection.Protocol.VNC.ProtocolVNC)
            .GetMethod("HardStopVncClientWithoutHandle", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.That(hardStop, Is.Not.Null);

        using var vnc = new VncSharpCore.RemoteDesktop();

        // Simulate the #170 precondition without a network: give the control a live VncClient
        // whose ConnectionLost has a subscriber (as RemoteDesktop wires internally on connect).
        var client = new VncSharpCore.VncClient();
        client.ConnectionLost += (_, _) => { };
        typeof(VncSharpCore.RemoteDesktop)
            .GetField("vnc", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(vnc, client);

        Assert.DoesNotThrow(() => hardStop!.Invoke(null, [vnc]));

        FieldInfo lostField = typeof(VncSharpCore.VncClient)
            .GetField(nameof(VncSharpCore.VncClient.ConnectionLost), BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.That(lostField.GetValue(client), Is.Null,
            "ConnectionLost must be cleared so the worker cannot Invoke onto a dead control");

        FieldInfo stateField = typeof(VncSharpCore.RemoteDesktop)
            .GetField("state", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assert.That(stateField.GetValue(vnc)!.ToString(), Is.EqualTo("Disconnected"),
            "state must read Disconnected so RemoteDesktop.Dispose does not re-enter Disconnect");
    }
}

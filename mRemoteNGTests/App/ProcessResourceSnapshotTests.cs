using System.Linq;
using mRemoteNG.App;
using mRemoteNG.App.Diagnostics;
using NUnit.Framework;

namespace mRemoteNGTests.App;

/// <summary>
/// The retention log has to name GDI, USER, and handle types from the process itself.
/// </summary>
public class ProcessResourceSnapshotTests
{
    [Test]
    public void CaptureNamesHandleTypesWithoutPaths()
    {
        ResourceSample sample = ProcessResourceSnapshot.Capture(refreshExpensive: true);

        Assert.That(sample.Handles, Is.GreaterThan(0));
        Assert.That(sample.Gdi, Is.GreaterThanOrEqualTo(0));
        Assert.That(sample.User, Is.GreaterThanOrEqualTo(0));
        Assert.That(sample.PrivateMb, Is.GreaterThanOrEqualTo(0));
        Assert.That(sample.HandleTypes, Does.Not.Contain("\\"));
        Assert.That(sample.HandleTypes, Does.Not.Contain(" "));
        Assert.That(sample.ThreadModules, Does.Not.Contain("\\"));
        Assert.That(sample.HandleTypes, Does.Contain(":"));
        Assert.That(sample.HandleTypes.Split(',').Any(part => part.StartsWith("Event:") || part.StartsWith("File:") || part.StartsWith("Thread:")),
            Is.True, sample.HandleTypes);
    }

    [Test]
    public void MachineFactsIncludeTheOsBuild()
    {
        MachineFacts facts = ProcessResourceSnapshot.ReadMachine();

        Assert.That(facts.Build, Is.GreaterThan(10000));
        Assert.That(facts.Product, Does.Not.Contain("\\"));
        Assert.That(facts.Product, Does.Not.Contain(" "));
    }

    [TestCase(2, 12u, "user_logoff")]
    [TestCase(1, 2u, "api_logoff")]
    [TestCase(0, 4u, "logoff")]
    [TestCase(1, 0u, "local_disconnect")]
    [TestCase(2, 0u, "remote_user")]
    [TestCase(3, 0u, "remote_server")]
    [TestCase(0xB08, 0u, "normal")]
    public void DisconnectClassComesFromTheExtendedReasonFirst(int code, uint extended, string expected) =>
        Assert.That(RuntimeDiagnostics.ClassifyDisconnect(code, extended), Is.EqualTo(expected));
}

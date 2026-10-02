using System.Net;
using System.Reflection;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

public class PortScanStartGateTests
{
    private static readonly int[] Port80 = [80];

    [Test]
    public void RejectedAddressNamesTheParserReasonAndDoesNotCreateAScanner()
    {
        const string address = "not an address";
        IpRangeParser.TryParse(address, out IPAddress? _, out IPAddress? _, out string parserReason);

        bool created = PortScanStartGate.TryCreate(address, Port80, 1000, 1, out PortScanner? scanner, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.False);
            Assert.That(scanner, Is.Null);
            Assert.That(reason, Is.EqualTo(parserReason));
            Assert.That(reason, Is.Not.Empty);
        });
    }

    [Test]
    public void OversizedRangeNamesWhyAndDoesNotCreateAScanner()
    {
        bool created = PortScanStartGate.TryCreate(
            "10.0.0.0 - 10.1.0.0", Port80, 1000, 1, out PortScanner? scanner, out string reason);

        Assert.Multiple(() =>
        {
            Assert.That(created, Is.False);
            Assert.That(scanner, Is.Null);
            Assert.That(reason, Is.Not.Empty);
        });
    }

    [Test]
    public void AcceptedAddressCreatesAScannerThatHasNotBeenStarted()
    {
        bool created = PortScanStartGate.TryCreate(
            "192.0.2.1", Port80, 1000, 1, out PortScanner? scanner, out string reason);

        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(created, Is.True, reason);
                Assert.That(scanner, Is.Not.Null);
                Assert.That(reason, Is.Empty);
                Assert.That(CancellationOf(scanner), Is.Null,
                    "preparing a scan must not start it, so the window can still refuse to show Stop");
            });
        }
        finally
        {
            scanner?.Dispose();
        }
    }

    private static object? CancellationOf(PortScanner? scanner) =>
        typeof(PortScanner).GetField("_cancellation", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(scanner);
}

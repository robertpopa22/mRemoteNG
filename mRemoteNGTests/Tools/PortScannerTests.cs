using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools;

public class PortScannerTests
{
    private static readonly int[] Port80 = [80];

    private static List<IPAddress> GetScannedAddresses(PortScanner scanner)
    {
        FieldInfo field = typeof(PortScanner).GetField("_ipAddresses", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (List<IPAddress>)field.GetValue(scanner)!;
    }

    [Test]
    public void RangeStraddling128_ProducesCorrectOrderedAddresses()
    {
        // 127.255.255.250 -> 128.0.0.5 straddles 128.0.0.0. Treating IPv4 as a signed Int32 made
        // any address >= 128.0.0.0 negative, inverting the range so the host list was wrong/empty.
        var scanner = new PortScanner(
            IPAddress.Parse("127.255.255.250"),
            IPAddress.Parse("128.0.0.5"),
            Port80);
        List<IPAddress> addresses = GetScannedAddresses(scanner);

        Assert.That(addresses, Has.Count.EqualTo(12));
        Assert.That(addresses, Is.All.Not.Null);
        Assert.That(addresses[0].ToString(), Is.EqualTo("127.255.255.250"));
        Assert.That(addresses[^1].ToString(), Is.EqualTo("128.0.0.5"));
    }

    [Test]
    public void NormalLanRange_ProducesCorrectCount()
    {
        var scanner = new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.10"),
            Port80);
        List<IPAddress> addresses = GetScannedAddresses(scanner);

        Assert.That(addresses, Has.Count.EqualTo(10));
        Assert.That(addresses[0].ToString(), Is.EqualTo("192.168.1.1"));
        Assert.That(addresses[^1].ToString(), Is.EqualTo("192.168.1.10"));
    }

    [Test]
    public void RangeExceedingLimit_Throws()
    {
        // A range larger than the /16 cap must throw rather than attempt a multi-GB allocation.
        Assert.That(() => new PortScanner(
            IPAddress.Parse("10.0.0.0"),
            IPAddress.Parse("200.0.0.0"),
            Port80), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void Ipv6Range_ProducesCorrectOrderedAddresses()
    {
        var scanner = new PortScanner(
            IPAddress.Parse("2001:db8::1"),
            IPAddress.Parse("2001:db8::a"),
            Port80);
        List<IPAddress> addresses = GetScannedAddresses(scanner);

        Assert.That(addresses, Has.Count.EqualTo(10));
        Assert.That(addresses[0].ToString(), Is.EqualTo("2001:db8::1"));
        Assert.That(addresses[^1].ToString(), Is.EqualTo("2001:db8::a"));
    }

    [Test]
    public void Ipv6Range_HandlesGroupBoundaryCarry()
    {
        // Crossing a 16-bit group boundary (::ffff -> ::1:0001) exercises the multi-byte carry that
        // a 32-bit implementation could not represent.
        var scanner = new PortScanner(
            IPAddress.Parse("2001:db8::ffff"),
            IPAddress.Parse("2001:db8::1:0001"),
            Port80);
        List<IPAddress> addresses = GetScannedAddresses(scanner);

        Assert.That(addresses, Has.Count.EqualTo(3));
        Assert.That(addresses[0].ToString(), Is.EqualTo("2001:db8::ffff"));
        Assert.That(addresses[1].ToString(), Is.EqualTo("2001:db8::1:0"));
        Assert.That(addresses[^1].ToString(), Is.EqualTo("2001:db8::1:1"));
    }

    [Test]
    public void Ipv6RangeExceedingLimit_Throws()
    {
        // 2001:db8:: -> 2001:db9:: is 2^80 addresses — far past the cap.
        Assert.That(() => new PortScanner(
            IPAddress.Parse("2001:db8::"),
            IPAddress.Parse("2001:db9::"),
            Port80), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void MixedAddressFamilies_Throws()
    {
        Assert.That(() => new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("2001:db8::1"),
            Port80), Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void NullPorts_Throws()
    {
        Assert.That(() => new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            null!), Throws.InstanceOf<ArgumentNullException>());
    }

    [Test]
    public void EmptyPorts_Throws()
    {
        // An empty list would otherwise ping every host in the range and probe nothing.
        Assert.That(() => new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            []), Throws.InstanceOf<ArgumentException>());
    }

    [TestCase(0)]
    [TestCase(-1)]
    [TestCase(65536)]
    public void PortOutsideValidRange_Throws(int port)
    {
        // Without the up-front guard this only failed later, inside the per-port TcpClient connect.
        Assert.That(() => new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            new[] { 80, port }), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void LazyPortSequence_IsEnumeratedOnlyOnce()
    {
        int enumerations = 0;

        IEnumerable<int> CountingPorts()
        {
            enumerations++;
            yield return 80;
        }

        _ = new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            CountingPorts());

        Assert.That(enumerations, Is.EqualTo(1));
    }

    [Test]
    public void ValidPortBoundaries_AreAccepted()
    {
        Assert.DoesNotThrow(() => _ = new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            new[] { PortListParser.MinPort, PortListParser.MaxPort }));
    }

    private static List<int> GetScannedPorts(PortScanner scanner)
    {
        FieldInfo field = typeof(PortScanner).GetField("_ports", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (List<int>)field.GetValue(scanner)!;
    }

    [Test]
    public void PortRangeCtor_ZeroStartStillMeansASinglePort()
    {
        // (0, 3389) is this overload's "only one port was specified" convention. Validation must
        // run after that has been resolved, or the convention would start throwing.
        var scanner = new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            0, 3389);

        Assert.That(GetScannedPorts(scanner), Is.EqualTo(new[] { 3389 }));
    }

    [TestCase(0, 0)]
    [TestCase(-1, 80)]
    [TestCase(80, 65536)]
    public void PortRangeCtor_InvalidBounds_Throw(int port1, int port2)
    {
        Assert.That(() => new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            port1, port2), Throws.InstanceOf<ArgumentOutOfRangeException>());
    }

    [Test]
    public void PortRangeCtor_DefaultPortsOnly_IgnoresThePortArguments()
    {
        // port1/port2 are unused in this mode, so they must not be validated either.
        var scanner = new PortScanner(
            IPAddress.Parse("192.168.1.1"),
            IPAddress.Parse("192.168.1.1"),
            0, 0, checkDefaultPortsOnly: true);

        Assert.That(GetScannedPorts(scanner), Does.Contain(ScanHost.RdpPort));
    }
}

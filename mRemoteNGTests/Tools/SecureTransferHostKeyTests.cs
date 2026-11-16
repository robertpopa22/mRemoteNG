using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using mRemoteNG.Tools;
using NUnit.Framework;
using Renci.SshNet.Common;
using Renci.SshNet.Security;

namespace mRemoteNGTests.Tools;

[TestFixture]
public class SecureTransferHostKeyTests
{
    private DirectoryInfo _directory;

    [SetUp]
    public void SetUp() => _directory = Directory.CreateTempSubdirectory("mrng-transfer-key-");

    [TearDown]
    public void TearDown() => _directory.Delete(recursive: true);

    [Test]
    public void OnlyTheExplicitlyTrustedKeyIsAcceptedAndClearsThePreviousRejection()
    {
        using var transfer = CreateTransfer(22, SecureTransfer.SSHTransferProtocol.SFTP);
        using RsaKey firstKey = CreatePublicKey();
        using RsaKey secondKey = CreatePublicKey();
        var first = new HostKeyEventArgs(new KeyHostAlgorithm("rsa-sha2-512", firstKey));
        var second = new HostKeyEventArgs(new KeyHostAlgorithm("rsa-sha2-512", secondKey));

        transfer.OnHostKeyReceived(null, first);
        Assert.That(first.CanTrust, Is.False);
        Assert.That(transfer.RejectedHostKey.Status, Is.EqualTo(SshHostKeyStatus.Unknown));
        Assert.That(transfer.RejectedHostKey.KeyType, Is.EqualTo("ssh-rsa"), "The wire key type, not the signature algorithm, is stored.");
        transfer.HostKeys.Trust(transfer.RejectedHostKey);

        transfer.OnHostKeyReceived(null, second);
        Assert.That(second.CanTrust, Is.False);
        Assert.That(transfer.RejectedHostKey.Status, Is.EqualTo(SshHostKeyStatus.Changed));
        Assert.That(transfer.RejectedHostKey.TrustedFingerprintSha256, Is.EqualTo(first.FingerPrintSHA256));

        transfer.OnHostKeyReceived(null, first);
        Assert.That(first.CanTrust, Is.True);
        Assert.That(transfer.RejectedHostKey, Is.Null);
    }

    [Test]
    public void AnUnreadableTrustStoreCannotLeaveTheLibraryDefaultTrustEnabled()
    {
        using var transfer = CreateTransfer(22, SecureTransfer.SSHTransferProtocol.SFTP);
        using RsaKey key = CreatePublicKey();
        var received = new HostKeyEventArgs(new KeyHostAlgorithm("rsa-sha2-512", key));
        Assert.That(received.CanTrust, Is.True, "SSH.NET defaults to accepting a host key.");
        using var locked = new FileStream(transfer.HostKeys.FilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

        Assert.Throws<IOException>(() => transfer.OnHostKeyReceived(null, received));
        Assert.That(received.CanTrust, Is.False);
    }

    [TestCase(0)] // SCP
    [TestCase(1)] // SFTP
    public void ANewConnectionAttemptClearsAStaleHostKeyRejection(int protocolValue)
    {
        var protocol = (SecureTransfer.SSHTransferProtocol)protocolValue;
        // Reserve a loopback port without listening: no real SSH service or credentials are used.
        using var closedPort = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        closedPort.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        using var transfer = CreateTransfer(((IPEndPoint)closedPort.LocalEndPoint).Port, protocol);
        using RsaKey key = CreatePublicKey();
        transfer.OnHostKeyReceived(null, new HostKeyEventArgs(new KeyHostAlgorithm("rsa-sha2-512", key)));
        Assert.That(transfer.RejectedHostKey, Is.Not.Null);

        Assert.Throws<SocketException>(transfer.Connect);
        Assert.That(transfer.RejectedHostKey, Is.Null, "A transport failure must not report the previous server's key.");
        Assert.That(protocol == SecureTransfer.SSHTransferProtocol.SCP ? (object)transfer.ScpClt : transfer.SftpClt, Is.Not.Null);
    }

    private SecureTransfer CreateTransfer(int port, SecureTransfer.SSHTransferProtocol protocol) =>
        new("127.0.0.1", "test", string.Empty, port, protocol)
        {
            HostKeys = new SshHostKeyStore(Path.Combine(_directory.FullName, SshHostKeyStore.FileName))
        };

    private static RsaKey CreatePublicKey()
    {
        using RSA rsa = RSA.Create(2048);
        RSAParameters parameters = rsa.ExportParameters(false);
        return new RsaKey(new SshKeyData("ssh-rsa", [
            new BigInteger(parameters.Exponent, isUnsigned: true, isBigEndian: true),
            new BigInteger(parameters.Modulus, isUnsigned: true, isBigEndian: true)]));
    }
}

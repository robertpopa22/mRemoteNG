using System;
using System.IO;
using System.Linq;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    [TestFixture]
    public class SshHostKeyStoreTests
    {
        private string _dir = string.Empty;
        private string _file = string.Empty;

        private static readonly byte[] KeyA = Enumerable.Range(1, 51).Select(i => (byte)i).ToArray();
        private static readonly byte[] KeyB = Enumerable.Range(2, 51).Select(i => (byte)i).ToArray();

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "mrng-hostkeys-" + Guid.NewGuid().ToString("N"));
            _file = Path.Combine(_dir, SshHostKeyStore.FileName);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir))
                Directory.Delete(_dir, true);
        }

        [Test]
        public void HostWithoutAFileIsUnknown()
        {
            SshHostKeyOffer offer = new SshHostKeyStore(_file).Check("server", 22, "ssh-ed25519", KeyA);
            Assert.That(offer.Status, Is.EqualTo(SshHostKeyStatus.Unknown));
            Assert.That(offer.TrustedFingerprintSha256, Is.Null);
        }

        [Test]
        public void TrustedKeyIsTrustedOnTheNextCheck()
        {
            SshHostKeyStore store = new(_file);
            store.Trust(store.Check("Server", 22, "ssh-ed25519", KeyA));

            Assert.That(new SshHostKeyStore(_file).Check("server", 22, "ssh-ed25519", KeyA).Status,
                        Is.EqualTo(SshHostKeyStatus.Trusted));
        }

        [Test]
        public void DifferentKeyForAKnownHostIsChangedAndNamesBothFingerprints()
        {
            SshHostKeyStore store = new(_file);
            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyA));

            SshHostKeyOffer offer = store.Check("server", 22, "ssh-ed25519", KeyB);
            Assert.That(offer.Status, Is.EqualTo(SshHostKeyStatus.Changed));
            Assert.That(offer.TrustedFingerprintSha256, Is.EqualTo(SshHostKeyStore.Fingerprint(KeyA)));
            Assert.That(offer.FingerprintSha256, Is.EqualTo(SshHostKeyStore.Fingerprint(KeyB)));
        }

        [Test]
        public void SameKeyBytesUnderAnotherKeyTypeIsChanged()
        {
            SshHostKeyStore store = new(_file);
            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyA));
            Assert.That(store.Check("server", 22, "ssh-rsa", KeyA).Status, Is.EqualTo(SshHostKeyStatus.Changed));
        }

        [Test]
        public void PortIsPartOfTheHostIdentity()
        {
            SshHostKeyStore store = new(_file);
            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyA));

            Assert.That(store.Check("server", 2222, "ssh-ed25519", KeyA).Status, Is.EqualTo(SshHostKeyStatus.Unknown));
            Assert.That(File.ReadAllText(_file), Does.Contain("server ssh-ed25519 "));

            store.Trust(store.Check("server", 2222, "ssh-ed25519", KeyB));
            Assert.That(File.ReadAllText(_file), Does.Contain("[server]:2222 ssh-ed25519 "));
            Assert.That(store.Check("server", 22, "ssh-ed25519", KeyA).Status, Is.EqualTo(SshHostKeyStatus.Trusted));
        }

        [Test]
        public void TrustingAChangedKeyReplacesTheOldOne()
        {
            SshHostKeyStore store = new(_file);
            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyA));
            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyB));

            Assert.That(store.Check("server", 22, "ssh-ed25519", KeyB).Status, Is.EqualTo(SshHostKeyStatus.Trusted));
            Assert.That(store.Check("server", 22, "ssh-ed25519", KeyA).Status, Is.EqualTo(SshHostKeyStatus.Changed));
            Assert.That(File.ReadAllLines(_file).Count(l => l.StartsWith("server ", StringComparison.Ordinal)), Is.EqualTo(1));
        }

        [TestCase("|1|c2FsdA==|aGFzaA== ssh-ed25519 {0}")]
        [TestCase("@cert-authority server ssh-ed25519 {0}")]
        [TestCase("server,other ssh-ed25519 {0}")]
        [TestCase("# server ssh-ed25519 {0}")]
        [TestCase("server ssh-ed25519")]
        public void LinesThisStoreDoesNotUnderstandNeverGrantTrustAndSurviveATrust(string line)
        {
            Directory.CreateDirectory(_dir);
            string text = string.Format(System.Globalization.CultureInfo.InvariantCulture, line, Convert.ToBase64String(KeyA));
            File.WriteAllLines(_file, [text]);

            SshHostKeyStore store = new(_file);
            Assert.That(store.Check("server", 22, "ssh-ed25519", KeyA).Status, Is.Not.EqualTo(SshHostKeyStatus.Trusted));

            store.Trust(store.Check("server", 22, "ssh-ed25519", KeyB));
            Assert.That(File.ReadAllLines(_file), Does.Contain(text));
        }

        [TestCase("ssh-ed25519")]
        [TestCase("ssh-rsa")]
        [TestCase("ecdsa-sha2-nistp256")]
        public void KeyTypeIsReadFromTheBlobNotTheSignatureAlgorithm(string type)
        {
            byte[] name = System.Text.Encoding.ASCII.GetBytes(type);
            byte[] blob = [0, 0, 0, (byte)name.Length, .. name, 0, 0, 0, 3, 1, 2, 3];
            Assert.That(SshHostKeyStore.KeyTypeOf(blob), Is.EqualTo(type));
        }

        [Test]
        public void BlobWithoutALeadingTypeHasNoKeyType()
        {
            Assert.That(SshHostKeyStore.KeyTypeOf(KeyA), Is.Null);
            Assert.That(SshHostKeyStore.KeyTypeOf([0, 0, 0, 9, 1]), Is.Null);
        }

        [Test]
        public void FingerprintMatchesTheOpenSshFormat()
        {
            // OpenSSH prints SHA256 fingerprints as unpadded base64 of the key blob's hash.
            string fp = SshHostKeyStore.Fingerprint(KeyA);
            Assert.That(fp, Does.Not.EndWith("="));
            Assert.That(Convert.FromBase64String(fp + "="), Has.Length.EqualTo(32));
        }
    }
}

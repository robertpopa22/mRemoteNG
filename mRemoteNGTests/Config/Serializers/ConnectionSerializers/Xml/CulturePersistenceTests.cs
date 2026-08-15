using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Xml
{
    /// <summary>
    /// A connection file written on one machine has to be readable on another, and the two machines
    /// do not share a regional format. Anything numeric that goes through a culture-sensitive
    /// conversion round-trips fine on the machine that wrote it and breaks on the one that reads
    /// it — the failure only appears for users whose regional settings differ from the developer's,
    /// which is why it survives a full green suite.
    ///
    /// #162 was this defect class in the UI: the property grid resolved its strings by regional
    /// format instead of display language, so an English install showed an Italian grid. These
    /// tests cover the persistence half of the same hazard.
    ///
    /// Culture is set on the test thread only. The workstation's OS settings are never touched —
    /// changing those to run a test is how you break the machine you debug on.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class CulturePersistenceTests
    {
        private const string MasterPassword = "culture-oracle-master";
        private CultureInfo _originalCulture = CultureInfo.CurrentCulture;
        private CultureInfo _originalUiCulture = CultureInfo.CurrentUICulture;

        [SetUp]
        public void Setup()
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _originalUiCulture = Thread.CurrentThread.CurrentUICulture;
        }

        [TearDown]
        public void Teardown()
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            Thread.CurrentThread.CurrentUICulture = _originalUiCulture;
        }

        private static string Serialize(ConnectionInfo connection)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            XmlConnectionNodeSerializer28 nodeSerializer =
                new(crypto, MasterPassword.ConvertToSecureString(), new SaveFilter());
            XmlConnectionsSerializer serializer = new(crypto, nodeSerializer);

            RootNodeInfo root = new(RootNodeType.Connection) { PasswordString = MasterPassword };
            root.AddChild(connection);
            return serializer.Serialize(root);
        }

        private static ConnectionInfo Deserialize(string xml)
        {
            XmlConnectionsDeserializer deserializer =
                new("", () => MasterPassword.ConvertToSecureString());
            return deserializer.Deserialize(xml)
                               .RootNodes.OfType<ContainerInfo>()
                               .SelectMany(r => r.GetRecursiveChildList())
                               .First(n => n is not ContainerInfo);
        }

        private static ConnectionInfo BuildNumericConnection() => new()
        {
            Name = "culture probe",
            Hostname = "host.example.invalid",
            Port = 34567,
            VNCProxyPort = 5901,
            RDPMinutesToIdleTimeout = 45,
        };

        /// <summary>
        /// Cultures whose number formatting differs from the invariant one in ways that break naive
        /// conversions: ro-RO and de-DE use a comma for the decimal separator, and ro-RO is
        /// installed on the maintainer's workstation, which is where #162 was reported from.
        /// </summary>
        [TestCase("ro-RO", "en-US")]
        [TestCase("de-DE", "en-US")]
        [TestCase("en-US", "ro-RO")]
        [TestCase("fr-FR", "fr-FR")]
        public void NumericFieldsSurviveWhenFormatAndDisplayCulturesDiffer(string formatCulture,
                                                                          string displayCulture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(formatCulture);
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(displayCulture);

            ConnectionInfo original = BuildNumericConnection();
            ConnectionInfo loaded = Deserialize(Serialize(original));

            Assert.Multiple(() =>
            {
                Assert.That(loaded.Port, Is.EqualTo(original.Port), "Port did not survive");
                Assert.That(loaded.VNCProxyPort, Is.EqualTo(original.VNCProxyPort),
                            "VNCProxyPort did not survive");
                Assert.That(loaded.RDPMinutesToIdleTimeout, Is.EqualTo(original.RDPMinutesToIdleTimeout),
                            "RDPMinutesToIdleTimeout did not survive");
            });
        }

        [Test]
        public void AFileWrittenUnderOneCultureReadsCorrectlyUnderAnother()
        {
            // The real scenario: the file crosses machines. Writing and reading under the same
            // culture can hide a bug that only appears when the two differ.
            Thread.CurrentThread.CurrentCulture = new CultureInfo("ro-RO");
            string xml = Serialize(BuildNumericConnection());

            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            ConnectionInfo loaded = Deserialize(xml);

            Assert.Multiple(() =>
            {
                Assert.That(loaded.Port, Is.EqualTo(34567));
                Assert.That(loaded.VNCProxyPort, Is.EqualTo(5901));
                Assert.That(loaded.RDPMinutesToIdleTimeout, Is.EqualTo(45));
            });
        }

        [Test]
        public void TextFieldsSurviveACultureWithADifferentAlphabet()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");

            // Turkish is the classic trap: uppercasing "i" yields a dotted capital, so any code
            // that normalises an attribute name or an enum with the current culture silently stops
            // matching.
            ConnectionInfo original = new()
            {
                Name = "Istanbul ile bağlantı",
                Hostname = "istanbul.example.invalid",
                Username = "iismet",
            };

            ConnectionInfo loaded = Deserialize(Serialize(original));

            Assert.Multiple(() =>
            {
                Assert.That(loaded.Name, Is.EqualTo(original.Name));
                Assert.That(loaded.Hostname, Is.EqualTo(original.Hostname));
                Assert.That(loaded.Username, Is.EqualTo(original.Username));
            });
        }
    }
}

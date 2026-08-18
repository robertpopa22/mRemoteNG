using System;
using System.Collections.Generic;
using System.Linq;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Tree;
using mRemoteNGTests.Properties;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Xml
{
    /// <summary>
    /// Proves the current build can still decrypt connection files written by older versions.
    ///
    /// The repository already carried historical fixtures — confCons v2.5 and v2.6, in plain and
    /// full-encryption variants, with 5,000-iteration and custom-password cases — but every test
    /// against them asserted structure only: a root exists, it has three children, a folder is
    /// named Folder1. All of those pass while every stored credential comes back as an empty
    /// string, which is the failure that actually costs a user their data.
    ///
    /// These fixtures contain real ciphertext produced by an older build. That makes them the one
    /// thing a same-build round trip can never be: evidence that the key derivation, cipher and
    /// encoding still agree with what shipped years ago. Any change to the crypto path that cannot
    /// read them is a change that silently empties users' passwords on upgrade.
    ///
    /// The passwords below are the fixtures' own synthetic values, not secrets.
    /// </summary>
    [TestFixture]
    public class HistoricalConnectionFileDecryptionTests
    {
        // The fixtures' own synthetic values, read back from the files themselves rather than
        // assumed. Connection1.1 stores no password of its own and inherits Folder1's, so asserting
        // this exact string proves two things at once: the old ciphertext still decrypts, and
        // inheritance still resolves to the same answer it did when the file was written.
        private const string ExpectedInheritedPassword = "folder1";
        private const string ExpectedUsername = "userFolder1";

        private static IEnumerable<TestCaseData> HistoricalFiles()
        {
            yield return new TestCaseData(Resources.confCons_v2_5, "mR3m")
                .SetName("v2.5, default key");
            yield return new TestCaseData(Resources.confCons_v2_5_fullencryption, "mR3m")
                .SetName("v2.5, whole file encrypted");
            yield return new TestCaseData(Resources.confCons_v2_6, "mR3m")
                .SetName("v2.6, default key");
            yield return new TestCaseData(Resources.confCons_v2_6_fullencryption, "mR3m")
                .SetName("v2.6, whole file encrypted");
            yield return new TestCaseData(Resources.confCons_v2_6_5k_iterations, "mR3m")
                .SetName("v2.6, 5000 KDF iterations");
            yield return new TestCaseData(Resources.confCons_v2_6_passwordis_Password, "Password")
                .SetName("v2.6, custom master password");
        }

        private static ConnectionTreeModel Load(string confCons, string masterPassword)
        {
            XmlConnectionsDeserializer deserializer =
                new("", () => masterPassword.ConvertToSecureString());
            return deserializer.Deserialize(confCons);
        }

        private static IEnumerable<ConnectionInfo> AllConnections(ConnectionTreeModel model) =>
            model.RootNodes
                 .OfType<ContainerInfo>()
                 .SelectMany(root => root.GetRecursiveChildList())
                 .Where(node => node is not ContainerInfo);

        [TestCaseSource(nameof(HistoricalFiles))]
        public void StoredPasswordsAreStillDecryptable(string confCons, string masterPassword)
        {
            ConnectionTreeModel model = Load(confCons, masterPassword);
            List<ConnectionInfo> connections = AllConnections(model).ToList();

            Assert.That(connections, Is.Not.Empty, "the file produced no connections at all");

            // The precise failure this guards against: the structure survives, the secrets do not.
            //
            // The fixtures deliberately include connections that inherit from a parent carrying no
            // password, so an empty value there is the correct answer and not a decryption failure.
            // They are named for what they test, which is how they are excluded — narrowing the
            // assertion to the connections that actually store a secret.
            List<string> emptied = connections
                .Where(c => !c.Name.Contains("Inherit", StringComparison.OrdinalIgnoreCase))
                .Where(c => string.IsNullOrEmpty(c.Password))
                .Select(c => c.Name)
                .ToList();

            Assert.That(emptied, Is.Empty,
                        "connections loaded but their stored passwords came back empty — the "
                        + "current build can no longer decrypt what this version wrote: "
                        + string.Join(", ", emptied));
        }

        [TestCaseSource(nameof(HistoricalFiles))]
        public void ADecryptedPasswordHasItsOriginalValue(string confCons, string masterPassword)
        {
            ConnectionTreeModel model = Load(confCons, masterPassword);
            ConnectionInfo? connection = AllConnections(model)
                .FirstOrDefault(c => string.Equals(c.Name, "Connection1.1", StringComparison.Ordinal));

            Assert.That(connection, Is.Not.Null, "fixture no longer contains Connection1.1");

            // Not merely "something came back" — the right plaintext. A cipher or KDF change can
            // produce non-empty garbage just as easily as an empty string.
            Assert.That(connection!.Password, Is.EqualTo(ExpectedInheritedPassword));
        }

        [TestCaseSource(nameof(HistoricalFiles))]
        public void InheritedUsernamesSurviveTheLoad(string confCons, string masterPassword)
        {
            ConnectionTreeModel model = Load(confCons, masterPassword);
            ConnectionInfo? connection = AllConnections(model)
                .FirstOrDefault(c => string.Equals(c.Name, "Connection1.1", StringComparison.Ordinal));

            Assert.That(connection, Is.Not.Null);
            Assert.Multiple(() =>
            {
                // Also inherited from Folder1: a credential is not just its password.
                Assert.That(connection!.Username, Is.EqualTo(ExpectedUsername), "username was lost");
                Assert.That(connection.Protocol.ToString(), Is.Not.Empty, "protocol was lost");
            });
        }

        [Test]
        public void EncryptedCredentialsStoredOnFoldersAreAppliedToTheFolder()
        {
            ConnectionTreeModel model = Load(Resources.confCons_v2_6, "mR3m");
            ContainerInfo? folder2 = model.RootNodes
                .OfType<ContainerInfo>()
                .SelectMany(root => root.GetRecursiveChildList())
                .OfType<ContainerInfo>()
                .FirstOrDefault(folder => string.Equals(folder.Name, "Folder2", StringComparison.Ordinal));

            Assert.That(folder2, Is.Not.Null);
            Assert.That(folder2!.Password, Is.EqualTo("folder2"),
                "deferred decryption must update the real folder node, not a temporary connection copied before decryption");
        }
    }
}

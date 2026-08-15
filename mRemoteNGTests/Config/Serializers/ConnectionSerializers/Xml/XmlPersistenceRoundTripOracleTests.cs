using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// The oracle for the defect class this project ships most often: a save that quietly writes a
    /// subset of what it was given.
    ///
    /// Existing serializer tests assert named fields, so a property that stops being written is
    /// invisible until a user notices it missing — which is how #148's root name, and the seven
    /// misaligned CSV inheritance columns, both shipped. This walks every persisted property by
    /// reflection instead of naming them, so a field added tomorrow is covered without anyone
    /// remembering to add an assertion, and a field that stops round-tripping fails immediately.
    ///
    /// Two properties are asserted:
    ///   * everything survives a write/read cycle;
    ///   * changing exactly one field changes that field AND NOTHING ELSE. The second is the one
    ///     that catches a save writing a subset, because a subset write usually looks correct for
    ///     the field you were looking at.
    /// </summary>
    [TestFixture]
    public class XmlPersistenceRoundTripOracleTests
    {
        private const string MasterPassword = "round-trip-oracle-master";

        /// <summary>
        /// Properties that are legitimately not expected to survive a file round trip: runtime
        /// state, computed views, and the inheritance object compared separately.
        /// </summary>
        private static readonly HashSet<string> NotPersisted = new(StringComparer.Ordinal)
        {
            "OpenConnections", "Parent", "Inheritance", "IsContainer", "IsDefault",
            "PositionID", "ConstantID", "TreeNode", "IsQuickConnect", "Favorite",
            "IsExpanded", "Children", "PleaseConnect", "Domain",
        };

        private static ConnectionInfo BuildSaturatedConnection() => new()
        {
            Name = "Oracle probe — ünïcode; \"quoted\"",
            Hostname = "host.example.invalid",
            Username = "user'name",
            Password = "p@ss;word\"1",
            Description = "Multi\nline description with ; and \" characters",
            Port = 34567,
            Panel = "General",
            Protocol = mRemoteNG.Connection.Protocol.ProtocolType.SSH2,
            SSHOptions = "-o StrictHostKeyChecking=accept-new",
            OpeningCommand = "uname -a",
            MacAddress = "00:11:22:33:44:55",
            UserField = "field with spaces",
            LoadBalanceInfo = "tsv://MS Terminal Services Plugin.1.Farm",
            RDGatewayHostname = "gw.example.invalid",
            RDGatewayUsername = "gwuser",
            RDGatewayPassword = "gwp@ss",
            RDGatewayDomain = "gwdomain",
            PreExtApp = "pre",
            PostExtApp = "post",
            Notes = "notes; with separators and ünïcode",
            VNCProxyIP = "10.0.0.1",
            VNCProxyUsername = "vncuser",
            VNCProxyPassword = "vncp@ss",
            VNCProxyPort = 5901,
            EC2InstanceId = "i-0123456789abcdef0",
            EC2Region = "eu-central-1",
        };

        private static string Serialize(ConnectionInfo connection)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            XmlConnectionNodeSerializer28 nodeSerializer =
                new(crypto, MasterPassword.ConvertToSecureString(), new SaveFilter());
            XmlConnectionsSerializer serializer = new(crypto, nodeSerializer);

            RootNodeInfo root = new(RootNodeType.Connection)
            {
                PasswordString = MasterPassword
            };
            root.AddChild(connection);
            return serializer.Serialize(root);
        }

        private static ConnectionInfo Deserialize(string xml)
        {
            XmlConnectionsDeserializer deserializer =
                new("", () => MasterPassword.ConvertToSecureString());
            ConnectionTreeModel model = deserializer.Deserialize(xml);

            ConnectionInfo? loaded = model.RootNodes
                                          .OfType<ContainerInfo>()
                                          .SelectMany(r => r.GetRecursiveChildList())
                                          .FirstOrDefault(n => n is not ContainerInfo);

            Assert.That(loaded, Is.Not.Null, "the round trip produced no connection");
            return loaded!;
        }

        /// <summary>Readable, writable, scalar properties — the ones a file is expected to carry.</summary>
        private static IEnumerable<PropertyInfo> PersistedProperties() =>
            typeof(ConnectionInfo)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite)
                .Where(p => !NotPersisted.Contains(p.Name))
                .Where(p => p.PropertyType.IsPrimitive
                            || p.PropertyType.IsEnum
                            || p.PropertyType == typeof(string));

        private static Dictionary<string, object?> Snapshot(ConnectionInfo connection) =>
            PersistedProperties().ToDictionary(p => p.Name, p => p.GetValue(connection));

        [Test]
        public void EveryPersistedPropertySurvivesAWriteAndReadCycle()
        {
            ConnectionInfo original = BuildSaturatedConnection();
            Dictionary<string, object?> before = Snapshot(original);
            Dictionary<string, object?> after = Snapshot(Deserialize(Serialize(original)));

            List<string> lost = before
                .Where(kv => !Equals(kv.Value, after.TryGetValue(kv.Key, out object? v) ? v : null))
                .Select(kv => $"{kv.Key}: wrote [{kv.Value}] read back [{(after.TryGetValue(kv.Key, out object? v2) ? v2 : "<missing>")}]")
                .ToList();

            Assert.That(lost, Is.Empty,
                        "properties did not survive the round trip:" + Environment.NewLine
                        + string.Join(Environment.NewLine, lost));
        }

        [Test]
        public void ChangingOneFieldMovesThatFieldAndNothingElse()
        {
            // The oracle that catches a save writing a subset: a partial write usually looks
            // correct for the field under inspection and quietly resets its neighbours.
            ConnectionInfo baseline = BuildSaturatedConnection();
            Dictionary<string, object?> beforeAll = Snapshot(Deserialize(Serialize(baseline)));

            ConnectionInfo mutated = BuildSaturatedConnection();
            mutated.Description = "changed description";
            Dictionary<string, object?> afterAll = Snapshot(Deserialize(Serialize(mutated)));

            List<string> unexpectedlyMoved = beforeAll
                .Where(kv => !string.Equals(kv.Key, nameof(ConnectionInfo.Description), StringComparison.Ordinal))
                .Where(kv => !Equals(kv.Value, afterAll.TryGetValue(kv.Key, out object? v) ? v : null))
                .Select(kv => $"{kv.Key}: [{kv.Value}] -> [{(afterAll.TryGetValue(kv.Key, out object? v2) ? v2 : "<missing>")}]")
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(afterAll[nameof(ConnectionInfo.Description)], Is.EqualTo("changed description"),
                            "the field that was changed did not persist");
                Assert.That(unexpectedlyMoved, Is.Empty,
                            "changing one field disturbed others:" + Environment.NewLine
                            + string.Join(Environment.NewLine, unexpectedlyMoved));
            });
        }

        [Test]
        public void TheOracleActuallyCoversAMeaningfulNumberOfProperties()
        {
            // A reflection-driven oracle is only as good as what it reflects over. If an exclusion
            // list or a type filter ever swallows most of the surface, the tests above would go
            // quietly green while checking almost nothing.
            Assert.That(PersistedProperties().Count(), Is.GreaterThan(60));
        }
    }
}

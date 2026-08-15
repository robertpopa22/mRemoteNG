using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.IntegrationTests
{
    /// <summary>
    /// The round-trip oracle of <see cref="mRemoteNGTests.Config.Serializers.ConnectionSerializers.Xml.XmlPersistenceRoundTripOracleTests"/>,
    /// applied to the SQL backend.
    ///
    /// SQL is where this project's silent data loss actually happened: #148 lost a rename, and the
    /// schema-migration defects lost or blocked whole databases. The XML oracle cannot speak for
    /// SQL — the two backends have separate serializers, separate column sets, and separate
    /// inheritance handling, so a property can round-trip perfectly through a file and vanish
    /// through a database.
    ///
    /// Two layers, because they fail differently:
    ///   * the in-memory pair (DataTableSerializer/DataTableDeserializer) catches a property that is
    ///     never written to, or never read from, its column. No database needed, so it runs
    ///     everywhere;
    ///   * the live pass sends the same data through a real SQL Server, which is the only thing that
    ///     can catch a column whose declared type or width silently truncates what fits in memory.
    ///
    /// The live pass uses a throwaway database, always dropped, and is skipped where no local
    /// server exists.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class SqlPersistenceRoundTripLiveTests
    {
        private const string MasterPassword = "sql-round-trip-oracle-master";
        private const string ServerInstance = @".\SQLEXPRESS";
        private string _database = "";

        private static string MasterConnectionString =>
            $"Server={ServerInstance};Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5";

        /// <summary>
        /// Properties that legitimately do not survive a persistence round trip. Each is either
        /// runtime state, a computed view over other properties, or handled outside the connection
        /// row itself. Kept deliberately short: every name added here is a property the oracle stops
        /// watching, which is how the gaps this test exists to find got in.
        /// </summary>
        private static readonly HashSet<string> NotPersisted = new(StringComparer.Ordinal)
        {
            // Runtime state and tree wiring, not data.
            "OpenConnections", "Parent", "Inheritance", "IsContainer", "IsDefault", "TreeNode",
            "IsQuickConnect", "Children", "PleaseConnect", "IsExpanded", "Favorite", "IsRoot",
            "HostReachabilityStatus",
            // Identity and ordering, assigned by the store rather than carried by the connection.
            "PositionID", "ConstantID",
            // Computed views over Hostname/Port.
            "ConnectionAddressPrimary",
        };

        private static ConnectionInfo BuildSaturatedConnection() => new()
        {
            Name = "SQL oracle probe — ünïcode; \"quoted\"",
            Hostname = "host.example.invalid",
            Username = "user'name",
            Password = "p@ss;word\"1",
            Description = "Multi line description with ; and \" characters",
            Port = 34567,
            Panel = "General",
            Protocol = mRemoteNG.Connection.Protocol.ProtocolType.SSH2,
            SSHOptions = "-o StrictHostKeyChecking=accept-new",
            OpeningCommand = "uname -a",
            MacAddress = "00:11:22:33:44:55",
            UserField = "field with spaces",
            RDGatewayHostname = "gw.example.invalid",
            RDGatewayUsername = "gwuser",
            RDGatewayPassword = "gwp@ss",
            Notes = "notes; with separators and ünïcode",
            VNCProxyIP = "10.0.0.1",
            VNCProxyPort = 5901,
        };

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

        private static ConnectionTreeModel ModelContaining(ConnectionInfo connection)
        {
            RootNodeInfo root = new(RootNodeType.Connection) { PasswordString = MasterPassword };
            root.AddChild(connection);

            ConnectionTreeModel model = new();
            model.AddRootNode(root);
            return model;
        }

        private static DataTable Serialize(ConnectionInfo connection)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            DataTableSerializer serializer =
                new(new SaveFilter(), crypto, MasterPassword.ConvertToSecureString());
            return serializer.Serialize(ModelContaining(connection));
        }

        private static ConnectionInfo Deserialize(DataTable table)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            DataTableDeserializer deserializer = new(crypto, MasterPassword.ConvertToSecureString());

            ConnectionInfo? loaded = deserializer.Deserialize(table)
                                                 .RootNodes.OfType<ContainerInfo>()
                                                 .SelectMany(r => r.GetRecursiveChildList())
                                                 .FirstOrDefault(n => n is not ContainerInfo);

            Assert.That(loaded, Is.Not.Null, "the SQL round trip produced no connection");
            return loaded!;
        }

        private static List<string> Differences(Dictionary<string, object?> before,
                                                Dictionary<string, object?> after) =>
            before
                .Where(kv => !Equals(kv.Value, after.TryGetValue(kv.Key, out object? v) ? v : null))
                .Select(kv => $"{kv.Key}: wrote [{kv.Value}] read back "
                              + $"[{(after.TryGetValue(kv.Key, out object? v2) ? v2 : "<missing>")}]")
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

        [Test]
        public void EveryPersistedPropertySurvivesTheSqlSerializerPair()
        {
            ConnectionInfo original = BuildSaturatedConnection();
            Dictionary<string, object?> before = Snapshot(original);
            Dictionary<string, object?> after = Snapshot(Deserialize(Serialize(original)));

            Assert.That(Differences(before, after), Is.Empty,
                        "properties did not survive the SQL serializer round trip:"
                        + Environment.NewLine
                        + string.Join(Environment.NewLine, Differences(before, after)));
        }

        [Test]
        public void ChangingOneFieldMovesThatFieldAndNothingElse()
        {
            // A save that writes a subset usually looks correct for the field under inspection and
            // quietly resets its neighbours — the shape of #148.
            Dictionary<string, object?> baseline =
                Snapshot(Deserialize(Serialize(BuildSaturatedConnection())));

            ConnectionInfo mutated = BuildSaturatedConnection();
            mutated.Description = "changed description";
            Dictionary<string, object?> after = Snapshot(Deserialize(Serialize(mutated)));

            List<string> moved = baseline
                .Where(kv => !string.Equals(kv.Key, nameof(ConnectionInfo.Description),
                                            StringComparison.Ordinal))
                .Where(kv => !Equals(kv.Value, after.TryGetValue(kv.Key, out object? v) ? v : null))
                .Select(kv => $"{kv.Key}: [{kv.Value}] -> "
                              + $"[{(after.TryGetValue(kv.Key, out object? v2) ? v2 : "<missing>")}]")
                .ToList();

            Assert.Multiple(() =>
            {
                Assert.That(after[nameof(ConnectionInfo.Description)],
                            Is.EqualTo("changed description"),
                            "the field that was changed did not persist");
                Assert.That(moved, Is.Empty,
                            "changing one field disturbed others:" + Environment.NewLine
                            + string.Join(Environment.NewLine, moved));
            });
        }

        [Test]
        public void TheOracleCoversAMeaningfulNumberOfProperties()
        {
            // A reflection-driven oracle is only as good as what it reflects over. If the exclusion
            // list or the type filter ever swallows most of the surface, the tests above go quietly
            // green while checking almost nothing.
            Assert.That(PersistedProperties().Count(), Is.GreaterThan(60));
        }

        // --- live database ---------------------------------------------------------------------

        private static bool SqlServerIsAvailable()
        {
            try
            {
                using SqlConnection probe = new(MasterConnectionString);
                probe.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ExecuteOnMaster(string sql)
        {
            using SqlConnection connection = new(MasterConnectionString);
            connection.Open();
            using SqlCommand command = new(sql, connection);
            command.ExecuteNonQuery();
        }

        [SetUp]
        public void CreateThrowawayDatabase()
        {
            if (!SqlServerIsAvailable())
                return;

            _database = "mRemoteNGRt_" + Guid.NewGuid().ToString("N")[..12];
            ExecuteOnMaster($"CREATE DATABASE [{_database}]");
        }

        [TearDown]
        public void DropThrowawayDatabase()
        {
            if (string.IsNullOrEmpty(_database))
                return;

            try
            {
                ExecuteOnMaster($"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                ExecuteOnMaster($"DROP DATABASE [{_database}]");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Could not drop {_database}: {ex.Message}");
            }
            finally
            {
                _database = "";
            }
        }

        /// <summary>
        /// The same comparison, but the data actually goes to disk through a real server. This is
        /// the only layer that can catch a column narrower than the value the application allows —
        /// a truncation looks like a successful save and shows up as corrupted data later.
        /// </summary>
        [Test]
        [Category("RequiresSqlServer")]
        public void EveryPersistedPropertySurvivesARealDatabase()
        {
            if (string.IsNullOrEmpty(_database))
                Assert.Ignore($"No local SQL Server at {ServerInstance}.");

            using IDatabaseConnector connector =
                new MSSqlDatabaseConnector(ServerInstance, _database, "", "");
            connector.Connect();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);

            SqlDataProvider provider = new(connector);
            ConnectionInfo original = BuildSaturatedConnection();
            Dictionary<string, object?> before = Snapshot(original);

            DataTable outgoing = Serialize(original);
            DataTable target = provider.Load();
            foreach (DataRow row in outgoing.Rows)
            {
                DataRow copy = target.NewRow();
                foreach (DataColumn column in outgoing.Columns)
                {
                    if (!target.Columns.Contains(column.ColumnName))
                        continue;

                    // The two tables do not agree on every CLR type — the in-memory one carries
                    // LastChange as SqlDateTime where the live one reports DateTime, and unset
                    // values arrive as empty strings that convert to neither. Coerce what converts
                    // and substitute a type default for the rest, so that NOT NULL columns are
                    // satisfied. This is scaffolding for getting the row into the database; the
                    // assertion is on what comes back out.
                    DataColumn targetColumn = target.Columns[column.ColumnName]!;
                    object value = row[column.ColumnName];

                    if (targetColumn.DataType.IsInstanceOfType(value))
                    {
                        copy[targetColumn] = value;
                        continue;
                    }

                    try
                    {
                        copy[targetColumn] = Convert.ChangeType(
                            value, targetColumn.DataType,
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch (Exception ex) when (ex is InvalidCastException or FormatException)
                    {
                        if (!targetColumn.AllowDBNull)
                        {
                            // default(DateTime) is year 1, below the SqlDateTime minimum of 1753,
                            // so the server rejects the whole INSERT.
                            copy[targetColumn] = targetColumn.DataType switch
                            {
                                Type t when t == typeof(string) => "",
                                Type t when t == typeof(DateTime) => new DateTime(2000, 1, 1),
                                Type t => Activator.CreateInstance(t)!,
                            };
                        }
                    }
                }

                target.Rows.Add(copy);
            }

            provider.Save(target);

            Dictionary<string, object?> after = Snapshot(Deserialize(provider.Load()));

            Assert.That(Differences(before, after), Is.Empty,
                        "properties did not survive a real SQL Server round trip:"
                        + Environment.NewLine
                        + string.Join(Environment.NewLine, Differences(before, after)));
        }
    }
}

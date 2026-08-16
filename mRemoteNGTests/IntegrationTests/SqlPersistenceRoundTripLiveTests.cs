using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
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
            // Deliberately longer than nvarchar(4000). A probe that fits the default width cannot
            // tell a correctly widened column from a truncating one, so the claim that the live
            // pass catches truncation would be untrue for the value being tested.
            Notes = "notes; with separators and ünïcode" + Environment.NewLine
                    + new string('n', 5000),
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
            PersistedProperties().ToDictionary(p => p.Name, p => p.GetValue(connection),
                                              StringComparer.Ordinal);

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

        /// <summary>
        /// The inheritance flags travel on a separate object, so reflecting over ConnectionInfo's
        /// own properties never sees them. Without this, the deserializer's InheritNotes read could
        /// be deleted outright and every other test here would still pass.
        /// </summary>
        [Test]
        public void TheInheritanceFlagForNotesSurvivesTheRoundTrip()
        {
            ConnectionInfo original = BuildSaturatedConnection();
            original.Inheritance.Notes = true;

            ConnectionInfo loaded = Deserialize(Serialize(original));

            Assert.That(loaded.Inheritance.Notes, Is.True,
                        "InheritNotes did not survive — a connection set to inherit its notes from "
                        + "the parent folder would silently stop doing so");
        }

        [Test]
        public void TheInheritanceFlagForNotesIsNotAlwaysTrue()
        {
            // Guards the test above: a deserializer that hard-coded true would pass it.
            ConnectionInfo original = BuildSaturatedConnection();
            original.Inheritance.Notes = false;

            Assert.That(Deserialize(Serialize(original)).Inheritance.Notes, Is.False);
        }

        // A test asserting that a NULL Notes column does not mark unmodified rows as changed was
        // written here and then deleted, because it could not pass for the right reason: the same
        // model serialized twice, with nothing changed and no stored passwords, already reports
        // every row as Modified. Change detection in DataTableSerializer never returns "unchanged"
        // in practice, so no assertion about avoiding a rewrite is reachable. Recorded in
        // .project-roadmap/VERIFICATION_PLAN.md rather than left as a test that passes by accident.

        /// <summary>
        /// Serializes an already-built model against an existing table, which is what a save does:
        /// load the current rows, then write the tree over them.
        ///
        /// Takes the model rather than the connection on purpose. Calling ModelContaining twice for
        /// the same connection re-parents it under a second root, and Serialize swallows the
        /// resulting exception and hands back the untouched source table — which looks exactly like
        /// "the row was correctly left alone" and made an earlier version of these tests pass
        /// without executing any of the code they claim to cover.
        /// </summary>
        private static DataTable SerializeAgainst(DataTable source, ConnectionTreeModel model)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            DataTableSerializer serializer =
                new(new SaveFilter(), crypto, MasterPassword.ConvertToSecureString());
            serializer.SetSourceDataTable(source);
            return serializer.Serialize(model);
        }

        private static DataTable SerializeModel(ConnectionTreeModel model)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            DataTableSerializer serializer =
                new(new SaveFilter(), crypto, MasterPassword.ConvertToSecureString());
            return serializer.Serialize(model);
        }

        /// <summary>
        /// The serialized table holds the root node as well as the connection, and the root's Notes
        /// is always empty. Select by identity — picking "the first non-root row" silently read the
        /// root instead and made one of these tests pass for the wrong reason.
        /// </summary>
        private static object NotesCellOf(DataTable table, ConnectionInfo connection)
        {
            DataRow? row = table.Rows.Cast<DataRow>()
                                .FirstOrDefault(r => string.Equals((string)r["ConstantID"],
                                                                   connection.ConstantID,
                                                                   StringComparison.Ordinal));
            Assert.That(row, Is.Not.Null, "the connection's row is not in the serialized table");
            return row!["Notes"];
        }

        [Test]
        public void AnActualNotesEditIsStillDetected()
        {
            // Guards the test above: treating NULL as "" must not blind change detection to a real
            // edit, which would resurrect the bug this whole change exists to fix.
            ConnectionInfo connection = BuildSaturatedConnection();
            connection.Notes = "";
            ConnectionTreeModel model = ModelContaining(connection);

            DataTable source = SerializeModel(model);
            foreach (DataRow row in source.Rows)
                row["Notes"] = DBNull.Value;
            source.AcceptChanges();

            connection.Notes = "a note the user just typed";

            Assert.That(NotesCellOf(SerializeAgainst(source, model), connection),
                        Is.EqualTo("a note the user just typed"),
                        "a new note was not written, so it would never reach the database");
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
        /// A fresh database takes the CREATE TABLE path, so nothing that only runs on an existing
        /// database is covered by the round-trip test below. This winds a current database back to
        /// what a 3.5 install looks like — the Notes columns gone, tblRoot claiming 3.5, a row
        /// present so a bad ALTER would be rejected — and then loads it exactly as the application
        /// would.
        ///
        /// Type matters as much as existence here: an upgrade that added Notes as nvarchar(4000)
        /// would satisfy a column-exists check and still truncate the first long note the user
        /// writes.
        /// </summary>
        [Test]
        [Category("RequiresSqlServer")]
        public void ADatabaseAtVersion35GainsAWideNotesColumn()
        {
            if (string.IsNullOrEmpty(_database))
                Assert.Ignore($"No local SQL Server at {ServerInstance}.");

            using MSSqlDatabaseConnector connector = new(ServerInstance, _database, "", "");
            connector.Connect();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);

            // Wind back to a 3.5-shaped database that already holds a connection. The row is
            // written through the real save path rather than a hand-written INSERT: tblCons has
            // dozens of NOT NULL columns and a partial INSERT tests nothing except my ability to
            // list them. A populated table is the point — an ALTER that adds a NOT NULL column
            // without a default is rejected only when rows exist, which is the #165 failure.
            InsertOneConnection(new SqlDataProvider(connector), BuildSaturatedConnection());

            // InheritNotes carries an auto-named DEFAULT constraint, and SQL Server refuses to drop
            // a column another object depends on — the same obstacle #113 hit on ALTER COLUMN.
            DropDefaultConstraint(connector, "InheritNotes");
            Execute(connector, "ALTER TABLE tblCons DROP COLUMN Notes");
            Execute(connector, "ALTER TABLE tblCons DROP COLUMN InheritNotes");
            Execute(connector, "UPDATE tblRoot SET ConfVersion = '3.5'");

            // The application's own load path: metadata retrieval forward-ports the schema, then
            // the version verifier runs the upgrade chain.
            SqlConnectionListMetaData? metaData = retriever.GetDatabaseMetaData(connector);
            Assert.That(metaData, Is.Not.Null, "the wound-back database could not be read at all");
            new SqlDatabaseVersionVerifier(connector).VerifyDatabaseVersion(metaData!.ConfVersion);

            using DbCommand query = connector.DbCommand(
                "SELECT c.name, t.name AS type_name, c.max_length FROM sys.columns c "
                + "JOIN sys.types t ON c.user_type_id = t.user_type_id "
                + "WHERE c.object_id = OBJECT_ID('tblCons') AND c.name IN ('Notes','InheritNotes')");

            Dictionary<string, (string Type, int MaxLength)> columns = new(StringComparer.OrdinalIgnoreCase);
            using (DbDataReader reader = query.ExecuteReader())
            {
                while (reader.Read())
                {
                    columns[reader.GetString(0)] =
                        (reader.GetString(1), Convert.ToInt32(reader["max_length"]));
                }
            }

            Assert.Multiple(() =>
            {
                Assert.That(columns.ContainsKey("Notes"), Is.True,
                            "the 3.5 -> 3.6 upgrade did not add the Notes column");
                Assert.That(columns.ContainsKey("InheritNotes"), Is.True,
                            "the 3.5 -> 3.6 upgrade did not add the InheritNotes column");

                if (columns.TryGetValue("Notes", out (string Type, int MaxLength) notes))
                {
                    // sys.columns reports max_length -1 for the MAX types.
                    Assert.That(notes.MaxLength, Is.EqualTo(-1),
                                $"Notes was added as {notes.Type}({notes.MaxLength}) instead of a "
                                + "MAX type, so long notes will be truncated on save");
                }
            });
        }

        private static void Execute(IDatabaseConnector connector, string sql)
        {
            using DbCommand command = connector.DbCommand(sql);
            command.ExecuteNonQuery();
        }

        private static void DropDefaultConstraint(IDatabaseConnector connector, string column)
        {
            Execute(connector, $@"
DECLARE @name sysname;
SELECT @name = d.name FROM sys.default_constraints d
JOIN sys.columns c ON c.object_id = d.parent_object_id AND c.column_id = d.parent_column_id
WHERE d.parent_object_id = OBJECT_ID('tblCons') AND c.name = '{column}';
IF @name IS NOT NULL EXEC('ALTER TABLE tblCons DROP CONSTRAINT [' + @name + ']');");
        }

        /// <summary>
        /// Writes one connection into the live tblCons through the application's own data provider.
        /// </summary>
        private static void InsertOneConnection(SqlDataProvider provider, ConnectionInfo connection)
        {
            DataTable outgoing = Serialize(connection);
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
                    // and substitute a type default for the rest, so NOT NULL columns are
                    // satisfied. This is scaffolding for getting a row into the database; the
                    // assertions are on what comes back out.
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

            using MSSqlDatabaseConnector connector = new(ServerInstance, _database, "", "");
            connector.Connect();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);

            SqlDataProvider provider = new(connector);
            ConnectionInfo original = BuildSaturatedConnection();
            Dictionary<string, object?> before = Snapshot(original);

            InsertOneConnection(provider, original);

            Dictionary<string, object?> after = Snapshot(Deserialize(provider.Load()));

            Assert.That(after[nameof(ConnectionInfo.Notes)], Is.EqualTo(original.Notes),
                        "a Notes value longer than nvarchar(4000) was truncated by the server — "
                        + "the save reported success and the data is already gone");

            Assert.That(Differences(before, after), Is.Empty,
                        "properties did not survive a real SQL Server round trip:"
                        + Environment.NewLine
                        + string.Join(Environment.NewLine, Differences(before, after)));
        }
    }
}

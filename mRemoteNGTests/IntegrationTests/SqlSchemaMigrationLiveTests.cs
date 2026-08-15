using System;
using System.Collections.Generic;
using System.IO;
using System.Data.Common;
using System.Data.Odbc;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using NUnit.Framework;

namespace mRemoteNGTests.IntegrationTests
{
    /// <summary>
    /// Replays the schema upgrade chain against a REAL SQL Server, through the real connectors.
    ///
    /// This gap is why the project keeps shipping migration bugs. The existing "integration" test
    /// for the SQL loader mocks the database entirely, so every ALTER TABLE this application issues
    /// had never been executed by a test before a user ran it: #113 (invalid T-SQL, PK widening,
    /// enum values dropped), #146/#147 (MySQL datetime and row-size limits), #148 (a stale version
    /// constant re-running the whole chain on every load) and #165 (the ODBC path excluded from the
    /// column forward-port, then ALTER TABLE adding NOT NULL columns with no DEFAULT to a non-empty
    /// table) were all found by reporters, not here.
    ///
    /// Runs against .\SQLEXPRESS with integrated auth, over BOTH connectors — including ODBC
    /// Driver 17, which is what the #165 reporter used. Skips cleanly when no local SQL Server is
    /// present, so it costs nothing on a machine or CI agent without one, and each case works in
    /// its own throwaway database that is always dropped.
    /// </summary>
    [TestFixture]
    [Category("RequiresSqlServer")]
    [NonParallelizable]
    public class SqlSchemaMigrationLiveTests
    {
        private const string ServerInstance = @".\SQLEXPRESS";
        private const string OdbcDriver = "ODBC Driver 17 for SQL Server";
        private string _database = "";

        private static string MasterConnectionString =>
            $"Server={ServerInstance};Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5";

        [OneTimeSetUp]
        public void EnsureSqlServerIsAvailable()
        {
            try
            {
                using SqlConnection probe = new(MasterConnectionString);
                probe.Open();
            }
            catch (Exception ex)
            {
                Assert.Ignore($"No local SQL Server at {ServerInstance}: {ex.Message}");
            }
        }

        [SetUp]
        public void CreateThrowawayDatabase()
        {
            _database = "mRemoteNGTest_" + Guid.NewGuid().ToString("N")[..12];
            ExecuteOnMaster($"CREATE DATABASE [{_database}]");
        }

        [TearDown]
        public void DropThrowawayDatabase()
        {
            if (string.IsNullOrEmpty(_database))
                return;

            try
            {
                // Single-user first: a connector that failed mid-test may still hold a session.
                ExecuteOnMaster($"ALTER DATABASE [{_database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE");
                ExecuteOnMaster($"DROP DATABASE [{_database}]");
            }
            catch (Exception ex)
            {
                TestContext.Out.WriteLine($"Could not drop {_database}: {ex.Message}");
            }
        }

        private static void ExecuteOnMaster(string sql)
        {
            using SqlConnection connection = new(MasterConnectionString);
            connection.Open();
            using SqlCommand command = new(sql, connection);
            command.ExecuteNonQuery();
        }

        private IDatabaseConnector OpenConnector(bool useOdbc)
        {
            IDatabaseConnector connector = useOdbc
                ? new OdbcDatabaseConnector($"Driver={{{OdbcDriver}}};Server={ServerInstance};Trusted_Connection=yes;TrustServerCertificate=yes",
                                            _database, "", "")
                // Empty credentials select integrated auth in the connector itself.
                : new MSSqlDatabaseConnector(ServerInstance, _database, "", "");
            connector.Connect();
            return connector;
        }

        /// <summary>
        /// Creates the schema and the tblRoot row. A freshly created database has the table but no
        /// row -- the row only appears on the first save -- so tests that read ConfVersion have to
        /// go through a metadata write first, exactly as the application does.
        /// </summary>
        private static void InitialiseSchemaAndMetadata(IDatabaseConnector connector)
        {
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(
                new mRemoteNG.Tree.Root.RootNodeInfo(mRemoteNG.Tree.Root.RootNodeType.Connection),
                connector);
        }

        private static string ReadConfVersion(IDatabaseConnector connector)
        {
            using DbCommand command = connector.DbCommand("SELECT TOP 1 ConfVersion FROM tblRoot");
            return Convert.ToString(command.ExecuteScalar()) ?? "";
        }

        /// <summary>
        /// Puts the database back to what a legacy install looks like: the columns that arrived
        /// after schema 2.6 are gone, tblCons has a row (so an ALTER adding a NOT NULL column with
        /// no DEFAULT will be rejected, exactly as on the reporter's database), and tblRoot claims
        /// version 2.6.
        /// </summary>
        private static void RegressSchemaToLegacyVersion(IDatabaseConnector connector)
        {
            foreach (string column in new[]
                     {
                         "RedirectClipboard", "InheritRedirectClipboard", "VmId", "UseVmId",
                         "UseEnhancedMode", "InheritVmId", "InheritUseVmId",
                         "SSHTunnelConnectionName", "InheritSSHTunnelConnectionName",
                         "SSHOptions", "InheritSSHOptions", "InheritUseEnhancedMode"
                     })
            {
                using DbCommand drop = connector.DbCommand(
                    $"IF COL_LENGTH('tblCons','{column}') IS NOT NULL ALTER TABLE tblCons DROP COLUMN [{column}]");
                drop.ExecuteNonQuery();
            }

            SeedOneConnectionRow(connector);

            using DbCommand version = connector.DbCommand("UPDATE tblRoot SET ConfVersion='2.6'");
            version.ExecuteNonQuery();
        }


        /// <summary>
        /// Inserts one row into tblCons. The table has many NOT NULL columns without defaults, so
        /// the statement is generated from the live schema rather than hard-coded — otherwise this
        /// helper would need editing every time a column is added, which is exactly the maintenance
        /// trap that lets a migration test rot.
        ///
        /// The row matters: an ALTER that adds a NOT NULL column with no DEFAULT is only rejected
        /// when the table already has data, which is the reporter's situation and not a fresh one.
        /// </summary>
        private static void SeedOneConnectionRow(IDatabaseConnector connector)
        {
            List<string> columns = [];
            List<string> values = [];

            using (DbCommand schema = connector.DbCommand(
                       "SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT "
                       + "FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblCons' "
                       + "AND COLUMNPROPERTY(OBJECT_ID('tblCons'), COLUMN_NAME, 'IsIdentity') = 0 "
                       + "AND DATA_TYPE <> 'timestamp'"))
            using (DbDataReader reader = schema.ExecuteReader())
            {
                while (reader.Read())
                {
                    string column = reader.GetString(0);
                    string type = reader.GetString(1).ToLowerInvariant();
                    bool nullable = string.Equals(reader.GetString(2), "YES", StringComparison.OrdinalIgnoreCase);
                    bool hasDefault = !reader.IsDBNull(3);

                    bool isKey = string.Equals(column, "ConstantID", StringComparison.OrdinalIgnoreCase);
                    if (!isKey && (nullable || hasDefault))
                        continue;

                    columns.Add($"[{column}]");
                    values.Add(isKey
                                   ? "'legacy-row-0001'"
                                   : type switch
                                   {
                                       "bit" => "0",
                                       "int" or "bigint" or "smallint" or "tinyint" => "0",
                                       "datetime" or "datetime2" or "date" => "GETDATE()",
                                       _ => "''"
                                   });
                }
            }

            using DbCommand seed = connector.DbCommand(
                $"IF NOT EXISTS (SELECT 1 FROM tblCons) INSERT INTO tblCons ({string.Join(", ", columns)}) "
                + $"VALUES ({string.Join(", ", values)})");
            seed.ExecuteNonQuery();
        }

        [TestCase(false, TestName = "Microsoft.Data.SqlClient")]
        [TestCase(true, TestName = "ODBC Driver 17 (the #165 reporter's driver)")]
        public void ALegacySchemaUpgradesCleanlyToCurrent(bool useOdbc)
        {
            using IDatabaseConnector connector = OpenConnector(useOdbc);

            // A fresh database first, then wound back to look like a 1.76-era install.
            InitialiseSchemaAndMetadata(connector);
            RegressSchemaToLegacyVersion(connector);

            Assert.That(ReadConfVersion(connector), Is.EqualTo("2.6"), "precondition: legacy version");

            // The load path runs the forward-port, then the versioned upgrade chain.
            new SqlDatabaseMetaDataRetriever().GetDatabaseMetaData(connector);
            SqlDatabaseVersionVerifier verifier = new(connector);
            bool upgraded = verifier.VerifyDatabaseVersion(new Version(2, 6));

            Assert.Multiple(() =>
            {
                Assert.That(upgraded, Is.True, "the upgrade chain reported failure");
                Assert.That(ReadConfVersion(connector),
                            Is.EqualTo(SqlDatabaseVersionVerifier.SupportedSchemaVersion.ToString()),
                            "tblRoot must record the schema version actually reached");
            });
        }

        [TestCase(false, TestName = "Microsoft.Data.SqlClient")]
        [TestCase(true, TestName = "ODBC Driver 17")]
        public void TheColumnsAddedByTheUpgradeExistAfterwards(bool useOdbc)
        {
            using IDatabaseConnector connector = OpenConnector(useOdbc);

            InitialiseSchemaAndMetadata(connector);
            RegressSchemaToLegacyVersion(connector);

            new SqlDatabaseMetaDataRetriever().GetDatabaseMetaData(connector);
            new SqlDatabaseVersionVerifier(connector).VerifyDatabaseVersion(new Version(2, 6));

            // RedirectClipboard is the column #165 died on. If the forward-port skipped this
            // connector, it is still missing here.
            foreach (string column in new[] { "RedirectClipboard", "VmId", "SSHOptions", "UseEnhancedMode" })
            {
                using DbCommand check = connector.DbCommand(
                    $"SELECT COL_LENGTH('tblCons','{column}')");
                Assert.That(check.ExecuteScalar(), Is.Not.EqualTo(DBNull.Value),
                            $"column {column} was never added — the schema upgrade did not complete");
            }
        }

        /// <summary>
        /// The same upgrade, but starting from a schema this build did not generate:
        /// testdata/sql/schema-2023-03.sql is the CREATE TABLE block extracted verbatim from the
        /// 2023 upstream commit that first added schema initialisation.
        ///
        /// This is the case the other tests here cannot cover. Winding back a schema the current
        /// code just created reproduces our own column set, our own ordering and our own
        /// constraint names; a user's database has none of those. Every migration defect this
        /// project shipped was found by a reporter running a genuinely older database, which is
        /// exactly what this replays.
        /// </summary>
        [TestCase(false, TestName = "Microsoft.Data.SqlClient")]
        [TestCase(true, TestName = "ODBC Driver 17")]
        public void AHistoricalSchemaFromTheRepositoryUpgradesCleanly(bool useOdbc)
        {
            string fixturePath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                                              "testdata", "sql", "schema-2023-03.sql");
            if (!File.Exists(fixturePath))
                Assert.Ignore($"Historical schema fixture not found at {fixturePath}");

            using IDatabaseConnector connector = OpenConnector(useOdbc);

            // Apply the historical schema as-is, then give it a row and a legacy version so the
            // upgrade has something it can actually be rejected by.
            string fixtureSql = File.ReadAllText(fixturePath);
            using (DbCommand apply = connector.DbCommand(fixtureSql))
                apply.ExecuteNonQuery();

            using (DbCommand seedRoot = connector.DbCommand(
                       "IF NOT EXISTS (SELECT 1 FROM tblRoot) "
                       + "INSERT INTO tblRoot (Name, Export, Protected, ConfVersion) "
                       + "VALUES ('Historical', 0, '', '2.6')"))
                seedRoot.ExecuteNonQuery();

            SeedOneConnectionRow(connector);

            // Now the real load path: forward-port, then the versioned chain.
            new SqlDatabaseMetaDataRetriever().GetDatabaseMetaData(connector);
            bool upgraded = new SqlDatabaseVersionVerifier(connector)
                .VerifyDatabaseVersion(new Version(2, 6));

            Assert.Multiple(() =>
            {
                Assert.That(upgraded, Is.True, "a genuinely historical schema failed to upgrade");
                Assert.That(ReadConfVersion(connector),
                            Is.EqualTo(SqlDatabaseVersionVerifier.SupportedSchemaVersion.ToString()));
            });
        }

        [Test]
        public void ReloadingAnUpToDateDatabaseDoesNotRerunTheUpgradeChain()
        {
            // #148: a stale version constant was stamped into tblRoot on every save, so every load
            // re-ran the whole 2.6 -> 3.5 chain against an already-current schema.
            using IDatabaseConnector connector = OpenConnector(useOdbc: false);

            InitialiseSchemaAndMetadata(connector);

            string afterCreate = ReadConfVersion(connector);
            Assert.That(afterCreate,
                        Is.EqualTo(SqlDatabaseVersionVerifier.SupportedSchemaVersion.ToString()),
                        "a freshly created database must record the current schema version");

            // Write the metadata the way a save does, then confirm the recorded version did not
            // regress to some other constant.
            new SqlDatabaseMetaDataRetriever().WriteDatabaseMetaData(
                new mRemoteNG.Tree.Root.RootNodeInfo(mRemoteNG.Tree.Root.RootNodeType.Connection),
                connector);

            Assert.That(ReadConfVersion(connector), Is.EqualTo(afterCreate),
                        "saving must not change the recorded schema version");
        }
    }
}

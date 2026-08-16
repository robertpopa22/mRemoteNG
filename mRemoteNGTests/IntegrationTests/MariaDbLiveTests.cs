using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Tree.Root;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace mRemoteNGTests.IntegrationTests
{
    /// <summary>
    /// The MariaDB half of the live database coverage.
    ///
    /// #146 (datetime conversion), #147 (row size exceeded on schema upgrade) and #148 (save fails
    /// under safe-update mode) were all fixed and shipped without a MariaDB existing anywhere in
    /// the project — verified only by "the build passes and the unit suite is green". They are the
    /// weakest claims this fork has made, because every one of them is a defect that only a real
    /// MariaDB server can exhibit: the row-size ceiling, the datetime marshalling and the
    /// safe-update refusal are all server-side behaviours.
    ///
    /// This mirrors <see cref="SqlSchemaMigrationLiveTests"/> exactly: a throwaway database per
    /// test, dropped in teardown, and <c>Assert.Ignore</c> when no server is reachable so the suite
    /// stays green on machines without the lab.
    ///
    /// IMPORTANT for #148: the server must have <c>sql_safe_updates=1</c>. It is OFF in a stock
    /// MariaDB install, and with it off the issue cannot reproduce at all — the test would pass
    /// while proving nothing. <see cref="SafeUpdatesIsEnabledOnTheServer"/> asserts the
    /// precondition rather than letting a silent false negative through.
    /// </summary>
    [TestFixture]
    [Category("RequiresMariaDb")]
    [NonParallelizable]
    public class MariaDbLiveTests
    {
        private const string Host = "192.168.221.10";
        private const string User = "mrng";
        private const string Password = "mRNG-lab!2026";

        private string _database = "";

        private static string ServerConnectionString =>
            $"Server={Host};Port=3306;Uid={User};Pwd={Password};Connection Timeout=5";

        private static bool ServerIsAvailable()
        {
            try
            {
                using MySqlConnection probe = new(ServerConnectionString);
                probe.Open();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void ExecuteOnServer(string sql)
        {
            using MySqlConnection connection = new(ServerConnectionString);
            connection.Open();
            using MySqlCommand command = new(sql, connection);
            command.ExecuteNonQuery();
        }

        [SetUp]
        public void CreateThrowawayDatabase()
        {
            if (!ServerIsAvailable())
                return;

            _database = "mrng_test_" + Guid.NewGuid().ToString("N")[..12];
            ExecuteOnServer($"CREATE DATABASE `{_database}`");
        }

        [TearDown]
        public void DropThrowawayDatabase()
        {
            if (string.IsNullOrEmpty(_database))
                return;

            try
            {
                ExecuteOnServer($"DROP DATABASE IF EXISTS `{_database}`");
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

        private void SkipIfNoServer()
        {
            if (string.IsNullOrEmpty(_database))
                Assert.Ignore($"No MariaDB reachable at {Host}:3306.");
        }

        private MySqlDatabaseConnector OpenConnector()
        {
            MySqlDatabaseConnector connector = new(Host, _database, User, Password);
            connector.Connect();
            return connector;
        }

        /// <summary>
        /// Guards the precondition for #148. Without safe-update mode the save path never hits the
        /// refusal it was fixed to survive, so a green run here would mean nothing.
        /// </summary>
        [Test]
        public void SafeUpdatesIsEnabledOnTheServer()
        {
            SkipIfNoServer();

            using MySqlConnection connection = new(ServerConnectionString);
            connection.Open();
            using MySqlCommand command = new("SELECT @@sql_safe_updates", connection);
            object? value = command.ExecuteScalar();

            Assert.That(Convert.ToInt32(value), Is.EqualTo(1),
                        "sql_safe_updates is OFF on the lab server, so #148 cannot reproduce and "
                        + "the MariaDB save tests would pass without proving anything. Set it in "
                        + "/etc/mysql/mariadb.conf.d/60-lab-safe-updates.cnf and restart mariadb.");
        }

        /// <summary>
        /// The first-run bootstrap: mRemoteNG creates its own schema when pointed at an empty
        /// database. #147 was a failure inside exactly this path — the generated table exceeded
        /// MariaDB's 65535-byte row limit, which no amount of unit testing could reveal because the
        /// limit is enforced by the server.
        /// </summary>
        [Test]
        public void AnEmptyDatabaseGetsAWorkingSchema()
        {
            SkipIfNoServer();

            using MySqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);

            List<string> tables = [];
            using (DbCommand command = connector.DbCommand("SHOW TABLES"))
            using (DbDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                    tables.Add(reader.GetString(0));
            }

            Assert.Multiple(() =>
            {
                Assert.That(tables, Does.Contain("tblCons"),
                            "the connections table was not created on a MariaDB first run (#147)");
                Assert.That(tables, Does.Contain("tblRoot"));
            });
        }

        /// <summary>
        /// #147 specifically: the row-size ceiling. MariaDB refuses a table whose inline row exceeds
        /// 65535 bytes, and the fix moved wide string columns to TEXT so they store off-page. This
        /// asserts the created table actually holds a row, which is what the ceiling prevents.
        /// </summary>
        [Test]
        public void TheGeneratedSchemaAcceptsARow()
        {
            SkipIfNoServer();

            using MySqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);

            using DbCommand count = connector.DbCommand("SELECT COUNT(*) FROM tblRoot");
            object? rows = count.ExecuteScalar();

            Assert.That(Convert.ToInt32(rows), Is.GreaterThan(0),
                        "tblRoot took no row — a row-size or column-type rejection (#147)");
        }

        /// <summary>
        /// #146: the datetime column. The fix stopped handing MySqlDateTime.ToString() (US-culture)
        /// to the server and passes a native DateTime instead. A wrong format is rejected by the
        /// server with error 1292, so a successful write is the assertion.
        /// </summary>
        [Test]
        public void TheMetadataWriteSurvivesADatetimeColumn()
        {
            SkipIfNoServer();

            using MySqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);

            // Two writes: the second overwrites the first, which is where the datetime round trip
            // and the safe-update delete (#148) both happen.
            Assert.DoesNotThrow(() =>
            {
                retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);
                retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);
            }, "a metadata write failed against MariaDB (#146 datetime / #148 safe-update mode)");
        }

        /// <summary>
        /// #148 end to end: the metadata write deletes all rows from tblRoot before inserting. Under
        /// safe-update mode an unqualified DELETE is refused with error 1175, and the fix issues a
        /// limited delete instead. Repeating the write proves the session survives rather than
        /// leaving a poisoned pooled connection behind.
        /// </summary>
        [Test]
        public void RepeatedSavesSucceedUnderSafeUpdateMode()
        {
            SkipIfNoServer();

            using MySqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);

            for (int i = 0; i < 5; i++)
            {
                Assert.DoesNotThrow(
                    () => retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector),
                    $"save #{i + 1} failed under safe-update mode (#148)");
            }

            using DbCommand count = connector.DbCommand("SELECT COUNT(*) FROM tblRoot");
            Assert.That(Convert.ToInt32(count.ExecuteScalar()), Is.EqualTo(1),
                        "repeated saves left more than one root row — the delete-before-insert did "
                        + "not take effect (#148)");
        }
    }
}

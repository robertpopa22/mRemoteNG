using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.IntegrationTests
{
    /// <summary>
    /// #148: renaming the connection tree's root did not survive a restart. Three defects stacked,
    /// and each one alone was enough to lose the name:
    ///
    ///   * the rename raised no PropertyChanged, so no save was ever queued;
    ///   * the save that eventually ran could be discarded by the multiuser reload window;
    ///   * and even when the name reached tblRoot, the loader read it and threw it away, while the
    ///     root was constructed carrying the base class's "New Folder" default.
    ///
    /// Every one of those is invisible to a unit test that stops at "the object has the new name".
    /// The property that matters is end to end: rename, save, and get the same name back from a
    /// real database through the real loader. That is what this asserts.
    ///
    /// Uses a throwaway database on local SQL Express, always dropped, and skips cleanly where no
    /// SQL Server is present.
    /// </summary>
    [TestFixture]
    [Category("RequiresSqlServer")]
    [NonParallelizable]
    public class SqlRootNameRoundTripLiveTests
    {
        private const string ServerInstance = @".\SQLEXPRESS";
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
            _database = "mRemoteNGRoot_" + Guid.NewGuid().ToString("N")[..12];
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
        }

        private static void ExecuteOnMaster(string sql)
        {
            using SqlConnection connection = new(MasterConnectionString);
            connection.Open();
            using SqlCommand command = new(sql, connection);
            command.ExecuteNonQuery();
        }

        private MSSqlDatabaseConnector OpenConnector()
        {
            MSSqlDatabaseConnector connector = new(ServerInstance, _database, "", "");
            connector.Connect();
            return connector;
        }

        /// <summary>Reads the name straight out of the table, bypassing the loader.</summary>
        private static string ReadRootNameFromDatabase(MSSqlDatabaseConnector connector)
        {
            using System.Data.Common.DbCommand command =
                connector.DbCommand("SELECT TOP 1 Name FROM tblRoot");
            return Convert.ToString(command.ExecuteScalar()) ?? "";
        }

        /// <summary>The real loader, with only the local-properties side stubbed out.</summary>
        private static SqlConnectionsLoader BuildLoader(IDatabaseConnector connector)
        {
            IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>> localProperties =
                Substitute.For<IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>>>();
            localProperties.Deserialize(Arg.Any<string>()).Returns([]);

            IDataProvider<string> localPropertiesProvider = Substitute.For<IDataProvider<string>>();
            localPropertiesProvider.Load().Returns("");

            return new SqlConnectionsLoader(
                localProperties,
                localPropertiesProvider,
                connector,
                new SqlDataProvider(connector),
                new SqlDatabaseMetaDataRetriever(),
                new SqlDatabaseVersionVerifier(connector),
                new LegacyRijndaelCryptographyProvider(),
                _ => new Optional<SecureString>());
        }

        [Test]
        public void ARenamedRootSurvivesASaveAndAReload()
        {
            const string newName = "Production estate";

            using MSSqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);

            // Rename, then persist exactly the way a save does.
            RootNodeInfo root = new(RootNodeType.Connection) { Name = newName };
            retriever.WriteDatabaseMetaData(root, connector);

            Assert.That(ReadRootNameFromDatabase(connector), Is.EqualTo(newName),
                        "the rename never reached tblRoot");

            // Now come back the way a restart does: a fresh load through the real loader.
            ConnectionTreeModel reloaded = BuildLoader(connector).Load();
            RootNodeInfo? reloadedRoot = reloaded.RootNodes.OfType<RootNodeInfo>().FirstOrDefault();

            Assert.That(reloadedRoot, Is.Not.Null, "the reload produced no root node");
            Assert.That(reloadedRoot!.Name, Is.EqualTo(newName),
                        "the name is in the database but the loader did not apply it — this is the "
                        + "#148 symptom: the root comes back as a constructor default");
        }

        [Test]
        public void ARootNameWithUnicodeAndSeparatorsSurvivesTheRoundTrip()
        {
            // Names are user text: they carry accents, quotes and separators. A name that only
            // round-trips while it is plain ASCII is not round-tripping, it is coincidence.
            const string awkwardName = "Producție \"live\"; O'Brien — 100% ✓";

            using MSSqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);

            retriever.WriteDatabaseMetaData(
                new RootNodeInfo(RootNodeType.Connection) { Name = awkwardName }, connector);

            ConnectionTreeModel reloaded = BuildLoader(connector).Load();
            RootNodeInfo? reloadedRoot = reloaded.RootNodes.OfType<RootNodeInfo>().FirstOrDefault();

            Assert.That(reloadedRoot?.Name, Is.EqualTo(awkwardName));
        }

        [Test]
        public void SavingTwiceDoesNotAccumulateRootRows()
        {
            // tblRoot holds exactly one row. The metadata write deletes before inserting; if that
            // delete ever regresses, the loader silently picks whichever row comes back first and
            // the name appears to flip between saves at random.
            using MSSqlDatabaseConnector connector = OpenConnector();
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);

            retriever.WriteDatabaseMetaData(
                new RootNodeInfo(RootNodeType.Connection) { Name = "First" }, connector);
            retriever.WriteDatabaseMetaData(
                new RootNodeInfo(RootNodeType.Connection) { Name = "Second" }, connector);

            using System.Data.Common.DbCommand count = connector.DbCommand("SELECT COUNT(*) FROM tblRoot");
            Assert.Multiple(() =>
            {
                Assert.That(Convert.ToInt32(count.ExecuteScalar()), Is.EqualTo(1),
                            "tblRoot accumulated rows across saves");
                Assert.That(ReadRootNameFromDatabase(connector), Is.EqualTo("Second"),
                            "the later save did not win");
            });
        }
    }
}

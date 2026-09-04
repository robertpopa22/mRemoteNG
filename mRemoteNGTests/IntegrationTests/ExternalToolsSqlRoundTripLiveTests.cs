using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Settings;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.IntegrationTests
{
    /// <summary>
    /// External Tools had a write-only database: ExternalAppsSaver chose SQL when UseSQLServer was
    /// set, but loading always read extApps.xml, so nothing ever came back out of tblExternalTools
    /// and the next shutdown wrote the stale file over it (#179).
    ///
    /// Nothing short of a real round trip catches that class of defect. A test of the writer alone
    /// passes with no reader at all — which is exactly the state the code was in.
    ///
    /// Uses a throwaway database, always dropped, skipped where no local server exists.
    /// </summary>
    [TestFixture]
    [NonParallelizable]
    public class ExternalToolsSqlRoundTripLiveTests
    {
        private const string ServerInstance = @".\SQLEXPRESS";
        private string _database = "";

        private static string MasterConnectionString =>
            $"Server={ServerInstance};Integrated Security=true;TrustServerCertificate=true;Connect Timeout=5";

        private static List<ExternalTool> BuildSaturatedTools() =>
        [
            new ExternalTool
            {
                DisplayName = "Round trip probe — ünïcode; \"quoted\"",
                FileName = @"C:\Program Files\probe\probe.exe",
                IconPath = @"C:\Program Files\probe\probe.ico",
                Arguments = "--host %HOSTNAME% --note \"a;b\"",
                WorkingDir = @"C:\Program Files\probe",
                WaitForExit = true,
                TryIntegrate = true,
                RunElevated = true,
                ShowOnToolbar = true,
                Category = "Diagnostics",
                RunOnStartup = true,
                StopOnShutdown = true,
                Hidden = true,
                Hotkey = Keys.Control | Keys.Shift | Keys.F9,
                AuthenticationType = "password",
                AuthenticationUsername = "user'name",
                AuthenticationPassword = "p@ss;word\"1",
                PrivateKeyFile = @"C:\keys\id_ed25519",
                Passphrase = "pass phrase; with \"quotes\""
            },
            new ExternalTool
            {
                // Everything left at its default, to catch a reader that only ever produces "true".
                DisplayName = "Plain",
                FileName = "cmd.exe"
            }
        ];

        private static Dictionary<string, object?> Snapshot(ExternalTool tool) => new(StringComparer.Ordinal)
        {
            [nameof(ExternalTool.DisplayName)] = tool.DisplayName,
            [nameof(ExternalTool.FileName)] = tool.FileName,
            [nameof(ExternalTool.IconPath)] = tool.IconPath,
            [nameof(ExternalTool.Arguments)] = tool.Arguments,
            [nameof(ExternalTool.WorkingDir)] = tool.WorkingDir,
            [nameof(ExternalTool.WaitForExit)] = tool.WaitForExit,
            [nameof(ExternalTool.TryIntegrate)] = tool.TryIntegrate,
            [nameof(ExternalTool.RunElevated)] = tool.RunElevated,
            [nameof(ExternalTool.ShowOnToolbar)] = tool.ShowOnToolbar,
            [nameof(ExternalTool.Category)] = tool.Category,
            [nameof(ExternalTool.RunOnStartup)] = tool.RunOnStartup,
            [nameof(ExternalTool.StopOnShutdown)] = tool.StopOnShutdown,
            [nameof(ExternalTool.Hidden)] = tool.Hidden,
            [nameof(ExternalTool.Hotkey)] = tool.Hotkey,
            [nameof(ExternalTool.AuthenticationType)] = tool.AuthenticationType,
            [nameof(ExternalTool.AuthenticationUsername)] = tool.AuthenticationUsername,
            [nameof(ExternalTool.AuthenticationPassword)] = tool.AuthenticationPassword,
            [nameof(ExternalTool.PrivateKeyFile)] = tool.PrivateKeyFile,
            [nameof(ExternalTool.Passphrase)] = tool.Passphrase
        };

        private static List<string> Differences(Dictionary<string, object?> before,
                                                Dictionary<string, object?> after) =>
            before
                .Where(kv => !Equals(kv.Value, after.TryGetValue(kv.Key, out object? v) ? v : null))
                .Select(kv => $"{kv.Key}: wrote [{kv.Value}] read back "
                              + $"[{(after.TryGetValue(kv.Key, out object? v2) ? v2 : "<missing>")}]")
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToList();

        [Test]
        [Category("RequiresSqlServer")]
        public void EveryExternalToolFieldSurvivesTheDatabaseRoundTrip()
        {
            using MSSqlDatabaseConnector connector = ConnectToPreparedDatabase();

            List<ExternalTool> original = BuildSaturatedTools();
            ExternalAppsSaver.WriteExternalToolsToSql(connector, original);
            List<ExternalTool> loaded = ExternalAppsLoader.ReadExternalToolsFromSql(connector);

            Assert.That(loaded, Has.Count.EqualTo(original.Count),
                        "the tools written to the database did not all come back");

            for (int i = 0; i < original.Count; i++)
            {
                List<string> differences = Differences(Snapshot(original[i]), Snapshot(loaded[i]));
                Assert.That(differences, Is.Empty,
                            $"tool '{original[i].DisplayName}' did not survive the round trip:"
                            + Environment.NewLine + string.Join(Environment.NewLine, differences));
            }
        }

        [Test]
        [Category("RequiresSqlServer")]
        public void ADeletedToolStaysDeleted()
        {
            // The save replaces the whole table, so this is the check that an edit made on one
            // machine is what the next start reads — not the union of every tool ever saved.
            using MSSqlDatabaseConnector connector = ConnectToPreparedDatabase();

            ExternalAppsSaver.WriteExternalToolsToSql(connector, BuildSaturatedTools());
            ExternalAppsSaver.WriteExternalToolsToSql(connector, [BuildSaturatedTools()[1]]);

            List<ExternalTool> loaded = ExternalAppsLoader.ReadExternalToolsFromSql(connector);

            Assert.Multiple(() =>
            {
                Assert.That(loaded, Has.Count.EqualTo(1));
                Assert.That(loaded[0].DisplayName, Is.EqualTo("Plain"));
            });
        }

        [Test]
        [Category("RequiresSqlServer")]
        public void AnEmptyTableReadsBackAsNoTools()
        {
            // Empty means the user has no tools, not "fall back to extApps.xml" — otherwise
            // deleting every tool would resurrect them from a file SQL mode never updates.
            using MSSqlDatabaseConnector connector = ConnectToPreparedDatabase();

            ExternalAppsSaver.WriteExternalToolsToSql(connector, []);

            Assert.That(ExternalAppsLoader.ReadExternalToolsFromSql(connector), Is.Empty);
        }

        private MSSqlDatabaseConnector ConnectToPreparedDatabase()
        {
            if (string.IsNullOrEmpty(_database))
                Assert.Ignore($"No local SQL Server at {ServerInstance}.");

            MSSqlDatabaseConnector connector = new(ServerInstance, _database, "", "");
            connector.Connect();

            // Creates the current schema, tblExternalTools included.
            SqlDatabaseMetaDataRetriever retriever = new();
            retriever.GetDatabaseMetaData(connector);
            retriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);
            return connector;
        }

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

            _database = "mRemoteNGXt_" + Guid.NewGuid().ToString("N")[..12];
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
    }
}

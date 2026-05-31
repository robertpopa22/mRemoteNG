using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.Versioning;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.Versioning;

public class SqlMigrationHelperTests
{
    private DbTransaction _transaction = null!;
    private FakeDbConnection _connection = null!;
    private List<string> _commandTexts = null!;

    [SetUp]
    public void Setup()
    {
        _transaction = Substitute.For<DbTransaction>();
        _connection = new FakeDbConnection(_transaction);
        _commandTexts = [];
    }

    private DbCommand CreateCommand(string sql)
    {
        _commandTexts.Add(sql);
        var command = Substitute.For<DbCommand>();
        command.Parameters.Returns(Substitute.For<DbParameterCollection>());
        command.CreateParameter().Returns(Substitute.For<DbParameter>());
        command.ExecuteNonQuery().Returns(1);
        return command;
    }

    #region ExecuteMigration — MSSql

    [Test]
    public void ExecuteMigration_MSSql_ExecutesAlterAndVersionUpdate()
    {
        var connector = new TestMssqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), "ALTER TABLE t ADD col int;", null);

        Assert.That(_commandTexts, Has.Count.EqualTo(2));
        Assert.That(_commandTexts[0], Does.Contain("ALTER TABLE t ADD col int;"));
        Assert.That(_commandTexts[1], Does.Contain("UPDATE tblRoot SET ConfVersion=@confVersion;"));
        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigration_MSSql_SkipsAlterWhenEmpty()
    {
        var connector = new TestMssqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), "", null);

        Assert.That(_commandTexts, Has.Count.EqualTo(1));
        Assert.That(_commandTexts[0], Does.Contain("UPDATE tblRoot SET ConfVersion=@confVersion;"));
        _transaction.Received(1).Commit();
    }

    #endregion

    #region ExecuteMigration — MySql

    [Test]
    public void ExecuteMigration_MySql_ExecutesAlterAndVersionUpdate()
    {
        var connector = new TestMysqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), null, "ALTER TABLE t ADD col int;");

        // The MySQL batch is now split per statement and trimmed (no trailing ';') so each
        // ADD COLUMN can be run individually with duplicate-column idempotency (#113).
        Assert.That(_commandTexts, Has.Count.EqualTo(2));
        Assert.That(_commandTexts[0], Is.EqualTo("ALTER TABLE t ADD col int"));
        Assert.That(_commandTexts[1], Does.Contain("SET SQL_SAFE_UPDATES=0;"));
        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigration_MySql_SplitsBatchIntoIndividualStatements()
    {
        var connector = new TestMysqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), null,
            "ALTER TABLE t ADD COLUMN a int;\nALTER TABLE t ADD COLUMN b int;");

        // 2 split ALTERs (trimmed) + 1 version update.
        Assert.That(_commandTexts, Has.Count.EqualTo(3));
        Assert.That(_commandTexts[0], Is.EqualTo("ALTER TABLE t ADD COLUMN a int"));
        Assert.That(_commandTexts[1], Is.EqualTo("ALTER TABLE t ADD COLUMN b int"));
        Assert.That(_commandTexts[2], Does.Contain("SET SQL_SAFE_UPDATES=0;"));
        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigration_MySql_CatchesDuplicateColumnError()
    {
        int callCount = 0;
        DbCommand CreateCommandWithDuplicateError(string sql)
        {
            _commandTexts.Add(sql);
            var cmd = Substitute.For<DbCommand>();
            cmd.Parameters.Returns(Substitute.For<DbParameterCollection>());
            cmd.CreateParameter().Returns(Substitute.For<DbParameter>());

            callCount++;
            if (callCount == 1)
            {
                // First ADD COLUMN duplicates a column already added by the schema
                // forward-port — must be caught, not abort the upgrade (#113).
                cmd.When(c => c.ExecuteNonQuery()).Do(_ =>
                    throw new InvalidOperationException("Duplicate column name 'a'"));
            }
            else
            {
                cmd.ExecuteNonQuery().Returns(1);
            }

            return cmd;
        }

        var connector = new TestMysqlConnector(_connection, CreateCommandWithDuplicateError);

        Assert.DoesNotThrow(() =>
            SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), null,
                "ALTER TABLE t ADD COLUMN a int;\nALTER TABLE t ADD COLUMN b int;"));

        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigration_MySql_SkipsAlterWhenNull()
    {
        var connector = new TestMysqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), null, null);

        Assert.That(_commandTexts, Has.Count.EqualTo(1));
        Assert.That(_commandTexts[0], Does.Contain("SET SQL_SAFE_UPDATES=0;"));
        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigration_MySql_SkipsAlterWhenEmpty()
    {
        var connector = new TestMysqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), null, "");

        Assert.That(_commandTexts, Has.Count.EqualTo(1));
        _transaction.Received(1).Commit();
    }

    #endregion

    #region ExecuteMigration — Unknown connector

    [Test]
    public void ExecuteMigration_UnknownConnector_ThrowsNotSupportedException()
    {
        var connector = Substitute.For<IDatabaseConnector>();
        connector.DbConnection().Returns(_connection);

        Assert.Throws<NotSupportedException>(() =>
            SqlMigrationHelper.ExecuteMigration(connector, new Version(2, 0), "ALTER ...", null));
    }

    #endregion

    #region ExecuteMigration — Version parameter

    [Test]
    public void ExecuteMigration_SetsVersionParameterCorrectly()
    {
        var capturedParam = Substitute.For<DbParameter>();
        DbCommand CreateCommandWithParam(string sql)
        {
            _commandTexts.Add(sql);
            var cmd = Substitute.For<DbCommand>();
            cmd.Parameters.Returns(Substitute.For<DbParameterCollection>());
            cmd.CreateParameter().Returns(capturedParam);
            cmd.ExecuteNonQuery().Returns(1);
            return cmd;
        }

        var connector = new TestMssqlConnector(_connection, CreateCommandWithParam);
        var version = new Version(3, 5);

        SqlMigrationHelper.ExecuteMigration(connector, version, "ALTER ...", null);

        Assert.That(capturedParam.Value, Is.EqualTo("3.5"));
        Assert.That(capturedParam.ParameterName, Is.EqualTo("confVersion"));
        Assert.That(capturedParam.DbType, Is.EqualTo(DbType.String));
    }

    #endregion

    #region ExecuteMigrationIdempotent — MSSql

    [Test]
    public void ExecuteMigrationIdempotent_MSSql_ExecutesAlterAndVersionUpdate()
    {
        var connector = new TestMssqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigrationIdempotent(connector, new Version(3, 3),
            "IF NOT EXISTS ... ALTER TABLE t ADD col int;",
            ["ALTER TABLE t ADD COLUMN col int"]);

        Assert.That(_commandTexts, Has.Count.EqualTo(2));
        Assert.That(_commandTexts[0], Does.Contain("IF NOT EXISTS"));
        Assert.That(_commandTexts[1], Does.Contain("UPDATE tblRoot SET ConfVersion=@confVersion;"));
        _transaction.Received(1).Commit();
    }

    #endregion

    #region ExecuteMigrationIdempotent — MySql

    [Test]
    public void ExecuteMigrationIdempotent_MySql_ExecutesEachAlterIndividually()
    {
        var connector = new TestMysqlConnector(_connection, CreateCommand);

        SqlMigrationHelper.ExecuteMigrationIdempotent(connector, new Version(3, 3),
            "IF NOT EXISTS ...",
            ["ALTER TABLE t ADD COLUMN col1 int", "ALTER TABLE t ADD COLUMN col2 int"]);

        // 2 individual ALTERs + 1 version update = 3 commands
        Assert.That(_commandTexts, Has.Count.EqualTo(3));
        Assert.That(_commandTexts[0], Does.Contain("col1"));
        Assert.That(_commandTexts[1], Does.Contain("col2"));
        Assert.That(_commandTexts[2], Does.Contain("SET SQL_SAFE_UPDATES=0;"));
        _transaction.Received(1).Commit();
    }

    [Test]
    public void ExecuteMigrationIdempotent_MySql_CatchesDuplicateColumnError()
    {
        int callCount = 0;
        DbCommand CreateCommandWithDuplicateError(string sql)
        {
            _commandTexts.Add(sql);
            var cmd = Substitute.For<DbCommand>();
            cmd.Parameters.Returns(Substitute.For<DbParameterCollection>());
            cmd.CreateParameter().Returns(Substitute.For<DbParameter>());

            callCount++;
            if (callCount == 1)
            {
                // First ALTER throws "Duplicate column" — should be caught
                cmd.When(c => c.ExecuteNonQuery()).Do(_ =>
                    throw new InvalidOperationException("Duplicate column name 'col1'"));
            }
            else
            {
                cmd.ExecuteNonQuery().Returns(1);
            }

            return cmd;
        }

        var connector = new TestMysqlConnector(_connection, CreateCommandWithDuplicateError);

        Assert.DoesNotThrow(() =>
            SqlMigrationHelper.ExecuteMigrationIdempotent(connector, new Version(3, 3),
                "IF NOT EXISTS ...",
                ["ALTER TABLE t ADD COLUMN col1 int", "ALTER TABLE t ADD COLUMN col2 int"]));

        _transaction.Received(1).Commit();
    }

    #endregion

    #region ExecuteMigrationIdempotent — Unknown connector

    [Test]
    public void ExecuteMigrationIdempotent_UnknownConnector_ThrowsNotSupportedException()
    {
        var connector = Substitute.For<IDatabaseConnector>();
        connector.DbConnection().Returns(_connection);

        Assert.Throws<NotSupportedException>(() =>
            SqlMigrationHelper.ExecuteMigrationIdempotent(connector, new Version(3, 3),
                "ALTER ...", ["ALTER ..."]));
    }

    #endregion

    #region Test Doubles

    private sealed class TestMssqlConnector(DbConnection connection, Func<string, DbCommand> commandFactory)
        : MSSqlDatabaseConnector("localhost", "mremoteng", "user", "password"), IDatabaseConnector
    {
        DbConnection IDatabaseConnector.DbConnection() => connection;
        DbCommand IDatabaseConnector.DbCommand(string dbCommand) => commandFactory(dbCommand);
        bool IDatabaseConnector.IsConnected => true;
        void IDatabaseConnector.Connect() { }
        Task IDatabaseConnector.ConnectAsync() => Task.CompletedTask;
        void IDatabaseConnector.Disconnect() { }
        void IDatabaseConnector.AssociateItemToThisConnector(DbCommand dbCommand) { }
        void IDisposable.Dispose() { }
    }

    private sealed class TestMysqlConnector(DbConnection connection, Func<string, DbCommand> commandFactory)
        : MySqlDatabaseConnector("localhost", "mremoteng", "user", "password"), IDatabaseConnector
    {
        DbConnection IDatabaseConnector.DbConnection() => connection;
        DbCommand IDatabaseConnector.DbCommand(string dbCommand) => commandFactory(dbCommand);
        bool IDatabaseConnector.IsConnected => true;
        void IDatabaseConnector.Connect() { }
        Task IDatabaseConnector.ConnectAsync() => Task.CompletedTask;
        void IDatabaseConnector.Disconnect() { }
        void IDatabaseConnector.AssociateItemToThisConnector(DbCommand dbCommand) { }
        void IDisposable.Dispose() { }
    }

    private sealed class FakeDbConnection(DbTransaction transaction) : DbConnection
    {
        public override string ConnectionString { get; set; } = string.Empty;
        public override string Database => "mremoteng";
        public override string DataSource => "localhost";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => ConnectionState.Open;
        public override void ChangeDatabase(string databaseName) { }
        public override void Close() { }
        public override void Open() { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => transaction;
        protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
    }

    #endregion
}

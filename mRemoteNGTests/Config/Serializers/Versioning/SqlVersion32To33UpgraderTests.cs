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

public class SqlVersion32To33UpgraderTests
{
    #region CanUpgrade

    [Test]
    public void CanUpgrade_Version32_ReturnsTrue()
    {
        var connector = Substitute.For<MSSqlDatabaseConnector>("", "", "", "");
        var sut = new SqlVersion32To33Upgrader(connector);

        Assert.That(sut.CanUpgrade(new Version(3, 2)), Is.True);
    }

    [Test]
    public void CanUpgrade_Version31_ReturnsTrue()
    {
        var connector = Substitute.For<MSSqlDatabaseConnector>("", "", "", "");
        var sut = new SqlVersion32To33Upgrader(connector);

        // 3.1 <= 3.3 && 3.1 < 3.3 → true
        Assert.That(sut.CanUpgrade(new Version(3, 1)), Is.True);
    }

    [Test]
    public void CanUpgrade_Version33_ReturnsFalse()
    {
        var connector = Substitute.For<MSSqlDatabaseConnector>("", "", "", "");
        var sut = new SqlVersion32To33Upgrader(connector);

        Assert.That(sut.CanUpgrade(new Version(3, 3)), Is.False);
    }

    [Test]
    public void CanUpgrade_Version34_ReturnsFalse()
    {
        var connector = Substitute.For<MSSqlDatabaseConnector>("", "", "", "");
        var sut = new SqlVersion32To33Upgrader(connector);

        Assert.That(sut.CanUpgrade(new Version(3, 4)), Is.False);
    }

    [Test]
    public void CanUpgrade_Version20_ReturnsTrue()
    {
        var connector = Substitute.For<MSSqlDatabaseConnector>("", "", "", "");
        var sut = new SqlVersion32To33Upgrader(connector);

        // 2.0 <= 3.3 && 2.0 < 3.3 → true (second branch)
        Assert.That(sut.CanUpgrade(new Version(2, 0)), Is.True);
    }

    #endregion

    #region Constructor

    [Test]
    public void Constructor_NullConnector_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new SqlVersion32To33Upgrader(null!));
    }

    #endregion

    #region Upgrade — MSSql

    [Test]
    public void Upgrade_MSSql_ExecutesIdempotentAlterAndReturnsVersion33()
    {
        var transaction = Substitute.For<DbTransaction>();
        var connection = new FakeDbConnection(transaction);
        var commandTexts = new List<string>();

        DbCommand CreateCommand(string sql)
        {
            commandTexts.Add(sql);
            var cmd = Substitute.For<DbCommand>();
            cmd.Parameters.Returns(Substitute.For<DbParameterCollection>());
            cmd.CreateParameter().Returns(Substitute.For<DbParameter>());
            cmd.ExecuteNonQuery().Returns(1);
            return cmd;
        }

        var connector = new TestMssqlConnector(connection, CreateCommand);
        var sut = new SqlVersion32To33Upgrader(connector);

        var result = sut.Upgrade();

        Assert.That(result, Is.EqualTo(new Version(3, 3)));
        Assert.That(commandTexts, Has.Count.EqualTo(2)); // idempotent alter + version update
        Assert.That(commandTexts[0], Does.Contain("tblExternalTools"));
        Assert.That(commandTexts[0], Does.Contain("Hidden"));
        Assert.That(commandTexts[0], Does.Contain("AuthType"));
        Assert.That(commandTexts[0], Does.Contain("AuthUsername"));
        Assert.That(commandTexts[0], Does.Contain("AuthPassword"));
        Assert.That(commandTexts[0], Does.Contain("PrivateKeyFile"));
        Assert.That(commandTexts[0], Does.Contain("Passphrase"));
        Assert.That(commandTexts[1], Does.Contain("UPDATE tblRoot SET ConfVersion=@confVersion;"));
        transaction.Received(1).Commit();
    }

    #endregion

    #region Upgrade — MySql

    [Test]
    public void Upgrade_MySql_ExecutesSixIndividualAlters()
    {
        var transaction = Substitute.For<DbTransaction>();
        var connection = new FakeDbConnection(transaction);
        var commandTexts = new List<string>();

        DbCommand CreateCommand(string sql)
        {
            commandTexts.Add(sql);
            var cmd = Substitute.For<DbCommand>();
            cmd.Parameters.Returns(Substitute.For<DbParameterCollection>());
            cmd.CreateParameter().Returns(Substitute.For<DbParameter>());
            cmd.ExecuteNonQuery().Returns(1);
            return cmd;
        }

        var connector = new TestMysqlConnector(connection, CreateCommand);
        var sut = new SqlVersion32To33Upgrader(connector);

        var result = sut.Upgrade();

        Assert.That(result, Is.EqualTo(new Version(3, 3)));
        // safe-updates read + disable (#145) + 6 individual ALTERs + 1 version update = 9 commands
        Assert.That(commandTexts, Has.Count.EqualTo(9));
        Assert.That(commandTexts[2], Does.Contain("Hidden"));
        Assert.That(commandTexts[3], Does.Contain("AuthType"));
        Assert.That(commandTexts[4], Does.Contain("AuthUsername"));
        Assert.That(commandTexts[5], Does.Contain("AuthPassword"));
        Assert.That(commandTexts[6], Does.Contain("PrivateKeyFile"));
        Assert.That(commandTexts[7], Does.Contain("Passphrase"));
        transaction.Received(1).Commit();
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

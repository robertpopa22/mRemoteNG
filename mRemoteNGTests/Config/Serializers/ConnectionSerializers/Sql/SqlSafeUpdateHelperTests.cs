using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Sql
{
    /// <summary>
    /// These tests previously asserted a "DELETE ... LIMIT 1" loop and passed for months while
    /// every save against a MariaDB with safe-update mode enabled failed — they encoded the
    /// workaround as the contract instead of the outcome. MariaDB rejects a full-scan DELETE
    /// whatever the LIMIT says, because the check is made by the planner (verified on 10.11, with
    /// and without a primary key). They now pin the behaviour that actually works: suspend
    /// safe-update mode for the statement, then restore it. (#148)
    /// </summary>
    [TestFixture]
    public class SqlSafeUpdateHelperTests
    {
        [Test]
        public void NonMySqlIssuesOnePlainDelete()
        {
            IDatabaseConnector connector = Substitute.For<IDatabaseConnector>();
            DbTransaction transaction = Substitute.For<DbTransaction>();
            DbCommand command = Substitute.For<DbCommand>();
            connector.DbCommand("DELETE FROM tblRoot").Returns(command);

            SqlSafeUpdateHelper.DeleteAllRows(connector, transaction, "DELETE FROM tblRoot");

            connector.Received(1).DbCommand("DELETE FROM tblRoot");
            Assert.That(command.Transaction, Is.SameAs(transaction));
        }

        [Test]
        public void NonMySqlDoesNotTouchSessionState()
        {
            IDatabaseConnector connector = Substitute.For<IDatabaseConnector>();
            connector.DbCommand(Arg.Any<string>()).Returns(_ => Substitute.For<DbCommand>());

            SqlSafeUpdateHelper.DeleteAllRows(connector, null, "DELETE FROM tblRoot");

            // SQL Server has no such mode; issuing SET SESSION there would be a syntax error.
            connector.DidNotReceive().DbCommand(Arg.Is<string>(t => t.Contains("sql_safe_updates")));
        }

        [Test]
        public void MySqlSuspendsSafeUpdatesAroundASinglePlainDelete()
        {
            List<string> texts = [];
            IDatabaseConnector connector = new TestMySqlConnector(text =>
            {
                texts.Add(text);
                DbCommand command = Substitute.For<DbCommand>();
                // The scope reads the current value before changing it.
                command.ExecuteScalar().Returns(1);
                return command;
            });

            SqlSafeUpdateHelper.DeleteAllRows(connector, null, "DELETE FROM tblRoot");

            Assert.Multiple(() =>
            {
                Assert.That(texts.Any(t => t.Contains("SELECT @@SESSION.sql_safe_updates")), Is.True,
                            "the original session value was never read, so it cannot be restored");
                Assert.That(texts.Any(t => t.Contains("sql_safe_updates=0")), Is.True,
                            "safe-update mode was never suspended, so MariaDB will reject the delete");
                Assert.That(texts, Does.Contain("DELETE FROM tblRoot"),
                            "the plain delete was not issued");
                Assert.That(texts.Any(t => t.Contains("LIMIT")), Is.False,
                            "a LIMIT delete is still being issued — MariaDB rejects it regardless (#148)");
            });
        }

        [Test]
        public void MySqlRestoresSafeUpdatesAfterwards()
        {
            List<string> texts = [];
            IDatabaseConnector connector = new TestMySqlConnector(text =>
            {
                texts.Add(text);
                DbCommand command = Substitute.For<DbCommand>();
                command.ExecuteScalar().Returns(1);   // it was ON before the operation
                return command;
            });

            SqlSafeUpdateHelper.DeleteAllRows(connector, null, "DELETE FROM tblRoot");

            // MySql.Data pools connections without a session reset, so leaving the mode off would
            // silently weaken safety for whatever borrows this connection next.
            Assert.That(texts.Last(), Does.Contain("sql_safe_updates=1"),
                        "safe-update mode was left disabled on a pooled connection");
        }

        [Test]
        public void MySqlLeavesSafeUpdatesOffWhenItWasAlreadyOff()
        {
            List<string> texts = [];
            IDatabaseConnector connector = new TestMySqlConnector(text =>
            {
                texts.Add(text);
                DbCommand command = Substitute.For<DbCommand>();
                command.ExecuteScalar().Returns(0);   // it was OFF before the operation
                return command;
            });

            SqlSafeUpdateHelper.DeleteAllRows(connector, null, "DELETE FROM tblRoot");

            Assert.That(texts.Any(t => t.Contains("sql_safe_updates=1")), Is.False,
                        "safe-update mode was switched ON, which the session never had");
        }

        private sealed class TestMySqlConnector(Func<string, DbCommand> commandFactory)
            : MySqlDatabaseConnector("localhost", "database", "user", "password"), IDatabaseConnector
        {
            DbCommand IDatabaseConnector.DbCommand(string commandText) => commandFactory(commandText);

            DbConnection IDatabaseConnector.DbConnection() => throw new NotSupportedException();

            bool IDatabaseConnector.IsConnected => true;

            void IDatabaseConnector.Connect() { }

            Task IDatabaseConnector.ConnectAsync() => Task.CompletedTask;

            void IDatabaseConnector.Disconnect() { }

            void IDatabaseConnector.AssociateItemToThisConnector(DbCommand dbCommand) { }

            void IDisposable.Dispose() { }
        }
    }
}

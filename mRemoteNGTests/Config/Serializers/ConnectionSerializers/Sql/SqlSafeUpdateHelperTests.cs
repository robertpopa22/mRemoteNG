using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Sql
{
    [TestFixture]
    public class SqlSafeUpdateHelperTests
    {
        [Test]
        public void DeleteAllRows_NonMySql_UsesSingleUnboundedDelete()
        {
            IDatabaseConnector connector = Substitute.For<IDatabaseConnector>();
            DbTransaction transaction = Substitute.For<DbTransaction>();
            DbCommand command = Substitute.For<DbCommand>();
            connector.DbCommand("DELETE FROM tblRoot").Returns(command);
            command.ExecuteNonQuery().Returns(2);

            SqlSafeUpdateHelper.DeleteAllRows(
                connector,
                transaction,
                "DELETE FROM tblRoot",
                "DELETE FROM tblRoot LIMIT 1");

            connector.Received(1).DbCommand("DELETE FROM tblRoot");
            connector.DidNotReceive().DbCommand("DELETE FROM tblRoot LIMIT 1");
            Assert.That(command.Transaction, Is.SameAs(transaction));
        }

        [Test]
        public void DeleteAllRows_MySql_DeletesOneRowUntilTableIsEmpty()
        {
            Queue<int> results = new([1, 1, 0]);
            List<string> commandTexts = [];
            List<DbCommand> commands = [];

            DbCommand CreateCommand(string commandText)
            {
                commandTexts.Add(commandText);
                DbCommand command = Substitute.For<DbCommand>();
                command.ExecuteNonQuery().Returns(results.Dequeue());
                commands.Add(command);
                return command;
            }

            IDatabaseConnector connector = new TestMySqlConnector(CreateCommand);
            DbTransaction transaction = Substitute.For<DbTransaction>();

            SqlSafeUpdateHelper.DeleteAllRows(
                connector,
                transaction,
                "DELETE FROM tblRoot",
                "DELETE FROM tblRoot LIMIT 1");

            Assert.That(commandTexts, Has.Count.EqualTo(3));
            Assert.That(commandTexts, Has.All.EqualTo("DELETE FROM tblRoot LIMIT 1"));
            foreach (DbCommand command in commands)
                Assert.That(command.Transaction, Is.SameAs(transaction));
        }

        private sealed class TestMySqlConnector(Func<string, DbCommand> commandFactory)
            : MySqlDatabaseConnector("localhost", "database", "user", "password"), IDatabaseConnector
        {
            DbCommand IDatabaseConnector.DbCommand(string commandText)
            {
                return commandFactory(commandText);
            }

            DbConnection IDatabaseConnector.DbConnection()
            {
                throw new NotSupportedException();
            }

            bool IDatabaseConnector.IsConnected => true;

            void IDatabaseConnector.Connect()
            {
            }

            Task IDatabaseConnector.ConnectAsync()
            {
                return Task.CompletedTask;
            }

            void IDatabaseConnector.Disconnect()
            {
            }

            void IDatabaseConnector.AssociateItemToThisConnector(DbCommand dbCommand)
            {
            }

            void IDisposable.Dispose()
            {
            }
        }
    }
}

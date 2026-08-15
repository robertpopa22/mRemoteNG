using System;
using System.Threading.Tasks;
using mRemoteNG.Config.DatabaseConnectors;
using NUnit.Framework;

namespace mRemoteNGTests.Config.DatabaseConnectors
{
    /// <summary>
    /// The database name reaches a CREATE DATABASE statement by string interpolation, so it is the
    /// one piece of user input on this path that has to be rejected before it is used. The check
    /// used to run after the connection was opened, which cost a round trip on an invalid name and
    /// made it impossible to test without a live server; it now runs first.
    /// </summary>
    public class DatabaseCreationValidationTests
    {
        [TestCase("robert'; DROP TABLE tblCons; --", TestName = "SQL injection attempt")]
        [TestCase("db]", TestName = "closing bracket escapes the quoted identifier")]
        [TestCase("db name", TestName = "space")]
        [TestCase("db;name", TestName = "statement separator")]
        [TestCase("", TestName = "empty")]
        [TestCase("täbelle", TestName = "non-ASCII")]
        public void AnInvalidDatabaseNameIsRejectedBeforeAnythingIsOpened(string database)
        {
            // The server address is deliberately unreachable: if validation ran after connecting,
            // this would fail with a connection error instead of ArgumentException.
            Assert.That(
                async () => await DatabaseConnectionTester.TryCreateDatabaseAsync(
                    "mssql", "255.255.255.255", database, "user", "password"),
                Throws.TypeOf<ArgumentException>());
        }

        [TestCase("mRemoteNG")]
        [TestCase("mremoteng_prod")]
        [TestCase("mremoteng-test")]
        [TestCase("db2024")]
        public void AValidDatabaseNamePassesValidationAndProceedsToConnect(string database)
        {
            // A valid name must NOT raise ArgumentException; it fails later, on the unreachable
            // server, which is what proves validation let it through.
            Assert.That(
                async () => await DatabaseConnectionTester.TryCreateDatabaseAsync(
                    "mssql", "255.255.255.255", database, "user", "password"),
                Throws.Exception.Not.TypeOf<ArgumentException>());
        }
    }
}

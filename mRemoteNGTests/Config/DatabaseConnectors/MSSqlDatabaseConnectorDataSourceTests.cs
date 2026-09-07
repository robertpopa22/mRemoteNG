using Microsoft.Data.SqlClient;
using mRemoteNG.Config.DatabaseConnectors;
using NUnit.Framework;

namespace mRemoteNGTests.Config.DatabaseConnectors
{
    /// <summary>
    /// The Data Source the connector hands SqlClient, for the host spellings people type into
    /// the SQL Server options page. No connection is opened; building a SqlConnection only
    /// parses the string.
    ///
    /// The named-instance case is #165: with SQL credentials the connector used to append a
    /// default ",1433" to every host, and an explicit port makes SqlClient skip the SQL Browser
    /// lookup, so "host\instance" resolved to port 1433 instead of the instance's dynamic port
    /// and failed with "server was not found" — on a setup that 1.76 opened fine.
    /// </summary>
    [TestFixture]
    public class MSSqlDatabaseConnectorDataSourceTests
    {
        private static string DataSourceFor(string host, string username)
        {
            using MSSqlDatabaseConnector connector = new(host, "mRemoteNG", username, username.Length > 0 ? "secret" : "");
            return new SqlConnectionStringBuilder(connector.DbConnection().ConnectionString).DataSource;
        }

        [TestCase(@"192.168.10.202\SQL19SAP10", TestName = "named instance, SQL credentials")]
        [TestCase("192.168.10.202,1433", TestName = "host with SqlClient port syntax, SQL credentials")]
        [TestCase("sqlhost", TestName = "bare host, SQL credentials")]
        public void AHostWithoutAColonPortGoesToSqlClientUntouched(string host)
        {
            Assert.That(DataSourceFor(host, "sa"), Is.EqualTo(host));
        }

        [TestCase(@"192.168.10.202\SQL19SAP10", TestName = "named instance, Windows auth")]
        [TestCase("sqlhost", TestName = "bare host, Windows auth")]
        public void WindowsAuthenticationPassesTheHostThroughAsWell(string host)
        {
            Assert.That(DataSourceFor(host, ""), Is.EqualTo(host));
        }

        [TestCase("sqlhost:1500", "sqlhost,1500", "sa", TestName = "SQL credentials")]
        [TestCase("sqlhost:1500", "sqlhost,1500", "", TestName = "Windows auth")]
        [TestCase(@"sqlhost\inst:1500", @"sqlhost\inst,1500", "sa", TestName = "named instance with explicit port")]
        public void AColonPortBecomesTheCommaSqlClientExpects(string host, string expected, string username)
        {
            Assert.That(DataSourceFor(host, username), Is.EqualTo(expected));
        }
    }
}

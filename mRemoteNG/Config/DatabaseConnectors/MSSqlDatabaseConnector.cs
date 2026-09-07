using System;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using static BrightIdeasSoftware.TreeListView;

// ReSharper disable ArrangeAccessorOwnerBody

namespace mRemoteNG.Config.DatabaseConnectors
{
    public class MSSqlDatabaseConnector : IDatabaseConnector
    {
        private DbConnection _dbConnection { get; set; } = default!;
        private string _dbConnectionString = "";
        private readonly string _dbHost;
        private readonly string _dbCatalog;
        private readonly string _dbUsername;
        private readonly string _dbPassword;

        public DbConnection DbConnection()
        {
            return _dbConnection;
        }

        public DbCommand DbCommand(string dbCommand)
        {
            return new SqlCommand(dbCommand, (SqlConnection) _dbConnection);
        }

        public bool IsConnected => (_dbConnection.State == ConnectionState.Open);

        public MSSqlDatabaseConnector(string sqlServer, string catalog, string username, string password)
        {
            _dbHost = sqlServer;
            _dbCatalog = catalog;
            _dbUsername = username;
            _dbPassword = password;
            Initialize();
        }

        private void Initialize()
        {
            BuildSqlConnectionString();
            _dbConnection = new SqlConnection(_dbConnectionString);
        }

        private void BuildSqlConnectionString()
        {
            if (!string.IsNullOrEmpty(_dbUsername) || !string.IsNullOrEmpty(_dbPassword))
                BuildDbConnectionStringWithCustomCredentials();
            else
                BuildDbConnectionStringWithDefaultCredentials();
        }

        /// <summary>
        /// "host:port" is how mRemoteNG spells a port (#1884); SqlClient wants "host,port".
        /// Without an explicit port the host goes through untouched. Appending ",1433" as a
        /// default made SqlClient skip the SQL Browser lookup for "host\instance" and dial 1433
        /// directly, which a named instance on a dynamic port never answers — "server was not
        /// found" for a database that 1.76 (no default port) opened fine (#165). A host that
        /// already carries ",port" is left alone for the same reason.
        /// </summary>
        internal static string BuildDataSource(string host)
        {
            string[] hostParts = host.Split(':', 2);
            return hostParts.Length == 2 ? $"{hostParts[0]},{hostParts[1]}" : host;
        }

        private void BuildDbConnectionStringWithCustomCredentials()
        {
            _dbConnectionString = new SqlConnectionStringBuilder
            {
                ApplicationName = "mRemoteNG",
                DataSource = BuildDataSource(_dbHost),
                InitialCatalog = _dbCatalog,
                UserID = _dbUsername,
                Password = _dbPassword,
                IntegratedSecurity = false,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 30,
                MultipleActiveResultSets = true
            }.ToString();
        }

        private void BuildDbConnectionStringWithDefaultCredentials()
        {
            _dbConnectionString = new SqlConnectionStringBuilder
            {
                ApplicationName = "mRemoteNG",
                DataSource = BuildDataSource(_dbHost),
                InitialCatalog = _dbCatalog,
                IntegratedSecurity = true,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 30,
                MultipleActiveResultSets = true
            }.ToString();
        }

        public void Connect()
        {
            _dbConnection.Open();
        }

        public async Task ConnectAsync()
        {
            await _dbConnection.OpenAsync();
        }

        public void Disconnect()
        {
            _dbConnection.Close();
        }

        public void AssociateItemToThisConnector(DbCommand dbCommand)
        {
            dbCommand.Connection = (SqlConnection) _dbConnection;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool itIsSafeToFreeManagedObjects)
        {
            if (!itIsSafeToFreeManagedObjects) return;
            _dbConnection.Close();
            _dbConnection.Dispose();
        }
    }
}
using System;
using System.Data.Common;
using System.Data.Odbc;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace mRemoteNG.Config.DatabaseConnectors
{
    /// <summary>
    /// A helper class for testing database connectivity.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class DatabaseConnectionTester
    {
        /// <summary>
        /// Upper bound for every regex here. These patterns run over data shaped by a remote
        /// server or by user input, so a pathological input must fail fast rather than spin.
        /// </summary>
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

        public static async Task<ConnectionTestResult> TestConnectivity(string type, string server, string database, string username, string password, string? authType = null)
        {
            return (await TestConnectivityDetailed(type, server, database, username, password, authType)).Result;
        }

        /// <summary>
        /// Same test, but also returns the provider's own error text. The classified enum alone
        /// cannot tell the user what went wrong — a timeout reaching a named instance and a
        /// blocked port both collapse to ServerNotAccessible, and anything unrecognized became a
        /// bare "unknown error" with no way to act on it. (#165)
        /// </summary>
        public static async Task<DatabaseConnectionTestOutcome> TestConnectivityDetailed(string type, string server, string database, string username, string password, string? authType = null)
        {
            try
            {
                using IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnector(type, server, database, username, password, authType);
                await dbConnector.ConnectAsync();
                return new DatabaseConnectionTestOutcome(ConnectionTestResult.ConnectionSucceded, null);
            }
            catch (SqlException ex)
            {
                return new DatabaseConnectionTestOutcome(HandleSqlException(ex), DescribeError(ex, ex.Number));
            }
            catch (MySqlException ex)
            {
                return new DatabaseConnectionTestOutcome(HandleMySqlException(ex), DescribeError(ex, ex.Number));
            }
            catch (OdbcException ex)
            {
                return new DatabaseConnectionTestOutcome(HandleOdbcException(ex), DescribeError(ex, null));
            }
            catch (Exception ex)
            {
                // Generic fallback using string matching (supports all connector types)
                string message = ex.Message;
                ConnectionTestResult result;
                if (message.Contains("server was not found", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("network-related", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("instance-specific", StringComparison.OrdinalIgnoreCase))
                    result = ConnectionTestResult.ServerNotAccessible;
                else if (message.Contains("Cannot open database", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Unknown database", StringComparison.OrdinalIgnoreCase))
                    result = ConnectionTestResult.UnknownDatabase;
                else if (message.Contains("Login failed", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Access denied", StringComparison.OrdinalIgnoreCase))
                    result = ConnectionTestResult.CredentialsRejected;
                else
                    result = ConnectionTestResult.UnknownError;

                return new DatabaseConnectionTestOutcome(result, DescribeError(ex, null));
            }
        }

        /// <summary>
        /// The provider message plus its error number. Credentials are never part of a provider
        /// error message, but the connection string can be — strip anything password-shaped
        /// before this reaches a dialog or a log.
        /// </summary>
        internal static string DescribeError(Exception ex, int? number)
        {
            // Bounded: this runs over a provider error message, whose content is influenced by the
            // server being contacted. An unbounded match on hostile input is a denial-of-service
            // waiting to happen, and a masking step is never worth hanging the UI for (S6444).
            string message = Regex.Replace(
                ex.Message,
                @"(?i)\b(password|pwd)\s*=\s*[^;]*",
                "$1=***",
                RegexOptions.None,
                RegexTimeout);

            return number is null or 0
                ? message
                : string.Format(CultureInfo.InvariantCulture, "{0} (error {1})", message, number);
        }

        private static ConnectionTestResult HandleSqlException(SqlException sqlException)
        {
            return sqlException.Number switch
            {
                4060 => ConnectionTestResult.UnknownDatabase,
                18456 or 18452 or 18451 or 18470 or 18486 or 18487 or 18488 => ConnectionTestResult.CredentialsRejected,
                -1 or -2 or 2 or 53 => ConnectionTestResult.ServerNotAccessible,
                _ => ConnectionTestResult.UnknownError
            };
        }

        private static ConnectionTestResult HandleMySqlException(MySqlException mySqlException)
        {
            return mySqlException.Number switch
            {
                1049 => ConnectionTestResult.UnknownDatabase,
                1045 => ConnectionTestResult.CredentialsRejected,
                2002 => ConnectionTestResult.ServerNotAccessible,
                2003 => ConnectionTestResult.ServerNotAccessible,
                2005 => ConnectionTestResult.ServerNotAccessible,
                _ => ConnectionTestResult.UnknownError
            };
        }

        /// <summary>
        /// Attempts to create the specified database by connecting to 'master' (MSSQL)
        /// or without a database (MySQL), then issuing CREATE DATABASE.
        /// </summary>
        public static async Task<bool> TryCreateDatabaseAsync(string type, string server, string database, string username, string password, string? authType = null)
        {
            string masterDb = string.Equals(type, DatabaseConnectorFactory.MySqlType, StringComparison.OrdinalIgnoreCase)
                ? ""
                : "master";

            using IDatabaseConnector dbConnector = DatabaseConnectorFactory.DatabaseConnector(type, server, masterDb, username, password, authType);
            await dbConnector.ConnectAsync();

            // Database names are user-supplied — validate to prevent injection.
            // Allow only alphanumeric, underscore, hyphen (safe subset).
            if (!System.Text.RegularExpressions.Regex.IsMatch(database, @"^[A-Za-z0-9_\-]+$",
                                                             System.Text.RegularExpressions.RegexOptions.None,
                                                             RegexTimeout))
                throw new ArgumentException($"Invalid database name: {database}");

            DbCommand cmd = dbConnector.DbCommand($"CREATE DATABASE [{database}]");
            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        private static ConnectionTestResult HandleOdbcException(OdbcException odbcException)
        {
            foreach (OdbcError error in odbcException.Errors)
            {
                switch (error.SQLState)
                {
                    case "28000":
                        return ConnectionTestResult.CredentialsRejected;
                    case "3D000":
                        return ConnectionTestResult.UnknownDatabase;
                    case "08001":
                    case "08004":
                    case "HYT00":
                    case "HYT01":
                        return ConnectionTestResult.ServerNotAccessible;
                }
            }

            string message = odbcException.Message;

            if (message.Contains("login failed", StringComparison.OrdinalIgnoreCase))
                return ConnectionTestResult.CredentialsRejected;
            if (message.Contains("cannot open database", StringComparison.OrdinalIgnoreCase))
                return ConnectionTestResult.UnknownDatabase;
            if (message.Contains("server was not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("data source name not found", StringComparison.OrdinalIgnoreCase)
                || message.Contains("network-related", StringComparison.OrdinalIgnoreCase))
                return ConnectionTestResult.ServerNotAccessible;

            return ConnectionTestResult.UnknownError;
        }
    }
}

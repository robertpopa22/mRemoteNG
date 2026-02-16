using mRemoteNG.App;
using mRemoteNG.Security.SymmetricEncryption;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.DatabaseConnectors
{
    [SupportedOSPlatform("windows")]
    public static class DatabaseConnectorFactory
    {
        public const string MsSqlType = "mssql";
        public const string MySqlType = "mysql";
        public const string OdbcType = "odbc";

        public static IDatabaseConnector DatabaseConnectorFromSettings()
        {
            // TODO: add custom port handling?
            string sqlType = Properties.OptionsDBsPage.Default.SQLServerType;
            string sqlHost = Properties.OptionsDBsPage.Default.SQLHost;
            string sqlCatalog = Properties.OptionsDBsPage.Default.SQLDatabaseName;
            string sqlUsername = Properties.OptionsDBsPage.Default.SQLUser;
            LegacyRijndaelCryptographyProvider cryptographyProvider = new();
            string sqlPassword = cryptographyProvider.Decrypt(Properties.OptionsDBsPage.Default.SQLPass, Runtime.EncryptionKey);

            return DatabaseConnector(sqlType, sqlHost, sqlCatalog, sqlUsername, sqlPassword);
        }

        public static IDatabaseConnector DatabaseConnector(string? type, string server, string database, string username, string password)
        {
            switch (NormalizeType(type))
            {
                case MySqlType:
                    return new MySqlDatabaseConnector(server, database, username, password);
                case OdbcType:
                    return new OdbcDatabaseConnector(server, database, username, password);
                case MsSqlType:
                default:
                    return new MSSqlDatabaseConnector(server, database, username, password);
            }
        }

        public static string NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return MsSqlType;

            string normalizedType = type.Trim();

            if (string.Equals(normalizedType, "MSSQL - developed by Microsoft", StringComparison.OrdinalIgnoreCase))
                return MsSqlType;
            if (string.Equals(normalizedType, "MySQL - developed by Oracle", StringComparison.OrdinalIgnoreCase))
                return MySqlType;
            if (string.Equals(normalizedType, "ODBC - Open Database Connectivity", StringComparison.OrdinalIgnoreCase))
                return OdbcType;

            return normalizedType.ToLowerInvariant() switch
            {
                MySqlType => MySqlType,
                OdbcType => OdbcType,
                _ => MsSqlType
            };
        }
    }
}
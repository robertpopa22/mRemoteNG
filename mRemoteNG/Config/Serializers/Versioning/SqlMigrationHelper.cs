using mRemoteNG.Config.DatabaseConnectors;
using System;
using System.Data;
using System.Data.Common;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    internal static class SqlMigrationHelper
    {
        private const string MsSqlVersionUpdate = "UPDATE tblRoot SET ConfVersion=@confVersion;";
        private const string MySqlVersionUpdate = "SET SQL_SAFE_UPDATES=0; UPDATE tblRoot SET ConfVersion=?; SET SQL_SAFE_UPDATES=1;";

        /// <summary>
        /// Executes a database migration with separate SQL for MS-SQL and MySQL backends,
        /// wrapped in a serializable transaction with version tracking.
        /// </summary>
        public static void ExecuteMigration(
            IDatabaseConnector connector,
            Version toVersion,
            string msSqlAlter,
            string? mySqlAlter)
        {
            using DbTransaction sqlTran = connector.DbConnection().BeginTransaction(IsolationLevel.Serializable);
            DbCommand dbCommand;
            if (connector is MSSqlDatabaseConnector or OdbcDatabaseConnector)
            {
                if (!string.IsNullOrEmpty(msSqlAlter))
                {
                    dbCommand = connector.DbCommand(MakeMssqlColumnAddsIdempotent(msSqlAlter));
                    dbCommand.Transaction = sqlTran;
                    dbCommand.ExecuteNonQuery();
                }

                dbCommand = connector.DbCommand(MsSqlVersionUpdate);
                dbCommand.Transaction = sqlTran;
            }
            else if (connector is MySqlDatabaseConnector)
            {
                if (!string.IsNullOrEmpty(mySqlAlter))
                {
                    ExecuteMySqlBatchIdempotent(connector, sqlTran, mySqlAlter);
                }

                dbCommand = connector.DbCommand(MySqlVersionUpdate);
                dbCommand.Transaction = sqlTran;
            }
            else
            {
                throw new NotSupportedException("Unknown database back-end");
            }

            DbParameter pConfVersion = dbCommand.CreateParameter();
            pConfVersion.ParameterName = "confVersion";
            pConfVersion.Value = toVersion.ToString();
            pConfVersion.DbType = DbType.String;
            pConfVersion.Direction = ParameterDirection.Input;
            dbCommand.Parameters.Add(pConfVersion);

            dbCommand.ExecuteNonQuery();
            sqlTran.Commit();
        }

        /// <summary>
        /// Runs a MySQL migration batch one statement at a time so that
        /// "ALTER TABLE ... ADD COLUMN" statements targeting columns already created by the
        /// generic schema forward-port (UpgradeMysqlSchema, which runs ahead of the versioned
        /// upgraders) are skipped instead of aborting the whole upgrade with MySQL error 1060
        /// ("Duplicate column name"). MySQL has no per-column IF-NOT-EXISTS guard for ADD COLUMN,
        /// so the duplicate-column error is caught per statement; every other failure still
        /// propagates and rolls back the transaction. This mirrors the MS-SQL idempotency the
        /// MakeMssqlColumnAddsIdempotent guard already provides. (#113)
        /// </summary>
        private static void ExecuteMySqlBatchIdempotent(IDatabaseConnector connector, DbTransaction sqlTran, string mySqlAlter)
        {
            foreach (string statement in mySqlAlter.Split(';'))
            {
                string trimmed = statement.Trim();
                if (trimmed.Length == 0)
                    continue;

                try
                {
                    DbCommand dbCommand = connector.DbCommand(trimmed);
                    dbCommand.Transaction = sqlTran;
                    dbCommand.ExecuteNonQuery();
                }
                catch (Exception ex) when (ex.Message.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase))
                {
                    // Column already added by the schema forward-port -- safe to skip.
                }
            }
        }

        /// <summary>
        /// Like ExecuteMigration but MySQL uses individual ALTERs with idempotency
        /// (catches "Duplicate column" errors).
        /// </summary>
        public static void ExecuteMigrationIdempotent(
            IDatabaseConnector connector,
            Version toVersion,
            string msSqlAlter,
            string[] mySqlAlters)
        {
            using DbTransaction sqlTran = connector.DbConnection().BeginTransaction(IsolationLevel.Serializable);
            DbCommand dbCommand;
            if (connector is MSSqlDatabaseConnector or OdbcDatabaseConnector)
            {
                dbCommand = connector.DbCommand(MakeMssqlColumnAddsIdempotent(msSqlAlter));
                dbCommand.Transaction = sqlTran;
                dbCommand.ExecuteNonQuery();
                dbCommand = connector.DbCommand(MsSqlVersionUpdate);
                dbCommand.Transaction = sqlTran;
            }
            else if (connector is MySqlDatabaseConnector)
            {
                foreach (string alterSql in mySqlAlters)
                {
                    try
                    {
                        dbCommand = connector.DbCommand(alterSql);
                        dbCommand.Transaction = sqlTran;
                        dbCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex) when (ex.Message.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase))
                    {
                        // Column already exists -- safe to ignore
                    }
                }

                dbCommand = connector.DbCommand(MySqlVersionUpdate);
                dbCommand.Transaction = sqlTran;
            }
            else
            {
                throw new NotSupportedException("Unknown database back-end");
            }

            DbParameter pConfVersion = dbCommand.CreateParameter();
            pConfVersion.ParameterName = "confVersion";
            pConfVersion.Value = toVersion.ToString();
            pConfVersion.DbType = DbType.String;
            pConfVersion.Direction = ParameterDirection.Input;
            dbCommand.Parameters.Add(pConfVersion);

            dbCommand.ExecuteNonQuery();
            sqlTran.Commit();
        }

        /// <summary>
        /// Guards each "ALTER TABLE tblCons ADD &lt;column&gt; ..." in an MS-SQL migration batch
        /// with "IF COL_LENGTH('tblCons','&lt;column&gt;') IS NULL" so the statement is skipped when
        /// the column already exists. The fork's generic schema forward-port (UpgradeMssqlSchema)
        /// adds current-schema columns ahead of the versioned upgraders, so without this guard the
        /// upgraders re-add the same columns and SQL Server throws error 2705 ("Column names in
        /// each table must be unique. ... is specified more than once") when importing an older
        /// database. Constraint/index/key ADDs are left untouched. (#113)
        /// </summary>
        private static string MakeMssqlColumnAddsIdempotent(string msSqlAlter)
        {
            if (string.IsNullOrEmpty(msSqlAlter))
                return msSqlAlter;

            return Regex.Replace(
                msSqlAlter,
                @"ALTER\s+TABLE\s+tblCons\s+ADD\s+(?!(?:CONSTRAINT|PRIMARY|FOREIGN|UNIQUE|INDEX|CHECK|COLUMN)\b)(\w+)",
                "IF COL_LENGTH('tblCons','$1') IS NULL ALTER TABLE tblCons ADD $1",
                RegexOptions.IgnoreCase);
        }
    }
}

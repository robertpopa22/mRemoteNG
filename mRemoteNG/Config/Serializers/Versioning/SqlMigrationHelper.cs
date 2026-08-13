using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    internal static class SqlMigrationHelper
    {
        private const string MsSqlVersionUpdate = "UPDATE tblRoot SET ConfVersion=@confVersion;";
        private const string MySqlVersionUpdate = "UPDATE tblRoot SET ConfVersion=?;";

        /// <summary>
        /// True when a failed schema statement means "this change is already in place". The two
        /// oldest upgraders re-apply changes the schema forward-port has usually already made, so
        /// this is their expected outcome; anything else is not.
        /// </summary>
        internal static bool IsSchemaAlreadyApplied(Exception ex)
        {
            if (ex == null)
                return false;

            string message = ex.Message ?? "";

            // Matched on text rather than provider error numbers because the same failure arrives
            // as SqlException 2705, MySqlException 1060 or an OdbcException wrapping the SQL
            // Server text, depending on which connector the profile uses.
            return message.Contains("Duplicate column", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("must be unique", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("already has a primary key", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("there is already an object named", StringComparison.OrdinalIgnoreCase)
                   || message.Contains("already exists", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Records a schema statement that was skipped after failing. The legacy upgraders have
        /// always swallowed these, which is right for the redundant case and silent for every
        /// other one -- an unexplained upgrade needed source reading to investigate because
        /// nothing was written down. The behaviour is unchanged; the failure is now on the record,
        /// and flagged when it is not the expected redundant-statement error. (#148, #165)
        /// </summary>
        internal static void ReportSkippedStatement(Exception ex, string step)
        {
            if (ex == null)
                return;

            bool expected = IsSchemaAlreadyApplied(ex);
            Runtime.MessageCollector?.AddMessage(
                expected ? MessageClass.DebugMsg : MessageClass.WarningMsg,
                string.Format(CultureInfo.InvariantCulture,
                              "Schema upgrade {0}: statement skipped after {1}: {2}{3}",
                              step,
                              ex.GetType().Name,
                              ex.Message,
                              expected
                                  ? " (change already applied -- expected)"
                                  : " -- this is NOT a duplicate-object error, so the upgrade may be incomplete"),
                true);
        }

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
            MySqlSafeUpdatesScope? safeUpdatesScope = null;
            try
            {
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
                    safeUpdatesScope = new MySqlSafeUpdatesScope(connector, sqlTran);

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
            }
            finally
            {
                safeUpdatesScope?.Dispose();
            }

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
            MySqlSafeUpdatesScope? safeUpdatesScope = null;
            try
            {
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
                    safeUpdatesScope = new MySqlSafeUpdatesScope(connector, sqlTran);

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
            }
            finally
            {
                safeUpdatesScope?.Dispose();
            }

            sqlTran.Commit();
        }

        /// <summary>
        /// Turns MySQL/MariaDB "safe updates" mode off for the duration of a migration and puts it
        /// back to whatever the session had before, instead of unconditionally forcing it back on.
        ///
        /// The migrations issue keyless UPDATEs (e.g. "UPDATE tblCons SET x = 0 WHERE x IS NULL"),
        /// which safe-updates mode rejects, so it has to be disabled while they run. The old code
        /// ended every MySQL migration with a hard-coded "SET SQL_SAFE_UPDATES=1". For anyone whose
        /// server/session default is 0 that silently *enabled* the mode, and because MySql.Data pools
        /// connections without resetting session state, the forced value outlived the migration on
        /// that physical connection and made later keyless statements fail with error 1175. (#145)
        /// </summary>
        private sealed class MySqlSafeUpdatesScope : IDisposable
        {
            private readonly IDatabaseConnector _connector;
            private readonly DbTransaction _transaction;
            private readonly bool _originalValue;
            private bool _disposed;

            public MySqlSafeUpdatesScope(IDatabaseConnector connector, DbTransaction transaction)
            {
                _connector = connector;
                _transaction = transaction;

                using DbCommand readCommand = connector.DbCommand("SELECT @@SESSION.sql_safe_updates;");
                readCommand.Transaction = transaction;
                _originalValue = Convert.ToBoolean(readCommand.ExecuteScalar(), CultureInfo.InvariantCulture);

                SetValue(false);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                if (!_originalValue)
                    return;

                try
                {
                    SetValue(true);
                }
                catch (DbException)
                {
                    // Best effort: the migration itself already failed hard enough to take the
                    // connection with it, and masking that exception would hide the real cause.
                }
            }

            private void SetValue(bool enabled)
            {
                using DbCommand command = _connector.DbCommand(
                    enabled ? "SET SESSION sql_safe_updates=1;" : "SET SESSION sql_safe_updates=0;");
                command.Transaction = _transaction;
                command.ExecuteNonQuery();
            }
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

using System;
using System.Data.Common;
using System.Globalization;
using System.Runtime.Versioning;
using mRemoteNG.Config.DatabaseConnectors;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Sql
{
    /// <summary>
    /// Turns MySQL/MariaDB safe-update mode off for one operation and puts it back exactly as it
    /// was found.
    ///
    /// Safe-update mode refuses any DELETE or UPDATE whose plan is a full table scan. That check is
    /// made by the planner, not the parser, so a LIMIT clause does not satisfy it — verified
    /// against MariaDB 10.11: `DELETE FROM t LIMIT 1` is rejected with error 1175 both with and
    /// without a primary key on the table. Every earlier attempt to work around this with a
    /// limited delete was therefore aimed at MySQL's documented behaviour, not MariaDB's actual
    /// behaviour, and could never have worked on the reporter's server. (#148)
    ///
    /// The value is read back before it is changed, and restored on dispose, because MySql.Data
    /// pools connections without a session reset: a variable left flipped here would silently
    /// weaken safety for whatever code borrows that connection next.
    ///
    /// This was previously a private nested class inside SqlMigrationHelper, which is why only the
    /// migration path was protected while every ordinary save still failed.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal sealed class MySqlSafeUpdatesScope : IDisposable
    {
        private readonly IDatabaseConnector _connector;
        private readonly DbTransaction? _transaction;
        private readonly bool _originalValue;
        private bool _disposed;

        internal MySqlSafeUpdatesScope(IDatabaseConnector connector, DbTransaction? transaction)
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
                // Best effort: if restoring fails the connection is already broken, and throwing
                // here would mask whatever actually went wrong.
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
}

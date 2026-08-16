using System.Data.Common;
using mRemoteNG.Config.DatabaseConnectors;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Sql
{
    internal static class SqlSafeUpdateHelper
    {
        /// <summary>
        /// Empties a table, surviving MySQL/MariaDB safe-update mode.
        ///
        /// The previous approach issued "DELETE ... LIMIT 1" in a loop, on the assumption that a
        /// LIMIT satisfies safe-update mode. It does not on MariaDB: the check is made by the
        /// planner and rejects any full-scan DELETE, so error 1175 came back regardless — verified
        /// on MariaDB 10.11 both with and without a primary key. Every save against a MariaDB with
        /// safe updates enabled therefore failed, which is what #148 reported.
        ///
        /// TRUNCATE and adding a key were both considered and rejected: they are DDL, so they
        /// commit the surrounding transaction implicitly and would break the save's atomicity.
        /// Suspending the mode for the statement and restoring it afterwards is the only option
        /// that leaves both the transaction and the connection's session state intact. (#148)
        /// </summary>
        internal static void DeleteAllRows(
            IDatabaseConnector databaseConnector,
            DbTransaction? transaction,
            string deleteAllCommandText)
        {
            if (databaseConnector is MySqlDatabaseConnector)
            {
                using MySqlSafeUpdatesScope scope = new(databaseConnector, transaction);
                Execute(databaseConnector, transaction, deleteAllCommandText);
                return;
            }

            Execute(databaseConnector, transaction, deleteAllCommandText);
        }

        private static void Execute(
            IDatabaseConnector databaseConnector,
            DbTransaction? transaction,
            string commandText)
        {
            using DbCommand command = databaseConnector.DbCommand(commandText);
            command.Transaction = transaction;
            SqlCommandDiagnostics.ExecuteNonQuery(command, "DeleteAllRows");
        }
    }
}

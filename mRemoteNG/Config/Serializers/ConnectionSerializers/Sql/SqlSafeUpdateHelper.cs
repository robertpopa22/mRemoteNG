using System.Data.Common;
using mRemoteNG.Config.DatabaseConnectors;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Sql
{
    internal static class SqlSafeUpdateHelper
    {
        internal static void DeleteAllRows(
            IDatabaseConnector databaseConnector,
            DbTransaction? transaction,
            string deleteAllCommandText,
            string deleteSingleCommandText)
        {
            bool useLimitedDelete = databaseConnector is MySqlDatabaseConnector;
            int deletedRows;

            do
            {
                using DbCommand command = databaseConnector.DbCommand(
                    useLimitedDelete ? deleteSingleCommandText : deleteAllCommandText);
                command.Transaction = transaction;
                deletedRows = command.ExecuteNonQuery();
            }
            while (useLimitedDelete && deletedRows > 0);
        }
    }
}

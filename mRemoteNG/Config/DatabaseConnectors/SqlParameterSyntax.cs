using System.Data.Common;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.DatabaseConnectors
{
    /// <summary>
    /// Bridges the one syntax difference that matters when the same SQL is sent through different
    /// connectors: Microsoft.Data.SqlClient and MySql.Data bind parameters by NAME (<c>@name</c>),
    /// while ODBC binds them by POSITION and only understands <c>?</c>.
    ///
    /// Everything else in this application treats an ODBC profile as SQL Server, and correctly so —
    /// it is SQL Server, reached through a different driver. But statements written with named
    /// markers and sent down the shared path failed on ODBC with "Must declare the scalar variable",
    /// which meant a user on an ODBC profile could not save at all: the metadata write, the schema
    /// version update and the update-marker insert all used named parameters.
    ///
    /// Positional binding has one hard rule: parameters must be added in the order their markers
    /// appear in the statement. AddParameter enforces that by construction.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class SqlParameterSyntax
    {
        /// <summary>True when the connector binds parameters by position rather than by name.</summary>
        internal static bool IsPositional(IDatabaseConnector connector) =>
            connector is OdbcDatabaseConnector;

        /// <summary>
        /// The marker to write into a statement for a given logical parameter name.
        /// </summary>
        internal static string Marker(IDatabaseConnector connector, string name) =>
            IsPositional(connector) ? "?" : "@" + name;

        /// <summary>
        /// Adds a parameter using the naming the connector expects. Call in the same order as the
        /// markers appear in the statement — under positional binding the name is ignored and the
        /// order is the only thing that matters.
        /// </summary>
        internal static DbParameter AddParameter(IDatabaseConnector connector, DbCommand command,
                                                 string name, object? value,
                                                 System.Data.DbType? dbType = null, int? size = null)
        {
            DbParameter parameter = command.CreateParameter();

            // ODBC accepts a name but ignores it; keeping one aids debugging without changing binding.
            parameter.ParameterName = IsPositional(connector) ? name : "@" + name;

            if (dbType.HasValue)
                parameter.DbType = dbType.Value;
            if (size.HasValue)
                parameter.Size = size.Value;

            parameter.Value = value ?? System.DBNull.Value;
            parameter.Direction = System.Data.ParameterDirection.Input;
            command.Parameters.Add(parameter);
            return parameter;
        }
    }
}

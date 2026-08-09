using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Reflection;
using System.Text;
using mRemoteNG.App;
using mRemoteNG.Messages;

namespace mRemoteNG.Config.Serializers.ConnectionSerializers.Sql
{
    /// <summary>
    /// Records which SQL statement a failed save came from and renders it as a redacted
    /// diagnostic block.
    ///
    /// A database error surfaced only as its provider message ("You are using safe update mode
    /// and you tried to update a table without a WHERE that uses a KEY column"), with no way to
    /// tell which statement produced it — the stack trace stops at the calling method, and a
    /// single save issues several statements across several tables. That made such reports
    /// effectively unreproducible without asking the user to disable safe-update mode. (#148)
    ///
    /// Parameter *values* are never recorded: they carry connection names, hostnames, usernames
    /// and encrypted secrets. Only the parameter name, its declared type, and whether a value was
    /// present are written out.
    /// </summary>
    internal static class SqlCommandDiagnostics
    {
        private const string OperationKey = "mRemoteNG.Sql.Operation";
        private const string CommandTextKey = "mRemoteNG.Sql.CommandText";
        private const string ParametersKey = "mRemoteNG.Sql.Parameters";

        /// <summary>
        /// Runs a command, tagging the exception with the statement that failed.
        /// </summary>
        internal static int ExecuteNonQuery(DbCommand command, string operation)
        {
            try
            {
                return command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Annotate(ex, command, operation);
                throw;
            }
        }

        /// <summary>
        /// DbDataAdapter handler. The adapter builds its own INSERT/UPDATE/DELETE from a
        /// CommandBuilder, so without this the row error carries no statement at all. With
        /// ContinueUpdateOnError left at false the adapter raises this event with e.Errors set and
        /// then throws that same exception instance, so annotating it here reaches the caller.
        ///
        /// Note: the MySQL path sets UpdateBatchSize, so for a batched update the captured
        /// statement identifies the batch rather than the individual row that failed. That is
        /// still the statement shape the server rejected, which is what the diagnostics are for.
        /// </summary>
        internal static void OnRowUpdated(object? sender, RowUpdatedEventArgs e)
        {
            if (e.Errors == null)
                return;

            Annotate(e.Errors, e.Command as DbCommand,
                FormattableString.Invariant($"tblCons {e.StatementType}"));
        }

        /// <summary>
        /// Attaches the failing statement to an exception. The first (innermost) annotation wins,
        /// so an outer handler re-annotating a bubbling exception cannot overwrite the real
        /// culprit with a more generic description.
        /// </summary>
        internal static void Annotate(Exception? ex, DbCommand? command, string operation)
        {
            if (ex == null || command == null)
                return;

            try
            {
                if (ex.Data.Contains(CommandTextKey))
                    return;

                ex.Data[OperationKey] = operation;
                ex.Data[CommandTextKey] = command.CommandText;
                ex.Data[ParametersKey] = DescribeParameters(command);
            }
            catch (Exception)
            {
                // Exception.Data can refuse writes in more than one way: the usual backing store
                // throws ArgumentException on a bad key, pre-allocated/agile exceptions expose a
                // read-only dictionary that throws InvalidOperationException, and a custom
                // IDictionary facade can throw NotSupportedException. This runs inside a caller's
                // catch block and inside the adapter's RowUpdated handler, so it must never throw
                // and replace the database exception the caller is about to see.
            }
        }

        /// <summary>
        /// Writes the diagnostics for a failed save as a debug message, so it lands in the log
        /// only when debug logging is enabled.
        /// </summary>
        internal static void LogFailure(Exception ex, string operation, bool rolledBack)
        {
            try
            {
                string? report = Describe(ex, operation, rolledBack);
                if (report != null)
                    Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg, report, true);
            }
            catch (Exception diagnosticsEx)
            {
                // This runs inside the save's catch block. Building the report must never
                // replace the database exception the caller is about to see.
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    FormattableString.Invariant($"Failed to build SQL diagnostics: {diagnosticsEx.Message}"), true);
            }
        }

        /// <summary>
        /// Renders the redacted diagnostic block, or null when nothing useful was recorded.
        /// </summary>
        internal static string? Describe(Exception ex, string operation, bool rolledBack)
        {
            if (ex == null)
                return null;

            StringBuilder sb = new();
            sb.AppendLine("Database command failed.");
            sb.AppendLine(FormattableString.Invariant($"Operation: {ex.Data[OperationKey] as string ?? operation}"));

            if (ex.Data[CommandTextKey] is string commandText && !string.IsNullOrWhiteSpace(commandText))
            {
                sb.AppendLine("SQL:");
                sb.AppendLine(commandText.Trim());
                sb.AppendLine(FormattableString.Invariant($"Statement type: {StatementType(commandText)}"));
            }
            else
            {
                sb.AppendLine("SQL: <not captured — the failure did not come from a tracked statement>");
            }

            if (ex.Data[ParametersKey] is string parameters && parameters.Length > 0)
            {
                sb.AppendLine("Parameters (names and types only; values withheld):");
                sb.AppendLine(parameters);
            }

            string? errorNumber = ErrorNumber(ex);
            if (errorNumber != null)
                sb.AppendLine(FormattableString.Invariant($"Provider error number: {errorNumber}"));

            sb.AppendLine(FormattableString.Invariant($"Provider error: {ex.GetType().Name}: {ex.Message}"));
            sb.AppendLine(FormattableString.Invariant($"Transaction rolled back: {(rolledBack ? "yes" : "no")}"));
            return sb.ToString();
        }

        private static string DescribeParameters(DbCommand command)
        {
            if (command.Parameters.Count == 0)
                return "";

            StringBuilder sb = new();
            foreach (DbParameter parameter in command.Parameters)
            {
                bool hasValue = parameter.Value != null && parameter.Value != DBNull.Value;
                sb.AppendLine(FormattableString.Invariant(
                    $"  {parameter.ParameterName} ({parameter.DbType}) = {(hasValue ? "<set>" : "<null>")}"));
            }

            return sb.ToString().TrimEnd();
        }

        private static string StatementType(string commandText)
        {
            string trimmed = commandText.TrimStart();
            foreach (string keyword in new[] { "SELECT", "INSERT", "UPDATE", "DELETE", "ALTER", "SET" })
            {
                if (trimmed.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
                    return keyword;
            }

            return "OTHER";
        }

        /// <summary>
        /// Provider-specific error numbers (MySQL 1175 for safe-update mode, SQL Server error
        /// numbers) live on a "Number" property rather than anywhere in the DbException contract.
        /// Read it without taking a hard dependency on either provider type.
        /// </summary>
        private static string? ErrorNumber(Exception ex)
        {
            // Reflection here runs while we are already handling a failure, so nothing it does
            // may throw: a derived type that shadows "Number" makes GetProperty ambiguous, and
            // the getter itself belongs to a third-party provider.
            object? number = null;
            try
            {
                number = ex.GetType().GetProperty("Number")?.GetValue(ex);
            }
            catch (AmbiguousMatchException)
            {
                // Provider type shadows the property — fall through to SQLSTATE.
            }
            catch (Exception)
            {
                // A provider getter threw; a diagnostic detail is not worth propagating.
            }

            if (number != null)
                return Convert.ToString(number, CultureInfo.InvariantCulture);

            if (ex is DbException dbException && dbException.SqlState != null)
                return FormattableString.Invariant($"SQLSTATE {dbException.SqlState}");

            return null;
        }
    }
}

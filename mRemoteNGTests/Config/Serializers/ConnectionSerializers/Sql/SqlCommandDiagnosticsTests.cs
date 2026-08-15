using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using NSubstitute;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Sql;

public class SqlCommandDiagnosticsTests
{
    private static DbCommand BuildCommand(string commandText, params (string Name, DbType Type, object? Value)[] parameters)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText.Returns(commandText);

        var collection = new FakeParameterCollection();
        foreach ((string name, DbType type, object? value) in parameters)
        {
            var parameter = Substitute.For<DbParameter>();
            parameter.ParameterName.Returns(name);
            parameter.DbType.Returns(type);
            parameter.Value.Returns(value);
            collection.Add(parameter);
        }

        command.Parameters.Returns(collection);
        return command;
    }

    [Test]
    public void Describe_ReportsOperationStatementAndRollback()
    {
        var ex = new InvalidOperationException("safe update mode");
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("DELETE FROM tblRoot LIMIT 1"), "DeleteAllRows");

        string? report = SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true);

        Assert.That(report, Does.Contain("Operation: DeleteAllRows"));
        Assert.That(report, Does.Contain("DELETE FROM tblRoot LIMIT 1"));
        Assert.That(report, Does.Contain("Statement type: DELETE"));
        Assert.That(report, Does.Contain("Transaction rolled back: yes"));
        Assert.That(report, Does.Contain("safe update mode"));
    }

    [Test]
    public void Describe_NeverLeaksParameterValues()
    {
        var ex = new InvalidOperationException("boom");
        SqlCommandDiagnostics.Annotate(
            ex,
            BuildCommand("INSERT INTO tblRoot (Name, Protected) VALUES(@Name, @Protected)",
                ("@Name", DbType.String, "super-secret-hostname"),
                ("@Protected", DbType.String, "ThisIsProtected-ciphertext")),
            "WriteDatabaseMetaData");

        string? report = SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true);

        // Names and declared types are diagnostic; the values are connection data and secrets.
        Assert.That(report, Does.Contain("@Name (String) = <set>"));
        Assert.That(report, Does.Contain("@Protected (String) = <set>"));
        Assert.That(report, Does.Not.Contain("super-secret-hostname"));
        Assert.That(report, Does.Not.Contain("ThisIsProtected-ciphertext"));
    }

    [Test]
    public void Describe_NeverLeaksCredentialParameterValues()
    {
        var ex = new InvalidOperationException("boom");
        SqlCommandDiagnostics.Annotate(
            ex,
            BuildCommand("UPDATE tblCons SET Password=@Password, RDGatewayPassword=@RDGatewayPassword, Hostname=@Hostname WHERE ConstantID=@ConstantID",
                ("@Password", DbType.String, "hunter2"),
                ("@RDGatewayPassword", DbType.String, "gateway-pa55"),
                ("@Hostname", DbType.String, "dc01.internal.corp"),
                ("@ConstantID", DbType.String, "5a1eacc7-c14b-4c58-9915-85be7ab805fb")),
            "tblCons Update");

        string? report = SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true);

        Assert.Multiple(() =>
        {
            Assert.That(report, Does.Not.Contain("hunter2"));
            Assert.That(report, Does.Not.Contain("gateway-pa55"));
            Assert.That(report, Does.Not.Contain("dc01.internal.corp"));
            Assert.That(report, Does.Not.Contain("5a1eacc7-c14b-4c58-9915-85be7ab805fb"));
            // The parameter names still have to be there — that is the diagnostic value.
            Assert.That(report, Does.Contain("@Password (String) = <set>"));
            Assert.That(report, Does.Contain("@RDGatewayPassword (String) = <set>"));
        });
    }

    [Test]
    public void Annotate_DoesNotThrowWhenExceptionDataRejectsWrites()
    {
        // Annotate runs inside the caller's catch block and inside the adapter's RowUpdated
        // handler. If it throws, it replaces the database exception the caller was about to
        // see. Exception.Data is read-only on pre-allocated/agile exceptions.
        Assert.DoesNotThrow(() =>
            SqlCommandDiagnostics.Annotate(new HostileException(), BuildCommand("DELETE FROM tblRoot"), "DeleteAllRows"));
    }

    [Test]
    public void ExecuteNonQuery_StillRethrowsTheOriginalWhenAnnotationFails()
    {
        var command = BuildCommand("DELETE FROM tblRoot");
        var original = new HostileException();
        command.When(c => c.ExecuteNonQuery()).Do(_ => throw original);

        var thrown = Assert.Throws<HostileException>(() =>
            SqlCommandDiagnostics.ExecuteNonQuery(command, "DeleteAllRows"));

        Assert.That(thrown, Is.SameAs(original));
    }

    [Test]
    public void OnRowUpdated_DoesNotThrowWhenAnnotationFails()
    {
        var args = new RowUpdatedEventArgs(new DataTable("tblCons").NewRow(),
            BuildCommand("UPDATE `tblCons` SET `Name`=@Name"), StatementType.Update, new DataTableMapping())
        {
            Errors = new HostileException()
        };

        Assert.DoesNotThrow(() => SqlCommandDiagnostics.OnRowUpdated(null, args));
    }

    [Test]
    public void LogFailure_DoesNotThrowWhenExceptionDataIsUnusable()
    {
        // LogFailure runs inside the save's catch block; it must never replace the real
        // database exception with one of its own.
        Assert.DoesNotThrow(() =>
            SqlCommandDiagnostics.LogFailure(new HostileException(), "SqlConnectionsSaver.Save", rolledBack: true));
    }

    [Test]
    public void Describe_SurvivesAProviderThatShadowsNumber()
    {
        var ex = new ShadowedNumberException("boom");
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("DELETE FROM tblRoot"), "DeleteAllRows");

        Assert.DoesNotThrow(() => SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true));
    }

    [Test]
    public void Describe_MarksNullParametersWithoutClaimingAValue()
    {
        var ex = new InvalidOperationException("boom");
        SqlCommandDiagnostics.Annotate(
            ex,
            BuildCommand("UPDATE tblCons SET Name=@Name", ("@Name", DbType.String, DBNull.Value)),
            "tblCons Update");

        Assert.That(SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: false),
            Does.Contain("@Name (String) = <null>"));
    }

    [Test]
    public void Describe_ReportsWhenRollbackDidNotHappen()
    {
        var ex = new InvalidOperationException("boom");
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("DELETE FROM tblUpdate LIMIT 1"), "DeleteAllRows");

        Assert.That(SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: false),
            Does.Contain("Transaction rolled back: no"));
    }

    [Test]
    public void Annotate_KeepsTheInnermostStatement()
    {
        var ex = new InvalidOperationException("boom");
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("DELETE FROM tblRoot LIMIT 1"), "DeleteAllRows");
        // An outer handler must not overwrite the statement that actually failed.
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("INSERT INTO tblUpdate (LastUpdate) VALUES(@x)"), "UpdateUpdatesTable");

        string? report = SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true);

        Assert.That(report, Does.Contain("DELETE FROM tblRoot LIMIT 1"));
        Assert.That(report, Does.Not.Contain("INSERT INTO tblUpdate"));
    }

    [Test]
    public void OnRowUpdated_AnnotatesTheAdapterGeneratedStatement()
    {
        // The adapter builds its own UPDATE, so this handler is the only place that statement
        // can be captured. With ContinueUpdateOnError false the adapter raises this event with
        // e.Errors set and then throws that same instance.
        var ex = new InvalidOperationException("safe update mode");
        var command = BuildCommand("UPDATE `tblCons` SET `Name`=@Name WHERE `ConstantID`=@ConstantID",
            ("@Name", DbType.String, "secret-box"));
        var row = new DataTable("tblCons").NewRow();
        var args = new RowUpdatedEventArgs(row, command, StatementType.Update, new DataTableMapping())
        {
            Errors = ex
        };

        SqlCommandDiagnostics.OnRowUpdated(null, args);

        string? report = SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true);
        Assert.That(report, Does.Contain("Operation: tblCons Update"));
        Assert.That(report, Does.Contain("UPDATE `tblCons` SET"));
        Assert.That(report, Does.Not.Contain("secret-box"));
    }

    [Test]
    public void OnRowUpdated_IgnoresRowsThatSucceeded()
    {
        var command = BuildCommand("UPDATE `tblCons` SET `Name`=@Name");
        var row = new DataTable("tblCons").NewRow();
        var args = new RowUpdatedEventArgs(row, command, StatementType.Update, new DataTableMapping());

        Assert.DoesNotThrow(() => SqlCommandDiagnostics.OnRowUpdated(null, args));
    }

    [Test]
    public void Describe_FallsBackWhenNoStatementWasCaptured()
    {
        var ex = new InvalidOperationException("boom");

        string? report = SqlCommandDiagnostics.Describe(ex, "SqlConnectionsSaver.Save", rolledBack: true);

        Assert.That(report, Does.Contain("Operation: SqlConnectionsSaver.Save"));
        Assert.That(report, Does.Contain("not captured"));
    }

    [Test]
    public void Describe_ReportsProviderErrorNumber()
    {
        var ex = new FakeProviderException("You are using safe update mode", 1175);
        SqlCommandDiagnostics.Annotate(ex, BuildCommand("DELETE FROM tblRoot"), "DeleteAllRows");

        Assert.That(SqlCommandDiagnostics.Describe(ex, "fallback", rolledBack: true),
            Does.Contain("Provider error number: 1175"));
    }

    [Test]
    public void ExecuteNonQuery_AnnotatesAndRethrows()
    {
        var command = BuildCommand("DELETE FROM tblRoot");
        command.When(c => c.ExecuteNonQuery()).Do(_ => throw new InvalidOperationException("boom"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            SqlCommandDiagnostics.ExecuteNonQuery(command, "DeleteAllRows"));

        Assert.That(SqlCommandDiagnostics.Describe(ex!, "fallback", rolledBack: true),
            Does.Contain("DELETE FROM tblRoot"));
    }

    [Test]
    public void ExecuteNonQuery_ReturnsAffectedRowsWhenItSucceeds()
    {
        var command = BuildCommand("DELETE FROM tblRoot");
        command.ExecuteNonQuery().Returns(3);

        Assert.That(SqlCommandDiagnostics.ExecuteNonQuery(command, "DeleteAllRows"), Is.EqualTo(3));
    }

    [Test]
    public void Annotate_IgnoresNullExceptionOrCommand()
    {
        Assert.DoesNotThrow(() => SqlCommandDiagnostics.Annotate(null, BuildCommand("SELECT 1"), "op"));
        Assert.DoesNotThrow(() => SqlCommandDiagnostics.Annotate(new InvalidOperationException(), null, "op"));
    }

    private sealed class FakeProviderException(string message, int number) : Exception(message)
    {
        public int Number { get; } = number;
    }

    private class BaseNumberException(string message) : Exception(message)
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static",
                         Justification = "Must stay an instance property: the diagnostics look it up "
                                         + "with Type.GetProperty(\"Number\"), which is what this "
                                         + "fixture exercises.")]
        public int Number => 1;
    }

    /// <summary>Shadows Number, which makes an unguarded Type.GetProperty("Number") ambiguous.</summary>
    private sealed class ShadowedNumberException(string message) : BaseNumberException(message)
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static",
                         Justification = "Must stay an instance property to shadow the base member "
                                         + "and reproduce the ambiguous-lookup case.")]
        public new string Number => "1175";
    }

    /// <summary>Exception whose Data throws, exercising the diagnostics' own failure path.</summary>
    private sealed class HostileException : Exception
    {
        public override System.Collections.IDictionary Data => throw new InvalidOperationException("no data for you");
    }

    private sealed class FakeParameterCollection : DbParameterCollection
    {
        private readonly System.Collections.Generic.List<DbParameter> _parameters = [];

        public override int Count => _parameters.Count;
        public override object SyncRoot => this;
        public override int Add(object value) { _parameters.Add((DbParameter)value); return _parameters.Count - 1; }
        public override void AddRange(Array values) { foreach (object v in values) Add(v); }
        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => value is DbParameter p && _parameters.Contains(p);
        public override bool Contains(string value) => IndexOf(value) >= 0;
        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_parameters).CopyTo(array, index);
        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);
        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(p => string.Equals(p.ParameterName, parameterName, StringComparison.Ordinal));
        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);
        public override void Remove(object value) => _parameters.Remove((DbParameter)value);
        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName) => _parameters.RemoveAt(IndexOf(parameterName));
        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName) => _parameters[IndexOf(parameterName)];
        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;
        protected override void SetParameter(string parameterName, DbParameter value) => _parameters[IndexOf(parameterName)] = value;
    }

    /// <summary>Exception whose Number getter throws, as a third-party provider's can.</summary>
    private sealed class ThrowingNumberException(string message) : Exception(message)
    {
        public static int Number => throw new InvalidOperationException("provider getter failed");
    }

    /// <summary>A DbException carrying only a SQLSTATE, which is the fallback identifier.</summary>
    private sealed class SqlStateOnlyException(string message, string sqlState) : DbException(message)
    {
        public override string SqlState { get; } = sqlState;
    }

    [Test]
    public void DescribingANullExceptionYieldsNothing()
    {
        Assert.That(SqlCommandDiagnostics.Describe(null, "Save", rolledBack: true), Is.Null);
    }

    [Test]
    public void LoggingAFailureDoesNotThrow()
    {
        // LogFailure runs inside the save's catch block: building a diagnostic must never replace
        // the database exception the caller is about to surface.
        Assert.DoesNotThrow(() => SqlCommandDiagnostics.LogFailure(
            new FakeProviderException("Duplicate column name 'x'", 1060), "SqlConnectionsSaver.Save", true));
    }

    [Test]
    public void LoggingANullExceptionDoesNotThrow()
    {
        Assert.DoesNotThrow(() => SqlCommandDiagnostics.LogFailure(null, "Save", false));
    }

    [Test]
    public void AProviderGetterThatThrowsFallsBackToSqlState()
    {
        // Reflection over "Number" happens while a failure is already being handled, so a getter
        // that throws must degrade to SQLSTATE rather than escape.
        SqlCommandDiagnostics.Annotate(new ThrowingNumberException("boom"),
                                       BuildCommand("SELECT 1"), "Save");
        Assert.DoesNotThrow(() => SqlCommandDiagnostics.Describe(
            new ThrowingNumberException("boom"), "Save", rolledBack: false));
    }

    [Test]
    public void AnExceptionWithOnlySqlStateIsIdentifiedByIt()
    {
        string? report = SqlCommandDiagnostics.Describe(
            new SqlStateOnlyException("connection dropped", "08S01"), "Save", rolledBack: true);

        Assert.That(report, Does.Contain("08S01"));
    }

    [TestCase("EXEC sp_who")]
    [TestCase("CREATE DATABASE mRemoteNG")]
    [TestCase("")]
    public void ACommandThatIsNotOneOfTheKnownVerbsIsReportedAsOther(string commandText)
    {
        SqlCommandDiagnostics.Annotate(new FakeProviderException("failed", 1),
                                       BuildCommand(commandText), "Save");
        string? report = SqlCommandDiagnostics.Describe(
            new FakeProviderException("failed", 1), "Save", rolledBack: false);

        Assert.That(report, Is.Not.Null);
    }
}

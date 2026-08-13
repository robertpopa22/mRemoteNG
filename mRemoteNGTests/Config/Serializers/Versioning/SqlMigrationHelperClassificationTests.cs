using System;
using mRemoteNG.Config.Serializers.Versioning;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.Versioning;

/// <summary>
/// The two oldest schema upgraders re-apply changes the forward-port has usually already made and
/// swallow the resulting failure. That is correct for a duplicate-object error and wrong to do
/// silently for anything else, so the failure is classified before being written to the log.
/// Classification is by message text, because the same condition arrives as a SqlException, a
/// MySqlException or an OdbcException depending on the profile's connector. (#148, #165)
/// </summary>
public class SqlMigrationHelperClassificationTests
{
    private sealed class FakeDbException(string message) : Exception(message);

    [TestCase("Column names in each table must be unique. Column name 'RedirectClipboard' in table 'tblCons' is specified more than once.")]
    [TestCase("Duplicate column name 'RedirectClipboard'")]
    [TestCase("ERROR [42S21] [Microsoft][ODBC Driver 17 for SQL Server][SQL Server]Column names in each table must be unique.")]
    [TestCase("Table 'tblCons' already has a primary key defined on it.")]
    [TestCase("There is already an object named 'PK_tblCons' in the database.")]
    public void RedundantSchemaStatementsAreRecognised(string message)
    {
        Assert.That(SqlMigrationHelper.IsSchemaAlreadyApplied(new FakeDbException(message)), Is.True);
    }

    [TestCase("The ALTER TABLE permission was denied on object 'tblCons'")]
    [TestCase("A network-related or instance-specific error occurred while establishing a connection to SQL Server.")]
    [TestCase("ALTER TABLE only allows columns to be added that can contain nulls, or have a DEFAULT definition specified")]
    [TestCase("Incorrect syntax near 'tblCons'.")]
    [TestCase("Login failed for user 'sa'.")]
    public void RealFailuresAreNotMistakenForRedundantStatements(string message)
    {
        Assert.That(SqlMigrationHelper.IsSchemaAlreadyApplied(new FakeDbException(message)), Is.False);
    }

    [Test]
    public void ANullExceptionIsNotTreatedAsRedundant()
    {
        Assert.That(SqlMigrationHelper.IsSchemaAlreadyApplied(null), Is.False);
    }

    [Test]
    public void ReportingANullExceptionIsHarmless()
    {
        Assert.DoesNotThrow(() => SqlMigrationHelper.ReportSkippedStatement(null, "2.6 -> 2.7"));
    }
}

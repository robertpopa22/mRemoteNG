using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    /// <summary>
    /// Adds the Notes column, which the SQL backend never had.
    ///
    /// Notes is an ordinary property-grid field: it round-trips through XML and CSV and can be
    /// expanded into an external tool's arguments as %Notes%. On a SQL profile it had no column, no
    /// write and no read, so every note a user typed was discarded on save without any error. Found
    /// by the reflection round-trip oracle, which compares every persisted property instead of the
    /// handful a hand-written test remembers to name.
    ///
    /// Notes is free-form and multiline, so it gets the widest text type rather than the varchar
    /// most string columns use — a note is exactly the kind of value that overflows a fixed width,
    /// and a truncating save would be the same silent loss in a smaller form.
    ///
    /// The column is nullable with no default, so existing rows read back as NULL rather than "".
    /// DataTableSerializer.NullableTextEquals exists because of that: without it, the first save
    /// after this upgrade would consider every connection changed and rewrite the whole table.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class SqlVersion35To36Upgrader(IDatabaseConnector databaseConnector) : IVersionUpgrader
    {
        private readonly Version _version = new(3, 6);
        private readonly IDatabaseConnector _databaseConnector = databaseConnector ?? throw new ArgumentNullException(nameof(databaseConnector));

        public bool CanUpgrade(Version currentVersion)
        {
            return currentVersion == new Version(3, 5) ||
                (currentVersion <= new Version(3, 6) &&
                currentVersion < _version);
        }

        public Version Upgrade()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                $"Upgrading database to version {_version}.");

            const string msSqlAlter = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tblCons' AND COLUMN_NAME='Notes')
    ALTER TABLE tblCons ADD [Notes] [nvarchar](MAX) NULL;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tblCons' AND COLUMN_NAME='InheritNotes')
    ALTER TABLE tblCons ADD [InheritNotes] [bit] NOT NULL DEFAULT 0;
";

            string[] mySqlAlters =
            [
                "ALTER TABLE `tblCons` ADD COLUMN `Notes` text",
                "ALTER TABLE `tblCons` ADD COLUMN `InheritNotes` tinyint NOT NULL DEFAULT 0",
            ];

            SqlMigrationHelper.ExecuteMigrationIdempotent(_databaseConnector, _version, msSqlAlter, mySqlAlters);
            return _version;
        }
    }
}

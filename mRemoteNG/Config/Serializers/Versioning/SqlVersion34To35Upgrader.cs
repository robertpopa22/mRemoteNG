using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    public class SqlVersion34To35Upgrader(IDatabaseConnector databaseConnector) : IVersionUpgrader
    {
        private readonly Version _version = new(3, 5);
        private readonly IDatabaseConnector _databaseConnector = databaseConnector ?? throw new ArgumentNullException(nameof(databaseConnector));

        public bool CanUpgrade(Version currentVersion)
        {
            return currentVersion == new Version(3, 4) ||
                (currentVersion <= new Version(3, 5) &&
                currentVersion < _version);
        }

        public Version Upgrade()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                $"Upgrading database to version {_version}.");

            const string msSqlAlter = @"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tblCons' AND COLUMN_NAME='UseRedirectionServerName')
    ALTER TABLE tblCons ADD [UseRedirectionServerName] [bit] NOT NULL DEFAULT 0;
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='tblCons' AND COLUMN_NAME='InheritUseRedirectionServerName')
    ALTER TABLE tblCons ADD [InheritUseRedirectionServerName] [bit] NOT NULL DEFAULT 0;
";

            string[] mySqlAlters =
            [
                "ALTER TABLE `tblCons` ADD COLUMN `UseRedirectionServerName` tinyint NOT NULL DEFAULT 0",
                "ALTER TABLE `tblCons` ADD COLUMN `InheritUseRedirectionServerName` tinyint NOT NULL DEFAULT 0",
            ];

            SqlMigrationHelper.ExecuteMigrationIdempotent(_databaseConnector, _version, msSqlAlter, mySqlAlters);
            return _version;
        }
    }
}

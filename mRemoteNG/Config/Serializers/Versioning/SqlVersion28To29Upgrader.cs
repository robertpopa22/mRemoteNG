using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    public class SqlVersion28To29Upgrader(IDatabaseConnector databaseConnector) : IVersionUpgrader
    {
        private readonly Version _version = new(2, 9);
        private readonly IDatabaseConnector _databaseConnector = databaseConnector ?? throw new ArgumentNullException(nameof(databaseConnector));

        public bool CanUpgrade(Version currentVersion)
        {
            return currentVersion == new Version(2, 8) ||
                // Support upgrading during dev revisions, 2.9.1, 2.9.2, etc...
                (currentVersion <= new Version(2, 9) &&
                currentVersion < _version);
        }

        public Version Upgrade()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                $"Upgrading database to version {_version}.");

            const string mySqlAlter = @"
ALTER TABLE tblCons ADD COLUMN `InheritUseRestrictedAdmin` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `UseRCG` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `UseRestrictedAdmin` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `InheritUseRCG` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `InheritRDGatewayExternalCredentialProvider` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `InheritRDGatewayUserViaAPI` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `InheritExternalCredentialProvider` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `InheritUserViaAPI` tinyint NOT NULL;
ALTER TABLE tblCons ADD COLUMN `EC2Region` varchar(32) DEFAULT NULL;
ALTER TABLE tblCons ADD COLUMN `EC2InstanceId` varchar(32) DEFAULT NULL;
ALTER TABLE tblCons ADD COLUMN `ExternalCredentialProvider` varchar(256) DEFAULT NULL;
ALTER TABLE tblCons ADD COLUMN `ExternalAddressProvider` varchar(256) DEFAULT NULL;
UPDATE tblCons SET InheritUseEnhancedMode = 0 WHERE InheritUseEnhancedMode IS NULL;
ALTER TABLE tblCons MODIFY COLUMN InheritUseEnhancedMode tinyint NOT NULL;
UPDATE tblCons SET UseEnhancedMode = 0 WHERE UseEnhancedMode IS NULL;
ALTER TABLE tblCons MODIFY COLUMN UseEnhancedMode tinyint NOT NULL;
UPDATE tblCons SET InheritVmId = 0 WHERE InheritVmId IS NULL;
ALTER TABLE tblCons MODIFY COLUMN InheritVmId tinyint NOT NULL;
UPDATE tblCons SET InheritUseVmId = 0 WHERE InheritUseVmId IS NULL;
ALTER TABLE tblCons MODIFY COLUMN InheritUseVmId tinyint NOT NULL;
UPDATE tblCons SET UseVmId = 0 WHERE UseVmId IS NULL;
ALTER TABLE tblCons MODIFY COLUMN UseVmId tinyint NOT NULL;
ALTER TABLE tblRoot MODIFY COLUMN ConfVersion VARCHAR(15) NOT NULL;
";

            // Every added NOT NULL column needs a DEFAULT: SQL Server refuses to add one to a
            // table that already has rows without it, which is what any real database looks like.
            // Invisible while testing against a schema this build generated -- the forward-port had
            // already added these columns, so the ADDs were skipped -- and fatal on a genuinely
            // older database. Same defect class as the 2.6 -> 2.7 step in #165, found by replaying
            // the historical schema fixture.
            const string msSqlAlter = @"
ALTER TABLE tblCons ADD InheritUseRestrictedAdmin bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD UseRCG bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD UseRestrictedAdmin bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD InheritUseRCG bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD InheritRDGatewayExternalCredentialProvider bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD InheritRDGatewayUserViaAPI bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD InheritExternalCredentialProvider bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD InheritUserViaAPI bit NOT NULL DEFAULT 0;
ALTER TABLE tblCons ADD EC2Region varchar(32) NULL;
ALTER TABLE tblCons ADD EC2InstanceId varchar(32) NULL;
ALTER TABLE tblCons ADD ExternalCredentialProvider varchar(256) NULL;
ALTER TABLE tblCons ADD ExternalAddressProvider varchar(256) NULL;
UPDATE tblCons SET InheritUseEnhancedMode = 0 WHERE InheritUseEnhancedMode IS NULL;
ALTER TABLE tblCons ALTER COLUMN InheritUseEnhancedMode bit NOT NULL;
UPDATE tblCons SET UseEnhancedMode = 0 WHERE UseEnhancedMode IS NULL;
ALTER TABLE tblCons ALTER COLUMN UseEnhancedMode bit NOT NULL;
UPDATE tblCons SET InheritVmId = 0 WHERE InheritVmId IS NULL;
ALTER TABLE tblCons ALTER COLUMN InheritVmId bit NOT NULL;
UPDATE tblCons SET InheritUseVmId = 0 WHERE InheritUseVmId IS NULL;
ALTER TABLE tblCons ALTER COLUMN InheritUseVmId bit NOT NULL;
UPDATE tblCons SET UseVmId = 0 WHERE UseVmId IS NULL;
ALTER TABLE tblCons ALTER COLUMN UseVmId bit NOT NULL;
ALTER TABLE tblRoot ALTER COLUMN [ConfVersion] VARCHAR(15) NOT NULL;
";

            SqlMigrationHelper.ExecuteMigration(_databaseConnector, _version, msSqlAlter, mySqlAlter);
            return _version;
        }
    }
}
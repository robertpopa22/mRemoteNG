using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    public class SqlVersion31To32Upgrader(IDatabaseConnector databaseConnector) : IVersionUpgrader
    {
        private readonly Version _version = new(3, 2);
        private readonly IDatabaseConnector _databaseConnector = databaseConnector ?? throw new ArgumentNullException(nameof(databaseConnector));

        public bool CanUpgrade(Version currentVersion)
        {
            return currentVersion == new Version(3, 1) ||
                // Support upgrading during dev revisions, 3.1.1, 3.1.2, etc...
                (currentVersion <= new Version(3, 2) &&
                currentVersion < _version);
        }

        public Version Upgrade()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                $"Upgrading database to version {_version}.");

            const string msSqlAlter = @"
-- ConstantID is the primary key of tblCons (added as PK_tblCons by the 2.7->2.8 step).
-- SQL Server refuses ALTER COLUMN on a PK member, so drop the PK (whatever its name),
-- widen the column, then re-add the PK. (#113)
DECLARE @pkName sysname;
SELECT @pkName = name FROM sys.key_constraints
WHERE [type] = 'PK' AND parent_object_id = OBJECT_ID(N'dbo.tblCons');
IF @pkName IS NOT NULL
    EXEC(N'ALTER TABLE tblCons DROP CONSTRAINT ' + QUOTENAME(@pkName));

ALTER TABLE tblCons ALTER COLUMN [ConstantID] nvarchar(128) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [ParentID] nvarchar(128) NULL;
ALTER TABLE tblCons ALTER COLUMN [Name] nvarchar(128) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [Type] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [Colors] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [Description] nvarchar(1024) NULL;
ALTER TABLE tblCons ALTER COLUMN [Domain] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [ExtApp] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [Hostname] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [Icon] nvarchar(128) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [LoadBalanceInfo] nvarchar(1024) NULL;
ALTER TABLE tblCons ALTER COLUMN [MacAddress] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [OpeningCommand] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [Panel] nvarchar(128) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [Password] nvarchar(1024) NULL;
ALTER TABLE tblCons ALTER COLUMN [PostExtApp] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [PreExtApp] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [Protocol] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [PuttySession] nvarchar(128) NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayDomain] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayHostname] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayPassword] nvarchar(1024) NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayUsageMethod] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayUseConnectionCredentials] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [RDGatewayUsername] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [RDPAuthenticationLevel] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [RdpVersion] nvarchar(10) NULL;
ALTER TABLE tblCons ALTER COLUMN [RedirectDiskDrives] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [RedirectDiskDrivesCustom] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [RedirectSound] nvarchar(64) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [RenderingEngine] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [Resolution] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [SSHOptions] nvarchar(1024) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [SSHTunnelConnectionName] nvarchar(128) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [SoundQuality] nvarchar(20) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [UserField] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [Username] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCAuthMode] nvarchar(10) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCColors] nvarchar(10) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCCompression] nvarchar(10) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCEncoding] nvarchar(20) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCProxyIP] nvarchar(128) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCProxyPassword] nvarchar(1024) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCProxyType] nvarchar(20) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCProxyUsername] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [VNCSmartSizeMode] nvarchar(20) NULL;
ALTER TABLE tblCons ALTER COLUMN [VmId] nvarchar(100) NULL;
ALTER TABLE tblCons ALTER COLUMN [ICAEncryptionStrength] nvarchar(32) NOT NULL;
ALTER TABLE tblCons ALTER COLUMN [StartProgram] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [StartProgramWorkDir] nvarchar(512) NULL;
ALTER TABLE tblCons ALTER COLUMN [EC2Region] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [EC2InstanceId] nvarchar(32) NULL;
ALTER TABLE tblCons ALTER COLUMN [ExternalCredentialProvider] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [ExternalAddressProvider] nvarchar(256) NULL;
ALTER TABLE tblCons ALTER COLUMN [UserViaAPI] nvarchar(512) NOT NULL;

ALTER TABLE tblRoot ALTER COLUMN [Name] nvarchar(2048) NOT NULL;
ALTER TABLE tblRoot ALTER COLUMN [Protected] nvarchar(MAX) NOT NULL;
ALTER TABLE tblRoot ALTER COLUMN [ConfVersion] nvarchar(15) NOT NULL;

ALTER TABLE tblExternalTools ALTER COLUMN [DisplayName] nvarchar(256) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [FileName] nvarchar(1024) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [Arguments] nvarchar(2048) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [WorkingDir] nvarchar(1024) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [Category] nvarchar(256) NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.key_constraints
    WHERE [type] = 'PK' AND parent_object_id = OBJECT_ID(N'dbo.tblCons'))
    ALTER TABLE tblCons ADD CONSTRAINT PK_tblCons PRIMARY KEY ([ConstantID]);
";

            // No MySQL ALTER needed -- varchar already supports Unicode in MySQL
            SqlMigrationHelper.ExecuteMigration(_databaseConnector, _version, msSqlAlter, null);
            return _version;
        }
    }
}

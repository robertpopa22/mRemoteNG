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
DECLARE @dropPkSql nvarchar(512);
SELECT @pkName = name FROM sys.key_constraints
WHERE [type] = 'PK' AND parent_object_id = OBJECT_ID(N'dbo.tblCons');
-- EXEC() rejects a concatenated expression (literal + QUOTENAME(...)); build the
-- statement into a variable first so it parses on every SQL Server version. (#113)
IF @pkName IS NOT NULL
BEGIN
    SET @dropPkSql = N'ALTER TABLE tblCons DROP CONSTRAINT ' + QUOTENAME(@pkName);
    EXEC(@dropPkSql);
END

-- Columns added by earlier upgraders (or absent in the 2.6 schema) can hold NULL in
-- existing rows. ALTER COLUMN ... NOT NULL validates existing data and SQL Server
-- rejects it as ""Cannot insert the value NULL ... UPDATE failed"" if any row is NULL.
-- Backfill every column this step tightens to NOT NULL with '' before the ALTERs. (#113)
UPDATE tblCons SET
    [Name] = ISNULL([Name], N''),
    [Type] = ISNULL([Type], N''),
    [Colors] = ISNULL([Colors], N''),
    [Icon] = ISNULL([Icon], N''),
    [Panel] = ISNULL([Panel], N''),
    [Protocol] = ISNULL([Protocol], N''),
    [RDGatewayUsageMethod] = ISNULL([RDGatewayUsageMethod], N''),
    [RDGatewayUseConnectionCredentials] = ISNULL([RDGatewayUseConnectionCredentials], N''),
    [RDPAuthenticationLevel] = ISNULL([RDPAuthenticationLevel], N''),
    [RedirectSound] = ISNULL([RedirectSound], N''),
    [Resolution] = ISNULL([Resolution], N''),
    [SSHOptions] = ISNULL([SSHOptions], N''),
    [SSHTunnelConnectionName] = ISNULL([SSHTunnelConnectionName], N''),
    [SoundQuality] = ISNULL([SoundQuality], N''),
    [ICAEncryptionStrength] = ISNULL([ICAEncryptionStrength], N''),
    [UserViaAPI] = ISNULL([UserViaAPI], N'');

-- Same trap as the tblExternalTools defaults handled further down, and as the 2.9->3.0 step:
-- columns added by an earlier upgrader with an unnamed DEFAULT (e.g. the 2.9->3.0 step's
-- ""RedirectDiskDrivesCustom varchar(32) DEFAULT NULL"") get an auto-named constraint such as
-- DF__tblCons__Redirec__45F365D3, and every ALTER COLUMN below is then rejected with error 5074,
-- rolling the whole step back and pinning the database at 3.1. Which columns carry such a
-- constraint depends on the history of the individual database, so rather than list suspects,
-- drop every auto-named default on the columns this step is about to alter. Found by replaying a
-- genuinely historical schema through the ODBC connector.
DECLARE @dropConsAlterDefaults nvarchar(MAX) = N'';
SELECT @dropConsAlterDefaults = @dropConsAlterDefaults +
    N'ALTER TABLE tblCons DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tblCons')
    AND dc.name LIKE N'DF[_][_]%'
    AND c.name IN (N'RedirectDiskDrives', N'RedirectDiskDrivesCustom', N'RenderingEngine',
                   N'SoundQuality', N'ICAEncryptionStrength', N'UserViaAPI', N'Colors',
                   N'Icon', N'Panel', N'Protocol', N'Type');
IF @dropConsAlterDefaults <> N''
    EXEC(@dropConsAlterDefaults);

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

UPDATE tblRoot SET
    [Name] = ISNULL([Name], N''),
    [Protected] = ISNULL([Protected], N''),
    [ConfVersion] = ISNULL([ConfVersion], N'');
ALTER TABLE tblRoot ALTER COLUMN [Name] nvarchar(2048) NOT NULL;
ALTER TABLE tblRoot ALTER COLUMN [Protected] nvarchar(MAX) NOT NULL;
ALTER TABLE tblRoot ALTER COLUMN [ConfVersion] nvarchar(15) NOT NULL;

-- tblExternalTools was created by the 3.0->3.1 step with unnamed ""NOT NULL DEFAULT ''""
-- on Arguments/WorkingDir/Category, so SQL Server auto-named the DEFAULT constraints
-- (e.g. DF__tblExtern__Argum__18EBB532). ALTER COLUMN below is blocked while such a
-- constraint depends on the column (error 5074: ""... one or more objects access the
-- column""), which rolled the whole 3.1->3.2 step back and pinned the DB at 3.1. Drop
-- the dependent defaults first; they are re-added (named) after the ALTERs. (#113)
DECLARE @dropExtDefaultsSql nvarchar(MAX) = N'';
SELECT @dropExtDefaultsSql = @dropExtDefaultsSql +
    N'ALTER TABLE tblExternalTools DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';'
FROM sys.default_constraints dc
INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tblExternalTools')
    AND c.name IN (N'Arguments', N'WorkingDir', N'Category');
IF @dropExtDefaultsSql <> N''
    EXEC(@dropExtDefaultsSql);

UPDATE tblExternalTools SET
    [DisplayName] = ISNULL([DisplayName], N''),
    [FileName] = ISNULL([FileName], N''),
    [Arguments] = ISNULL([Arguments], N''),
    [WorkingDir] = ISNULL([WorkingDir], N''),
    [Category] = ISNULL([Category], N'');
ALTER TABLE tblExternalTools ALTER COLUMN [DisplayName] nvarchar(256) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [FileName] nvarchar(1024) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [Arguments] nvarchar(2048) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [WorkingDir] nvarchar(1024) NOT NULL;
ALTER TABLE tblExternalTools ALTER COLUMN [Category] nvarchar(256) NOT NULL;

-- Re-create the '' defaults (matching the fresh 3.0->3.1 schema) with explicit,
-- deterministic names so a future ALTER COLUMN can drop them by name instead of
-- hitting another auto-named DF__ constraint. Idempotent. (#113)
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_tblExternalTools_Arguments')
    ALTER TABLE tblExternalTools ADD CONSTRAINT DF_tblExternalTools_Arguments DEFAULT N'' FOR [Arguments];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_tblExternalTools_WorkingDir')
    ALTER TABLE tblExternalTools ADD CONSTRAINT DF_tblExternalTools_WorkingDir DEFAULT N'' FOR [WorkingDir];
IF NOT EXISTS (SELECT 1 FROM sys.default_constraints WHERE name = N'DF_tblExternalTools_Category')
    ALTER TABLE tblExternalTools ADD CONSTRAINT DF_tblExternalTools_Category DEFAULT N'' FOR [Category];

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

using mRemoteNG.App;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using System;
using System.Data.Common;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    public class SqlVersion26To27Upgrader(IDatabaseConnector databaseConnector) : IVersionUpgrader
    {
        private readonly IDatabaseConnector _databaseConnector = databaseConnector ?? throw new ArgumentNullException(nameof(databaseConnector));

        public bool CanUpgrade(Version currentVersion)
        {
            return currentVersion.CompareTo(new Version(2, 6)) == 0;
        }

        public Version Upgrade()
        {
            Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                                                "Upgrading database from version 2.6 to version 2.7.");
            try
            {
                // Every added column needs a DEFAULT: SQL Server refuses to add a NOT NULL column
                // to a table that already has rows without one, which is exactly what an imported
                // 1.76 database looks like. "varchar NOT NULL DEFAULT NULL" was doubly wrong --
                // self-contradictory, and a bare varchar is a single character in T-SQL. (#165)
                const string sqlText = @"
ALTER TABLE tblCons
ADD RedirectClipboard bit NOT NULL DEFAULT 0,
	InheritRedirectClipboard bit NOT NULL DEFAULT 0,
    VmId varchar(4000) NULL,
    UseVmId bit NOT NULL DEFAULT 0,
    UseEnhancedMode bit NOT NULL DEFAULT 0,
    InheritVmId bit NOT NULL DEFAULT 0,
    InheritUseVmId bit NOT NULL DEFAULT 0,
    SSHTunnelConnectionName varchar(4000) NULL,
    InheritSSHTunnelConnectionName bit NOT NULL DEFAULT 0,
    SSHOptions varchar(4000) NULL,
    InheritSSHOptions bit NOT NULL DEFAULT 0,
    InheritUseEnhancedMode bit NOT NULL DEFAULT 0;
UPDATE tblRoot
    SET ConfVersion='2.7'";
                System.Data.Common.DbCommand dbCommand = _databaseConnector.DbCommand(sqlText);
                dbCommand.ExecuteNonQuery();
            }
            catch (DbException)
            {
                // The columns are normally already present, added with proper defaults by the
                // schema forward-port that runs ahead of the versioned upgraders; the redundant
                // ALTER then fails as a duplicate column and is ignored here. Catching only
                // SqlException let the identical ODBC failure (OdbcException) abort the whole
                // upgrade instead. (#165)
            }

            return new Version(2, 7);
        }
    }
}
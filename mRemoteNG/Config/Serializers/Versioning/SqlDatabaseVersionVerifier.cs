using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Messages;
using mRemoteNG.Resources.Language;
using System;
using System.Globalization;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Serializers.Versioning
{
    [SupportedOSPlatform("windows")]
    public class SqlDatabaseVersionVerifier : ISqlDatabaseVersionVerifier
    {
        /// <summary>
        /// The SQL schema version this build reads and writes. The single source of truth: the
        /// upgrade chain below converges on it, and WriteDatabaseMetaData stamps it into
        /// tblRoot.ConfVersion on every save. Those two used to disagree -- the metadata writer
        /// stamped a stale constant (3.2) while the chain was at 3.5, so every save regressed the
        /// recorded version and every subsequent load re-ran three upgrade steps against an
        /// already-current schema. (#148)
        /// </summary>
        public static readonly Version SupportedSchemaVersion = new(3, 6);

        private readonly Version _currentSupportedVersion = SupportedSchemaVersion;

        private readonly IDatabaseConnector _databaseConnector;

        public SqlDatabaseVersionVerifier(IDatabaseConnector databaseConnector)
        {
            ArgumentNullException.ThrowIfNull(databaseConnector);
            _databaseConnector = databaseConnector;
        }

        public bool VerifyDatabaseVersion(Version dbVersion)
        {
            try
            {
                Version databaseVersion = dbVersion;

                if (databaseVersion.Equals(_currentSupportedVersion))
                {
                    return true;
                }

                IVersionUpgrader[] dbUpgraders = new IVersionUpgrader[]
                {
                    new SqlVersion22To23Upgrader(_databaseConnector),
                    new SqlVersion23To24Upgrader(_databaseConnector),
                    new SqlVersion24To25Upgrader(_databaseConnector),
                    new SqlVersion25To26Upgrader(_databaseConnector),
                    new SqlVersion26To27Upgrader(_databaseConnector),
                    new SqlVersion27To28Upgrader(_databaseConnector),
                    new SqlVersion28To29Upgrader(_databaseConnector),
                    new SqlVersion29To30Upgrader(_databaseConnector),
                    new SqlVersion30To31Upgrader(_databaseConnector),
                    new SqlVersion31To32Upgrader(_databaseConnector),
                    new SqlVersion32To33Upgrader(_databaseConnector),
                    new SqlVersion33To34Upgrader(_databaseConnector),
                    new SqlVersion34To35Upgrader(_databaseConnector),
                    new SqlVersion35To36Upgrader(_databaseConnector),
                };

                foreach (IVersionUpgrader upgrader in dbUpgraders)
                {
                    if (upgrader.CanUpgrade(databaseVersion))
                    {
                        databaseVersion = upgrader.Upgrade();
                    }
                }

                // DB is at the highest current supported version
                if (databaseVersion.CompareTo(_currentSupportedVersion) == 0)
                {
                    return true;
                }

                Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, string.Format(CultureInfo.InvariantCulture, Language.ErrorBadDatabaseVersion, databaseVersion, GeneralAppInfo.ProductName));
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.ErrorMsg, string.Format(CultureInfo.InvariantCulture, Language.ErrorVerifyDatabaseVersionFailed, ex.Message));
            }

            return false;
        }
    }
}
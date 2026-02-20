using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Security;
using mRemoteNG.Config.DatabaseConnectors;
using System.Data;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.Authentication;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Config.Connections
{
    [SupportedOSPlatform("windows")]
    public class SqlConnectionsLoader(
        IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>> localConnectionPropertiesDeserializer,
        IDataProvider<string> localPropertiesDataProvider,
        IDatabaseConnector databaseConnector,
        IDataProvider<DataTable> sqlDataProvider,
        ISqlDatabaseMetaDataRetriever sqlMetaDataRetriever,
        ISqlDatabaseVersionVerifier sqlDatabaseVersionVerifier,
        ICryptographyProvider cryptographyProvider,
        Func<string, Optional<SecureString>>? authenticationRequestor = null) : IConnectionsLoader
    {
        private readonly IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>> _localConnectionPropertiesDeserializer = localConnectionPropertiesDeserializer.ThrowIfNull(nameof(localConnectionPropertiesDeserializer));
        private readonly IDataProvider<string> _localPropertiesDataProvider = localPropertiesDataProvider.ThrowIfNull(nameof(localPropertiesDataProvider));
        private readonly IDatabaseConnector _databaseConnector = databaseConnector.ThrowIfNull(nameof(databaseConnector));
        private readonly IDataProvider<DataTable> _sqlDataProvider = sqlDataProvider.ThrowIfNull(nameof(sqlDataProvider));
        private readonly ISqlDatabaseMetaDataRetriever _sqlMetaDataRetriever = sqlMetaDataRetriever.ThrowIfNull(nameof(sqlMetaDataRetriever));
        private readonly ISqlDatabaseVersionVerifier _sqlDatabaseVersionVerifier = sqlDatabaseVersionVerifier.ThrowIfNull(nameof(sqlDatabaseVersionVerifier));
        private readonly ICryptographyProvider _cryptographyProvider = cryptographyProvider.ThrowIfNull(nameof(cryptographyProvider));

        private Func<string, Optional<SecureString>> AuthenticationRequestor { get; } = authenticationRequestor ?? ((filename) => MiscTools.PasswordDialog(filename, false));

        public ConnectionTreeModel Load()
        {
            SqlConnectionListMetaData metaData = _sqlMetaDataRetriever.GetDatabaseMetaData(_databaseConnector) ?? HandleFirstRun(_sqlMetaDataRetriever, _databaseConnector);
            Optional<SecureString> decryptionKey = GetDecryptionKey(metaData);

            if (!decryptionKey.Any())
                throw new Exception("Could not load SQL connections");

            _sqlDatabaseVersionVerifier.VerifyDatabaseVersion(metaData.ConfVersion);
            System.Data.DataTable dataTable = _sqlDataProvider.Load();
            DataTableDeserializer deserializer = new(_cryptographyProvider, decryptionKey.First());
            ConnectionTreeModel connectionTree = deserializer.Deserialize(dataTable);
            ApplyLocalConnectionProperties(connectionTree.RootNodes.First(i => i is RootNodeInfo));
            return connectionTree;
        }

        private Optional<SecureString> GetDecryptionKey(SqlConnectionListMetaData metaData)
        {
            string cipherText = metaData.Protected;

            // If Protected is empty, the database has no master password set.
            // Return the default password directly without authentication.
            if (string.IsNullOrEmpty(cipherText))
                return new RootNodeInfo(RootNodeType.Connection).DefaultPassword.ConvertToSecureString();

            PasswordAuthenticator authenticator = new(_cryptographyProvider, cipherText, () => AuthenticationRequestor(""));
            bool authenticated = authenticator.Authenticate(new RootNodeInfo(RootNodeType.Connection).DefaultPassword.ConvertToSecureString());

            return authenticated && authenticator.LastAuthenticatedPassword is { } password
                ? password
                : Optional<SecureString>.Empty;
        }

        private void ApplyLocalConnectionProperties(ContainerInfo rootNode)
        {
            string localPropertiesXml = _localPropertiesDataProvider.Load();
            IEnumerable<LocalConnectionPropertiesModel> localConnectionProperties = _localConnectionPropertiesDeserializer.Deserialize(localPropertiesXml);

            rootNode
                .GetRecursiveChildList()
                .Join(localConnectionProperties,
                      con => con.ConstantID,
                      locals => locals.ConnectionId,
                      (con, locals) => new {Connection = con, LocalProperties = locals})
                .ForEach(x =>
                {
                    x.Connection.PleaseConnect = x.LocalProperties.Connected;
                    x.Connection.Favorite = x.LocalProperties.Favorite;
                    if (x.Connection is ContainerInfo container)
                        container.IsExpanded = x.LocalProperties.Expanded;
                });
        }

        private SqlConnectionListMetaData HandleFirstRun(ISqlDatabaseMetaDataRetriever metaDataRetriever, IDatabaseConnector connector)
        {
	        metaDataRetriever.WriteDatabaseMetaData(new RootNodeInfo(RootNodeType.Connection), connector);
	        return metaDataRetriever.GetDatabaseMetaData(connector)!;
		}
    }
}
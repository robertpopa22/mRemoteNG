using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security;
using Microsoft.Data.SqlClient;
using mRemoteNG.Config.Connections;
using mRemoteNG.Config.DatabaseConnectors;
using mRemoteNG.Config.DataProviders;
using mRemoteNG.Config.Serializers;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Sql;
using mRemoteNG.Config.Serializers.Versioning;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NSubstitute;
using NUnit.Framework;
using ConnectionInfoAlias = mRemoteNG.Connection.ConnectionInfo;

namespace mRemoteNGTests.Config.Connections;

[TestFixture]
public class SqlConnectionsLoaderIntegrationTests
{
    // Synthetic legacy marker encrypted with "sqlpass". The default key "mR3m"
    // also passes CBC padding for these bytes, but produces invalid plaintext.
    private const string PaddingCollisionCipherText = "prFr5uheaqzrPbIp+bOjhAiTvlNPrrOGWY/9NVDPx+Q=";
    private IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>> _localConnectionPropertiesDeserializerMock;
    private IDataProvider<string> _localPropertiesDataProviderMock;
    private IDatabaseConnector _databaseConnectorMock;
    private IDataProvider<DataTable> _sqlDataProviderMock;
    private ISqlDatabaseMetaDataRetriever _metaDataRetrieverMock;
    private ISqlDatabaseVersionVerifier _versionVerifierMock;
    private LegacyRijndaelCryptographyProvider _cryptographyProvider;

    [SetUp]
    public void Setup()
    {
        _localConnectionPropertiesDeserializerMock = Substitute.For<IDeserializer<string, IEnumerable<LocalConnectionPropertiesModel>>>();
        _localPropertiesDataProviderMock = Substitute.For<IDataProvider<string>>();
        _databaseConnectorMock = Substitute.For<IDatabaseConnector>();
        _sqlDataProviderMock = Substitute.For<IDataProvider<DataTable>>();
        _metaDataRetrieverMock = Substitute.For<ISqlDatabaseMetaDataRetriever>();
        _versionVerifierMock = Substitute.For<ISqlDatabaseVersionVerifier>();
        _cryptographyProvider = new LegacyRijndaelCryptographyProvider();

        _localConnectionPropertiesDeserializerMock.Deserialize(Arg.Any<string>())
            .Returns(new List<LocalConnectionPropertiesModel>());

        _versionVerifierMock.VerifyDatabaseVersion(Arg.Any<Version>()).Returns(true);
    }

    private SqlConnectionListMetaData CreateMetaData(SecureString masterPassword)
    {
        return new SqlConnectionListMetaData
        {
            Name = "Connections",
            Protected = _cryptographyProvider.Encrypt("ThisIsNotProtected", masterPassword),
            Export = false,
            ConfVersion = new Version(3, 0)
        };
    }

    private DataTable CreateEncryptedConnectionsDataTable(SecureString masterPassword, ConnectionInfoAlias connectionInfo)
    {
        var rootNode = new RootNodeInfo(RootNodeType.Connection);
        rootNode.AddChild(connectionInfo);
        rootNode.PasswordString = masterPassword.ConvertToUnsecureString();

        var saveFilter = new SaveFilter();
        var serializer = new DataTableSerializer(saveFilter, _cryptographyProvider, masterPassword);
        var connectionTreeModel = new ConnectionTreeModel();
        connectionTreeModel.AddRootNode(rootNode);
        return serializer.Serialize(connectionTreeModel);
    }

    private SqlConnectionsLoader CreateLoader(IDatabaseConnector? databaseConnector = null, Func<string, Optional<SecureString>>? authRequestor = null,
        ICryptographyProvider? cryptographyProvider = null)
    {
        return new SqlConnectionsLoader(
            _localConnectionPropertiesDeserializerMock,
            _localPropertiesDataProviderMock,
            databaseConnector ?? _databaseConnectorMock,
            _sqlDataProviderMock,
            _metaDataRetrieverMock,
            _versionVerifierMock,
            cryptographyProvider ?? _cryptographyProvider,
            authRequestor);
    }

    [Test]
    [TestCase(DatabaseConnectorFactory.OdbcType)]
    [TestCase("ODBC - Open Database Connectivity")]
    public void DatabaseConnectorFactory_WithOdbcSelection_ReturnsOdbcConnector(string selectedType)
    {
        using IDatabaseConnector connector = DatabaseConnectorFactory.DatabaseConnector(
            selectedType,
            "DSN=SqlConnectionsLoaderIntegrationTests",
            "mremoteng",
            "user",
            "password");

        Assert.That(connector, Is.TypeOf<OdbcDatabaseConnector>());
    }

    [Test]
    public void DatabaseConnectorFactory_WithMsSqlWindowsAuthentication_UsesIntegratedSecurity()
    {
        using IDatabaseConnector connector = DatabaseConnectorFactory.DatabaseConnector(
            DatabaseConnectorFactory.MsSqlType,
            "sqlserver.local",
            "mremoteng",
            "domain\\user",
            "password",
            DatabaseConnectorFactory.WindowsAuthentication);

        Assert.That(connector, Is.TypeOf<MSSqlDatabaseConnector>());

        var sqlConnection = (SqlConnection)connector.DbConnection();
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);

        Assert.That(builder.IntegratedSecurity, Is.True);
        Assert.That(builder.UserID, Is.Empty);
        Assert.That(builder.Password, Is.Empty);
        Assert.That(builder.Encrypt, Is.EqualTo(SqlConnectionEncryptOption.Mandatory));
        Assert.That(builder.TrustServerCertificate, Is.True);
    }

    [Test]
    public void DatabaseConnectorFactory_WithMsSqlSqlAuthentication_UsesCustomCredentials()
    {
        using IDatabaseConnector connector = DatabaseConnectorFactory.DatabaseConnector(
            DatabaseConnectorFactory.MsSqlType,
            "sqlserver.local",
            "mremoteng",
            "dbuser",
            string.Empty,
            "SQL Server Authentication");

        Assert.That(connector, Is.TypeOf<MSSqlDatabaseConnector>());

        var sqlConnection = (SqlConnection)connector.DbConnection();
        var builder = new SqlConnectionStringBuilder(sqlConnection.ConnectionString);

        Assert.That(builder.IntegratedSecurity, Is.False);
        Assert.That(builder.UserID, Is.EqualTo("dbuser"));
    }

    [Test]
    public void LoadsSqlConnections_WithCorrectPassword_Successfully()
    {
        // Arrange
        var masterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(masterPassword.AppendChar);
        masterPassword.MakeReadOnly();

        var connectionInfo = new ConnectionInfoAlias
        {
            Hostname = "sqlhost",
            Password = "somepassword",
            Name = "TestConnection",
            Protocol = mRemoteNG.Connection.Protocol.ProtocolType.RDP
        };

        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(CreateMetaData(masterPassword));

        _sqlDataProviderMock.Load()
            .Returns(CreateEncryptedConnectionsDataTable(masterPassword, connectionInfo));

        var loader = CreateLoader(authRequestor: (filename) => new Optional<SecureString>(masterPassword));

        // Act
        var loadedTree = loader.Load();

        // Assert
        Assert.That(loadedTree, Is.Not.Null);
        Assert.That(loadedTree.RootNodes.Count, Is.EqualTo(1));
        Assert.That(loadedTree.RootNodes[0].Children.Count, Is.EqualTo(1));
        Assert.That(loadedTree.RootNodes[0].Children[0].Hostname, Is.EqualTo("sqlhost"));
        Assert.That(loadedTree.RootNodes[0].Children[0].Password, Is.EqualTo("somepassword"));
        Assert.That(loadedTree.RootNodes[0].Children[0].Name, Is.EqualTo("TestConnection"));
    }

    [Test]
    public void LoadsSqlConnections_WithOdbcConnector_Successfully()
    {
        // Arrange
        var masterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(masterPassword.AppendChar);
        masterPassword.MakeReadOnly();

        using var odbcConnector = new OdbcDatabaseConnector(
            "DSN=SqlConnectionsLoaderIntegrationTests",
            "mremoteng",
            "user",
            "password");

        var connectionInfo = new ConnectionInfoAlias
        {
            Hostname = "sqlhost",
            Password = "somepassword",
            Name = "OdbcConnection",
            Protocol = mRemoteNG.Connection.Protocol.ProtocolType.RDP
        };

        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(CreateMetaData(masterPassword));

        _sqlDataProviderMock.Load()
            .Returns(CreateEncryptedConnectionsDataTable(masterPassword, connectionInfo));

        var loader = CreateLoader(odbcConnector, (filename) => new Optional<SecureString>(masterPassword));

        // Act
        var loadedTree = loader.Load();

        // Assert
        Assert.That(loadedTree, Is.Not.Null);
        Assert.That(loadedTree.RootNodes.Count, Is.EqualTo(1));
        Assert.That(loadedTree.RootNodes[0].Children.Count, Is.EqualTo(1));
        Assert.That(loadedTree.RootNodes[0].Children[0].Name, Is.EqualTo("OdbcConnection"));
        _metaDataRetrieverMock.Received(1)
            .GetDatabaseMetaData(Arg.Is<IDatabaseConnector>(connector => ReferenceEquals(connector, odbcConnector)));
    }

    [Test]
    public void LoadsSqlConnections_WithIncorrectPassword_ThrowsException()
    {
        // Arrange
        var correctMasterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(correctMasterPassword.AppendChar);
        correctMasterPassword.MakeReadOnly();

        var incorrectMasterPassword = new SecureString();
        "wrongpass".ToCharArray().ToList().ForEach(incorrectMasterPassword.AppendChar);
        incorrectMasterPassword.MakeReadOnly();

        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(CreateMetaData(correctMasterPassword));

        Func<string, Optional<SecureString>> mockPasswordRequestor =
            Substitute.For<Func<string, Optional<SecureString>>>();
        mockPasswordRequestor
            .Invoke(Arg.Any<string>())
            .Returns(new Optional<SecureString>(incorrectMasterPassword),
                     new Optional<SecureString>(incorrectMasterPassword),
                     new Optional<SecureString>(incorrectMasterPassword));

        var loader = CreateLoader(authRequestor: mockPasswordRequestor);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => loader.Load());
        Assert.That(ex.Message, Is.EqualTo("Could not load SQL connections"));
        mockPasswordRequestor.Received(3).Invoke("");
    }

    [Test]
    public void LoadsSqlConnections_WithNoPasswordProvided_ThrowsException()
    {
        // Arrange
        var correctMasterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(correctMasterPassword.AppendChar);
        correctMasterPassword.MakeReadOnly();

        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(CreateMetaData(correctMasterPassword));

        Func<string, Optional<SecureString>> mockPasswordRequestor =
            Substitute.For<Func<string, Optional<SecureString>>>();
        mockPasswordRequestor
            .Invoke(Arg.Any<string>())
            .Returns(Optional<SecureString>.Empty);

        var rejectedCipher = Substitute.For<ICryptographyProvider>();
        rejectedCipher.Decrypt(Arg.Any<string>(), Arg.Any<SecureString>())
            .Returns(_ => throw new System.Security.Cryptography.CryptographicException("Synthetic incorrect password"));
        var loader = CreateLoader(authRequestor: mockPasswordRequestor, cryptographyProvider: rejectedCipher);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => loader.Load());
        Assert.That(ex.Message, Is.EqualTo("Could not load SQL connections"));
        mockPasswordRequestor.Received(1).Invoke("");
        _sqlDataProviderMock.DidNotReceive().Load();
    }

    [Test]
    public void Load_WhenWrongKeyPassesLegacyPadding_StillRequestsPasswordAndHonorsCancel()
    {
        using var defaultPassword = "mR3m".ConvertToSecureString();
        Assert.That(_cryptographyProvider.Decrypt(PaddingCollisionCipherText, defaultPassword),
            Is.Not.EqualTo("ThisIsProtected"));
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(new SqlConnectionListMetaData { Protected = PaddingCollisionCipherText, ConfVersion = new Version(3, 0) });
        var requestor = Substitute.For<Func<string, Optional<SecureString>>>();
        requestor.Invoke(Arg.Any<string>()).Returns(Optional<SecureString>.Empty);

        Assert.That(() => CreateLoader(authRequestor: requestor).Load(),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Could not load SQL connections"));
        requestor.Received(1).Invoke("");
        _versionVerifierMock.DidNotReceive().VerifyDatabaseVersion(Arg.Any<Version>());
        _sqlDataProviderMock.DidNotReceive().Load();
    }

    [Test]
    public void Load_WhenWrongKeyPassesLegacyPadding_CanRetryWithCorrectPassword()
    {
        using var password = "sqlpass".ConvertToSecureString();
        Assert.That(_cryptographyProvider.Decrypt(PaddingCollisionCipherText, password), Is.EqualTo("ThisIsProtected"));
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(new SqlConnectionListMetaData { Protected = PaddingCollisionCipherText, ConfVersion = new Version(3, 0) });
        _sqlDataProviderMock.Load().Returns(CreateEncryptedConnectionsDataTable(password,
            new ConnectionInfoAlias { Name = "Retry", Password = "synthetic-secret" }));
        int requests = 0;
        var tree = CreateLoader(authRequestor: _ => { requests++; return password; }).Load();

        Assert.That(requests, Is.EqualTo(1));
        Assert.That(tree.RootNodes[0].Children[0].Password, Is.EqualTo("synthetic-secret"));
    }

    [TestCase("ThisIsProtected")]
    [TestCase("ThisIsNotProtected")]
    [TestCase("")]
    public void Load_WithSupportedUnprotectedMetadata_DoesNotPrompt(string marker)
    {
        using var password = "mR3m".ConvertToSecureString();
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(new SqlConnectionListMetaData {
                Protected = marker.Length == 0 ? "" : _cryptographyProvider.Encrypt(marker, password),
                ConfVersion = new Version(3, 0) });
        _sqlDataProviderMock.Load().Returns(CreateEncryptedConnectionsDataTable(password,
            new ConnectionInfoAlias { Name = "Default", Password = "synthetic-secret" }));
        var requestor = Substitute.For<Func<string, Optional<SecureString>>>();

        var tree = CreateLoader(authRequestor: requestor).Load();

        Assert.That(tree.RootNodes[0].Children[0].Password, Is.EqualTo("synthetic-secret"));
        requestor.DidNotReceive().Invoke(Arg.Any<string>());
    }

    [TestCase("thisisprotected")]
    [TestCase("ThisIsProtected ")]
    [TestCase("invalid")]
    public void Load_WithUnexpectedMetadataPlaintext_DoesNotReadConnections(string marker)
    {
        using var password = "mR3m".ConvertToSecureString();
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(new SqlConnectionListMetaData {
                Protected = _cryptographyProvider.Encrypt(marker, password), ConfVersion = new Version(3, 0) });

        Assert.That(() => CreateLoader(authRequestor: _ => Optional<SecureString>.Empty).Load(),
            Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Could not load SQL connections"));
        _sqlDataProviderMock.DidNotReceive().Load();
    }

    [Test]
    public void Load_CallsVersionVerifier_WithMetaDataVersion()
    {
        // Arrange
        var masterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(masterPassword.AppendChar);
        masterPassword.MakeReadOnly();

        var metaData = CreateMetaData(masterPassword);
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(metaData);

        var connectionInfo = new ConnectionInfoAlias { Name = "Test", Protocol = mRemoteNG.Connection.Protocol.ProtocolType.RDP };
        _sqlDataProviderMock.Load()
            .Returns(CreateEncryptedConnectionsDataTable(masterPassword, connectionInfo));

        var loader = CreateLoader(authRequestor: (filename) => new Optional<SecureString>(masterPassword));

        // Act
        loader.Load();

        // Assert
        _versionVerifierMock.Received(1).VerifyDatabaseVersion(metaData.ConfVersion);
    }

    [Test]
    public void Load_WhenMetaDataIsNull_CallsWriteDatabaseMetaData()
    {
        // Arrange
        var masterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(masterPassword.AppendChar);
        masterPassword.MakeReadOnly();

        // First call returns null (first run), second returns valid metadata
        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(null, CreateMetaData(masterPassword));

        var connectionInfo = new ConnectionInfoAlias { Name = "Test", Protocol = mRemoteNG.Connection.Protocol.ProtocolType.RDP };
        _sqlDataProviderMock.Load()
            .Returns(CreateEncryptedConnectionsDataTable(masterPassword, connectionInfo));

        var loader = CreateLoader(authRequestor: (filename) => new Optional<SecureString>(masterPassword));

        // Act
        loader.Load();

        // Assert
        _metaDataRetrieverMock.Received(1).WriteDatabaseMetaData(Arg.Any<RootNodeInfo>(), Arg.Any<IDatabaseConnector>());
    }

    [Test]
    public void Load_WhenMetaDataIsNull_WithOdbcConnector_CallsWriteDatabaseMetaData()
    {
        // Arrange
        var masterPassword = new SecureString();
        "sqlpass".ToCharArray().ToList().ForEach(masterPassword.AppendChar);
        masterPassword.MakeReadOnly();

        using var odbcConnector = new OdbcDatabaseConnector(
            "DSN=SqlConnectionsLoaderIntegrationTests",
            "mremoteng",
            "user",
            "password");

        _metaDataRetrieverMock.GetDatabaseMetaData(Arg.Any<IDatabaseConnector>())
            .Returns(null, CreateMetaData(masterPassword));

        var connectionInfo = new ConnectionInfoAlias { Name = "Test", Protocol = mRemoteNG.Connection.Protocol.ProtocolType.RDP };
        _sqlDataProviderMock.Load()
            .Returns(CreateEncryptedConnectionsDataTable(masterPassword, connectionInfo));

        var loader = CreateLoader(odbcConnector, (filename) => new Optional<SecureString>(masterPassword));

        // Act
        loader.Load();

        // Assert
        _metaDataRetrieverMock.Received(1)
            .WriteDatabaseMetaData(
                Arg.Any<RootNodeInfo>(),
                Arg.Is<IDatabaseConnector>(connector => ReferenceEquals(connector, odbcConnector)));
    }
}

using System;
using System.IO;
using System.Reflection;
using mRemoteNG.Connection;
using mRemoteNG.Messages;
using mRemoteNG.Properties;
using mRemoteNG.Tools;
using NUnit.Framework;

namespace mRemoteNGTests.Tools
{
    /// <summary>
    /// The gate that turns a quarantined ExternalConnectors.dll into a message instead of a
    /// crash on the first connect (#175/#191/#192).
    /// </summary>
    [TestFixture]
    public class ExternalConnectorsAssemblyTests
    {
        [Test]
        public void TheRealAssemblyLoadsInThisProcess()
        {
            // The test project references ExternalConnectors, so the file sits next to this DLL.
            // If this ever fails, the shipped layout is broken before any user sees it.
            Assert.That(ExternalConnectorsAssembly.IsAvailable, Is.True);
            Assert.That(File.Exists(ExternalConnectorsAssembly.ExpectedPath), Is.True, ExternalConnectorsAssembly.ExpectedPath);
        }

        [Test]
        public void EnsureAvailable_ReportsNothingWhenTheAssemblyLoads()
        {
            MessageCollector collector = new();

            bool available = ExternalConnectorsAssembly.EnsureAvailable(collector);

            Assert.That(available, Is.True);
        }

        [TestCase(typeof(FileNotFoundException))]
        [TestCase(typeof(FileLoadException))]
        [TestCase(typeof(BadImageFormatException))]
        public void TryLoad_ClassifiesLoaderFailuresAsUnavailable(Type exceptionType)
        {
            Exception thrown = (Exception)Activator.CreateInstance(exceptionType, "loader said no")!;

            bool loaded = ExternalConnectorsAssembly.TryLoad(() => throw thrown, out string? failure);

            Assert.That(loaded, Is.False);
            Assert.That(failure, Is.EqualTo("loader said no"));
        }

        [Test]
        public void TryLoad_LetsUnrelatedExceptionsThrough()
        {
            Assert.Throws<InvalidOperationException>(() =>
                ExternalConnectorsAssembly.TryLoad(() => throw new InvalidOperationException("bug"), out _));
        }

        [Test]
        public void TryLoad_ReportsSuccessWithNoFailureText()
        {
            bool loaded = ExternalConnectorsAssembly.TryLoad(() => Assembly.GetExecutingAssembly(), out string? failure);

            Assert.That(loaded, Is.True);
            Assert.That(failure, Is.Null);
        }

        // ---- which connections need the assembly at all -------------------------------------
        // Only these are refused when the file is missing; every other connection must open.

        private string _savedEmptyCredentials = string.Empty;
        private string _savedDefaultUsername = string.Empty;
        private ExternalCredentialProvider _savedDefaultProvider;

        [SetUp]
        public void SaveCredentialDefaults()
        {
            _savedEmptyCredentials = OptionsCredentialsPage.Default.EmptyCredentials;
            _savedDefaultUsername = OptionsCredentialsPage.Default.DefaultUsername;
            _savedDefaultProvider = OptionsCredentialsPage.Default.ExternalCredentialProviderDefault;
            OptionsCredentialsPage.Default.EmptyCredentials = "noinfo";
            OptionsCredentialsPage.Default.DefaultUsername = string.Empty;
            OptionsCredentialsPage.Default.ExternalCredentialProviderDefault = ExternalCredentialProvider.None;
        }

        [TearDown]
        public void RestoreCredentialDefaults()
        {
            OptionsCredentialsPage.Default.EmptyCredentials = _savedEmptyCredentials;
            OptionsCredentialsPage.Default.DefaultUsername = _savedDefaultUsername;
            OptionsCredentialsPage.Default.ExternalCredentialProviderDefault = _savedDefaultProvider;
        }

        private static ConnectionInfo Plain(string username = "admin") =>
            new() { Name = "plain", Hostname = "host", Username = username };

        [Test]
        public void IsNeededBy_APlainConnectionDoesNotNeedIt()
        {
            Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain()), Is.False);
            Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain(username: string.Empty)), Is.False);
        }

        [TestCase(ExternalCredentialProvider.DelineaSecretServer)]
        [TestCase(ExternalCredentialProvider.ClickstudiosPasswordState)]
        [TestCase(ExternalCredentialProvider.OnePassword)]
        [TestCase(ExternalCredentialProvider.VaultOpenbao)]
        [TestCase(ExternalCredentialProvider.PasswordSafe)]
        [TestCase(ExternalCredentialProvider.LAPS)]
        public void IsNeededBy_AConnectionWithAVaultProviderNeedsIt(ExternalCredentialProvider provider)
        {
            ConnectionInfo connection = Plain();
            connection.ExternalCredentialProvider = provider;

            Assert.That(ExternalConnectorsAssembly.IsNeededBy(connection), Is.True);
        }

        [Test]
        public void IsNeededBy_AGatewayVaultProviderNeedsIt()
        {
            ConnectionInfo connection = Plain();
            connection.RDGatewayExternalCredentialProvider = ExternalCredentialProvider.DelineaSecretServer;

            Assert.That(ExternalConnectorsAssembly.IsNeededBy(connection), Is.True);
        }

        [Test]
        public void IsNeededBy_TheEc2LookupNeedsItUnlessTheAlternativeAddressIsUsed()
        {
            ConnectionInfo connection = Plain();
            connection.EC2InstanceId = "i-0123456789abcdef0";

            Assert.That(ExternalConnectorsAssembly.IsNeededBy(connection), Is.True);
            Assert.That(ExternalConnectorsAssembly.IsNeededBy(connection, ConnectionInfo.Force.UseAlternativeAddress), Is.True,
                        "no alternative address set, so the connect path still does the lookup");

            connection.AlternativeAddress = "10.0.0.5";
            Assert.That(ExternalConnectorsAssembly.IsNeededBy(connection, ConnectionInfo.Force.UseAlternativeAddress), Is.False);
        }

        [Test]
        public void IsNeededBy_AnEmptyUsernameNeedsItOnlyWhenItFallsBackToADefaultProvider()
        {
            OptionsCredentialsPage.Default.EmptyCredentials = "custom";
            OptionsCredentialsPage.Default.ExternalCredentialProviderDefault = ExternalCredentialProvider.DelineaSecretServer;

            Assert.Multiple(() =>
            {
                Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain(username: string.Empty)), Is.True);
                Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain()), Is.False, "own username, default never consulted");
                Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain(), ConnectionInfo.Force.NoCredentials), Is.True,
                            "connect-without-credentials empties the username first");
            });

            OptionsCredentialsPage.Default.DefaultUsername = "fallback";
            Assert.That(ExternalConnectorsAssembly.IsNeededBy(Plain(username: string.Empty)), Is.False,
                        "a default username is used before the default provider");
        }

        [Test]
        public void DescribeMissing_NamesTheFileThePathAndTheLikelyCause()
        {
            string text = ExternalConnectorsAssembly.DescribeMissing(@"C:\apps\mRemoteNG\ExternalConnectors.dll", fileExists: false, "Could not load file or assembly");

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("ExternalConnectors.dll"));
                Assert.That(text, Does.Contain(@"C:\apps\mRemoteNG\ExternalConnectors.dll"));
                Assert.That(text, Does.Contain("not there"));
                Assert.That(text, Does.Contain("quarantined"));
                Assert.That(text, Does.Contain("Could not load file or assembly"));
            });
        }

        [Test]
        public void DescribeMissing_DistinguishesAPresentFileThatWillNotLoad()
        {
            string text = ExternalConnectorsAssembly.DescribeMissing(@"C:\apps\mRemoteNG\ExternalConnectors.dll", fileExists: true, null);

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("refused it"));
                Assert.That(text, Does.Not.Contain("not there"));
                Assert.That(text, Does.Contain("Loader: -"));
            });
        }
    }
}

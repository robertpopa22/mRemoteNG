using System.Linq;
using System.Xml.Linq;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using NUnit.Framework;

namespace mRemoteNGTests.Config.Serializers.ConnectionSerializers.Xml
{
    /// <summary>
    /// The RD Gateway access token is a credential. It is written encrypted, dropped with the
    /// passwords when an export leaves them out, and a token an earlier version wrote in plain text
    /// still loads.
    /// </summary>
    [TestFixture]
    public class RdGatewayAccessTokenPersistenceTests
    {
        private const string MasterPassword = "gateway-token-master";
        private const string Token = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJnYXRld2F5In0.c2lnbmF0dXJl";

        private static string Serialize(ConnectionInfo connection, SaveFilter filter)
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            XmlConnectionNodeSerializer28 nodeSerializer = new(crypto, MasterPassword.ConvertToSecureString(), filter);
            XmlConnectionsSerializer serializer = new(crypto, nodeSerializer);
            RootNodeInfo root = new(RootNodeType.Connection) { PasswordString = MasterPassword };
            root.AddChild(connection);
            return serializer.Serialize(root);
        }

        private static ConnectionInfo Deserialize(string xml)
        {
            ConnectionTreeModel model = new XmlConnectionsDeserializer("", () => MasterPassword.ConvertToSecureString())
                .Deserialize(xml);
            return model.RootNodes.OfType<ContainerInfo>()
                        .SelectMany(r => r.GetRecursiveChildList())
                        .First(n => n is not ContainerInfo);
        }

        private static string StoredToken(string xml) =>
            XDocument.Parse(xml).Descendants("Node").First().Attribute("RDGatewayAccessToken")!.Value;

        [Test]
        public void TokenIsNotWrittenInPlainTextAndReadsBack()
        {
            string xml = Serialize(new ConnectionInfo { Name = "gw", RDGatewayAccessToken = Token }, new SaveFilter());

            Assert.That(StoredToken(xml), Is.Not.Empty.And.Not.Contain(Token));
            Assert.That(xml, Does.Not.Contain(Token));
            Assert.That(Deserialize(xml).RDGatewayAccessToken, Is.EqualTo(Token));
        }

        [Test]
        public void SaveWithoutPasswordsLeavesTheTokenOut()
        {
            string xml = Serialize(new ConnectionInfo { Name = "gw", RDGatewayAccessToken = Token },
                                   new SaveFilter { SavePassword = false });

            Assert.That(StoredToken(xml), Is.Empty);
        }

        [Test]
        public void EmptyTokenStaysEmpty()
        {
            string xml = Serialize(new ConnectionInfo { Name = "gw" }, new SaveFilter());

            Assert.That(StoredToken(xml), Is.Empty);
            Assert.That(Deserialize(xml).RDGatewayAccessToken, Is.Empty);
        }

        [TestCase(Token)]
        [TestCase("QUJDREVGR0hJSktMTU5PUFFSU1RVVldYWVo=")]
        public void TokenWrittenInPlainTextByAnEarlierVersionStillLoads(string plain)
        {
            string xml = Serialize(new ConnectionInfo { Name = "gw", Password = "pw" }, new SaveFilter());
            Assert.That(xml, Does.Contain("RDGatewayAccessToken=\"\""));
            string legacy = xml.Replace("RDGatewayAccessToken=\"\"", $"RDGatewayAccessToken=\"{plain}\"", System.StringComparison.Ordinal);

            ConnectionInfo loaded = Deserialize(legacy);

            Assert.That(loaded.RDGatewayAccessToken, Is.EqualTo(plain));
            Assert.That(loaded.Password, Is.EqualTo("pw"), "the plain token broke the password batch");
        }
    }
}

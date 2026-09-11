using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;
using mRemoteNGSpecs.Support;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures
{
    /// <summary>
    /// A seeded credential has to survive the trip through the connections file, because that is
    /// the only way a lab scenario can hand one to the application. If it does not, every
    /// credentialed scenario fails as an authentication problem and the lab gets blamed for it —
    /// which is exactly the suspicion this test exists to settle for #182's RDP target.
    ///
    /// No application, no lab: this reads back what the seeder wrote.
    /// </summary>
    [TestFixture]
    [SupportedOSPlatform("windows")]
    public class SeededLogonRoundTripTests
    {
        private const string Password = "N0t-A-Real-Pw!42";
        private const string User = "Administrator";
        private const string Domain = "MRNG-LAB-TGT";

        [Test]
        public void TheUsernameDomainAndPasswordComeBackOutOfTheSeededFile()
        {
            string xml = new ConnectionsSeeder()
                .Add("lab-win-rdp", "192.168.221.21", ProtocolType.RDP, 3389, User, Password, Domain)
                .Build();

            RootNodeInfo root = new(RootNodeType.Connection);
            XmlConnectionsDeserializer deserializer =
                new(authenticationRequestor: () => root.PasswordString.ConvertToSecureString());
            ConnectionTreeModel model = deserializer.Deserialize(xml);

            ConnectionInfo connection = model.GetRecursiveChildList()
                                             .OfType<ConnectionInfo>()
                                             .Single(c => c.Name == "lab-win-rdp");

            Assert.Multiple(() =>
            {
                Assert.That(connection.Username, Is.EqualTo(User));
                Assert.That(connection.Domain, Is.EqualTo(Domain), "the domain is what scopes a workgroup account");
                Assert.That(connection.Password, Is.EqualTo(Password),
                            "the password did not survive the seeded connections file, so every credentialed "
                            + "lab scenario has been authenticating with the wrong thing");
                Assert.That(connection.Hostname, Is.EqualTo("192.168.221.21"));
                Assert.That(connection.Port, Is.EqualTo(3389));
            });
        }
    }
}

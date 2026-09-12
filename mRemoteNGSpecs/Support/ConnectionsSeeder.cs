using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using mRemoteNG.Config;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Container;
using mRemoteNG.Security;
using mRemoteNG.Security.SymmetricEncryption;
using mRemoteNG.Tools;
using mRemoteNG.Tree.Root;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Builds a connections file for a scenario using the application's own serializer.
    ///
    /// Hand-written XML would drift from the schema the moment the format changes, and a subtly
    /// invalid fixture fails as "the app showed no connections" — which looks exactly like the bug
    /// a test is hunting. Serialising through the production writer means the fixture is valid by
    /// construction, and encrypted with the default key so the app opens it without prompting.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class ConnectionsSeeder
    {
        private readonly List<ConnectionInfo> _connections = [];

        /// <summary>
        /// <paramref name="domain"/> matters more than it looks for a workgroup target. Left
        /// empty, the RDP client qualifies the user with the CLIENT machine's name and sends
        /// "WIN-XXXX\Administrator" to the far end, where no such account exists; the server
        /// answers "Your credentials did not work" even though the password is correct. Set it to
        /// the target's own computer name so the local account there is the one being asked for.
        /// </summary>
        public ConnectionsSeeder Add(string name, string hostname, ProtocolType protocol, int port,
                                     string? username = null, string? password = null,
                                     string? domain = null, Action<ConnectionInfo>? configure = null)
        {
            ConnectionInfo connection = new()
            {
                Name = name,
                Hostname = hostname,
                Protocol = protocol,
                Port = port,
                Username = username ?? "",
                Password = password ?? "",
                Domain = domain ?? "",
                Panel = "General",
                // What the application gives every connection it creates (it copies the defaults,
                // whose RdpVersion is Highest). A bare ConnectionInfo leaves the enum at zero, which
                // is Rdc6: the base RdpProtocol, an RDC 6 ActiveX class, and none of the dynamic
                // resize code from RdpProtocol8 onward -- a lab that measured a code path no user
                // has run since Windows 7 (found chasing #177).
                RdpVersion = RdpVersion.Highest,
            };

            configure?.Invoke(connection);
            _connections.Add(connection);
            return this;
        }

        /// <summary>Adds connections that will never answer, for tests that only need tabs to open.</summary>
        public ConnectionsSeeder AddUnreachable(string namePrefix, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Port 1 on loopback refuses immediately: a tab opens and the attempt fails fast,
                // with no external dependency and no waiting on a timeout.
                Add($"{namePrefix}-{i:D2}", "127.0.0.1", ProtocolType.SSH2, 1);
            }
            return this;
        }

        public string Build()
        {
            AeadCryptographyProvider crypto = new() { KeyDerivationIterations = 1000 };
            RootNodeInfo root = new(RootNodeType.Connection);

            foreach (ConnectionInfo connection in _connections)
                root.AddChild(connection);

            XmlConnectionNodeSerializer28 nodeSerializer =
                new(crypto, root.PasswordString.ConvertToSecureString(), new SaveFilter());
            XmlConnectionsSerializer serializer = new(crypto, nodeSerializer);

            return serializer.Serialize(root);
        }
    }
}

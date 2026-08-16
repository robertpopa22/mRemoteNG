using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using mRemoteNG.Config;
using mRemoteNG.Config.Serializers.ConnectionSerializers.Xml;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
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

        public ConnectionsSeeder Add(string name, string hostname, ProtocolType protocol, int port,
                                     string? username = null, string? password = null)
        {
            _connections.Add(new ConnectionInfo
            {
                Name = name,
                Hostname = hostname,
                Protocol = protocol,
                Port = port,
                Username = username ?? "",
                Password = password ?? "",
                Panel = "General",
            });
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

using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text.Json;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Container;
using mRemoteNG.Tree;
using mRemoteNG.Tree.Root;

namespace mRemoteNG.Config.Serializers.MiscSerializers
{
    [SupportedOSPlatform("windows")]
    public class MicrosoftRdClientBackupDeserializer : IDeserializer<string, ConnectionTreeModel>
    {
        public ConnectionTreeModel Deserialize(string rdbFileContent)
        {
            ConnectionTreeModel connectionTreeModel = new();
            RootNodeInfo root = new(RootNodeType.Connection);
            connectionTreeModel.AddRootNode(root);

            if (string.IsNullOrWhiteSpace(rdbFileContent))
                return connectionTreeModel;

            using JsonDocument doc = JsonDocument.Parse(rdbFileContent);
            JsonElement rootElement = doc.RootElement;

            // Build lookup tables for Groups and Credentials
            Dictionary<string, string> groupNames = new(StringComparer.Ordinal);
            Dictionary<string, ContainerInfo> groupContainers = new(StringComparer.Ordinal);

            if (rootElement.TryGetProperty("Groups", out JsonElement groupsElement) && groupsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement group in groupsElement.EnumerateArray())
                {
                    string id = GetStringProperty(group, "PersistentModelId");
                    string name = GetStringProperty(group, "Name");
                    if (!string.IsNullOrEmpty(id))
                    {
                        groupNames[id] = name;
                        ContainerInfo container = new() { Name = name };
                        groupContainers[id] = container;
                        root.AddChild(container);
                    }
                }
            }

            Dictionary<string, (string UserName, string Domain)> credentials = new(StringComparer.Ordinal);
            if (rootElement.TryGetProperty("Credentials", out JsonElement credsElement) && credsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement cred in credsElement.EnumerateArray())
                {
                    string id = GetStringProperty(cred, "PersistentModelId");
                    string userName = GetStringProperty(cred, "UserName");
                    string domain = GetStringProperty(cred, "Domain");
                    if (!string.IsNullOrEmpty(id))
                    {
                        credentials[id] = (userName, domain);
                    }
                }
            }

            // Parse Connections
            if (rootElement.TryGetProperty("Connections", out JsonElement connectionsElement) && connectionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement conn in connectionsElement.EnumerateArray())
                {
                    string hostname = GetStringProperty(conn, "HostName");
                    string friendlyName = GetStringProperty(conn, "FriendlyName");
                    string groupId = GetStringProperty(conn, "GroupId");
                    string credentialsId = GetStringProperty(conn, "CredentialsId");
                    string gatewayHostname = GetStringProperty(conn, "GatewayHostName");

                    ConnectionInfo connectionInfo = new()
                    {
                        Name = !string.IsNullOrEmpty(friendlyName) ? friendlyName : hostname,
                        Hostname = hostname,
                        Protocol = ProtocolType.RDP,
                        Port = 3389
                    };

                    // Resolve credentials
                    if (!string.IsNullOrEmpty(credentialsId) && credentials.TryGetValue(credentialsId, out var cred))
                    {
                        connectionInfo.Username = cred.UserName;
                        connectionInfo.Domain = cred.Domain;
                    }

                    // Set gateway
                    if (!string.IsNullOrEmpty(gatewayHostname))
                    {
                        connectionInfo.RDGatewayHostname = gatewayHostname;
                        connectionInfo.RDGatewayUsageMethod = RDGatewayUsageMethod.Always;
                    }

                    // Add to group container or root
                    if (!string.IsNullOrEmpty(groupId) && groupContainers.TryGetValue(groupId, out ContainerInfo? container))
                    {
                        container.AddChild(connectionInfo);
                    }
                    else
                    {
                        root.AddChild(connectionInfo);
                    }
                }
            }

            return connectionTreeModel;
        }

        private static string GetStringProperty(JsonElement element, string propertyName)
        {
            return element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? ""
                : "";
        }
    }
}

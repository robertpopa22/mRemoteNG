using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Container;

namespace mRemoteNG.UI.Controls.ConnectionTree
{
    [SupportedOSPlatform("windows")]
    public class ConnectionTreeSearchTextFilter : IModelFilter
    {
        public string FilterText { get; set; } = "";

        /// <summary>
        /// Optional protocol type filter. When set, only connections
        /// matching this protocol are shown.
        /// </summary>
        public ProtocolType? FilterProtocol { get; set; }

        /// <summary>
        /// A list of <see cref="ConnectionInfo"/> objects that should
        /// always be included in the output, regardless of matching
        /// the desired <see cref="FilterText"/>.
        /// </summary>
        public List<ConnectionInfo> SpecialInclusionList { get; } = [];

        public bool Filter(object modelObject)
        {
            if (!(modelObject is ConnectionInfo objectAsConnectionInfo))
                return false;

            if (SpecialInclusionList.Contains(objectAsConnectionInfo))
                return true;

            // Exclude nodes that belong to a folder marked ExcludeFromSearch
            if (IsUnderExcludedContainer(objectAsConnectionInfo))
                return false;

            if (NodeMatchesFilter(objectAsConnectionInfo))
                return true;

            // For containers, keep visible if any descendant matches so that
            // search finds connections inside collapsed/non-matching folders.
            if (objectAsConnectionInfo is ContainerInfo container)
                return container.GetRecursiveChildList()
                                .Where(child => !IsUnderExcludedContainer(child))
                                .Any(NodeMatchesFilter);

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="node"/> itself is a container with
        /// <see cref="ContainerInfo.ExcludeFromSearch"/> set, or if any ancestor
        /// container has that flag set.
        /// </summary>
        private static bool IsUnderExcludedContainer(ConnectionInfo node)
        {
            if (node is ContainerInfo self && self.ExcludeFromSearch)
                return true;

            ContainerInfo? parent = node.Parent;
            while (parent != null)
            {
                if (parent.ExcludeFromSearch)
                    return true;
                parent = parent.Parent;
            }
            return false;
        }

        private bool NodeMatchesFilter(ConnectionInfo node)
        {
            // Protocol filter: exclude connections that don't match
            if (FilterProtocol.HasValue && node.Protocol != FilterProtocol.Value)
                return false;

            string filterTextLower = FilterText.ToLowerInvariant();

            // Support "regex:" prefix syntax
            if (filterTextLower.StartsWith("regex:", StringComparison.Ordinal))
            {
                try
                {
                    string pattern = FilterText.Substring(6);
                    Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    return regex.IsMatch(node.Name) ||
                           regex.IsMatch(node.Hostname) ||
                           regex.IsMatch(node.Description) ||
                           regex.IsMatch(node.EnvironmentTags ?? "");
                }
                catch (ArgumentException)
                {
                    return false;
                }
            }

            // Support "protocol:RDP" and "tag:production" prefix syntax
            if (filterTextLower.StartsWith("protocol:", StringComparison.Ordinal))
            {
                string protocolFilter = filterTextLower.Substring(9).Trim();
                return node.Protocol.ToString().ToLowerInvariant().Contains(protocolFilter);
            }

            if (filterTextLower.StartsWith("tag:", StringComparison.Ordinal))
            {
                string tagFilter = filterTextLower.Substring(4).Trim();
                return (node.EnvironmentTags ?? "").ToLowerInvariant().Contains(tagFilter);
            }

            return node.Name.ToLowerInvariant().Contains(filterTextLower) ||
                   node.Hostname.ToLowerInvariant().Contains(filterTextLower) ||
                   node.Description.ToLowerInvariant().Contains(filterTextLower) ||
                   (node.EnvironmentTags ?? "").ToLowerInvariant().Contains(filterTextLower);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using BrightIdeasSoftware;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;

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

            // Protocol filter: exclude connections that don't match
            if (FilterProtocol.HasValue && objectAsConnectionInfo.Protocol != FilterProtocol.Value)
                return false;

            string filterTextLower = FilterText.ToLowerInvariant();

            // Support "regex:" prefix syntax
            if (filterTextLower.StartsWith("regex:", StringComparison.Ordinal))
            {
                try
                {
                    string pattern = FilterText.Substring(6);
                    Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);
                    return regex.IsMatch(objectAsConnectionInfo.Name) ||
                           regex.IsMatch(objectAsConnectionInfo.Hostname) ||
                           regex.IsMatch(objectAsConnectionInfo.Description) ||
                           regex.IsMatch(objectAsConnectionInfo.EnvironmentTags ?? "");
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
                return objectAsConnectionInfo.Protocol.ToString().ToLowerInvariant().Contains(protocolFilter);
            }

            if (filterTextLower.StartsWith("tag:", StringComparison.Ordinal))
            {
                string tagFilter = filterTextLower.Substring(4).Trim();
                return (objectAsConnectionInfo.EnvironmentTags ?? "").ToLowerInvariant().Contains(tagFilter);
            }

            return objectAsConnectionInfo.Name.ToLowerInvariant().Contains(filterTextLower) ||
                   objectAsConnectionInfo.Hostname.ToLowerInvariant().Contains(filterTextLower) ||
                   objectAsConnectionInfo.Description.ToLowerInvariant().Contains(filterTextLower) ||
                   (objectAsConnectionInfo.EnvironmentTags ?? "").ToLowerInvariant().Contains(filterTextLower);
        }
    }
}
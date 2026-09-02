using mRemoteNG.Config.Settings;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using System;
using System.Collections.Generic;
using System.Linq;
using mRemoteNG.App;
using mRemoteNG.UI.Controls.ConnectionTree;
using mRemoteNG.UI.Window;
using System.Runtime.Versioning;

namespace mRemoteNG.Tree
{
    [SupportedOSPlatform("windows")]
    public class PreviousSessionOpener : IConnectionTreeDelegate
    {
        private readonly IConnectionInitiator _connectionInitiator;
        private readonly Func<IEnumerable<ConnectionInfo>> _previousQuickConnectSessionLoader;
        private readonly Func<IEnumerable<string>> _layoutRestoredConnectionIdLoader;

        public PreviousSessionOpener(
            IConnectionInitiator connectionInitiator,
            Func<IEnumerable<ConnectionInfo>>? previousQuickConnectSessionLoader = null,
            Func<IEnumerable<string>>? layoutRestoredConnectionIdLoader = null)
        {
            ArgumentNullException.ThrowIfNull(connectionInitiator);
            _connectionInitiator = connectionInitiator;
            _previousQuickConnectSessionLoader = previousQuickConnectSessionLoader ?? QuickConnectHistoryLoader.LoadPreviouslyConnectedQuickConnectSessions;
            _layoutRestoredConnectionIdLoader = layoutRestoredConnectionIdLoader ?? CollectLayoutRestoredConnectionIds;
        }

        public void Execute(IConnectionTree connectionTree)
        {
            // The saved dock layout reopens the tabs it recorded (ConnectionWindow.LoadConnections),
            // and those same connections are still flagged PleaseConnect in the connections file.
            // Reopening them here as well gave one extra tab per previously open connection on
            // every start (#172). The layout owns them: it restores each tab in the panel it was
            // last in, which this path cannot do.
            HashSet<string> idsClaimedByLayout = _layoutRestoredConnectionIdLoader()
                .ToHashSet(StringComparer.Ordinal);

            IEnumerable<ConnectionInfo> connectionInfoList = connectionTree.GetRootConnectionNode().GetRecursiveChildList()
                                                   .Where(node => !(node is ContainerInfo));
            IEnumerable<ConnectionInfo> previouslyOpenedConnections = connectionInfoList
                .Where(item =>
                           item.PleaseConnect &&
                           //ignore items that have already connected
                           !_connectionInitiator.ActiveConnections.Contains(item.ConstantID, StringComparer.Ordinal) &&
                           !idsClaimedByLayout.Contains(item.ConstantID));

            foreach (ConnectionInfo connectionInfo in previouslyOpenedConnections)
            {
                _connectionInitiator.OpenConnection(connectionInfo);
            }

            OpenPreviouslyConnectedQuickConnectSessions();
        }

        private static IEnumerable<string> CollectLayoutRestoredConnectionIds()
        {
            if (Runtime.WindowList == null)
                return [];

            return Runtime.WindowList
                .OfType<ConnectionWindow>()
                .SelectMany(window => window.PendingConnectionIds)
                .ToArray();
        }

        private void OpenPreviouslyConnectedQuickConnectSessions()
        {
            IEnumerable<ConnectionInfo> previouslyOpenedQuickConnections = _previousQuickConnectSessionLoader()
                .Where(item =>
                    item.PleaseConnect &&
                    !_connectionInitiator.ActiveConnections.Contains(item.ConstantID, StringComparer.Ordinal));

            foreach (ConnectionInfo connectionInfo in previouslyOpenedQuickConnections)
            {
                _connectionInitiator.OpenConnection(connectionInfo);
            }
        }
    }
}

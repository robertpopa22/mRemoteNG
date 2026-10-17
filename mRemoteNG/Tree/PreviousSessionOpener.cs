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
        private readonly Func<IReadOnlyCollection<string>?> _layoutListedConnectionIdLoader;

        public PreviousSessionOpener(
            IConnectionInitiator connectionInitiator,
            Func<IEnumerable<ConnectionInfo>>? previousQuickConnectSessionLoader = null,
            Func<IEnumerable<string>>? layoutRestoredConnectionIdLoader = null,
            Func<IReadOnlyCollection<string>?>? layoutListedConnectionIdLoader = null)
        {
            ArgumentNullException.ThrowIfNull(connectionInitiator);
            _connectionInitiator = connectionInitiator;
            _previousQuickConnectSessionLoader = previousQuickConnectSessionLoader ?? QuickConnectHistoryLoader.LoadPreviouslyConnectedQuickConnectSessions;
            _layoutRestoredConnectionIdLoader = layoutRestoredConnectionIdLoader ?? CollectLayoutRestoredConnectionIds;
            // Null means no restored panel carried an id list (no layout, or one saved before the
            // field existed); an empty collection means the layout saved no open tabs.
            _layoutListedConnectionIdLoader = layoutListedConnectionIdLoader ?? (() => null);
        }

        public void Execute(IConnectionTree connectionTree)
        {
            // The saved dock layout reopens the tabs it recorded (ConnectionWindow.LoadConnections),
            // and those same connections are still flagged PleaseConnect in the connections file.
            // Reopening them here as well gave one extra tab per previously open connection on
            // every start (#172). The layout owns them: it restores each tab in the panel it was
            // last in, which this path cannot do.
            //
            // The loader below makes the layout finish its work first and then reports what it
            // ACTUALLY opened. Skipping on what the layout merely intended to open is what left a
            // reporter with no tabs at all: whenever that panel then opened nothing, both paths
            // stood aside.
            //
            // PleaseConnect is the Connected attribute from the last connections save. Opening or
            // closing a tab is not an edit, so with the default save-on-edit setting that attribute
            // still names sessions the user has since closed, and this path put the old set back
            // (#172, 1.84.0). The layout is saved on every exit with the tabs open at that moment,
            // so when it carried an id list, only an id it named and then did not open is left for
            // this path.
            HashSet<string> idsAlreadyOpenedByLayout = _layoutRestoredConnectionIdLoader()
                .ToHashSet(StringComparer.Ordinal);
            HashSet<string>? idsListedByLayout = _layoutListedConnectionIdLoader()?
                .ToHashSet(StringComparer.Ordinal);

            IEnumerable<ConnectionInfo> connectionInfoList = connectionTree.GetRootConnectionNode().GetRecursiveChildList()
                                                   .Where(node => !(node is ContainerInfo));
            IEnumerable<ConnectionInfo> previouslyOpenedConnections = connectionInfoList
                .Where(item =>
                           item.PleaseConnect &&
                           //ignore items that have already connected
                           !_connectionInitiator.ActiveConnections.Contains(item.ConstantID, StringComparer.Ordinal) &&
                           !idsAlreadyOpenedByLayout.Contains(item.ConstantID) &&
                           (idsListedByLayout == null || idsListedByLayout.Contains(item.ConstantID)));

            ConnectionInfo[] toOpen = previouslyOpenedConnections.ToArray();
            App.DevLog.Write($"[#172-diag] tree path: candidates={connectionInfoList.Count()} pleaseConnect={connectionInfoList.Count(c => c.PleaseConnect)} openedByLayout={idsAlreadyOpenedByLayout.Count} listedByLayout={(idsListedByLayout == null ? "none" : idsListedByLayout.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))} willOpen={toOpen.Length} names=[{string.Join(",", toOpen.Select(c => c.Name))}]");

            foreach (ConnectionInfo connectionInfo in toOpen)
            {
                App.DevLog.Write($"[#172-diag] tree path opening '{connectionInfo.Name}' id={connectionInfo.ConstantID}");
                _connectionInitiator.OpenConnection(connectionInfo);
            }

            OpenPreviouslyConnectedQuickConnectSessions();
        }

        private static IEnumerable<string> CollectLayoutRestoredConnectionIds()
        {
            if (Runtime.WindowList == null)
                return [];

            ConnectionWindow[] panels = Runtime.WindowList
                .OfType<ConnectionWindow>()
                .Where(window => !window.IsDisposed && !window.Disposing)
                .ToArray();

            // Give every live panel the chance to restore its tabs now, so what it reports below is
            // a fact rather than an intention. A panel that already did this is a no-op here.
            foreach (ConnectionWindow panel in panels)
            {
                panel.FlushPendingLayoutConnections();
            }

            return panels.SelectMany(panel => panel.OpenedFromLayoutConnectionIds).ToArray();
        }

        /// <summary>
        /// The ids the saved layout named across the restored panels, or null when no restored
        /// panel's persist string carried the id field.
        /// </summary>
        internal static IReadOnlyCollection<string>? CollectLayoutListedConnectionIds()
        {
            if (Runtime.WindowList == null)
                return null;

            ConnectionWindow[] listedPanels = Runtime.WindowList
                .OfType<ConnectionWindow>()
                .Where(window => !window.IsDisposed && !window.Disposing && window.LayoutListedConnections)
                .ToArray();

            if (listedPanels.Length == 0)
                return null;

            return listedPanels
                .SelectMany(panel => panel.ListedFromLayoutConnectionIds)
                .Distinct(StringComparer.Ordinal)
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

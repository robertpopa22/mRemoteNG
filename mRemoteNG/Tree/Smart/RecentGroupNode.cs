using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Versioning;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.Tree;

namespace mRemoteNG.Tree.Smart
{
    [SupportedOSPlatform("windows")]
    public class RecentGroupNode : ContainerInfo
    {
        public RecentGroupNode()
        {
            Name = "Recent";
            IsExpanded = true;
        }

        public void Initialize()
        {
            RecentConnectionsService.Instance.RecentConnectionsChanged += OnRecentConnectionsChanged;
            RefreshList();
        }

        /// <summary>
        /// Stops listening to the recent list. The service lives for the whole process, so a node
        /// of a replaced tree that stays subscribed keeps that whole tree alive (#227).
        /// </summary>
        public void Detach()
        {
            RecentConnectionsService.Instance.RecentConnectionsChanged -= OnRecentConnectionsChanged;
        }

        private void OnRecentConnectionsChanged(object? sender, EventArgs e)
        {
            // Must invoke on UI thread if needed? 
            // RecentConnectionsService raises event.
            // If we manipulate Children, CollectionChanged fires -> UI update.
            // This usually happens on UI thread if triggered by UI action.
            RefreshList();
        }

        private void RefreshList()
        {
            var recents = RecentConnectionsService.Instance.GetRecentConnections();

            // Remove existing items using RemoveChild (not Children.Clear())
            // so INotifyCollectionChanged events fire and the tree stays in sync.
            foreach (var item in Children.ToList())
                RemoveChild(item);

            foreach (var c in recents)
            {
                if (c == null) continue;
                AddChild(new SmartConnectionInfo(c));
            }
        }
    }
}

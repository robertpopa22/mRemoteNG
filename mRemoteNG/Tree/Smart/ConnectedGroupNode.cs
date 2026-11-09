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
    public class ConnectedGroupNode : ContainerInfo
    {
        public ConnectedGroupNode()
        {
            Name = "Connected";
            IsExpanded = true;
        }

        private ConnectionTreeModel? _model;

        // Each load builds a new tree and a new node for it (ConnectionTreeWindow), so this node
        // listens to its own model only. It used to subscribe to ConnectionsService.ConnectionsLoaded
        // as well and never unsubscribe: every later load then reached every earlier node, each of
        // which subscribed to the new model and kept its own old tree alive. A session with frequent
        // SQL sync reloads accumulated one full tree per reload until a 32-bit process ran out of
        // memory inside this node's RefreshList (#227).
        public void Initialize()
        {
            var model = Runtime.ConnectionsService.ConnectionTreeModel;
            if (model != null)
            {
                _model = model;
                // Subscribe to property changes to detect connection status changes
                model.PropertyChanged += OnModelPropertyChanged;

                // Initial population
                RefreshList();
            }
        }

        /// <summary>Stops listening, so a replaced tree does not keep reaching this node.</summary>
        public void Detach()
        {
            if (_model != null)
                _model.PropertyChanged -= OnModelPropertyChanged;
            _model = null;
        }

        private void OnModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
             // We are looking for OpenConnections changes on ConnectionInfo objects
             if (sender is ConnectionInfo ci && e.PropertyName == nameof(ConnectionInfo.OpenConnections))
             {
                 // Check if it's already in our list (to avoid infinite loop if SmartConnectionInfo triggers this)
                 // SmartConnectionInfo wraps ConnectionInfo.
                 // The sender is the ORIGINAL ConnectionInfo (or SmartConnectionInfo).
                 // If sender is SmartConnectionInfo, we ignore it (it's our child).
                 if (ci is SmartConnectionInfo) return;

                 UpdateConnection(ci);
             }
        }

        private void RefreshList()
        {
            // Clear children (careful: do not dispose SmartConnectionInfo if we want to reuse? 
            // For now, recreate. SmartConnectionInfo doesn't hold heavy resources other than event subscription).
            // We should unsubscribe SmartConnectionInfo from original when removing!
            // SmartConnectionInfo needs Dispose or we rely on GC (event reference might keep it alive).
            // Since SmartConnectionInfo subscribes to _original, _original keeps SmartConnectionInfo alive!
            // So we MUST Unsubscribe.
            // I'll add Dispose to SmartConnectionInfo later or handle it here.
            
            // Better: Scan all connections and update.
            var model = Runtime.ConnectionsService.ConnectionTreeModel;
            if (model == null) return;

            var allConnections = model.GetRecursiveChildList();
            
            // Remove those that are no longer connected
            var toRemove = Children.OfType<SmartConnectionInfo>()
                .Where(s => s.GetOriginal().OpenConnections.Count == 0 || !allConnections.Contains(s.GetOriginal()))
                .ToList();
            
            foreach (var item in toRemove)
            {
                RemoveChild(item);
            }

            // Add those that are connected and not in list
            var connected = allConnections.Where(c => c.OpenConnections.Count > 0 && !(c is SmartConnectionInfo));
            foreach (var c in connected)
            {
                if (!Children.OfType<SmartConnectionInfo>().Any(s => s.GetOriginal() == c))
                {
                    AddChild(new SmartConnectionInfo(c));
                }
            }
        }
        
        private void UpdateConnection(ConnectionInfo ci)
        {
            bool isConnected = ci.OpenConnections.Count > 0;
            var existing = Children.OfType<SmartConnectionInfo>().FirstOrDefault(s => s.GetOriginal() == ci);
            
            if (isConnected)
            {
                if (existing == null)
                {
                    AddChild(new SmartConnectionInfo(ci));
                }
            }
            else
            {
                if (existing != null)
                {
                    RemoveChild(existing);
                }
            }
        }
    }
}

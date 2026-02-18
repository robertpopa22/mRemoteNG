using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using mRemoteNG.App;
using mRemoteNG.Config.Import;
using mRemoteNG.Connection;
using mRemoteNG.Tree;
using System.Runtime.Versioning;

namespace mRemoteNG.Container
{
    [SupportedOSPlatform("windows")]
    public class DynamicFolderManager
    {
        private readonly Dictionary<string, Timer> _timers = new();

        public DynamicFolderManager()
        {
            Runtime.ConnectionsService.ConnectionsLoaded += OnConnectionsLoaded;
        }

        private void OnConnectionsLoaded(object sender, Config.Connections.ConnectionsLoadedEventArgs e)
        {
            StopAllTimers();
            if (e.NewConnectionTreeModel != null)
            {
                ScanAndSchedule(e.NewConnectionTreeModel.RootNodes);
            }
        }

        private void StopAllTimers()
        {
            foreach (var timer in _timers.Values)
            {
                timer.Stop();
                timer.Dispose();
            }
            _timers.Clear();
        }

        private void ScanAndSchedule(IEnumerable<ConnectionInfo> nodes)
        {
            foreach (var node in nodes)
            {
                if (node is ContainerInfo container)
                {
                    if (container.DynamicSource != DynamicSourceType.None)
                    {
                        ScheduleRefresh(container);
                    }
                    ScanAndSchedule(container.Children);
                }
            }
        }

        public void ScheduleRefresh(ContainerInfo container)
        {
            if (_timers.ContainsKey(container.ConstantID))
            {
                _timers[container.ConstantID].Stop();
                _timers[container.ConstantID].Dispose();
                _timers.Remove(container.ConstantID);
            }

            if (container.DynamicRefreshInterval > 0)
            {
                Timer timer = new Timer(container.DynamicRefreshInterval * 60 * 1000); // Minutes to ms
                timer.Elapsed += (s, e) => RefreshFolder(container);
                timer.AutoReset = true;
                timer.Start();
                _timers[container.ConstantID] = timer;
            }
        }
        
        public void UnscheduleRefresh(ContainerInfo container)
        {
             if (_timers.ContainsKey(container.ConstantID))
            {
                _timers[container.ConstantID].Stop();
                _timers[container.ConstantID].Dispose();
                _timers.Remove(container.ConstantID);
            }
        }

        public void RefreshFolder(ContainerInfo container)
        {
            try
            {
                if (container.DynamicSource == DynamicSourceType.ActiveDirectory)
                {
                    if (mRemoteNG.UI.Forms.FrmMain.Default?.InvokeRequired == true)
                    {
                        mRemoteNG.UI.Forms.FrmMain.Default.Invoke(new Action(() => RefreshFolderInternal(container)));
                    }
                    else
                    {
                        RefreshFolderInternal(container);
                    }
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionMessage($"Error refreshing dynamic folder {container.Name}", ex);
            }
        }

        private void RefreshFolderInternal(ContainerInfo container)
        {
            try 
            {
                var childrenToRemove = container.Children.ToList();
                container.RemoveChildRange(childrenToRemove);
                
                if (container.DynamicSource == DynamicSourceType.ActiveDirectory)
                {
                     // Assuming true for recursive import
                     ActiveDirectoryImporter.Import(container.DynamicSourceValue, container, true);
                }
                
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.InformationMsg, $"Dynamic folder '{container.Name}' refreshed.");
            }
            catch (Exception ex)
            {
                 Runtime.MessageCollector.AddExceptionMessage($"Error executing refresh for {container.Name}", ex);
            }
        }
    }
}

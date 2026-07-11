using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using mRemoteNG.PluginSystem;
using mRemoteNG.UI.Forms;

using mRemoteNG.App;
using mRemoteNG.Connection;
using System.Linq;

namespace mRemoteNG.Tools
{
    public class PluginManager : IPluginHost
    {
        private static PluginManager _instance;
        public static PluginManager Instance => _instance ??= new PluginManager();

        private List<IPlugin> _plugins = new List<IPlugin>();
        // Plugins ship with the app and live next to the exe. Under a Program Files install this
        // directory is read-only for a normal user, so we load it only if it already exists and
        // never try to create it — that create attempt was the "Access to the path
        // 'C:\Program Files\mRemoteNG\Plugins' is denied" failure (#145). Plugins are executable
        // code loaded in-process, so they deliberately stay in the admin-controlled install dir
        // rather than a user-writable location.
        private readonly string _pluginsPath;

        public Form MainWindow => FrmMain.Default;
        
        public IEnumerable<IConnectionNode> RootNodes => 
            Runtime.ConnectionsService?.ConnectionTreeModel?.RootNodes ?? Enumerable.Empty<IConnectionNode>();

        public event Action<string, string> OnConnectionOpened;
        public event Action<string, string> OnConnectionClosed;

        private PluginManager()
        {
            _pluginsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
            if (Runtime.ConnectionInitiator != null)
            {
                Runtime.ConnectionInitiator.ConnectionOpened += (host, protocol) => OnConnectionOpened?.Invoke(host, protocol);
                Runtime.ConnectionInitiator.ConnectionClosed += (host, protocol) => OnConnectionClosed?.Invoke(host, protocol);
            }
        }

        public void LoadPlugins()
        {
            // Load only if the install-dir Plugins folder already exists; do not create it (that
            // write fails under a read-only Program Files install — #145).
            if (!Directory.Exists(_pluginsPath))
                return;

            foreach (string file in Directory.GetFiles(_pluginsPath, "*.dll"))
            {
                try
                {
                    LoadPlugin(file);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to load plugin from {file}", ex);
                }
            }
        }

        private void LoadPlugin(string filePath)
        {
            Assembly assembly = Assembly.LoadFrom(filePath);
            foreach (Type type in assembly.GetTypes())
            {
                if (typeof(IPlugin).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                {
                    if (Activator.CreateInstance(type) is not IPlugin plugin)
                        continue;
                    plugin.Initialize(this);
                    _plugins.Add(plugin);
                    LogInfo($"Loaded plugin: {plugin.Name} v{plugin.Version}");
                }
            }
        }

        public void ShutdownPlugins()
        {
            foreach (var plugin in _plugins)
            {
                try
                {
                    plugin.Shutdown();
                }
                catch (Exception ex)
                {
                    LogError($"Error shutting down plugin {plugin.Name}", ex);
                }
            }
            _plugins.Clear();
        }

        public void RegisterMenu(string text, Action onClick)
        {
             if (MainWindow.InvokeRequired)
             {
                 MainWindow.Invoke(new Action(() => RegisterMenu(text, onClick)));
                 return;
             }

             var mainForm = MainWindow as FrmMain;
             if (mainForm != null && mainForm.msMain != null)
             {
                 var toolsMenu = mainForm.msMain.Items["mMenTools"] as ToolStripMenuItem;
                 if (toolsMenu != null)
                 {
                     toolsMenu.DropDownItems.Add(text, null, (s, e) => onClick());
                 }
             }
        }

        public void LogInfo(string message)
        {
            mRemoteNG.App.Runtime.MessageCollector.AddMessage(Messages.MessageClass.InformationMsg, $"[Plugin] {message}");
        }

        public void LogError(string message, Exception ex)
        {
            mRemoteNG.App.Runtime.MessageCollector.AddExceptionMessage($"[Plugin] {message}", ex);
        }
    }
}

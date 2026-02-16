using System;
using System.Windows.Forms;

namespace mRemoteNG.PluginSystem
{
    public interface IPluginHost
    {
        Form MainWindow { get; }
        void RegisterMenu(string text, Action onClick);
        void LogInfo(string message);
        void LogError(string message, Exception ex);
    }
}

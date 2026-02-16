using mRemoteNG.App;
using mRemoteNG.App.Info;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Window;
using System;
using System.Collections.Generic; // Added for Dictionary
using System.IO;
using mRemoteNG.Messages;
using WeifenLuo.WinFormsUI.Docking;
using System.Runtime.Versioning;

namespace mRemoteNG.Config.Settings
{
    [SupportedOSPlatform("windows")]
    public class DockPanelLayoutLoader
    {
        private readonly FrmMain _mainForm;
        private readonly MessageCollector _messageCollector;

        // Static dictionary for persistent string to content mapping
        private static readonly Dictionary<string, Func<IDockContent?>> _contentMap = new Dictionary<string, Func<IDockContent?>>();

        static DockPanelLayoutLoader() // Static constructor to initialize the map
        {
            _contentMap.Add(typeof(ConfigWindow).ToString(), () => AppWindows.ConfigForm);
            _contentMap.Add(typeof(ConnectionTreeWindow).ToString(), () => AppWindows.TreeForm);
            _contentMap.Add(typeof(ErrorAndInfoWindow).ToString(), () => AppWindows.ErrorsForm);
            // Add other dockable windows here as they are introduced
        }

        public DockPanelLayoutLoader(FrmMain mainForm, MessageCollector messageCollector)
        {
            if (mainForm == null)
                throw new ArgumentNullException(nameof(mainForm));
            if (messageCollector == null)
                throw new ArgumentNullException(nameof(messageCollector));

            _mainForm = mainForm;
            _messageCollector = messageCollector;
        }

        public void LoadPanelsFromXml()
        {
            try
            {
#if !PORTABLE
                string oldPath =
 Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\" + GeneralAppInfo.ProductName + "\\" + SettingsFileInfo.LayoutFileName;
#endif
                string newPath = SettingsFileInfo.SettingsPath + "\\" + SettingsFileInfo.LayoutFileName;
                if (File.Exists(newPath))
                {
                    LoadLayout(newPath);
#if !PORTABLE
                }
                else if (File.Exists(oldPath))
                {
                    LoadLayout(oldPath);
#endif
                }
                else
                {
                    _mainForm.SetDefaultLayout();
                }
            }
            catch (Exception ex)
            {
                _messageCollector.AddExceptionMessage("LoadPanelsFromXML failed. Resetting to default layout.", ex);
                try
                {
                    // Self-healing: Corrupted layout file detected. Reset to defaults to ensure UI is usable.
                    // This fixes issues #2907, #2910, #2914 where users get stuck with broken panels.
                    _mainForm.SetDefaultLayout();
                    Runtime.MessageCollector.AddMessage(MessageClass.WarningMsg, "Panel layout file was corrupted and has been reset to defaults.");
                }
                catch (Exception resetEx)
                {
                    _messageCollector.AddExceptionMessage("Failed to reset layout to defaults after corruption.", resetEx);
                }
            }
        }

        public void LoadLayout(string filePath)
        {
            while (_mainForm.pnlDock.Contents.Count > 0)
            {
                DockContent dc = (DockContent)_mainForm.pnlDock.Contents[0];
                dc.Close();
            }

            _mainForm.pnlDock.LoadFromXml(filePath, GetContentFromPersistString);
        }

        public void SaveLayout(string filePath)
        {
            _mainForm.pnlDock.SaveAsXml(filePath);
        }

        private IDockContent? GetContentFromPersistString(string persistString)
        {
            // pnlLayout.xml persistence XML fix for refactoring to mRemoteNG
            if (persistString.StartsWith("mRemote."))
                persistString = persistString.Replace("mRemote.", "mRemoteNG.");

            try
            {
                if (_contentMap.TryGetValue(persistString, out var contentFactory))
                {
                    return contentFactory.Invoke();
                }
            }
            catch (Exception ex)
            {
                _messageCollector.AddExceptionMessage($"GetContentFromPersistString failed for '{persistString}'", ex);
            }

            return null;
        }
    }
}
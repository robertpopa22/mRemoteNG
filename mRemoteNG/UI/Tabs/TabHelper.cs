using mRemoteNG.App;
using mRemoteNG.UI.Window;
using System;
using System.Runtime.Versioning;

namespace mRemoteNG.UI.Tabs
{
    [SupportedOSPlatform("windows")]
    class TabHelper
    {
        private static readonly Lazy<TabHelper> lazyHelper = new(() => new TabHelper());

        public static TabHelper Instance => lazyHelper.Value;

        private TabHelper()
        {
        }

        internal void ForgetPanel(ConnectionWindow panel)
        {
            if (ReferenceEquals(currentPanel, panel)) currentPanel = null;
            if (currentTab != null && (currentTab.IsDisposed || panel.Contains(currentTab))) currentTab = null;
        }

        internal void ForgetTab(ConnectionTab tab)
        {
            if (ReferenceEquals(currentTab, tab)) currentTab = null;
        }

        private ConnectionTab? currentTab;

        public ConnectionTab? CurrentTab
        {
            get => currentTab;
            set
            {
                currentTab = value;
                findCurrentPanel();
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.DebugMsg, "Tab got focused: " + currentTab?.TabText);
            }
        }

        private void findCurrentPanel()
        {
            System.Windows.Forms.Control? currentForm = currentTab?.Parent;
            while (currentForm != null && !(currentForm is ConnectionWindow))
            {
                currentForm = currentForm.Parent;
            }

            if (currentForm != null)
                CurrentPanel = (ConnectionWindow)currentForm;
        }

        private ConnectionWindow? currentPanel;

        public ConnectionWindow? CurrentPanel
        {
            get => currentPanel;
            set
            {
                currentPanel = value;
                Runtime.MessageCollector.AddMessage(Messages.MessageClass.DebugMsg,
                                                    "Panel got focused: " + currentPanel?.TabText);
            }
        }
    }
}

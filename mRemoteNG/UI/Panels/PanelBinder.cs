using System;
using System.Runtime.Versioning;
using mRemoteNG.App;
using mRemoteNG.Properties;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Panels
{
    /// <summary>
    /// Manages the binding between Connections and Config panels so they show/hide together when in auto-hide state
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class PanelBinder
    {
        private static PanelBinder _instance;
        private bool _isProcessing; // Prevent recursive calls

        public static PanelBinder Instance => _instance ?? (_instance = new PanelBinder());

        private PanelBinder()
        {
        }

        /// <summary>
        /// Initializes event handlers for the Connections and Config panels
        /// </summary>
        public void Initialize()
        {
            if (Windows.TreeForm != null)
            {
                Windows.TreeForm.VisibleChanged += OnTreeFormVisibleChanged;
                Windows.TreeForm.DockStateChanged += OnTreeFormDockStateChanged;
            }

            if (Windows.ConfigForm != null)
            {
                Windows.ConfigForm.VisibleChanged += OnConfigFormVisibleChanged;
                Windows.ConfigForm.DockStateChanged += OnConfigFormDockStateChanged;
            }
        }

        private void OnTreeFormVisibleChanged(object sender, EventArgs e)
        {
            if (!OptionsTabsPanelsPage.Default.BindConnectionsAndConfigPanels || _isProcessing)
                return;

            // Only act when the panel becomes visible
            if (!Windows.TreeForm.Visible)
                return;

            // Only bind when both panels are in auto-hide state
            if (!IsPanelAutoHidden(Windows.TreeForm) || !IsPanelAutoHidden(Windows.ConfigForm))
                return;

            _isProcessing = true;
            try
            {
                ShowPanel(Windows.ConfigForm);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnConfigFormVisibleChanged(object sender, EventArgs e)
        {
            if (!OptionsTabsPanelsPage.Default.BindConnectionsAndConfigPanels || _isProcessing)
                return;

            // Only act when the panel becomes visible
            if (!Windows.ConfigForm.Visible)
                return;

            // Only bind when both panels are in auto-hide state
            if (!IsPanelAutoHidden(Windows.TreeForm) || !IsPanelAutoHidden(Windows.ConfigForm))
                return;

            _isProcessing = true;
            try
            {
                ShowPanel(Windows.TreeForm);
            }
            finally
            {
                _isProcessing = false;
            }
        }

        private void OnTreeFormDockStateChanged(object sender, EventArgs e)
        {
            // This event helps us track when panels transition to/from auto-hide
            // We don't need to take action here, just be aware of state changes
        }

        private void OnConfigFormDockStateChanged(object sender, EventArgs e)
        {
            // This event helps us track when panels transition to/from auto-hide
            // We don't need to take action here, just be aware of state changes
        }

        /// <summary>
        /// Checks if a panel is in auto-hide state
        /// </summary>
        private bool IsPanelAutoHidden(DockContent panel)
        {
            if (panel == null)
                return false;

            return panel.DockState == DockState.DockLeftAutoHide ||
                   panel.DockState == DockState.DockRightAutoHide ||
                   panel.DockState == DockState.DockTopAutoHide ||
                   panel.DockState == DockState.DockBottomAutoHide;
        }

        /// <summary>
        /// Shows a panel by activating it (which brings it to front in auto-hide mode)
        /// </summary>
        private void ShowPanel(DockContent panel)
        {
            if (panel != null && IsPanelAutoHidden(panel))
            {
                panel.Activate();
            }
        }
    }
}

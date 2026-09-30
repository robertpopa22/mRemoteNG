using System.Drawing;
using System.Runtime.Versioning;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System;
using System.Collections.Generic;
using System.Reflection;
using mRemoteNG.UI.Tabs;
using WeifenLuo.WinFormsUI.Docking;
using WeifenLuo.WinFormsUI.ThemeVS2015;

namespace mRemoteNG.Themes
{
    [SupportedOSPlatform("windows")]

    /// <summary>
    /// Visual Studio 2015 Light theme.
    /// </summary>
    public class MremoteNGThemeBase : VS2015ThemeBase
    {
        private static readonly ConditionalWeakTable<ThemeBase, ToolStripRenderer> TransientRenderers = new();
        private static readonly ConditionalWeakTable<ToolStrip, HashSet<ThemeBase>> TransientThemes = new();
        // DockPanelSuite 3.1.1 has no per-strip cleanup API. Its DockContentHandler also
        // registers tab menus automatically, even when we assign their renderer directly.
        private static readonly FieldInfo StripRestorationField = typeof(ThemeBase)
            .GetField("_stripBefore", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new NotSupportedException("DockPanelSuite ToolStrip ownership contract changed.");

        internal static void ApplyToTransientToolStrip(ThemeBase? theme, ToolStrip strip)
        {
            if (theme != null && TransientThemes.GetOrCreateValue(strip).Add(theme))
            {
                if (StripRestorationField.GetValue(theme) is not Dictionary<ToolStrip,
                        KeyValuePair<ToolStripRenderMode, ToolStripRenderer>> restoration)
                    throw new NotSupportedException("DockPanelSuite ToolStrip restoration contract changed.");
                strip.Disposed += (_, _) => restoration.Remove(strip);
            }
            // ThemeBase.ApplyTo stores the strip in a strong dictionary until the whole theme
            // changes. Session menus must not keep closed panels and their RDP objects alive.
            if (theme is MremoteNGThemeBase ownTheme)
                strip.Renderer = ownTheme.ToolStripRenderer;
            else if (theme?.ColorPalette != null)
                strip.Renderer = TransientRenderers.GetValue(theme,
                    key => new VisualStudioToolStripRenderer(key.ColorPalette) { UseGlassOnMenuStrip = false });
            else
                strip.RenderMode = ToolStripRenderMode.ManagerRenderMode;
        }

        public MremoteNGThemeBase(byte[] themeResource)
            : base(themeResource)
        {
            Measures.SplitterSize = Properties.OptionsTabsPanelsPage.Default.SplitterSize;
            Measures.AutoHideSplitterSize = Properties.OptionsTabsPanelsPage.Default.SplitterSize;
            Measures.DockPadding = Properties.OptionsTabsPanelsPage.Default.DockPadding;
            ShowAutoHideContentOnHover = false;
        }
    }

    [SupportedOSPlatform("windows")]
    public class MremoteDockPaneStripFactory : DockPanelExtender.IDockPaneStripFactory
    {
        public DockPaneStripBase CreateDockPaneStrip(DockPane pane) => new DockPaneStripNG(pane);
    }

    public class MremoteFloatWindowFactory : DockPanelExtender.IFloatWindowFactory
    {
        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane, Rectangle bounds)
        {
            Rectangle? activeDocumentBounds = (dockPanel.ActiveDocument as ConnectionTab)?.Bounds;

            return new FloatWindowNG(dockPanel, pane, activeDocumentBounds ?? bounds);
        }

        public FloatWindow CreateFloatWindow(DockPanel dockPanel, DockPane pane)
        {
            return new FloatWindowNG(dockPanel, pane);
        }
    }
}

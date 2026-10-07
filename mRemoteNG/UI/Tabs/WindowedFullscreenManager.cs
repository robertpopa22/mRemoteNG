using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.Connection;
using mRemoteNG.Connection.Protocol;
using mRemoteNG.Connection.Protocol.RDP;
using mRemoteNG.Resources.Language;
using mRemoteNG.Messages;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNG.UI.Tabs
{
    internal static class WindowedFullscreenManager
    {
        private const int HotKeyId = 0x5746;
        private const uint ModAlt = 0x0001;
        private const uint ModControl = 0x0002;

        private sealed class State
        {
            public required ConnectionTab Tab { get; init; }
            public required DockPanel OriginalDockPanel { get; init; }
            public DockPane? OriginalPane { get; init; }
            public IDockContent? BeforeContent { get; init; }
            public DockState OriginalDockState { get; init; }
            public Rectangle OriginalFloatBounds { get; init; }
            public Rectangle WorkingArea { get; init; }
            public FloatWindow? Window { get; set; }
            public Button? ReturnButton { get; set; }
            public Panel? RevealZone { get; set; }
            public Timer? HideTimer { get; set; }
            public bool HotKeyRegistered { get; set; }
            public bool SmartSizeBefore { get; set; }
        }

        private static readonly Dictionary<ConnectionTab, State> States = new();

        internal static void Enter(ConnectionTab tab)
        {
            if (tab.IsDisposed || tab.Disposing || tab.DockPanel == null ||
                tab.Tag is not InterfaceControl interfaceControl ||
                interfaceControl.Protocol is not RdpProtocol rdp)
                return;

            if (States.TryGetValue(tab, out State? existing))
            {
                if (existing.Window?.WindowState == FormWindowState.Minimized)
                    existing.Window.WindowState = FormWindowState.Maximized;
                existing.Window?.Activate();
                rdp.Focus();
                return;
            }

            DockPanel dockPanel = tab.DockPanel;
            DockPane? pane = tab.DockHandler.Pane;
            IDockContent[] contents = pane?.DisplayingContents.ToArray() ?? [];
            int index = Array.IndexOf(contents, tab);
            FloatWindow? originalFloat = tab.DockHandler.FloatPane?.FloatWindow;
            Rectangle sourceBounds = originalFloat?.Bounds ?? tab.RectangleToScreen(tab.ClientRectangle);
            State state = new()
            {
                Tab = tab,
                OriginalDockPanel = dockPanel,
                OriginalPane = pane,
                BeforeContent = index >= 0 && index + 1 < contents.Length ? contents[index + 1] : null,
                OriginalDockState = tab.DockState,
                OriginalFloatBounds = sourceBounds,
                WorkingArea = Screen.FromRectangle(sourceBounds).WorkingArea,
                SmartSizeBefore = rdp.SmartSizeForWindowedFullscreen
            };
            States.Add(tab, state);
            tab.Disposed += TabDisposed;

            try
            {
                rdp.BeginWindowedFullscreenSizing();
                tab.Show(dockPanel, state.WorkingArea);
                state.Window = tab.DockHandler.FloatPane?.FloatWindow;
                if (state.Window == null)
                    throw new InvalidOperationException("The windowed fullscreen host was not created.");

                ConfigureWindow(state.Window);
                Size desktop = rdp.WindowedFullscreenDesktopSizeForDiagnostics;
                Runtime.MessageCollector.AddMessage(MessageClass.InformationMsg,
                    $"phase=windowed_fullscreen_enter float={state.Window.Bounds.Width}x{state.Window.Bounds.Height}@{state.Window.Bounds.X},{state.Window.Bounds.Y} " +
                    $"tab={tab.ClientSize.Width}x{tab.ClientSize.Height} bounds={tab.Bounds.Width}x{tab.Bounds.Height}@{tab.Bounds.X},{tab.Bounds.Y} " +
                    $"ic={interfaceControl.ClientSize.Width}x{interfaceControl.ClientSize.Height} bounds={interfaceControl.Bounds.Width}x{interfaceControl.Bounds.Height}@{interfaceControl.Bounds.X},{interfaceControl.Bounds.Y} " +
                    $"control={rdp.WindowedFullscreenControlSizeForDiagnostics.Width}x{rdp.WindowedFullscreenControlSizeForDiagnostics.Height} " +
                    $"dock={rdp.WindowedFullscreenControlDockForDiagnostics} " +
                    $"desktop={desktop.Width}x{desktop.Height} workingArea={state.WorkingArea.Width}x{state.WorkingArea.Height}@{state.WorkingArea.X},{state.WorkingArea.Y}");
                rdp.UpdateWindowedFullscreenSizing();
                tab.DockHandler.Activate();
                rdp.Focus();
            }
            catch
            {
                RollBack(state);
                throw;
            }
        }

        internal static bool IsWindowedFullscreenPane(DockPane pane) =>
            States.Values.Any(state => ReferenceEquals(state.Tab.DockHandler.Pane, pane));

        internal static void ConfigureWindow(FloatWindow window)
        {
            State? state = Find(window);
            if (state == null)
                return;

            state.Window = window;
            window.SuspendLayout();
            try
            {
                window.FormBorderStyle = FormBorderStyle.None;
                window.ShowInTaskbar = true;
                window.Owner = null;
                if (window is FloatWindowNG windowNg)
                    windowNg.SetWindowedFullscreenWorkingArea(state.WorkingArea);
                window.Bounds = state.WorkingArea;
                window.WindowState = FormWindowState.Maximized;

                if (state.ReturnButton == null)
                {
                    state.ReturnButton = new Button { Text = Language.WindowedFullscreenReturn, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right, TabStop = false };
                    state.ReturnButton.Click += (_, _) => Exit(state);
                    state.ReturnButton.MouseEnter += (_, _) => RestartHideTimer(state);
                    window.Controls.Add(state.ReturnButton);
                    state.RevealZone = new Panel { Dock = DockStyle.Top, Height = 3, BackColor = Color.Transparent };
                    state.RevealZone.MouseEnter += (_, _) => ShowReturnButton(state);
                    window.Controls.Add(state.RevealZone);
                    state.RevealZone.BringToFront();
                    state.HideTimer = new Timer { Interval = 1800 };
                    state.HideTimer.Tick += (_, _) =>
                    {
                        state.HideTimer.Stop();
                        if (state.ReturnButton != null && state.HotKeyRegistered)
                            state.ReturnButton.Visible = false;
                    };
                }

                PositionReturnButton(state);
                ShowReturnButton(state);
            }
            finally { window.ResumeLayout(true); }
        }

        internal static void WindowBoundsChanged(FloatWindow window)
        {
            State? state = Find(window);
            if (state == null || window.WindowState == FormWindowState.Minimized) return;
            if (window is FloatWindowNG windowNg)
                windowNg.SetWindowedFullscreenWorkingArea(Screen.FromControl(window).WorkingArea);
            PositionReturnButton(state);
        }

        internal static bool HandleHotKey(FloatWindow window, int id) => id == HotKeyId && TryExit(window);

        internal static void WindowActivated(FloatWindow window)
        {
            State? state = Find(window);
            if (state == null || state.HotKeyRegistered || !window.IsHandleCreated) return;
            state.HotKeyRegistered = NativeMethods.RegisterHotKey(window.Handle, HotKeyId, ModControl | ModAlt, (uint)Keys.Enter);
        }

        internal static void WindowDeactivated(FloatWindow window)
        {
            State? state = Find(window);
            if (state?.HotKeyRegistered != true || !window.IsHandleCreated) return;
            NativeMethods.UnregisterHotKey(window.Handle, HotKeyId);
            state.HotKeyRegistered = false;
        }

        internal static bool TryExit(FloatWindow window)
        {
            State? state = Find(window);
            if (state == null) return false;
            Exit(state);
            return true;
        }

        internal static void Forget(FloatWindow window)
        {
            foreach (State state in States.Values.Where(candidate => ReferenceEquals(candidate.Window, window)).ToArray()) Cleanup(state);
        }

        private static State? Find(FloatWindow window) => States.Values.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Window, window) || ReferenceEquals(candidate.Tab.DockHandler.FloatPane?.FloatWindow, window));

        private static void Exit(State state)
        {
            if (!States.Remove(state.Tab)) return;
            ReleaseWindowResources(state);
            if (state.Tab.IsDisposed || state.Tab.Disposing) return;
            RestoreTab(state);
            if (state.Tab.Tag is InterfaceControl interfaceControl && interfaceControl.Protocol is RdpProtocol rdp)
                rdp.EndWindowedFullscreenSizing(state.SmartSizeBefore);
            state.Tab.DockHandler.Activate();
            state.Tab.Focus();
            (state.Tab.Tag as InterfaceControl)?.Protocol?.Focus();
        }

        private static void RestoreTab(State state)
        {
            IDockContent? before = state.BeforeContent;
            if (before is DockContent beforeDockContent && beforeDockContent.IsDisposed) before = null;
            if (before != null && state.OriginalPane != null && !state.OriginalPane.DisplayingContents.Contains(before))
                before = null;
            if (state.OriginalPane is { IsDisposed: false } pane)
            {
                state.Tab.Show(pane, before);
            }
            else if (state.OriginalDockState == DockState.Float)
            {
                state.Tab.Show(state.OriginalDockPanel, state.OriginalFloatBounds);
                if (state.Tab.DockHandler.FloatPane?.FloatWindow is FloatWindow restored)
                {
                    restored.FormBorderStyle = FormBorderStyle.Sizable;
                    restored.Bounds = state.OriginalFloatBounds;
                    restored.WindowState = FormWindowState.Normal;
                }
            }
            else
            {
                state.Tab.Show(state.OriginalDockPanel, DockState.Document);
            }
        }

        private static void RollBack(State state)
        {
            States.Remove(state.Tab);
            ReleaseWindowResources(state);
            if (!state.Tab.IsDisposed && !state.Tab.Disposing) RestoreTab(state);
            if (state.Tab.Tag is InterfaceControl interfaceControl && interfaceControl.Protocol is RdpProtocol rdp)
                rdp.EndWindowedFullscreenSizing(state.SmartSizeBefore);
        }

        private static void Cleanup(State state) { States.Remove(state.Tab); ReleaseWindowResources(state); }

        private static void TabDisposed(object? sender, EventArgs e)
        {
            if (sender is ConnectionTab tab && States.TryGetValue(tab, out State? state)) Cleanup(state);
        }

        private static void ReleaseWindowResources(State state)
        {
            state.Tab.Disposed -= TabDisposed;
            if (state.HotKeyRegistered && state.Window?.IsHandleCreated == true) NativeMethods.UnregisterHotKey(state.Window.Handle, HotKeyId);
            state.HotKeyRegistered = false;
            state.HideTimer?.Stop();
            state.HideTimer?.Dispose();
            state.ReturnButton?.Dispose();
            state.RevealZone?.Dispose();
        }

        private static void ShowReturnButton(State state)
        {
            if (state.ReturnButton == null) return;
            state.ReturnButton.Visible = true;
            state.ReturnButton.BringToFront();
            RestartHideTimer(state);
        }

        private static void RestartHideTimer(State state) { state.HideTimer?.Stop(); state.HideTimer?.Start(); }

        private static void PositionReturnButton(State state)
        {
            if (state.Window == null || state.ReturnButton == null) return;
            state.ReturnButton.Location = new Point(Math.Max(0, state.Window.ClientSize.Width - state.ReturnButton.Width - 8), 8);
        }
    }
}

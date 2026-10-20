﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using AxMSTSCLib;
using Microsoft.Win32;
using mRemoteNG.App;
using mRemoteNG.Messages;
using MSTSCLib;
using mRemoteNG.Resources.Language;
using System.Runtime.Versioning;

namespace mRemoteNG.Connection.Protocol.RDP
{
    [SupportedOSPlatform("windows")]
    /* RDP v8 requires Windows 7 with:
		* https://support.microsoft.com/en-us/kb/2592687 
		* OR
		* https://support.microsoft.com/en-us/kb/2923545
		* 
		* Windows 8+ support RDP v8 out of the box.
		*/
    public class RdpProtocol8 : RdpProtocol7
    {
        private MsRdpClient8NotSafeForScripting? RdpClient8 => ((AxHost?)Control)?.GetOcx() as MsRdpClient8NotSafeForScripting;

        protected override RdpVersion RdpProtocolVersion => RDP.RdpVersion.Rdc8;
        protected FormWindowState LastWindowState = FormWindowState.Minimized;

        // Debounce timer to reduce flickering during resize
        private System.Timers.Timer? _resizeDebounceTimer;
        private Size _pendingResizeSize;
        private bool _hasPendingResize;
        private int _resizeGeneration;
        private System.Windows.Forms.Timer? _windowedFullscreenSettleTimer;
        private Size _windowedFullscreenTarget;
        private int _windowedFullscreenSettleAttempts;
        private bool _windowedFullscreenRetryPending;
        private RdpAdaptiveResolution? _adaptive;
        private IReadOnlyList<uint> _adaptiveLadder = [];
        private uint _adaptiveBaseScale;
        private uint _appliedRemoteScale;
        private bool _zoomedInsteadOfSmartSize;
        private bool _adaptiveUpdateRefused;
        private bool _smartSizeOffByUser;
        private System.Windows.Forms.Timer? _adaptiveRetryTimer;
        private int _adaptiveRetries;
        internal const int AdaptiveRetryMs = 2000;
        internal const int AdaptiveMaxRetries = 15;
        private Size _adaptiveTarget;
        private bool _adaptiveDisabled;

        public RdpProtocol8()
        {
            // ResizeEnd events are forwarded by ConnectionWindow/ConnectionTab.
            // Avoid wiring FrmMain directly to prevent duplicate and unrelated resize-end handling.

            // Initialize debounce timer (100ms delay)
            _resizeDebounceTimer = new System.Timers.Timer(100);
            _resizeDebounceTimer.AutoReset = false;
            _resizeDebounceTimer.Elapsed += ResizeDebounceTimer_Elapsed;
        }

        public override bool Initialize()
        {
            if (!base.Initialize())
                return false;

            return PostInitialize();
        }

        public override async System.Threading.Tasks.Task<bool> InitializeAsync()
        {
            if (!await base.InitializeAsync())
                return false;

            return PostInitialize();
        }

        private bool PostInitialize()
        {
            if (RdpVersion < Versions.RDC81) return false; // minimum dll version checked, loaded MSTSCLIB dll version is not capable

            // Subscribe to external events here (not in constructor) so temporary
            // probing instances from RdpProtocolFactory are not rooted by static
            // events, preventing memory leaks (upstream: 32d54235a).
            _frmMain.ResizeEnd += ResizeEnd;
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChangedHandler;

            // https://learn.microsoft.com/en-us/windows/win32/termserv/imsrdpextendedsettings-property
            if (connectionInfo.UseRestrictedAdmin)
            {
                SetExtendedProperty("RestrictedLogon", true);
            }
            else if (connectionInfo.UseRCG)
            {
                SetExtendedProperty("DisableCredentialsDelegation", true);
                SetExtendedProperty("RedirectedAuthentication", true);
            }

            return true;
        }

        public override bool Fullscreen
        {
            get => base.Fullscreen;
            protected set
            {
                DevLog.Write($"Fullscreen={value} host={connectionInfo?.Hostname}");
                base.Fullscreen = value;
                DoResizeClient();
            }
        }

        protected override void Resize(object sender, EventArgs e)
        {
            if (_frmMain == null) return;

            // Skip resize entirely when minimized or minimizing, but track state
            if (_frmMain.WindowState == FormWindowState.Minimized)
            {
                LastWindowState = FormWindowState.Minimized;
                return;
            }

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resize() called - WindowState={_frmMain.WindowState}, LastWindowState={LastWindowState}");

            DevLog.Write($"WindowState={_frmMain.WindowState} LastWindowState={LastWindowState} InterfaceControl.Size={InterfaceControl?.Size}");
            DoResizeControl();

            // Track window state transitions for minimize/restore handling
            if (LastWindowState != _frmMain.WindowState)
            {
                bool wasMinimized = LastWindowState == FormWindowState.Minimized;

                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"Resize() - Window state changed from {LastWindowState} to {_frmMain.WindowState}");
                LastWindowState = _frmMain.WindowState;

                if (wasMinimized)
                {
                    // After restoring from minimize, the RDP ActiveX control may not
                    // properly restore its layout. Force a re-dock cycle to ensure
                    // the control fills its container correctly. Also re-apply
                    // SmartSizing which may be lost during minimize/restore (#662).
                    if (Control != null && !Control.IsDisposed && Control.Dock == DockStyle.Fill)
                    {
                        Control.Dock = DockStyle.None;
                        Control.Dock = DockStyle.Fill;
                    }

                    // The control may have dropped its zoom with the layout; write it again.
                    if (ZoomLevel != 100)
                        ForgetZoom();
                    EnsureSmartSizing();
                }
            }

            UpscaleFixedSession();

            // Always use debounced resize — during state changes (Maximize/Restore),
            // InterfaceControl.Size may not yet reflect the final layout. The debounce
            // timer lets layout complete before the session size is applied.
            // For unchanged state, handles programmatic resizes (#69).
            ScheduleDebouncedResize();
        }

        protected override void ResizeEnd(object sender, EventArgs e)
        {
            if (_frmMain == null) return;

            // Skip resize when minimized
            if (_frmMain.WindowState == FormWindowState.Minimized) return;

            DevLog.Write($"WindowState={_frmMain.WindowState} InterfaceControl.Size={InterfaceControl?.Size}");

            // Update window state tracking
            LastWindowState = _frmMain.WindowState;

            // Update control size immediately (no flicker)
            DoResizeControl();

            UpscaleFixedSession();

            // Debounce the RDP session resize to reduce flickering
            ScheduleDebouncedResize();
        }

        private void ScheduleDebouncedResize()
        {
            if (InterfaceControl == null) return;

            // Store the pending size
            _pendingResizeSize = InterfaceControl.Size;
            _hasPendingResize = true;

            // Reset the timer (this delays the resize if called repeatedly)
            _resizeDebounceTimer?.Stop();
            _resizeDebounceTimer?.Start();

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"Resize debounced - will resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height} after 100ms");
        }

        private void ResizeDebounceTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_hasPendingResize) return;

            if (Control == null || Control.IsDisposed || InterfaceControl == null || InterfaceControl.IsDisposed)
            {
                _hasPendingResize = false;
                return;
            }

            _hasPendingResize = false;
            int generation = _resizeGeneration;
            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"Debounce timer fired - executing delayed resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height}");

            Action resizeIfCurrent = () =>
            {
                if (generation == _resizeGeneration) DoResizeClient();
            };
            if (InterfaceControl.InvokeRequired)
                InterfaceControl.BeginInvoke(resizeIfCurrent);
            else
                resizeIfCurrent();
        }

        private void OnDisplaySettingsChangedHandler(object? sender, EventArgs e) => OnDisplaySettingsChanged();

        public override void OnDisplaySettingsChanged()
        {
            if (_frmMain == null || _frmMain.WindowState == FormWindowState.Minimized) return;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"DisplaySettingsChanged for '{connectionInfo?.Hostname}' — scheduling resize");

            DoResizeControl();
            ScheduleDebouncedResize();
        }

        protected override AxHost CreateActiveXRdpClientControl()
        {
            return new RdpActiveXHosts.Client8();
        }

        private void DoResizeClient()
        {
            DevLog.Write($"host={connectionInfo?.Hostname} loginComplete={loginComplete} Fullscreen={Fullscreen}");

            if (!loginComplete)
            {
                DevLog.Write($"SKIP: login not complete");
                return;
            }

            if (Control == null || InterfaceControl == null || Control.IsDisposed || InterfaceControl.IsDisposed)
            {
                DevLog.Write($"SKIP: controls disposed");
                return;
            }

            if (WindowedFullscreenSizingActive)
            {
                SmartSize = true;
                SessionResizeDecision transientDecision = DecideWindowedFullscreenResize(
                    SupportsDynamicResize, loginComplete, InterfaceControl.ClientRectangle, InterfaceControl.Padding);
                if (transientDecision.Apply)
                    UpdateSessionDisplaySettings((uint)transientDecision.Size.Width, (uint)transientDecision.Size.Height);
                return;
            }

            DevLog.Write($"AutomaticResize={InterfaceControl.Info.AutomaticResize} Resolution={InterfaceControl.Info.Resolution} SmartSize={SmartSize}");

            if (!InterfaceControl.Info.AutomaticResize)
            {
                DevLog.Write($"SKIP: AutomaticResize disabled");
                return;
            }

            if (!(InterfaceControl.Info.Resolution == RDPResolutions.FitToWindow ||
                  InterfaceControl.Info.Resolution == RDPResolutions.Fullscreen ||
                  InterfaceControl.Info.Resolution == RDPResolutions.SmartSize ||
                  InterfaceControl.Info.Resolution == RDPResolutions.SmartSizeAspect))
            {
                DevLog.Write($"SKIP: Resolution={InterfaceControl.Info.Resolution} (needs FitToWindow, Fullscreen, or SmartSize)");
                // The session keeps its own size; only its zoom follows the panel.
                UpscaleFixedSession();
                return;
            }

            // Note: SmartSize (client-side scaling) is compatible with FitToWindow/Fullscreen.
            // We reconnect at the new panel size so the session runs at native resolution;
            // SmartSize handles smooth scaling during intermediate resize states.

            Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                $"Resizing RDP connection to host '{connectionInfo?.Hostname}' (SmartSize={SmartSize})");

            try
            {
                SessionResizeDecision decision = DecideSessionResize(
                    SupportsDynamicResize,
                    InterfaceControl.Info.Resolution,
                    Fullscreen,
                    Fullscreen ? Screen.FromControl(Control).Bounds.Size : Size.Empty,
                    InterfaceControl.ClientRectangle,
                    InterfaceControl.Padding);

                DevLog.Write($"Fullscreen={Fullscreen} apply={decision.Apply} targetSize={decision.Size.Width}x{decision.Size.Height} Control.Size={Control.Size} InterfaceControl.Size={InterfaceControl.Size}");

                if (!decision.Apply)
                {
                    DevLog.Write($"SKIP: session resize not applied ({decision.Size.Width}x{decision.Size.Height})");
                    return;
                }

                uint baseScale = DesktopScaleFactor;
                uint remoteScale = AdaptiveRemoteScale(baseScale);
                Size target = AdaptedSize(decision.Size, baseScale, remoteScale);
                bool reduced = target != decision.Size;

                uint sessionScale = reduced ? remoteScale : baseScale;
                DevLog.Write($"Calling UpdateSessionDisplaySettings({target.Width}, {target.Height}, scale {sessionScale})");
                _adaptiveUpdateRefused = false;
                UpdateSessionDisplaySettings((uint)target.Width, (uint)target.Height, sessionScale);
                // A refused smaller size leaves the session as it was: zooming it would overflow the panel.
                if (_adaptiveUpdateRefused)
                    return;
                _adaptiveTarget = reduced ? target : Size.Empty;
                _adaptiveRetries = 0;
                // The smaller session is zoomed back to the panel. Fullscreen keeps the SmartSizing
                // that fills the screen (#1402), which already reset the zoom.
                if (Fullscreen)
                    _zoomedInsteadOfSmartSize = false;
                else
                    ApplyUpscale(target, reduced ? "adaptive" : "panel");
                NoteAppliedRemoteScale(baseScale, sessionScale, target);

                EnsureSmartSizing();
                DevLog.Write($"Session display update done. SmartSize={SmartSize}");

                if (!InterfaceControl.IsDisposed &&
                    NeedsAnotherPass(Fullscreen, decision.Size, InterfaceControl.ClientRectangle, InterfaceControl.Padding))
                {
                    DevLog.Write($"Panel changed while the size was applied, scheduling another pass");
                    ScheduleDebouncedResize();
                }
            }
            catch (Exception ex)
            {
                DevLog.Write($"EXCEPTION: {ex.GetType().Name}: {ex.Message}");
                Runtime.MessageCollector.AddExceptionMessage(
                    string.Format(CultureInfo.InvariantCulture, Language.ChangeConnectionResolutionError, connectionInfo?.Hostname),
                    ex, MessageClass.WarningMsg, false);
            }
        }

        internal override void BeginWindowedFullscreenSizing()
        {
            _resizeDebounceTimer?.Stop();
            _hasPendingResize = false;
            _resizeGeneration++;
            base.BeginWindowedFullscreenSizing();
            // Windowed fullscreen always runs at the full size.
            _appliedRemoteScale = 0;
        }

        internal override void UpdateWindowedFullscreenSizing()
        {
            base.UpdateWindowedFullscreenSizing();
            if (Control == null || InterfaceControl == null || Control.IsDisposed || InterfaceControl.IsDisposed)
                return;

            SessionResizeDecision decision = DecideWindowedFullscreenResize(
                SupportsDynamicResize, loginComplete, InterfaceControl.ClientRectangle, InterfaceControl.Padding);
            if (decision.Apply)
            {
                UpdateSessionDisplaySettings((uint)decision.Size.Width, (uint)decision.Size.Height);
                StartWindowedFullscreenSettleCheck(decision.Size);
            }
            Size desktop = WindowedFullscreenDesktopSize;
            var tab = InterfaceControl.Parent as mRemoteNG.UI.Tabs.ConnectionTab;
            Rectangle floatBounds = tab?.DockHandler.FloatPane?.FloatWindow?.Bounds ?? Rectangle.Empty;
            Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                $"phase=windowed_fullscreen_sized panel={InterfaceControl.ClientSize.Width}x{InterfaceControl.ClientSize.Height} " +
                $"target={decision.Size.Width}x{decision.Size.Height} desktop={desktop.Width}x{desktop.Height} " +
                $"dynamic={SupportsDynamicResize} loginComplete={loginComplete} apply={decision.Apply} " +
                $"smartSize={SmartSize} icBounds={InterfaceControl.Bounds.Width}x{InterfaceControl.Bounds.Height}@{InterfaceControl.Bounds.X},{InterfaceControl.Bounds.Y} " +
                $"tab={tab?.ClientSize.Width ?? 0}x{tab?.ClientSize.Height ?? 0} " +
                $"control={Control.Width}x{Control.Height} controlBounds={Control.Bounds.Width}x{Control.Bounds.Height}@{Control.Bounds.X},{Control.Bounds.Y} " +
                $"dock={Control.Dock} float={floatBounds.Width}x{floatBounds.Height}@{floatBounds.X},{floatBounds.Y}");
        }

        internal override void EndWindowedFullscreenSizing(bool smartSizeBefore)
        {
            _resizeDebounceTimer?.Stop();
            _hasPendingResize = false;
            _resizeGeneration++;
            _windowedFullscreenSettleTimer?.Stop();
            _windowedFullscreenRetryPending = false;
            try
            {
                if (Control != null && InterfaceControl != null && !Control.IsDisposed && !InterfaceControl.IsDisposed)
                {
                    SessionResizeDecision decision = DecideWindowedFullscreenResize(
                        SupportsDynamicResize, loginComplete, InterfaceControl.ClientRectangle, InterfaceControl.Padding);
                    if (decision.Apply)
                        UpdateSessionDisplaySettings((uint)decision.Size.Width, (uint)decision.Size.Height);
                }
            }
            finally
            {
                base.EndWindowedFullscreenSizing(smartSizeBefore);
            }

            // The full size was written back above; a reduced level comes back on the next pass.
            _appliedRemoteScale = 0;
            if (AdaptiveRemoteScale(DesktopScaleFactor) < DesktopScaleFactor)
                ScheduleDebouncedResize();
            else
                UpscaleFixedSession();
        }

        internal static SessionResizeDecision DecideWindowedFullscreenResize(bool supportsDynamicResize,
                                                                               bool loginComplete,
                                                                               Rectangle client,
                                                                               Padding padding)
        {
            if (!supportsDynamicResize || !loginComplete)
                return new SessionResizeDecision(false, Size.Empty);

            Size size = ContentSize(client, padding);
            return new SessionResizeDecision(size.Width > 0 && size.Height > 0, size);
        }

        private bool DoResizeControl()
        {
            if (Control == null || InterfaceControl == null) return false;

            // Check if controls are being disposed during shutdown
            if (Control.IsDisposed || InterfaceControl.IsDisposed) return false;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"DoResizeControl - Before: Control.Size={Control.Size}, InterfaceControl.Size={InterfaceControl.Size}, Control.Dock={Control.Dock}");

            // If control is docked, we need to temporarily undock it, resize it, then redock it
            // because WinForms ignores Size assignments on docked controls
            bool wasDocked = Control.Dock == DockStyle.Fill;

            if (wasDocked)
            {
                Control.Dock = DockStyle.None;
            }

            Control.Location = InterfaceControl.Location;

            if (Control.Size == InterfaceControl.Size || InterfaceControl.Size == Size.Empty)
            {
                // Restore docking if we changed it
                if (wasDocked)
                {
                    Control.Dock = DockStyle.Fill;
                }

                Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                    $"DoResizeControl - Skipped: Sizes already match or InterfaceControl.Size is empty");
                return false;
            }

            Control.Size = InterfaceControl.Size;

            // Restore docking
            if (wasDocked)
            {
                Control.Dock = DockStyle.Fill;
            }

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"DoResizeControl - After: Control.Size={Control.Size}, Control.Dock={Control.Dock}");

            return true;
        }

        /// <summary>
        /// Re-applies SmartSizing based on the connection's configured settings.
        /// Called after restore from minimize to ensure the COM property wasn't lost (#662).
        /// </summary>
        /// <summary>
        /// While the control zooms a smaller session instead, the session is still scaled to its
        /// panel: report that, so the menu, a fullscreen round trip and a restore see the state the
        /// connection asked for. Setting it hands scaling back to SmartSizing.
        /// </summary>
        public override bool SmartSize
        {
            get => _zoomedInsteadOfSmartSize || base.SmartSize;
            protected set
            {
                _zoomedInsteadOfSmartSize = false;
                base.SmartSize = value;
            }
        }

        /// <summary>
        /// The menu command turns scaling off for a session the application zooms as well, and it
        /// stays off until the command turns it on again.
        /// </summary>
        public override void ToggleSmartSize()
        {
            try
            {
                bool on = SmartSize;
                _smartSizeOffByUser = on;
                if (on)
                {
                    TrySetZoom(100);
                    SmartSize = false;
                }
                else
                {
                    SmartSize = true;
                    UpscaleFixedSession();
                }
            }
            catch (Exception ex)
            {
                Runtime.MessageCollector.AddExceptionStackTrace(Language.RdpToggleSmartSizeFailed, ex);
            }
        }

        private void EnsureSmartSizing()
        {
            if (connectionInfo == null) return;
            // SmartSizing is off on purpose while the control zooms a smaller session instead.
            if (_zoomedInsteadOfSmartSize) return;

            if (ConfiguredSmartSize() && !SmartSize)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"EnsureSmartSizing - Re-applying SmartSizing for '{connectionInfo.Hostname}' after restore");
                SmartSize = true;
            }
        }

        private bool ConfiguredSmartSize()
        {
            if (connectionInfo == null) return false;

            var sizingMode = connectionInfo.RDPSizingMode;
            if (connectionInfo.Resolution == RDPResolutions.SmartSize)
                sizingMode = RDPSizingMode.SmartSize;
            else if (connectionInfo.Resolution == RDPResolutions.SmartSizeAspect)
                sizingMode = RDPSizingMode.SmartSizeAspect;

            return sizingMode == RDPSizingMode.SmartSize || sizingMode == RDPSizingMode.SmartSizeAspect;
        }

        /// <summary>
        /// False when a new session size is reached only by reconnecting.
        /// RDP 9 and later override this because the display channel can take the new size.
        /// </summary>
        protected virtual bool SupportsDynamicResize => false;

        internal readonly record struct SessionResizeDecision(bool Apply, Size Size);

        /// <summary>
        /// Panel client area minus the frame padding. The ActiveX control sizes itself to the
        /// session, so its own size is not a measurement of the space available.
        /// </summary>
        internal static Size ContentSize(Rectangle client, Padding padding)
        {
            int width = client.Width - padding.Horizontal;
            int height = client.Height - padding.Vertical;
            return new Size(Math.Max(0, width), Math.Max(0, height));
        }

        /// <summary>
        /// A fullscreen session applies the screen bounds even when the saved resolution is
        /// still FitToWindow, which is what ToggleFullscreen leaves in place. A windowed
        /// FitToWindow session on a client that cannot resize in place does not apply a size.
        /// A non-positive size does not apply.
        /// </summary>
        internal static SessionResizeDecision DecideSessionResize(
            bool supportsDynamicResize,
            RDPResolutions resolution,
            bool fullscreen,
            Size fullscreenBounds,
            Rectangle client,
            Padding padding)
        {
            if (!fullscreen && !supportsDynamicResize && resolution == RDPResolutions.FitToWindow)
                return new SessionResizeDecision(false, Size.Empty);

            Size size = fullscreen ? fullscreenBounds : ContentSize(client, padding);
            if (size.Width <= 0 || size.Height <= 0)
                return new SessionResizeDecision(false, size);

            return new SessionResizeDecision(true, size);
        }

        /// <summary>
        /// True when the panel's content size changed while a non-fullscreen size was applied.
        /// </summary>
        internal static bool NeedsAnotherPass(bool fullscreen, Size applied, Rectangle client, Padding padding)
        {
            if (fullscreen)
                return false;

            Size settled = ContentSize(client, padding);
            return settled.Width > 0 && settled.Height > 0 && settled != applied;
        }

        protected void UpdateSessionDisplaySettings(uint width, uint height) =>
            UpdateSessionDisplaySettings(width, height, DesktopScaleFactor);

        /// <param name="desktopScale">
        /// The remote desktop scale. It differs from <see cref="RdpProtocol.DesktopScaleFactor"/>
        /// only while adaptive resolution renders the session smaller than the panel.
        /// </param>
        protected virtual void UpdateSessionDisplaySettings(uint width, uint height, uint desktopScale)
        {
            if (WindowedFullscreenSizingActive)
            {
                SmartSize = true;
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"Windowed fullscreen sizing: skipped legacy reconnect for {width}x{height}; SmartSize remains enabled.");
                return;
            }

            if (RdpClient8 != null)
            {
                RdpClient8.Reconnect(width, height);
            }
        }

        #region Adaptive resolution

        protected override void OnLinkQuality(long atMs, int? rttMs) =>
            PostAdaptive(c => rttMs is int rtt ? c.Sample(atMs, rtt) : c.Loss(atMs));

        protected override void OnLinkDrop(long atMs) => PostAdaptive(c => c.Drop(atMs));

        /// <summary>
        /// Probe results arrive on a thread-pool thread; the controller, the DPI read and the resize
        /// all run on the UI thread, so nothing here needs a lock of its own.
        /// </summary>
        private void PostAdaptive(Func<RdpAdaptiveResolution, RdpAdaptiveResolutionDecision?> feed)
        {
            Control? host = InterfaceControl;
            if (host == null || host.IsDisposed || !host.IsHandleCreated)
                return;
            try
            {
                host.BeginInvoke(() => FeedAdaptive(feed));
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
                // The tab closed between the check and the post.
            }
        }

        private void FeedAdaptive(Func<RdpAdaptiveResolution, RdpAdaptiveResolutionDecision?> feed)
        {
            try
            {
                if (!loginComplete || InterfaceControl == null || InterfaceControl.IsDisposed)
                    return;

                uint baseScale = DesktopScaleFactor;
                if (_adaptive == null || _adaptiveBaseScale != baseScale)
                {
                    _adaptiveBaseScale = baseScale;
                    _adaptiveLadder = RdpAdaptiveResolution.RemoteScaleLadder(baseScale);
                    _adaptive = new RdpAdaptiveResolution(_adaptiveLadder.Count);
                }

                RdpAdaptiveResolutionDecision? decision = feed(_adaptive);
                if (decision == null)
                    return;

                bool enabled = AdaptiveResolutionEnabled;
                RuntimeDiagnostics.RdpResolutionDecision(DiagnosticSession, decision, baseScale,
                    _adaptiveLadder[decision.FromLevel], _adaptiveLadder[decision.ToLevel], enabled);
                // Fullscreen or a fixed resolution keeps the base scale; nothing to re-lay out then.
                uint applied = _appliedRemoteScale == 0 ? baseScale : _appliedRemoteScale;
                if (enabled && AdaptiveRemoteScale(baseScale) != applied)
                    DoResizeClient();
            }
            catch (Exception ex)
            {
                RuntimeDiagnostics.SafeException("rdp_resolution", ex);
            }
        }

        /// <summary>
        /// Leaving fullscreen restores the SmartSizing state from before it, which resets the zoom
        /// when that state was on; a reduced or fixed smaller session needs its zoom back.
        /// </summary>
        protected override void OnLeftFullscreen()
        {
            if (_adaptiveTarget != Size.Empty && AdaptiveRemoteScale(DesktopScaleFactor) < DesktopScaleFactor)
                ApplyUpscale(_adaptiveTarget, "adaptive");
            else
                UpscaleFixedSession();
        }

        /// <summary>
        /// Runs after the first logon and after every automatic reconnection. A level that could
        /// not be applied while the link was down (lab, 2026-10-20: the step down came with the
        /// drop, and the control answered 0x8000FFFF while reconnecting) is applied now.
        /// </summary>
        protected override void OnLoginCompleteSizing()
        {
            // Field, 2026-10-20: after an automatic reconnection the session came back at another
            // size while the control kept the 125 % zoom of the reduced one, so it overflowed the
            // panel with scroll bars. Unzoom first; the level is applied again below.
            if (ZoomLevel != 100)
            {
                ForgetZoom();
                TrySetZoom(100);
                if (_zoomedInsteadOfSmartSize)
                {
                    _zoomedInsteadOfSmartSize = false;
                    if (ConfiguredSmartSize() && !_smartSizeOffByUser)
                        SmartSize = true;
                }
            }
            _adaptiveTarget = Size.Empty;
            _appliedRemoteScale = 0;
            _adaptiveRetries = 0;
            UpscaleFixedSession();
            if (AdaptiveResolutionEnabled && _adaptive is { Level: > 0 })
                ScheduleDebouncedResize();
        }

        /// <summary>
        /// The control answers "not ready" for a while after an automatic reconnection, also after
        /// it has raised OnLoginComplete (field, 2026-10-20: refused 0.1 s after logon). The step is
        /// asked again every two seconds, a bounded number of times; the next decision or logon
        /// asks again after that.
        /// </summary>
        private void ScheduleAdaptiveRetry()
        {
            if (_adaptiveRetries >= AdaptiveMaxRetries)
                return;
            _adaptiveRetries++;
            if (_adaptiveRetryTimer == null)
            {
                _adaptiveRetryTimer = new System.Windows.Forms.Timer { Interval = AdaptiveRetryMs };
                _adaptiveRetryTimer.Tick += AdaptiveRetryTimer_Tick;
            }
            _adaptiveRetryTimer.Stop();
            _adaptiveRetryTimer.Start();
        }

        private void AdaptiveRetryTimer_Tick(object? sender, EventArgs e)
        {
            _adaptiveRetryTimer?.Stop();
            if (Control == null || Control.IsDisposed || InterfaceControl == null || InterfaceControl.IsDisposed)
                return;
            DoResizeClient();
        }

        /// <summary>
        /// A connection with its own resolution and SmartSizing asks for the session to be scaled
        /// to the panel. SmartSizing does that only when the session is larger; a smaller one
        /// (lab, 2026-10-20: 480x288 centred in 668x500; a laptop: 1288x815 in 1610x1019) is
        /// zoomed instead. Fit-to-panel sessions take their size from the panel and go through
        /// <see cref="DoResizeClient"/>; fullscreen and windowed fullscreen fill the screen.
        /// </summary>
        private void UpscaleFixedSession()
        {
            if (!loginComplete || connectionInfo == null || Control == null || Control.IsDisposed)
                return;
            if (Fullscreen || WindowedFullscreenSizingActive || !ConfiguredSmartSize() || _smartSizeOffByUser)
                return;
            if (connectionInfo.Resolution is RDPResolutions.FitToWindow or RDPResolutions.Fullscreen)
                return;
            Size session = WindowedFullscreenDesktopSize;
            if (session.Width <= 0 || session.Height <= 0)
                return;
            ApplyUpscale(session, "fixed");
        }

        /// <summary>
        /// Zooms a session smaller than the control's surface up to it, or, when it is not smaller,
        /// takes the zoom off and gives the connection its own SmartSizing back.
        /// </summary>
        private void ApplyUpscale(Size session, string reason)
        {
            if (Control == null || Control.IsDisposed)
                return;
            Size surface = Control.ClientSize;
            uint zoom = FitZoom(surface, session);
            uint before = ZoomLevel;
            if (zoom > 100)
            {
                if (TrySetZoom(zoom))
                    _zoomedInsteadOfSmartSize = true;
            }
            else if (_zoomedInsteadOfSmartSize || before != 100)
            {
                TrySetZoom(100);
                _zoomedInsteadOfSmartSize = false;
                if (ConfiguredSmartSize())
                    SmartSize = true;
            }
            if (ZoomLevel != before)
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"[rdp-zoom] {reason}: zoom {before} -> {ZoomLevel} % for session {session.Width}x{session.Height} " +
                    $"in surface {surface.Width}x{surface.Height}");
        }

        /// <summary>
        /// The largest whole-percent zoom at which <paramref name="session"/> still fits inside
        /// <paramref name="surface"/>, between 100 (no zoom) and <see cref="RdpProtocol.MaxZoom"/>.
        /// </summary>
        internal static uint FitZoom(Size surface, Size session)
        {
            if (session.Width <= 0 || session.Height <= 0 || surface.Width <= 0 || surface.Height <= 0)
                return 100;
            long zoom = Math.Min((long)surface.Width * 100 / session.Width, (long)surface.Height * 100 / session.Height);
            return (uint)Math.Clamp(zoom, 100, MaxZoom);
        }

        /// <summary>The device scale RDP accepts (100, 140, 180) nearest a desktop scale.</summary>
        internal static uint DeviceScaleFor(uint desktopScale) =>
            desktopScale <= 120 ? 100u : desktopScale <= 160 ? 140u : 180u;

        private bool AdaptiveResolutionEnabled =>
            Properties.OptionsAdvancedPage.Default.RdpAdaptiveResolution && !_adaptiveDisabled;

        /// <summary>
        /// The remote scale for the current level, or the base scale whenever the level must not
        /// apply: the feature is off, the session cannot resize in place, it is fullscreen, or the
        /// connection asked for a fixed or client-scaled resolution.
        /// </summary>
        private uint AdaptiveRemoteScale(uint baseScale)
        {
            if (!AdaptiveResolutionEnabled || _adaptive == null || _adaptiveBaseScale != baseScale)
                return baseScale;
            if (!SupportsDynamicResize || Fullscreen || WindowedFullscreenSizingActive)
                return baseScale;
            if (InterfaceControl?.Info.Resolution != RDPResolutions.FitToWindow)
                return baseScale;
            return _adaptiveLadder[_adaptive.Level];
        }

        internal const int MinAdaptedSide = 200;

        /// <summary>
        /// The session size that, stretched back to <paramref name="panel"/>, shows the remote
        /// desktop at the size the base scale would. Sides are even. A result below the smallest
        /// side the protocol accepts leaves the panel size unchanged.
        /// </summary>
        internal static Size AdaptedSize(Size panel, uint baseScale, uint remoteScale)
        {
            if (remoteScale >= baseScale || baseScale == 0)
                return panel;
            int width = (int)((long)panel.Width * remoteScale / baseScale) & ~1;
            int height = (int)((long)panel.Height * remoteScale / baseScale) & ~1;
            return width < MinAdaptedSide || height < MinAdaptedSide ? panel : new Size(width, height);
        }

        private void NoteAppliedRemoteScale(uint baseScale, uint remoteScale, Size target)
        {
            uint previous = _appliedRemoteScale == 0 ? baseScale : _appliedRemoteScale;
            _appliedRemoteScale = remoteScale;
            if (previous != remoteScale)
                RuntimeDiagnostics.RdpResolutionApplied(DiagnosticSession, "applied", baseScale, previous,
                    remoteScale, target.Width, target.Height, null);
        }

        /// <summary>
        /// A control that refuses the smaller size is not asked again for this session, and the
        /// session goes back to the panel size; the legacy fallback would reconnect instead.
        /// "Not ready" (0x8000FFFF) is the control between a drop and its reconnection: the step
        /// waits for the next logon instead.
        /// </summary>
        protected void AdaptiveUpdateFailed(uint width, uint height, uint desktopScale, Exception ex)
        {
            _adaptiveUpdateRefused = true;
            bool notReady = ex is System.Runtime.InteropServices.COMException && ex.HResult == unchecked((int)0x8000FFFF);
            RuntimeDiagnostics.RdpResolutionApplied(DiagnosticSession, notReady ? "deferred" : "failed", DesktopScaleFactor,
                _appliedRemoteScale, desktopScale, (int)width, (int)height, ex.HResult);
            if (notReady)
            {
                ScheduleAdaptiveRetry();
                return;
            }
            _adaptiveDisabled = true;
            ScheduleDebouncedResize();
        }

        #endregion

        public override void Close()
        {
            ReleaseResizeSubscriptions();
            base.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ReleaseResizeSubscriptions();
            base.Dispose(disposing);
        }

        private void ReleaseResizeSubscriptions()
        {
            _frmMain.ResizeEnd -= ResizeEnd;
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChangedHandler;

            // Clean up debounce timer
            if (_resizeDebounceTimer != null)
            {
                _resizeDebounceTimer.Stop();
                _resizeDebounceTimer.Elapsed -= ResizeDebounceTimer_Elapsed;
                _resizeDebounceTimer.Dispose();
                _resizeDebounceTimer = null;
            }

            _windowedFullscreenSettleTimer?.Stop();
            _windowedFullscreenSettleTimer?.Dispose();
            _windowedFullscreenSettleTimer = null;

            if (_adaptiveRetryTimer != null)
            {
                _adaptiveRetryTimer.Stop();
                _adaptiveRetryTimer.Tick -= AdaptiveRetryTimer_Tick;
                _adaptiveRetryTimer.Dispose();
                _adaptiveRetryTimer = null;
            }
            _windowedFullscreenRetryPending = false;

        }

        private void StartWindowedFullscreenSettleCheck(Size target)
        {
            _windowedFullscreenTarget = target;
            _windowedFullscreenSettleAttempts = 0;
            _windowedFullscreenSettleTimer ??= new System.Windows.Forms.Timer { Interval = 100 };
            _windowedFullscreenSettleTimer.Interval = _windowedFullscreenRetryPending ? 500 : 100;
            _windowedFullscreenSettleTimer.Tick -= WindowedFullscreenSettleTimer_Tick;
            _windowedFullscreenSettleTimer.Tick += WindowedFullscreenSettleTimer_Tick;
            _windowedFullscreenSettleTimer.Stop();
            _windowedFullscreenSettleTimer.Start();
        }

        private void WindowedFullscreenSettleTimer_Tick(object? sender, EventArgs e)
        {
            if (!WindowedFullscreenSizingActive || Control == null || InterfaceControl == null ||
                Control.IsDisposed || InterfaceControl.IsDisposed)
            {
                _windowedFullscreenSettleTimer?.Stop();
                return;
            }

            Size desktop = WindowedFullscreenDesktopSize;
            _windowedFullscreenSettleAttempts++;
            if (_windowedFullscreenRetryPending)
            {
                if (_windowedFullscreenSettleAttempts > 10)
                {
                    _windowedFullscreenRetryPending = false;
                    _windowedFullscreenSettleTimer?.Stop();
                    Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                        $"phase=windowed_fullscreen_retry_timeout target={_windowedFullscreenTarget.Width}x{_windowedFullscreenTarget.Height} " +
                        $"desktop={desktop.Width}x{desktop.Height} attempts={_windowedFullscreenSettleAttempts - 1}");
                    return;
                }

                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"phase=windowed_fullscreen_retry attempt={_windowedFullscreenSettleAttempts} " +
                    $"target={_windowedFullscreenTarget.Width}x{_windowedFullscreenTarget.Height} desktop={desktop.Width}x{desktop.Height}");
                _windowedFullscreenRetryPending = false;
                UpdateSessionDisplaySettings((uint)_windowedFullscreenTarget.Width, (uint)_windowedFullscreenTarget.Height);
                if (_windowedFullscreenRetryPending && _windowedFullscreenSettleAttempts >= 10)
                {
                    _windowedFullscreenRetryPending = false;
                    _windowedFullscreenSettleTimer?.Stop();
                    Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                        $"phase=windowed_fullscreen_retry_timeout target={_windowedFullscreenTarget.Width}x{_windowedFullscreenTarget.Height} " +
                        $"desktop={WindowedFullscreenDesktopSize.Width}x{WindowedFullscreenDesktopSize.Height} attempts={_windowedFullscreenSettleAttempts}");
                    return;
                }
                if (!_windowedFullscreenRetryPending && _windowedFullscreenSettleTimer != null)
                    _windowedFullscreenSettleTimer.Interval = 100;
                return;
            }

            if (desktop == _windowedFullscreenTarget)
            {
                _windowedFullscreenSettleTimer?.Stop();
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"phase=windowed_fullscreen_settled panel={InterfaceControl.ClientSize.Width}x{InterfaceControl.ClientSize.Height} " +
                    $"target={_windowedFullscreenTarget.Width}x{_windowedFullscreenTarget.Height} " +
                    $"desktop={desktop.Width}x{desktop.Height} smartSize={SmartSize} " +
                    $"control={Control.Width}x{Control.Height}");
            }
            else if (_windowedFullscreenSettleAttempts >= 20)
            {
                _windowedFullscreenSettleTimer?.Stop();
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    $"phase=windowed_fullscreen_settle_timeout target={_windowedFullscreenTarget.Width}x{_windowedFullscreenTarget.Height} " +
                    $"desktop={desktop.Width}x{desktop.Height}");
            }
        }

        protected void ScheduleWindowedFullscreenDynamicRetry(uint width, uint height)
        {
            if (!WindowedFullscreenSizingActive || !loginComplete)
                return;

            _windowedFullscreenTarget = new Size((int)width, (int)height);
            _windowedFullscreenRetryPending = true;
            _windowedFullscreenSettleTimer ??= new System.Windows.Forms.Timer();
            _windowedFullscreenSettleTimer.Tick -= WindowedFullscreenSettleTimer_Tick;
            _windowedFullscreenSettleTimer.Tick += WindowedFullscreenSettleTimer_Tick;
            _windowedFullscreenSettleTimer.Interval = 500;
            _windowedFullscreenSettleTimer.Start();
        }

        protected void ClearWindowedFullscreenDynamicRetry()
        {
            _windowedFullscreenRetryPending = false;
        }

    }
}

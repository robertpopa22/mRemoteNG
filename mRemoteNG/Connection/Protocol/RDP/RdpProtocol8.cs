using System;
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

                    EnsureSmartSizing();
                }
            }

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

            // Check if controls are still valid (not disposed during shutdown)
            if (Control == null || Control.IsDisposed || InterfaceControl == null || InterfaceControl.IsDisposed)
            {
                _hasPendingResize = false;
                return;
            }

            _hasPendingResize = false;

            Runtime.MessageCollector?.AddMessage(MessageClass.DebugMsg,
                $"Debounce timer fired - executing delayed resize to {_pendingResizeSize.Width}x{_pendingResizeSize.Height}");

            // Marshal to the UI thread because DoResizeClient() accesses WinForms and COM objects
            if (InterfaceControl.InvokeRequired)
            {
                InterfaceControl.BeginInvoke(new Action(DoResizeClient));
            }
            else
            {
                DoResizeClient();
            }
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

                DevLog.Write($"Calling UpdateSessionDisplaySettings({decision.Size.Width}, {decision.Size.Height})");
                UpdateSessionDisplaySettings((uint)decision.Size.Width, (uint)decision.Size.Height);

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
        private void EnsureSmartSizing()
        {
            if (connectionInfo == null) return;

            var sizingMode = connectionInfo.RDPSizingMode;
            if (connectionInfo.Resolution == RDPResolutions.SmartSize)
                sizingMode = RDPSizingMode.SmartSize;
            else if (connectionInfo.Resolution == RDPResolutions.SmartSizeAspect)
                sizingMode = RDPSizingMode.SmartSizeAspect;

            bool shouldBeSmartSized = sizingMode == RDPSizingMode.SmartSize ||
                                     sizingMode == RDPSizingMode.SmartSizeAspect;

            if (shouldBeSmartSized && !SmartSize)
            {
                Runtime.MessageCollector.AddMessage(MessageClass.DebugMsg,
                    $"EnsureSmartSizing - Re-applying SmartSizing for '{connectionInfo.Hostname}' after restore");
                SmartSize = true;
            }
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
        /// FitToWindow on a client that cannot resize in place does not apply a session size.
        /// Fullscreen and the other resolution modes still apply. A non-positive size does not apply.
        /// </summary>
        internal static SessionResizeDecision DecideSessionResize(
            bool supportsDynamicResize,
            RDPResolutions resolution,
            bool fullscreen,
            Size fullscreenBounds,
            Rectangle client,
            Padding padding)
        {
            if (!supportsDynamicResize && resolution == RDPResolutions.FitToWindow)
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

        protected virtual void UpdateSessionDisplaySettings(uint width, uint height)
        {
            if (RdpClient8 != null)
            {
                RdpClient8.Reconnect(width, height);
            }
        }

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

        }

    }
}

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using mRemoteNG.App;
using mRemoteNG.UI.Tabs;

namespace mRemoteNG.Connection.Protocol.RDP;

internal enum RdpInputFocus { Unavailable, None, Host, NativeChild, Other }
internal enum RdpInputForeground { None, Application, Other, Unavailable }
internal enum RdpInputActivation { Unavailable, None, HostRoot, Other }
internal enum RdpInputPhase { Initialized, Connecting, Connected, LoggedIn, Reconnecting, Disconnected, Closing }
internal enum RdpInputReason { Lifecycle, ManagedFocus, ManagedBlur, ControlState, FocusRequested, FocusReturned, Sample, Fullscreen }

// Deliberately no input values, arbitrary strings, window handles or window metadata.
internal readonly record struct RdpInputSnapshot(
    RdpInputPhase Phase, RdpInputFocus ThreadFocus, RdpInputForeground Foreground,
    RdpInputActivation ThreadActivation, bool Ready, bool Visible, bool Enabled,
    bool ManagedFocused, bool ManagedContainsFocus, bool CurrentTab,
    bool ViewOnly, bool RedirectKeys, bool RefocusDisabled)
{
    internal bool PeriodicActive => Ready && Visible && Foreground == RdpInputForeground.Application &&
        (CurrentTab || ThreadFocus is RdpInputFocus.Host or RdpInputFocus.NativeChild);
}

internal readonly record struct RdpInputSample(
    RdpInputSnapshot State, RdpInputReason Reason, long ElapsedMs, int Suppressed);

internal sealed class RdpInputBudget(int limit)
{
    private readonly System.Threading.Lock _sync = new();
    private long _windowStart;
    private int _count;

    internal bool TryTake(long now)
    {
        lock (_sync)
        {
            if (now < _windowStart || now - _windowStart >= 60_000)
            {
                _windowStart = now;
                _count = 0;
            }
            if (_count >= limit) return false;
            _count++;
            return true;
        }
    }
}

internal sealed class RdpInputTrace(RdpInputBudget processBudget)
{
    internal const int SessionLimitPerMinute = 24;
    internal const int ProcessLimitPerMinute = 120;
    private readonly RdpInputBudget _sessionBudget = new(SessionLimitPerMinute);
    private RdpInputSample? _last;
    private int _suppressed;

    internal static int SampleIntervalMs(bool lowPower) => lowPower ? 30_000 : 5_000;
    internal static int HeartbeatIntervalMs(bool lowPower) => lowPower ? 300_000 : 60_000;

    internal bool Observe(long now, long elapsedMs, RdpInputSnapshot state, RdpInputReason reason,
        bool lowPower, out RdpInputSample sample)
    {
        sample = default;
        if (_last is { } last)
        {
            bool changed = state != last.State;
            if (reason == RdpInputReason.Sample && !changed &&
                (!state.PeriodicActive || elapsedMs - last.ElapsedMs < HeartbeatIntervalMs(lowPower)))
                return false;
            if (!changed && reason == last.Reason && elapsedMs - last.ElapsedMs < 1_000)
                return false;
        }
        else if (reason == RdpInputReason.Sample && !state.PeriodicActive)
            return false;

        if (!_sessionBudget.TryTake(now) || !processBudget.TryTake(now))
        {
            if (_suppressed < int.MaxValue) _suppressed++;
            return false;
        }
        sample = new(state, reason, Math.Max(0, elapsedMs), _suppressed);
        _last = sample;
        _suppressed = 0;
        return true;
    }

    internal static RdpInputFocus ClassifyFocus(IntPtr host, IntPtr focus, Func<IntPtr, IntPtr, bool> isChild)
    {
        if (host == IntPtr.Zero) return RdpInputFocus.Unavailable;
        if (focus == IntPtr.Zero) return RdpInputFocus.None;
        if (focus == host) return RdpInputFocus.Host;
        return isChild(host, focus) ? RdpInputFocus.NativeChild : RdpInputFocus.Other;
    }
}

/// <summary>Observes focus on the control's UI thread; never handles keys or changes focus (#223).</summary>
internal sealed class RdpInputDiagnostics : IDisposable
{
    private static readonly RdpInputBudget ProcessBudget = new(RdpInputTrace.ProcessLimitPerMinute);
    private readonly Control _control;
    private readonly string _session;
    private readonly Func<(bool ViewOnly, bool RedirectKeys, bool RefocusDisabled)> _options;
    private readonly Timer _timer = new();
    private readonly RdpInputTrace _trace = new(ProcessBudget);
    private readonly long _startedMs = Environment.TickCount64;
    private RdpInputPhase _phase;
    private bool _disposed;
    private bool _periodicActive;
    private bool _captureFailed;

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, uint flags);

    internal RdpInputDiagnostics(Control control, string session,
        Func<(bool ViewOnly, bool RedirectKeys, bool RefocusDisabled)> options)
    {
        _control = control;
        _session = session;
        _options = options;
        try
        {
            _control.GotFocus += OnFocus;
            _control.Enter += OnFocus;
            _control.LostFocus += OnBlur;
            _control.Leave += OnBlur;
            _control.VisibleChanged += OnControlState;
            _control.EnabledChanged += OnControlState;
            _control.ParentChanged += OnControlState;
            _control.HandleCreated += OnControlState;
            _control.HandleDestroyed += OnControlState;
            _timer.Interval = RdpInputTrace.SampleIntervalMs(PowerAwareness.LowPowerMode);
            _timer.Tick += OnTick;
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void SetPhase(RdpInputPhase phase)
    {
        try
        {
            if (_disposed || _control.InvokeRequired) return;
            _phase = phase;
            if (phase is RdpInputPhase.Connecting or RdpInputPhase.Connected or RdpInputPhase.LoggedIn or RdpInputPhase.Reconnecting)
                _timer.Start();
            else
                _timer.Stop();
            Record(RdpInputReason.Lifecycle);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private bool IsCurrentTab()
    {
        for (Control? parent = _control.Parent; parent != null; parent = parent.Parent)
            if (parent is ConnectionTab tab)
                return ReferenceEquals(TabHelper.Instance.CurrentTab, tab);
        return false;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        try
        {
            if (_disposed) return;
            _timer.Interval = RdpInputTrace.SampleIntervalMs(PowerAwareness.LowPowerMode);
            // One final snapshot records a lost foreground/tab, then inactive tabs remain silent.
            if (!_periodicActive && (PowerAwareness.IsMinimized || !_control.Visible || !IsCurrentTab())) return;
            if (!_periodicActive && ReadForeground() != RdpInputForeground.Application) return;
            Record(RdpInputReason.Sample);
        }
        catch (Exception ex) { ReportFailure(ex); }
    }

    private static RdpInputForeground ReadForeground()
    {
        IntPtr foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero) return RdpInputForeground.None;
        if (NativeMethods.GetWindowThreadProcessId(foreground, out uint process) == 0)
            return RdpInputForeground.Unavailable;
        return process == (uint)Environment.ProcessId ? RdpInputForeground.Application : RdpInputForeground.Other;
    }

    private void OnFocus(object? sender, EventArgs e) => Record(RdpInputReason.ManagedFocus);
    private void OnBlur(object? sender, EventArgs e) => Record(RdpInputReason.ManagedBlur);
    private void OnControlState(object? sender, EventArgs e) => Record(RdpInputReason.ControlState);

    internal void Record(RdpInputReason reason)
    {
        try
        {
            if (_disposed || _control.IsDisposed || _control.InvokeRequired) return;
            bool ready = _control.IsHandleCreated && !_control.Disposing;
            // Reading Handle only after this check avoids creating a native window for diagnostics.
            IntPtr host = ready ? _control.Handle : IntPtr.Zero;
            IntPtr root = ready ? GetAncestor(host, 2 /* GA_ROOT */) : IntPtr.Zero;
            IntPtr active = GetActiveWindow();
            var options = _options();
            // GetFocus/GetActiveWindow describe this UI thread's queue, never another app's input.
            RdpInputSnapshot state = new(_phase,
                RdpInputTrace.ClassifyFocus(host, NativeMethods.GetFocus(), NativeMethods.IsChild),
                ReadForeground(),
                !ready || root == IntPtr.Zero ? RdpInputActivation.Unavailable : active == IntPtr.Zero ? RdpInputActivation.None :
                    active == root ? RdpInputActivation.HostRoot : RdpInputActivation.Other,
                ready, _control.Visible, _control.Enabled, _control.Focused, _control.ContainsFocus,
                IsCurrentTab(), options.ViewOnly, options.RedirectKeys, options.RefocusDisabled);
            _periodicActive = state.PeriodicActive;
            long now = Environment.TickCount64;
            if (_trace.Observe(now, now - _startedMs, state, reason, PowerAwareness.LowPowerMode, out var sample))
                RuntimeDiagnostics.RdpInput(_session, sample);
        }
        catch (Exception ex)
        {
            ReportFailure(ex);
        }
    }

    private void ReportFailure(Exception ex)
    {
        if (_captureFailed) return;
        _captureFailed = true;
        try { RuntimeDiagnostics.SafeException("rdp_input", ex); }
        catch { /* A diagnostic sink failure cannot change RDP behavior. */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            try { _timer.Stop(); }
            finally { _timer.Dispose(); }
        }
        catch (Exception ex) { ReportFailure(ex); }
        try
        {
            _timer.Tick -= OnTick;
            _control.GotFocus -= OnFocus;
            _control.Enter -= OnFocus;
            _control.LostFocus -= OnBlur;
            _control.Leave -= OnBlur;
            _control.VisibleChanged -= OnControlState;
            _control.EnabledChanged -= OnControlState;
            _control.ParentChanged -= OnControlState;
            _control.HandleCreated -= OnControlState;
            _control.HandleDestroyed -= OnControlState;
        }
        catch (Exception ex) { ReportFailure(ex); }
    }
}

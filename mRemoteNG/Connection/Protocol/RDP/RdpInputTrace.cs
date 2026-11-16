using System;

namespace mRemoteNG.Connection.Protocol.RDP;

internal sealed class RdpInputTrace
{
    internal const int SessionLimitPerMinute = 24;
    internal const int ProcessLimitPerMinute = 120;
    private readonly RdpInputBudget _processBudget;
    private readonly RdpInputBudget _sessionBudget;
    private RdpInputSample? _last;
    private int _suppressed;

    public RdpInputTrace(RdpInputBudget processBudget)
    {
        _processBudget = processBudget;
        _sessionBudget = new(SessionLimitPerMinute);
    }

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
            {
                return false;
            }
            if (!changed && reason == last.Reason && elapsedMs - last.ElapsedMs < 1_000)
                return false;
        }
        else if (reason == RdpInputReason.Sample && !state.PeriodicActive)
        {
            return false;
        }

        if (!_sessionBudget.TryTake(now) || !_processBudget.TryTake(now))
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

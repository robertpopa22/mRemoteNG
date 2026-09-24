using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Versioning;
using mRemoteNG.Messages;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Temporary instrumentation for the two regressions reported on #182 after the RDP memory
    /// fix: a connection panel that stays open after its last tab closes, and a process that
    /// outlives its main window. The fix attempts for #182 are used up; the repository's rule is
    /// that the next build carries evidence, not a third guess. Every line is tagged [#182-diag],
    /// names the managed thread, and carries elapsed milliseconds, so one log from an affected
    /// machine shows which close path ran, in which order and on which thread, and which step
    /// never returned. Log-only; remove once the cause is found (as with #118/#143).
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class ClosePathDiagnostics
    {
        internal static long Now() => Stopwatch.GetTimestamp();

        internal static long Since(long start) => (long)Stopwatch.GetElapsedTime(start).TotalMilliseconds;

        internal static void Log(string message)
        {
            try
            {
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    string.Create(CultureInfo.InvariantCulture, $"[#182-diag] t{Environment.CurrentManagedThreadId} {message}"),
                    true);
            }
            catch (Exception)
            {
                // Instrumentation must never change what it is measuring.
            }
        }

        /// <summary>Logs the start of a step and, even if it throws, how long it took.</summary>
        internal static void Time(string step, Action action)
        {
            long start = Now();
            Log(step + " start");
            try
            {
                action();
            }
            finally
            {
                Log(string.Create(CultureInfo.InvariantCulture, $"{step} returned after {Since(start)} ms"));
            }
        }
    }
}

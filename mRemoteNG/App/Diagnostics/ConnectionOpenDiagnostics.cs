using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using mRemoteNG.Messages;
using mRemoteNG.UI.Forms;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Temporary instrumentation for #216: a connection opened through an SSH tunnel builds its
    /// target control on a thread-pool thread, so it cannot be parented to the tab created on the
    /// UI thread. The source shows no await that drops the UI context on that path, so the next
    /// build records, at each step of the open, which thread runs it, which synchronization
    /// context it has, and which methods called OpenConnection. Every line is tagged [#216-diag].
    /// Method names and protocol only: no connection names, hosts or ports. Remove once the cause
    /// is found.
    /// </summary>
    [SupportedOSPlatform("windows")]
    internal static class ConnectionOpenDiagnostics
    {
        internal static void Log(string step, Control? owner = null)
        {
            try
            {
                SynchronizationContext? context = SynchronizationContext.Current;
                string contextName = context?.GetType().Name ?? "none";
                string ui = FrmMain.IsCreated ? (FrmMain.Default.InvokeRequired ? "no" : "yes") : "unknown";
                string ownerPart = owner == null
                    ? string.Empty
                    : string.Create(CultureInfo.InvariantCulture, $" owner_invoke_required={owner.InvokeRequired}");
                Runtime.MessageCollector?.AddMessage(MessageClass.InformationMsg,
                    string.Create(CultureInfo.InvariantCulture,
                        $"[#216-diag] {step} t{Environment.CurrentManagedThreadId} ui_thread={ui} sync_context={contextName}{ownerPart}"),
                    true);
            }
            catch (Exception)
            {
                // Instrumentation must never change what it is measuring.
            }
        }

        internal static void LogEntry(string protocol, bool tunnel, bool waitForIp, string force)
        {
            try
            {
                string callers = string.Join(" < ", new StackTrace(1, false).GetFrames()
                    .Select(f => f.GetMethod())
                    .Where(m => m != null)
                    .Take(7)
                    .Select(m => $"{m!.DeclaringType?.Name}.{m.Name}"));
                Log(string.Create(CultureInfo.InvariantCulture,
                    $"open_entry protocol={protocol} tunnel={tunnel} wait_for_ip={waitForIp} force={force} callers={callers}"));
            }
            catch (Exception)
            {
            }
        }
    }
}

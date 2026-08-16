using System;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools;

namespace mRemoteNGSpecs.Drivers
{
    /// <summary>
    /// Detects that the application crashed, rather than inferring it.
    ///
    /// Half the issues this battery covers are crashes, and "the test finished without throwing" is
    /// not evidence that the app survived — an unhandled exception on the UI thread shows a dialog
    /// and leaves the process alive, so a naive test sails past it. mRemoteNG routes every
    /// unhandled exception through a window of its own, which makes the crash directly observable
    /// instead of a guess.
    /// </summary>
    public static class CrashWatcher
    {
        public readonly record struct CrashResult(bool Occurred, string Description);

        private const string CrashWindowAutomationId = "FrmUnhandledException";

        public static CrashResult Check(AppDriver driver, TimeSpan? grace = null)
        {
            TimeSpan wait = grace ?? TimeSpan.FromSeconds(2);

            try
            {
                if (driver.Application.HasExited)
                    return new CrashResult(true,
                        $"the process exited unexpectedly (exit code {driver.Application.ExitCode})");
            }
            catch (InvalidOperationException)
            {
                return new CrashResult(true, "the application object is no longer usable");
            }

            AutomationElement? crashWindow = Retry.WhileNull(
                () => driver.Automation.GetDesktop().FindFirstChild(
                    cf => cf.ByAutomationId(CrashWindowAutomationId)
                            .Or(cf.ByName("mRemoteNG - Unhandled Exception"))),
                wait, TimeSpan.FromMilliseconds(150), throwOnTimeout: false).Result;

            if (crashWindow is null)
                return new CrashResult(false, "");

            string detail = SafeName(crashWindow);
            return new CrashResult(true, $"an unhandled-exception dialog appeared: '{detail}'");
        }

        private static string SafeName(AutomationElement element)
        {
            try
            {
                return element.Name;
            }
            catch (Exception)
            {
                return "<unreadable>";
            }
        }
    }
}

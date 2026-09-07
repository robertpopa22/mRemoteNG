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

            // The dialog is an owned window: UI Automation lists it under the application's main
            // window, not as a child of the desktop. Looking at the desktop's direct children
            // only — which is what this did until the lab reproduced #175 — found nothing while
            // the dialog was standing on screen, and every AssertNoCrash in the battery would
            // have passed straight over a crash. Search the whole subtree of each of the
            // application's top-level windows, plus the desktop's own children for the case where
            // the dialog comes up before, or without, a main window.
            AutomationElement? crashWindow = Retry.WhileNull(
                () => FindCrashWindow(driver),
                wait, TimeSpan.FromMilliseconds(150), throwOnTimeout: false).Result;

            if (crashWindow is null)
                return new CrashResult(false, "");

            string detail = SafeName(crashWindow);
            return new CrashResult(true, $"an unhandled-exception dialog appeared: '{detail}'");
        }

        private static AutomationElement? FindCrashWindow(AppDriver driver)
        {
            AutomationElement desktop = driver.Automation.GetDesktop();
            int pid = driver.Application.ProcessId;

            foreach (AutomationElement top in desktop.FindAllChildren())
            {
                int owner;
                try { owner = top.Properties.ProcessId.ValueOrDefault; }
                catch (Exception) { continue; }
                if (owner != pid)
                    continue;

                if (IsCrashWindow(top))
                    return top;

                AutomationElement? nested = top.FindFirstDescendant(
                    cf => cf.ByAutomationId(CrashWindowAutomationId)
                            .Or(cf.ByName("mRemoteNG Unhandled Exception"))
                            .Or(cf.ByName("mRemoteNG - Unhandled Exception")));
                if (nested is not null)
                    return nested;
            }

            return null;
        }

        private static bool IsCrashWindow(AutomationElement element)
        {
            try
            {
                if (string.Equals(element.AutomationId, CrashWindowAutomationId, StringComparison.Ordinal))
                    return true;
                string name = element.Name ?? "";
                return name.Contains("Unhandled Exception", StringComparison.Ordinal);
            }
            catch (Exception)
            {
                return false;
            }
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

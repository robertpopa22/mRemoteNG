using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using mRemoteNGSpecs.Drivers;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Finds and answers modal dialogs raised by the application under test.
    ///
    /// A battery that drives a real remote-connection client meets dialogs that are not defects: the
    /// application asks before closing while sessions are open, and the RDP client asks before
    /// trusting a certificate it has never seen. On the maintainer's workstation neither appears —
    /// the certificate is already trusted and the prompt was answered long ago — so tests written
    /// there quietly assume a dialog-free run and then fail the first time they execute somewhere
    /// clean. That is exactly what happened the first time this battery ran inside the lab guest:
    /// three failures, all reported as product symptoms, all actually an unanswered prompt.
    ///
    /// The rule this class enforces is that dismissing a dialog is never silent. Only prompts named
    /// here are answered, every answer is written to the test output, and anything else is returned
    /// to the caller so the test still fails — with the dialog's own words in the message instead of
    /// a misleading assertion about focus or exit.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class ModalDialogs
    {
        /// <summary>
        /// Describes a modal that belongs to the application: enough to answer it, or to report it.
        /// </summary>
        public sealed record Dialog(AutomationElement? Element, string Title, string Text)
        {
            public override string ToString() =>
                string.IsNullOrWhiteSpace(Text) ? $"'{Title}'" : $"'{Title}' — {Text}";
        }

        /// <summary>
        /// Every modal window owned by the application, excluding its main window.
        ///
        /// Descendants, not children. An owned WinForms MessageBox is not a child of the desktop:
        /// it appears beneath its owner in the UIA tree, so a search over the desktop's immediate
        /// children finds nothing and reports a clear screen while a dialog is holding the keyboard.
        /// That is measured — the first version of this class looked only at desktop children and
        /// answered nothing at all.
        /// </summary>
        public static Dialog[] Find(AppDriver driver)
        {
            try
            {
                HashSet<int> pids = RelevantProcessIds(driver);
                return driver.Automation.GetDesktop()
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Window))
                    .Where(w => pids.Contains(SafeProcessId(w)) && !IsMainWindow(w)
                                && (IsModal(w) || IsKnownHelperAlert(w)))
                    .Select(w => new Dialog(w, SafeName(w), DialogText(w)))
                    .ToArray();
            }
            catch (Exception)
            {
                return [];
            }
        }

        /// <summary>
        /// The application's own process, plus the protocol helpers it launches.
        ///
        /// mRemoteNG does not draw an SSH session itself — it starts PuTTY and reparents its window
        /// into a tab. PuTTY's dialogs therefore belong to PuTTY's process, and a search restricted
        /// to the application's own process cannot see them. That is not hypothetical: an
        /// unanswered "the host key is not cached" alert held an SSH connection open indefinitely,
        /// and a "close this session?" confirmation swallowed the close keystroke, while the dialog
        /// search reported a clear screen. The scenario failed as "the application did not exit" —
        /// the #110 symptom — with nothing wrong in the application at all.
        ///
        /// Matched by executable name rather than by walking the process tree: reading a parent
        /// process id needs native interop, and the set of helpers this application starts is small
        /// and known.
        /// </summary>
        private static HashSet<int> RelevantProcessIds(AppDriver driver)
        {
            HashSet<int> pids = [];

            try { pids.Add(driver.Application.ProcessId); } catch (Exception) { }

            foreach (string name in HelperProcessNames)
            {
                try
                {
                    foreach (System.Diagnostics.Process process in
                             System.Diagnostics.Process.GetProcessesByName(name))
                    {
                        pids.Add(process.Id);
                        process.Dispose();
                    }
                }
                catch (Exception)
                {
                    // A helper that is not running is the normal case.
                }
            }

            return pids;
        }

        private static readonly string[] HelperProcessNames = ["PuTTYNG", "putty", "plink"];

        /// <summary>
        /// Answers the prompts this battery expects, and reports what it answered.
        ///
        /// Returns the dialogs it did NOT recognise, so the caller can fail with their text. An
        /// empty result means the screen is clear.
        /// </summary>
        public static Dialog[] AnswerExpectedPrompts(AppDriver driver, Action<string> report,
                                                     TimeSpan? waitFor = null)
        {
            List<Dialog> unhandled = [];

            // A prompt raised by an action does not necessarily exist by the time the action
            // returns: PuTTY's host-key alert arrives once the TCP connection is up, well after the
            // double-click that started it. Looking exactly once reports a clear screen and then the
            // test proceeds into a dialog. The wait ends as soon as something appears, so the common
            // no-dialog case only costs the caller's budget when there is genuinely nothing.
            Dialog[] found = Find(driver);
            if (found.Length == 0 && waitFor is { } budget)
            {
                DateTime deadline = DateTime.UtcNow + budget;
                while (found.Length == 0 && DateTime.UtcNow < deadline)
                {
                    System.Threading.Thread.Sleep(300);
                    found = Find(driver);
                }
            }

            // Always say what was on screen. "Nothing was answered" and "nothing was there" are
            // different facts, and telling them apart from the test output is what turned the first
            // guest run from a guess into a measurement.
            report(found.Length == 0
                       ? "no application dialogs on screen"
                       : "dialogs on screen: " + string.Join("; ", found.Select(d => d.ToString())));

            foreach (Dialog dialog in found)
            {
                string[] buttons = ExpectedAnswer(dialog);
                string? pressed = buttons.FirstOrDefault(b => ClickButton(dialog, b));

                if (pressed is null)
                {
                    unhandled.Add(dialog);
                    continue;
                }

                report($"answered the {dialog} prompt with '{pressed}'");
                System.Threading.Thread.Sleep(250);
            }

            // UIA cannot see a dialog that is freezing its own provider, and reports a clear screen
            // instead of failing — so "found nothing" has to be checked a second way before it is
            // believed. This is not redundancy: it is the difference between "nothing there" and
            // "could not look", and getting that wrong cost a scenario that failed as #110.
            if (found.Length == 0)
                unhandled.AddRange(AnswerThroughWin32(driver, report));

            return [.. unhandled];
        }

        /// <summary>
        /// The same pass, done through Win32 window enumeration.
        ///
        /// Returns dialogs it could not answer, described the same way, so a caller cannot tell
        /// which route found a problem — only that one exists.
        /// </summary>
        private static Dialog[] AnswerThroughWin32(AppDriver driver, Action<string> report)
        {
            List<Dialog> unhandled = [];

            Win32Dialogs.Dialog[] found = Win32Dialogs.Find(RelevantProcessIds(driver));
            if (found.Length == 0)
                return [];

            report("dialogs found through Win32 that UIA could not see: "
                   + string.Join("; ", found.Select(d => d.ToString())));

            foreach (Win32Dialogs.Dialog dialog in found)
            {
                // Reuse the same whitelist: the answer must not depend on how the dialog was found.
                Dialog described = new(null, dialog.Title, dialog.Text);
                string[] buttons = ExpectedAnswer(described);
                string? pressed = buttons.FirstOrDefault(b => Win32Dialogs.ClickButton(dialog, b));

                if (pressed is null)
                {
                    unhandled.Add(described);
                    continue;
                }

                report($"answered the {dialog} prompt with '{pressed}' through Win32");
                System.Threading.Thread.Sleep(250);
            }

            return [.. unhandled];
        }

        /// <summary>
        /// The buttons that would answer a prompt which is expected behaviour rather than a defect.
        ///
        /// Several candidates, tried in order, because the caption does not tell you which buttons a
        /// dialog carries: "PuTTY Exit Confirmation" is a confirmation with OK and Cancel, while the
        /// application's own close prompt uses Yes and No. Returning a single name let the first
        /// matching rule decide the button, so the exit rule answered "Yes" to a dialog that had no
        /// Yes button, the click failed, and a perfectly ordinary prompt was reported as
        /// unrecognised.
        ///
        /// Deliberately narrow. "Answer anything with a Yes button" would turn this from a harness
        /// convenience into a way of hiding real dialogs — including the crash dialog this battery
        /// exists to catch. An empty result means "not expected", and the caller fails the test.
        /// </summary>
        private static string[] ExpectedAnswer(Dialog dialog)
        {
            string haystack = (dialog.Title + " " + dialog.Text).ToUpperInvariant();

            // The application asks before closing while connections are open. Confirming is what a
            // user does; refusing would make every close-related assertion untestable.
            if (haystack.Contains("EXIT", StringComparison.Ordinal)
                || haystack.Contains("CLOSE ALL", StringComparison.Ordinal)
                || haystack.Contains("OPEN CONNECTION", StringComparison.Ordinal)
                || haystack.Contains("STILL CONNECTED", StringComparison.Ordinal)
                || haystack.Contains("CLOSE THIS SESSION", StringComparison.Ordinal))
                return ["Yes", "OK"];

            // The RDP client cannot verify the lab's self-signed certificate. The lab is an isolated
            // network with no route to anything real, and the alternative — trusting the certificate
            // machine-wide — would weaken the host to make a test pass, which is not a trade this
            // project makes.
            if (haystack.Contains("CERTIFICATE", StringComparison.Ordinal)
                || haystack.Contains("IDENTITY OF THE REMOTE COMPUTER", StringComparison.Ordinal)
                || haystack.Contains("CANNOT BE VERIFIED", StringComparison.Ordinal))
                return ["Yes", "Connect", "OK"];

            // PuTTY has not seen the lab host's key before.
            //
            // This clicks a button a user would click; it does NOT change how the product validates
            // host keys, and nothing here is written into the product's trust store beyond the
            // throwaway deployment. The alternative — pre-seeding the key into the machine's PuTTY
            // cache — would make the same trust decision less visibly. The host is a lab guest on an
            // isolated network with no route to anything real. Accepting an unknown host key would
            // not be acceptable against any other target, and this must never be widened into the
            // product.
            if (haystack.Contains("HOST KEY IS NOT CACHED", StringComparison.Ordinal))
                return ["Accept"];

            return [];
        }

        private static bool ClickButton(Dialog dialog, string name)
        {
            try
            {
                if (dialog.Element is null)
                    return false;

                AutomationElement? button = dialog.Element
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Button))
                    .FirstOrDefault(b => string.Equals(SafeName(b).Trim(), name,
                                                     StringComparison.OrdinalIgnoreCase));

                if (button is null)
                    return false;

                button.Click();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>The dialog's message, gathered from its static text children.</summary>
        private static string DialogText(AutomationElement window)
        {
            try
            {
                string[] parts = window
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Text))
                    .Select(SafeName)
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .ToArray();
                return string.Join(" ", parts);
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// True only for a genuinely modal window.
        ///
        /// Control type alone is not enough: this application docks its panels with WeifenLuo, and
        /// every docked panel — Config, Connections, the tab host — is also a UIA Window. Matching on
        /// type reported six panels as unrecognised dialogs and failed a test that had nothing wrong
        /// with it. Modality is the property that actually distinguishes "something is blocking the
        /// user" from "this is part of the layout".
        /// </summary>
        private static bool IsModal(AutomationElement window)
        {
            try
            {
                return window.Patterns.Window.TryGetPattern(out var pattern)
                       && pattern.IsModal.TryGetValue(out bool modal)
                       && modal;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// A helper's alert window, matched by title.
        ///
        /// PuTTY's alerts do not necessarily report themselves as modal through UIA, and a battery
        /// that waits for a modality flag would sit behind them forever. Matching exact titles keeps
        /// this from catching the reparented terminal window, which must not be treated as a dialog.
        /// </summary>
        private static bool IsKnownHelperAlert(AutomationElement window)
        {
            string title = SafeName(window);
            return title.Contains("PuTTY Security Alert", StringComparison.Ordinal)
                   || title.Contains("PuTTY Exit Confirmation", StringComparison.Ordinal);
        }

        private static bool IsMainWindow(AutomationElement window)
        {
            try { return string.Equals(window.Properties.AutomationId.ValueOrDefault, "FrmMain",
                                       StringComparison.Ordinal); }
            catch (Exception) { return false; }
        }

        private static int SafeProcessId(AutomationElement e)
        {
            try { return e.Properties.ProcessId.ValueOrDefault; } catch (Exception) { return -1; }
        }

        private static string SafeName(AutomationElement e)
        {
            try { return e.Name ?? ""; } catch (Exception) { return ""; }
        }
    }
}

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
        public sealed record Dialog(AutomationElement Element, string Title, string Text)
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
                int pid = driver.Application.ProcessId;
                return driver.Automation.GetDesktop()
                    .FindAllDescendants(cf => cf.ByControlType(ControlType.Window))
                    .Where(w => SafeProcessId(w) == pid && !IsMainWindow(w) && IsModal(w))
                    .Select(w => new Dialog(w, SafeName(w), DialogText(w)))
                    .ToArray();
            }
            catch (Exception)
            {
                return [];
            }
        }

        /// <summary>
        /// Answers the prompts this battery expects, and reports what it answered.
        ///
        /// Returns the dialogs it did NOT recognise, so the caller can fail with their text. An
        /// empty result means the screen is clear.
        /// </summary>
        public static Dialog[] AnswerExpectedPrompts(AppDriver driver, Action<string> report)
        {
            List<Dialog> unhandled = [];
            Dialog[] found = Find(driver);

            // Always say what was on screen. "Nothing was answered" and "nothing was there" are
            // different facts, and telling them apart from the test output is what turned the first
            // guest run from a guess into a measurement.
            report(found.Length == 0
                       ? "no application dialogs on screen"
                       : "dialogs on screen: " + string.Join("; ", found.Select(d => d.ToString())));

            foreach (Dialog dialog in found)
            {
                string? button = ExpectedAnswer(dialog);
                if (button is null)
                {
                    unhandled.Add(dialog);
                    continue;
                }

                if (ClickButton(dialog, button))
                {
                    report($"answered the {dialog} prompt with '{button}'");
                    System.Threading.Thread.Sleep(250);
                }
                else
                {
                    unhandled.Add(dialog);
                }
            }

            return [.. unhandled];
        }

        /// <summary>
        /// The button to press for a prompt that is expected behaviour rather than a defect.
        ///
        /// Deliberately narrow. "Answer anything with a Yes button" would turn this from a harness
        /// convenience into a way of hiding real dialogs — including the crash dialog this battery
        /// exists to catch.
        /// </summary>
        private static string? ExpectedAnswer(Dialog dialog)
        {
            string haystack = (dialog.Title + " " + dialog.Text).ToUpperInvariant();

            // The application asks before closing while connections are open. Confirming is what a
            // user does; refusing would make every close-related assertion untestable.
            if (haystack.Contains("EXIT", StringComparison.Ordinal)
                || haystack.Contains("CLOSE ALL", StringComparison.Ordinal)
                || haystack.Contains("OPEN CONNECTION", StringComparison.Ordinal)
                || haystack.Contains("STILL CONNECTED", StringComparison.Ordinal))
                return "Yes";

            // The RDP client cannot verify the lab's self-signed certificate. The lab is an isolated
            // network with no route to anything real, and the alternative — trusting the certificate
            // machine-wide — would weaken the host to make a test pass, which is not a trade this
            // project makes.
            if (haystack.Contains("CERTIFICATE", StringComparison.Ordinal)
                || haystack.Contains("IDENTITY OF THE REMOTE COMPUTER", StringComparison.Ordinal)
                || haystack.Contains("CANNOT BE VERIFIED", StringComparison.Ordinal))
                return "Yes";

            return null;
        }

        private static bool ClickButton(Dialog dialog, string name)
        {
            try
            {
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

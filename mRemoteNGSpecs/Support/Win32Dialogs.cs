using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace mRemoteNGSpecs.Support
{
    /// <summary>
    /// Finds and answers dialogs through Win32, for the case UI Automation cannot serve.
    ///
    /// A modal message box freezes the UIA provider of the process that owns it: every call then
    /// blocks until it times out and throws, so a desktop-wide search returns nothing and the
    /// harness concludes the screen is clear while a dialog is holding the keyboard. That is
    /// documented in this repository's own notes as the reason the automation appears to hang, and
    /// it is what made a PuTTY exit confirmation invisible — the scenario failed as "the application
    /// did not exit", which is the #110 symptom, with nothing wrong in the application.
    ///
    /// Win32 window enumeration does not go through the provider and answers regardless. It is used
    /// as a fallback rather than the default because UIA gives richer text; when UIA finds nothing,
    /// this decides whether that means "nothing there" or "could not look".
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class Win32Dialogs
    {
        /// <summary>The window class Windows uses for dialog boxes, including message boxes.</summary>
        private const string DialogClass = "#32770";

        private const int BM_CLICK = 0x00F5;

        private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr parent, EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetWindowText(IntPtr window, [Out] char[] text, int max);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int GetClassName(IntPtr window, [Out] char[] name, int max);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr window, int message, IntPtr wParam, IntPtr lParam);

        /// <summary>A dialog window: its handle, caption and the text of its labels.</summary>
        public sealed record Dialog(IntPtr Handle, string Title, string Text)
        {
            public override string ToString() =>
                string.IsNullOrWhiteSpace(Text) ? $"'{Title}'" : $"'{Title}' — {Text}";
        }

        /// <summary>Every visible dialog belonging to one of the given processes.</summary>
        public static Dialog[] Find(HashSet<int> processIds)
        {
            List<Dialog> dialogs = [];

            EnumWindows((window, _) =>
            {
                try
                {
                    if (!IsWindowVisible(window))
                        return true;

                    if (!string.Equals(ClassOf(window), DialogClass, StringComparison.Ordinal))
                        return true;

                    // A zero thread id means the handle died mid-enumeration.
                    uint thread = GetWindowThreadProcessId(window, out uint pid);
                    if (thread == 0 || !processIds.Contains((int)pid))
                        return true;

                    dialogs.Add(new Dialog(window, TextOf(window), LabelsOf(window)));
                }
                catch (Exception)
                {
                    // A window that disappears mid-enumeration is normal; keep going.
                }

                return true;
            }, IntPtr.Zero);

            return [.. dialogs];
        }

        /// <summary>
        /// Presses a named button in a dialog.
        ///
        /// BM_CLICK is posted to the button directly rather than synthesising a mouse click, so it
        /// works while the dialog holds the input queue and cannot land somewhere else if focus
        /// moves — which matters on a machine where the desktop may be shared.
        /// </summary>
        public static bool ClickButton(Dialog dialog, string name)
        {
            IntPtr target = IntPtr.Zero;

            EnumChildWindows(dialog.Handle, (child, _) =>
            {
                try
                {
                    if (!string.Equals(ClassOf(child), "Button", StringComparison.Ordinal))
                        return true;

                    // Button captions carry the keyboard mnemonic as an ampersand ("&Yes").
                    string caption = TextOf(child).Replace("&", "", StringComparison.Ordinal).Trim();
                    if (string.Equals(caption, name, StringComparison.OrdinalIgnoreCase))
                    {
                        target = child;
                        return false;
                    }
                }
                catch (Exception)
                {
                }

                return true;
            }, IntPtr.Zero);

            if (target == IntPtr.Zero)
                return false;

            SendMessage(target, BM_CLICK, IntPtr.Zero, IntPtr.Zero);
            return true;
        }

        /// <summary>The text of every label in the dialog, which is its message.</summary>
        private static string LabelsOf(IntPtr dialog)
        {
            List<string> parts = [];

            EnumChildWindows(dialog, (child, _) =>
            {
                try
                {
                    if (string.Equals(ClassOf(child), "Static", StringComparison.Ordinal))
                    {
                        string text = TextOf(child).Trim();
                        if (text.Length > 0)
                            parts.Add(text);
                    }
                }
                catch (Exception)
                {
                }

                return true;
            }, IntPtr.Zero);

            return string.Join(" ", parts);
        }

        private static string TextOf(IntPtr window)
        {
            char[] buffer = new char[512];
            int length = GetWindowText(window, buffer, buffer.Length);
            return length > 0 ? new string(buffer, 0, length) : "";
        }

        private static string ClassOf(IntPtr window)
        {
            char[] buffer = new char[256];
            int length = GetClassName(window, buffer, buffer.Length);
            return length > 0 ? new string(buffer, 0, length) : "";
        }
    }
}

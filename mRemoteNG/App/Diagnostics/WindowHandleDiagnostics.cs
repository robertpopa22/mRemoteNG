using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Windows.Forms;

namespace mRemoteNG.App.Diagnostics
{
    /// <summary>
    /// Evidence for "Error creating window handle" crash reports (#174, #183, #197).
    ///
    /// Those reports all end in the same stock WinForms frames: a DPI change arrives
    /// (WM_DPICHANGED_BEFOREPARENT), a combo box rescales its font, recreates its window, and
    /// Windows refuses to create it. The stack cannot say which control it was, whether the
    /// process was near its window or GDI object limits, what DPI the app was at, or which
    /// WinForms build the machine runs (a WmDpiChangedBeforeParent regression was fixed upstream
    /// in dotnet/winforms#14454, and this app does not pin a runtime patch level). This collects
    /// exactly those facts, once, at the moment the exception is reported; collecting them changes
    /// nothing. <see cref="RecreateLostComboBoxes"/> is the one repair (#209), run by the main window
    /// after a DPI change.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public static class WindowHandleDiagnostics
    {
        private const uint GR_GDIOBJECTS = 0;
        private const uint GR_USEROBJECTS = 1;
        private const uint GR_GDIOBJECTS_PEAK = 2;
        private const uint GR_USEROBJECTS_PEAK = 4;
        private const int MaxControlsListed = 10;

        [DllImport("user32.dll")]
        private static extern uint GetGuiResources(IntPtr hProcess, uint uiFlags);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>The runtime line every crash report carries: which .NET and which WinForms build.</summary>
        public static string RuntimeLine() =>
            string.Create(CultureInfo.InvariantCulture,
                $".NET: {RuntimeInformation.FrameworkDescription}; WinForms {FileVersionOf(typeof(Form))}");

        /// <summary>True for the window-creation failures this class exists to explain.</summary>
        public static bool IsWindowCreationFailure(Exception? exception)
        {
            for (Exception? e = exception; e != null; e = e.InnerException)
            {
                if (e is Win32Exception && (e.StackTrace?.Contains("CreateHandle", StringComparison.Ordinal) ?? false))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// A block of text for the crash report, or an empty string when the exception is not a
        /// window-creation failure. Never throws: a diagnostic that crashes the crash reporter is
        /// worse than no diagnostic.
        /// </summary>
        public static string Describe(Exception? exception, IEnumerable<Form>? openForms = null)
        {
            if (!IsWindowCreationFailure(exception))
                return string.Empty;

            StringBuilder sb = new();
            sb.AppendLine("Window handle diagnostics:");
            // #209: the Win32 error is what tells a refused child window (bad parent, quota)
            // apart from anything else; the message text alone is always the same.
            for (Exception? e = exception; e != null; e = e.InnerException)
            {
                if (e is Win32Exception win32)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  Win32 error: {win32.NativeErrorCode} (0x{win32.NativeErrorCode:X})");
                    break;
                }
            }

            try
            {
                IntPtr process = GetCurrentProcess();
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"  USER objects: {GetGuiResources(process, GR_USEROBJECTS)} (peak {GetGuiResources(process, GR_USEROBJECTS_PEAK)})");
                sb.AppendLine(CultureInfo.InvariantCulture,
                    $"  GDI objects: {GetGuiResources(process, GR_GDIOBJECTS)} (peak {GetGuiResources(process, GR_GDIOBJECTS_PEAK)})");
            }
            catch (Exception ex)
            {
                sb.AppendLine("  object counts unavailable: " + ex.GetType().Name);
            }

            try
            {
                List<Form> forms = (openForms ?? Application.OpenForms.Cast<Form>()).ToList();
                foreach (Form form in forms.Where(f => !f.IsDisposed).Take(5))
                    sb.AppendLine(CultureInfo.InvariantCulture,
                        $"  form {form.GetType().Name}: DeviceDpi {form.DeviceDpi}, handle {(form.IsHandleCreated ? "yes" : "no")}");

                List<string> lost = FindControlsWithoutHandle(forms).Take(MaxControlsListed).ToList();
                sb.AppendLine(lost.Count == 0
                    ? "  no control is missing its window"
                    : "  controls whose window is gone (a failed recreation leaves them so):");
                foreach (string entry in lost)
                    sb.AppendLine("    " + entry);
            }
            catch (Exception ex)
            {
                sb.AppendLine("  control scan unavailable: " + ex.GetType().Name);
            }

            return sb.ToString();
        }

        /// <summary>
        /// What RecreateHandleCore leaves behind when CreateHandle throws: a control with no window
        /// under a parent that still has one. Once the window is gone WinForms also reports the
        /// control as not Visible (checked in WindowHandleDiagnosticsTests), so Visible cannot pick
        /// out the victim. Combo boxes are therefore listed whenever they lack a window, since that
        /// is the control class in every report of this crash; any other control only if it still
        /// claims to be visible, so controls that were simply never shown do not drown the list.
        /// ToolStrip-hosted controls (the Quick Connect combo box) are reached through their host.
        /// </summary>
        internal static IEnumerable<string> FindControlsWithoutHandle(IEnumerable<Form> forms) =>
            FindLostControls(forms).Select(PathOf);

        internal static IEnumerable<Control> FindLostControls(IEnumerable<Form> forms)
        {
            HashSet<Control> seen = [];
            foreach (Form form in forms)
            {
                if (form.IsDisposed || !form.IsHandleCreated)
                    continue;

                foreach (Control control in Descendants(form))
                {
                    if (!seen.Add(control))
                        continue;
                    if (control.IsDisposed || control.IsHandleCreated)
                        continue;
                    if (control is not ComboBox && !control.Visible)
                        continue;
                    if (control.Parent is { IsHandleCreated: true })
                        yield return control;
                }
            }
        }

        private static IEnumerable<Control> Descendants(Control root)
        {
            Stack<Control> pending = new();
            pending.Push(root);
            while (pending.Count > 0)
            {
                Control current = pending.Pop();
                foreach (Control child in current.Controls)
                {
                    yield return child;
                    pending.Push(child);
                    if (child is ToolStrip strip)
                    {
                        foreach (ToolStripItem item in strip.Items)
                        {
                            if (item is ToolStripControlHost { Control: { } hosted })
                            {
                                yield return hosted;
                                pending.Push(hosted);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// #209: gives a combo box its window back after a DPI change refused to recreate it.
        /// ComboBox.OnFontChanged recreates the window whenever AutoCompleteMode is not None (the
        /// Quick Connect box uses SuggestAppend), and that recreation runs inside
        /// WM_DPICHANGED_BEFOREPARENT, where Windows has refused it. Called once the DPI change has
        /// completed. Returns one line per combo box tried.
        /// </summary>
        internal static IReadOnlyList<string> RecreateLostComboBoxes(IEnumerable<Form> forms)
        {
            List<string> results = [];
            foreach (ComboBox combo in FindLostControls(forms).OfType<ComboBox>().ToList())
            {
                string path = PathOf(combo);
                try
                {
                    _ = combo.Handle;
                    // The lost window also took the visible state with it, and the ToolStrip then laid
                    // its item out as unavailable. The only hosted combo box (Quick Connect) is shown
                    // whenever its strip is, so the strip's visibility is the intent to restore.
                    if (combo.Parent is ToolStrip { Visible: true } strip)
                    {
                        ToolStripControlHost? host = strip.Items.OfType<ToolStripControlHost>()
                            .FirstOrDefault(h => ReferenceEquals(h.Control, combo));
                        if (host != null)
                            host.Available = true;
                        combo.Visible = true;
                    }
                    results.Add($"{path}: window recreated");
                }
                catch (Win32Exception ex)
                {
                    results.Add(string.Create(CultureInfo.InvariantCulture, $"{path}: still refused, Win32 error {ex.NativeErrorCode}"));
                }
            }

            return results;
        }

        private static string PathOf(Control control)
        {
            List<string> path = [];
            for (Control? c = control; c != null && path.Count < 6; c = c.Parent)
                path.Add(string.IsNullOrEmpty(c.Name) ? c.GetType().Name : $"{c.GetType().Name}:{c.Name}");
            path.Reverse();
            return string.Join(" > ", path);
        }

        private static string FileVersionOf(Type type)
        {
            try
            {
                string location = type.Assembly.Location;
                return string.IsNullOrEmpty(location)
                    ? type.Assembly.GetName().Version?.ToString() ?? "?"
                    : System.Diagnostics.FileVersionInfo.GetVersionInfo(location).FileVersion ?? "?";
            }
            catch (Exception)
            {
                return "?";
            }
        }
    }
}

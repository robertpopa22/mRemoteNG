using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.Capturing;
using mRemoteNG.App;
using mRemoteNG.Config;
using mRemoteNG.Connection;
using mRemoteNG.Container;
using mRemoteNG.UI;
using mRemoteNG.UI.Forms;
using mRemoteNG.UI.Forms.OptionsPages;
using mRemoteNG.UI.Window;
using mRemoteNGSpecs.Support;
using NUnit.Framework;
using WeifenLuo.WinFormsUI.Docking;

namespace mRemoteNGSpecs.Fixtures;

/// <summary>Rendered product classes with synthetic data, distinct from real-executable navigation captures.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class FormGalleryAuditTests
{
    private static readonly JsonSerializerOptions IndentedJson = new() { WriteIndented = true };
    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

    private sealed class DpiScope : IDisposable
    {
        private readonly IntPtr _original = SetThreadDpiAwarenessContext(new IntPtr(-4));
        public void Dispose() => SetThreadDpiAwarenessContext(_original);
    }
    private string _directory = null!;
    private readonly ConnectionInfo _connection = new() { Name = "Synthetic connection", Hostname = "127.0.0.1", Username = "test-user" };
    private readonly ContainerInfo _container = new() { Name = "Synthetic folder" };

    [Test]
    public void CaptureEveryProductFormAndPageWithSyntheticData()
        => CaptureTypes(null);

    [Test]
    public void CriticalInputRowsRemainReachableAfterChangingFont()
        => CaptureTypes(new[] { typeof(FrmChoosePanel), typeof(FrmConnectWithCredentials), typeof(FrmExport),
            typeof(mRemoteNG.UI.TaskDialog.frmTaskDialog), typeof(ExternalToolsWindow), typeof(FrmConnectionPresets),
            typeof(SecurityPage), typeof(PortScanWindow), typeof(ThemePage) });

    private void CaptureTypes(Type[]? selectedTypes)
    {
        VisualAuditEnvironment.Initialize();
        using var dpiScope = new DpiScope();
        _directory = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", selectedTypes is null ? "FormGalleryAudit" : "FormGalleryAudit-Critical");
        Directory.CreateDirectory(_directory);
        using (Graphics desktop = Graphics.FromHwnd(IntPtr.Zero))
            File.WriteAllText(Path.Combine(_directory, "dpi-bootstrap.txt"), $"Applied={VisualAuditEnvironment.DpiModeApplied}; desktopDpi={desktop.DpiY}; defaultFontHeight={SystemFonts.DefaultFont.Height}");
        _container.AddChild(_connection);
        Runtime.WindowList = new WindowList();
        var rows = new List<object>();
        var failures = new List<string>();
        var types = typeof(FrmOptions).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && t != typeof(ResponsiveForm) && t != typeof(OptionsPage) && t != typeof(BaseWindow)
                && (typeof(Form).IsAssignableFrom(t) || typeof(OptionsPage).IsAssignableFrom(t)))
            .Where(t => selectedTypes is null || selectedTypes.Contains(t))
            .OrderBy(t => t.FullName, StringComparer.Ordinal).ToArray();
        foreach (Type type in types)
        {
            File.AppendAllText(Path.Combine(_directory, "progress.txt"), type.FullName + " start" + Environment.NewLine);
            if (type == typeof(FrmMain) || type == typeof(FrmOptions) || type == typeof(OptionsWindow) || type == typeof(ConnectionTreeWindow) || type == typeof(ConfigWindow))
            {
                rows.Add(new { name = type.FullName, state = "real executable capture", file = "VisualLayoutAudit" });
                continue;
            }
            Control? control = null;
            Form? form = null;
            try
            {
                control = (Control)Create(type);
                File.AppendAllText(Path.Combine(_directory, "progress.txt"), type.FullName + " created" + Environment.NewLine);
                if (control is OptionsPage page)
                {
                    page.ApplyLanguage();
                    page.LoadSettings();
                    AdaptiveLayout.Prepare(page);
                    form = new Form { Text = type.Name, ClientSize = new Size(800, 580), StartPosition = FormStartPosition.Manual, Location = Point.Empty };
                    page.Dock = DockStyle.Fill;
                    form.Controls.Add(page);
                }
                else
                    form = (Form)control;
                if (type == typeof(mRemoteNG.UI.TaskDialog.frmTaskDialog))
                {
                    // Match CTaskDialog.ShowTaskDialogBox, which supplies a logical
                    // emulated width before BuildForm rather than the designer preview width.
                    form.Width = mRemoteNG.UI.TaskDialog.CTaskDialog.EmulatedFormWidth;
                    type.GetProperty("MainInstruction")!.SetValue(form, "Synthetic confirmation with a long instruction to test wrapping.");
                    type.GetProperty("Content")!.SetValue(form, "This is a visual audit. No connection, credential, database or external message is used.");
                    type.GetMethod("BuildForm")!.Invoke(form, null);
                }
                form.StartPosition = FormStartPosition.Manual;
                form.Location = Point.Empty;
                form.TopMost = true;
                form.Show();
                File.AppendAllText(Path.Combine(_directory, "progress.txt"), type.FullName + " shown" + Environment.NewLine);
                form.Activate();
                if (control is SqlServerPage sqlPage)
                {
                    sqlPage.Controls.Find("chkUseSQLServer", true).OfType<CheckBox>().Single().Checked = true;
                    sqlPage.Controls.Find("btnExpandOptions", true).OfType<Button>().Single().PerformClick();
                }
                Application.DoEvents();
                form.Refresh();
                string file = type.FullName!.Replace('.', '-') + ".png";
                CaptureStable(form, Path.Combine(_directory, file));
                CheckFontMetrics(control, failures);
                File.AppendAllText(Path.Combine(_directory, "progress.txt"), type.FullName + " captured" + Environment.NewLine);
                foreach (TabControl tabs in Descendants(form).OfType<TabControl>())
                {
                    foreach (TabPage tab in tabs.TabPages)
                    {
                        tabs.SelectedTab = tab;
                        CaptureStable(form, Path.Combine(_directory, type.FullName.Replace('.', '-') + "-tab-" + tab.Name + ".png"));
                    }
                }
                var bounds = form.Bounds;
                string border = form.FormBorderStyle.ToString();
                File.WriteAllText(Path.Combine(_directory, type.FullName.Replace('.', '-') + "-layout.json"),
                    JsonSerializer.Serialize(Descendants(form).Where(c => c.Visible).Select(c => new {
                        type = c.GetType().Name, c.Name, parent = c.Parent?.Name, bounds = c.Bounds.ToString(),
                        font = c.Font.SizeInPoints, dpi = c.DeviceDpi, maximum = c.MaximumSize.ToString(), minimum = c.MinimumSize.ToString(),
                        fontUnit = c.Font.Unit.ToString(), fontSize = c.Font.Size, fontHeight = c.Font.Height, fontDeviceHeight = c.Font.GetHeight(c.DeviceDpi),
                        compatibleText = c is Label label ? label.UseCompatibleTextRendering : c is ButtonBase button ? button.UseCompatibleTextRendering : (bool?)null,
                        caption = c is Label or Button or CheckBox or RadioButton ? c.Text : "",
                        columns = c is TableLayoutPanel table ? table.GetColumnWidths() : null
                    }), IndentedJson));
                var clipped = new List<string>();
                Inspect(control, clipped);
                if (form.FormBorderStyle is FormBorderStyle.Sizable or FormBorderStyle.SizableToolWindow)
                {
                    form.Size = new Size(Math.Min(600, bounds.Width), Math.Min(460, bounds.Height));
                    Application.DoEvents();
                    form.Refresh();
                    CaptureStable(form, Path.Combine(_directory, type.FullName.Replace('.', '-') + "-small.png"));
                    using var largeFont = new Font(form.Font.FontFamily, 14, form.Font.Style);
                    mRemoteNG.Themes.InterfaceFont.Apply(form, largeFont);
                    CaptureStable(form, Path.Combine(_directory, type.FullName.Replace('.', '-') + "-large-font.png"));
                    CheckFontMetrics(control, failures);
                    if (control is ThemePage)
                    {
                        using var maximumFont = new Font(form.Font.FontFamily, 24, form.Font.Style);
                        mRemoteNG.Themes.InterfaceFont.Apply(form, maximumFont);
                        CaptureStable(form, Path.Combine(_directory, type.FullName.Replace('.', '-') + "-maximum-font.png"));
                        CheckFontMetrics(control, failures);
                        File.WriteAllText(Path.Combine(_directory, type.FullName.Replace('.', '-') + "-maximum-font-layout.json"),
                            JsonSerializer.Serialize(Descendants(form).Where(c => c.Visible).Select(c => new {
                                type=c.GetType().Name, c.Name, parent=c.Parent?.Name, bounds=c.Bounds.ToString(),
                                preferred=c.GetPreferredSize(new Size(c.Width,0)).ToString(), c.AutoSize,
                                maximum=c.MaximumSize.ToString(), minimum=c.MinimumSize.ToString(), font=c.Font.SizeInPoints,
                                columns=c is TableLayoutPanel table ? table.GetColumnWidths() : null,
                                rows=c is TableLayoutPanel rowTable ? rowTable.GetRowHeights() : null
                            }), IndentedJson));
                    }
                    var unreachable = new List<string>();
                    CheckReachability(control, unreachable);
                    if (unreachable.Count > 0)
                        File.WriteAllText(Path.Combine(_directory, type.FullName.Replace('.', '-') + "-unreachable.json"),
                            JsonSerializer.Serialize(Descendants(form).Where(c => c.Visible).Select(c => new {
                                type = c.GetType().Name, c.Name, parent = c.Parent?.Name, bounds = c.Bounds.ToString(),
                                client = c.ClientRectangle.ToString(), preferred = c.GetPreferredSize(Size.Empty).ToString(),
                                minimum = c.MinimumSize.ToString(),
                                columns = c is TableLayoutPanel table ? table.GetColumnWidths() : null,
                                rows = c is TableLayoutPanel rowsTable ? rowsTable.GetRowHeights() : null
                            }), IndentedJson));
                    if (unreachable.Count > 0)
                        failures.Add(type.FullName + ": " + string.Join("; ", unreachable));
                }
                rows.Add(new { name = type.FullName, state = "captured", file, border, dpi = form.DeviceDpi, bounds = bounds.ToString(), clipped });
            }
            catch (Exception ex)
            {
                rows.Add(new { name = type.FullName, state = "capture failed", error = ex.GetBaseException().Message });
                failures.Add($"{type.FullName}: {ex.GetBaseException().Message}");
            }
            finally
            {
                form?.Dispose();
                control?.Dispose();
                Application.DoEvents();
            }
            File.WriteAllText(Path.Combine(_directory, "inventory.json"), JsonSerializer.Serialize(rows, IndentedJson));
        }
        Assert.That(rows.Count, Is.EqualTo(types.Length), "every discovered product form/page has a disposition");
        Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
    }

    private object Create(Type type)
    {
        if (type == typeof(Form)) return new Form();
        if (type == typeof(ConnectionPresetService)) return new ConnectionPresetService(Path.Combine(_directory, "synthetic-presets.xml"));
        if (type == typeof(ConnectionInfo)) return _connection;
        if (type == typeof(ContainerInfo)) return _container;
        if (type == typeof(DockContent)) return new DockContent();
        if (type == typeof(mRemoteNG.UI.Tabs.FloatWindowNG))
        {
            var panel = new DockPanel { Theme = new VS2015LightTheme(), Size = new Size(600, 420) };
            var content = new DockContent { Text = "Synthetic floating panel" };
            content.Show(panel, DockState.Document);
            return Activator.CreateInstance(type, panel, content.DockHandler.Pane)!;
        }
        if (type == typeof(FullscreenHandler)) return new FullscreenHandler(new Form());
        if (type == typeof(ConnectionsFileResolver.Candidate)) return new ConnectionsFileResolver.Candidate("synthetic-confCons.xml", DateTime.UtcNow, 1024, "Synthetic profile");
        if (type == typeof(bool)) return false;
        if (type == typeof(string)) return "Synthetic value";
        if (type == typeof(Exception)) return new InvalidOperationException("Synthetic failure for visual audit");
        if (type.IsGenericType && type.GetGenericArguments().Length == 1 && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            Type itemType = type.GetGenericArguments()[0];
            Array items = Array.CreateInstance(itemType, 1);
            items.SetValue(Create(itemType), 0);
            return items;
        }
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderBy(c => c.GetParameters().Length).FirstOrDefault() ?? throw new NotSupportedException(type.FullName);
        object?[] arguments = constructor.GetParameters().Select(p => p.HasDefaultValue ? p.DefaultValue : Create(p.ParameterType)).ToArray();
        return constructor.Invoke(arguments);
    }

    private static void CaptureStable(Form form, string path)
    {
        string previous = "";
        int stable = 0;
        UiWait.Until(() =>
        {
            Application.DoEvents();
            form.Refresh();
            using var capture = Capture.Rectangle(form.Bounds);
            capture.ToFile(path);
            string hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            stable = string.Equals(hash, previous, StringComparison.Ordinal) ? stable + 1 : 0;
            previous = hash;
            return stable >= 2;
        }, "the captured window to finish painting", TimeSpan.FromSeconds(5));
    }

    private static void Inspect(Control control, List<string> clipped)
    {
        foreach (Control child in control.Controls)
        {
            if (!child.Visible) continue;
            if (control is not ScrollableControl { AutoScroll: true } &&
                (child.Left < 0 || child.Top < 0 || child.Right > control.ClientSize.Width + 2 || child.Bottom > control.ClientSize.Height + 2))
                clipped.Add($"{control.Name}/{child.Name}: {child.Bounds} outside {control.ClientRectangle}");
            Inspect(child, clipped);
        }
    }

    private static void CheckFontMetrics(Control root, List<string> failures)
    {
        foreach (Control control in Descendants(root).Where(c => c.Visible))
        {
            if (control.GetType().Name != "PropertyGridView" && control is not (ComboBox or ButtonBase)) continue;
            if (control is Button && (string.IsNullOrEmpty(control.Text) || control.Parent is NumericUpDown)) continue;
            // Symbol-font buttons draw icons; the "Ag" line box is not their ink.
            if (control.Font.FontFamily.Name == "Marlett") continue;
            using Graphics graphics = control.CreateGraphics();
            int glyphHeight = TextRenderer.MeasureText(graphics, "Ag", control.Font, Size.Empty, TextFormatFlags.SingleLine).Height;
            if (control is mRemoteNG.UI.Controls.MrngButton or mRemoteNG.UI.Controls.MrngCheckBox or mRemoteNG.UI.Controls.MrngRadioButton)
            {
                int offset = control is mRemoteNG.UI.Controls.MrngCheckBox ? 11 * control.DeviceDpi / 96 + 2
                    : control is mRemoteNG.UI.Controls.MrngRadioButton ? 16 * control.DeviceDpi / 96 : 0;
                glyphHeight = TextRenderer.MeasureText(graphics, control.Text, control.Font,
                    new Size(Math.Max(1, control.ClientSize.Width - offset), 32767), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height;
            }
            if (control is ButtonBase)
            {
                if (control.ClientSize.Height + 2 < glyphHeight)
                    failures.Add($"{root.GetType().Name}/{control.Name}: button height {control.Height} clips glyph height {glyphHeight}");
            }
            else if (control is ComboBox)
            {
                int padding = control is mRemoteNG.UI.Controls.MrngComboBox ? 4 * control.DeviceDpi / 96 : 0;
                if (control.ClientSize.Height < glyphHeight + padding)
                    failures.Add($"{root.GetType().Name}/{control.Name}: combo height {control.Height} clips glyph height {glyphHeight}");
            }
            else
            {
                int rowHeight = (int)control.GetType().GetProperty("GridEntryHeight", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!.GetValue(control)!;
                if (rowHeight < glyphHeight)
                    failures.Add($"{root.GetType().Name}: property grid row {rowHeight} clips glyph height {glyphHeight}");
            }
        }
    }

    private static void CheckReachability(Control root, List<string> failures)
    {
        foreach (Control child in root.Controls)
        {
            if (!child.Visible) continue;
            if (root is NumericUpDown) continue;
            if (child is ButtonBase or ComboBox or NumericUpDown or TextBox { Multiline: false })
            {
                for (Control? parent = child.Parent; parent is not null; parent = parent.Parent)
                    if (parent is ScrollableControl { AutoScroll: true } scroll)
                        scroll.ScrollControlIntoView(child);
                Application.DoEvents();
                Rectangle bounds = child.RectangleToScreen(child.ClientRectangle);
                if (child is CheckBox or RadioButton)
                {
                    int glyph = 16 * child.DeviceDpi / 96;
                    bounds = child.RectangleToScreen(new Rectangle(0, Math.Max(0, (child.Height - glyph) / 2), glyph, Math.Min(glyph, child.Height)));
                }
                for (Control? parent = child.Parent; parent is not null; parent = parent.Parent)
                {
                    Rectangle viewport = parent.RectangleToScreen(parent.ClientRectangle);
                    viewport.Inflate(2, 2);
                    if (!viewport.Contains(bounds))
                    {
                        failures.Add($"{child.GetType().Name} {child.Name} {child.Text}: {bounds} outside {parent.Name} {viewport}");
                        break;
                    }
                }
            }
            CheckReachability(child, failures);
        }
    }

    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (Control nested in Descendants(child)) yield return nested;
        }
    }
}

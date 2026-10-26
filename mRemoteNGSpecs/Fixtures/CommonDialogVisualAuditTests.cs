using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows.Forms;
using FlaUI.Core.Capturing;
using NUnit.Framework;

namespace mRemoteNGSpecs.Fixtures;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
[SupportedOSPlatform("windows")]
public class CommonDialogVisualAuditTests
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out NativeRectangle rectangle);
    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle { public int Left, Top, Right, Bottom; }

    [Test]
    public void CaptureAndCancelSystemFileFontAndColorPickers()
    {
        mRemoteNGSpecs.Support.VisualAuditEnvironment.Initialize();
        string directory = Path.Combine(AppContext.BaseDirectory, "_uiscenarios", "_evidence", "CommonDialogVisualAudit");
        Directory.CreateDirectory(directory);
        using var owner = new Form { Text = "Synthetic dialog owner", Size = new Size(500, 300) };
        owner.Show();
        owner.Activate();
        using var open = new OpenFileDialog { Title = "Open synthetic connections", InitialDirectory = directory, Filter = "XML files|*.xml" };
        using var save = new SaveFileDialog { Title = "Save synthetic connections", InitialDirectory = directory, FileName = "synthetic-connections.xml", Filter = "XML files|*.xml" };
        using var font = new FontDialog { Font = new Font("Segoe UI", 14), ShowEffects = false };
        using var color = new ColorDialog { FullOpen = true, Color = Color.SteelBlue };
        CaptureAndCancel(open, owner, Path.Combine(directory, "open-file.png"));
        CaptureAndCancel(save, owner, Path.Combine(directory, "save-file.png"));
        CaptureAndCancel(font, owner, Path.Combine(directory, "font.png"));
        CaptureAndCancel(color, owner, Path.Combine(directory, "color.png"));
        Assert.That(File.Exists(Path.Combine(directory, "synthetic-connections.xml")), Is.False, "Cancel must not write a profile");
    }

    private static void CaptureAndCancel(CommonDialog dialog, Form owner, string path)
    {
        bool captured = false;
        Exception? error = null;
        using var timer = new System.Windows.Forms.Timer { Interval = 750 };
        timer.Tick += (_, _) =>
        {
            IntPtr window = GetForegroundWindow();
            if (GetWindowThreadProcessId(window, out uint process) == 0) return;
            if (process != Environment.ProcessId || window == owner.Handle) return;
            timer.Stop();
            try
            {
                if (!GetWindowRect(window, out var rectangle)) throw new InvalidOperationException("No picker bounds");
                using var capture = Capture.Rectangle(Rectangle.FromLTRB(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom));
                capture.ToFile(path);
                captured = true;
            }
            catch (Exception ex) { error = ex; }
            finally { PostMessage(window, 0x0010, IntPtr.Zero, IntPtr.Zero); }
        };
        timer.Start();
        DialogResult result = dialog.ShowDialog(owner);
        if (error is not null) throw error;
        Assert.Multiple(() =>
        {
            Assert.That(captured, Is.True, path);
            Assert.That(result, Is.EqualTo(DialogResult.Cancel));
        });
    }
}
